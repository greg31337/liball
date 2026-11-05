using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvSchemaMetadataTests
{
    [Test]
    public void DetectTypes_ExposesBoundedValueCategoryCountsAndFormatHints()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Present", "Nullable", "Empty", "NullToken", "Amount", "Day", "Clock", "Stamp", "Bytes", "Missing" };
        table.Rows.Add(CreateRow(new string?[] { "A", null, "", "NULL", "1.234,50", "31/12/2024", "01:02:03", "2024-12-31T01:02:03", "0A FF" }));
        table.Rows.Add(CreateRow(new string?[] { "B", "value", "filled", "NULL", "2.000,00", "01/01/2025", "04:05:06", "2025-01-01T04:05:06", "0B-01", "available" }));
        table.Rows.Add(CreateRow(new string?[] { "excluded", "excluded", "excluded", "excluded", "9,99", "02/01/2025", "07:08:09", "2025-01-02T07:08:09", "FF", "excluded" }));
        CsvInferenceOptions options = new CsvInferenceOptions();
        options.MaxSampleRows = 2;
        options.TreatEmptyAsNull = true;
        options.ConversionOptions.HasNullString = true;
        options.ConversionOptions.NullString = "NULL";
        options.ConversionOptions.DecimalSeparator = ",";
        options.ConversionOptions.ThousandsSeparator = ".";
        options.ConversionOptions.DateTimeFormats = new string[] { "dd/MM/yyyy", "HH:mm:ss" };
        ICsvSniffer sniffer = new CsvSnifferService();

        CsvSchema schema = sniffer.DetectTypes(table, options);

        CsvColumn present = schema.Columns[0];
        Assert.That(present.SampledCount, Is.EqualTo(2));
        Assert.That(present.MissingCount, Is.EqualTo(0));
        Assert.That(present.NullCount, Is.EqualTo(0));
        Assert.That(present.EmptyCount, Is.EqualTo(0));
        Assert.That(present.NonNullCount, Is.EqualTo(2));

        CsvColumn nullable = schema.Columns[1];
        Assert.That(nullable.NullCount, Is.EqualTo(1));
        Assert.That(nullable.NonNullCount, Is.EqualTo(1));
        Assert.That(nullable.IsNullable, Is.True);

        CsvColumn empty = schema.Columns[2];
        Assert.That(empty.EmptyCount, Is.EqualTo(1));
        Assert.That(empty.NullCount, Is.EqualTo(0));
        Assert.That(empty.NonNullCount, Is.EqualTo(1));
        Assert.That(empty.IsNullable, Is.True);

        CsvColumn nullToken = schema.Columns[3];
        Assert.That(nullToken.NullCount, Is.EqualTo(2));
        Assert.That(nullToken.NonNullCount, Is.EqualTo(0));

        CsvColumn missing = schema.Columns[9];
        Assert.That(missing.SampledCount, Is.EqualTo(2));
        Assert.That(missing.MissingCount, Is.EqualTo(1));
        Assert.That(missing.NonNullCount, Is.EqualTo(1));
        Assert.That(missing.IsNullable, Is.True);

        Assert.That(schema.Columns[4].TypeKind, Is.EqualTo(CsvTypeKind.Decimal));
        Assert.That(schema.Columns[4].FormatHint, Is.EqualTo("#,##0.00"));
        Assert.That(schema.Columns[5].TypeKind, Is.EqualTo(CsvTypeKind.DateOnly));
        Assert.That(schema.Columns[5].FormatHint, Is.EqualTo("dd/MM/yyyy"));
        Assert.That(schema.Columns[6].TypeKind, Is.EqualTo(CsvTypeKind.TimeOnly));
        Assert.That(schema.Columns[6].FormatHint, Is.EqualTo("HH:mm:ss"));
        Assert.That(schema.Columns[7].TypeKind, Is.EqualTo(CsvTypeKind.DateTime));
        Assert.That(schema.Columns[7].FormatHint, Is.EqualTo("yyyy-MM-ddTHH:mm:ss"));
        Assert.That(schema.Columns[8].TypeKind, Is.EqualTo(CsvTypeKind.Binary));
        Assert.That(schema.Columns[8].FormatHint, Is.EqualTo("X2"));
    }

    [Test]
    public void DetectTypes_ClearsFormatHintWhenSampleFormatsConflict()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Clock" };
        table.Rows.Add(CreateRow(new string?[] { "01:02" }));
        table.Rows.Add(CreateRow(new string?[] { "03:04:05" }));
        ICsvSniffer sniffer = new CsvSnifferService();

        CsvSchema schema = sniffer.DetectTypes(table, new CsvInferenceOptions());

        Assert.That(schema.Columns[0].TypeKind, Is.EqualTo(CsvTypeKind.TimeOnly));
        Assert.That(schema.Columns[0].FormatHint, Is.EqualTo(""));
        Assert.That(schema.Columns[0].SampledCount, Is.EqualTo(2));
        Assert.That(schema.Columns[0].NonNullCount, Is.EqualTo(2));
    }

    static CsvRow CreateRow(string?[] fields)
    {
        CsvRow row = new CsvRow();
        row.Fields = fields;
        return row;
    }
}
