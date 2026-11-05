namespace barcode.Tests;

public class CodabarPublicApiTests
{
    [Test]
    public void PublicRoundTripPreservesTheCompleteCodabarDataAlphabet()
    {
        const string text = "0123456789-$:/.+";
        BarcodeEncodeRequest request = CreateRequest(text);

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(request);
        BarcodeDecodeResult result = DecodeSymbol(
            symbol,
            BarcodeChecksumMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Codabar));
            Assert.That(symbol.Modules, Has.Length.EqualTo(
                symbol.WidthModules));
            Assert.That(symbol.RequiredQuietZone!.LeftModules, Is.EqualTo(10));
            Assert.That(symbol.RequiredQuietZone.RightModules, Is.EqualTo(10));
            Assert.That(symbol.Codewords![0], Is.EqualTo(16));
            Assert.That(symbol.Codewords[^1], Is.EqualTo(16));
            Assert.That(symbol.HasChecksum, Is.False);
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo(
                BarcodeSymbologyIdentifiers.Codabar));
            Assert.That(result.Payload!.Text, Is.EqualTo(text));
            Assert.That(result.Payload.RawBytes, Is.EqualTo(
                global::System.Text.Encoding.ASCII.GetBytes(text)));
            Assert.That(result.Metadata!.HasChecksum, Is.False);
            Assert.That(result.Metadata.SymbologyIdentifier,
                Is.EqualTo(BarcodeSymbologyIdentifiers.Codabar));
        });
    }

    [Test]
    public void PublicAliasChecksumRoundTripUsesCanonicalCarrierPatterns()
    {
        BarcodeEncodeRequest request = CreateRequest("12");
        CodabarOptions codabar = new CodabarOptions();
        codabar.StartCharacter = "T";
        codabar.StopCharacter = "E";
        codabar.AcceptAliases = true;
        codabar.TransmitStartStop = true;
        codabar.TransmitCheckCharacter = true;
        codabar.ChecksumMode = BarcodeChecksumMode.Calculate;
        codabar.WideToNarrowRatio = 3.0;
        request.Codabar = codabar;
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        request.Options = encodeOptions;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(request);
        BarcodeDecodeResult result = DecodeSymbol(
            symbol,
            BarcodeChecksumMode.VerifyStrip);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Modules, Has.Length.EqualTo(
                symbol.WidthModules));
            Assert.That(symbol.Codewords, Is.EqualTo(
                new int[] { 16, 1, 2, 10, 19 }));
            Assert.That(symbol.TextLayout!.Runs![0].Text,
                Is.EqualTo("T12-E"));
            Assert.That(symbol.HasChecksum, Is.True);
            Assert.That(symbol.ChecksumValue, Is.EqualTo(10));
            Assert.That(result.Payload!.Text, Is.EqualTo("12"));
            Assert.That(result.Metadata!.HasChecksum, Is.True);
            Assert.That(result.Metadata.ChecksumValid, Is.True);
            Assert.That(result.Metadata.ChecksumValue, Is.EqualTo(10));
        });
    }

    [Test]
    public void PublicEncoderRejectsMalformedDataTerminalsChecksAndGeometry()
    {
        BarcodeEncodeRequest invalidData = CreateRequest("12_3");

        BarcodeEncodeRequest disabledAlias = CreateRequest("12");
        CodabarOptions alias = new CodabarOptions();
        alias.StartCharacter = "T";
        disabledAlias.Codabar = alias;

        BarcodeEncodeRequest conflictingFrame = CreateRequest("A12B");
        CodabarOptions conflict = new CodabarOptions();
        conflict.StartCharacter = "C";
        conflictingFrame.Codabar = conflict;

        BarcodeEncodeRequest wrongCheck = CreateRequest("A12346B");
        CodabarOptions check = new CodabarOptions();
        check.ChecksumMode = BarcodeChecksumMode.Require;
        wrongCheck.Codabar = check;

        BarcodeEncodeRequest invalidRatio = CreateRequest("12");
        CodabarOptions ratio = new CodabarOptions();
        ratio.WideToNarrowRatio = 1.99;
        invalidRatio.Codabar = ratio;

        BarcodeEncodeRequest invalidGap = CreateRequest("12");
        CodabarOptions gap = new CodabarOptions();
        gap.IntercharacterGapModules = -1;
        invalidGap.Codabar = gap;

        BarcodeEncodeRequest[] invalidRequests = new BarcodeEncodeRequest[]
        {
            invalidData,
            disabledAlias,
            conflictingFrame,
            wrongCheck,
            invalidRatio,
            invalidGap
        };
        IBarcodeEncoder encoder = new BarcodeEncoder();
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
    public void PublicDecoderRejectsDamagedPureCodabarCarrier()
    {
        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            CreateRequest("1234"));
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
        request.Format = BarcodeFormat.Codabar;
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
            BarcodeFormat.Codabar
        };
        options.ChecksumMode = checksumMode;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }
}
