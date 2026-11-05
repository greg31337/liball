namespace sdp.Tests.Unit.Attributes;

public class SdpSctpAttributeCodecTests
{
    [TestCase("0", 0)]
    [TestCase("5000", 5000)]
    [TestCase("65535", 65535)]
    public void SctpPort_DecodesAndEncodesCanonicalPorts(string value, int expectedPort)
    {
        SdpAttribute source = ValueAttribute("SCTP-PORT", value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.SctpPortAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.SctpPortAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.SctpPortAttribute.Port, Is.EqualTo(expectedPort));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.SctpPortAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute.Name, Is.EqualTo(SdpAttributeNames.SctpPort));
            Assert.That(encoded.Attribute.Value, Is.EqualTo(value));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("sctp-port:" + value));
        });
    }

    [TestCase(0L, "0")]
    [TestCase(65536L, "65536")]
    [TestCase(long.MaxValue, "9223372036854775807")]
    public void MaximumMessageSize_DecodesAndEncodesSupportedValues(long expectedSize, string value)
    {
        SdpAttribute source = ValueAttribute("MAX-MESSAGE-SIZE", value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.MaximumMessageSizeAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.MaximumMessageSizeAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.MaximumMessageSizeAttribute.MaximumMessageSize, Is.EqualTo(expectedSize));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.MaximumMessageSizeAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Name, Is.EqualTo(SdpAttributeNames.MaximumMessageSize));
            Assert.That(encoded.Attribute.Value, Is.EqualTo(value));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("max-message-size:" + value));
        });
    }

    [TestCase("5000 webrtc-datachannel", false, 0)]
    [TestCase("5000\twebrtc-datachannel\t16", true, 16)]
    [TestCase("65535 X-Custom 65535", true, 65535)]
    [TestCase("5000 custom/application 1024", true, 1024)]
    public void LegacySctpMap_DecodesOptionalStreamCountAndNormalizesWhitespace(string value, bool hasStreams, int streams)
    {
        SdpAttribute source = ValueAttribute("SCTPMAP", value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.SctpMapAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.SctpMapAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.SctpMapAttribute.Port, Is.EqualTo(value.StartsWith("65535") ? 65535 : 5000));
            Assert.That(decoded.SctpMapAttribute.Format, Is.EqualTo(value.Contains("X-Custom")
                ? "X-Custom"
                : value.Contains("custom/application") ? "custom/application" : "webrtc-datachannel"));
            Assert.That(decoded.SctpMapAttribute.HasMaximumStreams, Is.EqualTo(hasStreams));
            Assert.That(decoded.SctpMapAttribute.MaximumStreams, Is.EqualTo(streams));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.SctpMapAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.RawText, Is.EqualTo(hasStreams
                ? "sctpmap:" + decoded.SctpMapAttribute.Port + " " + decoded.SctpMapAttribute.Format + " " + streams
                : "sctpmap:5000 webrtc-datachannel"));
        });
    }

    [TestCase(SdpAttributeKind.Property, "5000")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "01")]
    [TestCase(SdpAttributeKind.Value, "+1")]
    [TestCase(SdpAttributeKind.Value, "-1")]
    [TestCase(SdpAttributeKind.Value, "65536")]
    [TestCase(SdpAttributeKind.Value, " 5000")]
    public void InvalidSctpPort_FailsWithoutAProjection(SdpAttributeKind kind, string value)
    {
        AssertInvalid(kind, SdpAttributeNames.SctpPort, value);
    }

    [TestCase(SdpAttributeKind.Property, "65536")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "00")]
    [TestCase(SdpAttributeKind.Value, "+1")]
    [TestCase(SdpAttributeKind.Value, "-1")]
    [TestCase(SdpAttributeKind.Value, "9223372036854775808")]
    [TestCase(SdpAttributeKind.Value, "65536 ")]
    public void InvalidMaximumMessageSize_FailsWithoutAProjection(SdpAttributeKind kind, string value)
    {
        AssertInvalid(kind, SdpAttributeNames.MaximumMessageSize, value);
    }

    [TestCase(SdpAttributeKind.Property, "5000 webrtc-datachannel")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "01 webrtc-datachannel")]
    [TestCase(SdpAttributeKind.Value, "65536 webrtc-datachannel")]
    [TestCase(SdpAttributeKind.Value, "5000 bad\nvalue")]
    [TestCase(SdpAttributeKind.Value, "5000 webrtc-datachannel 0")]
    [TestCase(SdpAttributeKind.Value, "5000 webrtc-datachannel 01")]
    [TestCase(SdpAttributeKind.Value, "5000 webrtc-datachannel 65536")]
    [TestCase(SdpAttributeKind.Value, "5000 webrtc-datachannel 16 extra")]
    public void InvalidLegacySctpMap_FailsWithoutAProjection(SdpAttributeKind kind, string value)
    {
        AssertInvalid(kind, SdpAttributeNames.SctpMap, value);
    }

    [Test]
    public void InvalidCallerConstructedValues_FailEncoding()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttributeEncodeResult negativePort = codec.EncodeAttribute(new SdpSctpPortAttribute { Port = -1 });
        SdpAttributeEncodeResult highPort = codec.EncodeAttribute(new SdpSctpPortAttribute { Port = 65536 });
        SdpAttributeEncodeResult negativeSize = codec.EncodeAttribute(new SdpMaximumMessageSizeAttribute { MaximumMessageSize = -1 });
        SdpAttributeEncodeResult emptyFormat = codec.EncodeAttribute(new SdpSctpMapAttribute { Port = 5000 });
        SdpAttributeEncodeResult zeroStreams = codec.EncodeAttribute(new SdpSctpMapAttribute
        {
            Port = 5000,
            Format = SdpDataChannelTokens.WebRtcDataChannel,
            HasMaximumStreams = true,
            MaximumStreams = 0
        });

        SdpAttributeEncodeResult[] results = { negativePort, highPort, negativeSize, emptyFormat, zeroStreams };

        Assert.That(results, Has.All.Matches<SdpAttributeEncodeResult>(result =>
            !result.Success && result.Attribute == null && result.Diagnostics.Count == 1));
    }

    [Test]
    public void ModernMediaProjection_UsesModernPortAndExplicitUnlimitedSize()
    {
        SdpMediaDescription media = DataChannelMedia();
        SdpAttribute legacy = ValueAttribute("sctpmap", "6000 legacy-format 12");
        SdpAttribute modern = ValueAttribute("sctp-port", "5000");
        SdpAttribute maximum = ValueAttribute("max-message-size", "0");
        media.Attributes.Add(legacy);
        media.Attributes.Add(ValueAttribute("sctp-port", "01"));
        media.Attributes.Add(modern);
        media.Attributes.Add(maximum);

        SdpSctpDescription projection = SdpSctpAttributeCodecCore.ResolveMediaSctp(media);

        Assert.Multiple(() =>
        {
            Assert.That(projection.Port, Is.EqualTo(5000));
            Assert.That(projection.Format, Is.EqualTo(SdpDataChannelTokens.WebRtcDataChannel));
            Assert.That(projection.LegacySctpMap, Is.False);
            Assert.That(projection.HasMaximumStreams, Is.False);
            Assert.That(projection.HasMaximumMessageSize, Is.True);
            Assert.That(projection.MaximumMessageSize, Is.Zero);
            Assert.That(projection.PortAttribute!.SourceAttribute, Is.SameAs(modern));
            Assert.That(projection.MaximumMessageSizeAttribute!.SourceAttribute, Is.SameAs(maximum));
            Assert.That(projection.SctpMapAttribute!.SourceAttribute, Is.SameAs(legacy));
        });
    }

    [Test]
    public void MediaProjection_UsesDefaultSizeWhenAttributeIsAbsent()
    {
        SdpMediaDescription media = DataChannelMedia();
        media.Attributes.Add(ValueAttribute("sctp-port", "5000"));

        SdpSctpDescription projection = SdpSctpAttributeCodecCore.ResolveMediaSctp(media);

        Assert.Multiple(() =>
        {
            Assert.That(projection.HasMaximumMessageSize, Is.False);
            Assert.That(projection.MaximumMessageSizeAttribute, Is.Null);
            Assert.That(projection.MaximumMessageSize, Is.EqualTo(SdpDataChannelTokens.DefaultMaximumMessageSize));
        });
    }

    [Test]
    public void LegacyMediaProjection_RetainsApplicationAndIncomingStreams()
    {
        SdpMediaDescription media = new SdpMediaDescription { MediaType = "application", Protocol = "DTLS/SCTP" };
        media.Formats.Add("5000");
        SdpAttribute mapping = ValueAttribute("sctpmap", "5000 webrtc-datachannel 16");
        media.Attributes.Add(mapping);

        SdpSctpDescription first = SdpSctpAttributeCodecCore.ResolveMediaSctp(media);
        SdpSctpDescription second = SdpSctpAttributeCodecCore.ResolveMediaSctp(media);

        Assert.Multiple(() =>
        {
            Assert.That(first.Port, Is.EqualTo(5000));
            Assert.That(first.Format, Is.EqualTo(SdpDataChannelTokens.WebRtcDataChannel));
            Assert.That(first.LegacySctpMap, Is.True);
            Assert.That(first.HasMaximumStreams, Is.True);
            Assert.That(first.MaximumStreams, Is.EqualTo(16));
            Assert.That(first.HasMaximumMessageSize, Is.False);
            Assert.That(first.MaximumMessageSize, Is.EqualTo(SdpDataChannelTokens.DefaultMaximumMessageSize));
            Assert.That(first.SctpMapAttribute!.SourceAttribute, Is.SameAs(mapping));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.SctpMapAttribute, Is.Not.SameAs(first.SctpMapAttribute));
        });
    }

    static void AssertInvalid(SdpAttributeKind kind, string name, string value)
    {
        SdpAttribute source = new SdpAttribute
        {
            Kind = kind,
            Name = name,
            Value = value,
            RawText = "opaque-source"
        };
        source.Location.LineNumber = 27;

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute!.RawText, Is.EqualTo("opaque-source"));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.SctpPortAttribute, Is.Null);
            Assert.That(result.MaximumMessageSizeAttribute, Is.Null);
            Assert.That(result.SctpMapAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo(name));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(27));
        });
    }

    static SdpMediaDescription DataChannelMedia()
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "application",
            Protocol = SdpTransportProtocols.UdpDtlsSctp
        };
        media.Formats.Add(SdpDataChannelTokens.WebRtcDataChannel);
        return media;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
