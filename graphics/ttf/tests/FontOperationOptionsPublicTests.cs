using ttf;

namespace ttf.Tests;

internal class FontOperationOptionsPublicTests
{
    [Test]
    public void SnapshotsModeLimitsAndCancellationAcrossOptionTypes()
    {
        FontOperationOptions operation = new FontOperationOptions();
        FontLimits limits = CreateLimits(123456, 321, 654, 7);
        operation.SetMode(FontLoadMode.InspectOnly);
        operation.SetLimits(limits);
        TextShapingOptions shaping = new TextShapingOptions();
        TextLayoutOptions layout = new TextLayoutOptions();
        ColorGlyphOptions color = new ColorGlyphOptions();
        FontSubsetOptions subset = new FontSubsetOptions();
        PdfFontOptions pdf = new PdfFontOptions();

        shaping.SetOperationOptions(operation);
        layout.SetOperationOptions(operation);
        color.SetOperationOptions(operation);
        subset.SetOperationOptions(operation);
        pdf.SetOperationOptions(operation);
        operation.SetMode(FontLoadMode.Strict);

        Assert.Multiple(() =>
        {
            AssertOperation(shaping.GetOperationOptions(), limits);
            AssertOperation(layout.GetOperationOptions(), limits);
            AssertOperation(color.GetOperationOptions(), limits);
            AssertOperation(subset.GetOperationOptions(), limits);
            AssertOperation(pdf.GetOperationOptions(), limits);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                operation.SetMode((FontLoadMode)99));
            Assert.Throws<ArgumentNullException>(() =>
                operation.SetLimits(null!));
            Assert.Throws<ArgumentNullException>(() =>
                shaping.SetOperationOptions(null!));
            Assert.Throws<ArgumentNullException>(() =>
                layout.SetOperationOptions(null!));
            Assert.Throws<ArgumentNullException>(() =>
                color.SetOperationOptions(null!));
            Assert.Throws<ArgumentNullException>(() =>
                subset.SetOperationOptions(null!));
            Assert.Throws<ArgumentNullException>(() =>
                pdf.SetOperationOptions(null!));
        });
    }

    [Test]
    public void ObservesCancellationAtEveryPublicOperationBoundary()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        FontInstance instance = face.CreateDefaultInstance();
        FontSet set = new FontSet();
        set.Add(face);
        string family = face.GetNames().GetFamilyName()!.GetValue();
        FontRequest request = new FontRequest(family);
        FontOperationOptions canceled = CreateCanceledOperation();
        TextShapingOptions shaping = new TextShapingOptions();
        shaping.SetOperationOptions(canceled);
        TextLayoutOptions layout = new TextLayoutOptions();
        layout.SetOperationOptions(canceled);
        ColorGlyphOptions color = new ColorGlyphOptions();
        color.SetOperationOptions(canceled);
        FontSubsetOptions subsetOptions = new FontSubsetOptions();
        subsetOptions.SetOperationOptions(canceled);
        FontSubsetBuilder subset = new FontSubsetBuilder(
            instance,
            subsetOptions);

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                instance.ShapeTextWithOptions("A", shaping));
            Assert.Throws<OperationCanceledException>(() =>
                new TextLayoutEngine().Prepare(
                    set,
                    "A",
                    new TextLayoutStyle[]
                    {
                        new TextLayoutStyle(
                            0,
                            1,
                            request,
                            new TextStyle(12))
                    },
                    layout));
            Assert.Throws<OperationCanceledException>(() =>
                new ColorGlyphResolver().ResolveGlyphWithOptions(
                    instance,
                    2,
                    color));
            Assert.Throws<OperationCanceledException>(() =>
                subset.AddGlyph(2));
            Assert.Throws<OperationCanceledException>(() =>
                instance.CopyStaticSfntBytesWithOptions(canceled));
        });

        System.Threading.CancellationTokenSource cancellation =
            new System.Threading.CancellationTokenSource();
        FontOperationOptions pdfOperation = new FontOperationOptions();
        pdfOperation.SetCancellationToken(cancellation.Token);
        PdfFontOptions pdfOptions = new PdfFontOptions();
        pdfOptions.SetOperationOptions(pdfOperation);
        PdfFontBuilder pdf = new PdfFontBuilder(instance, pdfOptions);
        pdf.AddText("A");
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => pdf.FinalizeFont());
        Assert.That(pdf.IsFinalized(), Is.False);
    }

    [Test]
    public void AppliesStricterOperationLimitsToShapingAndFontOutputs()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        FontInstance instance = face.CreateDefaultInstance();
        FontOperationOptions runeLimited = new FontOperationOptions();
        runeLimited.SetLimits(CreateLimits(536870912, 1, 2000000, 4096));
        TextShapingOptions shaping = new TextShapingOptions();
        shaping.SetOperationOptions(runeLimited);
        FontOperationOptions outputLimited = new FontOperationOptions();
        outputLimited.SetLimits(CreateLimits(1, 65535, 2000000, 4096));
        FontSubsetOptions subsetOptions = new FontSubsetOptions();
        subsetOptions.SetOperationOptions(outputLimited);
        FontSubsetBuilder subset = new FontSubsetBuilder(
            instance,
            subsetOptions);
        subset.AddText("A");
        PdfFontOptions pdfOptions = new PdfFontOptions();
        pdfOptions.SetOperationOptions(outputLimited);
        PdfFontBuilder pdf = new PdfFontBuilder(instance, pdfOptions);
        pdf.AddText("A");

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() =>
                instance.ShapeTextWithOptions("AA", shaping));
            Assert.Throws<FontLimitException>(() => subset.BuildResult());
            Assert.Throws<FontLimitException>(() =>
                instance.CopyStaticSfntBytesWithOptions(outputLimited));
            Assert.Throws<FontLimitException>(() => pdf.FinalizeFont());
            Assert.That(pdf.IsFinalized(), Is.False);
        });
    }

    [Test]
    public void CarriesPoliciesAndDeterministicDiagnosticsThroughResults()
    {
        FontOperationOptions inspect = new FontOperationOptions();
        inspect.SetMode(FontLoadMode.InspectOnly);
        TextShapingOptions shapingOptions = new TextShapingOptions();
        shapingOptions.SetOperationOptions(inspect);
        TextShapingResult shaping = new FontSet().ShapeTextWithOptions(
            "A",
            new FontRequest("Missing"),
            shapingOptions);
        TextLayoutOptions layoutOptions = new TextLayoutOptions();
        layoutOptions.SetOperationOptions(inspect);
        PreparedTextResult prepared = new TextLayoutEngine().Prepare(
            new FontSet(),
            "A",
            new TextLayoutStyle[]
            {
                new TextLayoutStyle(
                    0,
                    1,
                    new FontRequest("Missing"),
                    new TextStyle(12))
            },
            layoutOptions);
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        FontInstance instance = face.CreateDefaultInstance();
        ColorGlyphOptions colorOptions = new ColorGlyphOptions();
        colorOptions.SetPreferredRepresentation(
            ColorGlyphRepresentation.Svg);
        colorOptions.SetOperationOptions(inspect);
        ColorGlyph color = new ColorGlyphResolver()
            .ResolveGlyphWithOptions(instance, 2, colorOptions);
        FontSubsetOptions subsetOptions = new FontSubsetOptions();
        subsetOptions.SetOperationOptions(inspect);
        FontSubsetBuilder subsetBuilder = new FontSubsetBuilder(
            instance,
            subsetOptions);
        subsetBuilder.AddText("A");
        FontSubsetResult subset = subsetBuilder.BuildResult();
        PdfFontOptions pdfOptions = new PdfFontOptions();
        pdfOptions.SetOperationOptions(inspect);
        PdfFontBuilder pdfBuilder = new PdfFontBuilder(
            instance,
            pdfOptions);
        pdfBuilder.AddText("A");
        PdfFontResource pdf = pdfBuilder.FinalizeFont();

        Assert.Multiple(() =>
        {
            Assert.That(shaping.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(shaping.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(prepared.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(prepared.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(prepared.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(color.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(color.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(color.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.InvalidColorGlyph));
            Assert.That(subset.GetEffectiveOptions().GetOperationOptions()
                .GetMode(), Is.EqualTo(FontLoadMode.InspectOnly));
            Assert.That(pdf.GetOperationOptions().GetMode(),
                Is.EqualTo(FontLoadMode.InspectOnly));
        });

        FontOperationOptions strict = new FontOperationOptions();
        strict.SetMode(FontLoadMode.Strict);
        shapingOptions.SetOperationOptions(strict);
        colorOptions.SetOperationOptions(strict);

        Assert.Multiple(() =>
        {
            FontFormatException shapingFailure = Assert.Throws<
                FontFormatException>(() =>
                    new FontSet().ShapeTextWithOptions(
                        "A",
                        new FontRequest("Missing"),
                        shapingOptions))!;
            Assert.That(shapingFailure.GetDiagnosticCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            FontFormatException colorFailure = Assert.Throws<
                FontFormatException>(() =>
                    new ColorGlyphResolver().ResolveGlyphWithOptions(
                        instance,
                        2,
                        colorOptions))!;
            Assert.That(colorFailure.GetDiagnosticCode(),
                Is.EqualTo(FontDiagnosticCode.InvalidColorGlyph));
        });
    }

    static FontOperationOptions CreateCanceledOperation()
    {
        System.Threading.CancellationTokenSource cancellation =
            new System.Threading.CancellationTokenSource();
        cancellation.Cancel();
        FontOperationOptions operation = new FontOperationOptions();
        operation.SetCancellationToken(cancellation.Token);
        return operation;
    }

    static FontLimits CreateLimits(
        int maxOutputBytes,
        int maxGlyphsPerFace,
        int maxCoverageEntries,
        int maxDiagnostics)
    {
        return new FontApiFactory().CreateConfiguredLimits(
            268435456,
            maxOutputBytes,
            256,
            1024,
            maxGlyphsPerFace,
            1048576,
            32767,
            64,
            10,
            32,
            16384,
            5000000,
            maxCoverageEntries,
            4194304,
            536870912,
            250,
            65536,
            128,
            maxDiagnostics);
    }

    static void AssertOperation(
        FontOperationOptions operation,
        FontLimits limits)
    {
        Assert.That(operation.GetMode(),
            Is.EqualTo(FontLoadMode.InspectOnly));
        Assert.That(operation.GetLimits(), Is.SameAs(limits));
    }
}
