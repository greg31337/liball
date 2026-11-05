namespace sip.Tests;

public class SipIncomingInviteSessionTests
{
    [Test]
    public void IncomingInviteQueuesAutomaticTryingAndRemoteOffer()
    {
        SipInviteSession session = new SipInviteSession();
        SipMessage invite = IncomingInvite(OfferBody());

        SipInviteEvent received = session.ReceiveInviteMessage(invite);
        invite.Body[0] = 0;
        SipInviteEvent trying = session.PollInviteEvent();
        SipInviteEvent queuedInvite = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(received.State,
                Is.EqualTo(SipInviteSessionState.Proceeding));
            Assert.That(received.Message!.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(received.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(received.OfferAnswer.RemoteOffer,
                Is.EqualTo(OfferBody()));
            Assert.That(trying.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Trying));
            Assert.That(trying.Message.CSeq!.Method,
                Is.EqualTo(SipMethod.Invite));
            Assert.That(trying.Reason, Does.Contain("Automatic"));
            Assert.That(queuedInvite.Message!.Body, Is.EqualTo(OfferBody()));
        });
    }

    [Test]
    public void RingingAndReliableProgressEstablishEarlyDialogAndAnswerOffer()
    {
        SipInviteSession session = StartedIncoming(OfferBody());
        DrainEvents(session);

        SipInviteEvent ringing = session.StartInvite(
            Command(IncomingResponse(180, "Ringing", null, false)));
        SipInviteEvent progress = session.StartInvite(
            Command(IncomingResponse(183, "Session Progress", AnswerBody(), true)));

        Assert.Multiple(() =>
        {
            Assert.That(ringing.State, Is.EqualTo(SipInviteSessionState.Early));
            Assert.That(ringing.Dialog!.Role, Is.EqualTo(SipDialogRole.Uas));
            Assert.That(ringing.Dialog.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(ringing.Reason, Does.Contain("Ringing"));
            Assert.That(progress.State, Is.EqualTo(SipInviteSessionState.Early));
            Assert.That(progress.Reason, Does.Contain("Reliable"));
            Assert.That(progress.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(progress.OfferAnswer.LocalAnswer,
                Is.EqualTo(AnswerBody()));
            Assert.That(progress.OfferAnswer.NegotiatedBody,
                Is.EqualTo(AnswerBody()));
        });
    }

    [Test]
    public void SuccessfulAnswerWaitsForMatchingAckAndConfirmsUsage()
    {
        SipInviteSession session = StartedIncoming(OfferBody());
        DrainEvents(session);

        SipInviteEvent answered = session.StartInvite(
            Command(IncomingResponse(200, "OK", AnswerBody(), false)));
        SipInviteEvent ack = session.ReceiveInviteMessage(
            AcceptedAck(null));

        Assert.Multiple(() =>
        {
            Assert.That(answered.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(answered.Dialog!.State,
                Is.EqualTo(SipDialogState.Confirmed));
            Assert.That(answered.Dialog.Role, Is.EqualTo(SipDialogRole.Uas));
            Assert.That(answered.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(ack.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(ack.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ack.Reason, Does.Contain("confirmed"));
        });
    }

    [Test]
    public void SuccessIsReplayedAfterAckLossAndRepeatedAckIsAccepted()
    {
        SipInviteSession session = StartedIncoming(OfferBody());
        DrainEvents(session);
        session.StartInvite(Command(
            IncomingResponse(200, "OK", AnswerBody(), false)));
        DrainEvents(session);

        SipInviteEvent retransmitted = session.ReceiveInviteMessage(
            IncomingInvite(OfferBody()));
        SipInviteEvent inviteEvent = session.PollInviteEvent();
        SipInviteEvent replay = session.PollInviteEvent();
        SipInviteEvent firstAck = session.ReceiveInviteMessage(AcceptedAck(null));
        session.PollInviteEvent();
        SipInviteEvent repeatedAck = session.ReceiveInviteMessage(AcceptedAck(null));

        Assert.Multiple(() =>
        {
            Assert.That(retransmitted.Reason, Does.Contain("retransmission"));
            Assert.That(inviteEvent.Message!.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(replay.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(replay.Reason, Does.Contain("replayed"));
            Assert.That(firstAck.Reason, Does.Contain("confirmed"));
            Assert.That(repeatedAck.Reason, Does.Contain("repeated"));
            Assert.That(repeatedAck.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(session.GetInviteSnapshot().Dialog!.State,
                Is.EqualTo(SipDialogState.Confirmed));
        });
    }

    [Test]
    public void RejectionAcceptsFailureAckAndReplaysFinalForInviteRetransmission()
    {
        SipInviteSession session = StartedIncoming(OfferBody());
        DrainEvents(session);

        SipInviteEvent rejected = session.StartInvite(
            Command(IncomingResponse(486, "Busy Here", null, false)));
        DrainEvents(session);
        SipInviteEvent ack = session.ReceiveInviteMessage(FailureAck("local-tag"));
        SipInviteEvent retransmission = session.ReceiveInviteMessage(
            IncomingInvite(OfferBody()));
        SipInviteEvent ackEvent = session.PollInviteEvent();
        SipInviteEvent inviteEvent = session.PollInviteEvent();
        SipInviteEvent replay = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(rejected.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Failed));
            Assert.That(ack.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(ackEvent.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(retransmission.Reason, Does.Contain("retransmission"));
            Assert.That(inviteEvent.Message!.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(replay.Message!.StatusCode, Is.EqualTo(486));
            Assert.That(replay.Reason, Does.Contain("replayed"));
        });
    }

    [Test]
    public void CancelBeforeFinalGenerates200And487ThenAcceptsAck()
    {
        SipInviteSession session = StartedIncoming(OfferBody());
        DrainEvents(session);

        SipInviteEvent cancelled = session.ReceiveInviteMessage(CancelRequest());
        SipInviteEvent cancelEvent = session.PollInviteEvent();
        SipInviteEvent cancelOk = session.PollInviteEvent();
        SipInviteEvent inviteTerminated = session.PollInviteEvent();
        string localTag = ReadTag(inviteTerminated.Message!.To!);
        SipInviteEvent ack = session.ReceiveInviteMessage(FailureAck(localTag));

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(cancelEvent.Message!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancelOk.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(cancelOk.Message.CSeq!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(inviteTerminated.Message.StatusCode, Is.EqualTo(487));
            Assert.That(inviteTerminated.Message.CSeq!.Method,
                Is.EqualTo(SipMethod.Invite));
            Assert.That(ack.Reason, Does.Contain("completed"));
        });
    }

    [Test]
    public void CancelAfterSuccessOnlyAcknowledgesCancelAndPreservesCall()
    {
        SipInviteSession session = StartedIncoming(OfferBody());
        DrainEvents(session);
        session.StartInvite(Command(
            IncomingResponse(200, "OK", AnswerBody(), false)));
        DrainEvents(session);

        SipInviteEvent cancel = session.ReceiveInviteMessage(CancelRequest());
        SipInviteEvent cancelEvent = session.PollInviteEvent();
        SipInviteEvent ok = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(cancel.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(cancel.Reason, Does.Contain("after"));
            Assert.That(cancelEvent.Message!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(ok.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(ok.Message.CSeq!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(session.PollInviteEvent().State,
                Is.EqualTo(SipInviteSessionState.None));
        });
    }

    [Test]
    public void RemoteByeTerminatesDialogAndGeneratesAutomaticOk()
    {
        SipInviteSession session = ConfirmedIncoming();
        DrainEvents(session);

        SipInviteEvent ended = session.ReceiveInviteMessage(RemoteBye());
        SipInviteEvent bye = session.PollInviteEvent();
        SipInviteEvent ok = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(ended.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(ended.Dialog!.State,
                Is.EqualTo(SipDialogState.Terminated));
            Assert.That(bye.Message!.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(ok.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(ok.Message.CSeq!.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(ok.Reason, Does.Contain("Automatic"));
        });
    }

    [Test]
    public void LocalTerminationRejectsPendingInviteOrSendsByeForConfirmedCall()
    {
        SipInviteSession pending = StartedIncoming(OfferBody());
        DrainEvents(pending);
        pending.TerminateInvite(Cancellation("Declined locally."));
        SipInviteEvent rejected = pending.PollInviteEvent();

        SipInviteSession confirmed = ConfirmedIncoming();
        DrainEvents(confirmed);
        confirmed.TerminateInvite(Cancellation("Local hangup."));
        SipInviteEvent bye = confirmed.PollInviteEvent();
        SipInviteEvent ended = confirmed.ReceiveInviteMessage(
            ResponseForLocalBye(bye.Message!, 200, "OK"));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(rejected.Message!.StatusCode, Is.EqualTo(487));
            Assert.That(rejected.Reason, Is.EqualTo("Declined locally."));
            Assert.That(bye.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(bye.Message!.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(ended.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
        });
    }

    [Test]
    public void OfferlessInviteCanReceiveOfferAndAnswerItInAck()
    {
        SipInviteSession session = StartedIncoming(new byte[0]);
        DrainEvents(session);

        SipInviteEvent offered = session.StartInvite(
            Command(IncomingResponse(200, "OK", OfferBody(), false)));
        SipInviteEvent answered = session.ReceiveInviteMessage(
            AcceptedAck(AnswerBody()));

        Assert.Multiple(() =>
        {
            Assert.That(offered.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(offered.OfferAnswer.LocalOffer, Is.EqualTo(OfferBody()));
            Assert.That(answered.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(answered.OfferAnswer.RemoteAnswer,
                Is.EqualTo(AnswerBody()));
            Assert.That(answered.OfferAnswer.NegotiatedBody,
                Is.EqualTo(AnswerBody()));
        });
    }

    [Test]
    public void MismatchedCommandsAndMessagesAreRejectedWithoutStateChange()
    {
        SipInviteSession session = StartedIncoming(OfferBody());
        DrainEvents(session);
        SipMessage wrongResponse = IncomingResponse(
            180, "Ringing", null, false);
        wrongResponse.CSeq!.SequenceNumber = 78;
        SipMessage wrongCancel = CancelRequest();
        wrongCancel.ViaValues[0].Branch = "z9hG4bK-other";

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                session.StartInvite(Command(wrongResponse)));
            Assert.Throws<ArgumentException>(() =>
                session.ReceiveInviteMessage(wrongCancel));
            Assert.That(session.PollInviteEvent().State,
                Is.EqualTo(SipInviteSessionState.None));
        });
    }

    static SipInviteSession StartedIncoming(byte[] body)
    {
        SipInviteSession session = new SipInviteSession();
        session.ReceiveInviteMessage(IncomingInvite(body));
        return session;
    }

    static SipInviteSession ConfirmedIncoming()
    {
        SipInviteSession session = StartedIncoming(OfferBody());
        DrainEvents(session);
        session.StartInvite(Command(
            IncomingResponse(200, "OK", AnswerBody(), false)));
        session.ReceiveInviteMessage(AcceptedAck(null));
        return session;
    }

    static SipInviteRequest Command(SipMessage response)
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Message = response;
        return command;
    }

    static SipMessage IncomingInvite(byte[] body)
    {
        return Parse("INVITE sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-incoming\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 77 INVITE\r\n"
            + "Contact: <sip:alice@caller.example.com>\r\n"
            + "Supported: 100rel\r\n"
            + BodyHeaders(body) + System.Text.Encoding.ASCII.GetString(body));
    }

    static SipMessage IncomingResponse(
        int status, string reason, byte[]? body, bool reliable)
    {
        byte[] actualBody = body ?? new byte[0];
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-incoming\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=local-tag\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 77 INVITE\r\n"
            + (status > 100 && status < 300
                ? "Contact: <sip:bob@server.example.com>\r\n" : "")
            + (reliable ? "Require: 100rel\r\nRSeq: 1\r\n" : "")
            + BodyHeaders(actualBody)
            + System.Text.Encoding.ASCII.GetString(actualBody));
    }

    static SipMessage CancelRequest()
    {
        return Parse("CANCEL sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-incoming\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 77 CANCEL\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage FailureAck(string localTag)
    {
        return Parse("ACK sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-incoming\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=" + localTag + "\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 77 ACK\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage AcceptedAck(byte[]? body)
    {
        byte[] actualBody = body ?? new byte[0];
        return Parse("ACK sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-ack\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=local-tag\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 77 ACK\r\n" + BodyHeaders(actualBody)
            + System.Text.Encoding.ASCII.GetString(actualBody));
    }

    static SipMessage RemoteBye()
    {
        return Parse("BYE sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-bye\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=local-tag\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: 78 BYE\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage ResponseForLocalBye(
        SipMessage bye, int status, string reason)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + reason + "\r\n"
            + "Via: SIP/2.0/" + bye.ViaValues[0].Transport + " "
            + bye.ViaValues[0].Host + ";branch=" + bye.ViaValues[0].Branch + "\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=local-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=remote-tag\r\n"
            + "Call-ID: incoming-call@example.com\r\n"
            + "CSeq: " + bye.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " BYE\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static string BodyHeaders(byte[] body)
    {
        return (body.Length == 0 ? "" : "Content-Type: application/sdp\r\n")
            + "Content-Length: " + body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n\r\n";
    }

    static string ReadTag(SipNameAddress address)
    {
        return address.Parameters.Single(parameter =>
            string.Equals(parameter.Name, "tag",
                StringComparison.OrdinalIgnoreCase)).Value!;
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
