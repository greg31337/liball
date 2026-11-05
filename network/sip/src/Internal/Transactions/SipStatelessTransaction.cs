namespace sip;

internal static class SipStatelessTransaction
{
    internal static SipStatelessResult SendRequest(SipMessage? request)
    {
        if (!MessageIsValid(request, SipMessageKind.Request) || request == null)
        {
            return CreateMalformedResult("A valid SIP request is required.");
        }

        SipStatelessResult result = new SipStatelessResult();
        result.Success = true;
        result.OutboundMessages = new SipMessage[] { CopyRequiredMessage(request) };
        return result;
    }

    internal static SipStatelessResult SendResponse(SipMessage? response)
    {
        if (!MessageIsValid(response, SipMessageKind.Response) || response == null)
        {
            return CreateMalformedResult("A valid SIP response is required.");
        }

        SipStatelessResult result = new SipStatelessResult();
        result.Success = true;
        result.OutboundMessages = new SipMessage[] { CopyRequiredMessage(response) };
        return result;
    }

    internal static SipStatelessResult ForwardRequest(SipMessage? request)
    {
        if (!ForwardRequestIsValid(request) || request == null)
        {
            return CreateMalformedResult("A valid SIP request is required for forwarding.");
        }

        int maxForwards = ReadMaxForwards(request);
        if (maxForwards < -1)
        {
            return CreateMalformedResult("The Max-Forwards header field is malformed.");
        }
        if (maxForwards == 0)
        {
            SipStatelessResult exhausted = new SipStatelessResult();
            exhausted.Rejected = true;
            exhausted.StatusCode = SipStatusCodes.TooManyHops;
            exhausted.ErrorMessage = "The request has no remaining forwarding hops.";
            return exhausted;
        }

        SipMessage forwarded = CopyRequiredMessage(request);
        SipRequestRouting.PrepareMaxForwards(forwarded);
        SipStatelessResult result = new SipStatelessResult();
        result.Success = true;
        result.Forwarded = true;
        result.OutboundMessages = new SipMessage[] { forwarded };
        return result;
    }

    internal static SipStatelessResult ForwardResponse(SipMessage? response,
        string? sentByHost, int sentByPort, string? branch)
    {
        if (!MessageIsValid(response, SipMessageKind.Response) || response == null)
        {
            return CreateMalformedResult("A valid SIP response is required for forwarding.");
        }
        if (sentByHost == null || sentByHost.Length == 0
            || sentByHost.Length > SipDefaults.MaxHeaderLineBytes
            || sentByPort < -1 || sentByPort > 65535 || !SipAscii.IsToken(branch))
        {
            return CreateMalformedResult("A valid local top Via identity is required.");
        }

        SipMessage forwarded = CopyRequiredMessage(response);
        if (!SipViaRouting.RemoveResponseVia(forwarded, sentByHost, sentByPort, branch))
        {
            return CreateMalformedResult("The response top Via does not match the local proxy.");
        }

        SipStatelessResult result = new SipStatelessResult();
        result.Success = true;
        if (forwarded.ViaValues.Length == 0)
        {
            result.DeliveredLocally = true;
            result.DeliveredMessages = new SipMessage[] { forwarded };
        }
        else
        {
            result.Forwarded = true;
            result.OutboundMessages = new SipMessage[] { forwarded };
        }
        return result;
    }

    static bool MessageIsValid(SipMessage? message, SipMessageKind kind)
    {
        if (message == null || message.Kind != kind)
        {
            return false;
        }
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(message, true);
        return !SipMessageValidator.HasErrorIssues(issues);
    }

    static bool ForwardRequestIsValid(SipMessage? message)
    {
        if (message == null || message.Kind != SipMessageKind.Request)
        {
            return false;
        }
        SipValidationIssue[] issues =
            SipMessageValidator.ValidateMessageModel(message, true);
        int index = 0;
        while (index < issues.Length)
        {
            SipValidationIssue issue = issues[index];
            if (issue.Severity == SipValidationSeverity.Error
                && !(issue.Code == "header-missing"
                    && SipAscii.EqualsIgnoreCase(issue.HeaderName,
                        SipHeaderNames.MaxForwards)))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static int ReadMaxForwards(SipMessage request)
    {
        int value = -1;
        bool found = false;
        int index = 0;
        while (index < request.Headers.Length)
        {
            SipHeader header = request.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.MaxForwards))
            {
                if (found)
                {
                    return -2;
                }
                found = true;
                SipStructuredHeaderResult parsed =
                    SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                        SipHeaderNames.MaxForwards, header.Value, new SipParseOptions());
                if (!parsed.Success)
                {
                    return -2;
                }
                value = parsed.MaxForwards;
            }
            index = index + 1;
        }
        return value;
    }

    static SipStatelessResult CreateMalformedResult(string errorMessage)
    {
        SipStatelessResult result = new SipStatelessResult();
        result.Malformed = true;
        result.StatusCode = SipStatusCodes.BadRequest;
        result.ErrorMessage = errorMessage;
        return result;
    }

    static SipMessage CopyRequiredMessage(SipMessage source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException("The stateless message cannot be copied.");
        }
        return copy;
    }
}
