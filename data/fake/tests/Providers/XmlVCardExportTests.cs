using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using fake;

namespace fake.Tests;

[TestFixture]
public class XmlVCardExportTests
{
    public enum SampleKind
    {
        First = 0,
        Second = 1
    }

    public class XmlNested
    {
        public string Text;
        public bool Ready;
    }

    public class XmlRecord
    {
        public int Zeta;
        public XmlNested Alpha;
        public string Empty;
        public object Missing;
        public object[] Items;

        public string IgnoredProperty { get; set; }
    }

    public class XmlCycle
    {
        public object Next;
    }

    [Test]
    public void XmlPublicFieldsAreDeterministicEscapedAndParseable()
    {
        XmlNested nested = new XmlNested();
        nested.Text = "&<>\"'\r\n東京😀";
        nested.Ready = true;
        XmlRecord value = new XmlRecord();
        value.Zeta = 9;
        value.Alpha = nested;
        value.Empty = "";
        value.Missing = null;
        value.Items = new object[] { "first", 2, false };
        value.IgnoredProperty = "not exported";
        ExportProvider export = Fake.Create(14300).Export();

        string first = export.Xml(value);
        string second = export.Xml(value);
        XDocument document = XDocument.Parse(first);
        XElement root = document.Root;

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.StartWith(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
            Assert.That(root.Name.LocalName, Is.EqualTo("XmlRecord"));
            Assert.That(root.Elements().Select(item => item.Name.LocalName),
                Is.EqualTo(new string[]
                {
                    "Alpha", "Empty", "Items", "Missing", "Zeta"
                }));
            Assert.That(root.Element("Alpha").Element("Text").Value,
                Is.EqualTo(nested.Text));
            Assert.That(root.Element("Alpha").Element("Ready").Value,
                Is.EqualTo("true"));
            Assert.That(root.Element("Items").Elements("Item")
                    .Select(item => item.Value),
                Is.EqualTo(new string[] { "first", "2", "false" }));
            Assert.That(root.Element("Empty").IsEmpty, Is.False);
            Assert.That(root.Element("Empty").Value, Is.Empty);
            Assert.That(root.Element("Missing").Value, Is.Empty);
            Assert.That(root.Element("IgnoredProperty"), Is.Null);
        });
    }

    [Test]
    public void XmlSchemaAndDictionaryOrderArePreservedAndNamesAreEncoded()
    {
        Fake fake = Fake.Create(14301);
        SchemaGenerator schema = fake.Schema();
        SchemaNode rootNode = schema.Object(
            new SchemaField[]
            {
                schema.Field("zeta", schema.Constant(1)),
                schema.Field("alpha", schema.Constant(2))
            });
        object generated = schema.GenerateFromRoot(rootNode);
        OrderedDictionary dictionary = new OrderedDictionary();
        dictionary["zeta"] = generated;
        dictionary["display name"] = "Ada & Lin";
        dictionary[""] = "empty key";

        string xml = fake.Export().XmlFromRootNameAndValue("Dataset", dictionary);
        XDocument document = XDocument.Parse(xml);
        XElement root = document.Root;

        Assert.Multiple(() =>
        {
            Assert.That(root.Name.LocalName, Is.EqualTo("Dataset"));
            Assert.That(root.Elements().Select(item => item.Name.LocalName),
                Is.EqualTo(new string[]
                {
                    "zeta", "display_x0020_name", "_x0000_"
                }));
            Assert.That(root.Element("zeta").Elements()
                    .Select(item => item.Name.LocalName),
                Is.EqualTo(new string[] { "zeta", "alpha" }));
            Assert.That(root.Element("display_x0020_name").Value,
                Is.EqualTo("Ada & Lin"));
            Assert.That(root.Element("_x0000_").Value,
                Is.EqualTo("empty key"));
        });
    }

    [Test]
    public void XmlMachineValuesAreCultureInvariant()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        string xml;
        try
        {
            xml = Fake.Create(14302).Export().Xml(new object[]
            {
                1234.5m,
                0.125d,
                new DateTime(
                    2024,
                    1,
                    2,
                    3,
                    4,
                    5,
                    DateTimeKind.Utc),
                new DateTimeOffset(
                    2024,
                    1,
                    2,
                    3,
                    4,
                    5,
                    TimeSpan.FromHours(5.5)),
                new DateOnly(2024, 1, 2),
                new TimeOnly(6, 7, 8),
                new TimeSpan(1, 2, 3, 4),
                new Guid("00112233-4455-6677-8899-aabbccddeeff"),
                SampleKind.Second
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        string[] values = XDocument.Parse(xml).Root.Elements("Item")
            .Select(item => item.Value).ToArray();

        Assert.That(values, Is.EqualTo(new string[]
        {
            "1234.5",
            "0.125",
            "2024-01-02T03:04:05.0000000Z",
            "2024-01-02T03:04:05.0000000+05:30",
            "2024-01-02",
            "06:07:08.0000000",
            "1.02:03:04",
            "00112233-4455-6677-8899-aabbccddeeff",
            "Second"
        }));
    }

    [Test]
    public void InvalidXmlGraphsNamesAndValuesAreRejected()
    {
        ExportProvider export = Fake.Create(14303).Export();
        XmlCycle cycle = new XmlCycle();
        cycle.Next = cycle;
        Array multidimensional = Array.CreateInstance(
            typeof(string),
            new int[] { 1, 1 });
        Hashtable invalidDictionary = new Hashtable();
        invalidDictionary[1] = "value";
        object nested = "end";
        int depth = 0;
        while (depth < 130)
        {
            nested = new object[] { nested };
            depth++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(() => export.Xml(cycle),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("cycle"));
            Assert.That(() => export.Xml(multidimensional),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("one-dimensional"));
            Assert.That(() => export.Xml(invalidDictionary),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("keys must be strings"));
            Assert.That(() => export.Xml(nested),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("maximum depth"));
            Assert.That(() => export.Xml(double.NaN),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => export.XmlFromRootNameAndValue("bad name", "value"),
                Throws.TypeOf<XmlException>());
            Assert.That(() => export.XmlFromRootNameAndValue((string)null, "value"),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => export.Xml("\ud800"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void PersonVCardEscapesFieldsAndUsesRfcLineEndings()
    {
        PersonProfile profile = new PersonProfile();
        profile.FirstName = "Élo;die";
        profile.LastName = "山田\\家";
        profile.FullName = "Élodie, 山田";
        profile.BirthDate = new DateOnly(2000, 2, 3);
        profile.Username = "elodie.yamada";
        profile.Email = "elodie@example.com";
        profile.Phone = "+1-415-555-0100";
        profile.Address = "12 Rue; Exemple\r\nParis, France";
        profile.Employer = "Example, SARL";
        profile.Locale = "fr-fr";
        profile.TimeZoneId = "Europe/Paris";

        string card = Fake.Create(14304).Export().VCard(profile);
        string unfolded = Unfold(card);

        Assert.Multiple(() =>
        {
            Assert.That(card, Does.EndWith("END:VCARD\r\n"));
            Assert.That(card.Replace("\r\n", ""),
                Does.Not.Contain("\r").And.Not.Contain("\n"));
            Assert.That(unfolded, Does.Contain("VERSION:4.0\r\n"));
            Assert.That(unfolded,
                Does.Contain("FN:Élodie\\, 山田\r\n"));
            Assert.That(unfolded,
                Does.Contain("N:山田\\\\家;Élo\\;die;;;\r\n"));
            Assert.That(unfolded,
                Does.Contain("BDAY:20000203\r\n"));
            Assert.That(unfolded,
                Does.Contain("ADR;TYPE=home:;;12 Rue\\; Exemple\\nParis\\, France;;;;\r\n"));
            Assert.That(unfolded,
                Does.Contain("ORG:Example\\, SARL\r\n"));
            Assert.That(unfolded, Does.Contain("LANG:fr-FR\r\n"));
        });
    }

    [Test]
    public void GeneratedProfilesMapLocalizedNamesAndAddresses()
    {
        Fake personFake = Fake.Create("fr-FR", 14305);
        PersonProfile person = personFake.Person().Record();
        string personCard = Unfold(personFake.Export().VCard(person));
        Fake companyFake = Fake.Create("ja-JP", 14306);
        CompanyProfile company = companyFake.Company().Profile();
        string companyCard = Unfold(companyFake.Export().VCardFromProfile(company));

        Assert.Multiple(() =>
        {
            Assert.That(personCard,
                Does.Contain("FN:" + EscapeVCard(person.FullName) + "\r\n"));
            Assert.That(personCard,
                Does.Contain("ADR;TYPE=home:;;" +
                    EscapeVCard(person.Address) + ";;;;\r\n"));
            Assert.That(personCard,
                Does.Contain("LANG:" + person.Locale + "\r\n"));
            Assert.That(companyCard, Does.Contain("KIND:org\r\n"));
            Assert.That(companyCard,
                Does.Contain("FN:" + EscapeVCard(company.Name) + "\r\n"));
            Assert.That(companyCard,
                Does.Contain("ORG:" + EscapeVCard(company.Name) + "\r\n"));
            Assert.That(companyCard,
                Does.Contain("ADR;TYPE=work:" +
                    CompanyAddress(company.Address) + "\r\n"));
            Assert.That(companyCard,
                Does.Contain("LANG:" + company.Locale + "\r\n"));
        });
    }

    [Test]
    public void VCardFoldsAt75Utf8OctetsWithoutLosingUnicode()
    {
        PersonProfile profile = new PersonProfile();
        profile.FirstName = "名";
        profile.LastName = "姓";
        profile.FullName = String.Concat(
            Enumerable.Repeat("東京😀", 30));
        profile.Locale = "ja-JP";

        string card = Fake.Create(14307).Export().VCard(profile);
        string[] physicalLines = card.Split(
            new string[] { "\r\n" },
            StringSplitOptions.None);
        string unfolded = Unfold(card);

        int index = 0;
        while (index < physicalLines.Length - 1)
        {
            Assert.That(
                Encoding.UTF8.GetByteCount(physicalLines[index]),
                Is.LessThanOrEqualTo(75));
            index++;
        }
        Assert.Multiple(() =>
        {
            Assert.That(physicalLines.Any(line => line.StartsWith(" ")),
                Is.True);
            Assert.That(unfolded,
                Does.Contain("FN:" + EscapeVCard(profile.FullName) + "\r\n"));
        });
    }

    [Test]
    public void ExportLimitsAndNullProfilesFailWithoutConsumingEntropy()
    {
        FakeOptions countOptions = FakeOptions.Create(14308);
        countOptions.MaximumCount = 1;
        Fake countFake = Fake.Create(countOptions);
        Fake countUntouched = Fake.Create(countOptions);
        FakeOptions outputOptions = FakeOptions.Create(14309);
        outputOptions.MaximumOutputSize = 20;
        Fake outputFake = Fake.Create(outputOptions);
        Fake outputUntouched = Fake.Create(outputOptions);
        PersonProfile profile = new PersonProfile();
        profile.FirstName = "Ada";
        profile.LastName = "Lovelace";
        profile.FullName = "Ada Lovelace";

        Assert.Multiple(() =>
        {
            Assert.That(() => countFake.Export().Xml(
                    new object[] { 1, 2 }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputFake.Export().Xml("value"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputFake.Export().VCard(profile),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(14310).Export().VCard(
                    (PersonProfile)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => Fake.Create(14310).Export().VCardFromProfile(
                    (CompanyProfile)null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(countFake.Export().RandomValue.Long(),
                Is.EqualTo(countUntouched.Export().RandomValue.Long()));
            Assert.That(outputFake.Export().RandomValue.Long(),
                Is.EqualTo(outputUntouched.Export().RandomValue.Long()));
        });
    }

    static string Unfold(string value)
    {
        return value.Replace("\r\n ", "");
    }

    static string CompanyAddress(AddressRecord address)
    {
        return ";" + EscapeVCard(address.Unit) + ";" +
            EscapeVCard(address.StreetAddress) + ";" +
            EscapeVCard(address.City) + ";" +
            EscapeVCard(address.Subdivision) + ";" +
            EscapeVCard(address.PostalCode) + ";" +
            EscapeVCard(address.Country);
    }

    static string EscapeVCard(string value)
    {
        if (value == null)
            return "";
        return value.Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\r", "\\n")
            .Replace("\n", "\\n");
    }
}
