namespace sip.Tests;

public class SipNonInviteTransactionMatrixTests
{
    [TestCase(100, SipTransactionState.Proceeding)]
    [TestCase(199, SipTransactionState.Proceeding)]
    [TestCase(200, SipTransactionState.Completed)]
    [TestCase(299, SipTransactionState.Completed)]
    [TestCase(300, SipTransactionState.Completed)]
    [TestCase(699, SipTransactionState.Completed)]
    public void ClientTryingHandlesEveryResponseClassBoundary(
        int status, SipTransactionState expectedState)
    {
        SipNonInviteClientTransaction transaction = Client(
            false, "OPTIONS", "z9hG4bK-client-trying");
        transaction.StartTransaction();

        SipNonInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "OPTIONS", "z9hG4bK-client-trying"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.DeliveredResponses, Has.Length.EqualTo(1));
            Assert.That(result.DeliveredResponses[0].StatusCode, Is.EqualTo(status));
            Assert.That(result.OutboundMessages, Is.Empty);
            Assert.That(result.ResponseAbsorbed, Is.False);
            Assert.That(transaction.GetSnapshot().TimerNames,
                status < 200
                    ? Is.EqualTo(new string[] { SipTimerNames.TimerE, SipTimerNames.TimerF })
                    : Is.EqualTo(new string[] { SipTimerNames.TimerK }));
        });
    }

    [TestCase(100, SipTransactionState.Proceeding, false)]
    [TestCase(199, SipTransactionState.Proceeding, false)]
    [TestCase(200, SipTransactionState.Completed, true)]
    [TestCase(299, SipTransactionState.Completed, true)]
    [TestCase(300, SipTransactionState.Completed, true)]
    [TestCase(699, SipTransactionState.Completed, true)]
    public void ClientProceedingHandlesEveryResponseClassBoundary(int status,
        SipTransactionState expectedState, bool changed)
    {
        SipNonInviteClientTransaction transaction = ClientAt(
            SipTransactionState.Proceeding, false, "z9hG4bK-client-proceeding");

        SipNonInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "OPTIONS", "z9hG4bK-client-proceeding"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.StateChanged, Is.EqualTo(changed));
            Assert.That(result.DeliveredResponses, Has.Length.EqualTo(1));
            Assert.That(result.ResponseAbsorbed, Is.False);
            Assert.That(result.OutboundMessages, Is.Empty);
        });
    }

    [TestCase(100)]
    [TestCase(199)]
    [TestCase(200)]
    [TestCase(299)]
    [TestCase(300)]
    [TestCase(699)]
    public void ClientCompletedAbsorbsEveryResponseClassWithoutRestartingTimerK(int status)
    {
        SipNonInviteClientTransaction transaction = ClientAt(
            SipTransactionState.Completed, false, "z9hG4bK-client-completed");
        long deadline = transaction.GetSnapshot().TimerDeadlines.Single();

        SipNonInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "OPTIONS", "z9hG4bK-client-completed"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.ResponseAbsorbed, Is.True);
            Assert.That(result.DeliveredResponses, Is.Empty);
            Assert.That(result.OutboundMessages, Is.Empty);
            Assert.That(transaction.GetSnapshot().TimerDeadlines.Single(), Is.EqualTo(deadline));
        });
    }

    [TestCase(100)]
    [TestCase(199)]
    [TestCase(200)]
    [TestCase(299)]
    [TestCase(300)]
    [TestCase(699)]
    public void ClientTerminatedAbsorbsEveryLateResponseClass(int status)
    {
        SipNonInviteClientTransaction transaction = ClientAt(
            SipTransactionState.Terminated, false, "z9hG4bK-client-terminated");

        SipNonInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "OPTIONS", "z9hG4bK-client-terminated"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.ResponseAbsorbed, Is.True);
            Assert.That(result.DeliveredResponses, Is.Empty);
            Assert.That(result.OutboundMessages, Is.Empty);
        });
    }

    [TestCase(SipTransactionState.Trying)]
    [TestCase(SipTransactionState.Proceeding)]
    [TestCase(SipTransactionState.Completed)]
    public void ClientTransportFailureTerminatesEveryLiveState(
        SipTransactionState state)
    {
        SipNonInviteClientTransaction transaction = ClientAt(
            state, false, "z9hG4bK-client-error");

        SipNonInviteClientResult result =
            transaction.HandleTransportError("transport failed");

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.TransportFailed, Is.True);
            Assert.That(result.ErrorMessage, Is.EqualTo("transport failed"));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void ClientTransportFailureAfterTerminationIsIdempotent()
    {
        SipNonInviteClientTransaction transaction = ClientAt(
            SipTransactionState.Terminated, false, "z9hG4bK-client-idempotent");

        SipNonInviteClientResult result =
            transaction.HandleTransportError("late failure");

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.TransportFailed, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void ClientTimerEHonorsExactBoundariesAndCapsAtT2WhileTrying()
    {
        SipNonInviteClientTransaction transaction = Client(
            false, "OPTIONS", "z9hG4bK-timer-e-trying");
        transaction.StartTransaction();

        SipNonInviteClientResult beforeFirst = transaction.AdvanceTime(99);
        SipNonInviteClientResult first = transaction.AdvanceTime(100);
        SipTransactionSnapshot afterFirst = transaction.GetSnapshot();
        SipNonInviteClientResult beforeSecond = transaction.AdvanceTime(299);
        SipNonInviteClientResult second = transaction.AdvanceTime(300);
        transaction.AdvanceTime(700);
        SipTransactionSnapshot capped = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(beforeFirst.FiredTimerNames, Is.Empty);
            Assert.That(first.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerE }));
            Assert.That(first.Retransmitted, Is.True);
            Assert.That(afterFirst.TimerDeadlines[0], Is.EqualTo(300));
            Assert.That(beforeSecond.FiredTimerNames, Is.Empty);
            Assert.That(second.Retransmitted, Is.True);
            Assert.That(capped.TimerDeadlines[0], Is.EqualTo(1100));
        });
    }

    [Test]
    public void Rfc4320HundredTryingAfterTimerEReachesT2KeepsFixedRetransmissions()
    {
        SipNonInviteClientTransaction transaction = Client(
            false, "OPTIONS", "z9hG4bK-rfc4320");
        transaction.StartTransaction();
        transaction.AdvanceTime(100);
        transaction.AdvanceTime(300);
        SipTransactionSnapshot atT2 = transaction.GetSnapshot();

        SipNonInviteClientResult provisional = transaction.ReceiveResponse(
            Response(100, "OPTIONS", "z9hG4bK-rfc4320"));
        SipNonInviteClientResult before = transaction.AdvanceTime(699);
        SipNonInviteClientResult retransmitted = transaction.AdvanceTime(700);
        SipTransactionSnapshot after = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(atT2.TimerDeadlines, Is.EqualTo(new long[] { 700, 6400 }));
            Assert.That(provisional.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(provisional.DeliveredResponses.Single().StatusCode, Is.EqualTo(100));
            Assert.That(before.FiredTimerNames, Is.Empty);
            Assert.That(retransmitted.Retransmitted, Is.True);
            Assert.That(retransmitted.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerE }));
            Assert.That(after.TimerDeadlines, Is.EqualTo(new long[] { 1100, 6400 }));
        });
    }

    [TestCase(SipTransactionState.Trying)]
    [TestCase(SipTransactionState.Proceeding)]
    public void ClientTimerFUsesExactBoundaryInEveryActiveState(
        SipTransactionState state)
    {
        SipNonInviteClientTransaction transaction = ClientAt(
            state, true, "z9hG4bK-timer-f");

        SipNonInviteClientResult before = transaction.AdvanceTime(6399);
        SipNonInviteClientResult at = transaction.AdvanceTime(6400);

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(state));
            Assert.That(before.FiredTimerNames, Is.Empty);
            Assert.That(at.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(at.StateChanged, Is.True);
            Assert.That(at.TimedOut, Is.True);
            Assert.That(at.FiredTimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerF }));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [TestCase(false, 499, 500)]
    [TestCase(true, 0, 0)]
    public void ClientTimerKHonorsReliableAndUnreliableBoundaries(
        bool reliable, long beforeTime, long expiryTime)
    {
        SipNonInviteClientTransaction transaction = Client(
            reliable, "BYE", "z9hG4bK-timer-k");
        transaction.StartTransaction();
        transaction.ReceiveResponse(Response(200, "BYE", "z9hG4bK-timer-k"));

        SipNonInviteClientResult before = transaction.AdvanceTime(beforeTime);
        SipNonInviteClientResult at = transaction.AdvanceTime(expiryTime);

        Assert.Multiple(() =>
        {
            Assert.That(before.State,
                Is.EqualTo(reliable ? SipTransactionState.Terminated
                    : SipTransactionState.Completed));
            Assert.That(at.State, Is.EqualTo(SipTransactionState.Terminated));
            if (!reliable)
            {
                Assert.That(before.FiredTimerNames, Is.Empty);
                Assert.That(at.FiredTimerNames,
                    Is.EqualTo(new string[] { SipTimerNames.TimerK }));
            }
        });
    }

    [Test]
    public void ReliableClientSuppressesTimerEInTryingAndProceeding()
    {
        SipNonInviteClientTransaction transaction = Client(
            true, "OPTIONS", "z9hG4bK-reliable-client");
        transaction.StartTransaction();
        SipNonInviteClientResult trying = transaction.AdvanceTime(100);
        transaction.ReceiveResponse(
            Response(100, "OPTIONS", "z9hG4bK-reliable-client"));
        SipNonInviteClientResult proceeding = transaction.AdvanceTime(400);

        Assert.Multiple(() =>
        {
            Assert.That(trying.FiredTimerNames, Is.Empty);
            Assert.That(trying.OutboundMessages, Is.Empty);
            Assert.That(proceeding.FiredTimerNames, Is.Empty);
            Assert.That(proceeding.OutboundMessages, Is.Empty);
            Assert.That(transaction.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerF }));
        });
    }

    [TestCase("OPTIONS")]
    [TestCase("BYE")]
    [TestCase("CANCEL")]
    [TestCase("REGISTER")]
    [TestCase("MESSAGE")]
    [TestCase("INFO")]
    [TestCase("PRACK")]
    [TestCase("UPDATE")]
    [TestCase("SUBSCRIBE")]
    [TestCase("NOTIFY")]
    [TestCase("REFER")]
    [TestCase("PUBLISH")]
    public void ClientSupportsEveryRecognizedNonInviteTransactionMethod(string method)
    {
        SipNonInviteClientTransaction transaction = Client(
            false, method, "z9hG4bK-method");

        SipNonInviteClientResult result = transaction.StartTransaction();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(result.OutboundMessages.Single().MethodToken, Is.EqualTo(method));
            Assert.That(transaction.GetSnapshot().Kind,
                Is.EqualTo(SipTransactionKind.NonInvite));
        });
    }

    [TestCase(100, SipTransactionState.Proceeding, true)]
    [TestCase(199, SipTransactionState.Proceeding, true)]
    [TestCase(200, SipTransactionState.Completed, true)]
    [TestCase(299, SipTransactionState.Completed, true)]
    [TestCase(300, SipTransactionState.Completed, true)]
    [TestCase(699, SipTransactionState.Completed, true)]
    public void ServerTryingHandlesEveryResponseClassBoundary(int status,
        SipTransactionState expectedState, bool changed)
    {
        SipNonInviteServerTransaction transaction = Server(
            false, "OPTIONS", "z9hG4bK-server-trying");
        transaction.StartTransaction();

        SipNonInviteServerResult result = transaction.SendResponse(
            Response(status, "OPTIONS", "z9hG4bK-server-trying"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.StateChanged, Is.EqualTo(changed));
            Assert.That(result.OutboundMessages, Has.Length.EqualTo(1));
            Assert.That(result.OutboundMessages[0].StatusCode, Is.EqualTo(status));
            Assert.That(result.ResponseDiscarded, Is.False);
        });
    }

    [TestCase(100, SipTransactionState.Proceeding, false)]
    [TestCase(199, SipTransactionState.Proceeding, false)]
    [TestCase(200, SipTransactionState.Completed, true)]
    [TestCase(299, SipTransactionState.Completed, true)]
    [TestCase(300, SipTransactionState.Completed, true)]
    [TestCase(699, SipTransactionState.Completed, true)]
    public void ServerProceedingHandlesEveryResponseClassBoundary(int status,
        SipTransactionState expectedState, bool changed)
    {
        SipNonInviteServerTransaction transaction = ServerAt(
            SipTransactionState.Proceeding, false, "z9hG4bK-server-proceeding");

        SipNonInviteServerResult result = transaction.SendResponse(
            Response(status, "OPTIONS", "z9hG4bK-server-proceeding"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.StateChanged, Is.EqualTo(changed));
            Assert.That(result.OutboundMessages, Has.Length.EqualTo(1));
            Assert.That(result.ResponseDiscarded, Is.False);
        });
    }

    [TestCase(SipTransactionState.Completed, 100)]
    [TestCase(SipTransactionState.Completed, 199)]
    [TestCase(SipTransactionState.Completed, 200)]
    [TestCase(SipTransactionState.Completed, 699)]
    [TestCase(SipTransactionState.Terminated, 100)]
    [TestCase(SipTransactionState.Terminated, 199)]
    [TestCase(SipTransactionState.Terminated, 200)]
    [TestCase(SipTransactionState.Terminated, 699)]
    public void ServerDiscardsEveryResponseClassAfterCompletion(
        SipTransactionState state, int status)
    {
        SipNonInviteServerTransaction transaction = ServerAt(
            state, false, "z9hG4bK-server-discard");

        SipNonInviteServerResult result = transaction.SendResponse(
            Response(status, "OPTIONS", "z9hG4bK-server-discard"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.ResponseDiscarded, Is.True);
            Assert.That(result.OutboundMessages, Is.Empty);
        });
    }

    [TestCase(SipTransactionState.Trying, 0, 0)]
    [TestCase(SipTransactionState.Proceeding, 1, 100)]
    [TestCase(SipTransactionState.Completed, 1, 486)]
    [TestCase(SipTransactionState.Terminated, 0, 0)]
    public void ServerRetransmissionBehaviorIsDefinedInEveryState(
        SipTransactionState state, int outboundCount, int responseStatus)
    {
        SipNonInviteServerTransaction transaction = ServerAt(
            state, false, "z9hG4bK-server-repeat");

        SipNonInviteServerResult result = transaction.ReceiveRequest(
            Request("OPTIONS", "z9hG4bK-server-repeat"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.RequestAbsorbed, Is.True);
            Assert.That(result.DeliveredRequests, Is.Empty);
            Assert.That(result.OutboundMessages, Has.Length.EqualTo(outboundCount));
            Assert.That(result.Retransmitted, Is.EqualTo(outboundCount > 0));
            if (outboundCount > 0)
            {
                Assert.That(result.OutboundMessages[0].StatusCode,
                    Is.EqualTo(responseStatus));
            }
        });
    }

    [TestCase(SipTransactionState.Trying)]
    [TestCase(SipTransactionState.Proceeding)]
    [TestCase(SipTransactionState.Completed)]
    public void ServerTransportFailureTerminatesEveryLiveState(
        SipTransactionState state)
    {
        SipNonInviteServerTransaction transaction = ServerAt(
            state, false, "z9hG4bK-server-error");

        SipNonInviteServerResult result =
            transaction.HandleTransportError("send failed");

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.TransportFailed, Is.True);
            Assert.That(result.ErrorMessage, Is.EqualTo("send failed"));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [Test]
    public void ServerTransportFailureAfterTerminationIsIdempotent()
    {
        SipNonInviteServerTransaction transaction = ServerAt(
            SipTransactionState.Terminated, false, "z9hG4bK-server-idempotent");

        SipNonInviteServerResult result =
            transaction.HandleTransportError("late failure");

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.TransportFailed, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }

    [TestCase(false, 6399, 6400)]
    [TestCase(true, 0, 0)]
    public void ServerTimerJHonorsReliableAndUnreliableBoundaries(
        bool reliable, long beforeTime, long expiryTime)
    {
        SipNonInviteServerTransaction transaction = Server(
            reliable, "BYE", "z9hG4bK-timer-j");
        transaction.StartTransaction();
        transaction.SendResponse(Response(200, "BYE", "z9hG4bK-timer-j"));

        SipNonInviteServerResult before = transaction.AdvanceTime(beforeTime);
        SipNonInviteServerResult at = transaction.AdvanceTime(expiryTime);

        Assert.Multiple(() =>
        {
            Assert.That(before.State,
                Is.EqualTo(reliable ? SipTransactionState.Terminated
                    : SipTransactionState.Completed));
            Assert.That(at.State, Is.EqualTo(SipTransactionState.Terminated));
            if (!reliable)
            {
                Assert.That(before.FiredTimerNames, Is.Empty);
                Assert.That(at.FiredTimerNames,
                    Is.EqualTo(new string[] { SipTimerNames.TimerJ }));
            }
        });
    }

    [Test]
    public void InvalidTransitionsAndWrongMethodsLeaveStateUnchanged()
    {
        SipNonInviteClientTransaction client = Client(
            false, "OPTIONS", "z9hG4bK-invalid-client");
        SipNonInviteServerTransaction server = Server(
            false, "OPTIONS", "z9hG4bK-invalid-server");

        Assert.Throws<InvalidOperationException>(() => client.ReceiveResponse(
            Response(100, "OPTIONS", "z9hG4bK-invalid-client")));
        Assert.Throws<InvalidOperationException>(() => server.ReceiveRequest(
            Request("OPTIONS", "z9hG4bK-invalid-server")));
        client.StartTransaction();
        server.StartTransaction();

        Assert.Throws<ArgumentException>(() => client.ReceiveResponse(
            Response(200, "BYE", "z9hG4bK-invalid-client")));
        Assert.Throws<ArgumentException>(() => server.ReceiveRequest(
            Request("BYE", "z9hG4bK-invalid-server")));
        Assert.Throws<ArgumentException>(() => server.SendResponse(
            Response(200, "BYE", "z9hG4bK-invalid-server")));

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSnapshot().State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(server.GetSnapshot().State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(client.GetSnapshot().Response, Is.Null);
            Assert.That(server.GetSnapshot().Response, Is.Null);
        });
    }

    static SipNonInviteClientTransaction Client(
        bool reliable, string method, string branch)
    {
        return new SipNonInviteClientTransaction("client-transaction",
            Request(method, branch), Options(reliable),
            new SipManualMonotonicClock(0));
    }

    static SipNonInviteClientTransaction ClientAt(
        SipTransactionState state, bool reliable, string branch)
    {
        SipNonInviteClientTransaction transaction =
            Client(reliable, "OPTIONS", branch);
        transaction.StartTransaction();
        if (state == SipTransactionState.Proceeding)
        {
            transaction.ReceiveResponse(Response(100, "OPTIONS", branch));
        }
        else if (state == SipTransactionState.Completed)
        {
            transaction.ReceiveResponse(Response(200, "OPTIONS", branch));
        }
        else if (state == SipTransactionState.Terminated)
        {
            transaction.HandleTransportError("setup failure");
        }
        return transaction;
    }

    static SipNonInviteServerTransaction Server(
        bool reliable, string method, string branch)
    {
        return new SipNonInviteServerTransaction("server-transaction",
            Request(method, branch), Options(reliable),
            new SipManualMonotonicClock(0));
    }

    static SipNonInviteServerTransaction ServerAt(
        SipTransactionState state, bool reliable, string branch)
    {
        SipNonInviteServerTransaction transaction =
            Server(reliable, "OPTIONS", branch);
        transaction.StartTransaction();
        if (state == SipTransactionState.Proceeding)
        {
            transaction.SendResponse(Response(100, "OPTIONS", branch));
        }
        else if (state == SipTransactionState.Completed
            || state == SipTransactionState.Terminated)
        {
            transaction.SendResponse(Response(486, "OPTIONS", branch));
        }
        if (state == SipTransactionState.Terminated)
        {
            transaction.AdvanceTime(reliable ? 0 : 6400);
        }
        return transaction;
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
        return Parse(method + " sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: <sip:service@example.com>\r\n"
            + "Call-ID: non-invite-matrix@example.com\r\n"
            + "CSeq: 42 " + method + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string method, string branch)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Result\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "From: <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: <sip:service@example.com>;tag=server-tag\r\n"
            + "Call-ID: non-invite-matrix@example.com\r\n"
            + "CSeq: 42 " + method + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}
