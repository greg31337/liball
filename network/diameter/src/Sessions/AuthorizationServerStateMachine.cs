using System;

namespace diameter;

internal class AuthorizationServerTransitionResult
{
    internal bool Accepted;
    internal bool Changed;
    internal bool AuthorizationAnswerRequired;
    internal bool ReAuthRequestRequired;
    internal bool AbortRequestRequired;
    internal bool AbortRequestRetryRequired;
    internal bool TerminationAnswerRequired;
    internal bool CleanedUp;
    internal uint AnswerResultCode;
    internal DiameterReAuthRequestType ReAuthRequestType;
    internal DiameterTerminationCause TerminationCause;
    internal DiameterSessionState PreviousState;
    internal DiameterSessionRecord? Session;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class AuthorizationServerAnswerResult
{
    internal bool Valid;
    internal uint ResultCode;
}

internal class AuthorizationServerStateMachine
{
    internal SessionTable Table;
    internal IDiameterClock Clock;

    internal AuthorizationServerStateMachine(
        SessionTable table,
        IDiameterClock clock)
    {
        if (table == null || clock == null)
        {
            throw new ArgumentException(
                "A server session table and clock are required.");
        }

        Table = table;
        Clock = clock;
    }

    internal AuthorizationServerTransitionResult CreateStatefulServerSession(
        DiameterSessionConfiguration? configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentException(
                "A server session configuration is required.");
        }

        configuration.ValidateSessionConfiguration();
        if (configuration.Kind !=
                DiameterSessionKind.StatefulAuthorization ||
            configuration.Role != DiameterSessionRole.Server ||
            configuration.SessionId.Length == 0)
        {
            throw new ArgumentException(
                "A stateful server session with a Session-Id is required.");
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
        return CreateServerTransitionResult(
            saved,
            DiameterSessionState.Created,
            true,
            "server-session-created");
    }

