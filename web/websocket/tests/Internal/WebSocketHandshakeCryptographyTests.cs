using websocket;

namespace websocket.Tests;

public class WebSocketHandshakeCryptographyTests
{
    [Test]
    public void CreatesFreshCanonicalClientNoncesWithSixteenRandomBytes()
    {
        var values = new HashSet<string>();

        for (var index = 0; index < 32; index++)
        {
            var result = WebSocketHandshakeCryptography.CreateClientNonce();
            var decoded = new byte[WebSocketProtocolConstants.ClientNonceLength];
            var decoding = Base64Codec.DecodeClientNonce(result.Value, decoded);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(result.Value, Has.Length.EqualTo(24));
                Assert.That(result.Value, Does.EndWith("=="));
                Assert.That(decoding.Status, Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(decoding.ConsumedCount, Is.EqualTo(24));
                Assert.That(decoding.ProducedCount,
                    Is.EqualTo(WebSocketProtocolConstants.ClientNonceLength));
            });

            values.Add(result.Value);
        }

        Assert.That(values.Count, Is.EqualTo(32));
    }

    [TestCase(
        "dGhlIHNhbXBsZSBub25jZQ==",
        "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=")]
    [TestCase(
        "AAAAAAAAAAAAAAAAAAAAAA==",
        "ICX+Yqv66kxgM0FcWaLWlFLwTAI=")]
    [TestCase(
        "AAECAwQFBgcICQoLDA0ODw==",
        "Bz3qJYTGdOe8gUSpLosEdiLKDrk=")]
    public void ComputesCanonicalAcceptFromExactEncodedNonce(
        string nonce,
        string expectedAccept)
    {
        var result = WebSocketHandshakeCryptography.ComputeAccept(nonce);
        var decoded = new byte[WebSocketProtocolConstants.AcceptDigestLength];
        var decoding = Base64Codec.DecodeAcceptDigest(result.Value, decoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Value, Is.EqualTo(expectedAccept));
            Assert.That(result.Value, Has.Length.EqualTo(28));
            Assert.That(decoding.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(decoding.ProducedCount,
                Is.EqualTo(WebSocketProtocolConstants.AcceptDigestLength));
        });
    }

    [TestCase("")]
    [TestCase("AAAAAAAAAAAAAAAAAAAAAAAA")]
    [TestCase("dGhlIHNhbXBsZSBub25jZQ=")]
    [TestCase("dGhlIHNhbXBsZSBub25jZQ===")]
    [TestCase("dGhlIHNhbXBsZSBub25jZQ== ")]
    [TestCase("dGhlIHNhbXBsZSBub25jZ!==")]
    [TestCase("AQIDBAUGBwgJCgsMDQ4PEC==")]
    public void RejectsMalformedWrongLengthOrNonCanonicalClientNonces(string nonce)
    {
        var result = WebSocketHandshakeCryptography.ComputeAccept(nonce);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Value, Is.Empty);
        });
    }

    [Test]
    public void CorrectedRfc6455ErrataNonceIsCanonicalAndProducesExpectedAccept()
    {
        const string correctedNonce = "AQIDBAUGBwgJCgsMDQ4PEA==";
        const string nonCanonicalOriginal = "AQIDBAUGBwgJCgsMDQ4PEC==";
        const string expectedAccept = "C/0nmHhBztSRGR1CwL6Tf4ZjwpY=";
        byte[] decoded = new byte[WebSocketProtocolConstants.ClientNonceLength];

        Base64DecodeResult decoding = Base64Codec.DecodeClientNonce(correctedNonce, decoded);
        WebSocketHandshakeValueResult accept =
            WebSocketHandshakeCryptography.ComputeAccept(correctedNonce);
        WebSocketHandshakeValueResult original =
            WebSocketHandshakeCryptography.ComputeAccept(nonCanonicalOriginal);

        Assert.Multiple(() =>
        {
            Assert.That(decoding.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(decoded, Is.EqualTo(Enumerable.Range(1, 16).Select(value => (byte)value)));
            Assert.That(accept.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(accept.Value, Is.EqualTo(expectedAccept));
            Assert.That(original.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(original.Value, Is.Empty);
        });
    }
}
