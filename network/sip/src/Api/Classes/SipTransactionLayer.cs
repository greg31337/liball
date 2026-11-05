namespace sip;

/// <summary>Owns client and server transaction state, queues, matching, and lifecycle.</summary>
public class SipTransactionLayer : ISipTransactionLayer
{
    object gate = new object();
    System.Collections.Generic.List<SipTransactionLayerEntry> entries =
        new System.Collections.Generic.List<SipTransactionLayerEntry>();
    System.Collections.Generic.List<SipTransactionEvent> events =
        new System.Collections.Generic.List<SipTransactionEvent>();
    System.Collections.Generic.List<SipTransactionSnapshot> snapshots =
        new System.Collections.Generic.List<SipTransactionSnapshot>();
    SipTransactionOptions incomingOptions;
    SipTransactionLayerClock clock;
    SipCorrelationIdGenerator identifiers;

    /// <summary>Initializes an empty transaction layer.</summary>
    public SipTransactionLayer()
    {
        incomingOptions = new SipTransactionOptions();
        clock = new SipTransactionLayerClock(
            new SipSystemMonotonicClock().GetMilliseconds());
        identifiers = new SipCorrelationIdGenerator(
            new SipCryptographicRandomSource());
    }

    /// <summary>Creates and starts a client transaction.</summary>
    /// <param name="request">The initiating request.</param>
    /// <param name="options">The transaction options.</param>
    /// <returns>The initial transaction snapshot.</returns>
    public SipTransactionSnapshot CreateTransaction(
        SipMessage request, SipTransactionOptions options)
    {
        lock (gate)
        {
            ValidateOptions(options);
            if (entries.Count >= options.MaxTransactionCount)
            {
                throw new SipTransactionException(
                    "The live transaction limit has been reached.",
                    null, SipTransactionState.None);
            }

            string id = CreateTransactionId();
            string correlationId = options.CorrelationId == null
                || options.CorrelationId.Length == 0 ? id : options.CorrelationId;
            SipTransactionLayerEntry entry = CreateClientEntry(
                id, correlationId, request, options);
            if (FindDuplicateClient(entry) != null)
            {
                throw new SipTransactionException(
                    "A matching client transaction already exists.",
                    null, SipTransactionState.None);
            }

            SipTransactionLayerOutcome outcome = StartEntry(entry);
            int eventCount = 1 + CountOutcomeEvents(outcome, true, false);
            EnsureQueueCapacity(entry.MaximumEventCount, eventCount, 1, id);
            entries.Add(entry);
            QueueEvent(entry, SipTransactionEventKind.Created,
                outcome.State, null, null, "Client transaction created.");
            return CommitOutcome(entry, outcome,
                "Client transaction started.", true, false);
        }
    }

    /// <summary>Processes an incoming request or response.</summary>
    /// <param name="message">The received transport message.</param>
    /// <returns>The updated transaction snapshot.</returns>
    public SipTransactionSnapshot ReceiveTransactionMessage(
        SipTransportMessage message)
    {
        lock (gate)
        {
            ValidateReceivedMessage(message);
            SipMessage? receivedValue = message.Message;
            if (receivedValue == null)
            {
                throw new System.ArgumentNullException("message");
            }
            SipMessage received = receivedValue;
            SipTransactionLayerEntry? entry;
            SipTransactionLayerOutcome outcome;
            string reason;
            if (received.Kind == SipMessageKind.Response)
            {
                entry = FindResponseEntry(received);
                if (entry == null)
                {
                    throw new SipTransactionException(
                        "The response does not match a live client transaction.",
                        null, SipTransactionState.None);
                }
                EnsureQueueCapacity(entry.MaximumEventCount, 3, 1, entry.Id);
                outcome = ReceiveClientResponse(entry, received);
                reason = DescribeResponse(received);
                return CommitOutcome(entry, outcome, reason, false, false);
            }

            entry = FindRequestEntry(received);
            if (entry != null)
            {
                EnsureQueueCapacity(entry.MaximumEventCount, 5, 1, entry.Id);
                outcome = ReceiveServerRequest(entry, received);
                reason = DescribeRequest(received, outcome);
                return CommitOutcome(entry, outcome, reason, false, false);
            }
            if (received.Method == SipMethod.Ack)
            {
                throw new SipTransactionException(
                    "The ACK does not match a live INVITE server transaction.",
                    null, SipTransactionState.None);
            }
            EnsureNoMergedRequest(received);
            SipTransactionOptions options = CopyOptions(incomingOptions);
            options.ReliableTransport = IsReliableMessage(message);
            if (entries.Count >= options.MaxTransactionCount)
            {
                throw new SipTransactionException(
                    "The live transaction limit has been reached.",
                    null, SipTransactionState.None);
            }
            string id = CreateTransactionId();
            string correlationId = message.CorrelationId == null
                || message.CorrelationId.Length == 0 ? id : message.CorrelationId;
            entry = CreateServerEntry(id, correlationId, received, options);
            outcome = StartEntry(entry);
            int createdEventCount = 1 + CountOutcomeEvents(outcome, true, false);
            EnsureQueueCapacity(entry.MaximumEventCount,
                createdEventCount, 1, entry.Id);
            entries.Add(entry);
            QueueEvent(entry, SipTransactionEventKind.Created,
                outcome.State, null, null, "Server transaction created.");
            return CommitOutcome(entry, outcome,
                "Incoming request accepted.", true, false);
        }
    }

