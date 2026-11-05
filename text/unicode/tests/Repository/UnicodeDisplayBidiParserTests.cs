using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeDisplayBidiParserTests
{
    [Test]
    public void ReadEastAsianWidth_ParsesWidthRanges()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "EastAsianWidth.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "1100..115F ; W # Lo [96]",
                "231A..231B ; W",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadEastAsianWidth(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].StartCodePoint, Is.EqualTo(0x1100));
        Assert.That(records[0].EndCodePoint, Is.EqualTo(0x115F));
        Assert.That(records[0].PropertyName, Is.EqualTo("W"));
    }

    [Test]
    public void ParseBidiMirroringLine_ReadsSourceAndMirrorCodePoints()
    {
        UnicodeBidiMirrorRecord record = UnicodeDataParser.ParseBidiMirroringLine("0028; 0029 # LEFT PARENTHESIS");

        Assert.That(record.CodePoint, Is.EqualTo(0x0028));
        Assert.That(record.MirrorCodePoint, Is.EqualTo(0x0029));
    }

    [Test]
    public void ParseBidiBracketLine_ReadsPairedBracketAndType()
    {
        UnicodeBidiBracketRecord record = UnicodeDataParser.ParseBidiBracketLine("0028; 0029; o # LEFT PARENTHESIS");

        Assert.That(record.CodePoint, Is.EqualTo(0x0028));
        Assert.That(record.PairedBracketCodePoint, Is.EqualTo(0x0029));
        Assert.That(record.BracketType, Is.EqualTo("o"));
    }

    [Test]
    public void ParseJoiningLine_ReadsArabicShapingJoiningData()
    {
        UnicodeJoiningRecord record = UnicodeDataParser.ParseJoiningLine("0628; BEH; D; BEH # Lo ARABIC LETTER BEH");

        Assert.That(record.CodePoint, Is.EqualTo(0x0628));
        Assert.That(record.Name, Is.EqualTo("BEH"));
        Assert.That(record.JoiningType, Is.EqualTo("D"));
        Assert.That(record.JoiningGroup, Is.EqualTo("BEH"));
    }

    [Test]
    public void ReadLineBreak_ParsesLineBreakRanges()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "LineBreak.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "0020 ; SP # Zs SPACE",
                "0021 ; EX # Po EXCLAMATION MARK",
                "3400..4DBF ; ID",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadLineBreak(path);

        Assert.That(records, Has.Length.EqualTo(3));
        Assert.That(records[0].PropertyName, Is.EqualTo("SP"));
        Assert.That(records[1].PropertyName, Is.EqualTo("EX"));
        Assert.That(records[2].StartCodePoint, Is.EqualTo(0x3400));
        Assert.That(records[2].EndCodePoint, Is.EqualTo(0x4DBF));
    }

    [Test]
    public void BidiAndJoiningReaders_StripCommentsAndBlankLines()
    {
        string mirrorPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "BidiMirroring.sample.txt");
        string bracketPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "BidiBrackets.sample.txt");
        string joiningPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ArabicShaping.sample.txt");
        File.WriteAllLines(mirrorPath, new string[] { "# comment", "", "0028; 0029 # LEFT PARENTHESIS" });
        File.WriteAllLines(bracketPath, new string[] { "# comment", "", "0028; 0029; o # LEFT PARENTHESIS" });
        File.WriteAllLines(joiningPath, new string[] { "# comment", "", "0628; BEH; D; BEH # Lo ARABIC LETTER BEH" });

        Assert.That(UnicodeDataParser.ReadBidiMirroring(mirrorPath), Has.Length.EqualTo(1));
        Assert.That(UnicodeDataParser.ReadBidiBrackets(bracketPath), Has.Length.EqualTo(1));
        Assert.That(UnicodeDataParser.ReadJoiningData(joiningPath), Has.Length.EqualTo(1));
    }

    [Test]
    public void DisplayBidiAndJoiningParsers_RejectMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseBidiMirroringLine("0028"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseBidiBracketLine("0028; 0029"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseJoiningLine("0628; BEH; D"));
    }
}
