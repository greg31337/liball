namespace sip;

/// <summary>Coordinates two isolated INVITE usages with application-controlled message mapping.</summary>
public class SipBackToBackSession : ISipBackToBackSession
{
    object gate = new object();
    System.Collections.Generic.List<SipBackToBackEvent> events =
        new System.Collections.Generic.List<SipBackToBackEvent>();
    SipInviteSession? left;
    SipInviteSession? right;
    SipBackToBackState state;
    SipMessage? leftMessage;
    SipMessage? rightMessage;
    SipTransactionSnapshot? leftTransaction;
    SipTransactionSnapshot? rightTransaction;
    SipCancellation? cancellation;
    string? leftCallId;
    string? rightCallId;
    string? correlationId;
    bool failed;

    /// <summary>Initializes an idle back-to-back session.</summary>
    public SipBackToBackSession()
    {
    }

    /// <summary>Creates independent incoming and outgoing INVITE usage legs.</summary>
    /// <param name="leftRequest">The incoming leg request.</param>
    /// <param name="rightRequest">The outgoing leg request.</param>
    /// <returns>The initial detached two-leg event.</returns>
    public SipBackToBackEvent CreateBackToBackSession(
        SipInviteRequest leftRequest, SipInviteRequest rightRequest)
    {
        lock (gate)
        {
            if (state != SipBackToBackState.None)
            {
                throw new System.InvalidOperationException(
                    "A back-to-back session is already active.");
            }
            ValidateInitialRequest(leftRequest, "leftRequest");
            ValidateInitialRequest(rightRequest, "rightRequest");
            string leftId = ReadRequiredCallId(
                leftRequest.Message, "leftRequest");
            string rightId = ReadRequiredCallId(
                rightRequest.Message, "rightRequest");
            if (leftId == rightId)
            {
                throw new System.ArgumentException(
                    "Back-to-back session legs require independent Call-ID values.",
                    "rightRequest");
            }

            SipInviteSession createdLeft = new SipInviteSession();
            SipInviteSession createdRight = new SipInviteSession();
            SipInviteEvent leftStarted = createdLeft.ReceiveInviteMessage(
                CopyRequiredMessage(leftRequest.Message));
            if (leftRequest.AuthenticationContextId != null
                && leftRequest.AuthenticationContextId.Length > 0)
            {
                createdLeft.AssociateInviteAuthenticationContext(
                    leftRequest.AuthenticationContextId);
            }
            SipInviteEvent rightStarted = createdRight.StartInvite(
                CopyInviteRequest(rightRequest));
            DrainInviteEvents(createdLeft);
            DrainInviteEvents(createdRight);

            left = createdLeft;
            right = createdRight;
            leftCallId = leftId;
            rightCallId = rightId;
            leftMessage = CopyRequiredMessage(leftStarted.Message);
            rightMessage = CopyRequiredMessage(rightStarted.Message);
            leftTransaction = CreateTransactionSnapshot(
                leftMessage, SipTransactionRole.Server, "left");
            rightTransaction = CreateTransactionSnapshot(
                rightMessage, SipTransactionRole.Client, "right");
            correlationId = rightRequest.CorrelationId == null
                ? leftRequest.CorrelationId : rightRequest.CorrelationId;
            state = SipBackToBackState.Establishing;
            if (HasRequestedCancellation(leftRequest.Cancellation)
                || HasRequestedCancellation(rightRequest.Cancellation))
            {
                ApplyInitialCancellation(leftRequest.Cancellation,
                    rightRequest.Cancellation);
                return CreateBackToBackEvent(
                    SipBackToBackLeg.None, false,
                    "Independent back-to-back session legs were created and cancellation was requested.");
            }
            SipBackToBackEvent created = QueueBackToBackEvent(
                SipBackToBackLeg.None, false,
                "Independent back-to-back session legs were created.");
            return CopyRequiredBackToBackEvent(created);
        }
    }

    /// <summary>Processes one received message on the matching independent leg.</summary>
    /// <param name="message">The received leg message.</param>
    /// <returns>The resulting detached two-leg event.</returns>
    public SipBackToBackEvent MapBackToBackMessage(SipMessage message)
    {
        lock (gate)
        {
            EnsureCreated();
            SipBackToBackLeg leg = FindMessageLeg(message);
            ProcessRemoteLegMessage(leg, message);
            SetLegMessage(leg, message);
            UpdateLegTransaction(leg, message, false);
            UpdateBackToBackState(message);
            return QueueBackToBackEvent(leg, false,
                "A source-leg message was received for application mapping.");
        }
    }

