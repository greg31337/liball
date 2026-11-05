namespace sip.Tests;

public class SipDialogLayerTests
{
    [Test]
    public void UacProvisionalCreatesDetachedEarlyDialogIdentityAndSnapshot()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = new SipDialogOptions();
        SipMessage request = Request("sip", "UDP", "local-tag");
        SipMessage response = Response(180, "sip", "local-tag", "remote-tag",
            "dialog-call@example.com", 42, true);

        SipDialogSnapshot created = layer.CreateDialog(request, response, options);
        SipDialogEvent createdEvent = layer.PollDialogEvent();
        created.Id!.CallId = "changed@example.com";
        created.RemoteTarget!.Host = "changed.example.com";
        createdEvent.Snapshot!.LocalTarget!.Host = "event-change.example.com";
        SipDialogSnapshot found = layer.FindDialog(Id("dialog-call@example.com",
            "local-tag", "remote-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(found.Role, Is.EqualTo(SipDialogRole.Uac));
            Assert.That(found.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(found.Id!.CallId, Is.EqualTo("dialog-call@example.com"));
            Assert.That(found.Id.LocalTag, Is.EqualTo("local-tag"));
            Assert.That(found.Id.RemoteTag, Is.EqualTo("remote-tag"));
            Assert.That(found.LocalUri!.Host, Is.EqualTo("alice.example.com"));
            Assert.That(found.RemoteUri!.Host, Is.EqualTo("bob.example.com"));
            Assert.That(found.LocalTarget!.Host, Is.EqualTo("client.example.com"));
            Assert.That(found.RemoteTarget!.Host, Is.EqualTo("server.example.com"));
            Assert.That(found.LocalCSeq, Is.EqualTo(42));
            Assert.That(found.RemoteCSeq, Is.EqualTo(-1));
            Assert.That(found.Secure, Is.False);
            Assert.That(createdEvent.Kind, Is.EqualTo("created"));
            Assert.That(createdEvent.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(createdEvent.Message!.StatusCode, Is.EqualTo(180));
            Assert.That(createdEvent.Snapshot, Is.Not.SameAs(found));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    [Test]
    public void UasFinalCreatesConfirmedDialogWithReversedIdentityAndSequenceDirection()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = new SipDialogOptions();
        options.Role = SipDialogRole.Uas;
        SipMessage request = Request("sips", "TLS", "caller-tag");
        SipMessage response = Response(200, "sips", "caller-tag", "server-tag",
            "dialog-call@example.com", 42, true);

        SipDialogSnapshot created = layer.CreateDialog(request, response, options);

        Assert.Multiple(() =>
        {
            Assert.That(created.Role, Is.EqualTo(SipDialogRole.Uas));
            Assert.That(created.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(created.Id!.CallId, Is.EqualTo("dialog-call@example.com"));
            Assert.That(created.Id.LocalTag, Is.EqualTo("server-tag"));
            Assert.That(created.Id.RemoteTag, Is.EqualTo("caller-tag"));
            Assert.That(created.LocalUri!.Host, Is.EqualTo("bob.example.com"));
            Assert.That(created.RemoteUri!.Host, Is.EqualTo("alice.example.com"));
            Assert.That(created.LocalTarget!.Host, Is.EqualTo("server.example.com"));
            Assert.That(created.RemoteTarget!.Host, Is.EqualTo("client.example.com"));
            Assert.That(created.LocalCSeq, Is.EqualTo(-1));
            Assert.That(created.RemoteCSeq, Is.EqualTo(42));
            Assert.That(created.Secure, Is.True);
        });
    }

    [Test]
    public void UasCreationRejectsDialogPairWithoutRemoteFromTag()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = new SipDialogOptions();
        options.Role = SipDialogRole.Uas;
        SipMessage request = Request("sip", "UDP", "caller-tag");
        SipMessage response = Response(180, "sip", "caller-tag", "server-tag",
            "dialog-call@example.com", 42, true);
        request.From!.Parameters = new SipUriParameter[0];
        response.From!.Parameters = new SipUriParameter[0];

        Assert.Throws<ArgumentException>(() =>
            layer.CreateDialog(request, response, options));

        Assert.That(layer.PollDialogEvent().Kind, Is.Null);
    }

    [TestCase("sips", "TLS", true)]
    [TestCase("sips", "TCP", false)]
    [TestCase("sip", "TLS", false)]
    public void SecureFlagRequiresSipsRequestTargetAndTlsTransport(
        string scheme, string transport, bool expected)
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request(scheme, transport, "local-tag"),
            Response(200, scheme, "local-tag", "remote-tag",
                "dialog-call@example.com", 42, true),
            new SipDialogOptions());

        Assert.That(dialog.Secure, Is.EqualTo(expected));
    }

    [Test]
    public void MatchingFinalConfirmsEarlyDialogAndFailureTerminatesOnlyEarlyDialog()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = new SipDialogOptions();
        SipMessage request = Request("sip", "UDP", "local-tag");
        layer.CreateDialog(request, Response(180, "sip", "local-tag", "first-tag",
            "dialog-call@example.com", 42, true), options);
        layer.PollDialogEvent();

        SipDialogSnapshot confirmed = layer.CreateDialog(request,
            Response(200, "sip", "local-tag", "first-tag",
                "dialog-call@example.com", 42, true), options);
        SipDialogEvent confirmedEvent = layer.PollDialogEvent();
        SipDialogSnapshot stillConfirmed = layer.UpdateDialog(confirmed.Id!,
            Response(488, "sip", "local-tag", "first-tag",
                "dialog-call@example.com", 42, false));
        SipDialogEvent updateEvent = layer.PollDialogEvent();

        SipDialogSnapshot early = layer.CreateDialog(request,
            Response(183, "sip", "local-tag", "second-tag",
                "dialog-call@example.com", 42, true), options);
        layer.PollDialogEvent();
        SipDialogSnapshot terminated = layer.UpdateDialog(early.Id!,
            Response(486, "sip", "local-tag", "second-tag",
                "dialog-call@example.com", 42, false));
        SipDialogEvent terminatedEvent = layer.PollDialogEvent();

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(confirmedEvent.Kind, Is.EqualTo("state-changed"));
            Assert.That(confirmedEvent.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(stillConfirmed.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(updateEvent.Kind, Is.EqualTo("updated"));
            Assert.That(terminated.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(terminatedEvent.Kind, Is.EqualTo("state-changed"));
            Assert.That(terminatedEvent.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(early.Id!));
            Assert.That(layer.FindDialog(confirmed.Id!).State,
                Is.EqualTo(SipDialogState.Confirmed));
        });
    }

    [Test]
    public void RepeatedIdentityBypassesCapacityButDistinctDialogDoesNot()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogOptions options = new SipDialogOptions();
        options.MaxDialogCount = 1;
        SipMessage request = Request("sip", "UDP", "local-tag");
        layer.CreateDialog(request, Response(180, "sip", "local-tag", "first-tag",
            "dialog-call@example.com", 42, true), options);

        SipDialogSnapshot confirmed = layer.CreateDialog(request,
            Response(200, "sip", "local-tag", "first-tag",
                "dialog-call@example.com", 42, true), options);
        SipDialogException full = Assert.Throws<SipDialogException>(() => layer.CreateDialog(
            request, Response(180, "sip", "local-tag", "second-tag",
                "dialog-call@example.com", 42, true), options))!;

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.State, Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(full.Message, Does.Contain("limit"));
            Assert.That(full.DialogId!.RemoteTag, Is.EqualTo("second-tag"));
            Assert.That(layer.FindDialog(confirmed.Id!).State,
                Is.EqualTo(SipDialogState.Confirmed));
        });
    }

    [Test]
    public void ExplicitTerminationQueuesFinalDetachedSnapshotAndRemovesDialog()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = layer.CreateDialog(
            Request("sip", "UDP", "local-tag"),
            Response(200, "sip", "local-tag", "remote-tag",
                "dialog-call@example.com", 42, true), new SipDialogOptions());
        layer.PollDialogEvent();
        SipCancellation cancellation = new SipCancellation();
        cancellation.Reason = "Session ended.";

        layer.TerminateDialog(dialog.Id!, cancellation);
        SipDialogEvent terminated = layer.PollDialogEvent();
        terminated.Snapshot!.Id!.CallId = "changed@example.com";

        Assert.Multiple(() =>
        {
            Assert.That(terminated.Kind, Is.EqualTo("terminated"));
            Assert.That(terminated.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(terminated.Reason, Is.EqualTo("Session ended."));
            Assert.That(terminated.DialogId!.CallId, Is.EqualTo("dialog-call@example.com"));
            Assert.That(terminated.Snapshot.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.Throws<SipDialogException>(() => layer.FindDialog(dialog.Id!));
            Assert.Throws<SipDialogException>(() =>
                layer.TerminateDialog(dialog.Id!, cancellation));
        });
    }

    [Test]
    public void InvalidDialogPairsAndIdentifiersAreRejectedWithoutEvents()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipMessage request = Request("sip", "UDP", "local-tag");
        SipDialogOptions unknownRole = new SipDialogOptions();
        unknownRole.Role = SipDialogRole.Unknown;
        SipDialogOptions zeroCapacity = new SipDialogOptions();
        zeroCapacity.MaxDialogCount = 0;
        SipMessage missingTag = Response(180, "sip", "local-tag", null,
            "dialog-call@example.com", 42, true);
        SipMessage wrongCall = Response(180, "sip", "local-tag", "remote-tag",
            "other-call@example.com", 42, true);
        SipMessage wrongCseq = Response(180, "sip", "local-tag", "remote-tag",
            "dialog-call@example.com", 43, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => layer.CreateDialog(request,
                Response(100, "sip", "local-tag", null,
                    "dialog-call@example.com", 42, false), new SipDialogOptions()));
            Assert.Throws<ArgumentException>(() => layer.CreateDialog(request,
                Response(486, "sip", "local-tag", "remote-tag",
                    "dialog-call@example.com", 42, false), new SipDialogOptions()));
            Assert.Throws<ArgumentException>(() => layer.CreateDialog(
                request, missingTag, new SipDialogOptions()));
            Assert.Throws<ArgumentException>(() => layer.CreateDialog(
                request, wrongCall, new SipDialogOptions()));
            Assert.Throws<ArgumentException>(() => layer.CreateDialog(
                request, wrongCseq, new SipDialogOptions()));
            Assert.Throws<ArgumentException>(() => layer.CreateDialog(
                request, Response(180, "sip", "local-tag", "remote-tag",
                    "dialog-call@example.com", 42, true), unknownRole));
            Assert.Throws<ArgumentException>(() => layer.CreateDialog(
                request, Response(180, "sip", "local-tag", "remote-tag",
                    "dialog-call@example.com", 42, true), zeroCapacity));
            Assert.Throws<ArgumentException>(() => layer.FindDialog(new SipDialogId()));
            Assert.That(layer.PollDialogEvent().Kind, Is.Null);
        });
    }

    static SipDialogId Id(string callId, string localTag, string remoteTag)
    {
        SipDialogId id = new SipDialogId();
        id.CallId = callId;
        id.LocalTag = localTag;
        id.RemoteTag = remoteTag;
        return id;
    }

    static SipMessage Request(string scheme, string transport, string fromTag)
    {
        return Parse("INVITE " + scheme + ":bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/" + transport
            + " client.example.com:5060;branch=z9hG4bK-dialog\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <" + scheme + ":alice@alice.example.com>;tag=" + fromTag + "\r\n"
            + "To: Bob <" + scheme + ":bob@bob.example.com>\r\n"
            + "Call-ID: dialog-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <" + scheme + ":alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string scheme, string fromTag,
        string? toTag, string callId, int cseq, bool contact)
    {
        string toParameters = toTag == null ? "" : ";tag=" + toTag;
        string contactHeader = contact
            ? "Contact: <" + scheme + ":bob@server.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Dialog\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-dialog\r\n"
            + "From: Alice <" + scheme + ":alice@alice.example.com>;tag=" + fromTag + "\r\n"
            + "To: Bob <" + scheme + ":bob@bob.example.com>" + toParameters + "\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " INVITE\r\n" + contactHeader + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}
