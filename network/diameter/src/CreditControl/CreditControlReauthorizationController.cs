using System;

namespace diameter;

internal class CreditControlReauthorizationResult
{
    internal DiameterCreditControlSession? Session;
    internal bool ReAuthRequestRequired;
    internal bool ReAuthAnswerRequired;
    internal bool CreditControlUpdateRequired;
    internal uint AnswerResultCode;
    internal string Code = string.Empty;
}

internal class CreditControlReAuthScope
{
    internal ulong CreditControlSubSessionId;
    internal bool CreditControlSubSessionIdPresent;
    internal uint GsuPoolIdentifier;
    internal bool GsuPoolIdentifierPresent;
    internal uint ServiceIdentifier;
    internal bool ServiceIdentifierPresent;
    internal uint RatingGroup;
    internal bool RatingGroupPresent;
    internal uint AuthApplicationId;
    internal bool AuthApplicationIdPresent;
}

internal static class CreditControlReauthorizationController
{
    internal static CreditControlReauthorizationResult
        BeginCreditControlServerReauthorization(
            ICreditControlStore store,
            AuthorizationServerStateMachine authorization,
            string? sessionId,
            DiameterMessage? request,
            long pendingRequestIdentifier)
    {
        DiameterCreditControlSession session =
            ReadRequiredCreditControlReAuthSession(
                store,
                sessionId,
                DiameterSessionRole.Server);
        ValidateCreditControlReAuthScope(session, request);
        AuthorizationServerTransitionResult transition = authorization
            .BeginServerReauthorization(
                sessionId,
                request,
                pendingRequestIdentifier);
        session.ReauthorizationRequested = true;
        session.ReAuthRequestType = transition.ReAuthRequestType;
        session.UpdatedAt = authorization.Clock.ReadUtcTime();
        session.Code = "credit-control-reauth-request-pending";
        session.Message =
            "A server-initiated credit re-authorization is awaiting RAA.";
        DiameterCreditControlSession saved =
            SaveCreditControlReAuthSession(store, session);
        CreditControlReauthorizationResult result =
            new CreditControlReauthorizationResult();
        result.Session = saved;
        result.ReAuthRequestRequired = true;
        result.Code = session.Code;
        return result;
    }

    internal static CreditControlReauthorizationResult
        ProcessCreditControlServerReAuthAnswer(
            ICreditControlStore store,
            AuthorizationServerStateMachine authorization,
            string? sessionId,
            DiameterMessage? answer)
    {
        DiameterCreditControlSession session =
            ReadRequiredCreditControlReAuthSession(
                store,
                sessionId,
                DiameterSessionRole.Server);
        AuthorizationServerTransitionResult transition = authorization
            .ProcessServerReAuthAnswer(sessionId, answer);
        bool successful = !transition.CleanedUp &&
            transition.Session != null &&
            transition.Session.State == DiameterSessionState.Open;
        if (successful)
        {
            session.Code = "credit-control-reauth-update-required";
            session.Message =
                "RAA succeeded and the server is awaiting a CCR UPDATE.";
        }
        else
        {
            session.State = DiameterCreditControlState.Failed;
            session.ReauthorizationRequested = false;
            session.Code = "credit-control-reauth-failed";
            session.Message =
                "The server-initiated credit re-authorization failed.";
        }

        session.UpdatedAt = authorization.Clock.ReadUtcTime();
        DiameterCreditControlSession saved =
            SaveCreditControlReAuthSession(store, session);
        CreditControlReauthorizationResult result =
            new CreditControlReauthorizationResult();
        result.Session = saved;
        result.CreditControlUpdateRequired = successful;
        result.Code = session.Code;
        return result;
    }

    internal static CreditControlReauthorizationResult
        ProcessCreditControlClientReAuthRequest(
            ICreditControlStore store,
            AuthorizationClientStateMachine authorization,
            string? sessionId,
            DiameterMessage? request,
            bool performReauthorization)
    {
        DiameterCreditControlSession session =
            ReadRequiredCreditControlReAuthSession(
                store,
                sessionId,
                DiameterSessionRole.Client);
        ValidateCreditControlReAuthScope(session, request);
        AuthorizationClientTransitionResult transition = authorization
            .ProcessClientReAuthRequest(
                sessionId,
                request,
                performReauthorization);
        bool accepted = transition.AnswerResultCode ==
            DiameterResultCodes.Success;
        session.ReauthorizationRequested = accepted;
        session.ReAuthRequestType = transition.ReAuthRequestType;
        if (accepted)
        {
            session.Code = "credit-control-reauth-update-required";
            session.Message =
                "The credit-control client must send a CCR UPDATE.";
        }
        else
        {
            session.State = DiameterCreditControlState.Failed;
            session.Code = "credit-control-reauth-refused";
            session.Message =
                "The credit-control client refused re-authorization.";
        }

        session.UpdatedAt = authorization.Clock.ReadUtcTime();
        DiameterCreditControlSession saved =
            SaveCreditControlReAuthSession(store, session);
        CreditControlReauthorizationResult result =
            new CreditControlReauthorizationResult();
        result.Session = saved;
        result.ReAuthAnswerRequired = true;
        result.CreditControlUpdateRequired = accepted;
        result.AnswerResultCode = transition.AnswerResultCode;
        result.Code = session.Code;
        return result;
    }

