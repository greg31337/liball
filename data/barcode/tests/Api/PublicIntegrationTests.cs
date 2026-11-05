namespace barcode.Tests;

public class PublicIntegrationTests
{
    [Test]
    public void EncoderDispatchesEverySupportedFormatToItsCodec()
    {
        IBarcodeEncoder encoder = new BarcodeEncoder();
        BarcodeEncodeRequest[] requests = new BarcodeEncodeRequest[]
        {
            CreateRequest(BarcodeFormat.Code128, "ABC"),
            CreateGs1Request(BarcodeFormat.Gs1128),
            CreateRequest(BarcodeFormat.Ean8, "1234567"),
            CreateRequest(BarcodeFormat.Ean13, "123456789012"),
            CreateRequest(BarcodeFormat.UpcA, "01234567890"),
            CreateRequest(BarcodeFormat.UpcE, "123450"),
            CreateRequest(BarcodeFormat.EanUpcAddon2, "12"),
            CreateRequest(BarcodeFormat.EanUpcAddon5, "51234"),
            CreateRequest(BarcodeFormat.Code39, "CODE39"),
            CreateRequest(BarcodeFormat.Itf, "123456"),
            CreateRequest(BarcodeFormat.Itf14, "0950110153001"),
            CreateRequest(BarcodeFormat.Codabar, "1234"),
            CreateRequest(BarcodeFormat.DataMatrix, "DATA"),
            CreateGs1Request(BarcodeFormat.Gs1DataMatrix),
            CreateRequest(BarcodeFormat.Pdf417, "PDF417"),
            CreateCompactPdf417Request(),
            CreateMacroPdf417Request()
        };

        int index = 0;
        while (index < requests.Length)
        {
            BarcodeSymbol symbol = encoder.EncodeBarcode(requests[index]);
            BarcodeSymbol? attempted = encoder.TryEncodeBarcode(requests[index]);
            Assert.Multiple(() =>
            {
                Assert.That(symbol.Format, Is.EqualTo(requests[index].Format));
                Assert.That(symbol.WidthModules, Is.GreaterThan(0));
                Assert.That(symbol.HeightModules, Is.GreaterThan(0));
                Assert.That(symbol.Payload, Is.Not.SameAs(requests[index].Payload));
                Assert.That(attempted, Is.Not.Null);
                Assert.That(attempted, Is.Not.SameAs(symbol));
                if (symbol.Modules != null)
                {
                    Assert.That(attempted!.Modules, Is.Not.SameAs(symbol.Modules));
                }
                else
                {
                    Assert.That(attempted!.Codewords, Is.Not.SameAs(symbol.Codewords));
                }
            });
            index++;
        }
    }

    [Test]
    public void EncoderSelectsAutomaticFormatsAndDoesNotExposeCallerBuffers()
    {
        IBarcodeEncoder encoder = new BarcodeEncoder();
        BarcodeEncodeRequest textRequest = CreateRequest(
            BarcodeFormat.Automatic,
            "AUTO");
        BarcodeEncodeRequest byteRequest = CreateRequest(
            BarcodeFormat.Automatic,
            null);
        byteRequest.Payload!.RawBytes = new byte[] { 1, 2, 3 };

        BarcodeSymbol text = encoder.EncodeBarcode(textRequest);
        BarcodeSymbol bytes = encoder.EncodeBarcode(byteRequest);
        byteRequest.Payload.RawBytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(text.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(bytes.Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(bytes.Payload!.RawBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(bytes.Payload.RawBytes, Is.Not.SameAs(byteRequest.Payload.RawBytes));
        });
    }

    [Test]
    public void PublicServicesReturnIndependentlyOwnedResultGraphs()
    {
        IBarcodeValidator validator = new BarcodeValidator();
        BarcodeValidationRequest invalid = new BarcodeValidationRequest();
        BarcodeValidationReport firstReport = validator.ValidateBarcodeData(invalid);
        BarcodeValidationReport secondReport = validator.ValidateBarcodeData(invalid);
        firstReport.Issues![0].Code = "changed";

        IGs1DataProcessor processor = new Gs1DataProcessor();
        Gs1ProcessRequest gs1Request = new Gs1ProcessRequest();
        gs1Request.SourceSyntax = Gs1Syntax.Bracketed;
        gs1Request.TargetSyntax = Gs1Syntax.ElementString;
        gs1Request.SourceText = "(01)09506000134352";
        Gs1ProcessResult firstGs1 = processor.ProcessGs1Data(gs1Request);
        Gs1ProcessResult secondGs1 = processor.ProcessGs1Data(gs1Request);
        firstGs1.Message!.Elements![0].Value = "changed";

        Gs1ApplicationIdentifierInfo firstInfo =
            processor.LookupGs1ApplicationIdentifier("01")!;
        Gs1ApplicationIdentifierInfo secondInfo =
            processor.LookupGs1ApplicationIdentifier("01")!;
        firstInfo.Qualifiers![0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(secondReport.Issues![0].Code, Is.EqualTo("ENCODE_REQUEST_MISSING"));
            Assert.That(firstReport.Issues, Is.Not.SameAs(secondReport.Issues));
            Assert.That(secondGs1.Message!.Elements![0].Value, Is.EqualTo("09506000134352"));
            Assert.That(firstGs1.Message.Elements, Is.Not.SameAs(secondGs1.Message.Elements));
            Assert.That(secondInfo.Qualifiers, Does.Not.Contain("changed"));
            Assert.That(firstInfo.Qualifiers, Is.Not.SameAs(secondInfo.Qualifiers));
        });
    }

