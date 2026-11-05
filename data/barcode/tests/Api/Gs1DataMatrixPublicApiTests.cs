namespace barcode.Tests;

public class Gs1DataMatrixPublicApiTests
{
    [Test]
    public void PublicEncoderAndDecoderPreserveProfileHriIdentifierAndElementString()
    {
        string elementString = "010950600013435210LOT\u001d21SERIAL";
        BarcodePayload payload = CreatePayload(elementString);
        payload.IsGs1 = true;
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        BarcodeEncodeRequest encodeRequest = new BarcodeEncodeRequest();
        encodeRequest.Format = BarcodeFormat.Gs1DataMatrix;
        encodeRequest.Payload = payload;
        encodeRequest.Options = encodeOptions;
        IBarcodeEncoder encoder = new BarcodeEncoder();

        BarcodeSymbol symbol = encoder.EncodeBarcode(encodeRequest);
        BarcodeDecodeRequest decodeRequest = CreatePureDecodeRequest(symbol);
        BarcodeDecodeResult[] decoded = new BarcodeDecoder().DecodeBarcode(
            decodeRequest);

        Assert.That(decoded, Has.Length.EqualTo(1));
        BarcodePayload decodedPayload = decoded[0].Payload!;
        BarcodeDecodeMetadata decodedMetadata = decoded[0].Metadata!;
        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo(BarcodeSymbologyIdentifiers.Gs1DataMatrix));
            Assert.That(symbol.Payload!.Text, Is.EqualTo(elementString));
            Assert.That(symbol.Payload.RawBytes, Is.EqualTo(global::System.Text.Encoding.ASCII.GetBytes(elementString)));
            Assert.That(symbol.Payload.CharacterEncoding, Is.EqualTo("ISO/IEC 646"));
            Assert.That(symbol.Payload.IsGs1, Is.True);
            Assert.That(symbol.TextLayout!.Runs![0].Text, Is.EqualTo("(01)09506000134352(10)LOT(21)SERIAL"));
            Assert.That(symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(Gs1DataMatrixCodec.NominalModuleSizeMillimetres));
            Assert.That(decoded[0].Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(decodedPayload.RawBytes, Is.EqualTo(symbol.Payload.RawBytes));
            Assert.That(decodedPayload.Text, Is.EqualTo(elementString));
            Assert.That(decodedPayload.IsGs1, Is.True);
            Assert.That(decodedMetadata.IsGs1, Is.True);
            Assert.That(decodedMetadata.SymbologyIdentifier, Is.EqualTo(BarcodeSymbologyIdentifiers.Gs1DataMatrix));
            Assert.That(decodedMetadata.Gs1ElementString, Is.EqualTo(elementString));
        });
    }

    [Test]
    public void PublicApiRejectsUnknownAiInputAndMalformedFnc1Payloads()
    {
        BarcodeEncodeRequest invalidEncode = new BarcodeEncodeRequest();
        invalidEncode.Format = BarcodeFormat.Gs1DataMatrix;
        invalidEncode.Payload = CreatePayload("04ABC");
        invalidEncode.Payload.IsGs1 = true;
        IBarcodeEncoder encoder = new BarcodeEncoder();

        byte[] malformedBytes = global::System.Text.Encoding.ASCII.GetBytes("04ABC");
        DataMatrixEncodingResult malformed = DataMatrixEncoder.Encode(
            malformedBytes,
            CreatePayload("04ABC"),
            null,
            null,
            true);
        BarcodeDecodeRequest decodeRequest = CreatePureDecodeRequest(
            malformed.Symbol);
        IBarcodeDecoder decoder = new BarcodeDecoder();
        BarcodeDecodeAttempt attempt = decoder.TryDecodeBarcode(decodeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(() => encoder.EncodeBarcode(invalidEncode), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(encoder.TryEncodeBarcode(invalidEncode), Is.Null);
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.FailureCode, Is.EqualTo("PURE_SYMBOL_NOT_DECODED"));
            Assert.That(attempt.Results, Is.Empty);
            Assert.That(() => decoder.DecodeBarcode(decodeRequest), Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    static BarcodeDecodeRequest CreatePureDecodeRequest(BarcodeSymbol symbol)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { BarcodeFormat.Gs1DataMatrix };
        options.PureSymbol = true;
        options.ParseGs1 = true;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }

    static BarcodePayload CreatePayload(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        return payload;
    }
}
