using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeBoundaryParserTests
{
    [Test]
    public void ReadGraphemeBreakProperty_ParsesBoundaryRanges()
    {
        string path = WriteSample(
            "GraphemeBreakProperty.sample.txt",
            new string[]
            {
                "000D ; CR # Cc <control-000D>",
                "0300..036F ; Extend # Mn [112]",
                "1F1E6..1F1FF ; Regional_Indicator",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadGraphemeBreakProperty(path);

        Assert.That(records, Has.Length.EqualTo(3));
        Assert.That(records[0].StartCodePoint, Is.EqualTo(0x000D));
        Assert.That(records[0].PropertyName, Is.EqualTo("CR"));
        Assert.That(records[1].EndCodePoint, Is.EqualTo(0x036F));
        Assert.That(records[1].PropertyName, Is.EqualTo("Extend"));
        Assert.That(records[2].PropertyName, Is.EqualTo("Regional_Indicator"));
    }

    [Test]
    public void ReadWordBreakProperty_ParsesBoundaryRanges()
    {
        string path = WriteSample(
            "WordBreakProperty.sample.txt",
            new string[]
            {
                "0030..0039 ; Numeric # Nd [10]",
                "0041..005A ; ALetter",
                "0027 ; Single_Quote",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadWordBreakProperty(path);

        Assert.That(records, Has.Length.EqualTo(3));
        Assert.That(records[0].PropertyName, Is.EqualTo("Numeric"));
        Assert.That(records[1].StartCodePoint, Is.EqualTo(0x0041));
        Assert.That(records[1].EndCodePoint, Is.EqualTo(0x005A));
        Assert.That(records[2].PropertyName, Is.EqualTo("Single_Quote"));
    }

    [Test]
    public void ReadSentenceBreakProperty_ParsesBoundaryRanges()
    {
        string path = WriteSample(
            "SentenceBreakProperty.sample.txt",
            new string[]
            {
                "000D ; CR",
                "000A ; LF",
                "0041..005A ; Upper",
                "0061..007A ; Lower",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadSentenceBreakProperty(path);

        Assert.That(records, Has.Length.EqualTo(4));
        Assert.That(records[2].PropertyName, Is.EqualTo("Upper"));
        Assert.That(records[3].PropertyName, Is.EqualTo("Lower"));
    }

    [Test]
    public void ReadLineBreakAndHangulSyllableType_ParseBoundaryRanges()
    {
        string lineBreakPath = WriteSample(
            "LineBreak.boundary.sample.txt",
            new string[]
            {
                "0020 ; SP",
                "3400..4DBF ; ID",
            });
        string hangulPath = WriteSample(
            "HangulSyllableType.sample.txt",
            new string[]
            {
                "1100..115F ; L",
                "1160..11A7 ; V",
                "11A8..11FF ; T",
                "AC00 ; LV",
                "AC01 ; LVT",
            });

        UnicodePropertyRangeRecord[] lineBreak = UnicodeDataParser.ReadLineBreak(lineBreakPath);
        UnicodePropertyRangeRecord[] hangul = UnicodeDataParser.ReadHangulSyllableType(hangulPath);

        Assert.That(lineBreak[0].PropertyName, Is.EqualTo("SP"));
        Assert.That(lineBreak[1].PropertyName, Is.EqualTo("ID"));
        Assert.That(hangul, Has.Length.EqualTo(5));
        Assert.That(hangul[0].PropertyName, Is.EqualTo("L"));
        Assert.That(hangul[3].StartCodePoint, Is.EqualTo(0xAC00));
        Assert.That(hangul[4].PropertyName, Is.EqualTo("LVT"));
    }

    [Test]
    public void ReadIndicConjunctBreak_ParsesDerivedCorePropertyValues()
    {
        UnicodePropertyRangeRecord linker = UnicodeDataParser.ParseIndicConjunctBreakLine("094D ; InCB; Linker # Mn DEVANAGARI SIGN VIRAMA");
        UnicodePropertyRangeRecord consonant = UnicodeDataParser.ParseIndicConjunctBreakLine("0915..0939 ; Indic_Conjunct_Break; Consonant");
        string path = WriteSample(
            "DerivedCoreProperties.InCB.sample.txt",
            new string[]
            {
                "# comment",
                "",
                "094D ; InCB; Linker",
                "0915..0939 ; InCB; Consonant",
                "0300..036F ; InCB; Extend",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadIndicConjunctBreak(path);

        Assert.That(linker.PropertyName, Is.EqualTo("Linker"));
        Assert.That(consonant.PropertyName, Is.EqualTo("Consonant"));
        Assert.That(records, Has.Length.EqualTo(3));
        Assert.That(records[0].PropertyName, Is.EqualTo("Linker"));
        Assert.That(records[1].StartCodePoint, Is.EqualTo(0x0915));
        Assert.That(records[1].EndCodePoint, Is.EqualTo(0x0939));
        Assert.That(records[2].PropertyName, Is.EqualTo("Extend"));
    }

    [Test]
    public void ReadEmojiData_ParsesBoundaryEmojiProperties()
    {
        string path = WriteSample(
            "emoji-boundary.sample.txt",
            new string[]
            {
                "200D ; Emoji_Component",
                "1F3FB..1F3FF ; Emoji_Modifier",
                "1F600..1F64F ; Extended_Pictographic",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadEmojiData(path);

        Assert.That(records, Has.Length.EqualTo(3));
        Assert.That(records[0].PropertyName, Is.EqualTo("Emoji_Component"));
        Assert.That(records[1].PropertyName, Is.EqualTo("Emoji_Modifier"));
        Assert.That(records[2].PropertyName, Is.EqualTo("Extended_Pictographic"));
    }

    [Test]
    public void BoundaryParsers_RejectMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParsePropertyRangeLine("0041"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseIndicConjunctBreakLine("094D"));
    }

    static string WriteSample(string fileName, string[] lines)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, fileName);
        File.WriteAllLines(path, lines);
        return path;
    }
}