    /// <summary>Sends a response through an existing server transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="response">The response to send.</param>
    /// <returns>The updated transaction snapshot.</returns>
    public SipTransactionSnapshot SendTransactionResponse(
        string transactionId, SipMessage response)
    {
        lock (gate)
        {
            SipTransactionLayerEntry entry = RequireEntry(transactionId);
            if (entry.InviteServer == null && entry.NonInviteServer == null)
            {
                throw new SipTransactionException(
                    "Only a server transaction can send a response.",
                    entry.Id, entry.GetSnapshot().State);
            }
            EnsureQueueCapacity(entry.MaximumEventCount, 3, 1, entry.Id);
            SipTransactionLayerOutcome outcome = SendServerResponse(entry, response);
            return CommitOutcome(entry, outcome,
                DescribeSentResponse(response, outcome), false, false);
        }
    }

    /// <summary>Reports a transport failure to a live transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="errorMessage">The bounded failure description.</param>
    public void ReportTransactionTransportError(
        string transactionId, string errorMessage)
    {
        lock (gate)
        {
            SipTransactionLayerEntry entry = RequireEntry(transactionId);
            EnsureQueueCapacity(entry.MaximumEventCount, 2, 1, entry.Id);
            SipTransactionLayerOutcome outcome =
                ReportEntryTransportError(entry, errorMessage);
            CommitOutcome(entry, outcome, errorMessage, false, false);
        }
    }

    /// <summary>Requests protocol cancellation or locally terminates the operation.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellation">The cancellation request.</param>
    public void CancelTransaction(
        string transactionId, SipCancellation cancellation)
    {
        lock (gate)
        {
            ValidateCancellation(cancellation);
            SipTransactionLayerEntry entry = RequireEntry(transactionId);
            if (!cancellation.Requested)
            {
                return;
            }
            string reason = ReadCancellationReason(cancellation,
                "Transaction cancellation requested.");
            if (entry.InviteClient == null)
            {
                ForceTerminate(entry, reason);
                return;
            }
            EnsureQueueCapacity(entry.MaximumEventCount, 3, 1, entry.Id);
            SipTransactionLayerOutcome outcome = CopyInviteClientOutcome(
                entry.InviteClient.RequestCancel());
            CommitOutcome(entry, outcome, reason, false, true);
        }
    }

    /// <summary>Immediately terminates and removes a transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellation">The termination request.</param>
    public void TerminateTransaction(
        string transactionId, SipCancellation cancellation)
    {
        lock (gate)
        {
            ValidateCancellation(cancellation);
            SipTransactionLayerEntry entry = RequireEntry(transactionId);
            ForceTerminate(entry, ReadCancellationReason(cancellation,
                "Transaction terminated by request."));
        }
    }

    /// <summary>Advances every live transaction to a monotonic timestamp.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    public void AdvanceTransactionTime(long nowMilliseconds)
    {
        lock (gate)
        {
            if (nowMilliseconds < clock.GetMilliseconds())
            {
                throw new System.ArgumentOutOfRangeException("nowMilliseconds");
            }
            PreflightAdvance(nowMilliseconds);
            clock.SetMilliseconds(nowMilliseconds);
            int index = 0;
            while (index < entries.Count)
            {
                SipTransactionLayerEntry entry = entries[index];
                SipTransactionLayerOutcome outcome = AdvanceEntry(entry, nowMilliseconds);
                if (OutcomeHasActivity(outcome))
                {
                    CommitOutcome(entry, outcome,
                        DescribeTimerOutcome(outcome), false, false);
                    if (index < entries.Count
                        && object.ReferenceEquals(entries[index], entry))
                    {
                        index = index + 1;
                    }
                }
                else
                {
                    index = index + 1;
                }
            }
        }
    }

