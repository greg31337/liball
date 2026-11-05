namespace barcode.Tests;

public class Code128PublicApiTests
{
    [Test]
    public void PublicEncoderAndPureDecoderRoundTripLatin1Bytes()
    {
        BarcodePayload payload = new BarcodePayload();
        payload.RawBytes = new byte[] { 193, 194, 195 };
        BarcodeEncodeRequest encodeRequest = new BarcodeEncodeRequest();
        encodeRequest.Format = BarcodeFormat.Code128;
        encodeRequest.Payload = payload;
        Code128Options code128 = new Code128Options();
        code128.AllowFnc4 = true;
        code128.UseLatin1UpperShift = true;
        encodeRequest.Code128 = code128;

        BarcodeSymbol symbol = new BarcodeEncoder().EncodeBarcode(
            encodeRequest);
        BarcodeDecodeOptions decodeOptions = new BarcodeDecodeOptions();
        decodeOptions.AllowedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.Code128
        };
        BarcodeDecodeRequest decodeRequest = new BarcodeDecodeRequest();
        decodeRequest.Symbol = symbol;
        decodeRequest.Options = decodeOptions;
        BarcodeDecodeResult[] results = new BarcodeDecoder().DecodeBarcode(
            decodeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(symbol.Payload!.RawBytes, Is.EqualTo(
                new byte[] { 193, 194, 195 }));
            Assert.That(symbol.Codewords, Does.Contain(
                Code128Patterns.Function4InCodeSetBCodeword));
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(results[0].Payload!.RawBytes, Is.EqualTo(
                new byte[] { 193, 194, 195 }));
        });
    }

    [Test]
    public void PublicEncoderRejectsOddForcedCodeSetCData()
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "123";
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.Code128;
        request.Payload = payload;
        Code128Options code128 = new Code128Options();
        code128.CodeSet = Code128CodeSet.C;
        request.Code128 = code128;
        IBarcodeEncoder encoder = new BarcodeEncoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => encoder.EncodeBarcode(request),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(encoder.TryEncodeBarcode(request), Is.Null);
        });
    }
}
