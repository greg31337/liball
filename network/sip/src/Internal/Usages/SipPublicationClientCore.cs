namespace sip;

internal class SipPublicationClientCore
{
    const int InitialOperation = 1;
    const int ModifyOperation = 2;
    const int RefreshOperation = 3;
    const int RemoveOperation = 4;

    object gate = new object();
    System.Collections.Generic.List<SipPublicationClientContext> contexts =
        new System.Collections.Generic.List<SipPublicationClientContext>();
    System.Collections.Generic.List<SipPublicationEvent> events =
        new System.Collections.Generic.List<SipPublicationEvent>();
    SipProtocolTokenGenerator tokens;
    ISipAuthenticationClient? authenticationClient;
    long nowMilliseconds;

    internal SipPublicationClientCore(ISipRandomSource randomSource)
    {
        if (randomSource == null) throw new System.ArgumentNullException("randomSource");
        tokens = new SipProtocolTokenGenerator(randomSource);
    }

    internal void ConfigureAuthentication(
        ISipAuthenticationClient configuredAuthenticationClient)
    {
        lock (gate)
        {
            if (configuredAuthenticationClient == null)
                throw new System.ArgumentNullException("authenticationClient");
            authenticationClient = configuredAuthenticationClient;
        }
    }

    internal SipPublicationEvent Create(SipPublicationRequest request)
    {
        lock (gate)
        {
            ValidateCreateRequest(request);
            if (contexts.Count >= SipDefaults.MaxTransactionCount)
                throw new System.InvalidOperationException(
                    "The active publication limit was reached.");
            SipPublicationClientContext context = CreateContext(CopyRequest(request));
            contexts.Add(context);
            SipPublicationEvent value = CreateEvent(context, context.Message,
                null, 0, false, false, "initial", null);
            QueueEvent(value);
            return CopyEvent(value);
        }
    }

    internal SipPublicationEvent Modify(SipPublicationRequest request)
    {
        lock (gate)
        {
            ValidateConditionalRequest(request, ModifyOperation);
            SipPublicationClientContext context = FindActiveContext(request);
            context.Request = MergeRequest(context, request, ModifyOperation);
            return BeginOperation(context, ModifyOperation, "modify");
        }
    }

    internal SipPublicationEvent Refresh(SipPublicationRequest request)
    {
        lock (gate)
        {
            ValidateConditionalRequest(request, RefreshOperation);
            SipPublicationClientContext context = FindActiveContext(request);
            context.Request = MergeRequest(context, request, RefreshOperation);
            return BeginOperation(context, RefreshOperation, "refresh");
        }
    }

    internal SipPublicationEvent Remove(SipPublicationRequest request)
    {
        lock (gate)
        {
            ValidateConditionalRequest(request, RemoveOperation);
            SipPublicationClientContext context = FindActiveContext(request);
            context.Request = MergeRequest(context, request, RemoveOperation);
            return BeginOperation(context, RemoveOperation, "remove");
        }
    }

    internal SipPublicationEvent ReceiveResponse(SipMessage response)
    {
        lock (gate)
        {
            ValidateResponse(response);
            int index = FindResponseContext(response);
            if (index < 0)
                throw new System.ArgumentException(
                    "The PUBLISH response does not match an outstanding request.",
                    "message");
            SipPublicationClientContext context = contexts[index];
            if (response.StatusCode < 200)
            {
                SipPublicationEvent provisional = CreateEvent(context, null,
                    response, response.StatusCode, false, false, "provisional", null);
                QueueEvent(provisional);
                return CopyEvent(provisional);
            }
            if (response.StatusCode == SipStatusCodes.Unauthorized
                || response.StatusCode == SipStatusCodes.ProxyAuthenticationRequired)
                return RetryAuthentication(context, response, index);
            if (response.StatusCode == SipStatusCodes.ConditionalRequestFailed)
                return RecoverConditionalFailure(context, response, index);
            if (response.StatusCode == SipStatusCodes.IntervalTooBrief)
                return RetryMinimumExpiry(context, response, index);
            if (response.StatusCode >= 200 && response.StatusCode < 300)
                return AcceptResponse(context, response, index);
            return FailOperation(context, response, index,
                "The peer rejected the PUBLISH request.");
        }
    }

