namespace sip.Tests;

public class SipInviteClientTransactionTests
{
    [Test]
    public void UnreliableStartSendsInviteAndSchedulesTimersAAndB()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipMessage invite = Invite("z9hG4bK-start", false);
        SipInviteClientTransaction transaction = Transaction(invite, false, clock);

        SipInviteClientResult started = transaction.StartTransaction();
        invite.RequestUri!.Host = "changed.example.com";
        started.OutboundMessages[0].RequestUri!.Host = "outbound-change.example.com";
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SipTransactionState.Calling));
            Assert.That(started.StateChanged, Is.True);
            Assert.That(started.OutboundMessages.Length, Is.EqualTo(1));
            Assert.That(started.OutboundMessages[0].Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(snapshot.Request!.RequestUri!.Host, Is.EqualTo("callee.example.com"));
            Assert.That(snapshot.State, Is.EqualTo(SipTransactionState.Calling));
            Assert.That(snapshot.TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerA, SipTimerNames.TimerB }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 1100, 7400 }));
            Assert.That(snapshot.CreatedAtMilliseconds, Is.EqualTo(1000));
            Assert.That(snapshot.UpdatedAtMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void ReliableStartSuppressesTimerAButRetainsTimerB()
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-reliable", false),
            true, new SipManualMonotonicClock(0));

        transaction.StartTransaction();
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerB }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 6400 }));
        });
    }

    [Test]
    public void TimerARetransmitsWithExponentialBackoffOnlyWhileCalling()
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-backoff", false),
            false, new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipInviteClientResult early = transaction.AdvanceTime(99);
        SipInviteClientResult first = transaction.AdvanceTime(100);
        first.OutboundMessages[0].RequestUri!.Host = "changed.example.com";
        SipInviteClientResult second = transaction.AdvanceTime(300);
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(early.OutboundMessages, Is.Empty);
            Assert.That(first.Retransmitted, Is.True);
            Assert.That(first.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerA }));
            Assert.That(second.Retransmitted, Is.True);
            Assert.That(second.OutboundMessages[0].RequestUri!.Host,
                Is.EqualTo("callee.example.com"));
            Assert.That(snapshot.TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerA, SipTimerNames.TimerB }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 700, 6400 }));
        });
    }

    [Test]
    public void TimerBTerminatesAndReportsTimeoutWithoutAck()
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-timeout", false),
            true, new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipInviteClientResult before = transaction.AdvanceTime(6399);
        SipInviteClientResult timeout = transaction.AdvanceTime(6400);
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Calling));
            Assert.That(timeout.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(timeout.TimedOut, Is.True);
            Assert.That(timeout.OutboundMessages, Is.Empty);
            Assert.That(timeout.DeliveredResponses, Is.Empty);
            Assert.That(timeout.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerB }));
            Assert.That(timeout.ErrorMessage, Does.Contain("timed out"));
            Assert.That(snapshot.TimerNames, Is.Empty);
            Assert.That(snapshot.UpdatedAtMilliseconds, Is.EqualTo(6400));
        });
    }

    [Test]
    public void ProvisionalResponsesEnterProceedingStopRetransmissionAndAreDelivered()
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-progress", false),
            false, new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipInviteClientResult first = transaction.ReceiveResponse(
            Response(180, "Ringing", "z9hG4bK-progress", "one"));
        SipInviteClientResult second = transaction.ReceiveResponse(
            Response(183, "Session Progress", "z9hG4bK-progress", "one"));
        SipInviteClientResult muchLater = transaction.AdvanceTime(100000);
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(first.StateChanged, Is.True);
            Assert.That(first.DeliveredResponses[0].StatusCode, Is.EqualTo(180));
            Assert.That(second.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(second.StateChanged, Is.False);
            Assert.That(second.DeliveredResponses[0].StatusCode, Is.EqualTo(183));
            Assert.That(muchLater.OutboundMessages, Is.Empty);
            Assert.That(snapshot.TimerNames, Is.Empty);
            Assert.That(snapshot.Response!.StatusCode, Is.EqualTo(183));
        });
    }

    [TestCase(200)]
    [TestCase(299)]
    public void SuccessfulFinalResponseIsDeliveredAndEntersAcceptedWithoutTransactionAck(int status)
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-success", false),
            false, new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(Response(180, "Ringing", "z9hG4bK-success", "final-tag"));

        SipInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "Success", "z9hG4bK-success", "final-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(result.DeliveredResponses.Length, Is.EqualTo(1));
            Assert.That(result.DeliveredResponses[0].StatusCode, Is.EqualTo(status));
            Assert.That(result.OutboundMessages, Is.Empty);
            Assert.That(transaction.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerM }));
            Assert.That(transaction.GetSnapshot().TimerDeadlines,
                Is.EqualTo(new long[] { 6400 }));
        });
    }

    [TestCase(300)]
    [TestCase(487)]
    [TestCase(699)]
    public void FailureFinalResponseCreatesStandardsCompliantAckAndTimerD(int status)
    {
        SipMessage invite = Invite("z9hG4bK-failure", true);
        SipInviteClientTransaction transaction = Transaction(invite, false,
            new SipManualMonotonicClock(50));
        transaction.StartTransaction();

        SipInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "Failure", "z9hG4bK-failure", "response-tag"));
        SipMessage ack = result.OutboundMessages[0];
        SipWriteResult written = new SipMessageCodec().SerializeMessage(ack, new SipWriteOptions());
        SipParseResult reparsed = new SipMessageCodec().ParseDatagramMessage(
            written.Bytes, new SipParseOptions());
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(result.DeliveredResponses[0].StatusCode, Is.EqualTo(status));
            Assert.That(ack.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ack.MethodToken, Is.EqualTo(SipMethods.Ack));
            Assert.That(ack.RequestUri!.Host, Is.EqualTo("callee.example.com"));
            Assert.That(ack.ViaValues.Length, Is.EqualTo(1));
            Assert.That(ack.ViaValues[0].Branch, Is.EqualTo("z9hG4bK-failure"));
            Assert.That(Tag(ack.From), Is.EqualTo("caller-tag"));
            Assert.That(Tag(ack.To), Is.EqualTo("response-tag"));
            Assert.That(ack.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(ack.CSeq.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ack.RouteValues.Length, Is.EqualTo(2));
            Assert.That(ack.Body, Is.Empty);
            Assert.That(HeaderValues(ack, SipHeaderNames.CallId),
                Is.EqualTo(new string[] { "invite-call@example.com" }));
            Assert.That(HeaderValues(ack, SipHeaderNames.CSeq),
                Is.EqualTo(new string[] { "42 ACK" }));
            Assert.That(HeaderValues(ack, SipHeaderNames.Route).Length, Is.EqualTo(2));
            Assert.That(HeaderValues(ack, SipHeaderNames.ContentLength),
                Is.EqualTo(new string[] { "0" }));
            Assert.That(written.Success, Is.True, written.ErrorMessage);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete), reparsed.ErrorMessage);
            Assert.That(snapshot.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerD }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 32050 }));
        });
    }

    [Test]
    public void RepeatedFailureResponseRetransmitsAckWithoutRedeliveryOrRestartingTimerD()
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-repeat", false),
            false, new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.ReceiveResponse(Response(486, "Busy Here", "z9hG4bK-repeat", "busy-tag"));
        transaction.AdvanceTime(1000);

        SipInviteClientResult repeated = transaction.ReceiveResponse(
            Response(486, "Busy Here", "z9hG4bK-repeat", "busy-tag"));
        SipTransactionSnapshot beforeExpiry = transaction.GetSnapshot();
        SipInviteClientResult expired = transaction.AdvanceTime(32000);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(repeated.Retransmitted, Is.True);
            Assert.That(repeated.OutboundMessages.Single().Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(repeated.DeliveredResponses, Is.Empty);
            Assert.That(beforeExpiry.TimerDeadlines, Is.EqualTo(new long[] { 32000 }));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerD }));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void ReliableFailureUsesZeroTimerDAndTerminatesOnExplicitAdvance()
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-fast-d", false),
            true, new SipManualMonotonicClock(75));
        transaction.StartTransaction();

        SipInviteClientResult failure = transaction.ReceiveResponse(
            Response(404, "Not Found", "z9hG4bK-fast-d", "not-found"));
        SipTransactionSnapshot completed = transaction.GetSnapshot();
        SipInviteClientResult advanced = transaction.AdvanceTime(75);

        Assert.Multiple(() =>
        {
            Assert.That(failure.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(completed.TimerDeadlines, Is.EqualTo(new long[] { 75 }));
            Assert.That(advanced.State, Is.EqualTo(SipTransactionState.Terminated));
        });
    }

    [Test]
    public void TransportErrorTerminatesActiveTransactionAndCleansTimers()
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-error", false),
            false, new SipManualMonotonicClock(10));
        transaction.StartTransaction();

        SipInviteClientResult failed = transaction.HandleTransportError("connection failed");
        SipInviteClientResult repeated = transaction.HandleTransportError("late failure");

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(failed.StateChanged, Is.True);
            Assert.That(failed.TransportFailed, Is.True);
            Assert.That(failed.ErrorMessage, Is.EqualTo("connection failed"));
            Assert.That(failed.OutboundMessages, Is.Empty);
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(repeated.TransportFailed, Is.False);
        });
    }

    [Test]
    public void InvalidLifecycleAndMismatchedResponsesAreRejected()
    {
        SipInviteClientTransaction transaction = Transaction(Invite("z9hG4bK-valid", false),
            false, new SipManualMonotonicClock(0));

        Assert.Throws<InvalidOperationException>(() => transaction.AdvanceTime(0));
        Assert.Throws<InvalidOperationException>(() => transaction.ReceiveResponse(
            Response(180, "Ringing", "z9hG4bK-valid", "tag")));
        transaction.StartTransaction();
        SipMessage malformed = Response(180, "Ringing", "z9hG4bK-valid", "tag");
        malformed.Headers = new SipHeader[0];

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => transaction.StartTransaction());
            Assert.Throws<ArgumentException>(() => transaction.ReceiveResponse(
                Response(180, "Ringing", "z9hG4bK-other", "tag")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveResponse(malformed));
            Assert.Throws<ArgumentException>(() => transaction.HandleTransportError(""));
            Assert.Throws<ArgumentException>(() => new SipInviteClientTransaction("tx",
                Request("OPTIONS", "z9hG4bK-valid"), Options(false),
                new SipManualMonotonicClock(0)));
        });
    }

    static SipInviteClientTransaction Transaction(SipMessage invite, bool reliable,
        SipManualMonotonicClock clock)
    {
        return new SipInviteClientTransaction("transaction-one", invite, Options(reliable), clock);
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
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Via: SIP/2.0/TCP previous.example.com:5070;branch=z9hG4bK-previous\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + routeHeaders
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Type: application/sdp\r\n"
            + "Content-Length: 3\r\n\r\nv=0");
    }

    static SipMessage Request(string method, string branch)
    {
        return Parse(method + " sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 " + method + "\r\nContent-Length: 0\r\n\r\n");
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
