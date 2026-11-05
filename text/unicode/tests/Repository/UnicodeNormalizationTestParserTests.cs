using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNormalizationTestParserTests
{
    [Test]
    public void ParseNormalizationTestLine_ReadsFiveNormalizationColumns()
    {
        UnicodeNormalizationTestRecord record = UnicodeDataParser.ParseNormalizationTestLine("00B2; 00B2; 00B2; 0032; 0032; # SUPERSCRIPT TWO");

        Assert.That(record.Source, Is.EqualTo(new int[] { 0x00B2 }));
        Assert.That(record.Nfc, Is.EqualTo(new int[] { 0x00B2 }));
        Assert.That(record.Nfd, Is.EqualTo(new int[] { 0x00B2 }));
        Assert.That(record.Nfkc, Is.EqualTo(new int[] { 0x0032 }));
        Assert.That(record.Nfkd, Is.EqualTo(new int[] { 0x0032 }));
        Assert.That(record.Comment, Is.EqualTo("SUPERSCRIPT TWO"));
    }

    [Test]
    public void ReadNormalizationTest_SkipsCommentsBlankLinesAndPartHeaders()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "NormalizationTest.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "# comment",
                "",
                "@Part0 # header",
                "00B2; 00B2; 00B2; 0032; 0032;",
                "1100 1161; AC00; 1100 1161; AC00; 1100 1161;",
            });

        UnicodeNormalizationTestRecord[] records = UnicodeDataParser.ReadNormalizationTest(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].Source, Is.EqualTo(new int[] { 0x00B2 }));
        Assert.That(records[1].Nfc, Is.EqualTo(new int[] { 0xAC00 }));
    }

    [Test]
    public void ParseCodePointSequence_ReadsWhitespaceSeparatedHexCodePoints()
    {
        Assert.That(UnicodeDataParser.ParseCodePointSequence("0055 0308 0300"), Is.EqualTo(new int[] { 0x0055, 0x0308, 0x0300 }));
        Assert.That(UnicodeDataParser.ParseCodePointSequence(""), Is.Empty);
    }

    [Test]
    public void NormalizationTestParser_RejectsMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseNormalizationTestLine("00B2; 00B2; 00B2; 0032"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseNormalizationTestLine("00XX; 00B2; 00B2; 0032; 0032"));
    }
}
