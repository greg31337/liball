using htmlgui;

namespace htmlgui.Tests;

public class BridgeBinaryCodecTests
{
    [Test]
    public void EncodesAndDecodesCanonicalBase64WithoutSharingBuffers()
    {
        byte[] source = new byte[] { 0, 1, 2, 127, 128, 254, 255 };
        BridgeBinaryCodec codec = new BridgeBinaryCodec();

        BridgeBinaryResult encoded = codec.EncodeBridgeBinary(source, source.Length);
        BridgeBinaryResult decoded = codec.DecodeBridgeBinary(encoded.Encoded, source.Length);
        source[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Encoded, Is.EqualTo("AAECf4D+/w=="));
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0, 1, 2, 127, 128, 254, 255 }));
            Assert.That(encoded.Data, Is.Not.SameAs(source));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Data, Is.EqualTo(new byte[] { 0, 1, 2, 127, 128, 254, 255 }));
        });
    }

    [TestCase("A")]
    [TestCase("AAA")]
    [TestCase("AA=A")]
    [TestCase("A===")]
    [TestCase("AA A")]
    [TestCase("AA-A")]
    [TestCase("AB==")]
    [TestCase("AAB=")]
    public void RejectsMalformedOrNonCanonicalBase64(string encoded)
    {
        BridgeBinaryResult result = new BridgeBinaryCodec().DecodeBridgeBinary(encoded, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo("bridge.invalid-base64"));
            Assert.That(result.Data, Is.Empty);
        });
    }

    [Test]
    public void EnforcesBinaryBoundsForEncodingAndDecoding()
    {
        BridgeBinaryCodec codec = new BridgeBinaryCodec();

        BridgeBinaryResult encoded = codec.EncodeBridgeBinary(new byte[] { 1, 2 }, 1);
        BridgeBinaryResult decoded = codec.DecodeBridgeBinary("AQI=", 1);
        BridgeBinaryResult invalidLimit = codec.DecodeBridgeBinary(string.Empty, -1);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Code, Is.EqualTo("bridge.binary-too-large"));
            Assert.That(decoded.Code, Is.EqualTo("bridge.binary-too-large"));
            Assert.That(invalidLimit.Code, Is.EqualTo("bridge.invalid-binary-limit"));
        });
    }
}
