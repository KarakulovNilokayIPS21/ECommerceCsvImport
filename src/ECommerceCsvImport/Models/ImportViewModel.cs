namespace ECommerceCsvImport.Models;

public sealed class ImportViewModel
{
    public string FilePath { get; set; } = string.Empty;

    public ImportResult? Result { get; set; }

    public IReadOnlyList<string> SampleFiles { get; set; } = [];
}
