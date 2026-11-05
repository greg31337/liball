namespace sip;

internal class SipMessageServerCore
{
    object gate = new object();
    System.Collections.Generic.List<SipInstantMessageEvent> events =
        new System.Collections.Generic.List<SipInstantMessageEvent>();
    ISipAuthenticationServer? authenticationServer;
    SipNonceOptions? nonceOptions;

    internal void ConfigureAuthentication(
        ISipAuthenticationServer configuredAuthenticationServer,
        SipNonceOptions configuredNonceOptions)
    {
        lock (gate)
        {
            if (configuredAuthenticationServer == null)
            {
                throw new System.ArgumentNullException("authenticationServer");
            }
            if (configuredNonceOptions == null)
            {
                throw new System.ArgumentNullException("nonceOptions");
            }
            authenticationServer = configuredAuthenticationServer;
            nonceOptions = configuredNonceOptions;
        }
    }

    internal SipInstantMessageEvent Receive(
        SipMessage message, SipInstantMessageServerOptions options)
    {
        lock (gate)
        {
            if (message == null)
            {
                throw new System.ArgumentNullException("message");
            }
            ValidateOptions(options);
            bool inDialog = HasTag(message.To);
            string? protocolError = ValidateMessage(message);
            if (protocolError != null)
            {
                int status = message.Kind == SipMessageKind.Request
                    && !IsMessageRequest(message)
                    ? SipStatusCodes.MethodNotAllowed
                    : SipStatusCodes.BadRequest;
                return Reject(message, status, protocolError, inDialog,
                    options, false);
            }
            if (message.Body.Length > options.MaxBodyBytes)
            {
                return Reject(message,
                    SipStatusCodes.RequestEntityTooLarge,
                    "The MESSAGE body exceeds the configured size policy.",
                    inDialog, options, false);
            }
            if (message.Body.Length > 0 && message.ContentType == null)
            {
                return Reject(message, SipStatusCodes.UnsupportedMediaType,
                    "A nonempty MESSAGE body requires Content-Type.",
                    inDialog, options, true);
            }
            if (message.ContentType != null
                && !IsValidContentType(message.ContentType))
            {
                return Reject(message, SipStatusCodes.UnsupportedMediaType,
                    "The MESSAGE Content-Type is invalid.", inDialog,
                    options, true);
            }
            if (message.ContentType != null
                && !AcceptsContentType(
                    message.ContentType, options.AcceptedContentTypes))
            {
                return Reject(message, SipStatusCodes.UnsupportedMediaType,
                    "The MESSAGE Content-Type is not accepted.", inDialog,
                    options, true);
            }
            if (inDialog && !options.AllowInDialog
                || !inDialog && !options.AllowOutOfDialog)
            {
                return Reject(message,
                    SipStatusCodes.CallOrTransactionDoesNotExist,
                    "MESSAGE dialog policy rejected the request.",
                    inDialog, options, false);
            }
            SipDigestAuthorization? authenticationInformation = null;
            if (options.RequireAuthentication)
            {
                if (authenticationServer == null || nonceOptions == null)
                {
                    SipInstantMessageEvent unavailable = CreateEvent(
                        message, null, SipInstantMessageEventKind.Failed,
                        false, inDialog,
                        "MESSAGE authentication is required but is not configured.");
                    QueueEvent(unavailable);
                    return CopyEvent(unavailable);
                }
                SipAuthenticationRequest verifiedRequest =
                    CreateAuthenticationRequest(message);
                SipAuthenticationResult verified =
                    authenticationServer.VerifyAuthentication(
                        verifiedRequest);
                if (verified.Status != SipAuthenticationStatus.Success)
                {
                    if (verified.Message == null)
                    {
                        verified = authenticationServer
                            .CreateAuthenticationChallenge(
                                verifiedRequest, nonceOptions);
                    }
                    SipMessage? challenge = verified.Message;
                    if (challenge == null)
                    {
                        challenge = SipUasResponseCore.CreateResponseMessage(
                            message, SipStatusCodes.Unauthorized,
                            SipReasonPhrases.Unauthorized);
                    }
                    SipInstantMessageEvent authenticationFailure =
                        CreateEvent(message, challenge,
                            SipInstantMessageEventKind.Failed, false,
                            inDialog, verified.ErrorMessage == null
                                ? "MESSAGE authentication is required."
                                : verified.ErrorMessage);
                    QueueEvent(authenticationFailure);
                    return CopyEvent(authenticationFailure);
                }
                authenticationInformation = verified.DigestAuthorization;
            }
            SipMessage? response = SipUasResponseCore.CreateResponseMessage(
                message, SipStatusCodes.Ok, SipReasonPhrases.Ok);
            if (response != null && authenticationInformation != null)
            {
                string? information = SipAuthenticationHeaderCodec
                    .FormatAuthenticationInfoValue(authenticationInformation);
                if (information != null)
                {
                    SipUasResponseCore.AppendResponseHeader(response,
                        SipHeaderNames.AuthenticationInfo, information);
                }
            }
            SipInstantMessageEvent accepted = CreateEvent(message, response,
                SipInstantMessageEventKind.Received, true, inDialog, null);
            QueueEvent(accepted);
            return CopyEvent(accepted);
        }
    }