    /// <summary>Polls the next queued transaction event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipTransactionEvent PollTransactionEvent()
    {
        lock (gate)
        {
            if (events.Count == 0)
            {
                return new SipTransactionEvent();
            }
            SipTransactionEvent source = events[0];
            events.RemoveAt(0);
            SipTransactionEvent? copy = SipSnapshotCopy.CopyTransactionEvent(source);
            return copy == null ? new SipTransactionEvent() : copy;
        }
    }

    /// <summary>Polls the next queued transaction snapshot.</summary>
    /// <returns>The next snapshot, or an empty snapshot when none is queued.</returns>
    public SipTransactionSnapshot PollTransactionSnapshot()
    {
        lock (gate)
        {
            if (snapshots.Count == 0)
            {
                return new SipTransactionSnapshot();
            }
            SipTransactionSnapshot source = snapshots[0];
            snapshots.RemoveAt(0);
            return CopyRequiredSnapshot(source);
        }
    }

    /// <summary>Gets a detached snapshot of a live transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <returns>The current snapshot.</returns>
    public SipTransactionSnapshot GetTransactionSnapshot(string transactionId)
    {
        lock (gate)
        {
            return CopyRequiredSnapshot(RequireEntry(transactionId).GetSnapshot());
        }
    }

    internal void ConfigureTransactionSources(ISipMonotonicClock? sourceClock,
        ISipRandomSource? randomSource, SipTransactionOptions? serverOptions)
    {
        lock (gate)
        {
            if (sourceClock == null || randomSource == null || serverOptions == null)
            {
                throw new System.ArgumentNullException("sourceClock");
            }
            if (entries.Count != 0 || events.Count != 0 || snapshots.Count != 0)
            {
                throw new System.InvalidOperationException(
                    "Transaction sources can only be configured before the layer is used.");
            }
            ValidateOptions(serverOptions);
            long initial = sourceClock.GetMilliseconds();
            if (initial < 0)
            {
                throw new System.ArgumentOutOfRangeException("sourceClock");
            }
            incomingOptions = CopyOptions(serverOptions);
            clock = new SipTransactionLayerClock(initial);
            identifiers = new SipCorrelationIdGenerator(randomSource);
        }
    }

    SipTransactionSnapshot CommitOutcome(SipTransactionLayerEntry entry,
        SipTransactionLayerOutcome outcome, string reason,
        bool suppressStateEvent, bool operationFeedback)
    {
        SipTransactionSnapshot snapshot = entry.GetSnapshot();
        int requiredEvents = CountOutcomeEvents(
            outcome, suppressStateEvent, operationFeedback);
        EnsureQueueCapacity(entry.MaximumEventCount,
            requiredEvents, 1, entry.Id);
        QueueTimerEvents(entry, outcome, snapshot.State);
        if (outcome.TransportFailed)
        {
            QueueEvent(entry, SipTransactionEventKind.TransportError,
                snapshot.State, null, null, outcome.ErrorMessage == null
                    ? reason : outcome.ErrorMessage);
        }
        if (!suppressStateEvent && outcome.StateChanged)
        {
            SipTransactionEventKind kind = snapshot.State == SipTransactionState.Terminated
                ? SipTransactionEventKind.Terminated
                : SipTransactionEventKind.StateChanged;
            QueueEvent(entry, kind, snapshot.State, null, null, reason);
        }
        QueueOutcomeMessages(entry, outcome, snapshot.State, reason);
        if (operationFeedback && !outcome.StateChanged
            && outcome.OutboundMessages.Length == 0)
        {
            QueueEvent(entry, SipTransactionEventKind.StateChanged,
                snapshot.State, null, null, reason);
        }
        QueueSnapshot(entry, snapshot);
        if (snapshot.State == SipTransactionState.Terminated)
        {
            RemoveEntry(entry);
        }
        return CopyRequiredSnapshot(snapshot);
    }

    void ForceTerminate(SipTransactionLayerEntry entry, string reason)
    {
        EnsureQueueCapacity(entry.MaximumEventCount, 1, 1, entry.Id);
        SipTransactionSnapshot snapshot = entry.GetSnapshot();
        snapshot.State = SipTransactionState.Terminated;
        snapshot.TimerNames = new string[0];
        snapshot.TimerDeadlines = new long[0];
        snapshot.UpdatedAtMilliseconds = clock.GetMilliseconds();
        QueueEvent(entry, SipTransactionEventKind.Terminated,
            SipTransactionState.Terminated, null, null, reason);
        QueueSnapshot(entry, snapshot);
        RemoveEntry(entry);
    }

