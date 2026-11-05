namespace sdp.Tests.Unit.Attributes;

public class SdpRtpMapCodecTests
{
    [TestCase("0 PCMU/8000", 0, "PCMU", 8000, false, "", false, 1)]
    [TestCase("98 L16/16000/2", 98, "L16", 16000, true, "2", true, 2)]
    [TestCase("111 opus/48000/profile-A", 111, "opus", 48000, true, "profile-A", false, 1)]
    [TestCase("127 X-CODEC/2147483647/2147483647", 127, "X-CODEC", 2147483647, true, "2147483647", true, 2147483647)]
    public void ValidRtpMaps_DecodeAndEncodeEveryOptionalForm(string value, int payloadType, string encodingName, int clockRate, bool hasParameters, string parameters, bool hasChannelCount, int channelCount)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "RTPMAP",
            Value = value,
            RawText = "RTPMAP:" + value
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.RtpMapAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.RtpMapAttribute?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.RtpMapAttribute?.PayloadType, Is.EqualTo(payloadType));
            Assert.That(decoded.RtpMapAttribute?.EncodingName, Is.EqualTo(encodingName));
            Assert.That(decoded.RtpMapAttribute?.ClockRate, Is.EqualTo(clockRate));
            Assert.That(decoded.RtpMapAttribute?.HasEncodingParameters, Is.EqualTo(hasParameters));
            Assert.That(decoded.RtpMapAttribute?.EncodingParameters, Is.EqualTo(parameters));
            Assert.That(decoded.RtpMapAttribute?.HasChannelCount, Is.EqualTo(hasChannelCount));
            Assert.That(decoded.RtpMapAttribute?.ChannelCount, Is.EqualTo(channelCount));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.RtpMapAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Diagnostics, Is.Empty);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute?.Name, Is.EqualTo("rtpmap"));
            Assert.That(encoded.Attribute?.Value, Is.EqualTo(value));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("rtpmap:" + value));
        });
    }

    [TestCase(SdpAttributeKind.Property, "")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "96")]
    [TestCase(SdpAttributeKind.Value, "96  opus/48000")]
    [TestCase(SdpAttributeKind.Value, "96 opus/48000 extra")]
    [TestCase(SdpAttributeKind.Value, "01 opus/48000")]
    [TestCase(SdpAttributeKind.Value, "128 opus/48000")]
    [TestCase(SdpAttributeKind.Value, "96 /48000")]
    [TestCase(SdpAttributeKind.Value, "96 opus/0")]
    [TestCase(SdpAttributeKind.Value, "96 opus/08000")]
    [TestCase(SdpAttributeKind.Value, "96 opus/2147483648")]
    [TestCase(SdpAttributeKind.Value, "96 opus/48000/")]
    [TestCase(SdpAttributeKind.Value, "96 opus/48000/0")]
    [TestCase(SdpAttributeKind.Value, "96 opus/48000/02")]
    [TestCase(SdpAttributeKind.Value, "96 opus/48000/two/channels")]
    public void InvalidRtpMap_FailsWithoutDiscardingOpaqueSource(SdpAttributeKind kind, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "rtpmap", Value = value, RawText = "rtpmap:" + value };
        source.Location.LineNumber = 9;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.RtpMapAttribute, Is.Null);
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("rtpmap"));
            Assert.That(result.Diagnostics[0].Path.AttributeName, Is.EqualTo("rtpmap"));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(9));
        });
    }

    [TestCase("-1 opus/48000")]
    [TestCase("128 opus/48000")]
    [TestCase("01 opus/48000")]
    public void InvalidPayloadTypes_UseThePayloadDiagnostic(string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationPayloadType));
    }

    [Test]
    public void CallerConstructedRtpMaps_EncodePlainChannelAndExtensionParameters()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpRtpMapAttribute plain = new SdpRtpMapAttribute
        {
            PayloadType = 0,
            EncodingName = "PCMU",
            ClockRate = 8000
        };
        SdpRtpMapAttribute channels = new SdpRtpMapAttribute
        {
            PayloadType = 111,
            EncodingName = "opus",
            ClockRate = 48000,
            HasEncodingParameters = true,
            EncodingParameters = "ignored",
            HasChannelCount = true,
            ChannelCount = 2
        };
        SdpRtpMapAttribute extension = new SdpRtpMapAttribute
        {
            PayloadType = 112,
            EncodingName = "X-CODEC",
            ClockRate = 90000,
            HasEncodingParameters = true,
            EncodingParameters = "profile-A"
        };

        SdpAttributeEncodeResult plainResult = codec.EncodeAttribute(plain);
        SdpAttributeEncodeResult channelResult = codec.EncodeAttribute(channels);
        SdpAttributeEncodeResult extensionResult = codec.EncodeAttribute(extension);

        Assert.Multiple(() =>
        {
            Assert.That(plainResult.Success, Is.True);
            Assert.That(plainResult.Attribute?.RawText, Is.EqualTo("rtpmap:0 PCMU/8000"));
            Assert.That(channelResult.Success, Is.True);
            Assert.That(channelResult.Attribute?.RawText, Is.EqualTo("rtpmap:111 opus/48000/2"));
            Assert.That(extensionResult.Success, Is.True);
            Assert.That(extensionResult.Attribute?.RawText, Is.EqualTo("rtpmap:112 X-CODEC/90000/profile-A"));
        });
    }

    [Test]
    public void InvalidCallerConstructedRtpMaps_ReturnStructuredFailures()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpRtpMapAttribute badPayload = new SdpRtpMapAttribute { PayloadType = 128, EncodingName = "opus", ClockRate = 48000 };
        SdpRtpMapAttribute badEncoding = new SdpRtpMapAttribute { PayloadType = 96, EncodingName = "bad name", ClockRate = 48000 };
        SdpRtpMapAttribute badClock = new SdpRtpMapAttribute { PayloadType = 96, EncodingName = "opus", ClockRate = 0 };
        SdpRtpMapAttribute inconsistentChannel = new SdpRtpMapAttribute { PayloadType = 96, EncodingName = "opus", ClockRate = 48000, HasChannelCount = true, ChannelCount = 2 };
        SdpRtpMapAttribute zeroChannel = new SdpRtpMapAttribute { PayloadType = 96, EncodingName = "opus", ClockRate = 48000, HasEncodingParameters = true, HasChannelCount = true, ChannelCount = 0 };

        SdpAttributeEncodeResult payloadResult = codec.EncodeAttribute(badPayload);
        SdpAttributeEncodeResult encodingResult = codec.EncodeAttribute(badEncoding);
        SdpAttributeEncodeResult clockResult = codec.EncodeAttribute(badClock);
        SdpAttributeEncodeResult inconsistentResult = codec.EncodeAttribute(inconsistentChannel);
        SdpAttributeEncodeResult channelResult = codec.EncodeAttribute(zeroChannel);

        Assert.Multiple(() =>
        {
            Assert.That(payloadResult.Success, Is.False);
            Assert.That(payloadResult.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationPayloadType));
            Assert.That(encodingResult.Success, Is.False);
            Assert.That(encodingResult.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(clockResult.Success, Is.False);
            Assert.That(inconsistentResult.Success, Is.False);
            Assert.That(channelResult.Success, Is.False);
            Assert.That(channelResult.Attribute, Is.Null);
        });
    }
}
