namespace sip.Tests;

public class SipDialogTargetRefreshTests
{
    [Test]
    public void SuccessfulLocalUpdateCommitsBothTargetsWithoutChangingRouteSet()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer, false);
        string[] routes = RouteHosts(dialog.RouteSet);

        SipMessage update = layer.CreateTargetRefreshRequest(dialog.Id!,
            SipMethods.Update, ParseUri("sip:alice@new-client.example.com"));
        SipDialogSnapshot pending = layer.FindDialog(dialog.Id!);
        SipDialogSnapshot completed = layer.UpdateDialog(dialog.Id!,
            Response(200, SipMethods.Update, update.CSeq!.SequenceNumber, true,
                "server-new.example.com",
                "Record-Route: <sip:changed-route.example.com;lr>\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(ContactHost(update), Is.EqualTo("new-client.example.com"));
            Assert.That(update.RequestUri!.Host, Is.EqualTo("server-old.example.com"));
            Assert.That(pending.LocalTarget!.Host, Is.EqualTo("client-old.example.com"));
            Assert.That(completed.LocalTarget!.Host,
                Is.EqualTo("new-client.example.com"));
            Assert.That(completed.RemoteTarget!.Host,
                Is.EqualTo("server-new.example.com"));
            Assert.That(RouteHosts(completed.RouteSet), Is.EqualTo(routes));
        });
    }

    [Test]
    public void FailedLocalReinviteLeavesTargetsAndRouteSetUnchanged()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer, false);
        string[] routes = RouteHosts(dialog.RouteSet);
        SipMessage reinvite = layer.CreateTargetRefreshRequest(dialog.Id!,
            SipMethods.Invite, ParseUri("sip:alice@rejected-client.example.com"));

        SipDialogSnapshot failed = layer.UpdateDialog(dialog.Id!,
            Response(488, SipMethods.Invite, reinvite.CSeq!.SequenceNumber, true,
                "ignored-server.example.com",
                "Record-Route: <sip:ignored-route.example.com;lr>\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(failed.LocalTarget!.Host, Is.EqualTo("client-old.example.com"));
            Assert.That(failed.RemoteTarget!.Host, Is.EqualTo("server-old.example.com"));
            Assert.That(RouteHosts(failed.RouteSet), Is.EqualTo(routes));
        });
    }

    [Test]
    public void ReliableProgressCommitsRefreshAndLaterFailureDoesNotRollItBack()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer, false);
        string[] routes = RouteHosts(dialog.RouteSet);
        SipMessage reinvite = layer.CreateTargetRefreshRequest(dialog.Id!,
            SipMethods.Invite, ParseUri("sip:alice@progress-client.example.com"));
        int cseq = reinvite.CSeq!.SequenceNumber;

        SipDialogSnapshot progressed = layer.UpdateDialog(dialog.Id!,
            Response(183, SipMethods.Invite, cseq, true,
                "progress-server.example.com",
                "Require: 100rel\r\nRSeq: 1\r\n"
                + "Record-Route: <sip:progress-route.example.com;lr>\r\n"));
        SipDialogSnapshot failed = layer.UpdateDialog(dialog.Id!,
            Response(488, SipMethods.Invite, cseq, true,
                "failed-server.example.com", ""));

        Assert.Multiple(() =>
        {
            Assert.That(progressed.LocalTarget!.Host,
                Is.EqualTo("progress-client.example.com"));
            Assert.That(progressed.RemoteTarget!.Host,
                Is.EqualTo("progress-server.example.com"));
            Assert.That(failed.LocalTarget!.Host,
                Is.EqualTo("progress-client.example.com"));
            Assert.That(failed.RemoteTarget!.Host,
                Is.EqualTo("progress-server.example.com"));
            Assert.That(RouteHosts(failed.RouteSet), Is.EqualTo(routes));
        });
    }

    [Test]
    public void UnreliableProgressAndFinalFailureDoNotCommitLocalRefresh()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer, false);
        SipMessage reinvite = layer.CreateTargetRefreshRequest(dialog.Id!,
            SipMethods.Invite, ParseUri("sip:alice@pending-client.example.com"));
        int cseq = reinvite.CSeq!.SequenceNumber;

        SipDialogSnapshot provisional = layer.UpdateDialog(dialog.Id!,
            Response(180, SipMethods.Invite, cseq, true,
                "unreliable-server.example.com", ""));
        SipDialogSnapshot failed = layer.UpdateDialog(dialog.Id!,
            Response(500, SipMethods.Invite, cseq, true,
                "failed-server.example.com", ""));

        Assert.Multiple(() =>
        {
            Assert.That(provisional.LocalTarget!.Host,
                Is.EqualTo("client-old.example.com"));
            Assert.That(provisional.RemoteTarget!.Host,
                Is.EqualTo("server-old.example.com"));
            Assert.That(failed.LocalTarget!.Host, Is.EqualTo("client-old.example.com"));
            Assert.That(failed.RemoteTarget!.Host, Is.EqualTo("server-old.example.com"));
        });
    }

    [Test]
    public void IncomingRefreshCommitsOnlyAfterLocalSuccessResponse()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer, false);
        string[] routes = RouteHosts(dialog.RouteSet);

        layer.UpdateDialog(dialog.Id!, RemoteRequest(SipMethods.Update, 11,
            "server-rejected.example.com", "client-old.example.com"));
        SipDialogSnapshot rejected = layer.UpdateDialog(dialog.Id!,
            Response(488, SipMethods.Update, 11, false, "", ""));
        layer.UpdateDialog(dialog.Id!, RemoteRequest(SipMethods.Update, 12,
            "server-accepted.example.com", "client-old.example.com"));
        SipDialogSnapshot accepted = layer.UpdateDialog(dialog.Id!,
            Response(200, SipMethods.Update, 12, false,
                "client-new.example.com",
                "Record-Route: <sip:ignored-route.example.com;lr>\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.RemoteTarget!.Host,
                Is.EqualTo("server-old.example.com"));
            Assert.That(accepted.RemoteTarget!.Host,
                Is.EqualTo("server-accepted.example.com"));
            Assert.That(accepted.LocalTarget!.Host,
                Is.EqualTo("client-new.example.com"));
            Assert.That(RouteHosts(accepted.RouteSet), Is.EqualTo(routes));
        });
    }

    [Test]
    public void RequestToPendingRemoteTargetCommitsItDespiteLaterFailure()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer, false);
        layer.UpdateDialog(dialog.Id!, RemoteRequest(SipMethods.Invite, 11,
            "server-moved.example.com", "client-old.example.com"));

        SipMessage options = layer.CreateRequest(dialog.Id!, SipMethods.Options);
        SipDialogSnapshot failed = layer.UpdateDialog(dialog.Id!,
            Response(488, SipMethods.Invite, 11, false, "", ""));

        Assert.Multiple(() =>
        {
            Assert.That(options.RequestUri!.Host, Is.EqualTo("server-moved.example.com"));
            Assert.That(failed.RemoteTarget!.Host,
                Is.EqualTo("server-moved.example.com"));
        });
    }

    [Test]
    public void InvalidAndInsecureRefreshTargetsAreRejectedWithoutStateChanges()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer, true);
        layer.PollDialogEvent();

        Assert.Throws<ArgumentException>(() => layer.CreateTargetRefreshRequest(
            dialog.Id!, SipMethods.Options, ParseUri("sips:alice@new.example.com")));
        Assert.Throws<SipProtocolException>(() => layer.CreateTargetRefreshRequest(
            dialog.Id!, SipMethods.Update, ParseUri("sip:alice@insecure.example.com")));
        SipMessage invalidContacts = RemoteRequest(SipMethods.Update, 11,
            "secure-server-new.example.com", "secure-client.example.com");
        invalidContacts.ContactValues = new SipContactValue[]
        {
            invalidContacts.ContactValues[0], invalidContacts.ContactValues[0]
        };
        Assert.Throws<ArgumentException>(() =>
            layer.UpdateDialog(dialog.Id!, invalidContacts));

        SipDialogSnapshot unchanged = layer.FindDialog(dialog.Id!);
        Assert.Multiple(() =>
        {
            Assert.That(unchanged.LocalTarget!.Host,
                Is.EqualTo("secure-client.example.com"));
            Assert.That(unchanged.RemoteTarget!.Host,
                Is.EqualTo("secure-server.example.com"));
            Assert.That(unchanged.LocalCSeq, Is.EqualTo(10));
            Assert.That(unchanged.RemoteCSeq, Is.EqualTo(-1));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    static SipDialogSnapshot CreateDialog(SipDialogLayer layer, bool secure)
    {
        string scheme = secure ? "sips" : "sip";
        string transport = secure ? "TLS" : "UDP";
        string client = secure ? "secure-client.example.com" : "client-old.example.com";
        string server = secure ? "secure-server.example.com" : "server-old.example.com";
        string route = secure ? "sips:route.example.com;lr" : "sip:route.example.com;lr";
        SipMessage request = Parse("INVITE " + scheme + ":bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/" + transport
            + " client.example.com:5060;branch=z9hG4bK-refresh\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <" + scheme
            + ":alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <" + scheme + ":bob@bob.example.com>\r\n"
            + "Call-ID: refresh-call@example.com\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <" + scheme + ":alice@" + client + ">\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipMessage response = Parse("SIP/2.0 200 Dialog\r\n"
            + "Via: SIP/2.0/" + transport
            + " client.example.com:5060;branch=z9hG4bK-refresh\r\n"
            + "From: Alice <" + scheme
            + ":alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <" + scheme + ":bob@bob.example.com>;tag=remote-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\nCSeq: 10 INVITE\r\n"
            + "Record-Route: <" + route + ">\r\n"
            + "Contact: <" + scheme + ":bob@" + server + ">\r\n"
            + "Content-Length: 0\r\n\r\n");
        return layer.CreateDialog(request, response, new SipDialogOptions());
    }

    static SipMessage RemoteRequest(
        string method, int cseq, string contactHost, string requestHost)
    {
        string requestScheme = requestHost == "secure-client.example.com" ? "sips" : "sip";
        string contactScheme = requestScheme;
        string addressScheme = requestScheme;
        return Parse(method + " " + requestScheme + ":alice@" + requestHost
            + " SIP/2.0\r\nVia: SIP/2.0/UDP remote.example.com:5060;branch=z9hG4bK-peer\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <" + addressScheme + ":bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <" + addressScheme
            + ":alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\nCSeq: "
            + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\nContact: <" + contactScheme + ":bob@"
            + contactHost + ">\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string method, int cseq,
        bool localRequest, string contactHost, string extraHeaders)
    {
        string from = localRequest
            ? "Alice <sip:alice@alice.example.com>;tag=local-tag"
            : "Bob <sip:bob@bob.example.com>;tag=remote-tag";
        string to = localRequest
            ? "Bob <sip:bob@bob.example.com>;tag=remote-tag"
            : "Alice <sip:alice@alice.example.com>;tag=local-tag";
        string contact = contactHost.Length == 0 ? ""
            : "Contact: <sip:user@" + contactHost + ">\r\n";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Dialog\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-refresh-next\r\n"
            + "From: " + from + "\r\nTo: " + to + "\r\n"
            + "Call-ID: refresh-call@example.com\r\nCSeq: "
            + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\n" + extraHeaders + contact
            + "Content-Length: 0\r\n\r\n");
    }

    static string ContactHost(SipMessage message)
    {
        return message.ContactValues[0].Address!.Uri!.Host!;
    }

    static string[] RouteHosts(SipRouteValue[] routes)
    {
        return routes.Select(route => route.Address!.Uri!.Host!).ToArray();
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Uri!;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}
