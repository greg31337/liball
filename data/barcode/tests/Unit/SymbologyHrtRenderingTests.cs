namespace barcode.Tests;

public class SymbologyHrtRenderingTests
{
    [Test]
    public void EanAndUpcDigitGroupsKeepTheirStandardRenderPositions()
    {
        EanUpcEncodedData encoded = EanUpcEncoder.Encode(
            "4006381333931",
            BarcodeFormat.Ean13,
            BarcodeChecksumMode.Require);
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        BarcodeSymbol symbol = EanUpcLayout.CreateSymbol(
            CreateAssociation(encoded),
            CreatePayload(encoded.Text),
            encodeOptions,
            null);
        BarcodeRenderOptions renderOptions = CreateRenderOptions();

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, renderOptions));
        HumanReadableTextRenderResult text =
            HumanReadableTextRenderer.Render(plan);

        Assert.Multiple(() =>
        {
            Assert.That(ReadTexts(plan.TextLayout!.Runs!),
                Is.EqualTo(new string[] { "4", "006381", "333931" }));
            Assert.That(ReadX(plan.TextLayout.Runs!),
                Is.EqualTo(new double[] { 4.0, 14.0, 61.0 }));
            Assert.That(text.TextRunCount, Is.EqualTo(3));
            Assert.That(text.GlyphCount, Is.EqualTo(13));
            Assert.That(plan.TextTopModules, Is.Zero);
            Assert.That(plan.TextBottomModules, Is.EqualTo(7));
        });
    }

    [Test]
    public void AddOnTextUsesTopBandAndGuardBarsDescendIntoBottomBand()
    {
        EanUpcEncodedData encoded = EanUpcEncoder.Encode(
            "4006381333931",
            BarcodeFormat.Ean13,
            BarcodeChecksumMode.Require);
        EanUpcOptions eanOptions = new EanUpcOptions();
        eanOptions.AddOn = "12";
        eanOptions.ExtendGuardBars = true;
        eanOptions.ShowQuietZoneIndicators = true;
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        EanUpcLayoutResult layout = EanUpcLayout.CreateLayout(
            EanUpcAddon.Associate(encoded, eanOptions),
            CreatePayload(encoded.Text),
            encodeOptions,
            eanOptions);
        BarcodeRenderOptions renderOptions = CreateRenderOptions();

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(layout.Symbol, renderOptions));
        HumanReadableTextRenderResult text =
            HumanReadableTextRenderer.Render(plan);
        RasterCanvas canvas = RasterCanvas.CreateGray8(
            plan,
            renderOptions);
        BarcodeRenderOptions nativeSvgOptions = CreateRenderOptions();
        nativeSvgOptions.UseSvgText = true;
        SvgWriteResult nativeSvg = SvgWriter.Write(
            plan,
            nativeSvgOptions);
        global::System.Xml.Linq.XDocument document =
            global::System.Xml.Linq.XDocument.Parse(nativeSvg.Text);
        global::System.Xml.Linq.XNamespace svg =
            "http://www.w3.org/2000/svg";

        Assert.Multiple(() =>
        {
            Assert.That(plan.TextTopModules, Is.EqualTo(7));
            Assert.That(plan.TextBottomModules, Is.EqualTo(7));
            Assert.That(plan.ContentYModules, Is.EqualTo(7));
            Assert.That(plan.ContentYPixels, Is.EqualTo(14));
            Assert.That(plan.Size.HeightPixels, Is.EqualTo(30));
            Assert.That(plan.TextLayout!.Runs![3].Text, Is.EqualTo("12"));
            Assert.That(plan.TextLayout.Runs[3].BaselineY,
                Is.EqualTo(7.0));
            Assert.That(plan.TextLayout.Runs[0].BaselineY,
                Is.EqualTo(15.0));
            Assert.That(plan.TextLayout.BarExtensions,
                Has.Length.EqualTo(6));
            Assert.That(text.BarExtensionRectangleCount, Is.EqualTo(6));
            Assert.That(text.GlyphCount, Is.EqualTo(16));
            Assert.That(ReadGrayPixel(canvas, 22, 25), Is.EqualTo(0));
            Assert.That(ReadGrayPixel(canvas, 22, 26), Is.EqualTo(255));
            Assert.That(document.Descendants(svg + "path").Count(),
                Is.EqualTo(2));
            Assert.That(document.Descendants(svg + "text").Count(),
                Is.EqualTo(5));
        });
    }

    [Test]
    public void Gs1ApplicationIdentifiersRemainParenthesizedForRendering()
    {
        Gs1Element first = new Gs1Element();
        first.ApplicationIdentifier = "01";
        first.Value = "09506000134352";
        Gs1Element second = new Gs1Element();
        second.ApplicationIdentifier = "17";
        second.Value = "260101";
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        Gs1128EncodedData encoded = Gs1128Codec.EncodeElements(
            new Gs1Element[] { first, second },
            null,
            encodeOptions);

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(encoded.Symbol, CreateRenderOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Symbol.Payload!.Text,
                Is.EqualTo("010950600013435217260101"));
            Assert.That(plan.TextLayout!.Runs![0].Text,
                Is.EqualTo("(01)09506000134352(17)260101"));
            Assert.That(HumanReadableTextRenderer.Render(plan).GlyphCount,
                Is.EqualTo(28));
        });
    }

    [Test]
    public void CheckAndTerminalTransmissionPoliciesReachRenderedHrt()
    {
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        Code39Options code39Options = new Code39Options();
        code39Options.ChecksumMode = BarcodeChecksumMode.Calculate;
        code39Options.TransmitCheckCharacter = true;
        Code39LayoutResult code39 = Code39Layout.CreateLayout(
            "CODE39",
            CreatePayload("CODE39"),
            code39Options,
            encodeOptions);
        CodabarOptions codabarOptions = new CodabarOptions();
        codabarOptions.ChecksumMode = BarcodeChecksumMode.Calculate;
        codabarOptions.TransmitCheckCharacter = true;
        codabarOptions.TransmitStartStop = true;
        CodabarLayoutResult codabar = CodabarLayout.CreateLayout(
            "A1234B",
            CreatePayload("A1234B"),
            codabarOptions,
            encodeOptions);

        RenderPlan code39Plan = RenderPlanner.CreatePlan(
            CreateRequest(code39.Symbol, CreateRenderOptions()));
        RenderPlan codabarPlan = RenderPlanner.CreatePlan(
            CreateRequest(codabar.Symbol, CreateRenderOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(code39Plan.TextLayout!.Runs![0].Text,
                Is.EqualTo("CODE39W"));
            Assert.That(codabarPlan.TextLayout!.Runs![0].Text,
                Is.EqualTo("A12345B"));
            Assert.That(HumanReadableTextRenderer.Render(code39Plan)
                .GlyphCount, Is.EqualTo(7));
            Assert.That(HumanReadableTextRenderer.Render(codabarPlan)
                .GlyphCount, Is.EqualTo(7));
        });
    }

    static BarcodeRenderOptions CreateRenderOptions()
    {
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.TextMode = BarcodeTextMode.Standard;
        return options;
    }

    static BarcodeRenderRequest CreateRequest(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options)
    {
        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }

    static EanUpcAddonAssociation CreateAssociation(
        EanUpcEncodedData encoded)
    {
        EanUpcAddonAssociation association =
            new EanUpcAddonAssociation();
        association.Main = encoded;
        return association;
    }

    static BarcodePayload CreatePayload(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        return payload;
    }

    static string[] ReadTexts(BarcodeTextRun[] runs)
    {
        string[] values = new string[runs.Length];
        int index = 0;
        while (index < runs.Length)
        {
            string? text = runs[index].Text;
            values[index] = text == null
                ? ""
                : text;
            index = index + 1;
        }

        return values;
    }

    static double[] ReadX(BarcodeTextRun[] runs)
    {
        double[] values = new double[runs.Length];
        int index = 0;
        while (index < runs.Length)
        {
            values[index] = runs[index].X;
            index = index + 1;
        }

        return values;
    }

    static byte ReadGrayPixel(
        RasterCanvas canvas,
        int x,
        int y)
    {
        return canvas.Pixels[(y * canvas.Stride) + x];
    }
}
