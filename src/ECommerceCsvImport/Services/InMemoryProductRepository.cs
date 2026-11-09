using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly Lock _lock = new();
    private readonly List<Product> _products = [];

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _products.Count;
            }
        }
    }

    public void Add(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        lock (_lock)
        {
            _products.Add(product);
        }
    }

    public IReadOnlyList<Product> GetAll()
    {
        lock (_lock)
        {
            return _products.ToArray();
        }
    }

    public Product? GetBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        var normalizedSku = sku.Trim();

        lock (_lock)
        {
            return _products.FirstOrDefault(product =>
                string.Equals(product.Sku.Trim(), normalizedSku, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _products.Clear();
        }
    }
}
