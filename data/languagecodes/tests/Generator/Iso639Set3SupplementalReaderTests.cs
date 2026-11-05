using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.Tests.Generator;

[TestFixture]
public sealed class Iso639Set3SupplementalReaderTests
{
    [Test]
    public void NameIndexRead_ParsesAllFieldsAndDeduplicatesExactRows()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "names.tab",
            "Id\tPrint_Name\tInverted_Name\r\n" +
            "abc\tExample Language\tLanguage, Example\r\n" +
            "abc\tExample Language\tLanguage, Example\r\n" +
            "abc\tExample Dialect\tDialect, Example\r\n");

        var records = Iso639Set3NameIndexReader.Read(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(records[0].Id, Is.EqualTo("abc"));
            Assert.That(records[0].PrintName, Is.EqualTo("Example Language"));
            Assert.That(records[0].InvertedName, Is.EqualTo("Language, Example"));
        });
    }

    [Test]
    public void NameIndexRead_RejectsMalformedRow()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText("names.tab", "Id\tPrint_Name\tInverted_Name\nabc\tExample\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => Iso639Set3NameIndexReader.Read(path))!;

        Assert.That(exception.Message, Does.Contain("exactly three tab-delimited fields"));
    }

    [Test]
    public void MacrolanguageRead_ParsesActiveAndRetiredStatuses()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        var active = ReadActiveRecords(fixture);
        var retired = ReadRetirements(fixture);
        string path = fixture.WriteText(
            "macros.tab",
            "M_Id\tI_Id\tI_Status\n" +
            "aaa\tbbb\tA\n" +
            "aaa\tccc\tR\n");

        var mappings = Iso639Set3MacrolanguageReader.Read(path, active, retired);

        Assert.That(mappings, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(mappings[0].MacrolanguageId, Is.EqualTo("aaa"));
            Assert.That(mappings[0].IndividualLanguageId, Is.EqualTo("bbb"));
            Assert.That(mappings[0].Status, Is.EqualTo(Iso639Set3MappingStatus.Active));
            Assert.That(mappings[1].Status, Is.EqualTo(Iso639Set3MappingStatus.Retired));
        });
    }

    [TestCase("X", "unknown mapping status marker")]
    [TestCase("A\textra", "exactly three tab-delimited fields")]
    public void MacrolanguageRead_RejectsUnknownMarkerAndMalformedRow(string status, string expectedMessage)
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        var active = ReadActiveRecords(fixture);
        var retired = ReadRetirements(fixture);
        string path = fixture.WriteText("macros.tab", "M_Id\tI_Id\tI_Status\naaa\tbbb\t" + status + "\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => Iso639Set3MacrolanguageReader.Read(path, active, retired))!;

        Assert.That(exception.Message, Does.Contain(expectedMessage));
    }

    [Test]
    public void RetirementRead_ParsesEveryFieldAndReasonAndExtractsSplitCodes()
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "retirements.tab",
            "Id\tRef_Name\tRet_Reason\tChange_To\tRet_Remedy\tEffective\n" +
            "aaa\tChanged\tC\taab\tUse the replacement\t2020-01-02\n" +
            "bbb\tDuplicate\tD\tbbc\tDuplicate of bbc\t2020-02-03\n" +
            "ccc\tNonexistent\tN\t\tSpurious identifier\t2020-03-04\n" +
            "ddd\tSplit\tS\t\tSplit into [dde] and [ddf]\t2020-04-05\n" +
            "eee\tMerged\tM\teef\tMerged into eef\t2020-05-06\n");

        var records = Iso639Set3RetirementReader.Read(path);

        Assert.That(records, Has.Length.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(records[0].Reason, Is.EqualTo(Iso639Set3RetirementReason.Change));
            Assert.That(records[0].ChangeToCode, Is.EqualTo("aab"));
            Assert.That(records[0].Remedy, Is.EqualTo("Use the replacement"));
            Assert.That(records[0].EffectiveDate, Is.EqualTo(new DateOnly(2020, 1, 2)));
            Assert.That(records[0].ReplacementCodes, Is.EqualTo(new[] { "aab" }));
            Assert.That(records[1].Reason, Is.EqualTo(Iso639Set3RetirementReason.Duplicate));
            Assert.That(records[2].Reason, Is.EqualTo(Iso639Set3RetirementReason.Nonexistent));
            Assert.That(records[2].ReplacementCodes, Is.Empty);
            Assert.That(records[3].Reason, Is.EqualTo(Iso639Set3RetirementReason.Split));
            Assert.That(records[3].ReplacementCodes, Is.EqualTo(new[] { "dde", "ddf" }));
            Assert.That(records[4].Reason, Is.EqualTo(Iso639Set3RetirementReason.Merge));
        });
    }

    [TestCase("X", "unknown retirement reason marker")]
    [TestCase("C\textra", "exactly six tab-delimited fields")]
    public void RetirementRead_RejectsUnknownMarkerAndMalformedRow(string reason, string expectedMessage)
    {
        using GeneratorFixture fixture = new GeneratorFixture();
        string path = fixture.WriteText(
            "retirements.tab",
            "Id\tRef_Name\tRet_Reason\tChange_To\tRet_Remedy\tEffective\n" +
            "abc\tExample\t" + reason + "\tdef\tUse def\t2020-01-02\n");

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => Iso639Set3RetirementReader.Read(path))!;

        Assert.That(exception.Message, Does.Contain(expectedMessage));
    }

    private static Iso639Set3Record[] ReadActiveRecords(GeneratorFixture fixture)
    {
        string path = fixture.WriteText(
            "active.tab",
            "Id\tPart2b\tPart2t\tPart1\tScope\tLanguage_Type\tRef_Name\tComment\n" +
            "aaa\t\t\t\tM\tL\tMacro\t\n" +
            "bbb\t\t\t\tI\tL\tMember\t\n");
        return Iso639Set3Reader.Read(path);
    }

    private static Iso639Set3RetirementRecord[] ReadRetirements(GeneratorFixture fixture)
    {
        string path = fixture.WriteText(
            "retired.tab",
            "Id\tRef_Name\tRet_Reason\tChange_To\tRet_Remedy\tEffective\n" +
            "ccc\tFormer member\tN\t\tNo longer used\t2020-01-02\n");
        return Iso639Set3RetirementReader.Read(path);
    }
}
