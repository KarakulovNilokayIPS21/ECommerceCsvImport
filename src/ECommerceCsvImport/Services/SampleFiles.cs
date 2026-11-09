namespace ECommerceCsvImport.Services;

internal static class SampleFiles
{
    public const string FolderName = "csv";
    public const string TestDataFolderName = "testdata";
    public const string DefaultFileName = "sample_products.csv";
    public const string SearchPattern = "*.csv";

    public static IReadOnlyList<string> GetAll()
    {
        var mainFolder = Path.Combine(AppContext.BaseDirectory, FolderName);
        var testDataFolder = Path.Combine(mainFolder, TestDataFolderName);

        return [.. GetFiles(mainFolder), .. GetFiles(testDataFolder)];
    }

    public static string? FindDefault(IReadOnlyList<string> files)
    {
        return files.FirstOrDefault(file =>
            string.Equals(Path.GetFileName(file), DefaultFileName, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] GetFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory
            .GetFiles(folder, SearchPattern, SearchOption.TopDirectoryOnly)
            .Select(Path.GetFullPath)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
