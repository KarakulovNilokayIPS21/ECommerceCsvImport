using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

public interface IProductRepository
{
    int Count { get; }

    void Add(Product product);

    IReadOnlyList<Product> GetAll();

    Product? GetBySku(string sku);

    void Clear();
}
