using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceCsvImport.Controllers;

public class ProductsController(
    IProductRepository repository,
    IProductImportService importService) : Controller
{
    [HttpGet]
    public IActionResult Index(string? search, string? category, string? sort)
    {
        var allProducts = repository.GetAll();

        var model = new ProductsIndexViewModel
        {
            Products = ProductCatalog.Apply(allProducts, search, category, sort),
            Categories = ProductCatalog.GetCategories(allProducts),
            Search = search,
            Category = category,
            Sort = sort
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Import()
    {
        var samples = SampleFiles.GetAll();

        return View(new ImportViewModel
        {
            FilePath = SampleFiles.FindDefault(samples) ?? string.Empty,
            SampleFiles = samples
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Import(string? filePath)
    {
        var model = new ImportViewModel
        {
            FilePath = filePath ?? string.Empty,
            Result = importService.ImportFromCsv(filePath ?? string.Empty),
            SampleFiles = SampleFiles.GetAll()
        };

        return View(model);
    }
}
