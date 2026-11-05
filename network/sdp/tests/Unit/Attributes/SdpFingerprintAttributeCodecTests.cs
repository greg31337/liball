namespace sdp.Tests.Unit.Attributes;

public class SdpFingerprintAttributeCodecTests
{
    [Test]
    public void Rfc8122Fingerprint_DecodesOctetsAndEncodesCanonicalText()
    {
        string fingerprint = "12:DF:3E:5D:49:6B:19:E5:7C:AB:4A:AD:B9:B1:3F:82:18:3B:54:02:12:DF:3E:5D:49:6B:19:E5:7C:AB:4A:AD";
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "FINGERPRINT",
            Value = "SHA-256 " + fingerprint,
            RawText = "FINGERPRINT:SHA-256 " + fingerprint
        };
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.FingerprintAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.FingerprintAttribute, Is.Not.Null);
            Assert.That(decoded.FingerprintAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.FingerprintAttribute.Algorithm, Is.EqualTo("SHA-256"));
            Assert.That(decoded.FingerprintAttribute.Fingerprint, Is.EqualTo(fingerprint));
            Assert.That(decoded.FingerprintAttribute.Digest, Has.Count.EqualTo(32));
            Assert.That(decoded.FingerprintAttribute.Digest.Take(4), Is.EqualTo(new byte[] { 0x12, 0xDF, 0x3E, 0x5D }));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.FingerprintAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.TypedAttribute, Is.SameAs(decoded.FingerprintAttribute));
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute.Name, Is.EqualTo("fingerprint"));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("fingerprint:SHA-256 " + fingerprint));
        });
    }

    [TestCase("sha-1")]
    [TestCase("sha-224")]
    [TestCase("sha-256")]
    [TestCase("sha-384")]
    [TestCase("sha-512")]
    [TestCase("MD5")]
    [TestCase("MD2")]
    [TestCase("x-future.hash")]
    public void RegisteredAndExtensionAlgorithmTokens_ArePreserved(string algorithm)
    {
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fingerprint", Value = "\t" + algorithm + "  00\t" };
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.FingerprintAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.FingerprintAttribute!.Algorithm, Is.EqualTo(algorithm));
            Assert.That(decoded.FingerprintAttribute.Fingerprint, Is.EqualTo("00"));
            Assert.That(decoded.FingerprintAttribute.Digest, Is.EqualTo(new byte[] { 0x00 }));
            Assert.That(encoded.Attribute!.RawText, Is.EqualTo("fingerprint:" + algorithm + " 00"));
        });
    }

    [TestCase(SdpAttributeKind.Property, "sha-256 DE:AD:BE:EF")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "sha-256")]
    [TestCase(SdpAttributeKind.Value, "sha/256 DE:AD")]
    [TestCase(SdpAttributeKind.Value, "sha-256 DE:AD extra")]
    [TestCase(SdpAttributeKind.Value, "sha-256 D")]
    [TestCase(SdpAttributeKind.Value, "sha-256 DEA")]
    [TestCase(SdpAttributeKind.Value, "sha-256 deadbeef")]
    [TestCase(SdpAttributeKind.Value, "sha-256 de:ad:be:ef")]
    [TestCase(SdpAttributeKind.Value, "sha-256 DE-AD")]
    [TestCase(SdpAttributeKind.Value, "sha-256 DE::AD")]
    [TestCase(SdpAttributeKind.Value, "sha-256 :DE")]
    [TestCase(SdpAttributeKind.Value, "sha-256 DE:")]
    [TestCase(SdpAttributeKind.Value, "sha-256 DG:AD")]
    public void InvalidFingerprintSyntax_FailsWithoutExposingOrDiscardingInput(SdpAttributeKind kind, string value)
    {
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "fingerprint", Value = value, RawText = "opaque-sensitive-source" };
        source.Location.LineNumber = 61;
        string sensitiveText = value.Length == 0 ? "opaque-sensitive-source" : value;

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute!.RawText, Is.EqualTo("opaque-sensitive-source"));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.FingerprintAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("fingerprint"));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(61));
            Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain(sensitiveText));
            Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain("opaque-sensitive-source"));
        });
    }

    [Test]
    public void Encoder_UsesDigestBytesAndIgnoresStaleOriginalFingerprintText()
    {
        SdpFingerprintAttribute projection = new SdpFingerprintAttribute
        {
            Algorithm = "x-future",
            Fingerprint = "sensitive-stale-original"
        };
        projection.Digest.Add(0x00);
        projection.Digest.Add(0x0A);
        projection.Digest.Add(0x10);
        projection.Digest.Add(0xFF);

        SdpAttributeEncodeResult result = new SdpAttributeCodec().EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Attribute!.Value, Is.EqualTo("x-future 00:0A:10:FF"));
            Assert.That(result.Attribute.RawText, Is.EqualTo("fingerprint:x-future 00:0A:10:FF"));
            Assert.That(projection.Fingerprint, Is.EqualTo("sensitive-stale-original"));
        });
    }

    [Test]
    public void EncoderFailures_DoNotIncludeOriginalFingerprintContents()
    {
        SdpFingerprintAttribute invalidAlgorithm = new SdpFingerprintAttribute
        {
            Algorithm = "sha/256",
            Fingerprint = "DE:AD:BE:EF"
        };
        invalidAlgorithm.Digest.Add(0xDE);
        SdpFingerprintAttribute emptyDigest = new SdpFingerprintAttribute
        {
            Algorithm = "sha-256",
            Fingerprint = "CA:FE:BA:BE"
        };

        SdpAttributeEncodeResult algorithmResult = new SdpAttributeCodec().EncodeAttribute(invalidAlgorithm);
        SdpAttributeEncodeResult digestResult = new SdpAttributeCodec().EncodeAttribute(emptyDigest);

        Assert.Multiple(() =>
        {
            Assert.That(algorithmResult.Success, Is.False);
            Assert.That(algorithmResult.Attribute, Is.Null);
            Assert.That(algorithmResult.Diagnostics[0].Detail, Does.Not.Contain(invalidAlgorithm.Fingerprint));
            Assert.That(digestResult.Success, Is.False);
            Assert.That(digestResult.Attribute, Is.Null);
            Assert.That(digestResult.Diagnostics[0].Detail, Does.Not.Contain(emptyDigest.Fingerprint));
        });
    }
}
