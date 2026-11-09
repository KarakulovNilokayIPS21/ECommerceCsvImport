namespace ECommerceCsvImport.Models;

public sealed class Product
{
    public required string Sku { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    public required decimal Price { get; init; }

    public required string Currency { get; init; }

    public required string CategoryName { get; init; }

    public required int StockQuantity { get; init; }

    public string ImageUrl { get; init; } = string.Empty;

    public required bool IsActive { get; init; }

    public required string Brand { get; init; }

    public required int WarrantyMonths { get; init; }

    public string? ModelNumber { get; init; }
}