    internal void AdvanceTime(long nextMilliseconds)
    {
        lock (gate)
        {
            if (nextMilliseconds < nowMilliseconds)
                throw new System.ArgumentException(
                    "Publication time cannot move backwards.", "nowMilliseconds");
            nowMilliseconds = nextMilliseconds;
            int index = contexts.Count - 1;
            while (index >= 0)
            {
                SipPublicationClientContext context = contexts[index];
                if (context.ExpiresAtMilliseconds >= 0
                    && nowMilliseconds >= context.ExpiresAtMilliseconds)
                {
                    context.State = SipPublicationState.Terminated;
                    context.ExpiresSeconds = 0;
                    context.RefreshAtMilliseconds = -1;
                    context.ExpiresAtMilliseconds = -1;
                    SipPublicationEvent expired = CreateEvent(context, null,
                        null, 0, false, true, "expired",
                        "The publication expired.");
                    QueueEvent(expired);
                    contexts.RemoveAt(index);
                }
                else if (context.State == SipPublicationState.Published
                    && context.RefreshAtMilliseconds >= 0
                    && nowMilliseconds >= context.RefreshAtMilliseconds)
                {
                    BeginOperation(context, RefreshOperation, "automatic-refresh");
                }
                index = index - 1;
            }
        }
    }

    internal SipPublicationEvent PollEvent()
    {
        lock (gate)
        {
            if (events.Count == 0) return new SipPublicationEvent();
            SipPublicationEvent value = events[0];
            events.RemoveAt(0);
            return CopyEvent(value);
        }
    }

    SipPublicationClientContext CreateContext(SipPublicationRequest request)
    {
        SipPublicationClientContext context = new SipPublicationClientContext();
        context.Request = request;
        context.Target = SipUriModelCopy.CreateNormalizedCopy(request.Target);
        context.CallId = tokens.CreateCallId();
        context.LocalTag = tokens.CreateTag();
        context.PublicationId = context.CallId + ":" + context.LocalTag;
        context.CSeq = 1;
        context.Operation = InitialOperation;
        context.ExpiresSeconds = request.ExpiresSeconds;
        context.State = SipPublicationState.Publishing;
        context.Message = BuildRequestMessage(context);
        return context;
    }

    SipPublicationEvent BeginOperation(
        SipPublicationClientContext context, int operation, string reason)
    {
        IncrementCSeq(context);
        context.Operation = operation;
        context.AuthenticationAttemptCount = 0;
        context.AuthenticationScope = SipCredentialScope.None;
        context.ConditionalRetryCount = 0;
        context.RefreshAtMilliseconds = -1;
        context.State = operation == RefreshOperation
            ? SipPublicationState.Refreshing
            : operation == RemoveOperation
                ? SipPublicationState.Removing : SipPublicationState.Publishing;
        context.Message = BuildRequestMessage(context);
        SipPublicationEvent value = CreateEvent(context, context.Message,
            null, 0, false, false, reason, null);
        QueueEvent(value);
        return CopyEvent(value);
    }

    SipPublicationEvent AcceptResponse(
        SipPublicationClientContext context, SipMessage response, int index)
    {
        string? entityTag = ReadSingleTokenHeader(response, SipHeaderNames.SipETag);
        int expires = ReadIntegerHeader(response, SipHeaderNames.Expires);
        if (entityTag == null || expires < 0
            || expires > context.Request.ExpiresSeconds
            || expires > context.Request.Options.MaxExpiresSeconds)
            return FailOperation(context, response, index,
                "A successful PUBLISH response requires one valid SIP-ETag and Expires value.");
        string? verificationError = VerifyAuthenticationInformation(context, response);
        if (verificationError != null)
            return FailOperation(context, response, index, verificationError);
        context.EntityTag = entityTag;
        context.Request.EntityTag = entityTag;
        context.ExpiresSeconds = expires;
        if (context.Operation == RemoveOperation || expires == 0)
        {
            context.State = SipPublicationState.Terminated;
            context.RefreshAtMilliseconds = -1;
            context.ExpiresAtMilliseconds = -1;
            SipPublicationEvent removed = CreateEvent(context, null, response,
                response.StatusCode, true, true, "removed", null);
            QueueEvent(removed);
            contexts.RemoveAt(index);
            return CopyEvent(removed);
        }
        context.State = SipPublicationState.Published;
        SetLifetime(context, expires);
        context.CommittedRequest = CopyRequest(context.Request);
        SipPublicationEvent accepted = CreateEvent(context, null, response,
            response.StatusCode, true, true, "published", null);
        QueueEvent(accepted);
        context.AuthenticationAttemptCount = 0;
        context.AuthenticationScope = SipCredentialScope.None;
        context.ConditionalRetryCount = 0;
        return CopyEvent(accepted);
    }

