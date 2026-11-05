using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeScriptBlockAgeParserTests
{
    [Test]
    public void ReadScripts_ParsesScriptRanges()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Scripts.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "# comment",
                "0041..005A ; Latin # L& [26]",
                "0370..03FF ; Greek",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadScripts(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].StartCodePoint, Is.EqualTo(0x0041));
        Assert.That(records[0].EndCodePoint, Is.EqualTo(0x005A));
        Assert.That(records[0].PropertyName, Is.EqualTo("Latin"));
        Assert.That(records[1].PropertyName, Is.EqualTo("Greek"));
    }

    [Test]
    public void ParseScriptExtensionsLine_ParsesMultipleScripts()
    {
        UnicodeScriptExtensionsRecord record = UnicodeDataParser.ParseScriptExtensionsLine("0640 ; Arab Syrc # Lm ARABIC TATWEEL");

        Assert.That(record.StartCodePoint, Is.EqualTo(0x0640));
        Assert.That(record.EndCodePoint, Is.EqualTo(0x0640));
        Assert.That(record.Scripts, Is.EqualTo(new string[] { "Arab", "Syrc" }));
        Assert.That(record.Contains(0x0640), Is.True);
        Assert.That(record.Contains(0x0641), Is.False);
    }

    [Test]
    public void ReadScriptExtensions_StripsCommentsAndBlankLines()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ScriptExtensions.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "# comment",
                "",
                "0640 ; Arab Syrc # trailing comment",
                "30FC ; Hira Kana",
            });

        UnicodeScriptExtensionsRecord[] records = UnicodeDataParser.ReadScriptExtensions(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].Scripts, Is.EqualTo(new string[] { "Arab", "Syrc" }));
        Assert.That(records[1].Scripts, Is.EqualTo(new string[] { "Hira", "Kana" }));
    }

    [Test]
    public void ReadBlocksAndDerivedAge_UseRangeValueParser()
    {
        string blocksPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Blocks.sample.txt");
        string agePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "DerivedAge.sample.txt");
        File.WriteAllLines(
            blocksPath,
            new string[]
            {
                "0000..007F; Basic Latin",
                "0080..00FF; Latin-1 Supplement",
            });
        File.WriteAllLines(
            agePath,
            new string[]
            {
                "0000..001F ; 1.1",
                "20A0..20AB ; 2.0",
            });

        UnicodePropertyRangeRecord[] blocks = UnicodeDataParser.ReadBlocks(blocksPath);
        UnicodePropertyRangeRecord[] ages = UnicodeDataParser.ReadDerivedAge(agePath);

        Assert.That(blocks[0].PropertyName, Is.EqualTo("Basic Latin"));
        Assert.That(blocks[1].StartCodePoint, Is.EqualTo(0x0080));
        Assert.That(ages[0].PropertyName, Is.EqualTo("1.1"));
        Assert.That(ages[1].EndCodePoint, Is.EqualTo(0x20AB));
    }

    [Test]
    public void ReadDisplayPropertyFile_UsesRangeValueParser()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Display.sample.txt");
        File.WriteAllLines(
            path,
            new string[]
            {
                "0041..005A ; Uppercase",
                "0061..007A ; Lowercase",
            });

        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadDisplayPropertyFile(path);

        Assert.That(records, Has.Length.EqualTo(2));
        Assert.That(records[0].PropertyName, Is.EqualTo("Uppercase"));
        Assert.That(records[1].PropertyName, Is.EqualTo("Lowercase"));
    }

    [Test]
    public void ScriptExtensionsParser_RejectsMalformedRecords()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseScriptExtensionsLine("0640"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParseScriptExtensionsLine("0640 ; "));
        Assert.Throws<ArgumentException>(() => new UnicodeScriptExtensionsRecord(10, 9, new string[] { "Latn" }));
        Assert.Throws<ArgumentNullException>(() => new UnicodeScriptExtensionsRecord(10, 10, null!));
    }
}
