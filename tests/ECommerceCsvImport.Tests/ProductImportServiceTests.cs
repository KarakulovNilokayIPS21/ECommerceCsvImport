using System.Text;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests;

public class ProductImportServiceTests
{
    private const string Header =
        "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

    private static string Row(
        string sku = "EL-001",
        string name = "Смартфон",
        string price = "100",
        string currency = "RUB",
        string category = "Смартфоны",
        string stock = "5",
        string isActive = "true",
        string brand = "Samsung",
        string warranty = "12",
        string model = "SM-1") =>
        $"{sku},{name},Описание,{price},{currency},{category},{stock},img.jpg,{isActive},{brand},{warranty},{model}";

    private static (ProductImportService Service, InMemoryProductRepository Repository) CreateService()
    {
        var repository = new InMemoryProductRepository();
        return (new ProductImportService(repository), repository);
    }

    private static void AssertSingleError(ImportResult result, int rowNumber, string message)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(rowNumber, error.RowNumber);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void ImportFromLines_SingleValidRow_ImportsProduct()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, Row()]);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.ErrorCount);
        var product = Assert.Single(repository.GetAll());
        Assert.Equal("EL-001", product.Sku);
        Assert.Equal(100m, product.Price);
        Assert.Equal("Samsung", product.Brand);
        Assert.Equal(12, product.WarrantyMonths);
        Assert.Equal("SM-1", product.ModelNumber);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void ImportFromLines_MultipleValidRows_ImportsAll()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, Row("EL-001"), Row("EL-002"), Row("EL-003")]);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(3, result.ImportedCount);
        Assert.Equal(3, repository.Count);
    }

    [Fact]
    public void ImportFromCsv_FileNotFound_ReturnsError()
    {
        var (service, _) = CreateService();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

        var result = service.ImportFromCsv(path);

        AssertSingleError(result, 0, "Файл не найден");
        Assert.Equal(0, result.ImportedCount);
    }

    [Fact]
    public void ImportFromCsv_ValidFile_ImportsProductsWithCyrillic()
    {
        var (service, repository) = CreateService();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
        File.WriteAllLines(path, [Header, Row(name: "Ноутбук"), Row("EL-002", "Планшет")], new UTF8Encoding(false));

        try
        {
            var result = service.ImportFromCsv(path);

            Assert.Equal(2, result.ImportedCount);
            Assert.Equal(0, result.ErrorCount);
            Assert.Equal(["Ноутбук", "Планшет"], repository.GetAll().Select(product => product.Name));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ImportFromCsv_FileWithBom_ImportsProducts()
    {
        var (service, repository) = CreateService();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
        File.WriteAllLines(path, [Header, Row()], new UTF8Encoding(true));

        try
        {
            var result = service.ImportFromCsv(path);

            Assert.Equal(1, result.ImportedCount);
            Assert.Equal(1, repository.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ImportFromCsv_EmptyPath_ReturnsFileNotFound()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromCsv(string.Empty);

        AssertSingleError(result, 0, "Файл не найден");
    }

    [Fact]
    public void ImportFromCsv_EmptyFile_ReturnsNoDataError()
    {
        var (service, repository) = CreateService();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
        File.WriteAllBytes(path, []);

        try
        {
            var result = service.ImportFromCsv(path);

            AssertSingleError(result, 0, "Данные для импорта отсутствуют");
            Assert.Equal(0, repository.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ImportFromCsv_FileLocked_ReturnsReadError()
    {
        var (service, repository) = CreateService();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
        File.WriteAllLines(path, [Header, Row()], new UTF8Encoding(false));

        try
        {
            ImportResult result;
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                result = service.ImportFromCsv(path);
            }

            AssertSingleError(result, 0, "Не удалось прочитать файл");
            Assert.Equal(0, repository.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ImportFromLines_NoLines_ReturnsNoDataError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([]);

        AssertSingleError(result, 0, "Данные для импорта отсутствуют");
        Assert.Equal(0, result.TotalRows);
    }

    [Fact]
    public void ImportFromLines_OnlyBlankLines_ReturnsNoDataError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(["", "   ", ""]);

        AssertSingleError(result, 0, "Данные для импорта отсутствуют");
    }

    [Fact]
    public void ImportFromLines_HeaderOnly_ReturnsNoDataError()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header]);

        AssertSingleError(result, 0, "Данные для импорта отсутствуют");
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    public void ImportFromLines_WrongHeader_ReturnsInvalidHeadersError()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines(["a,b,c", Row()]);

        AssertSingleError(result, 1, "Неверные заголовки CSV");
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    public void ImportFromLines_WrongHeaderAfterBlankLine_ReportsPhysicalLineNumber()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(["", "a,b,c"]);

        AssertSingleError(result, 2, "Неверные заголовки CSV");
    }

    [Fact]
    public void ImportFromLines_MissingBrandColumn_ReturnsInvalidHeadersError()
    {
        var (service, _) = CreateService();
        const string header = "sku,name,description,price,currency,category_name,stock,image_url,is_active,warranty_months";

        var result = service.ImportFromLines([header]);

        AssertSingleError(result, 1, "Неверные заголовки CSV");
    }

    [Fact]
    public void ImportFromLines_HeaderCaseAndSpacesDiffer_IsAccepted()
    {
        var (service, _) = CreateService();
        var header = " " + Header.ToUpperInvariant().Replace(",", " , ");

        var result = service.ImportFromLines([header, Row()]);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void ImportFromLines_ColumnsInDifferentOrder_MapsByName()
    {
        var (service, repository) = CreateService();
        const string header =
            "name,sku,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months";
        const string row = "Смартфон,EL-9,Описание,100,RUB,Кат,1,img,true,Sony,6";

        var result = service.ImportFromLines([header, row]);

        Assert.Equal(1, result.ImportedCount);
        var product = Assert.Single(repository.GetAll());
        Assert.Equal("EL-9", product.Sku);
        Assert.Equal("Смартфон", product.Name);
        Assert.Equal("Sony", product.Brand);
        Assert.Null(product.ModelNumber);
    }

    [Fact]
    public void ImportFromLines_WrongFieldCount_ReturnsError()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, "a,b,c"]);

        Assert.Equal(1, result.TotalRows);
        AssertSingleError(result, 2, "Неверное количество полей в строке");
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    public void ImportFromLines_EmptySku_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, Row(sku: "")]);

        AssertSingleError(result, 2, "Поле sku не может быть пустым");
    }

    [Fact]
    public void ImportFromLines_EmptyName_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, Row(name: "")]);

        AssertSingleError(result, 2, "Поле name не может быть пустым");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("-0.5")]
    public void ImportFromLines_InvalidPrice_ReturnsError(string price)
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, Row(price: price)]);

        AssertSingleError(result, 2, "Поле price должно быть числом больше 0");
        Assert.Equal(0, repository.Count);
    }

    [Theory]
    [InlineData("1234.56")]
    [InlineData("\"1234,56\"")]
    [InlineData("\"1,234.56\"")]
    [InlineData("\"1.234,56\"")]
    public void ImportFromLines_DifferentDecimalSeparators_ParsesPrice(string price)
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, Row(price: price)]);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1234.56m, Assert.Single(repository.GetAll()).Price);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("")]
    public void ImportFromLines_InvalidStock_ReturnsError(string stock)
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, Row(stock: stock)]);

        AssertSingleError(result, 2, "Остаток на складе должен быть целым числом не меньше 0");
    }

    [Fact]
    public void ImportFromLines_ZeroStock_IsAccepted()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, Row(stock: "0")]);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, Assert.Single(repository.GetAll()).StockQuantity);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("да")]
    [InlineData("2")]
    [InlineData("")]
    public void ImportFromLines_InvalidIsActive_ReturnsError(string isActive)
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, Row(isActive: isActive)]);

        AssertSingleError(result, 2, "Поле is_active должно иметь значение true, false, 1 или 0");
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("0", false)]
    public void ImportFromLines_ValidIsActive_ParsesValue(string isActive, bool expected)
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, Row(isActive: isActive)]);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(expected, Assert.Single(repository.GetAll()).IsActive);
    }

    [Fact]
    public void ImportFromLines_EmptyBrand_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, Row(brand: "")]);

        AssertSingleError(result, 2, "Поле brand обязательно для варианта «Электроника»");
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("61")]
    [InlineData("abc")]
    [InlineData("12.5")]
    [InlineData("")]
    public void ImportFromLines_InvalidWarranty_ReturnsError(string warranty)
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, Row(warranty: warranty)]);

        AssertSingleError(result, 2, "Гарантия должна быть целым числом от 0 до 60 месяцев");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("60")]
    public void ImportFromLines_BoundaryWarranty_IsAccepted(string warranty)
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, Row(warranty: warranty)]);

        Assert.Equal(1, result.ImportedCount);
    }

    [Fact]
    public void ImportFromLines_DuplicateSkuInFile_ReportsErrorForSecondRow()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, Row("EL-001"), Row("el-001")]);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        AssertSingleError(result, 3, "Артикул товара уже существует");
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void ImportFromLines_SkuAlreadyInRepository_ReportsDuplicateError()
    {
        var (service, repository) = CreateService();
        service.ImportFromLines([Header, Row("EL-001")]);

        var result = service.ImportFromLines([Header, Row("EL-001"), Row("EL-002")]);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        AssertSingleError(result, 2, "Артикул товара уже существует");
        Assert.Equal(2, repository.Count);
    }

    [Fact]
    public void ImportFromLines_RepeatedImportOfSameFile_DoesNotThrow()
    {
        var (service, repository) = CreateService();
        string[] lines = [Header, Row()];

        var first = service.ImportFromLines(lines);
        var second = service.ImportFromLines(lines);

        Assert.Equal(1, first.ImportedCount);
        Assert.Equal(0, second.ImportedCount);
        Assert.Equal(1, second.ErrorCount);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void ImportFromLines_MultipleErrorsInOneRow_ReportsAllInOrder()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines(
            [Header, Row(name: "", price: "abc", stock: "-1", isActive: "x", brand: "", warranty: "61")]);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(6, result.ErrorCount);
        Assert.All(result.Errors, error => Assert.Equal(2, error.RowNumber));
        Assert.Equal(
            [
                "Поле name не может быть пустым",
                "Поле price должно быть числом больше 0",
                "Остаток на складе должен быть целым числом не меньше 0",
                "Поле is_active должно иметь значение true, false, 1 или 0",
                "Поле brand обязательно для варианта «Электроника»",
                "Гарантия должна быть целым числом от 0 до 60 месяцев"
            ],
            result.Errors.Select(error => error.Message));
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    public void ImportFromLines_EmptySkuAndName_ReportsBothInOrder()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, Row(sku: "", name: "")]);

        Assert.Equal(
            ["Поле sku не может быть пустым", "Поле name не может быть пустым"],
            result.Errors.Select(error => error.Message));
    }

    [Fact]
    public void ImportFromLines_ValidAndInvalidRows_ImportsOnlyValid()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines([Header, Row("EL-001"), Row("EL-002", name: ""), Row("EL-003")]);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(3, result.Errors[0].RowNumber);
        Assert.Equal(2, repository.Count);
    }

    [Fact]
    public void ImportFromLines_BlankLinesBetweenRows_AreSkippedWithoutCounting()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines([Header, "", Row("EL-001"), "   ", Row("EL-002", name: "")]);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        var error = Assert.Single(result.Errors);
        Assert.Equal(5, error.RowNumber);
    }

    [Fact]
    public void ImportFromLines_EmptyCurrencyAndCategory_AreNormalized()
    {
        var (service, repository) = CreateService();

        service.ImportFromLines([Header, Row(currency: "", category: "", model: "")]);

        var product = Assert.Single(repository.GetAll());
        Assert.Equal("RUB", product.Currency);
        Assert.Equal("Без категории", product.CategoryName);
        Assert.Null(product.ModelNumber);
    }

    [Fact]
    public void ImportFromLines_CyrillicAndQuotedCommas_ArePreserved()
    {
        var (service, repository) = CreateService();
        const string row = "EL-7,\"Наушники, беспроводные\",\"Звук \"\"Hi-Fi\"\", чёрный\",100,RUB,Аксессуары,5,img.jpg,true,Sony,12,WH-1000";

        var result = service.ImportFromLines([Header, row]);

        Assert.Equal(1, result.ImportedCount);
        var product = Assert.Single(repository.GetAll());
        Assert.Equal("Наушники, беспроводные", product.Name);
        Assert.Equal("Звук \"Hi-Fi\", чёрный", product.Description);
        Assert.Equal("Аксессуары", product.CategoryName);
    }

    [Fact]
    public void ImportFromLines_NullLines_ThrowsArgumentNullException()
    {
        var (service, _) = CreateService();

        Assert.Throws<ArgumentNullException>(() => service.ImportFromLines(null!));
    }
}
