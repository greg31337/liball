namespace sip.Tests;

public class SipInviteSessionTests
{
    [Test]
    public void StartInviteQueuesDetachedCallingEventAndLocalOffer()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest request = InviteRequest(OfferBody());
        request.CorrelationId = "call-one";

        SipInviteEvent started = session.StartInvite(request);
        request.Message!.RequestUri!.Host = "changed.example.com";
        request.Message.Body[0] = 0;
        SipInviteEvent queued = session.PollInviteEvent();
        started.Message!.RequestUri!.Host = "returned.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SipInviteSessionState.Calling));
            Assert.That(started.Message.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(started.CorrelationId, Is.EqualTo("call-one"));
            Assert.That(started.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(queued.Message!.RequestUri!.Host,
                Is.EqualTo("callee.example.com"));
            Assert.That(queued.Message.Body, Is.EqualTo(OfferBody()));
            Assert.That(queued.Message, Is.Not.SameAs(started.Message));
            Assert.That(session.PollInviteEvent().State,
                Is.EqualTo(SipInviteSessionState.None));
        });
    }

    [Test]
    public void ProvisionalResponseEstablishesEarlyDialogAndSignalsEarlyMedia()
    {
        SipInviteSession session = StartedSession(OfferBody());

        SipInviteEvent trying = session.ReceiveInviteMessage(
            InviteResponse(100, "Trying", null, null, null));
        SipInviteEvent progress = session.ReceiveInviteMessage(
            InviteResponse(183, "Session Progress", "remote-tag",
                AnswerBody(), "sip:bob@early.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(trying.State,
                Is.EqualTo(SipInviteSessionState.Proceeding));
            Assert.That(trying.Dialog, Is.Null);
            Assert.That(progress.State, Is.EqualTo(SipInviteSessionState.Early));
            Assert.That(progress.Dialog!.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(progress.Dialog.RemoteTarget!.Host,
                Is.EqualTo("early.example.com"));
            Assert.That(progress.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(progress.OfferAnswer.RemoteAnswer,
                Is.EqualTo(AnswerBody()));
            Assert.That(progress.OfferAnswer.NegotiatedBody,
                Is.EqualTo(AnswerBody()));
            Assert.That(progress.Reason, Does.Contain("media"));
        });
    }

    [Test]
    public void SuccessfulInviteConfirmsDialogAndGeneratesEndToEndAck()
    {
        SipInviteSession session = StartedSession(OfferBody());
        DrainEvents(session);

        SipInviteEvent confirmed = session.ReceiveInviteMessage(
            InviteResponse(200, "OK", "remote-tag", AnswerBody(),
                "sip:bob@confirmed.example.com"));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent ackEvent = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(confirmed.Dialog!.State,
                Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(confirmed.Dialog.RemoteTarget!.Host,
                Is.EqualTo("confirmed.example.com"));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(ackEvent.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(ackEvent.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ackEvent.Message.RequestUri!.Host,
                Is.EqualTo("confirmed.example.com"));
            Assert.That(ackEvent.Message.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(ackEvent.Message.CSeq.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ackEvent.Message.Body, Is.Empty);
        });
    }

    [Test]
    public void RepeatedSuccessfulResponseRegeneratesAckWithoutAdvancingCSeq()
    {
        SipInviteSession session = StartedSession(OfferBody());
        DrainEvents(session);
        SipMessage success = InviteResponse(200, "OK", "remote-tag", AnswerBody(),
            "sip:bob@confirmed.example.com");
        session.ReceiveInviteMessage(success);
        DrainEvents(session);

        SipInviteEvent repeated = session.ReceiveInviteMessage(success);
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent ackEvent = session.PollInviteEvent();
        SipInviteSnapshot current = session.GetInviteSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(repeated.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(ackEvent.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ackEvent.Message.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(current.Dialog!.LocalCSeq, Is.EqualTo(42));
            Assert.That(current.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(session.PollInviteEvent().State,
                Is.EqualTo(SipInviteSessionState.None));
        });
    }

    [Test]
    public void RedirectAcknowledgesFailureAndStartsFreshInviteAtBestContact()
    {
        SipInviteSession session = StartedSession(OfferBody());
        DrainEvents(session);
        SipMessage redirect = Parse("SIP/2.0 302 Moved Temporarily\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-invite\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=redirect-tag\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:bob@lower.example.com>;q=0.2\r\n"
            + "Contact: <sip:bob@preferred.example.com>;q=0.9\r\n"
            + "Content-Length: 0\r\n\r\n");

        SipInviteEvent followed = session.ReceiveInviteMessage(redirect);
        SipInviteEvent ack = session.PollInviteEvent();
        SipInviteEvent redirectedInvite = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(followed.State,
                Is.EqualTo(SipInviteSessionState.Calling));
            Assert.That(followed.Message!.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(followed.Message.RequestUri!.Host,
                Is.EqualTo("preferred.example.com"));
            Assert.That(followed.Message.CSeq!.SequenceNumber, Is.EqualTo(43));
            Assert.That(followed.Message.ViaValues[0].Branch,
                Is.Not.EqualTo("z9hG4bK-invite"));
            Assert.That(ack.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ack.Message.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(redirectedInvite.Message!.Method,
                Is.EqualTo(SipMethod.Invite));
            Assert.That(redirectedInvite.Message.CSeq!.SequenceNumber,
                Is.EqualTo(43));
        });
    }

    [Test]
    public void FailedInviteGeneratesAckRollsBackOfferAndTerminates()
    {
        SipInviteSession session = StartedSession(OfferBody());
        DrainEvents(session);

        SipInviteEvent failed = session.ReceiveInviteMessage(
            InviteResponse(486, "Busy Here", "remote-tag", null, null));
        SipInviteEvent ack = session.PollInviteEvent();
        SipInviteEvent terminal = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(failed.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Failed));
            Assert.That(ack.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ack.Message.RequestUri!.Host,
                Is.EqualTo("callee.example.com"));
            Assert.That(terminal.Message!.StatusCode, Is.EqualTo(486));
            Assert.That(terminal.Reason, Does.Contain("failed"));
            Assert.Throws<InvalidOperationException>(() =>
                session.ReceiveInviteMessage(
                    InviteResponse(486, "Busy Here", "remote-tag", null, null)));
        });
    }

    [TestCase(300)]
    [TestCase(404)]
    [TestCase(503)]
    [TestCase(603)]
    public void EveryUnsuccessfulFinalClassGeneratesAckAndTerminates(int status)
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest request = InviteRequest(OfferBody());
        request.Options.MaxRedirectCount = 0;
        session.StartInvite(request);
        DrainEvents(session);

        SipInviteEvent failed = session.ReceiveInviteMessage(
            InviteResponse(status, "Final Response", "remote-tag", null, null));
        SipInviteEvent ack = session.PollInviteEvent();
        SipInviteEvent terminal = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(ack.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ack.Message.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(terminal.Message!.StatusCode, Is.EqualTo(status));
            Assert.That(session.GetInviteSnapshot().State,
                Is.EqualTo(SipInviteSessionState.Terminated));
        });
    }

    [Test]
    public void CancellationWaitsForProvisionalThenSendsCancelAndEndsOn487()
    {
        SipInviteSession session = StartedSession(OfferBody());
        DrainEvents(session);
        SipCancellation cancellation = Cancellation("Caller changed their mind.");

        session.TerminateInvite(cancellation);
        SipInviteEvent pending = session.PollInviteEvent();
        SipInviteEvent trying = session.ReceiveInviteMessage(
            InviteResponse(100, "Trying", null, null, null));
        SipInviteEvent tryingEvent = session.PollInviteEvent();
        SipInviteEvent cancel = session.PollInviteEvent();
        SipInviteEvent cancelOk = session.ReceiveInviteMessage(
            RelatedResponse(cancel.Message!, 200, "OK"));
        SipInviteEvent terminated = session.ReceiveInviteMessage(
            InviteResponse(487, "Request Terminated", "remote-tag", null, null));

        Assert.Multiple(() =>
        {
            Assert.That(pending.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(pending.Message, Is.Null);
            Assert.That(trying.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(tryingEvent.Message!.StatusCode, Is.EqualTo(100));
            Assert.That(cancel.Message!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancel.Message.ViaValues[0].Branch,
                Is.EqualTo("z9hG4bK-invite"));
            Assert.That(cancel.Message.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(cancelOk.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(terminated.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
        });
    }

    [Test]
    public void SuccessfulCancellationRaceGeneratesAckThenByeAndCompletes()
    {
        SipInviteSession session = StartedSession(OfferBody());
        DrainEvents(session);
        session.ReceiveInviteMessage(
            InviteResponse(180, "Ringing", "remote-tag", null,
                "sip:bob@callee-dialog.example.com"));
        DrainEvents(session);
        session.TerminateInvite(Cancellation("Stop the call."));
        DrainEvents(session);

        SipInviteEvent raced = session.ReceiveInviteMessage(
            InviteResponse(200, "OK", "remote-tag", AnswerBody(),
                "sip:bob@callee-dialog.example.com"));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent ack = session.PollInviteEvent();
        SipInviteEvent bye = session.PollInviteEvent();
        SipInviteEvent completed = session.ReceiveInviteMessage(
            RelatedResponse(bye.Message!, 200, "OK"));

        Assert.Multiple(() =>
        {
            Assert.That(raced.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(ack.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(bye.Message!.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(bye.Message.CSeq!.SequenceNumber, Is.EqualTo(43));
            Assert.That(completed.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(completed.Dialog!.State,
                Is.EqualTo(SipDialogState.Terminated));
        });
    }

    [Test]
    public void ConfirmedTerminationSendsByeAndTreatsFinalFailureAsEnded()
    {
        SipInviteSession session = StartedSession(OfferBody());
        DrainEvents(session);
        session.ReceiveInviteMessage(
            InviteResponse(200, "OK", "remote-tag", AnswerBody(),
                "sip:bob@callee-dialog.example.com"));
        DrainEvents(session);

        session.TerminateInvite(Cancellation("Hang up."));
        SipInviteEvent bye = session.PollInviteEvent();
        SipInviteEvent ended = session.ReceiveInviteMessage(
            RelatedResponse(bye.Message!, 481, "Call Does Not Exist"));

        Assert.Multiple(() =>
        {
            Assert.That(bye.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(bye.Message!.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(ended.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(ended.Reason, Does.Contain("failed"));
        });
    }

    [Test]
    public void RedirectPolicyAndResponseMatchingRejectUnsafeOperations()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest noRedirects = InviteRequest(OfferBody());
        noRedirects.Options.MaxRedirectCount = 0;
        session.StartInvite(noRedirects);
        DrainEvents(session);

        SipInviteEvent ended = session.ReceiveInviteMessage(
            InviteResponse(302, "Moved Temporarily", "remote-tag", null,
                "sip:bob@redirect.example.com"));
        SipInviteSession other = StartedSession(OfferBody());
        SipMessage unmatched = InviteResponse(
            180, "Ringing", "remote-tag", null,
            "sip:bob@callee-dialog.example.com");
        unmatched.ViaValues[0].Branch = "z9hG4bK-other";

        Assert.Multiple(() =>
        {
            Assert.That(ended.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(ended.Reason, Does.Contain("redirect"));
            Assert.Throws<ArgumentException>(() =>
                other.ReceiveInviteMessage(unmatched));
            Assert.Throws<InvalidOperationException>(() =>
                other.StartInvite(InviteRequest(OfferBody())));
        });
    }

    static SipInviteSession StartedSession(byte[] body)
    {
        SipInviteSession session = new SipInviteSession();
        session.StartInvite(InviteRequest(body));
        return session;
    }

    static SipInviteRequest InviteRequest(byte[] body)
    {
        SipInviteRequest request = new SipInviteRequest();
        request.Message = Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-invite\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + (body.Length == 0 ? "" : "Content-Type: application/sdp\r\n")
            + "Content-Length: " + body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n\r\n"
            + System.Text.Encoding.ASCII.GetString(body));
        return request;
    }

    static SipMessage InviteResponse(int status, string reason, string? toTag,
        byte[]? body, string? contact)
    {
        byte[] actualBody = body ?? new byte[0];
        string toParameters = toTag == null ? "" : ";tag=" + toTag;
        string contactHeader = contact == null ? "" : "Contact: <" + contact + ">\r\n";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-invite\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>" + toParameters + "\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n" + contactHeader
            + (actualBody.Length == 0 ? "" : "Content-Type: application/sdp\r\n")
            + "Content-Length: " + actualBody.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n\r\n"
            + System.Text.Encoding.ASCII.GetString(actualBody));
    }

    static SipMessage RelatedResponse(SipMessage request, int status, string reason)
    {
        string method = request.MethodToken!;
        string toTag = request.To!.Parameters.Length == 0
            ? ";tag=remote-tag" : "";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + reason + "\r\n"
            + "Via: SIP/2.0/" + request.ViaValues[0].Transport + " "
            + request.ViaValues[0].Host + ";branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>" + toTag
            + (toTag.Length == 0 ? ";tag=remote-tag" : "") + "\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + method + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipCancellation Cancellation(string reason)
    {
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Reason = reason;
        return cancellation;
    }

    static void DrainEvents(SipInviteSession session)
    {
        while (session.PollInviteEvent().State != SipInviteSessionState.None)
        {
        }
    }

    static byte[] OfferBody()
    {
        return System.Text.Encoding.ASCII.GetBytes("v=0\r\no=offer\r\n");
    }

    static byte[] AnswerBody()
    {
        return System.Text.Encoding.ASCII.GetBytes("v=0\r\no=answer\r\n");
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
