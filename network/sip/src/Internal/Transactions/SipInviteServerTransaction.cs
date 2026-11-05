namespace sip;

internal class SipInviteServerTransaction
{
    string id;
    SipMessage request;
    SipMessage? response;
    SipMessage? cancelResponse;
    SipTransactionKey key;
    SipTransactionKey responseKey;
    SipTimerScheduler scheduler;
    bool reliableTransport;
    bool allowLegacyMatching;
    System.Collections.Generic.List<string> acceptedToTags;
    SipTransactionState state;
    long timerGId = -1;
    long timerHId = -1;
    long timerIId = -1;
    long timerLId = -1;
    long timerGInterval;
    long createdAtMilliseconds = -1;
    long updatedAtMilliseconds = -1;

    internal SipInviteServerTransaction(string? transactionId, SipMessage? invite,
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
        key = SipTransactionMatching.CreateKey(request, SipTransactionRole.Server);
        responseKey = SipTransactionMatching.CreateKey(request, SipTransactionRole.Client);
        scheduler = new SipTimerScheduler(options.Timers, clock);
        reliableTransport = options.ReliableTransport;
        allowLegacyMatching = options.AllowLegacyMatching;
        acceptedToTags = new System.Collections.Generic.List<string>();
        state = SipTransactionState.None;
        timerGInterval = scheduler.GetT1Milliseconds();
    }

    internal SipInviteServerResult StartTransaction()
    {
        if (state != SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE server transaction has already started.");
        }

        state = SipTransactionState.Proceeding;
        createdAtMilliseconds = scheduler.GetCurrentMilliseconds();
        updatedAtMilliseconds = createdAtMilliseconds;
        response = SipTransactionMessageFactory.CreateTryingResponse(request);
        SipInviteServerResult result = CreateResult(true);
        result.OutboundMessages = new SipMessage[] { CopyRequiredMessage(response) };
        result.DeliveredRequests = new SipMessage[] { CopyRequiredMessage(request) };
        return result;
    }

