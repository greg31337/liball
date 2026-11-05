namespace barcode.Tests;

public class Gs1128PublicApiTests
{
    [Test]
    public void PublicEncoderAndPureDecoderRoundTripOrderedGs1Data()
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "10LOT123\u001d172601010109506000134352";
        payload.IsGs1 = true;
        BarcodeEncodeRequest encodeRequest = new BarcodeEncodeRequest();
        encodeRequest.Format = BarcodeFormat.Gs1128;
        encodeRequest.Payload = payload;
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;
        encodeRequest.Options = encodeOptions;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            encodeRequest);
        BarcodeDecodeOptions decodeOptions = new BarcodeDecodeOptions();
        decodeOptions.AllowedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.Gs1128
        };
        BarcodeDecodeRequest decodeRequest = new BarcodeDecodeRequest();
        decodeRequest.Symbol = symbol;
        decodeRequest.Options = decodeOptions;
        BarcodeDecodeResult[] results = new BarcodeDecoder().DecodeBarcode(
            decodeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Gs1128));
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo(
                BarcodeSymbologyIdentifiers.Gs1128));
            Assert.That(symbol.Payload!.Text, Is.EqualTo(
                "01095060001343521726010110LOT123"));
            Assert.That(symbol.Payload.IsGs1, Is.True);
            Assert.That(symbol.TextLayout!.Runs![0].Text, Is.EqualTo(
                "(01)09506000134352(17)260101(10)LOT123"));
            Assert.That(symbol.Codewords![1], Is.EqualTo(
                Code128Patterns.Function1Codeword));
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Format, Is.EqualTo(
                BarcodeFormat.Gs1128));
            Assert.That(results[0].Payload!.Text, Is.EqualTo(
                symbol.Payload.Text));
            Assert.That(results[0].Metadata!.SymbologyIdentifier, Is.EqualTo(
                BarcodeSymbologyIdentifiers.Gs1128));
            Assert.That(results[0].Metadata!.IsGs1, Is.True);
            Assert.That(results[0].Metadata!.Gs1ElementString, Is.EqualTo(
                symbol.Payload.Text));
        });
    }

    [Test]
    public void PublicEncoderReportsMalformedAndOverCapacityElementStrings()
    {
        BarcodeEncodeRequest malformed = CreateRequest(
            "10ABCDEFGHIJKLMNOPQRST17260101");
        BarcodeEncodeRequest excessive = CreateRequest(
            "10ABCDEFGHIJKLMNOPQRST\u001d21ABCDEFGHIJKLMNOPQRST\u001d17260101");
        IBarcodeEncoder encoder = new BarcodeEncoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => encoder.EncodeBarcode(malformed),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(encoder.TryEncodeBarcode(malformed), Is.Null);
            Assert.That(
                () => encoder.EncodeBarcode(excessive),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(encoder.TryEncodeBarcode(excessive), Is.Null);
        });
    }

    static BarcodeEncodeRequest CreateRequest(string elementString)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = elementString;
        payload.IsGs1 = true;
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.Gs1128;
        request.Payload = payload;
        return request;
    }
}
