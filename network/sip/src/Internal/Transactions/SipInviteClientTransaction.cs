namespace sip;

internal class SipInviteClientTransaction
{
    string id;
    SipMessage request;
    SipMessage? response;
    SipTransactionKey key;
    SipTimerScheduler scheduler;
    bool reliableTransport;
    bool allowLegacyMatching;
    bool cancellationRequested;
    bool cancelSent;
    SipTransactionState state;
    long timerAId = -1;
    long timerBId = -1;
    long timerDId = -1;
    long timerMId = -1;
    long timerAInterval;
    long createdAtMilliseconds = -1;
    long updatedAtMilliseconds = -1;

    internal SipInviteClientTransaction(string? transactionId, SipMessage? invite,
        SipTransactionOptions? options, ISipMonotonicClock? clock)
    {
        if (transactionId == null || transactionId.Length == 0
            || transactionId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException("A bounded transaction identifier is required.",
                "transactionId");
        }
        if (invite == null || invite.Method != SipMethod.Invite
            || invite.MethodToken != SipMethods.Invite)
        {
            throw new System.ArgumentException("An INVITE request is required.", "invite");
        }
        SipValidationIssue[] requestIssues = SipMessageValidator.ValidateMessageModel(invite, true);
        if (SipMessageValidator.HasErrorIssues(requestIssues))
        {
            throw new System.ArgumentException("A valid INVITE request is required.", "invite");
        }
        if (options == null || options.Timers == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (!options.ReliableTransport && options.Timers.MaxTimerCount < 2)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        SipMessage? copiedRequest = SipSnapshotCopy.CopyMessageModel(invite);
        if (copiedRequest == null)
        {
            throw new System.ArgumentException("An INVITE request is required.", "invite");
        }

        id = transactionId;
        request = copiedRequest;
        key = SipTransactionMatching.CreateKey(request, SipTransactionRole.Client);
        scheduler = new SipTimerScheduler(options.Timers, clock);
        reliableTransport = options.ReliableTransport;
        allowLegacyMatching = options.AllowLegacyMatching;
        state = SipTransactionState.None;
        timerAInterval = scheduler.GetT1Milliseconds();
    }

    internal SipInviteClientResult StartTransaction()
    {
        if (state != SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE client transaction has already started.");
        }

        state = SipTransactionState.Calling;
        createdAtMilliseconds = scheduler.GetCurrentMilliseconds();
        updatedAtMilliseconds = createdAtMilliseconds;
        if (!reliableTransport)
        {
            timerAId = scheduler.ScheduleTimer(id, SipTimerNames.TimerA, timerAInterval);
        }
        timerBId = scheduler.ScheduleTimer(id, SipTimerNames.TimerB,
            MultiplyMilliseconds(scheduler.GetT1Milliseconds(), 64));

        SipInviteClientResult result = CreateResult(true);
        result.OutboundMessages = new SipMessage[] { CopyRequiredMessage(request) };
        return result;
    }

    internal SipInviteClientResult ReceiveResponse(SipMessage? received)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE client transaction has not started.");
        }
        SipValidationIssue[] responseIssues = SipMessageValidator.ValidateMessageModel(received, true);
        if (SipMessageValidator.HasErrorIssues(responseIssues)
            || !SipTransactionMatching.MatchResponse(key, received, allowLegacyMatching)
            || received == null)
        {
            throw new System.ArgumentException("The response does not match this transaction.",
                "received");
        }

