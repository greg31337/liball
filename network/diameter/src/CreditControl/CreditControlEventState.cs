namespace diameter;

internal static class CreditControlEventState
{
    internal static void ValidateCreditControlEventRequest(
        DiameterCreditControlRequest request)
    {
        request.ValidateCreditControlRequest();
        if (request.RequestType != DiameterCreditControlRequestType.Event ||
            !request.RequestedActionPresent)
        {
            throw new DiameterCreditControlException(
                "A one-time credit-control event requires Requested-Action.");
        }
    }

    internal static void ValidateCreditControlEventAnswer(
        DiameterCreditControlRequest request,
        DiameterCreditControlAnswer answer)
    {
        answer.ValidateCreditControlAnswer();
        if (answer.SessionId != request.SessionId ||
            answer.RequestType != DiameterCreditControlRequestType.Event ||
            answer.RequestNumber != request.RequestNumber)
        {
            throw new DiameterCreditControlException(
                "The credit-control event answer does not match its request.");
        }

        bool successful = answer.ResultCode >= 2000U &&
            answer.ResultCode < 3000U;
        if (!successful)
        {
            return;
        }

        if (request.RequestedAction ==
                DiameterRequestedAction.PriceEnquiry &&
            answer.CostInformation == null)
        {
            throw new DiameterCreditControlException(
                "A successful price enquiry requires Cost-Information.");
        }

        if (request.RequestedAction ==
                DiameterRequestedAction.CheckBalance &&
            !answer.CheckBalanceResultPresent)
        {
            throw new DiameterCreditControlException(
                "A successful balance check requires Check-Balance-Result.");
        }

        if ((request.RequestedAction ==
                DiameterRequestedAction.DirectDebiting ||
            request.RequestedAction ==
                DiameterRequestedAction.RefundAccount) &&
            answer.GrantedServiceUnit == null)
        {
            throw new DiameterCreditControlException(
                "A successful debit or refund requires Granted-Service-Unit.");
        }
    }

    internal static DiameterCreditControlResult
        CreateCreditControlEventResult(
            DiameterCreditControlRequest request,
            DiameterCreditControlAnswer answer,
            bool delivered)
    {
        ValidateCreditControlEventRequest(request);
        ValidateCreditControlEventAnswer(request, answer);
        bool successful = answer.ResultCode >= 2000U &&
            answer.ResultCode < 3000U;
        bool notApplicable = request.RequestedAction ==
                DiameterRequestedAction.DirectDebiting &&
            answer.ResultCode ==
                DiameterResultCodes.CreditControlNotApplicable;
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request =
            CreditControlDataCloner.CloneCreditControlRequest(request);
        result.Answer =
            CreditControlDataCloner.CloneCreditControlAnswer(answer);
        result.State = DiameterCreditControlState.Idle;
        result.Accepted = true;
        result.Delivered = delivered;
        result.AnswerReceived = true;
        result.Successful = successful;
        result.Disposition = ReadCreditControlEventDisposition(
            request,
            answer,
            successful,
            notApplicable);
        result.ContinueService = successful || notApplicable;
        if (request.RequestedAction ==
                DiameterRequestedAction.CheckBalance &&
            answer.CheckBalanceResultPresent &&
            answer.CheckBalanceResult ==
                DiameterCheckBalanceResult.NoCredit)
        {
            result.ContinueService = false;
        }

        result.TerminateService = answer.ResultCode ==
                DiameterResultCodes.EndUserServiceDenied ||
            answer.ResultCode == DiameterResultCodes.UserUnknown;
        result.ResultCode = answer.ResultCode;
        result.Code = ReadCreditControlEventCode(
            request.RequestedAction,
            successful,
            notApplicable);
        result.Message = successful ?
            "The one-time credit-control event completed." :
            "The one-time credit-control event was not successful.";
        CreditControlFailurePolicy.ApplyCreditControlEventAnswerFailure(
            result,
            request,
            answer);
        return result;
    }

    internal static DiameterCreditControlDisposition
        ReadCreditControlEventDisposition(
            DiameterCreditControlRequest request,
            DiameterCreditControlAnswer answer,
            bool successful,
            bool notApplicable)
    {
        if (!successful && !notApplicable)
        {
            return DiameterCreditControlDisposition.Failed;
        }

        if (request.RequestedAction ==
                DiameterRequestedAction.CheckBalance &&
            answer.CheckBalanceResultPresent &&
            answer.CheckBalanceResult ==
                DiameterCheckBalanceResult.NoCredit)
        {
            return DiameterCreditControlDisposition.Denied;
        }

        if (answer.GrantedServiceUnit != null)
        {
            return CreditControlUnitArithmetic
                .ReadCreditControlGrantDisposition(
                    request.RequestedServiceUnit,
                    answer.GrantedServiceUnit,
                    answer.Disposition);
        }

        return DiameterCreditControlDisposition.Granted;
    }

    internal static string ReadCreditControlEventCode(
        DiameterRequestedAction action,
        bool successful,
        bool notApplicable)
    {
        if (!successful && !notApplicable)
        {
            return "credit-control-event-rejected";
        }

        if (action == DiameterRequestedAction.PriceEnquiry)
        {
            return "credit-control-price-enquiry-completed";
        }

        if (action == DiameterRequestedAction.CheckBalance)
        {
            return "credit-control-balance-check-completed";
        }

        if (action == DiameterRequestedAction.RefundAccount)
        {
            return "credit-control-refund-completed";
        }

        return notApplicable ?
            "credit-control-debit-not-applicable" :
            "credit-control-direct-debit-completed";
    }
}
