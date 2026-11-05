namespace sdp.Tests.Conformance.Rtp;

public class SdpDownloadedRfcFixtureTests
{
    [TestCase("RFC 2198", "m=audio 12345 RTP/AVP 121 0 5\r\na=rtpmap:121 red/8000/1\r\na=fmtp:121 0/5\r\n", "red")]
    [TestCase("RFC 3389", "m=audio 49230 RTP/AVP 101 102\r\na=rtpmap:101 G7221/16000\r\na=fmtp:101 bitrate=24000\r\na=rtpmap:102 CN/16000\r\n", "CN")]
    [TestCase("RFC 4588", "m=video 49174 RTP/AVPF 96 98\r\na=rtpmap:96 H264/90000\r\na=rtpmap:98 rtx/90000\r\na=fmtp:98 apt=96;rtx-time=3000\r\n", "rtx")]
    [TestCase("RFC 4733", "m=audio 12346 RTP/AVP 0 100\r\na=rtpmap:0 PCMU/8000\r\na=rtpmap:100 telephone-event/8000\r\na=fmtp:100 0-15,66,70\r\n", "telephone-event")]
    [TestCase("RFC 5109", "m=video 49170 RTP/AVP 96 97\r\na=rtpmap:96 red/90000\r\na=rtpmap:97 ulpfec/90000\r\na=fmtp:96 97/97\r\n", "ulpfec")]
    [TestCase("RFC 6184", "m=video 49170 RTP/AVP 98\r\na=rtpmap:98 H264/90000\r\na=fmtp:98 profile-level-id=42e01f;packetization-mode=1\r\n", "H264")]
    [TestCase("RFC 7587", "m=audio 54312 RTP/AVP 101\r\na=rtpmap:101 opus/48000/2\r\na=fmtp:101 maxplaybackrate=16000;sprop-maxcapturerate=16000;maxaveragebitrate=20000;stereo=1;useinbandfec=1;usedtx=0\r\na=ptime:40\r\na=maxptime:40\r\n", "opus")]
    [TestCase("RFC 7741", "m=video 49170 RTP/AVPF 98\r\na=rtpmap:98 VP8/90000\r\na=fmtp:98 max-fr=30;max-fs=3600\r\n", "VP8")]
    [TestCase("RFC 7798", "m=video 49170 RTP/AVP 98\r\na=rtpmap:98 H265/90000\r\na=fmtp:98 profile-id=1;level-id=93\r\n", "H265")]
    public void PublishedCodecFixtures_ProjectValidateAndCanonicalRoundTrip(string rfc, string mediaLines, string expectedEncoding)
    {
        string text = Header("192.0.2.1") + mediaLines;

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.Rtp, SdpNegotiationRole.Unknown);
        List<SdpCodec> codecs = new SdpCodecService().ResolveMediaCodecs(description, 0);

