using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeEmojiParserTests
{
    [Test]
    public void ReadEmojiData_ParsesEmojiPropertyRanges()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "emoji-data.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "1F3FB..1F3FF ; Emoji_Modifier # E1.0 [5]",
                "1F600..1F64F ; Emoji_Presentation",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadEmojiData(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].StartCodePoint, Is.EqualTo(0x1F3FB));
        Assert.That(records[0].EndCodePoint, Is.EqualTo(0x1F3FF));
        Assert.That(records[0].PropertyName, Is.EqualTo("Emoji_Modifier"));
        Assert.That(records[1].PropertyName, Is.EqualTo("Emoji_Presentation"));
    }

    [Test]
    public void ParseEmojiSequenceLine_ParsesRangeSequences()
    {
        UnicodeEmojiSequenceRecord record = UnicodeDataParser.ParseEmojiSequenceLine("231A..231B ; Basic_Emoji # E0.6 [2] watch..hourglass done");

        Assert.That(record.StartCodePoints, Is.EqualTo(new int[] { 0x231A }));
        Assert.That(record.EndCodePoints, Is.EqualTo(new int[] { 0x231B }));
        Assert.That(record.PropertyName, Is.EqualTo("Basic_Emoji"));
        Assert.That(record.Comment, Is.EqualTo("E0.6 [2] watch..hourglass done"));
        Assert.That(record.HasRanges(), Is.True);
    }

    [Test]
    public void ParseEmojiSequenceLine_ParsesMultiCodePointZwjSequences()
    {
        UnicodeEmojiSequenceRecord record = UnicodeDataParser.ParseEmojiSequenceLine("1F468 200D 1F469 200D 1F466 ; RGI_Emoji_ZWJ_Sequence # E2.0 family");

        Assert.That(record.StartCodePoints, Is.EqualTo(new int[] { 0x1F468, 0x200D, 0x1F469, 0x200D, 0x1F466 }));
        Assert.That(record.EndCodePoints, Is.EqualTo(record.StartCodePoints));
        Assert.That(record.PropertyName, Is.EqualTo("RGI_Emoji_ZWJ_Sequence"));
        Assert.That(record.HasRanges(), Is.False);
    }

    [Test]
    public void ParseEmojiSequenceLine_ParsesVariationSequences()
    {
        UnicodeEmojiSequenceRecord record = UnicodeDataParser.ParseEmojiSequenceLine("0023 FE0F ; emoji style # number sign");

        Assert.That(record.StartCodePoints, Is.EqualTo(new int[] { 0x0023, 0xFE0F }));
        Assert.That(record.EndCodePoints, Is.EqualTo(new int[] { 0x0023, 0xFE0F }));
        Assert.That(record.PropertyName, Is.EqualTo("emoji style"));
        Assert.That(record.Comment, Is.EqualTo("number sign"));
    }

    [Test]
    public void ReadEmojiSequenceWrappers_StripCommentsAndBlankLines()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "emoji-sequences.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "# comment",
                "",
                "231A..231B ; Basic_Emoji # E0.6 [2]",
                "1F468 200D 1F466 ; RGI_Emoji_ZWJ_Sequence # E2.0",
            });

        Assert.That(UnicodeDataParser.ReadEmojiSequences(path), Has.Length.EqualTo(2));
        Assert.That(UnicodeDataParser.ReadEmojiZwjSequences(path), Has.Length.EqualTo(2));
        Assert.That(UnicodeDataParser.ReadEmojiVariationSequences(path), Has.Length.EqualTo(2));
    }

    [Test]
    public void EmojiSequenceParser_RejectsMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseEmojiSequenceLine("231A"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseEmojiSequenceLine(" ; Basic_Emoji"));
        Assert.Throws<ArgumentException>(() => new UnicodeEmojiSequenceRecord(new int[] { 1 }, new int[] { 1, 2 }, "Broken", ""));
        Assert.Throws<ArgumentNullException>(() => new UnicodeEmojiSequenceRecord(null!, new int[] { 1 }, "Broken", ""));
    }
}
