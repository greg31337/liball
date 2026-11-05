namespace sip;

internal static class SipBearerAuthenticationCore
{
    const int MaximumRetryAttempt = 2;

    internal static SipAuthenticationResult Authorize(
        SipAuthenticationRequest request)
    {
        if (request.Message == null
            || request.Message.Kind != SipMessageKind.Request)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "Bearer authorization requires a SIP request message.", request);
        }
        if (request.Scope != SipCredentialScope.Origin
            && request.Scope != SipCredentialScope.Proxy)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "An origin or proxy credential scope is required.", request);
        }
        if (request.Attempt < 0 || request.Attempt > MaximumRetryAttempt)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The bounded authentication retry limit has been reached.",
                request);
        }
        SipBearerToken? token = request.BearerToken;
        if (token == null || token.Token == null || token.Token.Length == 0)
        {
            SipAuthenticationResult required = CreateFailure(
                SipAuthenticationStatus.ChallengeRequired,
                "An externally issued Bearer access token is required.", request);
            required.RetryAllowed = request.Attempt < MaximumRetryAttempt;
            return required;
        }
        string? value = SipAuthenticationHeaderCodec
            .FormatBearerAuthorizationValue(token);
        if (value == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The Bearer access token is malformed.", request);
        }
        SipMessage? message = SipSnapshotCopy.CopyMessageModel(request.Message);
        if (message == null)
        {
            return CreateFailure(SipAuthenticationStatus.Failed,
                "The SIP request could not be copied.", request);
        }
        AddAuthorizationHeader(message, request.Scope, value);

        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = SipAuthenticationStatus.Success;
        result.Message = message;
        result.Identity = token.Subject;
        result.RetryAllowed = true;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    static void AddAuthorizationHeader(SipMessage message,
        SipCredentialScope scope, string value)
    {
        string name = scope == SipCredentialScope.Proxy
            ? SipHeaderNames.ProxyAuthorization : SipHeaderNames.Authorization;
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (!SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                headers.Add(header);
            }
            index = index + 1;
        }
        SipHeader authorization = new SipHeader();
        authorization.Name = name;
        authorization.Value = value;
        headers.Add(authorization);
        message.Headers = headers.ToArray();
        message.OriginalBytes = new byte[0];
    }

    static SipAuthenticationResult CreateFailure(
        SipAuthenticationStatus status, string message,
        SipAuthenticationRequest request)
    {
        SipAuthenticationResult result = new SipAuthenticationResult();
        result.Status = status;
        result.ErrorMessage = message;
        result.CorrelationId = request.CorrelationId;
        return result;
    }
}
