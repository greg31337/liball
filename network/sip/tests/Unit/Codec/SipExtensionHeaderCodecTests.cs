namespace sip.Tests;

public class SipExtensionHeaderCodecTests
{
    [Test]
    public void ParsesAndFormatsPathAndServiceRoute()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult path = Parse(codec,
            "Path: <sip:edge.EXAMPLE.com;lr>, <sip:backup.example.com>", new SipParseOptions());
        SipHeaderParseResult service = Parse(codec,
            "Service-Route: <sips:service.EXAMPLE.com;lr>", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(path.Header!, new SipWriteOptions());

        Assert.That(path.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(path.RouteValues, Has.Length.EqualTo(2));
        Assert.That(path.RouteValues[0].LooseRouting, Is.True);
        Assert.That(service.RouteValues.Single().Address!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sips));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("Path: <sip:edge.example.com;lr>, <sip:backup.example.com>\r\n"));
    }

    [Test]
    public void ParsesGruuOutboundAndPushContactParameters()
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(),
            "Contact: <sip:alice@example.com;ob;pn-provider=apns;pn-param=topic;pn-prid=device;pn-purr=ref>"
                + ";reg-id=2;+sip.instance=\"<urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6>\""
                + ";pub-gruu=\"sip:alice@example.com;gr=public\""
                + ";temp-gruu=\"sip:temp@example.com;gr\"", new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        SipContactValue contact = result.ContactValues.Single();
        Assert.Multiple(() =>
        {
            Assert.That(contact.Outbound, Is.True);
            Assert.That(contact.RegistrationId, Is.EqualTo(2));
            Assert.That(contact.InstanceId, Is.EqualTo("<urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6>"));
            Assert.That(contact.PublicGruu, Is.EqualTo("sip:alice@example.com;gr=public"));
            Assert.That(contact.TemporaryGruu, Is.EqualTo("sip:temp@example.com;gr"));
            Assert.That(contact.PushProvider, Is.EqualTo("apns"));
            Assert.That(contact.PushParameter, Is.EqualTo("topic"));
            Assert.That(contact.PushResourceId, Is.EqualTo("device"));
            Assert.That(contact.PushRegistrationReference, Is.EqualTo("ref"));
        });
    }

    [TestCase("Contact: <sip:a@example.com>;reg-id=0;+sip.instance=\"<urn:uuid:abc>\"")]
    [TestCase("Contact: <sip:a@example.com>;reg-id=1;+sip.instance=<urn:uuid:abc>")]
    [TestCase("Contact: <sip:a@example.com>;pub-gruu=sip:a@example.com")]
    [TestCase("Contact: <sip:a@example.com;ob=1>")]
    [TestCase("Contact: <sip:a@example.com;pn-prid>")]
    [TestCase("Contact: <sip:a@example.com;pn-provider=a;pn-provider=b>")]
    public void RejectsMalformedGruuOutboundAndPushParameters(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void ParsesReliableProvisionalResponseHeaders()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult rseq = Parse(codec, "RSeq: 00042", new SipParseOptions());
        SipHeaderParseResult rack = Parse(codec, "RAck: 42 0007 PRACK", new SipParseOptions());

        SipWriteResult formattedRseq = codec.FormatHeader(rseq.Header!, new SipWriteOptions());
        SipWriteResult formattedRack = codec.FormatHeader(rack.Header!, new SipWriteOptions());

        Assert.That(rseq.ExtensionValues[0].Number, Is.EqualTo(42));
        Assert.That(rack.ExtensionValues[0].Number, Is.EqualTo(42));
        Assert.That(rack.ExtensionValues[0].SecondNumber, Is.EqualTo(7));
        Assert.That(rack.ExtensionValues[0].Method, Is.EqualTo(SipMethod.Prack));
        Assert.That(System.Text.Encoding.ASCII.GetString(formattedRseq.Bytes), Is.EqualTo("RSeq: 42\r\n"));
        Assert.That(System.Text.Encoding.ASCII.GetString(formattedRack.Bytes), Is.EqualTo("RAck: 42 7 PRACK\r\n"));
    }

    [TestCase("RSeq: 0")]
    [TestCase("RSeq: -1")]
    [TestCase("RAck: 1 2")]
    [TestCase("RAck: 0 2 INVITE")]
    [TestCase("RAck: 1 x INVITE")]
    [TestCase("RAck: 1 2 bad method")]
    public void RejectsMalformedReliableResponseHeaders(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void ParsesSessionTimerHeadersAndCompactName()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult session = Parse(codec,
            "x: 01800;refresher=uas;foo=bar", new SipParseOptions());
        SipHeaderParseResult minimum = Parse(codec, "Min-SE: 090", new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult formatted = codec.FormatHeader(session.Header!, options);

        Assert.That(session.ExtensionValues[0].Number, Is.EqualTo(1800));
        Assert.That(session.ExtensionValues[0].SecondaryToken, Is.EqualTo("uas"));
        Assert.That(minimum.ExtensionValues[0].Number, Is.EqualTo(90));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("x: 1800;refresher=uas;foo=bar\r\n"));
    }

    [TestCase("Session-Expires: -1")]
    [TestCase("Session-Expires: 90;refresher=proxy")]
    [TestCase("Session-Expires: 90;refresher=uas;Refresher=uac")]
    [TestCase("Min-SE: value")]
    public void RejectsMalformedSessionTimerHeaders(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void ParsesReasonValuesAndCanonicalizesParameters()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult result = Parse(codec,
            "Reason: SIP;Cause=486;Text=\"Busy Here\", Q.850;cause=16;text=\"Terminated\"",
            new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(result.Header!, new SipWriteOptions());

        Assert.That(result.ExtensionValues, Has.Length.EqualTo(2));
        Assert.That(result.ExtensionValues[0].Token, Is.EqualTo("SIP"));
        Assert.That(result.ExtensionValues[0].Number, Is.EqualTo(486));
        Assert.That(result.ExtensionValues[0].SecondaryToken, Is.EqualTo("Busy Here"));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes), Is.EqualTo(
            "Reason: SIP;cause=486;text=\"Busy Here\", Q.850;cause=16;text=\"Terminated\"\r\n"));
    }

    [TestCase("Reason: SIP;cause=x")]
    [TestCase("Reason: SIP;text=unquoted")]
    [TestCase("Reason: SIP;cause=486;Cause=500")]
    public void RejectsMalformedReasonHeaders(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void ParsesEventPublicationAndSubscriptionHeaders()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult eventValue = Parse(codec, "o: presence.winfo;id=abc", new SipParseOptions());
        SipHeaderParseResult allowed = Parse(codec, "u: presence, dialog", new SipParseOptions());
        SipHeaderParseResult state = Parse(codec,
            "Subscription-State: terminated;reason=timeout;expires=0;retry-after=30", new SipParseOptions());
        SipHeaderParseResult etag = Parse(codec, "SIP-ETag: tag-1", new SipParseOptions());
        SipHeaderParseResult match = Parse(codec, "SIP-If-Match: tag-1", new SipParseOptions());

        Assert.That(eventValue.ExtensionValues[0].Token, Is.EqualTo("presence.winfo"));
        Assert.That(eventValue.ExtensionValues[0].SecondaryToken, Is.EqualTo("abc"));
        Assert.That(allowed.ExtensionValues.Select(value => value.Token),
            Is.EqualTo(new string[] { "presence", "dialog" }));
        Assert.That(state.ExtensionValues[0].SecondaryToken, Is.EqualTo("timeout"));
        Assert.That(state.ExtensionValues[0].Number, Is.EqualTo(0));
        Assert.That(state.ExtensionValues[0].SecondNumber, Is.EqualTo(30));
        Assert.That(etag.ExtensionValues[0].Token, Is.EqualTo("tag-1"));
        Assert.That(match.ExtensionValues[0].Token, Is.EqualTo("tag-1"));
    }

    [TestCase("Event: presence;id")]
    [TestCase("Subscription-State: active;expires=x")]
    [TestCase("SIP-ETag: tag;foo=bar")]
    [TestCase("SIP-If-Match: one,two")]
    public void RejectsMalformedEventAndPublicationHeaders(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void ParsesTransferHeadersAndCompactNames()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult referTo = Parse(codec,
            "r: \"Bob\" <sip:bob@EXAMPLE.com?Replaces=call%3Bto-tag%3Da>", new SipParseOptions());
        SipHeaderParseResult referredBy = Parse(codec,
            "b: <sip:alice@example.com>", new SipParseOptions());
        SipHeaderParseResult referSub = Parse(codec, "Refer-Sub: false", new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult formatted = codec.FormatHeader(referTo.Header!, options);

        Assert.That(referTo.ExtensionValues[0].Address!.DisplayName, Is.EqualTo("Bob"));
        Assert.That(referredBy.ExtensionValues[0].Uri!.Host, Is.EqualTo("example.com"));
        Assert.That(referSub.ExtensionValues[0].HasFlag, Is.True);
        Assert.That(referSub.ExtensionValues[0].Flag, Is.False);
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("r: Bob <sip:bob@example.com?Replaces=call%3Bto-tag%3Da>\r\n"));
    }

    [TestCase("Refer-To: <sip:a@example.com>,<sip:b@example.com>")]
    [TestCase("Refer-Sub: maybe")]
    public void RejectsMalformedTransferHeaders(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [TestCase("Replaces: call-1@example.com;to-tag=to1;from-tag=from1;early-only")]
    [TestCase("Join: call-2@example.com;to-tag=to2;from-tag=from2")]
    [TestCase("Target-Dialog: call-3@example.com;local-tag=local;remote-tag=remote")]
    public void ParsesDialogReferenceHeaders(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.ExtensionValues[0].Token, Does.StartWith("call-"));
        Assert.That(result.ExtensionValues[0].SecondaryToken, Is.Not.Empty);
        Assert.That(result.ExtensionValues[0].TertiaryToken, Is.Not.Empty);
    }

    [TestCase("Replaces: bad call;to-tag=a")]
    [TestCase("Join: call@example.com;to-tag")]
    [TestCase("Join: call@example.com;to-tag=a")]
    [TestCase("Target-Dialog: call@example.com;early-only=true")]
    public void RejectsMalformedDialogReferences(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void ParsesCallerPreferenceAndFeatureCapabilityHeaders()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult accept = Parse(codec,
            "a: *;+audio;mobility=fixed", new SipParseOptions());
        SipHeaderParseResult reject = Parse(codec,
            "j: *;+video", new SipParseOptions());
        SipHeaderParseResult disposition = Parse(codec,
            "d: proxy,redirect", new SipParseOptions());
        SipHeaderParseResult caps = Parse(codec,
            "Feature-Caps: *;+sip.pns=apns;+sip.pnsreg=10", new SipParseOptions());

        Assert.That(accept.ExtensionValues[0].Parameters, Has.Length.EqualTo(2));
        Assert.That(reject.ExtensionValues[0].Parameters[0].Name, Is.EqualTo("+video"));
        Assert.That(disposition.ExtensionValues.Select(value => value.Token),
            Is.EqualTo(new string[] { "proxy", "redirect" }));
        Assert.That(caps.ExtensionValues[0].Parameters[0].Name, Is.EqualTo("+sip.pns"));
    }

    [Test]
    public void ParsesPrivacyIdentityHistoryGeolocationAndPriority()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult privacy = Parse(codec, "Privacy: id;user", new SipParseOptions());
        SipHeaderParseResult asserted = Parse(codec,
            "P-Asserted-Identity: \"Alice\" <sip:alice@example.com>, <tel:+12025550100>",
            new SipParseOptions());
        SipHeaderParseResult preferred = Parse(codec,
            "P-Preferred-Identity: <sip:alice@example.com>", new SipParseOptions());
        SipHeaderParseResult history = Parse(codec,
            "History-Info: <sip:old@example.com>;index=1.1", new SipParseOptions());
        SipHeaderParseResult location = Parse(codec,
            "Geolocation: <cid:target@example.com>;routing-allowed=yes", new SipParseOptions());
        SipHeaderParseResult priority = Parse(codec,
            "Resource-Priority: dsn.flash, ets.0", new SipParseOptions());

        Assert.That(privacy.ExtensionValues.Select(value => value.Token),
            Is.EqualTo(new string[] { "id", "user" }));
        Assert.That(asserted.ExtensionValues, Has.Length.EqualTo(2));
        Assert.That(asserted.ExtensionValues[0].Address!.DisplayName, Is.EqualTo("Alice"));
        Assert.That(preferred.ExtensionValues.Single().Uri!.Host, Is.EqualTo("example.com"));
        Assert.That(history.ExtensionValues[0].Parameters[0].Value, Is.EqualTo("1.1"));
        Assert.That(location.ExtensionValues[0].Uri!.Scheme, Is.EqualTo(SipUriScheme.Absolute));
        Assert.That(priority.ExtensionValues.Select(value => value.Token),
            Is.EqualTo(new string[] { "dsn.flash", "ets.0" }));
    }

    [TestCase("Privacy: id;;user")]
    [TestCase("History-Info: sip:a@example.com;index=1")]
    [TestCase("Geolocation: cid:target@example.com")]
    [TestCase("Resource-Priority: .flash")]
    [TestCase("Resource-Priority: flash")]
    public void RejectsMalformedIdentityHistoryGeolocationAndPriority(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void ParsesSessionIdAndPreservesUnknownParameters()
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(),
            "Session-ID: 1234567890abcdef1234567890abcdef;remote=abcdef;future=\"x y\"",
            new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.ExtensionValues[0].Token, Is.EqualTo("1234567890abcdef1234567890abcdef"));
        Assert.That(result.ExtensionValues[0].Parameters[1].Value, Is.EqualTo("x y"));
        Assert.That(result.ExtensionValues[0].OriginalText, Is.Not.Null);
    }

    [Test]
    public void ExtensionHeadersHonorLimitsAndPreservationOptions()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions listOptions = new SipParseOptions();
        listOptions.MaxListValueCount = 1;
        SipParseOptions noPreservation = new SipParseOptions();
        noPreservation.PreserveOriginal = false;
        SipHeaderParseResult limitedList = Parse(codec, "Allow-Events: presence,dialog", listOptions);
        SipHeaderParseResult parsed = Parse(codec, "Event: presence;id=one", noPreservation);
        SipWriteOptions outputLimit = new SipWriteOptions();
        outputLimit.MaxHeaderLineBytes = 8;

        SipWriteResult limitedOutput = codec.FormatHeader(parsed.Header!, outputLimit);

        Assert.That(limitedList.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(parsed.ExtensionValues[0].OriginalText, Is.Null);
        Assert.That(parsed.ExtensionValues[0].Parameters[0].OriginalText, Is.Null);
        Assert.That(limitedOutput.Success, Is.False);
        Assert.That(limitedOutput.RequiredBytes, Is.GreaterThan(8));
    }

    static SipHeaderParseResult Parse(SipHeaderCodec codec, string text, SipParseOptions options)
    {
        return codec.ParseHeader(System.Text.Encoding.UTF8.GetBytes(text), options);
    }
}