    internal static DiameterCreditControlResult
        BeginCreditControlClientReauthorizationUpdate(
            CreditControlClientStateMachine creditControl,
            AuthorizationClientStateMachine authorization,
            DiameterCreditControlRequest? request,
            DiameterMessage? encodedRequest,
            long pendingRequestIdentifier)
    {
        if (request == null || request.RequestType !=
            DiameterCreditControlRequestType.Update)
        {
            throw new ArgumentException(
                "Credit re-authorization requires a CCR UPDATE.");
        }

        DiameterCreditControlSession? session = creditControl
            .FindCreditControlClientSession(request.SessionId);
        if (session == null || !session.ReauthorizationRequested)
        {
            throw new DiameterCreditControlException(
                "The credit-control session has no pending re-authorization.");
        }

        authorization.BeginClientReauthorization(
            request.SessionId,
            encodedRequest,
            pendingRequestIdentifier);
        return creditControl.UpdateCreditControlClientSession(
            request,
            pendingRequestIdentifier);
    }

    internal static DiameterCreditControlResult
        ProcessCreditControlClientReauthorizationAnswer(
            CreditControlClientStateMachine creditControl,
            AuthorizationClientStateMachine authorization,
            long pendingRequestIdentifier,
            DiameterCreditControlAnswer? answer,
            DiameterMessage? encodedAnswer)
    {
        if (answer == null)
        {
            throw new ArgumentException(
                "A credit-control re-authorization answer is required.");
        }

        DiameterCreditControlResult result = creditControl
            .ProcessCreditControlClientAnswer(
                pendingRequestIdentifier,
                answer);
        authorization.ProcessClientAuthorizationAnswer(
            answer.SessionId,
            encodedAnswer);
        return result;
    }

    internal static DiameterCreditControlResult
        ProcessCreditControlServerReauthorizationUpdate(
            CreditControlServerStateMachine creditControl,
            AuthorizationServerStateMachine authorization,
            DiameterCreditControlRequest? request,
            DiameterMessage? encodedRequest)
    {
        if (request == null || request.RequestType !=
            DiameterCreditControlRequestType.Update)
        {
            throw new ArgumentException(
                "Credit re-authorization requires a CCR UPDATE.");
        }

        DiameterCreditControlSession? session = creditControl
            .FindCreditControlServerSession(request.SessionId);
        if (session == null || !session.ReauthorizationRequested)
        {
            throw new DiameterCreditControlException(
                "The credit-control server is not awaiting re-authorization.");
        }

        DiameterCreditControlResult result = creditControl
            .ProcessCreditControlServerRequest(request);
        authorization.ProcessServerAuthorizationRequest(
            request.SessionId,
            encodedRequest,
            result.ResultCode);
        return result;
    }

    internal static DiameterCreditControlSession
        ReadRequiredCreditControlReAuthSession(
            ICreditControlStore store,
            string? sessionId,
            DiameterSessionRole role)
    {
        if (store == null)
        {
            throw new ArgumentException(
                "A credit-control store is required.");
        }

        DiameterCreditControlSession? session =
            store.LoadDiameterCreditControlSession(sessionId);
        if (session == null || session.Role != role ||
            session.State != DiameterCreditControlState.Open ||
            session.PendingRequestPresent)
        {
            throw new DiameterCreditControlException(
                "Credit re-authorization requires a matching open session.");
        }

        return session;
    }

    internal static DiameterCreditControlSession
        SaveCreditControlReAuthSession(
            ICreditControlStore store,
            DiameterCreditControlSession session)
    {
        CreditControlQuotaState.AdvanceCreditControlInterrogationSchedule(
            session,
            session.UpdatedAt);
        session.ValidateCreditControlSession();
        return store.SaveDiameterCreditControlSession(
            session,
            session.Version);
    }

    internal static void ValidateCreditControlReAuthScope(
        DiameterCreditControlSession session,
        DiameterMessage? message)
    {
        if (message == null || message.Avps == null ||
            message.ApplicationId !=
                DiameterApplicationIds.CreditControl ||
            message.CommandCode != DiameterCommandCodes.ReAuth ||
            (message.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None)
        {
            throw new DiameterCreditControlException(
                "A credit-control Re-Auth request is required.");
        }

        CreditControlReAuthScope scope =
            ReadCreditControlReAuthScope(message);
        if (!scope.AuthApplicationIdPresent ||
            scope.AuthApplicationId !=
                DiameterApplicationIds.CreditControl ||
            scope.CreditControlSubSessionIdPresent &&
            (!session.CreditControlSubSessionIdPresent ||
            scope.CreditControlSubSessionId !=
                session.CreditControlSubSessionId) ||
            scope.GsuPoolIdentifierPresent &&
            !ContainsCreditControlReAuthPool(
                session,
                scope.GsuPoolIdentifier) ||
            (scope.ServiceIdentifierPresent || scope.RatingGroupPresent) &&
            !ContainsCreditControlReAuthService(session, scope))
        {
            throw new DiameterCreditControlException(
                "The Re-Auth request does not select this credit-control session.");
        }
    }

    internal static CreditControlReAuthScope ReadCreditControlReAuthScope(
        DiameterMessage message)
    {
        CreditControlReAuthScope result =
            new CreditControlReAuthScope();
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf)
            {
                ReadCreditControlReAuthScopeAvp(result, avp);
            }

            index = index + 1;
        }

