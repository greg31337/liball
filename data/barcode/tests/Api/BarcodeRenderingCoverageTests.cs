namespace barcode.Tests;

public class BarcodeRenderingCoverageTests
{
    [TestCase(BarcodeFormat.Code128)]
    [TestCase(BarcodeFormat.Gs1128)]
    [TestCase(BarcodeFormat.Ean8)]
    [TestCase(BarcodeFormat.Ean13)]
    [TestCase(BarcodeFormat.UpcA)]
    [TestCase(BarcodeFormat.UpcE)]
    [TestCase(BarcodeFormat.EanUpcAddon2)]
    [TestCase(BarcodeFormat.EanUpcAddon5)]
    [TestCase(BarcodeFormat.Code39)]
    [TestCase(BarcodeFormat.Itf)]
    [TestCase(BarcodeFormat.Itf14)]
    [TestCase(BarcodeFormat.Codabar)]
    [TestCase(BarcodeFormat.DataMatrix)]
    [TestCase(BarcodeFormat.Gs1DataMatrix)]
    [TestCase(BarcodeFormat.Pdf417)]
    [TestCase(BarcodeFormat.CompactPdf417)]
    [TestCase(BarcodeFormat.MacroPdf417)]
    public void EverySupportedSymbologyHasMatchingRasterAndSvgLayout(
        BarcodeFormat format)
    {
        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            CreateEncodeRequest(format));
        BarcodeRenderedImage raster = Render(
            symbol,
            BarcodeRenderFormat.Gray8,
            BarcodeRotation.None,
            BarcodeQuietZonePolicy.Standard,
            BarcodeTextMode.Standard,
            false);
        BarcodeRenderedImage svg = Render(
            symbol,
            BarcodeRenderFormat.Svg,
            BarcodeRotation.None,
            BarcodeQuietZonePolicy.Standard,
            BarcodeTextMode.Standard,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(format));
            Assert.That(raster.WidthPixels, Is.EqualTo(svg.WidthPixels));
            Assert.That(raster.HeightPixels, Is.EqualTo(svg.HeightPixels));
            Assert.That(raster.Size?.WidthModules,
                Is.EqualTo(svg.Size?.WidthModules));
            Assert.That(raster.Size?.HeightModules,
                Is.EqualTo(svg.Size?.HeightModules));
            Assert.That(raster.Size?.QuietZone?.LeftPixels,
                Is.EqualTo(svg.Size?.QuietZone?.LeftPixels));
            Assert.That(raster.Bytes,
                Has.Length.EqualTo(raster.WidthPixels * raster.HeightPixels));
            Assert.That(raster.Bytes, Does.Contain((byte)143));
            Assert.That(raster.Bytes, Does.Contain((byte)241));
            Assert.That(svg.Text,
                Does.Contain(
                    $"viewBox=\"0 0 {svg.WidthPixels} {svg.HeightPixels}\""));
            Assert.That(svg.Text, Does.Contain("<path"));
            Assert.That(raster.TextLayout, Is.Not.Null);
            Assert.That(svg.TextLayout, Is.Not.Null);
        });
    }

    [TestCase(BarcodeQuietZonePolicy.Standard)]
    [TestCase(BarcodeQuietZonePolicy.CustomAtLeastStandard)]
    [TestCase(BarcodeQuietZonePolicy.ExactCustom)]
    public void EveryQuietZonePolicyMatchesBetweenRasterAndSvg(
        BarcodeQuietZonePolicy policy)
    {
        BarcodeSymbol symbol = CreateMatrixSymbol();
        BarcodeRenderedImage raster = Render(
            symbol,
            BarcodeRenderFormat.Gray8,
            BarcodeRotation.None,
            policy,
            BarcodeTextMode.Hidden,
            false);
        BarcodeRenderedImage svg = Render(
            symbol,
            BarcodeRenderFormat.Svg,
            BarcodeRotation.None,
            policy,
            BarcodeTextMode.Hidden,
            false);
        int expectedLeft = policy == BarcodeQuietZonePolicy.Standard
            ? 2
            : 6;

        Assert.Multiple(() =>
        {
            Assert.That(raster.Size?.QuietZone?.LeftPixels,
                Is.EqualTo(expectedLeft));
            Assert.That(raster.Size?.QuietZone?.TopPixels,
                Is.EqualTo(policy == BarcodeQuietZonePolicy.Standard
                    ? 2
                    : 8));
            Assert.That(raster.Size?.QuietZone?.LeftPixels,
                Is.EqualTo(svg.Size?.QuietZone?.LeftPixels));
            Assert.That(raster.WidthPixels, Is.EqualTo(svg.WidthPixels));
            Assert.That(raster.HeightPixels, Is.EqualTo(svg.HeightPixels));
        });
    }

    [TestCase(BarcodeRotation.None, BarcodeTextMode.Hidden, false)]
    [TestCase(BarcodeRotation.Clockwise90, BarcodeTextMode.Standard, false)]
    [TestCase(BarcodeRotation.Clockwise180, BarcodeTextMode.RawPayload, true)]
    [TestCase(BarcodeRotation.Clockwise270, BarcodeTextMode.Custom, true)]
    public void RotationsColorsAlphaAndHrtModesStayAlignedAcrossOutputs(
        BarcodeRotation rotation,
        BarcodeTextMode textMode,
        bool transparentBackground)
    {
        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            CreateEncodeRequest(BarcodeFormat.Code128));
        BarcodeRenderedImage raster = Render(
            symbol,
            BarcodeRenderFormat.Gray8,
            rotation,
            BarcodeQuietZonePolicy.Standard,
            textMode,
            transparentBackground);
        BarcodeRenderedImage svg = Render(
            symbol,
            BarcodeRenderFormat.Svg,
            rotation,
            BarcodeQuietZonePolicy.Standard,
            textMode,
            transparentBackground);

        Assert.Multiple(() =>
        {
            Assert.That(raster.WidthPixels, Is.EqualTo(svg.WidthPixels));
            Assert.That(raster.HeightPixels, Is.EqualTo(svg.HeightPixels));
            Assert.That(raster.TextLayout == null,
                Is.EqualTo(textMode == BarcodeTextMode.Hidden));
            Assert.That(svg.TextLayout == null,
                Is.EqualTo(textMode == BarcodeTextMode.Hidden));
            Assert.That(svg.Text, Does.Contain("fill=\"#123456\""));
            Assert.That(svg.Text, Does.Contain("fill-opacity=\"0.501961\""));
            Assert.That(svg.Text!.Contains("fill=\"#ABCDEF\""),
                Is.EqualTo(!transparentBackground));
            Assert.That(raster.Bytes, Does.Contain(
                transparentBackground ? (byte)150 : (byte)143));
            Assert.That(raster.Bytes, Does.Contain(
                transparentBackground ? (byte)255 : (byte)241));
        });

        if (textMode != BarcodeTextMode.Hidden)
        {
            string expected = textMode == BarcodeTextMode.Custom
                ? "CUSTOM"
                : "ABC123";
            Assert.That(ReadText(raster.TextLayout), Does.Contain(expected));
            Assert.That(ReadText(svg.TextLayout), Does.Contain(expected));
        }
    }

    [Test]
    public void PixelDpiPhysicalAndExplicitScaleSizingMatchAcrossOutputs()
    {
        BarcodeSymbol symbol = CreateMatrixSymbol();
        BarcodeRenderOptions[] options = new BarcodeRenderOptions[]
        {
            CreateScaleOptions(),
            CreatePixelOptions(),
            CreateDpiOptions(),
            CreatePhysicalOptions()
        };

        int index = 0;
        while (index < options.Length)
        {
            BarcodeRenderedImage raster = RenderWithOptions(
                symbol,
                BarcodeRenderFormat.Gray8,
                options[index]);
            BarcodeRenderedImage svg = RenderWithOptions(
                symbol,
                BarcodeRenderFormat.Svg,
                options[index]);
            Assert.Multiple(() =>
            {
                Assert.That(raster.WidthPixels,
                    Is.EqualTo(svg.WidthPixels),
                    $"sizing case {index}");
                Assert.That(raster.HeightPixels,
                    Is.EqualTo(svg.HeightPixels),
                    $"sizing case {index}");
                Assert.That(raster.WidthMillimetres,
                    Is.EqualTo(svg.WidthMillimetres).Within(0.0000001),
                    $"sizing case {index}");
                Assert.That(raster.HeightMillimetres,
                    Is.EqualTo(svg.HeightMillimetres).Within(0.0000001),
                    $"sizing case {index}");
                Assert.That(raster.Size?.ModuleSizeMillimetres,
                    Is.EqualTo(svg.Size?.ModuleSizeMillimetres)
                        .Within(0.0000001),
                    $"sizing case {index}");
            });
            index++;
        }
    }

    static BarcodeEncodeRequest CreateEncodeRequest(BarcodeFormat format)
    {
        string text = DataForFormat(format);
        BarcodePayload payload = BarcodeTestBuilder.CreateAsciiPayload(text);
        payload.IsGs1 = format == BarcodeFormat.Gs1128
            || format == BarcodeFormat.Gs1DataMatrix
            || format == BarcodeFormat.Itf14;
        BarcodeEncodeRequest request =
            BarcodeTestBuilder.CreateEncodeRequest(format, payload);
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.TextMode = BarcodeTextMode.Standard;
        request.Options = options;
        if (format == BarcodeFormat.CompactPdf417)
        {
            Pdf417Options pdf417 = new Pdf417Options();
            pdf417.Variant = Pdf417Variant.Compact;
            pdf417.ErrorCorrectionLevel = -1;
            request.Pdf417 = pdf417;
        }
        else if (format == BarcodeFormat.MacroPdf417)
        {
            StructuredAppendInfo macro =
                BarcodeTestBuilder.CreateStructuredAppend(
                    BarcodeFormat.MacroPdf417,
                    0,
                    1,
                    "017053");
            Pdf417Options pdf417 = new Pdf417Options();
            pdf417.ErrorCorrectionLevel = -1;
            pdf417.Macro = macro;
            request.Pdf417 = pdf417;
        }
        else if (format == BarcodeFormat.Itf14)
        {
            ItfOptions itf = new ItfOptions();
            itf.WideToNarrowRatio = 3.0;
            request.Itf = itf;
        }

        return request;
    }

    static string DataForFormat(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Gs1128:
            case BarcodeFormat.Gs1DataMatrix:
                return "0109506000134352";
            case BarcodeFormat.Ean8:
                return "5512345";
            case BarcodeFormat.Ean13:
                return "400638133393";
            case BarcodeFormat.UpcA:
                return "03600029145";
            case BarcodeFormat.UpcE:
                return "123455";
            case BarcodeFormat.EanUpcAddon2:
                return "12";
            case BarcodeFormat.EanUpcAddon5:
                return "51234";
            case BarcodeFormat.Code39:
                return "ABC-39";
            case BarcodeFormat.Itf:
                return "123456";
            case BarcodeFormat.Itf14:
                return "0950110153001";
            case BarcodeFormat.Codabar:
                return "1234";
            case BarcodeFormat.DataMatrix:
                return "DATA";
            case BarcodeFormat.Pdf417:
                return "PDF417";
            case BarcodeFormat.CompactPdf417:
                return "COMPACT";
            case BarcodeFormat.MacroPdf417:
                return "MACRO";
            default:
                return "ABC123";
        }
    }

    static BarcodeRenderedImage Render(
        BarcodeSymbol symbol,
        BarcodeRenderFormat format,
        BarcodeRotation rotation,
        BarcodeQuietZonePolicy quietZonePolicy,
        BarcodeTextMode textMode,
        bool transparentBackground)
    {
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Format = format;
        options.ModuleScale = 2;
        options.Rotation = rotation;
        options.QuietZonePolicy = quietZonePolicy;
        options.QuietZone = CreateCustomQuietZone();
        options.TextMode = textMode;
        options.CustomText = "CUSTOM";
        options.ForegroundRgba = 0x12345680u;
        options.BackgroundRgba = 0xABCDEF40u;
        options.TransparentBackground = transparentBackground;
        return RenderWithOptions(symbol, format, options);
    }

    static BarcodeRenderedImage RenderWithOptions(
        BarcodeSymbol symbol,
        BarcodeRenderFormat format,
        BarcodeRenderOptions options)
    {
        options.Format = format;
        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return new BarcodeRenderer().RenderBarcode(request);
    }

    static BarcodeSymbol CreateMatrixSymbol()
    {
        ModuleMatrix modules = new ModuleMatrix(3, 2);
        modules.SetModule(0, 0, true);
        modules.SetModule(2, 0, true);
        modules.SetModule(1, 1, true);
        BarcodeSymbol symbol = BarcodeTestBuilder.CreateSymbol(
            BarcodeFormat.DataMatrix,
            BarcodeDimension.Matrix,
            modules,
            BarcodeTestBuilder.CreateAsciiPayload("ABC123"));
        BarcodeQuietZone required = new BarcodeQuietZone();
        required.LeftModules = 1;
        required.TopModules = 1;
        required.RightModules = 1;
        required.BottomModules = 1;
        symbol.RequiredQuietZone = required;
        return symbol;
    }

    static BarcodeQuietZone CreateCustomQuietZone()
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = 3;
        quietZone.TopModules = 4;
        quietZone.RightModules = 5;
        quietZone.BottomModules = 6;
        return quietZone;
    }

    static BarcodeRenderOptions CreateScaleOptions()
    {
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 3;
        return options;
    }

    static BarcodeRenderOptions CreatePixelOptions()
    {
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthPixels = 15;
        sizing.RequestedHeightPixels = 12;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;
        return options;
    }

    static BarcodeRenderOptions CreateDpiOptions()
    {
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.DotsPerInch = 100.0;
        sizing.ModuleSizeMillimetres = 0.254;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;
        return options;
    }

    static BarcodeRenderOptions CreatePhysicalOptions()
    {
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthMillimetres = 10.0;
        sizing.RequestedHeightMillimetres = 8.0;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;
        return options;
    }

    static string ReadText(BarcodeTextLayout? layout)
    {
        if (layout == null || layout.Runs == null)
        {
            return "";
        }

        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        int index = 0;
        while (index < layout.Runs.Length)
        {
            text.Append(layout.Runs[index].Text);
            index++;
        }

        return text.ToString();
    }
}
