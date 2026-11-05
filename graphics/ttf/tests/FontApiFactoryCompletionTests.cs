using System.Text;
using ttf;

namespace ttf.Tests;

internal class FontApiFactoryCompletionTests
{
    [Test]
    public void CreatesSafeDefaultOptionsAndPolicyProfiles()
    {
        IFontApiFactory factory = new FontApiFactory();
        FontLoadOptions load = factory.CreateCompatibleLoadOptions();
        FontOperationOptions strict =
            factory.CreateStrictOperationOptions();
        FontOperationOptions compatible =
            factory.CreateCompatibleOperationOptions();
        FontOperationOptions inspect =
            factory.CreateInspectOnlyOperationOptions();
        TextShapingOptions shaping = factory.CreateShapingOptions();
        TextLayoutOptions layout = factory.CreateTextLayoutOptions();
        ColorGlyphOptions color = factory.CreateColorGlyphOptions();
        FontSubsetOptions subset =
            factory.CreateCompatibleSubsetOptions();
        PdfFontOptions pdf = factory.CreateCompatiblePdfOptions();

        Assert.Multiple(() =>
        {
            Assert.That(load.GetMode(),
                Is.EqualTo(FontLoadMode.Compatible));
            Assert.That(load.GetInputOwnership(),
                Is.EqualTo(FontInputOwnership.Copy));
            Assert.That(strict.GetMode(), Is.EqualTo(FontLoadMode.Strict));
            Assert.That(strict.GetLimits().GetMaxDiagnostics(),
                Is.EqualTo(factory.CreateStrictLimits()
                    .GetMaxDiagnostics()));
            Assert.That(compatible.GetMode(),
                Is.EqualTo(FontLoadMode.Compatible));
            Assert.That(compatible.GetLimits().GetMaxDiagnostics(),
                Is.EqualTo(factory.CreateCompatibleLimits()
                    .GetMaxDiagnostics()));
            Assert.That(inspect.GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(inspect.GetLimits().GetMaxDiagnostics(),
                Is.EqualTo(factory.CreateInspectOnlyLimits()
                    .GetMaxDiagnostics()));
            Assert.That(shaping.GetDecodingMode(),
                Is.EqualTo(Utf16DecodingMode.Strict));
            Assert.That(shaping.GetNormalizationMode(),
                Is.EqualTo(UnicodeNormalizationMode.Nfc));
            Assert.That(shaping.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.Compatible));
            Assert.That(layout.GetAlignment(),
                Is.EqualTo(TextAlignment.Start));
            Assert.That(layout.GetWritingMode(),
                Is.EqualTo(TextWritingMode.Horizontal));
            Assert.That(color.GetPaletteIndex(), Is.Zero);
            Assert.That(color.GetPreferredRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.None));
            Assert.That(subset.GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(subset.GetRightsPolicy(),
                Is.EqualTo(FontSubsetRightsPolicy.EmbedFullFont));
            Assert.That(pdf.GetFontKind(),
                Is.EqualTo(PdfFontKind.Automatic));
            Assert.That(pdf.GetRightsPolicy(),
                Is.EqualTo(PdfFontRightsPolicy.EmbedFullFont));
        });
    }

    [Test]
    public void CreatesConfiguredCoordinatesOperationsAndHyphenation()
    {
        IFontApiFactory factory = new FontApiFactory();
        int weightTag = (int)TestSfntEncoding.EncodeTag("wght");
        int widthTag = (int)TestSfntEncoding.EncodeTag("wdth");
        int[] tags = new int[] { weightTag, widthTag };
        double[] values = new double[] { 650, 85 };
        FontCoordinates coordinates =
            factory.CreateConfiguredCoordinates(tags, values);
        tags[0] = widthTag;
        values[0] = 1;
        FontLimits limits = factory.CreateCompatibleLimits();
        System.Threading.CancellationTokenSource cancellation =
            new System.Threading.CancellationTokenSource();
        FontOperationOptions operation =
            factory.CreateConfiguredOperationOptions(
                FontLoadMode.InspectOnly,
                limits,
                cancellation.Token);
        TextShapingOptions shaping =
            factory.CreateShapingOptionsWithOperation(operation);
        TextLayoutOptions layout =
            factory.CreateTextLayoutOptionsWithOperation(operation);
        ColorGlyphOptions color =
            factory.CreateColorGlyphOptionsWithOperation(operation);
        operation.SetMode(FontLoadMode.Strict);
        HyphenationLimits hyphenationLimits =
            factory.CreateConfiguredHyphenationLimits(
                1024,
                10,
                20,
                5,
                100,
                30);
        HyphenationDictionary dictionary =
            factory.CreateHyphenationDictionaryLoader().LoadFromBytesWithLimits(
                Encoding.UTF8.GetBytes("exception hy-phenation\n"),
                hyphenationLimits);
        FontRequest request = factory.CreateFamilyRequest("Example Sans");

        Assert.Multiple(() =>
        {
            Assert.That(coordinates.GetCount(), Is.EqualTo(2));
            Assert.That(coordinates.Get(weightTag), Is.EqualTo(650));
            Assert.That(coordinates.Get(widthTag), Is.EqualTo(85));
            Assert.That(shaping.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(layout.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(color.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(shaping.GetOperationOptions().GetLimits(),
                Is.SameAs(limits));
            Assert.That(shaping.GetOperationOptions().GetCancellationToken(),
                Is.EqualTo(cancellation.Token));
            Assert.That(dictionary.GetExceptionCount(), Is.EqualTo(1));
            Assert.That(dictionary.GetPatternCount(), Is.Zero);
            Assert.That(factory.CreateHyphenationLimits()
                .GetMaxInputBytes(), Is.EqualTo(1048576));
            Assert.That(request.GetFamilyName(), Is.EqualTo("Example Sans"));
            Assert.That(request.GetWeight(), Is.EqualTo(400));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateConfiguredCoordinates(null!, values));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateConfiguredCoordinates(tags, null!));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateConfiguredCoordinates(
                    new int[] { weightTag },
                    new double[0]));
        });
    }

