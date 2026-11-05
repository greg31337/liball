namespace sdp.Tests.Unit.Attributes;

public class SdpDtlsAttributeCodecTests
{
    [TestCase("active", SdpDtlsSetupRole.Active)]
    [TestCase("PASSIVE", SdpDtlsSetupRole.Passive)]
    [TestCase("ActPass", SdpDtlsSetupRole.ActPass)]
    [TestCase("holdconn", SdpDtlsSetupRole.HoldConnection)]
    public void SetupRoles_DecodeRecognizedValuesAndEncodeCanonicalTokens(string token, SdpDtlsSetupRole expectedRole)
    {
        SdpAttribute source = ValueAttribute("SETUP", " \t" + token + "  ");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.SetupAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.SetupAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.SetupAttribute.Role, Is.EqualTo(expectedRole));
            Assert.That(decoded.SetupAttribute.RoleToken, Is.EqualTo(token));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.SetupAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.TypedAttribute, Is.SameAs(decoded.SetupAttribute));
            Assert.That(encoded.Attribute!.RawText, Is.EqualTo("setup:" + expectedRole.ToString().ToLowerInvariant().Replace("holdconnection", "holdconn")));
        });
    }

    [Test]
    public void SetupExtensionToken_RoundTripsWithoutBeingAssignedARegisteredRole()
    {
        SdpAttribute source = ValueAttribute("setup", "x-future.role");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.SetupAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.SetupAttribute!.Role, Is.EqualTo(SdpDtlsSetupRole.Unknown));
            Assert.That(decoded.SetupAttribute.RoleToken, Is.EqualTo("x-future.role"));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.RawText, Is.EqualTo("setup:x-future.role"));
        });
    }

    [TestCase("new", "new")]
    [TestCase("NEW", "new")]
    [TestCase("existing", "existing")]
    [TestCase("Existing", "existing")]
    [TestCase("x-reuse", "x-reuse")]
    public void ConnectionRoles_CanonicalizeRegisteredValuesAndRetainExtensions(string token, string expectedToken)
    {
        SdpAttribute source = ValueAttribute("CONNECTION", "\t" + token + " ");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.ConnectionAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.ConnectionAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.ConnectionAttribute.ConnectionRole, Is.EqualTo(token));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.ConnectionAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("connection:" + expectedToken));
        });
    }

    [Test]
    public void TlsId_Rfc8842ExampleAndCompleteAlphabetRoundTrip()
    {
        string id = "abc3de65cddef001be82";
        SdpAttribute source = ValueAttribute("TLS-ID", " " + id + "\t");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.TlsIdAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.TlsIdAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.TlsIdAttribute.Id, Is.EqualTo(id));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.TlsIdAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.RawText, Is.EqualTo("tls-id:" + id));
        });

        string alphabet = "AZaz09+/-_AZaz09+/-_";
        SdpAttributeDecodeResult alphabetResult = codec.DecodeAttribute(ValueAttribute("tls-id", alphabet));
        Assert.That(alphabetResult.Success, Is.True);
    }

    [TestCase(19, false)]
    [TestCase(20, true)]
    [TestCase(255, true)]
    [TestCase(256, false)]
    public void TlsId_DecodeAndEncodeEnforceLengthBoundaries(int length, bool expectedSuccess)
    {
        string id = new string('a', length);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute("tls-id", id));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(new SdpTlsIdAttribute { Id = id });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.EqualTo(expectedSuccess));
            Assert.That(encoded.Success, Is.EqualTo(expectedSuccess));
            Assert.That(encoded.Attribute == null, Is.EqualTo(!expectedSuccess));
        });
    }

    [TestCase(SdpAttributeKind.Property, "setup", "active")]
    [TestCase(SdpAttributeKind.Value, "setup", "")]
    [TestCase(SdpAttributeKind.Value, "setup", "active passive")]
    [TestCase(SdpAttributeKind.Value, "setup", "bad/role")]
    [TestCase(SdpAttributeKind.Property, "connection", "new")]
    [TestCase(SdpAttributeKind.Value, "connection", "")]
    [TestCase(SdpAttributeKind.Value, "connection", "new existing")]
    [TestCase(SdpAttributeKind.Value, "connection", "bad/role")]
    [TestCase(SdpAttributeKind.Property, "tls-id", "abc3de65cddef001be82")]
    [TestCase(SdpAttributeKind.Value, "tls-id", "abc3de65cddef001be8!")]
    [TestCase(SdpAttributeKind.Value, "tls-id", "abc3de65cddef001be8.")]
    [TestCase(SdpAttributeKind.Value, "tls-id", "abc3de65cddef001be8 ")]
    public void InvalidDtlsAttributeSyntax_FailsAndRetainsOpaqueSource(SdpAttributeKind kind, string name, string value)
    {
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = name, Value = value, RawText = "opaque:" + name + ":" + value };
        source.Location.LineNumber = 62;

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute!.RawText, Is.EqualTo("opaque:" + name + ":" + value));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.SetupAttribute, Is.Null);
            Assert.That(result.ConnectionAttribute, Is.Null);
            Assert.That(result.TlsIdAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo(name));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(62));
        });
    }

    [Test]
    public void SetupEncoder_RejectsInconsistentKindsAndTokens()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpSetupAttribute mismatch = new SdpSetupAttribute { Role = SdpDtlsSetupRole.Active, RoleToken = "passive" };
        SdpSetupAttribute knownAsUnknown = new SdpSetupAttribute { Role = SdpDtlsSetupRole.Unknown, RoleToken = "actpass" };
        SdpSetupAttribute missingExtension = new SdpSetupAttribute { Role = SdpDtlsSetupRole.Unknown };

        SdpAttributeEncodeResult mismatchResult = codec.EncodeAttribute(mismatch);
        SdpAttributeEncodeResult knownResult = codec.EncodeAttribute(knownAsUnknown);
        SdpAttributeEncodeResult missingResult = codec.EncodeAttribute(missingExtension);

        Assert.Multiple(() =>
        {
            Assert.That(mismatchResult.Success, Is.False);
            Assert.That(knownResult.Success, Is.False);
            Assert.That(missingResult.Success, Is.False);
            Assert.That(mismatchResult.Attribute, Is.Null);
            Assert.That(knownResult.Attribute, Is.Null);
            Assert.That(missingResult.Attribute, Is.Null);
        });
    }

    [Test]
    public void ConnectionAndTlsIdEncoders_RejectInvalidCallerData()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpConnectionAttribute connection = new SdpConnectionAttribute { ConnectionRole = "bad/role" };
        SdpTlsIdAttribute tlsId = new SdpTlsIdAttribute { Id = "abc3de65cddef001be8!" };

        SdpAttributeEncodeResult connectionResult = codec.EncodeAttribute(connection);
        SdpAttributeEncodeResult tlsIdResult = codec.EncodeAttribute(tlsId);

        Assert.Multiple(() =>
        {
            Assert.That(connectionResult.Success, Is.False);
            Assert.That(connectionResult.Attribute, Is.Null);
            Assert.That(tlsIdResult.Success, Is.False);
            Assert.That(tlsIdResult.Attribute, Is.Null);
        });
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
