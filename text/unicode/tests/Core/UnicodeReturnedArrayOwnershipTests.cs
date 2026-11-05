using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeReturnedArrayOwnershipTests
{
    [Test]
    public void CoreLookupArrays_AreIndependentCallerOwnedSnapshots()
    {
        IUnicodeData data = new UnicodeData();
        UnicodeDataSource[] sources = data.GetDataSources();
        sources[0].Name = "changed";
        Assert.That(data.GetDataSources()[0].Name, Is.EqualTo("Unicode Version"));

        IUnicodeHangul hangul = new UnicodeHangul();
        int[] jamo = hangul.DecomposeSyllable(0xAC01);
        jamo[0] = 0x0041;
        Assert.That(hangul.DecomposeSyllable(0xAC01), Is.EqualTo(new int[] { 0x1100, 0x1161, 0x11A8 }));

        IUnicodeProperties properties = new UnicodeProperties();
        int[] decomposition = properties.GetDecompositionMapping(0x00C0);
        decomposition[0] = 0x0042;
        Assert.That(properties.GetDecompositionMapping(0x00C0), Is.EqualTo(new int[] { 0x0041, 0x0300 }));

        UnicodeScript[] extensions = properties.GetScriptExtensions(0x30FC);
        UnicodeScript expectedScript = extensions[0];
        extensions[0] = UnicodeScript.Unknown;
        Assert.That(properties.GetScriptExtensions(0x30FC)[0], Is.EqualTo(expectedScript));

        UnicodeNameAlias[] aliases = properties.GetNameAliases(0x0000);
        string expectedAlias = aliases[0].Name;
        aliases[0].Name = "changed";
        Assert.That(properties.GetNameAliases(0x0000)[0].Name, Is.EqualTo(expectedAlias));

        UnicodeNamedSequence[] sequences = properties.GetNamedSequences();
        string expectedName = sequences[0].Name;
        int expectedCodePoint = sequences[0].CodePoints[0];
        sequences[0].Name = "changed";
        sequences[0].CodePoints[0] = 0x0041;
        UnicodeNamedSequence[] secondSequences = properties.GetNamedSequences();
        Assert.That(secondSequences[0].Name, Is.EqualTo(expectedName));
        Assert.That(secondSequences[0].CodePoints[0], Is.EqualTo(expectedCodePoint));
        int[] namedSequence = properties.LookupNamedSequence(expectedName);
        namedSequence[0] = 0x0041;
        Assert.That(properties.LookupNamedSequence(expectedName)[0], Is.EqualTo(expectedCodePoint));
    }

    [Test]
    public void ReaderSegmentationAndSecurityArrays_AreIndependentSnapshots()
    {
        IUnicodeScalarReader reader = new UnicodeScalarReader();
        UnicodeScalarRecord[] records = reader.ReadAll("A");
        records[0].CodePoint = 0x0042;
        Assert.That(reader.ReadAll("A")[0].CodePoint, Is.EqualTo(0x0041));

        IUnicodeSegments segments = new UnicodeSegments();
        int[] breaks = segments.GetGraphemeBreaks("A\u0301B");
        breaks[1] = 1;
        Assert.That(segments.GetGraphemeBreaks("A\u0301B"), Is.EqualTo(new int[] { 0, 2, 3 }));
        UnicodeSegmentRecord[] wordSegments = segments.GetWordSegments("one two");
        wordSegments[0].EndUtf16Index = 0;
        Assert.That(segments.GetWordSegments("one two")[0].EndUtf16Index, Is.EqualTo(3));
        UnicodeLineBreakRecord[] lineBreaks = segments.GetLineBreakOpportunities("A B");
        lineBreaks[0].Utf16Index = 0;
        Assert.That(segments.GetLineBreakOpportunities("A B")[0].Utf16Index, Is.EqualTo(2));

        IUnicodeSecurity security = new UnicodeSecurity();
        UnicodeIdentifierType[] types = security.GetIdentifierTypes(0x0041);
        UnicodeIdentifierType expectedType = types[0];
        types[0] = UnicodeIdentifierType.NotCharacter;
        Assert.That(security.GetIdentifierTypes(0x0041)[0], Is.EqualTo(expectedType));
        UnicodeScript[] scripts = security.GetScriptSet("A\u03B1");
        scripts[0] = UnicodeScript.Unknown;
        Assert.That(security.GetScriptSet("A\u03B1")[0], Is.EqualTo(UnicodeScript.Latin));
        UnicodeSecurityDiagnostic[] diagnostics = security.GetSecurityDiagnostics("A\u200B");
        string expectedRule = diagnostics[0].Rule;
        diagnostics[0].Rule = "changed";
        Assert.That(security.GetSecurityDiagnostics("A\u200B")[0].Rule, Is.EqualTo(expectedRule));
    }

    [Test]
    public void PdfReturnedArrays_AreIndependentSnapshotsAcrossCalls()
    {
        IUnicodePdfEncoding encoding = new UnicodePdfEncoding();
        byte[] encoded = encoding.EncodeTextString("A");
        encoded[0] = 0;
        Assert.That(encoding.EncodeTextString("A"), Is.EqualTo(new byte[] { 0x41 }));

        IUnicodePdfGlyphMap glyphMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
            });
        UnicodePdfGlyphMapping[] mappings = glyphMap.GetMappings()!;
        mappings[0].UnicodeCodePoints[0] = 0x0042;
        Assert.That(glyphMap.GetMappings()![0].UnicodeCodePoints, Is.EqualTo(new int[] { 0x0041 }));

        IUnicodePdfText pdfText = new UnicodePdfText();
        UnicodePdfLayoutInfo firstLayout = pdfText.GetLayoutInfo("A");
        firstLayout.GraphemeClusters[0].StartUtf16Index = 8;
        firstLayout.WordSegments[0].StartUtf16Index = 8;
        firstLayout.LineBreaks[0].Utf16Index = 8;
        firstLayout.ScriptRuns[0].StartUtf16Index = 8;
        firstLayout.BidiRuns[0].StartUtf16Index = 8;
        firstLayout.Scalars[0].CodePoint = 0x0042;
        UnicodePdfLayoutInfo secondLayout = pdfText.GetLayoutInfo("A");
        Assert.That(secondLayout.GraphemeClusters[0].StartUtf16Index, Is.EqualTo(0));
        Assert.That(secondLayout.WordSegments[0].StartUtf16Index, Is.EqualTo(0));
        Assert.That(secondLayout.LineBreaks[0].Utf16Index, Is.EqualTo(1));
        Assert.That(secondLayout.ScriptRuns[0].StartUtf16Index, Is.EqualTo(0));
        Assert.That(secondLayout.BidiRuns[0].StartUtf16Index, Is.EqualTo(0));
        Assert.That(secondLayout.Scalars[0].CodePoint, Is.EqualTo(0x0041));

        IUnicodePdfFontCoverage coverage = new LatinCoverage();
        IUnicodePdfFontSubset subset = new UnicodePdfFontSubset();
        UnicodePdfFontSubsetPlan firstPlan = subset.CreateSubsetPlan("A", coverage);
        firstPlan.RequiredScalars[0] = 0x0042;
        firstPlan.ScriptPlans[0].RequiredScalars[0] = 0x0042;
        firstPlan.Clusters[0].Scalars[0] = 0x0042;
        UnicodePdfFontSubsetPlan secondPlan = subset.CreateSubsetPlan("A", coverage);
        Assert.That(secondPlan.RequiredScalars, Is.EqualTo(new int[] { 0x0041 }));
        Assert.That(secondPlan.ScriptPlans[0].RequiredScalars, Is.EqualTo(new int[] { 0x0041 }));
        Assert.That(secondPlan.Clusters[0].Scalars, Is.EqualTo(new int[] { 0x0041 }));

        IUnicodePdfGlyphMap invalidMap = new DuplicateGlyphMap();
        IUnicodePdfCMap cmap = new UnicodePdfCMap();
        UnicodePdfCMapValidationResult firstValidation = cmap.ValidateToUnicodeMap(invalidMap);
        string expectedValidationRule = firstValidation.Diagnostics[0].Rule;
        firstValidation.Diagnostics[0].Rule = "changed";
        Assert.That(cmap.ValidateToUnicodeMap(invalidMap).Diagnostics[0].Rule, Is.EqualTo(expectedValidationRule));
    }

    class LatinCoverage : IUnicodePdfFontCoverage
    {
        public bool CoversScalar(int codePoint)
        {
            return codePoint == 0x0041;
        }
    }

    class DuplicateGlyphMap : IUnicodePdfGlyphMap
    {
        public string GetFontName()
        {
            return "Duplicate";
        }

        public UnicodePdfGlyphMapping[]? GetMappings()
        {
            return new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(1, new int[] { 0x0042 }),
            };
        }
    }
}
