using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests;

public class CsvLineParserTests
{
    [Fact]
    public void Parse_SimpleLine_ReturnsFields()
    {
        var fields = CsvLineParser.Parse("a,b,c");

        Assert.Equal(["a", "b", "c"], fields);
    }

    [Fact]
    public void Parse_EmptyFields_ReturnsEmptyStrings()
    {
        var fields = CsvLineParser.Parse("a,,c,");

        Assert.Equal(["a", "", "c", ""], fields);
    }

    [Fact]
    public void Parse_EmptyLine_ReturnsSingleEmptyField()
    {
        var fields = CsvLineParser.Parse(string.Empty);

        Assert.Equal([""], fields);
    }

    [Fact]
    public void Parse_CommaInsideQuotes_KeepsCommaInField()
    {
        var fields = CsvLineParser.Parse("a,\"b,c\",d");

        Assert.Equal(["a", "b,c", "d"], fields);
    }

    [Fact]
    public void Parse_EscapedQuotes_ReturnsSingleQuote()
    {
        var fields = CsvLineParser.Parse("a,\"say \"\"hi\"\"\",c");

        Assert.Equal(["a", "say \"hi\"", "c"], fields);
    }

    [Fact]
    public void Parse_FieldsWithSpaces_TrimsFields()
    {
        var fields = CsvLineParser.Parse("  a , b  ,  c  ");

        Assert.Equal(["a", "b", "c"], fields);
    }

    [Fact]
    public void Parse_CyrillicText_PreservesText()
    {
        var fields = CsvLineParser.Parse("Смартфон,\"Экран 6,2\"\", чёрный\"");

        Assert.Equal(["Смартфон", "Экран 6,2\", чёрный"], fields);
    }

    [Fact]
    public void Parse_QuoteInsideUnquotedField_TreatsQuoteAsLiteral()
    {
        var fields = CsvLineParser.Parse("a,Кабель 2\" USB,b");

        Assert.Equal(["a", "Кабель 2\" USB", "b"], fields);
    }

    [Fact]
    public void Parse_LeadingWhitespaceBeforeQuote_StillStartsQuotedField()
    {
        var fields = CsvLineParser.Parse("a,  \"b,c\",d");

        Assert.Equal(["a", "b,c", "d"], fields);
    }

    [Fact]
    public void Parse_NullLine_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CsvLineParser.Parse(null!));
    }
}
