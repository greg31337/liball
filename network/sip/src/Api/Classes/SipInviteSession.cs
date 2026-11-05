namespace sip;

/// <summary>Coordinates outgoing and incoming INVITE usages.</summary>
public class SipInviteSession : ISipInviteSession
{
    object gate = new object();
    System.Collections.Generic.List<SipInviteEvent> events =
        new System.Collections.Generic.List<SipInviteEvent>();
    System.Collections.Generic.List<SipInviteSnapshot> snapshots =
        new System.Collections.Generic.List<SipInviteSnapshot>();
    SipDialogLayer dialogs = new SipDialogLayer();
    SipOfferAnswerSession offerAnswers = new SipOfferAnswerSession();
    SipProtocolTokenGenerator tokens;
    SipInviteSessionState state;
    SipDialogRole role;
    SipMessage? invite;
    SipTransactionKey? inviteKey;
    SipTransactionKey? cancelKey;
    SipTransactionKey? byeKey;
    SipDialogSnapshot? dialog;
    SipInviteOptions options = new SipInviteOptions();
    string? correlationId;
    int redirectCount;
    bool provisionalReceived;
    bool cancellationPending;
    bool cancelSent;
    string? localTag;
    SipMessage? lastInviteResponse;
    SipMessage? lastObservedResponse;
    bool acceptedInvite;
    bool awaitingAck;
    long nowMilliseconds;
    bool peerSupportsReliableProvisional;
    bool peerRequiresReliableProvisional;
    int nextLocalRSeq = 1;
    int pendingLocalRSeq;
    int lastRemoteRSeq;
    int pendingRemoteRSeq;
    int pendingRemoteInviteCSeq;
    bool awaitingPrackAnswer;
    SipMessage? reliableProvisionalResponse;
    long reliableRetransmitAt;
    long reliableTimeoutAt;
    int reliableRetransmitInterval;
    SipMessage? prackRequest;
    SipTransactionKey? prackKey;
    long prackRetransmitAt;
    long prackTimeoutAt;
    int prackRetransmitInterval;
    bool prackReliableTransport;
    SipTransactionKey? receivedPrackKey;
    SipMessage? receivedPrackResponse;
    SipMessage? localRefreshRequest;
    SipTransactionKey? localRefreshKey;
    SipMessage? incomingRefreshRequest;
    SipTransactionKey? incomingRefreshKey;
    SipMessage? lastRefreshResponse;
    bool incomingRefreshAccepted;
    bool incomingRefreshAwaitingAck;
    bool sessionTimerActive;
    bool localSessionRefresher;
    bool automaticSessionRefresh;
    bool peerAllowsUpdate;
    int sessionIntervalSeconds;
    int sessionMinimumSeconds = 90;
    long sessionRefreshAt;
    long sessionExpiresAt;
    long suspendedSessionRefreshAt;
    SipMessage? sessionTimerSource;
    SipNameAddress? connectedIdentity;
    SipNameAddress? pendingConnectedIdentity;
    string? localSessionId;
    string? remoteSessionId;
    SipCapabilitySet remoteCapabilities = new SipCapabilitySet();
    SipExtensionHeaderValue[] remoteFeatureCapabilities =
        new SipExtensionHeaderValue[0];
    bool peerSupportsFromChange;
    bool peerSupportsEarlyDialogTermination;
    long operationDeadlineMilliseconds = -1;
    long createdAtMilliseconds = -1;
    string? cancellationId;
    string? authenticationContextId;
    int authenticationRetryCount;
    string? errorMessage;

    /// <summary>Initializes an idle INVITE session.</summary>
    public SipInviteSession()
    {
        tokens = new SipProtocolTokenGenerator(new SipCryptographicRandomSource());
    }

    /// <summary>Starts an INVITE or dialog refresh, or sends an incoming response.</summary>
    /// <param name="request">The initial or in-dialog request or response command.</param>
    /// <returns>The initial event.</returns>
    public SipInviteEvent StartInvite(SipInviteRequest request)
    {
        lock (gate)
        {
            ValidateStartRequest(request);
            if (incomingRefreshRequest != null && request.Message != null
                && request.Message.Kind == SipMessageKind.Response)
            {
                return SendIncomingRefreshResponse(request);
            }
            if (state == SipInviteSessionState.Confirmed
                && request.Message != null
                && request.Message.Kind == SipMessageKind.Request
                && (request.Message.Method == SipMethod.Invite
                    || request.Message.Method == SipMethod.Update))
            {
                return StartLocalRefresh(request);
            }
            if (role == SipDialogRole.Uas
                && state != SipInviteSessionState.None
                && request.Message != null
                && request.Message.Kind == SipMessageKind.Response)
            {
                return SendIncomingResponse(request);
            }
            if (role == SipDialogRole.Uac && awaitingPrackAnswer
                && request.OfferAnswer != null)
            {
                return SendPrackAnswer(request);
            }
            if (state != SipInviteSessionState.None
                && state != SipInviteSessionState.Terminated)
            {
                throw new System.InvalidOperationException(
                    "An INVITE usage is already active.");
            }

            bool authenticationRetry = IsAuthenticationRetry(request);
            if (authenticationRetry && !options.AllowAuthenticationRetry)
            {
                throw new System.InvalidOperationException(
                    "Authentication retry is disabled for this INVITE usage.");
            }
            string? retainedSessionId = authenticationRetry
                ? localSessionId : null;
            string? retainedCorrelationId = authenticationRetry
                ? correlationId : null;
            int retainedRetryCount = authenticationRetry
                ? authenticationRetryCount + 1 : 0;
            ResetSession(request);
            if (authenticationRetry)
            {
                localSessionId = retainedSessionId;
                authenticationRetryCount = retainedRetryCount;
                if (correlationId == null)
                {
                    correlationId = retainedCorrelationId;
                }
            }
            role = SipDialogRole.Uac;
            SipMessage outgoing = CopyRequiredMessage(request.Message);
            if (request.Target != null)
            {
                outgoing.RequestUri = CopyTarget(request.Target, "request");
                outgoing.OriginalBytes = new byte[0];
            }
            PrepareInitialOutgoingMetadata(outgoing);
            if (authenticationRetry && retainedSessionId != null)
            {
                localSessionId = retainedSessionId;
                ApplyLocalSessionIdentity(outgoing);
            }
            ApplyOutgoingReliableProvisionalPolicy(outgoing);
            ApplyOutgoingSessionTimerPolicy(outgoing);
            ValidateOutgoingInvite(outgoing);
            InitializeOfferAnswer(request, outgoing);
            invite = outgoing;
            inviteKey = SipTransactionMatching.CreateKey(
                outgoing, SipTransactionRole.Client);
            state = SipInviteSessionState.Calling;
            SipInviteEvent started = QueueEvent(outgoing,
                authenticationRetry
                    ? "Authenticated INVITE retry started."
                    : "Outgoing INVITE started.");

            if (operationDeadlineMilliseconds >= 0
                && operationDeadlineMilliseconds <= nowMilliseconds)
            {
                AdvanceOperationDeadline();
            }
            else if (request.Cancellation != null
                && request.Cancellation.Requested)
            {
                cancellationId = request.Cancellation.Id;
                RequestTermination(request.Cancellation);
            }
            return CopyRequiredEvent(started);
        }
    }

    /// <summary>Processes an INVITE usage message.</summary>
    /// <param name="message">The received message.</param>
    /// <returns>The resulting event.</returns>
    public SipInviteEvent ReceiveInviteMessage(SipMessage message)
    {
        lock (gate)
        {
            if (message == null)
            {
                throw new System.ArgumentException(
                    "A SIP message is required.", "message");
            }
            if (state == SipInviteSessionState.None)
            {
                return StartIncomingInvite(message);
            }
            if (message.Kind == SipMessageKind.Request
                && state == SipInviteSessionState.Confirmed
                && (message.Method == SipMethod.Invite
                    || message.Method == SipMethod.Update))
            {
                if (role == SipDialogRole.Uas
                    && message.Method == SipMethod.Invite
                    && SipTransactionMatching.MatchRequest(
                        inviteKey, message, localTag, true))
                {
                    return ProcessIncomingInviteRetransmission(message);
                }
                return ProcessIncomingRefreshRequest(message);
            }
            if (message.Kind == SipMessageKind.Request
                && message.Method == SipMethod.Ack
                && incomingRefreshAwaitingAck)
            {
                return ProcessIncomingRefreshAck(message);
            }
            if (role == SipDialogRole.Uas)
            {
                return ProcessIncomingMessage(message);
            }
            if (state == SipInviteSessionState.Terminated || invite == null)
            {
                throw new System.InvalidOperationException(
                    "No INVITE usage is active.");
            }
            ValidateResponse(message);
            CaptureRemoteMessageMetadata(message, false);
            if (message.CSeq == null)
            {
                throw new System.ArgumentException(
                    "A response CSeq is required.", "message");
            }
            if (message.CSeq.Method == SipMethod.Invite)
            {
                if (localRefreshRequest != null
                    && localRefreshRequest.Method == SipMethod.Invite)
                {
                    return ProcessLocalRefreshResponse(message);
                }
                return ProcessInviteResponse(message);
            }
            if (message.CSeq.Method == SipMethod.Cancel)
            {
                return ProcessCancelResponse(message);
            }
            if (message.CSeq.Method == SipMethod.Bye)
            {
                return ProcessByeResponse(message);
            }
            if (message.CSeq.Method == SipMethod.Prack)
            {
                return ProcessPrackResponse(message);
            }
            if (message.CSeq.Method == SipMethod.Update)
            {
                return ProcessLocalRefreshResponse(message);
            }
            throw new System.ArgumentException(
                "The response does not belong to this INVITE usage.", "message");
        }
    }

    /// <summary>Terminates an INVITE usage.</summary>
    /// <param name="cancellation">The termination request.</param>
    public void TerminateInvite(SipCancellation cancellation)
    {
        lock (gate)
        {
            ValidateCancellation(cancellation);
            if (!cancellation.Requested)
            {
                return;
            }
            cancellationId = cancellation.Id;
            if (state == SipInviteSessionState.None
                || state == SipInviteSessionState.Terminated)
            {
                throw new System.InvalidOperationException(
                    "No live INVITE usage can be terminated.");
            }
            if (role == SipDialogRole.Uas)
            {
                RequestIncomingTermination(cancellation);
            }
            else
            {
                RequestTermination(cancellation);
            }
        }
    }

    /// <summary>Advances reliable provisional, PRACK, and session timers.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    public void AdvanceInviteTime(long nowMilliseconds)
    {
        lock (gate)
        {
            if (nowMilliseconds < this.nowMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException("nowMilliseconds");
            }
            this.nowMilliseconds = nowMilliseconds;
            AdvanceOperationDeadline();
            AdvanceReliableProvisionalTimer();
            AdvancePrackTimer();
            AdvanceSessionTimer();
        }
    }

    /// <summary>Associates an authentication context with retries and any live dialog.</summary>
    /// <param name="authenticationContextId">The authentication context identifier.</param>
    public void AssociateInviteAuthenticationContext(string authenticationContextId)
    {
        lock (gate)
        {
            ValidateAuthenticationContextId(authenticationContextId);
            if (state == SipInviteSessionState.None
                || state == SipInviteSessionState.Terminated)
            {
                throw new System.InvalidOperationException(
                    "A live INVITE usage is required.");
            }
            this.authenticationContextId = authenticationContextId;
            AssociateCurrentDialogAuthentication();
            QueueEvent(null,
                "An authentication context was associated with the INVITE usage.");
        }
    }

    /// <summary>Reports a transport failure for the active INVITE usage.</summary>
    /// <param name="errorMessage">The bounded failure description.</param>
    public void ReportInviteTransportError(string errorMessage)
    {
        lock (gate)
        {
            ValidateTransportError(errorMessage);
            if (state == SipInviteSessionState.None
                || state == SipInviteSessionState.Terminated)
            {
                throw new System.InvalidOperationException(
                    "A live INVITE usage is required.");
            }
            this.errorMessage = errorMessage;
            FailOutstandingOfferAnswer();
            TerminateCurrentDialog(errorMessage);
            state = SipInviteSessionState.Terminated;
            CleanupTerminalState();
            QueueEvent(null, "INVITE transport failure: " + errorMessage);
        }
    }

    /// <summary>Polls the next INVITE usage event.</summary>
    /// <returns>The next event.</returns>
    public SipInviteEvent PollInviteEvent()
    {
        lock (gate)
        {
            if (events.Count == 0)
            {
                return new SipInviteEvent();
            }
            SipInviteEvent value = events[0];
            events.RemoveAt(0);
            return CopyRequiredEvent(value);
        }
    }

    /// <summary>Polls the next queued INVITE usage snapshot.</summary>
    /// <returns>The next snapshot, or an empty snapshot when none is queued.</returns>
    public SipInviteSnapshot PollInviteSnapshot()
    {
        lock (gate)
        {
            if (snapshots.Count == 0)
            {
                return new SipInviteSnapshot();
            }
            SipInviteSnapshot value = snapshots[0];
            snapshots.RemoveAt(0);
            return CopyRequiredInviteSnapshot(value);
        }
    }

    /// <summary>Gets the current detached INVITE usage snapshot.</summary>
    /// <returns>The current snapshot.</returns>
    public SipInviteSnapshot GetInviteSnapshot()
    {
        lock (gate)
        {
            return CreateInviteSnapshot();
        }
    }

    SipInviteEvent StartIncomingInvite(SipMessage message)
    {
        ValidateIncomingInvite(message);
        ResetIncomingSession();
        role = SipDialogRole.Uas;
        invite = CopyRequiredMessage(message);
        inviteKey = SipTransactionMatching.CreateKey(
            invite, SipTransactionRole.Server);
        CaptureRemoteMessageMetadata(invite, true);
        CaptureSessionTimerRequest(invite);
        peerSupportsReliableProvisional = MessageHasOptionTag(
            invite, SipHeaderNames.Supported,
            SipOptionTags.ReliableProvisional)
            || MessageHasOptionTag(invite, SipHeaderNames.Require,
                SipOptionTags.ReliableProvisional);
        peerRequiresReliableProvisional = MessageHasOptionTag(
            invite, SipHeaderNames.Require,
            SipOptionTags.ReliableProvisional);
        InitializeIncomingOfferAnswer(invite);
        state = SipInviteSessionState.Proceeding;
        SipMessage trying = SipTransactionMessageFactory.CreateTryingResponse(invite);
        ApplyLocalSessionIdentity(trying);
        lastInviteResponse = CopyRequiredMessage(trying);
        QueueEvent(trying, "Automatic 100 Trying response generated.");
        SipInviteEvent received = QueueEvent(invite,
            "Incoming INVITE received.");
        return CopyRequiredEvent(received);
    }

    SipInviteEvent SendIncomingResponse(SipInviteRequest command)
    {
        if (state == SipInviteSessionState.Terminated || invite == null)
        {
            throw new System.InvalidOperationException(
                "The incoming INVITE can no longer send a response.");
        }
        AdoptCommandAuthenticationContext(command);
        SipMessage response = CopyRequiredMessage(command.Message);
        ValidateIncomingResponsePair(response);
        int status = response.StatusCode;
        if (status > 100)
        {
            AdoptLocalTag(response);
            AddOptionTag(response, SipHeaderNames.Supported,
                SipOptionTags.FromChange);
        }
        ApplyLocalSessionIdentity(response);
        response = PrepareIncomingSessionTimerResponse(
            response, invite, command.Options);
        status = response.StatusCode;
        if (status < 200)
        {
            PrepareOutgoingReliableProvisional(response, command.Options);
            return SendIncomingProvisional(response);
        }
        if (status < 300)
        {
            return SendIncomingSuccess(response);
        }
        return SendIncomingFailure(response);
    }

    SipInviteEvent SendIncomingProvisional(SipMessage response)
    {
        if (acceptedInvite)
        {
            throw new System.InvalidOperationException(
                "A provisional response cannot follow a final response.");
        }
        if (response.StatusCode == SipStatusCodes.EarlyDialogTerminated)
        {
            return SendEarlyDialogTerminated(response);
        }
        if (response.StatusCode > SipStatusCodes.Trying)
        {
            ApplyLocalResponseBody(response);
            dialog = dialogs.CreateDialog(
                CopyRequiredMessage(invite), response, CreateUasDialogOptions());
            AssociateCurrentDialogAuthentication();
            state = SipInviteSessionState.Early;
        }
        else
        {
            state = SipInviteSessionState.Proceeding;
        }
        lastInviteResponse = CopyRequiredMessage(response);
        string reason = ResponseIsReliableProvisional(response)
            ? "Reliable provisional INVITE response generated."
            : response.StatusCode == SipStatusCodes.Ringing
                ? "Ringing response generated."
                : "Provisional INVITE response generated.";
        SipInviteEvent sent = QueueEvent(response, reason);
        return CopyRequiredEvent(sent);
    }

    SipInviteEvent SendEarlyDialogTerminated(SipMessage response)
    {
        ValidateEarlyDialogTerminated(response, true);
        if (dialog == null || dialog.Id == null
            || dialog.State != SipDialogState.Early
            || ReadTag(response.To) != dialog.Id.LocalTag)
        {
            throw new System.InvalidOperationException(
                "A 199 response requires the matching early dialog.");
        }
        SipDialogSnapshot terminated = CopyRequiredDialogSnapshot(dialog);
        SipCancellation cancellation = CreateEarlyDialogCancellation(response);
        dialogs.TerminateDialog(dialog.Id, cancellation);
        terminated.State = SipDialogState.Terminated;
        ResetInitialOfferAnswer();
        if (!ResponseIsReliableProvisional(response))
        {
            ClearReliableProvisionalTimer();
        }
        state = SipInviteSessionState.Proceeding;
        dialog = terminated;
        lastInviteResponse = CopyRequiredMessage(response);
        SipInviteEvent sent = QueueEvent(response,
            "The 199 response terminated the early dialog.");
        dialog = null;
        return CopyRequiredEvent(sent);
    }

