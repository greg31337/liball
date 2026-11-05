using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvSnifferTests
{
    [Test]
    public void Sniff_DetectsDelimiterQuoteDoubleQuoteEscapeAndLfNewline()
    {
        string sample = "'Name';'Note'\n'Ada';'uses ; inside field'\n'Grace';'said ''hello'''\n";

        CsvDialect dialect = CsvSniffer.Sniff(sample, CsvSnifferOptions.CreateDefault());

        Assert.That(dialect.Delimiter, Is.EqualTo(';'));
        Assert.That(dialect.Quote, Is.EqualTo('\''));
        Assert.That(dialect.Escape, Is.EqualTo('\''));
        Assert.That(dialect.UseDoubleQuoteEscaping, Is.True);
        Assert.That(dialect.LineTerminator, Is.EqualTo("\n"));
        Assert.That(dialect.NewLineMode, Is.EqualTo(CsvNewLineMode.Lf));
    }

    [Test]
    public void Sniff_DetectsBackslashEscapePipeDelimiterAndCrlfNewline()
    {
        string sample = "\"Name\"|\"Note\"\r\n\"Ada\"|\"said \\\"hello\\\"\"\r\n";

        CsvDialect dialect = CsvSniffer.Sniff(sample, CsvSnifferOptions.CreateDefault());

        Assert.That(dialect.Delimiter, Is.EqualTo('|'));
        Assert.That(dialect.Quote, Is.EqualTo('"'));
        Assert.That(dialect.Escape, Is.EqualTo('\\'));
        Assert.That(dialect.UseDoubleQuoteEscaping, Is.False);
        Assert.That(dialect.LineTerminator, Is.EqualTo("\r\n"));
        Assert.That(dialect.NewLineMode, Is.EqualTo(CsvNewLineMode.Crlf));
    }

    [Test]
    public void Sniff_DetectsCommentMarkerFromCandidateLines()
    {
        CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();
        options.CandidateComments = new char[] { '#', '!' };
        string sample = "! exported\n! source\nName\tAge\nAda\t42\n";

        CsvDialect dialect = CsvSniffer.Sniff(sample, options);

        Assert.That(dialect.Delimiter, Is.EqualTo('\t'));
        Assert.That(dialect.HasComment, Is.True);
        Assert.That(dialect.Comment, Is.EqualTo('!'));
    }

    [Test]
    public void DetectHeader_ReturnsTrueWhenFirstRowLooksLikeNames()
    {
        string sample = "Name,Age\nAda,42\nGrace,43\n";
        CsvDialect dialect = CsvSniffer.Sniff(sample, CsvSnifferOptions.CreateDefault());

        bool hasHeader = CsvSniffer.DetectHeader(sample, dialect, CsvSnifferOptions.CreateDefault());

        Assert.That(hasHeader, Is.True);
    }

    [Test]
    public void DetectHeader_ReturnsFalseForNumericRowsOrWhenDisabled()
    {
        string sample = "1,2\n3,4\n";
        CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();
        CsvDialect dialect = CsvSniffer.Sniff(sample, options);

        Assert.That(CsvSniffer.DetectHeader(sample, dialect, options), Is.False);

        options.DetectHeader = false;

        Assert.That(CsvSniffer.DetectHeader("Name,Age\nAda,42\n", dialect, options), Is.False);
    }

    [Test]
    public void Sniff_ReturnsOverrideDialectWhenConfigured()
    {
        CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();
        options.HasOverrideDialect = true;
        options.OverrideDialect = CsvDialect.CreateTsv();
        options.OverrideDialect.HasComment = true;
        options.OverrideDialect.Comment = '#';

        CsvDialect dialect = CsvSniffer.Sniff("A;B\n1;2\n", options);

        Assert.That(dialect.Delimiter, Is.EqualTo('\t'));
        Assert.That(dialect.LineTerminator, Is.EqualTo("\n"));
        Assert.That(dialect.HasComment, Is.True);
        Assert.That(dialect.Comment, Is.EqualTo('#'));
    }

    [Test]
    public void Sniff_AmbiguousDelimiterUsesCandidateOrder()
    {
        CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();
        options.CandidateDelimiters = new char[] { '|', ',', ';' };
        string sample = "A,B|C;D\n1,2|3;4\n";

        CsvDialect dialect = CsvSniffer.Sniff(sample, options);

        Assert.That(dialect.Delimiter, Is.EqualTo('|'));
    }

    [Test]
    public void Sniff_AmbiguousQuoteUsesCandidateOrder()
    {
        CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();
        options.CandidateQuotes = new char[] { '\'', '"' };
        string sample = "\"A\",'B'\n\"1\",'2'\n";

        CsvDialect dialect = CsvSniffer.Sniff(sample, options);

        Assert.That(dialect.Quote, Is.EqualTo('\''));
    }

    [Test]
    public void Sniff_DetectCommentsCanBeDisabledForCommentLikeLines()
    {
        CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();
        options.DetectComments = false;
        string sample = "# exported\n# source\nA,B\n1,2\n";

        CsvDialect dialect = CsvSniffer.Sniff(sample, options);

        Assert.That(dialect.HasComment, Is.False);
        Assert.That(dialect.Comment, Is.EqualTo('\0'));
    }

    [Test]
    public void Sniff_OverrideDialectIsReturnedAsIndependentClone()
    {
        CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();
        options.HasOverrideDialect = true;
        options.OverrideDialect = CsvDialect.CreateTsv();
        options.OverrideDialect.HasComment = true;
        options.OverrideDialect.Comment = '!';

        CsvDialect dialect = CsvSniffer.Sniff("A;B\n1;2\n", options);
        dialect.Delimiter = '|';
        dialect.Comment = '#';

        Assert.That(options.OverrideDialect.Delimiter, Is.EqualTo('\t'));
        Assert.That(options.OverrideDialect.Comment, Is.EqualTo('!'));
    }

    [Test]
    public void DetectHeader_ReturnsFalseForAmbiguousTextRows()
    {
        string sample = "Name,Code\nAda,Blue\nGrace,Green\n";
        CsvDialect dialect = CsvSniffer.Sniff(sample, CsvSnifferOptions.CreateDefault());

        bool hasHeader = CsvSniffer.DetectHeader(sample, dialect, CsvSnifferOptions.CreateDefault());

        Assert.That(hasHeader, Is.False);
    }

    [Test]
    public void DetectTypes_InfersNullBooleanNumbersDatesGuidBinaryAndString()
    {
        CsvHeaderOptions headerOptions = CsvHeaderOptions.CreateDefault();
        CsvInferenceOptions inferenceOptions = CsvInferenceOptions.CreateDefault();
        inferenceOptions.TreatEmptyAsNull = true;
        inferenceOptions.ConversionOptions.HasNullString = true;
        inferenceOptions.ConversionOptions.NullString = "NULL";
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Nothing", "Flag", "Small", "Large", "Money", "Ratio", "Day", "Clock", "Stamp", "Key", "Bytes", "Name" }, 1), headerOptions);
        table.AddRow(CsvRow.Create(new string[] { "", "true", "42", "2147483648", "12.50", "1.25e3", "2024-02-29", "13:14", "2024-02-29T01:02:03", "01234567-89ab-cdef-0123-456789abcdef", "0A FF", "Ada" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "NULL", "false", "-7", "9223372036854775807", "13.75", "5E-1", "2024-03-01", "09:10:11", "2024-03-01 04:05:06", "11111111-2222-3333-4444-555555555555", "0B-01", "not typed" }, 3));

        CsvSchema schema = CsvSniffer.DetectTypes(table, inferenceOptions);

        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.Null));
        Assert.That(schema.GetColumn(0).IsNullable, Is.True);
        Assert.That(schema.GetColumn(1).TypeKind, Is.EqualTo(CsvTypeKind.Boolean));
        Assert.That(schema.GetColumn(2).TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(schema.GetColumn(3).TypeKind, Is.EqualTo(CsvTypeKind.Int64));
        Assert.That(schema.GetColumn(4).TypeKind, Is.EqualTo(CsvTypeKind.Decimal));
        Assert.That(schema.GetColumn(5).TypeKind, Is.EqualTo(CsvTypeKind.Double));
        Assert.That(schema.GetColumn(6).TypeKind, Is.EqualTo(CsvTypeKind.DateOnly));
        Assert.That(schema.GetColumn(7).TypeKind, Is.EqualTo(CsvTypeKind.TimeOnly));
        Assert.That(schema.GetColumn(8).TypeKind, Is.EqualTo(CsvTypeKind.DateTime));
        Assert.That(schema.GetColumn(9).TypeKind, Is.EqualTo(CsvTypeKind.Guid));
        Assert.That(schema.GetColumn(10).TypeKind, Is.EqualTo(CsvTypeKind.Binary));
        Assert.That(schema.GetColumn(11).TypeKind, Is.EqualTo(CsvTypeKind.String));
    }
}