    SipPublicationEvent RetryAuthentication(
        SipPublicationClientContext context, SipMessage response, int index)
    {
        if (authenticationClient == null
            || context.AuthenticationAttemptCount
                >= context.Request.Options.MaxAuthenticationAttempts)
            return FailOperation(context, response, index,
                authenticationClient == null
                    ? "The PUBLISH challenge requires an authentication client."
                    : "The PUBLISH authentication retry limit was reached.");
        SipCredentialScope scope = response.StatusCode == SipStatusCodes.Unauthorized
            ? SipCredentialScope.Origin : SipCredentialScope.Proxy;
        SipDigestChallenge[] challenges = ReadChallenges(response, scope);
        if (challenges.Length == 0)
            return FailOperation(context, response, index,
                "The PUBLISH authentication challenge is missing or malformed.");
        IncrementCSeq(context);
        SipMessage retry = BuildRequestMessage(context);
        SipAuthenticationRequest authentication = new SipAuthenticationRequest();
        authentication.Message = retry;
        authentication.Challenges = challenges;
        authentication.Target = context.Target;
        authentication.Body = retry.Body;
        authentication.Scope = scope;
        authentication.Attempt = context.AuthenticationAttemptCount;
        authentication.CorrelationId = context.Request.AuthenticationContextId == null
            ? context.Request.CorrelationId : context.Request.AuthenticationContextId;
        SipAuthenticationResult authorized =
            authenticationClient.AuthorizeRequest(authentication);
        if (authorized.Status != SipAuthenticationStatus.Success
            || !authorized.RetryAllowed || authorized.Message == null)
            return FailOperation(context, response, index,
                authorized.ErrorMessage == null
                    ? "The PUBLISH request could not be authorized."
                    : authorized.ErrorMessage);
        context.AuthenticationAttemptCount = context.AuthenticationAttemptCount + 1;
        context.AuthenticationScope = scope;
        context.Message = SipReferCore.CopyMessage(authorized.Message);
        SipPublicationEvent value = CreateEvent(context, context.Message, response,
            response.StatusCode, false, false, "authentication-retry", null);
        QueueEvent(value);
        return CopyEvent(value);
    }

    SipPublicationEvent RecoverConditionalFailure(
        SipPublicationClientContext context, SipMessage response, int index)
    {
        context.EntityTag = null;
        context.Request.EntityTag = null;
        if (context.Operation == RemoveOperation)
        {
            context.State = SipPublicationState.Terminated;
            context.ExpiresSeconds = 0;
            context.RefreshAtMilliseconds = -1;
            context.ExpiresAtMilliseconds = -1;
            SipPublicationEvent absent = CreateEvent(context, null, response,
                response.StatusCode, true, true, "already-absent", null);
            QueueEvent(absent);
            contexts.RemoveAt(index);
            return CopyEvent(absent);
        }
        if (context.Operation == InitialOperation || context.ConditionalRetryCount > 0)
            return FailOperation(context, response, index,
                "The conditional PUBLISH recovery failed.");
        context.ConditionalRetryCount = context.ConditionalRetryCount + 1;
        context.Operation = InitialOperation;
        context.State = SipPublicationState.Publishing;
        context.RefreshAtMilliseconds = -1;
        IncrementCSeq(context);
        context.Message = BuildRequestMessage(context);
        SipPublicationEvent retry = CreateEvent(context, context.Message, response,
            response.StatusCode, false, false, "conditional-retry", null);
        QueueEvent(retry);
        return CopyEvent(retry);
    }

