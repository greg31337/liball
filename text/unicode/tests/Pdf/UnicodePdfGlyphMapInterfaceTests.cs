using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfGlyphMapInterfaceTests
{
    [Test]
    public void CMapGeneration_AcceptsExternalGlyphMapSource()
    {
        IUnicodePdfGlyphMap glyphMap = new TestGlyphMap(
            "ExternalFont",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(2, new int[] { 0x0042 }),
            });

        string cmap = new UnicodePdfCMap().CreateToUnicodeMap(glyphMap);

        Assert.That(cmap, Does.Contain("/CMapName /ExternalFont-ToUnicode def"));
        Assert.That(cmap, Does.Contain("<01> <02> <0041>"));
    }

    [Test]
    public void CMapValidation_HandlesMalformedExternalSnapshot()
    {
        IUnicodePdfGlyphMap glyphMap = new TestGlyphMap("ExternalFont", null);

        UnicodePdfCMapValidationResult result = new UnicodePdfCMap().ValidateToUnicodeMap(glyphMap);
        UnicodePdfDiagnosticResult diagnostics = new UnicodePdfDiagnostics().CheckFontUnicodeMapping(glyphMap);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfCMapStatus.InvalidMapping));
        Assert.That(diagnostics.IsValid, Is.False);
        Assert.That(diagnostics.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.InvalidUnicodeMap));
    }

    [Test]
    public void BuiltInGlyphMap_ImplementsSnapshotContract()
    {
        UnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
            });

        UnicodePdfGlyphMapping[] first = glyphMap.GetMappings();
        UnicodePdfGlyphMapping[] second = glyphMap.GetMappings();
        first[0] = new UnicodePdfGlyphMapping(2, new int[] { 0x0042 });

        Assert.That(glyphMap.GetFontName(), Is.EqualTo("F1"));
        Assert.That(second[0].CharacterCode, Is.EqualTo(1));
    }

    class TestGlyphMap : IUnicodePdfGlyphMap
    {
        string fontName;
        UnicodePdfGlyphMapping[]? mappings;

        public TestGlyphMap(string fontName, UnicodePdfGlyphMapping[]? mappings)
        {
            this.fontName = fontName;
            this.mappings = mappings;
        }

        public string GetFontName()
        {
            return fontName;
        }

        public UnicodePdfGlyphMapping[]? GetMappings()
        {
            return mappings;
        }
    }
}
