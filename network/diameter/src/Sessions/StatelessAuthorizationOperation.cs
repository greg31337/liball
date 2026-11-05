using System;

namespace diameter;

internal class StatelessAuthorizationResult
{
    internal bool Accepted;
    internal bool Changed;
    internal bool Completed;
    internal bool AccessGranted;
    internal bool StatefulSessionRequired;
    internal bool AuthorizationRequestRequired;
    internal bool AuthorizationAnswerRequired;
    internal bool DisconnectRequired;
    internal bool SessionDeadlinePresent;
    internal uint AnswerResultCode;
    internal DateTimeOffset SessionDeadline;
    internal DiameterAuthSessionState AuthSessionState =
        DiameterAuthSessionState.NoStateMaintained;
    internal DiameterSessionConfiguration? Configuration;
    internal DiameterMessage? Request;
    internal DiameterMessage? Answer;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class StatelessAuthorizationAnswerPolicy
{
    internal bool Valid;
    internal bool Successful;
    internal bool MultiRound;
    internal bool AuthSessionStatePresent;
    internal bool SessionTimeoutPresent;
    internal uint ResultCode;
    internal uint AuthSessionState;
    internal uint SessionTimeoutSeconds;
    internal string Code = string.Empty;
}

internal class StatelessAuthorizationOperation
{
    internal SessionTable Table;
    internal IDiameterClock Clock;

    internal StatelessAuthorizationOperation(
        SessionTable table,
        IDiameterClock clock)
    {
        if (table == null || clock == null)
        {
            throw new ArgumentException(
                "A stateless session table and clock are required.");
        }

        Table = table;
        Clock = clock;
    }

    internal StatelessAuthorizationResult ExecuteStatelessClientAuthorization(
        DiameterSessionConfiguration? configuration,
        DiameterMessage? request,
        DiameterMessage? answer)
    {
        DiameterSessionConfiguration ownedConfiguration =
            ValidateStatelessConfiguration(
                configuration,
                DiameterSessionRole.Client);
        EnsureStatelessSessionIsAbsent(ownedConfiguration.SessionId);
        ValidateStatelessAuthorizationRequest(
            ownedConfiguration,
            request);
        if (request == null)
        {
            throw new ArgumentException(
                "A stateless authorization request is required.");
        }

        string correlationFailure = ValidateStatelessAnswerCorrelation(
            ownedConfiguration.SessionId,
            request,
            answer);
        if (correlationFailure.Length > 0 || answer == null)
        {
            StatelessAuthorizationResult invalid =
                CreateStatelessResult(
                    ownedConfiguration,
                    request,
                    answer,
                    "stateless-client-answer-correlation-invalid");
            invalid.Completed = true;
            invalid.DisconnectRequired = true;
            return invalid;
        }

        StatelessAuthorizationAnswerPolicy policy =
            ReadStatelessAnswerPolicy(answer);
        StatelessAuthorizationResult result = CreateStatelessResult(
            ownedConfiguration,
            request,
            answer,
            policy.Code);
        result.Completed = true;
        result.AnswerResultCode = policy.ResultCode;
        if (!policy.Valid)
        {
            result.DisconnectRequired = true;
            return result;
        }

        result.Accepted = true;
        if (policy.MultiRound)
        {
            result.Code = "stateless-client-multi-round-not-supported";
            result.Message =
                "A stateless authorization operation cannot retain a multi-round exchange.";
            return result;
        }

        if (!policy.Successful)
        {
            result.Code = "stateless-client-authorization-rejected";
            result.Message =
                "The stateless authorization request was rejected.";
            return result;
        }

        result.AccessGranted = true;
        if (!policy.AuthSessionStatePresent ||
            policy.AuthSessionState ==
                (uint)DiameterAuthSessionState.StateMaintained)
        {
            result.StatefulSessionRequired = true;
            result.AuthSessionState =
                DiameterAuthSessionState.StateMaintained;
            result.Code = "stateless-client-stateful-session-required";
            result.Message =
                "The server requires state to be maintained for the authorized session.";
        }
        else
        {
            result.Code = "stateless-client-authorization-succeeded";
            result.Message =
                "The stateless authorization operation completed successfully.";
        }

        if (policy.SessionTimeoutPresent &&
            policy.SessionTimeoutSeconds > 0U)
        {
            result.SessionDeadline = AddStatelessTimerSeconds(
                Clock.ReadUtcTime(),
                policy.SessionTimeoutSeconds);
            result.SessionDeadlinePresent = true;
        }

        return result;
    }

