namespace barcode.Tests;

public class ImageDecodeDetectionDataTests
{
    [Test]
    public void ImageDecodeOptionsAndRequestHoldAllInputs()
    {
        byte[] pixels = new byte[] { 10, 20, 30, 40, 50, 60 };
        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = 2;
        image.Height = 1;
        image.Stride = -8;
        image.Offset = 4;
        image.PixelFormat = BarcodePixelFormat.Rgb24;
        image.DpiX = 300.0;
        image.DpiY = 301.0;
        BarcodeFormat[] formats = new BarcodeFormat[] { BarcodeFormat.Code128, BarcodeFormat.DataMatrix };
        int[] lengths = new int[] { 8, 12 };
        BarcodeRectangle region = new BarcodeRectangle();
        BarcodeRotation[] rotations = new BarcodeRotation[] { BarcodeRotation.None, BarcodeRotation.Clockwise90 };
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = formats;
        options.AllowedLengths = lengths;
        options.RegionOfInterest = region;
        options.MaximumResults = 5;
        options.ExpectedResultCount = 2;
        options.PureSymbol = true;
        options.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        options.ParseGs1 = true;
        options.CharacterEncoding = "ISO-8859-1";
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.Rotations = rotations;
        options.Effort = BarcodeEffort.Thorough;
        options.BinarizationMode = BarcodeBinarizationMode.Hybrid;
        options.LocalizationMode = BarcodeLocalizationMode.FinderPatterns;
        options.MaximumDiagnostics = 64;
        BarcodeSymbol symbol = new BarcodeSymbol();
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Image = image;
        request.Symbol = symbol;
        request.Options = options;

        pixels[0] = 99;
        formats[0] = BarcodeFormat.Code39;
        lengths[0] = 14;
        rotations[0] = BarcodeRotation.Clockwise180;

        Assert.Multiple(() =>
        {
            Assert.That(image.Pixels, Is.SameAs(pixels));
            Assert.That(image.Pixels?[0], Is.EqualTo(99));
            Assert.That(image.Width, Is.EqualTo(2));
            Assert.That(image.Height, Is.EqualTo(1));
            Assert.That(image.Stride, Is.EqualTo(-8));
            Assert.That(image.Offset, Is.EqualTo(4));
            Assert.That(image.PixelFormat, Is.EqualTo(BarcodePixelFormat.Rgb24));
            Assert.That(image.DpiX, Is.EqualTo(300.0));
            Assert.That(image.DpiY, Is.EqualTo(301.0));
            Assert.That(options.AllowedFormats, Is.SameAs(formats));
            Assert.That(options.AllowedFormats?[0], Is.EqualTo(BarcodeFormat.Code39));
            Assert.That(options.AllowedLengths, Is.SameAs(lengths));
            Assert.That(options.AllowedLengths?[0], Is.EqualTo(14));
            Assert.That(options.RegionOfInterest, Is.SameAs(region));
            Assert.That(options.MaximumResults, Is.EqualTo(5));
            Assert.That(options.ExpectedResultCount, Is.EqualTo(2));
            Assert.That(options.PureSymbol, Is.True);
            Assert.That(options.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.VerifyKeep));
            Assert.That(options.ParseGs1, Is.True);
            Assert.That(options.CharacterEncoding, Is.EqualTo("ISO-8859-1"));
            Assert.That(options.Polarity, Is.EqualTo(BarcodePolarity.Automatic));
            Assert.That(options.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Both));
            Assert.That(options.Rotations, Is.SameAs(rotations));
            Assert.That(options.Rotations?[0], Is.EqualTo(BarcodeRotation.Clockwise180));
            Assert.That(options.Effort, Is.EqualTo(BarcodeEffort.Thorough));
            Assert.That(options.BinarizationMode, Is.EqualTo(BarcodeBinarizationMode.Hybrid));
            Assert.That(options.LocalizationMode, Is.EqualTo(BarcodeLocalizationMode.FinderPatterns));
            Assert.That(options.MaximumDiagnostics, Is.EqualTo(64));
            Assert.That(request.Image, Is.SameAs(image));
            Assert.That(request.Symbol, Is.SameAs(symbol));
            Assert.That(request.Options, Is.SameAs(options));
        });
    }

    [Test]
    public void DiagnosticMetadataAndDecodeResultHoldAllEvidence()
    {
        BarcodePoint point = new BarcodePoint();
        BarcodeRectangle region = new BarcodeRectangle();
        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        diagnostic.Stage = "checksum";
        diagnostic.Code = "CHECKSUM_VALID";
        diagnostic.Message = "Checksum matched.";
        diagnostic.HasNumericEvidence = true;
        diagnostic.Value = 42.0;
        diagnostic.Threshold = 40.0;
        diagnostic.Location = point;
        diagnostic.Region = region;
        int[] codewords = new int[] { 1, 2, 3 };
        StructuredAppendInfo append = new StructuredAppendInfo();
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.HasChecksum = true;
        metadata.ChecksumValid = true;
        metadata.ChecksumValue = 42;
        metadata.ErrorCorrectionApplied = true;
        metadata.CorrectedErrorCount = 3;
        metadata.ErasureCount = 1;
        metadata.SymbologyIdentifier = "]d2";
        metadata.Codewords = codewords;
        metadata.Confidence = 0.975;
        metadata.IsGs1 = true;
        metadata.Gs1ElementString = "0109501101530003";
        metadata.StructuredAppend = append;
        BarcodePayload payload = new BarcodePayload();
        BarcodeQuadrilateral location = new BarcodeQuadrilateral();
        BarcodeDiagnostic[] diagnostics = new BarcodeDiagnostic[] { diagnostic };
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Payload = payload;
        result.Format = BarcodeFormat.Gs1DataMatrix;
        result.Dimension = BarcodeDimension.Matrix;
        result.Location = location;
        result.Rotation = BarcodeRotation.Clockwise90;
        result.OrientationDegrees = 91.25;
        result.SkewDegrees = 1.25;
        result.Polarity = BarcodePolarity.DarkOnLight;
        result.MirrorMode = BarcodeMirrorMode.Mirrored;
        result.Metadata = metadata;
        result.Diagnostics = diagnostics;

        codewords[1] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Stage, Is.EqualTo("checksum"));
            Assert.That(diagnostic.Code, Is.EqualTo("CHECKSUM_VALID"));
            Assert.That(diagnostic.Message, Is.EqualTo("Checksum matched."));
            Assert.That(diagnostic.HasNumericEvidence, Is.True);
            Assert.That(diagnostic.Value, Is.EqualTo(42.0));
            Assert.That(diagnostic.Threshold, Is.EqualTo(40.0));
            Assert.That(diagnostic.Location, Is.SameAs(point));
            Assert.That(diagnostic.Region, Is.SameAs(region));
            Assert.That(metadata.HasChecksum, Is.True);
            Assert.That(metadata.ChecksumValid, Is.True);
            Assert.That(metadata.ChecksumValue, Is.EqualTo(42));
            Assert.That(metadata.ErrorCorrectionApplied, Is.True);
            Assert.That(metadata.CorrectedErrorCount, Is.EqualTo(3));
            Assert.That(metadata.ErasureCount, Is.EqualTo(1));
            Assert.That(metadata.SymbologyIdentifier, Is.EqualTo("]d2"));
            Assert.That(metadata.Codewords, Is.SameAs(codewords));
            Assert.That(metadata.Codewords?[1], Is.EqualTo(9));
            Assert.That(metadata.Confidence, Is.EqualTo(0.975));
            Assert.That(metadata.IsGs1, Is.True);
            Assert.That(metadata.Gs1ElementString, Is.EqualTo("0109501101530003"));
            Assert.That(metadata.StructuredAppend, Is.SameAs(append));
            Assert.That(result.Payload, Is.SameAs(payload));
            Assert.That(result.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(result.Dimension, Is.EqualTo(BarcodeDimension.Matrix));
            Assert.That(result.Location, Is.SameAs(location));
            Assert.That(result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise90));
            Assert.That(result.OrientationDegrees, Is.EqualTo(91.25));
            Assert.That(result.SkewDegrees, Is.EqualTo(1.25));
            Assert.That(result.Polarity, Is.EqualTo(BarcodePolarity.DarkOnLight));
            Assert.That(result.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Mirrored));
            Assert.That(result.Metadata, Is.SameAs(metadata));
            Assert.That(result.Diagnostics, Is.SameAs(diagnostics));
        });
    }

    [Test]
    public void DecodeAttemptHoldsSuccessFailureResultsAndDiagnostics()
    {
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        BarcodeDecodeResult[] results = new BarcodeDecodeResult[] { result };
        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        BarcodeDiagnostic[] diagnostics = new BarcodeDiagnostic[] { diagnostic };
        BarcodeDecodeAttempt attempt = new BarcodeDecodeAttempt();
        attempt.Success = false;
        attempt.Results = results;
        attempt.Diagnostics = diagnostics;
        attempt.FailureCode = "NO_SYMBOL";
        attempt.FailureMessage = "No supported symbol was decoded.";

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.Results, Is.SameAs(results));
            Assert.That(attempt.Diagnostics, Is.SameAs(diagnostics));
            Assert.That(attempt.FailureCode, Is.EqualTo("NO_SYMBOL"));
            Assert.That(attempt.FailureMessage, Is.EqualTo("No supported symbol was decoded."));
        });
    }

    [Test]
    public void DetectionTypesHoldOptionsCandidatesAndDiagnostics()
    {
        BarcodeFormat[] formats = new BarcodeFormat[] { BarcodeFormat.Pdf417 };
        BarcodeRectangle region = new BarcodeRectangle();
        BarcodeDetectionOptions options = new BarcodeDetectionOptions();
        options.AllowedFormats = formats;
        options.RegionOfInterest = region;
        options.MaximumCandidates = 8;
        options.ExpectedCandidateCount = 3;
        options.Effort = BarcodeEffort.Balanced;
        options.BinarizationMode = BarcodeBinarizationMode.Otsu;
        options.LocalizationMode = BarcodeLocalizationMode.Lines;
        options.Polarity = BarcodePolarity.LightOnDark;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.TryRotations = true;
        options.MaximumDiagnostics = 32;
        BarcodeImage image = new BarcodeImage();
        BarcodeDetectionRequest request = new BarcodeDetectionRequest();
        request.Image = image;
        request.Options = options;
        BarcodeQuadrilateral location = new BarcodeQuadrilateral();
        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        BarcodeDiagnostic[] candidateDiagnostics = new BarcodeDiagnostic[] { diagnostic };
        BarcodeCandidate candidate = new BarcodeCandidate();
        candidate.Format = BarcodeFormat.Pdf417;
        candidate.Dimension = BarcodeDimension.StackedLinear;
        candidate.Location = location;
        candidate.AngleDegrees = 17.5;
        candidate.Confidence = 0.8;
        candidate.Polarity = BarcodePolarity.LightOnDark;
        candidate.MirrorMode = BarcodeMirrorMode.Normal;
        candidate.Diagnostics = candidateDiagnostics;
        BarcodeCandidate[] candidates = new BarcodeCandidate[] { candidate };
        BarcodeDiagnostic[] diagnostics = new BarcodeDiagnostic[] { diagnostic };
        BarcodeDetectionResult result = new BarcodeDetectionResult();
        result.Candidates = candidates;
        result.Diagnostics = diagnostics;

        formats[0] = BarcodeFormat.CompactPdf417;

        Assert.Multiple(() =>
        {
            Assert.That(options.AllowedFormats, Is.SameAs(formats));
            Assert.That(options.AllowedFormats?[0], Is.EqualTo(BarcodeFormat.CompactPdf417));
            Assert.That(options.RegionOfInterest, Is.SameAs(region));
            Assert.That(options.MaximumCandidates, Is.EqualTo(8));
            Assert.That(options.ExpectedCandidateCount, Is.EqualTo(3));
            Assert.That(options.Effort, Is.EqualTo(BarcodeEffort.Balanced));
            Assert.That(options.BinarizationMode, Is.EqualTo(BarcodeBinarizationMode.Otsu));
            Assert.That(options.LocalizationMode, Is.EqualTo(BarcodeLocalizationMode.Lines));
            Assert.That(options.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(options.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Both));
            Assert.That(options.TryRotations, Is.True);
            Assert.That(options.MaximumDiagnostics, Is.EqualTo(32));
            Assert.That(request.Image, Is.SameAs(image));
            Assert.That(request.Options, Is.SameAs(options));
            Assert.That(candidate.Format, Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(candidate.Dimension, Is.EqualTo(BarcodeDimension.StackedLinear));
            Assert.That(candidate.Location, Is.SameAs(location));
            Assert.That(candidate.AngleDegrees, Is.EqualTo(17.5));
            Assert.That(candidate.Confidence, Is.EqualTo(0.8));
            Assert.That(candidate.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(candidate.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Normal));
            Assert.That(candidate.Diagnostics, Is.SameAs(candidateDiagnostics));
            Assert.That(result.Candidates, Is.SameAs(candidates));
            Assert.That(result.Diagnostics, Is.SameAs(diagnostics));
        });
    }

    [Test]
    public void ConfigurationHoldsDecodeAndDetectionObjects()
    {
        BarcodeDecodeRequest decodeRequest = new BarcodeDecodeRequest();
        BarcodeDecodeAttempt decodeAttempt = new BarcodeDecodeAttempt();
        BarcodeDetectionRequest detectionRequest = new BarcodeDetectionRequest();
        BarcodeDetectionResult detectionResult = new BarcodeDetectionResult();
        BarcodeConfiguration configuration = new BarcodeConfiguration();
        configuration.DecodeRequest = decodeRequest;
        configuration.DecodeAttempt = decodeAttempt;
        configuration.DetectionRequest = detectionRequest;
        configuration.DetectionResult = detectionResult;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.DecodeRequest, Is.SameAs(decodeRequest));
            Assert.That(configuration.DecodeAttempt, Is.SameAs(decodeAttempt));
            Assert.That(configuration.DetectionRequest, Is.SameAs(detectionRequest));
            Assert.That(configuration.DetectionResult, Is.SameAs(detectionResult));
        });
    }
}
