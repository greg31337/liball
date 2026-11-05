using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodePropertyRangeParserTests
{
    [Test]
    public void ParsePropertyRangeLine_ReadsSingleCodePointBinaryProperty()
    {
        UnicodePropertyRangeRecord record = UnicodeDataParser.ParsePropertyRangeLine("00AD ; Default_Ignorable_Code_Point # Cf SOFT HYPHEN");

        Assert.That(record.StartCodePoint, Is.EqualTo(0x00AD));
        Assert.That(record.EndCodePoint, Is.EqualTo(0x00AD));
        Assert.That(record.PropertyName, Is.EqualTo("Default_Ignorable_Code_Point"));
        Assert.That(record.Contains(0x00AD), Is.True);
        Assert.That(record.Contains(0x00AE), Is.False);
    }

    [Test]
    public void ParsePropertyRangeLine_ReadsInclusiveCodePointRanges()
    {
        UnicodePropertyRangeRecord record = UnicodeDataParser.ParsePropertyRangeLine("0009..000D ; White_Space # Cc [5]");

        Assert.That(record.StartCodePoint, Is.EqualTo(0x0009));
        Assert.That(record.EndCodePoint, Is.EqualTo(0x000D));
        Assert.That(record.PropertyName, Is.EqualTo("White_Space"));
        Assert.That(record.Contains(0x0008), Is.False);
        Assert.That(record.Contains(0x0009), Is.True);
        Assert.That(record.Contains(0x000D), Is.True);
        Assert.That(record.Contains(0x000E), Is.False);
    }

    [Test]
    public void ParseCodePointRange_ReturnsStartAndEnd()
    {
        Assert.That(UnicodeDataParser.ParseCodePointRange("0041"), Is.EqualTo(new int[] { 0x0041, 0x0041 }));
        Assert.That(UnicodeDataParser.ParseCodePointRange("0041..005A"), Is.EqualTo(new int[] { 0x0041, 0x005A }));
    }

    [Test]
    public void ReadPropertyRangeFiles_StripCommentsAndBlankLines()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PropList.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "# comment",
                "",
                "0009..000D ; White_Space # Cc [5]",
                "200E..200F ; Bidi_Control",
                "00AD ; Default_Ignorable_Code_Point",
            });

        UnicodePropertyRangeRecord[] propList = UnicodeDataParser.ReadPropList(path);
        UnicodePropertyRangeRecord[] derivedCoreProperties = UnicodeDataParser.ReadDerivedCoreProperties(path);
        UnicodePropertyRangeRecord[] extracted = UnicodeDataParser.ReadExtractedPropertyFile(path);

        Assert.That(propList, Has.Length.EqualTo(3));
        Assert.That(propList[1].PropertyName, Is.EqualTo("Bidi_Control"));
        Assert.That(derivedCoreProperties[2].StartCodePoint, Is.EqualTo(0x00AD));
        Assert.That(extracted[0].EndCodePoint, Is.EqualTo(0x000D));
    }

    [Test]
    public void PropertyRangeParsers_RejectMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParsePropertyRangeLine("0041"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParsePropertyRangeLine("005A..0041 ; Broken"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseCodePointRange("005A..0041"));
        Assert.Throws<ArgumentException>(() => new UnicodePropertyRangeRecord(10, 9, "Broken"));
    }
}
