namespace sip.Tests;

public class SipDialogRoutingTests
{
    [Test]
    public void UacReversesResponseRecordRoutesWhileUasKeepsRequestOrder()
    {
        string routes = "Record-Route: <sip:proxy-one.example.com;lr>, "
            + "<sip:proxy-two.example.com;lr>\r\n";
        SipMessage request = Request("INVITE", "sip:bob@callee.example.com",
            "caller-tag", null, 42, routes);
        SipMessage response = Response(200, "caller-tag", "server-tag", 42, routes);

        SipDialogLayer uacLayer = new SipDialogLayer();
        SipDialogSnapshot uac = uacLayer.CreateDialog(request, response,
            new SipDialogOptions());
        SipDialogOptions uasOptions = new SipDialogOptions();
        uasOptions.Role = SipDialogRole.Uas;
        SipDialogLayer uasLayer = new SipDialogLayer();
        SipDialogSnapshot uas = uasLayer.CreateDialog(request, response, uasOptions);

        response.RecordRouteValues[0].Address!.Uri!.Host = "changed.example.com";
        request.RecordRouteValues[0].Address!.Uri!.Host = "changed.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(RouteHosts(uac.RouteSet), Is.EqualTo(new string[]
            {
                "proxy-two.example.com", "proxy-one.example.com"
            }));
            Assert.That(RouteHosts(uas.RouteSet), Is.EqualTo(new string[]
            {
                "proxy-one.example.com", "proxy-two.example.com"
            }));
            Assert.That(uac.RouteSet.All(route => route.LooseRouting), Is.True);
            Assert.That(uas.RouteSet.All(route => route.LooseRouting), Is.True);
            Assert.That(RouteHosts(uacLayer.FindDialog(uac.Id!).RouteSet),
                Is.EqualTo(new string[]
                {
                    "proxy-two.example.com", "proxy-one.example.com"
                }));
            Assert.That(RouteHosts(uasLayer.FindDialog(uas.Id!).RouteSet),
                Is.EqualTo(new string[]
                {
                    "proxy-one.example.com", "proxy-two.example.com"
                }));
        });
    }

    [Test]
    public void LocalRequestsUseDialogIdentityLooseRoutesAndMonotonicCSeq()
    {
        string routes = "Record-Route: <sip:proxy-one.example.com;lr>, "
            + "<sip:proxy-two.example.com;lr>\r\n";
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("INVITE", "sip:bob@callee.example.com", "caller-tag", null, 42, ""),
            Response(200, "caller-tag", "server-tag", 42, routes),
            new SipDialogOptions());
        layer.PollDialogEvent();

        SipMessage ack = layer.CreateRequest(dialog.Id!, SipMethods.Ack);
        SipMessage bye = layer.CreateRequest(dialog.Id!, SipMethods.Bye);
        SipMessage options = layer.CreateRequest(dialog.Id!, SipMethods.Options);
        SipDialogSnapshot updated = layer.FindDialog(dialog.Id!);
        SipWriteResult serialized = new SipMessageCodec().SerializeMessage(options,
            new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(ack.RequestUri!.Host, Is.EqualTo("server.example.com"));
            Assert.That(ack.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(bye.CSeq!.SequenceNumber, Is.EqualTo(43));
            Assert.That(options.CSeq!.SequenceNumber, Is.EqualTo(44));
            Assert.That(updated.LocalCSeq, Is.EqualTo(44));
            Assert.That(Tag(options.From), Is.EqualTo("caller-tag"));
            Assert.That(Tag(options.To), Is.EqualTo("server-tag"));
            Assert.That(CallId(options), Is.EqualTo("dialog-call@example.com"));
            Assert.That(RouteHosts(options.RouteValues), Is.EqualTo(new string[]
            {
                "proxy-two.example.com", "proxy-one.example.com"
            }));
            Assert.That(ack.ViaValues[0].Branch, Is.Not.EqualTo(bye.ViaValues[0].Branch));
            Assert.That(bye.ViaValues[0].Branch, Is.Not.EqualTo(options.ViaValues[0].Branch));
            Assert.That(serialized.Success, Is.True, serialized.ErrorMessage);
            Assert.That(layer.PollDialogEvent().Kind, Is.EqualTo("request-created"));
            Assert.That(layer.PollDialogEvent().Kind, Is.EqualTo("request-created"));
            Assert.That(layer.PollDialogEvent().Kind, Is.EqualTo("request-created"));
        });
    }

    [Test]
    public void StrictRouteMovesFirstRouteToRequestUriAndAppendsRemoteTarget()
    {
        string routes = "Record-Route: <sip:loose.example.com;lr>, "
            + "<sip:strict.example.com>\r\n";
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("INVITE", "sip:bob@callee.example.com", "caller-tag", null, 42, ""),
            Response(200, "caller-tag", "server-tag", 42, routes),
            new SipDialogOptions());

        SipMessage bye = layer.CreateRequest(dialog.Id!, SipMethods.Bye);

        Assert.Multiple(() =>
        {
            Assert.That(bye.RequestUri!.Host, Is.EqualTo("strict.example.com"));
            Assert.That(RouteHosts(bye.RouteValues), Is.EqualTo(new string[]
            {
                "loose.example.com", "server.example.com"
            }));
            Assert.That(bye.RouteValues[0].LooseRouting, Is.True);
            Assert.That(bye.RouteValues[1].LooseRouting, Is.False);
        });
    }

    [Test]
    public void ConfirmingEarlyDialogRecomputesRouteSetWithoutResettingSequence()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipMessage request = Request("INVITE", "sip:bob@callee.example.com",
            "caller-tag", null, 42, "");
        SipDialogSnapshot early = layer.CreateDialog(request,
            Response(180, "caller-tag", "server-tag", 42,
                "Record-Route: <sip:early.example.com;lr>\r\n"),
            new SipDialogOptions());
        layer.CreateRequest(early.Id!, SipMethods.Options);

        SipDialogSnapshot confirmed = layer.CreateDialog(request,
            Response(200, "caller-tag", "server-tag", 42,
                "Record-Route: <sip:final.example.com;lr>\r\n"),
            new SipDialogOptions());

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(RouteHosts(confirmed.RouteSet),
                Is.EqualTo(new string[] { "final.example.com" }));
            Assert.That(confirmed.LocalCSeq, Is.EqualTo(43));
        });
    }

    [Test]
    public void UasFirstLocalRequestSelectsInitialSequenceAndUasRouteDirection()
    {
        string routes = "Record-Route: <sip:first.example.com;lr>, "
            + "<sip:second.example.com;lr>\r\n";
        SipDialogOptions options = new SipDialogOptions();
        options.Role = SipDialogRole.Uas;
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("INVITE", "sip:bob@callee.example.com", "caller-tag", null, 42, routes),
            Response(200, "caller-tag", "server-tag", 42, routes), options);

        SipMessage bye = layer.CreateRequest(dialog.Id!, SipMethods.Bye);

        Assert.Multiple(() =>
        {
            Assert.That(bye.CSeq!.SequenceNumber, Is.EqualTo(1));
            Assert.That(Tag(bye.From), Is.EqualTo("server-tag"));
            Assert.That(Tag(bye.To), Is.EqualTo("caller-tag"));
            Assert.That(bye.RequestUri!.Host, Is.EqualTo("client.example.com"));
            Assert.That(RouteHosts(bye.RouteValues), Is.EqualTo(new string[]
            {
                "first.example.com", "second.example.com"
            }));
            Assert.That(layer.FindDialog(dialog.Id!).LocalCSeq, Is.EqualTo(1));
        });
    }

    [Test]
    public void UasStrictRouteUsesRequestOrderAndAppendsRemoteTarget()
    {
        string routes = "Record-Route: <sip:strict.example.com>, "
            + "<sip:loose.example.com;lr>\r\n";
        SipDialogOptions options = new SipDialogOptions();
        options.Role = SipDialogRole.Uas;
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("INVITE", "sip:bob@callee.example.com", "caller-tag", null, 42,
                routes),
            Response(200, "caller-tag", "server-tag", 42, ""), options);

        SipMessage bye = layer.CreateRequest(dialog.Id!, SipMethods.Bye);

        Assert.Multiple(() =>
        {
            Assert.That(bye.RequestUri!.Host, Is.EqualTo("strict.example.com"));
            Assert.That(RouteHosts(bye.RouteValues), Is.EqualTo(new string[]
            {
                "loose.example.com", "client.example.com"
            }));
            Assert.That(bye.RouteValues[0].LooseRouting, Is.True);
            Assert.That(bye.RouteValues[1].LooseRouting, Is.False);
        });
    }

    [Test]
    public void RemoteRequestsEnforceStrictCSeqButAckDoesNotAdvanceIt()
    {
        SipDialogOptions options = new SipDialogOptions();
        options.Role = SipDialogRole.Uas;
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("INVITE", "sip:bob@callee.example.com", "caller-tag", null, 42, ""),
            Response(200, "caller-tag", "server-tag", 42, ""), options);
        layer.PollDialogEvent();

        SipDialogSnapshot advanced = layer.UpdateDialog(dialog.Id!,
            Request("OPTIONS", "sip:bob@server.example.com",
                "caller-tag", "server-tag", 43, ""));
        SipDialogException duplicate = Assert.Throws<SipDialogException>(() =>
            layer.UpdateDialog(dialog.Id!, Request("OPTIONS", "sip:bob@server.example.com",
                "caller-tag", "server-tag", 43, "")))!;
        SipDialogSnapshot acknowledged = layer.UpdateDialog(dialog.Id!,
            Request("ACK", "sip:bob@server.example.com", "caller-tag", "server-tag", 42, ""));

        Assert.Multiple(() =>
        {
            Assert.That(advanced.RemoteCSeq, Is.EqualTo(43));
            Assert.That(duplicate.Message, Does.Contain("sequence"));
            Assert.That(acknowledged.RemoteCSeq, Is.EqualTo(43));
            Assert.That(layer.FindDialog(dialog.Id!).RemoteCSeq, Is.EqualTo(43));
        });
    }

    [Test]
    public void RelaxedRemoteOrderingAcceptsOlderRequestWithoutLoweringHighWaterMark()
    {
        SipDialogOptions options = new SipDialogOptions();
        options.Role = SipDialogRole.Uas;
        options.StrictRemoteCSeq = false;
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("INVITE", "sip:bob@callee.example.com", "caller-tag", null, 42, ""),
            Response(200, "caller-tag", "server-tag", 42, ""), options);

        SipDialogSnapshot older = layer.UpdateDialog(dialog.Id!,
            Request("OPTIONS", "sip:bob@server.example.com", "caller-tag", "server-tag",
                40, ""));

        Assert.That(older.RemoteCSeq, Is.EqualTo(42));
    }

    [Test]
    public void InvalidLocalAndRemoteDialogRequestsLeaveSequenceStateUnchanged()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            SecureRequest(),
            SecureResponse(200, "caller-tag", "server-tag", 42),
            new SipDialogOptions());
        layer.PollDialogEvent();

        Assert.Throws<ArgumentException>(() =>
            layer.CreateRequest(dialog.Id!, SipMethods.Cancel));
        Assert.Throws<ArgumentException>(() =>
            layer.CreateRequest(dialog.Id!, "BAD METHOD"));
        Assert.Throws<ArgumentException>(() => layer.UpdateDialog(dialog.Id!,
            Request("BYE", "sips:bob@server.example.com", "other-tag", "server-tag", 1, "")));
        Assert.Throws<SipProtocolException>(() => layer.UpdateDialog(dialog.Id!,
            Request("BYE", "sip:bob@server.example.com", "server-tag", "caller-tag", 1, "")));

        Assert.Multiple(() =>
        {
            Assert.That(layer.FindDialog(dialog.Id!).LocalCSeq, Is.EqualTo(42));
            Assert.That(layer.FindDialog(dialog.Id!).RemoteCSeq, Is.EqualTo(-1));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void MalformedAndOutOfDialogRequestsAreRejectedAtomically()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("INVITE", "sip:bob@callee.example.com", "caller-tag", null, 42, ""),
            Response(200, "caller-tag", "server-tag", 42, ""),
            new SipDialogOptions());
        layer.PollDialogEvent();
        SipMessage missingTag = Request("OPTIONS", "sip:alice@client.example.com",
            "server-tag", null, 43, "");
        SipMessage foreignCall = Request("OPTIONS", "sip:alice@client.example.com",
            "server-tag", "caller-tag", 43, "");
        foreignCall.Headers.Single(header => string.Equals(header.Name,
            SipHeaderNames.CallId, StringComparison.OrdinalIgnoreCase)).Value =
            "foreign-call@example.com";
        SipMessage malformed = Request("OPTIONS", "sip:alice@client.example.com",
            "server-tag", "caller-tag", 43, "");
        malformed.CSeq!.Method = SipMethod.Bye;
        malformed.CSeq.MethodToken = SipMethods.Bye;

        Assert.Throws<ArgumentException>(() =>
            layer.UpdateDialog(dialog.Id!, missingTag));
        Assert.Throws<ArgumentException>(() =>
            layer.UpdateDialog(dialog.Id!, foreignCall));
        Assert.Throws<ArgumentException>(() =>
            layer.UpdateDialog(dialog.Id!, malformed));

        Assert.Multiple(() =>
        {
            Assert.That(layer.FindDialog(dialog.Id!).State,
                Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(layer.FindDialog(dialog.Id!).RemoteCSeq, Is.EqualTo(-1));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void MaximumLocalCSeqAllowsAckButRejectsIncrement()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("INVITE", "sip:bob@callee.example.com", "caller-tag", null,
                int.MaxValue, ""),
            Response(200, "caller-tag", "server-tag", int.MaxValue, ""),
            new SipDialogOptions());
        layer.PollDialogEvent();

        SipMessage ack = layer.CreateRequest(dialog.Id!, SipMethods.Ack);
        SipDialogException exhausted = Assert.Throws<SipDialogException>(() =>
            layer.CreateRequest(dialog.Id!, SipMethods.Options))!;

        Assert.Multiple(() =>
        {
            Assert.That(ack.CSeq!.SequenceNumber, Is.EqualTo(int.MaxValue));
            Assert.That(exhausted.Message, Does.Contain("exhausted"));
            Assert.That(layer.FindDialog(dialog.Id!).LocalCSeq,
                Is.EqualTo(int.MaxValue));
            Assert.That(layer.PollDialogEvent().Kind, Is.EqualTo("request-created"));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void SecureDialogConstructsSipsRequestOverTls()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            SecureRequest(), SecureResponse(200, "caller-tag", "server-tag", 42),
            new SipDialogOptions());

        SipMessage bye = layer.CreateRequest(dialog.Id!, SipMethods.Bye);

        Assert.Multiple(() =>
        {
            Assert.That(bye.RequestUri!.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(bye.ViaValues[0].Transport, Is.EqualTo("TLS"));
            Assert.That(bye.From!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(bye.To!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sips));
        });
    }

    [Test]
    public void SecureDialogRejectsDowngradedContactAndRouteSet()
    {
        SipDialogLayer layer = new SipDialogLayer();

        Assert.Multiple(() =>
        {
            Assert.Throws<SipProtocolException>(() => layer.CreateDialog(
                SecureRequest(), SecureDowngradeResponse(false),
                new SipDialogOptions()));
            Assert.Throws<SipProtocolException>(() => layer.CreateDialog(
                SecureRequest(), SecureDowngradeResponse(true),
                new SipDialogOptions()));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    static SipMessage Request(string method, string uri, string fromTag,
        string? toTag, int cseq, string recordRoutes)
    {
        string toParameters = toTag == null ? "" : ";tag=" + toTag;
        string contact = method == "INVITE"
            ? "Contact: <sip:alice@client.example.com>\r\n" : "";
        return Parse(method + " " + uri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-dialog-route\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=" + fromTag + "\r\n"
            + "To: Bob <sip:bob@bob.example.com>" + toParameters + "\r\n"
            + "Call-ID: dialog-call@example.com\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\n" + recordRoutes + contact
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string fromTag, string toTag,
        int cseq, string recordRoutes)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Dialog\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-dialog-route\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=" + fromTag + "\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: dialog-call@example.com\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " INVITE\r\n" + recordRoutes
            + "Contact: <sip:bob@server.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage SecureResponse(int status, string fromTag, string toTag, int cseq)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Dialog\r\n"
            + "Via: SIP/2.0/TLS client.example.com:5061;branch=z9hG4bK-dialog-route\r\n"
            + "From: Alice <sips:alice@alice.example.com>;tag=" + fromTag + "\r\n"
            + "To: Bob <sips:bob@bob.example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: dialog-call@example.com\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " INVITE\r\nContact: <sips:bob@server.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage SecureRequest()
    {
        return Parse("INVITE sips:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TLS client.example.com:5061;branch=z9hG4bK-dialog-route\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sips:alice@alice.example.com>;tag=caller-tag\r\n"
            + "To: Bob <sips:bob@bob.example.com>\r\n"
            + "Call-ID: dialog-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sips:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage SecureDowngradeResponse(bool secureContact)
    {
        string contactScheme = secureContact ? "sips" : "sip";
        string routes = secureContact
            ? "Record-Route: <sip:proxy.example.com;lr>\r\n" : "";
        return Parse("SIP/2.0 200 Dialog\r\n"
            + "Via: SIP/2.0/TLS client.example.com:5061;branch=z9hG4bK-dialog-route\r\n"
            + "From: Alice <sips:alice@alice.example.com>;tag=caller-tag\r\n"
            + "To: Bob <sips:bob@bob.example.com>;tag=server-tag\r\n"
            + "Call-ID: dialog-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n" + routes
            + "Contact: <" + contactScheme + ":bob@server.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static string[] RouteHosts(SipRouteValue[] routes)
    {
        return routes.Select(route => route.Address!.Uri!.Host!).ToArray();
    }

    static string? Tag(SipNameAddress? address)
    {
        return address?.Parameters.SingleOrDefault(parameter =>
            string.Equals(parameter.Name, "tag", StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static string? CallId(SipMessage message)
    {
        return message.Headers.Single(header =>
            string.Equals(header.Name, SipHeaderNames.CallId,
                StringComparison.OrdinalIgnoreCase)).Value;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}
