using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityParserTests
{
    [Test]
    public void ParseConfusableLine_ReadsSourceTargetTypeAndComment()
    {
        UnicodeConfusableRecord record = UnicodeDataParser.ParseConfusableLine("0041 ; 0061 ; SL # LATIN CAPITAL LETTER A");

        Assert.That(record.Source, Is.EqualTo(new int[] { 0x0041 }));
        Assert.That(record.Target, Is.EqualTo(new int[] { 0x0061 }));
        Assert.That(record.Type, Is.EqualTo("SL"));
        Assert.That(record.Comment, Is.EqualTo("LATIN CAPITAL LETTER A"));
    }

    [Test]
    public void ParseConfusableLine_ReadsCodePointSequences()
    {
        UnicodeConfusableRecord record = UnicodeDataParser.ParseConfusableLine("006D 0300 ; 0072 006E ; ML # sequence mapping");

        Assert.That(record.Source, Is.EqualTo(new int[] { 0x006D, 0x0300 }));
        Assert.That(record.Target, Is.EqualTo(new int[] { 0x0072, 0x006E }));
        Assert.That(record.Type, Is.EqualTo("ML"));
    }

    [Test]
    public void ReadIdentifierStatus_SkipsCommentsAndMissingDirectives()
    {
        string path = WriteSample(
            "IdentifierStatus.sample.txt",
            new string[]
            {
                "# comment",
                "",
                "# @missing: 0000..10FFFF; Restricted",
                "@missing: 0000..10FFFF; Restricted",
                "0041..005A ; Allowed # Latin capitals",
                "0378 ; Restricted",
            });

        UnicodeIdentifierStatusRecord[] records = UnicodeDataParser.ReadIdentifierStatus(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].StartCodePoint, Is.EqualTo(0x0041));
        Assert.That(records[0].EndCodePoint, Is.EqualTo(0x005A));
        Assert.That(records[0].Status, Is.EqualTo("Allowed"));
        Assert.That(records[0].Comment, Is.EqualTo("Latin capitals"));
        Assert.That(records[1].Status, Is.EqualTo("Restricted"));
    }

    [Test]
    public void ReadIdentifierType_ParsesSingleAndMultipleTypeValues()
    {
        string path = WriteSample(
            "IdentifierType.sample.txt",
            new string[]
            {
                "# comment",
                "@missing: 0000..10FFFF; Not_Character",
                "0030..0039 ; Recommended",
                "2460..2468 ; Technical Not_XID Not_NFKC # circled digits",
            });

        UnicodeIdentifierTypeRecord[] records = UnicodeDataParser.ReadIdentifierType(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].StartCodePoint, Is.EqualTo(0x0030));
        Assert.That(records[0].EndCodePoint, Is.EqualTo(0x0039));
        Assert.That(records[0].Types, Is.EqualTo(new string[] { "Recommended" }));
        Assert.That(records[1].Types, Is.EqualTo(new string[] { "Technical", "Not_XID", "Not_NFKC" }));
        Assert.That(records[1].Comment, Is.EqualTo("circled digits"));
    }

    [Test]
    public void ReadConfusables_SkipsCommentsAndDirectives()
    {
        string path = WriteSample(
            "confusables.sample.txt",
            new string[]
            {
                "# confusables",
                "",
                "@missing: 0000..10FFFF; ;",
                "0041 ; 0061 ; SL",
                "0391 ; 0041 ; MA",
            });

        UnicodeConfusableRecord[] records = UnicodeDataParser.ReadConfusables(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].Target, Is.EqualTo(new int[] { 0x0061 }));
        Assert.That(records[1].Source, Is.EqualTo(new int[] { 0x0391 }));
    }

    [Test]
    public void SecurityParsers_RejectMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseConfusableLine("0041 ; 0061"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseIdentifierStatusLine("0041"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseIdentifierTypeLine("0041"));
        Assert.Throws<ArgumentException>(() => new UnicodeConfusableRecord(new int[] { }, new int[] { 0x0061 }, "SL", ""));
        Assert.Throws<ArgumentException>(() => new UnicodeIdentifierTypeRecord(0x0041, 0x0041, new string[] { }, ""));
    }

    static string WriteSample(string fileName, string[] lines)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, fileName);
        File.WriteAllLines(path, lines);
        return path;
    }
}
