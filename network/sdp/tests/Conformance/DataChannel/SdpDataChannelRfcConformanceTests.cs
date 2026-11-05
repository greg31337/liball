namespace sdp.Tests.Conformance.DataChannel;

public class SdpDataChannelRfcConformanceTests
{
    [Test]
    public void ModernSctpDataChannel_ParsesProjectsAndValidatesNestedSignaling()
    {
        SdpSessionDescription description = Parse(Header()
            + "m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=setup:active\r\n"
            + "a=sctp-port:5000\r\n"
            + "a=max-message-size:0\r\n"
            + "a=dcmap:0 subprotocol=\"msrp\";label=\"Messages\";ordered=false;max-retr=5;priority=256;X-Future=one\r\n"
            + "a=dcsa:0 accept-types:message/cpim text/plain\r\n"
            + "a=dcsa:0 path:msrp://example.test/session;dc\r\n");
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpValidationResult validation = Validate(description, SdpNegotiationRole.Offerer, null);
        SdpSctpDescription sctp = service.ResolveMediaSctp(description, 0);
        List<SdpDataChannelAttribute> channels = service.ResolveDataChannels(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(validation.Issues, Is.Empty);
            Assert.That(sctp.Port, Is.EqualTo(5000));
            Assert.That(sctp.Format, Is.EqualTo(SdpDataChannelTokens.WebRtcDataChannel));
            Assert.That(sctp.HasMaximumMessageSize, Is.True);
            Assert.That(sctp.MaximumMessageSize, Is.Zero);
            Assert.That(channels, Has.Count.EqualTo(1));
            Assert.That(channels[0].ChannelId, Is.Zero);
            Assert.That(channels[0].Parameters.Select(parameter => parameter.Name), Is.EqualTo(new[]
            {
                "subprotocol", "label", "ordered", "max-retr", "priority", "X-Future"
            }));
            Assert.That(channels[0].SubprotocolAttributes.Select(attribute => attribute.RawText), Is.EqualTo(new[]
            {
                "accept-types:message/cpim text/plain", "path:msrp://example.test/session;dc"
            }));
        });
    }

    [TestCase("DTLS/SCTP")]
    [TestCase("SCTP/DTLS")]
    [TestCase("SCTP")]
    public void LegacySctpForms_ProjectApplicationAndIncomingStreamCount(string protocol)
    {
        SdpSessionDescription description = Parse(Header()
            + "m=application 9 " + protocol + " 5000\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=sctpmap:5000 webrtc-datachannel 16\r\n");
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpValidationResult validation = Validate(description, SdpNegotiationRole.Unknown, null);
        SdpSctpDescription sctp = service.ResolveMediaSctp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(sctp.LegacySctpMap, Is.True);
            Assert.That(sctp.Port, Is.EqualTo(5000));
            Assert.That(sctp.Format, Is.EqualTo(SdpDataChannelTokens.WebRtcDataChannel));
            Assert.That(sctp.HasMaximumStreams, Is.True);
            Assert.That(sctp.MaximumStreams, Is.EqualTo(16));
            Assert.That(sctp.HasMaximumMessageSize, Is.False);
            Assert.That(sctp.MaximumMessageSize, Is.EqualTo(SdpDataChannelTokens.DefaultMaximumMessageSize));
        });
    }

    [Test]
    public void MessageSize_DefaultUnlimitedAndFiniteCapabilitiesIntersectSafely()
    {
        SdpSessionDescription defaultDescription = Parse(ModernMedia(string.Empty));
        SdpSessionDescription unlimitedDescription = Parse(ModernMedia("a=max-message-size:0\r\n"));
        SdpSessionDescription finiteDescription = Parse(ModernMedia("a=max-message-size:200000\r\n"));
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpSctpDescription defaultSize = service.ResolveMediaSctp(defaultDescription, 0);
        SdpSctpDescription unlimited = service.ResolveMediaSctp(unlimitedDescription, 0);
        SdpSctpDescription finite = service.ResolveMediaSctp(finiteDescription, 0);
        SdpSctpDescription? defaultWithUnlimited = service.IntersectSctpCapabilities(defaultSize, unlimited);
        SdpSctpDescription? unlimitedWithFinite = service.IntersectSctpCapabilities(unlimited, finite);

        Assert.Multiple(() =>
        {
            Assert.That(defaultSize.HasMaximumMessageSize, Is.False);
            Assert.That(defaultSize.MaximumMessageSize, Is.EqualTo(65536));
            Assert.That(unlimited.HasMaximumMessageSize, Is.True);
            Assert.That(unlimited.MaximumMessageSize, Is.Zero);
            Assert.That(defaultWithUnlimited, Is.Not.Null);
            Assert.That(defaultWithUnlimited!.MaximumMessageSize, Is.EqualTo(65536));
            Assert.That(unlimitedWithFinite, Is.Not.Null);
            Assert.That(unlimitedWithFinite!.MaximumMessageSize, Is.EqualTo(200000));
        });
    }

    [Test]
    public void DataChannelCapabilityIntersection_IsExactAndRetainsNestedAttributes()
    {
        SdpSessionDescription description = Parse(Header()
            + "m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=sctp-port:5000\r\n"
            + "a=dcmap:0 subprotocol=\"msrp\";ordered=true\r\n"
            + "a=dcsa:0 accept-types:text/plain\r\n"
            + "a=dcmap:2 subprotocol=\"MSRP\"\r\n"
            + "a=dcmap:4 label=\"No protocol\"\r\n");
        ISdpDataChannelService service = new SdpDataChannelService();
        List<SdpDataChannelAttribute> offered = service.ResolveDataChannels(description, 0);

        List<SdpDataChannelAttribute> intersection = service.IntersectDataChannelCapabilities(offered, new List<string> { "msrp", string.Empty });

        Assert.Multiple(() =>
        {
            Assert.That(intersection.Select(channel => channel.ChannelId), Is.EqualTo(new[] { 0, 4 }));
            Assert.That(intersection[0].SubprotocolAttributes.Single().RawText, Is.EqualTo("accept-types:text/plain"));
            Assert.That(intersection[0], Is.Not.SameAs(offered[0]));
            Assert.That(intersection[0].SubprotocolAttributes[0], Is.Not.SameAs(offered[0].SubprotocolAttributes[0]));
        });
    }

    [Test]
    public void InvalidModernLegacyMixAndChannelGraph_ReportIndependentCauses()
    {
        SdpSessionDescription description = Parse(Header()
            + "m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=setup:active\r\n"
            + "a=sctp-port:5000\r\n"
            + "a=sctpmap:5000 webrtc-datachannel 16\r\n"
            + "a=dcmap:0 max-retr=5;max-time=10\r\n"
            + "a=dcmap:0 ordered=maybe\r\n"
            + "a=dcsa:4 inactive\r\n");

        SdpValidationResult validation = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == "sctpmap" && issue.Detail.Contains("not applicable", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Detail.Contains("mutually exclusive", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Detail.Contains("true or false", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == "dcsa" && issue.Detail.Contains("reference", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void AnswerCannotChangeTransportReliabilityOrIntroduceChannelIdentifiers()
    {
        SdpSessionDescription offer = Parse(Header()
            + "m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=setup:active\r\n"
            + "a=sctp-port:5000\r\n"
            + "a=dcmap:0 subprotocol=\"msrp\";max-retr=5\r\n");
        SdpSessionDescription answer = Parse(Header()
            + "m=application 9 TCP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP4 192.0.2.2\r\n"
            + "a=setup:passive\r\n"
            + "a=sctp-port:5000\r\n"
            + "a=dcmap:0 subprotocol=\"msrp\";max-retr=6\r\n"
            + "a=dcmap:2 subprotocol=\"chat\"\r\n");

        SdpValidationResult validation = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Issues.Any(issue => issue.FieldName == 'm' && issue.Detail.Contains("transport protocol", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Detail.Contains("retain", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Detail.Contains("absent from the offer", StringComparison.Ordinal)), Is.True);
        });
    }

    static string Header()
    {
        return "v=0\r\n"
            + "o=- 0 0 IN IP4 192.0.2.1\r\n"
            + "s=-\r\n"
            + "t=0 0\r\n";
    }

    static string ModernMedia(string maximumSizeLine)
    {
        return Header()
            + "m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=sctp-port:5000\r\n"
            + maximumSizeLine;
    }

    static SdpSessionDescription Parse(string text)
    {
        SdpParseResult result = new SdpParser().ParseText(text, new SdpParseOptions());
        Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Detail)));
        return result.Description!;
    }

    static SdpValidationResult Validate(SdpSessionDescription description, SdpNegotiationRole role, SdpSessionDescription? offer)
    {
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.DataChannel,
            NegotiationRole = role,
            OfferDescription = offer
        };
        return new SdpValidator().ValidateDescription(description, options);
    }
}
