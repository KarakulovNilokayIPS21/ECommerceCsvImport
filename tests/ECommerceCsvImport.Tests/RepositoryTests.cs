using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests;

public class RepositoryTests
{
    private static Product CreateProduct(string sku) => new()
    {
        Sku = sku,
        Name = "Смартфон",
        Price = 100m,
        Currency = "RUB",
        CategoryName = "Смартфоны",
        StockQuantity = 1,
        IsActive = true,
        Brand = "Samsung",
        WarrantyMonths = 12
    };

    [Fact]
    public void Count_EmptyRepository_ReturnsZero()
    {
        var repository = new InMemoryProductRepository();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
    }

    [Fact]
    public void Add_Product_IncreasesCountAndStoresProduct()
    {
        var repository = new InMemoryProductRepository();
        var product = CreateProduct("EL-001");

        repository.Add(product);

        Assert.Equal(1, repository.Count);
        Assert.Same(product, Assert.Single(repository.GetAll()));
    }

    [Fact]
    public void Add_NullProduct_ThrowsArgumentNullException()
    {
        var repository = new InMemoryProductRepository();

        Assert.Throws<ArgumentNullException>(() => repository.Add(null!));
    }

    [Fact]
    public void GetAll_AfterAddingMore_ReturnsIndependentSnapshot()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct("EL-001"));

        var snapshot = repository.GetAll();
        repository.Add(CreateProduct("EL-002"));

        Assert.Single(snapshot);
        Assert.Equal(2, repository.GetAll().Count);
    }

    [Theory]
    [InlineData("EL-001")]
    [InlineData("el-001")]
    [InlineData("  EL-001  ")]
    [InlineData(" eL-001")]
    public void GetBySku_DifferentCaseOrWhitespace_FindsProduct(string sku)
    {
        var repository = new InMemoryProductRepository();
        var product = CreateProduct("EL-001");
        repository.Add(product);

        var found = repository.GetBySku(sku);

        Assert.Same(product, found);
    }

    [Fact]
    public void GetBySku_UnknownSku_ReturnsNull()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct("EL-001"));

        Assert.Null(repository.GetBySku("EL-999"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetBySku_BlankSku_ReturnsNull(string sku)
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct("EL-001"));

        Assert.Null(repository.GetBySku(sku));
    }

    [Fact]
    public void GetBySku_EmptyRepository_ReturnsNull()
    {
        var repository = new InMemoryProductRepository();

        Assert.Null(repository.GetBySku("EL-001"));
    }

    [Fact]
    public void Clear_RepositoryWithProducts_RemovesAll()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct("EL-001"));
        repository.Add(CreateProduct("EL-002"));

        repository.Clear();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
    }
}
