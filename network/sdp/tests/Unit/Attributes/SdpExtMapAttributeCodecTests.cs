namespace sdp.Tests.Unit.Attributes;

public class SdpExtMapAttributeCodecTests
{
    [Test]
    public void PlainMapping_DecodesAndEncodesCanonicalForm()
    {
        SdpAttribute source = ValueAttribute("EXTMAP", "\t2/sendonly  http://example.com/082005/ext.htm#xmeta  short opaque data\t");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.ExtMapAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.ExtMapAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.ExtMapAttribute.Id, Is.EqualTo(2));
            Assert.That(decoded.ExtMapAttribute.HasDirection, Is.True);
            Assert.That(decoded.ExtMapAttribute.Direction, Is.EqualTo(SdpDirection.SendOnly));
            Assert.That(decoded.ExtMapAttribute.Encrypted, Is.False);
            Assert.That(decoded.ExtMapAttribute.Uri, Is.EqualTo("http://example.com/082005/ext.htm#xmeta"));
            Assert.That(decoded.ExtMapAttribute.ExtensionAttributes, Is.EqualTo("short opaque data"));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.ExtMapAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.TypedAttribute, Is.SameAs(decoded.ExtMapAttribute));
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute.Name, Is.EqualTo("extmap"));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("extmap:2/sendonly http://example.com/082005/ext.htm#xmeta short opaque data"));
        });
    }

    [Test]
    public void EncryptedMapping_SeparatesMarkerUriAndOpaqueAttributes()
    {
        string uri = "urn:ietf:params:rtp-hdrext:smpte-tc";
        string value = "3/recvonly " + SdpHeaderExtensionUris.Encryption + " " + uri + " 25@600/24 vendor data";
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute("extmap", value));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.ExtMapAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.ExtMapAttribute!.Id, Is.EqualTo(3));
            Assert.That(decoded.ExtMapAttribute.Direction, Is.EqualTo(SdpDirection.ReceiveOnly));
            Assert.That(decoded.ExtMapAttribute.Encrypted, Is.True);
            Assert.That(decoded.ExtMapAttribute.Uri, Is.EqualTo(uri));
            Assert.That(decoded.ExtMapAttribute.ExtensionAttributes, Is.EqualTo("25@600/24 vendor data"));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [TestCase("1 urn:example:extension", false, SdpDirection.SendReceive)]
    [TestCase("2/sendrecv urn:example:extension", true, SdpDirection.SendReceive)]
    [TestCase("3/sendonly urn:example:extension", true, SdpDirection.SendOnly)]
    [TestCase("4/recvonly urn:example:extension", true, SdpDirection.ReceiveOnly)]
    [TestCase("5/inactive urn:example:extension", true, SdpDirection.Inactive)]
    public void DirectionForms_DecodeAndEncodeWithoutLosingPresence(string value, bool hasDirection, SdpDirection direction)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute("extmap", value));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.ExtMapAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.ExtMapAttribute!.HasDirection, Is.EqualTo(hasDirection));
            Assert.That(decoded.ExtMapAttribute!.Direction, Is.EqualTo(direction));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void AllowMixed_IsAPropertyTypedCommonAttribute()
    {
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = "EXTMAP-ALLOW-MIXED",
            RawText = "EXTMAP-ALLOW-MIXED"
        };
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.CommonAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.CommonAttribute!.Name, Is.EqualTo(SdpAttributeNames.ExtMapAllowMixed));
            Assert.That(decoded.CommonAttribute.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.CommonAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Property));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("extmap-allow-mixed"));
        });
    }

    [TestCase(SdpAttributeKind.Property, "1 urn:example:extension")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "1")]
    [TestCase(SdpAttributeKind.Value, "123456 urn:example:extension")]
    [TestCase(SdpAttributeKind.Value, "a/sendonly urn:example:extension")]
    [TestCase(SdpAttributeKind.Value, "1/both urn:example:extension")]
    [TestCase(SdpAttributeKind.Value, "1/sendonly/recvonly urn:example:extension")]
    [TestCase(SdpAttributeKind.Value, "1 relative-extension-name")]
    [TestCase(SdpAttributeKind.Value, "1 urn:ietf:params:rtp-hdrext:encrypt")]
    [TestCase(SdpAttributeKind.Value, "1 urn:example:extension bad\nattributes")]
    public void InvalidExtMapSyntax_FailsAndRetainsOpaqueSource(SdpAttributeKind kind, string value)
    {
        SdpAttribute source = new SdpAttribute
        {
            Kind = kind,
            Name = "extmap",
            Value = value,
            RawText = "opaque-extmap"
        };
        source.Location.LineNumber = 41;

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute!.RawText, Is.EqualTo("opaque-extmap"));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.ExtMapAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("extmap"));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(41));
        });
    }

    [Test]
    public void CallerConstructedEncryptedMapping_EncodesAllFields()
    {
        SdpExtMapAttribute extMap = new SdpExtMapAttribute
        {
            Id = 99999,
            HasDirection = true,
            Direction = SdpDirection.Inactive,
            Encrypted = true,
            Uri = "https://example.test/header-extension?v=2",
            ExtensionAttributes = "opaque one two"
        };

        SdpAttributeEncodeResult result = new SdpAttributeCodec().EncodeAttribute(extMap);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TypedAttribute, Is.SameAs(extMap));
            Assert.That(result.Attribute!.RawText, Is.EqualTo("extmap:99999/inactive " + SdpHeaderExtensionUris.Encryption + " https://example.test/header-extension?v=2 opaque one two"));
            Assert.That(extMap.ExtensionAttributes, Is.EqualTo("opaque one two"));
        });
    }

    [Test]
    public void InvalidCallerData_ReturnsDiagnosticsWithoutAnAttribute()
    {
        SdpExtMapAttribute invalidId = Mapping(-1, SdpDirection.SendReceive, "urn:example:id", "");
        SdpExtMapAttribute invalidDirection = Mapping(1, SdpDirection.Unknown, "urn:example:direction", "");
        SdpExtMapAttribute invalidUri = Mapping(1, SdpDirection.SendReceive, "relative", "");
        SdpExtMapAttribute invalidAttributes = Mapping(1, SdpDirection.SendReceive, "urn:example:attributes", "bad\rdata");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeEncodeResult[] results =
        {
            codec.EncodeAttribute(invalidId),
            codec.EncodeAttribute(invalidDirection),
            codec.EncodeAttribute(invalidUri),
            codec.EncodeAttribute(invalidAttributes)
        };

        Assert.That(results, Has.All.Matches<SdpAttributeEncodeResult>(result =>
            !result.Success && result.Attribute == null && result.Diagnostics.Count == 1));
    }

    static SdpExtMapAttribute Mapping(int id, SdpDirection direction, string uri, string extensionAttributes)
    {
        return new SdpExtMapAttribute
        {
            Id = id,
            Direction = direction,
            Uri = uri,
            ExtensionAttributes = extensionAttributes
        };
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
