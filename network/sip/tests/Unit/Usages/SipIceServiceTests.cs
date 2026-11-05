namespace sip.Tests;

public class SipIceServiceTests
{
    const string Password = "1234567890123456789012";

    [Test]
    public void InspectsIceCandidatesOptionsAndFutureAttributes()
    {
        SipSessionDescription description = Sdp(
            "a=ice-ufrag:abcd\r\n"
            + "a=ice-pwd:" + Password + "\r\n"
            + "a=ice-options:trickle renomination\r\n"
            + "a=ice-lite\r\n"
            + "a=ice-future:kept\r\n"
            + "m=audio 9 RTP/AVP 0\r\n"
            + "a=mid:audio\r\n"
            + "a=candidate:1 1 UDP 2130706431 192.0.2.1 5000 typ host generation 0\r\n"
            + "a=end-of-candidates\r\n");

        SipIceService service = new SipIceService();
        SipIceResult result = service.InspectIceDescription(description);
        result.Description!.Media[0].Candidates[0].Address = "changed";
        SipIceResult repeated = service.InspectIceDescription(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Description.UsernameFragment, Is.EqualTo("abcd"));
            Assert.That(result.Description.Password, Is.EqualTo(Password));
            Assert.That(result.Description.Options,
                Is.EqualTo(new string[] { "trickle", "renomination" }));
            Assert.That(result.Description.IceLite, Is.True);
            Assert.That(result.Description.ExtensionAttributes[0].Name,
                Is.EqualTo("ice-future"));
            Assert.That(result.Description.Media[0].Mid, Is.EqualTo("audio"));
            Assert.That(result.Description.Media[0].EndOfCandidates, Is.True);
            Assert.That(result.Description.Media[0].Candidates[0].Priority,
                Is.EqualTo(2130706431));
            Assert.That(result.Description.Media[0].Candidates[0].Extensions,
                Is.EqualTo(new string[] { "generation", "0" }));
            Assert.That(repeated.Description!.Media[0].Candidates[0].Address,
                Is.EqualTo("192.0.2.1"));
        });
    }

    [TestCase("a=ice-ufrag:abc\r\na=ice-pwd:" + Password + "\r\n")]
    [TestCase("a=ice-ufrag:abcd\r\na=ice-pwd:short\r\n")]
    [TestCase("a=ice-ufrag:abcd\r\na=ice-ufrag:efgh\r\na=ice-pwd:" + Password + "\r\n")]
    [TestCase("a=ice-ufrag:abcd\r\na=ice-pwd:" + Password + "\r\n"
        + "m=audio 9 RTP/AVP 0\r\na=mid:audio\r\n"
        + "a=candidate:1 1 UDP 999 address 70000 typ host\r\n")]
    public void RejectsMalformedIceAttributes(string attributes)
    {
        SipIceResult result = new SipIceService()
            .InspectIceDescription(Sdp(attributes));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
        });
    }

    [Test]
    public void AppliesTypedIceWhilePreservingUnrelatedSdpAttributes()
    {
        SipSessionDescription description = Sdp(
            "a=group:BUNDLE audio\r\n"
            + "a=ice-ufrag:old1\r\n"
            + "a=ice-pwd:abcdefghijklmnopqrstuv\r\n"
            + "m=audio 49170 RTP/AVP 0\r\n"
            + "a=sendrecv\r\n"
            + "a=mid:old\r\n");
        SipIceDescription ice = IceDescription();

        SipIceResult result = new SipIceService()
            .ApplyIceDescription(description, ice);
        string formatted = Format(result.SessionDescription!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(formatted, Does.Contain("a=group:BUNDLE audio\r\n"));
            Assert.That(formatted, Does.Contain("a=sendrecv\r\n"));
            Assert.That(formatted, Does.Contain("a=ice-ufrag:abcd\r\n"));
            Assert.That(formatted, Does.Contain("a=ice-options:trickle\r\n"));
            Assert.That(formatted, Does.Contain("a=mid:audio\r\n"));
            Assert.That(formatted, Does.Contain(
                "a=candidate:1 1 UDP 2130706431 192.0.2.1 5000 typ host"));
            Assert.That(formatted, Does.Not.Contain("ice-ufrag:old1"));
            Assert.That(Format(description), Does.Contain("ice-ufrag:old1"));
        });
    }

    [Test]
    public void CreatesAndParsesTrickleIceInfoFragment()
    {
        SipIceTrickleRequest request = new SipIceTrickleRequest();
        request.DialogId = new SipDialogId();
        request.DialogId.CallId = "dialog@example.com";
        request.DialogId.LocalTag = "local";
        request.DialogId.RemoteTag = "remote";
        request.Description = IceDescription();
        request.Description.Media[0].EndOfCandidates = true;
        request.Description.Media[0].ExtensionAttributes =
            new SipSdpAttribute[] { Attribute("x-future", "kept") };
        request.CorrelationId = "trickle-1";
        SipIceService service = new SipIceService();

        SipIceResult created = service.CreateTrickleInfo(request);
        SipIceResult parsed = service.ParseTrickleFragment(created.Fragment);
        string text = System.Text.Encoding.UTF8.GetString(created.Fragment);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True, created.ErrorMessage);
            Assert.That(created.InfoRequest!.Package,
                Is.EqualTo(SipOptionTags.TrickleIce));
            Assert.That(created.InfoRequest.ContentType!.Type + "/"
                + created.InfoRequest.ContentType.Subtype,
                Is.EqualTo(SipMediaTypes.TrickleIceSdpFragment));
            Assert.That(created.InfoRequest.CorrelationId,
                Is.EqualTo("trickle-1"));
            Assert.That(text, Does.Contain("m=audio 9 RTP/AVP 0\r\n"));
            Assert.That(text, Does.Contain("a=mid:audio\r\n"));
            Assert.That(text, Does.Contain("a=end-of-candidates\r\n"));
            Assert.That(text, Does.Contain("a=x-future:kept\r\n"));
            Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
            Assert.That(parsed.Description!.Media[0].Candidates,
                Has.Length.EqualTo(1));
            Assert.That(parsed.Description.Media[0].ExtensionAttributes[0].Name,
                Is.EqualTo("x-future"));
        });
    }

    [Test]
    public void MergesTrickledCandidatesByMidAndRejectsOtherGeneration()
    {
        SipSessionDescription current = Sdp(
            "a=ice-ufrag:abcd\r\n"
            + "a=ice-pwd:" + Password + "\r\n"
            + "a=ice-options:trickle\r\n"
            + "m=audio 9 RTP/AVP 0\r\n"
            + "a=mid:audio\r\n"
            + "a=candidate:1 1 UDP 2130706431 192.0.2.1 5000 typ host\r\n");
        byte[] fragment = System.Text.Encoding.UTF8.GetBytes(
            "a=ice-ufrag:abcd\r\n"
            + "a=ice-pwd:" + Password + "\r\n"
            + "m=audio 9 RTP/AVP 0\r\n"
            + "a=mid:audio\r\n"
            + "a=candidate:1 1 UDP 2130706431 192.0.2.1 5000 typ host\r\n"
            + "a=candidate:2 1 UDP 1694498815 198.51.100.1 6000 typ srflx raddr 192.0.2.1 rport 5000\r\n"
            + "a=end-of-candidates\r\n");
        byte[] wrongGeneration = System.Text.Encoding.UTF8.GetBytes(
            "a=ice-ufrag:wxyz\r\n"
            + "a=ice-pwd:abcdefghijklmnopqrstuv\r\n"
            + "m=audio 9 RTP/AVP 0\r\na=mid:audio\r\n"
            + "a=end-of-candidates\r\n");
        SipIceService service = new SipIceService();

        SipIceResult merged = service.ApplyTrickleFragment(current, fragment);
        SipIceResult rejected = service.ApplyTrickleFragment(
            current, wrongGeneration);

        Assert.Multiple(() =>
        {
            Assert.That(merged.Success, Is.True, merged.ErrorMessage);
            Assert.That(merged.Description!.Media[0].Candidates,
                Has.Length.EqualTo(2));
            Assert.That(merged.Description.Media[0].EndOfCandidates, Is.True);
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.StatusCode, Is.EqualTo(SipStatusCodes.Conflict));
            Assert.That(new SipIceService().InspectIceDescription(current)
                .Description!.Media[0].Candidates, Has.Length.EqualTo(1));
        });
    }

    [TestCase("a=ice-ufrag:abcd\r\na=ice-pwd:" + Password + "\r\n"
        + "a=candidate:1 1 UDP 1 192.0.2.1 5000 typ host\r\n")]
    [TestCase("m=audio 9 RTP/AVP 0\r\na=mid:audio\r\n"
        + "a=candidate:1 1 UDP 1 192.0.2.1 5000 typ host\r\n")]
    [TestCase("v=0\r\na=ice-ufrag:abcd\r\na=ice-pwd:" + Password + "\r\n")]
    public void RejectsMalformedTrickleFragments(string text)
    {
        SipIceResult result = new SipIceService().ParseTrickleFragment(
            System.Text.Encoding.UTF8.GetBytes(text));

        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
    }

    [Test]
    public void AddsAndInspectsIceOptionTagsWithoutMutatingMessage()
    {
        SipMessage message = Message(
            "Supported: timer\r\nRequire: 100rel\r\n");
        SipIceService service = new SipIceService();

        SipIceResult added = service.AddIceMessageOptions(
            message, true, true);
        SipIceResult inspected = service.InspectIceMessageOptions(
            added.Message!);

        Assert.Multiple(() =>
        {
            Assert.That(added.Success, Is.True, added.ErrorMessage);
            Assert.That(Header(added.Message!, SipHeaderNames.Supported),
                Is.EqualTo("timer, ice, trickle-ice"));
            Assert.That(Header(added.Message!, SipHeaderNames.Require),
                Is.EqualTo("100rel, trickle-ice"));
            Assert.That(inspected.IceSupported, Is.True);
            Assert.That(inspected.TrickleIceSupported, Is.True);
            Assert.That(inspected.TrickleIceRequired, Is.True);
            Assert.That(Header(message, SipHeaderNames.Supported),
                Is.EqualTo("timer"));
        });
    }

    [Test]
    public void ApplyingIcePreservesMediaIdentifierWhenNoReplacementIsGiven()
    {
        SipSessionDescription description = Sdp(
            "a=ice-ufrag:old1\r\n"
            + "a=ice-pwd:abcdefghijklmnopqrstuv\r\n"
            + "m=audio 49170 RTP/AVP 0\r\n"
            + "a=mid:audio\r\n"
            + "a=sendrecv\r\n"
            + "a=x-future:retained\r\n");
        SipIceDescription ice = IceDescription();
        ice.Media[0].Mid = null;
        ice.Options = new string[0];

        SipIceResult result = new SipIceService()
            .ApplyIceDescription(description, ice);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        string formatted = Format(result.SessionDescription!);

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Does.Contain("a=mid:audio\r\n"));
            Assert.That(formatted, Does.Contain("a=sendrecv\r\n"));
            Assert.That(formatted, Does.Contain("a=x-future:retained\r\n"));
            Assert.That(formatted, Does.Contain("a=ice-ufrag:abcd\r\n"));
            Assert.That(Format(description), Does.Contain("a=ice-ufrag:old1\r\n"));
        });
    }

    static SipIceDescription IceDescription()
    {
        SipIceDescription description = new SipIceDescription();
        description.UsernameFragment = "abcd";
        description.Password = Password;
        description.Options = new string[] { "trickle" };
        SipIceMedia media = new SipIceMedia();
        media.Mid = "audio";
        media.Candidates = new SipIceCandidate[] { Candidate() };
        description.Media = new SipIceMedia[] { media };
        return description;
    }

    static SipIceCandidate Candidate()
    {
        SipIceCandidate candidate = new SipIceCandidate();
        candidate.Foundation = "1";
        candidate.Component = 1;
        candidate.Transport = "UDP";
        candidate.Priority = 2130706431;
        candidate.Address = "192.0.2.1";
        candidate.Port = 5000;
        candidate.Type = "host";
        return candidate;
    }

    static SipSdpAttribute Attribute(string name, string value)
    {
        SipSdpAttribute attribute = new SipSdpAttribute();
        attribute.Name = name;
        attribute.Value = value;
        attribute.HasValue = true;
        return attribute;
    }

    static SipSessionDescription Sdp(string lines)
    {
        string text = "v=0\r\n"
            + "o=- 1 1 IN IP4 192.0.2.1\r\n"
            + "s=-\r\n"
            + "c=IN IP4 0.0.0.0\r\n"
            + "t=0 0\r\n" + lines;
        SipParseResult parsed = new SipSdpCodec().ParseSessionDescription(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.SessionDescription!;
    }

    static string Format(SipSessionDescription description)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipWriteResult result = new SipSdpCodec()
            .FormatSessionDescription(description, options);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return System.Text.Encoding.UTF8.GetString(result.Bytes);
    }

    static SipMessage Message(string headers)
    {
        string text = "INVITE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-ice\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: ice@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + headers + "Content-Length: 0\r\n\r\n";
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header => string.Equals(
            header.Name, name, System.StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }
}
