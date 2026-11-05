namespace sip.Tests;

public class SipInviteTransactionMatrixTests
{
    [TestCase(100, SipTransactionState.Proceeding, 1, 0)]
    [TestCase(199, SipTransactionState.Proceeding, 1, 0)]
    [TestCase(200, SipTransactionState.Accepted, 1, 0)]
    [TestCase(299, SipTransactionState.Accepted, 1, 0)]
    [TestCase(300, SipTransactionState.Completed, 1, 1)]
    [TestCase(699, SipTransactionState.Completed, 1, 1)]
    public void ClientCallingHandlesEveryResponseClassBoundary(int status,
        SipTransactionState expectedState, int deliveredCount, int outboundCount)
    {
        SipInviteClientTransaction transaction = Client(false, "z9hG4bK-calling");
        transaction.StartTransaction();

        SipInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "z9hG4bK-calling", "response-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.DeliveredResponses, Has.Length.EqualTo(deliveredCount));
            Assert.That(result.OutboundMessages, Has.Length.EqualTo(outboundCount));
            Assert.That(transaction.GetSnapshot().State, Is.EqualTo(expectedState));
            if (outboundCount > 0)
            {
                Assert.That(result.OutboundMessages[0].Method, Is.EqualTo(SipMethod.Ack));
            }
        });
    }

    [TestCase(100, SipTransactionState.Proceeding, 1, 0)]
    [TestCase(199, SipTransactionState.Proceeding, 1, 0)]
    [TestCase(200, SipTransactionState.Accepted, 1, 0)]
    [TestCase(299, SipTransactionState.Accepted, 1, 0)]
    [TestCase(300, SipTransactionState.Completed, 1, 1)]
    [TestCase(699, SipTransactionState.Completed, 1, 1)]
    public void ClientProceedingHandlesEveryResponseClassBoundary(int status,
        SipTransactionState expectedState, int deliveredCount, int outboundCount)
    {
        SipInviteClientTransaction transaction = Client(false, "z9hG4bK-proceeding");
        transaction.StartTransaction();
        transaction.ReceiveResponse(Response(180, "z9hG4bK-proceeding", "early-tag"));

        SipInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "z9hG4bK-proceeding", "response-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.StateChanged,
                Is.EqualTo(expectedState != SipTransactionState.Proceeding));
            Assert.That(result.DeliveredResponses, Has.Length.EqualTo(deliveredCount));
            Assert.That(result.OutboundMessages, Has.Length.EqualTo(outboundCount));
        });
    }

    [TestCase(100, 0, 0, false)]
    [TestCase(199, 0, 0, false)]
    [TestCase(200, 0, 0, false)]
    [TestCase(299, 0, 0, false)]
    [TestCase(300, 0, 1, true)]
    [TestCase(699, 0, 1, true)]
    public void ClientCompletedAbsorbsResponsesAndOnlyReacksFailures(int status,
        int deliveredCount, int outboundCount, bool retransmitted)
    {
        SipInviteClientTransaction transaction =
            ClientAt(SipTransactionState.Completed, false, "z9hG4bK-completed");

        SipInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "z9hG4bK-completed", "later-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.DeliveredResponses, Has.Length.EqualTo(deliveredCount));
            Assert.That(result.OutboundMessages, Has.Length.EqualTo(outboundCount));
            Assert.That(result.Retransmitted, Is.EqualTo(retransmitted));
            Assert.That(transaction.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerD }));
        });
    }

    [TestCase(100, 0)]
    [TestCase(199, 0)]
    [TestCase(200, 1)]
    [TestCase(299, 1)]
    [TestCase(300, 0)]
    [TestCase(699, 0)]
    public void ClientAcceptedOnlyDeliversAdditionalSuccessResponses(
        int status, int deliveredCount)
    {
        SipInviteClientTransaction transaction =
            ClientAt(SipTransactionState.Accepted, false, "z9hG4bK-accepted");
        long deadline = transaction.GetSnapshot().TimerDeadlines.Single();

        SipInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "z9hG4bK-accepted", "fork-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.DeliveredResponses, Has.Length.EqualTo(deliveredCount));
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
    public void ClientTerminatedIgnoresEveryValidResponseClass(int status)
    {
        SipInviteClientTransaction transaction =
            ClientAt(SipTransactionState.Terminated, false, "z9hG4bK-terminated");

        SipInviteClientResult result = transaction.ReceiveResponse(
            Response(status, "z9hG4bK-terminated", "late-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.DeliveredResponses, Is.Empty);
            Assert.That(result.OutboundMessages, Is.Empty);
            Assert.That(result.Retransmitted, Is.False);
        });
    }

    [TestCase(SipTransactionState.Calling)]
    [TestCase(SipTransactionState.Proceeding)]
    [TestCase(SipTransactionState.Completed)]
    [TestCase(SipTransactionState.Accepted)]
    public void ClientTransportFailureTerminatesEveryLiveState(
        SipTransactionState initialState)
    {
        SipInviteClientTransaction transaction = ClientAt(initialState, false,
            "z9hG4bK-client-error");

        SipInviteClientResult result = transaction.HandleTransportError("transport failed");

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
        SipInviteClientTransaction transaction =
            ClientAt(SipTransactionState.Terminated, false, "z9hG4bK-idempotent");

        SipInviteClientResult result = transaction.HandleTransportError("late failure");

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.TransportFailed, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void ClientUnreliableTimersHonorExactABAndDBoundaries()
    {
        SipInviteClientTransaction retransmitting = Client(false, "z9hG4bK-timer-a");
        retransmitting.StartTransaction();
        SipInviteClientResult beforeA = retransmitting.AdvanceTime(99);
        SipInviteClientResult atA = retransmitting.AdvanceTime(100);

        SipInviteClientTransaction timeout = Client(true, "z9hG4bK-timer-b");
        timeout.StartTransaction();
        SipInviteClientResult beforeB = timeout.AdvanceTime(6399);
        SipInviteClientResult atB = timeout.AdvanceTime(6400);

        SipInviteClientTransaction completed = Client(false, "z9hG4bK-timer-d");
        completed.StartTransaction();
        completed.ReceiveResponse(Response(486, "z9hG4bK-timer-d", "final-tag"));
        SipInviteClientResult beforeD = completed.AdvanceTime(31999);
        SipInviteClientResult atD = completed.AdvanceTime(32000);

        Assert.Multiple(() =>
        {
            Assert.That(beforeA.FiredTimerNames, Is.Empty);
            Assert.That(atA.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerA }));
            Assert.That(atA.Retransmitted, Is.True);
            Assert.That(beforeB.State, Is.EqualTo(SipTransactionState.Calling));
            Assert.That(beforeB.FiredTimerNames, Is.Empty);
            Assert.That(atB.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(atB.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerB }));
            Assert.That(beforeD.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(beforeD.FiredTimerNames, Is.Empty);
            Assert.That(atD.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(atD.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerD }));
        });
    }

    [Test]
    public void ClientReliableModeSuppressesRequestAndFailureRetransmissionTimers()
    {
        SipInviteClientTransaction calling = Client(true, "z9hG4bK-reliable-calling");
        calling.StartTransaction();
        SipInviteClientResult noTimerA = calling.AdvanceTime(100);

        SipInviteClientTransaction completed = Client(true, "z9hG4bK-reliable-completed");
        completed.StartTransaction();
        completed.ReceiveResponse(
            Response(486, "z9hG4bK-reliable-completed", "final-tag"));
        SipTransactionSnapshot snapshot = completed.GetSnapshot();
        SipInviteClientResult atTimerD = completed.AdvanceTime(0);

        Assert.Multiple(() =>
        {
            Assert.That(noTimerA.FiredTimerNames, Is.Empty);
            Assert.That(noTimerA.OutboundMessages, Is.Empty);
            Assert.That(calling.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerB }));
            Assert.That(snapshot.TimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerD }));
            Assert.That(snapshot.TimerDeadlines, Is.EqualTo(new long[] { 0 }));
            Assert.That(atTimerD.State, Is.EqualTo(SipTransactionState.Terminated));
        });
    }

    [TestCase(100, SipTransactionState.Proceeding, "")]
    [TestCase(199, SipTransactionState.Proceeding, "")]
    [TestCase(200, SipTransactionState.Accepted, "L")]
    [TestCase(299, SipTransactionState.Accepted, "L")]
    [TestCase(300, SipTransactionState.Completed, "GH")]
    [TestCase(699, SipTransactionState.Completed, "GH")]
    public void ServerProceedingHandlesEveryResponseClassBoundary(int status,
        SipTransactionState expectedState, string expectedTimers)
    {
        SipInviteServerTransaction transaction = Server(false, "z9hG4bK-server-response");
        transaction.StartTransaction();

        SipInviteServerResult result = transaction.SendResponse(
            Response(status, "z9hG4bK-server-response", "server-tag"));
        SipTransactionSnapshot snapshot = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.StateChanged,
                Is.EqualTo(status >= 200));
            Assert.That(result.OutboundMessages, Has.Length.EqualTo(1));
            Assert.That(result.OutboundMessages[0].StatusCode, Is.EqualTo(status));
            Assert.That(snapshot.TimerNames,
                Is.EqualTo(ExpectedServerTimers(expectedTimers)));
        });
    }

    [TestCase(SipTransactionState.Completed, 180)]
    [TestCase(SipTransactionState.Completed, 200)]
    [TestCase(SipTransactionState.Completed, 486)]
    [TestCase(SipTransactionState.Confirmed, 180)]
    [TestCase(SipTransactionState.Confirmed, 200)]
    [TestCase(SipTransactionState.Confirmed, 486)]
    [TestCase(SipTransactionState.Terminated, 180)]
    [TestCase(SipTransactionState.Terminated, 200)]
    [TestCase(SipTransactionState.Terminated, 486)]
    public void ServerDiscardsEveryResponseClassOutsideProceedingAndAccepted(
        SipTransactionState state, int status)
    {
        SipInviteServerTransaction transaction =
            ServerAt(state, false, "z9hG4bK-server-discard");

        SipInviteServerResult result = transaction.SendResponse(
            Response(status, "z9hG4bK-server-discard", "later-tag"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.ResponseDiscarded, Is.True);
            Assert.That(result.OutboundMessages, Is.Empty);
        });
    }

    [TestCase(SipTransactionState.Proceeding, 1, 100)]
    [TestCase(SipTransactionState.Completed, 1, 486)]
    [TestCase(SipTransactionState.Confirmed, 0, 0)]
    [TestCase(SipTransactionState.Accepted, 0, 0)]
    [TestCase(SipTransactionState.Terminated, 0, 0)]
    public void ServerInviteRetransmissionBehaviorIsDefinedInEveryState(
        SipTransactionState state, int outboundCount, int replayStatus)
    {
        SipInviteServerTransaction transaction =
            ServerAt(state, false, "z9hG4bK-server-repeat");

        SipInviteServerResult result = transaction.ReceiveRequest(
            Invite("z9hG4bK-server-repeat"));

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
                Assert.That(result.OutboundMessages[0].StatusCode, Is.EqualTo(replayStatus));
            }
        });
    }

    [TestCase(SipTransactionState.Proceeding, SipTransactionState.Proceeding, false)]
    [TestCase(SipTransactionState.Completed, SipTransactionState.Confirmed, true)]
    [TestCase(SipTransactionState.Confirmed, SipTransactionState.Confirmed, false)]
    [TestCase(SipTransactionState.Terminated, SipTransactionState.Terminated, false)]
    public void ServerNonSuccessAckBehaviorIsDefinedInEveryApplicableState(
        SipTransactionState state, SipTransactionState expectedState, bool changed)
    {
        SipInviteServerTransaction transaction =
            ServerAt(state, false, "z9hG4bK-server-ack");

        SipInviteServerResult result = transaction.ReceiveRequest(
            Ack("z9hG4bK-server-ack", "final-tag", "client.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.StateChanged, Is.EqualTo(changed));
            Assert.That(result.AckAbsorbed, Is.True);
            Assert.That(result.AckDelivered, Is.False);
            Assert.That(result.OutboundMessages, Is.Empty);
        });
    }

    [Test]
    public void MagicCookieFailureAckMatchesBranchAndSentByRatherThanToTag()
    {
        SipInviteServerTransaction transaction =
            ServerAt(SipTransactionState.Completed, false, "z9hG4bK-ack-identity");

        SipInviteServerResult differentTag = transaction.ReceiveRequest(
            Ack("z9hG4bK-ack-identity", "different-tag", "client.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(differentTag.State, Is.EqualTo(SipTransactionState.Confirmed));
            Assert.Throws<ArgumentException>(() => ServerAt(
                SipTransactionState.Completed, false, "z9hG4bK-wrong-branch")
                .ReceiveRequest(Ack("z9hG4bK-other", "final-tag", "client.example.com")));
            Assert.Throws<ArgumentException>(() => ServerAt(
                SipTransactionState.Completed, false, "z9hG4bK-wrong-sentby")
                .ReceiveRequest(Ack("z9hG4bK-wrong-sentby", "final-tag", "other.example.com")));
        });
    }

    [TestCase(SipTransactionState.Proceeding, true)]
    [TestCase(SipTransactionState.Completed, true)]
    [TestCase(SipTransactionState.Confirmed, true)]
    [TestCase(SipTransactionState.Accepted, true)]
    [TestCase(SipTransactionState.Terminated, false)]
    public void ServerTransportFailureIsReportedWithoutChangingAnyState(
        SipTransactionState state, bool reported)
    {
        SipInviteServerTransaction transaction =
            ServerAt(state, false, "z9hG4bK-server-error");
        string[] timerNames = transaction.GetSnapshot().TimerNames;

        SipInviteServerResult result = transaction.HandleTransportError("send failed");

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.TransportFailed, Is.EqualTo(reported));
            Assert.That(result.ErrorMessage,
                reported ? Is.EqualTo("send failed") : Is.Null);
            Assert.That(transaction.GetSnapshot().TimerNames, Is.EqualTo(timerNames));
        });
    }

    [Test]
    public void ServerTimerGRetransmissionUsesExactBoundariesAndCapsAtT2()
    {
        SipInviteServerTransaction transaction = Server(false, "z9hG4bK-timer-g");
        transaction.StartTransaction();
        transaction.SendResponse(Response(486, "z9hG4bK-timer-g", "final-tag"));

        SipInviteServerResult beforeFirst = transaction.AdvanceTime(99);
        SipInviteServerResult first = transaction.AdvanceTime(100);
        SipTransactionSnapshot afterFirst = transaction.GetSnapshot();
        SipInviteServerResult beforeSecond = transaction.AdvanceTime(299);
        SipInviteServerResult second = transaction.AdvanceTime(300);
        transaction.AdvanceTime(700);
        SipTransactionSnapshot capped = transaction.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(beforeFirst.FiredTimerNames, Is.Empty);
            Assert.That(first.FiredTimerNames, Is.EqualTo(new string[] { SipTimerNames.TimerG }));
            Assert.That(first.OutboundMessages.Single().StatusCode, Is.EqualTo(486));
            Assert.That(afterFirst.TimerDeadlines[0], Is.EqualTo(300));
            Assert.That(beforeSecond.FiredTimerNames, Is.Empty);
            Assert.That(second.Retransmitted, Is.True);
            Assert.That(capped.TimerDeadlines[0], Is.EqualTo(1100));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ServerTimerHUsesExactBoundaryForBothTransportModes(bool reliable)
    {
        SipInviteServerTransaction transaction = Server(reliable, "z9hG4bK-timer-h");
        transaction.StartTransaction();
        transaction.SendResponse(Response(486, "z9hG4bK-timer-h", "final-tag"));

        SipInviteServerResult before = transaction.AdvanceTime(6399);
        SipInviteServerResult at = transaction.AdvanceTime(6400);

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(before.FiredTimerNames,
                reliable ? Is.Empty : Does.Contain(SipTimerNames.TimerG));
            Assert.That(at.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(at.TimedOut, Is.True);
            Assert.That(at.FiredTimerNames, Does.Contain(SipTimerNames.TimerH));
            Assert.That(transaction.GetSnapshot().TimerNames, Is.Empty);
        });
    }

    [TestCase(false, 499, 500)]
    [TestCase(true, 0, 0)]
    public void ServerTimerIHonorsReliableAndUnreliableBoundaries(
        bool reliable, long beforeTime, long expiryTime)
    {
        SipInviteServerTransaction transaction = Server(reliable, "z9hG4bK-timer-i");
        transaction.StartTransaction();
        transaction.SendResponse(Response(486, "z9hG4bK-timer-i", "final-tag"));
        transaction.ReceiveRequest(
            Ack("z9hG4bK-timer-i", "final-tag", "client.example.com"));

        SipInviteServerResult before = transaction.AdvanceTime(beforeTime);
        SipInviteServerResult at = transaction.AdvanceTime(expiryTime);

        Assert.Multiple(() =>
        {
            Assert.That(before.State,
                Is.EqualTo(reliable ? SipTransactionState.Terminated
                    : SipTransactionState.Confirmed));
            Assert.That(at.State, Is.EqualTo(SipTransactionState.Terminated));
            if (!reliable)
            {
                Assert.That(before.FiredTimerNames, Is.Empty);
                Assert.That(at.FiredTimerNames,
                    Is.EqualTo(new string[] { SipTimerNames.TimerI }));
            }
        });
    }

    [Test]
    public void InvalidTransitionsDoNotMutateClientOrServerState()
    {
        SipInviteClientTransaction client = Client(false, "z9hG4bK-invalid-client");
        SipInviteServerTransaction server = Server(false, "z9hG4bK-invalid-server");

        Assert.Throws<InvalidOperationException>(() => client.ReceiveResponse(
            Response(180, "z9hG4bK-invalid-client", "tag")));
        Assert.Throws<InvalidOperationException>(() => server.ReceiveRequest(
            Invite("z9hG4bK-invalid-server")));
        Assert.That(client.GetSnapshot().State, Is.EqualTo(SipTransactionState.None));
        Assert.That(server.GetSnapshot().State, Is.EqualTo(SipTransactionState.None));

        client.StartTransaction();
        server.StartTransaction();
        Assert.Throws<ArgumentException>(() => client.ReceiveResponse(
            Response(180, "z9hG4bK-other", "tag")));
        Assert.Throws<ArgumentException>(() => server.ReceiveRequest(
            Request("BYE", "z9hG4bK-invalid-server")));

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSnapshot().State, Is.EqualTo(SipTransactionState.Calling));
            Assert.That(server.GetSnapshot().State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(client.GetSnapshot().TimerNames,
                Is.EqualTo(new string[] { SipTimerNames.TimerA, SipTimerNames.TimerB }));
            Assert.That(server.GetSnapshot().Response!.StatusCode, Is.EqualTo(100));
        });
    }

    static SipInviteClientTransaction Client(bool reliable, string branch)
    {
        return new SipInviteClientTransaction("client-transaction", Invite(branch),
            Options(reliable), new SipManualMonotonicClock(0));
    }

    static SipInviteClientTransaction ClientAt(SipTransactionState state,
        bool reliable, string branch)
    {
        SipInviteClientTransaction transaction = Client(reliable, branch);
        transaction.StartTransaction();
        if (state == SipTransactionState.Proceeding)
        {
            transaction.ReceiveResponse(Response(180, branch, "early-tag"));
        }
        else if (state == SipTransactionState.Completed)
        {
            transaction.ReceiveResponse(Response(486, branch, "final-tag"));
        }
        else if (state == SipTransactionState.Accepted)
        {
            transaction.ReceiveResponse(Response(200, branch, "success-tag"));
        }
        else if (state == SipTransactionState.Terminated)
        {
            transaction.HandleTransportError("setup failure");
        }
        return transaction;
    }

    static SipInviteServerTransaction Server(bool reliable, string branch)
    {
        return new SipInviteServerTransaction("server-transaction", Invite(branch),
            Options(reliable), new SipManualMonotonicClock(0));
    }

    static SipInviteServerTransaction ServerAt(SipTransactionState state,
        bool reliable, string branch)
    {
        SipInviteServerTransaction transaction = Server(reliable, branch);
        transaction.StartTransaction();
        if (state == SipTransactionState.Completed
            || state == SipTransactionState.Confirmed
            || state == SipTransactionState.Terminated)
        {
            transaction.SendResponse(Response(486, branch, "final-tag"));
        }
        if (state == SipTransactionState.Confirmed
            || state == SipTransactionState.Terminated)
        {
            transaction.ReceiveRequest(Ack(branch, "final-tag", "client.example.com"));
        }
        if (state == SipTransactionState.Terminated)
        {
            transaction.AdvanceTime(reliable ? 0 : 500);
        }
        if (state == SipTransactionState.Accepted)
        {
            transaction.SendResponse(Response(200, branch, "success-tag"));
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

    static string[] ExpectedServerTimers(string names)
    {
        if (names == "L")
        {
            return new string[] { SipTimerNames.TimerL };
        }
        if (names == "GH")
        {
            return new string[] { SipTimerNames.TimerG, SipTimerNames.TimerH };
        }
        return new string[0];
    }

    static SipMessage Invite(string branch)
    {
        return Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: matrix-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Request(string method, string branch)
    {
        return Parse(method + " sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: matrix-call@example.com\r\n"
            + "CSeq: 42 " + method + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Ack(string branch, string toTag, string sentBy)
    {
        return Parse("ACK sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP " + sentBy + ":5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: <sip:bob@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: matrix-call@example.com\r\n"
            + "CSeq: 42 ACK\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string branch, string toTag)
    {
        string contact = status >= 200 && status < 300
            ? "Contact: <sip:bob@server.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Result\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "From: <sip:alice@example.com>;tag=caller-tag\r\n"
            + "To: <sip:bob@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: matrix-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + contact
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
