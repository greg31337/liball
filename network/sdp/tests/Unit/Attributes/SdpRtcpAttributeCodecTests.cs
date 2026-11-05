namespace sdp.Tests.Unit.Attributes;

public class SdpRtcpAttributeCodecTests
{
    [TestCase("RTCP-MUX", "rtcp-mux")]
    [TestCase("rtcp-mux-only", "rtcp-mux-only")]
    [TestCase("RTCP-RSIZE", "rtcp-rsize")]
    public void RtcpMarkers_DecodeAndEncodeAsCanonicalProperties(string sourceName, string canonicalName)
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
            Assert.That(decoded.CommonAttribute?.Direction, Is.EqualTo(SdpDirection.Unknown));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Property));
            Assert.That(encoded.Attribute?.Name, Is.EqualTo(canonicalName));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo(canonicalName));
        });
    }

    [Test]
    public void RtcpMarker_WithValueSyntaxFailsAndRetainsSource()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtcp-mux", Value = string.Empty, RawText = "rtcp-mux:" };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        });
    }

    [TestCase("53020", 53020, false, "", "", "", SdpAddressKind.Unknown, "53020")]
    [TestCase("0", 0, false, "", "", "", SdpAddressKind.Unknown, "0")]
    [TestCase("65535", 65535, false, "", "", "", SdpAddressKind.Unknown, "65535")]
    [TestCase(" 53020   IN IP4 126.16.64.4 ", 53020, true, "IN", "IP4", "126.16.64.4", SdpAddressKind.Ipv4, "53020 IN IP4 126.16.64.4")]
    [TestCase("53020\tIN\tIP6\t2001:db8::1", 53020, true, "IN", "IP6", "2001:db8::1", SdpAddressKind.Ipv6, "53020 IN IP6 2001:db8::1")]
    [TestCase("5005 IN IP4 233.252.0.1/64/2", 5005, true, "IN", "IP4", "233.252.0.1/64/2", SdpAddressKind.Ipv4Multicast, "5005 IN IP4 233.252.0.1/64/2")]
    [TestCase("5005 IN IP6 FF0E::11A/2", 5005, true, "IN", "IP6", "FF0E::11A/2", SdpAddressKind.Ipv6Multicast, "5005 IN IP6 FF0E::11A/2")]
    [TestCase("53020 IN X-HOST opaque-address", 53020, true, "IN", "X-HOST", "opaque-address", SdpAddressKind.Unknown, "53020 IN X-HOST opaque-address")]
    public void RtcpEndpoint_DecodesOptionalAddressAndEncodesCanonically(string value, int port, bool hasAddress, string networkType, string addressType, string address, SdpAddressKind addressKind, string canonicalValue)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "RTCP", Value = value, RawText = "RTCP:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpRtcpAttribute projection = decoded.RtcpAttribute ?? new SdpRtcpAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.Port, Is.EqualTo(port));
            Assert.That(projection.HasNetworkAddress, Is.EqualTo(hasAddress));
            Assert.That(projection.NetworkType, Is.EqualTo(networkType));
            Assert.That(projection.AddressType, Is.EqualTo(addressType));
            Assert.That(projection.Address, Is.EqualTo(address));
            Assert.That(projection.AddressKind, Is.EqualTo(addressKind));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.RtcpAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("rtcp:" + canonicalValue));
        });
    }

    [TestCase(SdpAttributeKind.Property, "", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "70000", "SDP_VALIDATION_PORT")]
    [TestCase(SdpAttributeKind.Value, "5004 IN IP4", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "5004 IN IP4 999.0.0.1", "SDP_VALIDATION_CONNECTION")]
    [TestCase(SdpAttributeKind.Value, "5004 IN IP6 2001:::1", "SDP_VALIDATION_CONNECTION")]
    [TestCase(SdpAttributeKind.Value, "5004 IN IP4 233.252.0.1/256", "SDP_VALIDATION_CONNECTION")]
    [TestCase(SdpAttributeKind.Value, "5004 IN IP4 233.252.0.1/64/0", "SDP_VALIDATION_CONNECTION")]
    [TestCase(SdpAttributeKind.Value, "5004 IN IP6 FF0E::11A/0", "SDP_VALIDATION_CONNECTION")]
    public void InvalidRtcpEndpoint_FailsWithStructuredDiagnostic(SdpAttributeKind kind, string value, string code)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "rtcp", Value = value, RawText = "rtcp:" + value };
        source.Location.LineNumber = 14;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.RtcpAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(14));
        });
    }

    [TestCase("* nack pli", "*", "nack", true, "pli", "* nack pli")]
    [TestCase("* nack", "*", "nack", false, "", "* nack")]
    [TestCase("96 ack rpsi", "96", "ack", true, "rpsi", "96 ack rpsi")]
    [TestCase("96 nack sli", "96", "nack", true, "sli", "96 nack sli")]
    [TestCase("96 nack rpsi", "96", "nack", true, "rpsi", "96 nack rpsi")]
    [TestCase("96 nack app opaque-data", "96", "nack", true, "app opaque-data", "96 nack app opaque-data")]
    [TestCase(" 96   ccm   fir ", "96", "ccm", true, "fir", "96 ccm fir")]
    [TestCase("96 ccm tmmbr", "96", "ccm", true, "tmmbr", "96 ccm tmmbr")]
    [TestCase("96 ccm tmmbn", "96", "ccm", true, "tmmbn", "96 ccm tmmbn")]
    [TestCase("96 ccm tstr", "96", "ccm", true, "tstr", "96 ccm tstr")]
    [TestCase("96 ccm tstn", "96", "ccm", true, "tstn", "96 ccm tstn")]
    [TestCase("96 ccm vbcm", "96", "ccm", true, "vbcm", "96 ccm vbcm")]
    [TestCase("111 transport-cc", "111", "transport-cc", false, "", "111 transport-cc")]
    [TestCase("111 goog-remb", "111", "goog-remb", false, "", "111 goog-remb")]
    [TestCase("96 trr-int 0", "96", "trr-int", true, "0", "96 trr-int 0")]
    [TestCase("98 x-vendor mode opaque data", "98", "x-vendor", true, "mode opaque data", "98 x-vendor mode opaque data")]
    public void RtcpFeedback_DecodesStandardAndExtensionForms(string value, string payload, string type, bool hasParameter, string parameter, string canonicalValue)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "RTCP-FB", Value = value, RawText = "RTCP-FB:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpRtcpFeedbackAttribute projection = decoded.RtcpFeedbackAttribute ?? new SdpRtcpFeedbackAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.PayloadType, Is.EqualTo(payload));
            Assert.That(projection.Type, Is.EqualTo(type));
            Assert.That(projection.HasParameter, Is.EqualTo(hasParameter));
            Assert.That(projection.Parameter, Is.EqualTo(parameter));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.RtcpFeedbackAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("rtcp-fb:" + canonicalValue));
        });
    }

    [TestCase("01 nack")]
    [TestCase("128 nack")]
    [TestCase("payload nack")]
    public void InvalidRtcpFeedbackPayload_UsesPayloadDiagnostic(string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtcp-fb", Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.RtcpFeedbackAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationPayloadType));
        });
    }

    [TestCase("96")]
    [TestCase("96 bad.type")]
    [TestCase("96 trr-int")]
    [TestCase("96 trr-int ten")]
    [TestCase("96 nack bad:param")]
    public void InvalidRtcpFeedbackForm_ReturnsStructuredFailure(string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtcp-fb", Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
        });
    }

    [TestCase("", 0, "")]
    [TestCase("pkt-loss-rle=120   stat-summary=loss,jitt\tx-future=opaque", 3, "pkt-loss-rle=120 stat-summary=loss,jitt x-future=opaque")]
    [TestCase("pkt-loss-rle pkt-dup-rle=80 pkt-rcpt-times rcvr-rtt=sender:64 stat-summary=loss,dup,jitt,TTL voip-metrics", 6, "pkt-loss-rle pkt-dup-rle=80 pkt-rcpt-times rcvr-rtt=sender:64 stat-summary=loss,dup,jitt,TTL voip-metrics")]
    public void RtcpExtendedReports_PreserveRawTextAndUnknownBlocks(string value, int blockCount, string canonicalValue)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "RTCP-XR", Value = value, RawText = "RTCP-XR:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpRtcpExtendedReportAttribute projection = decoded.RtcpExtendedReportAttribute ?? new SdpRtcpExtendedReportAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.RawValue, Is.EqualTo(value));
            Assert.That(projection.Blocks, Has.Count.EqualTo(blockCount));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.RtcpExtendedReportAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("rtcp-xr:" + canonicalValue));
        });
    }

    [Test]
    public void CallerConstructedRtcpProjections_ValidateInconsistentAndUnsafeState()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpRtcpAttribute incompleteAddress = new SdpRtcpAttribute { Port = 5005, NetworkType = "IN" };
        SdpRtcpFeedbackAttribute invalidFeedback = new SdpRtcpFeedbackAttribute { PayloadType = "96", Type = "trr-int" };
        SdpRtcpExtendedReportAttribute invalidReport = new SdpRtcpExtendedReportAttribute();
        invalidReport.Blocks.Add("bad block");

        SdpAttributeEncodeResult addressResult = codec.EncodeAttribute(incompleteAddress);
        SdpAttributeEncodeResult feedbackResult = codec.EncodeAttribute(invalidFeedback);
        SdpAttributeEncodeResult reportResult = codec.EncodeAttribute(invalidReport);

        Assert.Multiple(() =>
        {
            Assert.That(addressResult.Success, Is.False);
            Assert.That(addressResult.Attribute, Is.Null);
            Assert.That(feedbackResult.Success, Is.False);
            Assert.That(feedbackResult.Attribute, Is.Null);
            Assert.That(reportResult.Success, Is.False);
            Assert.That(reportResult.Attribute, Is.Null);
        });
    }
}
