using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.Tests.Generator;

[TestFixture]
public sealed class IanaLanguageSubtagRegistryReaderTests
{
    [Test]
    public void Read_ParsesEveryRecordTypeFieldRepeatedValueAndFoldedValue()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText("registry.txt", CompleteRegistry());

        var registry = IanaLanguageSubtagRegistryReader.Read(path);

        Assert.That(registry.FileDate, Is.EqualTo(new DateOnly(2025, 8, 25)));
        Assert.That(registry.Records, Has.Length.EqualTo(7));
        IanaRegistryRecord language = registry.Records[0];
        Assert.Multiple(() =>
        {
            Assert.That(language.RecordType, Is.EqualTo(IanaRegistryRecordType.Language));
            Assert.That(language.Subtag, Is.EqualTo("sh"));
            Assert.That(language.Tag, Is.Null);
            Assert.That(language.Descriptions, Is.EqualTo(new[]
            {
                "Serbo-Croatian",
                "Serbo-Croatian macrolanguage name"
            }));
            Assert.That(language.AddedDate, Is.EqualTo(new DateOnly(2005, 10, 16)));
            Assert.That(language.DeprecatedDate.HasValue, Is.True);
            Assert.That(language.DeprecatedDate.GetValueOrDefault(), Is.EqualTo(new DateOnly(2000, 2, 18)));
            Assert.That(language.PreferredValue, Is.EqualTo("sr-Latn"));
            Assert.That(language.SuppressScript, Is.EqualTo("Latn"));
            Assert.That(language.Macrolanguage, Is.EqualTo("hbs"));
            Assert.That(language.Scope, Is.EqualTo("macrolanguage"));
            Assert.That(language.Comments, Is.EqualTo(new[] { "First comment continued", "Second comment" }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(registry.Records[1].RecordType, Is.EqualTo(IanaRegistryRecordType.Extlang));
            Assert.That(registry.Records[1].Prefixes, Is.EqualTo(new[] { "zh", "zh-Latn" }));
            Assert.That(registry.Records[2].RecordType, Is.EqualTo(IanaRegistryRecordType.Script));
            Assert.That(registry.Records[3].RecordType, Is.EqualTo(IanaRegistryRecordType.Region));
            Assert.That(registry.Records[4].RecordType, Is.EqualTo(IanaRegistryRecordType.Variant));
            Assert.That(registry.Records[4].Prefixes, Is.EqualTo(new[] { "de" }));
            Assert.That(registry.Records[5].RecordType, Is.EqualTo(IanaRegistryRecordType.Grandfathered));
            Assert.That(registry.Records[5].Tag, Is.EqualTo("i-klingon"));
            Assert.That(registry.Records[6].RecordType, Is.EqualTo(IanaRegistryRecordType.Redundant));
        });
    }

    [Test]
    public void Read_ParsesLanguageScriptAndRegionRanges()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "ranges.txt",
            "File-Date: 2025-08-25\n%%\n" +
            Record("language", "Subtag: qaa..qtz", "Private use", "2005-10-16") + "%%\n" +
            Record("script", "Subtag: Qaaa..Qabx", "Private use", "2005-10-16") + "%%\n" +
            Record("region", "Subtag: QM..QZ", "Private use", "2005-10-16"));

        var registry = IanaLanguageSubtagRegistryReader.Read(path);

        Assert.That(registry.Records.Select(record => record.Subtag),
            Is.EqualTo(new[] { "qaa..qtz", "Qaaa..Qabx", "QM..QZ" }));
    }

    [Test]
    public void Read_RejectsRepeatedScalarField()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "registry.txt",
            "File-Date: 2025-08-25\n%%\n" +
            "Type: language\nSubtag: en\nDescription: English\nAdded: 2005-10-16\nAdded: 2006-01-01\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => IanaLanguageSubtagRegistryReader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("field 'Added' cannot be repeated"));
    }

    [Test]
    public void Read_RejectsUnknownEnumMarker()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "registry.txt",
            "File-Date: 2025-08-25\n%%\n" +
            "Type: unknown\nSubtag: en\nDescription: English\nAdded: 2005-10-16\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => IanaLanguageSubtagRegistryReader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("unknown registry record type 'unknown'"));
    }

    [TestCase("Malformed field", "expected a registry field")]
    [TestCase("Unknown: field", "unknown registry field")]
    public void Read_RejectsMalformedRow(string malformedLine, string expectedMessage)
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "registry.txt",
            "File-Date: 2025-08-25\n%%\n" + malformedLine + "\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => IanaLanguageSubtagRegistryReader.Read(path))!;

        Assert.That(exception.Message, Does.Contain(expectedMessage));
    }

    [Test]
    public void Read_RejectsInvalidUtf8()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        byte[] prefix = System.Text.Encoding.UTF8.GetBytes("File-Date: 2025-08-25\n%%\n");
        string path = fixture.WriteBytes("registry.txt", prefix.Concat(new byte[] { 0xc3, 0x28 }).ToArray());

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => IanaLanguageSubtagRegistryReader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("not valid UTF-8"));
    }

    private static string CompleteRegistry()
    {
        return "File-Date: 2025-08-25\n%%\n" +
            "Type: language\n" +
            "Subtag: sh\n" +
            "Description: Serbo-Croatian\n" +
            "Description: Serbo-Croatian macrolanguage\n" +
            "  name\n" +
            "Added: 2005-10-16\n" +
            "Deprecated: 2000-02-18\n" +
            "Preferred-Value: sr-Latn\n" +
            "Suppress-Script: Latn\n" +
            "Macrolanguage: hbs\n" +
            "Scope: macrolanguage\n" +
            "Comments: First comment\n" +
            "  continued\n" +
            "Comments: Second comment\n" +
            "%%\n" +
            "Type: extlang\n" +
            "Subtag: cmn\n" +
            "Description: Mandarin Chinese\n" +
            "Added: 2009-07-29\n" +
            "Prefix: zh\n" +
            "Prefix: zh-Latn\n" +
            "%%\n" +
            Record("script", "Subtag: Latn", "Latin", "2005-10-16") +
            "%%\n" +
            Record("region", "Subtag: US", "United States", "2005-10-16") +
            "%%\n" +
            "Type: variant\nSubtag: 1901\nDescription: Traditional German orthography\n" +
            "Added: 2005-10-16\nPrefix: de\n" +
            "%%\n" +
            "Type: grandfathered\nTag: i-klingon\nDescription: Klingon\n" +
            "Added: 1997-09-19\nDeprecated: 2004-02-24\nPreferred-Value: tlh\n" +
            "%%\n" +
            "Type: redundant\nTag: zh-cmn\nDescription: Mandarin Chinese\n" +
            "Added: 2005-07-15\nDeprecated: 2009-07-29\nPreferred-Value: cmn\n";
    }

    private static string Record(string type, string key, string description, string added)
    {
        return "Type: " + type + "\n" + key + "\nDescription: " + description + "\nAdded: " + added + "\n";
    }
}
