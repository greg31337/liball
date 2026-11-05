using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeCaseParserTests
{
    [Test]
    public void ParseSpecialCasingLine_ReadsUnconditionalMappings()
    {
        UnicodeSpecialCasingRecord record = UnicodeDataParser.ParseSpecialCasingLine("00DF; 00DF; 0053 0073; 0053 0053; # LATIN SMALL LETTER SHARP S");

        Assert.That(record.CodePoint, Is.EqualTo(0x00DF));
        Assert.That(record.LowercaseMapping, Is.EqualTo(new int[] { 0x00DF }));
        Assert.That(record.TitlecaseMapping, Is.EqualTo(new int[] { 0x0053, 0x0073 }));
        Assert.That(record.UppercaseMapping, Is.EqualTo(new int[] { 0x0053, 0x0053 }));
        Assert.That(record.Conditions, Is.Empty);
        Assert.That(record.Comment, Is.EqualTo("LATIN SMALL LETTER SHARP S"));
    }

    [Test]
    public void ParseSpecialCasingLine_ReadsConditionalMappings()
    {
        UnicodeSpecialCasingRecord record = UnicodeDataParser.ParseSpecialCasingLine("0049; 0069 0307; 0049; 0049; lt More_Above; # LATIN CAPITAL LETTER I");

        Assert.That(record.CodePoint, Is.EqualTo(0x0049));
        Assert.That(record.LowercaseMapping, Is.EqualTo(new int[] { 0x0069, 0x0307 }));
        Assert.That(record.Conditions, Is.EqualTo(new string[] { "lt", "More_Above" }));
    }

    [Test]
    public void ParseCaseFoldingLine_ReadsStatusAndMapping()
    {
        UnicodeCaseFoldingRecord common = UnicodeDataParser.ParseCaseFoldingLine("0041; C; 0061; # LATIN CAPITAL LETTER A");
        UnicodeCaseFoldingRecord full = UnicodeDataParser.ParseCaseFoldingLine("00DF; F; 0073 0073; # LATIN SMALL LETTER SHARP S");
        UnicodeCaseFoldingRecord turkic = UnicodeDataParser.ParseCaseFoldingLine("0049; T; 0131; # LATIN CAPITAL LETTER I");

        Assert.That(common.CodePoint, Is.EqualTo(0x0041));
        Assert.That(common.Status, Is.EqualTo("C"));
        Assert.That(common.Mapping, Is.EqualTo(new int[] { 0x0061 }));
        Assert.That(common.Comment, Is.EqualTo("LATIN CAPITAL LETTER A"));
        Assert.That(full.Status, Is.EqualTo("F"));
        Assert.That(full.Mapping, Is.EqualTo(new int[] { 0x0073, 0x0073 }));
        Assert.That(turkic.Status, Is.EqualTo("T"));
        Assert.That(turkic.Mapping, Is.EqualTo(new int[] { 0x0131 }));
    }

    [Test]
    public void CaseParsers_StripCommentsAndBlankLines()
    {
        string specialPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "SpecialCasing.sample.txt");
        string foldingPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "CaseFolding.sample.txt");
        File.WriteAllLines(
            specialPath,
            new string[]
            {
                "# comment",
                "",
                "00DF; 00DF; 0053 0073; 0053 0053;",
                "0049; 0069 0307; 0049; 0049; lt More_Above;",
            });
        File.WriteAllLines(
            foldingPath,
            new string[]
            {
                "# comment",
                "",
                "0041; C; 0061;",
                "00DF; F; 0073 0073;",
            });

        Assert.That(UnicodeDataParser.ReadSpecialCasing(specialPath), Has.Length.EqualTo(2));
        Assert.That(UnicodeDataParser.ReadCaseFolding(foldingPath), Has.Length.EqualTo(2));
    }

    [Test]
    public void CaseParsers_RejectMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseSpecialCasingLine("00DF; 00DF; 0053 0073"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseCaseFoldingLine("0041; C"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseCaseFoldingLine("0041; X; 0061"));
    }
}
