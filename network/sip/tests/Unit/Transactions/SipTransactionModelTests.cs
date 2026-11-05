namespace sip.Tests;

public class SipTransactionModelTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void GeneratedInviteClientTracesMatchReferenceModel(bool reliable)
    {
        ModelAction[] actions = new ModelAction[]
        {
            ModelAction.Provisional,
            ModelAction.Success,
            ModelAction.Failure,
            ModelAction.Expire,
            ModelAction.TransportFailure,
            ModelAction.Invalid
        };
        foreach (ModelAction[] sequence in Sequences(actions, 3))
        {
            SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
            SipInviteClientTransaction transaction = new SipInviteClientTransaction(
                "invite-client-model", Request("INVITE", "z9hG4bK-invite-client", null),
                Options(reliable), clock);
            transaction.StartTransaction();
            SipTransactionState expected = SipTransactionState.Calling;
            long now = 0;

            for (int step = 0; step < sequence.Length; step++)
            {
                ModelAction action = sequence[step];
                SipTransactionState before = expected;
                int expectedDeliveries = 0;
                SipInviteClientResult? result = null;
                if (action == ModelAction.Provisional)
                {
                    if (expected == SipTransactionState.Calling
                        || expected == SipTransactionState.Proceeding)
                    {
                        expectedDeliveries = 1;
                        expected = SipTransactionState.Proceeding;
                    }
                    result = transaction.ReceiveResponse(Response(180, "INVITE",
                        "z9hG4bK-invite-client", "model-tag"));
                }
                else if (action == ModelAction.Success)
                {
                    if (expected == SipTransactionState.Calling
                        || expected == SipTransactionState.Proceeding)
                    {
                        expected = SipTransactionState.Accepted;
                        expectedDeliveries = 1;
                    }
                    else if (expected == SipTransactionState.Accepted)
                    {
                        expectedDeliveries = 1;
                    }
                    result = transaction.ReceiveResponse(Response(200, "INVITE",
                        "z9hG4bK-invite-client", "model-tag"));
                }
                else if (action == ModelAction.Failure)
                {
                    if (expected == SipTransactionState.Calling
                        || expected == SipTransactionState.Proceeding)
                    {
                        expected = SipTransactionState.Completed;
                        expectedDeliveries = 1;
                    }
                    result = transaction.ReceiveResponse(Response(486, "INVITE",
                        "z9hG4bK-invite-client", "model-tag"));
                }
                else if (action == ModelAction.Expire)
                {
                    now = now + 40000;
                    if (expected == SipTransactionState.Calling
                        || expected == SipTransactionState.Completed
                        || expected == SipTransactionState.Accepted)
                    {
                        expected = SipTransactionState.Terminated;
                    }
                    result = transaction.AdvanceTime(now);
                }
                else if (action == ModelAction.TransportFailure)
                {
                    if (expected != SipTransactionState.Terminated)
                    {
                        expected = SipTransactionState.Terminated;
                    }
                    result = transaction.HandleTransportError("model transport failure");
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => transaction.ReceiveResponse(
                        Response(180, "INVITE", "z9hG4bK-wrong-client", "model-tag")),
                        Trace(sequence, step));
                }

                SipTransactionSnapshot snapshot = transaction.GetSnapshot();
                Assert.That(snapshot.State, Is.EqualTo(expected), Trace(sequence, step));
                if (result != null)
                {
                    Assert.That(result.State, Is.EqualTo(expected), Trace(sequence, step));
                    Assert.That(result.StateChanged, Is.EqualTo(before != expected),
                        Trace(sequence, step));
                    Assert.That(result.DeliveredResponses, Has.Length.EqualTo(expectedDeliveries),
                        Trace(sequence, step));
                }
                AssertTerminalCleanup(snapshot, sequence, step);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GeneratedNonInviteClientTracesMatchReferenceModel(bool reliable)
    {
        ModelAction[] actions = new ModelAction[]
        {
            ModelAction.Provisional,
            ModelAction.Success,
            ModelAction.Failure,
            ModelAction.Expire,
            ModelAction.TransportFailure,
            ModelAction.Invalid
        };
        foreach (ModelAction[] sequence in Sequences(actions, 3))
        {
            SipNonInviteClientTransaction transaction = new SipNonInviteClientTransaction(
                "non-invite-client-model",
                Request("OPTIONS", "z9hG4bK-non-invite-client", null),
                Options(reliable), new SipManualMonotonicClock(0));
            transaction.StartTransaction();
            SipTransactionState expected = SipTransactionState.Trying;
            long now = 0;

            for (int step = 0; step < sequence.Length; step++)
            {
                ModelAction action = sequence[step];
                SipTransactionState before = expected;
                int expectedDeliveries = 0;
                SipNonInviteClientResult? result = null;
                if (action == ModelAction.Provisional)
                {
                    if (expected == SipTransactionState.Trying
                        || expected == SipTransactionState.Proceeding)
                    {
                        expected = SipTransactionState.Proceeding;
                        expectedDeliveries = 1;
                    }
                    result = transaction.ReceiveResponse(Response(180, "OPTIONS",
                        "z9hG4bK-non-invite-client", "model-tag"));
                }
                else if (action == ModelAction.Success || action == ModelAction.Failure)
                {
                    if (expected == SipTransactionState.Trying
                        || expected == SipTransactionState.Proceeding)
                    {
                        expected = SipTransactionState.Completed;
                        expectedDeliveries = 1;
                    }
                    int status = action == ModelAction.Success ? 200 : 486;
                    result = transaction.ReceiveResponse(Response(status, "OPTIONS",
                        "z9hG4bK-non-invite-client", "model-tag"));
                }
                else if (action == ModelAction.Expire)
                {
                    now = now + 40000;
                    if (expected == SipTransactionState.Trying
                        || expected == SipTransactionState.Proceeding
                        || expected == SipTransactionState.Completed)
                    {
                        expected = SipTransactionState.Terminated;
                    }
                    result = transaction.AdvanceTime(now);
                }
                else if (action == ModelAction.TransportFailure)
                {
                    if (expected != SipTransactionState.Terminated)
                    {
                        expected = SipTransactionState.Terminated;
                    }
                    result = transaction.HandleTransportError("model transport failure");
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => transaction.ReceiveResponse(
                        Response(180, "OPTIONS", "z9hG4bK-wrong-client", "model-tag")),
                        Trace(sequence, step));
                }

                SipTransactionSnapshot snapshot = transaction.GetSnapshot();
                Assert.That(snapshot.State, Is.EqualTo(expected), Trace(sequence, step));
                if (result != null)
                {
                    Assert.That(result.State, Is.EqualTo(expected), Trace(sequence, step));
                    Assert.That(result.StateChanged, Is.EqualTo(before != expected),
                        Trace(sequence, step));
                    Assert.That(result.DeliveredResponses, Has.Length.EqualTo(expectedDeliveries),
                        Trace(sequence, step));
                }
                AssertTerminalCleanup(snapshot, sequence, step);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GeneratedInviteServerTracesMatchReferenceModel(bool reliable)
    {
        ModelAction[] actions = new ModelAction[]
        {
            ModelAction.Provisional,
            ModelAction.Success,
            ModelAction.Failure,
            ModelAction.DuplicateRequest,
            ModelAction.Ack,
            ModelAction.Expire,
            ModelAction.TransportFailure,
            ModelAction.Invalid
        };
        foreach (ModelAction[] sequence in Sequences(actions, 3))
        {
            SipInviteServerTransaction transaction = new SipInviteServerTransaction(
                "invite-server-model", Request("INVITE", "z9hG4bK-invite-server", null),
                Options(reliable), new SipManualMonotonicClock(0));
            SipInviteServerResult started = transaction.StartTransaction();
            Assert.That(started.DeliveredRequests, Has.Length.EqualTo(1), Trace(sequence, -1));
            SipTransactionState expected = SipTransactionState.Proceeding;
            long now = 0;

            for (int step = 0; step < sequence.Length; step++)
            {
                ModelAction action = sequence[step];
                SipTransactionState before = expected;
                int expectedDeliveries = 0;
                SipInviteServerResult? result = null;
                if (action == ModelAction.Provisional)
                {
                    result = transaction.SendResponse(Response(180, "INVITE",
                        "z9hG4bK-invite-server", "model-tag"));
                }
                else if (action == ModelAction.Success)
                {
                    if (expected == SipTransactionState.Proceeding)
                    {
                        expected = SipTransactionState.Accepted;
                    }
                    result = transaction.SendResponse(Response(200, "INVITE",
                        "z9hG4bK-invite-server", "model-tag"));
                }
                else if (action == ModelAction.Failure)
                {
                    if (expected == SipTransactionState.Proceeding)
                    {
                        expected = SipTransactionState.Completed;
                    }
                    result = transaction.SendResponse(Response(486, "INVITE",
                        "z9hG4bK-invite-server", "model-tag"));
                }
                else if (action == ModelAction.DuplicateRequest)
                {
                    result = transaction.ReceiveRequest(
                        Request("INVITE", "z9hG4bK-invite-server", null));
                }
                else if (action == ModelAction.Ack)
                {
                    if (expected == SipTransactionState.Accepted)
                    {
                        expectedDeliveries = 1;
                    }
                    else if (expected == SipTransactionState.Completed)
                    {
                        expected = SipTransactionState.Confirmed;
                    }
                    result = transaction.ReceiveRequest(
                        Request("ACK", "z9hG4bK-invite-server", "model-tag"));
                }
                else if (action == ModelAction.Expire)
                {
                    now = now + 40000;
                    if (expected == SipTransactionState.Accepted
                        || expected == SipTransactionState.Completed
                        || expected == SipTransactionState.Confirmed)
                    {
                        expected = SipTransactionState.Terminated;
                    }
                    result = transaction.AdvanceTime(now);
                }
                else if (action == ModelAction.TransportFailure)
                {
                    result = transaction.HandleTransportError("model transport failure");
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
                        Request("OPTIONS", "z9hG4bK-invite-server", null)),
                        Trace(sequence, step));
                }

                SipTransactionSnapshot snapshot = transaction.GetSnapshot();
                Assert.That(snapshot.State, Is.EqualTo(expected), Trace(sequence, step));
                if (result != null)
                {
                    Assert.That(result.State, Is.EqualTo(expected), Trace(sequence, step));
                    Assert.That(result.StateChanged, Is.EqualTo(before != expected),
                        Trace(sequence, step));
                    Assert.That(result.DeliveredRequests, Has.Length.EqualTo(expectedDeliveries),
                        Trace(sequence, step));
                    if (action == ModelAction.DuplicateRequest)
                    {
                        Assert.That(result.RequestAbsorbed, Is.True, Trace(sequence, step));
                    }
                }
                AssertTerminalCleanup(snapshot, sequence, step);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GeneratedNonInviteServerTracesMatchReferenceModel(bool reliable)
    {
        ModelAction[] actions = new ModelAction[]
        {
            ModelAction.Provisional,
            ModelAction.Success,
            ModelAction.Failure,
            ModelAction.DuplicateRequest,
            ModelAction.Expire,
            ModelAction.TransportFailure,
            ModelAction.Invalid
        };
        foreach (ModelAction[] sequence in Sequences(actions, 3))
        {
            SipNonInviteServerTransaction transaction = new SipNonInviteServerTransaction(
                "non-invite-server-model",
                Request("OPTIONS", "z9hG4bK-non-invite-server", null), Options(reliable),
                new SipManualMonotonicClock(0));
            SipNonInviteServerResult started = transaction.StartTransaction();
            Assert.That(started.DeliveredRequests, Has.Length.EqualTo(1), Trace(sequence, -1));
            SipTransactionState expected = SipTransactionState.Trying;
            long now = 0;

            for (int step = 0; step < sequence.Length; step++)
            {
                ModelAction action = sequence[step];
                SipTransactionState before = expected;
                SipNonInviteServerResult? result = null;
                if (action == ModelAction.Provisional)
                {
                    if (expected == SipTransactionState.Trying)
                    {
                        expected = SipTransactionState.Proceeding;
                    }
                    result = transaction.SendResponse(Response(180, "OPTIONS",
                        "z9hG4bK-non-invite-server", "model-tag"));
                }
                else if (action == ModelAction.Success || action == ModelAction.Failure)
                {
                    if (expected == SipTransactionState.Trying
                        || expected == SipTransactionState.Proceeding)
                    {
                        expected = SipTransactionState.Completed;
                    }
                    int status = action == ModelAction.Success ? 200 : 486;
                    result = transaction.SendResponse(Response(status, "OPTIONS",
                        "z9hG4bK-non-invite-server", "model-tag"));
                }
                else if (action == ModelAction.DuplicateRequest)
                {
                    result = transaction.ReceiveRequest(
                        Request("OPTIONS", "z9hG4bK-non-invite-server", null));
                }
                else if (action == ModelAction.Expire)
                {
                    now = now + 40000;
                    if (expected == SipTransactionState.Completed)
                    {
                        expected = SipTransactionState.Terminated;
                    }
                    result = transaction.AdvanceTime(now);
                }
                else if (action == ModelAction.TransportFailure)
                {
                    if (expected != SipTransactionState.Terminated)
                    {
                        expected = SipTransactionState.Terminated;
                    }
                    result = transaction.HandleTransportError("model transport failure");
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => transaction.ReceiveRequest(
                        Request("INVITE", "z9hG4bK-non-invite-server", null)),
                        Trace(sequence, step));
                }

                SipTransactionSnapshot snapshot = transaction.GetSnapshot();
                Assert.That(snapshot.State, Is.EqualTo(expected), Trace(sequence, step));
                if (result != null)
                {
                    Assert.That(result.State, Is.EqualTo(expected), Trace(sequence, step));
                    Assert.That(result.StateChanged, Is.EqualTo(before != expected),
                        Trace(sequence, step));
                    Assert.That(result.DeliveredRequests, Is.Empty, Trace(sequence, step));
                    if (action == ModelAction.DuplicateRequest)
                    {
                        Assert.That(result.RequestAbsorbed, Is.True, Trace(sequence, step));
                    }
                }
                AssertTerminalCleanup(snapshot, sequence, step);
            }
        }
    }

    static IEnumerable<ModelAction[]> Sequences(ModelAction[] actions, int length)
    {
        int count = 1;
        for (int index = 0; index < length; index++)
        {
            count = count * actions.Length;
        }
        for (int encoded = 0; encoded < count; encoded++)
        {
            int remaining = encoded;
            ModelAction[] sequence = new ModelAction[length];
            for (int index = 0; index < length; index++)
            {
                sequence[index] = actions[remaining % actions.Length];
                remaining = remaining / actions.Length;
            }
            yield return sequence;
        }
    }

    static void AssertTerminalCleanup(SipTransactionSnapshot snapshot,
        ModelAction[] sequence, int step)
    {
        if (snapshot.State == SipTransactionState.Terminated)
        {
            Assert.That(snapshot.TimerNames, Is.Empty, Trace(sequence, step));
            Assert.That(snapshot.TimerDeadlines, Is.Empty, Trace(sequence, step));
        }
    }

    static string Trace(ModelAction[] sequence, int step)
    {
        return "Trace " + string.Join(",", sequence) + " at step "
            + step.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

    static SipMessage Request(string method, string branch, string? toTag)
    {
        string to = toTag == null ? "" : ";tag=" + toTag;
        string contact = method == "INVITE"
            ? "Contact: <sip:alice@client.example.com>\r\n" : "";
        return Parse(method + " sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>" + to + "\r\n"
            + "Call-ID: model-call@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + contact + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string method, string branch, string toTag)
    {
        string contact = method == "INVITE" && status >= 200 && status < 300
            ? "Contact: <sip:service@server.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " Model\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: model-call@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + contact + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }

    enum ModelAction
    {
        Provisional,
        Success,
        Failure,
        DuplicateRequest,
        Ack,
        Expire,
        TransportFailure,
        Invalid
    }
}
