namespace sip;

internal class SipNonInviteClientTransaction
{
    string id;
    SipMessage request;
    SipMessage? response;
    SipTransactionKey key;
    SipTimerScheduler scheduler;
    bool reliableTransport;
    bool allowLegacyMatching;
    SipTransactionState state;
    long timerEId = -1;
    long timerFId = -1;
    long timerKId = -1;
    long timerEInterval;
    long createdAtMilliseconds = -1;
    long updatedAtMilliseconds = -1;

    internal SipNonInviteClientTransaction(string? transactionId, SipMessage? sentRequest,
        SipTransactionOptions? options, ISipMonotonicClock? clock)
    {
        if (transactionId == null || transactionId.Length == 0
            || transactionId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException("A bounded transaction identifier is required.",
                "transactionId");
        }
        if (sentRequest == null || sentRequest.Kind != SipMessageKind.Request
            || sentRequest.Method == SipMethod.Invite || sentRequest.Method == SipMethod.Ack)
        {
            throw new System.ArgumentException("A non-INVITE transactional request is required.",
                "sentRequest");
        }
        SipValidationIssue[] requestIssues = SipMessageValidator.ValidateMessageModel(
            sentRequest, true);
        if (SipMessageValidator.HasErrorIssues(requestIssues))
        {
            throw new System.ArgumentException("A valid non-INVITE request is required.",
                "sentRequest");
        }
        if (options == null || options.Timers == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (!options.ReliableTransport && options.Timers.MaxTimerCount < 2)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        SipMessage? copiedRequest = SipSnapshotCopy.CopyMessageModel(sentRequest);
        if (copiedRequest == null)
        {
            throw new System.ArgumentException("A non-INVITE request is required.",
                "sentRequest");
        }

        id = transactionId;
        request = copiedRequest;
        key = SipTransactionMatching.CreateKey(request, SipTransactionRole.Client);
        scheduler = new SipTimerScheduler(options.Timers, clock);
        reliableTransport = options.ReliableTransport;
        allowLegacyMatching = options.AllowLegacyMatching;
        state = SipTransactionState.None;
        timerEInterval = scheduler.GetT1Milliseconds();
    }

    internal SipNonInviteClientResult StartTransaction()
    {
        if (state != SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE client transaction has already started.");
        }

        state = SipTransactionState.Trying;
        createdAtMilliseconds = scheduler.GetCurrentMilliseconds();
        updatedAtMilliseconds = createdAtMilliseconds;
        if (!reliableTransport)
        {
            timerEId = scheduler.ScheduleTimer(id, SipTimerNames.TimerE, timerEInterval);
        }
        timerFId = scheduler.ScheduleTimer(id, SipTimerNames.TimerF,
            MultiplyMilliseconds(scheduler.GetT1Milliseconds(), 64));

        SipNonInviteClientResult result = CreateResult(true);
        result.OutboundMessages = new SipMessage[] { CopyRequiredMessage(request) };
        return result;
    }

    internal SipNonInviteClientResult ReceiveResponse(SipMessage? received)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE client transaction has not started.");
        }
        SipValidationIssue[] responseIssues = SipMessageValidator.ValidateMessageModel(
            received, true);
        if (SipMessageValidator.HasErrorIssues(responseIssues)
            || !SipTransactionMatching.MatchResponse(key, received, allowLegacyMatching)
            || received == null)
        {
            throw new System.ArgumentException("The response does not match this transaction.",
                "received");
        }

