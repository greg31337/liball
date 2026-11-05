namespace sip;

/// <summary>Creates, finds, updates, and terminates SIP dialogs.</summary>
public class SipDialogLayer : ISipDialogLayer
{
    object gate = new object();
    System.Collections.Generic.List<SipDialogLayerEntry> dialogs =
        new System.Collections.Generic.List<SipDialogLayerEntry>();
    System.Collections.Generic.List<SipDialogEvent> events =
        new System.Collections.Generic.List<SipDialogEvent>();
    System.Collections.Generic.List<SipDialogSnapshot> snapshots =
        new System.Collections.Generic.List<SipDialogSnapshot>();
    SipProtocolTokenGenerator tokens;

    /// <summary>Initializes an empty dialog layer.</summary>
    public SipDialogLayer()
    {
        tokens = new SipProtocolTokenGenerator(new SipCryptographicRandomSource());
    }

    /// <summary>Creates a dialog from a request and response.</summary>
    /// <param name="request">The dialog-forming request.</param>
    /// <param name="response">The provisional or final response.</param>
    /// <param name="options">The dialog options.</param>
    /// <returns>The created dialog snapshot.</returns>
    public SipDialogSnapshot CreateDialog(
        SipMessage request, SipMessage response, SipDialogOptions options)
    {
        lock (gate)
        {
            ValidateDialogPair(request, response, options);
            SipDialogSnapshot candidate = BuildDialogSnapshot(request, response, options);
            int existingIndex = FindDialogIndex(candidate.Id);
            if (existingIndex >= 0)
            {
                SipDialogLayerEntry existingEntry = dialogs[existingIndex];
                SipDialogSnapshot existing = existingEntry.Snapshot;
                if (existing.Role != candidate.Role)
                {
                    throw new SipDialogException(
                        "The dialog identifier belongs to another local role.",
                        candidate.Id, existing.State);
                }
                if (existing.State == SipDialogState.Early
                    && candidate.State == SipDialogState.Confirmed)
                {
                    ValidateSecureDialogState(candidate,
                        existingEntry.PreventSecureDowngrade);
                    EnsureDialogQueueCapacity(existingEntry.MaximumEventCount,
                        1, 1, existing);
                    existing.State = SipDialogState.Confirmed;
                    existing.RouteSet = candidate.RouteSet;
                    if (existing.Role == SipDialogRole.Uac)
                    {
                        existing.RemoteTarget = candidate.RemoteTarget;
                    }
                    else
                    {
                        existing.LocalTarget = candidate.LocalTarget;
                    }
                    QueueDialogEvent(existing, response, "state-changed",
                        "The early dialog was confirmed.", existingEntry.InviteUsageId);
                }
                else
                {
                    EnsureDialogQueueCapacity(existingEntry.MaximumEventCount,
                        1, 1, existing);
                    QueueDialogEvent(existing, response, "updated",
                        "The dialog-forming response was observed.",
                        existingEntry.InviteUsageId);
                }
                return CopyRequiredDialogSnapshot(existing);
            }
            if (dialogs.Count >= options.MaxDialogCount)
            {
                throw new SipDialogException(
                    "The live dialog limit has been reached.", candidate.Id,
                    SipDialogState.None);
            }

            SipDialogLayerEntry added = new SipDialogLayerEntry();
            string inviteUsageId = CreateUsageIdentifier();
            candidate.UsageIds = new string[] { inviteUsageId };
            candidate.CorrelationId = options.CorrelationId == null
                || options.CorrelationId.Length == 0
                ? inviteUsageId : options.CorrelationId;
            added.Snapshot = candidate;
            added.InitialInviteCSeq = request.CSeq == null
                ? -1 : request.CSeq.SequenceNumber;
            added.InviteUsageId = inviteUsageId;
            added.MaximumUsageCount = options.MaxUsageCount;
            added.MaximumTransactionCount = options.MaxTransactionCount;
            added.MaximumEventCount = options.MaxEventCount;
            added.StrictRemoteCSeq = options.StrictRemoteCSeq;
            added.PreventSecureDowngrade = options.PreventSecureDowngrade;
            EnsureDialogQueueCapacity(added.MaximumEventCount, 1, 1, candidate);
            dialogs.Add(added);
            QueueDialogEvent(candidate, response, "created",
                candidate.State == SipDialogState.Early
                    ? "An early dialog was created."
                    : "A confirmed dialog was created.", inviteUsageId);
            return CopyRequiredDialogSnapshot(candidate);
        }
    }

