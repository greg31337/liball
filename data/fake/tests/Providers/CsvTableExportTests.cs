using System.Collections;
using System.Globalization;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using fake;

namespace fake.Tests;

[TestFixture]
public class CsvTableExportTests
{
    public class NestedCell
    {
        public string Label;
        public bool Ready;
    }

    public class CsvRecord
    {
        public decimal Amount;
        public string Empty;
        public string Formula;
        public string Name;
        public NestedCell Nested;
        public string Optional;
        public DateTimeOffset Timestamp;
    }

    public class OtherRecord
    {
        public string Value;
    }

    public class EmptyRecord
    {
        public string Ignored { get; set; }
    }

    [Test]
    public void PublicFieldRowsHaveHeadersQuotingNullsAndFormulaSafety()
    {
        NestedCell nested = new NestedCell();
        nested.Label = "nested";
        nested.Ready = true;
        CsvRecord row = new CsvRecord();
        row.Amount = -12.5m;
        row.Empty = "";
        row.Formula = "=SUM(1,2)";
        row.Name = "A, \"B\"\r\nC";
        row.Nested = nested;
        row.Optional = null;
        row.Timestamp = new DateTimeOffset(
            2024,
            2,
            3,
            4,
            5,
            6,
            TimeSpan.FromHours(-7));

        string csv = Fake.Create(14200).Export().Csv(row);
        string[][] records = Parse(csv, ",");

        Assert.Multiple(() =>
        {
            Assert.That(records, Has.Length.EqualTo(2));
            Assert.That(records[0], Is.EqualTo(new string[]
            {
                "Amount",
                "Empty",
                "Formula",
                "Name",
                "Nested",
                "Optional",
                "Timestamp"
            }));
            Assert.That(records[1][0], Is.EqualTo("-12.5"));
            Assert.That(records[1][1], Is.Empty);
            Assert.That(records[1][2], Is.EqualTo("'=SUM(1,2)"));
            Assert.That(records[1][3], Is.EqualTo(row.Name));
            Assert.That(records[1][5], Is.Empty);
            Assert.That(records[1][6],
                Is.EqualTo("2024-02-03T04:05:06.0000000-07:00"));
            Assert.That(csv, Does.Contain("\r\n"));
        });
        using JsonDocument document = JsonDocument.Parse(records[1][4]);
        Assert.That(document.RootElement.GetProperty("Label").GetString(),
            Is.EqualTo("nested"));
    }

    [Test]
    public void CustomDelimiterNewlineHeaderNullAndSafetyOptionsAreApplied()
    {
        Dictionary<string, object> first =
            new Dictionary<string, object>();
        first["zeta"] = "a;b";
        first["alpha"] = null;
        Dictionary<string, object> second =
            new Dictionary<string, object>();
        second["zeta"] = "=1+1";
        second["alpha"] = "";
        CsvOptions options = new CsvOptions();
        options.IncludeHeader = false;
        options.Delimiter = ';';
        options.NewLine = "\n";
        options.NullValue = "NULL";
        options.GuardSpreadsheetFormula = false;

        string csv = Fake.Create(14201).Export().CsvFromRowsAndOptions(
            new object[] { first, second },
            options);

        Assert.That(csv, Is.EqualTo("\"a;b\";NULL\n=1+1;"));
        Assert.That(Parse(csv, ";"), Is.EqualTo(new string[][]
        {
            new string[] { "a;b", "NULL" },
            new string[] { "=1+1", "" }
        }));
    }

    [Test]
    public void SchemaDictionaryColumnOrderIsPreservedAcrossRows()
    {
        Fake fake = Fake.Create(14202);
        SchemaGenerator schema = fake.Schema();
        SchemaNode root = schema.Object(
            new SchemaField[]
            {
                schema.Field("zeta", schema.Sequence("csv-order")),
                schema.Field("alpha", schema.Constant("value"))
            });
        object first = schema.GenerateFromRoot(root);
        object second = schema.GenerateFromRoot(root);

        string csv = fake.Export().CsvFromRows(new object[] { first, second });
        string[][] records = Parse(csv, ",");

        Assert.Multiple(() =>
        {
            Assert.That(records[0],
                Is.EqualTo(new string[] { "zeta", "alpha" }));
            Assert.That(records[1],
                Is.EqualTo(new string[] { "0", "value" }));
            Assert.That(records[2],
                Is.EqualTo(new string[] { "1", "value" }));
        });
    }

    [Test]
    public void ScalarAndSequenceRowsUseInvariantMachineFormats()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        string scalars;
        string sequence;
        try
        {
            ExportProvider export = Fake.Create(14203).Export();
            scalars = export.CsvFromRows(new object[]
            {
                -1.5m,
                "-1.5",
                new DateOnly(2024, 3, 4),
                true
            });
            sequence = export.Csv(
                (object)new object[] { "left", "right,quoted" });
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        string[][] scalarRecords = Parse(scalars, ",");
        string[][] sequenceRecords = Parse(sequence, ",");

        Assert.Multiple(() =>
        {
            Assert.That(scalarRecords[0],
                Is.EqualTo(new string[] { "Value" }));
            Assert.That(scalarRecords[1][0], Is.EqualTo("-1.5"));
            Assert.That(scalarRecords[2][0], Is.EqualTo("'-1.5"));
            Assert.That(scalarRecords[3][0], Is.EqualTo("2024-03-04"));
            Assert.That(scalarRecords[4][0], Is.EqualTo("true"));
            Assert.That(sequenceRecords[0],
                Is.EqualTo(new string[] { "Column1", "Column2" }));
            Assert.That(sequenceRecords[1],
                Is.EqualTo(new string[] { "left", "right,quoted" }));
        });
    }

