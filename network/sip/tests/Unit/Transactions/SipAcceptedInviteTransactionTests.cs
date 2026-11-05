namespace sip.Tests;

public class SipAcceptedInviteTransactionTests
{
    [Test]
    public void ClientFirstSuccessFromCallingEntersAcceptedAndStartsTimerM()
    {
        SipInviteClientTransaction transaction = Client("z9hG4bK-client-first", false,
            new SipManualMonotonicClock(25));
        transaction.StartTransaction();

        SipInviteClientResult accepted = transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-first", "dialog-one"));
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(accepted.StateChanged, Is.True);
            Assert.That(accepted.DeliveredResponses.Single().StatusCode, Is.EqualTo(200));
            Assert.That(accepted.OutboundMessages, Is.Empty);
            Assert.That(snapshot.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerM }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 6425 }));
        });
    }

    [Test]
    public void ClientAcceptedDeliversEverySuccessWithoutRestartingTimerM()
    {
        SipInviteClientTransaction transaction = Client("z9hG4bK-client-repeat", false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-repeat", "dialog-one"));
        transaction.AdvanceTime(1000);

        SipInviteClientResult additional = transaction.ReceiveResponse(
            Response(202, "Accepted", "z9hG4bK-client-repeat", "dialog-two"));
        additional.DeliveredResponses[0].To!.Parameters[0].Value = "changed";
        SipInviteClientResult retransmitted = transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-repeat", "dialog-one"));
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(additional.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(additional.StateChanged, Is.False);
            Assert.That(additional.OutboundMessages, Is.Empty);
            Assert.That(retransmitted.DeliveredResponses.Single().StatusCode, Is.EqualTo(200));
            Assert.That(retransmitted.OutboundMessages, Is.Empty);
            Assert.That(Tag(snapshot.Response!.To), Is.EqualTo("dialog-one"));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 6400 }));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ClientForkedAndRepeatedSuccessesDoNotRestartTimerM(bool reliable)
    {
        SipInviteClientTransaction transaction = Client("z9hG4bK-client-timer-m", reliable,
            new SipManualMonotonicClock(50));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(180, "Ringing", "z9hG4bK-client-timer-m", "early"));
        SipInviteClientResult first = transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-timer-m", "dialog-one"));

        SipInviteClientResult before = transaction.AdvanceTime(6449);
        SipInviteClientResult forked = transaction.ReceiveResponse(
            Response(202, "Accepted", "z9hG4bK-client-timer-m", "dialog-two"));
        SipInviteClientResult repeated = transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-timer-m", "dialog-one"));
        SipTransactionSnapshot accepted = transaction.GetSnapshot();
        SipInviteClientResult expired = transaction.AdvanceTime(6450);

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(first.DeliveredResponses.Single().StatusCode, Is.EqualTo(200));
            Assert.That(first.OutboundMessages, Is.Empty);
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(forked.StateChanged, Is.False);
            Assert.That(forked.DeliveredResponses.Single().StatusCode, Is.EqualTo(202));
            Assert.That(forked.OutboundMessages, Is.Empty);
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(Tag(repeated.DeliveredResponses.Single().To), Is.EqualTo("dialog-one"));
            Assert.That(repeated.OutboundMessages, Is.Empty);
            Assert.That(accepted.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerM }));
            Assert.That(accepted.TimerDeadlines, Is.EqualTo(new long[] { 6450 }));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerM }));
        });
    }

    [Test]
    public void ClientAcceptedIgnoresNonSuccessResponses()
    {
        SipInviteClientTransaction transaction = Client("z9hG4bK-client-ignore", false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-ignore", "dialog-one"));

        SipInviteClientResult provisional = transaction.ReceiveResponse(
            Response(180, "Ringing", "z9hG4bK-client-ignore", "early"));
        SipInviteClientResult failure = transaction.ReceiveResponse(
            Response(486, "Busy Here", "z9hG4bK-client-ignore", "failed"));

        Assert.Multiple(() =>
        {
            Assert.That(provisional.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(provisional.DeliveredResponses, Is.Empty);
            Assert.That(failure.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(failure.DeliveredResponses, Is.Empty);
            Assert.That(failure.OutboundMessages, Is.Empty);
            Assert.That(transaction.GetSnapshot().TimerDeadlines,
                Is.EqualTo(new long[] { 6400 }));
        });
    }

    [Test]
    public void TimerMTerminatesClientAtSixtyFourT1ForEveryTransport()
    {
        SipInviteClientTransaction unreliable = Client("z9hG4bK-client-m-one", false,
            new SipManualMonotonicClock(10));
        unreliable.StartTransaction();
        unreliable.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-m-one", "one"));

        SipInviteClientTransaction reliable = Client("z9hG4bK-client-m-two", true,
            new SipManualMonotonicClock(10));
        reliable.StartTransaction();
        reliable.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-m-two", "two"));

        SipInviteClientResult before = unreliable.AdvanceTime(6409);
        SipInviteClientResult firstExpired = unreliable.AdvanceTime(6410);
        SipInviteClientResult secondExpired = reliable.AdvanceTime(6410);

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(firstExpired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(firstExpired.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerM }));
            Assert.That(secondExpired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(secondExpired.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerM }));
            Assert.That(unreliable.GetSnapshot().TimerNames, Is.Empty);
            Assert.That(reliable.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void ClientTransportErrorCanTerminateAcceptedState()
    {
        SipInviteClientTransaction transaction = Client("z9hG4bK-client-error", false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-client-error", "accepted"));

        SipInviteClientResult failed = transaction.HandleTransportError("connection failed");

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(failed.StateChanged, Is.True);
            Assert.That(failed.TransportFailed, Is.True);
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void ServerSuccessEntersAcceptedAndStartsTimerLForEveryTransport()
    {
        SipInviteServerTransaction unreliable = Server("z9hG4bK-server-l-one", false,
            new SipManualMonotonicClock(30));
        unreliable.StartTransaction();
        SipInviteServerResult first = unreliable.SendResponse(
            Response(200, "OK", "z9hG4bK-server-l-one", "one"));

        SipInviteServerTransaction reliable = Server("z9hG4bK-server-l-two", true,
            new SipManualMonotonicClock(30));
        reliable.StartTransaction();
        SipInviteServerResult second = reliable.SendResponse(
            Response(200, "OK", "z9hG4bK-server-l-two", "two"));

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(first.OutboundMessages.Single().StatusCode, Is.EqualTo(200));
            Assert.That(second.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(unreliable.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerL }));
            Assert.That(unreliable.GetSnapshot().TimerDeadlines,
                Is.EqualTo(new long[] { 6430 }));
            Assert.That(reliable.GetSnapshot().TimerDeadlines,
                Is.EqualTo(new long[] { 6430 }));
        });
    }

    [Test]
    public void AcceptedInviteRetransmissionIsAbsorbedWithoutReplayingSuccess()
    {
        SipInviteServerTransaction transaction = Server("z9hG4bK-server-absorb", false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(200, "OK", "z9hG4bK-server-absorb", "accepted"));

        SipInviteServerResult repeated = transaction.ReceiveRequest(
            Invite("z9hG4bK-server-absorb"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(repeated.RequestAbsorbed, Is.True);
            Assert.That(repeated.Retransmitted, Is.False);
            Assert.That(repeated.OutboundMessages, Is.Empty);
            Assert.That(repeated.DeliveredRequests, Is.Empty);
        });
    }

    [Test]
    public void ServerPassesTuSuccessRetransmissionsWithoutRestartingTimerL()
    {
        SipInviteServerTransaction transaction = Server("z9hG4bK-server-repeat", false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(200, "OK", "z9hG4bK-server-repeat", "dialog-one"));
        transaction.AdvanceTime(1000);

        SipInviteServerResult repeated = transaction.SendResponse(
            Response(202, "Accepted", "z9hG4bK-server-repeat", "dialog-two"));
        SipInviteServerResult discarded = transaction.SendResponse(
            Response(486, "Busy Here", "z9hG4bK-server-repeat", "failure"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(repeated.OutboundMessages.Single().StatusCode, Is.EqualTo(202));
            Assert.That(discarded.ResponseDiscarded, Is.True);
            Assert.That(discarded.OutboundMessages, Is.Empty);
            Assert.That(transaction.GetSnapshot().Response!.StatusCode, Is.EqualTo(202));
            Assert.That(transaction.GetSnapshot().TimerDeadlines,
                Is.EqualTo(new long[] { 6400 }));
        });
    }

    [Test]
    public void EndToEndAckWithNewBranchIsDeliveredAndAcceptedStateIsRetained()
    {
        SipInviteServerTransaction transaction = Server("z9hG4bK-server-ack", false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(200, "OK", "z9hG4bK-server-ack", "dialog-one"));

        SipInviteServerResult acknowledged = transaction.ReceiveRequest(
            Ack("z9hG4bK-new-ack-branch", "dialog-one", 42, "invite-call@example.com",
                "caller-tag"));
        acknowledged.DeliveredRequests[0].To!.Parameters[0].Value = "changed";
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(acknowledged.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(acknowledged.StateChanged, Is.False);
            Assert.That(acknowledged.AckDelivered, Is.True);
            Assert.That(acknowledged.AckAbsorbed, Is.False);
            Assert.That(acknowledged.DeliveredRequests.Single().Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(acknowledged.OutboundMessages, Is.Empty);
            Assert.That(Tag(snapshot.Response!.To), Is.EqualTo("dialog-one"));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 6400 }));
        });
    }

    [Test]
    public void AcceptedAckCanMatchAnySuccessTagButRejectsWrongDialogIdentity()
    {
        SipInviteServerTransaction transaction = Server("z9hG4bK-server-dialogs", false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(200, "OK", "z9hG4bK-server-dialogs", "dialog-one"));
        transaction.SendResponse(
            Response(202, "Accepted", "z9hG4bK-server-dialogs", "dialog-two"));

        SipInviteServerResult first = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack-one", "dialog-one", 42, "invite-call@example.com",
                "caller-tag"));
        SipInviteServerResult second = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack-two", "dialog-two", 42, "invite-call@example.com",
                "caller-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(first.AckDelivered, Is.True);
            Assert.That(second.AckDelivered, Is.True);
            Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
                Ack("z9hG4bK-wrong-tag", "other", 42, "invite-call@example.com",
                    "caller-tag")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
                Ack("z9hG4bK-wrong-call", "dialog-one", 42, "other@example.com",
                    "caller-tag")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
                Ack("z9hG4bK-wrong-cseq", "dialog-one", 43,
                    "invite-call@example.com", "caller-tag")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
                Ack("z9hG4bK-wrong-from", "dialog-one", 42,
                    "invite-call@example.com", "other-caller")));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RepeatedEndToEndAcksForEverySuccessAreDeliveredWithoutRestartingTimerL(
        bool reliable)
    {
        SipInviteServerTransaction transaction = Server("z9hG4bK-server-repeat-ack", reliable,
            new SipManualMonotonicClock(10));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(200, "OK", "z9hG4bK-server-repeat-ack", "dialog-one"));
        transaction.AdvanceTime(1000);
        transaction.SendResponse(
            Response(202, "Accepted", "z9hG4bK-server-repeat-ack", "dialog-two"));

        SipInviteServerResult first = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack-one", "dialog-one", 42, "invite-call@example.com",
                "caller-tag"));
        SipInviteServerResult firstRepeated = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack-one", "dialog-one", 42, "invite-call@example.com",
                "caller-tag"));
        SipInviteServerResult second = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack-two", "dialog-two", 42, "invite-call@example.com",
                "caller-tag"));
        SipInviteServerResult secondRepeated = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack-two-repeated", "dialog-two", 42,
                "invite-call@example.com", "caller-tag"));
        SipTransactionSnapshot accepted = transaction.GetSnapshot();
        SipInviteServerResult before = transaction.AdvanceTime(6409);
        SipInviteServerResult expired = transaction.AdvanceTime(6410);

        Assert.Multiple(() =>
        {
            Assert.That(first.AckDelivered, Is.True);
            Assert.That(firstRepeated.AckDelivered, Is.True);
            Assert.That(second.AckDelivered, Is.True);
            Assert.That(secondRepeated.AckDelivered, Is.True);
            Assert.That(first.AckAbsorbed, Is.False);
            Assert.That(firstRepeated.AckAbsorbed, Is.False);
            Assert.That(second.AckAbsorbed, Is.False);
            Assert.That(secondRepeated.AckAbsorbed, Is.False);
            Assert.That(first.DeliveredRequests.Single().Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(firstRepeated.DeliveredRequests.Single().Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(second.DeliveredRequests.Single().Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(secondRepeated.DeliveredRequests.Single().Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(first.OutboundMessages, Is.Empty);
            Assert.That(firstRepeated.OutboundMessages, Is.Empty);
            Assert.That(second.OutboundMessages, Is.Empty);
            Assert.That(secondRepeated.OutboundMessages, Is.Empty);
            Assert.That(accepted.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerL }));
            Assert.That(accepted.TimerDeadlines, Is.EqualTo(new long[] { 6410 }));
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerL }));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ServerTransactionNeverRetransmitsSuccessWithoutTransactionUser(
        bool reliable)
    {
        SipInviteServerTransaction transaction = Server("z9hG4bK-server-tu-owner", reliable,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        SipMessage success = Response(200, "OK", "z9hG4bK-server-tu-owner", "dialog-one");
        transaction.SendResponse(success);

        SipInviteServerResult firstInterval = transaction.AdvanceTime(100);
        SipInviteServerResult secondInterval = transaction.AdvanceTime(800);
        SipInviteServerResult repeatedInvite = transaction.ReceiveRequest(
            Invite("z9hG4bK-server-tu-owner"));
        SipInviteServerResult thirdInterval = transaction.AdvanceTime(1600);
        SipInviteServerResult userRetransmission = transaction.SendResponse(success);
        SipInviteServerResult beforeExpiry = transaction.AdvanceTime(6399);

        Assert.Multiple(() =>
        {
            Assert.That(firstInterval.OutboundMessages, Is.Empty);
            Assert.That(secondInterval.OutboundMessages, Is.Empty);
            Assert.That(thirdInterval.OutboundMessages, Is.Empty);
            Assert.That(beforeExpiry.OutboundMessages, Is.Empty);
            Assert.That(repeatedInvite.RequestAbsorbed, Is.True);
            Assert.That(repeatedInvite.Retransmitted, Is.False);
            Assert.That(repeatedInvite.OutboundMessages, Is.Empty);
            Assert.That(userRetransmission.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(userRetransmission.StateChanged, Is.False);
            Assert.That(userRetransmission.OutboundMessages.Single().StatusCode,
                Is.EqualTo(200));
            Assert.That(transaction.GetSnapshot().TimerDeadlines,
                Is.EqualTo(new long[] { 6400 }));
        });
    }

    [Test]
    public void LegacyAcceptedAckIsDeliveredToTransactionUser()
    {
        SipInviteServerTransaction transaction = Server("legacy-branch", false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(Response(200, "OK", "legacy-branch", "legacy-dialog"));

        SipInviteServerResult acknowledged = transaction.ReceiveRequest(
            Ack("legacy-branch", "legacy-dialog", 42, "invite-call@example.com",
                "caller-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(acknowledged.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(acknowledged.AckDelivered, Is.True);
            Assert.That(acknowledged.DeliveredRequests.Single().Method, Is.EqualTo(SipMethod.Ack));
        });
    }

    [Test]
    public void TimerLTerminatesServerAndCleansAcceptedState()
    {
        SipInviteServerTransaction transaction = Server("z9hG4bK-server-expire", false,
            new SipManualMonotonicClock(10));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(200, "OK", "z9hG4bK-server-expire", "accepted"));

        SipInviteServerResult before = transaction.AdvanceTime(6409);
        SipInviteServerResult expired = transaction.AdvanceTime(6410);

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerL }));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
            Assert.That(transaction.GetSnapshot().UpdatedAtMilliseconds, Is.EqualTo(6410));
        });
    }

    [Test]
    public void ServerTransportErrorPreservesProceedingAndAcceptedStates()
    {
        SipInviteServerTransaction proceeding = Server("z9hG4bK-server-error-one", false,
            new SipManualMonotonicClock(0));
        proceeding.StartTransaction();
        SipInviteServerResult proceedingError = proceeding.HandleTransportError("trying failed");

        SipInviteServerTransaction accepted = Server("z9hG4bK-server-error-two", false,
            new SipManualMonotonicClock(0));
        accepted.StartTransaction();
        accepted.SendResponse(
            Response(200, "OK", "z9hG4bK-server-error-two", "accepted"));
        SipInviteServerResult acceptedError = accepted.HandleTransportError("success failed");
        SipInviteServerResult retransmittedInvite = accepted.ReceiveRequest(
            Invite("z9hG4bK-server-error-two"));

        Assert.Multiple(() =>
        {
            Assert.That(proceedingError.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(proceedingError.StateChanged, Is.False);
            Assert.That(proceedingError.TransportFailed, Is.True);
            Assert.That(acceptedError.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(acceptedError.StateChanged, Is.False);
            Assert.That(acceptedError.TransportFailed, Is.True);
            Assert.That(accepted.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerL }));
            Assert.That(retransmittedInvite.RequestAbsorbed, Is.True);
            Assert.That(retransmittedInvite.OutboundMessages, Is.Empty);
        });
    }

    static SipInviteClientTransaction Client(string branch, bool reliable,
        SipManualMonotonicClock clock)
    {
        return new SipInviteClientTransaction("client-transaction", Invite(branch),
            Options(reliable), clock);
    }

    static SipInviteServerTransaction Server(string branch, bool reliable,
        SipManualMonotonicClock clock)
    {
        return new SipInviteServerTransaction("server-transaction", Invite(branch),
            Options(reliable), clock);
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

    static SipMessage Invite(string branch)
    {
        return Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string reason, string branch, string toTag)
    {
        string contact = status >= 200 && status < 300
            ? "Contact: <sip:bob@server.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + status.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n" + contact + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Ack(string branch, string toTag, int cseq, string callId,
        string fromTag)
    {
        return Parse("ACK sip:bob@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=" + fromTag + "\r\n"
            + "To: Bob <sip:bob@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " ACK\r\nContent-Length: 0\r\n\r\n");
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
}