    internal SipInstantMessageEvent PollEvent()
    {
        lock (gate)
        {
            if (events.Count == 0)
            {
                return new SipInstantMessageEvent();
            }
            SipInstantMessageEvent value = events[0];
            events.RemoveAt(0);
            return CopyEvent(value);
        }
    }

    SipInstantMessageEvent Reject(
        SipMessage message, int statusCode, string error, bool inDialog,
        SipInstantMessageServerOptions options, bool includeAccept)
    {
        SipMessage? response = SipUasResponseCore.CreateResponseMessage(
            message, statusCode, ReasonPhrase(statusCode));
        if (response != null && statusCode == SipStatusCodes.MethodNotAllowed)
        {
            SipUasResponseCore.AppendResponseHeader(response,
                SipHeaderNames.Allow, SipMethods.Message);
        }
        if (response != null && includeAccept
            && options.AcceptedContentTypes.Length > 0)
        {
            string? accept = FormatAccept(options.AcceptedContentTypes);
            if (accept != null)
            {
                SipUasResponseCore.AppendResponseHeader(response,
                    SipHeaderNames.Accept, accept);
            }
        }
        SipInstantMessageEvent rejected = CreateEvent(message, response,
            SipInstantMessageEventKind.Failed, false, inDialog, error);
        QueueEvent(rejected);
        return CopyEvent(rejected);
    }

