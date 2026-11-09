using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests;

public class TestDataFileTests
{
    private const string DuplicateSkuMessage = "Артикул товара уже существует";
    private const string InvalidPriceMessage = "Поле price должно быть числом больше 0";
    private const string EmptyBrandMessage = "Поле brand обязательно для варианта «Электроника»";
    private const string InvalidWarrantyMessage = "Гарантия должна быть целым числом от 0 до 60 месяцев";
    private const string InvalidIsActiveMessage = "Поле is_active должно иметь значение true, false, 1 или 0";
    private const string NoDataMessage = "Данные для импорта отсутствуют";

    private static string GetPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "testdata", fileName);

    private static (ProductImportService Service, InMemoryProductRepository Repository) CreateService()
    {
        var repository = new InMemoryProductRepository();
        return (new ProductImportService(repository), repository);
    }

    private static int[] ParseRows(string rows) =>
        rows.Split(',').Select(int.Parse).ToArray();

    [Theory]
    [InlineData("valid_electronics.csv")]
    [InlineData("invalid_price.csv")]
    [InlineData("duplicate_sku.csv")]
    [InlineData("missing_brand.csv")]
    [InlineData("warranty_out_of_range.csv")]
    [InlineData("bad_active.csv")]
    [InlineData("empty_file.csv")]
    [InlineData("sample_products.csv")]
    public void TestDataFile_Exists_InOutputFolder(string fileName)
    {
        Assert.True(File.Exists(GetPath(fileName)));
    }

    [Fact]
    public void ImportFromCsv_ValidElectronics_ImportsFiveProductsWithoutErrors()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromCsv(GetPath("valid_electronics.csv"));

        Assert.Equal(5, result.TotalRows);
        Assert.Equal(5, result.ImportedCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(5, repository.Count);
    }

    [Fact]
    public void ImportFromCsv_ValidElectronics_AppliesDefaultsAndParsesValues()
    {
        var (service, repository) = CreateService();

        service.ImportFromCsv(GetPath("valid_electronics.csv"));

        var laptop = repository.GetBySku("TV-002");
        var headphones = repository.GetBySku("TV-003");
        var monitor = repository.GetBySku("TV-004");
        var router = repository.GetBySku("TV-005");
        var phone = repository.GetBySku("TV-001");

        Assert.NotNull(laptop);
        Assert.NotNull(headphones);
        Assert.NotNull(monitor);
        Assert.NotNull(router);
        Assert.NotNull(phone);
        Assert.Equal(99990.50m, laptop.Price);
        Assert.True(laptop.IsActive);
        Assert.Equal("RUB", headphones.Currency);
        Assert.Null(headphones.ModelNumber);
        Assert.Equal("Наушники проводные, белые", headphones.Name);
        Assert.Equal("Без категории", monitor.CategoryName);
        Assert.False(monitor.IsActive);
        Assert.False(router.IsActive);
        Assert.Equal("Экран 6.2\", 128 ГБ", phone.Description);
    }

    [Theory]
    [InlineData("invalid_price.csv", 1, InvalidPriceMessage, "3,4,5")]
    [InlineData("duplicate_sku.csv", 2, DuplicateSkuMessage, "4")]
    [InlineData("missing_brand.csv", 1, EmptyBrandMessage, "3")]
    [InlineData("warranty_out_of_range.csv", 1, InvalidWarrantyMessage, "3,4")]
    [InlineData("bad_active.csv", 1, InvalidIsActiveMessage, "3,4")]
    public void ImportFromCsv_InvalidFile_ReportsExpectedErrorsOnExpectedRows(
        string fileName,
        int expectedImported,
        string expectedMessage,
        string expectedRows)
    {
        var (service, repository) = CreateService();
        var rows = ParseRows(expectedRows);

        var result = service.ImportFromCsv(GetPath(fileName));

        Assert.Equal(expectedImported, result.ImportedCount);
        Assert.Equal(expectedImported, repository.Count);
        Assert.Equal(rows, result.Errors.Select(error => error.RowNumber));
        Assert.All(result.Errors, error => Assert.Equal(expectedMessage, error.Message));
    }

    [Fact]
    public void ImportFromCsv_DuplicateSkuFile_TreatsSkuAsCaseInsensitive()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromCsv(GetPath("duplicate_sku.csv"));

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(["DS-001", "DS-002"], repository.GetAll().Select(product => product.Sku));
    }

    [Fact]
    public void ImportFromCsv_WarrantyOutOfRangeFile_AcceptsBoundaryValueOfSixty()
    {
        var (service, repository) = CreateService();

        service.ImportFromCsv(GetPath("warranty_out_of_range.csv"));

        var product = Assert.Single(repository.GetAll());
        Assert.Equal("WR-001", product.Sku);
        Assert.Equal(60, product.WarrantyMonths);
    }

    [Fact]
    public void ImportFromCsv_EmptyFile_ReturnsNoDataError()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromCsv(GetPath("empty_file.csv"));

        var error = Assert.Single(result.Errors);
        Assert.Equal(0, error.RowNumber);
        Assert.Equal(NoDataMessage, error.Message);
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    public void ImportFromCsv_SampleProducts_ImportsEighteenProductsWithoutErrors()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromCsv(GetPath("sample_products.csv"));

        Assert.Equal(18, result.TotalRows);
        Assert.Equal(18, result.ImportedCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(18, repository.Count);
    }

    [Fact]
    public void ImportFromCsv_SampleProducts_PreservesCyrillicAndQuotedCommas()
    {
        var (service, repository) = CreateService();

        service.ImportFromCsv(GetPath("sample_products.csv"));

        var phone = repository.GetBySku("EL-001");
        var headphones = repository.GetBySku("EL-011");
        var laptop = repository.GetBySku("EL-004");

        Assert.NotNull(phone);
        Assert.NotNull(headphones);
        Assert.NotNull(laptop);
        Assert.Equal("Смартфон Galaxy S24", phone.Name);
        Assert.Equal("Смартфоны", phone.CategoryName);
        Assert.Equal("Наушники беспроводные, чёрные", headphones.Name);
        Assert.Equal(64990.50m, laptop.Price);
        Assert.DoesNotContain(repository.GetAll(), product => product.Name.Contains('�'));
    }

    [Fact]
    public void ImportFromCsv_SampleProductsImportedTwice_ReportsDuplicateForEveryRow()
    {
        var (service, repository) = CreateService();
        var path = GetPath("sample_products.csv");

        service.ImportFromCsv(path);
        var second = service.ImportFromCsv(path);

        Assert.Equal(18, second.TotalRows);
        Assert.Equal(0, second.ImportedCount);
        Assert.Equal(18, second.ErrorCount);
        Assert.All(second.Errors, error => Assert.Equal(DuplicateSkuMessage, error.Message));
        Assert.Equal(18, repository.Count);
    }
}