        if (state == SipTransactionState.Terminated)
        {
            return CreateResult(false);
        }
        if (received.StatusCode < 200)
        {
            return ReceiveProvisionalResponse(received);
        }
        if (received.StatusCode < 300)
        {
            return ReceiveSuccessfulResponse(received);
        }
        return ReceiveFailureResponse(received);
    }

    internal SipInviteClientResult RequestCancel()
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE client transaction has not started.");
        }

        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        if (state == SipTransactionState.Completed || state == SipTransactionState.Accepted
            || state == SipTransactionState.Terminated)
        {
            SipInviteClientResult suppressed = CreateResult(false);
            suppressed.CancelSuppressed = true;
            return suppressed;
        }

        cancellationRequested = true;
        SipInviteClientResult result = CreateResult(false);
        if (state == SipTransactionState.Calling)
        {
            result.CancellationPending = true;
            return result;
        }
        if (!cancelSent)
        {
            SendCancel(result);
        }
        return result;
    }

    internal SipInviteClientResult AdvanceTime(long nowMilliseconds)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE client transaction has not started.");
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
            if (timer.Id == timerAId)
            {
                timerAId = -1;
                if (state == SipTransactionState.Calling)
                {
                    outbound.Add(CopyRequiredMessage(request));
                    retransmitted = true;
                    timerAInterval = DoubleMilliseconds(timerAInterval);
                    timerAId = scheduler.ScheduleTimer(id, SipTimerNames.TimerA, timerAInterval);
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            else if (timer.Id == timerBId)
            {
                timerBId = -1;
                if (state == SipTransactionState.Calling
                    || state == SipTransactionState.Proceeding && cancelSent)
                {
                    state = SipTransactionState.Terminated;
                    timedOut = true;
                    changed = true;
                    CancelAllTimers();
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            else if (timer.Id == timerDId)
            {
                timerDId = -1;
                if (state == SipTransactionState.Completed)
                {
                    state = SipTransactionState.Terminated;
                    changed = true;
                    CancelAllTimers();
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            else if (timer.Id == timerMId)
            {
                timerMId = -1;
                if (state == SipTransactionState.Accepted)
                {
                    state = SipTransactionState.Terminated;
                    changed = true;
                    CancelAllTimers();
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            index = index + 1;
        }

        SipInviteClientResult result = CreateResult(changed);
        result.OutboundMessages = outbound.ToArray();
        result.FiredTimerNames = fired.ToArray();
        result.TimedOut = timedOut;
        result.Retransmitted = retransmitted;
        if (timedOut)
        {
            result.ErrorMessage = "The INVITE client transaction timed out.";
        }
        return result;
    }

    internal SipInviteClientResult HandleTransportError(string? errorMessage)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE client transaction has not started.");
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
        SipInviteClientResult result = CreateResult(true);
        result.TransportFailed = true;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal SipTransactionSnapshot GetSnapshot()
    {
        SipTransactionSnapshot snapshot = new SipTransactionSnapshot();
        snapshot.Id = id;
        snapshot.Key = key;
        snapshot.Kind = SipTransactionKind.Invite;
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

    SipInviteClientResult ReceiveProvisionalResponse(SipMessage received)
    {
        if (state != SipTransactionState.Calling && state != SipTransactionState.Proceeding)
        {
            return CreateResult(false);
        }

        bool changed = state == SipTransactionState.Calling;
        state = SipTransactionState.Proceeding;
        response = CopyRequiredMessage(received);
        CancelCallingTimers();
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        SipInviteClientResult result = CreateResult(changed);
        result.DeliveredResponses = new SipMessage[] { CopyRequiredMessage(received) };
        if (cancellationRequested && !cancelSent)
        {
            SendCancel(result);
        }
        return result;
    }

    SipInviteClientResult ReceiveSuccessfulResponse(SipMessage received)
    {
        if (state == SipTransactionState.Accepted)
        {
            response = CopyRequiredMessage(received);
            updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
            SipInviteClientResult repeated = CreateResult(false);
            repeated.DeliveredResponses = new SipMessage[] { CopyRequiredMessage(received) };
            return repeated;
        }
        if (state != SipTransactionState.Calling && state != SipTransactionState.Proceeding)
        {
            return CreateResult(false);
        }

        response = CopyRequiredMessage(received);
        state = SipTransactionState.Accepted;
        CancelCallingTimers();
        timerMId = scheduler.ScheduleTimer(id, SipTimerNames.TimerM,
            MultiplyMilliseconds(scheduler.GetT1Milliseconds(), 64));
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        SipInviteClientResult result = CreateResult(true);
        result.DeliveredResponses = new SipMessage[] { CopyRequiredMessage(received) };
        if (cancellationRequested && !cancelSent)
        {
            result.CancelSuppressed = true;
        }
        return result;
    }

    SipInviteClientResult ReceiveFailureResponse(SipMessage received)
    {
        if (state == SipTransactionState.Completed)
        {
            response = CopyRequiredMessage(received);
            updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
            SipInviteClientResult repeated = CreateResult(false);
            repeated.OutboundMessages = new SipMessage[] {
                SipTransactionMessageFactory.CreateFailureAck(request, received) };
            repeated.Retransmitted = true;
            return repeated;
        }
        if (state != SipTransactionState.Calling && state != SipTransactionState.Proceeding)
        {
            return CreateResult(false);
        }

        response = CopyRequiredMessage(received);
        state = SipTransactionState.Completed;
        CancelCallingTimers();
        timerDId = scheduler.ScheduleTimer(id, SipTimerNames.TimerD,
            reliableTransport ? 0 : 32000);
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        SipInviteClientResult result = CreateResult(true);
        result.DeliveredResponses = new SipMessage[] { CopyRequiredMessage(received) };
        result.OutboundMessages = new SipMessage[] {
            SipTransactionMessageFactory.CreateFailureAck(request, received) };
        if (cancellationRequested && !cancelSent)
        {
            result.CancelSuppressed = true;
        }
        return result;
    }

    void SendCancel(SipInviteClientResult result)
    {
        cancelSent = true;
        result.CancelSent = true;
        result.OutboundMessages = new SipMessage[] {
            SipTransactionMessageFactory.CreateCancel(request) };
        if (timerBId >= 0)
        {
            scheduler.CancelTimer(timerBId);
        }
        timerBId = scheduler.ScheduleTimer(id, SipTimerNames.TimerB,
            MultiplyMilliseconds(scheduler.GetT1Milliseconds(), 64));
    }

    SipInviteClientResult CreateResult(bool changed)
    {
        SipInviteClientResult result = new SipInviteClientResult();
        result.State = state;
        result.StateChanged = changed;
        return result;
    }

    void CancelCallingTimers()
    {
        if (timerAId >= 0)
        {
            scheduler.CancelTimer(timerAId);
            timerAId = -1;
        }
        if (timerBId >= 0)
        {
            scheduler.CancelTimer(timerBId);
            timerBId = -1;
        }
    }

    void CancelAllTimers()
    {
        scheduler.CancelOwnerTimers(id);
        timerAId = -1;
        timerBId = -1;
        timerDId = -1;
        timerMId = -1;
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