        return result;
    }

    internal static void ReadCreditControlReAuthScopeAvp(
        CreditControlReAuthScope scope,
        DiameterAvp avp)
    {
        if (avp.Code == DiameterAvpCodes.CcSubSessionId)
        {
            if (scope.CreditControlSubSessionIdPresent ||
                avp.Data == null || avp.Data.Length != 8)
            {
                throw new DiameterCreditControlException(
                    "CC-Sub-Session-Id is malformed or duplicated.");
            }

            scope.CreditControlSubSessionId =
                NetworkByteOrder.ReadUInt64(avp.Data);
            scope.CreditControlSubSessionIdPresent = true;
        }
        else if (avp.Code == DiameterAvpCodes.AuthApplicationId)
        {
            ReadCreditControlReAuthUnsigned32(
                scope,
                avp,
                DiameterAvpCodes.AuthApplicationId);
        }
        else if (avp.Code == DiameterAvpCodes.GsuPoolIdentifier)
        {
            ReadCreditControlReAuthUnsigned32(
                scope,
                avp,
                DiameterAvpCodes.GsuPoolIdentifier);
        }
        else if (avp.Code == DiameterAvpCodes.ServiceIdentifier)
        {
            ReadCreditControlReAuthUnsigned32(
                scope,
                avp,
                DiameterAvpCodes.ServiceIdentifier);
        }
        else if (avp.Code == DiameterAvpCodes.RatingGroup)
        {
            ReadCreditControlReAuthUnsigned32(
                scope,
                avp,
                DiameterAvpCodes.RatingGroup);
        }
    }

    internal static void ReadCreditControlReAuthUnsigned32(
        CreditControlReAuthScope scope,
        DiameterAvp avp,
        uint code)
    {
        bool present = code == DiameterAvpCodes.AuthApplicationId ?
            scope.AuthApplicationIdPresent :
            code == DiameterAvpCodes.GsuPoolIdentifier ?
            scope.GsuPoolIdentifierPresent :
            code == DiameterAvpCodes.ServiceIdentifier ?
            scope.ServiceIdentifierPresent : scope.RatingGroupPresent;
        if (present || avp.Data == null || avp.Data.Length != 4)
        {
            throw new DiameterCreditControlException(
                "A credit-control Re-Auth selector is malformed or duplicated.");
        }

        uint value = NetworkByteOrder.ReadUInt32(avp.Data);
        if (code == DiameterAvpCodes.AuthApplicationId)
        {
            scope.AuthApplicationId = value;
            scope.AuthApplicationIdPresent = true;
        }
        else if (code == DiameterAvpCodes.GsuPoolIdentifier)
        {
            scope.GsuPoolIdentifier = value;
            scope.GsuPoolIdentifierPresent = true;
        }
        else if (code == DiameterAvpCodes.ServiceIdentifier)
        {
            scope.ServiceIdentifier = value;
            scope.ServiceIdentifierPresent = true;
        }
        else
        {
            scope.RatingGroup = value;
            scope.RatingGroupPresent = true;
        }
    }

    internal static bool ContainsCreditControlReAuthPool(
        DiameterCreditControlSession session,
        uint poolIdentifier)
    {
        int index = 0;
        while (index < session.PoolReferences.Count)
        {
            if (session.PoolReferences[index].PoolIdentifier ==
                poolIdentifier)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsCreditControlReAuthService(
        DiameterCreditControlSession session,
        CreditControlReAuthScope scope)
    {
        int index = 0;
        while (index < session.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit credit =
                session.MultipleServiceCredits[index];
            bool serviceMatches = !scope.ServiceIdentifierPresent;
            int serviceIndex = 0;
            while (serviceIndex < credit.ServiceIdentifiers.Count)
            {
                if (credit.ServiceIdentifiers[serviceIndex] ==
                    scope.ServiceIdentifier)
                {
                    serviceMatches = true;
                }

                serviceIndex = serviceIndex + 1;
            }

            bool ratingMatches = !scope.RatingGroupPresent ||
                credit.RatingGroupPresent &&
                credit.RatingGroup == scope.RatingGroup;
            if (serviceMatches && ratingMatches)
            {
                return true;
            }

            index = index + 1;
        }

        if (scope.RatingGroupPresent || session.LastRequest == null)
        {
            return false;
        }

        int topIndex = 0;
        while (topIndex < session.LastRequest.ServiceIdentifiers.Count)
        {
            if (session.LastRequest.ServiceIdentifiers[topIndex] ==
                scope.ServiceIdentifier)
            {
                return true;
            }

            topIndex = topIndex + 1;
        }

        return false;
    }
}
