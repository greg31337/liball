using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfCMapValidationTests
{
    [Test]
    public void ValidateToUnicodeMap_AcceptsUniqueScalarMappings()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(0x02, new int[] { 0x0066, 0x0069 }),
            });

        UnicodePdfCMapValidationResult result = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfCMapStatus.Valid));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ValidateToUnicodeMap_RejectsEmptyMaps()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap("F1", new UnicodePdfGlyphMapping[] { });

        UnicodePdfCMapValidationResult result = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfCMapStatus.EmptyMap));
        AssertDiagnostic(result.Diagnostics[0], -1, -1, -1, UnicodePdfCMapStatus.EmptyMap, UnicodePdfCMap.EmptyMapRule);
    }

    [Test]
    public void ValidateToUnicodeMap_RejectsDuplicateCharacterCodes()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(0x01, new int[] { 0x0042 }),
            });

        UnicodePdfCMapValidationResult result = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfCMapStatus.DuplicateCharacterCode));
        AssertDiagnostic(
            FindDiagnostic(result, UnicodePdfCMap.DuplicateCharacterCodeRule),
            1,
            0x01,
            -1,
            UnicodePdfCMapStatus.DuplicateCharacterCode,
            UnicodePdfCMap.DuplicateCharacterCodeRule);
    }

    [Test]
    public void ValidateToUnicodeMap_DefensivelyRejectsMutatedNullAndEmptyMappings()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[]
        {
            new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 }),
            new UnicodePdfGlyphMapping(0x02, new int[] { 0x0042 }),
        };
        mappings[0] = null!;
        mappings[1].UnicodeCodePoints = new int[] { };

        UnicodePdfCMapValidationResult result = UnicodePdfCMap.ValidateToUnicodeMappings(mappings);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfCMapStatus.InvalidMapping));
        AssertDiagnostic(
            FindDiagnostic(result, UnicodePdfCMap.InvalidMappingRule),
            0,
            -1,
            -1,
            UnicodePdfCMapStatus.InvalidMapping,
            UnicodePdfCMap.InvalidMappingRule);
        AssertDiagnostic(
            FindDiagnostic(result, UnicodePdfCMap.MissingUnicodeScalarsRule),
            1,
            0x02,
            -1,
            UnicodePdfCMapStatus.MissingUnicodeScalars,
            UnicodePdfCMap.MissingUnicodeScalarsRule);
    }

    [Test]
    public void ValidateToUnicodeMap_RejectsInvalidAndReplacementScalars()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[]
        {
            new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 }),
            new UnicodePdfGlyphMapping(0x02, new int[] { 0x0042 }),
        };
        mappings[0].UnicodeCodePoints = new int[] { 0xD800 };
        mappings[1].UnicodeCodePoints = new int[] { UnicodeScalar.ReplacementCharacter };

        UnicodePdfCMapValidationResult result = UnicodePdfCMap.ValidateToUnicodeMappings(mappings);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfCMapStatus.InvalidUnicodeScalar));
        AssertDiagnostic(
            FindDiagnostic(result, UnicodePdfCMap.InvalidUnicodeScalarRule),
            0,
            0x01,
            0xD800,
            UnicodePdfCMapStatus.InvalidUnicodeScalar,
            UnicodePdfCMap.InvalidUnicodeScalarRule);
        AssertDiagnostic(
            FindDiagnostic(result, UnicodePdfCMap.ReplacementCharacterRule),
            1,
            0x02,
            UnicodeScalar.ReplacementCharacter,
            UnicodePdfCMapStatus.ReplacementCharacter,
            UnicodePdfCMap.ReplacementCharacterRule);
    }

    [Test]
    public void ValidateToUnicodeMap_DefensivelyRejectsMutatedNegativeCharacterCodes()
    {
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[]
        {
            new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 }),
        };
        mappings[0].CharacterCode = -1;

        UnicodePdfCMapValidationResult result = UnicodePdfCMap.ValidateToUnicodeMappings(mappings);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfCMapStatus.InvalidMapping));
    }

    [Test]
    public void CMapValidationHelpers_RejectInvalidArguments()
    {
        System.Collections.Generic.List<UnicodePdfCMapDiagnostic> nullMappingDiagnostics = new System.Collections.Generic.List<UnicodePdfCMapDiagnostic>();

        Assert.Throws<ArgumentNullException>(() => new UnicodePdfCMap().ValidateToUnicodeMap(null!));
        UnicodePdfCMap.AddMappingDiagnostics(null!, 0, nullMappingDiagnostics);
        Assert.That(nullMappingDiagnostics, Has.Count.EqualTo(1));
        AssertDiagnostic(
            nullMappingDiagnostics[0],
            0,
            -1,
            -1,
            UnicodePdfCMapStatus.InvalidMapping,
            UnicodePdfCMap.InvalidMappingRule);
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AddMappingDiagnostics(new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }), 0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.AddMappingDiagnostics(new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }), -1, new System.Collections.Generic.List<UnicodePdfCMapDiagnostic>()));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AddDuplicateCharacterCodeDiagnostics(null!, 0, new System.Collections.Generic.List<UnicodePdfCMapDiagnostic>()));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.AddDuplicateCharacterCodeDiagnostics(new UnicodePdfGlyphMapping[] { }, 0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.AddDuplicateCharacterCodeDiagnostics(new UnicodePdfGlyphMapping[] { }, -1, new System.Collections.Generic.List<UnicodePdfCMapDiagnostic>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfCMap.AddDuplicateCharacterCodeDiagnostics(new UnicodePdfGlyphMapping[] { }, 0, new System.Collections.Generic.List<UnicodePdfCMapDiagnostic>()));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.CreateValidationResult(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfCMap.GetValidationStatus(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfCMapValidationResult(true, UnicodePdfCMapStatus.Valid, null!));
        Assert.Throws<ArgumentException>(() => new UnicodePdfCMapDiagnostic(0, 0, 0, UnicodePdfCMapStatus.Valid, "", "message"));
        Assert.Throws<ArgumentException>(() => new UnicodePdfCMapDiagnostic(0, 0, 0, UnicodePdfCMapStatus.Valid, "rule", ""));
    }

    static UnicodePdfCMapDiagnostic FindDiagnostic(UnicodePdfCMapValidationResult result, string rule)
    {
        int index = 0;

        while (index < result.Diagnostics.Length)
        {
            if (result.Diagnostics[index].Rule == rule)
            {
                return result.Diagnostics[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing CMap diagnostic rule " + rule);
        return result.Diagnostics[0];
    }

    static void AssertDiagnostic(
        UnicodePdfCMapDiagnostic diagnostic,
        int mappingIndex,
        int characterCode,
        int codePoint,
        UnicodePdfCMapStatus status,
        string rule)
    {
        Assert.That(diagnostic.MappingIndex, Is.EqualTo(mappingIndex));
        Assert.That(diagnostic.CharacterCode, Is.EqualTo(characterCode));
        Assert.That(diagnostic.CodePoint, Is.EqualTo(codePoint));
        Assert.That(diagnostic.Status, Is.EqualTo(status));
        Assert.That(diagnostic.Rule, Is.EqualTo(rule));
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
