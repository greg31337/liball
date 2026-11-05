using System.Text;
using ttf;

namespace ttf.ConsumerTests;

internal class PublicApiConsumerTests
{
    const int SbixGlyphId = 19;

    [Test]
    public void LoadsAndInspectsAFontThroughPublicContracts()
    {
        IFontApiFactory factory = new FontApiFactory();
        FontPackage package = factory.CreateFontLoader().LoadFromFile(
            GetAssetPath(),
            factory.CreateCompatibleLoadOptions());
        FontFace face = package.GetFace(0);
        FontSet set = factory.CreateFontSet();
        set.Add(face);
        FontCharacterMapping mapping =
            face.GetCharacterMap().MapUnicode(0x41);
        ColorGlyph glyph = factory.CreateColorGlyphResolver().ResolveGlyphWithOptions(
            face.CreateDefaultInstance(),
            SbixGlyphId,
            factory.CreateColorGlyphOptions());

        Assert.Multiple(() =>
        {
            Assert.That(package.GetFaceCount(), Is.EqualTo(1));
            Assert.That(package.GetSourceKind(),
                Is.EqualTo(FontContainerKind.Sfnt));
            Assert.That(face.CreateDefaultInstance().GetGlyphCount(),
                Is.GreaterThan(SbixGlyphId));
            Assert.That(set.GetFaceCount(), Is.EqualTo(1));
            Assert.That(mapping.GetUnicodeValue(), Is.EqualTo(0x41));
            Assert.That(glyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Sbix));
            Assert.That(glyph.GetResolutionOptions().GetPreferredRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.None));
            Assert.That(glyph.GetSelectedBitmapStrike(), Is.Not.Null);
        });
    }

    [Test]
    public void ShapesLaysOutSubsetsAndBuildsPdfResourcesPublicly()
    {
        IFontApiFactory factory = new FontApiFactory();
        FontFace face = LoadFace(factory);
        FontInstance instance = face.CreateDefaultInstance();
        FontSet set = factory.CreateFontSet();
        set.Add(face);
        FontNameValue? familyName = face.GetNames().GetFamilyName();
        Assert.That(familyName, Is.Not.Null);
        FontRequest request = factory.CreateFamilyRequest(
            familyName == null ? "Missing" : familyName.GetValue());
        set.SetGenericFamily(
            FontGenericFamily.SansSerif,
            familyName == null ? "Missing" : familyName.GetValue());
        FontFallbackList genericFallbacks = set.GetFallbacks(
            factory.CreateGenericRequest(FontGenericFamily.SansSerif),
            "Latn");
        TextShapingResult shaped = set.ShapeTextWithOptions(
            "A",
            request,
            factory.CreateShapingOptions());
        TextLayoutStyle style = factory.CreateTextLayoutStyle(
            0,
            1,
            request,
            factory.CreateTextStyle(12));
        TextLayoutOptions layoutOptions = factory.CreateTextLayoutOptions();
        layoutOptions.SetAlignment(TextAlignment.Center);
        PreparedTextResult prepared = factory.CreateTextLayoutEngine().Prepare(
            set,
            "A",
            new TextLayoutStyle[] { style },
            layoutOptions);
        TextLayoutOptions retainedLayoutOptions = prepared.GetLayoutOptions();
        retainedLayoutOptions.SetAlignment(TextAlignment.End);
        FontSubsetBuilder subset = factory.CreateSubsetBuilder(
            instance,
            factory.CreateCompatibleSubsetOptions());
        subset.AddResolvedRun(shaped.GetRun(0));
        subset.AddGlyph(SbixGlyphId);
        FontSubsetResult subsetResult = subset.BuildResult();
        PdfFontBuilder pdfBuilder = factory.CreatePdfFontBuilder(
            instance,
            factory.CreateCompatiblePdfOptions());
        PdfEncodedRun resolvedPdfRun =
            pdfBuilder.AddResolvedRun(shaped.GetRun(0));
        pdfBuilder.AddGlyph(SbixGlyphId, new int[] { 0x41 });
        PdfFontResource resource = pdfBuilder.FinalizeFont();
        PdfFontResourceCollection collection =
            factory.CreatePdfFontResourceCollection();

        Assert.Multiple(() =>
        {
            Assert.That(shaped.GetRunCount(), Is.EqualTo(1));
            Assert.That(shaped.GetRun(0).GetPlanKind(),
                Is.EqualTo(TextShapingPlanKind.Default));
            Assert.That(shaped.GetRun(0).GetPlanStageCount(),
                Is.GreaterThan(0));
            Assert.That(genericFallbacks.GetMatch(0).GetFallbackMatchKind(),
                Is.EqualTo(FontFallbackMatchKind.GenericFamily));
            Assert.That(genericFallbacks.GetMatch(0).GetConfiguredFamilyName(),
                Is.EqualTo(familyName == null ? "Missing" : familyName.GetValue()));
            Assert.That(prepared.GetLayoutOptions().GetAlignment(),
                Is.EqualTo(TextAlignment.Center));
            Assert.That(prepared.Reflow(100).GetLineCount(),
                Is.EqualTo(1));
            Assert.That(subsetResult.CopyFontBytes(), Is.Not.Empty);
            Assert.That(subset.GetSourceInstance(), Is.SameAs(instance));
            Assert.That(subset.GetEffectiveOptions().GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(subsetResult.GetClosureReport().GetCompositeGlyphIds(),
                Is.Not.Null);
            Assert.That(resource.CopyEmbeddedFont(), Is.Not.Empty);
            Assert.That(resolvedPdfRun.GetSourceUtf16Length(),
                Is.EqualTo(1));
            Assert.That(pdfBuilder.GetSourceInstance(), Is.SameAs(instance));
            Assert.That(pdfBuilder.GetFontOptions().GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Horizontal));
            Assert.That(resource.GetFontOptions().GetFontKind(),
                Is.EqualTo(PdfFontKind.Automatic));
            Assert.That(collection.GetOrAdd(resource), Is.SameAs(resource));
            Assert.That(collection.GetCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ConfiguresIndependentPublicOptionAndUtilityObjects()
    {
        IFontApiFactory factory = new FontApiFactory();
        int weightTag = 0x77676874;
        FontCoordinates coordinates = factory.CreateConfiguredCoordinates(
            new int[] { weightTag },
            new double[] { 625 });
        FontOperationOptions operation =
            factory.CreateInspectOnlyOperationOptions();
        TextShapingOptions shaping =
            factory.CreateShapingOptionsWithOperation(operation);
        HyphenationDictionary dictionary =
            factory.CreateHyphenationDictionaryLoader().LoadFromBytes(
                Encoding.UTF8.GetBytes("exception con-sumer\n"));
        PdfToUnicodeCMap cmap = factory.CreatePdfToUnicodeCMapWriter()
            .BuildCMap(
                1,
                new PdfToUnicodeMapping[]
                {
                    new PdfToUnicodeMapping(1, new int[] { 0x41 })
                });

        Assert.Multiple(() =>
        {
            Assert.That(coordinates.Get(weightTag), Is.EqualTo(625));
            Assert.That(shaping.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(dictionary.GetExceptionCount(), Is.EqualTo(1));
            Assert.That(cmap.GetMappingCount(), Is.EqualTo(1));
            Assert.That(cmap.GetText(), Does.Contain("<01> <0041>"));
        });
    }

    static FontFace LoadFace(IFontApiFactory factory)
    {
        return factory.CreateFontLoader().LoadFromFile(
            GetAssetPath(),
            factory.CreateCompatibleLoadOptions()).GetFace(0);
    }

    static string GetAssetPath()
    {
        return Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Assets",
            "samples-sbix.ttf");
    }
}
