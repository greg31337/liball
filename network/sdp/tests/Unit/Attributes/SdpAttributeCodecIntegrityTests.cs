namespace sdp.Tests.Unit.Attributes;

public class SdpAttributeCodecIntegrityTests
{
    [TestCase(SdpAttributeKind.Property, "sendrecv", "", "sendrecv")]
    [TestCase(SdpAttributeKind.Value, "tool", "codec/1.0", "tool:codec/1.0")]
    [TestCase(SdpAttributeKind.Value, "rtpmap", "96 VP8/90000", "rtpmap:96 VP8/90000")]
    [TestCase(SdpAttributeKind.Value, "fmtp", "96 profile-id=0", "fmtp:96 profile-id=0")]
    [TestCase(SdpAttributeKind.Value, "rtcp", "5005", "rtcp:5005")]
    [TestCase(SdpAttributeKind.Value, "rtcp-fb", "* nack pli", "rtcp-fb:* nack pli")]
    [TestCase(SdpAttributeKind.Value, "rtcp-xr", "", "rtcp-xr:")]
    [TestCase(SdpAttributeKind.Property, "rtcp-mux", "", "rtcp-mux")]
    [TestCase(SdpAttributeKind.Property, "rtcp-mux-only", "", "rtcp-mux-only")]
    [TestCase(SdpAttributeKind.Property, "rtcp-rsize", "", "rtcp-rsize")]
    [TestCase(SdpAttributeKind.Value, "ssrc", "1 cname:user@example.test", "ssrc:1 cname:user@example.test")]
    [TestCase(SdpAttributeKind.Value, "ssrc-group", "FID 1 2", "ssrc-group:FID 1 2")]
    [TestCase(SdpAttributeKind.Value, "msid", "stream track", "msid:stream track")]
    [TestCase(SdpAttributeKind.Value, "msid-semantic", "WMS stream", "msid-semantic:WMS stream")]
    [TestCase(SdpAttributeKind.Value, "source-filter", "incl IN IP4 232.3.4.5 192.0.2.10", "source-filter:incl IN IP4 232.3.4.5 192.0.2.10")]
    [TestCase(SdpAttributeKind.Value, "ts-refclk", "local", "ts-refclk:local")]
    [TestCase(SdpAttributeKind.Value, "mediaclk", "sender", "mediaclk:sender")]
    [TestCase(SdpAttributeKind.Value, "imageattr", "96 send *", "imageattr:96 send *")]
    [TestCase(SdpAttributeKind.Value, "group", "BUNDLE audio video", "group:BUNDLE audio video")]
    [TestCase(SdpAttributeKind.Value, "mid", "audio", "mid:audio")]
    [TestCase(SdpAttributeKind.Property, "bundle-only", "", "bundle-only")]
    [TestCase(SdpAttributeKind.Value, "ice-ufrag", "8hhY", "ice-ufrag:8hhY")]
    [TestCase(SdpAttributeKind.Value, "ice-pwd", "asd88fgpdd777uzjYhagZg", "ice-pwd:asd88fgpdd777uzjYhagZg")]
    [TestCase(SdpAttributeKind.Value, "ice-options", "trickle ice2", "ice-options:trickle ice2")]
    [TestCase(SdpAttributeKind.Property, "ice-lite", "", "ice-lite")]
    [TestCase(SdpAttributeKind.Property, "ice-mismatch", "", "ice-mismatch")]
    [TestCase(SdpAttributeKind.Property, "end-of-candidates", "", "end-of-candidates")]
    [TestCase(SdpAttributeKind.Value, "candidate", "1 1 UDP 2122260223 192.0.2.1 54400 typ host", "candidate:1 1 UDP 2122260223 192.0.2.1 54400 typ host")]
    [TestCase(SdpAttributeKind.Value, "remote-candidates", "1 192.0.2.1 54400 2 192.0.2.1 54401", "remote-candidates:1 192.0.2.1 54400 2 192.0.2.1 54401")]
    [TestCase(SdpAttributeKind.Value, "fingerprint", "sha-256 00:11:AA:FF", "fingerprint:sha-256 00:11:AA:FF")]
    [TestCase(SdpAttributeKind.Value, "setup", "actpass", "setup:actpass")]
    [TestCase(SdpAttributeKind.Value, "connection", "new", "connection:new")]
    [TestCase(SdpAttributeKind.Value, "tls-id", "abc3de65cddef001be82", "tls-id:abc3de65cddef001be82")]
    [TestCase(SdpAttributeKind.Value, "crypto", "1 AES_CM_128_HMAC_SHA1_80 inline:key", "crypto:1 AES_CM_128_HMAC_SHA1_80 inline:key")]
    [TestCase(SdpAttributeKind.Value, "extmap", "1 urn:ietf:params:rtp-hdrext:sdes:mid", "extmap:1 urn:ietf:params:rtp-hdrext:sdes:mid")]
    [TestCase(SdpAttributeKind.Property, "extmap-allow-mixed", "", "extmap-allow-mixed")]
    public void EveryDecodedProjection_RetainsSourceAndEncodesToGenericForm(SdpAttributeKind kind, string name, string value, string expectedRawText)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = kind,
            Name = name.ToUpperInvariant(),
            Value = value,
            RawText = "original-" + name
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.TypedAttribute!);
        object? projectionSource = decoded.TypedAttribute?.GetType().GetField("SourceAttribute")?.GetValue(decoded.TypedAttribute);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.TypedAttribute, Is.Not.Null);
            Assert.That(projectionSource, Is.SameAs(source));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.TypedAttribute, Is.SameAs(decoded.TypedAttribute));
            Assert.That(encoded.Attribute, Is.Not.SameAs(source));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo(expectedRawText));
            Assert.That(encoded.Diagnostics, Is.Empty);
        });
    }

    [TestCase(SdpAttributeKind.Value, "sendrecv", "")]
    [TestCase(SdpAttributeKind.Property, "tool", "")]
    [TestCase(SdpAttributeKind.Property, "rtpmap", "")]
    [TestCase(SdpAttributeKind.Property, "fmtp", "")]
    [TestCase(SdpAttributeKind.Property, "rtcp", "")]
    [TestCase(SdpAttributeKind.Property, "rtcp-fb", "")]
    [TestCase(SdpAttributeKind.Property, "rtcp-xr", "")]
    [TestCase(SdpAttributeKind.Value, "rtcp-mux", "")]
    [TestCase(SdpAttributeKind.Property, "ssrc", "")]
    [TestCase(SdpAttributeKind.Property, "ssrc-group", "")]
    [TestCase(SdpAttributeKind.Property, "msid", "")]
    [TestCase(SdpAttributeKind.Property, "msid-semantic", "")]
    [TestCase(SdpAttributeKind.Property, "source-filter", "")]
    [TestCase(SdpAttributeKind.Property, "ts-refclk", "")]
    [TestCase(SdpAttributeKind.Property, "mediaclk", "")]
    [TestCase(SdpAttributeKind.Property, "imageattr", "")]
    [TestCase(SdpAttributeKind.Property, "group", "")]
    [TestCase(SdpAttributeKind.Property, "mid", "")]
    [TestCase(SdpAttributeKind.Value, "bundle-only", "")]
    [TestCase(SdpAttributeKind.Property, "ice-ufrag", "")]
    [TestCase(SdpAttributeKind.Property, "ice-pwd", "")]
    [TestCase(SdpAttributeKind.Property, "ice-options", "")]
    [TestCase(SdpAttributeKind.Value, "ice-lite", "")]
    [TestCase(SdpAttributeKind.Value, "ice-mismatch", "")]
    [TestCase(SdpAttributeKind.Value, "end-of-candidates", "")]
    [TestCase(SdpAttributeKind.Property, "candidate", "")]
    [TestCase(SdpAttributeKind.Property, "remote-candidates", "")]
    [TestCase(SdpAttributeKind.Property, "fingerprint", "")]
    [TestCase(SdpAttributeKind.Property, "setup", "")]
    [TestCase(SdpAttributeKind.Property, "connection", "")]
    [TestCase(SdpAttributeKind.Property, "tls-id", "")]
    [TestCase(SdpAttributeKind.Property, "crypto", "")]
    [TestCase(SdpAttributeKind.Property, "extmap", "")]
    [TestCase(SdpAttributeKind.Value, "extmap-allow-mixed", "")]
    public void EveryFailedTypedDecode_RetainsOpaqueSourceAndClearsProjections(SdpAttributeKind kind, string name, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = kind,
            Name = name,
            Value = value,
            RawText = "opaque:" + name + ":" + value
        };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute?.RawText, Is.EqualTo("opaque:" + name + ":" + value));
            Assert.That(result.TypedAttribute, Is.Null);
            AssertTypedProjectionsAreEmpty(result);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        });
    }

    static void AssertTypedProjectionsAreEmpty(SdpAttributeDecodeResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.CommonAttribute, Is.Null);
            Assert.That(result.RtpMapAttribute, Is.Null);
            Assert.That(result.FormatParametersAttribute, Is.Null);
            Assert.That(result.RtcpAttribute, Is.Null);
            Assert.That(result.RtcpFeedbackAttribute, Is.Null);
            Assert.That(result.RtcpExtendedReportAttribute, Is.Null);
            Assert.That(result.SynchronizationSourceAttribute, Is.Null);
            Assert.That(result.SourceGroupAttribute, Is.Null);
            Assert.That(result.MediaStreamId, Is.Null);
            Assert.That(result.MediaStreamSemanticAttribute, Is.Null);
            Assert.That(result.SourceFilterAttribute, Is.Null);
            Assert.That(result.ClockReferenceAttribute, Is.Null);
            Assert.That(result.MediaClockAttribute, Is.Null);
            Assert.That(result.ImageAttribute, Is.Null);
            Assert.That(result.GroupAttribute, Is.Null);
            Assert.That(result.MediaIdAttribute, Is.Null);
            Assert.That(result.IceUsernameFragmentAttribute, Is.Null);
            Assert.That(result.IcePasswordAttribute, Is.Null);
            Assert.That(result.IceOptionsAttribute, Is.Null);
            Assert.That(result.IceCandidate, Is.Null);
            Assert.That(result.RemoteCandidatesAttribute, Is.Null);
            Assert.That(result.FingerprintAttribute, Is.Null);
            Assert.That(result.SetupAttribute, Is.Null);
            Assert.That(result.ConnectionAttribute, Is.Null);
            Assert.That(result.TlsIdAttribute, Is.Null);
            Assert.That(result.CryptoAttribute, Is.Null);
            Assert.That(result.ExtMapAttribute, Is.Null);
        });
    }
}
