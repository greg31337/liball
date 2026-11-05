namespace sdp.Tests.Api;

public class SdpAttributeCodecTests
{
    [Test]
    public void UnknownAttribute_DecodesAsSuccessfulOpaqueData()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute attribute = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "x-vendor-mode",
            Value = "opaque:data",
            RawText = "x-vendor-mode:opaque:data"
        };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(attribute);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recognized, Is.False);
            Assert.That(result.SourceAttribute, Is.SameAs(attribute));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.SourceAttribute?.Name, Is.EqualTo("x-vendor-mode"));
            Assert.That(result.SourceAttribute?.Value, Is.EqualTo("opaque:data"));
            Assert.That(result.SourceAttribute?.RawText, Is.EqualTo("x-vendor-mode:opaque:data"));
        });
    }

    [Test]
    public void UnsupportedTypedData_ReturnsStructuredEncodingFailure()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        object typedAttribute = new object();

        SdpAttributeEncodeResult result = codec.EncodeAttribute(typedAttribute);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.TypedAttribute, Is.SameAs(typedAttribute));
            Assert.That(result.Attribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('a'));
            Assert.That(result.Diagnostics[0].Path.FieldName, Is.EqualTo('a'));
        });
    }

    [Test]
    public void CodecResults_HaveSafeIndependentDefaults()
    {
        SdpAttributeDecodeResult firstDecode = new SdpAttributeDecodeResult();
        SdpAttributeDecodeResult secondDecode = new SdpAttributeDecodeResult();
        SdpAttributeEncodeResult firstEncode = new SdpAttributeEncodeResult();
        SdpAttributeEncodeResult secondEncode = new SdpAttributeEncodeResult();
        firstDecode.Diagnostics.Add(new SdpDiagnostic());
        firstEncode.Diagnostics.Add(new SdpDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(secondDecode.Success, Is.False);
            Assert.That(secondDecode.Recognized, Is.False);
            Assert.That(secondDecode.SourceAttribute, Is.Null);
            Assert.That(secondDecode.TypedAttribute, Is.Null);
            Assert.That(secondDecode.CommonAttribute, Is.Null);
            Assert.That(secondDecode.RtpMapAttribute, Is.Null);
            Assert.That(secondDecode.FormatParametersAttribute, Is.Null);
            Assert.That(secondDecode.RtcpAttribute, Is.Null);
            Assert.That(secondDecode.RtcpFeedbackAttribute, Is.Null);
            Assert.That(secondDecode.RtcpExtendedReportAttribute, Is.Null);
            Assert.That(secondDecode.SynchronizationSourceAttribute, Is.Null);
            Assert.That(secondDecode.SourceGroupAttribute, Is.Null);
            Assert.That(secondDecode.MediaStreamId, Is.Null);
            Assert.That(secondDecode.MediaStreamSemanticAttribute, Is.Null);
            Assert.That(secondDecode.SourceFilterAttribute, Is.Null);
            Assert.That(secondDecode.ClockReferenceAttribute, Is.Null);
            Assert.That(secondDecode.MediaClockAttribute, Is.Null);
            Assert.That(secondDecode.ImageAttribute, Is.Null);
            Assert.That(secondDecode.GroupAttribute, Is.Null);
            Assert.That(secondDecode.MediaIdAttribute, Is.Null);
            Assert.That(secondDecode.IceUsernameFragmentAttribute, Is.Null);
            Assert.That(secondDecode.IcePasswordAttribute, Is.Null);
            Assert.That(secondDecode.IceOptionsAttribute, Is.Null);
            Assert.That(secondDecode.IceCandidate, Is.Null);
            Assert.That(secondDecode.RemoteCandidatesAttribute, Is.Null);
            Assert.That(secondDecode.FingerprintAttribute, Is.Null);
            Assert.That(secondDecode.SetupAttribute, Is.Null);
            Assert.That(secondDecode.ConnectionAttribute, Is.Null);
            Assert.That(secondDecode.TlsIdAttribute, Is.Null);
            Assert.That(secondDecode.CryptoAttribute, Is.Null);
            Assert.That(secondDecode.ExtMapAttribute, Is.Null);
            Assert.That(secondDecode.RidAttribute, Is.Null);
            Assert.That(secondDecode.SimulcastAttribute, Is.Null);
            Assert.That(secondDecode.SctpPortAttribute, Is.Null);
            Assert.That(secondDecode.MaximumMessageSizeAttribute, Is.Null);
            Assert.That(secondDecode.SctpMapAttribute, Is.Null);
            Assert.That(secondDecode.DataChannelAttribute, Is.Null);
            Assert.That(secondDecode.DataChannelSubprotocolAttribute, Is.Null);
            Assert.That(secondDecode.Diagnostics, Is.Empty);
            Assert.That(firstDecode.Diagnostics, Is.Not.SameAs(secondDecode.Diagnostics));
            Assert.That(secondEncode.Success, Is.False);
            Assert.That(secondEncode.TypedAttribute, Is.Null);
            Assert.That(secondEncode.Attribute, Is.Null);
            Assert.That(secondEncode.Diagnostics, Is.Empty);
            Assert.That(firstEncode.Diagnostics, Is.Not.SameAs(secondEncode.Diagnostics));
        });
    }

    [Test]
    public void Codec_RejectsNullArguments()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => codec.DecodeAttribute(null!));
            Assert.Throws<ArgumentNullException>(() => codec.EncodeAttribute(null!));
        });
    }

    [Test]
    public void CodecCalls_ReturnIndependentResultsAndDiagnostics()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute attribute = new SdpAttribute { Name = "x-opaque" };

        SdpAttributeDecodeResult firstDecode = codec.DecodeAttribute(attribute);
        SdpAttributeDecodeResult secondDecode = codec.DecodeAttribute(attribute);
        SdpAttributeEncodeResult firstEncode = codec.EncodeAttribute(new object());
        SdpAttributeEncodeResult secondEncode = codec.EncodeAttribute(new object());
        firstDecode.Diagnostics.Add(new SdpDiagnostic());
        firstEncode.Diagnostics.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(firstDecode, Is.Not.SameAs(secondDecode));
            Assert.That(firstDecode.Diagnostics, Is.Not.SameAs(secondDecode.Diagnostics));
            Assert.That(secondDecode.Diagnostics, Is.Empty);
            Assert.That(firstEncode, Is.Not.SameAs(secondEncode));
            Assert.That(firstEncode.Diagnostics, Is.Not.SameAs(secondEncode.Diagnostics));
            Assert.That(firstEncode.Diagnostics, Is.Empty);
            Assert.That(secondEncode.Diagnostics, Has.Count.EqualTo(1));
        });
    }
}