    SipPublicationEvent RetryMinimumExpiry(
        SipPublicationClientContext context, SipMessage response, int index)
    {
        int minimum = ReadIntegerHeader(response, SipHeaderNames.MinExpires);
        if (context.Operation == RemoveOperation || minimum <= 0
            || minimum < context.Request.Options.MinExpiresSeconds
            || minimum > context.Request.Options.MaxExpiresSeconds)
            return FailOperation(context, response, index,
                "The 423 response does not contain an acceptable Min-Expires value.");
        context.Request.ExpiresSeconds = minimum;
        context.ExpiresSeconds = minimum;
        IncrementCSeq(context);
        context.Message = BuildRequestMessage(context);
        SipPublicationEvent retry = CreateEvent(context, context.Message, response,
            response.StatusCode, false, false, "minimum-expiry", null);
        QueueEvent(retry);
        return CopyEvent(retry);
    }

    SipPublicationEvent FailOperation(
        SipPublicationClientContext context, SipMessage response,
        int index, string error)
    {
        bool remainsPublished = context.EntityTag != null
            && context.ExpiresAtMilliseconds > nowMilliseconds;
        if (remainsPublished && context.CommittedRequest != null)
        {
            context.Request = CopyRequest(context.CommittedRequest);
            RestoreRefreshSchedule(context);
        }
        context.State = SipPublicationState.Failed;
        SipPublicationEvent failed = CreateEvent(context, null, response,
            response.StatusCode, false, true, "failed", error);
        QueueEvent(failed);
        if (remainsPublished) context.State = SipPublicationState.Published;
        if (!remainsPublished) contexts.RemoveAt(index);
        return CopyEvent(failed);
    }

    void RestoreRefreshSchedule(SipPublicationClientContext context)
    {
        long lead = SecondsToMilliseconds(
            context.Request.Options.RefreshBeforeSeconds);
        long refresh = context.ExpiresAtMilliseconds - lead;
        context.RefreshAtMilliseconds = refresh < nowMilliseconds
            ? nowMilliseconds : refresh;
    }

