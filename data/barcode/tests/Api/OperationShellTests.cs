namespace barcode.Tests;

public class OperationShellTests
{
    [Test]
    public void EncoderExposesThrowingAndExceptionlessShellPaths()
    {
        IBarcodeEncoder encoder = new BarcodeEncoder();
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => encoder.EncodeBarcode(request),
                Throws.TypeOf<BarcodeEncodingException>().With.Message.EqualTo("A barcode payload is required."));
            Assert.That(encoder.TryEncodeBarcode(request), Is.Null);
        });
    }

    [Test]
    public void DecoderExposesThrowingAndStructuredInvalidInputPaths()
    {
        IBarcodeDecoder decoder = new BarcodeDecoder();
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();

        BarcodeDecodeAttempt attempt = decoder.TryDecodeBarcode(request);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => decoder.DecodeBarcode(request),
                Throws.TypeOf<BarcodeDecodingException>().With.Message.EqualTo(
                    "Exactly one image or pure symbol must be supplied."));
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.Results, Is.Empty);
            Assert.That(attempt.Diagnostics, Is.Empty);
            Assert.That(attempt.FailureCode, Is.EqualTo("INVALID_INPUT"));
            Assert.That(attempt.FailureMessage, Is.EqualTo(
                "Exactly one image or pure symbol must be supplied."));
        });
    }

    [Test]
    public void DetectorRequiresAnImageWithDomainException()
    {
        IBarcodeDetector detector = new BarcodeDetector();
        BarcodeDetectionRequest request = new BarcodeDetectionRequest();

        Assert.That(
            () => detector.DetectBarcodes(request),
            Throws.TypeOf<BarcodeDetectionException>().With.Message.EqualTo(
                "A barcode detection request with an image is required."));
    }

    [Test]
    public void RendererRejectsARequestWithoutASymbol()
    {
        IBarcodeRenderer renderer = new BarcodeRenderer();
        BarcodeRenderRequest request = new BarcodeRenderRequest();

        Assert.That(
            () => renderer.RenderBarcode(request),
            Throws.TypeOf<BarcodeRenderingException>().With.Message.EqualTo("A barcode symbol is required to create a render plan."));
    }

    [Test]
    public void ValidatorReportsInvalidDataAndUnavailableTargetsPrecisely()
    {
        IBarcodeValidator validator = new BarcodeValidator();
        BarcodeValidationRequest request = new BarcodeValidationRequest();

        Assert.Multiple(() =>
        {
            BarcodeValidationReport data = validator.ValidateBarcodeData(request);
            Assert.That(data.IsConformant, Is.False);
            Assert.That(data.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "ENCODE_REQUEST_MISSING"));
            BarcodeValidationReport symbol = validator.ValidateBarcodeSymbol(request);
            Assert.That(symbol.IsConformant, Is.False);
            Assert.That(symbol.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "SYMBOL_MISSING"));
            BarcodeValidationReport render = validator.ValidateBarcodeRender(request);
            Assert.That(render.IsConformant, Is.False);
            Assert.That(render.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_REQUEST_MISSING"));
            BarcodeValidationReport image = validator.ValidateBarcodeImage(request);
            Assert.That(image.IsConformant, Is.False);
            Assert.That(image.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "IMAGE_MISSING"));
        });
    }

    [Test]
    public void ChecksumCalculatorRejectsAnAmbiguousDefaultRequest()
    {
        IBarcodeChecksumCalculator calculator = new BarcodeChecksumCalculator();
        BarcodeChecksumRequest request = new BarcodeChecksumRequest();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => calculator.CalculateBarcodeChecksum(request),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("The barcode format does not select a supported checksum algorithm."));
            Assert.That(
                () => calculator.VerifyBarcodeChecksum(request),
                Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("The barcode format does not select a supported checksum algorithm."));
        });
    }

    [Test]
    public void Gs1ProcessorReturnsStructuredFailureForAnEmptyRequest()
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();
        Gs1ProcessRequest request = new Gs1ProcessRequest();

        Assert.Multiple(() =>
        {
            Gs1ProcessResult result = processor.ProcessGs1Data(request);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues, Is.Not.Empty);
            Assert.That(
                processor.LookupGs1ApplicationIdentifier("01"),
                Is.Not.Null);
        });
    }

    [Test]
    public void StructuredAppendAssemblerRejectsUnsupportedFormat()
    {
        IStructuredAppendAssembler assembler = new StructuredAppendAssembler();
        StructuredAppendRequest request = new StructuredAppendRequest();

        Assert.That(
            () => assembler.AssembleStructuredAppend(request),
            Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo(
                "Structured-append assembly supports Data Matrix, GS1 DataMatrix, and Macro PDF417 only."));
    }
}
