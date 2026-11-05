namespace sdp.Tests.Unit.Attributes;

public class SdpIceAttributeCodecTests
{
    [TestCase("8hhY")]
    [TestCase("abc+/123")]
    public void UsernameFragment_DecodesAndEncodesIceCharacters(string usernameFragment)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "ICE-UFRAG",
            Value = usernameFragment,
            RawText = "ICE-UFRAG:" + usernameFragment
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.IceUsernameFragmentAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.IceUsernameFragmentAttribute?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.IceUsernameFragmentAttribute?.UsernameFragment, Is.EqualTo(usernameFragment));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.IceUsernameFragmentAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("ice-ufrag:" + usernameFragment));
        });
    }

    [Test]
    public void UsernameFragment_AcceptsReceiveLimitButEnforcesSendLimit()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        string usernameFragment = new string('u', 256);
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ice-ufrag", Value = usernameFragment };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.IceUsernameFragmentAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.IceUsernameFragmentAttribute?.UsernameFragment, Has.Length.EqualTo(256));
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.Attribute, Is.Null);
            Assert.That(encoded.Diagnostics, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Password_DecodesAndEncodesWithoutExposingSecretInDiagnostics()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        string password = "asd88fgpdd777uzjYhagZg";
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ICE-PWD", Value = password };
        SdpIcePasswordAttribute invalid = new SdpIcePasswordAttribute { Password = "private-secret-value!" };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.IcePasswordAttribute!);
        SdpAttributeEncodeResult failed = codec.EncodeAttribute(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.IcePasswordAttribute?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.IcePasswordAttribute?.Password, Is.EqualTo(password));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.IcePasswordAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("ice-pwd:" + password));
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Diagnostics[0].Detail, Does.Not.Contain(invalid.Password));
        });
    }

    [Test]
    public void Options_PreserveOrderDuplicatesAndUnknownTokensWhileNormalizingWhitespace()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "Ice-Options",
            Value = "  trickle\tXFuture  trickle rtp+ecn  "
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.IceOptionsAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.IceOptionsAttribute?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.IceOptionsAttribute?.Options, Is.EqualTo(new[] { "trickle", "XFuture", "trickle", "rtp+ecn" }));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.IceOptionsAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("ice-options:trickle XFuture trickle rtp+ecn"));
        });
    }

    [TestCase("ICE-LITE", "ice-lite")]
    [TestCase("ICE-MISMATCH", "ice-mismatch")]
    [TestCase("END-OF-CANDIDATES", "end-of-candidates")]
    public void IceMarkers_DecodeAndEncodeAsCanonicalProperties(string sourceName, string canonicalName)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Property, Name = sourceName, RawText = sourceName };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.CommonAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.CommonAttribute?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.CommonAttribute?.Name, Is.EqualTo(canonicalName));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Property));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo(canonicalName));
        });
    }

    [TestCase(SdpAttributeKind.Property, "ice-ufrag", "8hhY")]
    [TestCase(SdpAttributeKind.Value, "ice-ufrag", "abc")]
    [TestCase(SdpAttributeKind.Value, "ice-ufrag", "abcd-")]
    [TestCase(SdpAttributeKind.Property, "ice-pwd", "asd88fgpdd777uzjYhagZg")]
    [TestCase(SdpAttributeKind.Value, "ice-pwd", "too-short")]
    [TestCase(SdpAttributeKind.Value, "ice-pwd", "asd88fgpdd777uzjYhagZ!")]
    [TestCase(SdpAttributeKind.Property, "ice-options", "trickle")]
    [TestCase(SdpAttributeKind.Value, "ice-options", "")]
    [TestCase(SdpAttributeKind.Value, "ice-options", "trickle bad-option")]
    [TestCase(SdpAttributeKind.Value, "ice-lite", "")]
    [TestCase(SdpAttributeKind.Value, "ice-mismatch", "")]
    [TestCase(SdpAttributeKind.Value, "end-of-candidates", "")]
    public void InvalidIceAttribute_FailsAndRetainsOpaqueSource(SdpAttributeKind kind, string name, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = name, Value = value, RawText = "opaque-" + name };
        source.Location.LineNumber = 44;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute?.RawText, Is.EqualTo("opaque-" + name));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.IceUsernameFragmentAttribute, Is.Null);
            Assert.That(result.IcePasswordAttribute, Is.Null);
            Assert.That(result.IceOptionsAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(44));
        });
    }

    [Test]
    public void CallerConstructedIceData_RejectsInvalidValues()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpIceUsernameFragmentAttribute username = new SdpIceUsernameFragmentAttribute { UsernameFragment = new string('u', 33) };
        SdpIcePasswordAttribute password = new SdpIcePasswordAttribute { Password = new string('p', 21) };
        SdpIceOptionsAttribute emptyOptions = new SdpIceOptionsAttribute();
        SdpIceOptionsAttribute invalidOptions = new SdpIceOptionsAttribute();
        invalidOptions.Options.Add("not-supported-");

        SdpAttributeEncodeResult usernameResult = codec.EncodeAttribute(username);
        SdpAttributeEncodeResult passwordResult = codec.EncodeAttribute(password);
        SdpAttributeEncodeResult emptyOptionsResult = codec.EncodeAttribute(emptyOptions);
        SdpAttributeEncodeResult invalidOptionsResult = codec.EncodeAttribute(invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(usernameResult.Success, Is.False);
            Assert.That(passwordResult.Success, Is.False);
            Assert.That(emptyOptionsResult.Success, Is.False);
            Assert.That(invalidOptionsResult.Success, Is.False);
            Assert.That(invalidOptionsResult.Attribute, Is.Null);
        });
    }
}