    /// <summary>Processes a source message and the application-created message for the opposite leg.</summary>
    /// <param name="mapping">The source message, destination message, and source leg.</param>
    /// <returns>The resulting detached two-leg event.</returns>
    public SipBackToBackEvent ApplyBackToBackMapping(
        SipBackToBackMapping mapping)
    {
        lock (gate)
        {
            EnsureCreated();
            ValidateMapping(mapping);
            SipBackToBackLeg destination = mapping.SourceLeg
                == SipBackToBackLeg.Left
                    ? SipBackToBackLeg.Right : SipBackToBackLeg.Left;
            SipMessage source = CopyRequiredMessage(mapping.SourceMessage);
            ProcessRemoteLegMessage(mapping.SourceLeg, source);
            SipMessage applied = ProcessLocalLegMessage(destination,
                CopyRequiredMessage(mapping.MappedMessage));
            SetLegMessage(mapping.SourceLeg, source);
            SetLegMessage(destination, applied);
            UpdateLegTransaction(
                mapping.SourceLeg, source, false);
            UpdateLegTransaction(destination, applied, true);
            if (mapping.CorrelationId != null)
            {
                correlationId = mapping.CorrelationId;
            }
            UpdateBackToBackState(source);
            UpdateBackToBackState(applied);
            return QueueBackToBackEvent(mapping.SourceLeg, true,
                "The application mapping was applied to both independent legs.");
        }
    }

    /// <summary>Associates authentication state with only one leg.</summary>
    /// <param name="leg">The leg to update.</param>
    /// <param name="authenticationContextId">The authentication context identifier.</param>
    public void AssociateBackToBackAuthenticationContext(
        SipBackToBackLeg leg, string authenticationContextId)
    {
        lock (gate)
        {
            SipInviteSession selected = SelectLeg(leg);
            selected.AssociateInviteAuthenticationContext(
                authenticationContextId);
            DrainInviteEvents(selected);
            QueueBackToBackEvent(leg, false,
                "Authentication state was associated with one session leg.");
        }
    }

    /// <summary>Reports a transport failure for only one leg.</summary>
    /// <param name="leg">The failed leg.</param>
    /// <param name="errorMessage">The bounded failure description.</param>
    public void ReportBackToBackTransportError(
        SipBackToBackLeg leg, string errorMessage)
    {
        lock (gate)
        {
            SipInviteSession selected = SelectLeg(leg);
            selected.ReportInviteTransportError(errorMessage);
            DrainInviteEvents(selected);
            failed = true;
            state = SipBackToBackState.Failed;
            QueueBackToBackEvent(leg, false,
                "One back-to-back session leg reported a transport failure.");
        }
    }

    /// <summary>Advances timers independently on both legs.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    public void AdvanceBackToBackTime(long nowMilliseconds)
    {
        lock (gate)
        {
            EnsureCreated();
            if (left != null)
            {
                left.AdvanceInviteTime(nowMilliseconds);
                DrainInviteEvents(left);
            }
            if (right != null)
            {
                right.AdvanceInviteTime(nowMilliseconds);
                DrainInviteEvents(right);
            }
            UpdateBackToBackState(null);
            QueueBackToBackEvent(SipBackToBackLeg.None, false,
                "Back-to-back session leg timers were advanced.");
        }
    }

    /// <summary>Requests orderly termination of both live session legs.</summary>
    /// <param name="cancellation">The termination request.</param>
    public void TerminateBackToBackSession(SipCancellation cancellation)
    {
        lock (gate)
        {
            EnsureCreated();
            ValidateCancellation(cancellation);
            if (!cancellation.Requested)
            {
                return;
            }
            this.cancellation = CopyCancellation(cancellation);
            SipMessage? leftEnding = TerminateLiveLeg(left, cancellation);
            SipMessage? rightEnding = TerminateLiveLeg(right, cancellation);
            if (leftEnding != null)
            {
                leftMessage = leftEnding;
                UpdateLegTransaction(
                    SipBackToBackLeg.Left, leftEnding, true);
            }
            if (rightEnding != null)
            {
                rightMessage = rightEnding;
                UpdateLegTransaction(
                    SipBackToBackLeg.Right, rightEnding, true);
            }
            UpdateBackToBackState(null);
            if (state != SipBackToBackState.Terminated
                && state != SipBackToBackState.Failed)
            {
                state = SipBackToBackState.Terminating;
            }
            QueueBackToBackEvent(SipBackToBackLeg.None, false,
                cancellation.Reason == null
                    ? "Back-to-back session termination was requested."
                    : cancellation.Reason);
        }
    }

