namespace sip;

internal class SipNonInviteServerTransaction
{
    string id;
    SipMessage request;
    SipMessage? response;
    SipTransactionKey key;
    SipTransactionKey responseKey;
    SipTimerScheduler scheduler;
    bool reliableTransport;
    bool allowLegacyMatching;
    SipTransactionState state;
    long timerJId = -1;
    long createdAtMilliseconds = -1;
    long updatedAtMilliseconds = -1;

    internal SipNonInviteServerTransaction(string? transactionId, SipMessage? receivedRequest,
        SipTransactionOptions? options, ISipMonotonicClock? clock)
    {
        if (transactionId == null || transactionId.Length == 0
            || transactionId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException("A bounded transaction identifier is required.",
                "transactionId");
        }
        if (receivedRequest == null || receivedRequest.Kind != SipMessageKind.Request
            || receivedRequest.Method == SipMethod.Invite
            || receivedRequest.Method == SipMethod.Ack)
        {
            throw new System.ArgumentException("A non-INVITE transactional request is required.",
                "receivedRequest");
        }
        SipValidationIssue[] requestIssues = SipMessageValidator.ValidateMessageModel(
            receivedRequest, true);
        if (SipMessageValidator.HasErrorIssues(requestIssues))
        {
            throw new System.ArgumentException("A valid non-INVITE request is required.",
                "receivedRequest");
        }
        if (options == null || options.Timers == null)
        {
            throw new System.ArgumentNullException("options");
        }

        SipMessage? copiedRequest = SipSnapshotCopy.CopyMessageModel(receivedRequest);
        if (copiedRequest == null)
        {
            throw new System.ArgumentException("A non-INVITE request is required.",
                "receivedRequest");
        }

        id = transactionId;
        request = copiedRequest;
        key = SipTransactionMatching.CreateKey(request, SipTransactionRole.Server);
        responseKey = SipTransactionMatching.CreateKey(request, SipTransactionRole.Client);
        scheduler = new SipTimerScheduler(options.Timers, clock);
        reliableTransport = options.ReliableTransport;
        allowLegacyMatching = options.AllowLegacyMatching;
        state = SipTransactionState.None;
    }

    internal SipNonInviteServerResult StartTransaction()
    {
        if (state != SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE server transaction has already started.");
        }

        state = SipTransactionState.Trying;
        createdAtMilliseconds = scheduler.GetCurrentMilliseconds();
        updatedAtMilliseconds = createdAtMilliseconds;
        SipNonInviteServerResult result = CreateResult(true);
        result.DeliveredRequests = new SipMessage[] { CopyRequiredMessage(request) };
        return result;
    }

