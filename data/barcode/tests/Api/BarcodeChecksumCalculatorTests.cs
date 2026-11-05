namespace barcode.Tests;

public class BarcodeChecksumCalculatorTests
{
    [TestCase(BarcodeFormat.Ean8, "5512345", 7)]
    [TestCase(BarcodeFormat.Ean13, "400638133393", 1)]
    [TestCase(BarcodeFormat.UpcA, "03600029145", 2)]
    [TestCase(BarcodeFormat.Itf, "12345", 7)]
    [TestCase(BarcodeFormat.Itf14, "1234567890123", 1)]
    public void CalculateRoutesGs1Modulo10Formats(BarcodeFormat format, string text, int expected)
    {
        BarcodeChecksumRequest request = new BarcodeChecksumRequest();
        request.Format = format;
        request.Text = text;

        BarcodeChecksumResult result = new BarcodeChecksumCalculator().CalculateBarcodeChecksum(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(format));
            Assert.That(result.Algorithm, Is.EqualTo("GS1 modulo-10"));
            Assert.That(result.CalculatedValue, Is.EqualTo(expected));
            Assert.That(result.HasObservedValue, Is.False);
            Assert.That(result.ObservedValue, Is.Zero);
            Assert.That(result.IsValid, Is.False);
        });
    }

    [Test]
    public void CalculateRoutesCode128Code39AndCodabarFormats()
    {
        BarcodeChecksumRequest code128Request = new BarcodeChecksumRequest();
        code128Request.Format = BarcodeFormat.Code128;
        code128Request.Codewords = new int[] { 104, 48, 42, 42, 17, 18, 19, 35 };
        BarcodeChecksumRequest gs1128Request = new BarcodeChecksumRequest();
        gs1128Request.Format = BarcodeFormat.Gs1128;
        gs1128Request.Codewords = new int[] { 105, 12, 34 };
        BarcodeChecksumRequest code39Request = new BarcodeChecksumRequest();
        code39Request.Format = BarcodeFormat.Code39;
        code39Request.Text = "CODE39";
        BarcodeChecksumRequest codabarRequest = new BarcodeChecksumRequest();
        codabarRequest.Format = BarcodeFormat.Codabar;
        codabarRequest.Text = "A1234B";
        BarcodeChecksumCalculator calculator = new BarcodeChecksumCalculator();

        BarcodeChecksumResult code128 = calculator.CalculateBarcodeChecksum(code128Request);
        BarcodeChecksumResult gs1128 = calculator.CalculateBarcodeChecksum(gs1128Request);
        BarcodeChecksumResult code39 = calculator.CalculateBarcodeChecksum(code39Request);
        BarcodeChecksumResult codabar = calculator.CalculateBarcodeChecksum(codabarRequest);

        Assert.Multiple(() =>
        {
            Assert.That(code128.Algorithm, Is.EqualTo("Code 128 modulo-103"));
            Assert.That(code128.CalculatedValue, Is.EqualTo(55));
            Assert.That(gs1128.Algorithm, Is.EqualTo("Code 128 modulo-103"));
            Assert.That(gs1128.CalculatedValue, Is.EqualTo(82));
            Assert.That(code39.Algorithm, Is.EqualTo("Code 39 modulo-43"));
            Assert.That(code39.CalculatedValue, Is.EqualTo(32));
            Assert.That(codabar.Algorithm, Is.EqualTo("Codabar modulo-16"));
            Assert.That(codabar.CalculatedValue, Is.EqualTo(5));
        });
    }

    [Test]
    public void ExplicitAlgorithmAliasesSupportAutomaticFormat()
    {
        BarcodeChecksumRequest gs1Request = new BarcodeChecksumRequest();
        gs1Request.Format = BarcodeFormat.Automatic;
        gs1Request.Algorithm = "Modulo10";
        gs1Request.Text = "1234";
        BarcodeChecksumRequest code128Request = new BarcodeChecksumRequest();
        code128Request.Format = BarcodeFormat.Automatic;
        code128Request.Algorithm = "Modulo103";
        code128Request.Codewords = new int[] { 105, 12, 34 };
        BarcodeChecksumRequest code39Request = new BarcodeChecksumRequest();
        code39Request.Format = BarcodeFormat.Automatic;
        code39Request.Algorithm = "Code39Modulo43";
        code39Request.Text = "ABC";
        BarcodeChecksumRequest codabarRequest = new BarcodeChecksumRequest();
        codabarRequest.Format = BarcodeFormat.Automatic;
        codabarRequest.Algorithm = "Codabar modulo-16";
        codabarRequest.Text = "A12B";
        BarcodeChecksumCalculator calculator = new BarcodeChecksumCalculator();

        Assert.Multiple(() =>
        {
            Assert.That(calculator.CalculateBarcodeChecksum(gs1Request).CalculatedValue, Is.EqualTo(8));
            Assert.That(calculator.CalculateBarcodeChecksum(code128Request).CalculatedValue, Is.EqualTo(82));
            Assert.That(calculator.CalculateBarcodeChecksum(code39Request).CalculatedValue, Is.EqualTo(33));
            Assert.That(calculator.CalculateBarcodeChecksum(codabarRequest).CalculatedValue, Is.EqualTo(12));
        });
    }

    [TestCase(BarcodeFormat.Ean8, "551234", "55123457")]
    [TestCase(BarcodeFormat.Ean13, "40063813339", "4006381333931")]
    [TestCase(BarcodeFormat.UpcA, "0360002914", "036000291452")]
    [TestCase(BarcodeFormat.Itf14, "123456789012", "12345678901231")]
    public void FixedLengthFormatsRequireDataWithoutTheObservedCheckDigit(BarcodeFormat format, string tooShort, string complete)
    {
        BarcodeChecksumRequest request = new BarcodeChecksumRequest();
        request.Format = format;
        request.Text = tooShort;
        BarcodeChecksumRequest completeRequest = new BarcodeChecksumRequest();
        completeRequest.Format = format;
        completeRequest.Text = complete;
        BarcodeChecksumCalculator calculator = new BarcodeChecksumCalculator();

        Assert.Multiple(() =>
        {
            Assert.That(() => calculator.CalculateBarcodeChecksum(request), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.CalculateBarcodeChecksum(completeRequest), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void ItfCalculationRequiresADataLengthThatBecomesEvenAfterAppendingTheCheckDigit()
    {
        BarcodeChecksumRequest request = new BarcodeChecksumRequest();
        request.Format = BarcodeFormat.Itf;
        request.Text = "1234";

        Assert.That(
            () => new BarcodeChecksumCalculator().CalculateBarcodeChecksum(request),
            Throws.TypeOf<BarcodeValidationException>());
    }

    [Test]
    public void VerificationReturnsResultsForMatchesAndMismatches()
    {
        BarcodeChecksumRequest matching = new BarcodeChecksumRequest();
        matching.Format = BarcodeFormat.Ean13;
        matching.Text = "400638133393";
        matching.HasObservedValue = true;
        matching.ObservedValue = 1;
        BarcodeChecksumRequest mismatching = new BarcodeChecksumRequest();
        mismatching.Format = BarcodeFormat.Code39;
        mismatching.Text = "CODE39";
        mismatching.HasObservedValue = true;
        mismatching.ObservedValue = 31;
        BarcodeChecksumCalculator calculator = new BarcodeChecksumCalculator();

        BarcodeChecksumResult valid = calculator.VerifyBarcodeChecksum(matching);
        BarcodeChecksumResult invalid = calculator.VerifyBarcodeChecksum(mismatching);

        Assert.Multiple(() =>
        {
            Assert.That(valid.CalculatedValue, Is.EqualTo(1));
            Assert.That(valid.HasObservedValue, Is.True);
            Assert.That(valid.ObservedValue, Is.EqualTo(1));
            Assert.That(valid.IsValid, Is.True);
            Assert.That(invalid.CalculatedValue, Is.EqualTo(32));
            Assert.That(invalid.HasObservedValue, Is.True);
            Assert.That(invalid.ObservedValue, Is.EqualTo(31));
            Assert.That(invalid.IsValid, Is.False);
        });
    }

    [Test]
    public void VerificationSupportsCodewordAndCodabarChecksumsWithoutThrowingOnMismatch()
    {
        BarcodeChecksumRequest code128 = new BarcodeChecksumRequest();
        code128.Format = BarcodeFormat.Code128;
        code128.Codewords = new int[] { 104, 48, 42, 42, 17, 18, 19, 35 };
        code128.HasObservedValue = true;
        code128.ObservedValue = 54;
        BarcodeChecksumRequest codabar = new BarcodeChecksumRequest();
        codabar.Format = BarcodeFormat.Codabar;
        codabar.Text = "A1234B";
        codabar.HasObservedValue = true;
        codabar.ObservedValue = 5;
        BarcodeChecksumCalculator calculator = new BarcodeChecksumCalculator();

        Assert.Multiple(() =>
        {
            Assert.That(calculator.VerifyBarcodeChecksum(code128).IsValid, Is.False);
            Assert.That(calculator.VerifyBarcodeChecksum(codabar).IsValid, Is.True);
        });
    }

    [Test]
    public void RequestsRejectMissingSourcesMissingObservationsAndInvalidObservedRanges()
    {
        BarcodeChecksumRequest missingGs1Text = new BarcodeChecksumRequest();
        missingGs1Text.Format = BarcodeFormat.Ean8;
        BarcodeChecksumRequest missingText = new BarcodeChecksumRequest();
        missingText.Format = BarcodeFormat.Code39;
        BarcodeChecksumRequest missingCodabarText = new BarcodeChecksumRequest();
        missingCodabarText.Format = BarcodeFormat.Codabar;
        BarcodeChecksumRequest missingCodewords = new BarcodeChecksumRequest();
        missingCodewords.Format = BarcodeFormat.Code128;
        BarcodeChecksumRequest missingObservation = new BarcodeChecksumRequest();
        missingObservation.Format = BarcodeFormat.Ean8;
        missingObservation.Text = "5512345";
        BarcodeChecksumRequest invalidObservation = new BarcodeChecksumRequest();
        invalidObservation.Format = BarcodeFormat.Codabar;
        invalidObservation.Text = "A1234B";
        invalidObservation.HasObservedValue = true;
        invalidObservation.ObservedValue = 16;
        BarcodeChecksumRequest negativeObservation = new BarcodeChecksumRequest();
        negativeObservation.Format = BarcodeFormat.Ean8;
        negativeObservation.Text = "5512345";
        negativeObservation.HasObservedValue = true;
        negativeObservation.ObservedValue = -1;
        BarcodeChecksumCalculator calculator = new BarcodeChecksumCalculator();

        Assert.Multiple(() =>
        {
            Assert.That(() => calculator.CalculateBarcodeChecksum(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.VerifyBarcodeChecksum(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.CalculateBarcodeChecksum(missingGs1Text), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.CalculateBarcodeChecksum(missingText), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.CalculateBarcodeChecksum(missingCodabarText), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.CalculateBarcodeChecksum(missingCodewords), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.VerifyBarcodeChecksum(missingObservation), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.VerifyBarcodeChecksum(invalidObservation), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.VerifyBarcodeChecksum(negativeObservation), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void RequestsRejectUnsupportedFormatsAlgorithmsAndFormatConflicts()
    {
        BarcodeChecksumRequest unsupportedFormat = new BarcodeChecksumRequest();
        unsupportedFormat.Format = BarcodeFormat.DataMatrix;
        unsupportedFormat.Text = "123";
        BarcodeChecksumRequest unsupportedAlgorithm = new BarcodeChecksumRequest();
        unsupportedAlgorithm.Format = BarcodeFormat.Automatic;
        unsupportedAlgorithm.Algorithm = "CRC-32";
        unsupportedAlgorithm.Bytes = new byte[] { 1, 2, 3 };
        BarcodeChecksumRequest conflictingAlgorithm = new BarcodeChecksumRequest();
        conflictingAlgorithm.Format = BarcodeFormat.Code39;
        conflictingAlgorithm.Algorithm = "Modulo10";
        conflictingAlgorithm.Text = "123";
        BarcodeChecksumCalculator calculator = new BarcodeChecksumCalculator();

        Assert.Multiple(() =>
        {
            Assert.That(() => calculator.CalculateBarcodeChecksum(unsupportedFormat), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.CalculateBarcodeChecksum(unsupportedAlgorithm), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => calculator.CalculateBarcodeChecksum(conflictingAlgorithm), Throws.TypeOf<BarcodeValidationException>());
        });
    }
}
