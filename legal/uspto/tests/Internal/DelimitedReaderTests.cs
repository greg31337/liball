using System.Text;
using uspto;

namespace uspto.Tests;

public class DelimitedReaderTests
{
    [Test]
    public void CsvHeadersQuotedNewlinesAndEscapedQuotesAreReadIncrementally()
    {
        string csv = "id,description,note\r\n1,\"hello, world\",\"line1\r\nline2\"\r\n2,\"He said \"\"Hi\"\"\",plain\n";
        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        using MemoryStream input = new MemoryStream(bytes);
        DelimitedReader reader = new DelimitedReader(input, ',', true, "records.csv", CreateLimits(100, 10, 10), CancellationToken.None);

        DelimitedRow first = RequireRow(reader.ReadRow());
        DelimitedRow second = RequireRow(reader.ReadRow());

        Assert.Multiple(() =>
        {
            Assert.That(reader.Headers, Is.EqualTo(new string[] { "id", "description", "note" }));
            Assert.That(first.RowNumber, Is.EqualTo(2L));
            Assert.That(first.StartLineNumber, Is.EqualTo(2L));
            Assert.That(first.EndLineNumber, Is.EqualTo(3L));
            Assert.That(first.Values, Is.EqualTo(new string[] { "1", "hello, world", "line1\r\nline2" }));
            Assert.That(first.ReadValue("description"), Is.EqualTo("hello, world"));
            Assert.That(first.HasMissingColumns, Is.False);
            Assert.That(first.HasExtraColumns, Is.False);
            Assert.That(second.RowNumber, Is.EqualTo(3L));
            Assert.That(second.StartLineNumber, Is.EqualTo(4L));
            Assert.That(second.Values, Is.EqualTo(new string[] { "2", "He said \"Hi\"", "plain" }));
            Assert.That(reader.ReadRow(), Is.Null);
        });

        reader.Close();
        Assert.That(input.CanRead, Is.True);
    }

    [Test]
    public void TabDelimitedInputHonorsUtf16BomAndQuotedTabs()
    {
        string tsv = "key\tvalue\r\nx\t\"Résumé\t漢字\"\r\n";
        byte[] bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(tsv)).ToArray();
        using MemoryStream input = new MemoryStream(bytes);
        DelimitedReader reader = new DelimitedReader(input, '\t', true, "records.tsv", CreateLimits(100, 10, 10), CancellationToken.None);

        DelimitedRow row = RequireRow(reader.ReadRow());

