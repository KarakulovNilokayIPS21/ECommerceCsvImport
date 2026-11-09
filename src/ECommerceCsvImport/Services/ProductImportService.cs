using System.Globalization;
using System.Text;
using ECommerceCsvImport.Models;
using static ECommerceCsvImport.Services.ImportConstants;

namespace ECommerceCsvImport.Services;

public sealed class ProductImportService(IProductRepository repository) : IProductImportService
{
    private const char DotSeparator = '.';
    private const char CommaSeparator = ',';

    private readonly Lock _importLock = new();

    public ImportResult ImportFromCsv(string filePath)
    {
        var path = filePath?.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return CreateFailure(ImportMessages.FileNotFound);
        }

        try
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            return ImportFromLines(lines);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return CreateFailure(ImportMessages.FileNotFound);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return CreateFailure(ImportMessages.FileReadFailed);
        }
    }

    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var result = new ImportResult();

        lock (_importLock)
        {
            ProcessLines(lines, result);
        }

        return result;
    }

    private void ProcessLines(IEnumerable<string> lines, ImportResult result)
    {
        Dictionary<string, int>? columns = null;
        var headerFieldCount = 0;
        var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowNumber = 0;

        foreach (var line in lines)
        {
            rowNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (columns is null)
            {
                var headerFields = CsvLineParser.Parse(line.TrimStart(ImportDefaults.ByteOrderMark));
                columns = BuildColumnMap(headerFields);
                headerFieldCount = headerFields.Count;

                if (!CsvColumns.Required.All(columns.ContainsKey))
                {
                    result.Errors.Add(new ImportError(rowNumber, ImportMessages.InvalidHeaders));
                    return;
                }

                continue;
            }

            result.TotalRows++;
            ProcessRow(line, rowNumber, columns, headerFieldCount, importedSkus, result);
        }

        if (result.TotalRows == 0 && result.ErrorCount == 0)
        {
            result.Errors.Add(new ImportError(ImportDefaults.NoRowNumber, ImportMessages.NoData));
        }
    }

    private void ProcessRow(
        string line,
        int rowNumber,
        Dictionary<string, int> columns,
        int headerFieldCount,
        HashSet<string> importedSkus,
        ImportResult result)
    {
        var fields = CsvLineParser.Parse(line);

        if (fields.Count != headerFieldCount)
        {
            result.Errors.Add(new ImportError(rowNumber, ImportMessages.WrongFieldCount));
            return;
        }

        var errors = new List<string>();

        var sku = GetField(fields, columns, CsvColumns.Sku);
        if (sku.Length == 0)
        {
            errors.Add(ImportMessages.EmptySku);
        }

        var name = GetField(fields, columns, CsvColumns.Name);
        if (name.Length == 0)
        {
            errors.Add(ImportMessages.EmptyName);
        }

        if (!TryParsePrice(GetField(fields, columns, CsvColumns.Price), out var price))
        {
            errors.Add(ImportMessages.InvalidPrice);
        }

        if (!TryParseStock(GetField(fields, columns, CsvColumns.Stock), out var stock))
        {
            errors.Add(ImportMessages.InvalidStock);
        }

        if (!TryParseIsActive(GetField(fields, columns, CsvColumns.IsActive), out var isActive))
        {
            errors.Add(ImportMessages.InvalidIsActive);
        }

        var brand = GetField(fields, columns, CsvColumns.Brand);
        if (brand.Length == 0)
        {
            errors.Add(ImportMessages.EmptyBrand);
        }

        if (!TryParseWarranty(GetField(fields, columns, CsvColumns.WarrantyMonths), out var warrantyMonths))
        {
            errors.Add(ImportMessages.InvalidWarranty);
        }

        if (errors.Count > 0)
        {
            result.Errors.AddRange(errors.Select(message => new ImportError(rowNumber, message)));
            return;
        }

        if (repository.GetBySku(sku) is not null || importedSkus.Contains(sku))
        {
            result.Errors.Add(new ImportError(rowNumber, ImportMessages.DuplicateSku));
            return;
        }

        var currency = GetField(fields, columns, CsvColumns.Currency);
        var categoryName = GetField(fields, columns, CsvColumns.CategoryName);
        var modelNumber = columns.ContainsKey(CsvColumns.ModelNumber)
            ? GetField(fields, columns, CsvColumns.ModelNumber)
            : string.Empty;

        repository.Add(new Product
        {
            Sku = sku,
            Name = name,
            Description = GetField(fields, columns, CsvColumns.Description),
            Price = price,
            Currency = currency.Length == 0 ? ImportDefaults.Currency : currency,
            CategoryName = categoryName.Length == 0 ? ImportDefaults.CategoryName : categoryName,
            StockQuantity = stock,
            ImageUrl = GetField(fields, columns, CsvColumns.ImageUrl),
            IsActive = isActive,
            Brand = brand,
            WarrantyMonths = warrantyMonths,
            ModelNumber = modelNumber.Length == 0 ? null : modelNumber
        });

        importedSkus.Add(sku);
        result.ImportedCount++;
    }

    private static ImportResult CreateFailure(string message)
    {
        var result = new ImportResult();
        result.Errors.Add(new ImportError(ImportDefaults.NoRowNumber, message));
        return result;
    }

    private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string> headerFields)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < headerFields.Count; index++)
        {
            columns.TryAdd(headerFields[index], index);
        }

        return columns;
    }

    private static string GetField(IReadOnlyList<string> fields, Dictionary<string, int> columns, string column) =>
        fields[columns[column]];

    private static bool TryParsePrice(string value, out decimal price)
    {
        price = 0;

        var compact = string.Concat(value.Where(symbol => !char.IsWhiteSpace(symbol)));
        if (compact.Length == 0)
        {
            return false;
        }

        var normalized = NormalizeDecimalSeparator(compact);

        return decimal.TryParse(
                   normalized,
                   NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out price)
               && price > 0;
    }

    private static string NormalizeDecimalSeparator(string value)
    {
        var lastDot = value.LastIndexOf(DotSeparator);
        var lastComma = value.LastIndexOf(CommaSeparator);

        if (lastDot < 0 && lastComma < 0)
        {
            return value;
        }

        if (lastDot >= 0 && lastComma >= 0)
        {
            var decimalSeparator = lastDot > lastComma ? DotSeparator : CommaSeparator;
            var thousandsSeparator = decimalSeparator == DotSeparator ? CommaSeparator : DotSeparator;

            return value
                .Replace(thousandsSeparator.ToString(), string.Empty)
                .Replace(decimalSeparator, DotSeparator);
        }

        var separator = lastDot >= 0 ? DotSeparator : CommaSeparator;
        var separatorCount = value.Count(symbol => symbol == separator);

        return separatorCount > 1
            ? value.Replace(separator.ToString(), string.Empty)
            : value.Replace(separator, DotSeparator);
    }

    private static bool TryParseStock(string value, out int stock) =>
        int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out stock)
        && stock >= ImportDefaults.MinStock;

    private static bool TryParseWarranty(string value, out int warrantyMonths) =>
        int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out warrantyMonths)
        && warrantyMonths is >= ImportDefaults.MinWarrantyMonths and <= ImportDefaults.MaxWarrantyMonths;

    private static bool TryParseIsActive(string value, out bool isActive)
    {
        bool? parsed = value.ToLowerInvariant() switch
        {
            "true" or "1" => true,
            "false" or "0" => false,
            _ => null
        };

        isActive = parsed ?? false;
        return parsed.HasValue;
    }
}
