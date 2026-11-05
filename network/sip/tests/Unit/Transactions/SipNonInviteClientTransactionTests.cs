namespace sip.Tests;

public class SipNonInviteClientTransactionTests
{
    [Test]
    public void UnreliableStartEntersTryingSendsDetachedRequestAndSchedulesEAndF()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipMessage request = Request("OPTIONS", "z9hG4bK-start");
        SipNonInviteClientTransaction transaction = Transaction(request, false, clock);

        SipNonInviteClientResult started = transaction.StartTransaction();
        request.RequestUri!.Host = "changed.example.com";
        started.OutboundMessages[0].RequestUri!.Host = "also-changed.example.com";
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(started.StateChanged, Is.True);
            Assert.That(started.OutboundMessages.Single().Method, Is.EqualTo(SipMethod.Options));
            Assert.That(snapshot.Kind, Is.EqualTo(SipTransactionKind.NonInvite));
            Assert.That(snapshot.Role, Is.EqualTo(SipTransactionRole.Client));
            Assert.That(snapshot.Request!.RequestUri!.Host, Is.EqualTo("service.example.com"));
            Assert.That(snapshot.TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerE, SipTimerNames.TimerF }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 1100, 7400 }));
            Assert.That(snapshot.CreatedAtMilliseconds, Is.EqualTo(1000));
            Assert.That(snapshot.UpdatedAtMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void ReliableStartSuppressesTimerEButRetainsTimerF()
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-reliable"), true,
            new SipManualMonotonicClock(0));

        transaction.StartTransaction();
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerF }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 6400 }));
        });
    }

    [Test]
    public void TimerERetransmitsWithExponentialBackoffCappedAtT2WhileTrying()
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-backoff"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipNonInviteClientResult early = transaction.AdvanceTime(99);
        SipNonInviteClientResult first = transaction.AdvanceTime(100);
        first.OutboundMessages[0].RequestUri!.Host = "changed.example.com";
        SipNonInviteClientResult second = transaction.AdvanceTime(300);
        SipNonInviteClientResult third = transaction.AdvanceTime(700);
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(early.OutboundMessages, Is.Empty);
            Assert.That(first.Retransmitted, Is.True);
            Assert.That(first.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerE }));
            Assert.That(second.Retransmitted, Is.True);
            Assert.That(second.OutboundMessages.Single().RequestUri!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(third.Retransmitted, Is.True);
            Assert.That(snapshot.TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerE, SipTimerNames.TimerF }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 1100, 6400 }));
        });
    }

    [Test]
    public void ProvisionalResponsesEnterProceedingButKeepTimerEAndFActive()
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-progress"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipMessage provisional = Response(100, "Trying", "z9hG4bK-progress");
        SipNonInviteClientResult first = transaction.ReceiveResponse(provisional);
        provisional.StatusCode = 199;
        first.DeliveredResponses[0].StatusCode = 198;
        SipNonInviteClientResult retransmission = transaction.AdvanceTime(100);
        SipNonInviteClientResult repeated = transaction.ReceiveResponse(
            Response(100, "Trying", "z9hG4bK-progress"));
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(first.StateChanged, Is.True);
            Assert.That(first.DeliveredResponses.Single().StatusCode, Is.EqualTo(198));
            Assert.That(retransmission.Retransmitted, Is.True);
            Assert.That(retransmission.OutboundMessages.Single().Method,
                Is.EqualTo(SipMethod.Options));
            Assert.That(repeated.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(repeated.DeliveredResponses.Single().StatusCode, Is.EqualTo(100));
            Assert.That(snapshot.Response!.StatusCode, Is.EqualTo(100));
            Assert.That(snapshot.TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerE, SipTimerNames.TimerF }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 500, 6400 }));
        });
    }

    [Test]
    public void ProceedingTimerERetransmitsAtFixedT2Intervals()
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-fixed"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(Response(100, "Trying", "z9hG4bK-fixed"));

        SipNonInviteClientResult first = transaction.AdvanceTime(100);
        SipNonInviteClientResult early = transaction.AdvanceTime(499);
        SipNonInviteClientResult second = transaction.AdvanceTime(500);
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(first.Retransmitted, Is.True);
            Assert.That(early.OutboundMessages, Is.Empty);
            Assert.That(second.Retransmitted, Is.True);
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 900, 6400 }));
        });
    }

    [TestCase(200)]
    [TestCase(408)]
    [TestCase(699)]
    public void EveryFinalResponseIsDeliveredOnceAndStartsTimerK(int status)
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-final"), false,
            new SipManualMonotonicClock(50));
        transaction.StartTransaction();
        transaction.ReceiveResponse(Response(100, "Trying", "z9hG4bK-final"));

        SipNonInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "Final", "z9hG4bK-final"));
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.DeliveredResponses.Single().StatusCode, Is.EqualTo(status));
            Assert.That(result.OutboundMessages, Is.Empty);
            Assert.That(snapshot.Response!.StatusCode, Is.EqualTo(status));
            Assert.That(snapshot.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerK }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 550 }));
        });
    }

    [Test]
    public void DuplicateFinalAndLateResponsesAreAbsorbedWithoutRedelivery()
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-absorb"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(Response(200, "OK", "z9hG4bK-absorb"));

        SipNonInviteClientResult duplicate = transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-absorb"));
        transaction.AdvanceTime(500);
        SipNonInviteClientResult late = transaction.ReceiveResponse(
            Response(200, "OK", "z9hG4bK-absorb"));

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(duplicate.ResponseAbsorbed, Is.True);
            Assert.That(duplicate.DeliveredResponses, Is.Empty);
            Assert.That(late.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(late.ResponseAbsorbed, Is.True);
            Assert.That(late.DeliveredResponses, Is.Empty);
        });
    }

    [Test]
    public void TimerFExpiresInTryingAndProceedingAndReportsTimeout()
    {
        SipNonInviteClientTransaction trying = Transaction(
            Request("OPTIONS", "z9hG4bK-timeout-one"), true,
            new SipManualMonotonicClock(0));
        trying.StartTransaction();
        SipNonInviteClientResult tryingTimeout = trying.AdvanceTime(6400);

        SipNonInviteClientTransaction proceeding = Transaction(
            Request("OPTIONS", "z9hG4bK-timeout-two"), true,
            new SipManualMonotonicClock(10));
        proceeding.StartTransaction();
        proceeding.ReceiveResponse(Response(100, "Trying", "z9hG4bK-timeout-two"));
        SipNonInviteClientResult proceedingTimeout = proceeding.AdvanceTime(6410);

        Assert.Multiple(() =>
        {
            Assert.That(tryingTimeout.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(tryingTimeout.TimedOut, Is.True);
            Assert.That(tryingTimeout.ErrorMessage, Does.Contain("timed out"));
            Assert.That(tryingTimeout.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerF }));
            Assert.That(proceedingTimeout.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(proceedingTimeout.TimedOut, Is.True);
            Assert.That(proceeding.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void ReliableFinalUsesZeroTimerKAndTerminatesOnExplicitAdvance()
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("BYE", "z9hG4bK-fast-k"), true,
            new SipManualMonotonicClock(75));
        transaction.StartTransaction();

        SipNonInviteClientResult final = transaction.ReceiveResponse(
            ResponseForMethod(200, "OK", "z9hG4bK-fast-k", "BYE"));
        SipTransactionSnapshot completed = transaction.GetSnapshot();
        SipNonInviteClientResult expired = transaction.AdvanceTime(75);

        Assert.Multiple(() =>
        {
            Assert.That(final.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(completed.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerK }));
            Assert.That(completed.TimerDeadlines, Is.EqualTo(new long[] { 75 }));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerK }));
        });
    }

    [Test]
    public void TransportErrorTerminatesAndCleansTimers()
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-error"), false,
            new SipManualMonotonicClock(10));
        transaction.StartTransaction();

        SipNonInviteClientResult failed = transaction.HandleTransportError("connection failed");
        SipNonInviteClientResult repeated = transaction.HandleTransportError("late failure");

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(failed.StateChanged, Is.True);
            Assert.That(failed.TransportFailed, Is.True);
            Assert.That(failed.ErrorMessage, Is.EqualTo("connection failed"));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(repeated.TransportFailed, Is.False);
        });
    }

    [Test]
    public void InvalidLifecycleRequestKindsAndMismatchedResponsesAreRejected()
    {
        SipNonInviteClientTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-valid"), false,
            new SipManualMonotonicClock(0));

        Assert.Throws<InvalidOperationException>(() => transaction.AdvanceTime(0));
        Assert.Throws<InvalidOperationException>(() => transaction.ReceiveResponse(
            Response(100, "Trying", "z9hG4bK-valid")));
        transaction.StartTransaction();
        SipMessage malformed = Response(100, "Trying", "z9hG4bK-valid");
        malformed.Headers = new SipHeader[0];

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => transaction.StartTransaction());
            Assert.Throws<ArgumentException>(() => transaction.ReceiveResponse(
                Response(100, "Trying", "z9hG4bK-other")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveResponse(malformed));
            Assert.Throws<ArgumentException>(() => transaction.HandleTransportError(""));
            Assert.Throws<ArgumentException>(() => new SipNonInviteClientTransaction("invite",
                Request("INVITE", "z9hG4bK-invite"), Options(false),
                new SipManualMonotonicClock(0)));
            Assert.Throws<ArgumentException>(() => new SipNonInviteClientTransaction("ack",
                Request("ACK", "z9hG4bK-ack"), Options(false),
                new SipManualMonotonicClock(0)));
        });
    }

    static SipNonInviteClientTransaction Transaction(SipMessage request, bool reliable,
        SipManualMonotonicClock clock)
    {
        return new SipNonInviteClientTransaction("transaction-one", request, Options(reliable),
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

    static SipMessage Request(string method, string branch)
    {
        string contact = method == "INVITE"
            ? "Contact: <sip:alice@client.example.com>\r\n" : "";
        return Parse(method + " sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: non-invite-call@example.com\r\n"
            + "CSeq: 42 " + method + "\r\n" + contact
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string reason, string branch)
    {
        return ResponseForMethod(status, reason, branch, "OPTIONS");
    }

    static SipMessage ResponseForMethod(int status, string reason, string branch, string method)
    {
        return Parse("SIP/2.0 " + status.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Service <sip:service@example.com>;tag=server-tag\r\n"
            + "Call-ID: non-invite-call@example.com\r\n"
            + "CSeq: 42 " + method + "\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}