        Assert.Multiple(() =>
        {
            Assert.That(reader.Headers, Is.EqualTo(new string[] { "key", "value" }));
            Assert.That(row.Values, Is.EqualTo(new string[] { "x", "Résumé\t漢字" }));
            Assert.That(row.ReadValue("value"), Is.EqualTo("Résumé\t漢字"));
            Assert.That(row.StartLineNumber, Is.EqualTo(2L));
            Assert.That(reader.ReadRow(), Is.Null);
        });
    }

    [Test]
    public void BigEndianUtf32CsvPreservesUnicodeQuotedFields()
    {
        const string csv = "id,value\r\n1,\"Résumé, 漢字\"\r\n";
        Encoding encoding = new UTF32Encoding(true, true, true);
        byte[] bytes = encoding.GetPreamble().Concat(encoding.GetBytes(csv)).ToArray();
        using MemoryStream input = new MemoryStream(bytes);
        DelimitedReader reader = new DelimitedReader(input, ',', true, "utf32.csv", CreateLimits(100, 10, 10), CancellationToken.None);

        DelimitedRow row = RequireRow(reader.ReadRow());

        Assert.Multiple(() =>
        {
            Assert.That(reader.Headers, Is.EqualTo(new[] { "id", "value" }));
            Assert.That(row.Values, Is.EqualTo(new[] { "1", "Résumé, 漢字" }));
            Assert.That(row.ReadValue("value"), Is.EqualTo("Résumé, 漢字"));
            Assert.That(row.StartLineNumber, Is.EqualTo(2L));
            Assert.That(reader.ReadRow(), Is.Null);
        });
    }

    [Test]
    public void HeaderWidthReportsMissingAndExtraColumnsWithoutDiscardingValues()
    {
        string csv = "a,b,c\n1,2\n3,4,5,6";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        DelimitedReader reader = new DelimitedReader(input, ',', true, "width.csv", CreateLimits(100, 10, 10), CancellationToken.None);

        DelimitedRow shortRow = RequireRow(reader.ReadRow());
        DelimitedRow wideRow = RequireRow(reader.ReadRow());

        Assert.Multiple(() =>
        {
            Assert.That(shortRow.ExpectedColumnCount, Is.EqualTo(3));
            Assert.That(shortRow.HasMissingColumns, Is.True);
            Assert.That(shortRow.MissingColumnCount, Is.EqualTo(1));
            Assert.That(shortRow.ReadValue("c"), Is.Null);
            Assert.That(wideRow.HasExtraColumns, Is.True);
            Assert.That(wideRow.ExtraColumnCount, Is.EqualTo(1));
            Assert.That(wideRow.Values, Is.EqualTo(new string[] { "3", "4", "5", "6" }));
        });
    }

    [Test]
    public void EmptyFieldsBlankRecordsAndTrailingDelimitersArePreserved()
    {
        string csv = ",b,\r\n\"\",x,\n\n";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        DelimitedReader reader = new DelimitedReader(input, ',', false, "empty.csv", CreateLimits(100, 10, 10), CancellationToken.None);

        DelimitedRow first = RequireRow(reader.ReadRow());
        DelimitedRow second = RequireRow(reader.ReadRow());
        DelimitedRow blank = RequireRow(reader.ReadRow());

        Assert.Multiple(() =>
        {
            Assert.That(first.Values, Is.EqualTo(new string[] { "", "b", "" }));
            Assert.That(second.Values, Is.EqualTo(new string[] { "", "x", "" }));
            Assert.That(blank.Values, Is.EqualTo(new string[] { "" }));
            Assert.That(blank.RowNumber, Is.EqualTo(3L));
            Assert.That(reader.ReadRow(), Is.Null);
        });
    }

    [TestCase("ab\"c,d", 2L)]
    [TestCase("\"a\"x,b", 3L)]
    [TestCase("\"abc", 4L)]
    public void MalformedQuotingReportsLogicalFieldLineAndOffset(string csv, long expectedOffset)
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        DelimitedReader reader = new DelimitedReader(input, ',', false, "malformed.csv", CreateLimits(100, 10, 10), CancellationToken.None);

        ParseException exception = Assert.Throws<ParseException>(() => reader.ReadRow());

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Csv));
            Assert.That(exception.SourceName, Is.EqualTo("malformed.csv"));
            Assert.That(exception.DataPath, Is.EqualTo("row[1].column[1]"));
            Assert.That(exception.HasLineNumber, Is.True);
            Assert.That(exception.LineNumber, Is.EqualTo(1));
            Assert.That(exception.HasOffset, Is.True);
            Assert.That(exception.Offset, Is.EqualTo(expectedOffset));
        });
    }

    [Test]
    public void DecodedFieldLengthIsBoundedAcrossEscapedQuotes()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("\"ab\"\"cd\""));
        DelimitedReader reader = new DelimitedReader(input, ',', false, "large-field.csv", CreateLimits(4, 10, 10), CancellationToken.None);

        ParseException exception = Assert.Throws<ParseException>(() => reader.ReadRow());

        Assert.Multiple(() =>
        {
            Assert.That(exception.DataPath, Is.EqualTo("row[1].column[1]"));
            Assert.That(exception.Offset, Is.EqualTo(6L));
            Assert.That(exception.Message, Does.Contain("character limit"));
        });
    }

    [Test]
    public void ColumnCountIsBoundedForEveryRecordIncludingHeaders()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("a,b,c"));
        DelimitedReader reader = new DelimitedReader(input, ',', true, "wide.csv", CreateLimits(100, 2, 10), CancellationToken.None);

        ParseException exception = Assert.Throws<ParseException>(() => reader.ReadRow());

        Assert.Multiple(() =>
        {
            Assert.That(exception.DataPath, Is.EqualTo("row[1].column[3]"));
            Assert.That(exception.Message, Does.Contain("column limit"));
        });
    }

    [Test]
    public void BatchesContinueFromThePreviousReadWithoutBufferingTheRemainder()
    {
        string csv = "id\n1\n2\n3\n4\n5\n";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        DelimitedReader reader = new DelimitedReader(input, ',', true, "batch.csv", CreateLimits(100, 10, 10), CancellationToken.None);

        DelimitedRow[] first = reader.ReadBatch(2);
        DelimitedRow[] second = reader.ReadBatch(2);
        DelimitedRow[] third = reader.ReadBatch(2);
        DelimitedRow[] finished = reader.ReadBatch(2);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(row => row.Values[0]), Is.EqualTo(new string[] { "1", "2" }));
            Assert.That(second.Select(row => row.Values[0]), Is.EqualTo(new string[] { "3", "4" }));
            Assert.That(third.Select(row => row.Values[0]), Is.EqualTo(new string[] { "5" }));
            Assert.That(finished, Is.Empty);
            Assert.That(first[0].RowNumber, Is.EqualTo(2L));
            Assert.That(third[0].RowNumber, Is.EqualTo(6L));
        });
    }

    [Test]
    public void UnsupportedDelimiterAndOversizedBatchAreRejected()
    {
        using MemoryStream invalidInput = new MemoryStream();
        ValidationException delimiter = Assert.Throws<ValidationException>(() => new DelimitedReader(invalidInput, '|', false, "invalid.txt", CreateLimits(100, 10, 10), CancellationToken.None));

        using MemoryStream input = new MemoryStream();
        DelimitedReader reader = new DelimitedReader(input, ',', false, "batch.csv", CreateLimits(100, 10, 2), CancellationToken.None);
        ValidationException batch = Assert.Throws<ValidationException>(() => reader.ReadBatch(3));

        Assert.Multiple(() =>
        {
            Assert.That(delimiter.ErrorCode, Is.EqualTo("UNSUPPORTED_DELIMITER"));
            Assert.That(batch.ErrorCode, Is.EqualTo("BATCH_SIZE_OUT_OF_RANGE"));
        });
    }

    [Test]
    public void CancellationStopsReadingBetweenStreamedRows()
    {
        string csv = "id,name\n1,first\n2,second\n";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        DelimitedReader reader = new DelimitedReader(input, ',', true, "cancelled.csv", CreateLimits(100, 10, 10), cancellation.Token);

        DelimitedRow first = RequireRow(reader.ReadRow());
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(first.ReadValue("name"), Is.EqualTo("first"));
            Assert.Throws<OperationCanceledException>(() => reader.ReadRow());
        });
    }

    private static DelimitedRow RequireRow(DelimitedRow? row)
    {
        return row ?? throw new AssertionException("Expected another delimited row.");
    }

    private static UsptoLimits CreateLimits(int maximumFieldCharacters, int maximumColumns, int maximumResults)
    {
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumDelimitedFieldCharacters = maximumFieldCharacters;
        limits.MaximumDelimitedColumns = maximumColumns;
        limits.MaximumResults = maximumResults;
        return limits;
    }
}