    internal StatelessAuthorizationResult ExecuteStatelessServerAuthorization(
        DiameterSessionConfiguration? configuration,
        DiameterMessage? request,
        uint resultCode)
    {
        DiameterSessionConfiguration ownedConfiguration =
            ValidateStatelessConfiguration(
                configuration,
                DiameterSessionRole.Server);
        EnsureStatelessSessionIsAbsent(ownedConfiguration.SessionId);
        ValidateStatelessAuthorizationRequest(
            ownedConfiguration,
            request);
        if (request == null)
        {
            throw new ArgumentException(
                "A stateless authorization request is required.");
        }

        if (resultCode < 1000U || resultCode > 5999U)
        {
            throw new ArgumentOutOfRangeException("resultCode");
        }

        if (resultCode == DiameterResultCodes.MultiRoundAuth)
        {
            throw new ArgumentException(
                "A stateless server cannot retain a multi-round exchange.");
        }

        StatelessAuthorizationResult result = CreateStatelessResult(
            ownedConfiguration,
            request,
            null,
            "stateless-server-authorization-completed");
        result.Accepted = true;
        result.Completed = true;
        result.AuthorizationAnswerRequired = true;
        result.AnswerResultCode = resultCode;
        result.AuthSessionState =
            DiameterAuthSessionState.NoStateMaintained;
        result.AccessGranted = resultCode >= 2000U && resultCode <= 2999U;
        result.Message =
            "The stateless server authorization operation completed without retained state.";
        return result;
    }

    internal StatelessAuthorizationResult EvaluateStatelessClientAccess(
        StatelessAuthorizationResult? current)
    {
        ValidateStatelessAccessResult(current);
        if (current == null)
        {
            throw new ArgumentException(
                "A stateless client authorization result is required.");
        }

        StatelessAuthorizationResult result =
            CloneStatelessResult(current);
        result.Changed = false;
        if (!current.SessionDeadlinePresent ||
            current.SessionDeadline > Clock.ReadUtcTime())
        {
            result.Code = "stateless-client-access-timer-not-due";
            result.Message =
                "The stateless client access timeout has not expired.";
            return result;
        }

        result.Changed = true;
        result.AccessGranted = false;
        result.DisconnectRequired = true;
        result.SessionDeadlinePresent = false;
        result.Code = "stateless-client-session-timeout-expired";
        result.Message =
            "The stateless client access timeout expired.";
        return result;
    }

    internal StatelessAuthorizationResult EndStatelessClientAccess(
        StatelessAuthorizationResult? current)
    {
        ValidateStatelessAccessResult(current);
        if (current == null)
        {
            throw new ArgumentException(
                "A stateless client authorization result is required.");
        }

        StatelessAuthorizationResult result =
            CloneStatelessResult(current);
        result.Changed = current.AccessGranted;
        result.AccessGranted = false;
        result.DisconnectRequired = current.AccessGranted;
        result.SessionDeadlinePresent = false;
        result.Code = "stateless-client-service-ended";
        result.Message =
            "The locally authorized stateless service ended.";
        return result;
    }

    internal DiameterSessionConfiguration ValidateStatelessConfiguration(
        DiameterSessionConfiguration? configuration,
        DiameterSessionRole role)
    {
        if (configuration == null)
        {
            throw new ArgumentException(
                "A stateless authorization configuration is required.");
        }

        configuration.ValidateSessionConfiguration();
        if (configuration.Kind !=
                DiameterSessionKind.StatelessAuthorization ||
            configuration.Role != role ||
            configuration.AuthSessionState !=
                DiameterAuthSessionState.NoStateMaintained ||
            configuration.SessionId.Length == 0)
        {
            throw new ArgumentException(
                "The stateless authorization configuration has the wrong role or mode.");
        }

        return SessionRecordCloner.CloneSessionConfiguration(configuration);
    }

    internal void EnsureStatelessSessionIsAbsent(string sessionId)
    {
        if (Table.ReadSessionRecord(sessionId) != null)
        {
            throw new DiameterSessionException(
                "A stateless operation cannot reuse a retained Session-Id.");
        }
    }

    internal void ValidateStatelessAuthorizationRequest(
        DiameterSessionConfiguration configuration,
        DiameterMessage? request)
    {
        if (request == null || request.Avps == null ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            request.CommandCode == 0U ||
            request.ApplicationId != configuration.ApplicationId ||
            !ContainsStatelessSessionIdentifier(
                request,
                configuration.SessionId) ||
            !ContainsValidStatelessRequestState(request))
        {
            throw new ArgumentException(
                "A valid stateless application authorization request is required.");
        }
    }