    [Test]
    public void NullRowsUseTheConfiguredNullPolicyForEveryColumn()
    {
        CsvRecord row = new CsvRecord();
        row.Amount = 1m;
        row.Empty = "";
        row.Formula = "safe";
        row.Name = "name";
        row.Nested = null;
        row.Optional = null;
        row.Timestamp = DateTimeOffset.UnixEpoch;
        CsvOptions options = new CsvOptions();
        options.NullValue = "<NULL>";

        string csv = Fake.Create(14204).Export().CsvFromRowsAndOptions(
            new object[] { null, row, null },
            options);
        string[][] records = Parse(csv, ",");

        Assert.Multiple(() =>
        {
            Assert.That(records, Has.Length.EqualTo(4));
            Assert.That(records[1],
                Has.All.EqualTo("<NULL>"));
            Assert.That(records[2][1], Is.Empty);
            Assert.That(records[2][4], Is.EqualTo("<NULL>"));
            Assert.That(records[2][5], Is.EqualTo("<NULL>"));
            Assert.That(records[3],
                Has.All.EqualTo("<NULL>"));
        });
    }

    [Test]
    public void InconsistentRowsAndInvalidOptionsAreRejected()
    {
        ExportProvider export = Fake.Create(14205).Export();
        CsvRecord first = new CsvRecord();
        OtherRecord other = new OtherRecord();
        other.Value = "value";
        Dictionary<string, object> dictionary =
            new Dictionary<string, object>();
        dictionary["first"] = 1;
        Dictionary<string, object> extra =
            new Dictionary<string, object>();
        extra["first"] = 1;
        extra["extra"] = 2;
        Hashtable invalidKeys = new Hashtable();
        invalidKeys[1] = "value";
        Array multidimensional = Array.CreateInstance(
            typeof(string),
            new int[] { 1, 1 });
        CsvOptions delimiter = new CsvOptions();
        delimiter.Delimiter = '"';
        CsvOptions newline = new CsvOptions();
        newline.NewLine = "--";
        CsvOptions nullValue = new CsvOptions();
        nullValue.NullValue = null;

        Assert.Multiple(() =>
        {
            Assert.That(() => export.CsvFromRows((object[])null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => export.CsvFromValueAndOptions(first, null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => export.CsvFromRows(
                    new object[] { first, other }),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("row 1"));
            Assert.That(() => export.CsvFromRows(
                    new object[] { dictionary, extra }),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("unexpected column"));
            Assert.That(() => export.Csv((object)invalidKeys),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("column names"));
            Assert.That(() => export.Csv((object)multidimensional),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("one-dimensional"));
            Assert.That(() => export.Csv(new EmptyRecord()),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("public field"));
            Assert.That(() => export.CsvFromValueAndOptions(first, delimiter),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => export.CsvFromValueAndOptions(first, newline),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => export.CsvFromValueAndOptions(first, nullValue),
                Throws.TypeOf<ArgumentNullException>());
        });
    }

    [Test]
    public void EmptyDatasetsAndConfiguredLimitsDoNotConsumeEntropy()
    {
        FakeOptions countOptions = FakeOptions.Create(14206);
        countOptions.MaximumCount = 1;
        Fake countFake = Fake.Create(countOptions);
        Fake countUntouched = Fake.Create(countOptions);
        FakeOptions outputOptions = FakeOptions.Create(14207);
        outputOptions.MaximumOutputSize = 4;
        Fake outputFake = Fake.Create(outputOptions);
        Fake outputUntouched = Fake.Create(outputOptions);

        Assert.Multiple(() =>
        {
            Assert.That(countFake.Export().CsvFromRows(new object[0]), Is.Empty);
            Assert.That(() => countFake.Export().CsvFromRows(
                    new object[] { 1, 2 }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputFake.Export().Csv("value"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(countFake.Export().RandomValue.Long(),
                Is.EqualTo(countUntouched.Export().RandomValue.Long()));
            Assert.That(outputFake.Export().RandomValue.Long(),
                Is.EqualTo(outputUntouched.Export().RandomValue.Long()));
        });
    }

    static string[][] Parse(string value, string delimiter)
    {
        List<string[]> records = new List<string[]>();
        using StringReader reader = new StringReader(value);
        using TextFieldParser parser = new TextFieldParser(reader);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(delimiter);
        parser.HasFieldsEnclosedInQuotes = true;
        while (!parser.EndOfData)
        {
            records.Add(parser.ReadFields());
        }
        return records.ToArray();
    }
}
