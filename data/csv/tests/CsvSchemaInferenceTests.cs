using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvSchemaInferenceTests
{
    [Test]
    public void InferFromSample_UsesConfiguredSeparatorsAndNullString()
    {
        CsvTable table = CreateTable(new string[] { "Amount", "Flag", "Missing" });
        table.AddRow(CsvRow.Create(new string[] { "1.234,50", "Y", "NULL" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "2.000,00", "N", "NULL" }, 3));
        CsvInferenceOptions options = CsvInferenceOptions.CreateDefault();
        options.ConversionOptions.DecimalSeparator = ",";
        options.ConversionOptions.ThousandsSeparator = ".";
        options.ConversionOptions.HasNullString = true;
        options.ConversionOptions.NullString = "NULL";

        CsvSchema schema = CsvSchema.InferFromSample(table, options);

        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.Decimal));
        Assert.That(schema.GetColumn(0).IsNullable, Is.False);
        Assert.That(schema.GetColumn(1).TypeKind, Is.EqualTo(CsvTypeKind.Boolean));
        Assert.That(schema.GetColumn(1).IsNullable, Is.False);
        Assert.That(schema.GetColumn(2).TypeKind, Is.EqualTo(CsvTypeKind.Null));
        Assert.That(schema.GetColumn(2).IsNullable, Is.True);
    }

    [Test]
    public void InferFromSample_FallsBackToStringWhenValuesConflict()
    {
        CsvTable table = CreateTable(new string[] { "Value" });
        table.AddRow(CsvRow.Create(new string[] { "123" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "abc" }, 3));

        CsvSchema schema = CsvSchema.InferFromSample(table, CsvInferenceOptions.CreateDefault());

        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.String));
        Assert.That(schema.GetColumn(0).IsNullable, Is.False);
    }

    [Test]
    public void InferFromSample_RecognizesActualNullFields()
    {
        CsvTable table = CreateTable(new string[] { "Value" });
        table.AddRow(CsvRow.Create(new string?[] { null }, 2));

        CsvSchema schema = CsvSchema.InferFromSample(table, null);

        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.Null));
        Assert.That(schema.GetColumn(0).IsNullable, Is.True);
    }

    [Test]
    public void InferFromSample_MissingFieldsMakeTrailingColumnsNullable()
    {
        CsvTable table = CreateTable(new string[] { "Id", "Name", "Note" });
        table.AddRow(CsvRow.Create(new string[] { "1", "Ada" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "2", "Grace", "ok" }, 3));

        CsvSchema schema = CsvSchema.InferFromSample(table, CsvInferenceOptions.CreateDefault());

        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(schema.GetColumn(0).IsNullable, Is.False);
        Assert.That(schema.GetColumn(2).TypeKind, Is.EqualTo(CsvTypeKind.String));
        Assert.That(schema.GetColumn(2).IsNullable, Is.True);
    }

    [Test]
    public void InferFromSample_UsesConfiguredDateAndTimeFormats()
    {
        CsvTable table = CreateTable(new string[] { "Day", "Clock" });
        table.AddRow(CsvRow.Create(new string[] { "31/12/2024", "01:02:03" }, 2));
        CsvInferenceOptions options = CsvInferenceOptions.CreateDefault();
        options.ConversionOptions.DateTimeFormats = new string[] { "dd/MM/yyyy", "HH:mm:ss" };

        CsvSchema schema = CsvSchema.InferFromSample(table, options);

        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.DateOnly));
        Assert.That(schema.GetColumn(1).TypeKind, Is.EqualTo(CsvTypeKind.TimeOnly));
    }

    public static CsvTable CreateTable(string[] names)
    {
        CsvHeaderOptions headerOptions = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(names, 1), headerOptions);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        return table;
    }
}