    [Test]
    public void ConstructsServicesAndBuildersForACompleteWorkflow()
    {
        IFontApiFactory factory = new FontApiFactory();
        byte[] fontBytes = Convert.FromBase64String(
            HarfBuzzLayoutDifferentialTests.OracleFontBase64);
        FontPackage package = factory.CreateFontLoader().LoadFromBytes(
            fontBytes,
            factory.CreateCompatibleLoadOptions());
        FontFace face = package.GetFace(0);
        FontInstance instance = face.CreateDefaultInstance();
        FontSet set = factory.CreateFontSet();
        set.Add(face);
        string family = face.GetNames().GetFamilyName()!.GetValue();
        FontRequest request = factory.CreateFamilyRequest(family);
        TextShapingResult shaped = set.ShapeTextWithOptions(
            "A",
            request,
            factory.CreateShapingOptions());
        TextStyle textStyle = factory.CreateTextStyle(12);
        TextLayoutStyle layoutStyle = factory.CreateTextLayoutStyle(
            0,
            1,
            request,
            textStyle);
        PreparedTextResult prepared = factory.CreateTextLayoutEngine().Prepare(
            set,
            "A",
            new TextLayoutStyle[] { layoutStyle },
            factory.CreateTextLayoutOptions());
        ColorGlyph color = factory.CreateColorGlyphResolver()
            .ResolveGlyphWithOptions(
                instance,
                2,
                factory.CreateColorGlyphOptions());
        FontSubsetBuilder subset = factory.CreateSubsetBuilder(
            instance,
            factory.CreateCompatibleSubsetOptions());
        subset.AddText("A");
        FontSubsetResult subsetResult = subset.BuildResult();
        PdfFontBuilder pdf = factory.CreatePdfFontBuilder(
            instance,
            factory.CreateCompatiblePdfOptions());
        pdf.AddText("A");
        PdfFontResource resource = pdf.FinalizeFont();
        PdfTextUsageBuilder textUsage = factory.CreatePdfTextUsageBuilder(
            set,
            factory.CreateCompatiblePdfOptions());
        PdfTextUsageResult textResult = textUsage.AddText("A", request);
        PdfFontResource[] textResources = textUsage.FinalizeFonts();
        PdfFontResourceCollection resources =
            factory.CreatePdfFontResourceCollection();
        PdfFontResource registered = resources.GetOrAdd(resource);
        PdfToUnicodeCMap cmap = factory.CreatePdfToUnicodeCMapWriter()
            .BuildCMap(
                1,
                new PdfToUnicodeMapping[]
                {
                    new PdfToUnicodeMapping(1, new int[] { 0x41 })
                });

        Assert.Multiple(() =>
        {
            Assert.That(package.GetFaceCount(), Is.EqualTo(1));
            Assert.That(set.GetFaceCount(), Is.EqualTo(1));
            Assert.That(shaped.GetRunCount(), Is.EqualTo(1));
            Assert.That(prepared.GetStyleCount(), Is.EqualTo(1));
            Assert.That(prepared.Reflow(100).GetLineCount(),
                Is.EqualTo(1));
            Assert.That(color.GetGlyphId(), Is.EqualTo(2));
            Assert.That(subsetResult.CopyFontBytes().Length,
                Is.GreaterThan(0));
            Assert.That(pdf.IsFinalized(), Is.True);
            Assert.That(resources.GetCount(), Is.EqualTo(1));
            Assert.That(registered, Is.SameAs(resource));
            Assert.That(textResult.GetRunCount(), Is.EqualTo(1));
            Assert.That(textResources, Has.Length.EqualTo(1));
            Assert.That(textUsage.IsFinalized(), Is.True);
            Assert.That(cmap.GetMappingCount(), Is.EqualTo(1));
            Assert.That(cmap.GetText(), Does.Contain("<01> <0041>"));
        });
    }
}
