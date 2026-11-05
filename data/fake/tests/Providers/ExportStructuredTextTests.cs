using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO;
using fake;

namespace fake.Tests;

[TestFixture]
public class ExportStructuredTextTests
{
    static string[] YamlScalars = new string[]
    {
        "\"Sample: value\"", "42", "true"
    };

    static string[] YamlSequences = new string[]
    {
        "- sample\n- \"quoted value\"\n- 42\n",
        "- name: alpha\n  active: true\n- name: beta\n  active: false\n"
    };

    static string[] YamlMappings = new string[]
    {
        "name: sample\nnote: \"quoted: value\"\nactive: true\n",
        "item:\n  id: 42\n  label: example\n"
    };

    [Test]
    public void JsonKindsAlwaysProduceParseableRequestedShapes()
    {
        ExportProvider export = Fake.Create(12000).Export();
        JsonTextKind[] kinds = Enum.GetValues<JsonTextKind>();
        JsonValueKind[] expected = new JsonValueKind[]
        {
            JsonValueKind.String,
            JsonValueKind.Object,
            JsonValueKind.Array
        };

        int kindIndex = 0;
        while (kindIndex < kinds.Length)
        {
            int sampleIndex = 0;
            while (sampleIndex < 64)
            {
                using JsonDocument document = JsonDocument.Parse(
                    export.JsonFromKind(kinds[kindIndex]));
                if (kinds[kindIndex] == JsonTextKind.Scalar)
                {
                    Assert.That(document.RootElement.ValueKind,
                        Is.AnyOf(
                            JsonValueKind.String,
                            JsonValueKind.Number,
                            JsonValueKind.True));
                }
                else
                {
                    Assert.That(document.RootElement.ValueKind,
                        Is.EqualTo(expected[kindIndex]));
                }
                sampleIndex++;
            }
            kindIndex++;
        }

        int randomIndex = 0;
        while (randomIndex < 128)
        {
            Assert.That(() => JsonDocument.Parse(export.JsonFromNoArguments()).Dispose(),
                Throws.Nothing);
            randomIndex++;
        }
    }

