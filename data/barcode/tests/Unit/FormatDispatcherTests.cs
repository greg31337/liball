namespace barcode.Tests;

public class FormatDispatcherTests
{
    [TestCase(BarcodeFormat.Code128, 0, BarcodeDimension.Linear)]
    [TestCase(BarcodeFormat.Gs1128, 0, BarcodeDimension.Linear)]
    [TestCase(BarcodeFormat.Ean13, 1, BarcodeDimension.Linear)]
    [TestCase(BarcodeFormat.Code39, 2, BarcodeDimension.Linear)]
    [TestCase(BarcodeFormat.Itf14, 3, BarcodeDimension.Linear)]
    [TestCase(BarcodeFormat.Codabar, 4, BarcodeDimension.Linear)]
    [TestCase(BarcodeFormat.DataMatrix, 5, BarcodeDimension.Matrix)]
    [TestCase(BarcodeFormat.Pdf417, 6, BarcodeDimension.StackedLinear)]
    public void SupportedFormatsResolveToCodecFamilyAndDimension(
        BarcodeFormat format,
        int family,
        BarcodeDimension dimension)
    {
        Assert.Multiple(() =>
        {
            Assert.That(FormatDispatcher.IsSupportedFormat(format), Is.True);
            Assert.That((int)FormatDispatcher.ResolveCodecFamily(format), Is.EqualTo(family));
            Assert.That(FormatDispatcher.ResolveDimension(format), Is.EqualTo(dimension));
        });
    }

    [TestCase(BarcodeFormat.Automatic)]
    [TestCase((BarcodeFormat)999)]
    public void NonConcreteFormatsAreRejected(BarcodeFormat format)
    {
        Assert.Multiple(() =>
        {
            Assert.That(FormatDispatcher.IsSupportedFormat(format), Is.False);
            Assert.That(() => FormatDispatcher.ResolveCodecFamily(format), Throws.TypeOf<UnsupportedBarcodeFormatException>());
            Assert.That(() => FormatDispatcher.ResolveDimension(format), Throws.TypeOf<UnsupportedBarcodeFormatException>());
        });
    }

    [TestCase(false, false, BarcodeFormat.Code128)]
    [TestCase(true, false, BarcodeFormat.DataMatrix)]
    [TestCase(false, true, BarcodeFormat.Gs1128)]
    [TestCase(true, true, BarcodeFormat.Gs1DataMatrix)]
    public void AutomaticEncodingSelectionUsesPayloadSemantics(bool hasBytes, bool isGs1, BarcodeFormat expected)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "ABC";
        payload.IsGs1 = isGs1;
        if (hasBytes)
        {
            payload.RawBytes = new byte[] { 1, 2 };
        }

        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.Automatic;
        request.Payload = payload;

        Assert.That(FormatDispatcher.ResolveEncodingFormat(request), Is.EqualTo(expected));
    }

    [Test]
    public void ExplicitEncodingSelectionIsPreserved()
    {
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.CompactPdf417;

        Assert.That(FormatDispatcher.ResolveEncodingFormat(request), Is.EqualTo(BarcodeFormat.CompactPdf417));
    }

    [Test]
    public void EncodingSelectionRejectsMissingRequestPayloadAndUnsupportedFormat()
    {
        BarcodeEncodeRequest missingPayload = new BarcodeEncodeRequest();
        missingPayload.Format = BarcodeFormat.Automatic;
        BarcodeEncodeRequest unsupported = new BarcodeEncodeRequest();
        unsupported.Format = (BarcodeFormat)999;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => FormatDispatcher.ResolveEncodingFormat(null!),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => FormatDispatcher.ResolveEncodingFormat(missingPayload),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => FormatDispatcher.ResolveEncodingFormat(unsupported),
                Throws.TypeOf<UnsupportedBarcodeFormatException>());
        });
    }

    [Test]
    public void DecodeDefaultsExpandToEveryConcreteFormat()
    {
        BarcodeFormat[] formats = FormatDispatcher.ResolveDecodingFormats(null);

        Assert.That(formats, Is.EqualTo(new BarcodeFormat[]
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
        }));
    }

    [Test]
    public void DecodeSelectionExpandsAutomaticAndDeduplicatesExplicitFormats()
    {
        BarcodeDecodeOptions automatic = new BarcodeDecodeOptions();
        automatic.AllowedFormats = new BarcodeFormat[] { BarcodeFormat.Code39, BarcodeFormat.Automatic };
        BarcodeDecodeOptions explicitOptions = new BarcodeDecodeOptions();
        explicitOptions.AllowedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.Pdf417,
            BarcodeFormat.Code128,
            BarcodeFormat.Pdf417,
            BarcodeFormat.DataMatrix
        };

        BarcodeFormat[] expanded = FormatDispatcher.ResolveDecodingFormats(automatic);
        BarcodeFormat[] selected = FormatDispatcher.ResolveDecodingFormats(explicitOptions);
        explicitOptions.AllowedFormats[0] = BarcodeFormat.Code39;

        Assert.Multiple(() =>
        {
            Assert.That(expanded.Length, Is.EqualTo(17));
            Assert.That(selected, Is.EqualTo(new BarcodeFormat[]
            {
                BarcodeFormat.Pdf417,
                BarcodeFormat.Code128,
                BarcodeFormat.DataMatrix
            }));
        });
    }

    [Test]
    public void DecodeSelectionRejectsUnsupportedFormat()
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { (BarcodeFormat)999 };

        Assert.That(
            () => FormatDispatcher.ResolveDecodingFormats(options),
            Throws.TypeOf<UnsupportedBarcodeFormatException>());
    }
}
