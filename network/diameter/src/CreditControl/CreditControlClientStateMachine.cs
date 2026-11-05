using System;
using System.Collections.Generic;

namespace diameter;

internal class CreditControlClientStateMachine
{
    internal ICreditControlStore Store;
    internal IDiameterClock Clock;

    internal CreditControlClientStateMachine(
        ICreditControlStore? store,
        IDiameterClock? clock)
    {
        if (store == null || clock == null)
        {
            throw new ArgumentException(
                "A credit-control store and clock are required.");
        }

        Store = store;
        Clock = clock;
    }

    internal DiameterCreditControlResult BeginCreditControlClientSession(
        DiameterCreditControlRequest? request,
        long pendingRequestIdentifier)
    {
        DiameterCreditControlRequest owned =
            ReadCreditControlClientRequest(
                request,
                DiameterCreditControlRequestType.Initial,
                pendingRequestIdentifier);
        if (Store.LoadDiameterCreditControlSession(owned.SessionId) != null)
        {
            throw new DiameterCreditControlException(
                "A credit-control session with this Session-Id already exists.");
        }

        owned.RequestNumber = 0U;
        DateTimeOffset now = Clock.ReadUtcTime();
        DiameterCreditControlSession session =
            new DiameterCreditControlSession();
        session.SessionId = owned.SessionId;
        session.Role = DiameterSessionRole.Client;
        session.State = DiameterCreditControlState.PendingInitial;
        session.ServiceContextId = owned.ServiceContextId;
        session.DestinationRealm =
            CreditControlDataCloner.CloneCreditControlIdentity(
                owned.DestinationRealm);
        session.DestinationHost =
            CreditControlDataCloner.CloneOptionalCreditControlIdentity(
                owned.DestinationHost);
        session.PeerIdentity =
            CreditControlDataCloner.CloneOptionalCreditControlIdentity(
                owned.PeerIdentity);
        session.UserName = owned.UserName;
        session.FailureHandling = owned.FailureHandling;
        session.DirectDebitFailureHandling =
            owned.DirectDebitFailureHandling;
        session.NextRequestNumber = 1U;
        CopyCreditControlRequestContext(session, owned);
        CreditControlFailoverState.InitializeCreditControlServerAffinity(
            session,
            owned);
        session.CreatedAt = now;
        session.UpdatedAt = now;
        BeginPendingCreditControlRequest(
            session,
            owned,
            pendingRequestIdentifier,
            now,
            DiameterCreditControlState.PendingInitial,
            "credit-control-initial-pending");
        DiameterCreditControlResult pending =
            CreatePendingCreditControlResult(session, owned);
        session.LastOperationResult =
            CreditControlDataCloner.CloneCreditControlResultWithoutSession(
                pending);
        session.ValidateCreditControlSession();
        DiameterCreditControlSession saved =
            Store.SaveDiameterCreditControlSession(session, 0);
        pending.Session =
            CreditControlDataCloner.CloneCreditControlSession(saved);
        return pending;
    }

    internal DiameterCreditControlResult UpdateCreditControlClientSession(
        DiameterCreditControlRequest? request,
        long pendingRequestIdentifier)
    {
        return BeginExistingCreditControlRequest(
            request,
            pendingRequestIdentifier,
            DiameterCreditControlRequestType.Update,
            DiameterCreditControlState.PendingUpdate,
            "credit-control-update-pending");
    }

    internal DiameterCreditControlResult TerminateCreditControlClientSession(
        DiameterCreditControlRequest? request,
        long pendingRequestIdentifier)
    {
        return BeginExistingCreditControlRequest(
            request,
            pendingRequestIdentifier,
            DiameterCreditControlRequestType.Termination,
            DiameterCreditControlState.PendingTermination,
            "credit-control-termination-pending");
    }

    internal DiameterCreditControlResult ProcessCreditControlClientEvent(
        DiameterCreditControlRequest? request,
        DiameterCreditControlAnswer? answer)
    {
        if (request == null || answer == null)
        {
            throw new ArgumentException(
                "A credit-control event request and answer are required.");
        }

        DiameterCreditControlRequest ownedRequest =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        DiameterCreditControlAnswer ownedAnswer =
            CreditControlDataCloner.CloneCreditControlAnswer(answer);
        return CreditControlEventState.CreateCreditControlEventResult(
            ownedRequest,
            ownedAnswer,
            true);
    }