    /// <summary>Polls the next detached back-to-back session event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipBackToBackEvent PollBackToBackEvent()
    {
        lock (gate)
        {
            if (events.Count == 0)
            {
                return new SipBackToBackEvent();
            }
            SipBackToBackEvent value = events[0];
            events.RemoveAt(0);
            return CopyRequiredBackToBackEvent(value);
        }
    }

    /// <summary>Gets the current detached two-leg session state.</summary>
    /// <returns>The current two-leg event snapshot.</returns>
    public SipBackToBackEvent GetBackToBackSession()
    {
        lock (gate)
        {
            EnsureCreated();
            return CreateBackToBackEvent(
                SipBackToBackLeg.None, false, null);
        }
    }

    void ProcessRemoteLegMessage(SipBackToBackLeg leg,
        SipMessage message)
    {
        ValidateMessageBelongsToLeg(leg, message);
        if (IsInviteUsageMessage(message))
        {
            SipInviteSession selected = SelectLeg(leg);
            selected.ReceiveInviteMessage(message);
            DrainInviteEvents(selected);
        }
    }

    SipMessage ProcessLocalLegMessage(SipBackToBackLeg leg,
        SipMessage message)
    {
        ValidateMessageBelongsToLeg(leg, message);
        SipInviteSession selected = SelectLeg(leg);
        if (message.Kind == SipMessageKind.Response)
        {
            SipInviteRequest command = new SipInviteRequest();
            command.Message = message;
            command.CorrelationId = correlationId;
            SipInviteEvent applied = selected.StartInvite(command);
            DrainInviteEvents(selected);
            return applied.Message == null
                ? CopyRequiredMessage(message)
                : CopyRequiredMessage(applied.Message);
        }
        if (message.Kind == SipMessageKind.Request
            && (message.Method == SipMethod.Invite
                || message.Method == SipMethod.Update))
        {
            SipInviteRequest command = new SipInviteRequest();
            command.Message = message;
            command.CorrelationId = correlationId;
            SipInviteEvent applied = selected.StartInvite(command);
            DrainInviteEvents(selected);
            return applied.Message == null
                ? CopyRequiredMessage(message)
                : CopyRequiredMessage(applied.Message);
        }
        if (message.Kind == SipMessageKind.Request
            && (message.Method == SipMethod.Bye
                || message.Method == SipMethod.Cancel))
        {
            SipCancellation ending = new SipCancellation();
            ending.Requested = true;
            ending.Id = correlationId;
            ending.Reason = "The opposite B2BUA leg ended.";
            SipMessage? generated = TerminateLiveLeg(selected, ending);
            return generated == null
                ? CopyRequiredMessage(message)
                : CopyRequiredMessage(generated);
        }
        return CopyRequiredMessage(message);
    }

    void UpdateBackToBackState(SipMessage? message)
    {
        if (message != null && message.Kind == SipMessageKind.Response
            && message.StatusCode >= 300
            && message.StatusCode != SipStatusCodes.RequestTerminated
            && message.CSeq != null
            && message.CSeq.Method == SipMethod.Invite)
        {
            failed = true;
        }
        SipInviteSnapshot leftSnapshot = RequiredLeft().GetInviteSnapshot();
        SipInviteSnapshot rightSnapshot = RequiredRight().GetInviteSnapshot();
        if (failed)
        {
            state = SipBackToBackState.Failed;
            return;
        }
        if (leftSnapshot.State == SipInviteSessionState.Terminated
            && rightSnapshot.State == SipInviteSessionState.Terminated)
        {
            state = SipBackToBackState.Terminated;
        }
        else if (leftSnapshot.State == SipInviteSessionState.Terminating
            || rightSnapshot.State == SipInviteSessionState.Terminating
            || cancellation != null)
        {
            state = SipBackToBackState.Terminating;
        }
        else if (leftSnapshot.State == SipInviteSessionState.Confirmed
            && rightSnapshot.State == SipInviteSessionState.Confirmed)
        {
            state = SipBackToBackState.Confirmed;
        }
        else
        {
            state = SipBackToBackState.Establishing;
        }
    }

