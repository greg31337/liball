using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfCMapCompletionTests
{
    [Test]
    public void GlyphMap_SnapshotsMappingsAndNestedScalarArrays()
    {
        UnicodePdfGlyphMapping sourceMapping = new UnicodePdfGlyphMapping(
            0x20,
            new int[] { 0x0066, 0x0069 });
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[] { sourceMapping });

        sourceMapping.CharacterCode = 0x30;
        sourceMapping.UnicodeCodePoints[0] = 0x0041;
        UnicodePdfGlyphMapping[] firstSnapshot = glyphMap.GetMappings();
        firstSnapshot[0].CharacterCode = 0x40;
        firstSnapshot[0].UnicodeCodePoints[1] = 0x0042;

        UnicodePdfGlyphMapping[] secondSnapshot = glyphMap.GetMappings();

        Assert.That(secondSnapshot, Is.Not.SameAs(firstSnapshot));
        Assert.That(secondSnapshot[0], Is.Not.SameAs(firstSnapshot[0]));
        Assert.That(secondSnapshot[0].CharacterCode, Is.EqualTo(0x20));
        Assert.That(secondSnapshot[0].UnicodeCodePoints, Is.EqualTo(new int[] { 0x0066, 0x0069 }));
    }

    [Test]
    public void Validation_ReturnsActionableInvalidFontNameDiagnostic()
    {
        IUnicodePdfGlyphMap glyphMap = new ExternalGlyphMap(
            "   ",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 }),
            });

        UnicodePdfCMapValidationResult result = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfCMapStatus.InvalidFontName));
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.Diagnostics[0].MappingIndex, Is.EqualTo(-1));
        Assert.That(result.Diagnostics[0].CharacterCode, Is.EqualTo(-1));
        Assert.That(result.Diagnostics[0].Rule, Is.EqualTo(UnicodePdfCMapData.InvalidFontNameRule));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("font name"));
        Assert.Throws<ArgumentException>(() => new UnicodePdfCMap().CreateToUnicodeMap(glyphMap));
    }

    [Test]
    public void Validation_ReportsEachDuplicateAfterItsOriginalOnce()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x20, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(0x20, new int[] { 0x0042 }),
                new UnicodePdfGlyphMapping(0x20, new int[] { 0x0043 }),
            });

        UnicodePdfCMapValidationResult result = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
        Assert.That(result.Diagnostics[0].MappingIndex, Is.EqualTo(1));
        Assert.That(result.Diagnostics[1].MappingIndex, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].CharacterCode, Is.EqualTo(0x20));
        Assert.That(result.Diagnostics[1].CharacterCode, Is.EqualTo(0x20));
        Assert.That(result.Diagnostics[0].Rule, Is.EqualTo(UnicodePdfCMapData.DuplicateCharacterCodeRule));
        Assert.That(result.Diagnostics[1].Rule, Is.EqualTo(UnicodePdfCMapData.DuplicateCharacterCodeRule));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("index 0"));
        Assert.That(result.Diagnostics[1].Message, Does.Contain("index 0"));
    }

    [Test]
    public void CMapGeneration_SelectsOneThroughFourByteCodeSpaces()
    {
        int[] characterCodes = new int[] { 0xFF, 0x0100, 0x010000, 0x01000000 };
        string[] expectedRanges = new string[]
        {
            "<00> <FF>",
            "<0000> <FFFF>",
            "<000000> <FFFFFF>",
            "<00000000> <FFFFFFFF>",
        };
        string[] expectedCodes = new string[]
        {
            "<FF> <0041>",
            "<0100> <0041>",
            "<010000> <0041>",
            "<01000000> <0041>",
        };

        int index = 0;
        while (index < characterCodes.Length)
        {
            UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
                "F" + index.ToString(),
                new UnicodePdfGlyphMapping[]
                {
                    new UnicodePdfGlyphMapping(characterCodes[index], new int[] { 0x0041 }),
                });

            string cmap = new UnicodePdfCMap().CreateToUnicodeMap(glyphMap);

            Assert.That(cmap, Does.Contain(expectedRanges[index]), "codespace " + index.ToString());
            Assert.That(cmap, Does.Contain(expectedCodes[index]), "mapping " + index.ToString());
            index = index + 1;
        }
    }

    [Test]
    public void CMapGeneration_IsDeterministicAndEncodesScalarSequences()
    {
        UnicodePdfGlyphMapping first = new UnicodePdfGlyphMapping(0x20, new int[] { 0x0041 });
        UnicodePdfGlyphMapping second = new UnicodePdfGlyphMapping(0x21, new int[] { 0x0042 });
        UnicodePdfGlyphMapping third = new UnicodePdfGlyphMapping(0x22, new int[] { 0x0043 });
        UnicodePdfGlyphMapping supplementary = new UnicodePdfGlyphMapping(0x30, new int[] { 0x1F600 });
        UnicodePdfGlyphMapping sequence = new UnicodePdfGlyphMapping(0x31, new int[] { 0x0066, 0x0069, 0x1F600 });
        UnicodePdfGlyphMap forward = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[] { first, second, third, supplementary, sequence });
        UnicodePdfGlyphMap reverse = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[] { sequence, supplementary, third, second, first });

        string forwardCMap = new UnicodePdfCMap().CreateToUnicodeMap(forward);
        string reverseCMap = new UnicodePdfCMap().CreateToUnicodeMap(reverse);

        Assert.That(reverseCMap, Is.EqualTo(forwardCMap));
        Assert.That(forwardCMap, Does.Contain("1 beginbfrange\n<20> <22> <0041>\nendbfrange\n"));
        Assert.That(forwardCMap, Does.Contain("<30> <D83DDE00>"));
        Assert.That(forwardCMap, Does.Contain("<31> <00660069D83DDE00>"));
    }

    [Test]
    public void CMapGeneration_SplitsCoalescedRangesAtSectionLimit()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[202];
        int index = 0;
        while (index < mappings.Length)
        {
            int pairIndex = index / 2;
            int pairOffset = index % 2;
            mappings[index] = new UnicodePdfGlyphMapping(
                (pairIndex * 3) + pairOffset,
                new int[] { 0xE000 + (pairIndex * 2) + pairOffset });
            index = index + 1;
        }

        string cmap = new UnicodePdfCMap().CreateToUnicodeMap(
            new UnicodePdfGlyphMap("F1", mappings));

        Assert.That(CountOccurrences(cmap, "100 beginbfrange\n"), Is.EqualTo(1));
        Assert.That(CountOccurrences(cmap, "1 beginbfrange\n"), Is.EqualTo(1));
        Assert.That(cmap, Does.Not.Contain("beginbfchar"));
    }

    static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while (index < text.Length)
        {
            int found = text.IndexOf(value, index, StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }

            count = count + 1;
            index = found + value.Length;
        }

        return count;
    }

    class ExternalGlyphMap : IUnicodePdfGlyphMap
    {
        string fontName;
        UnicodePdfGlyphMapping[] mappings;

        public ExternalGlyphMap(string fontName, UnicodePdfGlyphMapping[] mappings)
        {
            this.fontName = fontName;
            this.mappings = mappings;
        }

        public string GetFontName()
        {
            return fontName;
        }

        public UnicodePdfGlyphMapping[] GetMappings()
        {
            return mappings;
        }
    }
}
