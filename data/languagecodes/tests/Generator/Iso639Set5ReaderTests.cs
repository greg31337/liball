using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.Tests.Generator;

[TestFixture]
public sealed class Iso639Set5ReaderTests
{
    private const string Header = "URI\tcode\tLabel (English)\tLabel (French)\n";
    private const string UriPrefix = "http://id.loc.gov/vocabulary/iso639-5/";

    [Test]
    public void Read_ParsesUriCodeAndMultilingualNames()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "set5.tsv",
            Header + UriPrefix + "aaa\taaa\tFamily; Alternate family\tFamille; Autre famille\n");

        var records = Iso639Set5Reader.Read(path);

        Assert.That(records, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(records[0].Uri, Is.EqualTo(UriPrefix + "aaa"));
            Assert.That(records[0].Code, Is.EqualTo("aaa"));
            Assert.That(records[0].EnglishNames, Is.EqualTo(new[] { "Family", "Alternate family" }));
            Assert.That(records[0].FrenchNames, Is.EqualTo(new[] { "Famille", "Autre famille" }));
        });
    }

    [Test]
    public void Read_RejectsMalformedDelimiterCount()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText("set5.tsv", Header + "aaa\tFamily\tFamille\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => Iso639Set5Reader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("exactly four tab-delimited fields"));
    }

    [Test]
    public void Read_RejectsInvalidUtf8()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        byte[] header = System.Text.Encoding.UTF8.GetBytes(Header);
        string path = fixture.WriteBytes("set5.tsv", header.Concat(new byte[] { 0xc3, 0x28 }).ToArray());

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => Iso639Set5Reader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("not valid UTF-8"));
    }

    [Test]
    public void HierarchyRead_ParsesEveryCellClassificationPathAndNote()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        Iso639Set5Record[] records = ReadHierarchyRecords(fixture);
        string html =
            "<html><table class=\"Dynamic639-5OutputTables\">" +
            Row("aaa", "Root", "Racine", "", "aaa", "") +
            Row("bbb", "Branch", "Branche", "language group<br>groupe de langues", "aaa : bbb",
                "<a href=\"#iso639-5-Note-a\">a</a>") +
            Row("ccc", "Remainder", "Reste", "remainder group<br/>groupe de langues restantes", "aaa:ccc",
                "<a href=\"#iso639-5-Note-b\">b</a><a href=\"#iso639-5-Note-b\">b</a>") +
            "</table></html>";
        string path = fixture.WriteText("hierarchy.html", html);

        var hierarchy = Iso639Set5HierarchyReader.Read(path, records);

        Assert.That(hierarchy.Entries, Has.Length.EqualTo(3));
        Assert.That(hierarchy.Relationships, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(hierarchy.Entries[0].GroupKind, Is.EqualTo(Iso639Set5GroupKind.Family));
            Assert.That(hierarchy.Entries[1].GroupKind, Is.EqualTo(Iso639Set5GroupKind.LanguageGroup));
            Assert.That(hierarchy.Entries[1].HierarchyPath, Is.EqualTo(new[] { "aaa", "bbb" }));
            Assert.That(hierarchy.Entries[1].NoteIds, Is.EqualTo(new[] { "a" }));
            Assert.That(hierarchy.Entries[2].GroupKind, Is.EqualTo(Iso639Set5GroupKind.RemainderGroup));
            Assert.That(hierarchy.Entries[2].NoteIds, Is.EqualTo(new[] { "b" }));
            Assert.That(hierarchy.Relationships[0].ParentCode, Is.EqualTo("aaa"));
            Assert.That(hierarchy.Relationships[0].ChildCode, Is.EqualTo("bbb"));
        });
    }

    [Test]
    public void HierarchyRead_RejectsMalformedRow()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        Iso639Set5Record[] records = ReadSingleRecord(fixture);
        string path = fixture.WriteText(
            "hierarchy.html",
            "<table class=\"Dynamic639-5OutputTables\"><tr><td>aaa</td><td>Root</td></tr></table>");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => Iso639Set5HierarchyReader.Read(path, records))!;

        Assert.That(exception.Message, Does.Contain("expected six cells"));
    }

    [Test]
    public void HierarchyRead_RejectsUnknownGroupClassificationMarker()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        Iso639Set5Record[] records = ReadSingleRecord(fixture);
        string path = fixture.WriteText(
            "hierarchy.html",
            "<table class=\"Dynamic639-5OutputTables\">" +
            Row("aaa", "Root", "Racine", "unknown group<br>groupe inconnu", "aaa", "") +
            "</table>");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => Iso639Set5HierarchyReader.Read(path, records))!;

        Assert.That(exception.Message, Does.Contain("unknown group classification"));
    }

    private static Iso639Set5Record[] ReadHierarchyRecords(GeneratorFixture fixture)
    {
        string path = fixture.WriteText(
            "hierarchy.tsv",
            Header +
            UriPrefix + "aaa\taaa\tRoot\tRacine\n" +
            UriPrefix + "bbb\tbbb\tBranch\tBranche\n" +
            UriPrefix + "ccc\tccc\tRemainder\tReste\n");
        return Iso639Set5Reader.Read(path);
    }

    private static Iso639Set5Record[] ReadSingleRecord(GeneratorFixture fixture)
    {
        string path = fixture.WriteText(
            "single.tsv",
            Header + UriPrefix + "aaa\taaa\tRoot\tRacine\n");
        return Iso639Set5Reader.Read(path);
    }

    private static string Row(
        string code,
        string english,
        string french,
        string classification,
        string path,
        string notes)
    {
        return "<tr><td>" + code + "</td><td>" + english + "</td><td>" + french +
            "</td><td>" + classification + "</td><td>" + path + "</td><td>" + notes + "</td></tr>";
    }
}
