namespace sdp.Tests.Unit.Attributes;

public class SdpCryptoAttributeCodecTests
{
    [Test]
    public void Rfc4568CryptoAttribute_DecodesAndEncodesEveryField()
    {
        string key = "inline:PS1uQCVeeCFCanVmcjkpPywjNWhcYD0mXXtxaVBR|2^20|1:32";
        SdpAttribute source = ValueAttribute("CRYPTO", "\t1  AES_CM_128_HMAC_SHA1_80 " + key + "  UNENCRYPTED_SRTCP\t");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.CryptoAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.CryptoAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.CryptoAttribute.Tag, Is.EqualTo(1));
            Assert.That(decoded.CryptoAttribute.Suite, Is.EqualTo(SdpCryptoSuites.AesCounterMode128HmacSha1_80));
            Assert.That(decoded.CryptoAttribute.KeyParameters, Is.EqualTo(new[] { key }));
            Assert.That(decoded.CryptoAttribute.SessionParameters, Is.EqualTo(new[] { "UNENCRYPTED_SRTCP" }));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.CryptoAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.TypedAttribute, Is.SameAs(decoded.CryptoAttribute));
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute.Name, Is.EqualTo("crypto"));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("crypto:1 AES_CM_128_HMAC_SHA1_80 " + key + " UNENCRYPTED_SRTCP"));
        });
    }

    [Test]
    public void ExtensionFormsAndMultipleKeys_RoundTripWithoutInterpretation()
    {
        string value = "999999999 FUTURE_SUITE_2 x_method:secret:data;INLINE:other-secret KDR=1 -x-mode=value x/opaque";
        SdpAttribute source = ValueAttribute("crypto", value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.CryptoAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.CryptoAttribute!.Tag, Is.EqualTo(999999999));
            Assert.That(decoded.CryptoAttribute.Suite, Is.EqualTo("FUTURE_SUITE_2"));
            Assert.That(decoded.CryptoAttribute.KeyParameters, Is.EqualTo(new[] { "x_method:secret:data", "INLINE:other-secret" }));
            Assert.That(decoded.CryptoAttribute.SessionParameters, Is.EqualTo(new[] { "KDR=1", "-x-mode=value", "x/opaque" }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [TestCase("0", true)]
    [TestCase("1", true)]
    [TestCase("999999999", true)]
    [TestCase("", false)]
    [TestCase("01", false)]
    [TestCase("1000000000", false)]
    [TestCase("-1", false)]
    [TestCase("1a", false)]
    public void CryptoTag_EnforcesCanonicalOneThroughNineDigitForm(string tag, bool expectedSuccess)
    {
        string value = tag + " AES_CM_128_HMAC_SHA1_80 inline:key";

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(ValueAttribute("crypto", value));

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [TestCase(SdpAttributeKind.Property, "1 AES_CM_128_HMAC_SHA1_80 inline:secret")]
    [TestCase(SdpAttributeKind.Value, "1 AES_CM_128_HMAC_SHA1_80")]
    [TestCase(SdpAttributeKind.Value, "1 AES-CM-128 inline:secret")]
    [TestCase(SdpAttributeKind.Value, "1 AES_CM_128_HMAC_SHA1_80 inline:")]
    [TestCase(SdpAttributeKind.Value, "1 AES_CM_128_HMAC_SHA1_80 bad-method:secret")]
    [TestCase(SdpAttributeKind.Value, "1 AES_CM_128_HMAC_SHA1_80 inline:secret;")]
    [TestCase(SdpAttributeKind.Value, "1 AES_CM_128_HMAC_SHA1_80 ;inline:secret")]
    [TestCase(SdpAttributeKind.Value, "1 AES_CM_128_HMAC_SHA1_80 inline:secret bad©")]
    public void InvalidCryptoSyntax_FailsWithoutLoggingOrDiscardingKeyMaterial(SdpAttributeKind kind, string value)
    {
        SdpAttribute source = new SdpAttribute
        {
            Kind = kind,
            Name = "crypto",
            Value = value,
            RawText = "opaque-sensitive-crypto"
        };
        source.Location.LineNumber = 63;

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute!.RawText, Is.EqualTo("opaque-sensitive-crypto"));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.CryptoAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("crypto"));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(63));
            Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain("secret"));
            Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain("opaque-sensitive-crypto"));
        });
    }

    [Test]
    public void CallerConstructedExtensionData_EncodesWithoutChangingOpaqueFields()
    {
        SdpCryptoAttribute crypto = new SdpCryptoAttribute { Tag = 42, Suite = "X_SUITE" };
        crypto.KeyParameters.Add("X_METHOD:key:with:colons");
        crypto.KeyParameters.Add("inline:other-key");
        crypto.SessionParameters.Add("X_OPTION=a/b;c");

        SdpAttributeEncodeResult result = new SdpAttributeCodec().EncodeAttribute(crypto);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Attribute!.RawText, Is.EqualTo("crypto:42 X_SUITE X_METHOD:key:with:colons;inline:other-key X_OPTION=a/b;c"));
            Assert.That(crypto.KeyParameters, Is.EqualTo(new[] { "X_METHOD:key:with:colons", "inline:other-key" }));
            Assert.That(crypto.SessionParameters, Is.EqualTo(new[] { "X_OPTION=a/b;c" }));
        });
    }

    [Test]
    public void EncoderFailures_DoNotIncludeSensitiveCallerData()
    {
        SdpCryptoAttribute invalidTag = CryptoWithKey(1000000000, "VALID_SUITE", "inline:tag-secret");
        SdpCryptoAttribute invalidSuite = CryptoWithKey(1, "BAD-SUITE", "inline:suite-secret");
        SdpCryptoAttribute invalidKey = CryptoWithKey(1, "VALID_SUITE", "inline:key-secret;");
        SdpCryptoAttribute invalidSession = CryptoWithKey(1, "VALID_SUITE", "inline:session-secret");
        invalidSession.SessionParameters.Add("bad\nparameter");

        SdpAttributeEncodeResult tagResult = new SdpAttributeCodec().EncodeAttribute(invalidTag);
        SdpAttributeEncodeResult suiteResult = new SdpAttributeCodec().EncodeAttribute(invalidSuite);
        SdpAttributeEncodeResult keyResult = new SdpAttributeCodec().EncodeAttribute(invalidKey);
        SdpAttributeEncodeResult sessionResult = new SdpAttributeCodec().EncodeAttribute(invalidSession);

        Assert.Multiple(() =>
        {
            AssertRedactedFailure(tagResult, "tag-secret");
            AssertRedactedFailure(suiteResult, "suite-secret");
            AssertRedactedFailure(keyResult, "key-secret");
            AssertRedactedFailure(sessionResult, "session-secret");
        });
    }

    static SdpCryptoAttribute CryptoWithKey(int tag, string suite, string key)
    {
        SdpCryptoAttribute result = new SdpCryptoAttribute { Tag = tag, Suite = suite };
        result.KeyParameters.Add(key);
        return result;
    }

    static void AssertRedactedFailure(SdpAttributeEncodeResult result, string sensitiveText)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Attribute, Is.Null);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain(sensitiveText));
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
