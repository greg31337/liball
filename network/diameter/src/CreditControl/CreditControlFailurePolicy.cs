namespace diameter;

internal static class CreditControlFailurePolicy
{
    internal static bool IsCreditControlTemporaryFailure(uint resultCode)
    {
        return resultCode == DiameterResultCodes.UnableToDeliver ||
            resultCode == DiameterResultCodes.TooBusy ||
            resultCode == DiameterResultCodes.LoopDetected;
    }

    internal static bool ShouldRetryCreditControlSessionAnswer(
        DiameterCreditControlSession session,
        DiameterCreditControlRequest request,
        DiameterCreditControlAnswer answer)
    {
        return session.PendingRequestType !=
                DiameterCreditControlRequestType.Termination &&
            (session.PendingRequestType ==
                DiameterCreditControlRequestType.Initial ||
            session.SessionFailoverSupported) &&
            session.FailureHandling ==
                DiameterCreditControlFailureHandling.RetryAndTerminate &&
            request.AllowFailover &&
            IsCreditControlTemporaryFailure(answer.ResultCode);
    }

    internal static bool ShouldContinueCreditControlSessionFailure(
        DiameterCreditControlSession session,
        DiameterCreditControlAnswer answer)
    {
        if (answer.ResultCode == DiameterResultCodes.EndUserServiceDenied ||
            answer.ResultCode == DiameterResultCodes.UserUnknown)
        {
            return false;
        }

        return answer.ResultCode ==
                DiameterResultCodes.CreditControlNotApplicable ||
            session.FailureHandling ==
                DiameterCreditControlFailureHandling.Continue;
    }

    internal static bool ShouldKeepCreditControlSessionAfterTimeout(
        DiameterCreditControlSession session)
    {
        return session.PendingRequestType !=
                DiameterCreditControlRequestType.Termination &&
            session.FailureHandling !=
                DiameterCreditControlFailureHandling.Terminate;
    }

    internal static void ApplyCreditControlEventAnswerFailure(
        DiameterCreditControlResult result,
        DiameterCreditControlRequest request,
        DiameterCreditControlAnswer answer)
    {
        if (result.Successful ||
            answer.ResultCode ==
                DiameterResultCodes.CreditControlNotApplicable)
        {
            return;
        }

        bool denied = answer.ResultCode ==
                DiameterResultCodes.EndUserServiceDenied ||
            answer.ResultCode == DiameterResultCodes.UserUnknown;
        bool temporary = IsCreditControlTemporaryFailure(
            answer.ResultCode);
        if (temporary && request.AllowFailover)
        {
            result.RetryRequired = true;
            result.Code = "credit-control-event-retry-required";
        }

        if (request.RequestedAction ==
            DiameterRequestedAction.DirectDebiting)
        {
            DiameterDirectDebitFailureHandling handling =
                answer.DirectDebitFailureHandlingPresent ?
                    answer.DirectDebitFailureHandling :
                    request.DirectDebitFailureHandling;
            if (!denied && handling ==
                DiameterDirectDebitFailureHandling.Continue)
            {
                result.ContinueService = true;
                result.TerminateService = false;
                result.Code = result.RetryRequired ?
                    result.Code :
                    "credit-control-direct-debit-continued";
            }
            else if (!result.RetryRequired)
            {
                result.TerminateService = true;
                result.Code =
                    "credit-control-direct-debit-terminated";
            }
        }
        else if (denied)
        {
            result.TerminateService = true;
        }
    }

    internal static DiameterCreditControlResult
        CreateCreditControlEventDeliveryFailure(
            DiameterCreditControlRequest request,
            bool delivered)
    {
        CreditControlEventState.ValidateCreditControlEventRequest(request);
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        result.State = DiameterCreditControlState.Idle;
        result.Disposition = DiameterCreditControlDisposition.Failed;
        result.Accepted = true;
        result.Delivered = delivered;
        result.RetryRequired = request.AllowFailover;
        result.Code = result.RetryRequired ?
            "credit-control-event-retry-required" :
            "credit-control-event-delivery-failed";
        result.Message =
            "The one-time credit-control event could not be completed.";
        if (request.RequestedAction ==
            DiameterRequestedAction.DirectDebiting)
        {
            result.ContinueService =
                request.DirectDebitFailureHandling ==
                    DiameterDirectDebitFailureHandling.Continue ||
                request.DirectDebitFailureHandling ==
                    DiameterDirectDebitFailureHandling.TerminateOrBuffer;
            result.BufferRequest =
                request.DirectDebitFailureHandling ==
                    DiameterDirectDebitFailureHandling.TerminateOrBuffer;
        }
        else if (request.RequestedAction ==
            DiameterRequestedAction.RefundAccount)
        {
            result.BufferRequest = true;
        }

        return result;
    }
}
