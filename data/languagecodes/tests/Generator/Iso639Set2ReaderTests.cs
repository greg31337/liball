using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.Tests.Generator;

[TestFixture]
public sealed class Iso639Set2ReaderTests
{
    [Test]
    public void Read_ParsesEveryFieldAndReservedRangeWithBomAndCrLf()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteBomText(
            "set2.txt",
            "ger|deu|de|German; German language|allemand; langue allemande\r\n" +
            "qaa-qtz|||Reserved for local use|réservée à l'usage local\r\n");

        var data = Iso639Set2Reader.Read(path);

        Assert.That(data.Records, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(data.Records[0].BibliographicCode, Is.EqualTo("ger"));
            Assert.That(data.Records[0].TerminologyCode, Is.EqualTo("deu"));
            Assert.That(data.Records[0].Alpha2Code, Is.EqualTo("de"));
            Assert.That(data.Records[0].EnglishNames, Is.EqualTo(new[] { "German", "German language" }));
            Assert.That(data.Records[0].FrenchNames, Is.EqualTo(new[] { "allemand", "langue allemande" }));
            Assert.That(data.ReservedRanges, Has.Length.EqualTo(1));
            Assert.That(data.ReservedRanges[0].FirstCode, Is.EqualTo("qaa"));
            Assert.That(data.ReservedRanges[0].LastCode, Is.EqualTo("qtz"));
        });
    }

    [Test]
    public void Read_RejectsMissingBom()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText("set2.txt", "eng||en|English|anglais\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => Iso639Set2Reader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("byte-order mark"));
    }

    [Test]
    public void Read_RejectsMalformedDelimiterCount()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteBomText("set2.txt", "eng|en|English|anglais\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => Iso639Set2Reader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("exactly five pipe-delimited fields"));
    }

    [Test]
    public void Read_RejectsInvalidUtf8()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteBytes("set2.txt", new byte[] { 0xef, 0xbb, 0xbf, 0xc3, 0x28 });

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => Iso639Set2Reader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("not valid UTF-8"));
    }
}