    internal SipNonInviteServerResult SendResponse(SipMessage? sent)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE server transaction has not started.");
        }
        SipValidationIssue[] responseIssues = SipMessageValidator.ValidateMessageModel(sent, true);
        if (SipMessageValidator.HasErrorIssues(responseIssues)
            || !SipTransactionMatching.MatchResponse(responseKey, sent, allowLegacyMatching)
            || sent == null)
        {
            throw new System.ArgumentException("The response does not match this transaction.",
                "sent");
        }
        if (state == SipTransactionState.Completed || state == SipTransactionState.Terminated)
        {
            SipNonInviteServerResult discarded = CreateResult(false);
            discarded.ResponseDiscarded = true;
            return discarded;
        }

        response = CopyRequiredMessage(sent);
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        if (sent.StatusCode < 200)
        {
            bool changed = state == SipTransactionState.Trying;
            state = SipTransactionState.Proceeding;
            SipNonInviteServerResult provisional = CreateResult(changed);
            provisional.OutboundMessages = new SipMessage[] { CopyRequiredMessage(sent) };
            return provisional;
        }

        state = SipTransactionState.Completed;
        timerJId = scheduler.ScheduleTimer(id, SipTimerNames.TimerJ,
            reliableTransport ? 0 : MultiplyMilliseconds(scheduler.GetT1Milliseconds(), 64));
        SipNonInviteServerResult final = CreateResult(true);
        final.OutboundMessages = new SipMessage[] { CopyRequiredMessage(sent) };
        return final;
    }

    internal SipNonInviteServerResult ReceiveRequest(SipMessage? received)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE server transaction has not started.");
        }
        SipValidationIssue[] requestIssues = SipMessageValidator.ValidateMessageModel(
            received, true);
        if (SipMessageValidator.HasErrorIssues(requestIssues)
            || !SipTransactionMatching.MatchRequest(key, received, null, allowLegacyMatching)
            || received == null)
        {
            throw new System.ArgumentException("The request does not match this transaction.",
                "received");
        }

        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        SipNonInviteServerResult result = CreateResult(false);
        result.RequestAbsorbed = true;
        if ((state == SipTransactionState.Proceeding || state == SipTransactionState.Completed)
            && response != null)
        {
            result.OutboundMessages = new SipMessage[] { CopyRequiredMessage(response) };
            result.Retransmitted = true;
        }
        return result;
    }

    internal SipNonInviteServerResult AdvanceTime(long nowMilliseconds)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE server transaction has not started.");
        }

        SipScheduledTimer[] due = scheduler.AdvanceTime(nowMilliseconds);
        System.Collections.Generic.List<string> fired = new System.Collections.Generic.List<string>();
        bool changed = false;
        int index = 0;
        while (index < due.Length)
        {
            SipScheduledTimer timer = due[index];
            string? timerName = timer.Name;
            if (timerName != null)
            {
                fired.Add(timerName);
            }
            if (timer.Id == timerJId)
            {
                timerJId = -1;
                if (state == SipTransactionState.Completed)
                {
                    state = SipTransactionState.Terminated;
                    changed = true;
                    CancelAllTimers();
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            index = index + 1;
        }

        SipNonInviteServerResult result = CreateResult(changed);
        result.FiredTimerNames = fired.ToArray();
        return result;
    }

    internal SipNonInviteServerResult HandleTransportError(string? errorMessage)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE server transaction has not started.");
        }
        if (errorMessage == null || errorMessage.Length == 0
            || errorMessage.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException("A bounded transport error message is required.",
                "errorMessage");
        }
        if (state == SipTransactionState.Terminated)
        {
            return CreateResult(false);
        }

        state = SipTransactionState.Terminated;
        CancelAllTimers();
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        SipNonInviteServerResult result = CreateResult(true);
        result.TransportFailed = true;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal SipTransactionSnapshot GetSnapshot()
    {
        SipTransactionSnapshot snapshot = new SipTransactionSnapshot();
        snapshot.Id = id;
        snapshot.Key = key;
        snapshot.Kind = SipTransactionKind.NonInvite;
        snapshot.Role = SipTransactionRole.Server;
        snapshot.State = state;
        snapshot.Request = request;
        snapshot.Response = response;
        SipScheduledTimer[] timers = scheduler.CopyTimers();
        snapshot.TimerNames = new string[timers.Length];
        snapshot.TimerDeadlines = new long[timers.Length];
        int index = 0;
        while (index < timers.Length)
        {
            string? timerName = timers[index].Name;
            snapshot.TimerNames[index] = timerName == null ? "" : timerName;
            snapshot.TimerDeadlines[index] = timers[index].DeadlineMilliseconds;
            index = index + 1;
        }
        snapshot.CreatedAtMilliseconds = createdAtMilliseconds;
        snapshot.UpdatedAtMilliseconds = updatedAtMilliseconds;
        SipTransactionSnapshot? copy = SipSnapshotCopy.CopyTransactionSnapshot(snapshot);
        if (copy == null)
        {
            throw new System.InvalidOperationException("The transaction snapshot cannot be copied.");
        }
        return copy;
    }

    SipNonInviteServerResult CreateResult(bool changed)
    {
        SipNonInviteServerResult result = new SipNonInviteServerResult();
        result.State = state;
        result.StateChanged = changed;
        return result;
    }

    void CancelAllTimers()
    {
        scheduler.CancelOwnerTimers(id);
        timerJId = -1;
    }

    static long MultiplyMilliseconds(int value, int multiplier)
    {
        return (long)value * multiplier;
    }

    static SipMessage CopyRequiredMessage(SipMessage source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException("The SIP message cannot be copied.");
        }
        return copy;
    }
}