    static void ValidateOptions(SipInstantMessageServerOptions? options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.MaxBodyBytes < 0
            || options.AcceptedContentTypes == null)
        {
            throw new System.ArgumentException(
                "Valid MESSAGE server size and content policy is required.",
                "options");
        }
        int index = 0;
        while (index < options.AcceptedContentTypes.Length)
        {
            if (!IsValidAcceptedContentType(
                    options.AcceptedContentTypes[index]))
            {
                throw new System.ArgumentException(
                    "Every accepted MESSAGE media type must be valid.",
                    "options");
            }
            index = index + 1;
        }
    }

    static string? ValidateMessage(SipMessage message)
    {
        if (!IsMessageRequest(message))
        {
            return "A MESSAGE request is required.";
        }
        SipValidationIssue[] issues =
            SipMessageValidator.ValidateMessageModel(message, true);
        if (issues.Length > 0)
        {
            return issues[0].Message == null
                ? "The MESSAGE request is malformed."
                : issues[0].Message;
        }
        return null;
    }

    static bool IsMessageRequest(SipMessage message)
    {
        return message.Kind == SipMessageKind.Request
            && message.Method == SipMethod.Message
            && SipAscii.EqualsIgnoreCase(
                message.MethodToken, SipMethods.Message);
    }

    static bool HasTag(SipNameAddress? address)
    {
        int index = 0;
        while (address != null && address.Parameters != null
            && index < address.Parameters.Length)
        {
            SipUriParameter? parameter = address.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(
                    parameter.Name, "tag")
                && parameter.Value != null
                && parameter.Value.Length > 0)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool AcceptsContentType(
        SipMediaType contentType, SipMediaType[] accepted)
    {
        if (accepted.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < accepted.Length)
        {
            SipMediaType candidate = accepted[index];
            bool typeMatches = candidate.Type == "*"
                || SipAscii.EqualsIgnoreCase(
                    candidate.Type, contentType.Type);
            bool subtypeMatches = candidate.Subtype == "*"
                || SipAscii.EqualsIgnoreCase(
                    candidate.Subtype, contentType.Subtype);
            if (typeMatches && subtypeMatches)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsValidContentType(SipMediaType? contentType)
    {
        return contentType != null
            && SipAscii.IsToken(contentType.Type)
            && SipAscii.IsToken(contentType.Subtype)
            && contentType.Parameters != null;
    }

    static bool IsValidAcceptedContentType(SipMediaType? contentType)
    {
        return contentType != null
            && (contentType.Type == "*"
                || SipAscii.IsToken(contentType.Type))
            && (contentType.Subtype == "*"
                || SipAscii.IsToken(contentType.Subtype))
            && contentType.Parameters != null;
    }

    static string? FormatAccept(SipMediaType[] mediaTypes)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < mediaTypes.Length)
        {
            if (index > 0)
            {
                text.Append(", ");
            }
            text.Append(mediaTypes[index].Type);
            text.Append('/');
            text.Append(mediaTypes[index].Subtype);
            index = index + 1;
        }
        return text.Length == 0 ? null : text.ToString();
    }

    static SipAuthenticationRequest CreateAuthenticationRequest(
        SipMessage message)
    {
        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = SipSnapshotCopy.CopyMessageModel(message);
        request.Target = message.RequestUri == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(message.RequestUri);
        request.Body = message.Body == null
            ? new byte[0] : (byte[])message.Body.Clone();
        request.Scope = SipCredentialScope.Origin;
        request.CorrelationId = ReadCallId(message);
        return request;
    }

    static SipInstantMessageEvent CreateEvent(
        SipMessage message, SipMessage? response,
        SipInstantMessageEventKind kind, bool success, bool inDialog,
        string? error)
    {
        SipInstantMessageEvent value = new SipInstantMessageEvent();
        value.Kind = kind;
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.Success = success;
        value.Final = true;
        value.InDialog = inDialog;
        value.StatusCode = response == null ? 0 : response.StatusCode;
        value.ErrorMessage = error;
        value.CorrelationId = ReadCallId(message);
        return value;
    }

    void QueueEvent(SipInstantMessageEvent value)
    {
        if (events.Count >= SipDefaults.MaxQueueCount)
        {
            throw new System.InvalidOperationException(
                "The MESSAGE event queue limit was reached.");
        }
        events.Add(CopyEvent(value));
    }

    static SipInstantMessageEvent CopyEvent(
        SipInstantMessageEvent source)
    {
        SipInstantMessageEvent copy = new SipInstantMessageEvent();
        copy.Kind = source.Kind;
        copy.Request = source.Request;
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Response = SipSnapshotCopy.CopyMessageModel(source.Response);
        copy.Success = source.Success;
        copy.Final = source.Final;
        copy.InDialog = source.InDialog;
        copy.StatusCode = source.StatusCode;
        copy.AuthenticationAttemptCount = source.AuthenticationAttemptCount;
        copy.RedirectCount = source.RedirectCount;
        copy.ErrorMessage = source.ErrorMessage;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static string? ReadCallId(SipMessage message)
    {
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.CallId))
            {
                return header.Value;
            }
            index = index + 1;
        }
        return null;
    }

    static string ReasonPhrase(int statusCode)
    {
        switch (statusCode)
        {
            case SipStatusCodes.MethodNotAllowed:
                return SipReasonPhrases.MethodNotAllowed;
            case SipStatusCodes.RequestEntityTooLarge:
                return SipReasonPhrases.RequestEntityTooLarge;
            case SipStatusCodes.UnsupportedMediaType:
                return SipReasonPhrases.UnsupportedMediaType;
            case SipStatusCodes.CallOrTransactionDoesNotExist:
                return SipReasonPhrases.CallOrTransactionDoesNotExist;
            default:
                return SipReasonPhrases.BadRequest;
        }
    }
}
