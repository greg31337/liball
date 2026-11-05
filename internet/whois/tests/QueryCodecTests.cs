using whois;

namespace whois.Tests;

public class QueryCodecTests
{
    [Test]
    public void EncodesAsciiAndAppendsExactlyOneCrlf()
    {
        IWhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = "example.com";

        byte[] payload = codec.EncodeQuery(request);

        Assert.That(payload, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("example.com\r\n")));
        Assert.That(payload[^2], Is.EqualTo(13));
        Assert.That(payload[^1], Is.EqualTo(10));
    }

    [Test]
    public void EncodesUtf8WithoutAddingAPreamble()
    {
        WhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = "café.example";
        request.Encoding.RequestEncoding = System.Text.Encoding.UTF8;

        byte[] payload = codec.EncodeQuery(request);

        Assert.That(payload, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("café.example\r\n")));
        Assert.That(payload[0], Is.EqualTo(99));
    }

    [Test]
    public void EncodesWithAlternateBuiltInEncodingBeforeAsciiFraming()
    {
        WhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = "Aé";
        request.Encoding.RequestEncoding = System.Text.Encoding.Unicode;
        request.Limits.MaximumEncodedQueryBytes = 4;

        byte[] payload = codec.EncodeQuery(request);

        Assert.That(payload, Is.EqualTo(new byte[] { 65, 0, 233, 0, 13, 10 }));
    }

    [Test]
    public void AcceptsQueryAtEncodedByteLimit()
    {
        WhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = "four";
        request.Limits.MaximumEncodedQueryBytes = 4;

        byte[] payload = codec.EncodeQuery(request);

        Assert.That(payload, Is.EqualTo(new byte[] { 102, 111, 117, 114, 13, 10 }));
    }

    [Test]
    public void RejectsQueryBeyondEncodedByteLimit()
    {
        WhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = "four";
        request.Limits.MaximumEncodedQueryBytes = 3;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => codec.EncodeQuery(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Limits.MaximumEncodedQueryBytes"));
        Assert.That(exception.QueryText, Is.EqualTo("four"));
    }

    [Test]
    public void AcceptsMultiByteQueryAtExactEncodedByteLimit()
    {
        WhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = "é";
        request.Encoding.RequestEncoding = System.Text.Encoding.UTF8;
        request.Limits.MaximumEncodedQueryBytes = 2;

        byte[] payload = codec.EncodeQuery(request);

        Assert.That(payload, Is.EqualTo(new byte[] { 195, 169, 13, 10 }));
    }

    [Test]
    public void RejectsLimitThatWouldSplitMultiByteCharacter()
    {
        WhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = "é";
        request.Encoding.RequestEncoding = System.Text.Encoding.UTF8;
        request.Limits.MaximumEncodedQueryBytes = 1;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => codec.EncodeQuery(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Limits.MaximumEncodedQueryBytes"));
        Assert.That(exception.QueryText, Is.EqualTo("é"));
    }

    [TestCase("example.com\r")]
    [TestCase("example.com\n")]
    [TestCase("example.com\r\n")]
    [TestCase("example.com\0ignored")]
    public void RejectsInjectedTerminatorsAndControlCharacters(string queryText)
    {
        WhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = queryText;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => codec.EncodeQuery(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(queryText));
    }

    [Test]
    public void RejectsNonEncodableTextDespitePermissiveCallerFallback()
    {
        WhoisQueryCodec codec = new WhoisQueryCodec();
        WhoisRequest request = new WhoisRequest();
        request.QueryText = "café";
        request.Encoding.RequestEncoding = System.Text.Encoding.GetEncoding(20127, new System.Text.EncoderReplacementFallback("?"), System.Text.DecoderFallback.ReplacementFallback);

        Assert.Throws<System.Text.EncoderFallbackException>(() => codec.EncodeQuery(request));
    }
}
