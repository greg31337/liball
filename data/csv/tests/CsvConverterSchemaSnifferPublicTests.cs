using System;
using System.Globalization;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvConverterSchemaSnifferPublicTests
{
    [Test]
    public void Converter_PublicApiCoversEverySupportedTargetType()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        options.DateTimeFormats = new string[] { "dd/MM/yyyy", "HH:mm:ss" };

        Assert.That(converter.ToBoolean("yes", options), Is.True);
        Assert.That(converter.ToInt32("-42", options), Is.EqualTo(-42));
        Assert.That(converter.ToInt64("9000000000", options), Is.EqualTo(9000000000L));
        Assert.That(converter.ToDecimal("12.50", options), Is.EqualTo(12.50m));
        Assert.That(converter.ToDouble("1.25e2", options), Is.EqualTo(125d));
        Assert.That(converter.ToDateTime("31/12/2024", options), Is.EqualTo(new DateTime(2024, 12, 31)));
        Assert.That(converter.ToDateOnly("31/12/2024", options), Is.EqualTo(new DateOnly(2024, 12, 31)));
        Assert.That(converter.ToTimeOnly("01:02:03", options), Is.EqualTo(new TimeOnly(1, 2, 3)));
        Assert.That(converter.ToGuid("00112233-4455-6677-8899-aabbccddeeff", options), Is.EqualTo(new Guid("00112233-4455-6677-8899-aabbccddeeff")));
        Assert.That(converter.ToByteArray("0A-ff:10", options), Is.EqualTo(new byte[] { 10, 255, 16 }));

        CsvColumn column = new CsvColumn();
        column.TypeKind = CsvTypeKind.Int32;
        column.FormatHint = "D4";
        Assert.That(converter.ToStringValue(42, column, options), Is.EqualTo("0042"));
    }

    [Test]
    public void Converter_PublicApiUsesCultureSeparatorsAndFormatOptions()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        options.Culture = new CultureInfo("fr-FR");
        options.DecimalSeparator = ",";
        options.ThousandsSeparator = ".";
        options.DateTimeFormats = new string[] { "dd MMMM yyyy" };

        Assert.That(converter.ToDecimal("1.234,50", options), Is.EqualTo(1234.50m));
        Assert.That(converter.ToDouble("1.234,5e1", options), Is.EqualTo(12345d));

        CsvColumn numberColumn = new CsvColumn();
        numberColumn.TypeKind = CsvTypeKind.Decimal;
        numberColumn.FormatHint = "#,##0.00";
        Assert.That(converter.ToStringValue(1234.5m, numberColumn, options), Is.EqualTo("1.234,50"));

        CsvColumn dateColumn = new CsvColumn();
        dateColumn.TypeKind = CsvTypeKind.DateOnly;
        Assert.That(converter.ToStringValue(new DateOnly(2024, 2, 29), dateColumn, options), Is.EqualTo("29 février 2024"));
    }

    [Test]
    public void Converter_PublicApiAppliesDefaultsAndContainsFailures()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        options.HasNullString = true;
        options.NullString = "NULL";
        options.UseDefaultForNull = true;
        options.UseDefaultForInvalid = true;
        options.DefaultValue = "7";

        Assert.That(converter.ToInt32("NULL", options), Is.EqualTo(7));
        Assert.That(converter.ToInt32("invalid", options), Is.EqualTo(7));
        Assert.That(converter.ToDecimal("999999999999999999999999999999999999999", options), Is.EqualTo(7m));
        Assert.That(converter.ToDouble("1e999999999999999999", options), Is.EqualTo(7d));

        CsvConversionOptions noDefaults = new CsvConversionOptions();
        Assert.That(converter.ToBoolean("invalid", noDefaults), Is.False);
        Assert.That(converter.ToInt64("9223372036854775808", noDefaults), Is.EqualTo(0));
        Assert.That(converter.ToDecimal("999999999999999999999999999999999999999", noDefaults), Is.EqualTo(0m));
        Assert.That(converter.ToDouble("1e999999999999999999", noDefaults), Is.EqualTo(0d));
        Assert.That(converter.ToDateTime("invalid", noDefaults), Is.EqualTo(default(DateTime)));
        Assert.That(converter.ToGuid("invalid", noDefaults), Is.EqualTo(Guid.Empty));
        Assert.That(converter.ToByteArray("xyz", noDefaults), Is.Empty);
    }

    [Test]
    public void DetectTypes_PublicApiInfersEverySupportedColumnType()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Null", "Boolean", "Int32", "Int64", "Decimal", "Double", "Date", "Time", "Stamp", "Guid", "Binary", "String" };
        table.Rows.Add(CreateRow(new string?[]
        {
            null, "true", "42", "2147483648", "12.50", "1.25e3", "2024-02-29", "13:14", "2024-02-29T01:02:03",
            "00112233-4455-6677-8899-aabbccddeeff", "0A FF", "Ada"
        }));
        table.Rows.Add(CreateRow(new string?[]
        {
            null, "false", "-7", "9000000000", "13.75", "5e-1", "2024-03-01", "09:10:11", "2024-03-01 04:05:06",
            "11111111-2222-3333-4444-555555555555", "0B-01", "Grace"
        }));
        ICsvSniffer sniffer = new CsvSnifferService();

        CsvSchema schema = sniffer.DetectTypes(table, new CsvInferenceOptions());

        CsvTypeKind[] expected = new CsvTypeKind[]
        {
            CsvTypeKind.Null, CsvTypeKind.Boolean, CsvTypeKind.Int32, CsvTypeKind.Int64, CsvTypeKind.Decimal, CsvTypeKind.Double,
            CsvTypeKind.DateOnly, CsvTypeKind.TimeOnly, CsvTypeKind.DateTime, CsvTypeKind.Guid, CsvTypeKind.Binary, CsvTypeKind.String
        };
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(schema.Columns[index].TypeKind, Is.EqualTo(expected[index]));
            index = index + 1;
        }
    }

    [Test]
    public void DetectTypes_PublicApiUsesFormatsSeparatorsAndExposesStatistics()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Amount", "Day", "Clock", "Nullable", "Empty", "Missing" };
        table.Rows.Add(CreateRow(new string?[] { "1.234,50", "31/12/2024", "01:02:03", null, "", "present" }));
        table.Rows.Add(CreateRow(new string?[] { "2.000,00", "01/01/2025", "04:05:06", "value", "filled" }));
        table.Rows.Add(CreateRow(new string?[] { "excluded", "excluded", "excluded", "excluded", "excluded", "excluded" }));
        CsvInferenceOptions options = new CsvInferenceOptions();
        options.MaxSampleRows = 2;
        options.TreatEmptyAsNull = true;
        options.ConversionOptions.DecimalSeparator = ",";
        options.ConversionOptions.ThousandsSeparator = ".";
        options.ConversionOptions.DateTimeFormats = new string[] { "dd/MM/yyyy", "HH:mm:ss" };
        ICsvSniffer sniffer = new CsvSnifferService();

        CsvSchema schema = sniffer.DetectTypes(table, options);

        Assert.That(schema.Columns[0].TypeKind, Is.EqualTo(CsvTypeKind.Decimal));
        Assert.That(schema.Columns[0].FormatHint, Is.EqualTo("#,##0.00"));
        Assert.That(schema.Columns[1].TypeKind, Is.EqualTo(CsvTypeKind.DateOnly));
        Assert.That(schema.Columns[1].FormatHint, Is.EqualTo("dd/MM/yyyy"));
        Assert.That(schema.Columns[2].TypeKind, Is.EqualTo(CsvTypeKind.TimeOnly));
        Assert.That(schema.Columns[2].FormatHint, Is.EqualTo("HH:mm:ss"));
        Assert.That(schema.Columns[3].NullCount, Is.EqualTo(1));
        Assert.That(schema.Columns[3].NonNullCount, Is.EqualTo(1));
        Assert.That(schema.Columns[3].IsNullable, Is.True);
        Assert.That(schema.Columns[4].EmptyCount, Is.EqualTo(1));
        Assert.That(schema.Columns[4].IsNullable, Is.True);
        Assert.That(schema.Columns[5].SampledCount, Is.EqualTo(2));
        Assert.That(schema.Columns[5].MissingCount, Is.EqualTo(1));
        Assert.That(schema.Columns[5].IsNullable, Is.True);
    }

    [Test]
    public void DetectTypes_PublicApiBoundsAmbiguousSamplesAndCanDisableTypeInference()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Value" };
        table.Rows.Add(CreateRow(new string?[] { "123" }));
        table.Rows.Add(CreateRow(new string?[] { "text" }));
        ICsvSniffer sniffer = new CsvSnifferService();

        CsvInferenceOptions oneRow = new CsvInferenceOptions();
        oneRow.MaxSampleRows = 1;
        CsvSchema bounded = sniffer.DetectTypes(table, oneRow);

        CsvInferenceOptions allRows = new CsvInferenceOptions();
        allRows.MaxSampleRows = 0;
        CsvSchema ambiguous = sniffer.DetectTypes(table, allRows);

        CsvInferenceOptions disabled = new CsvInferenceOptions();
        disabled.InferTypes = false;
        CsvSchema unknown = sniffer.DetectTypes(table, disabled);

        Assert.That(bounded.Columns[0].TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(bounded.Columns[0].SampledCount, Is.EqualTo(1));
        Assert.That(ambiguous.Columns[0].TypeKind, Is.EqualTo(CsvTypeKind.String));
        Assert.That(ambiguous.Columns[0].SampledCount, Is.EqualTo(2));
        Assert.That(unknown.Columns[0].TypeKind, Is.EqualTo(CsvTypeKind.Unknown));
    }

    [Test]
    public void Sniff_PublicApiDetectsDialectCommentsEscapesAndHeaders()
    {
        ICsvSniffer sniffer = new CsvSnifferService();
        CsvSnifferOptions options = new CsvSnifferOptions();
        options.CandidateDelimiters = new char[] { ',', ';', '|' };
        options.CandidateQuotes = new char[] { '"', '\'' };
        options.CandidateComments = new char[] { '#', '!' };
        string sample = "! export\r\n'Name';'Amount'\r\n'Ada';'42'\r\n'Grace';'said ''hello'''\r\n";

        CsvDialect dialect = sniffer.Sniff(sample, options);

        Assert.That(dialect.Delimiter, Is.EqualTo(';'));
        Assert.That(dialect.Quote, Is.EqualTo('\''));
        Assert.That(dialect.Escape, Is.EqualTo('\''));
        Assert.That(dialect.UseDoubleQuoteEscaping, Is.True);
        Assert.That(dialect.NewLineMode, Is.EqualTo(CsvNewLineMode.Crlf));
        Assert.That(dialect.HasComment, Is.True);
        Assert.That(dialect.Comment, Is.EqualTo('!'));
        Assert.That(sniffer.DetectHeader(sample, dialect, options), Is.True);
        Assert.That(sniffer.DetectHeader("1;2\r\n3;4\r\n", dialect, options), Is.False);
    }

    [Test]
    public void Sniff_PublicApiUsesCandidateOrderForAmbiguousSamples()
    {
        ICsvSniffer sniffer = new CsvSnifferService();
        CsvSnifferOptions options = new CsvSnifferOptions();
        options.CandidateDelimiters = new char[] { '|', ',', ';' };
        options.CandidateQuotes = new char[] { '\'', '"' };

        CsvDialect dialect = sniffer.Sniff("A,B|C;D\n1,2|3;4\n", options);

        Assert.That(dialect.Delimiter, Is.EqualTo('|'));
        Assert.That(dialect.Quote, Is.EqualTo('\''));
        Assert.That(sniffer.DetectHeader("Name,Code\nAda,Blue\n", dialect, options), Is.False);
    }

    [Test]
    public void Sniff_PublicApiAppliesCharacterAndRowSampleBoundsToEveryDecision()
    {
        ICsvSniffer sniffer = new CsvSnifferService();
        CsvSnifferOptions rowOptions = new CsvSnifferOptions();
        rowOptions.MaxSampleRows = 1;
        rowOptions.CandidateDelimiters = new char[] { ',', ';' };
        rowOptions.CandidateQuotes = new char[] { '"', '\'' };
        string sample = "A,B\n'C';'D'\n'E';'F'\n";

        CsvDialect rowBounded = sniffer.Sniff(sample, rowOptions);

        Assert.That(rowBounded.Delimiter, Is.EqualTo(','));
        Assert.That(rowBounded.Quote, Is.EqualTo('"'));
        Assert.That(sniffer.DetectHeader("Name,Age\nAda,42\n", rowBounded, rowOptions), Is.False);

        CsvSnifferOptions characterOptions = new CsvSnifferOptions();
        characterOptions.MaxSampleCharacters = 4;
        characterOptions.CandidateDelimiters = new char[] { '|', ',' };
        CsvDialect characterBounded = sniffer.Sniff("A|B\nC,D\n", characterOptions);

        Assert.That(characterBounded.Delimiter, Is.EqualTo('|'));
    }

    [Test]
    public void Sniff_PublicApiReturnsAnIndependentDialectOverride()
    {
        ICsvSniffer sniffer = new CsvSnifferService();
        CsvSnifferOptions options = new CsvSnifferOptions();
        options.HasOverrideDialect = true;
        options.OverrideDialect.Delimiter = '\t';
        options.OverrideDialect.Quote = '\'';
        options.OverrideDialect.LineTerminator = "\n";
        options.OverrideDialect.NewLineMode = CsvNewLineMode.Lf;

        CsvDialect dialect = sniffer.Sniff("A;B\r\n1;2\r\n", options);
        dialect.Delimiter = '|';

        Assert.That(options.OverrideDialect.Delimiter, Is.EqualTo('\t'));
        Assert.That(dialect.Quote, Is.EqualTo('\''));
        Assert.That(dialect.NewLineMode, Is.EqualTo(CsvNewLineMode.Lf));
    }

    static CsvRow CreateRow(string?[] fields)
    {
        CsvRow row = new CsvRow();
        row.Fields = fields;
        return row;
    }
}