    void QueueTimerEvents(SipTransactionLayerEntry entry,
        SipTransactionLayerOutcome outcome, SipTransactionState state)
    {
        int index = 0;
        while (index < outcome.FiredTimerNames.Length)
        {
            string name = outcome.FiredTimerNames[index];
            QueueEvent(entry, SipTransactionEventKind.TimerFired,
                state, null, name, "Transaction timer fired.");
            index = index + 1;
        }
    }

    void QueueOutcomeMessages(SipTransactionLayerEntry entry,
        SipTransactionLayerOutcome outcome, SipTransactionState state, string reason)
    {
        int index = 0;
        while (index < outcome.OutboundMessages.Length)
        {
            SipTransactionEventKind kind = outcome.Retransmitted
                ? SipTransactionEventKind.Retransmitted
                : SipTransactionEventKind.MessageDelivered;
            QueueEvent(entry, kind, state,
                outcome.OutboundMessages[index], null, reason);
            index = index + 1;
        }
        index = 0;
        while (index < outcome.DeliveredMessages.Length)
        {
            QueueEvent(entry, SipTransactionEventKind.MessageDelivered,
                state, outcome.DeliveredMessages[index], null, reason);
            index = index + 1;
        }
    }

    void QueueEvent(SipTransactionLayerEntry entry,
        SipTransactionEventKind kind, SipTransactionState state,
        SipMessage? message, string? timerName, string reason)
    {
        SipTransactionEvent value = new SipTransactionEvent();
        value.Kind = kind;
        value.TransactionId = entry.Id;
        value.State = state;
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.TimerName = timerName;
        value.Reason = reason;
        value.CorrelationId = entry.CorrelationId;
        events.Add(value);
    }

    void QueueSnapshot(SipTransactionLayerEntry entry,
        SipTransactionSnapshot snapshot)
    {
        snapshot.CorrelationId = entry.CorrelationId;
        snapshots.Add(CopyRequiredSnapshot(snapshot));
    }

    void EnsureQueueCapacity(int maximum, int eventCount,
        int snapshotCount, string? transactionId)
    {
        if (maximum <= 0 || eventCount < 0 || snapshotCount < 0
            || eventCount > maximum - events.Count
            || snapshotCount > maximum - snapshots.Count)
        {
            throw new SipTransactionException(
                "The transaction event or snapshot queue is full.",
                transactionId, SipTransactionState.None);
        }
    }

    void PreflightAdvance(long nowMilliseconds)
    {
        int eventCount = 0;
        int snapshotCount = 0;
        int maximum = SipDefaults.MaxQueueCount;
        int index = 0;
        while (index < entries.Count)
        {
            SipTransactionLayerEntry entry = entries[index];
            SipTransactionSnapshot snapshot = entry.GetSnapshot();
            int due = CountDueTimers(snapshot, nowMilliseconds);
            if (due > 0)
            {
                if (due > (int.MaxValue - eventCount - 1) / 3)
                {
                    throw new SipTransactionException(
                        "The transaction event estimate is too large.",
                        entry.Id, snapshot.State);
                }
                eventCount = eventCount + due * 3 + 1;
                snapshotCount = snapshotCount + 1;
            }
            if (entry.MaximumEventCount < maximum)
            {
                maximum = entry.MaximumEventCount;
            }
            index = index + 1;
        }
        EnsureQueueCapacity(maximum, eventCount, snapshotCount, null);
    }

