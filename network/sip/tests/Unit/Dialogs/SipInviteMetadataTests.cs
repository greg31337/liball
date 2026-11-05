namespace sip.Tests;

public class SipInviteMetadataTests
{
    const string LocalUuid = "11111111111111111111111111111111";
    const string RemoteUuid = "22222222222222222222222222222222";

    [Test]
    public void InitialInviteAdvertisesIdentityFeaturesAndCreatesSessionId()
    {
        SipInviteSession session = new SipInviteSession();

        SipInviteEvent started = session.StartInvite(OutgoingCommand(null));

        Assert.Multiple(() =>
        {
            Assert.That(HeaderValue(started.Message!, SipHeaderNames.Supported),
                Does.Contain(SipOptionTags.FromChange));
            Assert.That(HeaderValue(started.Message!, SipHeaderNames.Supported),
                Does.Contain(SipOptionTags.EarlyDialogTermination));
            Assert.That(started.LocalSessionId, Has.Length.EqualTo(32));
            Assert.That(HeaderValue(started.Message!, SipHeaderNames.SessionId),
                Is.EqualTo(started.LocalSessionId
                    + ";remote=00000000000000000000000000000000"));
        });
    }

    [Test]
    public void RemoteIdentitySessionAndCapabilitiesFlowIntoDetachedEventsAndAck()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteEvent started = session.StartInvite(OutgoingCommand(LocalUuid));
        DrainEvents(session);

