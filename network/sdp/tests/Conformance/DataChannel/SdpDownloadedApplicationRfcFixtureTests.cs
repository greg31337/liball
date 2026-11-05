namespace sdp.Tests.Conformance.DataChannel;

public class SdpDownloadedApplicationRfcFixtureTests
{
    [Test]
    public void Rfc8841SctpFixture_ProjectsPortAndMaximumMessageSize()
    {
        string text = Header()
            + "m=application 12345 UDP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=sctp-port:5000\r\n"
            + "a=max-message-size:100000\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.DataChannel);
        SdpSctpDescription sctp = new SdpDataChannelService().ResolveMediaSctp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(sctp.Port, Is.EqualTo(5000));
            Assert.That(sctp.Format, Is.EqualTo(SdpDataChannelTokens.WebRtcDataChannel));
            Assert.That(sctp.HasMaximumMessageSize, Is.True);
            Assert.That(sctp.MaximumMessageSize, Is.EqualTo(100000));
        });
    }

    [Test]
    public void Rfc8864DataChannelFixture_PreservesMappingsAndNestedAttributes()
    {
        string text = Header()
            + "m=application 10001 UDP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=max-message-size:100000\r\n"
            + "a=sctp-port:5000\r\n"
            + "a=setup:actpass\r\n"
            + "a=dcmap:0 subprotocol=\"bfcp\";label=\"bfcp\"\r\n"
            + "a=dcmap:2 subprotocol=\"msrp\";label=\"msrp\"\r\n"
            + "a=dcsa:2 accept-types:message/cpim text/plain\r\n"
            + "a=dcsa:2 path:msrp://alice.example.com:10001/2s93i93idj;dc\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.DataChannel);
        List<SdpDataChannelAttribute> channels = new SdpDataChannelService().ResolveDataChannels(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(channels.Select(channel => channel.ChannelId), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(channels.Select(channel => channel.Parameters.Single(parameter => parameter.Name == "subprotocol").Value), Is.EqualTo(new[] { "bfcp", "msrp" }));
            Assert.That(channels[1].SubprotocolAttributes.Select(attribute => attribute.RawText), Is.EqualTo(new[]
            {
                "accept-types:message/cpim text/plain",
                "path:msrp://alice.example.com:10001/2s93i93idj;dc"
            }));
        });
    }

    [Test]
    public void Rfc8856BfcpFixture_ProjectsConferenceFloorsAndControlledMedia()
    {
        string text = Header()
            + "m=application 50000 TCP/TLS/BFCP *\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=setup:actpass\r\n"
            + "a=connection:new\r\n"
            + "a=floorctrl:c-only s-only\r\n"
            + "a=confid:4321\r\n"
            + "a=userid:1234\r\n"
            + "a=floorid:1 mstrm:10\r\n"
            + "a=floorid:2 mstrm:11\r\n"
            + "a=bfcpver:1 2\r\n"
            + "m=audio 50002 RTP/AVP 0\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=label:10\r\n"
            + "m=video 50004 RTP/AVP 31\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=label:11\r\n";

        SdpSessionDescription description = AssertCanonicalRoundTrip(text, SdpValidationProfile.Bfcp);
        SdpBfcpDescription? bfcp = new SdpDataChannelService().ResolveMediaBfcp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(bfcp, Is.Not.Null);
            Assert.That(bfcp!.ConferenceId, Is.EqualTo("4321"));
            Assert.That(bfcp.UserId, Is.EqualTo("1234"));
            Assert.That(bfcp.FloorIds, Is.EqualTo(new[] { "1 mstrm:10", "2 mstrm:11" }));
            Assert.That(bfcp.Versions, Is.EqualTo(new[] { "1", "2" }));
            Assert.That(bfcp.SetupRole, Is.EqualTo(SdpDtlsSetupRole.ActPass));
        });
    }

    static string Header()
    {
        return "v=0\r\n"
            + "o=- 12345 1 IN IP4 192.0.2.1\r\n"
            + "s=Downloaded application RFC conformance\r\n"
            + "t=0 0\r\n";
    }

    static SdpSessionDescription AssertCanonicalRoundTrip(string text, SdpValidationProfile profile)
    {
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(text, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!, profile);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = new SdpParser().ParseText(written.Text, new SdpParseOptions());
        SdpValidationResult roundTripValidation = Validate(reparsed.Description!, profile);
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

    static SdpValidationResult Validate(SdpSessionDescription description, SdpValidationProfile profile)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions
        {
            Profile = profile,
            NegotiationRole = SdpNegotiationRole.Offerer
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