    int CountDueTimers(SipTransactionSnapshot snapshot, long nowMilliseconds)
    {
        int count = 0;
        int index = 0;
        while (index < snapshot.TimerDeadlines.Length)
        {
            if (snapshot.TimerDeadlines[index] <= nowMilliseconds)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    SipTransactionLayerEntry CreateClientEntry(string id, string correlationId,
        SipMessage request, SipTransactionOptions options)
    {
        SipTransactionLayerEntry entry = CreateEntry(
            id, correlationId, options);
        if (request != null && request.Method == SipMethod.Invite)
        {
            entry.InviteClient = new SipInviteClientTransaction(
                id, request, options, clock);
        }
        else
        {
            entry.NonInviteClient = new SipNonInviteClientTransaction(
                id, request, options, clock);
        }
        return entry;
    }

    SipTransactionLayerEntry CreateServerEntry(string id, string correlationId,
        SipMessage request, SipTransactionOptions options)
    {
        SipTransactionLayerEntry entry = CreateEntry(
            id, correlationId, options);
        if (request != null && request.Method == SipMethod.Invite)
        {
            entry.InviteServer = new SipInviteServerTransaction(
                id, request, options, clock);
        }
        else
        {
            entry.NonInviteServer = new SipNonInviteServerTransaction(
                id, request, options, clock);
        }
        return entry;
    }

    SipTransactionLayerEntry CreateEntry(string id, string correlationId,
        SipTransactionOptions options)
    {
        SipTransactionLayerEntry entry = new SipTransactionLayerEntry();
        entry.Id = id;
        entry.CorrelationId = correlationId;
        entry.MaximumEventCount = options.MaxEventCount;
        entry.AllowLegacyMatching = options.AllowLegacyMatching;
        return entry;
    }

    SipTransactionLayerOutcome StartEntry(SipTransactionLayerEntry entry)
    {
        if (entry.InviteClient != null)
        {
            return CopyInviteClientOutcome(entry.InviteClient.StartTransaction());
        }
        if (entry.InviteServer != null)
        {
            return CopyInviteServerOutcome(entry.InviteServer.StartTransaction());
        }
        if (entry.NonInviteClient != null)
        {
            return CopyNonInviteClientOutcome(
                entry.NonInviteClient.StartTransaction());
        }
        if (entry.NonInviteServer != null)
        {
            return CopyNonInviteServerOutcome(
                entry.NonInviteServer.StartTransaction());
        }
        throw new System.InvalidOperationException(
            "The transaction entry has no state machine.");
    }

    SipTransactionLayerOutcome ReceiveClientResponse(
        SipTransactionLayerEntry entry, SipMessage response)
    {
        if (entry.InviteClient != null)
        {
            return CopyInviteClientOutcome(
                entry.InviteClient.ReceiveResponse(response));
        }
        if (entry.NonInviteClient != null)
        {
            return CopyNonInviteClientOutcome(
                entry.NonInviteClient.ReceiveResponse(response));
        }
        throw new SipTransactionException(
            "The response matched no client state machine.",
            entry.Id, entry.GetSnapshot().State);
    }

    SipTransactionLayerOutcome ReceiveServerRequest(
        SipTransactionLayerEntry entry, SipMessage request)
    {
        if (entry.InviteServer != null)
        {
            return request.Method == SipMethod.Cancel
                ? CopyInviteServerOutcome(entry.InviteServer.ReceiveCancel(request))
                : CopyInviteServerOutcome(entry.InviteServer.ReceiveRequest(request));
        }
        if (entry.NonInviteServer != null)
        {
            return CopyNonInviteServerOutcome(
                entry.NonInviteServer.ReceiveRequest(request));
        }
        throw new SipTransactionException(
            "The request matched no server state machine.",
            entry.Id, entry.GetSnapshot().State);
    }

    SipTransactionLayerOutcome SendServerResponse(
        SipTransactionLayerEntry entry, SipMessage response)
    {
        if (entry.InviteServer != null)
        {
            return CopyInviteServerOutcome(
                entry.InviteServer.SendResponse(response));
        }
        if (entry.NonInviteServer != null)
        {
            return CopyNonInviteServerOutcome(
                entry.NonInviteServer.SendResponse(response));
        }
        throw new SipTransactionException(
            "The transaction is not a server transaction.",
            entry.Id, entry.GetSnapshot().State);
    }

    SipTransactionLayerOutcome ReportEntryTransportError(
        SipTransactionLayerEntry entry, string errorMessage)
    {
        if (entry.InviteClient != null)
        {
            return CopyInviteClientOutcome(
                entry.InviteClient.HandleTransportError(errorMessage));
        }
        if (entry.InviteServer != null)
        {
            return CopyInviteServerOutcome(
                entry.InviteServer.HandleTransportError(errorMessage));
        }
        if (entry.NonInviteClient != null)
        {
            return CopyNonInviteClientOutcome(
                entry.NonInviteClient.HandleTransportError(errorMessage));
        }
        if (entry.NonInviteServer != null)
        {
            return CopyNonInviteServerOutcome(
                entry.NonInviteServer.HandleTransportError(errorMessage));
        }
        throw new System.InvalidOperationException(
            "The transaction entry has no state machine.");
    }

    SipTransactionLayerOutcome AdvanceEntry(
        SipTransactionLayerEntry entry, long nowMilliseconds)
    {
        if (entry.InviteClient != null)
        {
            return CopyInviteClientOutcome(
                entry.InviteClient.AdvanceTime(nowMilliseconds));
        }
        if (entry.InviteServer != null)
        {
            return CopyInviteServerOutcome(
                entry.InviteServer.AdvanceTime(nowMilliseconds));
        }
        if (entry.NonInviteClient != null)
        {
            return CopyNonInviteClientOutcome(
                entry.NonInviteClient.AdvanceTime(nowMilliseconds));
        }
        if (entry.NonInviteServer != null)
        {
            return CopyNonInviteServerOutcome(
                entry.NonInviteServer.AdvanceTime(nowMilliseconds));
        }
        throw new System.InvalidOperationException(
            "The transaction entry has no state machine.");
    }

    SipTransactionLayerEntry? FindResponseEntry(SipMessage response)
    {
        int index = 0;
        while (index < entries.Count)
        {
            SipTransactionLayerEntry entry = entries[index];
            SipTransactionSnapshot snapshot = entry.GetSnapshot();
            if (snapshot.Role == SipTransactionRole.Client
                && SipTransactionMatching.MatchResponse(
                    snapshot.Key, response, entry.AllowLegacyMatching))
            {
                return entry;
            }
            index = index + 1;
        }
        return null;
    }

    SipTransactionLayerEntry? FindRequestEntry(SipMessage request)
    {
        int index = 0;
        while (index < entries.Count)
        {
            SipTransactionLayerEntry entry = entries[index];
            if (entry.InviteServer != null)
            {
                bool matches = request.Method == SipMethod.Cancel
                    ? entry.InviteServer.MatchesCancel(request)
                    : entry.InviteServer.MatchesRequest(request);
                if (matches)
                {
                    return entry;
                }
            }
            else if (entry.NonInviteServer != null
                && request.Method != SipMethod.Ack
                && SipTransactionMatching.MatchRequest(
                    entry.GetSnapshot().Key, request, null,
                    entry.AllowLegacyMatching))
            {
                return entry;
            }
            index = index + 1;
        }
        return null;
    }

    SipTransactionLayerEntry? FindDuplicateClient(
        SipTransactionLayerEntry candidate)
    {
        SipTransactionSnapshot candidateSnapshot = candidate.GetSnapshot();
        int index = 0;
        while (index < entries.Count)
        {
            SipTransactionSnapshot existing = entries[index].GetSnapshot();
            if (existing.Role == SipTransactionRole.Client
                && KeysIdentifySameTransaction(
                    existing.Key, candidateSnapshot.Key))
            {
                return entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    bool KeysIdentifySameTransaction(
        SipTransactionKey? first, SipTransactionKey? second)
    {
        return first != null && second != null
            && first.Branch == second.Branch
            && first.SentBy == second.SentBy
            && first.Method == second.Method
            && first.MethodToken == second.MethodToken;
    }

    void EnsureNoMergedRequest(SipMessage request)
    {
        int index = 0;
        while (index < entries.Count)
        {
            SipTransactionLayerEntry entry = entries[index];
            SipTransactionSnapshot snapshot = entry.GetSnapshot();
            if (snapshot.Role == SipTransactionRole.Server
                && snapshot.Request != null
                && SipRequestRouting.IsMergedRequest(snapshot.Request, request))
            {
                throw new SipTransactionException(
                    "The request is merged with an existing server transaction.",
                    entry.Id, snapshot.State);
            }
            index = index + 1;
        }
    }

    SipTransactionLayerEntry RequireEntry(string? transactionId)
    {
        if (transactionId == null || transactionId.Length == 0
            || transactionId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "A bounded transaction identifier is required.", "transactionId");
        }
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Id == transactionId)
            {
                return entries[index];
            }
            index = index + 1;
        }
        throw new SipTransactionException(
            "The transaction is not live.", transactionId,
            SipTransactionState.Terminated);
    }

    void RemoveEntry(SipTransactionLayerEntry entry)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (object.ReferenceEquals(entries[index], entry))
            {
                entries.RemoveAt(index);
                return;
            }
            index = index + 1;
        }
    }

