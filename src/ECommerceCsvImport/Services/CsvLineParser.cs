using System.Text;

namespace ECommerceCsvImport.Services;

public static class CsvLineParser
{
    private const char Delimiter = ',';
    private const char Quote = '"';

    public static IReadOnlyList<string> Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var atFieldStart = true;

        for (var i = 0; i < line.Length; i++)
        {
            var symbol = line[i];

            if (inQuotes)
            {
                if (symbol != Quote)
                {
                    current.Append(symbol);
                }
                else if (i + 1 < line.Length && line[i + 1] == Quote)
                {
                    current.Append(Quote);
                    i++;
                }
                else
                {
                    inQuotes = false;
                }

                continue;
            }

            if (symbol == Quote && atFieldStart)
            {
                inQuotes = true;
                atFieldStart = false;
            }
            else if (symbol == Delimiter)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
                atFieldStart = true;
            }
            else
            {
                current.Append(symbol);
                atFieldStart &= char.IsWhiteSpace(symbol);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}
