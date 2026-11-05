namespace sdp.Tests.Conformance.DataChannel;

public class SdpBfcpRfc8856ConformanceTests
{
    [TestCase("TCP/BFCP", "1")]
    [TestCase("TCP/DTLS/BFCP", "1")]
    [TestCase("TCP/TLS/BFCP", "1")]
    [TestCase("UDP/BFCP", "2")]
    [TestCase("UDP/TLS/BFCP", "2")]
    public void CurrentBfcpTransports_ProjectTheirWireVersionDefault(string protocol, string expectedVersion)
    {
        SdpSessionDescription description = Parse(Header()
            + "m=application 50000 " + protocol + " *\r\n"
            + "c=IN IP4 192.0.2.1\r\n");

        SdpBfcpDescription? projection = new SdpDataChannelService().ResolveMediaBfcp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(projection, Is.Not.Null);
            Assert.That(projection!.Versions, Is.EqualTo(new[] { expectedVersion }));
        });
    }

    [Test]
    public void Rfc8856ServerOffer_ParsesProjectsAndValidatesIdentifiersAndFloors()
    {
        SdpSessionDescription description = Parse(Header()
            + "m=application 50000 TCP/TLS/BFCP *\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=setup:actpass\r\n"
            + "a=connection:new\r\n"
            + "a=floorctrl:c-only s-only\r\n"
            + "a=confid:4294967295\r\n"
            + "a=userid:65535\r\n"
            + "a=floorid:65535 mstrm:10 11\r\n"
            + "a=bfcpver:1 2\r\n"
            + "m=audio 50002 RTP/AVP 0\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=label:10\r\n"
            + "m=video 50004 RTP/AVP 31\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=label:11\r\n");
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpValidationResult validation = Validate(description, SdpNegotiationRole.Offerer, null);
        SdpBfcpDescription? projection = service.ResolveMediaBfcp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(validation.Issues, Is.Empty);
            Assert.That(projection, Is.Not.Null);
            Assert.That(projection!.FloorControl, Is.EqualTo("c-only s-only"));
            Assert.That(projection.ConferenceId, Is.EqualTo("4294967295"));
            Assert.That(projection.UserId, Is.EqualTo("65535"));
            Assert.That(projection.FloorIds, Is.EqualTo(new[] { "65535 mstrm:10 11" }));
            Assert.That(projection.Versions, Is.EqualTo(new[] { "1", "2" }));
            Assert.That(projection.SetupRole, Is.EqualTo(SdpDtlsSetupRole.ActPass));
            Assert.That(projection.ConnectionRole, Is.EqualTo("new"));
        });
    }

    [TestCase("c-only", "s-only")]
    [TestCase("s-only", "c-only")]
    [TestCase("c-only s-only", "c-only")]
    [TestCase("c-s", "s-only")]
    public void FloorControlOfferAnswerMatrix_AcceptsOppositeOfferedRoles(string offeredRole, string answeredRole)
    {
        SdpSessionDescription offer = Parse(BfcpDocument(SdpTransportProtocols.TcpBfcp, offeredRole, "1 2", true));
        SdpSessionDescription answer = Parse(BfcpDocument(SdpTransportProtocols.TcpBfcp, answeredRole, "1", true));

        SdpValidationResult validation = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(validation.Issues, Is.Empty);
        });
    }

    [TestCase("c-only", "c-only")]
    [TestCase("s-only", "s-only")]
    [TestCase("c-only", "c-only s-only")]
    [TestCase("c-only", "c-s")]
    public void FloorControlAnswer_RejectsSameOrMultipleRoles(string offeredRole, string answeredRole)
    {
        SdpSessionDescription offer = Parse(BfcpDocument(SdpTransportProtocols.TcpBfcp, offeredRole, "1 2", true));
        SdpSessionDescription answer = Parse(BfcpDocument(SdpTransportProtocols.TcpBfcp, answeredRole, "1", true));

        SdpValidationResult validation = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorControl), Is.True);
    }

    [Test]
    public void AnswerVersionTransportSetupAndReuse_MustRemainCompatible()
    {
        SdpSessionDescription offer = Parse(Header()
            + "m=application 50000 TCP/BFCP *\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=setup:passive\r\n"
            + "a=connection:new\r\n"
            + "a=floorctrl:c-only\r\n"
            + "a=bfcpver:1\r\n");
        SdpSessionDescription answer = Parse(Header()
            + "m=application 50000 TCP/TLS/BFCP *\r\n"
            + "c=IN IP4 192.0.2.2\r\n"
            + "a=setup:passive\r\n"
            + "a=connection:existing\r\n"
            + "a=floorctrl:s-only\r\n"
            + "a=confid:10\r\n"
            + "a=userid:20\r\n"
            + "a=floorid:1 mstrm:audio\r\n"
            + "a=bfcpver:2\r\n"
            + "m=audio 50002 RTP/AVP 0\r\n"
            + "c=IN IP4 192.0.2.2\r\n"
            + "a=label:audio\r\n");

        SdpValidationResult validation = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Issues.Any(issue => issue.FieldName == 'm' && issue.Detail.Contains("transport protocol", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.BfcpVersion && issue.Detail.Contains("offered", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.Setup && issue.Detail.Contains("compatible", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.Connection && issue.Detail.Contains("connection:new", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void IdentifierOverflowDuplicateFloorsAndUnknownLabels_AreIndependentErrors()
    {
        SdpSessionDescription description = Parse(Header()
            + "m=application 50000 UDP/BFCP *\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=floorctrl:s-only\r\n"
            + "a=confid:4294967296\r\n"
            + "a=userid:65536\r\n"
            + "a=floorid:65536 mstrm:missing\r\n"
            + "a=floorid:01 m-stream:missing\r\n"
            + "a=floorid:1 mstrm:missing\r\n"
            + "a=bfcpver:2\r\n");

        SdpValidationResult validation = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.ConferenceId && issue.Detail.Contains("32-bit", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.UserId && issue.Detail.Contains("16-bit", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorId && issue.Detail.Contains("16-bit", StringComparison.Ordinal)), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorId && issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorId && issue.Detail.Contains("existing media-level label", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void LegacyFloorPointerAndDecimalVersionSpellingsRemainInteroperable()
    {
        SdpSessionDescription offer = Parse(Header()
            + "m=application 50000 UDP/BFCP *\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=floorctrl:c-only\r\n"
            + "a=bfcpver:01 02\r\n");
        SdpSessionDescription answer = Parse(Header()
            + "m=application 50000 UDP/BFCP *\r\n"
            + "c=IN IP4 192.0.2.2\r\n"
            + "a=floorctrl:s-only\r\n"
            + "a=confid:00010\r\n"
            + "a=userid:00020\r\n"
            + "a=floorid:00001 m-stream:audio\r\n"
            + "a=bfcpver:02\r\n"
            + "m=audio 50002 RTP/AVP 0\r\n"
            + "c=IN IP4 192.0.2.2\r\n"
            + "a=label:audio\r\n");

        SdpValidationResult validation = Validate(answer, SdpNegotiationRole.Answerer, offer);
        SdpBfcpDescription? projection = new SdpDataChannelService().ResolveMediaBfcp(answer, 0);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(projection, Is.Not.Null);
            Assert.That(projection!.ConferenceId, Is.EqualTo("00010"));
            Assert.That(projection.FloorIds, Is.EqualTo(new[] { "00001 m-stream:audio" }));
            Assert.That(projection.Versions, Is.EqualTo(new[] { "02" }));
        });
    }

    static string Header()
    {
        return "v=0\r\n"
            + "o=- 0 0 IN IP4 192.0.2.1\r\n"
            + "s=-\r\n"
            + "t=0 0\r\n";
    }

    static string BfcpDocument(string protocol, string role, string versions, bool includeServerData)
    {
        string serverData = string.Empty;
        bool serverRole = role.Contains("s-only", StringComparison.Ordinal) || role == "c-s";
        if (includeServerData && serverRole)
        {
            serverData = "a=confid:10\r\n"
                + "a=userid:20\r\n"
                + "a=floorid:1 mstrm:media-label\r\n";
        }
        string controlledMedia = string.Empty;
        if (serverData.Length > 0)
        {
            controlledMedia = "m=audio 50002 RTP/AVP 0\r\n"
                + "c=IN IP4 192.0.2.1\r\n"
                + "a=label:media-label\r\n";
        }
        return Header()
            + "m=application 50000 " + protocol + " *\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "a=floorctrl:" + role + "\r\n"
            + serverData
            + "a=bfcpver:" + versions + "\r\n"
            + controlledMedia;
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
            Profile = SdpValidationProfile.Bfcp,
            NegotiationRole = role,
            OfferDescription = offer
        };
        return new SdpValidator().ValidateDescription(description, options);
    }
}