    /// <summary>Finds a dialog by identifier.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <returns>The matching snapshot.</returns>
    public SipDialogSnapshot FindDialog(SipDialogId id)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            return CopyRequiredDialogSnapshot(dialogs[index].Snapshot);
        }
    }

    /// <summary>Finds the live dialog matching a message.</summary>
    /// <param name="message">The in-dialog message.</param>
    /// <returns>The matching snapshot.</returns>
    public SipDialogSnapshot FindDialogByMessage(SipMessage message)
    {
        lock (gate)
        {
            ValidateDialogLookupMessage(message);
            int index = 0;
            while (index < dialogs.Count)
            {
                if (MessageHasDialogIdentifier(dialogs[index].Snapshot, message))
                {
                    return CopyRequiredDialogSnapshot(dialogs[index].Snapshot);
                }
                index = index + 1;
            }
            throw new SipDialogException(
                "The message does not match a live dialog.", null,
                SipDialogState.Terminated);
        }
    }

    /// <summary>Finds the live dialog associated with a transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <returns>The matching snapshot.</returns>
    public SipDialogSnapshot FindDialogByTransaction(string transactionId)
    {
        lock (gate)
        {
            ValidateAssociationIdentifier(transactionId, "transactionId");
            int index = FindTransactionDialogIndex(transactionId);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The transaction is not associated with a live dialog.",
                    null, SipDialogState.Terminated);
            }
            return CopyRequiredDialogSnapshot(dialogs[index].Snapshot);
        }
    }

    /// <summary>Constructs the next local request within a dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="method">The request method token.</param>
    /// <returns>The constructed request.</returns>
    public SipMessage CreateRequest(SipDialogId id, string method)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            if (!SipAscii.IsToken(method) || method == SipMethods.Cancel)
            {
                throw new System.ArgumentException(
                    "A valid in-dialog request method is required.", "method");
            }
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            SipUri? contact = IsTargetRefreshMethodToken(method)
                ? entry.Snapshot.LocalTarget : null;
            return CreateDialogRequest(entry, method, contact);
        }
    }

    /// <summary>Constructs a target-refresh request with a new local target.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="method">The INVITE or UPDATE method token.</param>
    /// <param name="localTarget">The local Contact URI to advertise.</param>
    /// <returns>The constructed target-refresh request.</returns>
    public SipMessage CreateTargetRefreshRequest(
        SipDialogId id, string method, SipUri localTarget)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            if (!IsTargetRefreshMethodToken(method))
            {
                throw new System.ArgumentException(
                    "A target refresh requires INVITE or UPDATE.", "method");
            }
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            SipUri target = CopyTargetRefreshUri(entry, localTarget, "localTarget");
            return CreateDialogRequest(entry, method, target);
        }
    }

    /// <summary>Creates another usage sharing an existing dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <returns>The new application-visible usage identifier.</returns>
    public string CreateUsage(SipDialogId id)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            if (entry.Snapshot.UsageIds.Length >= entry.MaximumUsageCount)
            {
                throw new SipDialogException(
                    "The dialog usage limit has been reached.", id,
                    entry.Snapshot.State);
            }
            EnsureDialogQueueCapacity(entry.MaximumEventCount,
                1, 1, entry.Snapshot);
            string usageId = CreateUsageIdentifier();
            entry.Snapshot.UsageIds = AppendUsageIdentifier(
                entry.Snapshot.UsageIds, usageId);
            QueueDialogEvent(entry.Snapshot, null, "usage-created",
                "A dialog usage was created.", usageId);
            return usageId;
        }
    }

    /// <summary>Terminates one usage without affecting sibling usages.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="usageId">The usage identifier.</param>
    /// <param name="cancellation">The termination request.</param>
    public void TerminateUsage(
        SipDialogId id, string usageId, SipCancellation cancellation)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            ValidateUsageTermination(usageId, cancellation);
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            if (FindIdentifierIndex(entry.Snapshot.UsageIds, usageId) < 0)
            {
                throw new SipDialogException(
                    "The dialog usage is not live.", id, entry.Snapshot.State);
            }
            EnsureDialogQueueCapacity(entry.MaximumEventCount,
                1, 1, entry.Snapshot);
            RemoveUsageIdentifier(entry, usageId);
            CompleteUsageTermination(entry, index, usageId, cancellation.Reason);
        }
    }

    /// <summary>Associates a live transaction with a dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    public void AssociateTransaction(SipDialogId id, string transactionId)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            ValidateAssociationIdentifier(transactionId, "transactionId");
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            int associatedIndex = FindTransactionDialogIndex(transactionId);
            if (associatedIndex == index)
            {
                return;
            }
            if (associatedIndex >= 0)
            {
                throw new SipDialogException(
                    "The transaction belongs to another dialog.", id,
                    entry.Snapshot.State);
            }
            if (entry.Snapshot.TransactionIds.Length
                >= entry.MaximumTransactionCount)
            {
                throw new SipDialogException(
                    "The dialog transaction association limit has been reached.",
                    id, entry.Snapshot.State);
            }
            EnsureDialogQueueCapacity(entry.MaximumEventCount,
                1, 1, entry.Snapshot);
            entry.Snapshot.TransactionIds = AppendTransactionIdentifier(
                entry.Snapshot.TransactionIds, transactionId);
            QueueDialogEvent(entry.Snapshot, null, "transaction-associated",
                "A transaction was associated with the dialog.", null);
        }
    }

    /// <summary>Removes a transaction association from a dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    public void DissociateTransaction(SipDialogId id, string transactionId)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            ValidateAssociationIdentifier(transactionId, "transactionId");
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            int transactionIndex = FindIdentifierIndex(
                entry.Snapshot.TransactionIds, transactionId);
            if (transactionIndex < 0)
            {
                throw new SipDialogException(
                    "The transaction is not associated with the dialog.", id,
                    entry.Snapshot.State);
            }
            EnsureDialogQueueCapacity(entry.MaximumEventCount,
                1, 1, entry.Snapshot);
            entry.Snapshot.TransactionIds = RemoveIdentifierAt(
                entry.Snapshot.TransactionIds, transactionIndex);
            QueueDialogEvent(entry.Snapshot, null, "transaction-dissociated",
                "A transaction association was removed from the dialog.", null);
        }
    }

    /// <summary>Associates an authentication context with a dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="authenticationContextId">The authentication context identifier.</param>
    public void AssociateAuthenticationContext(
        SipDialogId id, string authenticationContextId)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            ValidateAssociationIdentifier(
                authenticationContextId, "authenticationContextId");
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            if (entry.Snapshot.AuthenticationContextId == authenticationContextId)
            {
                return;
            }
            EnsureDialogQueueCapacity(entry.MaximumEventCount,
                1, 1, entry.Snapshot);
            entry.Snapshot.AuthenticationContextId = authenticationContextId;
            QueueDialogEvent(entry.Snapshot, null, "authentication-associated",
                "An authentication context was associated with the dialog.", null);
        }
    }

    /// <summary>Clears the dialog authentication context association.</summary>
    /// <param name="id">The dialog identifier.</param>
    public void ClearAuthenticationContext(SipDialogId id)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            if (entry.Snapshot.AuthenticationContextId == null)
            {
                return;
            }
            EnsureDialogQueueCapacity(entry.MaximumEventCount,
                1, 1, entry.Snapshot);
            entry.Snapshot.AuthenticationContextId = null;
            QueueDialogEvent(entry.Snapshot, null, "authentication-cleared",
                "The dialog authentication context was cleared.", null);
        }
    }

    /// <summary>Processes an in-dialog message.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="message">The in-dialog message.</param>
    /// <returns>The updated snapshot.</returns>
    public SipDialogSnapshot UpdateDialog(SipDialogId id, SipMessage message)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                index = FindEarlyFailureIndex(message);
                if (index < 0)
                {
                    throw new SipDialogException(
                        "The dialog is not live.", id, SipDialogState.Terminated);
                }
            }
            SipDialogLayerEntry entry = dialogs[index];
            SipDialogSnapshot dialog = entry.Snapshot;
            if (IsInitialInviteFailure(entry, message))
            {
                EnsureEarlyFailureQueueCapacity(entry);
                return TerminateEarlyDialogs(entry, message);
            }
            ValidateDialogMessage(entry, message);
            ValidateLocalByeResponse(entry, message);
            if (message.Kind == SipMessageKind.Request)
            {
                ValidateTargetRefreshRequest(entry, message);
            }
            else
            {
                ValidateTargetRefreshResponse(entry, message);
            }
            EnsureDialogQueueCapacity(entry.MaximumEventCount,
                1, 1, dialog);
            SipDialogState previous = dialog.State;
            string? terminatedUsageId = null;
            if (message.Kind == SipMessageKind.Response)
            {
                ProcessTargetRefreshResponse(entry, message);
                if (IsInitialInviteSuccess(entry, message))
                {
                    SipUri finalTarget = ReadSingleContactUri(message, "message");
                    if (dialog.Role == SipDialogRole.Uac)
                    {
                        SipRouteValue[] replacement = SipRouteRouting.ReverseRouteSet(
                            message.RecordRouteValues);
                        ValidateSecureDialogRouteSet(dialog, replacement,
                            entry.PreventSecureDowngrade);
                        dialog.RouteSet = replacement;
                        dialog.RemoteTarget = finalTarget;
                    }
                    else
                    {
                        dialog.LocalTarget = finalTarget;
                    }
                    dialog.State = SipDialogState.Confirmed;
                }
            }
            else
            {
                ApplyRemoteSequence(entry, message);
                ProcessTargetRefreshRequest(entry, message);
                if (message.Method == SipMethod.Bye
                    && MessageComesFromRemote(dialog, message))
                {
                    terminatedUsageId = TerminateInviteUsage(entry);
                }
            }
            if (message.Kind == SipMessageKind.Response
                && message.CSeq != null && message.CSeq.Method == SipMethod.Bye
                && message.CSeq.SequenceNumber == entry.LocalByeCSeq
                && message.StatusCode >= 200)
            {
                terminatedUsageId = TerminateInviteUsage(entry);
            }

            string kind = terminatedUsageId == null
                ? previous == dialog.State ? "updated" : "state-changed"
                : dialog.State == SipDialogState.Terminated
                    ? "state-changed" : "usage-terminated";
            string reason = terminatedUsageId != null
                ? message.Kind == SipMessageKind.Request
                    ? "The remote endpoint terminated the INVITE usage with BYE."
                    : "The local BYE operation completed the INVITE usage."
                : dialog.State == SipDialogState.Confirmed
                    && previous == SipDialogState.Early
                    ? "The early dialog was confirmed."
                    : "The dialog message was observed.";
            QueueDialogEvent(dialog, message, kind, reason, terminatedUsageId);
            SipDialogSnapshot result = CopyRequiredDialogSnapshot(dialog);
            if (dialog.State == SipDialogState.Terminated)
            {
                dialogs.RemoveAt(index);
            }
            return result;
        }
    }

    /// <summary>Terminates one dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="cancellation">The termination request.</param>
    public void TerminateDialog(SipDialogId id, SipCancellation cancellation)
    {
        lock (gate)
        {
            ValidateDialogIdentifier(id);
            if (cancellation == null || cancellation.Reason != null
                && cancellation.Reason.Length > SipDefaults.MaxHeaderLineBytes)
            {
                throw new System.ArgumentException(
                    "A valid dialog termination request is required.", "cancellation");
            }
            int index = FindDialogIndex(id);
            if (index < 0)
            {
                throw new SipDialogException(
                    "The dialog is not live.", id, SipDialogState.Terminated);
            }
            SipDialogLayerEntry entry = dialogs[index];
            SipDialogSnapshot dialog = entry.Snapshot;
            EnsureDialogQueueCapacity(entry.MaximumEventCount,
                1, 1, dialog);
            dialog.State = SipDialogState.Terminated;
            dialog.UsageIds = new string[0];
            ClearDialogAssociations(dialog);
            entry.InviteUsageId = null;
            string reason = cancellation.Reason == null || cancellation.Reason.Length == 0
                ? "The dialog was terminated."
                : cancellation.Reason;
            QueueDialogEvent(dialog, null, "terminated", reason, null);
            dialogs.RemoveAt(index);
        }
    }

    /// <summary>Polls the next dialog event.</summary>
    /// <returns>The next event.</returns>
    public SipDialogEvent PollDialogEvent()
    {
        lock (gate)
        {
            if (events.Count == 0)
            {
                return new SipDialogEvent();
            }
            SipDialogEvent source = events[0];
            events.RemoveAt(0);
            SipDialogEvent? copy = SipSnapshotCopy.CopyDialogEvent(source);
            return copy == null ? new SipDialogEvent() : copy;
        }
    }

    /// <summary>Polls the next queued dialog snapshot.</summary>
    /// <returns>The next snapshot, or an empty snapshot when none is available.</returns>
    public SipDialogSnapshot PollDialogSnapshot()
    {
        lock (gate)
        {
            if (snapshots.Count == 0)
            {
                return new SipDialogSnapshot();
            }
            SipDialogSnapshot source = snapshots[0];
            snapshots.RemoveAt(0);
            return CopyRequiredDialogSnapshot(source);
        }
    }

    void ValidateDialogPair(SipMessage? request, SipMessage? response,
        SipDialogOptions? options)
    {
        if (options == null || options.MaxDialogCount <= 0
            || options.MaxUsageCount <= 0 || options.MaxTransactionCount <= 0
            || options.MaxEventCount <= 0
            || options.CorrelationId != null
                && options.CorrelationId.Length > SipDefaults.MaxHeaderLineBytes
            || options.Role != SipDialogRole.Uac && options.Role != SipDialogRole.Uas)
        {
            throw new System.ArgumentException("Valid dialog options are required.", "options");
        }
        SipValidationIssue[] requestIssues = SipMessageValidator.ValidateMessageModel(
            request, true);
        SipValidationIssue[] responseIssues = SipMessageValidator.ValidateMessageModel(
            response, true);
        if (request == null || response == null
            || SipMessageValidator.HasErrorIssues(requestIssues)
            || SipMessageValidator.HasErrorIssues(responseIssues)
            || request.Kind != SipMessageKind.Request
            || request.Method != SipMethod.Invite
            || response.Kind != SipMessageKind.Response
            || response.StatusCode <= 100 || response.StatusCode >= 300
            || request.CSeq == null || response.CSeq == null
            || response.CSeq.Method != SipMethod.Invite
            || request.CSeq.SequenceNumber != response.CSeq.SequenceNumber
            || !DialogPairHeadersMatch(request, response))
        {
            throw new System.ArgumentException(
                "A matching INVITE request and dialog-forming response are required.",
                "response");
        }
        ReadSingleContactUri(request, "request");
        ReadSingleContactUri(response, "response");
        string? localTag = options.Role == SipDialogRole.Uac
            ? ReadAddressTag(response.From) : ReadAddressTag(response.To);
        string? remoteTag = options.Role == SipDialogRole.Uac
            ? ReadAddressTag(response.To) : ReadAddressTag(response.From);
        if (localTag == null || localTag.Length == 0
            || remoteTag == null || remoteTag.Length == 0)
        {
            throw new System.ArgumentException(
                "The dialog-forming response requires valid dialog tags.", "response");
        }
    }

    bool DialogPairHeadersMatch(SipMessage request, SipMessage response)
    {
        string? requestCallId = ReadMessageCallId(request);
        string? responseCallId = ReadMessageCallId(response);
        return requestCallId != null && requestCallId == responseCallId
            && request.From != null && response.From != null
            && request.To != null && response.To != null
            && request.From.Uri != null && response.From.Uri != null
            && request.To.Uri != null && response.To.Uri != null
            && SipUriComparison.CompareUriModels(request.From.Uri, response.From.Uri)
            && SipUriComparison.CompareUriModels(request.To.Uri, response.To.Uri)
            && ReadAddressTag(request.From) == ReadAddressTag(response.From);
    }

    SipDialogSnapshot BuildDialogSnapshot(SipMessage request, SipMessage response,
        SipDialogOptions options)
    {
        SipDialogSnapshot snapshot = new SipDialogSnapshot();
        snapshot.Id = BuildDialogIdentifier(response, options.Role);
        snapshot.Role = options.Role;
        snapshot.State = response.StatusCode < 200
            ? SipDialogState.Early : SipDialogState.Confirmed;
        if (options.Role == SipDialogRole.Uac)
        {
            snapshot.LocalUri = CopyRequiredDialogUri(response.From == null
                ? null : response.From.Uri, "response");
            snapshot.RemoteUri = CopyRequiredDialogUri(response.To == null
                ? null : response.To.Uri, "response");
            snapshot.LocalTarget = ReadSingleContactUri(request, "request");
            snapshot.RemoteTarget = ReadSingleContactUri(response, "response");
            snapshot.LocalCSeq = request.CSeq == null ? -1 : request.CSeq.SequenceNumber;
        }
        else
        {
            snapshot.LocalUri = CopyRequiredDialogUri(response.To == null
                ? null : response.To.Uri, "response");
            snapshot.RemoteUri = CopyRequiredDialogUri(response.From == null
                ? null : response.From.Uri, "response");
            snapshot.LocalTarget = ReadSingleContactUri(response, "response");
            snapshot.RemoteTarget = ReadSingleContactUri(request, "request");
            snapshot.RemoteCSeq = request.CSeq == null ? -1 : request.CSeq.SequenceNumber;
        }
        snapshot.Secure = RequestCreatesSecureDialog(request);
        snapshot.RouteSet = options.Role == SipDialogRole.Uac
            ? SipRouteRouting.ReverseRouteSet(response.RecordRouteValues)
            : SipRouteRouting.CopyRouteSet(request.RecordRouteValues);
        ValidateSecureDialogState(snapshot, options.PreventSecureDowngrade);
        return snapshot;
    }

    void ValidateSecureDialogState(SipDialogSnapshot dialog, bool preventDowngrade)
    {
        if (!preventDowngrade || !dialog.Secure)
        {
            return;
        }
        if (dialog.LocalTarget == null || dialog.RemoteTarget == null
            || dialog.LocalTarget.Scheme != SipUriScheme.Sips
            || dialog.RemoteTarget.Scheme != SipUriScheme.Sips)
        {
            throw new SipProtocolException(
                "A secure dialog requires SIPS local and remote targets.",
                SipStatusCodes.UnsupportedUriScheme, SipMethods.Invite);
        }
        ValidateSecureDialogRouteSet(dialog, dialog.RouteSet, preventDowngrade);
    }

    void ValidateSecureDialogRouteSet(
        SipDialogSnapshot dialog, SipRouteValue[] routeSet, bool preventDowngrade)
    {
        if (!preventDowngrade || !dialog.Secure)
        {
            return;
        }
        int index = 0;
        while (index < routeSet.Length)
        {
            SipRouteValue route = routeSet[index];
            SipUri? uri = route.Address == null ? null : route.Address.Uri;
            if (uri == null || uri.Scheme != SipUriScheme.Sips)
            {
                throw new SipProtocolException(
                    "A secure dialog requires a SIPS route set.",
                    SipStatusCodes.UnsupportedUriScheme, SipMethods.Invite);
            }
            index = index + 1;
        }
    }

    void ApplyRemoteSequence(SipDialogLayerEntry entry, SipMessage message)
    {
        SipDialogSnapshot dialog = entry.Snapshot;
        if (dialog.Id == null || message.CSeq == null
            || message.From == null || message.To == null)
        {
            throw new System.ArgumentException(
                "The in-dialog request requires dialog sequence state.", "message");
        }
        if (entry.PreventSecureDowngrade && dialog.Secure
            && (message.RequestUri == null
                || message.RequestUri.Scheme != SipUriScheme.Sips))
        {
            throw new SipProtocolException(
                "A secure dialog request cannot downgrade its request target.",
                SipStatusCodes.UnsupportedUriScheme, message.MethodToken);
        }
        string? fromTag = ReadAddressTag(message.From);
        string? toTag = ReadAddressTag(message.To);
        if (fromTag == dialog.Id.LocalTag && toTag == dialog.Id.RemoteTag)
        {
            return;
        }
        if (message.Method == SipMethod.Ack || message.Method == SipMethod.Cancel)
        {
            return;
        }
        int sequence = message.CSeq.SequenceNumber;
        if (entry.StrictRemoteCSeq && dialog.RemoteCSeq >= 0
            && sequence <= dialog.RemoteCSeq)
        {
            throw new SipDialogException(
                "The remote dialog sequence did not increase.", dialog.Id,
                dialog.State);
        }
        if (sequence > dialog.RemoteCSeq)
        {
            dialog.RemoteCSeq = sequence;
        }
    }

    SipMessage CreateDialogRequest(
        SipDialogLayerEntry entry, string method, SipUri? contact)
    {
        SipDialogSnapshot dialog = entry.Snapshot;
        if (method == SipMethods.Bye && dialog.State == SipDialogState.Early
            && dialog.Role == SipDialogRole.Uas)
        {
            throw new SipDialogException(
                "A UAS cannot create BYE in an early dialog.", dialog.Id,
                dialog.State);
        }
        int sequence;
        if (method == SipMethods.Ack)
        {
            if (dialog.State != SipDialogState.Confirmed
                || dialog.Role != SipDialogRole.Uac
                || entry.InitialInviteCSeq < 0)
            {
                throw new SipDialogException(
                    "The dialog cannot create an ACK in its current state.", dialog.Id,
                    dialog.State);
            }
            sequence = entry.InitialInviteCSeq;
        }
        else
        {
            if (dialog.LocalCSeq == int.MaxValue)
            {
                throw new SipDialogException(
                    "The local dialog sequence is exhausted.", dialog.Id,
                    dialog.State);
            }
            sequence = dialog.LocalCSeq < 0 ? 1 : dialog.LocalCSeq + 1;
        }

        EnsureDialogQueueCapacity(entry.MaximumEventCount,
            1, 1, dialog);
        if (entry.PendingRemoteTarget != null)
        {
            dialog.RemoteTarget = SipUriModelCopy.CreateNormalizedCopy(
                entry.PendingRemoteTarget);
        }
        SipMessage request = BuildInDialogRequest(dialog, method, sequence, contact);
        if (method != SipMethods.Ack)
        {
            dialog.LocalCSeq = sequence;
        }
        if (method == SipMethods.Bye)
        {
            entry.LocalByeCSeq = sequence;
        }
        if (IsTargetRefreshMethodToken(method) && contact != null)
        {
            entry.PendingLocalTarget = SipUriModelCopy.CreateNormalizedCopy(contact);
            entry.LocalTargetRefreshCSeq = sequence;
            entry.LocalTargetRefreshMethod = request.Method;
        }
        QueueDialogEvent(dialog, request, "request-created",
            "An in-dialog request was created.",
            IsInviteUsageMethod(request.Method) ? entry.InviteUsageId : null);
        return CopyRequiredDialogMessage(request);
    }

    SipMessage BuildInDialogRequest(
        SipDialogSnapshot dialog, string method, int sequence, SipUri? contact)
    {
        if (dialog.Id == null || dialog.LocalUri == null || dialog.RemoteUri == null
            || dialog.RemoteTarget == null || dialog.Id.CallId == null)
        {
            throw new SipDialogException(
                "The dialog lacks request construction state.", dialog.Id,
                dialog.State);
        }
        SipNameAddress from = BuildDialogAddress(dialog.LocalUri, dialog.Id.LocalTag);
        SipNameAddress to = BuildDialogAddress(dialog.RemoteUri, dialog.Id.RemoteTag);
        string target = FormatDialogUriText(dialog.RemoteTarget);
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.PreserveOriginal = false;
        string fromText = SipHeaderPrimitives.FormatNameAddressValue(from, writeOptions);
        string toText = SipHeaderPrimitives.FormatNameAddressValue(to, writeOptions);
        string transport = dialog.Secure ? "TLS" : "UDP";
        string branch = tokens.CreateBranch();
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append(method);
        text.Append(" ");
        text.Append(target);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/");
        text.Append(transport);
        text.Append(" dialog.invalid;branch=");
        text.Append(branch);
        text.Append("\r\nMax-Forwards: 70\r\nFrom: ");
        text.Append(fromText);
        text.Append("\r\nTo: ");
        text.Append(toText);
        text.Append("\r\nCall-ID: ");
        text.Append(dialog.Id.CallId);
        text.Append("\r\nCSeq: ");
        text.Append(sequence.ToString(System.Globalization.CultureInfo.InvariantCulture));
        text.Append(" ");
        text.Append(method);
        text.Append("\r\n");
        if (contact != null)
        {
            text.Append("Contact: <");
            text.Append(FormatDialogUriText(contact));
            text.Append(">\r\n");
        }
        text.Append("Content-Length: 0\r\n\r\n");
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(
            System.Text.Encoding.UTF8.GetBytes(text.ToString()), new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete || parsed.Message == null)
        {
            throw new SipDialogException(
                parsed.ErrorMessage == null
                    ? "The in-dialog request could not be constructed."
                    : parsed.ErrorMessage,
                dialog.Id, dialog.State);
        }
        SipRouteRouting.ApplyRouteSet(parsed.Message, dialog.RemoteTarget, dialog.RouteSet);
        return parsed.Message;
    }

    SipUri CopyTargetRefreshUri(
        SipDialogLayerEntry entry, SipUri? uri, string parameterName)
    {
        if (uri == null
            || uri.Scheme != SipUriScheme.Sip && uri.Scheme != SipUriScheme.Sips
            || SipUriValidator.ValidateUriModel(uri).Length > 0)
        {
            throw new System.ArgumentException(
                "A valid SIP or SIPS target is required.", parameterName);
        }
        if (entry.PreventSecureDowngrade && entry.Snapshot.Secure
            && uri.Scheme != SipUriScheme.Sips)
        {
            throw new SipProtocolException(
                "A secure dialog target refresh requires a SIPS URI.",
                SipStatusCodes.UnsupportedUriScheme, null);
        }
        return SipUriModelCopy.CreateNormalizedCopy(uri);
    }

    SipNameAddress BuildDialogAddress(SipUri uri, string? tag)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = SipUriModelCopy.CreateNormalizedCopy(uri);
        if (tag != null)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = "tag";
            parameter.Value = tag;
            parameter.HasValue = true;
            address.Parameters = new SipUriParameter[] { parameter };
        }
        return address;
    }

    string FormatDialogUriText(SipUri uri)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipWriteResult formatted = SipUriFormatter.FormatUriModel(uri, options);
        if (!formatted.Success)
        {
            throw new System.ArgumentException(
                "The dialog URI cannot be formatted.", "uri");
        }
        return System.Text.Encoding.UTF8.GetString(formatted.Bytes);
    }

    SipDialogId BuildDialogIdentifier(SipMessage response, SipDialogRole role)
    {
        SipDialogId id = new SipDialogId();
        id.CallId = ReadMessageCallId(response);
        if (role == SipDialogRole.Uac)
        {
            id.LocalTag = ReadAddressTag(response.From);
            id.RemoteTag = ReadAddressTag(response.To);
        }
        else
        {
            id.LocalTag = ReadAddressTag(response.To);
            id.RemoteTag = ReadAddressTag(response.From);
        }
        return id;
    }

    void ValidateDialogMessage(SipDialogLayerEntry entry, SipMessage? message)
    {
        SipDialogSnapshot dialog = entry.Snapshot;
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(message, true);
        if (message == null || SipMessageValidator.HasErrorIssues(issues)
            || !MessageHasDialogIdentifier(dialog, message)
            || message.Kind == SipMessageKind.Response && message.StatusCode == 100)
        {
            throw new System.ArgumentException(
                "A valid message matching the dialog is required.", "message");
        }
    }

    bool IsInitialInviteSuccess(SipDialogLayerEntry entry, SipMessage message)
    {
        return entry.Snapshot.State == SipDialogState.Early
            && message.Kind == SipMessageKind.Response
            && message.StatusCode >= 200 && message.StatusCode < 300
            && message.CSeq != null && message.CSeq.Method == SipMethod.Invite
            && message.CSeq.SequenceNumber == entry.InitialInviteCSeq;
    }

    void ValidateLocalByeResponse(SipDialogLayerEntry entry, SipMessage message)
    {
        if (message.Kind == SipMessageKind.Response && message.CSeq != null
            && message.CSeq.Method == SipMethod.Bye
            && (entry.LocalByeCSeq < 0
                || message.CSeq.SequenceNumber != entry.LocalByeCSeq))
        {
            throw new System.ArgumentException(
                "The BYE response does not match a local dialog request.", "message");
        }
    }

    void ProcessTargetRefreshRequest(SipDialogLayerEntry entry, SipMessage message)
    {
        CommitLocalTargetFromRequestDestination(entry, message);
        if (!IsTargetRefreshMethod(message.Method))
        {
            CommitRemoteTargetFromRequestDestination(entry, message);
            return;
        }
        CommitRemoteTargetFromRequestDestination(entry, message);
        SipUri? target = ReadOptionalTargetRefreshContact(entry, message);
        if (message.CSeq == null)
        {
            return;
        }
        if (MessageHasLocalRequestOrientation(entry.Snapshot, message))
        {
            entry.PendingLocalTarget = target;
            entry.LocalTargetRefreshCSeq = message.CSeq.SequenceNumber;
            entry.LocalTargetRefreshMethod = message.Method;
        }
        else
        {
            entry.PendingRemoteTarget = target;
            entry.RemoteTargetRefreshCSeq = message.CSeq.SequenceNumber;
            entry.RemoteTargetRefreshMethod = message.Method;
        }
    }

    void ProcessTargetRefreshResponse(SipDialogLayerEntry entry, SipMessage message)
    {
        if (message.CSeq == null || !IsTargetRefreshMethod(message.CSeq.Method))
        {
            return;
        }
        bool accepted = message.StatusCode >= 200 && message.StatusCode < 300
            || ResponseIsReliableProvisional(message);
        bool final = message.StatusCode >= 200;
        if (MessageHasLocalRequestOrientation(entry.Snapshot, message))
        {
            if (message.CSeq.SequenceNumber != entry.LocalTargetRefreshCSeq
                || message.CSeq.Method != entry.LocalTargetRefreshMethod)
            {
                return;
            }
            if (accepted)
            {
                if (entry.PendingLocalTarget != null)
                {
                    entry.Snapshot.LocalTarget = SipUriModelCopy.CreateNormalizedCopy(
                        entry.PendingLocalTarget);
                }
                SipUri? responseTarget = ReadOptionalTargetRefreshContact(entry, message);
                if (responseTarget != null)
                {
                    entry.Snapshot.RemoteTarget = responseTarget;
                }
            }
            if (final)
            {
                ClearLocalTargetRefresh(entry);
            }
        }
        else
        {
            if (message.CSeq.SequenceNumber != entry.RemoteTargetRefreshCSeq
                || message.CSeq.Method != entry.RemoteTargetRefreshMethod)
            {
                return;
            }
            if (accepted && entry.PendingRemoteTarget != null)
            {
                entry.Snapshot.RemoteTarget = SipUriModelCopy.CreateNormalizedCopy(
                    entry.PendingRemoteTarget);
            }
            if (accepted)
            {
                SipUri? responseTarget = ReadOptionalTargetRefreshContact(entry, message);
                if (responseTarget != null)
                {
                    entry.Snapshot.LocalTarget = responseTarget;
                }
            }
            if (final)
            {
                ClearRemoteTargetRefresh(entry);
            }
        }
    }

    void CommitRemoteTargetFromRequestDestination(
        SipDialogLayerEntry entry, SipMessage message)
    {
        if (entry.PendingRemoteTarget == null
            || !MessageHasLocalRequestOrientation(entry.Snapshot, message)
            || !RequestUsesTarget(message, entry.PendingRemoteTarget))
        {
            return;
        }
        entry.Snapshot.RemoteTarget = SipUriModelCopy.CreateNormalizedCopy(
            entry.PendingRemoteTarget);
    }

    void CommitLocalTargetFromRequestDestination(
        SipDialogLayerEntry entry, SipMessage message)
    {
        if (entry.PendingLocalTarget == null
            || !MessageComesFromRemote(entry.Snapshot, message)
            || !RequestUsesTarget(message, entry.PendingLocalTarget))
        {
            return;
        }
        entry.Snapshot.LocalTarget = SipUriModelCopy.CreateNormalizedCopy(
            entry.PendingLocalTarget);
    }

    void ValidateTargetRefreshRequest(SipDialogLayerEntry entry, SipMessage message)
    {
        if (IsTargetRefreshMethod(message.Method)
            && message.ContactValues.Length > 0)
        {
            ReadOptionalTargetRefreshContact(entry, message);
        }
    }

    void ValidateTargetRefreshResponse(
        SipDialogLayerEntry entry, SipMessage message)
    {
        if (message.CSeq != null
            && IsTargetRefreshMethod(message.CSeq.Method)
            && (message.StatusCode >= 200 && message.StatusCode < 300
                || ResponseIsReliableProvisional(message))
            && message.ContactValues.Length > 0)
        {
            ReadOptionalTargetRefreshContact(entry, message);
        }
    }

    bool RequestUsesTarget(SipMessage request, SipUri target)
    {
        if (request.RequestUri != null
            && SipUriComparison.CompareUriModels(request.RequestUri, target))
        {
            return true;
        }
        if (request.RouteValues.Length == 0)
        {
            return false;
        }
        SipRouteValue route = request.RouteValues[request.RouteValues.Length - 1];
        SipUri? uri = route.Address == null ? null : route.Address.Uri;
        return uri != null && SipUriComparison.CompareUriModels(uri, target);
    }

    SipUri? ReadOptionalTargetRefreshContact(
        SipDialogLayerEntry entry, SipMessage message)
    {
        if (message.ContactValues.Length == 0)
        {
            return null;
        }
        SipUri target = ReadSingleContactUri(message, "message");
        return CopyTargetRefreshUri(entry, target, "message");
    }

    bool ResponseIsReliableProvisional(SipMessage message)
    {
        if (message.StatusCode <= 100 || message.StatusCode >= 200)
        {
            return false;
        }
        bool hasRSeq = false;
        bool requiresReliability = false;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.RSeq))
            {
                hasRSeq = true;
            }
            else if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.Require))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    SipHeaderNames.Require, header.Value, new SipParseOptions());
                int optionIndex = 0;
                while (parsed.Success && optionIndex < parsed.OptionTags.Length)
                {
                    if (SipAscii.EqualsIgnoreCase(parsed.OptionTags[optionIndex],
                        SipOptionTags.ReliableProvisional))
                    {
                        requiresReliability = true;
                    }
                    optionIndex = optionIndex + 1;
                }
            }
            index = index + 1;
        }
        return hasRSeq && requiresReliability;
    }

    void ClearLocalTargetRefresh(SipDialogLayerEntry entry)
    {
        entry.PendingLocalTarget = null;
        entry.LocalTargetRefreshCSeq = -1;
        entry.LocalTargetRefreshMethod = SipMethod.Unknown;
    }

    void ClearRemoteTargetRefresh(SipDialogLayerEntry entry)
    {
        entry.PendingRemoteTarget = null;
        entry.RemoteTargetRefreshCSeq = -1;
        entry.RemoteTargetRefreshMethod = SipMethod.Unknown;
    }

    bool IsTargetRefreshMethod(SipMethod method)
    {
        return method == SipMethod.Invite || method == SipMethod.Update;
    }

    bool IsTargetRefreshMethodToken(string? method)
    {
        return method == SipMethods.Invite || method == SipMethods.Update;
    }

    bool IsInviteUsageMethod(SipMethod method)
    {
        return method == SipMethod.Invite || method == SipMethod.Update
            || method == SipMethod.Prack || method == SipMethod.Ack
            || method == SipMethod.Cancel || method == SipMethod.Bye
            || method == SipMethod.Info;
    }

    string CreateUsageIdentifier()
    {
        int attempts = 0;
        while (attempts < 8)
        {
            string candidate = "usage-" + tokens.CreateOpaqueToken();
            if (!UsageIdentifierExists(candidate))
            {
                return candidate;
            }
            attempts = attempts + 1;
        }
        throw new System.InvalidOperationException(
            "A unique dialog usage identifier cannot be generated.");
    }

    bool UsageIdentifierExists(string usageId)
    {
        int dialogIndex = 0;
        while (dialogIndex < dialogs.Count)
        {
            string[] identifiers = dialogs[dialogIndex].Snapshot.UsageIds;
            int usageIndex = 0;
            while (usageIndex < identifiers.Length)
            {
                if (identifiers[usageIndex] == usageId)
                {
                    return true;
                }
                usageIndex = usageIndex + 1;
            }
            dialogIndex = dialogIndex + 1;
        }
        return false;
    }

    string[] AppendUsageIdentifier(string[] identifiers, string usageId)
    {
        string[] result = new string[identifiers.Length + 1];
        int index = 0;
        while (index < identifiers.Length)
        {
            result[index] = identifiers[index];
            index = index + 1;
        }
        result[identifiers.Length] = usageId;
        return result;
    }

    bool RemoveUsageIdentifier(SipDialogLayerEntry entry, string usageId)
    {
        string[] identifiers = entry.Snapshot.UsageIds;
        int removeIndex = -1;
        int index = 0;
        while (index < identifiers.Length)
        {
            if (identifiers[index] == usageId)
            {
                removeIndex = index;
                break;
            }
            index = index + 1;
        }
        if (removeIndex < 0)
        {
            return false;
        }
        string[] result = new string[identifiers.Length - 1];
        index = 0;
        int resultIndex = 0;
        while (index < identifiers.Length)
        {
            if (index != removeIndex)
            {
                result[resultIndex] = identifiers[index];
                resultIndex = resultIndex + 1;
            }
            index = index + 1;
        }
        entry.Snapshot.UsageIds = result;
        if (entry.InviteUsageId == usageId)
        {
            entry.InviteUsageId = null;
        }
        return true;
    }

    string? TerminateInviteUsage(SipDialogLayerEntry entry)
    {
        string? usageId = entry.InviteUsageId;
        if (usageId == null || !RemoveUsageIdentifier(entry, usageId))
        {
            return null;
        }
        if (entry.Snapshot.UsageIds.Length == 0)
        {
            entry.Snapshot.State = SipDialogState.Terminated;
            ClearDialogAssociations(entry.Snapshot);
        }
        return usageId;
    }

    void ValidateUsageTermination(
        string? usageId, SipCancellation? cancellation)
    {
        if (usageId == null || usageId.Length == 0
            || usageId.Length > SipDefaults.MaxHeaderLineBytes
            || !SipAscii.IsToken(usageId))
        {
            throw new System.ArgumentException(
                "A valid dialog usage identifier is required.", "usageId");
        }
        if (cancellation == null || cancellation.Reason != null
            && cancellation.Reason.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "A valid usage termination request is required.", "cancellation");
        }
    }

    void CompleteUsageTermination(SipDialogLayerEntry entry, int dialogIndex,
        string usageId, string? requestedReason)
    {
        bool lastUsage = entry.Snapshot.UsageIds.Length == 0;
        if (lastUsage)
        {
            entry.Snapshot.State = SipDialogState.Terminated;
            ClearDialogAssociations(entry.Snapshot);
        }
        string reason = requestedReason == null || requestedReason.Length == 0
            ? "The dialog usage was terminated." : requestedReason;
        QueueDialogEvent(entry.Snapshot, null,
            lastUsage ? "terminated" : "usage-terminated", reason, usageId);
        if (lastUsage)
        {
            dialogs.RemoveAt(dialogIndex);
        }
    }

    void ClearDialogAssociations(SipDialogSnapshot dialog)
    {
        dialog.TransactionIds = new string[0];
        dialog.AuthenticationContextId = null;
    }

    bool MessageHasLocalRequestOrientation(
        SipDialogSnapshot dialog, SipMessage message)
    {
        if (dialog.Id == null || message.From == null || message.To == null)
        {
            return false;
        }
        return ReadAddressTag(message.From) == dialog.Id.LocalTag
            && ReadAddressTag(message.To) == dialog.Id.RemoteTag;
    }

    bool MessageComesFromRemote(SipDialogSnapshot dialog, SipMessage message)
    {
        if (dialog.Id == null || message.From == null || message.To == null)
        {
            return false;
        }
        return ReadAddressTag(message.From) == dialog.Id.RemoteTag
            && ReadAddressTag(message.To) == dialog.Id.LocalTag;
    }

    bool IsInitialInviteFailure(SipDialogLayerEntry entry, SipMessage? message)
    {
        SipDialogSnapshot dialog = entry.Snapshot;
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(message, true);
        if (message == null || SipMessageValidator.HasErrorIssues(issues)
            || dialog.State != SipDialogState.Early
            || dialog.Id == null || message.Kind != SipMessageKind.Response
            || message.StatusCode < 300 || message.CSeq == null
            || message.CSeq.Method != SipMethod.Invite
            || message.CSeq.SequenceNumber != entry.InitialInviteCSeq
            || ReadMessageCallId(message) != dialog.Id.CallId)
        {
            return false;
        }
        string? localTag = dialog.Role == SipDialogRole.Uac
            ? ReadAddressTag(message.From) : ReadAddressTag(message.To);
        return localTag == dialog.Id.LocalTag;
    }

    int FindEarlyFailureIndex(SipMessage? message)
    {
        int index = 0;
        while (index < dialogs.Count)
        {
            if (IsInitialInviteFailure(dialogs[index], message))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    SipDialogSnapshot TerminateEarlyDialogs(
        SipDialogLayerEntry selected, SipMessage message)
    {
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(message, true);
        if (SipMessageValidator.HasErrorIssues(issues))
        {
            throw new System.ArgumentException(
                "A valid final INVITE response is required.", "message");
        }
        SipDialogSnapshot? result = null;
        int index = 0;
        while (index < dialogs.Count)
        {
            SipDialogLayerEntry candidate = dialogs[index];
            if (DialogsShareInitialInvite(selected, candidate)
                && candidate.Snapshot.State == SipDialogState.Early)
            {
                string? usageId = TerminateInviteUsage(candidate);
                string kind = usageId == null ? "updated"
                    : candidate.Snapshot.State == SipDialogState.Terminated
                        ? "state-changed" : "usage-terminated";
                QueueDialogEvent(candidate.Snapshot, message, kind,
                    "The dialog-forming INVITE received a failure response.",
                    usageId);
                if (candidate == selected)
                {
                    result = CopyRequiredDialogSnapshot(candidate.Snapshot);
                }
            }
            index = index + 1;
        }
        index = dialogs.Count - 1;
        while (index >= 0)
        {
            if (DialogsShareInitialInvite(selected, dialogs[index])
                && dialogs[index].Snapshot.State == SipDialogState.Terminated)
            {
                dialogs.RemoveAt(index);
            }
            index = index - 1;
        }
        if (result == null)
        {
            throw new System.InvalidOperationException(
                "The selected early dialog was not updated.");
        }
        return result;
    }

    bool DialogsShareInitialInvite(
        SipDialogLayerEntry first, SipDialogLayerEntry second)
    {
        SipDialogId? firstId = first.Snapshot.Id;
        SipDialogId? secondId = second.Snapshot.Id;
        if (firstId == null || secondId == null
            || first.Snapshot.Role != second.Snapshot.Role
            || first.InitialInviteCSeq != second.InitialInviteCSeq
            || firstId.CallId != secondId.CallId)
        {
            return false;
        }
        return first.Snapshot.Role == SipDialogRole.Uac
            ? firstId.LocalTag == secondId.LocalTag
            : firstId.RemoteTag == secondId.RemoteTag;
    }

    bool MessageHasDialogIdentifier(SipDialogSnapshot dialog, SipMessage message)
    {
        if (dialog.Id == null || ReadMessageCallId(message) != dialog.Id.CallId
            || message.From == null || message.To == null)
        {
            return false;
        }
        string? fromTag = ReadAddressTag(message.From);
        string? toTag = ReadAddressTag(message.To);
        if (message.Kind == SipMessageKind.Response)
        {
            return fromTag == dialog.Id.LocalTag && toTag == dialog.Id.RemoteTag
                || fromTag == dialog.Id.RemoteTag && toTag == dialog.Id.LocalTag;
        }
        return fromTag == dialog.Id.RemoteTag && toTag == dialog.Id.LocalTag
            || fromTag == dialog.Id.LocalTag && toTag == dialog.Id.RemoteTag;
    }

    int FindDialogIndex(SipDialogId? id)
    {
        int index = 0;
        while (index < dialogs.Count)
        {
            if (DialogIdentifiersEqual(dialogs[index].Snapshot.Id, id))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    int FindTransactionDialogIndex(string transactionId)
    {
        int index = 0;
        while (index < dialogs.Count)
        {
            if (FindIdentifierIndex(
                dialogs[index].Snapshot.TransactionIds, transactionId) >= 0)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    int FindIdentifierIndex(string[] identifiers, string identifier)
    {
        int index = 0;
        while (index < identifiers.Length)
        {
            if (identifiers[index] == identifier)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    string[] AppendTransactionIdentifier(string[] identifiers, string identifier)
    {
        string[] result = new string[identifiers.Length + 1];
        int index = 0;
        while (index < identifiers.Length)
        {
            result[index] = identifiers[index];
            index = index + 1;
        }
        result[identifiers.Length] = identifier;
        return result;
    }

    string[] RemoveIdentifierAt(string[] identifiers, int removeIndex)
    {
        string[] result = new string[identifiers.Length - 1];
        int index = 0;
        int resultIndex = 0;
        while (index < identifiers.Length)
        {
            if (index != removeIndex)
            {
                result[resultIndex] = identifiers[index];
                resultIndex = resultIndex + 1;
            }
            index = index + 1;
        }
        return result;
    }

    bool DialogIdentifiersEqual(SipDialogId? first, SipDialogId? second)
    {
        return first != null && second != null
            && first.CallId == second.CallId
            && first.LocalTag == second.LocalTag
            && first.RemoteTag == second.RemoteTag;
    }

    void ValidateDialogIdentifier(SipDialogId? id)
    {
        if (id == null || !BoundedDialogText(id.CallId)
            || !BoundedDialogText(id.LocalTag)
            || id.RemoteTag != null && !BoundedDialogText(id.RemoteTag))
        {
            throw new System.ArgumentException(
                "A bounded dialog identifier is required.", "id");
        }
    }

    void ValidateAssociationIdentifier(string? identifier, string parameterName)
    {
        if (identifier == null || identifier.Length == 0
            || identifier.Length > SipDefaults.MaxHeaderLineBytes
            || !SipAscii.IsToken(identifier))
        {
            throw new System.ArgumentException(
                "A bounded token identifier is required.", parameterName);
        }
    }

    void ValidateDialogLookupMessage(SipMessage? message)
    {
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(
            message, true);
        if (message == null || SipMessageValidator.HasErrorIssues(issues)
            || message.From == null || message.To == null
            || ReadMessageCallId(message) == null
            || ReadAddressTag(message.From) == null
            || ReadAddressTag(message.To) == null)
        {
            throw new System.ArgumentException(
                "A valid in-dialog message is required.", "message");
        }
    }

    bool BoundedDialogText(string? value)
    {
        return value != null && value.Length > 0
            && value.Length <= SipDefaults.MaxHeaderLineBytes
            && System.Text.Encoding.UTF8.GetByteCount(value)
                <= SipDefaults.MaxHeaderLineBytes;
    }

    string? ReadMessageCallId(SipMessage message)
    {
        string? value = null;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CallId)
                || SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CompactCallId))
            {
                if (value != null)
                {
                    return null;
                }
                SipStructuredHeaderResult parsed =
                    SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                        SipHeaderNames.CallId, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.CallId == null)
                {
                    return null;
                }
                value = parsed.CallId;
            }
            index = index + 1;
        }
        return value;
    }

    string? ReadAddressTag(SipNameAddress? address)
    {
        if (address == null)
        {
            return null;
        }
        int index = 0;
        while (index < address.Parameters.Length)
        {
            SipUriParameter parameter = address.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "tag"))
            {
                return parameter.Value;
            }
            index = index + 1;
        }
        return null;
    }

    SipUri ReadSingleContactUri(SipMessage message, string parameterName)
    {
        SipContactValue? contact = message.ContactValues.Length == 1
            ? message.ContactValues[0] : null;
        SipNameAddress? address = contact == null ? null : contact.Address;
        SipUri? uri = address == null ? null : address.Uri;
        if (contact == null || contact.Wildcard || uri == null)
        {
            throw new System.ArgumentException(
                "A single Contact URI is required.", parameterName);
        }
        if (uri.Scheme != SipUriScheme.Sip && uri.Scheme != SipUriScheme.Sips)
        {
            throw new System.ArgumentException(
                "The dialog Contact must use SIP or SIPS.", parameterName);
        }
        return SipUriModelCopy.CreateNormalizedCopy(uri);
    }

    SipUri CopyRequiredDialogUri(SipUri? uri, string parameterName)
    {
        if (uri == null)
        {
            throw new System.ArgumentException(
                "The dialog address requires a URI.", parameterName);
        }
        return SipUriModelCopy.CreateNormalizedCopy(uri);
    }

    bool RequestCreatesSecureDialog(SipMessage request)
    {
        return request.RequestUri != null
            && request.RequestUri.Scheme == SipUriScheme.Sips
            && request.ViaValues.Length > 0
            && SipAscii.EqualsIgnoreCase(request.ViaValues[0].Transport, "TLS");
    }

    void EnsureDialogQueueCapacity(int maximum, int eventCount,
        int snapshotCount, SipDialogSnapshot dialog)
    {
        if (maximum <= 0 || eventCount < 0 || snapshotCount < 0
            || eventCount > maximum - events.Count
            || snapshotCount > maximum - snapshots.Count)
        {
            throw new SipDialogException(
                "The dialog event or snapshot queue is full.",
                dialog.Id, dialog.State);
        }
    }

    void EnsureEarlyFailureQueueCapacity(SipDialogLayerEntry selected)
    {
        int count = 0;
        int maximum = SipDefaults.MaxQueueCount;
        int index = 0;
        while (index < dialogs.Count)
        {
            SipDialogLayerEntry candidate = dialogs[index];
            if (DialogsShareInitialInvite(selected, candidate)
                && candidate.Snapshot.State == SipDialogState.Early)
            {
                count = count + 1;
                if (candidate.MaximumEventCount < maximum)
                {
                    maximum = candidate.MaximumEventCount;
                }
            }
            index = index + 1;
        }
        EnsureDialogQueueCapacity(maximum, count, count, selected.Snapshot);
    }

    void QueueDialogEvent(SipDialogSnapshot dialog, SipMessage? message,
        string kind, string reason, string? usageId)
    {
        SipDialogEvent value = new SipDialogEvent();
        value.Kind = kind;
        value.DialogId = dialog.Id;
        value.UsageId = usageId;
        value.State = dialog.State;
        value.Snapshot = dialog;
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Reason = reason;
        value.CorrelationId = dialog.CorrelationId;
        SipDialogEvent? copy = SipSnapshotCopy.CopyDialogEvent(value);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The dialog event cannot be copied.");
        }
        events.Add(copy);
        snapshots.Add(CopyRequiredDialogSnapshot(dialog));
    }

    SipDialogSnapshot CopyRequiredDialogSnapshot(SipDialogSnapshot source)
    {
        SipDialogSnapshot? copy = SipSnapshotCopy.CopyDialogSnapshot(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The dialog snapshot cannot be copied.");
        }
        return copy;
    }

    SipMessage CopyRequiredDialogMessage(SipMessage source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The dialog message cannot be copied.");
        }
        return copy;
    }
}