    SipBackToBackEvent QueueBackToBackEvent(SipBackToBackLeg sourceLeg,
        bool mapped, string? reason)
    {
        SipBackToBackEvent value = CreateBackToBackEvent(
            sourceLeg, mapped, reason);
        if (events.Count >= SipDefaults.MaxQueueCount)
        {
            events.RemoveAt(0);
        }
        events.Add(CopyRequiredBackToBackEvent(value));
        return CopyRequiredBackToBackEvent(value);
    }

    SipBackToBackEvent CreateBackToBackEvent(
        SipBackToBackLeg sourceLeg, bool mapped, string? reason)
    {
        SipBackToBackEvent value = new SipBackToBackEvent();
        value.State = state;
        value.LeftInvite = left == null ? null : left.GetInviteSnapshot();
        value.RightInvite = right == null ? null : right.GetInviteSnapshot();
        value.LeftTransaction = SipSnapshotCopy.CopyTransactionSnapshot(
            leftTransaction);
        value.RightTransaction = SipSnapshotCopy.CopyTransactionSnapshot(
            rightTransaction);
        value.LeftDialog = value.LeftInvite == null
            ? null : value.LeftInvite.Dialog;
        value.RightDialog = value.RightInvite == null
            ? null : value.RightInvite.Dialog;
        value.LeftMessage = SipSnapshotCopy.CopyMessageModel(leftMessage);
        value.RightMessage = SipSnapshotCopy.CopyMessageModel(rightMessage);
        value.SourceLeg = sourceLeg;
        value.Mapped = mapped;
        value.Cancellation = CopyCancellation(cancellation);
        value.Reason = reason;
        value.CorrelationId = correlationId;
        return CopyRequiredBackToBackEvent(value);
    }

    SipBackToBackEvent CopyRequiredBackToBackEvent(
        SipBackToBackEvent source)
    {
        SipBackToBackEvent value = new SipBackToBackEvent();
        value.State = source.State;
        value.LeftDialog = SipSnapshotCopy.CopyDialogSnapshot(
            source.LeftDialog);
        value.RightDialog = SipSnapshotCopy.CopyDialogSnapshot(
            source.RightDialog);
        value.LeftInvite = SipSnapshotCopy.CopyInviteSnapshot(
            source.LeftInvite);
        value.RightInvite = SipSnapshotCopy.CopyInviteSnapshot(
            source.RightInvite);
        value.LeftTransaction = SipSnapshotCopy.CopyTransactionSnapshot(
            source.LeftTransaction);
        value.RightTransaction = SipSnapshotCopy.CopyTransactionSnapshot(
            source.RightTransaction);
        value.LeftMessage = SipSnapshotCopy.CopyMessageModel(
            source.LeftMessage);
        value.RightMessage = SipSnapshotCopy.CopyMessageModel(
            source.RightMessage);
        value.SourceLeg = source.SourceLeg;
        value.Mapped = source.Mapped;
        value.Cancellation = CopyCancellation(source.Cancellation);
        value.Reason = source.Reason;
        value.CorrelationId = source.CorrelationId;
        return value;
    }