    internal DiameterCreditControlResult
        ProcessCreditControlClientEventFailure(
            DiameterCreditControlRequest? request,
            bool delivered)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "A credit-control event request is required.");
        }

        DiameterCreditControlRequest ownedRequest =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        return CreditControlFailurePolicy
            .CreateCreditControlEventDeliveryFailure(
                ownedRequest,
                delivered);
    }

    internal DiameterCreditControlResult ProcessCreditControlClientAnswer(
        long pendingRequestIdentifier,
        DiameterCreditControlAnswer? answer)
    {
        if (pendingRequestIdentifier <= 0 || answer == null)
        {
            throw new ArgumentException(
                "A pending request identifier and answer are required.");
        }

        answer.ValidateCreditControlAnswer();
        DiameterCreditControlAnswer owned =
            CreditControlDataCloner.CloneCreditControlAnswer(answer);
        DiameterCreditControlSession session =
            ReadRequiredCreditControlClientSession(owned.SessionId);
        DiameterCreditControlResult? duplicate =
            ReplayDuplicateCreditControlClientAnswer(
                session,
                pendingRequestIdentifier,
                owned);
        if (duplicate != null)
        {
            return duplicate;
        }

        ValidatePendingCreditControlAnswer(
            session,
            pendingRequestIdentifier,
            owned);
        DiameterCreditControlRequest? lastRequest = session.LastRequest;
        if (lastRequest == null)
        {
            throw new DiameterCreditControlException(
                "The pending credit-control request is unavailable.");
        }

        DiameterCreditControlRequest operationRequest =
            CreditControlDataCloner.CloneCreditControlRequest(lastRequest);
        DateTimeOffset now = Clock.ReadUtcTime();
        session.UpdatedAt = now;
        bool successful = owned.ResultCode >= 2000U &&
            owned.ResultCode < 3000U;
        bool retryRequired = !successful && CreditControlFailurePolicy
            .ShouldRetryCreditControlSessionAnswer(
                session,
                operationRequest,
                owned);
        bool failoverAttempted = false;
        if (retryRequired)
        {
            failoverAttempted = CreditControlFailoverState
                .TrySelectAlternateCreditControlServer(
                    session,
                    operationRequest,
                    now);
        }
        bool continueFailure = !successful && !retryRequired &&
            session.State !=
                DiameterCreditControlState.PendingTermination &&
            CreditControlFailurePolicy
                .ShouldContinueCreditControlSessionFailure(
                    session,
                    owned);
        bool terminal = session.State ==
                DiameterCreditControlState.PendingTermination ||
            !successful && !retryRequired;
        if (!retryRequired)
        {
            session.LastCompletedRequestIdentifier =
                session.PendingRequestIdentifier;
            ClearPendingCreditControlRequest(session);
        }

        if (successful && session.State !=
            DiameterCreditControlState.PendingTermination)
        {
            session.State = DiameterCreditControlState.Open;
            ApplyCreditControlAnswerState(session, owned, now);
            if (operationRequest.RequestType ==
                DiameterCreditControlRequestType.Update)
            {
                session.ReauthorizationRequested = false;
            }

            session.Code = "credit-control-session-open";
            session.Message =
                "The credit-control answer opened or updated the session.";
        }
        else if (successful)
        {
            session.State = DiameterCreditControlState.Closed;
            session.Disposition = owned.Disposition;
            session.Code = "credit-control-session-closed";
            session.Message =
                "The termination answer closed the credit-control session.";
        }
        else if (retryRequired)
        {
            session.Code = "credit-control-retry-required";
            session.Message =
                "The temporary credit-control failure requires another delivery attempt.";
        }
        else if (continueFailure)
        {
            session.State = DiameterCreditControlState.Idle;
            session.Disposition = owned.ResultCode ==
                    DiameterResultCodes.CreditControlNotApplicable ?
                DiameterCreditControlDisposition.Granted :
                DiameterCreditControlDisposition.Failed;
            session.Code = "credit-control-failure-continued";
            session.Message =
                "The credit-control failure policy allows service to continue.";
        }
        else
        {
            session.State = DiameterCreditControlState.Failed;
            session.Disposition = DiameterCreditControlDisposition.Failed;
            session.Code = "credit-control-answer-rejected";
            session.Message =
                "The credit-control answer did not report success.";
        }

        owned.Disposition = session.Disposition;
        session.LastAnswer =
            CreditControlDataCloner.CloneCreditControlAnswer(owned);

        DiameterCreditControlResult result = CreateAnsweredCreditControlResult(
            session,
            operationRequest,
            owned,
            successful);
        result.RetryRequired = retryRequired;
        result.FailoverAttempted = failoverAttempted;
        result.Retransmission = failoverAttempted;
        if (retryRequired)
        {
            result.ContinueService = false;
            result.TerminateService = false;
        }
        else if (continueFailure)
        {
            result.ContinueService = true;
            result.TerminateService = false;
        }

        session.LastOperationResult =
            CreditControlDataCloner.CloneCreditControlResultWithoutSession(
                result);
        session.ValidateCreditControlSession();
        if (terminal)
        {
            RemoveTerminalCreditControlSession(session);
            result.Session =
                CreditControlDataCloner.CloneCreditControlSession(session);
            return result;
        }

        DiameterCreditControlSession saved =
            Store.SaveDiameterCreditControlSession(
                session,
                session.Version);
        result.Session =
            CreditControlDataCloner.CloneCreditControlSession(saved);
        return result;
    }

    internal List<DiameterCreditControlResult>
        ExpireCreditControlClientTransactions()
    {
        List<DiameterCreditControlResult> result =
            new List<DiameterCreditControlResult>();
        List<DiameterCreditControlSession> sessions =
            Store.ReadStoredDiameterCreditControlSessions();
        DateTimeOffset now = Clock.ReadUtcTime();
        int index = 0;
        while (index < sessions.Count)
        {
            DiameterCreditControlSession session = sessions[index];
            if (session.Role == DiameterSessionRole.Client &&
                session.PendingRequestPresent &&
                session.TransactionDeadlinePresent &&
                session.TransactionDeadline <= now)
            {
                result.Add(ExpireCreditControlClientTransaction(
                    session,
                    now));
            }

            index = index + 1;
        }

        return result;
    }

    internal DiameterCreditControlResult
        ProcessCreditControlClientDeliveryFailure(
            long pendingRequestIdentifier,
            bool delivered)
    {
        if (pendingRequestIdentifier <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "pendingRequestIdentifier");
        }

        List<DiameterCreditControlSession> sessions =
            Store.ReadStoredDiameterCreditControlSessions();
        int index = 0;
        while (index < sessions.Count)
        {
            DiameterCreditControlSession session = sessions[index];
            if (session.Role == DiameterSessionRole.Client &&
                session.PendingRequestPresent &&
                session.PendingRequestIdentifier ==
                    pendingRequestIdentifier)
            {
                DiameterCreditControlResult result =
                    ExpireCreditControlClientTransaction(
                        session,
                        Clock.ReadUtcTime());
                result.Delivered = delivered;
                return result;
            }

            index = index + 1;
        }

        throw new DiameterCreditControlException(
            "The pending credit-control transaction does not exist.");
    }

    internal List<DiameterCreditControlResult>
        RecoverCreditControlClientTransactions()
    {
        List<DiameterCreditControlResult> result =
            new List<DiameterCreditControlResult>();
        List<DiameterCreditControlSession> sessions =
            Store.ReadStoredDiameterCreditControlSessions();
        DateTimeOffset now = Clock.ReadUtcTime();
        int index = 0;
        while (index < sessions.Count)
        {
            DiameterCreditControlSession session = sessions[index];
            DiameterCreditControlRequest? request = session.LastRequest;
            if (session.Role == DiameterSessionRole.Client &&
                session.PendingRequestPresent && request != null)
            {
                DiameterCreditControlRequest recoveredRequest =
                    CreditControlDataCloner.CloneCreditControlRequest(
                        request);
                bool failoverAttempted = CreditControlFailoverState
                    .TrySelectAlternateCreditControlServer(
                        session,
                        recoveredRequest,
                        now);
                session.TransactionDeadline = now.AddMilliseconds(
                    recoveredRequest.TimeoutMilliseconds);
                session.TransactionDeadlinePresent = true;
                session.LastRequest = CreditControlDataCloner
                    .CloneCreditControlRequest(recoveredRequest);
                session.UpdatedAt = now;
                session.Code = "credit-control-pending-recovered";
                session.Message =
                    "The persisted request requires duplicate-safe retransmission.";
                DiameterCreditControlResult recovered =
                    CreatePendingCreditControlResult(
                        session,
                        recoveredRequest);
                recovered.Delivered = false;
                recovered.RetryRequired = true;
                recovered.Retransmission = true;
                recovered.FailoverAttempted = failoverAttempted;
                recovered.Code = session.Code;
                recovered.Message = session.Message;
                session.LastOperationResult = CreditControlDataCloner
                    .CloneCreditControlResultWithoutSession(recovered);
                session.ValidateCreditControlSession();
                DiameterCreditControlSession saved =
                    Store.SaveDiameterCreditControlSession(
                        session,
                        session.Version);
                recovered.Session = CreditControlDataCloner
                    .CloneCreditControlSession(saved);
                result.Add(recovered);
            }

            index = index + 1;
        }

        return result;
    }

    internal DiameterCreditControlSession FailbackCreditControlClientSession(
        string? sessionId)
    {
        DiameterCreditControlSession session =
            ReadRequiredCreditControlClientSession(sessionId);
        if (session.State != DiameterCreditControlState.Open ||
            session.PendingRequestPresent)
        {
            throw new DiameterCreditControlException(
                "Only an open session without a pending request can fail back.");
        }

        if (!CreditControlFailoverState
            .TryFailbackCreditControlServer(session))
        {
            throw new DiameterCreditControlException(
                "The credit-control session has no alternate affinity to fail back.");
        }

        session.UpdatedAt = Clock.ReadUtcTime();
        session.Code = "credit-control-primary-restored";
        session.Message =
            "The credit-control session route affinity returned to its primary server.";
        session.ValidateCreditControlSession();
        return Store.SaveDiameterCreditControlSession(
            session,
            session.Version);
    }

    internal DiameterCreditControlSession? FindCreditControlClientSession(
        string? sessionId)
    {
        DiameterCreditControlSession? session =
            Store.LoadDiameterCreditControlSession(sessionId);
        if (session == null || session.Role != DiameterSessionRole.Client)
        {
            return null;
        }

        return CreditControlDataCloner.CloneCreditControlSession(session);
    }

    internal List<DiameterCreditControlSession>
        ReadCreditControlClientSessions()
    {
        List<DiameterCreditControlSession> stored =
            Store.ReadStoredDiameterCreditControlSessions();
        List<DiameterCreditControlSession> result =
            new List<DiameterCreditControlSession>();
        int index = 0;
        while (index < stored.Count)
        {
            if (stored[index].Role == DiameterSessionRole.Client)
            {
                result.Add(CreditControlDataCloner.CloneCreditControlSession(
                    stored[index]));
            }

            index = index + 1;
        }

        return result;
    }

    internal List<DiameterCreditControlResult>
        EvaluateCreditControlClientInterrogations()
    {
        List<DiameterCreditControlResult> result =
            new List<DiameterCreditControlResult>();
        List<DiameterCreditControlSession> sessions =
            Store.ReadStoredDiameterCreditControlSessions();
        DateTimeOffset now = Clock.ReadUtcTime();
        int index = 0;
        while (index < sessions.Count)
        {
            DiameterCreditControlSession session = sessions[index];
            if (session.Role == DiameterSessionRole.Client &&
                session.State == DiameterCreditControlState.Open &&
                session.NextInterrogationTimePresent &&
                session.NextInterrogationTime <= now)
            {
                CreditControlQuotaState
                    .AdvanceCreditControlInterrogationSchedule(
                        session,
                        now);
                session.UpdatedAt = now;
                session.Code = "credit-control-interrogation-due";
                session.Message =
                    "An intermediate credit-control interrogation is due.";
                DiameterCreditControlSession saved =
                    Store.SaveDiameterCreditControlSession(
                        session,
                        session.Version);
                result.Add(CreateCreditControlInterrogationResult(saved));
            }

            index = index + 1;
        }

        return result;
    }

    internal DiameterCreditControlResult
        CreateCreditControlInterrogationResult(
            DiameterCreditControlSession session)
    {
        DiameterCreditControlRequest? lastRequest = session.LastRequest;
        if (lastRequest == null)
        {
            throw new DiameterCreditControlException(
                "The scheduled interrogation has no retained request.");
        }

        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request = CreditControlDataCloner
            .CloneCreditControlRequest(lastRequest);
        result.Session = CreditControlDataCloner
            .CloneCreditControlSession(session);
        result.State = session.State;
        result.Disposition = session.Disposition;
        result.QuotaExhausted = session.QuotaExhausted;
        result.QuotaThresholdReached = session.QuotaThresholdReached;
        result.ContinueService = !session.QuotaExhausted;
        result.Code = session.Code;
        result.Message = session.Message;
        return result;
    }

    internal DiameterCreditControlResult BeginExistingCreditControlRequest(
        DiameterCreditControlRequest? request,
        long pendingRequestIdentifier,
        DiameterCreditControlRequestType requestType,
        DiameterCreditControlState pendingState,
        string code)
    {
        DiameterCreditControlRequest owned =
            ReadCreditControlClientRequest(
                request,
                requestType,
                pendingRequestIdentifier);
        DiameterCreditControlSession session =
            ReadRequiredCreditControlClientSession(owned.SessionId);
        if (session.State != DiameterCreditControlState.Open ||
            session.PendingRequestPresent)
        {
            throw new DiameterCreditControlException(
                "Only an open session without a pending request can be interrogated.");
        }

        if (owned.ServiceContextId != session.ServiceContextId)
        {
            throw new DiameterCreditControlException(
                "The credit-control Service-Context-Id cannot change during a session.");
        }

        if (session.NextRequestNumber == uint.MaxValue)
        {
            throw new DiameterCreditControlException(
                "The credit-control request number sequence is exhausted.");
        }

        if (requestType == DiameterCreditControlRequestType.Termination)
        {
            CreditControlQuotaState.ValidateCreditControlFinalUsageReport(
                session,
                owned);
        }

        CreditControlFailoverState.ApplyCreditControlServerAffinity(
            session,
            owned);
        owned.RequestNumber = session.NextRequestNumber;
        session.NextRequestNumber = session.NextRequestNumber + 1U;
        DateTimeOffset now = Clock.ReadUtcTime();
        CopyCreditControlRequestContext(session, owned);
        CreditControlQuotaState.ApplyCreditControlUsagePolicy(session);
        CreditControlFinalUnitState
            .ApplyCreditControlFinalUnitUsagePolicy(session);
        CreditControlQuotaState.AdvanceCreditControlInterrogationSchedule(
            session,
            now);
        if (requestType == DiameterCreditControlRequestType.Termination)
        {
            session.FinalUsageReportRequired = false;
        }

        BeginPendingCreditControlRequest(
            session,
            owned,
            pendingRequestIdentifier,
            now,
            pendingState,
            code);
        DiameterCreditControlResult pending =
            CreatePendingCreditControlResult(session, owned);
        session.LastOperationResult =
            CreditControlDataCloner.CloneCreditControlResultWithoutSession(
                pending);
        session.ValidateCreditControlSession();
        DiameterCreditControlSession saved =
            Store.SaveDiameterCreditControlSession(
                session,
                session.Version);
        pending.Session =
            CreditControlDataCloner.CloneCreditControlSession(saved);
        return pending;
    }

    internal DiameterCreditControlRequest ReadCreditControlClientRequest(
        DiameterCreditControlRequest? request,
        DiameterCreditControlRequestType requestType,
        long pendingRequestIdentifier)
    {
        if (request == null || pendingRequestIdentifier <= 0)
        {
            throw new ArgumentException(
                "A credit-control request and positive pending identifier are required.");
        }

        request.ValidateCreditControlRequest();
        if (request.RequestType != requestType)
        {
            throw new DiameterCreditControlException(
                "The credit-control request type does not match the operation.");
        }

        return CreditControlDataCloner.CloneCreditControlRequest(request);
    }

    internal DiameterCreditControlSession
        ReadRequiredCreditControlClientSession(string? sessionId)
    {
        DiameterCreditControlSession? session =
            Store.LoadDiameterCreditControlSession(sessionId);
        if (session == null || session.Role != DiameterSessionRole.Client)
        {
            throw new DiameterCreditControlException(
                "The credit-control client session does not exist.");
        }

        session.ValidateCreditControlSession();
        return session;
    }

    internal void CopyCreditControlRequestContext(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        session.DestinationRealm =
            CreditControlDataCloner.CloneCreditControlIdentity(
                request.DestinationRealm);
        session.DestinationHost =
            CreditControlDataCloner.CloneOptionalCreditControlIdentity(
                request.DestinationHost);
        session.PeerIdentity =
            CreditControlDataCloner.CloneOptionalCreditControlIdentity(
                request.PeerIdentity);
        session.UserName = request.UserName;
        session.CreditControlSubSessionId =
            request.CreditControlSubSessionId;
        session.CreditControlSubSessionIdPresent =
            request.CreditControlSubSessionIdPresent;
        session.AccountingMultiSessionId =
            request.AccountingMultiSessionId;
        session.CorrelationId = CreditControlDataCloner
            .CloneCreditControlBytes(request.CorrelationId);
        session.RequestedServiceUnit = CreditControlDataCloner
            .CloneOptionalCreditControlServiceUnit(
                request.RequestedServiceUnit);
        if (request.QuotaThreshold != null)
        {
            session.QuotaThreshold = CreditControlDataCloner
                .CloneOptionalCreditControlServiceUnit(
                    request.QuotaThreshold);
        }

        if (request.RequireFinalUsageReport)
        {
            session.FinalUsageReportRequired = true;
        }

        int usedIndex = 0;
        while (usedIndex < request.UsedServiceUnits.Count)
        {
            session.UsedServiceUnit = CreditControlUnitArithmetic
                .AddCreditControlServiceUnits(
                    session.UsedServiceUnit,
                    request.UsedServiceUnits[usedIndex]);
            usedIndex = usedIndex + 1;
        }

        CreditControlMultipleServiceState
            .ApplyCreditControlMultipleServiceRequest(session, request);
        session.SubscriptionIdentifiers = CreditControlDataCloner
            .CloneCreditControlSubscriptionIdentifiers(
                request.SubscriptionIdentifiers);
        session.UserEquipmentInformation = CreditControlDataCloner
            .CloneOptionalCreditControlEquipment(
                request.UserEquipmentInformation);
        session.RouteIdentifier = request.RouteIdentifier;
        session.ApplicationState = CreditControlDataCloner
            .CloneCreditControlBytes(request.ApplicationState);
    }

    internal void BeginPendingCreditControlRequest(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request,
        long pendingRequestIdentifier,
        DateTimeOffset now,
        DiameterCreditControlState state,
        string code)
    {
        session.State = state;
        session.PendingRequestType = request.RequestType;
        session.PendingRequestNumber = request.RequestNumber;
        session.PendingRequestPresent = true;
        session.PendingRequestIdentifier = pendingRequestIdentifier;
        session.TransactionDeadline = now.AddMilliseconds(
            request.TimeoutMilliseconds);
        session.TransactionDeadlinePresent = true;
        session.LastRequest =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        session.LastAnswer = null;
        session.AttemptCount = 1;
        CreditControlFailoverState.ResetCreditControlAttemptedServers(
            session,
            request);
        session.UpdatedAt = now;
        session.Code = code;
        session.Message =
            "The credit-control request is awaiting a terminal outcome.";
    }

    internal void ClearPendingCreditControlRequest(
        DiameterCreditControlSession session)
    {
        session.PendingRequestPresent = false;
        session.PendingRequestIdentifier = 0;
        session.TransactionDeadlinePresent = false;
        session.AttemptCount = 0;
        session.AttemptedServers.Clear();
    }

    internal DiameterCreditControlResult?
        ReplayDuplicateCreditControlClientAnswer(
            DiameterCreditControlSession session,
            long pendingRequestIdentifier,
            DiameterCreditControlAnswer answer)
    {
        DiameterCreditControlAnswer? lastAnswer = session.LastAnswer;
        DiameterCreditControlResult? lastResult =
            session.LastOperationResult;
        if (session.PendingRequestPresent ||
            session.LastCompletedRequestIdentifier !=
                pendingRequestIdentifier || lastAnswer == null ||
            lastResult == null ||
            lastAnswer.RequestType != answer.RequestType ||
            lastAnswer.RequestNumber != answer.RequestNumber)
        {
            return null;
        }

        if (lastAnswer.ResultCode != answer.ResultCode)
        {
            throw new DiameterCreditControlException(
                "The completed credit-control transaction received a conflicting answer.");
        }

        DiameterCreditControlResult result = CreditControlDataCloner
            .CloneCreditControlResultWithoutSession(lastResult);
        result.Answer = CreditControlDataCloner
            .CloneCreditControlAnswer(lastAnswer);
        result.Session = CreditControlDataCloner
            .CloneCreditControlSession(session);
        result.Duplicate = true;
        result.Retransmission = true;
        result.Code = "credit-control-answer-duplicate";
        result.Message =
            "The completed credit-control answer was recognized as a duplicate.";
        return result;
    }

    internal void ValidatePendingCreditControlAnswer(
        DiameterCreditControlSession session,
        long pendingRequestIdentifier,
        DiameterCreditControlAnswer answer)
    {
        if (!session.PendingRequestPresent ||
            session.PendingRequestIdentifier != pendingRequestIdentifier ||
            session.PendingRequestType != answer.RequestType ||
            session.PendingRequestNumber != answer.RequestNumber)
        {
            throw new DiameterCreditControlException(
                "The credit-control answer does not match the pending interrogation.");
        }
    }

    internal void ApplyCreditControlAnswerState(
        DiameterCreditControlSession session,
        DiameterCreditControlAnswer answer,
        DateTimeOffset now)
    {
        session.GrantedServiceUnit = CreditControlDataCloner
            .CloneOptionalCreditControlServiceUnit(
                answer.GrantedServiceUnit);
        session.Disposition = CreditControlUnitArithmetic
            .ReadCreditControlGrantDisposition(
                session.RequestedServiceUnit,
                answer.GrantedServiceUnit,
                answer.Disposition);
        session.Disposition = CreditControlMultipleServiceState
            .ApplyCreditControlMultipleServiceAnswer(
                session,
                answer.MultipleServiceCredits,
                session.Disposition);
        session.FinalUnitInstruction = CreditControlDataCloner
            .CloneOptionalCreditControlFinalUnit(
                answer.FinalUnitInstruction);
        session.QosFinalUnitInstruction = CreditControlDataCloner
            .CloneOptionalCreditControlFinalUnit(
                answer.QosFinalUnitInstruction);
        session.CostInformation = CreditControlDataCloner
            .CloneOptionalCreditControlCost(answer.CostInformation);
        if (answer.FailureHandlingPresent)
        {
            session.FailureHandling = answer.FailureHandling;
        }

        if (answer.DirectDebitFailureHandlingPresent)
        {
            session.DirectDebitFailureHandling =
                answer.DirectDebitFailureHandling;
        }

        if (answer.SessionFailoverPresent)
        {
            session.SessionFailoverSupported =
                answer.SessionFailoverSupported;
        }

        if (answer.OriginStateIdentifierPresent)
        {
            session.PeerOriginStateIdentifier =
                answer.OriginStateIdentifier;
            session.PeerOriginStateIdentifierPresent = true;
        }

        CreditControlQuotaState.ApplyCreditControlGrantPolicy(
            session,
            answer,
            now);
        CreditControlFinalUnitState
            .ApplyCreditControlFinalUnitGrantPolicy(session);
    }

    internal DiameterCreditControlResult CreatePendingCreditControlResult(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        result.State = session.State;
        result.Disposition = session.Disposition;
        result.Accepted = true;
        result.Delivered = true;
        result.Code = session.Code;
        result.Message = session.Message;
        result.QuotaExhausted = session.QuotaExhausted;
        result.QuotaThresholdReached = session.QuotaThresholdReached;
        CreditControlFinalUnitState.PopulateCreditControlFinalUnitResult(
            result,
            session);
        return result;
    }

    internal DiameterCreditControlResult CreateAnsweredCreditControlResult(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request,
        DiameterCreditControlAnswer answer,
        bool successful)
    {
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        result.Answer =
            CreditControlDataCloner.CloneCreditControlAnswer(answer);
        result.State = session.State;
        result.Disposition = session.Disposition;
        result.Accepted = true;
        result.Delivered = true;
        result.AnswerReceived = true;
        result.Successful = successful;
        result.ContinueService = successful &&
            session.State == DiameterCreditControlState.Open &&
            session.Disposition !=
                DiameterCreditControlDisposition.Denied &&
            session.Disposition !=
                DiameterCreditControlDisposition.Failed;
        result.TerminateService = session.State ==
            DiameterCreditControlState.Closed ||
            session.State == DiameterCreditControlState.Failed ||
            session.Disposition !=
                DiameterCreditControlDisposition.Granted &&
            session.Disposition !=
                DiameterCreditControlDisposition.FinalUnit;
        result.ResultCode = answer.ResultCode;
        result.Code = session.Code;
        result.Message = session.Message;
        CreditControlFinalUnitState.PopulateCreditControlFinalUnitResult(
            result,
            session);
        return result;
    }

    internal DiameterCreditControlResult ExpireCreditControlClientTransaction(
        DiameterCreditControlSession session,
        DateTimeOffset now)
    {
        DiameterCreditControlRequest? lastRequest = session.LastRequest;
        if (lastRequest == null)
        {
            throw new DiameterCreditControlException(
                "The expired credit-control request is unavailable.");
        }

        DiameterCreditControlRequest operationRequest =
            CreditControlDataCloner.CloneCreditControlRequest(lastRequest);
        bool keepPending = CreditControlFailurePolicy
            .ShouldKeepCreditControlSessionAfterTimeout(session);
        if (keepPending)
        {
            bool retryRequired = session.FailureHandling ==
                    DiameterCreditControlFailureHandling.RetryAndTerminate &&
                operationRequest.AllowFailover &&
                (session.PendingRequestType ==
                    DiameterCreditControlRequestType.Initial ||
                session.SessionFailoverSupported);
            bool failoverAttempted = false;
            if (retryRequired)
            {
                failoverAttempted = CreditControlFailoverState
                    .TrySelectAlternateCreditControlServer(
                        session,
                        operationRequest,
                        now);
            }

            if (!failoverAttempted)
            {
                session.TransactionDeadlinePresent = false;
            }

            session.UpdatedAt = now;
            session.Code = retryRequired ?
                "credit-control-tx-retry-required" :
                "credit-control-tx-expired-continued";
            session.Message = retryRequired ?
                "The expired transaction requires another delivery attempt." :
                "The failure policy allows service while awaiting a late answer.";
            DiameterCreditControlResult continued =
                new DiameterCreditControlResult();
            continued.Request = operationRequest;
            continued.State = session.State;
            continued.Disposition = session.Disposition;
            continued.Accepted = true;
            continued.Delivered = true;
            continued.RetryRequired = retryRequired;
            continued.FailoverAttempted = failoverAttempted;
            continued.Retransmission = failoverAttempted;
            continued.ContinueService = true;
            continued.Code = session.Code;
            continued.Message = session.Message;
            session.LastOperationResult = CreditControlDataCloner
                .CloneCreditControlResultWithoutSession(continued);
            session.ValidateCreditControlSession();
            DiameterCreditControlSession saved =
                Store.SaveDiameterCreditControlSession(
                    session,
                    session.Version);
            continued.Session = CreditControlDataCloner
                .CloneCreditControlSession(saved);
            return continued;
        }

        session.State = DiameterCreditControlState.Failed;
        session.Disposition = DiameterCreditControlDisposition.Failed;
        session.UpdatedAt = now;
        session.Code = "credit-control-tx-expired";
        session.Message =
            "The credit-control transaction deadline expired.";
        ClearPendingCreditControlRequest(session);
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request = operationRequest;
        result.State = session.State;
        result.Disposition = session.Disposition;
        result.Accepted = true;
        result.Delivered = true;
        result.TerminateService = true;
        result.Code = session.Code;
        result.Message = session.Message;
        session.LastOperationResult = CreditControlDataCloner
            .CloneCreditControlResultWithoutSession(result);
        session.ValidateCreditControlSession();
        RemoveTerminalCreditControlSession(session);
        result.Session =
            CreditControlDataCloner.CloneCreditControlSession(session);
        return result;
    }

    internal void RemoveTerminalCreditControlSession(
        DiameterCreditControlSession session)
    {
        if (!Store.RemoveDiameterCreditControlSession(
            session.SessionId,
            session.Version))
        {
            throw new DiameterCreditControlException(
                "The terminal credit-control session changed before cleanup.");
        }
    }
}
