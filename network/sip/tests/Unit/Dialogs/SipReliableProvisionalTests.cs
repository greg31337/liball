namespace sip.Tests;

public class SipReliableProvisionalTests
{
    [Test]
    public void OutgoingPolicyAdvertisesSupportAndReliableProgressGeneratesPrack()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest request = OutgoingRequest(OfferBody(), true);

        SipInviteEvent started = session.StartInvite(request);
        DrainEvents(session);
        SipInviteEvent progress = session.ReceiveInviteMessage(
            OutgoingReliableResponse(183, 31, AnswerBody()));
        SipInviteEvent progressEvent = session.PollInviteEvent();
        SipInviteEvent prackEvent = session.PollInviteEvent();
        SipMessage prack = prackEvent.Message!;

        Assert.Multiple(() =>
        {
            Assert.That(HeaderValue(started.Message!, SipHeaderNames.Supported),
                Does.Contain("100rel"));
            Assert.That(progress.State, Is.EqualTo(SipInviteSessionState.Early));
            Assert.That(progress.Reason, Does.Contain("reliable").IgnoreCase);
            Assert.That(progress.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(progressEvent.Message!.StatusCode, Is.EqualTo(183));
            Assert.That(prack.Method, Is.EqualTo(SipMethod.Prack));
            Assert.That(prack.CSeq!.SequenceNumber, Is.EqualTo(43));
            Assert.That(HeaderValue(prack, SipHeaderNames.RAck),
                Is.EqualTo("31 42 INVITE"));
            Assert.That(prack.Body, Is.Empty);
        });

        session.ReceiveInviteMessage(ResponseForRequest(prack, 200, "OK"));
        DrainEvents(session);
        session.AdvanceInviteTime(64000);
        Assert.That(session.PollInviteEvent().State,
            Is.EqualTo(SipInviteSessionState.None));
    }

