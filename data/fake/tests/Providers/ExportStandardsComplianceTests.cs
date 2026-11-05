using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using fake;

namespace fake.Tests;

[TestFixture]
public class ExportStandardsComplianceTests
{
    public class OrderingRecord
    {
        public string Zebra = "";
        public object? Missing;
        public string Alpha = "";
    }

    [Test]
    public void Rfc8259StringsRoundTripEscapesAndRejectMalformedUnicode()
    {
        IExportProvider export = Fake.Create(14700).Export();
        string value = "quote=\" slash=\\ controls=\b\f\n\r\t\0 é 東京 😀";
        string json = export.JsonString(value);
        char[] exact = new char[json.Length];

        int written = export.WriteJsonString(exact, value);
        using JsonDocument document = JsonDocument.Parse(new string(exact));

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(json.Length));
            Assert.That(new string(exact), Is.EqualTo(json));
            Assert.That(document.RootElement.GetString(), Is.EqualTo(value));
            Assert.That(json, Does.Contain("\\u0022").Or.Contain("\\\""));
            Assert.That(json, Does.Contain("\\\\")
                .And.Contain("\\b")
                .And.Contain("\\f")
                .And.Contain("\\n")
                .And.Contain("\\r")
                .And.Contain("\\t")
                .And.Contain("\\u0000"));
            Assert.That(() => export.JsonString("\ud800"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => export.JsonString("\udc00"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Rfc4180FieldsRoundTripDelimitersQuotesAndRecordBreaks()
    {
        IExportProvider export = Fake.Create(14701).Export();
        string value = "alpha,\"quoted\"\r\nomega";
        string field = export.CsvFieldFromValueAndGuardSpreadsheetFormula(
            value,
            false);
        string[][] records = ParseCsv(field);

        Assert.Multiple(() =>
        {
            Assert.That(field,
                Is.EqualTo("\"alpha,\"\"quoted\"\"\r\nomega\""));
            Assert.That(records, Has.Length.EqualTo(1));
            Assert.That(records[0], Is.EqualTo(new string[] { value }));
            Assert.That(() => export.CsvField("\ud800"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [TestCase("=1+1")]
    [TestCase("  +cmd")]
    [TestCase("-2")]
    [TestCase("@name")]
    [TestCase("\tcommand")]
    [TestCase("\rcommand")]
    [TestCase("\ncommand")]
    public void SpreadsheetFormulaGuardsSurviveCsvParsing(string value)
    {
        IExportProvider export = Fake.Create(14702).Export();
        string guarded = export.CsvField(value);
        string unguarded = export.CsvFieldFromValueAndGuardSpreadsheetFormula(
            value,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(ParseCsv(guarded)[0][0], Is.EqualTo("'" + value));
            Assert.That(ParseCsv(unguarded)[0][0], Is.EqualTo(value));
        });
    }

    [Test]
    public void Xml10ElementRoundTripsEntitiesAndRejectsForbiddenCharacters()
    {
        IExportProvider export = Fake.Create(14703).Export();
        string value = "&<>\"'\t\n é 東京 😀";
        string xml = export.XmlElement("message", value);
        char[] exact = new char[xml.Length];

        int written = export.WriteXmlElement(exact, "message", value);
        XElement parsed = XElement.Parse(new string(exact));

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(xml.Length));
            Assert.That(parsed.Name.LocalName, Is.EqualTo("message"));
            Assert.That(parsed.Value, Is.EqualTo(value));
            Assert.That(xml, Does.Contain("&amp;")
                .And.Contain("&lt;")
                .And.Contain("&gt;")
                .And.Contain("&quot;")
                .And.Contain("&apos;"));
            Assert.That(export.XmlElement("empty", ""),
                Is.EqualTo("<empty></empty>"));
            Assert.That(() => export.XmlElement("message", "bad\u0001text"),
                Throws.TypeOf<XmlException>());
            Assert.That(() => export.XmlElement("message", "\ud800"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Rfc6350LineRoundTripsEscapesAndFoldsOnUtf8Boundaries()
    {
        IExportProvider export = Fake.Create(14704).Export();
        string value = "a,b;c\\d\ne " +
            string.Concat(Enumerable.Repeat("東京😀", 20));
        string line = WriteVCardLine(export, "NOTE", value);
        string[] physicalLines = line.Split(
            new string[] { "\r\n" },
            StringSplitOptions.None);
        string unfolded = line.Replace("\r\n ", "");
        int separator = unfolded.IndexOf(':');
        string encoded = unfolded.Substring(
            separator + 1,
            unfolded.Length - separator - 3);

        int index = 0;
        while (index < physicalLines.Length - 1)
        {
            Assert.That(Encoding.UTF8.GetByteCount(physicalLines[index]),
                Is.LessThanOrEqualTo(75));
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(unfolded, Does.StartWith("NOTE:"));
            Assert.That(unfolded, Does.EndWith("\r\n"));
            Assert.That(physicalLines.Any(item => item.StartsWith(" ")),
                Is.True);
            Assert.That(DecodeVCardText(encoded), Is.EqualTo(value));
            Assert.That(() => export.WriteVCardLine(
                    new char[100],
                    "NOTE",
                    "\ud800"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Yaml122FixturesLoadAsScalarsSequencesAndMappings()
    {
        IExportProvider export = Fake.Create(14705).Export();

        int index = 0;
        while (index < 64)
        {
            object scalar = ParseYaml(export.YamlFromKind(YamlTextKind.Scalar));
            object sequence = ParseYaml(export.YamlFromKind(YamlTextKind.Sequence));
            object mapping = ParseYaml(export.YamlFromKind(YamlTextKind.Mapping));

            Assert.Multiple(() =>
            {
                Assert.That(scalar,
                    Is.AnyOf((object)"Sample: value", (object)42, (object)true));
                Assert.That(sequence, Is.InstanceOf<List<object>>());
                Assert.That((List<object>)sequence, Has.Count.GreaterThanOrEqualTo(2));
                Assert.That(mapping, Is.InstanceOf<Dictionary<string, object>>());
                Assert.That((Dictionary<string, object>)mapping,
                    Is.Not.Empty);
            });
            index++;
        }
    }

    [Test]
    public void StructuredFormatsKeepNullEmptyAndMemberOrderDistinct()
    {
        OrderingRecord value = new OrderingRecord();
        value.Zebra = "last";
        value.Missing = null;
        value.Alpha = "";
        ExportProvider export = Fake.Create(14706).Export();
        CsvOptions options = new CsvOptions();
        options.NullValue = "<null>";
        string json = export.Json(value);
        string csv = export.CsvFromValueAndOptions(value, options);
        string xml = export.Xml(value);
        string sql = export.SqlInsertFromTableAndValue("items", value);
        using JsonDocument jsonDocument = JsonDocument.Parse(json);
        XDocument xmlDocument = XDocument.Parse(xml);
        XElement xmlRoot = xmlDocument.Root ??
            throw new AssertionException("The XML document has no root.");

        Assert.Multiple(() =>
        {
            Assert.That(jsonDocument.RootElement.EnumerateObject()
                    .Select(item => item.Name),
                Is.EqualTo(new string[] { "Alpha", "Missing", "Zebra" }));
            Assert.That(jsonDocument.RootElement.GetProperty("Alpha").GetString(),
                Is.Empty);
            Assert.That(jsonDocument.RootElement.GetProperty("Missing").ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(ParseCsv(csv), Is.EqualTo(new string[][]
            {
                new string[] { "Alpha", "Missing", "Zebra" },
                new string[] { "", "<null>", "last" }
            }));
            Assert.That(xmlRoot.Elements()
                    .Select(item => item.Name.LocalName),
                Is.EqualTo(new string[] { "Alpha", "Missing", "Zebra" }));
            Assert.That(xmlRoot.Element("Alpha")?.Value, Is.Empty);
            Assert.That(xmlRoot.Element("Missing")?.Value, Is.Empty);
            Assert.That(sql, Is.EqualTo(
                "INSERT INTO \"items\" (\"Alpha\", \"Missing\", \"Zebra\") " +
                "VALUES ('', NULL, 'last');"));
        });
    }

    [TestCase(SqlDialect.Ansi,
        "INSERT INTO \"ta\"\"ble`]\" (\"co\"\"l`]\") VALUES ('');")]
    [TestCase(SqlDialect.PostgreSql,
        "INSERT INTO \"ta\"\"ble`]\" (\"co\"\"l`]\") VALUES ('');")]
    [TestCase(SqlDialect.MySql,
        "INSERT INTO `ta\"ble``]` (`co\"l``]`) VALUES ('');")]
    [TestCase(SqlDialect.SqlServer,
        "INSERT INTO [ta\"ble`]]] ([co\"l`]]]) VALUES (N'');")]
    [TestCase(SqlDialect.Sqlite,
        "INSERT INTO \"ta\"\"ble`]\" (\"co\"\"l`]\") VALUES ('');")]
    public void SqlDialectsQuoteEveryIdentifierDelimiter(
        SqlDialect dialect,
        string expected)
    {
        Dictionary<string, object> row = new Dictionary<string, object>();
        row["co\"l`]"] = "";

        string sql = Fake.Create(14707).Export()
            .SqlInsertFromTableAndValueAndDialect(
                "ta\"ble`]",
                row,
                dialect);

        Assert.That(sql, Is.EqualTo(expected));
    }

    static string[][] ParseCsv(string value)
    {
        List<string[]> records = new List<string[]>();
        List<string> fields = new List<string>();
        StringBuilder field = new StringBuilder();
        bool quoted = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (quoted)
            {
                if (character == '"')
                {
                    if (index + 1 < value.Length && value[index + 1] == '"')
                    {
                        field.Append('"');
                        index += 2;
                        continue;
                    }
                    quoted = false;
                }
                else
                {
                    field.Append(character);
                }
                index++;
                continue;
            }
            if (character == '"' && field.Length == 0)
            {
                quoted = true;
                index++;
                continue;
            }
            if (character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                index++;
                continue;
            }
            if (character == '\r' &&
                index + 1 < value.Length && value[index + 1] == '\n')
            {
                fields.Add(field.ToString());
                records.Add(fields.ToArray());
                fields.Clear();
                field.Clear();
                index += 2;
                continue;
            }
            field.Append(character);
            index++;
        }
        if (field.Length > 0 || fields.Count > 0 || records.Count == 0)
        {
            fields.Add(field.ToString());
            records.Add(fields.ToArray());
        }
        return records.ToArray();
    }

    static string WriteVCardLine(
        IExportProvider export,
        string name,
        string value)
    {
        string allocated = name + ":" + EscapeVCardText(value) + "\r\n";
        int folds = FoldCount(allocated);
        char[] destination = new char[allocated.Length + folds * 3];
        int written = export.WriteVCardLine(destination, name, value);
        return new string(destination, 0, written);
    }

    static int FoldCount(string value)
    {
        int folds = 0;
        int bytes = 0;
        int limit = 75;
        int index = 0;
        while (index < value.Length - 2)
        {
            int units = char.IsHighSurrogate(value[index]) ? 2 : 1;
            int count = Encoding.UTF8.GetByteCount(value.AsSpan(index, units));
            if (bytes + count > limit)
            {
                folds++;
                bytes = 0;
                limit = 74;
            }
            bytes += count;
            index += units;
        }
        return folds;
    }

    static string EscapeVCardText(string value)
    {
        return value.Replace("\\", "\\\\")
            .Replace(",", "\\,")
            .Replace(";", "\\;")
            .Replace("\r\n", "\\n")
            .Replace("\r", "\\n")
            .Replace("\n", "\\n");
    }

    static string DecodeVCardText(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != '\\')
            {
                result.Append(value[index]);
                index++;
                continue;
            }
            index++;
            if (value[index] == 'n' || value[index] == 'N')
                result.Append('\n');
            else
                result.Append(value[index]);
            index++;
        }
        return result.ToString();
    }

    static object ParseYaml(string value)
    {
        string[] lines = value.Split(
            new char[] { '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        return ParseYamlNode(lines, ref index, 0);
    }

    static object ParseYamlNode(string[] lines, ref int index, int indent)
    {
        string content = lines[index].Substring(indent);
        if (content.StartsWith("- "))
            return ParseYamlSequence(lines, ref index, indent);
        if (content.Contains(':') && !content.StartsWith("\""))
            return ParseYamlMapping(lines, ref index, indent);
        index++;
        return ParseYamlScalar(content);
    }

    static object ParseYamlSequence(string[] lines, ref int index, int indent)
    {
        List<object> result = new List<object>();
        while (index < lines.Length &&
            CountIndent(lines[index]) == indent &&
            lines[index].Substring(indent).StartsWith("- "))
        {
            string content = lines[index].Substring(indent + 2);
            int separator = content.IndexOf(": ", StringComparison.Ordinal);
            if (separator < 0)
            {
                result.Add(ParseYamlScalar(content));
                index++;
            }
            else
            {
                Dictionary<string, object> mapping =
                    new Dictionary<string, object>();
                AddYamlPair(mapping, content);
                index++;
                while (index < lines.Length &&
                    CountIndent(lines[index]) == indent + 2)
                {
                    AddYamlPair(mapping, lines[index].Substring(indent + 2));
                    index++;
                }
                result.Add(mapping);
            }
        }
        return result;
    }

    static object ParseYamlMapping(string[] lines, ref int index, int indent)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        while (index < lines.Length && CountIndent(lines[index]) == indent)
        {
            string content = lines[index].Substring(indent);
            int separator = content.IndexOf(':');
            string key = content.Substring(0, separator);
            string remainder = content.Substring(separator + 1).TrimStart();
            index++;
            if (remainder.Length == 0)
                result[key] = ParseYamlNode(lines, ref index, indent + 2);
            else
                result[key] = ParseYamlScalar(remainder);
        }
        return result;
    }

    static void AddYamlPair(
        Dictionary<string, object> mapping,
        string content)
    {
        int separator = content.IndexOf(": ", StringComparison.Ordinal);
        mapping[content.Substring(0, separator)] =
            ParseYamlScalar(content.Substring(separator + 2));
    }

    static object ParseYamlScalar(string value)
    {
        if (value.StartsWith("\"") && value.EndsWith("\""))
            return JsonSerializer.Deserialize<string>(value) ??
                throw new JsonException("The YAML string scalar was null.");
        if (value == "true")
            return true;
        if (value == "false")
            return false;
        if (int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int number))
            return number;
        return value;
    }

    static int CountIndent(string value)
    {
        int result = 0;
        while (result < value.Length && value[result] == ' ')
            result++;
        return result;
    }
}