    string CreateTransactionId()
    {
        int attempts = 0;
        while (attempts < 8)
        {
            string candidate = identifiers.CreateCorrelationId("tx-");
            if (!IdentifierExists(candidate))
            {
                return candidate;
            }
            attempts = attempts + 1;
        }
        throw new System.InvalidOperationException(
            "A unique transaction identifier cannot be generated.");
    }

    bool IdentifierExists(string candidate)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Id == candidate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static SipTransactionLayerOutcome CopyInviteClientOutcome(
        SipInviteClientResult source)
    {
        SipTransactionLayerOutcome outcome = new SipTransactionLayerOutcome();
        outcome.State = source.State;
        outcome.StateChanged = source.StateChanged;
        outcome.TimedOut = source.TimedOut;
        outcome.TransportFailed = source.TransportFailed;
        outcome.Retransmitted = source.Retransmitted;
        outcome.CancellationPending = source.CancellationPending;
        outcome.CancelSent = source.CancelSent;
        outcome.CancelSuppressed = source.CancelSuppressed;
        outcome.ErrorMessage = source.ErrorMessage;
        outcome.OutboundMessages = source.OutboundMessages;
        outcome.DeliveredMessages = source.DeliveredResponses;
        outcome.FiredTimerNames = source.FiredTimerNames;
        return outcome;
    }

