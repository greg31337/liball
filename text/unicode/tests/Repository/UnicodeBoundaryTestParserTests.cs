using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeBoundaryTestParserTests
{
    [Test]
    public void ParseBoundaryTestLine_ReadsCodePointsBreaksAndComments()
    {
        UnicodeBoundaryTestRecord record = UnicodeDataParser.ParseBoundaryTestLine("÷ 0061 × 0301 ÷ 0062 ÷ # sample");

        Assert.That(record.CodePoints, Is.EqualTo(new int[] { 0x0061, 0x0301, 0x0062 }));
        Assert.That(record.BreakBefore, Is.EqualTo(new bool[] { true, false, true, true }));
        Assert.That(record.Comment, Is.EqualTo("sample"));
    }

    [Test]
    public void ReadBoundaryTestWrappers_StripCommentsDirectivesAndBlankLines()
    {
        string path = WriteSample(
            "BoundaryTest.sample.txt",
            new string[]
            {
                "# comment",
                "",
                "@part 0 # directive",
                "÷ 0061 ÷ # A",
                "÷ 0062 × 0301 ÷ # B acute",
            });

        Assert.That(UnicodeDataParser.ReadGraphemeBreakTest(path), Has.Length.EqualTo(2));
        Assert.That(UnicodeDataParser.ReadWordBreakTest(path), Has.Length.EqualTo(2));
        Assert.That(UnicodeDataParser.ReadSentenceBreakTest(path), Has.Length.EqualTo(2));
        Assert.That(UnicodeDataParser.ReadLineBreakTest(path), Has.Length.EqualTo(2));
    }

    [Test]
    public void BoundaryTestRecord_RejectsBoundaryCountMismatch()
    {
        Assert.Throws<ArgumentException>(() => new UnicodeBoundaryTestRecord(new int[] { 0x0061 }, new bool[] { true }, ""));
    }

    [Test]
    public void BoundaryTestParser_RejectsMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseBoundaryTestLine("0061 ÷"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseBoundaryTestLine("÷ 0061"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseBoundaryTestLine("÷ 0061 0062 ÷"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseBoundaryTestLine("÷ ÷ 0061 ÷"));
    }

    static string WriteSample(string fileName, string[] lines)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, fileName);
        File.WriteAllLines(path, lines);
        return path;
    }
}