    internal string ValidateStatelessAnswerCorrelation(
        string sessionId,
        DiameterMessage request,
        DiameterMessage? answer)
    {
        if (answer == null || answer.Avps == null ||
            (answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            answer.CommandCode != request.CommandCode ||
            answer.ApplicationId != request.ApplicationId ||
            answer.HopByHopIdentifier != request.HopByHopIdentifier ||
            answer.EndToEndIdentifier != request.EndToEndIdentifier ||
            !ContainsStatelessSessionIdentifier(answer, sessionId))
        {
            return "stateless-answer-correlation-invalid";
        }

        return string.Empty;
    }

    internal bool ContainsStatelessSessionIdentifier(
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

    internal bool ContainsValidStatelessRequestState(
        DiameterMessage message)
    {
        int count = 0;
        uint value = (uint)DiameterAuthSessionState.NoStateMaintained;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.AuthSessionState)
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

        return count <= 1 && value ==
            (uint)DiameterAuthSessionState.NoStateMaintained;
    }

    internal StatelessAuthorizationAnswerPolicy ReadStatelessAnswerPolicy(
        DiameterMessage answer)
    {
        StatelessAuthorizationAnswerPolicy result =
            new StatelessAuthorizationAnswerPolicy();
        int resultCount = 0;
        int stateCount = 0;
        int timeoutCount = 0;
        bool malformed = false;
        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf)
            {
                if ((avp.Code == DiameterAvpCodes.ResultCode ||
                    avp.Code == DiameterAvpCodes.AuthSessionState ||
                    avp.Code == DiameterAvpCodes.SessionTimeout) &&
                    (avp.Data == null || avp.Data.Length != 4))
                {
                    malformed = true;
                }

                if (avp.Code == DiameterAvpCodes.ResultCode)
                {
                    resultCount = resultCount + 1;
                    result.ResultCode = ReadStatelessUnsigned32(avp);
                }
                else if (avp.Code == DiameterAvpCodes.AuthSessionState)
                {
                    stateCount = stateCount + 1;
                    result.AuthSessionState =
                        ReadStatelessUnsigned32(avp);
                    result.AuthSessionStatePresent = true;
                }
                else if (avp.Code == DiameterAvpCodes.SessionTimeout)
                {
                    timeoutCount = timeoutCount + 1;
                    result.SessionTimeoutSeconds =
                        ReadStatelessUnsigned32(avp);
                    result.SessionTimeoutPresent = true;
                }
            }

            index = index + 1;
        }

        if (malformed || resultCount != 1 || stateCount > 1 ||
            timeoutCount > 1 || result.ResultCode < 1000U ||
            result.ResultCode > 5999U || result.AuthSessionState > 1U)
        {
            result.Code = "stateless-client-authorization-avp-invalid";
            return result;
        }

        result.Valid = true;
        result.MultiRound =
            result.ResultCode == DiameterResultCodes.MultiRoundAuth;
        result.Successful = result.ResultCode >= 2000U &&
            result.ResultCode <= 2999U;
        result.Code = "stateless-client-authorization-answer-valid";
        return result;
    }

    internal uint ReadStatelessUnsigned32(DiameterAvp avp)
    {
        if (avp.Data == null || avp.Data.Length != 4)
        {
            return uint.MaxValue;
        }

        return NetworkByteOrder.ReadUInt32(avp.Data);
    }

    internal void ValidateStatelessAccessResult(
        StatelessAuthorizationResult? current)
    {
        if (current == null || !current.Completed ||
            !current.Accepted || !current.AccessGranted ||
            current.StatefulSessionRequired ||
            current.Configuration == null ||
            current.Configuration.Kind !=
                DiameterSessionKind.StatelessAuthorization ||
            current.Configuration.Role != DiameterSessionRole.Client)
        {
            throw new ArgumentException(
                "An active stateless client authorization result is required.");
        }

        EnsureStatelessSessionIsAbsent(current.Configuration.SessionId);
    }

    internal DateTimeOffset AddStatelessTimerSeconds(
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

    internal StatelessAuthorizationResult CreateStatelessResult(
        DiameterSessionConfiguration configuration,
        DiameterMessage? request,
        DiameterMessage? answer,
        string code)
    {
        StatelessAuthorizationResult result =
            new StatelessAuthorizationResult();
        result.Changed = true;
        result.Configuration =
            SessionRecordCloner.CloneSessionConfiguration(configuration);
        result.Request =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        result.Answer =
            SessionRecordCloner.CloneOptionalSessionMessage(answer);
        result.Code = code;
        result.Message =
            "The stateless authorization operation completed.";
        return result;
    }

    internal StatelessAuthorizationResult CloneStatelessResult(
        StatelessAuthorizationResult source)
    {
        StatelessAuthorizationResult result =
            new StatelessAuthorizationResult();
        result.Accepted = source.Accepted;
        result.Changed = source.Changed;
        result.Completed = source.Completed;
        result.AccessGranted = source.AccessGranted;
        result.StatefulSessionRequired = source.StatefulSessionRequired;
        result.AuthorizationRequestRequired =
            source.AuthorizationRequestRequired;
        result.AuthorizationAnswerRequired =
            source.AuthorizationAnswerRequired;
        result.DisconnectRequired = source.DisconnectRequired;
        result.SessionDeadlinePresent = source.SessionDeadlinePresent;
        result.AnswerResultCode = source.AnswerResultCode;
        result.SessionDeadline = source.SessionDeadline;
        result.AuthSessionState = source.AuthSessionState;
        if (source.Configuration != null)
        {
            result.Configuration =
                SessionRecordCloner.CloneSessionConfiguration(
                    source.Configuration);
        }

        result.Request =
            SessionRecordCloner.CloneOptionalSessionMessage(source.Request);
        result.Answer =
            SessionRecordCloner.CloneOptionalSessionMessage(source.Answer);
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }
}