    internal SipInviteServerResult SendResponse(SipMessage? sent)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE server transaction has not started.");
        }
        SipValidationIssue[] responseIssues = SipMessageValidator.ValidateMessageModel(sent, true);
        if (SipMessageValidator.HasErrorIssues(responseIssues)
            || !SipTransactionMatching.MatchResponse(responseKey, sent, allowLegacyMatching)
            || sent == null)
        {
            throw new System.ArgumentException("The response does not match this transaction.", "sent");
        }
        if (state == SipTransactionState.Accepted)
        {
            if (sent.StatusCode >= 200 && sent.StatusCode < 300)
            {
                response = CopyRequiredMessage(sent);
                RememberAcceptedToTag(sent);
                updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                SipInviteServerResult repeatedSuccess = CreateResult(false);
                repeatedSuccess.OutboundMessages = new SipMessage[] { CopyRequiredMessage(sent) };
                return repeatedSuccess;
            }
            SipInviteServerResult acceptedDiscard = CreateResult(false);
            acceptedDiscard.ResponseDiscarded = true;
            return acceptedDiscard;
        }
        if (state != SipTransactionState.Proceeding)
        {
            SipInviteServerResult discarded = CreateResult(false);
            discarded.ResponseDiscarded = true;
            return discarded;
        }

        response = CopyRequiredMessage(sent);
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        if (sent.StatusCode < 200)
        {
            SipInviteServerResult provisional = CreateResult(false);
            provisional.OutboundMessages = new SipMessage[] { CopyRequiredMessage(sent) };
            return provisional;
        }
        if (sent.StatusCode < 300)
        {
            state = SipTransactionState.Accepted;
            RememberAcceptedToTag(sent);
            timerLId = scheduler.ScheduleTimer(id, SipTimerNames.TimerL,
                MultiplyMilliseconds(scheduler.GetT1Milliseconds(), 64));
            SipInviteServerResult successful = CreateResult(true);
            successful.OutboundMessages = new SipMessage[] { CopyRequiredMessage(sent) };
            return successful;
        }

        state = SipTransactionState.Completed;
        if (!reliableTransport)
        {
            timerGId = scheduler.ScheduleTimer(id, SipTimerNames.TimerG, timerGInterval);
        }
        timerHId = scheduler.ScheduleTimer(id, SipTimerNames.TimerH,
            MultiplyMilliseconds(scheduler.GetT1Milliseconds(), 64));
        SipInviteServerResult failure = CreateResult(true);
        failure.OutboundMessages = new SipMessage[] { CopyRequiredMessage(sent) };
        return failure;
    }

    internal SipInviteServerResult ReceiveRequest(SipMessage? received)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE server transaction has not started.");
        }
        SipValidationIssue[] requestIssues = SipMessageValidator.ValidateMessageModel(received, true);
        if (SipMessageValidator.HasErrorIssues(requestIssues) || received == null)
        {
            throw new System.ArgumentException("A valid matching request is required.", "received");
        }

        if (received.Method == SipMethod.Invite)
        {
            if (!SipTransactionMatching.MatchRequest(key, received, null, allowLegacyMatching))
            {
                throw new System.ArgumentException("The request does not match this transaction.",
                    "received");
            }
            return ReceiveInviteRetransmission();
        }
        if (received.Method == SipMethod.Ack)
        {
            bool matched = state == SipTransactionState.Accepted
                ? SipTransactionMatching.MatchAcceptedAck(key, received,
                    acceptedToTags.ToArray())
                : SipTransactionMatching.MatchRequest(key, received, ReadResponseToTag(),
                    allowLegacyMatching);
            if (!matched)
            {
                throw new System.ArgumentException("The ACK does not match this transaction.",
                    "received");
            }
            return ReceiveAck(received);
        }
        throw new System.ArgumentException("An INVITE retransmission or ACK is required.",
            "received");
    }

    internal SipInviteServerResult ReceiveCancel(SipMessage? cancel)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE server transaction has not started.");
        }
        SipValidationIssue[] requestIssues = SipMessageValidator.ValidateMessageModel(cancel, true);
        if (SipMessageValidator.HasErrorIssues(requestIssues)
            || !SipTransactionMatching.MatchCancel(key, cancel, allowLegacyMatching)
            || cancel == null)
        {
            throw new System.ArgumentException("The CANCEL does not match this transaction.",
                "cancel");
        }

        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        if (cancelResponse != null)
        {
            SipInviteServerResult repeated = CreateResult(false);
            repeated.CancelMatched = true;
            repeated.RequestAbsorbed = true;
            repeated.Retransmitted = true;
            repeated.OutboundMessages = new SipMessage[] { CopyRequiredMessage(cancelResponse) };
            return repeated;
        }
        if (state == SipTransactionState.Terminated)
        {
            SipInviteServerResult late = CreateResult(false);
            late.RequestAbsorbed = true;
            return late;
        }

        string? responseTag = ReadResponseToTag();
        if (responseTag == null)
        {
            responseTag = CreateTag();
        }
        cancelResponse = SipTransactionMessageFactory.CreateCancelOk(cancel, responseTag);
        SipInviteServerResult result;
        if (state == SipTransactionState.Proceeding)
        {
            SipMessage terminated = SipTransactionMessageFactory.CreateInviteTerminated(
                request, responseTag);
            SendResponse(terminated);
            result = CreateResult(true);
            result.CancellationRequested = true;
            result.InviteTerminated = true;
            result.OutboundMessages = new SipMessage[] {
                CopyRequiredMessage(cancelResponse), CopyRequiredMessage(terminated) };
        }
        else
        {
            result = CreateResult(false);
            result.OutboundMessages = new SipMessage[] { CopyRequiredMessage(cancelResponse) };
        }
        result.CancelMatched = true;
        result.DeliveredRequests = new SipMessage[] { CopyRequiredMessage(cancel) };
        return result;
    }

    internal bool MatchesRequest(SipMessage? received)
    {
        if (received == null)
        {
            return false;
        }
        if (received.Method == SipMethod.Invite)
        {
            return SipTransactionMatching.MatchRequest(
                key, received, null, allowLegacyMatching);
        }
        if (received.Method == SipMethod.Ack)
        {
            return state == SipTransactionState.Accepted
                ? SipTransactionMatching.MatchAcceptedAck(
                    key, received, acceptedToTags.ToArray())
                : SipTransactionMatching.MatchRequest(
                    key, received, ReadResponseToTag(), allowLegacyMatching);
        }
        return false;
    }

    internal bool MatchesCancel(SipMessage? cancel)
    {
        return SipTransactionMatching.MatchCancel(key, cancel, allowLegacyMatching);
    }

    internal SipInviteServerResult AdvanceTime(long nowMilliseconds)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE server transaction has not started.");
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
            if (timer.Name != null)
            {
                fired.Add(timer.Name);
            }
            if (timer.Id == timerGId)
            {
                timerGId = -1;
                if (state == SipTransactionState.Completed && response != null)
                {
                    outbound.Add(CopyRequiredMessage(response));
                    retransmitted = true;
                    timerGInterval = MinimumMilliseconds(
                        DoubleMilliseconds(timerGInterval), scheduler.GetT2Milliseconds());
                    timerGId = scheduler.ScheduleTimer(id, SipTimerNames.TimerG, timerGInterval);
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            else if (timer.Id == timerHId)
            {
                timerHId = -1;
                if (state == SipTransactionState.Completed)
                {
                    state = SipTransactionState.Terminated;
                    timedOut = true;
                    changed = true;
                    CancelAllTimers();
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            else if (timer.Id == timerIId)
            {
                timerIId = -1;
                if (state == SipTransactionState.Confirmed)
                {
                    state = SipTransactionState.Terminated;
                    changed = true;
                    CancelAllTimers();
                    updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
                }
            }
            else if (timer.Id == timerLId)
            {
                timerLId = -1;
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

        SipInviteServerResult result = CreateResult(changed);
        result.OutboundMessages = outbound.ToArray();
        result.FiredTimerNames = fired.ToArray();
        result.TimedOut = timedOut;
        result.Retransmitted = retransmitted;
        if (timedOut)
        {
            result.ErrorMessage = "The INVITE server transaction timed out waiting for ACK.";
        }
        return result;
    }

    internal SipInviteServerResult HandleTransportError(string? errorMessage)
    {
        if (state == SipTransactionState.None)
        {
            throw new System.InvalidOperationException("The INVITE server transaction has not started.");
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

        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        SipInviteServerResult result = CreateResult(false);
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

    SipInviteServerResult ReceiveInviteRetransmission()
    {
        SipInviteServerResult result = CreateResult(false);
        result.RequestAbsorbed = true;
        if ((state == SipTransactionState.Proceeding || state == SipTransactionState.Completed)
            && response != null)
        {
            result.OutboundMessages = new SipMessage[] { CopyRequiredMessage(response) };
            result.Retransmitted = true;
        }
        updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
        return result;
    }

    SipInviteServerResult ReceiveAck(SipMessage received)
    {
        SipInviteServerResult result;
        if (state == SipTransactionState.Accepted)
        {
            updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
            result = CreateResult(false);
            result.AckDelivered = true;
            result.DeliveredRequests = new SipMessage[] { CopyRequiredMessage(received) };
        }
        else if (state == SipTransactionState.Completed)
        {
            state = SipTransactionState.Confirmed;
            CancelCompletedTimers();
            timerIId = scheduler.ScheduleTimer(id, SipTimerNames.TimerI,
                reliableTransport ? 0 : scheduler.GetT4Milliseconds());
            updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
            result = CreateResult(true);
        }
        else
        {
            updatedAtMilliseconds = scheduler.GetCurrentMilliseconds();
            result = CreateResult(false);
        }
        if (!result.AckDelivered)
        {
            result.AckAbsorbed = true;
        }
        return result;
    }

    void RememberAcceptedToTag(SipMessage sent)
    {
        string? tag = ReadTag(sent.To);
        if (tag == null)
        {
            return;
        }
        int index = 0;
        while (index < acceptedToTags.Count)
        {
            if (acceptedToTags[index] == tag)
            {
                return;
            }
            index = index + 1;
        }
        acceptedToTags.Add(tag);
    }

    string? ReadResponseToTag()
    {
        if (response == null)
        {
            return null;
        }
        return ReadTag(response.To);
    }

    static string? ReadTag(SipNameAddress? address)
    {
        if (address == null || address.Parameters == null)
        {
            return null;
        }
        int index = 0;
        while (index < address.Parameters.Length)
        {
            SipUriParameter? parameter = address.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "tag"))
            {
                return parameter.Value;
            }
            index = index + 1;
        }
        return null;
    }

    SipInviteServerResult CreateResult(bool changed)
    {
        SipInviteServerResult result = new SipInviteServerResult();
        result.State = state;
        result.StateChanged = changed;
        return result;
    }

    void CancelCompletedTimers()
    {
        if (timerGId >= 0)
        {
            scheduler.CancelTimer(timerGId);
            timerGId = -1;
        }
        if (timerHId >= 0)
        {
            scheduler.CancelTimer(timerHId);
            timerHId = -1;
        }
    }

    void CancelAllTimers()
    {
        scheduler.CancelOwnerTimers(id);
        timerGId = -1;
        timerHId = -1;
        timerIId = -1;
        timerLId = -1;
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

    static string CreateTag()
    {
        SipProtocolTokenGenerator generator = new SipProtocolTokenGenerator(
            new SipCryptographicRandomSource());
        return generator.CreateTag();
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
