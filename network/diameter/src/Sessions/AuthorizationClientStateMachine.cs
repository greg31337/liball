using System;

namespace diameter;

internal class AuthorizationClientTransitionResult
{
    internal bool Accepted;
    internal bool Changed;
    internal bool AuthorizationRequestRequired;
    internal bool ReAuthAnswerRequired;
    internal bool AbortAnswerRequired;
    internal bool TerminationRequestRequired;
    internal bool DisconnectRequired;
    internal uint AnswerResultCode;
    internal DiameterReAuthRequestType ReAuthRequestType;
    internal DiameterTerminationCause TerminationCause;
    internal DiameterSessionState PreviousState;
    internal DiameterSessionRecord? Session;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class AuthorizationClientAnswerPolicy
{
    internal bool Valid;
    internal bool MultiRound;
    internal bool Successful;
    internal uint ResultCode;
    internal uint AuthorizationLifetimeSeconds;
    internal bool AuthorizationLifetimePresent;
    internal uint GracePeriodSeconds;
    internal bool GracePeriodPresent;
    internal uint SessionTimeoutSeconds;
    internal bool SessionTimeoutPresent;
    internal uint MultiRoundTimeoutSeconds;
    internal bool MultiRoundTimeoutPresent;
    internal DiameterReAuthRequestType ReAuthRequestType;
    internal bool ReAuthRequestTypePresent;
    internal string Code = string.Empty;
}

internal class AuthorizationClientStateMachine
{
    internal SessionTable Table;
    internal IDiameterClock Clock;

    internal AuthorizationClientStateMachine(
        SessionTable table,
        IDiameterClock clock)
    {
        if (table == null || clock == null)
        {
            throw new ArgumentException(
                "A client session table and clock are required.");
        }

        Table = table;
        Clock = clock;
    }

    internal AuthorizationClientTransitionResult CreateStatefulClientSession(
        DiameterSessionConfiguration? configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentException(
                "A client session configuration is required.");
        }

        configuration.ValidateSessionConfiguration();
        if (configuration.Kind !=
                DiameterSessionKind.StatefulAuthorization ||
            configuration.Role != DiameterSessionRole.Client ||
            configuration.SessionId.Length == 0)
        {
            throw new ArgumentException(
                "A stateful client session with a Session-Id is required.");
        }

        DateTimeOffset now = Clock.ReadUtcTime();
        DiameterSessionRecord record = new DiameterSessionRecord();
        record.SessionId = configuration.SessionId;
        record.Configuration =
            SessionRecordCloner.CloneSessionConfiguration(configuration);
        record.State = DiameterSessionState.Created;
        record.PeerIdentity =
            SessionRecordCloner.CloneOptionalSessionIdentity(
                configuration.PeerIdentity);
        record.CreatedAt = now;
        record.UpdatedAt = now;
        record.StateChangedAt = now;
        record.ApplicationState = SessionRecordCloner.CloneSessionBytes(
            configuration.ApplicationState);
        DiameterSessionRecord saved = Table.SaveSessionRecord(record, 0);
        return CreateClientTransitionResult(
            saved,
            DiameterSessionState.Created,
            true,
            "client-session-created");
    }