    static SipInviteRequest CopyInviteRequest(SipInviteRequest source)
    {
        SipInviteRequest value = new SipInviteRequest();
        value.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        value.Target = source.Target == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Target);
        value.OfferAnswer = SipSnapshotCopy.CopyOfferAnswer(source.OfferAnswer);
        value.Options = source.Options;
        value.Cancellation = CopyCancellation(source.Cancellation);
        value.CorrelationId = source.CorrelationId;
        value.DeadlineMilliseconds = source.DeadlineMilliseconds;
        value.AuthenticationContextId = source.AuthenticationContextId;
        return value;
    }

    static SipCancellation? CopyCancellation(SipCancellation? source)
    {
        if (source == null)
        {
            return null;
        }
        SipCancellation value = new SipCancellation();
        value.Requested = source.Requested;
        value.Id = source.Id;
        value.Reason = source.Reason;
        return value;
    }

    static SipMessage CopyRequiredMessage(SipMessage? source)
    {
        SipMessage? value = SipSnapshotCopy.CopyMessageModel(source);
        if (value == null)
        {
            throw new System.ArgumentException("A SIP message is required.");
        }
        return value;
    }

    void SetLegMessage(SipBackToBackLeg leg, SipMessage? message)
    {
        if (leg == SipBackToBackLeg.Left)
        {
            leftMessage = SipSnapshotCopy.CopyMessageModel(message);
        }
        else
        {
            rightMessage = SipSnapshotCopy.CopyMessageModel(message);
        }
    }

    void UpdateLegTransaction(SipBackToBackLeg leg,
        SipMessage message, bool local)
    {
        SipTransactionSnapshot? current = leg == SipBackToBackLeg.Left
            ? leftTransaction : rightTransaction;
        SipTransactionSnapshot updated;
        if (message.Kind == SipMessageKind.Request
            && message.Method != SipMethod.Ack)
        {
            updated = CreateTransactionSnapshot(message,
                local ? SipTransactionRole.Client
                    : SipTransactionRole.Server,
                leg == SipBackToBackLeg.Left ? "left" : "right");
        }
        else if (current == null)
        {
            return;
        }
        else
        {
            SipTransactionSnapshot? copied =
                SipSnapshotCopy.CopyTransactionSnapshot(current);
            if (copied == null)
            {
                return;
            }
            updated = copied;
            if (message.Kind == SipMessageKind.Response)
            {
                updated.Response = SipSnapshotCopy.CopyMessageModel(message);
                if (message.StatusCode < 200)
                {
                    updated.State = SipTransactionState.Proceeding;
                }
                else if (updated.Kind == SipTransactionKind.Invite
                    && message.StatusCode < 300)
                {
                    updated.State = SipTransactionState.Accepted;
                }
                else
                {
                    updated.State = SipTransactionState.Completed;
                }
            }
            else if (message.Kind == SipMessageKind.Request
                && message.Method == SipMethod.Ack)
            {
                updated.State = SipTransactionState.Terminated;
            }
        }
        updated.CorrelationId = correlationId;
        if (leg == SipBackToBackLeg.Left)
        {
            leftTransaction = updated;
        }
        else
        {
            rightTransaction = updated;
        }
    }

    static SipTransactionSnapshot CreateTransactionSnapshot(
        SipMessage request, SipTransactionRole role, string legName)
    {
        SipTransactionSnapshot value = new SipTransactionSnapshot();
        value.Key = SipTransactionMatching.CreateKey(request, role);
        SipCSeqValue? cseq = request.CSeq;
        if (cseq == null)
        {
            throw new System.ArgumentException(
                "A complete bounded SIP request is required.", "request");
        }
        value.Id = legName + ":"
            + (value.Key.Branch == null ? "legacy" : value.Key.Branch)
            + ":" + cseq.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        value.Kind = request.Method == SipMethod.Invite
            ? SipTransactionKind.Invite : SipTransactionKind.NonInvite;
        value.Role = role;
        value.State = value.Kind == SipTransactionKind.Invite
            ? role == SipTransactionRole.Client
                ? SipTransactionState.Calling
                : SipTransactionState.Proceeding
            : SipTransactionState.Trying;
        value.Request = SipSnapshotCopy.CopyMessageModel(request);
        return value;
    }

    SipBackToBackLeg FindMessageLeg(SipMessage message)
    {
        string callId = ReadRequiredCallId(message, "message");
        if (callId == leftCallId)
        {
            return SipBackToBackLeg.Left;
        }
        if (callId == rightCallId)
        {
            return SipBackToBackLeg.Right;
        }
        throw new System.ArgumentException(
            "The message does not belong to either back-to-back session leg.",
            "message");
    }

    void ValidateMessageBelongsToLeg(
        SipBackToBackLeg leg, SipMessage message)
    {
        if (leg != SipBackToBackLeg.Left
            && leg != SipBackToBackLeg.Right)
        {
            throw new System.ArgumentException(
                "A left or right source leg is required.", "mapping");
        }
        if (FindMessageLeg(message) != leg)
        {
            throw new System.ArgumentException(
                "The mapped message Call-ID does not match its selected leg.",
                "mapping");
        }
    }

    void ValidateMapping(SipBackToBackMapping mapping)
    {
        if (mapping == null || mapping.SourceMessage == null
            || mapping.MappedMessage == null)
        {
            throw new System.ArgumentException(
                "A complete back-to-back message mapping is required.",
                "mapping");
        }
        ValidateMessageBelongsToLeg(mapping.SourceLeg,
            mapping.SourceMessage);
        SipBackToBackLeg destination = mapping.SourceLeg
            == SipBackToBackLeg.Left
                ? SipBackToBackLeg.Right : SipBackToBackLeg.Left;
        ValidateMessageBelongsToLeg(destination, mapping.MappedMessage);
    }

    static void ValidateInitialRequest(
        SipInviteRequest? request, string parameterName)
    {
        if (request == null || request.Message == null
            || request.Message.Kind != SipMessageKind.Request
            || request.Message.Method != SipMethod.Invite)
        {
            throw new System.ArgumentException(
                "An initial INVITE request is required.", parameterName);
        }
    }

    static string ReadRequiredCallId(
        SipMessage? message, string parameterName)
    {
        if (message != null && message.Headers != null)
        {
            int index = 0;
            while (index < message.Headers.Length)
            {
                SipHeader? header = message.Headers[index];
                if (header != null
                    && (SipAscii.EqualsIgnoreCase(
                            header.Name, SipHeaderNames.CallId)
                        || SipAscii.EqualsIgnoreCase(
                            header.Name, SipHeaderNames.CompactCallId))
                    && header.Value != null
                    && header.Value.Trim().Length > 0)
                {
                    return header.Value.Trim();
                }
                index = index + 1;
            }
        }
        throw new System.ArgumentException(
            "A non-empty Call-ID header field is required.", parameterName);
    }

    static bool IsInviteUsageMessage(SipMessage message)
    {
        if (message.Kind == SipMessageKind.Response)
        {
            return message.CSeq != null
                && (message.CSeq.Method == SipMethod.Invite
                    || message.CSeq.Method == SipMethod.Cancel
                    || message.CSeq.Method == SipMethod.Bye
                    || message.CSeq.Method == SipMethod.Update
                    || message.CSeq.Method == SipMethod.Prack);
        }
        return message.Kind == SipMessageKind.Request
            && (message.Method == SipMethod.Invite
                || message.Method == SipMethod.Cancel
                || message.Method == SipMethod.Ack
                || message.Method == SipMethod.Bye
                || message.Method == SipMethod.Update
                || message.Method == SipMethod.Prack);
    }

    void ApplyInitialCancellation(
        SipCancellation? leftCancellation,
        SipCancellation? rightCancellation)
    {
        if (leftCancellation != null && leftCancellation.Requested)
        {
            TerminateBackToBackSession(leftCancellation);
        }
        else if (rightCancellation != null && rightCancellation.Requested)
        {
            TerminateBackToBackSession(rightCancellation);
        }
    }

    static bool HasRequestedCancellation(SipCancellation? value)
    {
        return value != null && value.Requested;
    }

    static void ValidateCancellation(SipCancellation? value)
    {
        if (value == null)
        {
            throw new System.ArgumentException(
                "A cancellation request is required.", "cancellation");
        }
    }

    static SipMessage? TerminateLiveLeg(
        SipInviteSession? session, SipCancellation value)
    {
        if (session == null)
        {
            return null;
        }
        DrainInviteEvents(session);
        SipInviteSnapshot snapshot = session.GetInviteSnapshot();
        if (snapshot.State != SipInviteSessionState.None
            && snapshot.State != SipInviteSessionState.Terminated
            && snapshot.State != SipInviteSessionState.Terminating)
        {
            session.TerminateInvite(value);
            return DrainInviteEvents(session);
        }
        return null;
    }

    static SipMessage? DrainInviteEvents(SipInviteSession session)
    {
        SipMessage? message = null;
        SipInviteEvent value = session.PollInviteEvent();
        while (value.State != SipInviteSessionState.None)
        {
            if (value.Message != null)
            {
                message = SipSnapshotCopy.CopyMessageModel(value.Message);
            }
            value = session.PollInviteEvent();
        }
        return message;
    }

    SipInviteSession SelectLeg(SipBackToBackLeg leg)
    {
        EnsureCreated();
        if (leg == SipBackToBackLeg.Left)
        {
            return RequiredLeft();
        }
        if (leg == SipBackToBackLeg.Right)
        {
            return RequiredRight();
        }
        throw new System.ArgumentException(
            "A left or right back-to-back session leg is required.", "leg");
    }

    SipInviteSession RequiredLeft()
    {
        if (left == null)
        {
            throw new System.InvalidOperationException(
                "The left back-to-back session leg is unavailable.");
        }
        return left;
    }

    SipInviteSession RequiredRight()
    {
        if (right == null)
        {
            throw new System.InvalidOperationException(
                "The right back-to-back session leg is unavailable.");
        }
        return right;
    }

    void EnsureCreated()
    {
        if (left == null || right == null
            || state == SipBackToBackState.None)
        {
            throw new System.InvalidOperationException(
                "No back-to-back session has been created.");
        }
    }
}
