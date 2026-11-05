using System.Text;
using websocket;

namespace websocket.Tests;

public class Base64CodecTests
{
    [TestCase("", "")]
    [TestCase("f", "Zg==")]
    [TestCase("fo", "Zm8=")]
    [TestCase("foo", "Zm9v")]
    [TestCase("foob", "Zm9vYg==")]
    [TestCase("fooba", "Zm9vYmE=")]
    [TestCase("foobar", "Zm9vYmFy")]
    public void CanonicalEncodeMatchesRfc4648Vectors(string plainText, string expectedText)
    {
        var source = Encoding.ASCII.GetBytes(plainText);
        var destination = Enumerable.Repeat('?', expectedText.Length + 1).ToArray();

        var result = Base64Codec.EncodeCanonical(source, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ProducedCount, Is.EqualTo(expectedText.Length));
            Assert.That(new string(destination, 0, result.ProducedCount), Is.EqualTo(expectedText));
            Assert.That(destination[expectedText.Length], Is.EqualTo('?'));
        });
    }

    [TestCase("", "")]
    [TestCase("Zg==", "f")]
    [TestCase("Zm8=", "fo")]
    [TestCase("Zm9v", "foo")]
    [TestCase("Zm9vYg==", "foob")]
    [TestCase("Zm9vYmE=", "fooba")]
    [TestCase("Zm9vYmFy", "foobar")]
    public void CanonicalDecodeMatchesRfc4648Vectors(string encodedText, string expectedText)
    {
        var destination = Enumerable.Repeat((byte)0xAA, expectedText.Length + 1).ToArray();

        var result = Base64Codec.DecodeCanonical(encodedText, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(encodedText.Length));
            Assert.That(result.ProducedCount, Is.EqualTo(expectedText.Length));
            Assert.That(Encoding.ASCII.GetString(destination, 0, result.ProducedCount), Is.EqualTo(expectedText));
            Assert.That(destination[expectedText.Length], Is.EqualTo(0xAA));
        });
    }

    [TestCase("A")]
    [TestCase("AAA")]
    [TestCase("Zg")]
    [TestCase("Zg===")]
    [TestCase("Zg=A")]
    [TestCase("A=AA")]
    [TestCase("A===")]
    [TestCase("====")]
    [TestCase("Zm$v")]
    [TestCase("____")]
    [TestCase("Zg==\r\n")]
    [TestCase("Zh==")]
    [TestCase("Zm9=")]
    public void CanonicalDecodeRejectsMalformedOrNonCanonicalText(string encodedText)
    {
        var destination = Enumerable.Repeat((byte)0xAA, 32).ToArray();

        var result = Base64Codec.DecodeCanonical(encodedText, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, 32)));
        });
    }

    [Test]
    public void EncodeReportsCapacityWithoutPartiallyWriting()
    {
        var destination = new[] { '?', '?', '?' };

        var result = Base64Codec.EncodeCanonical(new byte[] { 1, 2, 3 }, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(destination, Is.EqualTo(new[] { '?', '?', '?' }));
        });
    }

    [Test]
    public void DecodeReportsCapacityWithoutPartiallyWriting()
    {
        var destination = new byte[] { 0xAA, 0xAA };

        var result = Base64Codec.DecodeCanonical("Zm9v", destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 0xAA, 0xAA }));
        });
    }

    [TestCase(-1, -1)]
    [TestCase(0, 0)]
    [TestCase(1, 4)]
    [TestCase(2, 4)]
    [TestCase(3, 4)]
    [TestCase(4, 8)]
    [TestCase(1610612733, 2147483644)]
    [TestCase(1610612734, -1)]
    public void EncodedLengthMeasurementIsOverflowSafe(int sourceLength, int expectedLength)
    {
        Assert.That(Base64Codec.MeasureEncodedLength(sourceLength), Is.EqualTo(expectedLength));
    }

    [Test]
    public void ClientNonceCodecMatchesRfc6455Example()
    {
        const string encodedNonce = "dGhlIHNhbXBsZSBub25jZQ==";
        var nonce = new byte[WebSocketProtocolConstants.ClientNonceLength];

        var decode = Base64Codec.DecodeClientNonce(encodedNonce, nonce);
        var encoded = new char[24];
        var encode = Base64Codec.EncodeClientNonce(nonce, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decode.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(decode.ConsumedCount, Is.EqualTo(24));
            Assert.That(decode.ProducedCount, Is.EqualTo(16));
            Assert.That(Encoding.ASCII.GetString(nonce), Is.EqualTo("the sample nonce"));
            Assert.That(encode.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(encode.ProducedCount, Is.EqualTo(24));
            Assert.That(new string(encoded), Is.EqualTo(encodedNonce));
        });
    }

    [Test]
    public void ClientNonceCodecRejectsWrongDecodedOrSourceLength()
    {
        var wrongDecodedLength = "AAAAAAAAAAAAAAAAAAAAAAAA";
        var destination = Enumerable.Repeat((byte)0xAA, 18).ToArray();
        var decode = Base64Codec.DecodeClientNonce(wrongDecodedLength, destination);
        var shortEncode = Base64Codec.EncodeClientNonce(new byte[15], new char[24]);
        var longEncode = Base64Codec.EncodeClientNonce(new byte[17], new char[24]);

        Assert.Multiple(() =>
        {
            Assert.That(decode.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, 18)));
            Assert.That(shortEncode.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(longEncode.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
        });
    }

    [Test]
    public void AcceptDigestCodecMatchesRfc6455Example()
    {
        const string encodedAccept = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";
        var digest = new byte[WebSocketProtocolConstants.AcceptDigestLength];

        var decode = Base64Codec.DecodeAcceptDigest(encodedAccept, digest);
        var encoded = new char[28];
        var encode = Base64Codec.EncodeAcceptDigest(digest, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decode.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(decode.ConsumedCount, Is.EqualTo(28));
            Assert.That(decode.ProducedCount, Is.EqualTo(20));
            Assert.That(encode.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(encode.ProducedCount, Is.EqualTo(28));
            Assert.That(new string(encoded), Is.EqualTo(encodedAccept));
        });
    }

    [Test]
    public void AcceptDigestCodecRejectsWrongDecodedOrSourceLength()
    {
        var wrongDecodedLength = "AAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        var destination = Enumerable.Repeat((byte)0xAA, 21).ToArray();
        var decode = Base64Codec.DecodeAcceptDigest(wrongDecodedLength, destination);
        var shortEncode = Base64Codec.EncodeAcceptDigest(new byte[19], new char[28]);
        var longEncode = Base64Codec.EncodeAcceptDigest(new byte[21], new char[28]);

        Assert.Multiple(() =>
        {
            Assert.That(decode.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, 21)));
            Assert.That(shortEncode.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(longEncode.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
        });
    }

    [Test]
    public void FixedLengthDecodersReportOutputCapacityWithoutWriting()
    {
        var nonceDestination = Enumerable.Repeat((byte)0xAA, 15).ToArray();
        var acceptDestination = Enumerable.Repeat((byte)0xAA, 19).ToArray();

        var nonce = Base64Codec.DecodeClientNonce("dGhlIHNhbXBsZSBub25jZQ==", nonceDestination);
        var accept = Base64Codec.DecodeAcceptDigest("s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", acceptDestination);

        Assert.Multiple(() =>
        {
            Assert.That(nonce.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(nonceDestination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, 15)));
            Assert.That(accept.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(acceptDestination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, 19)));
        });
    }
}