    internal AuthorizationClientTransitionResult BeginInitialClientAuthorization(
        string? sessionId,
        DiameterMessage? request,
        long pendingRequestIdentifier)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        RequireClientState(record, DiameterSessionState.Created);
        ValidateClientAuthorizationRequest(
            record,
            request,
            pendingRequestIdentifier);
        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Pending;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = pendingRequestIdentifier;
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        record.LastAnswer = null;
        record.MultiRoundDeadlinePresent = false;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        return CreateClientTransitionResult(
            saved,
            previous,
            true,
            "client-authorization-pending");
    }

    internal AuthorizationClientTransitionResult ContinueClientMultiRoundAuthorization(
        string? sessionId,
        DiameterMessage? request,
        long pendingRequestIdentifier)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        if ((record.State != DiameterSessionState.Pending &&
            record.State != DiameterSessionState.Reauthorizing) ||
            !record.MultiRoundDeadlinePresent ||
            record.MultiRoundDeadline <= Clock.ReadUtcTime())
        {
            throw new DiameterSessionException(
                "The session is not awaiting a multi-round response.");
        }

        ValidateClientAuthorizationRequest(
            record,
            request,
            pendingRequestIdentifier);
        DiameterSessionState previous = record.State;
        record.UpdatedAt = Clock.ReadUtcTime();
        record.PendingRequestIdentifier = pendingRequestIdentifier;
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        record.LastAnswer = null;
        record.MultiRoundDeadlinePresent = false;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        return CreateClientTransitionResult(
            saved,
            previous,
            true,
            "client-multi-round-response-sent");
    }

    internal AuthorizationClientTransitionResult ProcessClientAuthorizationAnswer(
        string? sessionId,
        DiameterMessage? answer)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        if (record.State != DiameterSessionState.Pending &&
            record.State != DiameterSessionState.Reauthorizing)
        {
            throw new DiameterSessionException(
                "The session is not awaiting an authorization answer.");
        }

        string correlationFailure = ValidateClientAnswerCorrelation(
            record,
            answer,
            0U);
        if (correlationFailure.Length > 0 || answer == null)
        {
            return FailClientRecord(
                record,
                "client-authorization-answer-invalid");
        }

        AuthorizationClientAnswerPolicy policy =
            ReadClientAuthorizationAnswerPolicy(answer);
        if (!policy.Valid)
        {
            DiameterSessionState invalidPrevious = record.State;
            DateTimeOffset invalidNow = Clock.ReadUtcTime();
            record.State = DiameterSessionState.Terminating;
            record.StateChangedAt = invalidNow;
            record.UpdatedAt = invalidNow;
            record.LastAnswer =
                SessionRecordCloner.CloneOptionalSessionMessage(answer);
            record.PendingRequestIdentifier = 0;
            record.TerminationCause = DiameterTerminationCause.BadAnswer;
            record.TerminationCausePresent = true;
            DiameterSessionRecord invalidSaved = Table.SaveSessionRecord(
                record,
                record.Version);
            AuthorizationClientTransitionResult invalid =
                CreateClientTransitionResult(
                    invalidSaved,
                    invalidPrevious,
                    true,
                    policy.Code);
            invalid.TerminationRequestRequired = true;
            invalid.DisconnectRequired = true;
            invalid.TerminationCause = DiameterTerminationCause.BadAnswer;
            return invalid;
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.LastAnswer =
            SessionRecordCloner.CloneOptionalSessionMessage(answer);
        record.PendingRequestIdentifier = 0;
        if (policy.MultiRound)
        {
            uint seconds = policy.MultiRoundTimeoutPresent ?
                policy.MultiRoundTimeoutSeconds :
                ReadConfiguredMultiRoundSeconds(record.Configuration);
            record.MultiRoundDeadline = AddClientTimerSeconds(now, seconds);
            record.MultiRoundDeadlinePresent = true;
            record.UpdatedAt = now;
            DiameterSessionRecord multiRound = Table.SaveSessionRecord(
                record,
                record.Version);
            return CreateClientTransitionResult(
                multiRound,
                previous,
                true,
                "client-multi-round-required");
        }

        if (!policy.Successful)
        {
            return FailClientRecord(
                record,
                "client-authorization-rejected");
        }

        record.State = DiameterSessionState.Open;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.InitialAuthorizationCompleted = true;
        record.ReauthorizationRequested = false;
        record.MultiRoundDeadlinePresent = false;
        ApplyClientAuthorizationTimers(record, policy, now);
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        return CreateClientTransitionResult(
            saved,
            previous,
            true,
            "client-authorization-open");
    }

    internal AuthorizationClientTransitionResult BeginClientReauthorization(
        string? sessionId,
        DiameterMessage? request,
        long pendingRequestIdentifier)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        if (record.State != DiameterSessionState.Open &&
            record.State != DiameterSessionState.Reauthorizing)
        {
            throw new DiameterSessionException(
                "The session cannot begin re-authorization.");
        }

        ValidateClientAuthorizationRequest(
            record,
            request,
            pendingRequestIdentifier);
        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Reauthorizing;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = pendingRequestIdentifier;
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        record.LastAnswer = null;
        record.ReauthorizationRequested = false;
        record.MultiRoundDeadlinePresent = false;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        return CreateClientTransitionResult(
            saved,
            previous,
            true,
            "client-reauthorization-pending");
    }

    internal AuthorizationClientTransitionResult EvaluateClientAuthorizationTimers(
        string? sessionId)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        DateTimeOffset now = Clock.ReadUtcTime();
        bool sessionExpired = record.SessionDeadlinePresent &&
            record.SessionDeadline <= now;
        bool graceExpired = record.GraceDeadlinePresent &&
            record.GraceDeadline <= now;
        bool multiRoundExpired = record.MultiRoundDeadlinePresent &&
            record.MultiRoundDeadline <= now;
        if (graceExpired &&
            (!sessionExpired ||
            record.GraceDeadline <= record.SessionDeadline) &&
            (!multiRoundExpired ||
            record.GraceDeadline <= record.MultiRoundDeadline))
        {
            return RequireClientTermination(
                record,
                DiameterTerminationCause.AuthExpired,
                "client-authorization-grace-expired");
        }

        if (multiRoundExpired &&
            (!sessionExpired ||
            record.MultiRoundDeadline <= record.SessionDeadline))
        {
            if (record.InitialAuthorizationCompleted)
            {
                return RequireClientTermination(
                    record,
                    DiameterTerminationCause.BadAnswer,
                    "client-multi-round-timeout");
            }

            return FailClientRecord(
                record,
                "client-multi-round-timeout");
        }

        if (sessionExpired)
        {
            return RequireClientTermination(
                record,
                DiameterTerminationCause.SessionTimeout,
                "client-session-timeout-expired");
        }

        if (record.State == DiameterSessionState.Open &&
            record.AuthorizationDeadlinePresent &&
            record.AuthorizationDeadline <= now)
        {
            DiameterSessionState previous = record.State;
            record.State = DiameterSessionState.Reauthorizing;
            record.StateChangedAt = now;
            record.UpdatedAt = now;
            record.ReauthorizationRequested = true;
            record.AuthorizationDeadlinePresent = false;
            DiameterSessionRecord saved = Table.SaveSessionRecord(
                record,
                record.Version);
            AuthorizationClientTransitionResult result =
                CreateClientTransitionResult(
                    saved,
                    previous,
                    true,
                    "client-authorization-lifetime-expired");
            result.AuthorizationRequestRequired = true;
            result.ReAuthRequestType = saved.ReAuthRequestType;
            return result;
        }

        return CreateClientTransitionResult(
            record,
            record.State,
            false,
            "client-session-timer-not-due");
    }

    internal AuthorizationClientTransitionResult BeginClientSessionTermination(
        string? sessionId,
        DiameterMessage? request,
        long pendingRequestIdentifier,
        DiameterTerminationCause cause)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        if (record.State != DiameterSessionState.Open &&
            record.State != DiameterSessionState.Reauthorizing &&
            record.State != DiameterSessionState.Aborting &&
            record.State != DiameterSessionState.Terminating)
        {
            throw new DiameterSessionException(
                "The session cannot begin termination.");
        }

        ValidateClientBaseRequest(
            record,
            request,
            DiameterCommandCodes.SessionTermination,
            pendingRequestIdentifier);
        int causeValue = (int)cause;
        if (causeValue < (int)DiameterTerminationCause.Logout ||
            causeValue > (int)DiameterTerminationCause.SessionTimeout)
        {
            throw new ArgumentOutOfRangeException("cause");
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Terminating;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = pendingRequestIdentifier;
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        record.LastAnswer = null;
        record.TerminationCause = cause;
        record.TerminationCausePresent = true;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        return CreateClientTransitionResult(
            saved,
            previous,
            true,
            "client-termination-pending");
    }

    internal AuthorizationClientTransitionResult ProcessClientTerminationAnswer(
        string? sessionId,
        DiameterMessage? answer)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        RequireClientState(record, DiameterSessionState.Terminating);
        string failure = ValidateClientAnswerCorrelation(
            record,
            answer,
            DiameterCommandCodes.SessionTermination);
        if (failure.Length > 0 || answer == null)
        {
            return FailClientRecord(
                record,
                "client-termination-answer-invalid");
        }

        AuthorizationClientAnswerPolicy terminalPolicy =
            ReadClientAuthorizationAnswerPolicy(answer);
        if (!terminalPolicy.Valid)
        {
            return FailClientRecord(
                record,
                "client-termination-answer-invalid");
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Closed;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = 0;
        record.LastAnswer =
            SessionRecordCloner.CloneOptionalSessionMessage(answer);
        ClearClientSessionTimers(record);
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        AuthorizationClientTransitionResult result =
            CreateClientTransitionResult(
                saved,
                previous,
                true,
                "client-session-closed");
        result.DisconnectRequired = true;
        return result;
    }

    internal AuthorizationClientTransitionResult ProcessClientReAuthRequest(
        string? sessionId,
        DiameterMessage? request,
        bool performReauthorization)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        RequireClientState(record, DiameterSessionState.Open);
        ValidateIncomingClientBaseRequest(
            record,
            request,
            DiameterCommandCodes.ReAuth);
        if (request == null)
        {
            throw new ArgumentException(
                "A Re-Auth request is required.");
        }

        DiameterMessage reauthRequest = request;
        AuthorizationClientTransitionResult result;
        if (!performReauthorization)
        {
            result = FailClientRecord(
                record,
                "client-server-reauthorization-refused");
            result.ReAuthAnswerRequired = true;
            result.AnswerResultCode = DiameterResultCodes.UnableToComply;
            return result;
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.UpdatedAt = now;
        record.ReauthorizationRequested = true;
        record.ReAuthRequestType = ReadClientReAuthRequestType(
            reauthRequest);
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(reauthRequest);
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        result = CreateClientTransitionResult(
            saved,
            previous,
            true,
            "client-server-reauthorization-accepted");
        result.ReAuthAnswerRequired = true;
        result.AnswerResultCode = DiameterResultCodes.Success;
        result.AuthorizationRequestRequired = true;
        result.ReAuthRequestType = saved.ReAuthRequestType;
        return result;
    }

    internal AuthorizationClientTransitionResult ProcessClientAbortRequest(
        string? sessionId,
        DiameterMessage? request,
        bool comply)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        if (record.State != DiameterSessionState.Open &&
            record.State != DiameterSessionState.Terminating &&
            record.State != DiameterSessionState.Aborting)
        {
            throw new DiameterSessionException(
                "The session cannot process an abort request.");
        }

        ValidateIncomingClientBaseRequest(
            record,
            request,
            DiameterCommandCodes.AbortSession);
        if (!comply)
        {
            AuthorizationClientTransitionResult refused =
                CreateClientTransitionResult(
                    record,
                    record.State,
                    false,
                    "client-abort-refused");
            refused.AbortAnswerRequired = true;
            refused.AnswerResultCode = DiameterResultCodes.UnableToComply;
            return refused;
        }

        if (record.State != DiameterSessionState.Open)
        {
            AuthorizationClientTransitionResult pending =
                CreateClientTransitionResult(
                    record,
                    record.State,
                    false,
                    "client-abort-already-pending");
            pending.AbortAnswerRequired = true;
            pending.AnswerResultCode = DiameterResultCodes.Success;
            return pending;
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Aborting;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        record.TerminationCause = DiameterTerminationCause.Administrative;
        record.TerminationCausePresent = true;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        AuthorizationClientTransitionResult result =
            CreateClientTransitionResult(
                saved,
                previous,
                true,
                "client-abort-accepted");
        result.AbortAnswerRequired = true;
        result.AnswerResultCode = DiameterResultCodes.Success;
        result.TerminationRequestRequired = true;
        result.DisconnectRequired = true;
        result.TerminationCause = DiameterTerminationCause.Administrative;
        return result;
    }

    internal AuthorizationClientTransitionResult RejectClientAuthorizedService(
        string? sessionId)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        RequireClientState(record, DiameterSessionState.Open);
        return RequireClientTermination(
            record,
            DiameterTerminationCause.ServiceNotProvided,
            "client-authorized-service-not-provided");
    }

    internal AuthorizationClientTransitionResult FailClientAuthorizationOperation(
        string? sessionId)
    {
        DiameterSessionRecord record = ReadRequiredClientSession(sessionId);
        if (record.State == DiameterSessionState.Closed ||
            record.State == DiameterSessionState.Failed)
        {
            throw new DiameterSessionException(
                "The session operation is already terminal.");
        }

        return FailClientRecord(record, "client-session-operation-failed");
    }

    internal DiameterSessionRecord ReadRequiredClientSession(
        string? sessionId)
    {
        DiameterSessionRecord? record = Table.ReadSessionRecord(sessionId);
        if (record == null ||
            record.Configuration.Kind !=
                DiameterSessionKind.StatefulAuthorization ||
            record.Configuration.Role != DiameterSessionRole.Client)
        {
            throw new DiameterSessionException(
                "A stateful client session was not found.");
        }

        return record;
    }

    internal void RequireClientState(
        DiameterSessionRecord record,
        DiameterSessionState state)
    {
        if (record.State != state)
        {
            throw new DiameterSessionException(
                "The client session state does not allow this transition.");
        }
    }

    internal void ValidateClientAuthorizationRequest(
        DiameterSessionRecord record,
        DiameterMessage? request,
        long pendingRequestIdentifier)
    {
        if (request == null || pendingRequestIdentifier <= 0 ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            request.ApplicationId != record.Configuration.ApplicationId ||
            request.CommandCode == 0U || request.Avps == null ||
            !ContainsClientSessionIdentifier(request, record.SessionId))
        {
            throw new ArgumentException(
                "A correlated application authorization request is required.");
        }
    }

    internal void ValidateClientBaseRequest(
        DiameterSessionRecord record,
        DiameterMessage? request,
        uint commandCode,
        long pendingRequestIdentifier)
    {
        if (request == null || pendingRequestIdentifier <= 0 ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            request.ApplicationId != DiameterApplicationIds.Common ||
            request.CommandCode != commandCode || request.Avps == null ||
            !ContainsClientSessionIdentifier(request, record.SessionId))
        {
            throw new ArgumentException(
                "A correlated base session request is required.");
        }
    }

    internal void ValidateIncomingClientBaseRequest(
        DiameterSessionRecord record,
        DiameterMessage? request,
        uint commandCode)
    {
        if (request == null ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            request.ApplicationId != DiameterApplicationIds.Common &&
            (commandCode != DiameterCommandCodes.ReAuth ||
            request.ApplicationId != record.Configuration.ApplicationId) ||
            request.CommandCode != commandCode || request.Avps == null ||
            !ContainsClientSessionIdentifier(request, record.SessionId))
        {
            throw new ArgumentException(
                "A correlated inbound base session request is required.");
        }

        if (commandCode == DiameterCommandCodes.ReAuth &&
            !ContainsClientReAuthRequestType(request))
        {
            throw new ArgumentException(
                "A Re-Auth request requires one valid request type.");
        }
    }

    internal string ValidateClientAnswerCorrelation(
        DiameterSessionRecord record,
        DiameterMessage? answer,
        uint requiredCommandCode)
    {
        DiameterMessage? request = record.LastRequest;
        if (answer == null || request == null || answer.Avps == null ||
            (answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            answer.CommandCode != request.CommandCode ||
            answer.ApplicationId != request.ApplicationId ||
            answer.HopByHopIdentifier != request.HopByHopIdentifier ||
            answer.EndToEndIdentifier != request.EndToEndIdentifier ||
            (requiredCommandCode != 0U &&
            answer.CommandCode != requiredCommandCode) ||
            !ContainsClientSessionIdentifier(answer, record.SessionId))
        {
            return "client-session-answer-correlation-invalid";
        }

        return string.Empty;
    }

    internal bool ContainsClientSessionIdentifier(
        DiameterMessage message,
        string sessionId)
    {
        int count = 0;
        string value = string.Empty;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.SessionId)
            {
                count = count + 1;
                DiameterValueResult decoded =
                    ValueCodecs.DecodeUtf8String(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    return false;
                }

                value = decoded.Text;
            }

            index = index + 1;
        }

        return count == 1 && value == sessionId;
    }

    internal bool ContainsClientReAuthRequestType(DiameterMessage message)
    {
        int count = 0;
        uint value = 0U;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ReAuthRequestType)
            {
                count = count + 1;
                if (avp.Data == null || avp.Data.Length != 4)
                {
                    return false;
                }

                value = NetworkByteOrder.ReadUInt32(avp.Data);
            }

            index = index + 1;
        }

        return count == 1 && value <= 1U;
    }

    internal DiameterReAuthRequestType ReadClientReAuthRequestType(
        DiameterMessage message)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ReAuthRequestType &&
                avp.Data != null && avp.Data.Length == 4)
            {
                return (DiameterReAuthRequestType)
                    NetworkByteOrder.ReadUInt32(avp.Data);
            }

            index = index + 1;
        }

        return DiameterReAuthRequestType.AuthorizeOnly;
    }

    internal AuthorizationClientAnswerPolicy ReadClientAuthorizationAnswerPolicy(
        DiameterMessage answer)
    {
        AuthorizationClientAnswerPolicy result =
            new AuthorizationClientAnswerPolicy();
        int resultCount = 0;
        int authStateCount = 0;
        int lifetimeCount = 0;
        int graceCount = 0;
        int sessionCount = 0;
        int multiRoundCount = 0;
        int reauthCount = 0;
        uint authState = 0U;
        uint reauthType = 0U;
        bool malformed = false;
        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf)
            {
                if ((avp.Code == DiameterAvpCodes.ResultCode ||
                    avp.Code == DiameterAvpCodes.AuthSessionState ||
                    avp.Code ==
                        DiameterAvpCodes.AuthorizationLifetime ||
                    avp.Code == DiameterAvpCodes.AuthGracePeriod ||
                    avp.Code == DiameterAvpCodes.SessionTimeout ||
                    avp.Code == DiameterAvpCodes.MultiRoundTimeOut ||
                    avp.Code == DiameterAvpCodes.ReAuthRequestType) &&
                    (avp.Data == null || avp.Data.Length != 4))
                {
                    malformed = true;
                }

                if (avp.Code == DiameterAvpCodes.ResultCode)
                {
                    resultCount = resultCount + 1;
                    result.ResultCode = ReadClientUnsigned32(avp);
                }
                else if (avp.Code == DiameterAvpCodes.AuthSessionState)
                {
                    authStateCount = authStateCount + 1;
                    authState = ReadClientUnsigned32(avp);
                }
                else if (avp.Code ==
                    DiameterAvpCodes.AuthorizationLifetime)
                {
                    lifetimeCount = lifetimeCount + 1;
                    result.AuthorizationLifetimeSeconds =
                        ReadClientUnsigned32(avp);
                    result.AuthorizationLifetimePresent = true;
                }
                else if (avp.Code == DiameterAvpCodes.AuthGracePeriod)
                {
                    graceCount = graceCount + 1;
                    result.GracePeriodSeconds = ReadClientUnsigned32(avp);
                    result.GracePeriodPresent = true;
                }
                else if (avp.Code == DiameterAvpCodes.SessionTimeout)
                {
                    sessionCount = sessionCount + 1;
                    result.SessionTimeoutSeconds =
                        ReadClientUnsigned32(avp);
                    result.SessionTimeoutPresent = true;
                }
                else if (avp.Code == DiameterAvpCodes.MultiRoundTimeOut)
                {
                    multiRoundCount = multiRoundCount + 1;
                    result.MultiRoundTimeoutSeconds =
                        ReadClientUnsigned32(avp);
                    result.MultiRoundTimeoutPresent = true;
                }
                else if (avp.Code == DiameterAvpCodes.ReAuthRequestType)
                {
                    reauthCount = reauthCount + 1;
                    reauthType = ReadClientUnsigned32(avp);
                    result.ReAuthRequestTypePresent = true;
                }
            }

            index = index + 1;
        }

        if (malformed || resultCount != 1 ||
            result.ResultCode < 1000U || result.ResultCode > 5999U ||
            authStateCount > 1 ||
            lifetimeCount > 1 || graceCount > 1 || sessionCount > 1 ||
            multiRoundCount > 1 || reauthCount > 1 || authState > 1U ||
            reauthType > 1U)
        {
            result.Code = "client-authorization-avp-invalid";
            return result;
        }

        if (authStateCount == 1 &&
            authState == (uint)DiameterAuthSessionState.NoStateMaintained)
        {
            result.Code = "client-authorization-state-not-maintained";
            return result;
        }

        result.MultiRound =
            result.ResultCode == DiameterResultCodes.MultiRoundAuth;
        result.Successful = result.ResultCode >= 2000U &&
            result.ResultCode <= 2999U;
        if (result.MultiRound && result.MultiRoundTimeoutPresent &&
            result.MultiRoundTimeoutSeconds == 0U)
        {
            result.Code = "client-multi-round-timeout-invalid";
            return result;
        }

        if (result.AuthorizationLifetimePresent &&
            result.AuthorizationLifetimeSeconds > 0U &&
            result.AuthorizationLifetimeSeconds != uint.MaxValue &&
            !result.ReAuthRequestTypePresent)
        {
            result.Code = "client-reauth-request-type-missing";
            return result;
        }

        if (result.GracePeriodPresent &&
            (!result.AuthorizationLifetimePresent ||
            result.AuthorizationLifetimeSeconds == uint.MaxValue))
        {
            result.Code = "client-authorization-grace-invalid";
            return result;
        }

        if (result.SessionTimeoutPresent &&
            result.SessionTimeoutSeconds > 0U &&
            result.AuthorizationLifetimePresent &&
            result.AuthorizationLifetimeSeconds != uint.MaxValue &&
            result.SessionTimeoutSeconds <
                result.AuthorizationLifetimeSeconds)
        {
            result.Code = "client-session-timeout-invalid";
            return result;
        }

        result.ReAuthRequestType =
            (DiameterReAuthRequestType)reauthType;
        result.Valid = true;
        result.Code = "client-authorization-answer-valid";
        return result;
    }

    internal uint ReadClientUnsigned32(DiameterAvp avp)
    {
        if (avp.Data == null || avp.Data.Length != 4)
        {
            return uint.MaxValue;
        }

        return NetworkByteOrder.ReadUInt32(avp.Data);
    }

    internal void ApplyClientAuthorizationTimers(
        DiameterSessionRecord record,
        AuthorizationClientAnswerPolicy policy,
        DateTimeOffset now)
    {
        if (policy.AuthorizationLifetimePresent &&
            policy.AuthorizationLifetimeSeconds != uint.MaxValue)
        {
            record.AuthorizationDeadline = AddClientTimerSeconds(
                now,
                policy.AuthorizationLifetimeSeconds);
            record.AuthorizationDeadlinePresent = true;
            record.ReAuthRequestType = policy.ReAuthRequestType;
            if (policy.GracePeriodPresent)
            {
                record.GraceDeadline = AddClientTimerSeconds(
                    record.AuthorizationDeadline,
                    policy.GracePeriodSeconds);
                record.GraceDeadlinePresent = true;
            }
            else
            {
                record.GraceDeadlinePresent = false;
            }
        }
        else
        {
            record.AuthorizationDeadlinePresent = false;
            record.GraceDeadlinePresent = false;
        }

        if (policy.SessionTimeoutPresent &&
            policy.SessionTimeoutSeconds > 0U)
        {
            record.SessionDeadline = AddClientTimerSeconds(
                now,
                policy.SessionTimeoutSeconds);
            record.SessionDeadlinePresent = true;
        }
        else
        {
            record.SessionDeadlinePresent = false;
        }
    }

    internal AuthorizationClientTransitionResult RequireClientTermination(
        DiameterSessionRecord record,
        DiameterTerminationCause cause,
        string code)
    {
        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Terminating;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = 0;
        record.TerminationCause = cause;
        record.TerminationCausePresent = true;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        AuthorizationClientTransitionResult result =
            CreateClientTransitionResult(saved, previous, true, code);
        result.TerminationRequestRequired = true;
        result.DisconnectRequired = true;
        result.TerminationCause = cause;
        return result;
    }

    internal AuthorizationClientTransitionResult FailClientRecord(
        DiameterSessionRecord record,
        string code)
    {
        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Failed;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = 0;
        ClearClientSessionTimers(record);
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        AuthorizationClientTransitionResult result =
            CreateClientTransitionResult(saved, previous, true, code);
        result.DisconnectRequired = true;
        return result;
    }

    internal void ClearClientSessionTimers(DiameterSessionRecord record)
    {
        record.AuthorizationDeadlinePresent = false;
        record.GraceDeadlinePresent = false;
        record.SessionDeadlinePresent = false;
        record.MultiRoundDeadlinePresent = false;
    }

    internal DateTimeOffset AddClientTimerSeconds(
        DateTimeOffset value,
        uint seconds)
    {
        long ticks = (long)seconds * TimeSpan.TicksPerSecond;
        if (value.UtcTicks > DateTimeOffset.MaxValue.UtcTicks - ticks)
        {
            return DateTimeOffset.MaxValue;
        }

        return value.AddTicks(ticks);
    }

    internal uint ReadConfiguredMultiRoundSeconds(
        DiameterSessionConfiguration configuration)
    {
        int milliseconds = configuration.MultiRoundTimeoutMilliseconds;
        int seconds = milliseconds / 1000;
        if (milliseconds % 1000 != 0)
        {
            seconds = seconds + 1;
        }

        if (seconds <= 0)
        {
            return 1U;
        }

        return (uint)seconds;
    }

    internal AuthorizationClientTransitionResult CreateClientTransitionResult(
        DiameterSessionRecord record,
        DiameterSessionState previous,
        bool changed,
        string code)
    {
        AuthorizationClientTransitionResult result =
            new AuthorizationClientTransitionResult();
        result.Accepted = true;
        result.Changed = changed;
        result.Session = SessionRecordCloner.CloneSessionRecord(record);
        result.PreviousState = previous;
        result.Code = code;
        result.Message = "The stateful client session transition completed.";
        return result;
    }
}