    [Test]
    public void RenderingAndDetectionResultsOwnTheirMutableBuffers()
    {
        IBarcodeEncoder encoder = new BarcodeEncoder();
        BarcodeSymbol symbol = encoder.EncodeBarcode(
            CreateRequest(BarcodeFormat.Code128, "COPY"));
        BarcodeRenderOptions renderOptions = new BarcodeRenderOptions();
        renderOptions.Format = BarcodeRenderFormat.Gray8;
        BarcodeRenderRequest renderRequest = new BarcodeRenderRequest();
        renderRequest.Symbol = symbol;
        renderRequest.Options = renderOptions;
        IBarcodeRenderer renderer = new BarcodeRenderer();
        BarcodeRenderedImage firstRender = renderer.RenderBarcode(renderRequest);
        BarcodeRenderedImage secondRender = renderer.RenderBarcode(renderRequest);
        byte secondFirstPixel = secondRender.Bytes![0];
        firstRender.Bytes![0] = firstRender.Bytes[0] == 0 ? (byte)255 : (byte)0;

        BarcodeImage image = new BarcodeImage();
        image.Pixels = new byte[400];
        image.Width = 20;
        image.Height = 20;
        image.Stride = 20;
        image.PixelFormat = BarcodePixelFormat.Gray8;
        BarcodeDetectionRequest detectionRequest = new BarcodeDetectionRequest();
        detectionRequest.Image = image;
        IBarcodeDetector detector = new BarcodeDetector();
        BarcodeDetectionResult firstDetection = detector.DetectBarcodes(
            detectionRequest);
        BarcodeDetectionResult secondDetection = detector.DetectBarcodes(
            detectionRequest);

        Assert.Multiple(() =>
        {
            Assert.That(firstRender.Bytes, Is.Not.SameAs(secondRender.Bytes));
            Assert.That(secondRender.Bytes![0], Is.EqualTo(secondFirstPixel));
            Assert.That(firstRender.Bytes![0], Is.Not.EqualTo(secondRender.Bytes[0]));
            Assert.That(firstRender.Size, Is.Not.SameAs(secondRender.Size));
            Assert.That(firstRender.Diagnostics, Is.Not.SameAs(secondRender.Diagnostics));
            Assert.That(firstDetection.Candidates, Is.Not.SameAs(secondDetection.Candidates));
            Assert.That(firstDetection.Diagnostics, Is.Not.SameAs(secondDetection.Diagnostics));
        });
    }

    [Test]
    public void ImageOperationsNormalizeMalformedInputToTheirDomainOutcomes()
    {
        BarcodeImage malformed = new BarcodeImage();
        malformed.Pixels = new byte[4];
        malformed.Width = 2;
        malformed.Height = 2;
        malformed.Stride = 1;
        malformed.PixelFormat = BarcodePixelFormat.Gray8;
        BarcodeDetectionRequest detectionRequest = new BarcodeDetectionRequest();
        detectionRequest.Image = malformed;
        BarcodeDecodeRequest decodeRequest = new BarcodeDecodeRequest();
        decodeRequest.Image = malformed;
        IBarcodeDetector detector = new BarcodeDetector();
        IBarcodeDecoder decoder = new BarcodeDecoder();

        BarcodeDecodeAttempt attempt = decoder.TryDecodeBarcode(decodeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => detector.DetectBarcodes(detectionRequest),
                Throws.TypeOf<BarcodeDetectionException>());
            Assert.That(
                () => decoder.DecodeBarcode(decodeRequest),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.FailureCode, Is.EqualTo("INVALID_REQUEST"));
        });
    }

    static BarcodeEncodeRequest CreateRequest(
        BarcodeFormat format,
        string? text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = format;
        request.Payload = payload;
        return request;
    }

    static BarcodeEncodeRequest CreateGs1Request(BarcodeFormat format)
    {
        BarcodeEncodeRequest request = CreateRequest(
            format,
            "0109506000134352");
        request.Payload!.IsGs1 = true;
        return request;
    }

    static BarcodeEncodeRequest CreateCompactPdf417Request()
    {
        BarcodeEncodeRequest request = CreateRequest(
            BarcodeFormat.CompactPdf417,
            "COMPACT");
        request.Pdf417 = new Pdf417Options();
        request.Pdf417.Variant = Pdf417Variant.Compact;
        return request;
    }

    static BarcodeEncodeRequest CreateMacroPdf417Request()
    {
        BarcodeEncodeRequest request = CreateRequest(
            BarcodeFormat.MacroPdf417,
            "MACRO");
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.MacroPdf417;
        append.Index = 0;
        append.Count = 1;
        append.FileIdentifier = "017053";
        append.IsLastSegment = true;
        request.Pdf417 = new Pdf417Options();
        request.Pdf417.Macro = append;
        return request;
    }

}
