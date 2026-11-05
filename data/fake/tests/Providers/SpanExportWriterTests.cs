using fake;

namespace fake.Tests;

[TestFixture]
public class SpanExportWriterTests
{
    public class SpanRow
    {
        public int Id;
        public string Name;
    }

    [Test]
    public void WritersReturnExactLengthsAndMatchAllocatedFormats()
    {
        Fake fake = Fake.Create(14600);
        IExportProvider export = fake.Export();
        SpanRow row = new SpanRow();
        row.Id = 7;
        row.Name = "A,B";
        CsvOptions rowOptions = new CsvOptions();
        rowOptions.IncludeHeader = false;
        string expectedJsonString = export.JsonString("a\n\"");
        string expectedJsonScalar = "null";
        string expectedCsvField = export.CsvField("=1,2");
        string expectedCsvRow = export.CsvFromValueAndOptions(
            row,
            rowOptions);
        string expectedXml = export.XmlElement("note", "<&>");
        string expectedVCard = "NOTE:a\\,b\\;\\n\r\n";
        string expectedSqlIdentifier =
            fake.Database().SqlIdentifierFromValue("a\"b");
        string expectedSqlLiteral =
            fake.Database().SqlStringLiteral("O'Brien");
        string expectedSqlInsert = export.SqlInsertFromTableAndValue(
            "order",
            row);
        char[] jsonString = new char[expectedJsonString.Length];
        char[] jsonScalar = new char[expectedJsonScalar.Length];
        char[] csvField = new char[expectedCsvField.Length];
        char[] csvRow = new char[expectedCsvRow.Length];
        char[] xml = new char[expectedXml.Length];
        char[] vCard = new char[expectedVCard.Length];
        char[] sqlIdentifier = new char[expectedSqlIdentifier.Length];
        char[] sqlLiteral = new char[expectedSqlLiteral.Length];
        char[] sqlInsert = new char[expectedSqlInsert.Length];

        int jsonStringWritten = export.WriteJsonString(
            jsonString,
            "a\n\"");
        int jsonScalarWritten = export.WriteJsonScalar(jsonScalar, null);
        int csvFieldWritten = export.WriteCsvField(csvField, "=1,2");
        int csvRowWritten = export.WriteCsvRow(csvRow, row);
        int xmlWritten = export.WriteXmlElement(xml, "note", "<&>");
        int vCardWritten = export.WriteVCardLine(
            vCard,
            "NOTE",
            "a,b;\n");
        int sqlIdentifierWritten = export.WriteSqlIdentifier(
            sqlIdentifier,
            "a\"b");
        int sqlLiteralWritten = export.WriteSqlLiteral(
            sqlLiteral,
            "O'Brien");
        int sqlInsertWritten = export.WriteSqlInsert(
            sqlInsert,
            "order",
            row);

        Assert.Multiple(() =>
        {
            Assert.That(jsonStringWritten,
                Is.EqualTo(expectedJsonString.Length));
            Assert.That(new string(jsonString),
                Is.EqualTo(expectedJsonString));
            Assert.That(jsonScalarWritten,
                Is.EqualTo(expectedJsonScalar.Length));
            Assert.That(new string(jsonScalar),
                Is.EqualTo(expectedJsonScalar));
            Assert.That(csvFieldWritten,
                Is.EqualTo(expectedCsvField.Length));
            Assert.That(new string(csvField), Is.EqualTo(expectedCsvField));
            Assert.That(csvRowWritten, Is.EqualTo(expectedCsvRow.Length));
            Assert.That(new string(csvRow), Is.EqualTo(expectedCsvRow));
            Assert.That(xmlWritten, Is.EqualTo(expectedXml.Length));
            Assert.That(new string(xml), Is.EqualTo(expectedXml));
            Assert.That(vCardWritten, Is.EqualTo(expectedVCard.Length));
            Assert.That(new string(vCard), Is.EqualTo(expectedVCard));
            Assert.That(sqlIdentifierWritten,
                Is.EqualTo(expectedSqlIdentifier.Length));
            Assert.That(new string(sqlIdentifier),
                Is.EqualTo(expectedSqlIdentifier));
            Assert.That(sqlLiteralWritten,
                Is.EqualTo(expectedSqlLiteral.Length));
            Assert.That(new string(sqlLiteral),
                Is.EqualTo(expectedSqlLiteral));
            Assert.That(sqlInsertWritten,
                Is.EqualTo(expectedSqlInsert.Length));
            Assert.That(new string(sqlInsert),
                Is.EqualTo(expectedSqlInsert));
        });
    }

