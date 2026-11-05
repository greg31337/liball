namespace barcode.Tests;

public class RenderDecodeRoundTripTests
{
    [TestCase(BarcodeRenderFormat.Png)]
    [TestCase(BarcodeRenderFormat.Bmp)]
    [TestCase(BarcodeRenderFormat.Svg)]
    public void IndependentParserReconstructsPublicRenderedPixels(
        BarcodeRenderFormat format)
    {
        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            CreateEncodeRequest(BarcodeFormat.DataMatrix));
        BarcodeRenderedImage expected = Render(
            symbol,
            BarcodeRenderFormat.Gray8);
        BarcodeRenderedImage serialized = Render(symbol, format);

        BarcodeImage parsed = IndependentRenderParser.Parse(serialized);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Width, Is.EqualTo(serialized.WidthPixels));
            Assert.That(parsed.Height, Is.EqualTo(serialized.HeightPixels));
            Assert.That(parsed.Stride, Is.EqualTo(parsed.Width));
            Assert.That(parsed.PixelFormat,
                Is.EqualTo(BarcodePixelFormat.Gray8));
            Assert.That(parsed.Pixels, Is.EqualTo(expected.Bytes));
        });
    }

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
    public void EverySymbologyRoundTripsThroughSerializedRenderings(
        BarcodeFormat format)
    {
        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            CreateEncodeRequest(format));
        BarcodeDecodeResult expected = new BarcodeDecoder().DecodeBarcode(
            CreatePureDecodeRequest(symbol, format))[0];
        BarcodeRenderFormat[] renderFormats = new BarcodeRenderFormat[]
        {
            BarcodeRenderFormat.Png,
            BarcodeRenderFormat.Bmp,
            BarcodeRenderFormat.Svg
        };

        int index = 0;
        while (index < renderFormats.Length)
        {
            BarcodeRenderFormat renderFormat = renderFormats[index];
            BarcodeRenderedImage rendered = Render(symbol, renderFormat);
            BarcodeImage parsed = IndependentRenderParser.Parse(rendered);
            BarcodeDecodeResult[] decoded = new BarcodeDecoder().DecodeBarcode(
                CreateDecodeRequest(parsed, format));

            Assert.That(
                decoded,
                Has.Length.EqualTo(1),
                renderFormat + " " + format);
            Assert.Multiple(() =>
            {
                Assert.That(
                    decoded[0].Format,
                    Is.EqualTo(format),
                    renderFormat + " " + format);
                Assert.That(
                    decoded[0].Payload?.Text,
                    Is.EqualTo(expected.Payload?.Text),
                    renderFormat + " " + format);
            });
            index++;
        }
    }

    static BarcodeRenderedImage Render(
        BarcodeSymbol symbol,
        BarcodeRenderFormat format)
    {
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Format = format;
        options.ModuleScale = 4;
        options.QuietZonePolicy = BarcodeQuietZonePolicy.Standard;
        options.TextMode = BarcodeTextMode.Hidden;
        if (symbol.Dimension == BarcodeDimension.Linear)
        {
            BarcodeSizing sizing = new BarcodeSizing();
            sizing.RequestedHeightPixels = 96;
            options.Sizing = sizing;
        }

        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return new BarcodeRenderer().RenderBarcode(request);
    }

    static BarcodeDecodeRequest CreateDecodeRequest(
        BarcodeImage image,
        BarcodeFormat format)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { format };
        options.MaximumResults = 1;
        options.ExpectedResultCount = 1;
        options.Polarity = BarcodePolarity.DarkOnLight;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.Effort = BarcodeEffort.Balanced;
        options.BinarizationMode = BarcodeBinarizationMode.Otsu;
        options.LocalizationMode = BarcodeLocalizationMode.Automatic;
        options.MaximumDiagnostics = 30;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Image = image;
        request.Options = options;
        return request;
    }

    static BarcodeDecodeRequest CreatePureDecodeRequest(
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { format };
        options.PureSymbol = true;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
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
        options.TextMode = BarcodeTextMode.Hidden;
        request.Options = options;
        if (format == BarcodeFormat.CompactPdf417)
        {
            Pdf417Options pdf417 = new Pdf417Options();
            pdf417.Variant = Pdf417Variant.Compact;
            pdf417.ErrorCorrectionLevel = 2;
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
            pdf417.ErrorCorrectionLevel = 2;
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
                return "ABC39";
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
}