    SipMessage BuildRequestMessage(SipPublicationClientContext context)
    {
        SipPublicationRequest request = context.Request;
        SipUri sender = request.Sender == null
            ? SipUriModelCopy.CreateNormalizedCopy(request.Target)
            : SipUriModelCopy.CreateNormalizedCopy(request.Sender);
        SipNameAddress from = CreateAddress(sender, context.LocalTag);
        SipNameAddress to = CreateAddress(request.Target, null);
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.PreserveOriginal = false;
        string targetText = FormatUri(context.Target, writeOptions);
        string fromText = SipHeaderPrimitives.FormatNameAddressValue(from, writeOptions);
        string toText = SipHeaderPrimitives.FormatNameAddressValue(to, writeOptions);
        bool hasBody = context.Operation == InitialOperation
            || context.Operation == ModifyOperation;
        byte[] body = hasBody ? request.Body : new byte[0];
        string transport = context.Target.Scheme == SipUriScheme.Sips ? "TLS" : "UDP";
        int expires = context.Operation == RemoveOperation ? 0 : request.ExpiresSeconds;
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append("PUBLISH ");
        text.Append(targetText);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/");
        text.Append(transport);
        text.Append(" publication.invalid;branch=");
        text.Append(tokens.CreateBranch());
        text.Append("\r\nMax-Forwards: 70\r\nFrom: ");
        text.Append(fromText);
        text.Append("\r\nTo: ");
        text.Append(toText);
        text.Append("\r\nCall-ID: ");
        text.Append(context.CallId);
        text.Append("\r\nCSeq: ");
        text.Append(context.CSeq.ToString(System.Globalization.CultureInfo.InvariantCulture));
        text.Append(" PUBLISH\r\nEvent: ");
        text.Append(request.EventPackage);
        text.Append("\r\nExpires: ");
        text.Append(expires.ToString(System.Globalization.CultureInfo.InvariantCulture));
        text.Append("\r\n");
        if (context.Operation != InitialOperation)
        {
            text.Append("SIP-If-Match: ");
            text.Append(context.EntityTag);
            text.Append("\r\n");
        }
        if (hasBody && request.ContentType != null)
        {
            text.Append("Content-Type: ");
            text.Append(FormatContentType(request.ContentType));
            text.Append("\r\n");
        }
        text.Append("Content-Length: ");
        text.Append(body.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        text.Append("\r\n\r\n");
        byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(text.ToString());
        byte[] wire = new byte[headerBytes.Length + body.Length];
        System.Array.Copy(headerBytes, 0, wire, 0, headerBytes.Length);
        System.Array.Copy(body, 0, wire, headerBytes.Length, body.Length);
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.MaxBodyBytes = request.Options.MaxBodyBytes;
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(wire, parseOptions);
        if (parsed.Status != SipParseStatus.Complete || parsed.Message == null)
            throw new System.ArgumentException(
                parsed.ErrorMessage == null
                    ? "The PUBLISH request could not be constructed."
                    : parsed.ErrorMessage, "request");
        return parsed.Message;
    }

    SipPublicationClientContext FindActiveContext(SipPublicationRequest request)
    {
        int index = FindEntityTagContext(request);
        if (index < 0)
            throw new System.ArgumentException(
                "The active publication entity-tag was not found.", "request");
        SipPublicationClientContext context = contexts[index];
        if (context.State != SipPublicationState.Published)
            throw new System.ArgumentException(
                "The publication is not ready for another operation.", "request");
        if (request.EventPackage != null
            && request.EventPackage != context.Request.EventPackage)
            throw new System.ArgumentException(
                "The publication event package cannot change.", "request");
        if (request.Target != null && !UrisEqual(request.Target, context.Target))
            throw new System.ArgumentException(
                "The publication target cannot change.", "request");
        return context;
    }

    static SipPublicationRequest MergeRequest(
        SipPublicationClientContext context, SipPublicationRequest supplied, int operation)
    {
        SipPublicationRequest merged = CopyRequest(context.Request);
        merged.EntityTag = context.EntityTag;
        merged.ExpiresSeconds = operation == RemoveOperation
            ? 0 : supplied.ExpiresSeconds;
        merged.Options = CopyOptions(supplied.Options);
        merged.Cancellation = supplied.Cancellation;
        merged.CorrelationId = supplied.CorrelationId;
        merged.AuthenticationContextId = supplied.AuthenticationContextId;
        if (operation == ModifyOperation)
        {
            merged.Body = (byte[])supplied.Body.Clone();
            merged.ContentType = SipReferCore.CopyMediaType(supplied.ContentType);
        }
        return merged;
    }

    void SetLifetime(SipPublicationClientContext context, int seconds)
    {
        context.ExpiresSeconds = seconds;
        long duration = SecondsToMilliseconds(seconds);
        context.ExpiresAtMilliseconds = AddTime(nowMilliseconds, duration);
        long lead = SecondsToMilliseconds(context.Request.Options.RefreshBeforeSeconds);
        long refresh = context.ExpiresAtMilliseconds - lead;
        context.RefreshAtMilliseconds = refresh < nowMilliseconds
            ? nowMilliseconds : refresh;
    }

    static long SecondsToMilliseconds(int seconds)
    {
        return (long)seconds * 1000;
    }

    static long AddTime(long value, long duration)
    {
        return duration > long.MaxValue - value ? long.MaxValue : value + duration;
    }

    static void ValidateCreateRequest(SipPublicationRequest? request)
    {
        ValidateCommonRequest(request);
        if (request == null) throw new System.ArgumentNullException("request");
        if (request.EntityTag != null || request.Body.Length == 0
            || !SipReferCore.IsValidMediaType(request.ContentType)
            || request.ExpiresSeconds <= 0)
            throw new System.ArgumentException(
                "An initial publication requires a body, no entity-tag, and a positive expiry.",
                "request");
    }

    static void ValidateConditionalRequest(SipPublicationRequest? request, int operation)
    {
        ValidateCommonRequest(request);
        if (request == null) throw new System.ArgumentNullException("request");
        if (request.EntityTag == null || !SipAscii.IsToken(request.EntityTag))
            throw new System.ArgumentException(
                "A conditional publication requires a valid entity-tag.", "request");
        bool modify = operation == ModifyOperation;
        if (modify && (request.Body.Length == 0
                || !SipReferCore.IsValidMediaType(request.ContentType)))
            throw new System.ArgumentException(
                "A publication modification requires a body and Content-Type.", "request");
        if (!modify && (request.Body.Length != 0 || request.ContentType != null))
            throw new System.ArgumentException(
                "A publication refresh or removal cannot contain a body.", "request");
        if (operation != RemoveOperation && request.ExpiresSeconds <= 0)
            throw new System.ArgumentException(
                "A publication refresh or modification requires a positive expiry.", "request");
    }

    static void ValidateCommonRequest(SipPublicationRequest? request)
    {
        if (request == null || request.Body == null || request.Options == null)
            throw new System.ArgumentNullException("request");
        if (!SipReferCore.IsValidSipUri(request.Target)
            || request.Sender != null && !SipReferCore.IsValidSipUri(request.Sender)
            || request.EventPackage == null || !SipAscii.IsToken(request.EventPackage)
            || request.Options.MinExpiresSeconds < 0
            || request.Options.MaxExpiresSeconds < request.Options.MinExpiresSeconds
            || request.Options.RefreshBeforeSeconds < 0
            || request.Options.MaxBodyBytes < 0
            || request.Options.MaxAuthenticationAttempts < 0
            || request.ExpiresSeconds < 0
            || request.ExpiresSeconds > request.Options.MaxExpiresSeconds
            || request.Body.Length > request.Options.MaxBodyBytes
            || request.ContentType != null
                && !SipReferCore.IsValidMediaType(request.ContentType))
            throw new System.ArgumentException(
                "The publication target, event, expiry, body, or options are invalid.",
                "request");
        if (request.Cancellation != null && request.Cancellation.Requested)
            throw new System.OperationCanceledException(request.Cancellation.Reason);
    }

    static void ValidateResponse(SipMessage? response)
    {
        if (response == null || response.Kind != SipMessageKind.Response
            || response.StatusCode < 100 || response.StatusCode > 699
            || response.CSeq == null || response.CSeq.Method != SipMethod.Publish
            || !SipAscii.EqualsIgnoreCase(response.CSeq.MethodToken, SipMethods.Publish)
            || SipReferCore.ReadCallId(response) == null)
            throw new System.ArgumentException(
                "A valid PUBLISH response is required.", "message");
    }

    int FindResponseContext(SipMessage response)
    {
        string? callId = SipReferCore.ReadCallId(response);
        int index = 0;
        while (index < contexts.Count)
        {
            SipPublicationClientContext context = contexts[index];
            if (context.CallId == callId && response.CSeq != null
                && context.CSeq == response.CSeq.SequenceNumber
                && context.State != SipPublicationState.Published)
                return index;
            index = index + 1;
        }
        return -1;
    }

    int FindEntityTagContext(SipPublicationRequest request)
    {
        int index = 0;
        while (index < contexts.Count)
        {
            SipPublicationClientContext context = contexts[index];
            if (context.EntityTag == request.EntityTag
                && context.Request.EventPackage == request.EventPackage
                && request.Target != null && UrisEqual(request.Target, context.Target))
                return index;
            index = index + 1;
        }
        return -1;
    }

    static string? ReadSingleTokenHeader(SipMessage message, string name)
    {
        string? result = null;
        int count = 0;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(header.Name, name))
            {
                count = count + 1;
                if (!SipAscii.IsToken(header.Value)) return null;
                result = header.Value;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static int ReadIntegerHeader(SipMessage message, string name)
    {
        int value = -1;
        int count = 0;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(header.Name, name))
            {
                count = count + 1;
                SipNumericParseResult parsed = SipNumericParser.ParseNonNegativeInt32Text(
                    header.Value, int.MaxValue);
                if (!parsed.Success) return -1;
                value = parsed.Value;
            }
            index = index + 1;
        }
        return count == 1 ? value : -1;
    }

