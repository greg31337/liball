namespace barcode.Tests;

public class ValidationModeBehaviorTests
{
    [Test]
    public void RecoverablePreEncodeDefectIsWarningOnlyInTolerantMode()
    {
        BarcodeEncodeRequest encode = new BarcodeEncodeRequest();
        encode.Format = BarcodeFormat.Ean13;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "4006381333932";
        encode.Payload = payload;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.ChecksumMode = BarcodeChecksumMode.Require;
        encode.Options = options;

        BarcodeValidationReport strict = ValidateData(
            encode,
            BarcodeValidationMode.Strict);
        BarcodeValidationReport tolerant = ValidateData(
            encode,
            BarcodeValidationMode.Tolerant);

        AssertModePair(
            strict,
            tolerant,
            "EAN_UPC_CHECK_DIGIT_INVALID");
    }

    [Test]
    public void RecoverableSymbolDefectIsWarningOnlyInTolerantMode()
    {
        BarcodeSymbol symbol = CreateCode128Symbol();
        symbol.RequiredQuietZone!.LeftModules = 1;

        BarcodeValidationReport strict = ValidateSymbol(
            symbol,
            BarcodeValidationMode.Strict);
        BarcodeValidationReport tolerant = ValidateSymbol(
            symbol,
            BarcodeValidationMode.Tolerant);

        AssertModePair(
            strict,
            tolerant,
            "SYMBOL_QUIET_ZONE_INSUFFICIENT");
    }

    [Test]
    public void RecoverableRenderDefectIsWarningOnlyInTolerantMode()
    {
        BarcodeRenderRequest render = new BarcodeRenderRequest();
        render.Symbol = CreateCode128Symbol();
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Format = BarcodeRenderFormat.Gray8;
        options.ForegroundRgba = 0x777777FFu;
        options.BackgroundRgba = 0x888888FFu;
        render.Options = options;

        BarcodeValidationReport strict = ValidateRender(
            render,
            BarcodeValidationMode.Strict);
        BarcodeValidationReport tolerant = ValidateRender(
            render,
            BarcodeValidationMode.Tolerant);

        AssertModePair(
            strict,
            tolerant,
            "RENDER_CONTRAST_INSUFFICIENT");
    }

    [Test]
    public void RecoverableImageDefectIsWarningOnlyInTolerantMode()
    {
        BarcodeImage image = new BarcodeImage();
        image.Width = 20;
        image.Height = 20;
        image.Stride = 20;
        image.PixelFormat = BarcodePixelFormat.Gray8;
        image.Pixels = new byte[400];
        int index = 0;
        while (index < image.Pixels.Length)
        {
            image.Pixels[index] = (byte)(index % 2 == 0 ? 120 : 140);
            index = index + 1;
        }

        BarcodeValidationReport strict = ValidateImage(
            image,
            BarcodeValidationMode.Strict);
        BarcodeValidationReport tolerant = ValidateImage(
            image,
            BarcodeValidationMode.Tolerant);

        AssertModePair(
            strict,
            tolerant,
            "IMAGE_CONTRAST_INSUFFICIENT");
        Assert.That(tolerant.Issues,
            Has.One.Matches<BarcodeValidationIssue>(
                issue => issue.Code == "IMAGE_NOT_DECODED" &&
                    issue.Severity == BarcodeValidationSeverity.Error));
    }

    [Test]
    public void FatalMissingTargetRemainsErrorInTolerantMode()
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Mode = BarcodeValidationMode.Tolerant;

        BarcodeValidationReport report =
            new BarcodeValidator().ValidateBarcodeData(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.False);
            Assert.That(report.Mode, Is.EqualTo(BarcodeValidationMode.Tolerant));
            Assert.That(report.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "ENCODE_REQUEST_MISSING" &&
                        issue.Severity == BarcodeValidationSeverity.Error));
        });
    }

    static void AssertModePair(
        BarcodeValidationReport strict,
        BarcodeValidationReport tolerant,
        string code)
    {
        BarcodeValidationIssue strictIssue = strict.Issues!.Single(
            issue => issue.Code == code);
        BarcodeValidationIssue tolerantIssue = tolerant.Issues!.Single(
            issue => issue.Code == code);
        Assert.Multiple(() =>
        {
            Assert.That(strict.IsConformant, Is.False);
            Assert.That(tolerant.IsConformant, Is.False);
            Assert.That(strict.Mode, Is.EqualTo(BarcodeValidationMode.Strict));
            Assert.That(tolerant.Mode, Is.EqualTo(BarcodeValidationMode.Tolerant));
            Assert.That(strictIssue.Severity,
                Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(tolerantIssue.Severity,
                Is.EqualTo(BarcodeValidationSeverity.Warning));
        });
    }

    static BarcodeValidationReport ValidateData(
        BarcodeEncodeRequest encode,
        BarcodeValidationMode mode)
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = encode.Format;
        request.Data = encode;
        request.Mode = mode;
        return new BarcodeValidator().ValidateBarcodeData(request);
    }

    static BarcodeValidationReport ValidateSymbol(
        BarcodeSymbol symbol,
        BarcodeValidationMode mode)
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = symbol.Format;
        request.Symbol = symbol;
        request.Mode = mode;
        return new BarcodeValidator().ValidateBarcodeSymbol(request);
    }

    static BarcodeValidationReport ValidateRender(
        BarcodeRenderRequest render,
        BarcodeValidationMode mode)
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = BarcodeFormat.Code128;
        request.Render = render;
        request.Mode = mode;
        return new BarcodeValidator().ValidateBarcodeRender(request);
    }

    static BarcodeValidationReport ValidateImage(
        BarcodeImage image,
        BarcodeValidationMode mode)
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = BarcodeFormat.Code128;
        request.Image = image;
        request.Mode = mode;
        return new BarcodeValidator().ValidateBarcodeImage(request);
    }

    static BarcodeSymbol CreateCode128Symbol()
    {
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes("MODE");
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "MODE";
        payload.RawBytes = bytes;
        return Code128Layout.CreateSymbol(
            Code128Encoder.EncodeBytes(bytes, null),
            payload,
            null);
    }
}