    static SipTransactionLayerOutcome CopyInviteServerOutcome(
        SipInviteServerResult source)
    {
        SipTransactionLayerOutcome outcome = new SipTransactionLayerOutcome();
        outcome.State = source.State;
        outcome.StateChanged = source.StateChanged;
        outcome.TimedOut = source.TimedOut;
        outcome.TransportFailed = source.TransportFailed;
        outcome.Retransmitted = source.Retransmitted;
        outcome.RequestAbsorbed = source.RequestAbsorbed;
        outcome.ResponseDiscarded = source.ResponseDiscarded;
        outcome.ErrorMessage = source.ErrorMessage;
        outcome.OutboundMessages = source.OutboundMessages;
        outcome.DeliveredMessages = source.DeliveredRequests;
        outcome.FiredTimerNames = source.FiredTimerNames;
        return outcome;
    }

    static SipTransactionLayerOutcome CopyNonInviteClientOutcome(
        SipNonInviteClientResult source)
    {
        SipTransactionLayerOutcome outcome = new SipTransactionLayerOutcome();
        outcome.State = source.State;
        outcome.StateChanged = source.StateChanged;
        outcome.TimedOut = source.TimedOut;
        outcome.TransportFailed = source.TransportFailed;
        outcome.Retransmitted = source.Retransmitted;
        outcome.ErrorMessage = source.ErrorMessage;
        outcome.OutboundMessages = source.OutboundMessages;
        outcome.DeliveredMessages = source.DeliveredResponses;
        outcome.FiredTimerNames = source.FiredTimerNames;
        return outcome;
    }

    static SipTransactionLayerOutcome CopyNonInviteServerOutcome(
        SipNonInviteServerResult source)
    {
        SipTransactionLayerOutcome outcome = new SipTransactionLayerOutcome();
        outcome.State = source.State;
        outcome.StateChanged = source.StateChanged;
        outcome.TransportFailed = source.TransportFailed;
        outcome.Retransmitted = source.Retransmitted;
        outcome.RequestAbsorbed = source.RequestAbsorbed;
        outcome.ResponseDiscarded = source.ResponseDiscarded;
        outcome.ErrorMessage = source.ErrorMessage;
        outcome.OutboundMessages = source.OutboundMessages;
        outcome.DeliveredMessages = source.DeliveredRequests;
        outcome.FiredTimerNames = source.FiredTimerNames;
        return outcome;
    }

    static int CountOutcomeEvents(SipTransactionLayerOutcome outcome,
        bool suppressStateEvent, bool operationFeedback)
    {
        int count = outcome.FiredTimerNames.Length
            + outcome.OutboundMessages.Length
            + outcome.DeliveredMessages.Length;
        if (outcome.TransportFailed)
        {
            count = count + 1;
        }
        if (!suppressStateEvent && outcome.StateChanged)
        {
            count = count + 1;
        }
        if (operationFeedback && !outcome.StateChanged
            && outcome.OutboundMessages.Length == 0)
        {
            count = count + 1;
        }
        return count;
    }

    static bool OutcomeHasActivity(SipTransactionLayerOutcome outcome)
    {
        return outcome.StateChanged || outcome.TransportFailed
            || outcome.OutboundMessages.Length > 0
            || outcome.DeliveredMessages.Length > 0
            || outcome.FiredTimerNames.Length > 0;
    }