        SipInviteEvent confirmed = session.ReceiveInviteMessage(Parse(
            "SIP/2.0 200 OK\r\n"
            + InitialResponseHeaders("Connected Bob <sip:bob@bob.example.com>")
            + "Contact: <sip:bob@target.example.com>\r\n"
            + "Session-ID: " + RemoteUuid + ";remote=" + LocalUuid + "\r\n"
            + "Supported: from-change, timer\r\n"
            + "Allow: INVITE, UPDATE, BYE\r\n"
            + "Accept: application/sdp, application/pidf+xml\r\n"
            + "Allow-Events: presence, dialog\r\n"
            + "Feature-Caps: *;+sip.example=\"enabled\"\r\n"
            + "Content-Length: 0\r\n\r\n"));
        confirmed.RemoteCapabilities!.Methods[0] = "changed";
        confirmed.ConnectedIdentity!.Uri!.Host = "changed.example.com";
        SipInviteEvent queuedResponse = session.PollInviteEvent();
        SipInviteEvent ack = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(queuedResponse.LocalSessionId, Is.EqualTo(LocalUuid));
            Assert.That(queuedResponse.RemoteSessionId, Is.EqualTo(RemoteUuid));
            Assert.That(queuedResponse.ConnectedIdentity!.DisplayName,
                Is.EqualTo("Connected Bob"));
            Assert.That(queuedResponse.ConnectedIdentity.Uri!.Host,
                Is.EqualTo("bob.example.com"));
            Assert.That(queuedResponse.RemoteCapabilities!.Methods,
                Is.EqualTo(new string[] { "INVITE", "UPDATE", "BYE" }));
            Assert.That(queuedResponse.RemoteCapabilities.OptionTags,
                Is.EqualTo(new string[] { "from-change", "timer" }));
            Assert.That(queuedResponse.RemoteCapabilities.MediaTypes,
                Is.EqualTo(new string[] {
                    "application/sdp", "application/pidf+xml" }));
            Assert.That(queuedResponse.RemoteCapabilities.EventPackages,
                Is.EqualTo(new string[] { "presence", "dialog" }));
            Assert.That(queuedResponse.RemoteFeatureCapabilities,
                Has.Length.EqualTo(1));
            Assert.That(HeaderValue(ack.Message!, SipHeaderNames.SessionId),
                Is.EqualTo(LocalUuid + ";remote=" + RemoteUuid));
            Assert.That(started.RemoteSessionId, Is.Null);
        });
    }

    [Test]
    public void EarlyDialogTerminationReturnsToProceedingAndRetainsInviteOffer()
    {
        SipInviteSession session = new SipInviteSession();
        session.StartInvite(OutgoingCommandWithOffer(LocalUuid));
        DrainEvents(session);
        session.ReceiveInviteMessage(Parse(
            "SIP/2.0 180 Ringing\r\n"
            + InitialResponseHeaders("Bob <sip:bob@bob.example.com>")
            + "Contact: <sip:bob@early.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n"));
        DrainEvents(session);

        SipInviteEvent ended = session.ReceiveInviteMessage(Parse(
            "SIP/2.0 199 Early Dialog Terminated\r\n"
            + InitialResponseHeaders("Bob <sip:bob@bob.example.com>")
            + "Reason: SIP;cause=486;text=\"Busy Here\"\r\n"
            + "Content-Length: 0\r\n\r\n"));
        SipInviteEvent confirmed = session.ReceiveInviteMessage(Parse(
            "SIP/2.0 200 OK\r\n"
            + InitialResponseHeaders("Bob <sip:bob@bob.example.com>")
            + "Contact: <sip:bob@final.example.com>\r\n"
            + "Content-Type: application/sdp\r\n"
            + "Content-Length: 15\r\n\r\n"
            + "v=0\r\no=answer\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(ended.State, Is.EqualTo(SipInviteSessionState.Proceeding));
            Assert.That(ended.Dialog!.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(ended.Reasons, Has.Length.EqualTo(1));
            Assert.That(ended.Reasons[0].Number, Is.EqualTo(486));
            Assert.That(ended.Reasons[0].SecondaryToken, Is.EqualTo("Busy Here"));
            Assert.That(ended.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(confirmed.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(confirmed.Dialog!.RemoteTarget!.Host,
                Is.EqualTo("final.example.com"));
        });
    }

    [Test]
    public void RedirectKeepsLocalSessionIdAndClearsPreviousPeerId()
    {
        SipInviteSession session = new SipInviteSession();
        session.StartInvite(OutgoingCommand(LocalUuid));
        DrainEvents(session);

        SipInviteEvent redirected = session.ReceiveInviteMessage(Parse(
            "SIP/2.0 302 Moved Temporarily\r\n"
            + InitialResponseHeaders("Bob <sip:bob@bob.example.com>")
            + "Contact: <sip:bob@redirected.example.com>\r\n"
            + "Session-ID: " + RemoteUuid + ";remote=" + LocalUuid + "\r\n"
            + "Content-Length: 0\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(redirected.State, Is.EqualTo(SipInviteSessionState.Calling));
            Assert.That(redirected.LocalSessionId, Is.EqualTo(LocalUuid));
            Assert.That(redirected.RemoteSessionId, Is.Null);
            Assert.That(HeaderValue(redirected.Message!, SipHeaderNames.SessionId),
                Is.EqualTo(LocalUuid
                    + ";remote=00000000000000000000000000000000"));
            Assert.That(redirected.Message!.RequestUri!.Host,
                Is.EqualTo("redirected.example.com"));
        });
    }

    [Test]
    public void CancelReasonFlowsToInviteFailureAndSessionIdIsReversed()
    {
        SipInviteSession session = new SipInviteSession();
        session.ReceiveInviteMessage(IncomingInvite());
        DrainEvents(session);
        SipInviteEvent ringing = session.StartInvite(IncomingResponseCommand(
            180, "Ringing", null));
        DrainEvents(session);

        SipInviteEvent cancelled = session.ReceiveInviteMessage(Parse(
            "CANCEL sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-incoming\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 77 CANCEL\r\n"
            + "Session-ID: " + RemoteUuid + ";remote="
                + ringing.LocalSessionId + "\r\n"
            + "Reason: SIP;cause=200;text=\"Completed elsewhere\"\r\n"
            + "Content-Length: 0\r\n\r\n"));
        session.PollInviteEvent();
        SipInviteEvent ok = session.PollInviteEvent();
        SipInviteEvent failedInvite = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Reasons[0].Number, Is.EqualTo(200));
            Assert.That(failedInvite.Reasons[0].SecondaryToken,
                Is.EqualTo("Completed elsewhere"));
            Assert.That(HeaderValue(ok.Message!, SipHeaderNames.SessionId),
                Is.EqualTo(ringing.LocalSessionId + ";remote=" + RemoteUuid));
            Assert.That(HeaderValue(failedInvite.Message!, SipHeaderNames.Reason),
                Is.EqualTo("SIP;cause=200;text=\"Completed elsewhere\""));
        });
    }

    [Test]
    public void NegotiatedUasCanReportAnEarlyDialogWith199()
    {
        SipInviteSession session = new SipInviteSession();
        session.ReceiveInviteMessage(IncomingInvite());
        DrainEvents(session);
        SipInviteEvent ringing = session.StartInvite(IncomingResponseCommand(
            180, "Ringing", null));
        DrainEvents(session);

        SipInviteEvent ended = session.StartInvite(IncomingResponseCommand(
            199, "Early Dialog Terminated",
            "Reason: SIP;cause=480;text=\"Temporarily unavailable\"\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(ended.State, Is.EqualTo(SipInviteSessionState.Proceeding));
            Assert.That(ended.Dialog!.State, Is.EqualTo(SipDialogState.Terminated));
            Assert.That(ended.Reasons[0].Number, Is.EqualTo(480));
            Assert.That(HeaderValue(ended.Message!, SipHeaderNames.Supported),
                Does.Not.Contain(SipOptionTags.EarlyDialogTermination));
            Assert.That(HeaderValue(ended.Message!, SipHeaderNames.SessionId),
                Is.EqualTo(ringing.LocalSessionId + ";remote=" + RemoteUuid));
        });
    }

    [Test]
    public void ConnectedIdentityChangesOnlyAfterRefreshIsAccepted()
    {
        SipInviteSession session = new SipInviteSession();
        session.StartInvite(OutgoingCommand(LocalUuid));
        DrainEvents(session);
        session.ReceiveInviteMessage(Parse(
            "SIP/2.0 200 OK\r\n"
            + InitialResponseHeaders("Bob <sip:bob@bob.example.com>")
            + "Contact: <sip:bob@target.example.com>\r\n"
            + "Supported: from-change\r\n"
            + "Content-Length: 0\r\n\r\n"));
        DrainEvents(session);

        SipMessage update = Parse(
            "UPDATE sip:alice@client.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP target.example.com:5060;branch=z9hG4bK-update\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Carol <sip:carol@new.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 81 UPDATE\r\n"
            + "Contact: <sip:carol@target.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipInviteEvent proposed = session.ReceiveInviteMessage(update);
        SipInviteEvent accepted = session.StartInvite(RefreshResponseCommand(update));
        DrainEvents(session);
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        session.TerminateInvite(cancellation);
        SipInviteEvent bye = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(proposed.ConnectedIdentity!.Uri!.User, Is.EqualTo("bob"));
            Assert.That(accepted.ConnectedIdentity!.DisplayName,
                Is.EqualTo("Carol"));
            Assert.That(accepted.ConnectedIdentity.Uri!.Host,
                Is.EqualTo("new.example.com"));
            Assert.That(bye.Message!.To!.Uri!.User, Is.EqualTo("carol"));
            Assert.That(bye.Message.To.Uri.Host, Is.EqualTo("new.example.com"));
        });
    }

    static SipInviteRequest OutgoingCommand(string? sessionId)
    {
        string header = sessionId == null ? ""
            : "Session-ID: " + sessionId
                + ";remote=00000000000000000000000000000000\r\n";
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Parse(
            "INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-invite\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + header + "Content-Length: 0\r\n\r\n");
        return command;
    }

    static SipInviteRequest OutgoingCommandWithOffer(string sessionId)
    {
        SipInviteRequest command = OutgoingCommand(sessionId);
        command.Message!.Body = System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\no=offer\r\n");
        command.Message.ContentType = new SipMediaType();
        command.Message.ContentType.Type = "application";
        command.Message.ContentType.Subtype = "sdp";
        SetHeader(command.Message, SipHeaderNames.ContentLength, "14");
        AddHeader(command.Message, SipHeaderNames.ContentType, "application/sdp");
        return command;
    }

    static string InitialResponseHeaders(string to)
    {
        return "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-invite\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: " + to + ";tag=remote-tag\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n";
    }

    static SipMessage IncomingInvite()
    {
        return Parse("INVITE sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-incoming\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 77 INVITE\r\n"
            + "Contact: <sip:alice@caller.example.com>\r\n"
            + "Supported: 199, from-change\r\n"
            + "Session-ID: " + RemoteUuid
                + ";remote=00000000000000000000000000000000\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipInviteRequest IncomingResponseCommand(
        int status, string reason, string? extraHeader)
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Parse("SIP/2.0 " + status + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-incoming\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=local-tag\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 77 INVITE\r\n"
            + (status > 100 ? "Contact: <sip:bob@server.example.com>\r\n" : "")
            + (extraHeader == null ? "" : extraHeader)
            + "Content-Length: 0\r\n\r\n");
        return command;
    }

    static SipInviteRequest RefreshResponseCommand(SipMessage update)
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Parse("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP target.example.com:5060;branch=z9hG4bK-update\r\n"
            + "From: Carol <sip:carol@new.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: " + update.CSeq!.SequenceNumber + " UPDATE\r\n"
            + "Content-Length: 0\r\n\r\n");
        return command;
    }

    static void SetHeader(SipMessage message, string name, string value)
    {
        SipHeader header = message.Headers.Single(valueHeader =>
            string.Equals(valueHeader.Name, name,
                StringComparison.OrdinalIgnoreCase));
        header.Value = value;
        header.OriginalBytes = new byte[0];
        message.OriginalBytes = new byte[0];
    }

    static void AddHeader(SipMessage message, string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        message.Headers = message.Headers.Concat(new SipHeader[] { header }).ToArray();
        message.OriginalBytes = new byte[0];
    }

    static string? HeaderValue(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header =>
            string.Equals(header.Name, name,
                StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static void DrainEvents(SipInviteSession session)
    {
        while (session.PollInviteEvent().State != SipInviteSessionState.None)
        {
        }
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }
}
