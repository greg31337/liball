namespace barcode.Tests;

public class ItfPublicApiTests
{
    [Test]
    public void PublicItfChecksumRoundTripHonoursRatioAndLengthPolicy()
    {
        BarcodeEncodeRequest request = CreateRequest(
            BarcodeFormat.Itf,
            "12345");
        ItfOptions itf = new ItfOptions();
        itf.WideToNarrowRatio = 2.0;
        itf.AllowedLengths = new int[] { 6 };
        itf.ChecksumMode = BarcodeChecksumMode.Calculate;
        request.Itf = itf;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(request);
        BarcodeDecodeResult result = DecodeSymbol(
            symbol,
            BarcodeFormat.Itf,
            BarcodeChecksumMode.VerifyStrip,
            new int[] { 5 });
        BarcodeDecodeAttempt encodedLengthAttempt =
            new BarcodeDecoder().TryDecodeBarcode(CreateDecodeRequest(
                symbol,
                BarcodeFormat.Itf,
                BarcodeChecksumMode.VerifyStrip,
                new int[] { 6 }));

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Itf));
            Assert.That(symbol.WidthModules, Is.EqualTo(50));
            Assert.That(symbol.Size!.WidthModules, Is.EqualTo(70));
            Assert.That(symbol.Codewords, Is.EqualTo(
                ConvertDigits("123457")));
            Assert.That(symbol.HasChecksum, Is.True);
            Assert.That(symbol.ChecksumValue, Is.EqualTo(7));
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo("]I1"));
            Assert.That(result.Payload!.Text, Is.EqualTo("12345"));
            Assert.That(result.Metadata!.HasChecksum, Is.True);
            Assert.That(result.Metadata.ChecksumValid, Is.True);
            Assert.That(result.Metadata.ChecksumValue, Is.EqualTo(7));
            Assert.That(result.Metadata.SymbologyIdentifier,
                Is.EqualTo("]I3"));
            Assert.That(encodedLengthAttempt.Success, Is.False);
            Assert.That(encodedLengthAttempt.Results, Is.Empty);
        });
    }

    [Test]
    public void PublicItf14NormalizesGtinAndRoundTripsBearerBoxCarrier()
    {
        BarcodeEncodeRequest request = CreateRequest(
            BarcodeFormat.Itf14,
            "0950110153001");
        ItfOptions itf = new ItfOptions();
        itf.UseBearerBox = true;
        request.Itf = itf;
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        request.Options = encodeOptions;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(request);
        BarcodeDecodeResult result = DecodeSymbol(
            symbol,
            BarcodeFormat.Itf14,
            BarcodeChecksumMode.Require,
            new int[] { 14 });

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Itf14));
            Assert.That(symbol.WidthModules, Is.EqualTo(121));
            Assert.That(symbol.Modules, Is.Null);
            Assert.That(symbol.Codewords, Is.EqualTo(
                ConvertDigits("09501101530010")));
            Assert.That(symbol.Payload!.Text, Is.EqualTo("09501101530010"));
            Assert.That(symbol.Payload.IsGs1, Is.True);
            Assert.That(symbol.BearerBars, Has.Length.EqualTo(4));
            Assert.That(symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(symbol.TextLayout!.Runs![0].Text,
                Is.EqualTo("0 95 01101 53001 0"));
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo(
                BarcodeSymbologyIdentifiers.Itf14));
            Assert.That(result.Format, Is.EqualTo(BarcodeFormat.Itf14));
            Assert.That(result.Payload!.Text, Is.EqualTo("09501101530010"));
            Assert.That(result.Payload.IsGs1, Is.True);
            Assert.That(result.Metadata!.IsGs1, Is.True);
            Assert.That(result.Metadata.ChecksumValid, Is.True);
            Assert.That(result.Metadata.SymbologyIdentifier, Is.EqualTo("]I1"));
        });
    }

    [Test]
    public void PublicEncoderRejectsMalformedItfDataAndProfileOptions()
    {
        BarcodeEncodeRequest oddCarrier = CreateRequest(
            BarcodeFormat.Itf,
            "123");
        BarcodeEncodeRequest nonNumeric = CreateRequest(
            BarcodeFormat.Itf,
            "12A4");

        BarcodeEncodeRequest rejectedLength = CreateRequest(
            BarcodeFormat.Itf,
            "1234");
        ItfOptions length = new ItfOptions();
        length.AllowedLengths = new int[] { 6 };
        rejectedLength.Itf = length;

        BarcodeEncodeRequest wrongGtinCheck = CreateRequest(
            BarcodeFormat.Itf14,
            "09501101530011");

        BarcodeEncodeRequest invalidProfileRatio = CreateRequest(
            BarcodeFormat.Itf14,
            "0950110153001");
        ItfOptions ratio = new ItfOptions();
        ratio.WideToNarrowRatio = 2.0;
        invalidProfileRatio.Itf = ratio;

        BarcodeEncodeRequest weakenedProfileLength = CreateRequest(
            BarcodeFormat.Itf14,
            "0950110153001");
        ItfOptions weakened = new ItfOptions();
        weakened.AllowedLengths = new int[] { 12, 14 };
        weakenedProfileLength.Itf = weakened;

        IBarcodeEncoder encoder = new BarcodeEncoder();
        BarcodeEncodeRequest[] invalidRequests = new BarcodeEncodeRequest[]
        {
            oddCarrier,
            nonNumeric,
            rejectedLength,
            wrongGtinCheck,
            invalidProfileRatio,
            weakenedProfileLength
        };
        int index = 0;
        while (index < invalidRequests.Length)
        {
            BarcodeEncodeRequest invalid = invalidRequests[index];
            Assert.Multiple(() =>
            {
                Assert.That(
                    () => encoder.EncodeBarcode(invalid),
                    Throws.TypeOf<BarcodeEncodingException>());
                Assert.That(encoder.TryEncodeBarcode(invalid), Is.Null);
            });
            index = index + 1;
        }
    }

    [Test]
    public void PublicDecoderRejectsDamagedAndPartialPureItfCarriers()
    {
        BarcodeSymbol damaged = new BarcodeEncoder().EncodeBarcode(
            CreateRequest(BarcodeFormat.Itf, "1234"));
        damaged.Modules![0] = 0;
        BarcodeDecodeRequest damagedRequest = CreateDecodeRequest(
            damaged,
            BarcodeFormat.Itf,
            BarcodeChecksumMode.None,
            new int[] { 4 });

        BarcodeSymbol partial = new BarcodeEncoder().EncodeBarcode(
            CreateRequest(BarcodeFormat.Itf14, "0950110153001"));
        partial.Codewords = ConvertDigits("095011015300");
        BarcodeDecodeRequest partialRequest = CreateDecodeRequest(
            partial,
            BarcodeFormat.Itf14,
            BarcodeChecksumMode.Require,
            new int[] { 14 });
        IBarcodeDecoder decoder = new BarcodeDecoder();

        BarcodeDecodeAttempt damagedAttempt = decoder.TryDecodeBarcode(
            damagedRequest);
        BarcodeDecodeAttempt partialAttempt = decoder.TryDecodeBarcode(
            partialRequest);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => decoder.DecodeBarcode(damagedRequest),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(damagedAttempt.Success, Is.False);
            Assert.That(damagedAttempt.Results, Is.Empty);
            Assert.That(partialAttempt.Success, Is.False);
            Assert.That(partialAttempt.Results, Is.Empty);
            Assert.That(partialAttempt.FailureCode, Is.Not.Null.And.Not.Empty);
        });
    }

    static BarcodeEncodeRequest CreateRequest(
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
        BarcodeFormat format,
        BarcodeChecksumMode checksumMode,
        int[] allowedLengths)
    {
        BarcodeDecodeRequest request = CreateDecodeRequest(
            symbol,
            format,
            checksumMode,
            allowedLengths);
        BarcodeDecodeResult[] results = new BarcodeDecoder().DecodeBarcode(
            request);
        Assert.That(results, Has.Length.EqualTo(1));
        return results[0];
    }

    static BarcodeDecodeRequest CreateDecodeRequest(
        BarcodeSymbol symbol,
        BarcodeFormat format,
        BarcodeChecksumMode checksumMode,
        int[] allowedLengths)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { format };
        options.AllowedLengths = allowedLengths;
        options.ChecksumMode = checksumMode;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
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
