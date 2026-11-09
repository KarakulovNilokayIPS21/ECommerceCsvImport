namespace ECommerceCsvImport.Models;

public sealed class ProductsIndexViewModel
{
    public required IReadOnlyList<Product> Products { get; init; }

    public IReadOnlyList<string> Categories { get; init; } = [];

    public string? Search { get; init; }

    public string? Category { get; init; }

    public string? Sort { get; init; }

    public int TotalCount => Products.Count;

    public decimal TotalValue => Products.Sum(product => product.Price * product.StockQuantity);
}