    internal AuthorizationServerTransitionResult ProcessServerAuthorizationRequest(
        string? sessionId,
        DiameterMessage? request,
        uint resultCode)
    {
        DiameterSessionRecord record = ReadRequiredServerSession(sessionId);
        if (record.State != DiameterSessionState.Created &&
            record.State != DiameterSessionState.Pending &&
            record.State != DiameterSessionState.Open)
        {
            throw new DiameterSessionException(
                "The server session cannot process authorization now.");
        }

        if (record.State == DiameterSessionState.Pending &&
            (!record.MultiRoundDeadlinePresent ||
            record.MultiRoundDeadline <= Clock.ReadUtcTime()))
        {
            throw new DiameterSessionException(
                "The multi-round authorization exchange has expired.");
        }

        ValidateServerAuthorizationRequest(record, request);
        if (resultCode < 1000U || resultCode > 5999U)
        {
            throw new ArgumentOutOfRangeException("resultCode");
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        record.LastAnswer = null;
        record.PendingRequestIdentifier = 0;
        record.UpdatedAt = now;
        if (resultCode == DiameterResultCodes.MultiRoundAuth)
        {
            record.State = DiameterSessionState.Pending;
            if (previous != DiameterSessionState.Pending)
            {
                record.StateChangedAt = now;
            }

            record.MultiRoundDeadline = AddServerTimerMilliseconds(
                now,
                record.Configuration.MultiRoundTimeoutMilliseconds);
            record.MultiRoundDeadlinePresent = true;
            DiameterSessionRecord challenged = Table.SaveSessionRecord(
                record,
                record.Version);
            AuthorizationServerTransitionResult challenge =
                CreateServerTransitionResult(
                    challenged,
                    previous,
                    true,
                    "server-multi-round-required");
            challenge.AuthorizationAnswerRequired = true;
            challenge.AnswerResultCode = resultCode;
            return challenge;
        }

        if (resultCode >= 2000U && resultCode <= 2999U)
        {
            record.State = DiameterSessionState.Open;
            if (previous != DiameterSessionState.Open)
            {
                record.StateChangedAt = now;
            }

            record.InitialAuthorizationCompleted = true;
            record.ReauthorizationRequested = false;
            record.MultiRoundDeadlinePresent = false;
            ApplyServerAuthorizationTimers(record, now);
            DiameterSessionRecord opened = Table.SaveSessionRecord(
                record,
                record.Version);
            AuthorizationServerTransitionResult authorized =
                CreateServerTransitionResult(
                    opened,
                    previous,
                    true,
                    "server-authorization-succeeded");
            authorized.AuthorizationAnswerRequired = true;
            authorized.AnswerResultCode = resultCode;
            return authorized;
        }

        AuthorizationServerTransitionResult denied = CleanupServerRecord(
            record,
            DiameterTerminationCause.ServiceNotProvided,
            true,
            "server-authorization-denied");
        denied.AuthorizationAnswerRequired = true;
        denied.AnswerResultCode = resultCode;
        return denied;
    }

    internal AuthorizationServerTransitionResult BeginServerReauthorization(
        string? sessionId,
        DiameterMessage? request,
        long pendingRequestIdentifier)
    {
        DiameterSessionRecord record = ReadRequiredServerSession(sessionId);
        RequireServerState(record, DiameterSessionState.Open);
        ValidateServerBaseRequest(
            record,
            request,
            DiameterCommandCodes.ReAuth,
            pendingRequestIdentifier);
        if (request == null || !ContainsServerReAuthRequestType(request))
        {
            throw new ArgumentException(
                "A Re-Auth request requires one valid request type.");
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Reauthorizing;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = pendingRequestIdentifier;
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        record.LastAnswer = null;
        record.ReauthorizationRequested = true;
        record.ReAuthRequestType = ReadServerReAuthRequestType(request);
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        AuthorizationServerTransitionResult result =
            CreateServerTransitionResult(
                saved,
                previous,
                true,
                "server-reauth-request-pending");
        result.ReAuthRequestRequired = true;
        result.ReAuthRequestType = saved.ReAuthRequestType;
        return result;
    }

    internal AuthorizationServerTransitionResult ProcessServerReAuthAnswer(
        string? sessionId,
        DiameterMessage? answer)
    {
        DiameterSessionRecord record = ReadRequiredServerSession(sessionId);
        RequireServerState(record, DiameterSessionState.Reauthorizing);
        if (ValidateServerAnswerCorrelation(
            record,
            answer,
            DiameterCommandCodes.ReAuth).Length > 0 || answer == null)
        {
            return CleanupServerRecord(
                record,
                DiameterTerminationCause.BadAnswer,
                true,
                "server-reauth-answer-invalid");
        }

        AuthorizationServerAnswerResult answerResult =
            ReadServerAnswerResult(answer);
        if (!answerResult.Valid ||
            answerResult.ResultCode != DiameterResultCodes.Success)
        {
            record.LastAnswer =
                SessionRecordCloner.CloneOptionalSessionMessage(answer);
            return CleanupServerRecord(
                record,
                DiameterTerminationCause.ServiceNotProvided,
                true,
                "server-reauth-failed");
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Open;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = 0;
        record.LastAnswer =
            SessionRecordCloner.CloneOptionalSessionMessage(answer);
        record.ReauthorizationRequested = false;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        return CreateServerTransitionResult(
            saved,
            previous,
            true,
            "server-reauth-succeeded");
    }

    internal AuthorizationServerTransitionResult BeginServerAbort(
        string? sessionId,
        DiameterMessage? request,
        long pendingRequestIdentifier)
    {
        DiameterSessionRecord record = ReadRequiredServerSession(sessionId);
        RequireServerState(record, DiameterSessionState.Open);
        ValidateServerBaseRequest(
            record,
            request,
            DiameterCommandCodes.AbortSession,
            pendingRequestIdentifier);
        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Aborting;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = pendingRequestIdentifier;
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        record.LastAnswer = null;
        record.ReleaseRequested = true;
        record.TerminationCause = DiameterTerminationCause.Administrative;
        record.TerminationCausePresent = true;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        AuthorizationServerTransitionResult result =
            CreateServerTransitionResult(
                saved,
                previous,
                true,
                "server-abort-request-pending");
        result.AbortRequestRequired = true;
        result.TerminationCause = DiameterTerminationCause.Administrative;
        return result;
    }

    internal AuthorizationServerTransitionResult FailServerRequestDelivery(
        string? sessionId)
    {
        DiameterSessionRecord record = ReadRequiredServerSession(sessionId);
        if (record.State == DiameterSessionState.Reauthorizing)
        {
            return CleanupServerRecord(
                record,
                DiameterTerminationCause.LinkBroken,
                true,
                "server-reauth-delivery-failed");
        }

        if (record.State != DiameterSessionState.Aborting)
        {
            throw new DiameterSessionException(
                "No server-initiated request is pending delivery.");
        }

        AuthorizationServerTransitionResult retry =
            CreateServerTransitionResult(
                record,
                record.State,
                false,
                "server-abort-retry-required");
        retry.AbortRequestRequired = true;
        retry.AbortRequestRetryRequired = true;
        retry.TerminationCause = DiameterTerminationCause.Administrative;
        return retry;
    }

    internal AuthorizationServerTransitionResult ProcessServerAbortAnswer(
        string? sessionId,
        DiameterMessage? answer)
    {
        DiameterSessionRecord record = ReadRequiredServerSession(sessionId);
        if (record.State != DiameterSessionState.Aborting)
        {
            ValidateIncomingServerBaseAnswer(
                record,
                answer,
                DiameterCommandCodes.AbortSession);
            return CreateServerTransitionResult(
                record,
                record.State,
                false,
                "server-unexpected-abort-answer-ignored");
        }

        if (ValidateServerAnswerCorrelation(
            record,
            answer,
            DiameterCommandCodes.AbortSession).Length > 0 ||
            answer == null)
        {
            throw new ArgumentException(
                "A correlated Abort-Session answer is required.");
        }

        AuthorizationServerAnswerResult answerResult =
            ReadServerAnswerResult(answer);
        if (!answerResult.Valid)
        {
            throw new ArgumentException(
                "An Abort-Session answer requires one Result-Code.");
        }

        record.LastAnswer =
            SessionRecordCloner.CloneOptionalSessionMessage(answer);
        return CleanupServerRecord(
            record,
            DiameterTerminationCause.Administrative,
            true,
            "server-abort-completed");
    }

    internal AuthorizationServerTransitionResult ProcessServerTerminationRequest(
        string? sessionId,
        DiameterMessage? request)
    {
        string key = ValidateServerSessionIdentifier(sessionId);
        DiameterSessionRecord? record = Table.ReadSessionRecord(key);
        if (record == null)
        {
            ValidateIncomingServerBaseRequest(
                key,
                request,
                DiameterCommandCodes.SessionTermination);
            AuthorizationServerTransitionResult unknown =
                new AuthorizationServerTransitionResult();
            unknown.Accepted = true;
            unknown.TerminationAnswerRequired = true;
            unknown.AnswerResultCode = DiameterResultCodes.UnknownSessionId;
            unknown.PreviousState = DiameterSessionState.Closed;
            unknown.Code = "server-termination-session-unknown";
            unknown.Message =
                "The termination request did not match a server session.";
            return unknown;
        }

        if (record.Configuration.Kind !=
                DiameterSessionKind.StatefulAuthorization ||
            record.Configuration.Role != DiameterSessionRole.Server)
        {
            throw new DiameterSessionException(
                "A stateful server session was not found.");
        }

        ValidateIncomingServerBaseRequest(
            record.SessionId,
            request,
            DiameterCommandCodes.SessionTermination);
        record.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        AuthorizationServerTransitionResult result = CleanupServerRecord(
            record,
            DiameterTerminationCause.Logout,
            true,
            "server-termination-completed");
        result.TerminationAnswerRequired = true;
        result.AnswerResultCode = DiameterResultCodes.Success;
        return result;
    }

    internal AuthorizationServerTransitionResult EvaluateServerAuthorizationTimers(
        string? sessionId)
    {
        DiameterSessionRecord record = ReadRequiredServerSession(sessionId);
        DateTimeOffset now = Clock.ReadUtcTime();
        if (record.State == DiameterSessionState.Pending &&
            record.MultiRoundDeadlinePresent &&
            record.MultiRoundDeadline <= now)
        {
            return CleanupServerRecord(
                record,
                DiameterTerminationCause.ServiceNotProvided,
                true,
                "server-multi-round-timeout-expired");
        }

        bool active = record.State == DiameterSessionState.Open ||
            record.State == DiameterSessionState.Reauthorizing;
        bool sessionExpired = active && record.SessionDeadlinePresent &&
            record.SessionDeadline <= now;
        bool authorizationExpired = active &&
            IsServerAuthorizationExpired(record, now);
        if (authorizationExpired &&
            (!sessionExpired || IsServerAuthorizationDeadlineFirst(record)))
        {
            return CleanupServerRecord(
                record,
                DiameterTerminationCause.AuthExpired,
                true,
                "server-authorization-timeout-expired");
        }

        if (sessionExpired)
        {
            return CleanupServerRecord(
                record,
                DiameterTerminationCause.SessionTimeout,
                true,
                "server-session-timeout-expired");
        }

        return CreateServerTransitionResult(
            record,
            record.State,
            false,
            "server-timer-not-due");
    }

    internal DiameterSessionRecord ReadRequiredServerSession(
        string? sessionId)
    {
        DiameterSessionRecord? record = Table.ReadSessionRecord(sessionId);
        if (record == null ||
            record.Configuration.Kind !=
                DiameterSessionKind.StatefulAuthorization ||
            record.Configuration.Role != DiameterSessionRole.Server)
        {
            throw new DiameterSessionException(
                "A stateful server session was not found.");
        }

        return record;
    }

    internal void RequireServerState(
        DiameterSessionRecord record,
        DiameterSessionState state)
    {
        if (record.State != state)
        {
            throw new DiameterSessionException(
                "The server session state does not allow this transition.");
        }
    }

    internal void ValidateServerAuthorizationRequest(
        DiameterSessionRecord record,
        DiameterMessage? request)
    {
        if (request == null ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            request.ApplicationId != record.Configuration.ApplicationId ||
            request.CommandCode == 0U || request.Avps == null ||
            !ContainsServerSessionIdentifier(request, record.SessionId))
        {
            throw new ArgumentException(
                "A correlated application authorization request is required.");
        }
    }

    internal void ValidateServerBaseRequest(
        DiameterSessionRecord record,
        DiameterMessage? request,
        uint commandCode,
        long pendingRequestIdentifier)
    {
        if (request == null || pendingRequestIdentifier <= 0 ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            request.ApplicationId != DiameterApplicationIds.Common &&
            (commandCode != DiameterCommandCodes.ReAuth ||
            request.ApplicationId != record.Configuration.ApplicationId) ||
            request.CommandCode != commandCode || request.Avps == null ||
            !ContainsServerSessionIdentifier(request, record.SessionId))
        {
            throw new ArgumentException(
                "A correlated base session request is required.");
        }
    }

    internal void ValidateIncomingServerBaseRequest(
        string sessionId,
        DiameterMessage? request,
        uint commandCode)
    {
        if (request == null ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            request.ApplicationId != DiameterApplicationIds.Common ||
            request.CommandCode != commandCode || request.Avps == null ||
            !ContainsServerSessionIdentifier(request, sessionId))
        {
            throw new ArgumentException(
                "A correlated inbound base session request is required.");
        }
    }

    internal void ValidateIncomingServerBaseAnswer(
        DiameterSessionRecord record,
        DiameterMessage? answer,
        uint commandCode)
    {
        if (answer == null || answer.Avps == null ||
            (answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            answer.ApplicationId != DiameterApplicationIds.Common ||
            answer.CommandCode != commandCode ||
            !ContainsServerSessionIdentifier(answer, record.SessionId))
        {
            throw new ArgumentException(
                "A correlated inbound base session answer is required.");
        }
    }

    internal string ValidateServerAnswerCorrelation(
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
            answer.CommandCode != requiredCommandCode ||
            !ContainsServerSessionIdentifier(answer, record.SessionId))
        {
            return "server-session-answer-correlation-invalid";
        }

        return string.Empty;
    }

    internal bool ContainsServerSessionIdentifier(
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

    internal bool ContainsServerReAuthRequestType(DiameterMessage message)
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

    internal DiameterReAuthRequestType ReadServerReAuthRequestType(
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

    internal AuthorizationServerAnswerResult ReadServerAnswerResult(
        DiameterMessage answer)
    {
        AuthorizationServerAnswerResult result =
            new AuthorizationServerAnswerResult();
        int count = 0;
        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ResultCode)
            {
                count = count + 1;
                if (avp.Data == null || avp.Data.Length != 4)
                {
                    return result;
                }

                result.ResultCode = NetworkByteOrder.ReadUInt32(avp.Data);
            }

            index = index + 1;
        }

        result.Valid = count == 1 && result.ResultCode >= 1000U &&
            result.ResultCode <= 5999U;
        return result;
    }

    internal void ApplyServerAuthorizationTimers(
        DiameterSessionRecord record,
        DateTimeOffset now)
    {
        int lifetime =
            record.Configuration.AuthorizationLifetimeMilliseconds;
        int grace = record.Configuration.AuthGracePeriodMilliseconds;
        if (lifetime > 0)
        {
            record.AuthorizationDeadline = AddServerTimerMilliseconds(
                now,
                lifetime);
            record.AuthorizationDeadlinePresent = true;
            if (grace > 0)
            {
                record.GraceDeadline = AddServerTimerMilliseconds(
                    record.AuthorizationDeadline,
                    grace);
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

        int timeout = record.Configuration.SessionTimeoutMilliseconds;
        if (timeout > 0)
        {
            record.SessionDeadline = AddServerTimerMilliseconds(
                now,
                timeout);
            record.SessionDeadlinePresent = true;
        }
        else
        {
            record.SessionDeadlinePresent = false;
        }
    }

    internal bool IsServerAuthorizationExpired(
        DiameterSessionRecord record,
        DateTimeOffset now)
    {
        if (record.GraceDeadlinePresent)
        {
            return record.GraceDeadline <= now;
        }

        return record.AuthorizationDeadlinePresent &&
            record.AuthorizationDeadline <= now;
    }

    internal bool IsServerAuthorizationDeadlineFirst(
        DiameterSessionRecord record)
    {
        if (record.GraceDeadlinePresent)
        {
            return record.GraceDeadline <= record.SessionDeadline;
        }

        return record.AuthorizationDeadline <= record.SessionDeadline;
    }

    internal AuthorizationServerTransitionResult CleanupServerRecord(
        DiameterSessionRecord record,
        DiameterTerminationCause cause,
        bool causePresent,
        string code)
    {
        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.State = DiameterSessionState.Closed;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = 0;
        record.TerminationCause = cause;
        record.TerminationCausePresent = causePresent;
        record.ReauthorizationRequested = false;
        record.ReleaseRequested = true;
        ClearServerSessionTimers(record);
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        if (!Table.RemoveSessionRecord(saved.SessionId, saved.Version))
        {
            throw new DiameterSessionException(
                "The server session could not be cleaned up.");
        }

        AuthorizationServerTransitionResult result =
            CreateServerTransitionResult(saved, previous, true, code);
        result.CleanedUp = true;
        result.TerminationCause = cause;
        return result;
    }

    internal void ClearServerSessionTimers(DiameterSessionRecord record)
    {
        record.AuthorizationDeadlinePresent = false;
        record.GraceDeadlinePresent = false;
        record.SessionDeadlinePresent = false;
        record.MultiRoundDeadlinePresent = false;
    }

    internal DateTimeOffset AddServerTimerMilliseconds(
        DateTimeOffset value,
        int milliseconds)
    {
        long ticks = (long)milliseconds * TimeSpan.TicksPerMillisecond;
        if (value.UtcTicks > DateTimeOffset.MaxValue.UtcTicks - ticks)
        {
            return DateTimeOffset.MaxValue;
        }

        return value.AddTicks(ticks);
    }

    internal string ValidateServerSessionIdentifier(string? sessionId)
    {
        if (sessionId == null || sessionId.Length == 0 ||
            ValueCodecs.FindInvalidUtf16Index(sessionId) >= 0)
        {
            throw new ArgumentException("A valid Session-Id is required.");
        }

        return sessionId;
    }

    internal AuthorizationServerTransitionResult CreateServerTransitionResult(
        DiameterSessionRecord record,
        DiameterSessionState previous,
        bool changed,
        string code)
    {
        AuthorizationServerTransitionResult result =
            new AuthorizationServerTransitionResult();
        result.Accepted = true;
        result.Changed = changed;
        result.Session = SessionRecordCloner.CloneSessionRecord(record);
        result.PreviousState = previous;
        result.Code = code;
        result.Message = "The stateful server session transition completed.";
        return result;
    }
}
