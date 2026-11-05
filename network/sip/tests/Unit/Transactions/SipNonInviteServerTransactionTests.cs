namespace sip.Tests;

public class SipNonInviteServerTransactionTests
{
    [Test]
    public void StartEntersTryingAndDeliversDetachedRequestWithoutSendingResponse()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipMessage request = Request("OPTIONS", "z9hG4bK-start");
        SipNonInviteServerTransaction transaction = Transaction(request, false, clock);

        SipNonInviteServerResult started = transaction.StartTransaction();
        request.RequestUri!.Host = "changed.example.com";
        started.DeliveredRequests[0].RequestUri!.Host = "also-changed.example.com";
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(started.StateChanged, Is.True);
            Assert.That(started.OutboundMessages, Is.Empty);
            Assert.That(started.DeliveredRequests.Single().Method, Is.EqualTo(SipMethod.Options));
            Assert.That(snapshot.Kind, Is.EqualTo(SipTransactionKind.NonInvite));
            Assert.That(snapshot.Role, Is.EqualTo(SipTransactionRole.Server));
            Assert.That(snapshot.Request!.RequestUri!.Host, Is.EqualTo("service.example.com"));
            Assert.That(snapshot.Response, Is.Null);
            Assert.That(snapshot.TimerNames, Is.Empty);
            Assert.That(snapshot.CreatedAtMilliseconds, Is.EqualTo(1000));
            Assert.That(snapshot.UpdatedAtMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void TryingRequestRetransmissionIsAbsorbedWithoutRedeliveryOrResponse()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-trying"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipNonInviteServerResult repeated = transaction.ReceiveRequest(
            Request("OPTIONS", "z9hG4bK-trying"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(repeated.RequestAbsorbed, Is.True);
            Assert.That(repeated.Retransmitted, Is.False);
            Assert.That(repeated.OutboundMessages, Is.Empty);
            Assert.That(repeated.DeliveredRequests, Is.Empty);
        });
    }

    [Test]
    public void ProvisionalResponsesEnterProceedingAndLatestResponseIsReplayed()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-progress"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipNonInviteServerResult first = transaction.SendResponse(
            Response(100, "Trying", "z9hG4bK-progress", "OPTIONS"));
        SipNonInviteServerResult second = transaction.SendResponse(
            Response(100, "Still Trying", "z9hG4bK-progress", "OPTIONS"));
        SipNonInviteServerResult repeated = transaction.ReceiveRequest(
            Request("OPTIONS", "z9hG4bK-progress"));

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(first.StateChanged, Is.True);
            Assert.That(first.OutboundMessages.Single().ReasonPhrase, Is.EqualTo("Trying"));
            Assert.That(second.StateChanged, Is.False);
            Assert.That(second.OutboundMessages.Single().ReasonPhrase, Is.EqualTo("Still Trying"));
            Assert.That(repeated.RequestAbsorbed, Is.True);
            Assert.That(repeated.Retransmitted, Is.True);
            Assert.That(repeated.OutboundMessages.Single().ReasonPhrase,
                Is.EqualTo("Still Trying"));
            Assert.That(repeated.DeliveredRequests, Is.Empty);
        });
    }

    [TestCase(200)]
    [TestCase(487)]
    [TestCase(699)]
    public void FinalResponseFromTryingEntersCompletedAndStartsTimerJ(int status)
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-final"), false,
            new SipManualMonotonicClock(50));
        transaction.StartTransaction();

        SipNonInviteServerResult result = transaction.SendResponse(
            Response(status, "Final", "z9hG4bK-final", "OPTIONS"));
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.OutboundMessages.Single().StatusCode, Is.EqualTo(status));
            Assert.That(snapshot.Response!.StatusCode, Is.EqualTo(status));
            Assert.That(snapshot.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerJ }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 6450 }));
        });
    }

    [Test]
    public void FinalResponseFromProceedingEntersCompleted()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-proceeding-final"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(100, "Trying", "z9hG4bK-proceeding-final", "OPTIONS"));

        SipNonInviteServerResult result = transaction.SendResponse(
            Response(204, "No Content", "z9hG4bK-proceeding-final", "OPTIONS"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(result.OutboundMessages.Single().StatusCode, Is.EqualTo(204));
            Assert.That(transaction.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerJ }));
        });
    }

    [Test]
    public void CompletedRequestRetransmissionReplaysFinalAndLaterTuResponseIsDiscarded()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-completed"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(404, "Not Found", "z9hG4bK-completed", "OPTIONS"));

        SipNonInviteServerResult repeated = transaction.ReceiveRequest(
            Request("OPTIONS", "z9hG4bK-completed"));
        repeated.OutboundMessages[0].StatusCode = 500;
        SipNonInviteServerResult discarded = transaction.SendResponse(
            Response(503, "Unavailable", "z9hG4bK-completed", "OPTIONS"));
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(repeated.RequestAbsorbed, Is.True);
            Assert.That(repeated.Retransmitted, Is.True);
            Assert.That(repeated.DeliveredRequests, Is.Empty);
            Assert.That(discarded.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(discarded.ResponseDiscarded, Is.True);
            Assert.That(discarded.OutboundMessages, Is.Empty);
            Assert.That(snapshot.Response!.StatusCode, Is.EqualTo(404));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 6400 }));
        });
    }

    [Test]
    public void TimerJTerminatesUnreliableTransactionAtSixtyFourT1()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-timer-j"), false,
            new SipManualMonotonicClock(10));
        transaction.StartTransaction();
        transaction.SendResponse(
            Response(200, "OK", "z9hG4bK-timer-j", "OPTIONS"));

        SipNonInviteServerResult before = transaction.AdvanceTime(6409);
        SipNonInviteServerResult expired = transaction.AdvanceTime(6410);

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(before.FiredTimerNames, Is.Empty);
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.StateChanged, Is.True);
            Assert.That(expired.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerJ }));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
            Assert.That(transaction.GetSnapshot().UpdatedAtMilliseconds, Is.EqualTo(6410));
        });
    }

    [Test]
    public void ReliableTransportUsesZeroTimerJAndTerminatesOnExplicitAdvance()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("BYE", "z9hG4bK-reliable"), true,
            new SipManualMonotonicClock(75));
        transaction.StartTransaction();

        SipNonInviteServerResult final = transaction.SendResponse(
            Response(200, "OK", "z9hG4bK-reliable", "BYE"));
        SipTransactionSnapshot completed = transaction.GetSnapshot();
        SipNonInviteServerResult expired = transaction.AdvanceTime(75);

        Assert.Multiple(() =>
        {
            Assert.That(final.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(completed.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerJ }));
            Assert.That(completed.TimerDeadlines, Is.EqualTo(new long[] { 75 }));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerJ }));
        });
    }

    [Test]
    public void TerminatedTransactionAbsorbsLateMatchingRequestWithoutResponse()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-late"), true,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(Response(200, "OK", "z9hG4bK-late", "OPTIONS"));
        transaction.AdvanceTime(0);

        SipNonInviteServerResult late = transaction.ReceiveRequest(
            Request("OPTIONS", "z9hG4bK-late"));

        Assert.Multiple(() =>
        {
            Assert.That(late.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(late.RequestAbsorbed, Is.True);
            Assert.That(late.Retransmitted, Is.False);
            Assert.That(late.OutboundMessages, Is.Empty);
            Assert.That(late.DeliveredRequests, Is.Empty);
        });
    }

    [Test]
    public void LegacyRequestRetransmissionUsesRfc2543MatchingRules()
    {
        SipTransactionOptions options = Options(false);
        options.AllowLegacyMatching = true;
        SipNonInviteServerTransaction transaction = new SipNonInviteServerTransaction("legacy",
            Request("OPTIONS", "legacy-branch"), options, new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(Response(100, "Trying", "legacy-branch", "OPTIONS"));

        SipNonInviteServerResult repeated = transaction.ReceiveRequest(
            Request("OPTIONS", "legacy-branch"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.RequestAbsorbed, Is.True);
            Assert.That(repeated.Retransmitted, Is.True);
            Assert.That(repeated.OutboundMessages.Single().StatusCode, Is.EqualTo(100));
        });
    }

    [Test]
    public void TransportErrorTerminatesActiveTransactionAndCleansTimerJ()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-error"), false,
            new SipManualMonotonicClock(10));
        transaction.StartTransaction();
        transaction.SendResponse(Response(503, "Unavailable", "z9hG4bK-error", "OPTIONS"));

        SipNonInviteServerResult failed = transaction.HandleTransportError("send failed");
        SipNonInviteServerResult repeated = transaction.HandleTransportError("late failure");

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(failed.StateChanged, Is.True);
            Assert.That(failed.TransportFailed, Is.True);
            Assert.That(failed.ErrorMessage, Is.EqualTo("send failed"));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(repeated.TransportFailed, Is.False);
        });
    }

    [Test]
    public void InvalidLifecycleRequestKindsAndMismatchedMessagesAreRejected()
    {
        SipNonInviteServerTransaction transaction = Transaction(
            Request("OPTIONS", "z9hG4bK-valid"), false,
            new SipManualMonotonicClock(0));

        Assert.Throws<InvalidOperationException>(() => transaction.AdvanceTime(0));
        Assert.Throws<InvalidOperationException>(() => transaction.SendResponse(
            Response(100, "Trying", "z9hG4bK-valid", "OPTIONS")));
        Assert.Throws<InvalidOperationException>(() => transaction.ReceiveRequest(
            Request("OPTIONS", "z9hG4bK-valid")));
        transaction.StartTransaction();
        SipMessage malformed = Request("OPTIONS", "z9hG4bK-valid");
        malformed.Headers = new SipHeader[0];

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => transaction.StartTransaction());
            Assert.Throws<ArgumentException>(() => transaction.SendResponse(
                Response(100, "Trying", "z9hG4bK-other", "OPTIONS")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
                Request("OPTIONS", "z9hG4bK-other")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(malformed));
            Assert.Throws<ArgumentException>(() => transaction.HandleTransportError(""));
            Assert.Throws<ArgumentException>(() => new SipNonInviteServerTransaction("invite",
                Request("INVITE", "z9hG4bK-invite"), Options(false),
                new SipManualMonotonicClock(0)));
            Assert.Throws<ArgumentException>(() => new SipNonInviteServerTransaction("ack",
                Request("ACK", "z9hG4bK-ack"), Options(false),
                new SipManualMonotonicClock(0)));
        });
    }

    static SipNonInviteServerTransaction Transaction(SipMessage request, bool reliable,
        SipManualMonotonicClock clock)
    {
        return new SipNonInviteServerTransaction("transaction-one", request, Options(reliable),
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

    static SipMessage Response(int status, string reason, string branch, string method)
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
