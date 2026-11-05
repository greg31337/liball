namespace sip.Tests;

public class SipInviteRefreshTests
{
    [Test]
    public void UpdateSignalsHoldAndResumeAndRefreshesBothTargets()
    {
        SipInviteSession session = ConfirmedSession();

        SipInviteEvent hold = session.StartInvite(
            RefreshCommand(SipMethods.Update, HoldOffer(),
                "sip:alice@hold-client.example.com"));
        SipMessage holdRequest = hold.Message!;
        DrainEvents(session);
        SipInviteEvent held = session.ReceiveInviteMessage(
            LocalResponse(holdRequest, 200, "OK", HoldAnswer(),
                "sip:bob@hold-peer.example.com", null));
        DrainEvents(session);

        SipInviteEvent resume = session.StartInvite(
            RefreshCommand(SipMethods.Update, ResumeOffer(),
                "sip:alice@resume-client.example.com"));
        SipMessage resumeRequest = resume.Message!;
        DrainEvents(session);
        SipInviteEvent resumed = session.ReceiveInviteMessage(
            LocalResponse(resumeRequest, 200, "OK", ResumeAnswer(),
                "sip:bob@resume-peer.example.com", null));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent none = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(holdRequest.Method, Is.EqualTo(SipMethod.Update));
            Assert.That(holdRequest.CSeq!.SequenceNumber, Is.EqualTo(43));
            Assert.That(holdRequest.Body, Is.EqualTo(HoldOffer()));
            Assert.That(held.Dialog!.LocalTarget!.Host,
                Is.EqualTo("hold-client.example.com"));
            Assert.That(held.Dialog.RemoteTarget!.Host,
                Is.EqualTo("hold-peer.example.com"));
            Assert.That(held.OfferAnswer!.NegotiatedBody,
                Is.EqualTo(HoldAnswer()));
            Assert.That(resumeRequest.CSeq!.SequenceNumber, Is.EqualTo(44));
            Assert.That(resumeRequest.RequestUri!.Host,
                Is.EqualTo("hold-peer.example.com"));
            Assert.That(resumeRequest.Body, Is.EqualTo(ResumeOffer()));
            Assert.That(resumed.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(resumed.OfferAnswer.NegotiatedBody,
                Is.EqualTo(ResumeAnswer()));
            Assert.That(resumed.Dialog!.LocalTarget!.Host,
                Is.EqualTo("resume-client.example.com"));
            Assert.That(resumed.Dialog.RemoteTarget!.Host,
                Is.EqualTo("resume-peer.example.com"));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(none.State, Is.EqualTo(SipInviteSessionState.None));
        });
    }

    [Test]
    public void OutgoingUpdateRejectsProvisionalResponseAndRemainsPending()
    {
        SipInviteSession session = ConfirmedSession();
        SipInviteEvent started = session.StartInvite(
            RefreshCommand(SipMethods.Update, HoldOffer(),
                "sip:alice@pending-client.example.com"));
        SipMessage update = started.Message!;
        DrainEvents(session);

        Assert.Throws<SipProtocolException>(() => session.ReceiveInviteMessage(
            LocalResponse(update, 180, "Ringing", null, null, null)));
        SipInviteSnapshot pending = session.GetInviteSnapshot();
        SipInviteEvent completed = session.ReceiveInviteMessage(
            LocalResponse(update, 200, "OK", HoldAnswer(),
                "sip:bob@updated-peer.example.com", null));

        Assert.Multiple(() =>
        {
            Assert.That(pending.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(pending.Dialog!.LocalTarget!.Host,
                Is.EqualTo("client.example.com"));
            Assert.That(pending.Dialog.RemoteTarget!.Host,
                Is.EqualTo("callee-dialog.example.com"));
            Assert.That(session.PollInviteEvent().Message!.StatusCode,
                Is.EqualTo(200));
            Assert.That(completed.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(completed.Dialog!.LocalTarget!.Host,
                Is.EqualTo("pending-client.example.com"));
        });
    }

    [Test]
    public void SuccessfulReinviteUsesRefreshCseqForAckAndNewRemoteTarget()
    {
        SipInviteSession session = ConfirmedSession();
        SipInviteEvent started = session.StartInvite(
            RefreshCommand(SipMethods.Invite, ResumeOffer(), null));
        SipMessage reinvite = started.Message!;
        DrainEvents(session);

        SipInviteEvent completed = session.ReceiveInviteMessage(
            LocalResponse(reinvite, 200, "OK", ResumeAnswer(),
                "sip:bob@reinvite-peer.example.com", null));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent ackEvent = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(reinvite.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(reinvite.CSeq!.SequenceNumber, Is.EqualTo(43));
            Assert.That(completed.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(completed.Dialog!.RemoteTarget!.Host,
                Is.EqualTo("reinvite-peer.example.com"));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(ackEvent.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ackEvent.Message.CSeq!.SequenceNumber,
                Is.EqualTo(reinvite.CSeq.SequenceNumber));
            Assert.That(ackEvent.Message.CSeq.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ackEvent.Message.RequestUri!.Host,
                Is.EqualTo("reinvite-peer.example.com"));
        });
    }

    [Test]
    public void FailedReinviteRollsBackOfferAndReportsRetryAfter()
    {
        SipInviteSession session = ConfirmedSession();
        SipInviteEvent started = session.StartInvite(
            RefreshCommand(SipMethods.Invite, HoldOffer(),
                "sip:alice@uncommitted.example.com"));
        SipMessage reinvite = started.Message!;
        DrainEvents(session);

        SipInviteEvent failed = session.ReceiveInviteMessage(
            LocalResponse(reinvite, 491, "Request Pending", null,
                null, "7"));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent ackEvent = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(failed.Reason, Does.Contain("Retry-After 7"));
            Assert.That(failed.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Failed));
            Assert.That(failed.OfferAnswer.NegotiatedBody,
                Is.EqualTo(InitialAnswer()));
            Assert.That(failed.Dialog!.LocalTarget!.Host,
                Is.EqualTo("client.example.com"));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(491));
            Assert.That(ackEvent.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ackEvent.Message.CSeq!.SequenceNumber,
                Is.EqualTo(reinvite.CSeq!.SequenceNumber));
            Assert.That(ackEvent.Message.RequestUri!.Host,
                Is.EqualTo("callee-dialog.example.com"));
        });

        Assert.DoesNotThrow(() => session.StartInvite(
            RefreshCommand(SipMethods.Update, ResumeOffer(), null)));
    }

    [Test]
    public void IncomingUpdateNegotiatesBodyAndCommitsTargetRefreshOnSuccess()
    {
        SipInviteSession session = ConfirmedSession();
        SipMessage update = IncomingRefresh(
            SipMethods.Update, 70, "z9hG4bK-update", HoldOffer(),
            "sip:bob@updated-peer.example.com");

        SipInviteEvent received = session.ReceiveInviteMessage(update);
        DrainEvents(session);
        SipInviteEvent accepted = session.StartInvite(
            IncomingResponseCommand(update, 200, "OK", HoldAnswer(),
                "sip:alice@updated-client.example.com"));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent none = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(received.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(received.Dialog!.RemoteTarget!.Host,
                Is.EqualTo("callee-dialog.example.com"));
            Assert.That(accepted.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(accepted.OfferAnswer.LocalAnswer,
                Is.EqualTo(HoldAnswer()));
            Assert.That(accepted.Dialog!.RemoteTarget!.Host,
                Is.EqualTo("updated-peer.example.com"));
            Assert.That(accepted.Dialog.LocalTarget!.Host,
                Is.EqualTo("updated-client.example.com"));
            Assert.That(responseEvent.Message!.CSeq!.Method,
                Is.EqualTo(SipMethod.Update));
            Assert.That(none.State, Is.EqualTo(SipInviteSessionState.None));
        });

        Assert.DoesNotThrow(() => session.StartInvite(
            RefreshCommand(SipMethods.Update, ResumeOffer(), null)));
    }

    [Test]
    public void FailedIncomingUpdateRollsBackBodyAndTargetRefresh()
    {
        SipInviteSession session = ConfirmedSession();
        SipMessage update = IncomingRefresh(
            SipMethods.Update, 70, "z9hG4bK-rejected-update", HoldOffer(),
            "sip:bob@rejected-peer.example.com");
        session.ReceiveInviteMessage(update);
        DrainEvents(session);

        SipInviteEvent rejected = session.StartInvite(
            IncomingResponseCommand(update, 488, "Not Acceptable Here",
                new byte[0], "sip:alice@rejected-client.example.com"));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent none = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(rejected.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Failed));
            Assert.That(rejected.OfferAnswer.NegotiatedBody,
                Is.EqualTo(InitialAnswer()));
            Assert.That(rejected.Dialog!.RemoteTarget!.Host,
                Is.EqualTo("callee-dialog.example.com"));
            Assert.That(rejected.Dialog.LocalTarget!.Host,
                Is.EqualTo("client.example.com"));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(488));
            Assert.That(none.State, Is.EqualTo(SipInviteSessionState.None));
        });

        Assert.DoesNotThrow(() => session.StartInvite(
            RefreshCommand(SipMethods.Update, ResumeOffer(), null)));
    }

    [Test]
    public void IncomingReinviteRejectsOverlapUntilMatchingAckCompletesIt()
    {
        SipInviteSession session = ConfirmedSession();
        SipMessage reinvite = IncomingRefresh(
            SipMethods.Invite, 80, "z9hG4bK-reinvite", HoldOffer(),
            "sip:bob@incoming-peer.example.com");
        session.ReceiveInviteMessage(reinvite);
        DrainEvents(session);

        Assert.Throws<InvalidOperationException>(() => session.StartInvite(
            RefreshCommand(SipMethods.Update, ResumeOffer(), null)));

        SipMessage overlap = IncomingRefresh(
            SipMethods.Update, 81, "z9hG4bK-overlap", ResumeOffer(),
            "sip:bob@overlap.example.com");
        SipInviteEvent glare = session.ReceiveInviteMessage(overlap);
        SipInviteEvent overlapEvent = session.PollInviteEvent();
        SipInviteEvent pendingResponse = session.PollInviteEvent();
        DrainEvents(session);

        SipInviteEvent accepted = session.StartInvite(
            IncomingResponseCommand(reinvite, 200, "OK", HoldAnswer(),
                "sip:alice@incoming-client.example.com"));
        DrainEvents(session);
        SipInviteEvent repeated = session.ReceiveInviteMessage(reinvite);
        SipInviteEvent repeatedEvent = session.PollInviteEvent();
        SipInviteEvent replayedResponse = session.PollInviteEvent();
        DrainEvents(session);

        Assert.Throws<InvalidOperationException>(() => session.StartInvite(
            RefreshCommand(SipMethods.Update, ResumeOffer(), null)));
        SipInviteEvent acknowledged = session.ReceiveInviteMessage(
            IncomingAck(reinvite));

        Assert.Multiple(() =>
        {
            Assert.That(glare.Reason, Does.Contain("overlapping"));
            Assert.That(overlapEvent.Message!.Method,
                Is.EqualTo(SipMethod.Update));
            Assert.That(pendingResponse.Message!.StatusCode, Is.EqualTo(491));
            Assert.That(HeaderValue(pendingResponse.Message,
                SipHeaderNames.RetryAfter), Is.EqualTo("1"));
            Assert.That(accepted.Dialog!.RemoteTarget!.Host,
                Is.EqualTo("incoming-peer.example.com"));
            Assert.That(repeated.Reason, Does.Contain("retransmission"));
            Assert.That(repeatedEvent.Message!.Method,
                Is.EqualTo(SipMethod.Invite));
            Assert.That(replayedResponse.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(acknowledged.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(acknowledged.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
        });

        DrainEvents(session);
        Assert.DoesNotThrow(() => session.StartInvite(
            RefreshCommand(SipMethods.Update, ResumeOffer(), null)));
    }

    static SipInviteSession ConfirmedSession()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest request = new SipInviteRequest();
        request.Message = Parse(
            "INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-initial\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: refresh-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + BodyHeaders(InitialOffer())
            + System.Text.Encoding.ASCII.GetString(InitialOffer()));
        session.StartInvite(request);
        DrainEvents(session);
        session.ReceiveInviteMessage(Parse(
            "SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-initial\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:bob@callee-dialog.example.com>\r\n"
            + BodyHeaders(InitialAnswer())
            + System.Text.Encoding.ASCII.GetString(InitialAnswer())));
        DrainEvents(session);
        return session;
    }

    static SipInviteRequest RefreshCommand(
        string method, byte[] body, string? contact)
    {
        string? actualContact = contact;
        if (actualContact == null && method == SipMethods.Invite)
        {
            actualContact = "sip:alice@client.example.com";
        }
        string contactHeader = actualContact == null
            ? "" : "Contact: <" + actualContact + ">\r\n";
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Parse(method
            + " sip:bob@template.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP template.example.com;branch=z9hG4bK-template\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:template@example.com>;tag=template-from\r\n"
            + "To: <sip:peer@example.com>;tag=template-to\r\n"
            + "Call-ID: template@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + contactHeader + BodyHeaders(body)
            + System.Text.Encoding.ASCII.GetString(body));
        return command;
    }

    static SipMessage LocalResponse(
        SipMessage request, int status, string reason, byte[]? body,
        string? contact, string? retryAfter)
    {
        byte[] actualBody = body ?? new byte[0];
        string contactHeader = contact == null
            ? "" : "Contact: <" + contact + ">\r\n";
        string retryHeader = retryAfter == null
            ? "" : "Retry-After: " + retryAfter + "\r\n";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP dialog.invalid;branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + request.MethodToken + "\r\n"
            + contactHeader + retryHeader + BodyHeaders(actualBody)
            + System.Text.Encoding.ASCII.GetString(actualBody));
    }

    static SipMessage IncomingRefresh(
        string method, int sequence, string branch, byte[] body,
        string contact)
    {
        return Parse(method + " sip:alice@client.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP peer.example.com;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\n"
            + "CSeq: " + sequence.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\n"
            + "Contact: <" + contact + ">\r\n"
            + BodyHeaders(body)
            + System.Text.Encoding.ASCII.GetString(body));
    }

    static SipInviteRequest IncomingResponseCommand(
        SipMessage request, int status, string reason, byte[] body,
        string contact)
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP peer.example.com;branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + request.MethodToken + "\r\n"
            + "Contact: <" + contact + ">\r\n"
            + BodyHeaders(body)
            + System.Text.Encoding.ASCII.GetString(body));
        return command;
    }

    static SipMessage IncomingAck(SipMessage request)
    {
        return Parse("ACK sip:alice@incoming-client.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP peer.example.com;branch=z9hG4bK-ack\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " ACK\r\nContent-Length: 0\r\n\r\n");
    }

    static string? HeaderValue(SipMessage message, string name)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            if (string.Equals(message.Headers[index].Name, name,
                StringComparison.OrdinalIgnoreCase))
            {
                return message.Headers[index].Value;
            }
            index++;
        }
        return null;
    }

    static string BodyHeaders(byte[] body)
    {
        return (body.Length == 0 ? "" : "Content-Type: application/sdp\r\n")
            + "Content-Length: " + body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n\r\n";
    }

    static void DrainEvents(SipInviteSession session)
    {
        while (session.PollInviteEvent().State != SipInviteSessionState.None)
        {
        }
    }

    static byte[] InitialOffer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\na=sendrecv\r\n");
    }

    static byte[] InitialAnswer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\na=sendrecv-answer\r\n");
    }

    static byte[] HoldOffer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\na=sendonly\r\n");
    }

    static byte[] HoldAnswer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\na=recvonly\r\n");
    }

    static byte[] ResumeOffer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\na=sendrecv\r\n");
    }

    static byte[] ResumeAnswer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\na=sendrecv-peer\r\n");
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