    [Test]
    public void DuplicateReliableResponseReplaysPrackAndSequenceGapCancelsInvite()
    {
        SipInviteSession session = StartedOutgoing(OfferBody(), true);
        DrainEvents(session);
        SipMessage first = OutgoingReliableResponse(183, 5, AnswerBody());
        session.ReceiveInviteMessage(first);
        DrainEvents(session);

        SipInviteEvent duplicate = session.ReceiveInviteMessage(first);
        SipInviteEvent duplicateEvent = session.PollInviteEvent();
        SipInviteEvent replayedPrack = session.PollInviteEvent();
        DrainEvents(session);
        SipInviteEvent gap = session.ReceiveInviteMessage(
            OutgoingReliableResponse(183, 7, AnswerBody()));
        SipInviteEvent gapEvent = session.PollInviteEvent();
        SipInviteEvent cancel = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Reason, Does.Contain("retransmitted"));
            Assert.That(duplicateEvent.Message!.StatusCode, Is.EqualTo(183));
            Assert.That(replayedPrack.Message!.Method, Is.EqualTo(SipMethod.Prack));
            Assert.That(gap.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(gap.Reason, Does.Contain("out of order"));
            Assert.That(gapEvent.Message!.StatusCode, Is.EqualTo(183));
            Assert.That(cancel.Message!.Method, Is.EqualTo(SipMethod.Cancel));
        });
    }

    [Test]
    public void PrackRetransmitsAndTimeoutStartsInviteCancellation()
    {
        SipInviteSession session = StartedOutgoing(OfferBody(), true);
        DrainEvents(session);
        session.ReceiveInviteMessage(
            OutgoingReliableResponse(183, 1, AnswerBody()));
        DrainEvents(session);

        session.AdvanceInviteTime(500);
        SipInviteEvent retransmission = session.PollInviteEvent();
        session.AdvanceInviteTime(32000);
        SipInviteEvent timeout = session.PollInviteEvent();
        SipInviteEvent cancel = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(retransmission.Message!.Method,
                Is.EqualTo(SipMethod.Prack));
            Assert.That(retransmission.Reason,
                Does.Contain("retransmission timer"));
            Assert.That(timeout.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(timeout.Reason, Does.Contain("timed out"));
            Assert.That(cancel.Message!.Method, Is.EqualTo(SipMethod.Cancel));
        });
    }

    [Test]
    public void OfferInReliableResponseWaitsForAnswerAndPlacesItInPrack()
    {
        SipInviteSession session = StartedOutgoing(new byte[0], true);
        DrainEvents(session);

        SipInviteEvent offered = session.ReceiveInviteMessage(
            OutgoingReliableResponse(183, 9, OfferBody()));
        SipInviteEvent offeredEvent = session.PollInviteEvent();
        SipInviteEvent none = session.PollInviteEvent();
        SipInviteRequest answer = new SipInviteRequest();
        answer.OfferAnswer = new SipOfferAnswer();
        answer.OfferAnswer.LocalAnswer = AnswerBody();
        answer.OfferAnswer.ContentType = SdpType();
        SipInviteEvent prack = session.StartInvite(answer);

        Assert.Multiple(() =>
        {
            Assert.That(offered.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(offeredEvent.Message!.StatusCode, Is.EqualTo(183));
            Assert.That(none.State, Is.EqualTo(SipInviteSessionState.None));
            Assert.That(prack.Message!.Method, Is.EqualTo(SipMethod.Prack));
            Assert.That(prack.Message.Body, Is.EqualTo(AnswerBody()));
            Assert.That(HeaderValue(prack.Message, SipHeaderNames.ContentType),
                Is.EqualTo("application/sdp"));
            Assert.That(prack.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
        });
    }

    [Test]
    public void InvalidPrackAnswerPlacementLeavesOfferPendingForValidAnswer()
    {
        SipInviteSession session = StartedOutgoing(new byte[0], true);
        DrainEvents(session);
        session.ReceiveInviteMessage(
            OutgoingReliableResponse(183, 9, OfferBody()));
        DrainEvents(session);
        SipInviteRequest invalid = new SipInviteRequest();
        invalid.OfferAnswer = new SipOfferAnswer();
        invalid.OfferAnswer.LocalAnswer = AnswerBody();
        invalid.OfferAnswer.RemoteAnswer = AnswerBody();
        invalid.OfferAnswer.ContentType = SdpType();

        Assert.Throws<ArgumentException>(() => session.StartInvite(invalid));
        SipInviteSnapshot pending = session.GetInviteSnapshot();
        SipInviteRequest valid = new SipInviteRequest();
        valid.OfferAnswer = new SipOfferAnswer();
        valid.OfferAnswer.LocalAnswer = AnswerBody();
        valid.OfferAnswer.ContentType = SdpType();
        SipInviteEvent prack = session.StartInvite(valid);

        Assert.Multiple(() =>
        {
            Assert.That(pending.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.RemoteOfferPending));
            Assert.That(session.PollInviteEvent().Message!.Method,
                Is.EqualTo(SipMethod.Prack));
            Assert.That(prack.Message!.Body, Is.EqualTo(AnswerBody()));
            Assert.That(prack.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
        });
    }

    [Test]
    public void RequiredIncomingReliabilityAddsHeadersRetransmitsAndStopsOnPrack()
    {
        SipInviteSession session = StartedIncoming(OfferBody(), true);
        DrainEvents(session);
        SipInviteRequest ringing = IncomingCommand(
            IncomingResponse(180, null, false));

        SipInviteEvent sent = session.StartInvite(ringing);
        DrainEvents(session);
        session.AdvanceInviteTime(500);
        SipInviteEvent retransmission = session.PollInviteEvent();
        SipInviteEvent prack = session.ReceiveInviteMessage(
            IncomingPrack(1, null));
        SipInviteEvent prackEvent = session.PollInviteEvent();
        SipInviteEvent ok = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(HeaderValue(sent.Message!, SipHeaderNames.Require),
                Does.Contain("100rel"));
            Assert.That(HeaderValue(sent.Message!, SipHeaderNames.RSeq),
                Is.EqualTo("1"));
            Assert.That(retransmission.Message!.StatusCode, Is.EqualTo(180));
            Assert.That(retransmission.Reason, Does.Contain("timer"));
            Assert.That(prack.Reason, Does.Contain("acknowledged"));
            Assert.That(prackEvent.Message!.Method, Is.EqualTo(SipMethod.Prack));
            Assert.That(ok.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(ok.Message.CSeq!.Method, Is.EqualTo(SipMethod.Prack));
        });

        DrainEvents(session);
        session.AdvanceInviteTime(64000);
        Assert.That(session.PollInviteEvent().State,
            Is.EqualTo(SipInviteSessionState.None));
    }

    [Test]
    public void IncomingOfferInReliableResponseIsAnsweredByPrack()
    {
        SipInviteSession session = StartedIncoming(new byte[0], true);
        DrainEvents(session);
        SipInviteRequest progress = IncomingCommand(
            IncomingResponse(183, OfferBody(), false));

        SipInviteEvent offered = session.StartInvite(progress);
        DrainEvents(session);
        SipInviteEvent answered = session.ReceiveInviteMessage(
            IncomingPrack(1, AnswerBody()));
        SipInviteEvent received = session.PollInviteEvent();
        SipInviteEvent ok = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(offered.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(answered.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(answered.OfferAnswer.RemoteAnswer,
                Is.EqualTo(AnswerBody()));
            Assert.That(received.Message!.Body, Is.EqualTo(AnswerBody()));
            Assert.That(ok.Message!.StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public void DuplicatePrackReplaysOkAndNextReliableResponseAdvancesRSeq()
    {
        SipInviteSession session = StartedIncoming(OfferBody(), true);
        DrainEvents(session);
        SipInviteEvent firstResponse = session.StartInvite(IncomingCommand(
            IncomingResponse(183, AnswerBody(), false)));
        DrainEvents(session);
        SipMessage firstPrack = IncomingPrackWithKey(
            1, 78, "z9hG4bK-prack-one", 77, SipMethods.Invite, null);
        session.ReceiveInviteMessage(firstPrack);
        DrainEvents(session);

        SipInviteEvent duplicate = session.ReceiveInviteMessage(firstPrack);
        SipInviteEvent duplicateEvent = session.PollInviteEvent();
        SipInviteEvent replay = session.PollInviteEvent();
        SipInviteEvent secondResponse = session.StartInvite(IncomingCommand(
            IncomingResponse(180, null, false)));
        DrainEvents(session);
        SipInviteEvent secondPrack = session.ReceiveInviteMessage(
            IncomingPrackWithKey(2, 79, "z9hG4bK-prack-two", 77,
                SipMethods.Invite, null));
        SipInviteEvent secondPrackEvent = session.PollInviteEvent();
        SipInviteEvent secondOk = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(HeaderValue(firstResponse.Message!, SipHeaderNames.RSeq),
                Is.EqualTo("1"));
            Assert.That(duplicate.Reason, Does.Contain("retransmission"));
            Assert.That(duplicateEvent.Message!.Method, Is.EqualTo(SipMethod.Prack));
            Assert.That(replay.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(replay.Reason, Does.Contain("replayed"));
            Assert.That(HeaderValue(secondResponse.Message!, SipHeaderNames.RSeq),
                Is.EqualTo("2"));
            Assert.That(secondPrack.Reason, Does.Contain("acknowledged"));
            Assert.That(secondPrackEvent.Message!.CSeq!.SequenceNumber,
                Is.EqualTo(79));
            Assert.That(secondOk.Message!.StatusCode, Is.EqualTo(200));
        });
    }

    [TestCase("1 76 INVITE")]
    [TestCase("1 77 UPDATE")]
    public void RackCSeqAndMethodMismatchGet481WithoutClearingPending(string rack)
    {
        SipInviteSession session = StartedIncoming(OfferBody(), true);
        DrainEvents(session);
        session.StartInvite(IncomingCommand(
            IncomingResponse(180, null, false)));
        DrainEvents(session);
        SipMessage invalidPrack = IncomingPrack(1, null);
        invalidPrack.Headers.Single(header => string.Equals(
            header.Name, SipHeaderNames.RAck,
            StringComparison.OrdinalIgnoreCase)).Value = rack;

        SipInviteEvent invalid = session.ReceiveInviteMessage(invalidPrack);
        SipInviteEvent invalidEvent = session.PollInviteEvent();
        SipInviteEvent missing = session.PollInviteEvent();
        SipInviteEvent accepted = session.ReceiveInviteMessage(
            IncomingPrackWithKey(1, 79, "z9hG4bK-valid-prack", 77,
                SipMethods.Invite, null));
        SipInviteEvent acceptedEvent = session.PollInviteEvent();
        SipInviteEvent ok = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Reason, Does.Contain("did not match"));
            Assert.That(invalidEvent.Message!.Method, Is.EqualTo(SipMethod.Prack));
            Assert.That(missing.Message!.StatusCode, Is.EqualTo(481));
            Assert.That(accepted.Reason, Does.Contain("acknowledged"));
            Assert.That(acceptedEvent.Message!.Method, Is.EqualTo(SipMethod.Prack));
            Assert.That(ok.Message!.StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public void UnexpectedPrackBodyGets488UntilEmptyPrackArrives()
    {
        SipInviteSession session = StartedIncoming(OfferBody(), true);
        DrainEvents(session);
        session.StartInvite(IncomingCommand(
            IncomingResponse(183, AnswerBody(), false)));
        DrainEvents(session);

        SipInviteEvent rejected = session.ReceiveInviteMessage(
            IncomingPrackWithKey(1, 78, "z9hG4bK-body-prack", 77,
                SipMethods.Invite, OfferBody()));
        SipInviteEvent rejectedEvent = session.PollInviteEvent();
        SipInviteEvent notAcceptable = session.PollInviteEvent();
        SipInviteEvent accepted = session.ReceiveInviteMessage(
            IncomingPrackWithKey(1, 79, "z9hG4bK-empty-prack", 77,
                SipMethods.Invite, null));
        SipInviteEvent acceptedEvent = session.PollInviteEvent();
        SipInviteEvent ok = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Reason, Does.Contain("required offer answer"));
            Assert.That(rejectedEvent.Message!.Body, Is.EqualTo(OfferBody()));
            Assert.That(notAcceptable.Message!.StatusCode, Is.EqualTo(488));
            Assert.That(accepted.Reason, Does.Contain("acknowledged"));
            Assert.That(acceptedEvent.Message!.Body, Is.Empty);
            Assert.That(ok.Message!.StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public void BadRackGets481AndPendingReliableResponseStillTimesOut()
    {
        SipInviteSession session = StartedIncoming(OfferBody(), true);
        DrainEvents(session);
        session.StartInvite(IncomingCommand(
            IncomingResponse(180, null, false)));
        DrainEvents(session);

        SipInviteEvent invalid = session.ReceiveInviteMessage(
            IncomingPrack(2, null));
        SipInviteEvent prack = session.PollInviteEvent();
        SipInviteEvent missing = session.PollInviteEvent();
        session.AdvanceInviteTime(32000);
        SipInviteEvent timeout = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Reason, Does.Contain("did not match"));
            Assert.That(prack.Message!.Method, Is.EqualTo(SipMethod.Prack));
            Assert.That(missing.Message!.StatusCode, Is.EqualTo(481));
            Assert.That(timeout.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(timeout.Message!.StatusCode, Is.EqualTo(504));
            Assert.That(timeout.Reason, Does.Contain("timed out"));
        });
    }

    [Test]
    public void PrackFailureStartsCancellationAndUnnegotiatedReliabilityIsRejected()
    {
        SipInviteSession session = StartedOutgoing(OfferBody(), true);
        DrainEvents(session);
        session.ReceiveInviteMessage(
            OutgoingReliableResponse(183, 1, AnswerBody()));
        SipInviteEvent provisional = session.PollInviteEvent();
        SipInviteEvent prack = session.PollInviteEvent();
        SipInviteEvent failed = session.ReceiveInviteMessage(
            ResponseForRequest(prack.Message!, 481,
                "Call/Transaction Does Not Exist"));
        SipInviteEvent failureEvent = session.PollInviteEvent();
        SipInviteEvent cancel = session.PollInviteEvent();

        SipInviteSession unsupported = StartedOutgoing(OfferBody(), false);
        DrainEvents(unsupported);

        Assert.Multiple(() =>
        {
            Assert.That(provisional.Message!.StatusCode, Is.EqualTo(183));
            Assert.That(failed.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(failureEvent.Message!.StatusCode, Is.EqualTo(481));
            Assert.That(cancel.Message!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.Throws<SipProtocolException>(() =>
                unsupported.ReceiveInviteMessage(
                    OutgoingReliableResponse(183, 1, AnswerBody())));
        });
    }

    [Test]
    public void ReliableTimerRejectsBackwardTimeAndFinalBeforePrack()
    {
        SipInviteSession session = StartedIncoming(OfferBody(), true);
        DrainEvents(session);
        session.StartInvite(IncomingCommand(
            IncomingResponse(180, null, false)));

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => session.StartInvite(
                IncomingCommand(IncomingResponse(200, AnswerBody(), false))));
            Assert.DoesNotThrow(() => session.AdvanceInviteTime(100));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                session.AdvanceInviteTime(99));
        });
    }

    static SipInviteSession StartedOutgoing(byte[] body, bool reliable)
    {
        SipInviteSession session = new SipInviteSession();
        session.StartInvite(OutgoingRequest(body, reliable));
        return session;
    }

    static SipInviteRequest OutgoingRequest(byte[] body, bool reliable)
    {
        SipInviteRequest request = new SipInviteRequest();
        request.Options.ReliableProvisional = reliable;
        request.Message = Parse(
            "INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-rel-out\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=out-local\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: reliable-out@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + BodyHeaders(body) + System.Text.Encoding.ASCII.GetString(body));
        return request;
    }

    static SipMessage OutgoingReliableResponse(
        int status, int rseq, byte[] body)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " Session Progress\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-rel-out\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=out-local\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=out-remote\r\n"
            + "Call-ID: reliable-out@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:bob@callee-dialog.example.com>\r\n"
            + "Require: 100rel\r\n"
            + "RSeq: " + rseq.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n"
            + BodyHeaders(body) + System.Text.Encoding.ASCII.GetString(body));
    }

    static SipMessage ResponseForRequest(
        SipMessage request, int status, string reason)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + reason + "\r\n"
            + "Via: SIP/2.0/" + request.ViaValues[0].Transport + " "
            + request.ViaValues[0].Host + ";branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=out-local\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=out-remote\r\n"
            + "Call-ID: reliable-out@example.com\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " PRACK\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipInviteSession StartedIncoming(byte[] body, bool require)
    {
        SipInviteSession session = new SipInviteSession();
        session.ReceiveInviteMessage(IncomingInvite(body, require));
        return session;
    }

    static SipMessage IncomingInvite(byte[] body, bool require)
    {
        return Parse("INVITE sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-rel-in\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=in-remote\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: reliable-in@example.com\r\n"
            + "CSeq: 77 INVITE\r\n"
            + "Contact: <sip:alice@caller.example.com>\r\n"
            + (require ? "Require: 100rel\r\n" : "Supported: 100rel\r\n")
            + BodyHeaders(body) + System.Text.Encoding.ASCII.GetString(body));
    }

    static SipInviteRequest IncomingCommand(SipMessage response)
    {
        SipInviteRequest request = new SipInviteRequest();
        request.Message = response;
        return request;
    }

    static SipMessage IncomingResponse(int status, byte[]? body, bool reliable)
    {
        byte[] actualBody = body ?? new byte[0];
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Progress\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=z9hG4bK-rel-in\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=in-remote\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=in-local\r\n"
            + "Call-ID: reliable-in@example.com\r\n"
            + "CSeq: 77 INVITE\r\n"
            + (status > 100 && status < 300
                ? "Contact: <sip:bob@server.example.com>\r\n" : "")
            + (reliable ? "Require: 100rel\r\nRSeq: 1\r\n" : "")
            + BodyHeaders(actualBody)
            + System.Text.Encoding.ASCII.GetString(actualBody));
    }

    static SipMessage IncomingPrack(int rseq, byte[]? body)
    {
        return IncomingPrackWithKey(rseq, 78, "z9hG4bK-prack-in", 77,
            SipMethods.Invite, body);
    }

    static SipMessage IncomingPrackWithKey(
        int rseq, int cseq, string branch, int inviteCseq,
        string rackMethod, byte[]? body)
    {
        byte[] actualBody = body ?? new byte[0];
        return Parse("PRACK sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=in-remote\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=in-local\r\n"
            + "Call-ID: reliable-in@example.com\r\n"
            + "CSeq: " + cseq.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " PRACK\r\n"
            + "RAck: " + rseq.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + inviteCseq.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + rackMethod + "\r\n" + BodyHeaders(actualBody)
            + System.Text.Encoding.ASCII.GetString(actualBody));
    }

    static string HeaderValue(SipMessage message, string name)
    {
        return message.Headers.Single(header => string.Equals(
            header.Name, name, StringComparison.OrdinalIgnoreCase)).Value!;
    }

    static string BodyHeaders(byte[] body)
    {
        return (body.Length == 0 ? "" : "Content-Type: application/sdp\r\n")
            + "Content-Length: " + body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n\r\n";
    }

    static SipMediaType SdpType()
    {
        SipMediaType type = new SipMediaType();
        type.Type = "application";
        type.Subtype = "sdp";
        return type;
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
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }
}