        Assert.That(codecs.Any(codec => string.Equals(codec.EncodingName, expectedEncoding, StringComparison.OrdinalIgnoreCase)), Is.True, rfc);
    }

    [Test]
    public void PublishedRtcpFixtures_ProjectEndpointFeedbackReportsAndMux()
    {
        string text = Header("126.16.64.4")
            + "m=audio 49170 RTP/AVPF 0 96\r\n"
            + "a=rtcp:53020 IN IP4 126.16.64.4\r\n"
            + "a=rtpmap:0 PCMU/8000\r\n"
            + "a=rtpmap:96 telephone-event/8000\r\n"
            + "a=fmtp:96 0-16\r\n"
            + "a=rtcp-fb:96 nack\r\n"
            + "a=rtcp-xr:rcvr-rtt=all:10000\r\n"
            + "a=rtcp-mux\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.Rtp, SdpNegotiationRole.Unknown);
        List<SdpAttributeDecodeResult> decoded = DecodeMediaAttributes(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Single(result => result.SourceAttribute!.Name == SdpAttributeNames.Rtcp).RtcpAttribute!.Port, Is.EqualTo(53020), "RFC 3605");
            Assert.That(decoded.Single(result => result.SourceAttribute!.Name == SdpAttributeNames.RtcpFeedback).RtcpFeedbackAttribute!.Type, Is.EqualTo("nack"), "RFC 4585");
            Assert.That(decoded.Single(result => result.SourceAttribute!.Name == SdpAttributeNames.RtcpExtendedReports).RtcpExtendedReportAttribute, Is.Not.Null, "RFC 3611");
            Assert.That(decoded.Single(result => result.SourceAttribute!.Name == SdpAttributeNames.RtcpMux).Success, Is.True, "RFC 5761");
        });
    }

    [Test]
    public void Rfc9143BundleOffer_PreservesGroupingAndRfc8285MidExtensions()
    {
        string text =
            "v=0\r\n"
            + "o=alice 2890844526 2890844526 IN IP6 2001:db8::3\r\n"
            + "s=-\r\n"
            + "c=IN IP6 2001:db8::3\r\n"
            + "t=0 0\r\n"
            + "a=group:BUNDLE foo bar\r\n"
            + "m=audio 10000 RTP/AVP 0 8 97\r\n"
            + "b=AS:200\r\n"
            + "a=mid:foo\r\n"
            + "a=rtcp-mux\r\n"
            + "a=rtpmap:0 PCMU/8000\r\n"
            + "a=rtpmap:8 PCMA/8000\r\n"
            + "a=rtpmap:97 iLBC/8000\r\n"
            + "a=extmap:1 urn:ietf:params:rtp-hdrext:sdes:mid\r\n"
            + "m=video 10002 RTP/AVP 31 32\r\n"
            + "b=AS:1000\r\n"
            + "a=mid:bar\r\n"
            + "a=rtcp-mux\r\n"
            + "a=rtpmap:31 H261/90000\r\n"
            + "a=rtpmap:32 MPV/90000\r\n"
            + "a=extmap:1 urn:ietf:params:rtp-hdrext:sdes:mid\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.Bundle, SdpNegotiationRole.Offerer);
        SdpValidationResult rtpValidation = Validate(description, SdpValidationProfile.Rtp, SdpNegotiationRole.Unknown);
        SdpGroupAttribute group = new SdpGroupService().ResolveGroups(description).Single();
        SdpBundleGroup bundle = new SdpBundleService().ResolveBundleGroups(description).Single();
        ISdpExtMapService extMaps = new SdpExtMapService();

        Assert.Multiple(() =>
        {
            Assert.That(rtpValidation.Success, Is.True, ValidationText(rtpValidation));
            Assert.That(group.Semantic, Is.EqualTo(SdpGroupSemantic.Bundle), "RFC 5888 and RFC 9143");
            Assert.That(group.IdentificationTags, Is.EqualTo(new[] { "foo", "bar" }));
            Assert.That(bundle.TaggedMediaId, Is.EqualTo("foo"));
            Assert.That(bundle.MediaIds, Is.EqualTo(new[] { "foo", "bar" }));
            Assert.That(extMaps.FindUriById(description, 0, 1), Is.EqualTo(SdpHeaderExtensionUris.MediaId), "RFC 8285");
            Assert.That(extMaps.FindUriById(description, 1, 1), Is.EqualTo(SdpHeaderExtensionUris.MediaId), "RFC 8285");
        });
    }

    [Test]
    public void Rfc5888LipSynchronizationFixture_PreservesOrderedMediaTags()
    {
        string text =
            "v=0\r\n"
            + "o=Laura 289083124 289083124 IN IP4 one.example.com\r\n"
            + "s=-\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "t=0 0\r\n"
            + "a=group:LS 1 2\r\n"
            + "m=audio 30000 RTP/AVP 0\r\n"
            + "a=mid:1\r\n"
            + "m=video 30002 RTP/AVP 31\r\n"
            + "a=mid:2\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.Bundle, SdpNegotiationRole.Unknown);
        SdpGroupAttribute group = new SdpGroupService().ResolveGroups(description).Single();

        Assert.Multiple(() =>
        {
            Assert.That(group.Semantic, Is.EqualTo(SdpGroupSemantic.LipSynchronization));
            Assert.That(group.IdentificationTags, Is.EqualTo(new[] { "1", "2" }));
            Assert.That(new SdpGroupService().FindMediaIndex(description, "2"), Is.EqualTo(1));
        });
    }

    [Test]
    public void Rfc8285ExtMapFixture_PreservesDirectionUriAndExtensionAttributes()
    {
        string text = Header("192.0.2.1")
            + "m=audio 49170 RTP/AVP 0\r\n"
            + "a=extmap:1 http://example.com/082005/ext.htm#ttime\r\n"
            + "a=extmap:2/sendrecv http://example.com/082005/ext.htm#xmeta short\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.Rtp, SdpNegotiationRole.Unknown);
        List<SdpExtMapAttribute> mappings = new SdpExtMapService().ResolveMediaExtMaps(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(mappings.Select(mapping => mapping.Id), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(mappings[0].HasDirection, Is.False);
            Assert.That(mappings[1].Direction, Is.EqualTo(SdpDirection.SendReceive));
            Assert.That(mappings[1].Uri, Is.EqualTo("http://example.com/082005/ext.htm#xmeta"));
            Assert.That(mappings[1].ExtensionAttributes, Is.EqualTo("short"));
        });
    }

    [Test]
    public void PublishedMsidSourceRidSimulcastAndImageFixture_ProjectsCompleteStreamGraph()
    {
        string text = Header("203.0.113.1")
            + "a=msid-semantic:WMS stream\r\n"
            + "m=video 49600 RTP/AVPF 97 98 99\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + "a=rtpmap:98 H264/90000\r\n"
            + "a=rtpmap:99 VP8/90000\r\n"
            + "a=fmtp:97 profile-level-id=42c01f;max-fs=3600;max-mbps=108000\r\n"
            + "a=fmtp:98 profile-level-id=42c00b;max-fs=240;max-mbps=3600\r\n"
            + "a=fmtp:99 max-fs=240;max-fr=30\r\n"
            + "a=msid:stream track\r\n"
            + "a=ssrc-group:FID 11111 22222\r\n"
            + "a=ssrc:11111 cname:user3@example.com\r\n"
            + "a=ssrc:11111 msid:stream track\r\n"
            + "a=ssrc:22222 cname:user3@example.com\r\n"
            + "a=ssrc:22222 msid:stream track\r\n"
            + "a=imageattr:97 send [x=1280,y=720] recv [x=1280,y=720]\r\n"
            + "a=imageattr:98 send [x=320,y=180] recv [x=320,y=180]\r\n"
            + "a=rid:1 send pt=97;max-width=1280;max-height=720\r\n"
            + "a=rid:2 send pt=98;max-width=320;max-height=180\r\n"
            + "a=rid:3 send pt=99;max-width=320;max-height=180\r\n"
            + "a=rid:4 recv pt=97\r\n"
            + "a=simulcast:send 1;2,3 recv 4\r\n"
            + "a=extmap:1 urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.Rtp, SdpNegotiationRole.Unknown);
        ISdpStreamService streams = new SdpStreamService();

        Assert.Multiple(() =>
        {
            Assert.That(streams.ResolveMediaStreamSemantics(description).Single().StreamIds, Is.EqualTo(new[] { "stream" }), "RFC 8830");
            Assert.That(streams.ResolveMediaStreamIds(description, 0), Has.Count.EqualTo(3), "RFC 8830");
            Assert.That(streams.ResolveSourceGroups(description, 0).Single().SynchronizationSources, Is.EqualTo(new uint[] { 11111u, 22222u }), "RFC 5576");
            Assert.That(streams.ResolveSourceAttributes(description, 0), Has.Count.EqualTo(4), "RFC 5576");
            Assert.That(streams.ResolveRids(description, 0).Select(rid => rid.Id), Is.EqualTo(new[] { "1", "2", "3", "4" }), "RFC 8851");
            Assert.That(streams.ResolveSimulcast(description, 0)!.SendStreams, Has.Count.EqualTo(2), "RFC 8853");
            Assert.That(streams.ResolveImageAttributes(description, 0), Has.Count.EqualTo(2), "RFC 6236");
        });
    }

    [Test]
    public void PublishedSourceFilterAndClockFixtures_ProjectInheritedClockState()
    {
        string text =
            "v=0\r\n"
            + "o=jdoe 2890844526 2890842807 IN IP4 192.0.2.10\r\n"
            + "s=SDP Seminar\r\n"
            + "c=IN IP4 233.252.0.1/64\r\n"
            + "t=2873397496 2873404696\r\n"
            + "a=source-filter:incl IN IP4 233.252.0.1 192.0.2.10\r\n"
            + "a=ts-refclk:ntp=/traceable/\r\n"
            + "m=audio 5004 RTP/AVP 96\r\n"
            + "a=rtpmap:96 L24/44100/2\r\n"
            + "a=sendonly\r\n"
            + "a=ts-refclk:ptp=IEEE1588-2008:39-A7-94-FF-FE-07-CB-D0:0\r\n"
            + "a=mediaclk:direct=963214424 rate=1000/1001\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.Rtp, SdpNegotiationRole.Unknown);
        SdpAttributeDecodeResult sourceFilter = new SdpAttributeCodec().DecodeAttribute(description.Attributes[0]);
        ISdpQueryService query = new SdpQueryService();
        List<SdpClockReferenceAttribute> references = query.ResolveEffectiveClockReferences(description, 0);
        List<SdpMediaClockAttribute> mediaClocks = query.ResolveEffectiveMediaClocks(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(sourceFilter.SourceFilterAttribute!.DestinationAddress, Is.EqualTo("233.252.0.1"), "RFC 4570");
            Assert.That(sourceFilter.SourceFilterAttribute.SourceAddresses, Is.EqualTo(new[] { "192.0.2.10" }));
            Assert.That(references.Single().SourceKind, Is.EqualTo(SdpClockSourceKind.Ptp), "RFC 7273");
            Assert.That(mediaClocks.Single().ClockValue, Is.EqualTo("963214424"), "RFC 7273");
            Assert.That(mediaClocks.Single().RateNumerator, Is.EqualTo(1000));
            Assert.That(mediaClocks.Single().RateDenominator, Is.EqualTo(1001));
        });
    }

    static string Header(string address)
    {
        return "v=0\r\n"
            + "o=- 12345 1 IN IP4 " + address + "\r\n"
            + "s=Downloaded RFC conformance\r\n"
            + "c=IN IP4 " + address + "\r\n"
            + "t=0 0\r\n";
    }

    static SdpSessionDescription AssertCanonicalRoundTrip(string text, SdpValidationProfile profile, SdpNegotiationRole role)
    {
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(text, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!, profile, role);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = new SdpParser().ParseText(written.Text, new SdpParseOptions());
        SdpValidationResult roundTripValidation = Validate(reparsed.Description!, profile, role);
        SdpComparisonResult comparison = new SdpComparer().CompareDescriptions(parsed.Description!, reparsed.Description!, new SdpComparisonOptions
        {
            CompareSemanticValues = true,
            IgnoreInsignificantOrdering = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, DiagnosticText(parsed));
            Assert.That(validation.Success, Is.True, ValidationText(validation));
            Assert.That(written.Success, Is.True, WriteDiagnosticText(written));
            Assert.That(written.Text, Is.EqualTo(text));
            Assert.That(reparsed.Success, Is.True, DiagnosticText(reparsed));
            Assert.That(roundTripValidation.Success, Is.True, ValidationText(roundTripValidation));
            Assert.That(comparison.Equal, Is.True, DifferenceText(comparison));
        });
        return reparsed.Description!;
    }

    static List<SdpAttributeDecodeResult> DecodeMediaAttributes(SdpSessionDescription description, int mediaIndex)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        return description.MediaDescriptions[mediaIndex].Attributes.Select(codec.DecodeAttribute).ToList();
    }

    static SdpValidationResult Validate(SdpSessionDescription description, SdpValidationProfile profile, SdpNegotiationRole role)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions
        {
            Profile = profile,
            NegotiationRole = role
        });
    }

    static string DiagnosticText(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string WriteDiagnosticText(SdpWriteResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string ValidationText(SdpValidationResult result)
    {
        return string.Join("|", result.Issues.Select(issue => issue.AttributeName + ":" + issue.Detail));
    }

    static string DifferenceText(SdpComparisonResult result)
    {
        return string.Join("|", result.Differences.Select(difference => difference.Path.AttributeName + ":" + difference.Detail));
    }
}
