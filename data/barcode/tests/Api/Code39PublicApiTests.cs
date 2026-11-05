namespace barcode.Tests;

public class Code39PublicApiTests
{
    [Test]
    public void PublicStandardRoundTripHonoursThreeToOneRatioAndLayout()
    {
        BarcodeEncodeRequest request = CreateRequest("CODE39");
        Code39Options code39 = new Code39Options();
        code39.CharacterMode = Code39CharacterMode.Standard;
        code39.WideToNarrowRatio = 3.0;
        request.Code39 = code39;
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        request.Options = encodeOptions;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(request);
        BarcodeDecodeResult result = DecodeSymbol(
            symbol,
            BarcodeChecksumMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Code39));
            Assert.That(symbol.WidthModules, Is.EqualTo(127));
            Assert.That(symbol.Size!.WidthModules, Is.EqualTo(147));
            Assert.That(symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(symbol.TextLayout!.Runs![0].Text, Is.EqualTo("CODE39"));
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo(
                BarcodeSymbologyIdentifiers.Code39));
            Assert.That(symbol.HasChecksum, Is.False);
            Assert.That(result.Format, Is.EqualTo(BarcodeFormat.Code39));
            Assert.That(result.Payload!.Text, Is.EqualTo("CODE39"));
            Assert.That(result.Payload.RawBytes, Is.EqualTo(
                global::System.Text.Encoding.ASCII.GetBytes("CODE39")));
            Assert.That(result.Metadata!.HasChecksum, Is.False);
            Assert.That(result.Metadata.SymbologyIdentifier,
                Is.EqualTo("]A4"));
        });
    }

    [Test]
    public void PublicFullAsciiChecksumRoundTripRestoresControlCharacters()
    {
        string text = "a\0z";
        BarcodeEncodeRequest request = CreateRequest(text);
        Code39Options code39 = new Code39Options();
        code39.CharacterMode = Code39CharacterMode.FullAscii;
        code39.ChecksumMode = BarcodeChecksumMode.Calculate;
        request.Code39 = code39;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(request);
        BarcodeDecodeResult result = DecodeSymbol(
            symbol,
            BarcodeChecksumMode.VerifyStrip);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.HasChecksum, Is.True);
            Assert.That(symbol.Codewords, Has.Length.GreaterThan(text.Length));
            Assert.That(result.Payload!.Text, Is.EqualTo(text));
            Assert.That(result.Payload.RawBytes, Is.EqualTo(
                new byte[] { 97, 0, 122 }));
            Assert.That(result.Metadata!.HasChecksum, Is.True);
            Assert.That(result.Metadata.ChecksumValid, Is.True);
            Assert.That(result.Metadata.ChecksumValue, Is.EqualTo(
                symbol.ChecksumValue));
            Assert.That(result.Metadata.SymbologyIdentifier,
                Is.EqualTo("]A7"));
        });
    }

    [Test]
    public void PublicEncoderRejectsMalformedTextChecksAndGeometryOptions()
    {
        BarcodeEncodeRequest standardLowercase = CreateRequest("lower");

        BarcodeEncodeRequest nonAscii = CreateRequest("caf\u00e9");
        Code39Options fullAscii = new Code39Options();
        fullAscii.CharacterMode = Code39CharacterMode.FullAscii;
        nonAscii.Code39 = fullAscii;

        BarcodeEncodeRequest wrongCheck = CreateRequest("CODE39X");
        Code39Options verify = new Code39Options();
        verify.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        wrongCheck.Code39 = verify;

        BarcodeEncodeRequest invalidRatio = CreateRequest("A");
        Code39Options ratio = new Code39Options();
        ratio.WideToNarrowRatio = 1.99;
        invalidRatio.Code39 = ratio;

        BarcodeEncodeRequest invalidGap = CreateRequest("A");
        Code39Options gap = new Code39Options();
        gap.IntercharacterGapModules = -1;
        invalidGap.Code39 = gap;

        IBarcodeEncoder encoder = new BarcodeEncoder();
        BarcodeEncodeRequest[] invalidRequests = new BarcodeEncodeRequest[]
        {
            standardLowercase,
            nonAscii,
            wrongCheck,
            invalidRatio,
            invalidGap
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
    public void PublicDecoderReportsDamagedPureCode39WithoutThrowingFromTry()
    {
        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            CreateRequest("DAMAGE"));
        symbol.Modules![0] = 0;
        BarcodeDecodeRequest request = CreateDecodeRequest(
            symbol,
            BarcodeChecksumMode.None);
        IBarcodeDecoder decoder = new BarcodeDecoder();

        BarcodeDecodeAttempt attempt = decoder.TryDecodeBarcode(request);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => decoder.DecodeBarcode(request),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.Results, Is.Empty);
            Assert.That(attempt.FailureCode, Is.Not.Null.And.Not.Empty);
        });
    }

    static BarcodeEncodeRequest CreateRequest(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.Code39;
        request.Payload = payload;
        return request;
    }

    static BarcodeDecodeResult DecodeSymbol(
        BarcodeSymbol symbol,
        BarcodeChecksumMode checksumMode)
    {
        BarcodeDecodeRequest request = CreateDecodeRequest(
            symbol,
            checksumMode);
        BarcodeDecodeResult[] results = new BarcodeDecoder().DecodeBarcode(
            request);
        Assert.That(results, Has.Length.EqualTo(1));
        return results[0];
    }

    static BarcodeDecodeRequest CreateDecodeRequest(
        BarcodeSymbol symbol,
        BarcodeChecksumMode checksumMode)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.Code39
        };
        options.ChecksumMode = checksumMode;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }
}
