namespace barcode.Tests;

public class OperationContractTests
{
    [Test]
    public void EncoderContractDispatchesEncodeAndExpectedFailureAttempt()
    {
        OperationContractStub stub = new OperationContractStub();
        IBarcodeEncoder encoder = stub;
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();

        BarcodeSymbol encoded = encoder.EncodeBarcode(request);
        BarcodeSymbol? attempted = encoder.TryEncodeBarcode(request);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.SameAs(stub.EncodedSymbol));
            Assert.That(attempted, Is.Null);
            Assert.That(stub.EncodeRequest, Is.SameAs(request));
            Assert.That(stub.TryEncodeRequest, Is.SameAs(request));
        });
    }

    [Test]
    public void DecoderContractDispatchesThrowingAndExceptionlessOperations()
    {
        OperationContractStub stub = new OperationContractStub();
        IBarcodeDecoder decoder = stub;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();

        BarcodeDecodeResult[] decoded = decoder.DecodeBarcode(request);
        BarcodeDecodeAttempt attempted = decoder.TryDecodeBarcode(request);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.SameAs(stub.DecodeResults));
            Assert.That(attempted, Is.SameAs(stub.DecodeAttempt));
            Assert.That(stub.DecodeRequest, Is.SameAs(request));
            Assert.That(stub.TryDecodeRequest, Is.SameAs(request));
        });
    }

    [Test]
    public void DetectorContractDispatchesDetection()
    {
        OperationContractStub stub = new OperationContractStub();
        IBarcodeDetector detector = stub;
        BarcodeDetectionRequest request = new BarcodeDetectionRequest();

        BarcodeDetectionResult result = detector.DetectBarcodes(request);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(stub.DetectionResult));
            Assert.That(stub.DetectionRequest, Is.SameAs(request));
        });
    }

    [Test]
    public void RendererContractDispatchesRendering()
    {
        OperationContractStub stub = new OperationContractStub();
        IBarcodeRenderer renderer = stub;
        BarcodeRenderRequest request = new BarcodeRenderRequest();

        BarcodeRenderedImage result = renderer.RenderBarcode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(stub.RenderedImage));
            Assert.That(stub.RenderRequest, Is.SameAs(request));
        });
    }

    [Test]
    public void ValidatorContractDispatchesEveryValidationTarget()
    {
        OperationContractStub stub = new OperationContractStub();
        IBarcodeValidator validator = stub;
        BarcodeValidationRequest dataRequest = new BarcodeValidationRequest();
        BarcodeValidationRequest symbolRequest = new BarcodeValidationRequest();
        BarcodeValidationRequest renderRequest = new BarcodeValidationRequest();
        BarcodeValidationRequest imageRequest = new BarcodeValidationRequest();

        BarcodeValidationReport data = validator.ValidateBarcodeData(dataRequest);
        BarcodeValidationReport symbol = validator.ValidateBarcodeSymbol(symbolRequest);
        BarcodeValidationReport render = validator.ValidateBarcodeRender(renderRequest);
        BarcodeValidationReport image = validator.ValidateBarcodeImage(imageRequest);

        Assert.Multiple(() =>
        {
            Assert.That(data, Is.SameAs(stub.ValidationReport));
            Assert.That(symbol, Is.SameAs(stub.ValidationReport));
            Assert.That(render, Is.SameAs(stub.ValidationReport));
            Assert.That(image, Is.SameAs(stub.ValidationReport));
            Assert.That(stub.DataValidationRequest, Is.SameAs(dataRequest));
            Assert.That(stub.SymbolValidationRequest, Is.SameAs(symbolRequest));
            Assert.That(stub.RenderValidationRequest, Is.SameAs(renderRequest));
            Assert.That(stub.ImageValidationRequest, Is.SameAs(imageRequest));
        });
    }

    [Test]
    public void ChecksumContractDispatchesCalculationAndVerification()
    {
        OperationContractStub stub = new OperationContractStub();
        IBarcodeChecksumCalculator calculator = stub;
        BarcodeChecksumRequest calculateRequest = new BarcodeChecksumRequest();
        BarcodeChecksumRequest verifyRequest = new BarcodeChecksumRequest();

        BarcodeChecksumResult calculated = calculator.CalculateBarcodeChecksum(calculateRequest);
        BarcodeChecksumResult verified = calculator.VerifyBarcodeChecksum(verifyRequest);

        Assert.Multiple(() =>
        {
            Assert.That(calculated, Is.SameAs(stub.ChecksumResult));
            Assert.That(verified, Is.SameAs(stub.ChecksumResult));
            Assert.That(stub.CalculateChecksumRequest, Is.SameAs(calculateRequest));
            Assert.That(stub.VerifyChecksumRequest, Is.SameAs(verifyRequest));
        });
    }

    [Test]
    public void Gs1ContractDispatchesProcessingAndMissingLookup()
    {
        OperationContractStub stub = new OperationContractStub();
        IGs1DataProcessor processor = stub;
        Gs1ProcessRequest request = new Gs1ProcessRequest();

        Gs1ProcessResult result = processor.ProcessGs1Data(request);
        Gs1ApplicationIdentifierInfo? definition = processor.LookupGs1ApplicationIdentifier("9999");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(stub.Gs1Result));
            Assert.That(definition, Is.Null);
            Assert.That(stub.Gs1Request, Is.SameAs(request));
            Assert.That(stub.ApplicationIdentifier, Is.EqualTo("9999"));
        });
    }

    [Test]
    public void StructuredAppendContractDispatchesAssembly()
    {
        OperationContractStub stub = new OperationContractStub();
        IStructuredAppendAssembler assembler = stub;
        StructuredAppendRequest request = new StructuredAppendRequest();

        StructuredAppendResult result = assembler.AssembleStructuredAppend(request);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(stub.StructuredAppendResult));
            Assert.That(stub.StructuredAppendRequest, Is.SameAs(request));
        });
    }
}

