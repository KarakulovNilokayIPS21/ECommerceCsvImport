namespace ECommerceCsvImport.Models;

public sealed class ImportResult
{
    public int TotalRows { get; set; }

    public int ImportedCount { get; set; }

    public List<ImportError> Errors { get; } = [];

    public int ErrorCount => Errors.Count;
}
