namespace ECommerceCsvImport.Models;

public sealed record ImportError(int RowNumber, string Message);
