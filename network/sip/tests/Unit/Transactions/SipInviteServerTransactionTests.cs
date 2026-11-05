namespace sip.Tests;

public class SipInviteServerTransactionTests
{
    [Test]
    public void StartEntersProceedingSendsTryingAndDeliversDetachedInvite()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipMessage invite = Invite("z9hG4bK-start");
        SipInviteServerTransaction transaction = Transaction(invite, false, clock);

        SipInviteServerResult started = transaction.StartTransaction();
        invite.RequestUri!.Host = "changed.example.com";
        started.DeliveredRequests[0].RequestUri!.Host = "also-changed.example.com";
        SipMessage trying = started.OutboundMessages.Single();
        SipWriteResult written = new SipMessageCodec().SerializeMessage(trying,
            new SipWriteOptions());
        SipParseResult reparsed = new SipMessageCodec().ParseDatagramMessage(written.Bytes,
            new SipParseOptions());
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(started.StateChanged, Is.True);
            Assert.That(started.DeliveredRequests.Length, Is.EqualTo(1));
            Assert.That(trying.Kind, Is.EqualTo(SipMessageKind.Response));
            Assert.That(trying.StatusCode, Is.EqualTo(100));
            Assert.That(trying.ReasonPhrase, Is.EqualTo("Trying"));
            Assert.That(trying.ViaValues.Length, Is.EqualTo(2));
            Assert.That(Tag(trying.To), Is.Null);
            Assert.That(trying.Body, Is.Empty);
            Assert.That(written.Success, Is.True, written.ErrorMessage);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete),
                reparsed.ErrorMessage);
            Assert.That(snapshot.Request!.RequestUri!.Host, Is.EqualTo("callee.example.com"));
            Assert.That(snapshot.Response!.StatusCode, Is.EqualTo(100));
            Assert.That(snapshot.TimerNames, Is.Empty);
            Assert.That(snapshot.CreatedAtMilliseconds, Is.EqualTo(1000));
            Assert.That(snapshot.UpdatedAtMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void InviteRetransmissionsAreAbsorbedAndReplayLatestProvisionalResponse()
    {
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-progress"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipInviteServerResult initialRepeat = transaction.ReceiveRequest(
            Invite("z9hG4bK-progress"));
        SipInviteServerResult ringing = transaction.SendResponse(
            Response(180, "Ringing", "z9hG4bK-progress", "server-tag"));
        SipInviteServerResult laterRepeat = transaction.ReceiveRequest(
            Invite("z9hG4bK-progress"));

        Assert.Multiple(() =>
        {
            Assert.That(initialRepeat.RequestAbsorbed, Is.True);
            Assert.That(initialRepeat.Retransmitted, Is.True);
            Assert.That(initialRepeat.OutboundMessages.Single().StatusCode, Is.EqualTo(100));
            Assert.That(initialRepeat.DeliveredRequests, Is.Empty);
            Assert.That(ringing.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(ringing.OutboundMessages.Single().StatusCode, Is.EqualTo(180));
            Assert.That(laterRepeat.RequestAbsorbed, Is.True);
            Assert.That(laterRepeat.OutboundMessages.Single().StatusCode, Is.EqualTo(180));
            Assert.That(laterRepeat.DeliveredRequests, Is.Empty);
        });
    }

    [TestCase(200)]
    [TestCase(299)]
    public void SuccessfulFinalResponseIsSentAndEntersAccepted(int status)
    {
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-success"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipInviteServerResult result = transaction.SendResponse(
            Response(status, "Success", "z9hG4bK-success", "success-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.OutboundMessages.Single().StatusCode, Is.EqualTo(status));
            Assert.That(transaction.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerL }));
            Assert.That(transaction.GetSnapshot().TimerDeadlines,
                Is.EqualTo(new long[] { 6400 }));
        });
    }

    [Test]
    public void UnreliableFailureSchedulesTimersGAndHAndRetransmitsWithCappedBackoff()
    {
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-backoff"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();

        SipInviteServerResult failure = transaction.SendResponse(
            Response(486, "Busy Here", "z9hG4bK-backoff", "busy-tag"));
        SipTransactionSnapshot firstSnapshot = transaction.GetSnapshot();
        SipInviteServerResult first = transaction.AdvanceTime(100);
        SipInviteServerResult second = transaction.AdvanceTime(300);
        SipInviteServerResult third = transaction.AdvanceTime(700);
        SipTransactionSnapshot lastSnapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(failure.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(failure.OutboundMessages.Single().StatusCode, Is.EqualTo(486));
            Assert.That(firstSnapshot.TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerG, SipTimerNames.TimerH }));
            Assert.That(firstSnapshot.TimerDeadlines, Is.EqualTo(new long[] { 100, 6400 }));
            Assert.That(first.Retransmitted, Is.True);
            Assert.That(first.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerG }));
            Assert.That(first.OutboundMessages.Single().StatusCode, Is.EqualTo(486));
            Assert.That(second.Retransmitted, Is.True);
            Assert.That(third.Retransmitted, Is.True);
            Assert.That(lastSnapshot.TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerG, SipTimerNames.TimerH }));
            Assert.That(lastSnapshot.TimerDeadlines, Is.EqualTo(new long[] { 1100, 6400 }));
        });
    }

    [Test]
    public void CompletedInviteRetransmissionReplaysFinalWithoutRestartingTimers()
    {
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-repeat"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(Response(404, "Not Found", "z9hG4bK-repeat", "not-found"));

        SipInviteServerResult repeated = transaction.ReceiveRequest(Invite("z9hG4bK-repeat"));
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(repeated.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(repeated.RequestAbsorbed, Is.True);
            Assert.That(repeated.Retransmitted, Is.True);
            Assert.That(repeated.OutboundMessages.Single().StatusCode, Is.EqualTo(404));
            Assert.That(repeated.DeliveredRequests, Is.Empty);
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 100, 6400 }));
        });
    }

    [Test]
    public void MatchingAckEntersConfirmedCancelsCompletedTimersAndTimerITerminates()
    {
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-ack"), false,
            new SipManualMonotonicClock(20));
        transaction.StartTransaction();
        transaction.SendResponse(Response(487, "Request Terminated", "z9hG4bK-ack", "final"));

        SipInviteServerResult acknowledged = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack", "final"));
        SipInviteServerResult duplicate = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack", "final"));
        SipTransactionSnapshot confirmed = transaction.GetSnapshot();
        SipInviteServerResult expired = transaction.AdvanceTime(520);

        Assert.Multiple(() =>
        {
            Assert.That(acknowledged.State, Is.EqualTo(SipTransactionState.Confirmed));
            Assert.That(acknowledged.StateChanged, Is.True);
            Assert.That(acknowledged.AckAbsorbed, Is.True);
            Assert.That(acknowledged.OutboundMessages, Is.Empty);
            Assert.That(duplicate.StateChanged, Is.False);
            Assert.That(duplicate.AckAbsorbed, Is.True);
            Assert.That(confirmed.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerI }));
            Assert.That(confirmed.TimerDeadlines, Is.EqualTo(new long[] { 520 }));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(expired.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerI }));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void ReliableFailureSuppressesTimerGAndUsesZeroTimerIAfterAck()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(75);
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-reliable"), true,
            clock);
        transaction.StartTransaction();

        transaction.SendResponse(Response(603, "Decline", "z9hG4bK-reliable", "declined"));
        SipTransactionSnapshot completed = transaction.GetSnapshot();
        SipInviteServerResult noRetransmission = transaction.AdvanceTime(1000);
        SipInviteServerResult acknowledged = transaction.ReceiveRequest(
            Ack("z9hG4bK-reliable", "declined"));
        SipTransactionSnapshot confirmed = transaction.GetSnapshot();
        SipInviteServerResult expired = transaction.AdvanceTime(1000);

        Assert.Multiple(() =>
        {
            Assert.That(completed.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerH }));
            Assert.That(completed.TimerDeadlines, Is.EqualTo(new long[] { 6475 }));
            Assert.That(noRetransmission.OutboundMessages, Is.Empty);
            Assert.That(noRetransmission.FiredTimerNames, Is.Empty);
            Assert.That(acknowledged.State, Is.EqualTo(SipTransactionState.Confirmed));
            Assert.That(confirmed.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerI }));
            Assert.That(confirmed.TimerDeadlines, Is.EqualTo(new long[] { 1000 }));
            Assert.That(expired.State, Is.EqualTo(SipTransactionState.Terminated));
        });
    }

    [Test]
    public void TimerHTerminatesAndReportsMissingAck()
    {
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-timeout"), false,
            new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(Response(500, "Failure", "z9hG4bK-timeout", "failed"));

        SipInviteServerResult before = transaction.AdvanceTime(6399);
        SipInviteServerResult timeout = transaction.AdvanceTime(6400);

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(timeout.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(timeout.TimedOut, Is.True);
            Assert.That(timeout.ErrorMessage, Does.Contain("waiting for ACK"));
            Assert.That(timeout.FiredTimerNames, Does.Contain(SipTimerNames.TimerH));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void LegacyAckRequiresFinalResponseToTag()
    {
        SipTransactionOptions options = Options(false);
        options.AllowLegacyMatching = true;
        SipInviteServerTransaction transaction = new SipInviteServerTransaction("legacy",
            Invite("legacy-branch"), options, new SipManualMonotonicClock(0));
        transaction.StartTransaction();
        transaction.SendResponse(Response(486, "Busy Here", "legacy-branch", "legacy-final"));

        Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
            Ack("legacy-branch", "wrong-tag")));
        SipInviteServerResult matched = transaction.ReceiveRequest(
            Ack("legacy-branch", "legacy-final"));

        Assert.Multiple(() =>
        {
            Assert.That(matched.State, Is.EqualTo(SipTransactionState.Confirmed));
            Assert.That(matched.AckAbsorbed, Is.True);
        });
    }

    [Test]
    public void TransportErrorReportsFailureWithoutDestroyingTransactionState()
    {
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-error"), false,
            new SipManualMonotonicClock(10));
        transaction.StartTransaction();
        transaction.SendResponse(Response(503, "Unavailable", "z9hG4bK-error", "error"));

        SipInviteServerResult failed = transaction.HandleTransportError("send failed");
        SipInviteServerResult repeated = transaction.HandleTransportError("late failure");

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(failed.StateChanged, Is.False);
            Assert.That(failed.TransportFailed, Is.True);
            Assert.That(failed.ErrorMessage, Is.EqualTo("send failed"));
            Assert.That(transaction.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerG, SipTimerNames.TimerH }));
            Assert.That(repeated.StateChanged, Is.False);
            Assert.That(repeated.TransportFailed, Is.True);
        });
    }

    [Test]
    public void InvalidLifecycleAndMismatchedMessagesAreRejected()
    {
        SipInviteServerTransaction transaction = Transaction(Invite("z9hG4bK-valid"), false,
            new SipManualMonotonicClock(0));

        Assert.Throws<InvalidOperationException>(() => transaction.AdvanceTime(0));
        Assert.Throws<InvalidOperationException>(() => transaction.SendResponse(
            Response(180, "Ringing", "z9hG4bK-valid", "tag")));
        Assert.Throws<InvalidOperationException>(() => transaction.ReceiveRequest(
            Invite("z9hG4bK-valid")));
        transaction.StartTransaction();
        SipMessage malformed = Ack("z9hG4bK-valid", "tag");
        malformed.Headers = new SipHeader[0];

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => transaction.StartTransaction());
            Assert.Throws<ArgumentException>(() => transaction.SendResponse(
                Response(180, "Ringing", "z9hG4bK-other", "tag")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
                Invite("z9hG4bK-other")));
            Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(malformed));
            Assert.Throws<ArgumentException>(() => transaction.HandleTransportError(""));
            Assert.Throws<ArgumentException>(() => new SipInviteServerTransaction("tx",
                Request("OPTIONS", "z9hG4bK-valid"), Options(false),
                new SipManualMonotonicClock(0)));
        });
    }

    static SipInviteServerTransaction Transaction(SipMessage invite, bool reliable,
        SipManualMonotonicClock clock)
    {
        return new SipInviteServerTransaction("transaction-one", invite, Options(reliable), clock);
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
            + "Via: SIP/2.0/TCP previous.example.com:5070;branch=z9hG4bK-previous\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Request(string method, string branch)
    {
        return Parse(method + " sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 " + method + "\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Ack(string branch, string toTag)
    {
        return Parse("ACK sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: invite-call@example.com\r\n"
            + "CSeq: 42 ACK\r\nContent-Length: 0\r\n\r\n");
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
}
