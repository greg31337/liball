using System.Text.Json;
using System.Xml.Linq;
using fake;

namespace fake.Tests;

[TestFixture]
public class ExportEscapingCoverageTests
{
    public class EscapingRecord
    {
        public string Payload;
    }

    [Test]
    public void OneAdversarialUnicodePayloadRoundTripsThroughStructuredExports()
    {
        string payload = "&<>\"',;\\\r\n東京😀\u2028";
        EscapingRecord record = new EscapingRecord();
        record.Payload = payload;
        ExportProvider export = Fake.Create(14530).Export();

        string json = export.Json(record);
        string xml = export.Xml(record);
        string csv = export.Csv(record);
        string sql = export.SqlInsertFromTableAndValue("escaped\"records", record);
        using JsonDocument jsonDocument = JsonDocument.Parse(json);
        XDocument xmlDocument = XDocument.Parse(xml);
        string[] csvFields = ParseCsv(csv);

        Assert.Multiple(() =>
        {
            Assert.That(jsonDocument.RootElement.GetProperty("Payload")
                    .GetString(),
                Is.EqualTo(payload));
            Assert.That(xmlDocument.Root.Element("Payload").Value,
                Is.EqualTo(payload));
            Assert.That(csvFields,
                Is.EqualTo(new string[] { "Payload", payload }));
            Assert.That(export.CsvField(
                    "next\u0085line\u2028paragraph\u2029end"),
                Is.EqualTo(
                    "\"next\u0085line\u2028paragraph\u2029end\""));
            Assert.That(sql, Is.EqualTo(
                "INSERT INTO \"escaped\"\"records\" (\"Payload\") " +
                "VALUES ('&<>\"'',;\\\r\n東京😀\u2028');"));
        });
    }

    [Test]
    public void VCardEscapesTheSamePayloadWithoutLeakingLogicalNewlines()
    {
        string payload = "&<>\"',;\\\r\n東京😀\u2028";
        PersonProfile profile = new PersonProfile();
        profile.FirstName = "First";
        profile.LastName = "Last";
        profile.FullName = payload;
        profile.Address = payload;
        profile.Locale = "en-US";

        string card = Fake.Create(14531).Export().VCard(profile);
        string unfolded = card.Replace("\r\n ", "");
        string escaped = "&<>\"'\\,\\;\\\\\\n東京😀\u2028";

        Assert.Multiple(() =>
        {
            Assert.That(unfolded,
                Does.Contain("FN:" + escaped + "\r\n"));
            Assert.That(unfolded,
                Does.Contain("ADR;TYPE=home:;;" + escaped +
                    ";;;;\r\n"));
            Assert.That(card.Replace("\r\n", ""),
                Does.Not.Contain("\r").And.Not.Contain("\n"));
            Assert.That(card, Does.EndWith("END:VCARD\r\n"));
        });
    }

    static string[] ParseCsv(string value)
    {
        int separator = value.IndexOf("\r\n", StringComparison.Ordinal);
        string header = value.Substring(0, separator);
        string row = value.Substring(separator + 2);
        Assert.That(row, Does.StartWith("\"").And.EndWith("\""));
        string field = row.Substring(1, row.Length - 2)
            .Replace("\"\"", "\"");
        return new string[] { header, field };
    }
}
