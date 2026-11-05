using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using fake;

namespace fake.Tests;

[TestFixture]
public class SqlInsertExportTests
{
    public enum SampleStatus
    {
        First = 0,
        Second = 1
    }

    public class SqlNested
    {
        public string Label;
        public bool Ready;
    }

    public class SqlRecord
    {
        public bool Active;
        public decimal Amount;
        public byte[] Binary;
        public DateTimeOffset Created;
        public int Id;
        public SqlNested Metadata;
        public string Name;
        public object Optional;
        public SampleStatus Status;

        public string IgnoredProperty { get; set; }
    }

    public class OtherRecord
    {
        public string Value;
    }

    public class EmptyRecord
    {
        public string Ignored { get; set; }
    }

    public class CyclicRecord
    {
        public object Value;
    }

    [Test]
    public void PublicFieldsUseDeterministicOrderAndTypedInvariantLiterals()
    {
        SqlNested nested = new SqlNested();
        nested.Label = "nested";
        nested.Ready = true;
        SqlRecord row = new SqlRecord();
        row.Active = true;
        row.Amount = -12.5m;
        row.Binary = new byte[] { 0, 255 };
        row.Created = new DateTimeOffset(
            2024,
            1,
            2,
            3,
            4,
            5,
            TimeSpan.FromHours(5.5));
        row.Id = 7;
        row.Metadata = nested;
        row.Name = "O'Brien";
        row.Optional = null;
        row.Status = SampleStatus.Second;
        row.IgnoredProperty = "not exported";
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        string first;
        string second;
        try
        {
            ExportProvider export = Fake.Create(14400).Export();
            first = export.SqlInsertFromTableAndValue("order", row);
            second = export.SqlInsertFromTableAndValue("order", row);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.That(first, Is.EqualTo(
            "INSERT INTO \"order\" (\"Active\", \"Amount\", \"Binary\", " +
            "\"Created\", \"Id\", \"Metadata\", \"Name\", \"Optional\", " +
            "\"Status\") VALUES (TRUE, -12.5, X'00FF', " +
            "'2024-01-02T03:04:05.0000000+05:30', 7, " +
            "'{\"Label\":\"nested\",\"Ready\":true}', 'O''Brien', NULL, " +
            "'Second');"));
        Assert.That(second, Is.EqualTo(first));
        Assert.That(first, Does.Not.Contain("IgnoredProperty"));
    }

    [Test]
    public void DialectsApplyTheirIdentifierBooleanTextAndBinaryShapes()
    {
        OrderedDictionary row = new OrderedDictionary();
        row["na\"me`]"] = "C:\\temp\\O'Brien 東京";
        row["enabled"] = true;
        row["payload"] = new byte[] { 10, 188 };
        ExportProvider export = Fake.Create(14401).Export();
        string table = "ta\"ble`]";

        string ansi = export.SqlInsertFromTableAndValueAndDialect(table, row, SqlDialect.Ansi);
        string postgreSql = export.SqlInsertFromTableAndValueAndDialect(
            table,
            row,
            SqlDialect.PostgreSql);
        string mySql = export.SqlInsertFromTableAndValueAndDialect(table, row, SqlDialect.MySql);
        string sqlServer = export.SqlInsertFromTableAndValueAndDialect(
            table,
            row,
            SqlDialect.SqlServer);
        string sqlite = export.SqlInsertFromTableAndValueAndDialect(table, row, SqlDialect.Sqlite);

        Assert.Multiple(() =>
        {
            Assert.That(ansi, Is.EqualTo(
                "INSERT INTO \"ta\"\"ble`]\" (\"na\"\"me`]\", " +
                "\"enabled\", \"payload\") VALUES " +
                "('C:\\temp\\O''Brien 東京', TRUE, X'0ABC');"));
            Assert.That(postgreSql, Is.EqualTo(
                "INSERT INTO \"ta\"\"ble`]\" (\"na\"\"me`]\", " +
                "\"enabled\", \"payload\") VALUES " +
                "('C:\\temp\\O''Brien 東京', TRUE, " +
                "decode('0ABC', 'hex'));"));
            Assert.That(mySql, Is.EqualTo(
                "INSERT INTO `ta\"ble``]` (`na\"me``]`, `enabled`, " +
                "`payload`) VALUES " +
                "('C:\\\\temp\\\\O''Brien 東京', 1, X'0ABC');"));
            Assert.That(sqlServer, Is.EqualTo(
                "INSERT INTO [ta\"ble`]]] ([na\"me`]]], [enabled], " +
                "[payload]) VALUES " +
                "(N'C:\\temp\\O''Brien 東京', 1, 0x0ABC);"));
            Assert.That(sqlite, Is.EqualTo(
                "INSERT INTO \"ta\"\"ble`]\" (\"na\"\"me`]\", " +
                "\"enabled\", \"payload\") VALUES " +
                "('C:\\temp\\O''Brien 東京', 1, X'0ABC');"));
        });
    }

    [Test]
    public void SchemaRowsPreserveColumnOrderAndSequenceState()
    {
        Fake fake = Fake.Create(14402);
        SchemaGenerator schema = fake.Schema();
        SchemaNode node = schema.Object(
            new SchemaField[]
            {
                schema.Field("sequence", schema.Sequence("sql-order")),
                schema.Field("label", schema.Constant("item"))
            });
        object first = schema.GenerateFromRoot(node);
        object second = schema.GenerateFromRoot(node);

        string sql = fake.Export().SqlInsertFromTableAndRows(
            "schema rows",
            new object[] { first, null, second });

        Assert.That(sql, Is.EqualTo(
            "INSERT INTO \"schema rows\" (\"sequence\", \"label\") " +
            "VALUES (0, 'item'), (NULL, NULL), (1, 'item');"));
    }

    [Test]
    public void ScalarAndSequenceRowsHaveDeterministicColumns()
    {
        ExportProvider export = Fake.Create(14403).Export();

        string scalars = export.SqlInsertFromTableAndRows(
            "values",
            new object[] { 1, false, "x", DBNull.Value });
        string sequence = export.SqlInsertFromTableAndValue(
            "pair",
            (object)new object[] { "left", 2 });
        string empty = export.SqlInsertFromTableAndRows("values", new object[0]);

        Assert.Multiple(() =>
        {
            Assert.That(scalars, Is.EqualTo(
                "INSERT INTO \"values\" (\"Value\") VALUES " +
                "(1), (FALSE), ('x'), (NULL);"));
            Assert.That(sequence, Is.EqualTo(
                "INSERT INTO \"pair\" (\"Column1\", \"Column2\") " +
                "VALUES ('left', 2);"));
            Assert.That(empty, Is.Empty);
        });
    }

    [Test]
    public void InjectionShapedTextRemainsInsideQuotedIdentifiersAndValues()
    {
        OrderedDictionary row = new OrderedDictionary();
        row["display_name"] = "x'); DROP TABLE users;--";

        string sql = Fake.Create(14404).Export().SqlInsertFromTableAndValue(
            "users\"; DROP TABLE users;--",
            row);

        Assert.That(sql, Is.EqualTo(
            "INSERT INTO \"users\"\"; DROP TABLE users;--\" " +
            "(\"display_name\") VALUES " +
            "('x''); DROP TABLE users;--');"));
    }

    [Test]
    public void InvalidShapesDialectsAndValuesAreRejected()
    {
        ExportProvider export = Fake.Create(14405).Export();
        SqlRecord first = new SqlRecord();
        OtherRecord other = new OtherRecord();
        Hashtable invalidKeys = new Hashtable();
        invalidKeys[1] = "value";
        OrderedDictionary dictionary = new OrderedDictionary();
        dictionary["first"] = 1;
        OrderedDictionary extra = new OrderedDictionary();
        extra["first"] = 1;
        extra["extra"] = 2;
        Array multidimensional = Array.CreateInstance(
            typeof(string),
            new int[] { 1, 1 });
        CyclicRecord cycle = new CyclicRecord();
        cycle.Value = cycle;

        Assert.Multiple(() =>
        {
            Assert.That(() => export.SqlInsertFromTableAndValue(null, 1),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => export.SqlInsertFromTableAndValue("", 1),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => export.SqlInsertFromTableAndValue("bad\nname", 1),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => export.SqlInsertFromTableAndRows("t", (object[])null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => export.SqlInsertFromTableAndValue("t", new EmptyRecord()),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("public field"));
            Assert.That(() => export.SqlInsertFromTableAndValue("t", invalidKeys),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("column names"));
            Assert.That(() => export.SqlInsertFromTableAndValue(
                    "t",
                    (object)multidimensional),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("one-dimensional"));
            Assert.That(() => export.SqlInsertFromTableAndRows(
                    "t",
                    new object[] { first, other }),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("row 1"));
            Assert.That(() => export.SqlInsertFromTableAndRows(
                    "t",
                    new object[] { dictionary, extra }),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("unexpected column"));
            Assert.That(() => export.SqlInsertFromTableAndValue("t", double.NaN),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => export.SqlInsertFromTableAndValue("t", "\ud800"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => export.SqlInsertFromTableAndValue("t", "a\0b"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => export.SqlInsertFromTableAndValue("t", cycle),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("cycle"));
            Assert.That(() => export.SqlInsertFromTableAndValueAndDialect(
                    "t",
                    1,
                    (SqlDialect)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void CountAndOutputLimitsFailWithoutConsumingEntropy()
    {
        FakeOptions countOptions = FakeOptions.Create(14406);
        countOptions.MaximumCount = 1;
        Fake countFake = Fake.Create(countOptions);
        Fake countUntouched = Fake.Create(countOptions);
        FakeOptions outputOptions = FakeOptions.Create(14407);
        outputOptions.MaximumOutputSize = 10;
        Fake outputFake = Fake.Create(outputOptions);
        Fake outputUntouched = Fake.Create(outputOptions);

        Assert.Multiple(() =>
        {
            Assert.That(() => countFake.Export().SqlInsertFromTableAndRows(
                    "t",
                    new object[] { 1, 2 }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputFake.Export().SqlInsertFromTableAndValue("t", 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(countFake.Export().RandomValue.Long(),
                Is.EqualTo(countUntouched.Export().RandomValue.Long()));
            Assert.That(outputFake.Export().RandomValue.Long(),
                Is.EqualTo(outputUntouched.Export().RandomValue.Long()));
        });
    }
}