    [Test]
    public void UndersizedDestinationIsRejectedWithoutPartialWrites()
    {
        IExportProvider export = Fake.Create(14601).Export();
        string expected = export.SqlInsertFromTableAndValue(
            "items",
            "O'Brien");
        char[] destination = new string(
            '#',
            expected.Length - 1).ToCharArray();
        char[] unchanged = new string(
            '#',
            expected.Length - 1).ToCharArray();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => export.WriteSqlInsert(
                destination,
                "items",
                "O'Brien"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ParamName, Is.EqualTo("destination"));
            Assert.That(destination, Is.EqualTo(unchanged));
        });
    }

    [Test]
    public void EveryStructuredWriterRejectsAnUndersizedSpanAtomically()
    {
        IExportProvider export = Fake.Create(14604).Export();
        SpanRow row = new SpanRow();
        row.Id = 8;
        row.Name = "A,B";

        AssertAtomicFailure(
            export.JsonString("a\n\""),
            destination => export.WriteJsonString(destination, "a\n\""));
        AssertAtomicFailure(
            export.Json((object)42),
            destination => export.WriteJsonScalar(destination, 42));
        AssertAtomicFailure(
            export.CsvField("a,b"),
            destination => export.WriteCsvField(destination, "a,b"));
        CsvOptions options = new CsvOptions();
        options.IncludeHeader = false;
        AssertAtomicFailure(
            export.CsvFromValueAndOptions(row, options),
            destination => export.WriteCsvRow(destination, row));
        AssertAtomicFailure(
            export.XmlElement("note", "<&>"),
            destination => export.WriteXmlElement(destination, "note", "<&>"));
        AssertAtomicFailure(
            "NOTE:a\\,b\\;\\n\r\n",
            destination => export.WriteVCardLine(
                destination,
                "NOTE",
                "a,b;\n"));
        AssertAtomicFailure(
            "\"a\"\"b\"",
            destination => export.WriteSqlIdentifier(destination, "a\"b"));
        AssertAtomicFailure(
            "'O''Brien'",
            destination => export.WriteSqlLiteral(destination, "O'Brien"));
        AssertAtomicFailure(
            export.SqlInsertFromTableAndValue("items", row),
            destination => export.WriteSqlInsert(destination, "items", row));
    }

    [Test]
    public void OversizedDestinationSuffixRemainsUntouched()
    {
        IExportProvider export = Fake.Create(14602).Export();
        string expected = export.CsvField("a,b");
        char[] destination = new string(
            '#',
            expected.Length + 3).ToCharArray();

        int written = export.WriteCsvField(destination, "a,b");

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(new string(destination, 0, written),
                Is.EqualTo(expected));
            Assert.That(new string(destination, written, 3),
                Is.EqualTo("###"));
        });
    }

    [Test]
    public void InvalidInputsLeaveDestinationUntouched()
    {
        IExportProvider export = Fake.Create(14603).Export();
        char[] destination = "########".ToCharArray();
        char[] unchanged = "########".ToCharArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => export.WriteJsonScalar(
                    destination,
                    new object[] { 1 }),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("value"));
            Assert.That(
                () => export.WriteVCardLine(
                    destination,
                    "BAD:NAME",
                    "value"),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("name"));
            Assert.That(
                () => export.WriteSqlLiteral(
                    destination,
                    "bad\0value"),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("value"));
        });

        Assert.That(destination, Is.EqualTo(unchanged));
    }

    static void AssertAtomicFailure(
        string expected,
        Action<char[]> write)
    {
        char[] destination = new string(
            '#',
            expected.Length - 1).ToCharArray();
        char[] unchanged = (char[])destination.Clone();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => write(destination));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ParamName, Is.EqualTo("destination"));
            Assert.That(destination, Is.EqualTo(unchanged));
        });
    }
}
