namespace barcode.Tests;

public class MixedPageEndToEndTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void PublicPipelineDecodesEveryFormatFromOneMixedPage(
        bool degraded)
    {
        BarcodeSymbol[] symbols = CreateSymbols();
        BarcodeImage page = ComposePage(symbols);
        if (degraded)
        {
            page = ApplyControlledDegradation(page);
        }

        BarcodeDecodeResult[] results = new BarcodeDecoder().DecodeBarcode(
            CreateDecodeRequest(page));

        Assert.That(results, Has.Length.EqualTo(symbols.Length));
        Assert.Multiple(() =>
        {
            foreach (BarcodeSymbol symbol in symbols)
            {
                BarcodeDecodeResult? result = FindResult(results, symbol.Format);
                Assert.That(result, Is.Not.Null,
                    (degraded ? "degraded " : "clean ") + symbol.Format);
                Assert.That(result?.Payload?.Text,
                    Is.EqualTo(ExpectedText(symbol.Format)),
                    (degraded ? "degraded " : "clean ") + symbol.Format);
            }
        });
    }

    static BarcodeSymbol[] CreateSymbols()
    {
        BarcodeFormat[] formats = new BarcodeFormat[]
        {
            BarcodeFormat.Code128,
            BarcodeFormat.Gs1128,
            BarcodeFormat.Ean8,
            BarcodeFormat.Ean13,
            BarcodeFormat.UpcA,
            BarcodeFormat.UpcE,
            BarcodeFormat.EanUpcAddon2,
            BarcodeFormat.EanUpcAddon5,
            BarcodeFormat.Code39,
            BarcodeFormat.Itf,
            BarcodeFormat.Itf14,
            BarcodeFormat.Codabar,
            BarcodeFormat.DataMatrix,
            BarcodeFormat.Gs1DataMatrix,
            BarcodeFormat.Pdf417,
            BarcodeFormat.CompactPdf417,
            BarcodeFormat.MacroPdf417
        };
        BarcodeSymbol[] symbols = new BarcodeSymbol[formats.Length];
        int index = 0;
        while (index < formats.Length)
        {
            symbols[index] = Encode(formats[index]);
            index++;
        }

        return symbols;
    }

    static BarcodeSymbol Encode(BarcodeFormat format)
    {
        BarcodePayload payload = BarcodeTestBuilder.CreateAsciiPayload(
            InputText(format));
        payload.IsGs1 = format == BarcodeFormat.Gs1128
            || format == BarcodeFormat.Gs1DataMatrix
            || format == BarcodeFormat.Itf14;
        BarcodeEncodeRequest request =
            BarcodeTestBuilder.CreateEncodeRequest(format, payload);
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.TextMode = BarcodeTextMode.Hidden;
        request.Options = common;
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
                    2,
                    "149001");
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

        return new BarcodeEncoder().EncodeBarcode(request);
    }

    static BarcodeImage ComposePage(BarcodeSymbol[] symbols)
    {
        const int columnCount = 3;
        const int cellWidth = 500;
        const int cellHeight = 180;
        int rowCount = (symbols.Length + columnCount - 1) / columnCount;
        BarcodeImage page = BarcodeTestBuilder.CreateGrayImage(
            columnCount * cellWidth,
            rowCount * cellHeight,
            255);
        int index = 0;
        while (index < symbols.Length)
        {
            BarcodeRenderedImage rendered = Render(symbols[index]);
            int column = index % columnCount;
            int row = index / columnCount;
            int left = (column * cellWidth) +
                ((cellWidth - rendered.WidthPixels) / 2);
            int top = (row * cellHeight) + 10 + (column * 30);
            CopyImage(rendered, page, left, top);
            index++;
        }

        return page;
    }

    static BarcodeRenderedImage Render(BarcodeSymbol symbol)
    {
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Format = BarcodeRenderFormat.Gray8;
        options.ModuleScale = symbol.Dimension == BarcodeDimension.Matrix
            ? 5
            : 3;
        options.QuietZonePolicy = BarcodeQuietZonePolicy.Standard;
        options.TextMode = BarcodeTextMode.Hidden;
        if (symbol.Dimension == BarcodeDimension.Linear)
        {
            BarcodeSizing sizing = new BarcodeSizing();
            sizing.RequestedHeightPixels = 88;
            options.Sizing = sizing;
        }

        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return new BarcodeRenderer().RenderBarcode(request);
    }

    static void CopyImage(
        BarcodeRenderedImage source,
        BarcodeImage target,
        int left,
        int top)
    {
        int row = 0;
        while (row < source.HeightPixels)
        {
            global::System.Array.Copy(
                source.Bytes!,
                row * source.Stride,
                target.Pixels!,
                ((top + row) * target.Stride) + left,
                source.WidthPixels);
            row++;
        }
    }

    static BarcodeImage ApplyControlledDegradation(BarcodeImage source)
    {
        BarcodeImage degraded = BarcodeTestBuilder.CopyImage(source);
        byte[] pixels = degraded.Pixels!;
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = (byte)(48 + ((pixels[index] * 167) / 255));
            index++;
        }

        return degraded;
    }

    static BarcodeDecodeRequest CreateDecodeRequest(BarcodeImage image)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.Code128,
            BarcodeFormat.Gs1128,
            BarcodeFormat.Ean8,
            BarcodeFormat.Ean13,
            BarcodeFormat.UpcA,
            BarcodeFormat.UpcE,
            BarcodeFormat.EanUpcAddon2,
            BarcodeFormat.EanUpcAddon5,
            BarcodeFormat.Code39,
            BarcodeFormat.Itf,
            BarcodeFormat.Itf14,
            BarcodeFormat.Codabar,
            BarcodeFormat.DataMatrix,
            BarcodeFormat.Gs1DataMatrix,
            BarcodeFormat.Pdf417,
            BarcodeFormat.CompactPdf417,
            BarcodeFormat.MacroPdf417
        };
        options.MaximumResults = 64;
        options.ExpectedResultCount = 17;
        options.ParseGs1 = true;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.Effort = BarcodeEffort.Fast;
        options.BinarizationMode = BarcodeBinarizationMode.Hybrid;
        options.LocalizationMode = BarcodeLocalizationMode.Automatic;
        options.MaximumDiagnostics = 128;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Image = image;
        request.Options = options;
        return request;
    }

    static BarcodeDecodeResult? FindResult(
        BarcodeDecodeResult[] results,
        BarcodeFormat format)
    {
        int index = 0;
        while (index < results.Length)
        {
            if (results[index].Format == format)
            {
                return results[index];
            }

            index++;
        }

        return null;
    }

    static string InputText(BarcodeFormat format)
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
                return "M39";
            case BarcodeFormat.Itf:
                return "123456";
            case BarcodeFormat.Itf14:
                return "0950110153001";
            case BarcodeFormat.Codabar:
                return "5678";
            case BarcodeFormat.DataMatrix:
                return "MDM";
            case BarcodeFormat.Pdf417:
                return "MPDF";
            case BarcodeFormat.CompactPdf417:
                return "MCPDF";
            case BarcodeFormat.MacroPdf417:
                return "MMACRO";
            default:
                return "M128";
        }
    }

    static string ExpectedText(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Ean8:
                return "55123457";
            case BarcodeFormat.Ean13:
                return "4006381333931";
            case BarcodeFormat.UpcA:
                return "036000291452";
            case BarcodeFormat.UpcE:
                return "01234558";
            case BarcodeFormat.Itf14:
                return "09501101530010";
            default:
                return InputText(format);
        }
    }
}