    [Test]
    public void CsvKindsProduceConsistentParsedRecords()
    {
        ExportProvider export = Fake.Create(12001).Export();

        int index = 0;
        while (index < 64)
        {
            string[][] row = ParseCsv(export.CsvFromKind(CsvTextKind.Row));
            string[][] document = ParseCsv(export.CsvFromKind(CsvTextKind.Document));

            Assert.Multiple(() =>
            {
                Assert.That(row, Has.Length.EqualTo(1));
                Assert.That(row[0], Has.Length.EqualTo(3));
                Assert.That(document.Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(document[0], Has.Length.EqualTo(2));
            });
            int documentIndex = 1;
            while (documentIndex < document.Length)
            {
                Assert.That(document[documentIndex],
                    Has.Length.EqualTo(document[0].Length));
                documentIndex++;
            }
            index++;
        }

        index = 0;
        while (index < 128)
        {
            Assert.That(ParseCsv(export.CsvFromNoArguments()), Is.Not.Empty);
            index++;
        }
    }

    [Test]
    public void XmlKindsAlwaysProduceWellFormedRequestedShapes()
    {
        ExportProvider export = Fake.Create(12002).Export();

        int index = 0;
        while (index < 64)
        {
            string elementText = export.XmlFromKind(XmlTextKind.Element);
            string documentText = export.XmlFromKind(XmlTextKind.Document);
            XDocument element = XDocument.Parse(elementText);
            XDocument document = XDocument.Parse(documentText);

            Assert.Multiple(() =>
            {
                Assert.That(element.Declaration, Is.Null);
                Assert.That(element.Root, Is.Not.Null);
                Assert.That(document.Declaration, Is.Not.Null);
                Assert.That(document.Declaration.Encoding,
                    Is.EqualTo("utf-8"));
                Assert.That(document.Root, Is.Not.Null);
            });
            index++;
        }

        index = 0;
        while (index < 128)
        {
            Assert.That(() => XDocument.Parse(export.XmlFromNoArguments()), Throws.Nothing);
            index++;
        }
    }

    [Test]
    public void YamlKindsUseValidReviewedScalarSequenceAndMappingShapes()
    {
        ExportProvider export = Fake.Create(12003).Export();

        int index = 0;
        while (index < 64)
        {
            Assert.Multiple(() =>
            {
                Assert.That(export.YamlFromKind(YamlTextKind.Scalar),
                    Is.AnyOf(YamlScalars));
                Assert.That(export.YamlFromKind(YamlTextKind.Sequence),
                    Is.AnyOf(YamlSequences));
                Assert.That(export.YamlFromKind(YamlTextKind.Mapping),
                    Is.AnyOf(YamlMappings));
            });
            index++;
        }
    }

    [Test]
    public void InvalidKindsReturnTheSelectedMalformedFixtures()
    {
        ExportProvider export = Fake.Create(12004).Export();

        Assert.Multiple(() =>
        {
            Assert.That(() => JsonDocument.Parse(export.InvalidJsonFromKind(
                StructuredTextInvalidKind.UnterminatedQuotedValue)),
                Throws.InstanceOf<JsonException>());
            Assert.That(() => JsonDocument.Parse(export.InvalidJsonFromKind(
                StructuredTextInvalidKind.MissingSeparator)),
                Throws.InstanceOf<JsonException>());
            Assert.That(() => JsonDocument.Parse(export.InvalidJsonFromKind(
                StructuredTextInvalidKind.UnexpectedToken)),
                Throws.InstanceOf<JsonException>());
            Assert.That(export.InvalidCsvFromKind(
                StructuredTextInvalidKind.UnterminatedQuotedValue),
                Is.EqualTo("name,note\r\nsample,\"unfinished"));
            Assert.That(ParseCsv(export.InvalidCsvFromKind(
                StructuredTextInvalidKind.MissingSeparator))[1],
                Has.Length.EqualTo(1));
            Assert.That(() => ParseCsv(export.InvalidCsvFromKind(
                StructuredTextInvalidKind.UnexpectedToken)),
                Throws.TypeOf<MalformedLineException>());
            Assert.That(() => XDocument.Parse(export.InvalidXmlFromKind(
                StructuredTextInvalidKind.UnterminatedQuotedValue)),
                Throws.TypeOf<XmlException>());
            Assert.That(() => XDocument.Parse(export.InvalidXmlFromKind(
                StructuredTextInvalidKind.MissingSeparator)),
                Throws.TypeOf<XmlException>());
            Assert.That(() => XDocument.Parse(export.InvalidXmlFromKind(
                StructuredTextInvalidKind.UnexpectedToken)),
                Throws.TypeOf<XmlException>());
            Assert.That(export.InvalidYamlFromKind(
                StructuredTextInvalidKind.UnterminatedQuotedValue),
                Is.EqualTo("name: \"sample"));
            Assert.That(export.InvalidYamlFromKind(
                StructuredTextInvalidKind.MissingSeparator),
                Does.Contain("status active"));
            Assert.That(export.InvalidYamlFromKind(
                StructuredTextInvalidKind.UnexpectedToken),
                Is.EqualTo("items: [one, two}\n"));
            Assert.That(export.InvalidSqlInsertFromKind(
                StructuredTextInvalidKind.UnterminatedQuotedValue),
                Does.EndWith("('sample);"));
            Assert.That(export.InvalidSqlInsertFromKind(
                StructuredTextInvalidKind.MissingSeparator),
                Does.Contain("'sample' 'ready'"));
            Assert.That(export.InvalidSqlInsertFromKind(
                StructuredTextInvalidKind.UnexpectedToken),
                Does.EndWith("('sample'];"));
        });
    }

    [Test]
    public void RandomValidAndInvalidTextIsDeterministicAndSeparated()
    {
        const long seed = 12005;
        ExportProvider first = Fake.Create(seed).Export();
        ExportProvider second = Fake.Create(seed).Export();

        int index = 0;
        while (index < 128)
        {
            string json = first.JsonFromNoArguments();
            string invalidJson = first.InvalidJson();
            string csv = first.CsvFromNoArguments();
            string invalidCsv = first.InvalidCsv();
            string xml = first.XmlFromNoArguments();
            string invalidXml = first.InvalidXml();
            string yaml = first.Yaml();
            string invalidYaml = first.InvalidYaml();
            string sql = first.SqlInsert();
            string invalidSql = first.InvalidSqlInsert();

            Assert.Multiple(() =>
            {
                Assert.That(json, Is.EqualTo(second.JsonFromNoArguments()));
                Assert.That(invalidJson, Is.EqualTo(second.InvalidJson()));
                Assert.That(csv, Is.EqualTo(second.CsvFromNoArguments()));
                Assert.That(invalidCsv, Is.EqualTo(second.InvalidCsv()));
                Assert.That(xml, Is.EqualTo(second.XmlFromNoArguments()));
                Assert.That(invalidXml, Is.EqualTo(second.InvalidXml()));
                Assert.That(yaml, Is.EqualTo(second.Yaml()));
                Assert.That(invalidYaml, Is.EqualTo(second.InvalidYaml()));
                Assert.That(sql, Is.EqualTo(second.SqlInsert()));
                Assert.That(invalidSql,
                    Is.EqualTo(second.InvalidSqlInsert()));
                Assert.That(() => JsonDocument.Parse(json).Dispose(),
                    Throws.Nothing);
                Assert.That(() => JsonDocument.Parse(invalidJson),
                    Throws.InstanceOf<JsonException>());
                Assert.That(() => XDocument.Parse(xml), Throws.Nothing);
                Assert.That(() => XDocument.Parse(invalidXml),
                    Throws.TypeOf<XmlException>());
                Assert.That(ParseCsv(csv), Is.Not.Empty);
                Assert.That(sql, Does.Match(
                    "^INSERT INTO \"[a-z_]+\" \\(\"[a-z_]+\"\\) VALUES \\('.*'\\);$"));
                Assert.That(sql, Does.Not.Contain(invalidSql));
            });
            index++;
        }
    }

    [Test]
    public void LimitsAndUnsupportedKindsFailBeforeEntropy()
    {
        const long seed = 12006;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        ExportProvider tinyInspected = Fake.Create(tinyOptions).Export();
        ExportProvider tinyUntouched = Fake.Create(tinyOptions).Export();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 1;
        ExportProvider countInspected = Fake.Create(countOptions).Export();
        ExportProvider countUntouched = Fake.Create(countOptions).Export();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.JsonFromNoArguments(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.InvalidJson(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.CsvFromNoArguments(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.InvalidCsv(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.XmlFromNoArguments(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.InvalidXml(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Yaml(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.InvalidYaml(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.SqlInsert(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.InvalidSqlInsert(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.JsonFromNoArguments(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.CsvFromNoArguments(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.XmlFromNoArguments(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Yaml(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.SqlInsert(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.JsonFromKind((JsonTextKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.CsvFromKind((CsvTextKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.XmlFromKind((XmlTextKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.YamlFromKind((YamlTextKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.InvalidJsonFromKind(
                (StructuredTextInvalidKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.InvalidSqlInsertFromKind(
                (StructuredTextInvalidKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void StructuredTextGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12007);
        Fake untouched = Fake.Create(12007);

        int index = 0;
        while (index < 32)
        {
            inspected.Export().JsonFromNoArguments();
            inspected.Export().InvalidJson();
            inspected.Export().CsvFromNoArguments();
            inspected.Export().InvalidCsv();
            inspected.Export().XmlFromNoArguments();
            inspected.Export().InvalidXml();
            inspected.Export().Yaml();
            inspected.Export().InvalidYaml();
            inspected.Export().SqlInsert();
            inspected.Export().InvalidSqlInsert();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static string[][] ParseCsv(string value)
    {
        List<string[]> records = new List<string[]>();
        using StringReader reader = new StringReader(value);
        using TextFieldParser parser = new TextFieldParser(reader);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        while (!parser.EndOfData)
        {
            records.Add(parser.ReadFields());
        }
        return records.ToArray();
    }
}
