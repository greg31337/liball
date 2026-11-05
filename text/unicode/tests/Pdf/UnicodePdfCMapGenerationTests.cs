using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfCMapGenerationTests
{
    [Test]
    public void CreateToUnicodeMap_EmitsDeterministicCMapSource()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F 1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x0101, new int[] { 0x1F600 }),
                new UnicodePdfGlyphMapping(0x0021, new int[] { 0x0042 }),
                new UnicodePdfGlyphMapping(0x0025, new int[] { 0x0066, 0x0069 }),
                new UnicodePdfGlyphMapping(0x0100, new int[] { 0x20AC }),
                new UnicodePdfGlyphMapping(0x0020, new int[] { 0x0041 }),
            });

        string cmap = new UnicodePdfCMap().CreateToUnicodeMap(glyphMap);

        Assert.That(cmap, Does.StartWith("/CIDInit /ProcSet findresource begin\n"));
        Assert.That(cmap, Does.Contain("/CMapName /F#201-ToUnicode def\n"));
        Assert.That(cmap, Does.Contain("1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n"));
        Assert.That(cmap, Does.Contain("1 beginbfrange\n<0020> <0021> <0041>\nendbfrange\n"));
        Assert.That(cmap, Does.Contain("3 beginbfchar\n<0025> <00660069>\n<0100> <20AC>\n<0101> <D83DDE00>\nendbfchar\n"));
        Assert.That(cmap, Does.EndWith("endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n"));
    }

    [Test]
    public void CreateToUnicodeMap_BatchesBfCharEntriesAtPdfLimit()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[101];
        int index = 0;
        while (index < mappings.Length)
        {
            mappings[index] = new UnicodePdfGlyphMapping(index * 2, new int[] { 0xE000 + index });
            index = index + 1;
        }

        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap("F1", mappings);

        string cmap = new UnicodePdfCMap().CreateToUnicodeMap(glyphMap);

        Assert.That(cmap, Does.Contain("100 beginbfchar\n"));
        Assert.That(cmap, Does.Contain("1 beginbfchar\n<C8> <E064>\nendbfchar\n"));
    }

    [Test]
    public void SequentialRangeHelpers_CoalesceOnlyBmpSingleScalarRuns()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[]
        {
            new UnicodePdfGlyphMapping(0x20, new int[] { 0x0041 }),
            new UnicodePdfGlyphMapping(0x21, new int[] { 0x0042 }),
            new UnicodePdfGlyphMapping(0x22, new int[] { 0x0043 }),
            new UnicodePdfGlyphMapping(0x30, new int[] { 0x1F600 }),
            new UnicodePdfGlyphMapping(0x31, new int[] { 0x1F601 }),
            new UnicodePdfGlyphMapping(0x40, new int[] { 0x0066, 0x0069 }),
        };

        UnicodePdfGlyphRange[] ranges = UnicodePdfCMap.CreateSequentialRanges(mappings);
        UnicodePdfGlyphMapping[] bfCharMappings = UnicodePdfCMap.CreateBfCharMappings(mappings);

        Assert.That(ranges, Has.Length.EqualTo(1));
        Assert.That(ranges[0].StartCharacterCode, Is.EqualTo(0x20));
        Assert.That(ranges[0].EndCharacterCode, Is.EqualTo(0x22));
        Assert.That(ranges[0].StartUnicodeCodePoint, Is.EqualTo(0x0041));
        Assert.That(bfCharMappings, Has.Length.EqualTo(3));
        Assert.That(bfCharMappings[0].CharacterCode, Is.EqualTo(0x30));
        Assert.That(bfCharMappings[1].CharacterCode, Is.EqualTo(0x31));
        Assert.That(bfCharMappings[2].CharacterCode, Is.EqualTo(0x40));
    }

    [Test]
    public void CMapGenerationHelpers_FormatHexSourcesAndUnicodeDestinations()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[]
        {
            new UnicodePdfGlyphMapping(0x0100, new int[] { 0x0041 }),
        };
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        Assert.That(UnicodePdfCMap.GetCharacterCodeHexWidth(mappings), Is.EqualTo(4));
        Assert.That(UnicodePdfCMap.FormatCharacterCode(0x0A, 2), Is.EqualTo("<0A>"));
        Assert.That(UnicodePdfCMap.FormatCharacterCode(0x0100, 4), Is.EqualTo("<0100>"));
        Assert.That(UnicodePdfCMap.FormatUnicodeString(new int[] { 0x0066, 0x0069 }), Is.EqualTo("<00660069>"));
        Assert.That(UnicodePdfCMap.FormatUnicodeString(new int[] { 0x1F600 }), Is.EqualTo("<D83DDE00>"));

        UnicodePdfCMap.AppendPdfNameText(builder, "A/B#C D");
        Assert.That(builder.ToString(), Is.EqualTo("A#2FB#23C#20D"));
    }

    [Test]
    public void CMapGenerationHelpers_RejectInvalidArguments()
    {
        UnicodePdfGlyphMap duplicateMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(0x01, new int[] { 0x0042 }),
            });

        Assert.Throws<ArgumentNullException>(() => new UnicodePdfCMap().CreateToUnicodeMap(null!));
        Assert.Throws<ArgumentException>(() => new UnicodePdfCMap().CreateToUnicodeMap(duplicateMap));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendCMapHeader(null!, "F1", 2));
        Assert.Throws<ArgumentException>(() => UnicodePdfCMap.AppendCMapHeader(new System.Text.StringBuilder(), "", 2));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendCMapFooter(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendCodeSpaceRange(null!, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.AppendCodeSpaceRange(new System.Text.StringBuilder(), 3));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendBfCharSection(null!, new UnicodePdfGlyphMapping[] { }, 2));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendBfRangeSection(null!, new UnicodePdfGlyphRange[] { }, 2));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.SortMappingsByCharacterCode(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.CreateSequentialRanges(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.CreateBfCharMappings(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.GetCharacterCodeHexWidth(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.FormatCharacterCode(-1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.FormatCharacterCode(0x0100, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.FormatCharacterCode(0, int.MaxValue));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.FormatUnicodeString(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendPdfNameText(null!, "F1"));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendPdfNameText(new System.Text.StringBuilder(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.GetCMapBatchEntryCount(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.GetCMapBatchEntryCount(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.GetCMapBatchEntryCount(1, 1));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendHexValue(null!, 0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.AppendHexValue(new System.Text.StringBuilder(), -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.AppendHexValue(new System.Text.StringBuilder(), 0x0100, 2));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AppendMaxHexValue(null!, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.ValidateCharacterCodeHexWidth(10));

        UnicodePdfGlyphMapping maximumCharacterCode = new UnicodePdfGlyphMapping(int.MaxValue, new int[] { 0x0041 });
        UnicodePdfGlyphMapping lowerCharacterCode = new UnicodePdfGlyphMapping(0, new int[] { 0x0042 });
        Assert.That(UnicodePdfCMap.CanExtendSequentialBmpRange(maximumCharacterCode, lowerCharacterCode), Is.False);
    }
}