    SipInviteEvent SendIncomingSuccess(SipMessage response)
    {
        if (acceptedInvite)
        {
            if (!IncomingResponsesEquivalent(lastInviteResponse, response))
            {
                throw new System.InvalidOperationException(
                    "A different final success response was already sent.");
            }
            SipInviteEvent replay = QueueEvent(response,
                "The successful INVITE response was retransmitted.");
            return CopyRequiredEvent(replay);
        }
        if (reliableProvisionalResponse != null)
        {
            throw new System.InvalidOperationException(
                "The reliable provisional response must be acknowledged before success.");
        }
        ApplyLocalResponseBody(response);
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        if (current.State == SipOfferAnswerState.RemoteOfferPending)
        {
            throw new SipProtocolException(
                "A successful INVITE response must answer the remote offer.",
                SipStatusCodes.NotAcceptableHere, SipMethods.Invite);
        }
        dialog = dialogs.CreateDialog(
            CopyRequiredMessage(invite), response, CreateUasDialogOptions());
        AssociateCurrentDialogAuthentication();
        acceptedInvite = true;
        awaitingAck = true;
        state = SipInviteSessionState.Confirmed;
        ConfigureSessionTimerFromSuccess(
            response, CopyRequiredMessage(invite), false);
        lastInviteResponse = CopyRequiredMessage(response);
        SipInviteEvent accepted = QueueEvent(response,
            "The incoming INVITE was answered successfully.");
        return CopyRequiredEvent(accepted);
    }

    SipInviteEvent SendIncomingFailure(SipMessage response)
    {
        if (acceptedInvite)
        {
            throw new System.InvalidOperationException(
                "A failure response cannot follow a successful final response.");
        }
        TerminateEarlyDialog(response);
        ClearReliableProvisionalTimer();
        FailIncomingOfferAnswer();
        state = SipInviteSessionState.Terminated;
        awaitingAck = true;
        lastInviteResponse = CopyRequiredMessage(response);
        SipInviteEvent rejected = QueueEvent(response,
            "The incoming INVITE was rejected.");
        return CopyRequiredEvent(rejected);
    }