    static SipDigestChallenge[] ReadChallenges(
        SipMessage response, SipCredentialScope scope)
    {
        string name = scope == SipCredentialScope.Origin
            ? SipHeaderNames.WwwAuthenticate : SipHeaderNames.ProxyAuthenticate;
        System.Collections.Generic.List<SipDigestChallenge> values =
            new System.Collections.Generic.List<SipDigestChallenge>();
        int index = 0;
        while (response.Headers != null && index < response.Headers.Length)
        {
            SipHeader? header = response.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(header.Name, name))
            {
                SipAuthenticationHeaderResult parsed =
                    SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(
                        header.Name, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.Challenge == null)
                    return new SipDigestChallenge[0];
                values.Add(parsed.Challenge);
            }
            index = index + 1;
        }
        return values.ToArray();
    }

    string? VerifyAuthenticationInformation(
        SipPublicationClientContext context, SipMessage response)
    {
        if (authenticationClient == null || context.AuthenticationAttemptCount == 0)
            return null;
        SipAuthenticationRequest verification = new SipAuthenticationRequest();
        verification.Message = response;
        verification.Target = context.Target;
        verification.Body = response.Body == null ? new byte[0] : response.Body;
        verification.Scope = context.AuthenticationScope;
        verification.CorrelationId = context.Request.AuthenticationContextId == null
            ? context.Request.CorrelationId : context.Request.AuthenticationContextId;
        SipAuthenticationResult verified =
            authenticationClient.VerifyAuthenticationInfo(verification);
        if (verified.Status == SipAuthenticationStatus.Success) return null;
        return verified.ErrorMessage == null
            ? "The PUBLISH Authentication-Info value is invalid."
            : verified.ErrorMessage;
    }

    static bool UrisEqual(SipUri first, SipUri second)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipWriteResult firstResult = SipUriFormatter.FormatUriModel(first, options);
        SipWriteResult secondResult = SipUriFormatter.FormatUriModel(second, options);
        return firstResult.Success && secondResult.Success
            && System.Text.Encoding.UTF8.GetString(firstResult.Bytes)
                == System.Text.Encoding.UTF8.GetString(secondResult.Bytes);
    }

    static SipNameAddress CreateAddress(SipUri? uri, string? tag)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = SipUriModelCopy.CreateNormalizedCopy(uri);
        if (tag != null)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = "tag";
            parameter.Value = tag;
            parameter.HasValue = true;
            address.Parameters = new SipUriParameter[] { parameter };
        }
        return address;
    }

    static string FormatUri(SipUri uri, SipWriteOptions options)
    {
        SipWriteResult result = SipUriFormatter.FormatUriModel(uri, options);
        if (!result.Success)
            throw new System.ArgumentException(
                result.ErrorMessage == null ? "The publication URI is invalid."
                    : result.ErrorMessage, "request");
        return System.Text.Encoding.ASCII.GetString(result.Bytes);
    }

    static string FormatContentType(SipMediaType contentType)
    {
        SipCoreHeaderResult parsed = new SipCoreHeaderResult();
        parsed.Success = true;
        parsed.MediaType = contentType;
        SipWriteResult formatted = SipCoreHeaderCodec.FormatCoreHeaderValue(
            SipHeaderNames.ContentType, parsed, new SipWriteOptions());
        if (!formatted.Success)
            throw new System.ArgumentException(
                "The publication Content-Type is invalid.", "request");
        string line = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = line.IndexOf(':');
        return colon < 0 ? line.Trim() : line.Substring(colon + 1).Trim();
    }

    static void IncrementCSeq(SipPublicationClientContext context)
    {
        if (context.CSeq == int.MaxValue)
            throw new System.InvalidOperationException("The PUBLISH CSeq is exhausted.");
        context.CSeq = context.CSeq + 1;
    }

    SipPublicationEvent CreateEvent(
        SipPublicationClientContext context, SipMessage? message,
        SipMessage? response, int statusCode, bool accepted,
        bool finalValue, string? reason, string? error)
    {
        SipPublicationEvent value = new SipPublicationEvent();
        value.PublicationId = context.PublicationId;
        value.State = context.State;
        value.Request = CopyRequest(context.Request);
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.StatusCode = statusCode;
        value.Accepted = accepted;
        value.Final = finalValue;
        value.EntityTag = context.EntityTag;
        value.ExpiresSeconds = RemainingExpires(context);
        value.RefreshAtMilliseconds = context.RefreshAtMilliseconds;
        value.ExpiresAtMilliseconds = context.ExpiresAtMilliseconds;
        value.AuthenticationAttemptCount = context.AuthenticationAttemptCount;
        value.Reason = reason;
        value.ErrorMessage = error;
        value.CorrelationId = context.Request.CorrelationId;
        return value;
    }

    int RemainingExpires(SipPublicationClientContext context)
    {
        if (context.ExpiresAtMilliseconds < 0) return context.ExpiresSeconds;
        long remaining = context.ExpiresAtMilliseconds - nowMilliseconds;
        if (remaining <= 0) return 0;
        long seconds = (remaining + 999) / 1000;
        return seconds > int.MaxValue ? int.MaxValue : (int)seconds;
    }

    void QueueEvent(SipPublicationEvent value)
    {
        if (events.Count >= SipDefaults.MaxQueueCount)
            throw new System.InvalidOperationException(
                "The publication event queue limit was reached.");
        events.Add(CopyEvent(value));
    }

    static SipPublicationEvent CopyEvent(SipPublicationEvent source)
    {
        SipPublicationEvent copy = new SipPublicationEvent();
        copy.PublicationId = source.PublicationId;
        copy.State = source.State;
        copy.Request = source.Request == null ? null : CopyRequest(source.Request);
        copy.Publication = source.Publication == null ? null
            : SipPublicationModelCopy.CopyPublication(source.Publication);
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Response = SipSnapshotCopy.CopyMessageModel(source.Response);
        copy.StatusCode = source.StatusCode;
        copy.Accepted = source.Accepted;
        copy.Final = source.Final;
        copy.AggregationRequired = source.AggregationRequired;
        copy.EntityTag = source.EntityTag;
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.RefreshAtMilliseconds = source.RefreshAtMilliseconds;
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        copy.AuthenticationAttemptCount = source.AuthenticationAttemptCount;
        copy.Reason = source.Reason;
        copy.Authorization = SipPublicationModelCopy.CopyAuthorization(source.Authorization);
        copy.ErrorMessage = source.ErrorMessage;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipPublicationRequest CopyRequest(SipPublicationRequest source)
    {
        SipPublicationRequest copy = new SipPublicationRequest();
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Sender = source.Sender == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Sender);
        copy.Target = source.Target == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Target);
        copy.EventPackage = source.EventPackage;
        copy.EntityTag = source.EntityTag;
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.Body = source.Body == null ? new byte[0] : (byte[])source.Body.Clone();
        copy.ContentType = SipReferCore.CopyMediaType(source.ContentType);
        copy.Options = CopyOptions(source.Options);
        copy.ServerOptions = SipPublicationModelCopy.CopyServerOptions(source.ServerOptions);
        copy.Cancellation = source.Cancellation;
        copy.CorrelationId = source.CorrelationId;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        return copy;
    }

    static SipPublicationOptions CopyOptions(SipPublicationOptions? source)
    {
        SipPublicationOptions copy = new SipPublicationOptions();
        if (source != null)
        {
            copy.MinExpiresSeconds = source.MinExpiresSeconds;
            copy.MaxExpiresSeconds = source.MaxExpiresSeconds;
            copy.RefreshBeforeSeconds = source.RefreshBeforeSeconds;
            copy.MaxBodyBytes = source.MaxBodyBytes;
            copy.MaxAuthenticationAttempts = source.MaxAuthenticationAttempts;
        }
        return copy;
    }
}
