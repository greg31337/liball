using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.Tests.Generator;

[TestFixture]
public sealed class Iso639Set3ReaderTests
{
    private const string Header = "Id\tPart2b\tPart2t\tPart1\tScope\tLanguage_Type\tRef_Name\tComment\n";

    [Test]
    public void Read_ParsesEveryFieldAndEveryScopeAndLanguageTypeMarker()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "set3.tab",
            Header +
            "deu\tger\tdeu\tde\tI\tL\tGerman\tModern standard\n" +
            "ara\tara\tara\tar\tM\tA\tArabic\t\n" +
            "mis\tmis\tmis\t\tS\tS\tUncoded languages\t\n" +
            "epo\tepo\tepo\teo\tI\tC\tEsperanto\t\n" +
            "got\tgot\tgot\t\tI\tE\tGothic\t\n" +
            "gmh\tgmh\tgmh\t\tI\tH\tGerman, Middle High\t\n");

        var records = Iso639Set3Reader.Read(path);

        Assert.That(records, Has.Length.EqualTo(6));
        Assert.Multiple(() =>
        {
            Assert.That(records[0].Id, Is.EqualTo("deu"));
            Assert.That(records[0].Part2BibliographicCode, Is.EqualTo("ger"));
            Assert.That(records[0].Part2TerminologyCode, Is.EqualTo("deu"));
            Assert.That(records[0].Alpha2Code, Is.EqualTo("de"));
            Assert.That(records[0].Scope, Is.EqualTo(Iso639Set3Scope.Individual));
            Assert.That(records[0].LanguageType, Is.EqualTo(Iso639Set3LanguageType.Living));
            Assert.That(records[0].ReferenceName, Is.EqualTo("German"));
            Assert.That(records[0].Comment, Is.EqualTo("Modern standard"));
            Assert.That(records[1].Scope, Is.EqualTo(Iso639Set3Scope.Macrolanguage));
            Assert.That(records[1].LanguageType, Is.EqualTo(Iso639Set3LanguageType.Ancient));
            Assert.That(records[2].Scope, Is.EqualTo(Iso639Set3Scope.Special));
            Assert.That(records[2].LanguageType, Is.EqualTo(Iso639Set3LanguageType.Special));
            Assert.That(records[3].LanguageType, Is.EqualTo(Iso639Set3LanguageType.Constructed));
            Assert.That(records[4].LanguageType, Is.EqualTo(Iso639Set3LanguageType.Extinct));
            Assert.That(records[5].LanguageType, Is.EqualTo(Iso639Set3LanguageType.Historical));
        });
    }

    [TestCase("X", "L", "unknown scope marker")]
    [TestCase("I", "X", "unknown language type marker")]
    public void Read_RejectsUnknownEnumMarker(string scope, string languageType, string expectedMessage)
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "set3.tab",
            Header + $"abc\t\t\t\t{scope}\t{languageType}\tExample\t\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => Iso639Set3Reader.Read(path))!;

        Assert.That(exception.Message, Does.Contain(expectedMessage));
    }

    [Test]
    public void Read_RejectsMalformedTabDelimitedRow()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText("set3.tab", Header + "abc\tI\tL\tExample\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => Iso639Set3Reader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("exactly eight tab-delimited fields"));
    }

    [Test]
    public void Read_RejectsInvalidUtf8()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        byte[] header = System.Text.Encoding.UTF8.GetBytes(Header);
        string path = fixture.WriteBytes("set3.tab", header.Concat(new byte[] { 0xc3, 0x28 }).ToArray());

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => Iso639Set3Reader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("not valid UTF-8"));
    }
}
