namespace sip.Tests;

public class SipCancelTransactionTests
{
    [Test]
    public void CancelReusesInviteIdentityTopViaAndRoutesButRemovesForbiddenFields()
    {
        SipInviteClientTransaction transaction = Client(Invite("z9hG4bK-cancel-shape", true),
            false, new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(180, "Ringing", "z9hG4bK-cancel-shape", "ringing", "INVITE"));

        SipInviteClientResult cancelled = transaction.RequestCancel();
        SipMessage cancel = cancelled.OutboundMessages.Single();
        SipWriteResult written = new SipMessageCodec().SerializeMessage(cancel,
            new SipWriteOptions());
        SipParseResult reparsed = new SipMessageCodec().ParseDatagramMessage(written.Bytes,
            new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.CancelSent, Is.True);
            Assert.That(cancel.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancel.MethodToken, Is.EqualTo(SipMethods.Cancel));
            Assert.That(cancel.RequestUri!.Host, Is.EqualTo("callee.example.com"));
            Assert.That(cancel.ViaValues.Length, Is.EqualTo(1));
            Assert.That(cancel.ViaValues[0].Branch, Is.EqualTo("z9hG4bK-cancel-shape"));
            Assert.That(cancel.ViaValues[0].Host, Is.EqualTo("client.example.com"));
            Assert.That(Tag(cancel.From), Is.EqualTo("caller-tag"));
            Assert.That(Tag(cancel.To), Is.Null);
            Assert.That(cancel.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(cancel.CSeq.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancel.RouteValues.Length, Is.EqualTo(2));
            Assert.That(cancel.ContactValues, Is.Empty);
            Assert.That(cancel.Body, Is.Empty);
            Assert.That(HeaderValues(cancel, SipHeaderNames.Require), Is.Empty);
            Assert.That(HeaderValues(cancel, SipHeaderNames.ProxyRequire), Is.Empty);
            Assert.That(HeaderValues(cancel, SipHeaderNames.ContentLength),
                Is.EqualTo(new string[] { "0" }));
            Assert.That(written.Success, Is.True, written.ErrorMessage);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete),
                reparsed.ErrorMessage);
        });
    }

    [Test]
    public void CancelRequestedBeforeResponseWaitsAndFirstProvisionalSendsIt()
    {
        SipInviteClientTransaction transaction = Client(
            Invite("z9hG4bK-wait-to-cancel", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipInviteClientResult pending = transaction.RequestCancel();
        SipInviteClientResult repeatedPending = transaction.RequestCancel();
        SipInviteClientResult provisional = transaction.ReceiveResponse(
            Response(100, "Trying", "z9hG4bK-wait-to-cancel", "server", "INVITE"));
        SipInviteClientResult repeatedCancel = transaction.RequestCancel();
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(pending.CancellationPending, Is.True);
            Assert.That(pending.OutboundMessages, Is.Empty);
            Assert.That(repeatedPending.CancellationPending, Is.True);
            Assert.That(provisional.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(provisional.CancelSent, Is.True);
            Assert.That(provisional.OutboundMessages.Single().Method,
                Is.EqualTo(SipMethod.Cancel));
            Assert.That(repeatedCancel.OutboundMessages, Is.Empty);
            Assert.That(snapshot.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerB }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 6400 }));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DuplicateProvisionalAfterCancelDoesNotSendAnotherCancel(bool reliable)
    {
        SipInviteClientTransaction transaction = Client(
            Invite("z9hG4bK-duplicate-provisional", false), reliable,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(180, "Ringing", "z9hG4bK-duplicate-provisional", "ringing", "INVITE"));
        SipInviteClientResult cancelled = transaction.RequestCancel();

        SipInviteClientResult repeated = transaction.ReceiveResponse(
            Response(183, "Session Progress", "z9hG4bK-duplicate-provisional", "ringing",
                "INVITE"));
        SipInviteClientResult requestedAgain = transaction.RequestCancel();

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.CancelSent, Is.True);
            Assert.That(cancelled.OutboundMessages.Single().Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(repeated.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(repeated.DeliveredResponses.Single().StatusCode, Is.EqualTo(183));
            Assert.That(repeated.CancelSent, Is.False);
            Assert.That(repeated.OutboundMessages, Is.Empty);
            Assert.That(requestedAgain.CancelSent, Is.False);
            Assert.That(requestedAgain.OutboundMessages, Is.Empty);
        });
    }

    [TestCase(200)]
    [TestCase(487)]
    public void FinalResponseWinningBeforeProvisionalSuppressesPendingCancel(int status)
    {
        SipInviteClientTransaction transaction = Client(
            Invite("z9hG4bK-final-wins", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.RequestCancel();

        SipInviteClientResult final = transaction.ReceiveResponse(
            Response(status, "Final", "z9hG4bK-final-wins", "final", "INVITE"));

        Assert.Multiple(() =>
        {
            Assert.That(final.CancelSuppressed, Is.True);
            Assert.That(final.DeliveredResponses.Single().StatusCode, Is.EqualTo(status));
            Assert.That(final.OutboundMessages.Any(message => message.Method == SipMethod.Cancel),
                Is.False);
        });
    }

    [Test]
    public void CancelRequestAfterFinalResponseIsSuppressed()
    {
        SipInviteClientTransaction transaction = Client(
            Invite("z9hG4bK-too-late", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-too-late", "accepted", "INVITE"));

        SipInviteClientResult result = transaction.RequestCancel();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(result.CancelSuppressed, Is.True);
            Assert.That(result.OutboundMessages, Is.Empty);
        });
    }

    [Test]
    public void SentCancelAddsSixtyFourT1DeadlineForOriginalInvite()
    {
        SipInviteClientTransaction transaction = Client(
            Invite("z9hG4bK-cancel-timeout", false), false,
            new SipManualMonotonicClock(20));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(180, "Ringing", "z9hG4bK-cancel-timeout", "ringing", "INVITE"));
        transaction.RequestCancel();

        SipInviteClientResult before = transaction.AdvanceTime(6419);
        SipInviteClientResult expired = transaction.AdvanceTime(6420);

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.TimedOut, Is.True);
            Assert.That(expired.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerB }));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void MatchingCancelProducesOkAndTerminatesProceedingInviteWithSharedTag()
    {
        SipInviteServerTransaction transaction = Server(
            Invite("z9hG4bK-server-cancel", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(180, "Ringing", "z9hG4bK-server-cancel", "ringing", "INVITE"));
        SipMessage cancel = Cancel("z9hG4bK-server-cancel");

        SipInviteServerResult result = transaction.ReceiveCancel(cancel);
        SipMessage ok = result.OutboundMessages[0];
        SipMessage terminated = result.OutboundMessages[1];
        SipWriteResult writtenOk = new SipMessageCodec().SerializeMessage(ok,
            new SipWriteOptions());
        SipWriteResult writtenTerminated = new SipMessageCodec().SerializeMessage(terminated,
            new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(result.CancelMatched, Is.True);
            Assert.That(result.CancellationRequested, Is.True);
            Assert.That(result.InviteTerminated, Is.True);
            Assert.That(result.DeliveredRequests.Single().Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(ok.StatusCode, Is.EqualTo(200));
            Assert.That(ok.CSeq!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(terminated.StatusCode, Is.EqualTo(487));
            Assert.That(terminated.CSeq!.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(Tag(ok.To), Is.EqualTo("ringing"));
            Assert.That(Tag(terminated.To), Is.EqualTo("ringing"));
            Assert.That(writtenOk.Success, Is.True, writtenOk.ErrorMessage);
            Assert.That(writtenTerminated.Success, Is.True, writtenTerminated.ErrorMessage);
            Assert.That(transaction.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerG, SipTimerNames.TimerH }));
        });
    }

    [Test]
    public void CancelWithoutExistingToTagCreatesOneForBothResponses()
    {
        SipInviteServerTransaction transaction = Server(
            Invite("z9hG4bK-new-tag", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipInviteServerResult result = transaction.ReceiveCancel(Cancel("z9hG4bK-new-tag"));
        string? cancelTag = Tag(result.OutboundMessages[0].To);
        string? inviteTag = Tag(result.OutboundMessages[1].To);

        Assert.Multiple(() =>
        {
            Assert.That(cancelTag, Is.Not.Null.And.Not.Empty);
            Assert.That(inviteTag, Is.EqualTo(cancelTag));
        });
    }

    [Test]
    public void DuplicateCancelReplaysOnlyOkWithoutRenotifyingTransactionUser()
    {
        SipInviteServerTransaction transaction = Server(
            Invite("z9hG4bK-duplicate-cancel", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        SipMessage cancel = Cancel("z9hG4bK-duplicate-cancel");
        transaction.ReceiveCancel(cancel);

        SipInviteServerResult duplicate = transaction.ReceiveCancel(cancel);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(duplicate.CancelMatched, Is.True);
            Assert.That(duplicate.RequestAbsorbed, Is.True);
            Assert.That(duplicate.Retransmitted, Is.True);
            Assert.That(duplicate.CancellationRequested, Is.False);
            Assert.That(duplicate.InviteTerminated, Is.False);
            Assert.That(duplicate.OutboundMessages.Single().StatusCode, Is.EqualTo(200));
            Assert.That(duplicate.OutboundMessages.Single().CSeq!.Method,
                Is.EqualTo(SipMethod.Cancel));
            Assert.That(duplicate.DeliveredRequests, Is.Empty);
            Assert.That(transaction.GetSnapshot().TimerDeadlines,
                Is.EqualTo(new long[] { 100, 6400 }));
        });
    }

    [TestCase(200)]
    [TestCase(486)]
    public void CancelLosingRaceToFinalResponseGetsOkWithoutChangingInvite(int status)
    {
        SipInviteServerTransaction transaction = Server(
            Invite("z9hG4bK-race", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(status, "Final", "z9hG4bK-race", "final-tag", "INVITE"));

        SipInviteServerResult cancel = transaction.ReceiveCancel(Cancel("z9hG4bK-race"));

        Assert.Multiple(() =>
        {
            Assert.That(cancel.CancelMatched, Is.True);
            Assert.That(cancel.CancellationRequested, Is.False);
            Assert.That(cancel.InviteTerminated, Is.False);
            Assert.That(cancel.OutboundMessages.Single().StatusCode, Is.EqualTo(200));
            Assert.That(cancel.OutboundMessages.Single().CSeq!.Method,
                Is.EqualTo(SipMethod.Cancel));
            Assert.That(Tag(cancel.OutboundMessages.Single().To), Is.EqualTo("final-tag"));
            Assert.That(transaction.GetSnapshot().Response!.StatusCode, Is.EqualTo(status));
            Assert.That(transaction.GetSnapshot().State, Is.EqualTo(status < 300
                ? SipTransactionState.Accepted : SipTransactionState.Completed));
        });
    }

    [Test]
    public void CancelAndInviteTransactionsCompleteIndependentlyWithFailureAck()
    {
        SipMessage invite = Invite("z9hG4bK-coordinated", false);
        SipInviteClientTransaction inviteClient = Client(invite, false,
            new SipManualMonotonicClock(0));
        SipInviteServerTransaction inviteServer = Server(invite, false,
            new SipManualMonotonicClock(0));
        inviteClient.StartTransaction();
        SipInviteServerResult serverStarted = inviteServer.StartTransaction();
        inviteClient.ReceiveResponse(serverStarted.OutboundMessages.Single());

        SipMessage cancel = inviteClient.RequestCancel().OutboundMessages.Single();
        SipNonInviteClientTransaction cancelClient = new SipNonInviteClientTransaction(
            "cancel-transaction", cancel, Options(false), new SipManualMonotonicClock(0));
        cancelClient.StartTransaction();
        SipInviteServerResult serverCancelled = inviteServer.ReceiveCancel(cancel);

        SipNonInviteClientResult cancelCompleted = cancelClient.ReceiveResponse(
            serverCancelled.OutboundMessages[0]);
        SipInviteClientResult inviteCompleted = inviteClient.ReceiveResponse(
            serverCancelled.OutboundMessages[1]);
        SipMessage ack = inviteCompleted.OutboundMessages.Single();
        SipInviteServerResult serverAcknowledged = inviteServer.ReceiveRequest(ack);

        Assert.Multiple(() =>
        {
            Assert.That(cancelCompleted.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(cancelCompleted.DeliveredResponses.Single().StatusCode, Is.EqualTo(200));
            Assert.That(inviteCompleted.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(inviteCompleted.DeliveredResponses.Single().StatusCode, Is.EqualTo(487));
            Assert.That(ack.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ack.ViaValues.Single().Branch, Is.EqualTo("z9hG4bK-coordinated"));
            Assert.That(ack.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(serverAcknowledged.State, Is.EqualTo(SipTransactionState.Confirmed));
            Assert.That(serverAcknowledged.AckAbsorbed, Is.True);
        });
    }

    [Test]
    public void LostCancelResponseInviteFinalAndAckRecoverThroughRetransmissions()
    {
        SipMessage invite = Invite("z9hG4bK-loss-race", false);
        SipInviteClientTransaction inviteClient = Client(invite, false,
            new SipManualMonotonicClock(0));
        SipInviteServerTransaction inviteServer = Server(invite, false,
            new SipManualMonotonicClock(0));
        inviteClient.StartTransaction();
        SipInviteServerResult serverStarted = inviteServer.StartTransaction();
        inviteClient.ReceiveResponse(serverStarted.OutboundMessages.Single());

        SipMessage cancel = inviteClient.RequestCancel().OutboundMessages.Single();
        SipNonInviteClientTransaction cancelClient = new SipNonInviteClientTransaction(
            "cancel-loss-transaction", cancel, Options(false),
            new SipManualMonotonicClock(0));
        cancelClient.StartTransaction();
        inviteServer.ReceiveCancel(cancel);

        SipNonInviteClientResult cancelRetransmission = cancelClient.AdvanceTime(100);
        SipInviteServerResult duplicateCancel = inviteServer.ReceiveCancel(
            cancelRetransmission.OutboundMessages.Single());
        SipNonInviteClientResult cancelCompleted = cancelClient.ReceiveResponse(
            duplicateCancel.OutboundMessages.Single());

        SipInviteServerResult firstFinalRetransmission = inviteServer.AdvanceTime(100);
        SipInviteClientResult inviteCompleted = inviteClient.ReceiveResponse(
            firstFinalRetransmission.OutboundMessages.Single());
        SipInviteServerResult secondFinalRetransmission = inviteServer.AdvanceTime(300);
        SipInviteClientResult repeatedFinal = inviteClient.ReceiveResponse(
            secondFinalRetransmission.OutboundMessages.Single());
        SipMessage recoveredAck = repeatedFinal.OutboundMessages.Single();
        SipInviteServerResult acknowledged = inviteServer.ReceiveRequest(recoveredAck);
        SipInviteServerResult duplicateAck = inviteServer.ReceiveRequest(recoveredAck);
        SipInviteServerResult timerIExpired = inviteServer.AdvanceTime(800);
        SipInviteServerResult lateAck = inviteServer.ReceiveRequest(recoveredAck);

        Assert.Multiple(() =>
        {
            Assert.That(cancelRetransmission.Retransmitted, Is.True);
            Assert.That(cancelRetransmission.OutboundMessages.Single().Method,
                Is.EqualTo(SipMethod.Cancel));
            Assert.That(duplicateCancel.RequestAbsorbed, Is.True);
            Assert.That(duplicateCancel.Retransmitted, Is.True);
            Assert.That(duplicateCancel.DeliveredRequests, Is.Empty);
            Assert.That(duplicateCancel.OutboundMessages.Single().StatusCode, Is.EqualTo(200));
            Assert.That(cancelCompleted.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(firstFinalRetransmission.Retransmitted, Is.True);
            Assert.That(firstFinalRetransmission.OutboundMessages.Single().StatusCode,
                Is.EqualTo(487));
            Assert.That(inviteCompleted.DeliveredResponses.Single().StatusCode,
                Is.EqualTo(487));
            Assert.That(inviteCompleted.OutboundMessages.Single().Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(secondFinalRetransmission.Retransmitted, Is.True);
            Assert.That(repeatedFinal.DeliveredResponses, Is.Empty);
            Assert.That(repeatedFinal.Retransmitted, Is.True);
            Assert.That(recoveredAck.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(acknowledged.State, Is.EqualTo(SipTransactionState.Confirmed));
            Assert.That(acknowledged.AckAbsorbed, Is.True);
            Assert.That(duplicateAck.State, Is.EqualTo(SipTransactionState.Confirmed));
            Assert.That(duplicateAck.AckAbsorbed, Is.True);
            Assert.That(timerIExpired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(timerIExpired.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerI }));
            Assert.That(lateAck.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(lateAck.AckAbsorbed, Is.True);
            Assert.That(lateAck.DeliveredRequests, Is.Empty);
        });
    }

    [Test]
    public void SuccessWinningAfterCancelUsesEndToEndAckPath()
    {
        SipInviteClientTransaction transaction = Client(
            Invite("z9hG4bK-success-race", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(180, "Ringing", "z9hG4bK-success-race", "ringing", "INVITE"));
        transaction.RequestCancel();

        SipInviteClientResult accepted = transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-success-race", "success", "INVITE"));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(accepted.DeliveredResponses.Single().StatusCode, Is.EqualTo(200));
            Assert.That(accepted.OutboundMessages, Is.Empty);
            Assert.That(transaction.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerM }));
        });
    }

    [Test]
    public void MismatchedAndMalformedCancelsAreRejected()
    {
        SipInviteServerTransaction transaction = Server(
            Invite("z9hG4bK-valid-cancel", false), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        SipMessage malformed = Cancel("z9hG4bK-valid-cancel");
        malformed.Headers = new SipHeader[0];

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => transaction.ReceiveCancel(
                Cancel("z9hG4bK-other-cancel")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveCancel(malformed));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveCancel(
                Request("OPTIONS", "z9hG4bK-valid-cancel")));
        });
    }

    [Test]
    public void ResponsesAndAckDeliveredToWrongRaceTransactionAreRejected()
    {
        SipMessage invite = Invite("z9hG4bK-unmatched-race", false);
        SipInviteClientTransaction inviteClient = Client(invite, false,
            new SipManualMonotonicClock(0));
        SipInviteServerTransaction inviteServer = Server(invite, false,
            new SipManualMonotonicClock(0));
        inviteClient.StartTransaction();
        SipInviteServerResult started = inviteServer.StartTransaction();
        inviteClient.ReceiveResponse(started.OutboundMessages.Single());
        SipMessage cancel = inviteClient.RequestCancel().OutboundMessages.Single();
        SipNonInviteClientTransaction cancelClient = new SipNonInviteClientTransaction(
            "cancel-unmatched-transaction", cancel, Options(false),
            new SipManualMonotonicClock(0));
        cancelClient.StartTransaction();
        SipInviteServerResult cancelled = inviteServer.ReceiveCancel(cancel);
        SipMessage cancelOk = cancelled.OutboundMessages[0];
        SipMessage inviteTerminated = cancelled.OutboundMessages[1];

        Assert.Throws<ArgumentException>(() => inviteClient.ReceiveResponse(cancelOk));
        Assert.Throws<ArgumentException>(() => cancelClient.ReceiveResponse(inviteTerminated));

        SipInviteClientResult completed = inviteClient.ReceiveResponse(inviteTerminated);
        SipMessage wrongAck = completed.OutboundMessages.Single();
        wrongAck.ViaValues[0].Branch = "z9hG4bK-wrong-ack";

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => inviteServer.ReceiveRequest(wrongAck));
            Assert.That(inviteClient.GetSnapshot().State,
                Is.EqualTo(SipTransactionState.Completed));
            Assert.That(cancelClient.GetSnapshot().State,
                Is.EqualTo(SipTransactionState.Trying));
            Assert.That(inviteServer.GetSnapshot().State,
                Is.EqualTo(SipTransactionState.Completed));
            Assert.That(inviteServer.GetSnapshot().Response!.StatusCode, Is.EqualTo(487));
        });
    }

    static SipInviteClientTransaction Client(SipMessage invite, bool reliable,
        SipManualMonotonicClock clock)
    {
        return new SipInviteClientTransaction("client-transaction", invite, Options(reliable),
            clock);
    }

    static SipInviteServerTransaction Server(SipMessage invite, bool reliable,
        SipManualMonotonicClock clock)
    {
        return new SipInviteServerTransaction("server-transaction", invite, Options(reliable),
            clock);
    }

    static SipTransactionOptions Options(bool reliable)
    {
        SipTransactionOptions options = new SipTransactionOptions();
        options.ReliableTransport = reliable;
        options.Timers.T1Milliseconds = 100;
        options.Timers.T2Milliseconds = 400;
        options.Timers.T4Milliseconds = 500;
        options.Timers.MaxTimerCount = 8;
        return options;
    }

    static SipMessage Invite(string branch, bool routes)
    {
        string routeHeaders = routes
            ? "Route: <sip:first.example.com;lr>\r\nRoute: <sip:second.example.com;lr>\r\n"
            : "";
        return Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + ";rport\r\n"
            + "Via: SIP/2.0/TCP previous.example.com:5070;branch=z9hG4bK-previous\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n" + routeHeaders
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Require: timer\r\nProxy-Require: example-option\r\n"
            + "Content-Type: application/sdp\r\nContent-Length: 3\r\n\r\nv=0");
    }

    static SipMessage Cancel(string branch)
    {
        return Parse("CANCEL sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + ";rport\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 CANCEL\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Request(string method, string branch)
    {
        return Parse(method + " sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + ";rport\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 " + method + "\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string reason, string branch, string toTag,
        string method)
    {
        string contact = method == "INVITE" && status >= 200 && status < 300
            ? "Contact: <sip:bob@server.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + status.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + ";rport\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 " + method + "\r\n" + contact
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }

    static string? Tag(SipNameAddress? address)
    {
        if (address == null)
        {
            return null;
        }
        return address.Parameters.SingleOrDefault(parameter =>
            string.Equals(parameter.Name, "tag", StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static string[] HeaderValues(SipMessage message, string name)
    {
        return message.Headers.Where(header =>
            string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))
            .Select(header => header.Value ?? "").ToArray();
    }
}
