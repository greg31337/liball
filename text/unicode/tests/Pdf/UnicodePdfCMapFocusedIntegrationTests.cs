using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfCMapFocusedIntegrationTests
{
    [Test]
    public void CharacterCodeWidthSelection_CoversEveryBoundaryFromOneThroughFourBytes()
    {
        int[] characterCodes = new int[]
        {
            0x00,
            0xFF,
            0x0100,
            0xFFFF,
            0x010000,
            0xFFFFFF,
            0x01000000,
            int.MaxValue,
        };
        int[] expectedWidths = new int[] { 2, 2, 4, 4, 6, 6, 8, 8 };
        string[] expectedCodeSpaces = new string[]
        {
            "<00> <FF>",
            "<00> <FF>",
            "<0000> <FFFF>",
            "<0000> <FFFF>",
            "<000000> <FFFFFF>",
            "<000000> <FFFFFF>",
            "<00000000> <FFFFFFFF>",
            "<00000000> <FFFFFFFF>",
        };

        int index = 0;
        while (index < characterCodes.Length)
        {
            UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(characterCodes[index], new int[] { 0x0041 }),
            };
            string cmap = new UnicodePdfCMap().CreateToUnicodeMap(
                new UnicodePdfGlyphMap("Width" + index.ToString(), mappings));

            Assert.That(
                UnicodePdfCMap.GetCharacterCodeHexWidth(mappings),
                Is.EqualTo(expectedWidths[index]),
                "width " + index.ToString());
            Assert.That(cmap, Does.Contain(expectedCodeSpaces[index]), "codespace " + index.ToString());
            Assert.That(
                cmap,
                Does.Contain(UnicodePdfCMap.FormatCharacterCode(characterCodes[index], expectedWidths[index]) + " <0041>"),
                "source " + index.ToString());
            index = index + 1;
        }
    }

    [Test]
    public void MappingSelection_UsesBfRangeOnlyForSequentialBmpSingleScalars()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x31, new int[] { 0x0066, 0x006C }),
                new UnicodePdfGlyphMapping(0x11, new int[] { 0x0042 }),
                new UnicodePdfGlyphMapping(0x21, new int[] { 0x1F601 }),
                new UnicodePdfGlyphMapping(0x10, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(0x12, new int[] { 0x0044 }),
                new UnicodePdfGlyphMapping(0x20, new int[] { 0x1F600 }),
                new UnicodePdfGlyphMapping(0x30, new int[] { 0x0066, 0x0069 }),
            });

        string cmap = new UnicodePdfCMap().CreateToUnicodeMap(glyphMap);

        Assert.That(cmap, Does.Contain("1 beginbfrange\n<10> <11> <0041>\nendbfrange\n"));
        Assert.That(cmap, Does.Contain("5 beginbfchar\n"));
        Assert.That(cmap, Does.Contain("<12> <0044>"));
        Assert.That(cmap, Does.Contain("<20> <D83DDE00>"));
        Assert.That(cmap, Does.Contain("<21> <D83DDE01>"));
        Assert.That(cmap, Does.Contain("<30> <00660069>"));
        Assert.That(cmap, Does.Contain("<31> <0066006C>"));
    }

    [Test]
    public void UnicodeDestinationEncoding_HandlesBmpBoundariesSupplementaryScalarsAndSequences()
    {
        int[] codePoints = new int[]
        {
            0x0000,
            0xD7FF,
            0xE000,
            0x10000,
            0x10FFFF,
        };
        string expected = "<0000D7FFE000D800DC00DBFFDFFF>";

        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x01, codePoints),
            });

        Assert.That(UnicodePdfCMap.FormatUnicodeString(codePoints), Is.EqualTo(expected));
        Assert.That(new UnicodePdfCMap().CreateToUnicodeMap(glyphMap), Does.Contain("<01> " + expected));
    }

    [Test]
    public void BfCharSections_SplitAtOneHundredEntries()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[201];
        int index = 0;
        while (index < mappings.Length)
        {
            mappings[index] = new UnicodePdfGlyphMapping(
                index * 2,
                new int[] { 0xE000 + index });
            index = index + 1;
        }

        string cmap = new UnicodePdfCMap().CreateToUnicodeMap(
            new UnicodePdfGlyphMap("F1", mappings));

        Assert.That(CountExactLines(cmap, "100 beginbfchar"), Is.EqualTo(2));
        Assert.That(CountExactLines(cmap, "1 beginbfchar"), Is.EqualTo(1));
        Assert.That(CountExactLines(cmap, "endbfchar"), Is.EqualTo(3));
        Assert.That(cmap, Does.Not.Contain("beginbfrange"));
    }

    [Test]
    public void BfRangeSections_SplitAtOneHundredEntries()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[402];
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

        Assert.That(CountExactLines(cmap, "100 beginbfrange"), Is.EqualTo(2));
        Assert.That(CountExactLines(cmap, "1 beginbfrange"), Is.EqualTo(1));
        Assert.That(CountExactLines(cmap, "endbfrange"), Is.EqualTo(3));
        Assert.That(cmap, Does.Not.Contain("beginbfchar"));
    }

    [Test]
    public void Validation_ReportsAllDuplicateAndMalformedMappingDetails()
    {
        UnicodePdfGlyphMapping negativeCharacterCode = new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 });
        negativeCharacterCode.CharacterCode = -1;
        UnicodePdfGlyphMapping nullScalars = new UnicodePdfGlyphMapping(0x02, new int[] { 0x0042 });
        nullScalars.UnicodeCodePoints = null!;
        UnicodePdfGlyphMapping emptyScalars = new UnicodePdfGlyphMapping(0x03, new int[] { 0x0043 });
        emptyScalars.UnicodeCodePoints = new int[] { };
        UnicodePdfGlyphMapping invalidScalars = new UnicodePdfGlyphMapping(0x04, new int[] { 0x0044 });
        invalidScalars.UnicodeCodePoints = new int[] { -1, 0xD800, 0x110000 };
        UnicodePdfGlyphMapping replacement = new UnicodePdfGlyphMapping(0x05, new int[] { UnicodeScalar.ReplacementCharacter });
        UnicodePdfGlyphMapping duplicateFirst = new UnicodePdfGlyphMapping(0x06, new int[] { 0x0046 });
        UnicodePdfGlyphMapping duplicateSecond = new UnicodePdfGlyphMapping(0x06, new int[] { 0x0047 });
        IUnicodePdfGlyphMap glyphMap = new ExternalGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                null!,
                negativeCharacterCode,
                nullScalars,
                emptyScalars,
                invalidScalars,
                replacement,
                duplicateFirst,
                duplicateSecond,
            });

        UnicodePdfCMapValidationResult result = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Length.EqualTo(9));
        AssertDiagnostic(result.Diagnostics[0], 0, -1, -1, UnicodePdfCMapData.InvalidMappingRule);
        AssertDiagnostic(result.Diagnostics[1], 1, -1, -1, UnicodePdfCMapData.InvalidMappingRule);
        AssertDiagnostic(result.Diagnostics[2], 2, 0x02, -1, UnicodePdfCMapData.MissingUnicodeScalarsRule);
        AssertDiagnostic(result.Diagnostics[3], 3, 0x03, -1, UnicodePdfCMapData.MissingUnicodeScalarsRule);
        AssertDiagnostic(result.Diagnostics[4], 4, 0x04, -1, UnicodePdfCMapData.InvalidUnicodeScalarRule);
        AssertDiagnostic(result.Diagnostics[5], 4, 0x04, 0xD800, UnicodePdfCMapData.InvalidUnicodeScalarRule);
        AssertDiagnostic(result.Diagnostics[6], 4, 0x04, 0x110000, UnicodePdfCMapData.InvalidUnicodeScalarRule);
        AssertDiagnostic(result.Diagnostics[7], 5, 0x05, UnicodeScalar.ReplacementCharacter, UnicodePdfCMapData.ReplacementCharacterRule);
        AssertDiagnostic(result.Diagnostics[8], 7, 0x06, -1, UnicodePdfCMapData.DuplicateCharacterCodeRule);
        Assert.Throws<ArgumentException>(() => new UnicodePdfCMap().CreateToUnicodeMap(glyphMap));
    }

    [Test]
    public void CMapOutput_IsIdenticalAcrossMappingPermutations()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[]
        {
            new UnicodePdfGlyphMapping(0x20, new int[] { 0x0041 }),
            new UnicodePdfGlyphMapping(0x21, new int[] { 0x0042 }),
            new UnicodePdfGlyphMapping(0x22, new int[] { 0x0043 }),
            new UnicodePdfGlyphMapping(0x30, new int[] { 0x0066, 0x0069 }),
            new UnicodePdfGlyphMapping(0x31, new int[] { 0x1F600 }),
            new UnicodePdfGlyphMapping(0x100, new int[] { 0x20AC }),
        };
        UnicodePdfGlyphMapping[] permutation = new UnicodePdfGlyphMapping[]
        {
            mappings[3],
            mappings[5],
            mappings[1],
            mappings[4],
            mappings[0],
            mappings[2],
        };

        string expected = new UnicodePdfCMap().CreateToUnicodeMap(
            new UnicodePdfGlyphMap("F1", mappings));
        string actual = new UnicodePdfCMap().CreateToUnicodeMap(
            new UnicodePdfGlyphMap("F1", permutation));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CMapGeneration_ConsumesOneExternalSnapshotDeterministically()
    {
        CountingGlyphMap glyphMap = new CountingGlyphMap();

        string cmap = new UnicodePdfCMap().CreateToUnicodeMap(glyphMap);

        Assert.That(glyphMap.FontNameCallCount, Is.EqualTo(1));
        Assert.That(glyphMap.MappingCallCount, Is.EqualTo(1));
        Assert.That(cmap, Does.Contain("/CMapName /SnapshotFont-ToUnicode def"));
        Assert.That(cmap, Does.Contain("<01> <02> <0041>"));
    }

    static int CountExactLines(string text, string expectedLine)
    {
        string[] lines = text.Split('\n');
        int count = 0;
        int index = 0;
        while (index < lines.Length)
        {
            if (lines[index] == expectedLine)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static void AssertDiagnostic(
        UnicodePdfCMapDiagnostic diagnostic,
        int mappingIndex,
        int characterCode,
        int codePoint,
        string rule)
    {
        Assert.That(diagnostic.MappingIndex, Is.EqualTo(mappingIndex));
        Assert.That(diagnostic.CharacterCode, Is.EqualTo(characterCode));
        Assert.That(diagnostic.CodePoint, Is.EqualTo(codePoint));
        Assert.That(diagnostic.Rule, Is.EqualTo(rule));
        Assert.That(diagnostic.Message, Is.Not.Empty);
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

    class CountingGlyphMap : IUnicodePdfGlyphMap
    {
        public int FontNameCallCount;
        public int MappingCallCount;

        public CountingGlyphMap()
        {
        }

        public string GetFontName()
        {
            FontNameCallCount = FontNameCallCount + 1;
            return "SnapshotFont";
        }

        public UnicodePdfGlyphMapping[] GetMappings()
        {
            MappingCallCount = MappingCallCount + 1;
            return new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(0x02, new int[] { 0x0042 }),
            };
        }
    }
}