        if (state == SipTransactionState.Terminated || state == SipTransactionState.Completed)
        {
            SipNonInviteClientResult absorbed = CreateResult(false);
            absorbed.ResponseAbsorbed = true;
            return absorbed;
        }
        if (received.StatusCode < 200)
        {
            return ReceiveProvisionalResponse(received);
        }
        return ReceiveFinalResponse(received);
    }

    internal SipNonInviteClientResult AdvanceTime(long nowMilliseconds)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE client transaction has not started.");
        }

        SipScheduledTimer[] due = scheduler.AdvanceTime(nowMilliseconds);
        System.Collections.Generic.List<SipMessage> outbound =
            new System.Collections.Generic.List<SipMessage>();
        System.Collections.Generic.List<string> fired = new System.Collections.Generic.List<string>();
        bool changed = false;
        bool timedOut = false;
        bool retransmitted = false;
        int index = 0;
        while (index < due.Length)
        {
            SipScheduledTimer timer = due[index];
            string? timerName = timer.Name;
            if (timerName != null)
            {
                fired.Add(timerName);
            }
            if (timer.Id == timerEId)
            {
                timerEId = -1;
                if (state == SipTransactionState.Trying || state == SipTransactionState.Proceeding)
                {
                    outbound.Add(CopyRequiredMessage(request));
                    retransmitted = true;
                    if (state == SipTransactionState.Trying)
                    {
                        timerEInterval = MinimumMilliseconds(
                            DoubleMilliseconds(timerEInterval), scheduler.GetT2Milliseconds());
                    }
                    else
                    {
                        timerEInterval = scheduler.GetT2Milliseconds();
                    }
                    timerEId = scheduler.ScheduleTimer(id, SipTimerNames.TimerE, timerEInterval);
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            else if (timer.Id == timerFId)
            {
                timerFId = -1;
                if (state == SipTransactionState.Trying || state == SipTransactionState.Proceeding)
                {
                    state = SipTransactionState.Terminated;
                    timedOut = true;
                    changed = true;
                    CancelAllTimers();
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            else if (timer.Id == timerKId)
            {
                timerKId = -1;
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

        SipNonInviteClientResult result = CreateResult(changed);
        result.OutboundMessages = outbound.ToArray();
        result.FiredTimerNames = fired.ToArray();
        result.TimedOut = timedOut;
        result.Retransmitted = retransmitted;
        if (timedOut)
        {
            result.ErrorMessage = "The non-INVITE client transaction timed out.";
        }
        return result;
    }

    internal SipNonInviteClientResult HandleTransportError(string? errorMessage)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException(
                "The non-INVITE client transaction has not started.");
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
        SipNonInviteClientResult result = CreateResult(true);
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
        snapshot.Role = SipTransactionRole.Client;
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

    SipNonInviteClientResult ReceiveProvisionalResponse(SipMessage received)
    {
        bool changed = state == SipTransactionState.Trying;
        state = SipTransactionState.Proceeding;
        response = CopyRequiredMessage(received);
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        SipNonInviteClientResult result = CreateResult(changed);
        result.DeliveredResponses = new SipMessage[] { CopyRequiredMessage(received) };
        return result;
    }

    SipNonInviteClientResult ReceiveFinalResponse(SipMessage received)
    {
        response = CopyRequiredMessage(received);
        state = SipTransactionState.Completed;
        CancelActiveTimers();
        timerKId = scheduler.ScheduleTimer(id, SipTimerNames.TimerK,
            reliableTransport ? 0 : scheduler.GetT4Milliseconds());
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        SipNonInviteClientResult result = CreateResult(true);
        result.DeliveredResponses = new SipMessage[] { CopyRequiredMessage(received) };
        return result;
    }

    SipNonInviteClientResult CreateResult(bool changed)
    {
        SipNonInviteClientResult result = new SipNonInviteClientResult();
        result.State = state;
        result.StateChanged = changed;
        return result;
    }

    void CancelActiveTimers()
    {
        if (timerEId >= 0)
        {
            scheduler.CancelTimer(timerEId);
            timerEId = -1;
        }
        if (timerFId >= 0)
        {
            scheduler.CancelTimer(timerFId);
            timerFId = -1;
        }
    }

    void CancelAllTimers()
    {
        scheduler.CancelOwnerTimers(id);
        timerEId = -1;
        timerFId = -1;
        timerKId = -1;
    }

    static long MultiplyMilliseconds(int value, int multiplier)
    {
        return (long)value * multiplier;
    }

    static long DoubleMilliseconds(long value)
    {
        if (value > long.MaxValue / 2)
        {
            return long.MaxValue;
        }
        return value * 2;
    }

    static long MinimumMilliseconds(long value, int maximum)
    {
        if (value > maximum)
        {
            return maximum;
        }
        return value;
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
