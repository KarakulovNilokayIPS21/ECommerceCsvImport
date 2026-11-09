using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

public static class ProductCatalog
{
    public const string SortPriceAscending = "price_asc";
    public const string SortPriceDescending = "price_desc";

    public static IReadOnlyList<Product> Apply(
        IEnumerable<Product> products,
        string? search,
        string? category,
        string? sort)
    {
        ArgumentNullException.ThrowIfNull(products);

        var query = products;

        var searchText = search?.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            query = query.Where(product =>
                product.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        var categoryName = category?.Trim();
        if (!string.IsNullOrEmpty(categoryName))
        {
            query = query.Where(product =>
                string.Equals(product.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase));
        }

        query = sort switch
        {
            SortPriceAscending => query.OrderBy(product => product.Price),
            SortPriceDescending => query.OrderByDescending(product => product.Price),
            _ => query
        };

        return query.ToList();
    }

    public static IReadOnlyList<string> GetCategories(IEnumerable<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);

        return products
            .Select(product => product.CategoryName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