    static string DescribeResponse(SipMessage response)
    {
        if (response.StatusCode < 200)
        {
            return "Provisional response received.";
        }
        if (response.StatusCode < 300)
        {
            return "Successful final response received.";
        }
        return "Failure final response received.";
    }

    static string DescribeRequest(SipMessage request,
        SipTransactionLayerOutcome outcome)
    {
        if (request.Method == SipMethod.Ack)
        {
            return "ACK received.";
        }
        if (request.Method == SipMethod.Cancel)
        {
            return "CANCEL received.";
        }
        if (outcome.RequestAbsorbed)
        {
            return "Request retransmission absorbed.";
        }
        return "Request received.";
    }

    static string DescribeSentResponse(SipMessage response,
        SipTransactionLayerOutcome outcome)
    {
        if (outcome.ResponseDiscarded)
        {
            return "Response discarded by transaction state.";
        }
        return response != null && response.StatusCode < 200
            ? "Provisional response sent." : "Final response sent.";
    }

    static string DescribeTimerOutcome(SipTransactionLayerOutcome outcome)
    {
        if (outcome.TimedOut)
        {
            return outcome.ErrorMessage == null
                ? "Transaction timed out." : outcome.ErrorMessage;
        }
        if (outcome.State == SipTransactionState.Terminated)
        {
            return "Transaction timer completed cleanup.";
        }
        return "Transaction timer advanced state.";
    }

    static string ReadCancellationReason(SipCancellation cancellation,
        string fallback)
    {
        return cancellation.Reason == null || cancellation.Reason.Length == 0
            ? fallback : cancellation.Reason;
    }

    static void ValidateCancellation(SipCancellation? cancellation)
    {
        if (cancellation == null)
        {
            throw new System.ArgumentNullException("cancellation");
        }
        if (cancellation.Id != null
            && cancellation.Id.Length > SipDefaults.MaxHeaderLineBytes
            || cancellation.Reason != null
            && cancellation.Reason.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "Cancellation identifiers and reasons must be bounded.", "cancellation");
        }
    }

    static void ValidateReceivedMessage(SipTransportMessage? envelope)
    {
        if (envelope == null || envelope.Message == null)
        {
            throw new System.ArgumentNullException("message");
        }
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(
            envelope.Message, true);
        if (SipMessageValidator.HasErrorIssues(issues)
            || envelope.Message.Kind != SipMessageKind.Request
                && envelope.Message.Kind != SipMessageKind.Response
            || envelope.CorrelationId != null
                && envelope.CorrelationId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "A valid bounded SIP transport message is required.", "message");
        }
    }

    static void ValidateOptions(SipTransactionOptions? options)
    {
        if (options == null || options.Timers == null
            || options.MaxTransactionCount <= 0 || options.MaxEventCount <= 0
            || options.Timers.T1Milliseconds <= 0
            || options.Timers.T2Milliseconds < options.Timers.T1Milliseconds
            || options.Timers.T4Milliseconds <= 0
            || options.Timers.MaxTimerCount <= 0
            || options.CorrelationId != null
                && options.CorrelationId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "Valid bounded transaction options are required.", "options");
        }
    }

    static SipTransactionOptions CopyOptions(SipTransactionOptions source)
    {
        SipTransactionOptions copy = new SipTransactionOptions();
        copy.ReliableTransport = source.ReliableTransport;
        copy.AllowLegacyMatching = source.AllowLegacyMatching;
        copy.MaxTransactionCount = source.MaxTransactionCount;
        copy.MaxEventCount = source.MaxEventCount;
        copy.CorrelationId = source.CorrelationId;
        copy.Timers.T1Milliseconds = source.Timers.T1Milliseconds;
        copy.Timers.T2Milliseconds = source.Timers.T2Milliseconds;
        copy.Timers.T4Milliseconds = source.Timers.T4Milliseconds;
        copy.Timers.MaxTimerCount = source.Timers.MaxTimerCount;
        return copy;
    }

    static bool IsReliableMessage(SipTransportMessage message)
    {
        return message.Transport == SipTransportKind.Tcp
            || message.Transport == SipTransportKind.Tls
            || message.Transport == SipTransportKind.WebSocket
            || message.Transport == SipTransportKind.SecureWebSocket
            || message.Transport == SipTransportKind.Sctp
            || message.Connection != null;
    }

    static SipTransactionSnapshot CopyRequiredSnapshot(
        SipTransactionSnapshot source)
    {
        SipTransactionSnapshot? copy =
            SipSnapshotCopy.CopyTransactionSnapshot(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The transaction snapshot cannot be copied.");
        }
        return copy;
    }
}