public class OperationContractStub : IBarcodeEncoder, IBarcodeDecoder, IBarcodeDetector, IBarcodeRenderer, IBarcodeValidator, IBarcodeChecksumCalculator, IGs1DataProcessor, IStructuredAppendAssembler
{
    public BarcodeSymbol EncodedSymbol = new BarcodeSymbol();
    public BarcodeSymbol? TryEncodedSymbol;
    public BarcodeDecodeResult[] DecodeResults = new BarcodeDecodeResult[] { new BarcodeDecodeResult() };
    public BarcodeDecodeAttempt DecodeAttempt = new BarcodeDecodeAttempt();
    public BarcodeDetectionResult DetectionResult = new BarcodeDetectionResult();
    public BarcodeRenderedImage RenderedImage = new BarcodeRenderedImage();
    public BarcodeValidationReport ValidationReport = new BarcodeValidationReport();
    public BarcodeChecksumResult ChecksumResult = new BarcodeChecksumResult();
    public Gs1ProcessResult Gs1Result = new Gs1ProcessResult();
    public Gs1ApplicationIdentifierInfo? ApplicationIdentifierInfo;
    public StructuredAppendResult StructuredAppendResult = new StructuredAppendResult();
    public BarcodeEncodeRequest? EncodeRequest;
    public BarcodeEncodeRequest? TryEncodeRequest;
    public BarcodeDecodeRequest? DecodeRequest;
    public BarcodeDecodeRequest? TryDecodeRequest;
    public BarcodeDetectionRequest? DetectionRequest;
    public BarcodeRenderRequest? RenderRequest;
    public BarcodeValidationRequest? DataValidationRequest;
    public BarcodeValidationRequest? SymbolValidationRequest;
    public BarcodeValidationRequest? RenderValidationRequest;
    public BarcodeValidationRequest? ImageValidationRequest;
    public BarcodeChecksumRequest? CalculateChecksumRequest;
    public BarcodeChecksumRequest? VerifyChecksumRequest;
    public Gs1ProcessRequest? Gs1Request;
    public string? ApplicationIdentifier;
    public StructuredAppendRequest? StructuredAppendRequest;

    public BarcodeSymbol EncodeBarcode(BarcodeEncodeRequest request)
    {
        EncodeRequest = request;
        return EncodedSymbol;
    }

    public BarcodeSymbol? TryEncodeBarcode(BarcodeEncodeRequest request)
    {
        TryEncodeRequest = request;
        return TryEncodedSymbol;
    }

    public BarcodeDecodeResult[] DecodeBarcode(BarcodeDecodeRequest request)
    {
        DecodeRequest = request;
        return DecodeResults;
    }

    public BarcodeDecodeAttempt TryDecodeBarcode(BarcodeDecodeRequest request)
    {
        TryDecodeRequest = request;
        return DecodeAttempt;
    }

    public BarcodeDetectionResult DetectBarcodes(BarcodeDetectionRequest request)
    {
        DetectionRequest = request;
        return DetectionResult;
    }

    public BarcodeRenderedImage RenderBarcode(BarcodeRenderRequest request)
    {
        RenderRequest = request;
        return RenderedImage;
    }

    public BarcodeValidationReport ValidateBarcodeData(BarcodeValidationRequest request)
    {
        DataValidationRequest = request;
        return ValidationReport;
    }

    public BarcodeValidationReport ValidateBarcodeSymbol(BarcodeValidationRequest request)
    {
        SymbolValidationRequest = request;
        return ValidationReport;
    }

    public BarcodeValidationReport ValidateBarcodeRender(BarcodeValidationRequest request)
    {
        RenderValidationRequest = request;
        return ValidationReport;
    }

    public BarcodeValidationReport ValidateBarcodeImage(BarcodeValidationRequest request)
    {
        ImageValidationRequest = request;
        return ValidationReport;
    }

    public BarcodeChecksumResult CalculateBarcodeChecksum(BarcodeChecksumRequest request)
    {
        CalculateChecksumRequest = request;
        return ChecksumResult;
    }

    public BarcodeChecksumResult VerifyBarcodeChecksum(BarcodeChecksumRequest request)
    {
        VerifyChecksumRequest = request;
        return ChecksumResult;
    }

    public Gs1ProcessResult ProcessGs1Data(Gs1ProcessRequest request)
    {
        Gs1Request = request;
        return Gs1Result;
    }

    public Gs1ApplicationIdentifierInfo? LookupGs1ApplicationIdentifier(string applicationIdentifier)
    {
        ApplicationIdentifier = applicationIdentifier;
        return ApplicationIdentifierInfo;
    }

    public StructuredAppendResult AssembleStructuredAppend(StructuredAppendRequest request)
    {
        StructuredAppendRequest = request;
        return StructuredAppendResult;
    }
}