    SipInviteEvent ProcessIncomingMessage(SipMessage message)
    {
        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }
        CaptureRemoteMessageMetadata(message, false);
        if (message.Kind == SipMessageKind.Response)
        {
            ValidateResponse(message);
            if (message.CSeq != null && localRefreshRequest != null
                && message.CSeq.Method == localRefreshRequest.Method)
            {
                return ProcessLocalRefreshResponse(message);
            }
            if (message.CSeq != null && message.CSeq.Method == SipMethod.Bye)
            {
                return ProcessByeResponse(message);
            }
            throw new System.ArgumentException(
                "The response does not belong to this incoming INVITE usage.",
                "message");
        }
        if (message.Kind != SipMessageKind.Request)
        {
            throw new System.ArgumentException(
                "A valid INVITE usage request is required.", "message");
        }
        if (message.Method == SipMethod.Invite)
        {
            if (state == SipInviteSessionState.Confirmed
                && !SipTransactionMatching.MatchRequest(
                    inviteKey, message, localTag, true))
            {
                return ProcessIncomingRefreshRequest(message);
            }
            return ProcessIncomingInviteRetransmission(message);
        }
        if (message.Method == SipMethod.Update)
        {
            return ProcessIncomingRefreshRequest(message);
        }
        if (message.Method == SipMethod.Cancel)
        {
            return ProcessIncomingCancel(message);
        }
        if (message.Method == SipMethod.Ack)
        {
            return ProcessIncomingAck(message);
        }
        if (message.Method == SipMethod.Bye)
        {
            return ProcessIncomingBye(message);
        }
        if (message.Method == SipMethod.Prack)
        {
            return ProcessIncomingPrack(message);
        }
        throw new System.ArgumentException(
            "The request does not belong to this INVITE usage.", "message");
    }

    SipInviteEvent ProcessIncomingInviteRetransmission(SipMessage message)
    {
        if (!SipTransactionMatching.MatchRequest(
            inviteKey, message, localTag, true))
        {
            throw new System.ArgumentException(
                "The INVITE does not match the active server transaction.",
                "message");
        }
        SipInviteEvent received = QueueEvent(message,
            "An INVITE retransmission was received.");
        if (lastInviteResponse != null)
        {
            QueueEvent(lastInviteResponse,
                "The latest INVITE response was automatically replayed.");
        }
        return CopyRequiredEvent(received);
    }

    SipInviteEvent ProcessIncomingCancel(SipMessage message)
    {
        if (!SipTransactionMatching.MatchCancel(inviteKey, message, true))
        {
            throw new System.ArgumentException(
                "The CANCEL does not match the incoming INVITE.", "message");
        }
        EnsureLocalTag();
        SipMessage ok = SipTransactionMessageFactory.CreateCancelOk(
            message, localTag);
        ApplyLocalSessionIdentity(ok);
        if (!acceptedInvite && state != SipInviteSessionState.Terminated)
        {
            SipMessage terminated = SipTransactionMessageFactory.CreateInviteTerminated(
                invite, localTag);
            ApplyLocalSessionIdentity(terminated);
            CopyMessageHeaders(message, terminated, SipHeaderNames.Reason);
            ClearReliableProvisionalTimer();
            TerminateEarlyDialog(terminated);
            FailIncomingOfferAnswer();
            state = SipInviteSessionState.Terminated;
            awaitingAck = true;
            lastInviteResponse = CopyRequiredMessage(terminated);
            SipInviteEvent received = QueueEvent(message,
                "The incoming INVITE was cancelled.");
            QueueEvent(ok, "Automatic 200 response generated for CANCEL.");
            QueueEvent(terminated,
                "Automatic 487 response generated for the INVITE.");
            return CopyRequiredEvent(received);
        }
        SipInviteEvent ignored = QueueEvent(message,
            acceptedInvite
                ? "CANCEL received after the INVITE was accepted."
                : "A repeated CANCEL was received.");
        QueueEvent(ok, "Automatic 200 response generated for CANCEL.");
        if (!acceptedInvite && lastInviteResponse != null)
        {
            QueueEvent(lastInviteResponse,
                "The final INVITE response was automatically replayed.");
        }
        return CopyRequiredEvent(ignored);
    }

    SipInviteEvent ProcessIncomingAck(SipMessage message)
    {
        if (incomingRefreshAwaitingAck)
        {
            return ProcessIncomingRefreshAck(message);
        }
        bool firstAck = awaitingAck;
        bool matched;
        if (acceptedInvite)
        {
            matched = localTag != null && SipTransactionMatching.MatchAcceptedAck(
                inviteKey, message, new string[] { localTag });
        }
        else
        {
            matched = SipTransactionMatching.MatchRequest(
                inviteKey, message, localTag, true);
        }
        if (!matched)
        {
            throw new System.ArgumentException(
                "The ACK does not match the incoming INVITE.", "message");
        }
        if (acceptedInvite)
        {
            ApplyIncomingAckBody(message);
            if (dialog == null || dialog.Id == null)
            {
                throw new System.InvalidOperationException(
                    "The accepted INVITE lacks a dialog.");
            }
            dialog = dialogs.UpdateDialog(dialog.Id, message);
        }
        awaitingAck = false;
        string reason = acceptedInvite && firstAck
            ? "ACK confirmed the incoming INVITE usage."
            : acceptedInvite
                ? "A repeated ACK was received."
                : firstAck
                    ? "ACK completed the rejected INVITE transaction."
                    : "A repeated failure ACK was received.";
        SipInviteEvent ack = QueueEvent(message, reason);
        return CopyRequiredEvent(ack);
    }

    SipInviteEvent ProcessIncomingBye(SipMessage message)
    {
        if (!acceptedInvite || dialog == null || dialog.Id == null
            || state != SipInviteSessionState.Confirmed)
        {
            throw new System.InvalidOperationException(
                "The incoming BYE requires a confirmed INVITE usage.");
        }
        EnsureLocalTag();
        SipMessage ok = SipTransactionMessageFactory.CreateCancelOk(
            message, localTag);
        ApplyLocalSessionIdentity(ok);
        dialog = dialogs.UpdateDialog(dialog.Id, message);
        state = SipInviteSessionState.Terminated;
        CleanupTerminalState();
        SipInviteEvent bye = QueueEvent(message,
            "The remote endpoint terminated the INVITE usage with BYE.");
        QueueEvent(ok, "Automatic 200 response generated for BYE.");
        return CopyRequiredEvent(bye);
    }

    SipInviteEvent StartLocalRefresh(SipInviteRequest command)
    {
        if (dialog == null || dialog.Id == null || command.Message == null)
        {
            throw new System.InvalidOperationException(
                "A confirmed dialog is required for a local refresh.");
        }
        if (localRefreshRequest != null || incomingRefreshRequest != null
            || awaitingAck || awaitingPrackAnswer || prackTimeoutAt > 0)
        {
            throw new System.InvalidOperationException(
                "Another dialog offer or refresh is already pending.");
        }
        AdoptCommandAuthenticationContext(command);
        SipMessage template = CopyRequiredMessage(command.Message);
        if (template.Method != SipMethod.Invite
            && template.Method != SipMethod.Update)
        {
            throw new System.ArgumentException(
                "A re-INVITE or UPDATE request is required.", "request");
        }
        ApplyLocalRefreshOffer(command, template);
        string method = template.Method == SipMethod.Invite
            ? SipMethods.Invite : SipMethods.Update;
        SipUri? target = ReadRefreshContact(template);
        if (command.Target != null)
        {
            target = CopyTarget(command.Target, "request");
        }
        SipMessage generated = target == null
            ? dialogs.CreateRequest(dialog.Id, method)
            : dialogs.CreateTargetRefreshRequest(
                dialog.Id, method, target);
        ApplyLocalSessionIdentity(generated);
        CopyRefreshBody(template, generated);
        ApplyLocalSessionTimerHeaders(generated, template);
        localRefreshRequest = CopyRequiredMessage(generated);
        localRefreshKey = SipTransactionMatching.CreateKey(
            generated, SipTransactionRole.Client);
        automaticSessionRefresh = false;
        if (sessionTimerActive && localSessionRefresher)
        {
            suspendedSessionRefreshAt = sessionRefreshAt;
            sessionRefreshAt = 0;
        }
        SipInviteEvent started = QueueEvent(generated,
            generated.Method == SipMethod.Invite
                ? "A local re-INVITE was started."
                : "A local UPDATE was started.");
        return CopyRequiredEvent(started);
    }

    SipInviteEvent ProcessLocalRefreshResponse(SipMessage response)
    {
        if (localRefreshRequest == null || localRefreshKey == null
            || dialog == null || dialog.Id == null
            || !SipTransactionMatching.MatchResponse(
                localRefreshKey, response, true))
        {
            throw new System.ArgumentException(
                "The response does not match the pending dialog refresh.",
                "message");
        }
        if (response.StatusCode < 200
            && localRefreshRequest.Method == SipMethod.Update)
        {
            throw new SipProtocolException(
                "UPDATE cannot receive a provisional response.",
                SipStatusCodes.BadRequest, SipMethods.Update);
        }
        lastRefreshResponse = CopyRequiredMessage(response);
        if (response.StatusCode < 200)
        {
            if (response.Body.Length > 0)
            {
                ApplyRemoteBody(response);
            }
            SipInviteEvent provisional = QueueEvent(response,
                "A provisional re-INVITE response was received.");
            return CopyRequiredEvent(provisional);
        }

        SipMessage request = localRefreshRequest;
        if (response.StatusCode < 300)
        {
            ApplySuccessfulLocalRefreshBody(response);
            dialog = dialogs.UpdateDialog(dialog.Id, response);
            ConfigureSessionTimerFromSuccess(response, request, false);
            SipInviteEvent completed = QueueEvent(response,
                request.Method == SipMethod.Invite
                    ? "The local re-INVITE completed successfully."
                    : "The local UPDATE completed successfully.");
            if (request.Method == SipMethod.Invite)
            {
                SipMessage ack = CreateRefreshSuccessAck(request);
                QueueEvent(ack,
                    "ACK generated for the successful re-INVITE response.");
            }
            ClearLocalRefresh();
            return CopyRequiredEvent(completed);
        }

        SipInviteEvent? timerRetry = RetryLocalSessionTimer(
            request, response);
        if (timerRetry != null)
        {
            return CopyRequiredEvent(timerRetry);
        }
        bool wasAutomaticRefresh = automaticSessionRefresh;
        int automaticRetrySeconds = wasAutomaticRefresh
            ? ReadSessionRefreshRetryDelay(response) : 1;
        bool terminateForTimer = wasAutomaticRefresh
            && (response.StatusCode == SipStatusCodes.RequestTimeout
                || response.StatusCode
                    == SipStatusCodes.CallOrTransactionDoesNotExist);
        dialog = dialogs.UpdateDialog(dialog.Id, response);
        FailOutstandingOfferAnswer();
        if (terminateForTimer)
        {
            state = SipInviteSessionState.Terminating;
            StopSessionTimer();
        }
        SipInviteEvent failed = QueueEvent(response,
            terminateForTimer
                ? "The automatic session refresh failed and terminated the session."
                : DescribeRefreshFailure(response));
        if (request.Method == SipMethod.Invite)
        {
            SipMessage ack = SipTransactionMessageFactory.CreateFailureAck(
                request, response);
            ApplyLocalSessionIdentity(ack);
            QueueEvent(ack,
                "ACK generated for the failed re-INVITE response.");
        }
        ClearLocalRefresh();
        if (terminateForTimer)
        {
            SendBye("BYE generated after the session refresh failed.");
        }
        else if (sessionTimerActive && localSessionRefresher)
        {
            sessionRefreshAt = wasAutomaticRefresh
                ? AddSessionTimerDelay(automaticRetrySeconds, 1000)
                : suspendedSessionRefreshAt > nowMilliseconds
                    ? suspendedSessionRefreshAt
                    : AddSessionTimerDelay(1, 1000);
            suspendedSessionRefreshAt = 0;
        }
        return CopyRequiredEvent(failed);
    }

    SipInviteEvent ProcessIncomingRefreshRequest(SipMessage message)
    {
        if (dialog == null || dialog.Id == null)
        {
            throw new System.InvalidOperationException(
                "An incoming refresh requires a confirmed dialog.");
        }
        CaptureRemoteMessageMetadata(message, false);
        if (incomingRefreshRequest != null)
        {
            if (incomingRefreshKey != null
                && SipTransactionMatching.MatchRequest(
                    incomingRefreshKey, message, DialogLocalTag(), true))
            {
                SipInviteEvent repeated = QueueEvent(message,
                    "An in-dialog request retransmission was received.");
                if (lastRefreshResponse != null)
                {
                    QueueEvent(lastRefreshResponse,
                        "The latest in-dialog response was automatically replayed.");
                }
                return CopyRequiredEvent(repeated);
            }
            return RejectRefreshGlare(message);
        }
        if (localRefreshRequest != null || awaitingPrackAnswer
            || prackTimeoutAt > 0)
        {
            return RejectRefreshGlare(message);
        }
        SipTransactionKey key = SipTransactionMatching.CreateKey(
            message, SipTransactionRole.Server);
        CaptureSessionTimerRequest(message);
        ApplyRemoteRefreshOffer(message);
        dialog = dialogs.UpdateDialog(dialog.Id, message);
        incomingRefreshRequest = CopyRequiredMessage(message);
        incomingRefreshKey = key;
        incomingRefreshAccepted = false;
        incomingRefreshAwaitingAck = false;
        lastRefreshResponse = null;
        SipInviteEvent received = QueueEvent(message,
            message.Method == SipMethod.Invite
                ? "An incoming re-INVITE was received."
                : "An incoming UPDATE was received.");
        return CopyRequiredEvent(received);
    }

    SipInviteEvent SendIncomingRefreshResponse(SipInviteRequest command)
    {
        if (incomingRefreshRequest == null || incomingRefreshKey == null
            || dialog == null || dialog.Id == null)
        {
            throw new System.InvalidOperationException(
                "No incoming dialog refresh is awaiting a response.");
        }
        AdoptCommandAuthenticationContext(command);
        SipMessage response = CopyRequiredMessage(command.Message);
        ValidateRefreshResponsePair(
            response, incomingRefreshRequest, incomingRefreshKey);
        response = PrepareIncomingSessionTimerResponse(
            response, incomingRefreshRequest, command.Options);
        ApplyLocalSessionIdentity(response);
        if (incomingRefreshRequest.Method == SipMethod.Update
            && response.StatusCode < 200)
        {
            throw new System.ArgumentException(
                "UPDATE cannot be completed with a provisional response.",
                "request");
        }
        if (response.StatusCode < 200)
        {
            if (response.Body.Length > 0)
            {
                ApplyLocalResponseBody(response);
            }
            lastRefreshResponse = CopyRequiredMessage(response);
            SipInviteEvent provisional = QueueEvent(response,
                "A provisional re-INVITE response was generated.");
            return CopyRequiredEvent(provisional);
        }
        if (response.StatusCode < 300)
        {
            ApplySuccessfulIncomingRefreshBody(response);
            dialog = dialogs.UpdateDialog(dialog.Id, response);
            CommitPendingConnectedIdentity();
            ConfigureSessionTimerFromSuccess(
                response, incomingRefreshRequest, false);
            incomingRefreshAccepted = true;
            lastRefreshResponse = CopyRequiredMessage(response);
            SipInviteEvent accepted = QueueEvent(response,
                incomingRefreshRequest.Method == SipMethod.Invite
                    ? "The incoming re-INVITE was accepted."
                    : "The incoming UPDATE was accepted.");
            if (incomingRefreshRequest.Method == SipMethod.Invite)
            {
                incomingRefreshAwaitingAck = true;
            }
            else
            {
                ClearIncomingRefresh();
            }
            return CopyRequiredEvent(accepted);
        }

        dialog = dialogs.UpdateDialog(dialog.Id, response);
        pendingConnectedIdentity = null;
        FailIncomingOfferAnswer();
        lastRefreshResponse = CopyRequiredMessage(response);
        SipInviteEvent rejected = QueueEvent(response,
            incomingRefreshRequest.Method == SipMethod.Invite
                ? "The incoming re-INVITE was rejected."
                : "The incoming UPDATE was rejected.");
        if (incomingRefreshRequest.Method == SipMethod.Invite)
        {
            incomingRefreshAwaitingAck = true;
        }
        else
        {
            ClearIncomingRefresh();
        }
        return CopyRequiredEvent(rejected);
    }

    SipInviteEvent ProcessIncomingRefreshAck(SipMessage message)
    {
        if (incomingRefreshRequest == null || incomingRefreshKey == null
            || dialog == null || dialog.Id == null)
        {
            throw new System.InvalidOperationException(
                "No re-INVITE is awaiting ACK.");
        }
        bool matched = incomingRefreshAccepted
            ? SipTransactionMatching.MatchAcceptedAck(
                incomingRefreshKey, message,
                new string[] { DialogLocalTag() })
            : SipTransactionMatching.MatchRequest(
                incomingRefreshKey, message, DialogLocalTag(), true);
        if (!matched)
        {
            throw new System.ArgumentException(
                "The ACK does not match the pending re-INVITE.", "message");
        }
        if (incomingRefreshAccepted)
        {
            ApplyIncomingAckBody(message);
            dialog = dialogs.UpdateDialog(dialog.Id, message);
        }
        SipInviteEvent ack = QueueEvent(message,
            "ACK completed the incoming re-INVITE exchange.");
        ClearIncomingRefresh();
        return CopyRequiredEvent(ack);
    }

    SipInviteEvent RejectRefreshGlare(SipMessage message)
    {
        SipTransactionMatching.CreateKey(message, SipTransactionRole.Server);
        string tag = DialogLocalTag();
        SipMessage pending = SipTransactionMessageFactory.CreateRequestResponse(
            message, SipStatusCodes.RequestPending,
            SipReasonPhrases.RequestPending, tag);
        ApplyLocalSessionIdentity(pending);
        SetOrAddHeaderValue(pending, SipHeaderNames.RetryAfter, "1");
        pendingConnectedIdentity = null;
        SipInviteEvent glare = QueueEvent(message,
            "An overlapping dialog refresh was rejected with glare.");
        QueueEvent(pending,
            "Automatic 491 response generated with Retry-After 1 second.");
        return CopyRequiredEvent(glare);
    }

    SipInviteEvent ProcessInviteResponse(SipMessage response)
    {
        if (!SipTransactionMatching.MatchResponse(inviteKey, response, true))
        {
            throw new System.ArgumentException(
                "The response does not match the active INVITE.", "message");
        }
        lastInviteResponse = CopyRequiredMessage(response);
        CaptureRemoteMessageMetadata(response, true);
        if (response.StatusCode < 200)
        {
            return ProcessProvisionalResponse(response);
        }
        if (response.StatusCode < 300)
        {
            return ProcessSuccessfulResponse(response);
        }
        return ProcessUnsuccessfulResponse(response);
    }

    SipInviteEvent ProcessProvisionalResponse(SipMessage response)
    {
        provisionalReceived = true;
        if (response.StatusCode == SipStatusCodes.Trying)
        {
            if (!cancellationPending)
            {
                state = SipInviteSessionState.Proceeding;
            }
            SipInviteEvent trying = QueueEvent(response,
                "Provisional INVITE response received.");
            SendPendingCancel();
            return CopyRequiredEvent(trying);
        }

        int reliableSequence = ReadReliableProvisionalSequence(response);
        if (reliableSequence > 0 && reliableSequence == lastRemoteRSeq)
        {
            SipInviteEvent repeated = QueueEvent(response,
                "A reliable provisional response was retransmitted.");
            if (prackRequest != null)
            {
                QueueEvent(prackRequest,
                    "PRACK was automatically retransmitted.");
            }
            return CopyRequiredEvent(repeated);
        }
        if (reliableSequence > 0 && lastRemoteRSeq > 0
            && (lastRemoteRSeq == int.MaxValue
                || reliableSequence != lastRemoteRSeq + 1))
        {
            return FailRemoteReliableProvisional(response,
                "The reliable provisional response RSeq is out of order.");
        }
        if (reliableSequence > 0
            && (awaitingPrackAnswer || prackTimeoutAt > 0))
        {
            return FailRemoteReliableProvisional(response,
                "A previous reliable provisional response is still pending.");
        }

        if (response.StatusCode == SipStatusCodes.EarlyDialogTerminated)
        {
            return ProcessEarlyDialogTerminated(response, reliableSequence);
        }

        SipDialogSnapshot created = dialogs.CreateDialog(
            CopyRequiredMessage(invite), response, CreateDialogOptions());
        dialog = created;
        AssociateCurrentDialogAuthentication();
        ApplyRemoteBody(response);
        if (!cancellationPending)
        {
            state = SipInviteSessionState.Early;
        }
        string reason = reliableSequence > 0
            ? response.Body.Length == 0
                ? "A reliable provisional response established an early dialog."
                : "A reliable provisional response established early media signaling."
            : response.Body.Length == 0
                ? "An early dialog was established."
                : "An early dialog with media signaling was established.";
        SipInviteEvent provisional = QueueEvent(response, reason);
        if (reliableSequence > 0)
        {
            BeginRemoteReliableProvisional(response, reliableSequence);
        }
        SendPendingCancel();
        return CopyRequiredEvent(provisional);
    }

    SipInviteEvent ProcessEarlyDialogTerminated(
        SipMessage response, int reliableSequence)
    {
        ValidateEarlyDialogTerminated(response, false);
        string? remoteTag = ReadTag(response.To);
        if (dialog == null || dialog.Id == null
            || dialog.State != SipDialogState.Early
            || remoteTag != dialog.Id.RemoteTag)
        {
            if (dialog == null && !cancellationPending)
            {
                state = SipInviteSessionState.Proceeding;
            }
            SipInviteEvent ignored = QueueEvent(response,
                "A 199 response for an unknown early dialog was discarded.");
            return CopyRequiredEvent(ignored);
        }
        if (reliableSequence > 0)
        {
            BeginRemoteReliableProvisional(response, reliableSequence);
        }
        SipDialogSnapshot terminated = CopyRequiredDialogSnapshot(dialog);
        SipCancellation cancellation = CreateEarlyDialogCancellation(response);
        dialogs.TerminateDialog(dialog.Id, cancellation);
        terminated.State = SipDialogState.Terminated;
        ResetInitialOfferAnswer();
        if (!ResponseIsReliableProvisional(response))
        {
            ClearReliableProvisionalTimer();
        }
        if (reliableSequence == 0)
        {
            ClearPrackTimer();
        }
        awaitingPrackAnswer = false;
        state = cancellationPending
            ? SipInviteSessionState.Terminating
            : SipInviteSessionState.Proceeding;
        dialog = terminated;
        SipInviteEvent ended = QueueEvent(response,
            "The 199 response terminated the early dialog; the INVITE is still proceeding.");
        dialog = null;
        SendPendingCancel();
        return CopyRequiredEvent(ended);
    }

    SipInviteEvent ProcessSuccessfulResponse(SipMessage response)
    {
        SipDialogSnapshot confirmed = dialogs.CreateDialog(
            CopyRequiredMessage(invite), response, CreateDialogOptions());
        dialog = confirmed;
        AssociateCurrentDialogAuthentication();
        ApplyRemoteBody(response);
        SipMessage ack = dialogs.CreateRequest(
            CopyRequiredDialogId(confirmed.Id), SipMethods.Ack);
        ApplyLocalSessionIdentity(ack);

        if (cancellationPending)
        {
            state = SipInviteSessionState.Terminating;
            SipInviteEvent raced = QueueEvent(response,
                "The INVITE succeeded while cancellation was pending.");
            QueueEvent(ack, "ACK generated for the successful INVITE response.");
            SendBye("BYE generated after the successful cancellation race.");
            return CopyRequiredEvent(raced);
        }

        state = SipInviteSessionState.Confirmed;
        if (invite != null)
        {
            ConfigureSessionTimerFromSuccess(response, invite, true);
        }
        SipInviteEvent succeeded = QueueEvent(response,
            "The outgoing INVITE usage was confirmed.");
        QueueEvent(ack, "ACK generated for the successful INVITE response.");
        return CopyRequiredEvent(succeeded);
    }

    SipInviteEvent ProcessUnsuccessfulResponse(SipMessage response)
    {
        ClearPrackTimer();
        awaitingPrackAnswer = false;
        SipInviteEvent? timerRetry = RetryInitialSessionTimer(response);
        if (timerRetry != null)
        {
            return CopyRequiredEvent(timerRetry);
        }
        SipMessage ack = SipTransactionMessageFactory.CreateFailureAck(
            invite, response);
        ApplyLocalSessionIdentity(ack);
        QueueEvent(ack, "ACK generated for the final INVITE response.");
        TerminateEarlyDialog(response);

        if (response.StatusCode < 400 && FollowRedirect(response))
        {
            dialog = null;
            SipInviteEvent redirected = QueueEvent(
                CopyRequiredMessage(invite), "The INVITE was redirected.");
            return CopyRequiredEvent(redirected);
        }

        FailOutstandingOfferAnswer();
        state = SipInviteSessionState.Terminated;
        string reason = response.StatusCode < 400
            ? "The redirect could not be followed."
            : "The outgoing INVITE failed.";
        CleanupTerminalState();
        SipInviteEvent failed = QueueEvent(response, reason);
        return CopyRequiredEvent(failed);
    }

    SipInviteEvent ProcessCancelResponse(SipMessage response)
    {
        if (cancelKey == null
            || !SipTransactionMatching.MatchResponse(cancelKey, response, true))
        {
            throw new System.ArgumentException(
                "The response does not match the active CANCEL.", "message");
        }
        SipInviteEvent value = QueueEvent(response,
            response.StatusCode < 200
                ? "Provisional CANCEL response received."
                : "Final CANCEL response received; the INVITE result is still pending.");
        return CopyRequiredEvent(value);
    }

    SipInviteEvent ProcessPrackResponse(SipMessage response)
    {
        if (prackKey == null
            || !SipTransactionMatching.MatchResponse(prackKey, response, true))
        {
            throw new System.ArgumentException(
                "The response does not match the active PRACK.", "message");
        }
        if (response.StatusCode < 200)
        {
            SipInviteEvent provisional = QueueEvent(response,
                "Provisional PRACK response received.");
            return CopyRequiredEvent(provisional);
        }
        ClearPrackTimer();
        if (response.StatusCode < 300)
        {
            SipInviteEvent accepted = QueueEvent(response,
                "The reliable provisional response was acknowledged.");
            return CopyRequiredEvent(accepted);
        }
        cancellationPending = true;
        state = SipInviteSessionState.Terminating;
        SipInviteEvent failed = QueueEvent(response,
            "PRACK failed; cancellation of the INVITE was started.");
        SendPendingCancel();
        return CopyRequiredEvent(failed);
    }

    SipInviteEvent ProcessIncomingPrack(SipMessage message)
    {
        SipTransactionKey receivedKey = SipTransactionMatching.CreateKey(
            message, SipTransactionRole.Server);
        SipExtensionHeaderValue? rack = ReadRAck(message);
        if (rack == null || invite == null || invite.CSeq == null
            || inviteKey == null || receivedKey.CallId != inviteKey.CallId
            || ReadTag(message.From) != ReadTag(invite.From)
            || ReadTag(message.To) != localTag
            || rack.Number != pendingLocalRSeq
            || rack.SecondNumber != invite.CSeq.SequenceNumber
            || rack.Method != SipMethod.Invite)
        {
            EnsureLocalTag();
            SipMessage missing = SipTransactionMessageFactory.CreateRequestResponse(
                message, SipStatusCodes.CallOrTransactionDoesNotExist,
                SipReasonPhrases.CallOrTransactionDoesNotExist, localTag);
            ApplyLocalSessionIdentity(missing);
            SipInviteEvent invalid = QueueEvent(message,
                "PRACK did not match a reliable provisional response.");
            QueueEvent(missing, "Automatic 481 response generated for PRACK.");
            return CopyRequiredEvent(invalid);
        }
        if (receivedPrackKey != null
            && SipTransactionMatching.MatchRequest(
                receivedPrackKey, message, localTag, true))
        {
            SipInviteEvent repeated = QueueEvent(message,
                "A PRACK retransmission was received.");
            if (receivedPrackResponse != null)
            {
                QueueEvent(receivedPrackResponse,
                    "The successful PRACK response was automatically replayed.");
            }
            return CopyRequiredEvent(repeated);
        }
        if (reliableProvisionalResponse == null)
        {
            EnsureLocalTag();
            SipMessage missing = SipTransactionMessageFactory.CreateRequestResponse(
                message, SipStatusCodes.CallOrTransactionDoesNotExist,
                SipReasonPhrases.CallOrTransactionDoesNotExist, localTag);
            ApplyLocalSessionIdentity(missing);
            SipInviteEvent stale = QueueEvent(message,
                "PRACK arrived without a pending reliable response.");
            QueueEvent(missing, "Automatic 481 response generated for PRACK.");
            return CopyRequiredEvent(stale);
        }
        if (!ApplyPrackAnswer(message))
        {
            EnsureLocalTag();
            SipMessage rejected = SipTransactionMessageFactory.CreateRequestResponse(
                message, SipStatusCodes.NotAcceptableHere,
                SipReasonPhrases.NotAcceptableHere, localTag);
            ApplyLocalSessionIdentity(rejected);
            SipInviteEvent invalidAnswer = QueueEvent(message,
                "PRACK did not contain the required offer answer.");
            QueueEvent(rejected, "Automatic 488 response generated for PRACK.");
            return CopyRequiredEvent(invalidAnswer);
        }
        EnsureLocalTag();
        SipMessage ok = SipTransactionMessageFactory.CreateRequestResponse(
            message, SipStatusCodes.Ok, SipReasonPhrases.Ok, localTag);
        ApplyLocalSessionIdentity(ok);
        receivedPrackKey = receivedKey;
        receivedPrackResponse = CopyRequiredMessage(ok);
        ClearReliableProvisionalTimer();
        SipInviteEvent received = QueueEvent(message,
            "PRACK acknowledged the reliable provisional response.");
        QueueEvent(ok, "Automatic 200 response generated for PRACK.");
        return CopyRequiredEvent(received);
    }

    SipInviteEvent ProcessByeResponse(SipMessage response)
    {
        if (byeKey == null || dialog == null || dialog.Id == null
            || !SipTransactionMatching.MatchResponse(byeKey, response, true))
        {
            throw new System.ArgumentException(
                "The response does not match the active BYE.", "message");
        }
        if (response.StatusCode < 200)
        {
            SipInviteEvent provisional = QueueEvent(response,
                "Provisional BYE response received.");
            return CopyRequiredEvent(provisional);
        }
        SipDialogSnapshot terminated = dialogs.UpdateDialog(
            dialog.Id, response);
        dialog = terminated;
        state = SipInviteSessionState.Terminated;
        CleanupTerminalState();
        SipInviteEvent completed = QueueEvent(response,
            response.StatusCode < 300
                ? "The BYE completed the INVITE usage."
                : "The INVITE usage ended after the BYE failed.");
        return CopyRequiredEvent(completed);
    }

    void RequestTermination(SipCancellation cancellation)
    {
        if (state == SipInviteSessionState.Confirmed)
        {
            cancellationPending = true;
            state = SipInviteSessionState.Terminating;
            SendBye(ReadCancellationReason(cancellation));
            return;
        }
        if (state == SipInviteSessionState.Terminating)
        {
            return;
        }
        cancellationPending = true;
        state = SipInviteSessionState.Terminating;
        QueueEvent(null, ReadCancellationReason(cancellation));
        SendPendingCancel();
    }

    void RequestIncomingTermination(SipCancellation cancellation)
    {
        if (acceptedInvite)
        {
            if (state == SipInviteSessionState.Terminating)
            {
                return;
            }
            state = SipInviteSessionState.Terminating;
            SendBye(ReadCancellationReason(cancellation));
            return;
        }
        if (invite == null)
        {
            throw new System.InvalidOperationException(
                "The incoming INVITE request is unavailable.");
        }
        EnsureLocalTag();
        SipMessage terminated = SipTransactionMessageFactory.CreateInviteTerminated(
            invite, localTag);
        ApplyLocalSessionIdentity(terminated);
        ClearReliableProvisionalTimer();
        TerminateEarlyDialog(terminated);
        FailIncomingOfferAnswer();
        state = SipInviteSessionState.Terminated;
        awaitingAck = true;
        lastInviteResponse = CopyRequiredMessage(terminated);
        QueueEvent(terminated, ReadCancellationReason(cancellation));
    }

    void SendPendingCancel()
    {
        if (!cancellationPending || cancelSent || !provisionalReceived
            || invite == null)
        {
            return;
        }
        SipMessage cancel = SipTransactionMessageFactory.CreateCancel(invite);
        CopyInitialSessionIdentity(cancel);
        cancelKey = SipTransactionMatching.CreateKey(
            cancel, SipTransactionRole.Client);
        cancelSent = true;
        QueueEvent(cancel, "CANCEL generated for the pending INVITE.");
    }

    SipInviteEvent SendPrackAnswer(SipInviteRequest command)
    {
        SipOfferAnswer? supplied = command.OfferAnswer;
        if (!awaitingPrackAnswer || supplied == null
            || supplied.LocalOffer == null || supplied.RemoteOffer == null
            || supplied.LocalAnswer == null || supplied.RemoteAnswer == null
            || supplied.LocalAnswer.Length == 0
            || supplied.LocalOffer.Length > 0 || supplied.RemoteOffer.Length > 0
            || supplied.RemoteAnswer.Length > 0)
        {
            throw new System.ArgumentException(
                "A non-empty local answer is required for PRACK.", "request");
        }
        SipOfferAnswer answer = new SipOfferAnswer();
        answer.LocalAnswer = CopyBytes(supplied.LocalAnswer);
        answer.ContentType = supplied.ContentType;
        SipOfferAnswer completed = offerAnswers.ApplyOfferAnswer(answer);
        awaitingPrackAnswer = false;
        SipMessage prack = CreatePrack(
            answer.LocalAnswer, completed.ContentType);
        SipInviteEvent generated = QueueEvent(prack,
            "PRACK with the local offer answer was generated.");
        return CopyRequiredEvent(generated);
    }

    void BeginRemoteReliableProvisional(
        SipMessage response, int reliableSequence)
    {
        if (invite == null || invite.CSeq == null)
        {
            throw new System.InvalidOperationException(
                "The reliable provisional response lacks INVITE state.");
        }
        lastRemoteRSeq = reliableSequence;
        pendingRemoteRSeq = reliableSequence;
        pendingRemoteInviteCSeq = invite.CSeq.SequenceNumber;
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        if (current.State == SipOfferAnswerState.RemoteOfferPending)
        {
            awaitingPrackAnswer = true;
            return;
        }
        SipMessage prack = CreatePrack(new byte[0], null);
        QueueEvent(prack,
            "PRACK was automatically generated for the reliable response.");
    }

    SipMessage CreatePrack(byte[] body, SipMediaType? contentType)
    {
        if (dialog == null || dialog.Id == null || pendingRemoteRSeq <= 0
            || pendingRemoteInviteCSeq < 0)
        {
            throw new System.InvalidOperationException(
                "A reliable early dialog is required to create PRACK.");
        }
        SipMessage prack = dialogs.CreateRequest(dialog.Id, SipMethods.Prack);
        ApplyLocalSessionIdentity(prack);
        AddHeader(prack, SipHeaderNames.RAck,
            pendingRemoteRSeq.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + pendingRemoteInviteCSeq.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + SipMethods.Invite);
        if (body.Length > 0)
        {
            if (contentType == null)
            {
                throw new System.ArgumentException(
                    "A PRACK answer requires a content type.", "request");
            }
            prack.Body = CopyBytes(body);
            prack.ContentType = CopyMediaType(contentType);
            SetOrAddHeaderValue(prack, SipHeaderNames.ContentType,
                FormatMediaType(contentType));
            SetOrAddHeaderValue(prack, SipHeaderNames.ContentLength,
                body.Length.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
        }
        prack.OriginalBytes = new byte[0];
        prackRequest = CopyRequiredMessage(prack);
        prackKey = SipTransactionMatching.CreateKey(
            prack, SipTransactionRole.Client);
        prackRetransmitInterval = SipDefaults.T1Milliseconds;
        prackRetransmitAt = nowMilliseconds + prackRetransmitInterval;
        prackTimeoutAt = nowMilliseconds
            + 64L * SipDefaults.T1Milliseconds;
        prackReliableTransport = invite != null
            && MessageUsesReliableTransport(invite);
        return prack;
    }

    void PrepareOutgoingReliableProvisional(
        SipMessage response, SipInviteOptions commandOptions)
    {
        if (response.StatusCode <= SipStatusCodes.Trying)
        {
            return;
        }
        bool responseRequires = MessageHasOptionTag(
            response, SipHeaderNames.Require,
            SipOptionTags.ReliableProvisional);
        int supplied = ReadPositiveHeaderInteger(response, SipHeaderNames.RSeq);
        bool requested = peerRequiresReliableProvisional
            || commandOptions.ReliableProvisional || responseRequires
            || supplied > 0;
        if (!requested)
        {
            return;
        }
        if (!peerSupportsReliableProvisional)
        {
            throw new SipProtocolException(
                "The peer did not negotiate reliable provisional responses.",
                SipStatusCodes.BadExtension, SipMethods.Invite);
        }
        if (reliableProvisionalResponse != null)
        {
            throw new System.InvalidOperationException(
                "A reliable provisional response is already awaiting PRACK.");
        }
        AddOptionTag(response, SipHeaderNames.Require,
            SipOptionTags.ReliableProvisional);
        int sequence = supplied > 0 ? supplied : nextLocalRSeq;
        if (nextLocalRSeq == 0
            || pendingLocalRSeq > 0 && sequence != nextLocalRSeq)
        {
            throw new System.ArgumentException(
                "RSeq must increase for each reliable provisional response.",
                "request");
        }
        SetOrAddHeaderValue(response, SipHeaderNames.RSeq,
            sequence.ToString(System.Globalization.CultureInfo.InvariantCulture));
        pendingLocalRSeq = sequence;
        nextLocalRSeq = sequence == int.MaxValue ? 0 : sequence + 1;
        reliableProvisionalResponse = CopyRequiredMessage(response);
        reliableRetransmitInterval = SipDefaults.T1Milliseconds;
        reliableRetransmitAt = nowMilliseconds + reliableRetransmitInterval;
        reliableTimeoutAt = nowMilliseconds
            + 64L * SipDefaults.T1Milliseconds;
    }

    void ApplyOutgoingReliableProvisionalPolicy(SipMessage outgoing)
    {
        if (options.ReliableProvisional)
        {
            AddOptionTag(outgoing, SipHeaderNames.Supported,
                SipOptionTags.ReliableProvisional);
        }
        peerSupportsReliableProvisional = MessageHasOptionTag(
            outgoing, SipHeaderNames.Supported,
            SipOptionTags.ReliableProvisional)
            || MessageHasOptionTag(outgoing, SipHeaderNames.Require,
                SipOptionTags.ReliableProvisional);
        peerRequiresReliableProvisional = MessageHasOptionTag(
            outgoing, SipHeaderNames.Require,
            SipOptionTags.ReliableProvisional);
    }

    void SendBye(string reason)
    {
        if (dialog == null || dialog.Id == null)
        {
            throw new System.InvalidOperationException(
                "A confirmed dialog is required to send BYE.");
        }
        SipMessage bye = dialogs.CreateRequest(dialog.Id, SipMethods.Bye);
        ApplyLocalSessionIdentity(bye);
        byeKey = SipTransactionMatching.CreateKey(
            bye, SipTransactionRole.Client);
        QueueEvent(bye, reason);
    }

    void AdvanceReliableProvisionalTimer()
    {
        if (reliableProvisionalResponse == null || reliableTimeoutAt <= 0)
        {
            return;
        }
        if (nowMilliseconds >= reliableTimeoutAt)
        {
            SipMessage timeout = CreateReliableProvisionalTimeoutResponse();
            ClearReliableProvisionalTimer();
            TerminateEarlyDialog(timeout);
            FailIncomingOfferAnswer();
            state = SipInviteSessionState.Terminated;
            awaitingAck = true;
            lastInviteResponse = CopyRequiredMessage(timeout);
            QueueEvent(timeout,
                "Reliable provisional response timed out waiting for PRACK.");
            return;
        }
        while (reliableProvisionalResponse != null
            && reliableRetransmitAt > 0
            && reliableRetransmitAt <= nowMilliseconds
            && reliableRetransmitAt < reliableTimeoutAt)
        {
            QueueEvent(reliableProvisionalResponse,
                "Reliable provisional response retransmission timer fired.");
            reliableRetransmitInterval = reliableRetransmitInterval
                >= SipDefaults.T2Milliseconds
                    ? SipDefaults.T2Milliseconds
                    : reliableRetransmitInterval * 2;
            reliableRetransmitAt = reliableRetransmitAt
                + reliableRetransmitInterval;
        }
    }

    void AdvancePrackTimer()
    {
        if (prackRequest == null || prackTimeoutAt <= 0)
        {
            return;
        }
        if (nowMilliseconds >= prackTimeoutAt)
        {
            ClearPrackTimer();
            cancellationPending = true;
            state = SipInviteSessionState.Terminating;
            QueueEvent(null,
                "PRACK timed out; cancellation of the INVITE was started.");
            SendPendingCancel();
            return;
        }
        if (prackReliableTransport)
        {
            return;
        }
        while (prackRequest != null && prackRetransmitAt > 0
            && prackRetransmitAt <= nowMilliseconds
            && prackRetransmitAt < prackTimeoutAt)
        {
            QueueEvent(prackRequest,
                "PRACK retransmission timer fired.");
            prackRetransmitInterval = prackRetransmitInterval
                >= SipDefaults.T2Milliseconds
                    ? SipDefaults.T2Milliseconds
                    : prackRetransmitInterval * 2;
            prackRetransmitAt = prackRetransmitAt
                + prackRetransmitInterval;
        }
    }

    SipMessage CreateReliableProvisionalTimeoutResponse()
    {
        if (invite == null)
        {
            throw new System.InvalidOperationException(
                "The incoming INVITE is unavailable.");
        }
        EnsureLocalTag();
        return SipTransactionMessageFactory.CreateRequestResponse(
            invite, SipStatusCodes.ServerTimeout,
            SipReasonPhrases.ServerTimeout, localTag);
    }

    void ClearReliableProvisionalTimer()
    {
        reliableProvisionalResponse = null;
        reliableRetransmitAt = 0;
        reliableTimeoutAt = 0;
        reliableRetransmitInterval = 0;
    }

    void ClearPrackTimer()
    {
        prackRetransmitAt = 0;
        prackTimeoutAt = 0;
        prackRetransmitInterval = 0;
    }

    bool FollowRedirect(SipMessage response)
    {
        if (cancellationPending || redirectCount >= options.MaxRedirectCount)
        {
            return false;
        }
        SipUri? target = SelectRedirectTarget(response);
        if (target == null || invite == null || invite.CSeq == null
            || invite.ViaValues.Length == 0)
        {
            return false;
        }
        SipMessage redirected = CopyRequiredMessage(invite);
        redirected.RequestUri = target;
        if (redirected.CSeq == null || redirected.CSeq.SequenceNumber == int.MaxValue
            || redirected.ViaValues.Length == 0)
        {
            return false;
        }
        redirected.CSeq.SequenceNumber = redirected.CSeq.SequenceNumber + 1;
        SetHeaderValue(redirected.Headers, SipHeaderNames.CSeq,
            redirected.CSeq.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + SipMethods.Invite);
        SetTopViaBranch(redirected, tokens.CreateBranch());
        connectedIdentity = null;
        pendingConnectedIdentity = null;
        remoteSessionId = null;
        remoteCapabilities = new SipCapabilitySet();
        remoteFeatureCapabilities = new SipExtensionHeaderValue[0];
        peerSupportsFromChange = false;
        peerSupportsEarlyDialogTermination = false;
        ApplyLocalSessionIdentity(redirected);
        redirected.OriginalBytes = new byte[0];
        invite = redirected;
        inviteKey = SipTransactionMatching.CreateKey(
            redirected, SipTransactionRole.Client);
        cancelKey = null;
        redirectCount = redirectCount + 1;
        provisionalReceived = false;
        cancelSent = false;
        state = cancellationPending
            ? SipInviteSessionState.Terminating : SipInviteSessionState.Calling;
        return true;
    }

    SipUri? SelectRedirectTarget(SipMessage response)
    {
        SipUri? selected = null;
        int selectedQuality = -1;
        int index = 0;
        while (index < response.ContactValues.Length)
        {
            SipContactValue contact = response.ContactValues[index];
            SipUri? candidate = contact.Address == null
                ? null : contact.Address.Uri;
            int quality = contact.QualityThousandths < 0
                ? 1000 : contact.QualityThousandths;
            bool secureDowngrade = invite != null && invite.RequestUri != null
                && invite.RequestUri.Scheme == SipUriScheme.Sips
                && candidate != null && candidate.Scheme != SipUriScheme.Sips;
            if (!contact.Wildcard && contact.ExpiresSeconds != 0
                && candidate != null
                && (candidate.Scheme == SipUriScheme.Sip
                    || candidate.Scheme == SipUriScheme.Sips)
                && !secureDowngrade && quality > selectedQuality
                && SipUriValidator.ValidateUriModel(candidate).Length == 0)
            {
                selected = SipUriModelCopy.CreateNormalizedCopy(candidate);
                selectedQuality = quality;
            }
            index = index + 1;
        }
        return selected;
    }

    void ApplyRemoteBody(SipMessage response)
    {
        if (response.Body.Length == 0)
        {
            return;
        }
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        SipOfferAnswer exchange = new SipOfferAnswer();
        exchange.ContentType = response.ContentType;
        if (current.State == SipOfferAnswerState.LocalOfferPending
            || current.State == SipOfferAnswerState.Completed)
        {
            exchange.RemoteAnswer = CopyBytes(response.Body);
            offerAnswers.ApplyOfferAnswer(exchange);
        }
        else if (current.State == SipOfferAnswerState.RemoteOfferRequired
            || current.State == SipOfferAnswerState.RemoteOfferPending)
        {
            exchange.RemoteOffer = CopyBytes(response.Body);
            offerAnswers.ApplyRemoteOffer(exchange);
        }
        else if (current.State == SipOfferAnswerState.Stable)
        {
            exchange.RemoteOffer = CopyBytes(response.Body);
            offerAnswers.ApplyRemoteOffer(exchange);
        }
        else
        {
            throw new SipProtocolException(
                "The response body conflicts with the offer/answer state.",
                SipStatusCodes.BadRequest, SipMethods.Invite);
        }
    }

    void InitializeIncomingOfferAnswer(SipMessage incoming)
    {
        if (incoming.Body.Length == 0)
        {
            return;
        }
        SipOfferAnswer exchange = new SipOfferAnswer();
        exchange.RemoteOffer = CopyBytes(incoming.Body);
        exchange.ContentType = incoming.ContentType;
        offerAnswers.ApplyRemoteOffer(exchange);
    }

    void ApplyOutgoingSessionTimerPolicy(SipMessage outgoing)
    {
        if (!options.SessionTimers)
        {
            return;
        }
        ValidateSessionTimerOptions(options);
        AddOptionTag(outgoing, SipHeaderNames.Supported, SipOptionTags.Timer);
        SipExtensionHeaderValue? suppliedMinimum = ReadSessionTimerValue(
            outgoing, SipHeaderNames.MinSE);
        SipExtensionHeaderValue? suppliedInterval = ReadSessionTimerValue(
            outgoing, SipHeaderNames.SessionExpires);
        int minimum = options.MinSessionExpiresSeconds;
        if (suppliedMinimum != null && suppliedMinimum.Number > minimum)
        {
            minimum = suppliedMinimum.Number;
        }
        int interval = suppliedInterval == null
            ? options.SessionExpiresSeconds : suppliedInterval.Number;
        if (minimum < 90 || interval < minimum)
        {
            throw new System.ArgumentException(
                "The session interval must be at least the effective Min-SE.",
                "request");
        }
        SetOrAddHeaderValue(outgoing, SipHeaderNames.MinSE,
            FormatMinimumSessionInterval(suppliedMinimum, minimum));
        SetOrAddHeaderValue(outgoing, SipHeaderNames.SessionExpires,
            FormatSessionInterval(suppliedInterval, interval,
                suppliedInterval == null
                    ? null : suppliedInterval.SecondaryToken));
        sessionMinimumSeconds = minimum;
    }

    void ValidateSessionTimerOptions(SipInviteOptions value)
    {
        if (value.SessionTimers
            && (value.MinSessionExpiresSeconds < 90
                || value.SessionExpiresSeconds
                    < value.MinSessionExpiresSeconds))
        {
            throw new System.ArgumentException(
                "Session timer intervals must be at least 90 seconds.",
                "request");
        }
    }

    void CaptureSessionTimerRequest(SipMessage request)
    {
        SipExtensionHeaderValue? minimum = ReadSessionTimerValue(
            request, SipHeaderNames.MinSE);
        if (minimum != null)
        {
            if (minimum.Number < 90)
            {
                throw new SipProtocolException(
                    "Min-SE cannot be less than 90 seconds.",
                    SipStatusCodes.BadRequest, request.MethodToken);
            }
            if (minimum.Number > sessionMinimumSeconds)
            {
                sessionMinimumSeconds = minimum.Number;
            }
        }
        if (MessageHasMethodToken(request, SipMethods.Update))
        {
            peerAllowsUpdate = true;
        }
    }

    SipMessage PrepareIncomingSessionTimerResponse(
        SipMessage response, SipMessage request, SipInviteOptions policy)
    {
        if (response.StatusCode < 200 || response.StatusCode >= 300)
        {
            return response;
        }
        options = CopyOptions(policy);
        if (!policy.SessionTimers)
        {
            if (!MessageHasOptionTag(
                request, SipHeaderNames.Require, SipOptionTags.Timer))
            {
                return response;
            }
            string? responseTag = ReadTag(response.To);
            SipMessage unsupported =
                SipTransactionMessageFactory.CreateRequestResponse(
                    request, SipStatusCodes.BadExtension,
                    SipReasonPhrases.BadExtension, responseTag);
            SetOrAddHeaderValue(unsupported,
                SipHeaderNames.Unsupported, SipOptionTags.Timer);
            return unsupported;
        }
        ValidateSessionTimerOptions(policy);
        bool peerSupports = MessageHasOptionTag(
            request, SipHeaderNames.Supported, SipOptionTags.Timer);
        SipExtensionHeaderValue? requested = ReadSessionTimerValue(
            request, SipHeaderNames.SessionExpires);
        SipExtensionHeaderValue? requestedMinimum = ReadSessionTimerValue(
            request, SipHeaderNames.MinSE);
        int minimum = policy.MinSessionExpiresSeconds;
        if (sessionTimerActive && sessionMinimumSeconds > minimum)
        {
            minimum = sessionMinimumSeconds;
        }
        if (requestedMinimum != null && requestedMinimum.Number > minimum)
        {
            minimum = requestedMinimum.Number;
        }
        if (requested != null && requested.Number < minimum && peerSupports)
        {
            string? responseTag = ReadTag(response.To);
            SipMessage tooSmall = SipTransactionMessageFactory.CreateRequestResponse(
                request, SipStatusCodes.SessionIntervalTooSmall,
                SipReasonPhrases.SessionIntervalTooSmall, responseTag);
            SetOrAddHeaderValue(tooSmall, SipHeaderNames.MinSE,
                FormatMinimumSessionInterval(requestedMinimum, minimum));
            sessionMinimumSeconds = minimum;
            return tooSmall;
        }
        SipExtensionHeaderValue? selected = ReadSessionTimerValue(
            response, SipHeaderNames.SessionExpires);
        if (requested == null && selected == null && !peerSupports)
        {
            return response;
        }
        int interval = selected != null
            ? selected.Number : requested != null
                ? requested.Number : sessionTimerActive
                    ? sessionIntervalSeconds : policy.SessionExpiresSeconds;
        if (interval < minimum
            || requested != null && interval > requested.Number)
        {
            throw new System.ArgumentException(
                "The response session interval is outside the allowed range.",
                "request");
        }
        string? requestedRefresher = requested == null
            ? null : requested.SecondaryToken;
        string? selectedRefresher = selected == null
            ? null : selected.SecondaryToken;
        if (requestedRefresher != null && selectedRefresher != null
            && !SipAscii.EqualsIgnoreCase(
                requestedRefresher, selectedRefresher))
        {
            throw new System.ArgumentException(
                "The response cannot change the requested refresher role.",
                "request");
        }
        string refresher = selectedRefresher != null
            ? selectedRefresher : requestedRefresher != null
                ? requestedRefresher : "uas";
        if (!peerSupports && !SipAscii.EqualsIgnoreCase(refresher, "uas"))
        {
            throw new System.ArgumentException(
                "An unsupported peer cannot be selected as refresher.",
                "request");
        }
        SipExtensionHeaderValue? basis = selected == null
            ? requested : selected;
        SetOrAddHeaderValue(response, SipHeaderNames.SessionExpires,
            FormatSessionInterval(basis, interval, refresher));
        if (peerSupports)
        {
            AddOptionTag(response, SipHeaderNames.Require, SipOptionTags.Timer);
        }
        sessionMinimumSeconds = minimum;
        return response;
    }

    void ConfigureSessionTimerFromSuccess(
        SipMessage response, SipMessage request, bool initialUac)
    {
        if (!options.SessionTimers)
        {
            StopSessionTimer();
            return;
        }
        if (MessageHasMethodToken(response, SipMethods.Update))
        {
            peerAllowsUpdate = true;
        }
        SipExtensionHeaderValue? selected = ReadSessionTimerValue(
            response, SipHeaderNames.SessionExpires);
        SipExtensionHeaderValue? requested = ReadSessionTimerValue(
            request, SipHeaderNames.SessionExpires);
        string? refresher = selected == null
            ? null : selected.SecondaryToken;
        if (selected == null && MessageHasOptionTag(
            response, SipHeaderNames.Require, SipOptionTags.Timer))
        {
            throw new SipProtocolException(
                "A response requiring timer must contain Session-Expires.",
                SipStatusCodes.BadRequest,
                response.CSeq == null ? null : response.CSeq.MethodToken);
        }
        if (selected != null
            && (selected.Number < sessionMinimumSeconds
                || requested != null
                    && selected.Number > requested.Number))
        {
            throw new SipProtocolException(
                "The negotiated session interval is outside the requested range.",
                SipStatusCodes.BadRequest,
                response.CSeq == null ? null : response.CSeq.MethodToken);
        }
        if (selected == null && initialUac && options.SessionTimers
            && !MessageHasOptionTag(
                response, SipHeaderNames.Require, SipOptionTags.Timer))
        {
            selected = requested;
            if (selected != null)
            {
                refresher = "uac";
            }
        }
        if (selected == null)
        {
            StopSessionTimer();
            return;
        }
        if (selected.Number < 90 || refresher == null)
        {
            throw new SipProtocolException(
                "A negotiated session timer requires an interval of at least "
                    + "90 seconds and a refresher role.",
                SipStatusCodes.BadRequest,
                response.CSeq == null ? null : response.CSeq.MethodToken);
        }
        bool local = role == SipDialogRole.Uac
            && SipAscii.EqualsIgnoreCase(refresher, "uac")
            || role == SipDialogRole.Uas
                && SipAscii.EqualsIgnoreCase(refresher, "uas");
        sessionTimerActive = true;
        localSessionRefresher = local;
        sessionIntervalSeconds = selected.Number;
        sessionTimerSource = CopyRequiredMessage(response);
        sessionExpiresAt = AddSessionTimerDelay(selected.Number, 1000);
        sessionRefreshAt = local
            ? AddSessionTimerDelay(selected.Number, 500) : 0;
        suspendedSessionRefreshAt = 0;
    }

    void ApplyLocalSessionTimerHeaders(
        SipMessage generated, SipMessage template)
    {
        if (!options.SessionTimers)
        {
            return;
        }
        if (role == SipDialogRole.Uac && invite != null)
        {
            CopySessionOptionHeaders(invite, generated);
        }
        CopySessionOptionHeaders(template, generated);
        AddOptionTag(generated, SipHeaderNames.Supported, SipOptionTags.Timer);
        SipExtensionHeaderValue? supplied = ReadSessionTimerValue(
            template, SipHeaderNames.SessionExpires);
        if (supplied == null && sessionTimerSource != null)
        {
            supplied = ReadSessionTimerValue(
                sessionTimerSource, SipHeaderNames.SessionExpires);
        }
        if (supplied == null && invite != null)
        {
            supplied = ReadSessionTimerValue(
                invite, SipHeaderNames.SessionExpires);
        }
        if (sessionTimerActive)
        {
            int interval = sessionIntervalSeconds > sessionMinimumSeconds
                ? sessionIntervalSeconds : sessionMinimumSeconds;
            string refresher = role == SipDialogRole.Uac ? "uac" : "uas";
            SetOrAddHeaderValue(generated, SipHeaderNames.SessionExpires,
                FormatSessionInterval(supplied, interval, refresher));
            SipExtensionHeaderValue? minimum = ReadSessionTimerValue(
                template, SipHeaderNames.MinSE);
            SetOrAddHeaderValue(generated, SipHeaderNames.MinSE,
                FormatMinimumSessionInterval(
                    minimum, sessionMinimumSeconds));
        }
        else if (supplied != null)
        {
            SetOrAddHeaderValue(generated, SipHeaderNames.SessionExpires,
                FormatSessionInterval(
                    supplied, supplied.Number, supplied.SecondaryToken));
        }
    }

    SipInviteEvent? RetryInitialSessionTimer(SipMessage response)
    {
        if (response.StatusCode != SipStatusCodes.SessionIntervalTooSmall
            || !options.SessionTimers || invite == null
            || invite.CSeq == null || invite.ViaValues.Length == 0)
        {
            return null;
        }
        SipExtensionHeaderValue minimum = ReadRequiredSessionMinimum(response);
        SipExtensionHeaderValue? current = ReadSessionTimerValue(
            invite, SipHeaderNames.SessionExpires);
        int currentInterval = current == null
            ? options.SessionExpiresSeconds : current.Number;
        if (minimum.Number <= currentInterval)
        {
            return null;
        }
        SipMessage previous = invite;
        SipMessage ack = SipTransactionMessageFactory.CreateFailureAck(
            previous, response);
        ApplyLocalSessionIdentity(ack);
        SipMessage retry = CopyRequiredMessage(previous);
        if (retry.CSeq == null || retry.CSeq.SequenceNumber == int.MaxValue)
        {
            throw new System.InvalidOperationException(
                "The session timer retry CSeq is exhausted.");
        }
        retry.CSeq.SequenceNumber = retry.CSeq.SequenceNumber + 1;
        SetHeaderValue(retry.Headers, SipHeaderNames.CSeq,
            retry.CSeq.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + SipMethods.Invite);
        SetTopViaBranch(retry, tokens.CreateBranch());
        sessionMinimumSeconds = minimum.Number;
        SetOrAddHeaderValue(retry, SipHeaderNames.MinSE,
            FormatMinimumSessionInterval(minimum, minimum.Number));
        SetOrAddHeaderValue(retry, SipHeaderNames.SessionExpires,
            FormatSessionInterval(current, minimum.Number,
                current == null ? null : current.SecondaryToken));
        retry.OriginalBytes = new byte[0];
        invite = retry;
        inviteKey = SipTransactionMatching.CreateKey(
            retry, SipTransactionRole.Client);
        state = SipInviteSessionState.Calling;
        SipInviteEvent received = QueueEvent(response,
            "The 422 response increased Min-SE and the INVITE will be retried.");
        QueueEvent(ack, "ACK generated for the 422 INVITE response.");
        QueueEvent(retry,
            "The INVITE was retried with the larger session interval.");
        return received;
    }

    SipInviteEvent? RetryLocalSessionTimer(
        SipMessage request, SipMessage response)
    {
        if (response.StatusCode != SipStatusCodes.SessionIntervalTooSmall
            || !options.SessionTimers || dialog == null || dialog.Id == null)
        {
            return null;
        }
        SipExtensionHeaderValue minimum = ReadRequiredSessionMinimum(response);
        SipExtensionHeaderValue? current = ReadSessionTimerValue(
            request, SipHeaderNames.SessionExpires);
        int currentInterval = current == null
            ? sessionIntervalSeconds : current.Number;
        if (minimum.Number <= currentInterval)
        {
            return null;
        }
        dialog = dialogs.UpdateDialog(dialog.Id, response);
        SipInviteEvent received = QueueEvent(response,
            "The 422 response increased Min-SE and the refresh will be retried.");
        if (request.Method == SipMethod.Invite)
        {
            SipMessage ack = SipTransactionMessageFactory.CreateFailureAck(
                request, response);
            ApplyLocalSessionIdentity(ack);
            QueueEvent(ack, "ACK generated for the 422 re-INVITE response.");
        }
        sessionMinimumSeconds = minimum.Number;
        SipUri? target = ReadRefreshContact(request);
        string method = request.Method == SipMethod.Invite
            ? SipMethods.Invite : SipMethods.Update;
        SipDialogId id = CopyRequiredDialogId(dialog.Id);
        SipMessage retry = target == null
            ? dialogs.CreateRequest(id, method)
            : dialogs.CreateTargetRefreshRequest(
                id, method, target);
        ApplyLocalSessionIdentity(retry);
        CopyRefreshBody(request, retry);
        ApplyLocalSessionTimerHeaders(retry, request);
        SetOrAddHeaderValue(retry, SipHeaderNames.MinSE,
            FormatMinimumSessionInterval(minimum, minimum.Number));
        SetOrAddHeaderValue(retry, SipHeaderNames.SessionExpires,
            FormatSessionInterval(current, minimum.Number,
                current == null ? null : current.SecondaryToken));
        localRefreshRequest = CopyRequiredMessage(retry);
        localRefreshKey = SipTransactionMatching.CreateKey(
            retry, SipTransactionRole.Client);
        QueueEvent(retry,
            "The dialog refresh was retried with the larger session interval.");
        return received;
    }

    SipExtensionHeaderValue ReadRequiredSessionMinimum(SipMessage response)
    {
        SipExtensionHeaderValue? minimum = ReadSessionTimerValue(
            response, SipHeaderNames.MinSE);
        if (minimum == null || minimum.Number < 90)
        {
            throw new SipProtocolException(
                "A 422 response requires Min-SE of at least 90 seconds.",
                SipStatusCodes.BadRequest,
                response.CSeq == null ? null : response.CSeq.MethodToken);
        }
        return minimum;
    }

    int ReadSessionRefreshRetryDelay(SipMessage response)
    {
        if (response.StatusCode != SipStatusCodes.RequestPending)
        {
            return 1;
        }
        int result = -1;
        int index = 0;
        while (index < response.Headers.Length)
        {
            SipHeader header = response.Headers[index];
            if (SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.RetryAfter))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    SipHeaderNames.RetryAfter, header.Value,
                    new SipParseOptions());
                if (result >= 0 || !parsed.Success
                    || parsed.RetryAfter == null
                    || parsed.RetryAfter.Seconds < 1)
                {
                    throw new SipProtocolException(
                        "A valid singular Retry-After value is required.",
                        SipStatusCodes.BadRequest,
                        response.CSeq == null
                            ? null : response.CSeq.MethodToken);
                }
                result = parsed.RetryAfter.Seconds;
            }
            index = index + 1;
        }
        return result < 1 ? 1 : result;
    }

    void AdvanceSessionTimer()
    {
        if (!sessionTimerActive || state != SipInviteSessionState.Confirmed)
        {
            return;
        }
        if (sessionExpiresAt > 0 && nowMilliseconds >= sessionExpiresAt)
        {
            state = SipInviteSessionState.Terminating;
            QueueEvent(null,
                "The negotiated session timer expired without a successful refresh.");
            StopSessionTimer();
            SendBye("BYE generated because the session timer expired.");
            return;
        }
        if (!localSessionRefresher || sessionRefreshAt <= 0
            || nowMilliseconds < sessionRefreshAt)
        {
            return;
        }
        if (localRefreshRequest != null || incomingRefreshRequest != null
            || awaitingAck || awaitingPrackAnswer)
        {
            sessionRefreshAt = AddSessionTimerDelay(1, 1000);
            return;
        }
        StartAutomaticSessionRefresh();
    }

    void StartAutomaticSessionRefresh()
    {
        if (dialog == null || dialog.Id == null)
        {
            throw new System.InvalidOperationException(
                "The session timer requires a confirmed dialog.");
        }
        string method = peerAllowsUpdate
            ? SipMethods.Update : SipMethods.Invite;
        SipMessage refresh = dialogs.CreateRequest(dialog.Id, method);
        ApplyLocalSessionIdentity(refresh);
        if (method == SipMethods.Invite)
        {
            ApplyAutomaticRefreshOffer(refresh);
        }
        ApplyLocalSessionTimerHeaders(refresh, refresh);
        localRefreshRequest = CopyRequiredMessage(refresh);
        localRefreshKey = SipTransactionMatching.CreateKey(
            refresh, SipTransactionRole.Client);
        automaticSessionRefresh = true;
        sessionRefreshAt = 0;
        suspendedSessionRefreshAt = 0;
        QueueEvent(refresh, method == SipMethods.Update
            ? "The session refresh timer generated UPDATE."
            : "The session refresh timer generated re-INVITE.");
    }

    void ApplyAutomaticRefreshOffer(SipMessage refresh)
    {
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        byte[] localBody = current.LocalOffer.Length > 0
            ? current.LocalOffer : current.LocalAnswer;
        if (localBody.Length == 0)
        {
            return;
        }
        refresh.Body = CopyBytes(localBody);
        refresh.ContentType = current.NegotiatedContentType == null
            ? current.ContentType : current.NegotiatedContentType;
        SetOrAddHeaderValue(refresh, SipHeaderNames.ContentLength,
            refresh.Body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        if (refresh.ContentType != null)
        {
            SetOrAddHeaderValue(refresh, SipHeaderNames.ContentType,
                FormatMediaType(refresh.ContentType));
        }
        SipOfferAnswer offer = new SipOfferAnswer();
        offer.LocalOffer = CopyBytes(localBody);
        offer.ContentType = refresh.ContentType;
        offerAnswers.ApplyLocalOffer(offer);
        refresh.OriginalBytes = new byte[0];
    }

    void StopSessionTimer()
    {
        sessionTimerActive = false;
        localSessionRefresher = false;
        automaticSessionRefresh = false;
        sessionIntervalSeconds = 0;
        sessionRefreshAt = 0;
        sessionExpiresAt = 0;
        suspendedSessionRefreshAt = 0;
        sessionTimerSource = null;
    }

    long AddSessionTimerDelay(int seconds, int multiplier)
    {
        long delay = (long)seconds * multiplier;
        return nowMilliseconds > long.MaxValue - delay
            ? long.MaxValue : nowMilliseconds + delay;
    }

    SipExtensionHeaderValue? ReadSessionTimerValue(
        SipMessage message, string name)
    {
        SipExtensionHeaderValue? result = null;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (HeaderNameMatches(header.Name, name))
            {
                if (result != null)
                {
                    throw new SipProtocolException(
                        "A session timer header cannot be repeated.",
                        SipStatusCodes.BadRequest, message.MethodToken);
                }
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        name, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.Values.Length != 1)
                {
                    throw new SipProtocolException(
                        "A valid session timer header is required.",
                        SipStatusCodes.BadRequest, message.MethodToken);
                }
                result = parsed.Values[0];
            }
            index = index + 1;
        }
        return result;
    }

    string FormatSessionInterval(
        SipExtensionHeaderValue? source, int seconds, string? refresher)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append(seconds.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        if (source != null)
        {
            int index = 0;
            while (index < source.Parameters.Length)
            {
                SipUriParameter parameter = source.Parameters[index];
                if (!SipAscii.EqualsIgnoreCase(parameter.Name, "refresher"))
                {
                    text.Append(SipHeaderPrimitives.FormatParameterValue(
                        parameter));
                }
                index = index + 1;
            }
        }
        if (refresher != null)
        {
            text.Append(";refresher=");
            text.Append(refresher);
        }
        return text.ToString();
    }

    string FormatMinimumSessionInterval(
        SipExtensionHeaderValue? source, int seconds)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append(seconds.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        if (source != null)
        {
            int index = 0;
            while (index < source.Parameters.Length)
            {
                text.Append(SipHeaderPrimitives.FormatParameterValue(
                    source.Parameters[index]));
                index = index + 1;
            }
        }
        return text.ToString();
    }

    void CopySessionOptionHeaders(SipMessage source, SipMessage destination)
    {
        CopyCombinedHeaderValue(
            source, destination, SipHeaderNames.Supported);
        CopyCombinedHeaderValue(
            source, destination, SipHeaderNames.Require);
        CopyCombinedHeaderValue(
            source, destination, SipHeaderNames.ProxyRequire);
    }

    void CopyCombinedHeaderValue(
        SipMessage source, SipMessage destination, string name)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < source.Headers.Length)
        {
            SipHeader header = source.Headers[index];
            if (HeaderNameMatches(header.Name, name)
                && header.Value != null && header.Value.Trim().Length > 0)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }
                text.Append(header.Value.Trim());
            }
            index = index + 1;
        }
        if (text.Length > 0)
        {
            SetOrAddHeaderValue(destination, name, text.ToString());
        }
    }

    bool MessageHasMethodToken(SipMessage message, string method)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.Allow))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    SipHeaderNames.Allow, header.Value, new SipParseOptions());
                int methodIndex = 0;
                while (parsed.Success && methodIndex < parsed.Methods.Length)
                {
                    if (SipAscii.EqualsIgnoreCase(
                        parsed.Methods[methodIndex], method))
                    {
                        return true;
                    }
                    methodIndex = methodIndex + 1;
                }
            }
            index = index + 1;
        }
        return false;
    }

    SipUri? ReadRefreshContact(SipMessage message)
    {
        if (message.ContactValues.Length == 0)
        {
            return null;
        }
        SipContactValue? contact = message.ContactValues.Length == 1
            ? message.ContactValues[0] : null;
        SipNameAddress? address = contact == null ? null : contact.Address;
        SipUri? uri = address == null ? null : address.Uri;
        if (contact == null || contact.Wildcard || uri == null)
        {
            throw new System.ArgumentException(
                "A target refresh requires at most one valid Contact URI.",
                "request");
        }
        return CopyTarget(uri, "request");
    }

    SipMessage CreateRefreshSuccessAck(SipMessage request)
    {
        if (dialog == null || dialog.Id == null || request.CSeq == null)
        {
            throw new System.InvalidOperationException(
                "The successful re-INVITE cannot be acknowledged.");
        }
        SipMessage ack = dialogs.CreateRequest(dialog.Id, SipMethods.Ack);
        ApplyLocalSessionIdentity(ack);
        if (ack.CSeq == null)
        {
            throw new System.InvalidOperationException(
                "The generated ACK lacks a CSeq.");
        }
        ack.CSeq.SequenceNumber = request.CSeq.SequenceNumber;
        SetHeaderValue(ack.Headers, SipHeaderNames.CSeq,
            request.CSeq.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + SipMethods.Ack);
        ack.OriginalBytes = new byte[0];
        return ack;
    }

    string DescribeRefreshFailure(SipMessage response)
    {
        if (response.StatusCode == SipStatusCodes.RequestPending)
        {
            string? retryAfter = ReadHeaderValue(
                response, SipHeaderNames.RetryAfter);
            return retryAfter == null
                ? "The local dialog refresh encountered glare."
                : "The local dialog refresh encountered glare; Retry-After "
                    + retryAfter + ".";
        }
        return "The local dialog refresh failed.";
    }

    string? ReadHeaderValue(SipMessage message, string name)
    {
        string? result = null;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                if (result != null)
                {
                    return null;
                }
                string value = header.Value == null ? "" : header.Value.Trim();
                if (value.Length == 0)
                {
                    return null;
                }
                result = value;
            }
            index = index + 1;
        }
        return result;
    }

    string DialogLocalTag()
    {
        string? tag = dialog == null || dialog.Id == null
            ? null : dialog.Id.LocalTag;
        if (tag == null || !SipAscii.IsToken(tag))
        {
            throw new System.InvalidOperationException(
                "The confirmed dialog lacks a valid local tag.");
        }
        return tag;
    }

    void ClearLocalRefresh()
    {
        localRefreshRequest = null;
        localRefreshKey = null;
        automaticSessionRefresh = false;
    }

    void ClearIncomingRefresh()
    {
        incomingRefreshRequest = null;
        incomingRefreshKey = null;
        lastRefreshResponse = null;
        incomingRefreshAccepted = false;
        incomingRefreshAwaitingAck = false;
    }

    void ApplyLocalRefreshOffer(
        SipInviteRequest command, SipMessage request)
    {
        if (request.Body.Length == 0)
        {
            if (command.OfferAnswer != null)
            {
                SipOfferAnswer offerless = new SipOfferAnswer();
                offerAnswers.ApplyLocalOffer(offerless);
            }
            return;
        }
        if (command.OfferAnswer != null
            && command.OfferAnswer.LocalOffer.Length > 0
            && !BytesEqual(command.OfferAnswer.LocalOffer, request.Body))
        {
            throw new System.ArgumentException(
                "The refresh offer must match the request body.", "request");
        }
        SipOfferAnswer offer = new SipOfferAnswer();
        offer.LocalOffer = CopyBytes(request.Body);
        offer.ContentType = command.OfferAnswer != null
            && command.OfferAnswer.ContentType != null
                ? command.OfferAnswer.ContentType : request.ContentType;
        offerAnswers.ApplyLocalOffer(offer);
    }

    void ApplyRemoteRefreshOffer(SipMessage request)
    {
        if (request.Body.Length == 0)
        {
            return;
        }
        SipOfferAnswer offer = new SipOfferAnswer();
        offer.RemoteOffer = CopyBytes(request.Body);
        offer.ContentType = request.ContentType;
        offerAnswers.ApplyRemoteOffer(offer);
    }

    void ApplySuccessfulLocalRefreshBody(SipMessage response)
    {
        if (localRefreshRequest == null)
        {
            throw new System.InvalidOperationException(
                "The local refresh request is unavailable.");
        }
        if (response.Body.Length > 0)
        {
            if (localRefreshRequest.Body.Length == 0)
            {
                SipOfferAnswer offer = new SipOfferAnswer();
                offer.RemoteOffer = CopyBytes(response.Body);
                offer.ContentType = response.ContentType;
                offerAnswers.ApplyRemoteOffer(offer);
            }
            else
            {
                ApplyRemoteBody(response);
            }
        }
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        if (localRefreshRequest.Body.Length > 0
            && current.State == SipOfferAnswerState.LocalOfferPending)
        {
            throw new SipProtocolException(
                "A successful refresh response must answer the local offer.",
                SipStatusCodes.NotAcceptableHere,
                localRefreshRequest.MethodToken);
        }
    }

    void ApplySuccessfulIncomingRefreshBody(SipMessage response)
    {
        if (incomingRefreshRequest == null)
        {
            throw new System.InvalidOperationException(
                "The incoming refresh request is unavailable.");
        }
        if (response.Body.Length > 0)
        {
            if (incomingRefreshRequest.Body.Length == 0)
            {
                SipOfferAnswer offer = new SipOfferAnswer();
                offer.LocalOffer = CopyBytes(response.Body);
                offer.ContentType = response.ContentType;
                offerAnswers.ApplyLocalOffer(offer);
            }
            else
            {
                ApplyLocalResponseBody(response);
            }
        }
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        if (incomingRefreshRequest.Body.Length > 0
            && current.State == SipOfferAnswerState.RemoteOfferPending)
        {
            throw new SipProtocolException(
                "A successful refresh response must answer the remote offer.",
                SipStatusCodes.NotAcceptableHere,
                incomingRefreshRequest.MethodToken);
        }
    }

    void CopyRefreshBody(SipMessage source, SipMessage destination)
    {
        SipMessage copied = CopyRequiredMessage(source);
        destination.Body = CopyBytes(source.Body);
        destination.ContentType = source.ContentType == null
            ? null : CopyMediaType(source.ContentType);
        destination.ContentDisposition = copied.ContentDisposition;
        if (source.Body.Length > 0 && source.ContentType != null)
        {
            SetOrAddHeaderValue(destination, SipHeaderNames.ContentType,
                FormatMediaType(source.ContentType));
        }
        SetOrAddHeaderValue(destination, SipHeaderNames.ContentLength,
            source.Body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        destination.OriginalBytes = new byte[0];
    }

    void ApplyLocalResponseBody(SipMessage response)
    {
        if (response.Body.Length == 0)
        {
            return;
        }
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        SipOfferAnswer exchange = new SipOfferAnswer();
        exchange.ContentType = response.ContentType;
        if (current.State == SipOfferAnswerState.RemoteOfferPending
            || current.State == SipOfferAnswerState.Completed)
        {
            exchange.LocalAnswer = CopyBytes(response.Body);
            offerAnswers.ApplyOfferAnswer(exchange);
        }
        else if (current.State == SipOfferAnswerState.Stable
            || current.State == SipOfferAnswerState.LocalOfferRequired
            || current.State == SipOfferAnswerState.LocalOfferPending)
        {
            exchange.LocalOffer = CopyBytes(response.Body);
            offerAnswers.ApplyLocalOffer(exchange);
        }
        else
        {
            throw new SipProtocolException(
                "The response body conflicts with the offer/answer state.",
                SipStatusCodes.BadRequest, SipMethods.Invite);
        }
    }

    void ApplyIncomingAckBody(SipMessage ack)
    {
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        if (current.State == SipOfferAnswerState.LocalOfferPending)
        {
            if (ack.Body.Length == 0)
            {
                throw new SipProtocolException(
                    "The ACK must answer the offer in the successful response.",
                    SipStatusCodes.BadRequest, SipMethods.Ack);
            }
            SipOfferAnswer exchange = new SipOfferAnswer();
            exchange.RemoteAnswer = CopyBytes(ack.Body);
            exchange.ContentType = ack.ContentType;
            offerAnswers.ApplyOfferAnswer(exchange);
        }
        else if (ack.Body.Length > 0)
        {
            throw new SipProtocolException(
                "The ACK contains an unexpected offer/answer body.",
                SipStatusCodes.BadRequest, SipMethods.Ack);
        }
    }

    bool ApplyPrackAnswer(SipMessage prack)
    {
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        if (current.State == SipOfferAnswerState.LocalOfferPending)
        {
            if (prack.Body.Length == 0)
            {
                return false;
            }
            SipOfferAnswer answer = new SipOfferAnswer();
            answer.RemoteAnswer = CopyBytes(prack.Body);
            answer.ContentType = prack.ContentType;
            offerAnswers.ApplyOfferAnswer(answer);
            return true;
        }
        return prack.Body.Length == 0;
    }

    int ReadReliableProvisionalSequence(SipMessage response)
    {
        bool requires = MessageHasOptionTag(
            response, SipHeaderNames.Require,
            SipOptionTags.ReliableProvisional);
        int sequence = ReadPositiveHeaderInteger(response, SipHeaderNames.RSeq);
        if (!requires && sequence < 0)
        {
            if (peerRequiresReliableProvisional)
            {
                throw new SipProtocolException(
                    "The peer omitted required reliable provisional headers.",
                    SipStatusCodes.BadExtension, SipMethods.Invite);
            }
            return -1;
        }
        if (!requires || sequence < 1)
        {
            throw new SipProtocolException(
                "A reliable provisional response requires Require and RSeq.",
                SipStatusCodes.BadExtension, SipMethods.Invite);
        }
        if (!peerSupportsReliableProvisional)
        {
            throw new SipProtocolException(
                "Reliable provisional responses were not negotiated.",
                SipStatusCodes.BadExtension, SipMethods.Invite);
        }
        return sequence;
    }

    SipInviteEvent FailRemoteReliableProvisional(
        SipMessage response, string reason)
    {
        cancellationPending = true;
        state = SipInviteSessionState.Terminating;
        SipInviteEvent failed = QueueEvent(response, reason);
        SendPendingCancel();
        return CopyRequiredEvent(failed);
    }

    SipExtensionHeaderValue? ReadRAck(SipMessage message)
    {
        SipExtensionHeaderValue? result = null;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.RAck))
            {
                if (result != null)
                {
                    return null;
                }
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.RAck, header.Value,
                        new SipParseOptions());
                if (!parsed.Success || parsed.Values.Length != 1)
                {
                    return null;
                }
                result = parsed.Values[0];
            }
            index = index + 1;
        }
        return result;
    }

    int ReadPositiveHeaderInteger(SipMessage message, string name)
    {
        int result = -1;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                if (result >= 0)
                {
                    throw new System.ArgumentException(
                        "A reliable provisional header cannot be repeated.",
                        "message");
                }
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        name, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.Values.Length != 1
                    || parsed.Values[0].Number < 1)
                {
                    throw new System.ArgumentException(
                        "A valid positive reliable sequence is required.",
                        "message");
                }
                result = parsed.Values[0].Number;
            }
            index = index + 1;
        }
        return result;
    }

    bool MessageHasOptionTag(
        SipMessage message, string headerName, string optionTag)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (HeaderNameMatches(header.Name, headerName))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    headerName, header.Value, new SipParseOptions());
                int optionIndex = 0;
                while (parsed.Success && optionIndex < parsed.OptionTags.Length)
                {
                    if (SipAscii.EqualsIgnoreCase(
                        parsed.OptionTags[optionIndex], optionTag))
                    {
                        return true;
                    }
                    optionIndex = optionIndex + 1;
                }
            }
            index = index + 1;
        }
        return false;
    }

    void AddOptionTag(
        SipMessage message, string headerName, string optionTag)
    {
        if (MessageHasOptionTag(message, headerName, optionTag))
        {
            return;
        }
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (HeaderNameMatches(header.Name, headerName))
            {
                string current = header.Value == null ? "" : header.Value.Trim();
                header.Value = current.Length == 0
                    ? optionTag : current + ", " + optionTag;
                header.OriginalBytes = new byte[0];
                message.OriginalBytes = new byte[0];
                return;
            }
            index = index + 1;
        }
        AddHeader(message, headerName, optionTag);
    }

    bool HeaderNameMatches(string? candidate, string name)
    {
        return SipAscii.EqualsIgnoreCase(candidate, name)
            || name == SipHeaderNames.Supported
                && SipAscii.EqualsIgnoreCase(
                    candidate, SipHeaderNames.CompactSupported)
            || name == SipHeaderNames.SessionExpires
                && SipAscii.EqualsIgnoreCase(
                    candidate, SipHeaderNames.CompactSessionExpires);
    }

    void AddHeader(SipMessage message, string name, string value)
    {
        SipHeader[] headers = new SipHeader[message.Headers.Length + 1];
        System.Array.Copy(message.Headers, headers, message.Headers.Length);
        SipHeader added = new SipHeader();
        added.Name = name;
        added.Value = value;
        headers[headers.Length - 1] = added;
        message.Headers = headers;
        message.OriginalBytes = new byte[0];
    }

    void SetOrAddHeaderValue(
        SipMessage message, string name, string value)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (HeaderNameMatches(header.Name, name))
            {
                header.Value = value;
                header.OriginalBytes = new byte[0];
                message.OriginalBytes = new byte[0];
                return;
            }
            index = index + 1;
        }
        AddHeader(message, name, value);
    }

    string FormatMediaType(SipMediaType contentType)
    {
        if (!SipAscii.IsToken(contentType.Type)
            || !SipAscii.IsToken(contentType.Subtype))
        {
            throw new System.ArgumentException(
                "A valid PRACK content type is required.", "request");
        }
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append(contentType.Type);
        text.Append('/');
        text.Append(contentType.Subtype);
        int index = 0;
        while (index < contentType.Parameters.Length)
        {
            text.Append(SipHeaderPrimitives.FormatParameterValue(
                contentType.Parameters[index]));
            index = index + 1;
        }
        return text.ToString();
    }

    SipMediaType CopyMediaType(SipMediaType source)
    {
        SipOfferAnswer holder = new SipOfferAnswer();
        holder.ContentType = source;
        SipOfferAnswer? copied = SipSnapshotCopy.CopyOfferAnswer(holder);
        if (copied == null || copied.ContentType == null)
        {
            throw new System.InvalidOperationException(
                "The media type cannot be copied.");
        }
        return copied.ContentType;
    }

    bool MessageUsesReliableTransport(SipMessage message)
    {
        if (message.ViaValues.Length == 0)
        {
            return false;
        }
        string? transport = message.ViaValues[0].Transport;
        return SipAscii.EqualsIgnoreCase(transport, "TCP")
            || SipAscii.EqualsIgnoreCase(transport, "TLS")
            || SipAscii.EqualsIgnoreCase(transport, "WS")
            || SipAscii.EqualsIgnoreCase(transport, "WSS");
    }

    void InitializeOfferAnswer(SipInviteRequest request, SipMessage outgoing)
    {
        if (request.OfferAnswer == null && outgoing.Body.Length == 0)
        {
            return;
        }
        SipOfferAnswer exchange = new SipOfferAnswer();
        if (request.OfferAnswer != null
            && request.OfferAnswer.LocalOffer.Length > 0)
        {
            if (!BytesEqual(request.OfferAnswer.LocalOffer, outgoing.Body))
            {
                throw new System.ArgumentException(
                    "The local offer must match the INVITE body.", "request");
            }
            exchange.LocalOffer = CopyBytes(request.OfferAnswer.LocalOffer);
            exchange.ContentType = request.OfferAnswer.ContentType == null
                ? outgoing.ContentType : request.OfferAnswer.ContentType;
        }
        else
        {
            exchange.LocalOffer = CopyBytes(outgoing.Body);
            exchange.ContentType = outgoing.Body.Length == 0
                ? null : outgoing.ContentType;
        }
        offerAnswers.ApplyLocalOffer(exchange);
    }

    void FailOutstandingOfferAnswer()
    {
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        if (current.State == SipOfferAnswerState.LocalOfferPending
            || current.State == SipOfferAnswerState.RemoteOfferPending
            || current.State == SipOfferAnswerState.LocalOfferRequired
            || current.State == SipOfferAnswerState.RemoteOfferRequired)
        {
            offerAnswers.RollbackOfferAnswer(current);
        }
    }

    void FailIncomingOfferAnswer()
    {
        SipOfferAnswer current = offerAnswers.GetOfferAnswer();
        if (current.State == SipOfferAnswerState.RemoteOfferPending
            || current.State == SipOfferAnswerState.LocalOfferRequired)
        {
            offerAnswers.RejectOfferAnswer(current);
        }
        else if (current.State == SipOfferAnswerState.LocalOfferPending
            || current.State == SipOfferAnswerState.RemoteOfferRequired)
        {
            offerAnswers.RollbackOfferAnswer(current);
        }
    }

    void TerminateEarlyDialog(SipMessage response)
    {
        if (dialog == null || dialog.Id == null
            || dialog.State != SipDialogState.Early)
        {
            return;
        }
        dialog = dialogs.UpdateDialog(dialog.Id, response);
    }

    void AdvanceOperationDeadline()
    {
        if (operationDeadlineMilliseconds < 0
            || nowMilliseconds < operationDeadlineMilliseconds
            || state == SipInviteSessionState.None
            || state == SipInviteSessionState.Terminated)
        {
            return;
        }
        operationDeadlineMilliseconds = -1;
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = "deadline";
        cancellation.Reason = "The INVITE usage deadline expired.";
        cancellationId = cancellation.Id;
        if (role == SipDialogRole.Uas)
        {
            RequestIncomingTermination(cancellation);
        }
        else
        {
            RequestTermination(cancellation);
        }
    }

    bool IsAuthenticationRetry(SipInviteRequest request)
    {
        return state == SipInviteSessionState.Terminated
            && role == SipDialogRole.Uac
            && authenticationContextId != null
            && request.AuthenticationContextId == authenticationContextId
            && lastObservedResponse != null
            && (lastObservedResponse.StatusCode == SipStatusCodes.Unauthorized
                || lastObservedResponse.StatusCode
                    == SipStatusCodes.ProxyAuthenticationRequired);
    }

    void AdoptCommandAuthenticationContext(SipInviteRequest command)
    {
        if (command.AuthenticationContextId != null)
        {
            ValidateAuthenticationContextId(command.AuthenticationContextId);
            authenticationContextId = command.AuthenticationContextId;
            AssociateCurrentDialogAuthentication();
        }
        if (command.DeadlineMilliseconds >= 0)
        {
            operationDeadlineMilliseconds = command.DeadlineMilliseconds;
        }
    }

    void AssociateCurrentDialogAuthentication()
    {
        if (authenticationContextId == null || dialog == null
            || dialog.Id == null || dialog.State == SipDialogState.Terminated)
        {
            return;
        }
        dialogs.AssociateAuthenticationContext(
            dialog.Id, authenticationContextId);
        dialog = dialogs.FindDialog(dialog.Id);
    }

    void TerminateCurrentDialog(string reason)
    {
        if (dialog == null || dialog.Id == null
            || dialog.State == SipDialogState.Terminated)
        {
            return;
        }
        SipDialogSnapshot terminal = CopyRequiredDialogSnapshot(dialog);
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Reason = reason;
        dialogs.TerminateDialog(dialog.Id, cancellation);
        terminal.State = SipDialogState.Terminated;
        terminal.TransactionIds = new string[0];
        terminal.AuthenticationContextId = null;
        dialog = terminal;
    }

    void CleanupTerminalState()
    {
        operationDeadlineMilliseconds = -1;
        cancellationPending = false;
        cancelSent = false;
        acceptedInvite = false;
        awaitingAck = false;
        invite = null;
        inviteKey = null;
        cancelKey = null;
        byeKey = null;
        localRefreshRequest = null;
        localRefreshKey = null;
        incomingRefreshRequest = null;
        incomingRefreshKey = null;
        lastRefreshResponse = null;
        reliableProvisionalResponse = null;
        reliableRetransmitAt = 0;
        reliableTimeoutAt = 0;
        reliableRetransmitInterval = 0;
        prackRequest = null;
        prackKey = null;
        prackRetransmitAt = 0;
        prackTimeoutAt = 0;
        prackRetransmitInterval = 0;
        receivedPrackKey = null;
        receivedPrackResponse = null;
        nextLocalRSeq = 1;
        pendingLocalRSeq = 0;
        lastRemoteRSeq = 0;
        pendingRemoteRSeq = 0;
        pendingRemoteInviteCSeq = -1;
        awaitingPrackAnswer = false;
        prackReliableTransport = false;
        incomingRefreshAccepted = false;
        incomingRefreshAwaitingAck = false;
        StopSessionTimer();
    }

    void PrepareInitialOutgoingMetadata(SipMessage outgoing)
    {
        SipExtensionHeaderValue? supplied = ReadSessionIdentity(outgoing);
        if (supplied != null && supplied.Token != null)
        {
            localSessionId = supplied.Token;
            if (supplied.SecondaryToken != null
                && !IsNilSessionId(supplied.SecondaryToken))
            {
                remoteSessionId = supplied.SecondaryToken;
            }
        }
        AddOptionTag(outgoing, SipHeaderNames.Supported,
            SipOptionTags.FromChange);
        AddOptionTag(outgoing, SipHeaderNames.Supported,
            SipOptionTags.EarlyDialogTermination);
        ApplyLocalSessionIdentity(outgoing);
    }

    void ApplyLocalSessionIdentity(SipMessage message)
    {
        ApplyConnectedRemoteIdentity(message);
        if (localSessionId == null)
        {
            return;
        }
        string remote = remoteSessionId == null
            ? "00000000000000000000000000000000"
            : remoteSessionId;
        SetOrAddHeaderValue(message, SipHeaderNames.SessionId,
            localSessionId + ";remote=" + remote);
    }

    void ApplyConnectedRemoteIdentity(SipMessage message)
    {
        if (message.Kind != SipMessageKind.Request
            || message.Method == SipMethod.Cancel
            || connectedIdentity == null)
        {
            return;
        }
        SipNameAddress? identity = SipSnapshotCopy.CopyNameAddressModel(
            connectedIdentity);
        if (identity == null)
        {
            return;
        }
        message.To = identity;
        SetOrAddHeaderValue(message, SipHeaderNames.To,
            SipHeaderPrimitives.FormatNameAddressValue(
                identity, new SipWriteOptions()));
    }

    void CopyInitialSessionIdentity(SipMessage message)
    {
        if (invite != null)
        {
            int index = 0;
            while (index < invite.Headers.Length)
            {
                SipHeader header = invite.Headers[index];
                if (SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.SessionId)
                    && header.Value != null)
                {
                    SetOrAddHeaderValue(message, SipHeaderNames.SessionId,
                        header.Value);
                    return;
                }
                index = index + 1;
            }
        }
        ApplyLocalSessionIdentity(message);
    }

    void CaptureRemoteMessageMetadata(SipMessage message, bool initial)
    {
        CaptureRemoteCapabilities(message);
        SipExtensionHeaderValue? sessionIdentity = ReadSessionIdentity(message);
        if (sessionIdentity != null && sessionIdentity.Token != null
            && !IsNilSessionId(sessionIdentity.Token))
        {
            remoteSessionId = sessionIdentity.Token;
        }
        bool dialogForming = initial
            && (message.Kind == SipMessageKind.Request
                || message.StatusCode > SipStatusCodes.Trying);
        if (dialogForming)
        {
            peerSupportsFromChange = peerSupportsFromChange
                || MessageHasOptionTag(message, SipHeaderNames.Supported,
                    SipOptionTags.FromChange);
            peerSupportsEarlyDialogTermination =
                peerSupportsEarlyDialogTermination
                || MessageHasOptionTag(message, SipHeaderNames.Supported,
                    SipOptionTags.EarlyDialogTermination);
            SipNameAddress? identity = role == SipDialogRole.Uac
                ? message.To : message.From;
            if (identity != null)
            {
                connectedIdentity = SipSnapshotCopy.CopyNameAddressModel(identity);
            }
        }
        else if (message.Kind == SipMessageKind.Request
            && state == SipInviteSessionState.Confirmed
            && peerSupportsFromChange
            && (message.Method == SipMethod.Invite
                || message.Method == SipMethod.Update)
            && message.From != null)
        {
            pendingConnectedIdentity =
                SipSnapshotCopy.CopyNameAddressModel(message.From);
        }
    }

    void CommitPendingConnectedIdentity()
    {
        if (pendingConnectedIdentity != null)
        {
            connectedIdentity = SipSnapshotCopy.CopyNameAddressModel(
                pendingConnectedIdentity);
            pendingConnectedIdentity = null;
        }
    }

    SipExtensionHeaderValue? ReadSessionIdentity(SipMessage message)
    {
        SipExtensionHeaderValue? result = null;
        int count = 0;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.SessionId))
            {
                count = count + 1;
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.SessionId, header.Value,
                        new SipParseOptions());
                if (count > 1 || !parsed.Success
                    || parsed.Values.Length != 1
                    || !IsSessionId(parsed.Values[0].Token))
                {
                    return null;
                }
                SipExtensionHeaderValue value = parsed.Values[0];
                SipUriParameter? remote = FindHeaderParameter(
                    value.Parameters, "remote");
                if (remote != null
                    && (!remote.HasValue || !IsSessionId(remote.Value)))
                {
                    return null;
                }
                value.Token = value.Token == null
                    ? null : value.Token.ToLowerInvariant();
                value.SecondaryToken = remote == null || remote.Value == null
                    ? null : remote.Value.ToLowerInvariant();
                result = value;
            }
            index = index + 1;
        }
        return result;
    }

    bool IsSessionId(string? value)
    {
        if (value == null || value.Length != 32)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (!(current >= '0' && current <= '9')
                && !(current >= 'a' && current <= 'f')
                && !(current >= 'A' && current <= 'F'))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    bool IsNilSessionId(string value)
    {
        return value == "00000000000000000000000000000000";
    }

    SipUriParameter? FindHeaderParameter(
        SipUriParameter[] parameters, string name)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            if (SipAscii.EqualsIgnoreCase(parameters[index].Name, name))
            {
                return parameters[index];
            }
            index = index + 1;
        }
        return null;
    }

    void CaptureRemoteCapabilities(SipMessage message)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.Allow))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    SipHeaderNames.Allow, header.Value, new SipParseOptions());
                if (parsed.Success)
                {
                    remoteCapabilities.Methods = AddCapabilityValues(
                        remoteCapabilities.Methods, parsed.Methods);
                }
            }
            else if (HeaderNameMatches(
                header.Name, SipHeaderNames.Supported))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    SipHeaderNames.Supported, header.Value,
                    new SipParseOptions());
                if (parsed.Success)
                {
                    remoteCapabilities.OptionTags = AddCapabilityValues(
                        remoteCapabilities.OptionTags, parsed.OptionTags);
                }
            }
            else if (SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.Accept))
            {
                CaptureAcceptedMediaTypes(header);
            }
            else if (SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.AllowEvents)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.CompactAllowEvents))
            {
                CaptureEventPackages(header);
            }
            else if (SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.FeatureCaps))
            {
                CaptureFeatureCapabilities(header);
            }
            index = index + 1;
        }
    }

    void CaptureAcceptedMediaTypes(SipHeader header)
    {
        SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
            SipHeaderNames.Accept, header.Value, new SipParseOptions());
        if (!parsed.Success)
        {
            return;
        }
        string[] values = new string[parsed.MediaTypes.Length];
        int index = 0;
        while (index < parsed.MediaTypes.Length)
        {
            SipMediaType mediaType = parsed.MediaTypes[index];
            values[index] = (mediaType.Type + "/" + mediaType.Subtype)
                .ToLowerInvariant();
            index = index + 1;
        }
        remoteCapabilities.MediaTypes = AddCapabilityValues(
            remoteCapabilities.MediaTypes, values);
    }

    void CaptureEventPackages(SipHeader header)
    {
        SipExtensionHeaderResult parsed =
            SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                SipHeaderNames.AllowEvents, header.Value,
                new SipParseOptions());
        if (!parsed.Success)
        {
            return;
        }
        string[] values = new string[parsed.Values.Length];
        int index = 0;
        while (index < parsed.Values.Length)
        {
            string? token = parsed.Values[index].Token;
            values[index] = token == null ? "" : token;
            index = index + 1;
        }
        remoteCapabilities.EventPackages = AddCapabilityValues(
            remoteCapabilities.EventPackages, values);
    }

    void CaptureFeatureCapabilities(SipHeader header)
    {
        SipExtensionHeaderResult parsed =
            SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                SipHeaderNames.FeatureCaps, header.Value,
                new SipParseOptions());
        if (!parsed.Success)
        {
            return;
        }
        int sourceIndex = 0;
        while (sourceIndex < parsed.Values.Length)
        {
            SipExtensionHeaderValue candidate = parsed.Values[sourceIndex];
            bool found = false;
            int currentIndex = 0;
            while (currentIndex < remoteFeatureCapabilities.Length)
            {
                SipExtensionHeaderValue current =
                    remoteFeatureCapabilities[currentIndex];
                if (SipAscii.EqualsIgnoreCase(current.Token, candidate.Token)
                    && current.OriginalText == candidate.OriginalText)
                {
                    found = true;
                }
                currentIndex = currentIndex + 1;
            }
            if (!found)
            {
                SipExtensionHeaderValue[] added =
                    new SipExtensionHeaderValue[
                        remoteFeatureCapabilities.Length + 1];
                System.Array.Copy(remoteFeatureCapabilities, added,
                    remoteFeatureCapabilities.Length);
                added[added.Length - 1] = candidate;
                remoteFeatureCapabilities = added;
            }
            sourceIndex = sourceIndex + 1;
        }
    }

    string[] AddCapabilityValues(string[] current, string[] added)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < current.Length)
        {
            result.Add(current[index]);
            index = index + 1;
        }
        index = 0;
        while (index < added.Length)
        {
            string candidate = added[index];
            bool found = false;
            int resultIndex = 0;
            while (candidate.Length > 0 && resultIndex < result.Count)
            {
                if (SipAscii.EqualsIgnoreCase(result[resultIndex], candidate))
                {
                    found = true;
                }
                resultIndex = resultIndex + 1;
            }
            if (candidate.Length > 0 && !found)
            {
                result.Add(candidate);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    SipExtensionHeaderValue[] ReadReasonValues(SipMessage? message)
    {
        System.Collections.Generic.List<SipExtensionHeaderValue> values =
            new System.Collections.Generic.List<SipExtensionHeaderValue>();
        if (message == null)
        {
            return values.ToArray();
        }
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.Reason))
            {
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.Reason, header.Value,
                        new SipParseOptions());
                int valueIndex = 0;
                while (parsed.Success && valueIndex < parsed.Values.Length)
                {
                    values.Add(parsed.Values[valueIndex]);
                    valueIndex = valueIndex + 1;
                }
            }
            index = index + 1;
        }
        return SipSnapshotCopy.CopyExtensionHeaderValues(values.ToArray());
    }

    void ValidateEarlyDialogTerminated(SipMessage response, bool outgoing)
    {
        if (ReadTag(response.To) == null
            || !HasSipReasonCause(response))
        {
            throw new System.ArgumentException(
                "A 199 response requires a To tag and SIP Reason cause.",
                outgoing ? "request" : "message");
        }
        if (outgoing && (!peerSupportsEarlyDialogTermination
            || MessageHasOptionTag(response, SipHeaderNames.Supported,
                SipOptionTags.EarlyDialogTermination)
            || MessageHasOptionTag(response, SipHeaderNames.Require,
                SipOptionTags.EarlyDialogTermination)
            || MessageHasOptionTag(response, SipHeaderNames.ProxyRequire,
                SipOptionTags.EarlyDialogTermination)))
        {
            throw new System.InvalidOperationException(
                "The peer did not negotiate a valid 199 response.");
        }
    }

    bool HasSipReasonCause(SipMessage response)
    {
        SipExtensionHeaderValue[] values = ReadReasonValues(response);
        int index = 0;
        while (index < values.Length)
        {
            if (SipAscii.EqualsIgnoreCase(values[index].Token, "SIP")
                && values[index].Number >= 100
                && values[index].Number <= 699)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    SipCancellation CreateEarlyDialogCancellation(SipMessage response)
    {
        SipExtensionHeaderValue[] values = ReadReasonValues(response);
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        int index = 0;
        while (index < values.Length)
        {
            if (SipAscii.EqualsIgnoreCase(values[index].Token, "SIP"))
            {
                cancellation.Reason = values[index].SecondaryToken == null
                    ? "SIP cause " + values[index].Number.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : values[index].SecondaryToken;
                return cancellation;
            }
            index = index + 1;
        }
        cancellation.Reason = "The early dialog was terminated.";
        return cancellation;
    }

    void ResetInitialOfferAnswer()
    {
        offerAnswers = new SipOfferAnswerSession();
        if (invite == null)
        {
            return;
        }
        if (role == SipDialogRole.Uac)
        {
            SipInviteRequest request = new SipInviteRequest();
            request.Message = invite;
            InitializeOfferAnswer(request, invite);
        }
        else
        {
            InitializeIncomingOfferAnswer(invite);
        }
    }

    SipDialogSnapshot CopyRequiredDialogSnapshot(SipDialogSnapshot source)
    {
        SipDialogSnapshot? copied = SipSnapshotCopy.CopyDialogSnapshot(source);
        if (copied == null)
        {
            throw new System.InvalidOperationException(
                "The early dialog snapshot is unavailable.");
        }
        return copied;
    }

    void CopyMessageHeaders(
        SipMessage source, SipMessage destination, string name)
    {
        int index = 0;
        while (index < source.Headers.Length)
        {
            SipHeader header = source.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, name)
                && header.Value != null)
            {
                AddHeader(destination, name, header.Value);
            }
            index = index + 1;
        }
    }

    void ResetSession(SipInviteRequest request)
    {
        events.Clear();
        snapshots.Clear();
        dialogs = new SipDialogLayer();
        offerAnswers = new SipOfferAnswerSession();
        state = SipInviteSessionState.None;
        invite = null;
        inviteKey = null;
        cancelKey = null;
        byeKey = null;
        dialog = null;
        options = CopyOptions(request.Options);
        correlationId = request.CorrelationId;
        redirectCount = 0;
        provisionalReceived = false;
        cancellationPending = false;
        cancelSent = false;
        localTag = null;
        lastInviteResponse = null;
        lastObservedResponse = null;
        acceptedInvite = false;
        awaitingAck = false;
        operationDeadlineMilliseconds = request.DeadlineMilliseconds;
        createdAtMilliseconds = nowMilliseconds;
        cancellationId = null;
        authenticationContextId = request.AuthenticationContextId;
        authenticationRetryCount = 0;
        errorMessage = null;
        ResetInviteMetadata();
        ResetReliableProvisionalState();
        ResetRefreshState();
    }

    void ResetIncomingSession()
    {
        events.Clear();
        snapshots.Clear();
        dialogs = new SipDialogLayer();
        offerAnswers = new SipOfferAnswerSession();
        state = SipInviteSessionState.None;
        role = SipDialogRole.Unknown;
        invite = null;
        inviteKey = null;
        cancelKey = null;
        byeKey = null;
        dialog = null;
        options = new SipInviteOptions();
        correlationId = null;
        redirectCount = 0;
        provisionalReceived = false;
        cancellationPending = false;
        cancelSent = false;
        localTag = null;
        lastInviteResponse = null;
        lastObservedResponse = null;
        acceptedInvite = false;
        awaitingAck = false;
        operationDeadlineMilliseconds = -1;
        createdAtMilliseconds = nowMilliseconds;
        cancellationId = null;
        authenticationContextId = null;
        authenticationRetryCount = 0;
        errorMessage = null;
        ResetInviteMetadata();
        ResetReliableProvisionalState();
        ResetRefreshState();
    }

    void ResetReliableProvisionalState()
    {
        peerSupportsReliableProvisional = false;
        peerRequiresReliableProvisional = false;
        nextLocalRSeq = 1;
        pendingLocalRSeq = 0;
        lastRemoteRSeq = 0;
        pendingRemoteRSeq = 0;
        pendingRemoteInviteCSeq = -1;
        awaitingPrackAnswer = false;
        reliableProvisionalResponse = null;
        reliableRetransmitAt = 0;
        reliableTimeoutAt = 0;
        reliableRetransmitInterval = 0;
        prackRequest = null;
        prackKey = null;
        prackRetransmitAt = 0;
        prackTimeoutAt = 0;
        prackRetransmitInterval = 0;
        prackReliableTransport = false;
        receivedPrackKey = null;
        receivedPrackResponse = null;
    }

    void ResetRefreshState()
    {
        localRefreshRequest = null;
        localRefreshKey = null;
        incomingRefreshRequest = null;
        incomingRefreshKey = null;
        lastRefreshResponse = null;
        incomingRefreshAccepted = false;
        incomingRefreshAwaitingAck = false;
        sessionTimerActive = false;
        localSessionRefresher = false;
        automaticSessionRefresh = false;
        peerAllowsUpdate = false;
        sessionIntervalSeconds = 0;
        sessionMinimumSeconds = 90;
        sessionRefreshAt = 0;
        sessionExpiresAt = 0;
        suspendedSessionRefreshAt = 0;
        sessionTimerSource = null;
    }

    void ResetInviteMetadata()
    {
        connectedIdentity = null;
        pendingConnectedIdentity = null;
        localSessionId = tokens.CreateSessionId();
        remoteSessionId = null;
        remoteCapabilities = new SipCapabilitySet();
        remoteFeatureCapabilities = new SipExtensionHeaderValue[0];
        peerSupportsFromChange = false;
        peerSupportsEarlyDialogTermination = false;
    }

    SipDialogOptions CreateDialogOptions()
    {
        SipDialogOptions value = new SipDialogOptions();
        value.Role = SipDialogRole.Uac;
        value.CorrelationId = correlationId;
        return value;
    }

    SipDialogOptions CreateUasDialogOptions()
    {
        SipDialogOptions value = new SipDialogOptions();
        value.Role = SipDialogRole.Uas;
        value.CorrelationId = correlationId;
        return value;
    }

    SipInviteEvent QueueEvent(SipMessage? message, string reason)
    {
        if (message != null && message.Kind == SipMessageKind.Response)
        {
            lastObservedResponse = CopyRequiredMessage(message);
        }
        SipInviteEvent value = new SipInviteEvent();
        value.State = state;
        value.Snapshot = CreateInviteSnapshot();
        value.Dialog = dialog;
        value.Message = message;
        value.OfferAnswer = offerAnswers.GetOfferAnswer();
        value.Reason = reason;
        value.CorrelationId = correlationId;
        value.Reasons = ReadReasonValues(message);
        value.ConnectedIdentity = connectedIdentity;
        value.LocalSessionId = localSessionId;
        value.RemoteSessionId = remoteSessionId;
        value.RemoteCapabilities = remoteCapabilities;
        value.RemoteFeatureCapabilities = remoteFeatureCapabilities;
        value.ErrorMessage = errorMessage;
        SipInviteEvent copy = CopyRequiredEvent(value);
        events.Add(copy);
        if (value.Snapshot != null)
        {
            snapshots.Add(CopyRequiredInviteSnapshot(value.Snapshot));
        }
        return CopyRequiredEvent(copy);
    }

    SipInviteEvent CopyRequiredEvent(SipInviteEvent source)
    {
        SipInviteEvent copy = new SipInviteEvent();
        copy.State = source.State;
        copy.Snapshot = SipSnapshotCopy.CopyInviteSnapshot(source.Snapshot);
        copy.Dialog = SipSnapshotCopy.CopyDialogSnapshot(source.Dialog);
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.OfferAnswer = SipSnapshotCopy.CopyOfferAnswer(source.OfferAnswer);
        copy.Reason = source.Reason;
        copy.CorrelationId = source.CorrelationId;
        copy.Reasons = SipSnapshotCopy.CopyExtensionHeaderValues(source.Reasons);
        copy.ConnectedIdentity = SipSnapshotCopy.CopyNameAddressModel(
            source.ConnectedIdentity);
        copy.LocalSessionId = source.LocalSessionId;
        copy.RemoteSessionId = source.RemoteSessionId;
        copy.RemoteCapabilities = SipSnapshotCopy.CopyCapabilitySet(
            source.RemoteCapabilities);
        copy.RemoteFeatureCapabilities =
            SipSnapshotCopy.CopyExtensionHeaderValues(
                source.RemoteFeatureCapabilities);
        copy.ErrorMessage = source.ErrorMessage;
        return copy;
    }

    SipInviteSnapshot CreateInviteSnapshot()
    {
        SipInviteSnapshot snapshot = new SipInviteSnapshot();
        snapshot.State = state;
        snapshot.Role = role;
        snapshot.Dialog = SipSnapshotCopy.CopyDialogSnapshot(dialog);
        snapshot.OfferAnswer = SipSnapshotCopy.CopyOfferAnswer(
            offerAnswers.GetOfferAnswer());
        snapshot.Invite = SipSnapshotCopy.CopyMessageModel(invite);
        snapshot.LastResponse = SipSnapshotCopy.CopyMessageModel(
            lastObservedResponse);
        SetInviteSnapshotTimers(snapshot);
        snapshot.CancellationPending = cancellationPending;
        snapshot.CancellationId = cancellationId;
        snapshot.AuthenticationContextId = authenticationContextId;
        snapshot.AuthenticationRetryCount = authenticationRetryCount;
        snapshot.ErrorMessage = errorMessage;
        snapshot.CreatedAtMilliseconds = createdAtMilliseconds;
        snapshot.UpdatedAtMilliseconds = nowMilliseconds;
        snapshot.CorrelationId = correlationId;
        return snapshot;
    }

    void SetInviteSnapshotTimers(SipInviteSnapshot snapshot)
    {
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<long> deadlines =
            new System.Collections.Generic.List<long>();
        if (operationDeadlineMilliseconds >= 0)
        {
            names.Add("operation");
            deadlines.Add(operationDeadlineMilliseconds);
        }
        AddInviteSnapshotTimer(names, deadlines,
            "reliable-provisional-retransmit", reliableRetransmitAt);
        AddInviteSnapshotTimer(names, deadlines,
            "reliable-provisional-timeout", reliableTimeoutAt);
        AddInviteSnapshotTimer(names, deadlines,
            "prack-retransmit", prackRetransmitAt);
        AddInviteSnapshotTimer(names, deadlines,
            "prack-timeout", prackTimeoutAt);
        AddInviteSnapshotTimer(names, deadlines,
            "session-refresh", sessionRefreshAt);
        AddInviteSnapshotTimer(names, deadlines,
            "session-expires", sessionExpiresAt);
        snapshot.TimerNames = names.ToArray();
        snapshot.TimerDeadlines = deadlines.ToArray();
        snapshot.NextDeadlineMilliseconds = -1;
        int index = 0;
        while (index < snapshot.TimerDeadlines.Length)
        {
            long candidate = snapshot.TimerDeadlines[index];
            if (snapshot.NextDeadlineMilliseconds < 0
                || candidate < snapshot.NextDeadlineMilliseconds)
            {
                snapshot.NextDeadlineMilliseconds = candidate;
            }
            index = index + 1;
        }
    }

    void AddInviteSnapshotTimer(
        System.Collections.Generic.List<string> names,
        System.Collections.Generic.List<long> deadlines,
        string name, long deadline)
    {
        if (deadline > 0)
        {
            names.Add(name);
            deadlines.Add(deadline);
        }
    }

    SipInviteSnapshot CopyRequiredInviteSnapshot(SipInviteSnapshot source)
    {
        SipInviteSnapshot? copy = SipSnapshotCopy.CopyInviteSnapshot(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The INVITE usage snapshot is unavailable.");
        }
        return copy;
    }

    SipMessage CopyRequiredMessage(SipMessage? source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.ArgumentException(
                "A complete INVITE message is required.", "request");
        }
        return copy;
    }

    SipDialogId CopyRequiredDialogId(SipDialogId? source)
    {
        if (source == null || source.CallId == null || source.LocalTag == null
            || source.RemoteTag == null)
        {
            throw new System.InvalidOperationException(
                "The confirmed dialog identifier is incomplete.");
        }
        SipDialogId copy = new SipDialogId();
        copy.CallId = source.CallId;
        copy.LocalTag = source.LocalTag;
        copy.RemoteTag = source.RemoteTag;
        return copy;
    }

    SipUri CopyTarget(SipUri target, string parameterName)
    {
        if ((target.Scheme != SipUriScheme.Sip
            && target.Scheme != SipUriScheme.Sips)
            || SipUriValidator.ValidateUriModel(target).Length > 0)
        {
            throw new System.ArgumentException(
                "A valid SIP or SIPS target is required.", parameterName);
        }
        return SipUriModelCopy.CreateNormalizedCopy(target);
    }

    void SetTopViaBranch(SipMessage message, string branch)
    {
        SipViaValue via = message.ViaValues[0];
        via.Branch = branch;
        int index = 0;
        bool found = false;
        while (index < via.Parameters.Length)
        {
            SipUriParameter parameter = via.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "branch"))
            {
                parameter.Value = branch;
                parameter.HasValue = true;
                parameter.OriginalText = null;
                found = true;
            }
            index = index + 1;
        }
        if (!found)
        {
            SipUriParameter[] parameters = new SipUriParameter[via.Parameters.Length + 1];
            index = 0;
            while (index < via.Parameters.Length)
            {
                parameters[index] = via.Parameters[index];
                index = index + 1;
            }
            SipUriParameter added = new SipUriParameter();
            added.Name = "branch";
            added.Value = branch;
            added.HasValue = true;
            parameters[via.Parameters.Length] = added;
            via.Parameters = parameters;
        }
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.PreserveOriginal = false;
        SipWriteResult formatted = SipViaCodec.FormatViaHeaderValues(
            new SipViaValue[] { via }, writeOptions);
        if (!formatted.Success)
        {
            throw new System.InvalidOperationException(
                "The redirected INVITE Via cannot be formatted.");
        }
        string text = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = text.IndexOf(':');
        SetHeaderValue(message.Headers, SipHeaderNames.Via,
            colon < 0 ? "" : text.Substring(colon + 1).Trim());
    }

    void SetHeaderValue(SipHeader[] headers, string name, string value)
    {
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader header = headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, name)
                || name == SipHeaderNames.Via
                    && SipAscii.EqualsIgnoreCase(
                        header.Name, SipHeaderNames.CompactVia))
            {
                header.Value = value;
                header.OriginalBytes = new byte[0];
                return;
            }
            index = index + 1;
        }
        throw new System.InvalidOperationException(
            "The generated request lacks a required header.");
    }

    void ValidateStartRequest(SipInviteRequest? request)
    {
        if (request == null || request.Options == null
            || request.Options.MaxRedirectCount < 0
            || request.DeadlineMilliseconds < -1
            || request.CorrelationId != null
                && request.CorrelationId.Length > SipDefaults.MaxHeaderLineBytes
            || request.AuthenticationContextId != null
                && (request.AuthenticationContextId.Length == 0
                    || request.AuthenticationContextId.Length
                        > SipDefaults.MaxHeaderLineBytes))
        {
            throw new System.ArgumentException(
                "Valid INVITE request data is required.", "request");
        }
        if (request.Cancellation != null)
        {
            ValidateCancellation(request.Cancellation);
        }
    }

    void ValidateOutgoingInvite(SipMessage message)
    {
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(
            message, true);
        SipContactValue? contact = message.ContactValues.Length == 1
            ? message.ContactValues[0] : null;
        if (SipMessageValidator.HasErrorIssues(issues)
            || message.Kind != SipMessageKind.Request
            || message.Method != SipMethod.Invite || message.CSeq == null
            || message.CSeq.Method != SipMethod.Invite
            || message.RequestUri == null || message.From == null
            || message.To == null || message.ViaValues.Length == 0
            || contact == null || contact.Wildcard
            || contact.Address == null || contact.Address.Uri == null)
        {
            throw new System.ArgumentException(
                "A complete valid INVITE request is required.", "request");
        }
    }

    void ValidateIncomingInvite(SipMessage? message)
    {
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(
            message, true);
        SipContactValue? contact = message != null
            && message.ContactValues.Length == 1
                ? message.ContactValues[0] : null;
        if (message == null || SipMessageValidator.HasErrorIssues(issues)
            || message.Kind != SipMessageKind.Request
            || message.Method != SipMethod.Invite || message.CSeq == null
            || message.CSeq.Method != SipMethod.Invite
            || message.RequestUri == null || message.From == null
            || message.To == null || message.ViaValues.Length == 0
            || contact == null || contact.Wildcard
            || contact.Address == null || contact.Address.Uri == null)
        {
            throw new System.ArgumentException(
                "A complete valid incoming INVITE is required.", "message");
        }
    }

    void ValidateIncomingResponsePair(SipMessage response)
    {
        ValidateResponse(response);
        string? responseCallId = ReadCallId(response);
        string? responseFromTag = ReadTag(response.From);
        string? inviteFromTag = invite == null ? null : ReadTag(invite.From);
        if (invite == null || inviteKey == null || response.CSeq == null
            || response.CSeq.Method != SipMethod.Invite
            || response.CSeq.SequenceNumber != inviteKey.CSeq
            || response.ViaValues.Length == 0
            || response.ViaValues[0].Branch != inviteKey.Branch
            || responseCallId != inviteKey.CallId
            || responseFromTag != inviteFromTag
            || response.From == null || invite.From == null
            || response.From.Uri == null || invite.From.Uri == null
            || response.To == null || invite.To == null
            || response.To.Uri == null || invite.To.Uri == null
            || !SipUriComparison.CompareUriModels(
                response.From.Uri, invite.From.Uri)
            || !SipUriComparison.CompareUriModels(
                response.To.Uri, invite.To.Uri))
        {
            throw new System.ArgumentException(
                "The response does not match the incoming INVITE.", "request");
        }
    }

    void ValidateRefreshResponsePair(
        SipMessage response, SipMessage request, SipTransactionKey key)
    {
        ValidateResponse(response);
        string? responseCallId = ReadCallId(response);
        string? responseFromTag = ReadTag(response.From);
        string? requestFromTag = ReadTag(request.From);
        string? responseToTag = ReadTag(response.To);
        string? requestToTag = ReadTag(request.To);
        if (response.CSeq == null || request.CSeq == null
            || response.CSeq.Method != request.Method
            || response.CSeq.SequenceNumber != request.CSeq.SequenceNumber
            || response.ViaValues.Length == 0
            || response.ViaValues[0].Branch != key.Branch
            || responseCallId != key.CallId
            || responseFromTag != requestFromTag
            || responseToTag != requestToTag
            || response.From == null || request.From == null
            || response.From.Uri == null || request.From.Uri == null
            || response.To == null || request.To == null
            || response.To.Uri == null || request.To.Uri == null
            || !SipUriComparison.CompareUriModels(
                response.From.Uri, request.From.Uri)
            || !SipUriComparison.CompareUriModels(
                response.To.Uri, request.To.Uri))
        {
            throw new System.ArgumentException(
                "The response does not match the incoming dialog refresh.",
                "request");
        }
    }

    void AdoptLocalTag(SipMessage response)
    {
        string? responseTag = ReadTag(response.To);
        if (!SipAscii.IsToken(responseTag))
        {
            throw new System.ArgumentException(
                "A non-100 INVITE response requires a valid To tag.", "request");
        }
        if (localTag == null)
        {
            localTag = responseTag;
        }
        else if (localTag != responseTag)
        {
            throw new System.ArgumentException(
                "All responses for an incoming INVITE must use one To tag.",
                "request");
        }
    }

    void EnsureLocalTag()
    {
        if (localTag == null)
        {
            localTag = tokens.CreateTag();
        }
    }

    string? ReadTag(SipNameAddress? address)
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

    string? ReadCallId(SipMessage message)
    {
        string? result = null;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CallId)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.CompactCallId))
            {
                if (result != null)
                {
                    return null;
                }
                SipStructuredHeaderResult parsed =
                    SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                        SipHeaderNames.CallId, header.Value,
                        new SipParseOptions());
                if (!parsed.Success || parsed.CallId == null)
                {
                    return null;
                }
                result = parsed.CallId;
            }
            index = index + 1;
        }
        return result;
    }

    bool ResponseIsReliableProvisional(SipMessage response)
    {
        if (response.StatusCode <= SipStatusCodes.Trying
            || response.StatusCode >= 200)
        {
            return false;
        }
        bool hasRSeq = false;
        bool requiresReliability = false;
        int index = 0;
        while (index < response.Headers.Length)
        {
            SipHeader header = response.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.RSeq))
            {
                hasRSeq = true;
            }
            else if (SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.Require))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    SipHeaderNames.Require, header.Value,
                    new SipParseOptions());
                int optionIndex = 0;
                while (parsed.Success
                    && optionIndex < parsed.OptionTags.Length)
                {
                    if (SipAscii.EqualsIgnoreCase(
                        parsed.OptionTags[optionIndex],
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

    bool IncomingResponsesEquivalent(
        SipMessage? first, SipMessage second)
    {
        return first != null && first.StatusCode == second.StatusCode
            && ReadTag(first.To) == ReadTag(second.To)
            && BytesEqual(first.Body, second.Body);
    }

    void ValidateResponse(SipMessage? message)
    {
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(
            message, true);
        if (message == null || SipMessageValidator.HasErrorIssues(issues)
            || message.Kind != SipMessageKind.Response
            || message.StatusCode < 100 || message.StatusCode > 699)
        {
            throw new System.ArgumentException(
                "A valid SIP response is required.", "message");
        }
    }

    void ValidateCancellation(SipCancellation? cancellation)
    {
        if (cancellation == null
            || cancellation.Id != null
                && cancellation.Id.Length > SipDefaults.MaxHeaderLineBytes
            || cancellation.Reason != null
                && cancellation.Reason.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "A valid cancellation request is required.", "cancellation");
        }
    }

    void ValidateAuthenticationContextId(string? authenticationContextId)
    {
        if (authenticationContextId == null
            || authenticationContextId.Length == 0
            || authenticationContextId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "A valid authentication context identifier is required.",
                "authenticationContextId");
        }
    }

    void ValidateTransportError(string? transportError)
    {
        if (transportError == null || transportError.Length == 0
            || transportError.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "A bounded transport failure description is required.",
                "errorMessage");
        }
    }

    string ReadCancellationReason(SipCancellation cancellation)
    {
        return cancellation.Reason == null || cancellation.Reason.Length == 0
            ? "INVITE usage termination requested." : cancellation.Reason;
    }

    SipInviteOptions CopyOptions(SipInviteOptions source)
    {
        SipInviteOptions copy = new SipInviteOptions();
        copy.ReliableProvisional = source.ReliableProvisional;
        copy.SessionTimers = source.SessionTimers;
        copy.SessionExpiresSeconds = source.SessionExpiresSeconds;
        copy.MinSessionExpiresSeconds = source.MinSessionExpiresSeconds;
        copy.MaxRedirectCount = source.MaxRedirectCount;
        copy.AllowAuthenticationRetry = source.AllowAuthenticationRetry;
        return copy;
    }

    byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    bool BytesEqual(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
