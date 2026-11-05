namespace sip.Tests;

public class SipTransactionLayerTests
{
    [Test]
    public void ClientCreationQueuesCorrelatedDetachedEventAndSnapshot()
    {
        SipTransactionLayer layer = Layer();
        SipTransactionOptions options = Options(false);
        options.CorrelationId = "operation-one";
        SipMessage request = Request("OPTIONS", "z9hG4bK-create", null);

        SipTransactionSnapshot created = layer.CreateTransaction(request, options);
        request.RequestUri!.Host = "changed.example.com";
        SipTransactionEvent createdEvent = layer.PollTransactionEvent();
        SipTransactionEvent outboundEvent = layer.PollTransactionEvent();
        SipTransactionSnapshot queued = layer.PollTransactionSnapshot();
        created.Request!.RequestUri!.Host = "returned-change.example.com";
        createdEvent.Message = request;
        SipTransactionSnapshot live = layer.GetTransactionSnapshot(created.Id!);

        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Does.StartWith("tx-"));
            Assert.That(created.Role, Is.EqualTo(SipTransactionRole.Client));
            Assert.That(created.Kind, Is.EqualTo(SipTransactionKind.NonInvite));
            Assert.That(created.State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(created.CorrelationId, Is.EqualTo("operation-one"));
            Assert.That(createdEvent.Kind, Is.EqualTo(SipTransactionEventKind.Created));
            Assert.That(createdEvent.TransactionId, Is.EqualTo(created.Id));
            Assert.That(createdEvent.CorrelationId, Is.EqualTo("operation-one"));
            Assert.That(createdEvent.Reason, Is.EqualTo("Client transaction created."));
            Assert.That(outboundEvent.Kind,
                Is.EqualTo(SipTransactionEventKind.MessageDelivered));
            Assert.That(outboundEvent.Message!.Method, Is.EqualTo(SipMethod.Options));
            Assert.That(queued.Id, Is.EqualTo(created.Id));
            Assert.That(queued.CorrelationId, Is.EqualTo("operation-one"));
            Assert.That(queued.Request!.RequestUri!.Host, Is.EqualTo("service.example.com"));
            Assert.That(live.Request!.RequestUri!.Host, Is.EqualTo("service.example.com"));
            Assert.That(layer.PollTransactionEvent().Kind,
                Is.EqualTo(SipTransactionEventKind.None));
            Assert.That(layer.PollTransactionSnapshot().State,
                Is.EqualTo(SipTransactionState.None));
        });
    }

    [Test]
    public void MatchingClientResponseQueuesTransitionDeliveryAndSnapshotInOrder()
    {
        SipTransactionLayer layer = Layer();
        SipTransactionSnapshot created = layer.CreateTransaction(
            Request("OPTIONS", "z9hG4bK-response", null), Options(false));
        DrainEvents(layer);
        layer.PollTransactionSnapshot();
        SipTransportMessage response = Envelope(
            Response(180, "Ringing", "OPTIONS", "z9hG4bK-response", "remote-tag"),
            SipTransportKind.Udp, "incoming-correlation");

        SipTransactionSnapshot updated = layer.ReceiveTransactionMessage(response);
        SipTransactionEvent transition = layer.PollTransactionEvent();
        SipTransactionEvent delivery = layer.PollTransactionEvent();
        SipTransactionSnapshot queued = layer.PollTransactionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(updated.Id, Is.EqualTo(created.Id));
            Assert.That(updated.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(updated.Response!.StatusCode, Is.EqualTo(180));
            Assert.That(transition.Kind, Is.EqualTo(SipTransactionEventKind.StateChanged));
            Assert.That(transition.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(transition.Reason, Is.EqualTo("Provisional response received."));
            Assert.That(delivery.Kind, Is.EqualTo(SipTransactionEventKind.MessageDelivered));
            Assert.That(delivery.Message!.StatusCode, Is.EqualTo(180));
            Assert.That(delivery.TransactionId, Is.EqualTo(created.Id));
            Assert.That(delivery.CorrelationId, Is.EqualTo(created.CorrelationId));
            Assert.That(queued.State, Is.EqualTo(SipTransactionState.Proceeding));
        });
    }

    [Test]
    public void IncomingInviteResponseAckAndTimerRunOneServerLifecycle()
    {
        SipTransactionLayer layer = Layer();
        SipMessage invite = Request("INVITE", "z9hG4bK-server", null);

        SipTransactionSnapshot proceeding = layer.ReceiveTransactionMessage(
            Envelope(invite, SipTransportKind.Udp, "server-operation"));
        SipTransactionEvent created = layer.PollTransactionEvent();
        SipTransactionEvent trying = layer.PollTransactionEvent();
        SipTransactionEvent delivered = layer.PollTransactionEvent();
        SipMessage failure = Response(486, "Busy Here", "INVITE",
            "z9hG4bK-server", "server-tag");
        SipTransactionSnapshot completed =
            layer.SendTransactionResponse(proceeding.Id!, failure);
        SipTransactionEvent completedEvent = layer.PollTransactionEvent();
        SipTransactionEvent responseEvent = layer.PollTransactionEvent();
        SipTransactionSnapshot confirmed = layer.ReceiveTransactionMessage(
            Envelope(Request("ACK", "z9hG4bK-server", "server-tag"),
                SipTransportKind.Udp, null));
        SipTransactionEvent confirmedEvent = layer.PollTransactionEvent();

        layer.AdvanceTransactionTime(500);
        SipTransactionEvent timerEvent = layer.PollTransactionEvent();
        SipTransactionEvent terminatedEvent = layer.PollTransactionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(proceeding.Role, Is.EqualTo(SipTransactionRole.Server));
            Assert.That(proceeding.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(proceeding.CorrelationId, Is.EqualTo("server-operation"));
            Assert.That(created.Kind, Is.EqualTo(SipTransactionEventKind.Created));
            Assert.That(trying.Message!.StatusCode, Is.EqualTo(100));
            Assert.That(delivered.Message!.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(completed.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(completedEvent.Kind,
                Is.EqualTo(SipTransactionEventKind.StateChanged));
            Assert.That(completedEvent.Reason, Is.EqualTo("Final response sent."));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(486));
            Assert.That(confirmed.State, Is.EqualTo(SipTransactionState.Confirmed));
            Assert.That(confirmedEvent.Kind,
                Is.EqualTo(SipTransactionEventKind.StateChanged));
            Assert.That(confirmedEvent.Reason, Is.EqualTo("ACK received."));
            Assert.That(timerEvent.Kind, Is.EqualTo(SipTransactionEventKind.TimerFired));
            Assert.That(timerEvent.TimerName, Is.EqualTo(SipTimerNames.TimerI));
            Assert.That(terminatedEvent.Kind,
                Is.EqualTo(SipTransactionEventKind.Terminated));
            Assert.Throws<SipTransactionException>(() =>
                layer.GetTransactionSnapshot(proceeding.Id!));
        });
    }

    [Test]
    public void ServerRetransmissionIsMatchedToSameTransactionAndReplaysResponse()
    {
        SipTransactionLayer layer = Layer();
        SipMessage request = Request("OPTIONS", "z9hG4bK-replay", null);
        SipTransactionSnapshot trying = layer.ReceiveTransactionMessage(
            Envelope(request, SipTransportKind.Udp, null));
        DrainEvents(layer);
        layer.SendTransactionResponse(trying.Id!, Response(200, "OK", "OPTIONS",
            "z9hG4bK-replay", "server-tag"));
        DrainEvents(layer);

        SipTransactionSnapshot replayed = layer.ReceiveTransactionMessage(
            Envelope(request, SipTransportKind.Udp, null));
        SipTransactionEvent retransmission = layer.PollTransactionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(replayed.Id, Is.EqualTo(trying.Id));
            Assert.That(replayed.State, Is.EqualTo(SipTransactionState.Completed));
            Assert.That(retransmission.Kind,
                Is.EqualTo(SipTransactionEventKind.Retransmitted));
            Assert.That(retransmission.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(retransmission.Reason,
                Is.EqualTo("Request retransmission absorbed."));
            Assert.That(layer.PollTransactionEvent().Kind,
                Is.EqualTo(SipTransactionEventKind.None));
        });
    }

    [Test]
    public void LegacyRetransmissionIsRoutedToExistingServerTransaction()
    {
        SipTransactionOptions options = Options(false);
        options.AllowLegacyMatching = true;
        SipTransactionLayer layer = new SipTransactionLayer();
        layer.ConfigureTransactionSources(new SipManualMonotonicClock(0),
            new LayerRandomSource(), options);
        SipMessage request = Request("OPTIONS", "legacy-branch", null);
        SipTransactionSnapshot created = layer.ReceiveTransactionMessage(
            Envelope(request, SipTransportKind.Udp, null));
        DrainEvents(layer);
        DrainSnapshots(layer);

        SipTransactionSnapshot replayed = layer.ReceiveTransactionMessage(
            Envelope(request, SipTransportKind.Udp, null));
        SipTransactionEvent queuedEvent = layer.PollTransactionEvent();
        SipTransactionSnapshot queuedSnapshot = layer.PollTransactionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(replayed.Id, Is.EqualTo(created.Id));
            Assert.That(replayed.State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(queuedEvent.Kind, Is.EqualTo(SipTransactionEventKind.None));
            Assert.That(queuedSnapshot.Id, Is.EqualTo(created.Id));
            Assert.That(queuedSnapshot.State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(layer.PollTransactionSnapshot().State,
                Is.EqualTo(SipTransactionState.None));
        });
    }

    [Test]
    public void InviteCancellationWaitsForProvisionalThenQueuesCancel()
    {
        SipTransactionLayer layer = Layer();
        SipTransactionSnapshot calling = layer.CreateTransaction(
            Request("INVITE", "z9hG4bK-cancel", null), Options(false));
        DrainEvents(layer);
        layer.PollTransactionSnapshot();
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = "cancel-one";
        cancellation.Reason = "User requested cancellation.";

        layer.CancelTransaction(calling.Id!, cancellation);
        SipTransactionEvent pending = layer.PollTransactionEvent();
        SipTransactionSnapshot pendingSnapshot = layer.PollTransactionSnapshot();
        layer.ReceiveTransactionMessage(Envelope(
            Response(180, "Ringing", "INVITE", "z9hG4bK-cancel", "remote-tag"),
            SipTransportKind.Udp, null));
        SipTransactionEvent transition = layer.PollTransactionEvent();
        SipTransactionEvent cancel = layer.PollTransactionEvent();
        SipTransactionEvent response = layer.PollTransactionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(pending.Kind, Is.EqualTo(SipTransactionEventKind.StateChanged));
            Assert.That(pending.State, Is.EqualTo(SipTransactionState.Calling));
            Assert.That(pending.Reason, Is.EqualTo("User requested cancellation."));
            Assert.That(pendingSnapshot.State, Is.EqualTo(SipTransactionState.Calling));
            Assert.That(transition.State, Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(cancel.Kind, Is.EqualTo(SipTransactionEventKind.MessageDelivered));
            Assert.That(cancel.Message!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancel.Message.ViaValues[0].Branch, Is.EqualTo("z9hG4bK-cancel"));
            Assert.That(response.Message!.StatusCode, Is.EqualTo(180));
        });
    }

    [Test]
    public void TransportErrorAndExplicitCancellationProduceTerminalCleanup()
    {
        SipTransactionLayer layer = Layer();
        SipTransactionSnapshot failed = layer.CreateTransaction(
            Request("OPTIONS", "z9hG4bK-error", null), Options(false));
        SipTransactionSnapshot cancelled = layer.CreateTransaction(
            Request("MESSAGE", "z9hG4bK-cancel-local", null), Options(false));
        DrainEvents(layer);
        DrainSnapshots(layer);

        layer.ReportTransactionTransportError(failed.Id!, "network unavailable");
        SipTransactionEvent transport = layer.PollTransactionEvent();
        SipTransactionEvent transportTermination = layer.PollTransactionEvent();
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Reason = "Application stopped the request.";
        layer.CancelTransaction(cancelled.Id!, cancellation);
        SipTransactionEvent cancelledEvent = layer.PollTransactionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(transport.Kind, Is.EqualTo(SipTransactionEventKind.TransportError));
            Assert.That(transport.Reason, Is.EqualTo("network unavailable"));
            Assert.That(transportTermination.Kind,
                Is.EqualTo(SipTransactionEventKind.Terminated));
            Assert.That(cancelledEvent.Kind,
                Is.EqualTo(SipTransactionEventKind.Terminated));
            Assert.That(cancelledEvent.Reason,
                Is.EqualTo("Application stopped the request."));
            Assert.Throws<SipTransactionException>(() =>
                layer.GetTransactionSnapshot(failed.Id!));
            Assert.Throws<SipTransactionException>(() =>
                layer.GetTransactionSnapshot(cancelled.Id!));
        });
    }

    [Test]
    public void ExplicitTerminationQueuesFinalSnapshotAndRemovesLiveEntry()
    {
        SipTransactionLayer layer = Layer();
        SipTransactionSnapshot created = layer.CreateTransaction(
            Request("INVITE", "z9hG4bK-terminate", null), Options(false));
        DrainEvents(layer);
        DrainSnapshots(layer);
        SipCancellation termination = new SipCancellation();
        termination.Reason = "Layer shutdown.";

        layer.TerminateTransaction(created.Id!, termination);
        SipTransactionEvent terminated = layer.PollTransactionEvent();
        SipTransactionSnapshot final = layer.PollTransactionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(terminated.Kind, Is.EqualTo(SipTransactionEventKind.Terminated));
            Assert.That(terminated.Reason, Is.EqualTo("Layer shutdown."));
            Assert.That(final.Id, Is.EqualTo(created.Id));
            Assert.That(final.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(final.TimerNames, Is.Empty);
            Assert.That(final.TimerDeadlines, Is.Empty);
            Assert.Throws<SipTransactionException>(() =>
                layer.TerminateTransaction(created.Id!, termination));
        });
    }

    [Test]
    public void MergedAndCapacityRejectionsAreAtomicAndCleanupPreservesQueueOrder()
    {
        SipTransactionOptions options = Options(false);
        options.MaxTransactionCount = 1;
        SipTransactionLayer layer = new SipTransactionLayer();
        layer.ConfigureTransactionSources(new SipManualMonotonicClock(0),
            new LayerRandomSource(), options);
        SipTransactionSnapshot original = layer.ReceiveTransactionMessage(
            Envelope(Request("OPTIONS", "z9hG4bK-original-branch", null),
                SipTransportKind.Udp, "original-operation"));
        DrainEvents(layer);
        DrainSnapshots(layer);

        SipTransactionException merged = Assert.Throws<SipTransactionException>(() =>
            layer.ReceiveTransactionMessage(Envelope(
                Request("OPTIONS", "z9hG4bK-forked-branch", null),
                SipTransportKind.Udp, "merged-operation")))!;
        SipTransactionException capacity = Assert.Throws<SipTransactionException>(() =>
            layer.ReceiveTransactionMessage(Envelope(
                Request("MESSAGE", "z9hG4bK-unrelated-branch", null),
                SipTransportKind.Udp, "capacity-operation")))!;
        SipCancellation termination = new SipCancellation();
        termination.Reason = "Original completed.";
        layer.TerminateTransaction(original.Id!, termination);
        SipTransactionSnapshot replacement = layer.ReceiveTransactionMessage(
            Envelope(Request("OPTIONS", "z9hG4bK-forked-branch", null),
                SipTransportKind.Udp, "replacement-operation"));

        SipTransactionEvent terminated = layer.PollTransactionEvent();
        SipTransactionEvent created = layer.PollTransactionEvent();
        SipTransactionEvent delivered = layer.PollTransactionEvent();
        SipTransactionSnapshot finalSnapshot = layer.PollTransactionSnapshot();
        SipTransactionSnapshot replacementSnapshot = layer.PollTransactionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(merged.Message, Does.Contain("merged"));
            Assert.That(capacity.Message, Does.Contain("limit"));
            Assert.That(replacement.Id, Is.Not.EqualTo(original.Id));
            Assert.That(terminated.Kind, Is.EqualTo(SipTransactionEventKind.Terminated));
            Assert.That(terminated.TransactionId, Is.EqualTo(original.Id));
            Assert.That(created.Kind, Is.EqualTo(SipTransactionEventKind.Created));
            Assert.That(created.TransactionId, Is.EqualTo(replacement.Id));
            Assert.That(delivered.Kind, Is.EqualTo(SipTransactionEventKind.MessageDelivered));
            Assert.That(delivered.Message!.Method, Is.EqualTo(SipMethod.Options));
            Assert.That(finalSnapshot.Id, Is.EqualTo(original.Id));
            Assert.That(finalSnapshot.State, Is.EqualTo(SipTransactionState.Terminated));
            Assert.That(replacementSnapshot.Id, Is.EqualTo(replacement.Id));
            Assert.That(replacementSnapshot.State, Is.EqualTo(SipTransactionState.Trying));
            Assert.That(layer.PollTransactionEvent().Kind,
                Is.EqualTo(SipTransactionEventKind.None));
            Assert.That(layer.PollTransactionSnapshot().State,
                Is.EqualTo(SipTransactionState.None));
        });
    }

    [Test]
    public void TimerAdvanceQueuesRetransmissionAndRejectsBackwardTime()
    {
        SipTransactionLayer layer = Layer();
        SipTransactionSnapshot created = layer.CreateTransaction(
            Request("OPTIONS", "z9hG4bK-timer", null), Options(false));
        DrainEvents(layer);
        DrainSnapshots(layer);

        layer.AdvanceTransactionTime(100);
        SipTransactionEvent timer = layer.PollTransactionEvent();
        SipTransactionEvent retransmission = layer.PollTransactionEvent();
        SipTransactionSnapshot queued = layer.PollTransactionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(timer.Kind, Is.EqualTo(SipTransactionEventKind.TimerFired));
            Assert.That(timer.TimerName, Is.EqualTo(SipTimerNames.TimerE));
            Assert.That(retransmission.Kind,
                Is.EqualTo(SipTransactionEventKind.Retransmitted));
            Assert.That(retransmission.Message!.Method, Is.EqualTo(SipMethod.Options));
            Assert.That(queued.Id, Is.EqualTo(created.Id));
            Assert.That(queued.UpdatedAtMilliseconds, Is.EqualTo(100));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layer.AdvanceTransactionTime(99));
        });
    }

    [Test]
    public void LimitsAndUnmatchedMessagesFailWithoutCreatingTransactions()
    {
        SipTransactionLayer layer = Layer();
        SipTransactionOptions oneEvent = Options(false);
        oneEvent.MaxEventCount = 1;
        Assert.Throws<SipTransactionException>(() => layer.CreateTransaction(
            Request("OPTIONS", "z9hG4bK-small", null), oneEvent));

        SipTransactionOptions oneTransaction = Options(false);
        oneTransaction.MaxTransactionCount = 1;
        SipTransactionSnapshot first = layer.CreateTransaction(
            Request("OPTIONS", "z9hG4bK-first", null), oneTransaction);

        Assert.Multiple(() =>
        {
            Assert.Throws<SipTransactionException>(() => layer.CreateTransaction(
                Request("MESSAGE", "z9hG4bK-second", null), oneTransaction));
            Assert.Throws<SipTransactionException>(() => layer.ReceiveTransactionMessage(
                Envelope(Response(200, "OK", "OPTIONS", "z9hG4bK-unmatched", "tag"),
                    SipTransportKind.Udp, null)));
            Assert.Throws<SipTransactionException>(() => layer.ReceiveTransactionMessage(
                Envelope(Request("ACK", "z9hG4bK-unmatched", "tag"),
                    SipTransportKind.Udp, null)));
            Assert.That(layer.GetTransactionSnapshot(first.Id!).Id, Is.EqualTo(first.Id));
        });
    }

    [Test]
    public void ConcurrentCreationRetainsUniqueOwnershipAndQueueEntries()
    {
        SipTransactionLayer layer = Layer();
        SipTransactionSnapshot[] results = new SipTransactionSnapshot[16];

        System.Threading.Tasks.Parallel.For(0, results.Length, index =>
        {
            results[index] = layer.CreateTransaction(Request("OPTIONS",
                "z9hG4bK-thread-" + index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture), null), Options(false));
        });

        string[] identifiers = results.Select(result => result.Id).ToArray()!;
        Assert.Multiple(() =>
        {
            Assert.That(identifiers.Distinct().Count(), Is.EqualTo(results.Length));
            Assert.That(results.All(result => result.State == SipTransactionState.Trying),
                Is.True);
            Assert.That(DrainEvents(layer), Is.EqualTo(results.Length * 2));
            Assert.That(DrainSnapshots(layer), Is.EqualTo(results.Length));
        });
    }

    static SipTransactionLayer Layer()
    {
        SipTransactionLayer layer = new SipTransactionLayer();
        layer.ConfigureTransactionSources(new SipManualMonotonicClock(0),
            new LayerRandomSource(), Options(false));
        return layer;
    }

    static SipTransactionOptions Options(bool reliable)
    {
        SipTransactionOptions options = new SipTransactionOptions();
        options.ReliableTransport = reliable;
        options.Timers.T1Milliseconds = 100;
        options.Timers.T2Milliseconds = 400;
        options.Timers.T4Milliseconds = 500;
        options.Timers.MaxTimerCount = 8;
        options.MaxTransactionCount = 100;
        options.MaxEventCount = 256;
        return options;
    }

    static SipTransportMessage Envelope(SipMessage message,
        SipTransportKind transport, string? correlationId)
    {
        SipTransportMessage envelope = new SipTransportMessage();
        envelope.Message = message;
        envelope.Transport = transport;
        envelope.CorrelationId = correlationId;
        return envelope;
    }

    static SipMessage Request(string method, string branch, string? toTag)
    {
        string to = toTag == null
            ? "To: <sip:service@example.com>\r\n"
            : "To: <sip:service@example.com>;tag=" + toTag + "\r\n";
        string contact = method == "INVITE"
            ? "Contact: <sip:alice@client.example.com>\r\n" : "";
        return Parse(method + " sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + to
            + "Call-ID: layer-call@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + contact
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(int status, string reason, string method,
        string branch, string toTag)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: layer-call@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }

    static int DrainEvents(SipTransactionLayer layer)
    {
        int count = 0;
        SipTransactionEvent value = layer.PollTransactionEvent();
        while (value.Kind != SipTransactionEventKind.None)
        {
            count = count + 1;
            value = layer.PollTransactionEvent();
        }
        return count;
    }

    static int DrainSnapshots(SipTransactionLayer layer)
    {
        int count = 0;
        SipTransactionSnapshot value = layer.PollTransactionSnapshot();
        while (value.State != SipTransactionState.None)
        {
            count = count + 1;
            value = layer.PollTransactionSnapshot();
        }
        return count;
    }
}

class LayerRandomSource : ISipRandomSource
{
    byte next = 1;

    public void FillBytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = next;
            index = index + 1;
        }
        next = (byte)(next + 1);
    }
}
