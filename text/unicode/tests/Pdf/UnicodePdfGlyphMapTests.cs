using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfGlyphMapTests
{
    [Test]
    public void GlyphMapping_StoresCharacterCodeAndUnicodeScalarSequence()
    {
        int[] codePoints = new int[] { 0x0066, 0x0069 };

        UnicodePdfGlyphMapping mapping = new UnicodePdfGlyphMapping(42, codePoints);
        codePoints[0] = 0x0041;

        Assert.That(mapping.CharacterCode, Is.EqualTo(42));
        Assert.That(mapping.UnicodeCodePoints, Is.EqualTo(new int[] { 0x0066, 0x0069 }));
        Assert.That(mapping.Count(), Is.EqualTo(2));
        Assert.That(mapping.MapsToSingleScalar(), Is.False);
        Assert.That(mapping.GetUnicodeCodePoints(), Is.EqualTo(new int[] { 0x0066, 0x0069 }));
    }

    [Test]
    public void GlyphMapping_ReportsSingleScalarMappings()
    {
        UnicodePdfGlyphMapping mapping = new UnicodePdfGlyphMapping(1, new int[] { 0x0041 });

        Assert.That(mapping.MapsToSingleScalar(), Is.True);
    }

    [Test]
    public void GlyphRange_StoresCharacterCodeRangeAndStartScalar()
    {
        UnicodePdfGlyphRange range = new UnicodePdfGlyphRange(0x20, 0x24, 0x0041);

        Assert.That(range.StartCharacterCode, Is.EqualTo(0x20));
        Assert.That(range.EndCharacterCode, Is.EqualTo(0x24));
        Assert.That(range.StartUnicodeCodePoint, Is.EqualTo(0x0041));
        Assert.That(range.Count(), Is.EqualTo(5));
        Assert.That(range.ContainsCharacterCode(0x20), Is.True);
        Assert.That(range.ContainsCharacterCode(0x24), Is.True);
        Assert.That(range.ContainsCharacterCode(0x25), Is.False);
    }

    [Test]
    public void GlyphMap_StoresFontNameAndMappingsWithDefensiveArrayCopy()
    {
        UnicodePdfGlyphMapping first = new UnicodePdfGlyphMapping(0x01, new int[] { 0x0041 });
        UnicodePdfGlyphMapping second = new UnicodePdfGlyphMapping(0x02, new int[] { 0x0042 });
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[] { first, second };

        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap("F1", mappings);
        mappings[0] = second;

        Assert.That(glyphMap.FontName, Is.EqualTo("F1"));
        Assert.That(glyphMap.Count(), Is.EqualTo(2));
        Assert.That(glyphMap.Mappings[0], Is.Not.SameAs(first));
        Assert.That(glyphMap.Mappings[0].CharacterCode, Is.EqualTo(0x01));
        Assert.That(glyphMap.Mappings[0].UnicodeCodePoints, Is.EqualTo(new int[] { 0x0041 }));
        Assert.That(glyphMap.ContainsCharacterCode(0x01), Is.True);
        Assert.That(glyphMap.ContainsCharacterCode(0x03), Is.False);
        Assert.That(glyphMap.FindMapping(0x02), Is.Not.SameAs(second));
        Assert.That(glyphMap.FindMapping(0x03), Is.Null);
        Assert.That(glyphMap.GetMappings()[0].CharacterCode, Is.EqualTo(first.CharacterCode));
        Assert.That(glyphMap.GetMappings()[1].CharacterCode, Is.EqualTo(second.CharacterCode));
    }

    [Test]
    public void GlyphRecordConstructors_RejectInvalidArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfGlyphMapping(-1, new int[] { 0x0041 }));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfGlyphMapping(1, null!));
        Assert.Throws<ArgumentException>(() => new UnicodePdfGlyphMapping(1, new int[] { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfGlyphMapping(1, new int[] { -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfGlyphMapping(1, new int[] { 0xD800 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfGlyphRange(-1, 1, 0x0041));
        Assert.Throws<ArgumentException>(() => new UnicodePdfGlyphRange(2, 1, 0x0041));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfGlyphRange(1, 1, 0x110000));
        Assert.Throws<ArgumentException>(() => new UnicodePdfGlyphMap("", new UnicodePdfGlyphMapping[] { }));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfGlyphMap("F1", null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfGlyphMap("F1", new UnicodePdfGlyphMapping[] { null! }));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfGlyphMapping.ValidateUnicodeCodePoints(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfGlyphMapping.CopyCodePoints(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfGlyphMap.ValidateMappings(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfGlyphMap.CopyMappings(null!));
    }
}
