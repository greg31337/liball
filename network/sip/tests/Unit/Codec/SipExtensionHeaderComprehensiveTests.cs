namespace sip.Tests;

public class SipExtensionHeaderComprehensiveTests
{
    const string Response32 = "0123456789abcdef0123456789abcdef";

    [TestCase("Path: <sip:edge.EXAMPLE.com;lr>", "Path: <sip:edge.example.com;lr>\r\n")]
    [TestCase("Service-Route: <sips:service.EXAMPLE.com;lr>", "Service-Route: <sips:service.example.com;lr>\r\n")]
    [TestCase("RSeq: 0001", "RSeq: 1\r\n")]
    [TestCase("RAck: 0001 0002 INVITE", "RAck: 1 2 INVITE\r\n")]
    [TestCase("Session-Expires: 00180;Refresher=UAC;Foo=bar", "Session-Expires: 180;refresher=UAC;foo=bar\r\n")]
    [TestCase("Min-SE: 00090;Foo=bar", "Min-SE: 90;foo=bar\r\n")]
    [TestCase("Reason: SIP;Cause=486;Text=\"Busy Here\"", "Reason: SIP;cause=486;text=\"Busy Here\"\r\n")]
    [TestCase("Event: Presence.Winfo;ID=abc", "Event: Presence.Winfo;id=abc\r\n")]
    [TestCase("Allow-Events: presence,dialog", "Allow-Events: presence, dialog\r\n")]
    [TestCase("Subscription-State: terminated;Reason=timeout;Expires=0;Retry-After=30", "Subscription-State: terminated;reason=timeout;expires=0;retry-after=30\r\n")]
    [TestCase("SIP-ETag: tag-1", "SIP-ETag: tag-1\r\n")]
    [TestCase("SIP-If-Match: tag-1", "SIP-If-Match: tag-1\r\n")]
    [TestCase("Refer-To: \"Bob\" <sip:bob@EXAMPLE.com>", "Refer-To: Bob <sip:bob@example.com>\r\n")]
    [TestCase("Referred-By: <sip:alice@EXAMPLE.com>", "Referred-By: <sip:alice@example.com>\r\n")]
    [TestCase("Refer-Sub: TRUE", "Refer-Sub: TRUE\r\n")]
    [TestCase("Replaces: call@example.com;To-Tag=to;From-Tag=from;Early-Only", "Replaces: call@example.com;to-tag=to;from-tag=from;early-only\r\n")]
    [TestCase("Join: call@example.com;To-Tag=to;From-Tag=from", "Join: call@example.com;to-tag=to;from-tag=from\r\n")]
    [TestCase("Target-Dialog: call@example.com;Local-Tag=local;Remote-Tag=remote", "Target-Dialog: call@example.com;local-tag=local;remote-tag=remote\r\n")]
    [TestCase("Accept-Contact: *;+Audio;Mobility=Fixed", "Accept-Contact: *;+audio;mobility=Fixed\r\n")]
    [TestCase("Reject-Contact: *;+Video", "Reject-Contact: *;+video\r\n")]
    [TestCase("Request-Disposition: proxy,redirect", "Request-Disposition: proxy, redirect\r\n")]
    [TestCase("Feature-Caps: *;+sip.pns=apns;+sip.pnsreg=10", "Feature-Caps: *;+sip.pns=apns;+sip.pnsreg=10\r\n")]
    [TestCase("Privacy: id ; user", "Privacy: id;user\r\n")]
    [TestCase("P-Asserted-Identity: \"Alice\" <sip:alice@EXAMPLE.com>,<tel:+12025550100>", "P-Asserted-Identity: Alice <sip:alice@example.com>, <tel:+12025550100>\r\n")]
    [TestCase("P-Preferred-Identity: <sip:alice@EXAMPLE.com>", "P-Preferred-Identity: <sip:alice@example.com>\r\n")]
    [TestCase("History-Info: <sip:old@EXAMPLE.com>;index=1.1", "History-Info: <sip:old@example.com>;index=1.1\r\n")]
    [TestCase("Session-ID: 1234567890abcdef1234567890abcdef;Remote=abcdef", "Session-ID: 1234567890abcdef1234567890abcdef;remote=abcdef\r\n")]
    [TestCase("Geolocation: <cid:target@example.com>;routing-allowed=yes", "Geolocation: <cid:target@example.com>;routing-allowed=yes\r\n")]
    [TestCase("Resource-Priority: dsn.flash,ets.0", "Resource-Priority: dsn.flash, ets.0\r\n")]
    public void CanonicallyFormatsEveryExtensionHeaderFamily(string source, string expected)
    {
        SipHeaderParseResult parsed = Parse(source);
        SipWriteResult formatted = new SipHeaderCodec().FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
            Assert.That(formatted.Success, Is.True, formatted.ErrorMessage);
            Assert.That(Text(formatted.Bytes), Is.EqualTo(expected));
        });
    }

    [TestCase("x: 90", "Session-Expires: 90\r\n", "x: 90\r\n")]
    [TestCase("o: presence", "Event: presence\r\n", "o: presence\r\n")]
    [TestCase("u: presence", "Allow-Events: presence\r\n", "u: presence\r\n")]
    [TestCase("r: <sip:bob@example.com>", "Refer-To: <sip:bob@example.com>\r\n", "r: <sip:bob@example.com>\r\n")]
    [TestCase("b: <sip:alice@example.com>", "Referred-By: <sip:alice@example.com>\r\n", "b: <sip:alice@example.com>\r\n")]
    [TestCase("a: *;+audio", "Accept-Contact: *;+audio\r\n", "a: *;+audio\r\n")]
    [TestCase("j: *;+video", "Reject-Contact: *;+video\r\n", "j: *;+video\r\n")]
    [TestCase("d: proxy", "Request-Disposition: proxy\r\n", "d: proxy\r\n")]
    public void SupportsEveryExtensionCompactHeaderName(string source, string full, string compact)
    {
        SipHeaderParseResult parsed = Parse(source);
        SipWriteOptions compactOptions = new SipWriteOptions();
        compactOptions.CompactHeaderNames = true;

        SipWriteResult fullResult = new SipHeaderCodec().FormatHeader(parsed.Header!, new SipWriteOptions());
        SipWriteResult compactResult = new SipHeaderCodec().FormatHeader(parsed.Header!, compactOptions);

        Assert.Multiple(() =>
        {
            Assert.That(Text(fullResult.Bytes), Is.EqualTo(full));
            Assert.That(Text(compactResult.Bytes), Is.EqualTo(compact));
        });
    }

    [TestCase("ACK", SipMethod.Ack)]
    [TestCase("BYE", SipMethod.Bye)]
    [TestCase("CANCEL", SipMethod.Cancel)]
    [TestCase("INFO", SipMethod.Info)]
    [TestCase("INVITE", SipMethod.Invite)]
    [TestCase("MESSAGE", SipMethod.Message)]
    [TestCase("NOTIFY", SipMethod.Notify)]
    [TestCase("OPTIONS", SipMethod.Options)]
    [TestCase("PRACK", SipMethod.Prack)]
    [TestCase("PUBLISH", SipMethod.Publish)]
    [TestCase("REFER", SipMethod.Refer)]
    [TestCase("REGISTER", SipMethod.Register)]
    [TestCase("SUBSCRIBE", SipMethod.Subscribe)]
    [TestCase("UPDATE", SipMethod.Update)]
    [TestCase("X-METHOD", SipMethod.Unknown)]
    public void MapsEveryRackMethodBranch(string method, SipMethod expected)
    {
        SipHeaderParseResult result = Parse("RAck: 1 2 " + method);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.ExtensionValues[0].Method, Is.EqualTo(expected));
            Assert.That(result.ExtensionValues[0].MethodToken, Is.EqualTo(method));
        });
    }

    [TestCase("MD5", SipDigestAlgorithm.Md5, "algorithm=MD5")]
    [TestCase("MD5-SESS", SipDigestAlgorithm.Md5Sess, "algorithm=MD5-sess")]
    [TestCase("SHA-256", SipDigestAlgorithm.Sha256, "algorithm=SHA-256")]
    [TestCase("SHA-256-SESS", SipDigestAlgorithm.Sha256Sess, "algorithm=SHA-256-sess")]
    [TestCase("SHA-512-256", SipDigestAlgorithm.Sha512256, "algorithm=SHA-512-256")]
    [TestCase("SHA-512-256-SESS", SipDigestAlgorithm.Sha512256Sess, "algorithm=SHA-512-256-sess")]
    [TestCase("FUTURE", SipDigestAlgorithm.Unknown, "algorithm=FUTURE")]
    public void MapsAndCanonicallyFormatsEveryDigestAlgorithm(string token,
        SipDigestAlgorithm expected, string expectedText)
    {
        SipHeaderParseResult result = Parse(
            "WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",algorithm=" + token);

        string formatted = Format(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.DigestChallenge!.Algorithm, Is.EqualTo(expected));
            Assert.That(formatted, Does.Contain(expectedText));
        });
    }

    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\"", "WWW-Authenticate:")]
    [TestCase("Proxy-Authenticate: Digest realm=\"r\",nonce=\"n\"", "Proxy-Authenticate:")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b@example.com\",response=\"0123456789abcdef0123456789abcdef\"", "Authorization:")]
    [TestCase("Proxy-Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b@example.com\",response=\"0123456789abcdef0123456789abcdef\"", "Proxy-Authorization:")]
    [TestCase("Authentication-Info: nextnonce=\"n\"", "Authentication-Info:")]
    public void ParsesAndFormatsEveryAuthenticationHeaderName(string source, string expectedPrefix)
    {
        SipHeaderParseResult parsed = Parse(source);

        string formatted = Format(parsed);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
            Assert.That(formatted, Does.StartWith(expectedPrefix));
        });
    }

    [Test]
    public void AuthenticationQuotedStringsRoundTripEscapes()
    {
        SipHeaderParseResult challenge = Parse(
            "WWW-Authenticate: Digest realm=\"a\\\"b\\\\c\",nonce=\"n\"");
        SipHeaderParseResult authorization = Parse(
            "Authorization: Digest username=\"a\\\"b\\\\c\",realm=\"r\",nonce=\"n\","
                + "uri=\"sip:b@example.com\",response=\"" + Response32 + "\"");
        SipHeaderParseResult info = Parse("Authentication-Info: nextnonce=\"a\\\"b\\\\c\"");

        Assert.Multiple(() =>
        {
            Assert.That(challenge.DigestChallenge!.Realm, Is.EqualTo("a\"b\\c"));
            Assert.That(authorization.DigestAuthorization!.UserName, Is.EqualTo("a\"b\\c"));
            Assert.That(info.DigestAuthorization!.NextNonce, Is.EqualTo("a\"b\\c"));
            Assert.That(Format(challenge), Does.Contain("realm=\"a\\\"b\\\\c\""));
            Assert.That(Format(authorization), Does.Contain("username=\"a\\\"b\\\\c\""));
            Assert.That(Format(info), Does.Contain("nextnonce=\"a\\\"b\\\\c\""));
        });
    }

    [Test]
    public void CoversPathServiceRouteIdentityHistoryAndFeatureLists()
    {
        SipHeaderParseResult path = Parse(
            "Path: <sip:one@example.com;lr>, <sip:two@example.com;lr>");
        SipHeaderParseResult service = Parse(
            "Service-Route: <sip:one@example.com;lr>, <sips:two@example.com;lr>");
        SipHeaderParseResult asserted = Parse(
            "P-Asserted-Identity: <sip:a@example.com>, <sips:b@example.com>, <tel:+12025550100>");
        SipHeaderParseResult history = Parse(
            "History-Info: <sip:a@example.com>;index=1, <sip:b@example.com>;index=1.1");
        SipHeaderParseResult feature = Parse(
            "Feature-Caps: *;+sip.pns=apns, *;+sip.vapid=key");

        Assert.Multiple(() =>
        {
            Assert.That(path.RouteValues, Has.Length.EqualTo(2));
            Assert.That(service.RouteValues, Has.Length.EqualTo(2));
            Assert.That(asserted.ExtensionValues, Has.Length.EqualTo(3));
            Assert.That(history.ExtensionValues, Has.Length.EqualTo(2));
            Assert.That(feature.ExtensionValues, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ParsesAndFormatsGruuOutboundAndEveryPushParameterForm()
    {
        string source = "Contact: <sip:alice@example.com;ob;pn-provider;pn-param=topic;"
            + "pn-prid=device;pn-purr=reference>;reg-id=2147483647;"
            + "+sip.instance=\"<urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6>\";"
            + "pub-gruu=\"sips:alice@example.com;gr=public\";"
            + "temp-gruu=\"sip:temp@example.com;gr\"";

        SipHeaderParseResult parsed = Parse(source);
        SipContactValue contact = parsed.ContactValues[0];

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
            Assert.That(contact.Outbound, Is.True);
            Assert.That(contact.RegistrationId, Is.EqualTo(2147483647));
            Assert.That(contact.PushProvider, Is.Empty);
            Assert.That(contact.PushParameter, Is.EqualTo("topic"));
            Assert.That(contact.PushResourceId, Is.EqualTo("device"));
            Assert.That(contact.PushRegistrationReference, Is.EqualTo("reference"));
            Assert.That(contact.PublicGruu, Does.StartWith("sips:"));
            Assert.That(contact.TemporaryGruu, Does.StartWith("sip:"));
            Assert.That(Format(parsed), Is.EqualTo(source + "\r\n"));
        });
    }

    [TestCase("Contact: <sip:a@example.com;pn-param>")]
    [TestCase("Contact: <sip:a@example.com;pn-purr>")]
    [TestCase("Contact: <sip:a@example.com;pn-param=one;PN-PARAM=two>")]
    [TestCase("Contact: <sip:a@example.com;pn-purr=one;PN-PURR=two>")]
    [TestCase("Contact: <sip:a@example.com>;temp-gruu=\"tel:+12025550100\"")]
    [TestCase("Contact: <sip:a@example.com>;pub-gruu=\"not a uri\"")]
    [TestCase("Contact: <sip:a@example.com>;reg-id=2147483648")]
    [TestCase("Contact: <sip:a@example.com>;temp-gruu=\"sip:a@example.com\";TEMP-GRUU=\"sip:b@example.com\"")]
    public void RejectsRemainingMalformedGruuOutboundAndPushForms(string source)
    {
        Assert.That(Parse(source).Status, Is.EqualTo(SipParseStatus.Error), source);
    }

    [TestCase("Subscription-State: active;reason")]
    [TestCase("Subscription-State: active;retry-after=x")]
    [TestCase("Join: call@example.com;to-tag=to;from-tag=from;early-only")]
    [TestCase("Privacy: id;user;header")]
    [TestCase("P-Asserted-Identity: <not a uri>")]
    [TestCase("Feature-Caps: *;+sip.pns=one;+SIP.PNS=two")]
    public void RejectsRemainingMalformedExtensionBranches(string source)
    {
        SipParseOptions options = new SipParseOptions();
        if (source.StartsWith("Privacy:", System.StringComparison.Ordinal)) options.MaxListValueCount = 2;

        Assert.That(Parse(source, options).Status, Is.EqualTo(SipParseStatus.Error), source);
    }

    static string Format(SipHeaderParseResult parsed)
    {
        return Text(new SipHeaderCodec().FormatHeader(parsed.Header!, new SipWriteOptions()).Bytes);
    }

    static SipHeaderParseResult Parse(string text)
    {
        return Parse(text, new SipParseOptions());
    }

    static SipHeaderParseResult Parse(string text, SipParseOptions options)
    {
        return new SipHeaderCodec().ParseHeader(System.Text.Encoding.UTF8.GetBytes(text), options);
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
