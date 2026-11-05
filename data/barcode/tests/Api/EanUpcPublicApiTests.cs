namespace barcode.Tests;

public class EanUpcPublicApiTests
{
    [TestCase(BarcodeFormat.Ean8, "5512345", "55123457", 67, 7)]
    [TestCase(BarcodeFormat.Ean13, "400638133393", "4006381333931", 95, 1)]
    [TestCase(BarcodeFormat.UpcA, "03600029145", "036000291452", 95, 2)]
    [TestCase(BarcodeFormat.UpcE, "123455", "01234558", 51, 8)]
    [TestCase(BarcodeFormat.EanUpcAddon2, "12", "12", 20, 0)]
    [TestCase(BarcodeFormat.EanUpcAddon5, "51234", "51234", 47, 9)]
    public void PublicEncoderAndPureDecoderRoundTripEveryEanUpcCarrier(
        BarcodeFormat format,
        string input,
        string expectedText,
        int expectedModuleCount,
        int expectedChecksum)
    {
        BarcodeEncodeRequest encodeRequest = CreateEncodeRequest(
            format,
            input);

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            encodeRequest);
        BarcodeDecodeResult result = DecodeSymbol(symbol, format);
        int[] expectedDigits = ConvertDigits(expectedText);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(format));
            Assert.That(symbol.WidthModules, Is.EqualTo(expectedModuleCount));
            Assert.That(symbol.ChecksumValue, Is.EqualTo(expectedChecksum));
            Assert.That(symbol.HasChecksum, Is.True);
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo(
                BarcodeSymbologyIdentifiers.EanUpc));
            Assert.That(result.Format, Is.EqualTo(format));
            Assert.That(result.Payload!.Text, Is.EqualTo(expectedText));
            Assert.That(result.Payload.RawBytes, Is.EqualTo(
                global::System.Text.Encoding.ASCII.GetBytes(expectedText)));
            Assert.That(result.Payload.IsGs1, Is.True);
            Assert.That(result.Metadata!.HasChecksum, Is.True);
            Assert.That(result.Metadata.ChecksumValid, Is.True);
            Assert.That(result.Metadata.ChecksumValue, Is.EqualTo(
                expectedChecksum));
            Assert.That(result.Metadata.Codewords, Is.EqualTo(expectedDigits));
            Assert.That(result.Metadata.SymbologyIdentifier, Is.EqualTo(
                BarcodeSymbologyIdentifiers.EanUpc));
        });
    }

    [Test]
    public void PublicUpcACompressionProducesAUpcECarrier()
    {
        BarcodeEncodeRequest request = CreateEncodeRequest(
            BarcodeFormat.UpcA,
            "01234500005");
        EanUpcOptions options = new EanUpcOptions();
        options.CompressToUpcE = true;
        request.EanUpc = options;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(request);
        BarcodeDecodeResult result = DecodeSymbol(
            symbol,
            BarcodeFormat.UpcE);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.UpcE));
            Assert.That(symbol.WidthModules, Is.EqualTo(51));
            Assert.That(result.Format, Is.EqualTo(BarcodeFormat.UpcE));
            Assert.That(result.Payload!.Text, Is.EqualTo("01234558"));
            Assert.That(result.Metadata!.ChecksumValue, Is.EqualTo(8));
            Assert.That(result.Metadata.Codewords, Is.EqualTo(
                ConvertDigits("01234558")));
        });
    }

    [Test]
    public void PublicEan13SupplementCreatesCombinedLayoutAndDecodesMain()
    {
        BarcodeEncodeRequest request = CreateEncodeRequest(
            BarcodeFormat.Ean13,
            "400638133393");
        EanUpcOptions eanUpc = new EanUpcOptions();
        eanUpc.AddOn = "12";
        eanUpc.AllowedAddOnLengths = new int[] { 2 };
        eanUpc.RequireAddOn = true;
        eanUpc.ShowQuietZoneIndicators = true;
        request.EanUpc = eanUpc;
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        request.Options = encodeOptions;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(request);
        BarcodeDecodeResult result = DecodeSymbol(
            symbol,
            BarcodeFormat.Ean13);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Ean13));
            Assert.That(symbol.WidthModules, Is.EqualTo(122));
            Assert.That(symbol.RequiredQuietZone!.RightModules, Is.EqualTo(5));
            Assert.That(symbol.Modules![95..102], Is.All.Zero);
            Assert.That(symbol.Modules[102], Is.EqualTo(1));
            Assert.That(symbol.TextLayout!.Runs,
                Has.Some.Matches<BarcodeTextRun>(run => run.Text == "12"));
            Assert.That(result.Format, Is.EqualTo(BarcodeFormat.Ean13));
            Assert.That(result.Payload!.Text, Is.EqualTo("4006381333931"));
        });
    }

    [TestCase(BarcodeFormat.Ean8, "123456")]
    [TestCase(BarcodeFormat.Ean13, "12345678901")]
    [TestCase(BarcodeFormat.UpcA, "1234567890")]
    [TestCase(BarcodeFormat.UpcE, "12345")]
    [TestCase(BarcodeFormat.EanUpcAddon2, "1")]
    [TestCase(BarcodeFormat.EanUpcAddon5, "1234")]
    public void PublicEncoderRejectsMalformedEanUpcPayloads(
        BarcodeFormat format,
        string text)
    {
        BarcodeEncodeRequest request = CreateEncodeRequest(format, text);
        IBarcodeEncoder encoder = new BarcodeEncoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => encoder.EncodeBarcode(request),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(encoder.TryEncodeBarcode(request), Is.Null);
        });
    }

    static BarcodeEncodeRequest CreateEncodeRequest(
        BarcodeFormat format,
        string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = format;
        request.Payload = payload;
        return request;
    }

    static BarcodeDecodeResult DecodeSymbol(
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { format };
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Symbol = symbol;
        request.Options = options;
        BarcodeDecodeResult[] results = new BarcodeDecoder().DecodeBarcode(
            request);
        Assert.That(results, Has.Length.EqualTo(1));
        return results[0];
    }

    static int[] ConvertDigits(string text)
    {
        int[] digits = new int[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            digits[index] = text[index] - '0';
            index = index + 1;
        }

        return digits;
    }
}
