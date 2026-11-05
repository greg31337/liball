namespace sip;

internal class SipMessageClientCore
{
    object gate = new object();
    System.Collections.Generic.List<SipInstantMessageOperation> operations =
        new System.Collections.Generic.List<SipInstantMessageOperation>();
    System.Collections.Generic.List<SipInstantMessageEvent> events =
        new System.Collections.Generic.List<SipInstantMessageEvent>();
    SipProtocolTokenGenerator tokens;
    ISipAuthenticationClient? authenticationClient;

    internal SipMessageClientCore(ISipRandomSource randomSource)
    {
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }
        tokens = new SipProtocolTokenGenerator(randomSource);
    }

    internal void ConfigureAuthentication(
        ISipAuthenticationClient configuredAuthenticationClient)
    {
        lock (gate)
        {
            if (configuredAuthenticationClient == null)
            {
                throw new System.ArgumentNullException("authenticationClient");
            }
            authenticationClient = configuredAuthenticationClient;
        }
    }

    internal SipInstantMessageEvent Send(SipInstantMessageRequest request)
    {
        lock (gate)
        {
            ValidateRequest(request);
            SipInstantMessageRequest copiedRequest = CopyRequest(request);
            SipInstantMessageOperation operation =
                CreateOperation(copiedRequest);
            if (operations.Count >= SipDefaults.MaxTransactionCount)
            {
                throw new System.InvalidOperationException(
                    "The outstanding MESSAGE operation limit was reached.");
            }
            operations.Add(operation);
            SipInstantMessageEvent created = CreateEvent(operation,
                SipInstantMessageEventKind.RequestCreated, operation.Message,
                null, true, false, null);
            QueueEvent(created);
            return CopyEvent(created);
        }
    }

    internal SipInstantMessageEvent ReceiveResponse(SipMessage response)
    {
        lock (gate)
        {
            ValidateResponse(response);
            int index = FindOperationIndex(response);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The MESSAGE response does not match an outstanding request.",
                    "message");
            }
            SipInstantMessageOperation operation = operations[index];
            if (response.StatusCode < 200)
            {
                SipInstantMessageEvent provisional = CreateEvent(operation,
                    SipInstantMessageEventKind.ProvisionalResponse, null,
                    response, true, false, null);
                QueueEvent(provisional);
                return CopyEvent(provisional);
            }
            if (response.StatusCode == SipStatusCodes.Unauthorized
                || response.StatusCode
                    == SipStatusCodes.ProxyAuthenticationRequired)
            {
                return RetryAuthentication(operation, response, index);
            }
            if (response.StatusCode >= 300 && response.StatusCode < 400)
            {
                return FollowRedirect(operation, response, index);
            }
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                string? verificationError = VerifyAuthenticationInformation(
                    operation, response);
                if (verificationError != null)
                {
                    operations.RemoveAt(index);
                    return CompleteFailure(operation, response,
                        verificationError);
                }
                operations.RemoveAt(index);
                SipInstantMessageEvent delivered = CreateEvent(operation,
                    SipInstantMessageEventKind.Delivered, null, response,
                    true, true, null);
                QueueEvent(delivered);
                return CopyEvent(delivered);
            }

            operations.RemoveAt(index);
            return CompleteFailure(operation, response,
                "The peer rejected the MESSAGE request.");
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

    SipInstantMessageOperation CreateOperation(
        SipInstantMessageRequest request)
    {
        SipInstantMessageOperation operation =
            new SipInstantMessageOperation();
        operation.Request = request;
        operation.InDialog = request.Dialog != null;
        if (request.Dialog != null)
        {
            operation.Target = SipUriModelCopy.CreateNormalizedCopy(
                request.Dialog.RemoteTarget);
            if (request.Dialog.Id == null
                || request.Dialog.Id.CallId == null)
            {
                throw new System.ArgumentException(
                    "The dialog has no protocol identifier.", "request");
            }
            operation.CallId = request.Dialog.Id.CallId;
            operation.LocalTag = request.Dialog.Id.LocalTag == null
                ? "" : request.Dialog.Id.LocalTag;
            operation.CSeq = request.Dialog.LocalCSeq + 1;
        }
        else
        {
            operation.Target = SipUriModelCopy.CreateNormalizedCopy(
                request.Target);
            operation.CallId = tokens.CreateCallId();
            operation.LocalTag = tokens.CreateTag();
            operation.CSeq = 1;
        }
        operation.Message = BuildRequestMessage(operation);
        return operation;
    }

    SipInstantMessageEvent RetryAuthentication(
        SipInstantMessageOperation operation, SipMessage response, int index)
    {
        if (authenticationClient == null)
        {
            operations.RemoveAt(index);
            return CompleteFailure(operation, response,
                "The MESSAGE challenge requires an authentication client.");
        }
        if (operation.AuthenticationAttemptCount
            >= operation.Request.Options.MaxAuthenticationAttempts)
        {
            operations.RemoveAt(index);
            return CompleteFailure(operation, response,
                "The MESSAGE authentication retry limit was reached.");
        }
        SipCredentialScope scope = response.StatusCode
            == SipStatusCodes.Unauthorized
            ? SipCredentialScope.Origin : SipCredentialScope.Proxy;
        SipDigestChallenge[] challenges = ReadChallenges(response, scope);
        if (challenges.Length == 0)
        {
            operations.RemoveAt(index);
            return CompleteFailure(operation, response,
                "The MESSAGE authentication challenge is missing or malformed.");
        }
        IncrementOperationCSeq(operation);
        SipMessage retry = BuildRequestMessage(operation);
        SipAuthenticationRequest authentication =
            new SipAuthenticationRequest();
        authentication.Message = retry;
        authentication.Challenges = challenges;
        authentication.Target = operation.Target;
        authentication.Body = retry.Body;
        authentication.Scope = scope;
        authentication.Attempt = operation.AuthenticationAttemptCount;
        authentication.CorrelationId =
            operation.Request.AuthenticationContextId == null
            ? operation.Request.CorrelationId
            : operation.Request.AuthenticationContextId;
        SipAuthenticationResult authorized =
            authenticationClient.AuthorizeRequest(authentication);
        if (authorized.Status != SipAuthenticationStatus.Success
            || !authorized.RetryAllowed || authorized.Message == null)
        {
            operations.RemoveAt(index);
            return CompleteFailure(operation, response,
                authorized.ErrorMessage == null
                    ? "The MESSAGE request could not be authorized."
                    : authorized.ErrorMessage);
        }
        operation.AuthenticationAttemptCount =
            operation.AuthenticationAttemptCount + 1;
        operation.AuthenticationScope = scope;
        operation.Message = CopyRequiredMessage(authorized.Message);
        SipInstantMessageEvent retried = CreateEvent(operation,
            SipInstantMessageEventKind.RetryCreated, operation.Message,
            response, true, false, null);
        QueueEvent(retried);
        return CopyEvent(retried);
    }

    SipInstantMessageEvent FollowRedirect(
        SipInstantMessageOperation operation, SipMessage response, int index)
    {
        if (!operation.Request.Options.FollowRedirects
            || operation.RedirectCount
                >= operation.Request.Options.MaxRedirects)
        {
            operations.RemoveAt(index);
            return CompleteFailure(operation, response,
                "MESSAGE redirect policy rejected the response.");
        }
        SipUri? target = SelectRedirectTarget(response);
        if (target == null)
        {
            operations.RemoveAt(index);
            return CompleteFailure(operation, response,
                "The MESSAGE redirect has no usable Contact target.");
        }
        if (operation.Target.Scheme == SipUriScheme.Sips
            && target.Scheme != SipUriScheme.Sips)
        {
            operations.RemoveAt(index);
            return CompleteFailure(operation, response,
                "A MESSAGE redirect cannot downgrade a SIPS target.");
        }
        operation.Target = SipUriModelCopy.CreateNormalizedCopy(target);
        operation.RedirectCount = operation.RedirectCount + 1;
        operation.AuthenticationAttemptCount = 0;
        operation.AuthenticationScope = SipCredentialScope.None;
        IncrementOperationCSeq(operation);
        operation.Message = BuildRequestMessage(operation);
        SipInstantMessageEvent redirected = CreateEvent(operation,
            SipInstantMessageEventKind.RetryCreated, operation.Message,
            response, true, false, null);
        QueueEvent(redirected);
        return CopyEvent(redirected);
    }

    SipMessage BuildRequestMessage(SipInstantMessageOperation operation)
    {
        SipInstantMessageRequest request = operation.Request;
        SipUri sender = request.Dialog == null
            ? SelectSender(request.Sender)
            : SipUriModelCopy.CreateNormalizedCopy(request.Dialog.LocalUri);
        SipUri recipient = request.Dialog == null
            ? SipUriModelCopy.CreateNormalizedCopy(request.Target)
            : SipUriModelCopy.CreateNormalizedCopy(request.Dialog.RemoteUri);
        string? localTag = request.Dialog == null
            ? operation.LocalTag : request.Dialog.Id == null
                ? null : request.Dialog.Id.LocalTag;
        string? remoteTag = request.Dialog == null
            ? null : request.Dialog.Id == null
                ? null : request.Dialog.Id.RemoteTag;
        SipNameAddress from = CreateAddress(sender, localTag);
        SipNameAddress to = CreateAddress(recipient, remoteTag);
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.PreserveOriginal = false;
        string targetText = FormatUri(operation.Target, writeOptions);
        string fromText = SipHeaderPrimitives.FormatNameAddressValue(
            from, writeOptions);
        string toText = SipHeaderPrimitives.FormatNameAddressValue(
            to, writeOptions);
        string contentTypeText = request.ContentType == null
            ? "" : FormatContentType(request.ContentType);
        string transport = operation.Target.Scheme == SipUriScheme.Sips
            ? "TLS" : "UDP";

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append("MESSAGE ");
        text.Append(targetText);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/");
        text.Append(transport);
        text.Append(" message.invalid;branch=");
        text.Append(tokens.CreateBranch());
        text.Append("\r\nMax-Forwards: 70\r\nFrom: ");
        text.Append(fromText);
        text.Append("\r\nTo: ");
        text.Append(toText);
        text.Append("\r\nCall-ID: ");
        text.Append(operation.CallId);
        text.Append("\r\nCSeq: ");
        text.Append(operation.CSeq.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append(" MESSAGE\r\n");
        if (request.ContentType != null)
        {
            text.Append("Content-Type: ");
            text.Append(contentTypeText);
            text.Append("\r\n");
        }
        text.Append("Content-Length: ");
        text.Append(request.Body.Length.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append("\r\n\r\n");
        byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(text.ToString());
        byte[] wire = new byte[headerBytes.Length + request.Body.Length];
        System.Array.Copy(headerBytes, 0, wire, 0, headerBytes.Length);
        System.Array.Copy(request.Body, 0, wire, headerBytes.Length,
            request.Body.Length);
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.MaxBodyBytes = request.Options.MaxBodyBytes;
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(
            wire, parseOptions);
        if (parsed.Status != SipParseStatus.Complete
            || parsed.Message == null)
        {
            throw new System.ArgumentException(
                parsed.ErrorMessage == null
                    ? "The MESSAGE request could not be constructed."
                    : parsed.ErrorMessage, "request");
        }
        if (request.Dialog != null)
        {
            SipRouteRouting.ApplyRouteSet(parsed.Message,
                operation.Target, request.Dialog.RouteSet);
        }
        return parsed.Message;
    }

    static void ValidateRequest(SipInstantMessageRequest? request)
    {
        if (request == null || request.Body == null
            || request.Options == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.Options.MaxBodyBytes < 0
            || request.Options.MaxAuthenticationAttempts < 0
            || request.Options.MaxRedirects < 0)
        {
            throw new System.ArgumentException(
                "Valid MESSAGE size and retry limits are required.", "request");
        }
        if (request.Body.Length > request.Options.MaxBodyBytes)
        {
            throw new System.ArgumentException(
                "The MESSAGE body exceeds the configured size policy.",
                "request");
        }
        if (request.Body.Length > 0
            && !IsValidContentType(request.ContentType))
        {
            throw new System.ArgumentException(
                "A valid Content-Type is required for a nonempty MESSAGE body.",
                "request");
        }
        if (request.ContentType != null
            && !IsValidContentType(request.ContentType))
        {
            throw new System.ArgumentException(
                "The MESSAGE Content-Type is invalid.", "request");
        }
        if (request.Dialog == null && !IsValidSipUri(request.Target))
        {
            throw new System.ArgumentException(
                "A valid MESSAGE target is required.", "request");
        }
        if (request.Sender != null && !IsValidSipUri(request.Sender))
        {
            throw new System.ArgumentException(
                "The MESSAGE sender is invalid.", "request");
        }
        if (request.Dialog != null)
        {
            ValidateDialog(request.Dialog);
        }
        if (request.Cancellation != null && request.Cancellation.Requested)
        {
            throw new System.OperationCanceledException(
                request.Cancellation.Reason == null
                    ? "The MESSAGE operation was cancelled."
                    : request.Cancellation.Reason);
        }
    }

    static void ValidateDialog(SipDialogSnapshot dialog)
    {
        if ((dialog.State != SipDialogState.Early
                && dialog.State != SipDialogState.Confirmed)
            || dialog.Id == null || dialog.Id.CallId == null
            || dialog.Id.CallId.Length == 0
            || dialog.Id.LocalTag == null || dialog.Id.LocalTag.Length == 0
            || dialog.Id.RemoteTag == null || dialog.Id.RemoteTag.Length == 0
            || !IsValidSipUri(dialog.LocalUri)
            || !IsValidSipUri(dialog.RemoteUri)
            || !IsValidSipUri(dialog.RemoteTarget)
            || dialog.LocalCSeq < 0 || dialog.LocalCSeq == int.MaxValue
            || dialog.RouteSet == null)
        {
            throw new System.ArgumentException(
                "A live dialog with complete local and remote state is required.",
                "request");
        }
    }

    static void ValidateResponse(SipMessage? response)
    {
        if (response == null || response.Kind != SipMessageKind.Response
            || response.StatusCode < 100 || response.StatusCode > 699
            || response.CSeq == null
            || response.CSeq.Method != SipMethod.Message
            || !SipAscii.EqualsIgnoreCase(
                response.CSeq.MethodToken, SipMethods.Message)
            || ReadCallId(response) == null)
        {
            throw new System.ArgumentException(
                "A valid MESSAGE response is required.", "message");
        }
    }

    int FindOperationIndex(SipMessage response)
    {
        string? callId = ReadCallId(response);
        int index = 0;
        while (index < operations.Count)
        {
            SipInstantMessageOperation operation = operations[index];
            if (operation.CallId == callId && response.CSeq != null
                && operation.CSeq == response.CSeq.SequenceNumber)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static string? ReadCallId(SipMessage message)
    {
        string? result = null;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.CallId))
            {
                if (result != null)
                {
                    return null;
                }
                result = header.Value;
            }
            index = index + 1;
        }
        return result;
    }

    static SipDigestChallenge[] ReadChallenges(
        SipMessage response, SipCredentialScope scope)
    {
        string name = scope == SipCredentialScope.Origin
            ? SipHeaderNames.WwwAuthenticate
            : SipHeaderNames.ProxyAuthenticate;
        System.Collections.Generic.List<SipDigestChallenge> values =
            new System.Collections.Generic.List<SipDigestChallenge>();
        int index = 0;
        while (response.Headers != null && index < response.Headers.Length)
        {
            SipHeader? header = response.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                SipAuthenticationHeaderResult parsed =
                    SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(
                        header.Name, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.Challenge == null)
                {
                    return new SipDigestChallenge[0];
                }
                values.Add(parsed.Challenge);
            }
            index = index + 1;
        }
        return values.ToArray();
    }

    string? VerifyAuthenticationInformation(
        SipInstantMessageOperation operation, SipMessage response)
    {
        if (authenticationClient == null
            || operation.AuthenticationAttemptCount == 0)
        {
            return null;
        }
        SipAuthenticationRequest verification =
            new SipAuthenticationRequest();
        verification.Message = response;
        verification.Target = operation.Target;
        verification.Body = response.Body == null
            ? new byte[0] : response.Body;
        verification.Scope = operation.AuthenticationScope;
        verification.CorrelationId =
            operation.Request.AuthenticationContextId == null
            ? operation.Request.CorrelationId
            : operation.Request.AuthenticationContextId;
        SipAuthenticationResult verified =
            authenticationClient.VerifyAuthenticationInfo(verification);
        if (verified.Status == SipAuthenticationStatus.Success)
        {
            return null;
        }
        return verified.ErrorMessage == null
            ? "The MESSAGE Authentication-Info value is invalid."
            : verified.ErrorMessage;
    }

    static SipUri? SelectRedirectTarget(SipMessage response)
    {
        SipContactValue? selected = null;
        int selectedQuality = -1;
        int index = 0;
        while (response.ContactValues != null
            && index < response.ContactValues.Length)
        {
            SipContactValue? candidate = response.ContactValues[index];
            int quality = candidate == null
                || candidate.QualityThousandths < 0
                ? 1000 : candidate.QualityThousandths;
            if (candidate != null && candidate.Address != null
                && IsValidSipUri(candidate.Address.Uri)
                && quality > selectedQuality)
            {
                selected = candidate;
                selectedQuality = quality;
            }
            index = index + 1;
        }
        return selected == null || selected.Address == null
            ? null : selected.Address.Uri;
    }

    static bool IsValidSipUri(SipUri? uri)
    {
        return uri != null
            && (uri.Scheme == SipUriScheme.Sip
                || uri.Scheme == SipUriScheme.Sips)
            && SipUriValidator.ValidateUriModel(uri).Length == 0;
    }

    static bool IsValidContentType(SipMediaType? contentType)
    {
        if (contentType == null || !SipAscii.IsToken(contentType.Type)
            || !SipAscii.IsToken(contentType.Subtype)
            || contentType.Parameters == null)
        {
            return false;
        }
        SipCoreHeaderResult parsed = new SipCoreHeaderResult();
        parsed.Success = true;
        parsed.MediaType = contentType;
        SipWriteResult formatted = SipCoreHeaderCodec.FormatCoreHeaderValue(
            SipHeaderNames.ContentType, parsed, new SipWriteOptions());
        return formatted.Success;
    }

    static string FormatContentType(SipMediaType contentType)
    {
        SipCoreHeaderResult parsed = new SipCoreHeaderResult();
        parsed.Success = true;
        parsed.MediaType = contentType;
        SipWriteResult formatted = SipCoreHeaderCodec.FormatCoreHeaderValue(
            SipHeaderNames.ContentType, parsed, new SipWriteOptions());
        if (!formatted.Success)
        {
            throw new System.ArgumentException(
                "The MESSAGE Content-Type is invalid.", "request");
        }
        string line = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = line.IndexOf(':');
        return colon < 0 ? line.Trim() : line.Substring(colon + 1).Trim();
    }

    static SipUri SelectSender(SipUri? sender)
    {
        if (sender != null)
        {
            return SipUriModelCopy.CreateNormalizedCopy(sender);
        }
        SipUri anonymous = new SipUri();
        anonymous.Scheme = SipUriScheme.Sip;
        anonymous.SchemeToken = "sip";
        anonymous.User = "anonymous";
        anonymous.Host = "anonymous.invalid";
        return anonymous;
    }

    static SipNameAddress CreateAddress(SipUri uri, string? tag)
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
        SipWriteResult formatted = SipUriFormatter.FormatUriModel(uri, options);
        if (!formatted.Success)
        {
            throw new System.ArgumentException(
                formatted.ErrorMessage == null
                    ? "The MESSAGE target is invalid."
                    : formatted.ErrorMessage, "request");
        }
        return System.Text.Encoding.ASCII.GetString(formatted.Bytes);
    }

    static void IncrementOperationCSeq(
        SipInstantMessageOperation operation)
    {
        if (operation.CSeq == int.MaxValue)
        {
            throw new System.InvalidOperationException(
                "The MESSAGE CSeq is exhausted.");
        }
        operation.CSeq = operation.CSeq + 1;
    }

    SipInstantMessageEvent CompleteFailure(
        SipInstantMessageOperation operation, SipMessage response,
        string error)
    {
        SipInstantMessageEvent failed = CreateEvent(operation,
            SipInstantMessageEventKind.Failed, null, response,
            false, true, error);
        QueueEvent(failed);
        return CopyEvent(failed);
    }

    static SipInstantMessageEvent CreateEvent(
        SipInstantMessageOperation operation,
        SipInstantMessageEventKind kind, SipMessage? message,
        SipMessage? response, bool success, bool finalValue,
        string? error)
    {
        SipInstantMessageEvent value = new SipInstantMessageEvent();
        value.Kind = kind;
        value.Request = CopyRequest(operation.Request);
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.Success = success;
        value.Final = finalValue;
        value.InDialog = operation.InDialog;
        value.StatusCode = response == null ? 0 : response.StatusCode;
        value.AuthenticationAttemptCount =
            operation.AuthenticationAttemptCount;
        value.RedirectCount = operation.RedirectCount;
        value.ErrorMessage = error;
        value.CorrelationId = operation.Request.CorrelationId;
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
        copy.Request = source.Request == null
            ? null : CopyRequest(source.Request);
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

    static SipInstantMessageRequest CopyRequest(
        SipInstantMessageRequest source)
    {
        SipInstantMessageRequest copy = new SipInstantMessageRequest();
        copy.Sender = source.Sender == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(source.Sender);
        copy.Target = source.Target == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(source.Target);
        copy.Dialog = SipSnapshotCopy.CopyDialogSnapshot(source.Dialog);
        copy.Body = source.Body == null
            ? new byte[0] : (byte[])source.Body.Clone();
        copy.ContentType = CopyContentType(source.ContentType);
        copy.RequestDeliveryNotification =
            source.RequestDeliveryNotification;
        copy.Options = CopyOptions(source.Options);
        copy.Cancellation = source.Cancellation;
        copy.CorrelationId = source.CorrelationId;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        return copy;
    }

    static SipInstantMessageOptions CopyOptions(
        SipInstantMessageOptions? source)
    {
        SipInstantMessageOptions copy = new SipInstantMessageOptions();
        if (source != null)
        {
            copy.MaxBodyBytes = source.MaxBodyBytes;
            copy.FollowRedirects = source.FollowRedirects;
            copy.MaxRedirects = source.MaxRedirects;
            copy.MaxAuthenticationAttempts =
                source.MaxAuthenticationAttempts;
        }
        return copy;
    }

    static SipMediaType? CopyContentType(SipMediaType? source)
    {
        if (source == null)
        {
            return null;
        }
        SipOfferAnswer holder = new SipOfferAnswer();
        holder.ContentType = source;
        SipOfferAnswer? copied = SipSnapshotCopy.CopyOfferAnswer(holder);
        return copied == null ? null : copied.ContentType;
    }

    static SipMessage CopyRequiredMessage(SipMessage source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The MESSAGE could not be copied.");
        }
        return copy;
    }
}
