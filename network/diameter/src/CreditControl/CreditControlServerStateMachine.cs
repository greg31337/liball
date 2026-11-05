using System;
using System.Collections.Generic;

namespace diameter;

internal class CreditControlServerStateMachine
{
    internal ICreditControlStore Store;
    internal ICreditControlRequestHandler Handler;
    internal IDiameterClock Clock;
    internal object ServerLock = new object();
    internal List<CreditControlEventReplay> EventReplays =
        new List<CreditControlEventReplay>();

    internal CreditControlServerStateMachine(
        ICreditControlStore? store,
        ICreditControlRequestHandler? handler,
        IDiameterClock? clock)
    {
        if (store == null || handler == null || clock == null)
        {
            throw new ArgumentException(
                "A credit-control store, handler, and clock are required.");
        }

        Store = store;
        Handler = handler;
        Clock = clock;
    }

    internal DiameterCreditControlResult ProcessCreditControlServerRequest(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "A credit-control server request is required.");
        }

        request.ValidateCreditControlRequest();
        DiameterCreditControlRequest owned =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        if (request.RequestType == DiameterCreditControlRequestType.Event)
        {
            lock (ServerLock)
            {
                return ProcessOwnedCreditControlServerEvent(owned);
            }
        }

        lock (ServerLock)
        {
            return ProcessOwnedCreditControlServerRequest(owned);
        }
    }

    internal DiameterCreditControlResult
        ProcessOwnedCreditControlServerRequest(
            DiameterCreditControlRequest owned)
    {
        DiameterCreditControlSession? loaded =
            Store.LoadDiameterCreditControlSession(owned.SessionId);
        if (loaded != null && loaded.Role != DiameterSessionRole.Server)
        {
            return CreateCreditControlServerFailure(
                owned,
                loaded,
                DiameterResultCodes.UnableToComply,
                "credit-control-session-role-conflict",
                "The Session-Id belongs to a client credit-control session.");
        }

        if (loaded != null && IsLastCreditControlServerRequest(
            loaded,
            owned))
        {
            return ReplayCreditControlServerAnswer(loaded, owned);
        }

        DiameterCreditControlResult? orderFailure =
            ValidateCreditControlServerOrder(loaded, owned);
        if (orderFailure != null)
        {
            return orderFailure;
        }

        CreditControlMultipleServiceState
            .ValidateCreditControlMultipleServiceRequestSupport(
                loaded,
                owned);
        if (loaded != null && owned.RequestType ==
            DiameterCreditControlRequestType.Termination)
        {
            CreditControlQuotaState.ValidateCreditControlFinalUsageReport(
                loaded,
                owned);
        }

        DiameterCreditControlRequest handoff =
            CreditControlDataCloner.CloneCreditControlRequest(owned);
        DiameterCreditControlAnswer applicationAnswer =
            Handler.HandleDiameterCreditControlRequest(handoff);
        ValidateCreditControlServerAnswer(owned, applicationAnswer);
        DiameterCreditControlAnswer answer =
            CreditControlDataCloner.CloneCreditControlAnswer(
                applicationAnswer);
        DateTimeOffset now = Clock.ReadUtcTime();
        DiameterCreditControlSession session;
        long expectedVersion;
        if (loaded == null)
        {
            session = CreateCreditControlServerSession(owned, now);
            expectedVersion = 0;
        }
        else
        {
            session = loaded;
            expectedVersion = loaded.Version;
        }

        ApplyCreditControlServerDecision(session, owned, answer, now);
        DiameterCreditControlResult result =
            CreateCreditControlServerDecisionResult(
                session,
                owned,
                answer);
        session.LastOperationResult = CreditControlDataCloner
            .CloneCreditControlResultWithoutSession(result);
        session.ValidateCreditControlSession();
        DiameterCreditControlSession saved =
            Store.SaveDiameterCreditControlSession(
                session,
                expectedVersion);
        result.Session =
            CreditControlDataCloner.CloneCreditControlSession(saved);
        return result;
    }

    internal DiameterCreditControlResult
        ProcessOwnedCreditControlServerEvent(
            DiameterCreditControlRequest request)
    {
        CreditControlEventState.ValidateCreditControlEventRequest(request);
        DateTimeOffset now = Clock.ReadUtcTime();
        RemoveExpiredCreditControlEventReplays(now);
        CreditControlEventReplay? replay =
            FindCreditControlEventReplay(request.SessionId);
        if (replay != null)
        {
            return ReplayCreditControlServerEvent(replay, request);
        }

        DiameterCreditControlRequest handoff =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        DiameterCreditControlAnswer applicationAnswer =
            Handler.HandleDiameterCreditControlRequest(handoff);
        ValidateCreditControlServerAnswer(request, applicationAnswer);
        DiameterCreditControlAnswer answer =
            CreditControlDataCloner.CloneCreditControlAnswer(
                applicationAnswer);
        DiameterCreditControlResult result =
            CreditControlEventState.CreateCreditControlEventResult(
                request,
                answer,
                false);
        CreditControlEventReplay retained =
            new CreditControlEventReplay();
        retained.Request = CreditControlDataCloner
            .CloneCreditControlRequest(request);
        retained.Result = CreditControlDataCloner
            .CloneCreditControlResultWithoutSession(result);
        retained.ExpiresAt = now.AddMilliseconds(
            request.TimeoutMilliseconds);
        EventReplays.Add(retained);
        return result;
    }

    internal void RemoveExpiredCreditControlEventReplays(
        DateTimeOffset now)
    {
        int index = EventReplays.Count - 1;
        while (index >= 0)
        {
            if (EventReplays[index].ExpiresAt <= now)
            {
                EventReplays.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal CreditControlEventReplay? FindCreditControlEventReplay(
        string sessionId)
    {
        int index = 0;
        while (index < EventReplays.Count)
        {
            if (EventReplays[index].Request.SessionId == sessionId)
            {
                return EventReplays[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal DiameterCreditControlResult ReplayCreditControlServerEvent(
        CreditControlEventReplay replay,
        DiameterCreditControlRequest request)
    {
        if (!CreditControlRequestComparer
            .AreCreditControlRequestsEquivalent(
                replay.Request,
                request))
        {
            DiameterCreditControlResult collision =
                CreateCreditControlServerFailure(
                    request,
                    null,
                    DiameterResultCodes.UnableToComply,
                    "credit-control-event-key-collision",
                    "The event Session-Id was reused with different rating data.");
            collision.Duplicate = true;
            return collision;
        }

        DiameterCreditControlResult result = CreditControlDataCloner
            .CloneCreditControlResultWithoutSession(replay.Result);
        result.Duplicate = true;
        result.Retransmission = true;
        result.Code = "credit-control-event-answer-replayed";
        result.Message =
            "The one-time credit-control answer was replayed without another rating decision.";
        return result;
    }

    internal DiameterCreditControlSession? FindCreditControlServerSession(
        string? sessionId)
    {
        DiameterCreditControlSession? session =
            Store.LoadDiameterCreditControlSession(sessionId);
        if (session == null || session.Role != DiameterSessionRole.Server)
        {
            return null;
        }

        return CreditControlDataCloner.CloneCreditControlSession(session);
    }

    internal List<DiameterCreditControlSession>
        ReadCreditControlServerSessions()
    {
        List<DiameterCreditControlSession> stored =
            Store.ReadStoredDiameterCreditControlSessions();
        List<DiameterCreditControlSession> result =
            new List<DiameterCreditControlSession>();
        int index = 0;
        while (index < stored.Count)
        {
            if (stored[index].Role == DiameterSessionRole.Server)
            {
                result.Add(CreditControlDataCloner.CloneCreditControlSession(
                    stored[index]));
            }

            index = index + 1;
        }

        return result;
    }

    internal bool IsLastCreditControlServerRequest(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        DiameterCreditControlRequest? lastRequest = session.LastRequest;
        return lastRequest != null &&
            lastRequest.RequestType == request.RequestType &&
            lastRequest.RequestNumber == request.RequestNumber;
    }

    internal DiameterCreditControlResult ReplayCreditControlServerAnswer(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request)
    {
        DiameterCreditControlRequest? lastRequest = session.LastRequest;
        DiameterCreditControlAnswer? lastAnswer = session.LastAnswer;
        if (lastRequest == null || lastAnswer == null ||
            !CreditControlRequestComparer
                .AreCreditControlRequestsEquivalent(lastRequest, request))
        {
            DiameterCreditControlResult collision =
                CreateCreditControlServerFailure(
                request,
                session,
                DiameterResultCodes.UnableToComply,
                "credit-control-request-key-collision",
                "The request type and number were reused with different rating data.");
            collision.Duplicate = true;
            return collision;
        }

        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        result.Answer =
            CreditControlDataCloner.CloneCreditControlAnswer(lastAnswer);
        result.Session =
            CreditControlDataCloner.CloneCreditControlSession(session);
        result.State = session.State;
        result.Disposition = lastAnswer.Disposition;
        result.Accepted = true;
        result.AnswerReceived = true;
        result.Successful = lastAnswer.ResultCode >= 2000U &&
            lastAnswer.ResultCode < 3000U;
        result.Duplicate = true;
        result.Retransmission = true;
        result.ContinueService = result.Successful &&
            session.State == DiameterCreditControlState.Open &&
            lastAnswer.Disposition !=
                DiameterCreditControlDisposition.Denied &&
            lastAnswer.Disposition !=
                DiameterCreditControlDisposition.Failed;
        result.TerminateService =
            session.State == DiameterCreditControlState.Closed ||
            session.State == DiameterCreditControlState.Failed;
        CreditControlFinalUnitState.PopulateCreditControlFinalUnitResult(
            result,
            session);
        result.ResultCode = lastAnswer.ResultCode;
        result.Code = "credit-control-answer-replayed";
        result.Message =
            "The persisted answer was replayed without another rating decision.";
        return result;
    }

    internal DiameterCreditControlResult? ValidateCreditControlServerOrder(
        DiameterCreditControlSession? session,
        DiameterCreditControlRequest request)
    {
        if (request.RequestType ==
            DiameterCreditControlRequestType.Initial)
        {
            if (session == null && request.RequestNumber == 0U)
            {
                return null;
            }

            return CreateCreditControlServerFailure(
                request,
                session,
                DiameterResultCodes.InvalidAvpValue,
                "credit-control-initial-out-of-order",
                "INITIAL must be request number zero for a new Session-Id.");
        }

        if (session == null || session.State !=
            DiameterCreditControlState.Open)
        {
            return CreateCreditControlServerFailure(
                request,
                session,
                DiameterResultCodes.UnknownSessionId,
                "credit-control-session-unknown",
                "UPDATE and TERMINATION require an open server session.");
        }

        if (request.ServiceContextId != session.ServiceContextId ||
            request.RequestNumber != session.NextRequestNumber ||
            request.RequestNumber == uint.MaxValue)
        {
            return CreateCreditControlServerFailure(
                request,
                session,
                DiameterResultCodes.InvalidAvpValue,
                "credit-control-request-out-of-order",
                "The request is not the next interrogation in the session sequence.");
        }

        return null;
    }

    internal void ValidateCreditControlServerAnswer(
        DiameterCreditControlRequest request,
        DiameterCreditControlAnswer? answer)
    {
        if (answer == null)
        {
            throw new DiameterCreditControlException(
                "The credit-control handler returned no answer.");
        }

        answer.ValidateCreditControlAnswer();
        if (answer.SessionId != request.SessionId ||
            answer.RequestType != request.RequestType ||
            answer.RequestNumber != request.RequestNumber)
        {
            throw new DiameterCreditControlException(
                "The credit-control handler answer does not correlate to the request.");
        }
    }

    internal DiameterCreditControlSession CreateCreditControlServerSession(
        DiameterCreditControlRequest request,
        DateTimeOffset now)
    {
        DiameterCreditControlSession result =
            new DiameterCreditControlSession();
        result.SessionId = request.SessionId;
        result.Role = DiameterSessionRole.Server;
        result.ServiceContextId = request.ServiceContextId;
        result.CreatedAt = now;
        result.UpdatedAt = now;
        return result;
    }

    internal void ApplyCreditControlServerDecision(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request,
        DiameterCreditControlAnswer answer,
        DateTimeOffset now)
    {
        bool successful = answer.ResultCode >= 2000U &&
            answer.ResultCode < 3000U;
        CreditControlQuotaState.AdvanceCreditControlInterrogationSchedule(
            session,
            now);
        answer.Disposition = CreditControlUnitArithmetic
            .ReadCreditControlGrantDisposition(
                request.RequestedServiceUnit,
                answer.GrantedServiceUnit,
                answer.Disposition);
        if (request.RequestType ==
            DiameterCreditControlRequestType.Termination)
        {
            session.State = DiameterCreditControlState.Closed;
        }
        else if (successful)
        {
            session.State = DiameterCreditControlState.Open;
        }
        else
        {
            session.State = DiameterCreditControlState.Failed;
        }

        session.NextRequestNumber = request.RequestNumber + 1U;
        session.DestinationRealm =
            CreditControlDataCloner.CloneCreditControlIdentity(
                request.DestinationRealm);
        session.DestinationHost = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(request.DestinationHost);
        session.PeerIdentity = CreditControlDataCloner
            .CloneOptionalCreditControlIdentity(request.PeerIdentity);
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

        CreditControlMultipleServiceState
            .ApplyCreditControlMultipleServiceRequest(session, request);
        int usedIndex = 0;
        while (usedIndex < request.UsedServiceUnits.Count)
        {
            session.UsedServiceUnit = CreditControlUnitArithmetic
                .AddCreditControlServiceUnits(
                    session.UsedServiceUnit,
                    request.UsedServiceUnits[usedIndex]);
            usedIndex = usedIndex + 1;
        }

        CreditControlQuotaState.ApplyCreditControlUsagePolicy(session);
        CreditControlFinalUnitState
            .ApplyCreditControlFinalUnitUsagePolicy(session);

        session.GrantedServiceUnit = CreditControlDataCloner
            .CloneOptionalCreditControlServiceUnit(
                answer.GrantedServiceUnit);
        session.Disposition = successful ?
            answer.Disposition :
            DiameterCreditControlDisposition.Failed;
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
        session.SubscriptionIdentifiers = CreditControlDataCloner
            .CloneCreditControlSubscriptionIdentifiers(
                request.SubscriptionIdentifiers);
        session.UserEquipmentInformation = CreditControlDataCloner
            .CloneOptionalCreditControlEquipment(
                request.UserEquipmentInformation);
        session.FailureHandling = answer.FailureHandlingPresent ?
            answer.FailureHandling : session.FailureHandling;
        session.DirectDebitFailureHandling =
            answer.DirectDebitFailureHandlingPresent ?
            answer.DirectDebitFailureHandling :
            session.DirectDebitFailureHandling;
        session.SessionFailoverSupported = answer.SessionFailoverPresent ?
            answer.SessionFailoverSupported :
            session.SessionFailoverSupported;
        if (!successful)
        {
            session.Disposition =
                DiameterCreditControlDisposition.Failed;
        }

        if (successful && request.RequestType !=
            DiameterCreditControlRequestType.Termination)
        {
            CreditControlQuotaState.ApplyCreditControlGrantPolicy(
                session,
                answer,
                now);
            CreditControlFinalUnitState
                .ApplyCreditControlFinalUnitGrantPolicy(session);
            if (request.RequestType ==
                DiameterCreditControlRequestType.Update)
            {
                session.ReauthorizationRequested = false;
            }
        }
        else if (request.RequestType ==
            DiameterCreditControlRequestType.Termination)
        {
            session.FinalUsageReportRequired = false;
        }

        answer.Disposition = session.Disposition;
        session.RouteIdentifier = request.RouteIdentifier;
        session.PeerOriginStateIdentifier =
            request.OriginStateIdentifier;
        session.PeerOriginStateIdentifierPresent =
            request.OriginStateIdentifierPresent;
        session.UpdatedAt = now;
        session.LastRequest =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        session.LastAnswer =
            CreditControlDataCloner.CloneCreditControlAnswer(answer);
        session.ApplicationState = CreditControlDataCloner
            .CloneCreditControlBytes(request.ApplicationState);
        session.Code = successful ?
            "credit-control-request-processed" :
            "credit-control-request-rejected";
        session.Message = successful ?
            "The credit-control request was processed in sequence." :
            "The rating decision rejected the credit-control request.";
    }

    internal DiameterCreditControlResult
        CreateCreditControlServerDecisionResult(
            DiameterCreditControlSession session,
            DiameterCreditControlRequest request,
            DiameterCreditControlAnswer answer)
    {
        bool successful = answer.ResultCode >= 2000U &&
            answer.ResultCode < 3000U;
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        result.Answer =
            CreditControlDataCloner.CloneCreditControlAnswer(answer);
        result.State = session.State;
        result.Disposition = session.Disposition;
        result.Accepted = true;
        result.AnswerReceived = true;
        result.Successful = successful;
        result.ContinueService = successful &&
            session.State == DiameterCreditControlState.Open &&
            answer.Disposition != DiameterCreditControlDisposition.Denied &&
            answer.Disposition != DiameterCreditControlDisposition.Failed;
        result.TerminateService =
            session.State == DiameterCreditControlState.Closed ||
            session.State == DiameterCreditControlState.Failed;
        CreditControlFinalUnitState.PopulateCreditControlFinalUnitResult(
            result,
            session);
        result.QuotaExhausted = session.QuotaExhausted;
        result.QuotaThresholdReached = session.QuotaThresholdReached;
        result.ResultCode = answer.ResultCode;
        result.Code = session.Code;
        result.Message = session.Message;
        return result;
    }

    internal DiameterCreditControlResult CreateCreditControlServerFailure(
        DiameterCreditControlRequest request,
        DiameterCreditControlSession? session,
        uint resultCode,
        string code,
        string message)
    {
        DiameterCreditControlAnswer answer =
            new DiameterCreditControlAnswer();
        answer.SessionId = request.SessionId;
        answer.RequestType = request.RequestType;
        answer.RequestNumber = request.RequestNumber;
        answer.ResultCode = resultCode;
        answer.Disposition = DiameterCreditControlDisposition.Failed;
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        result.Answer = answer;
        if (session != null)
        {
            result.Session =
                CreditControlDataCloner.CloneCreditControlSession(session);
            result.State = session.State;
        }

        result.Disposition = DiameterCreditControlDisposition.Failed;
        result.TerminateService = true;
        result.ResultCode = resultCode;
        result.Code = code;
        result.Message = message;
        return result;
    }
}
