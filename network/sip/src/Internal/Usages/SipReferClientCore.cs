namespace sip;

internal class SipReferClientCore
{
    object gate = new object();
    System.Collections.Generic.List<SipReferClientContext> contexts =
        new System.Collections.Generic.List<SipReferClientContext>();
    System.Collections.Generic.List<SipReferEvent> events =
        new System.Collections.Generic.List<SipReferEvent>();
    SipProtocolTokenGenerator tokens;
    ISipDialogLayer? dialogLayer;
    ISipAuthenticationClient? authenticationClient;

    internal SipReferClientCore(ISipRandomSource randomSource)
    {
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }
        tokens = new SipProtocolTokenGenerator(randomSource);
    }

    internal void ConfigureDialogLayer(ISipDialogLayer configuredDialogLayer)
    {
        lock (gate)
        {
            if (configuredDialogLayer == null)
            {
                throw new System.ArgumentNullException("dialogLayer");
            }
            dialogLayer = configuredDialogLayer;
        }
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

    internal SipReferEvent Send(SipReferRequest request)
    {
        lock (gate)
        {
            ValidateRequest(request);
            SipReferRequest copied = CopyRequest(request);
            SipMessage message = BuildRequest(copied);
            string? callId = SipReferCore.ReadCallId(message);
            if (callId == null || message.CSeq == null)
            {
                throw new System.InvalidOperationException(
                    "The REFER protocol identity is unavailable.");
            }
            SipReferClientContext context = new SipReferClientContext();
            context.Request = copied;
            context.Message = SipReferCore.CopyMessage(message);
            context.CallId = callId;
            context.CSeq = message.CSeq.SequenceNumber;
            context.SubscriptionId = callId + ":"
                + context.CSeq.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            context.DialogId = copied.DialogId == null ? null
                : SipReferCore.CopyDialogId(copied.DialogId);
            contexts.Add(context);
            SipReferEvent value = CreateEvent(context,
                SipReferState.Sending, message, null, null, 0,
                false, false, null);
            QueueEvent(value);
            return CopyEvent(value);
        }
    }

    internal SipReferEvent ReceiveResponse(SipMessage response)
    {
        lock (gate)
        {
            ValidateResponse(response);
            int index = FindResponseContext(response);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The REFER response does not match an outstanding request.",
                    "message");
            }
            SipReferClientContext context = contexts[index];
            if (response.StatusCode < 200)
            {
                SipReferEvent provisional = CreateEvent(context,
                    SipReferState.Sending, null, response, null,
                    response.StatusCode, false, false, null);
                QueueEvent(provisional);
                return CopyEvent(provisional);
            }
            if (response.StatusCode == SipStatusCodes.Unauthorized
                || response.StatusCode
                    == SipStatusCodes.ProxyAuthenticationRequired)
            {
                return RetryAuthentication(context, response, index);
            }
            context.ReferResponseFinal = true;
            if (response.StatusCode < 300)
            {
                UpdateDialogFromResponse(context, response);
                bool noSubscription = context.Request.NoSubscription
                    && SipReferCore.ReadReferSubFalse(response);
                SipReferState state = noSubscription
                    ? SipReferState.Succeeded
                    : context.SubscriptionFinal
                        ? SipReferState.Succeeded
                        : SipReferState.Accepted;
                SipReferEvent accepted = CreateEvent(context, state,
                    null, response, null, response.StatusCode,
                    true, noSubscription || context.SubscriptionFinal, null);
                accepted.NoSubscription = noSubscription;
                if (noSubscription || context.SubscriptionFinal)
                {
                    contexts.RemoveAt(index);
                }
                QueueEvent(accepted);
                return CopyEvent(accepted);
            }
            contexts.RemoveAt(index);
            SipReferEvent failed = CreateEvent(context,
                SipReferState.Failed, null, response, null,
                response.StatusCode, false, true,
                "The peer rejected the REFER request.");
            QueueEvent(failed);
            return CopyEvent(failed);
        }
    }

    internal SipReferEvent ReceiveNotification(SipMessage notification)
    {
        lock (gate)
        {
            ValidateNotification(notification);
            SipExtensionHeaderValue? eventValue =
                SipReferCore.ParseEventHeader(notification);
            SipExtensionHeaderValue? subscription =
                SipReferCore.ParseSubscriptionStateHeader(notification);
            SipSipfragParse sipfrag = SipReferCore.ParseSipfrag(notification);
            if (eventValue == null
                || !SipAscii.EqualsIgnoreCase(eventValue.Token, "refer")
                || subscription == null || !sipfrag.Success)
            {
                throw new System.ArgumentException(
                    sipfrag.ErrorMessage == null
                        ? "A valid refer-event NOTIFY is required."
                        : sipfrag.ErrorMessage, "message");
            }
            int index = FindNotificationContext(
                notification, eventValue.SecondaryToken);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The NOTIFY does not match a REFER subscription.",
                    "message");
            }
            SipReferClientContext context = contexts[index];
            UpdateDialogFromNotification(context, notification);
            bool terminated = SipAscii.EqualsIgnoreCase(
                subscription.Token, "terminated");
            SipReferState state = SelectProgressState(
                sipfrag.StatusCode, terminated);
            bool finalValue = terminated;
            bool success = terminated
                && sipfrag.StatusCode >= 200
                && sipfrag.StatusCode < 300;
            if (terminated)
            {
                context.SubscriptionFinal = true;
            }
            SipMessage? response = SipUasResponseCore.CreateResponseMessage(
                notification, SipStatusCodes.Ok, SipReasonPhrases.Ok);
            SipReferEvent value = CreateEvent(context, state,
                notification, response, null, sipfrag.StatusCode,
                success, finalValue, terminated && !success
                    ? "The referred operation failed." : null);
            if (context.ReferResponseFinal && context.SubscriptionFinal)
            {
                contexts.RemoveAt(index);
            }
            QueueEvent(value);
            return CopyEvent(value);
        }
    }

    internal SipReferEvent PollEvent()
    {
        lock (gate)
        {
            if (events.Count == 0)
            {
                return new SipReferEvent();
            }
            SipReferEvent value = events[0];
            events.RemoveAt(0);
            return CopyEvent(value);
        }
    }

    SipMessage BuildRequest(SipReferRequest request)
    {
        SipMessage message;
        if (request.DialogId != null)
        {
            if (dialogLayer == null)
            {
                throw new System.InvalidOperationException(
                    "A REFER dialog layer has not been configured.");
            }
            message = dialogLayer.CreateRequest(
                request.DialogId, SipMethods.Refer);
        }
        else
        {
            message = BuildOutsideDialogRequest(request);
        }
        message.Body = (byte[])request.Body.Clone();
        message.ContentType = SipReferCore.CopyMediaType(request.ContentType);
        SipReferCore.ReplaceBodyHeaders(message);
        AppendReferHeaders(message, request);
        return message;
    }

    SipMessage BuildOutsideDialogRequest(SipReferRequest request)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        string target = FormatUri(request.Target, options, "target");
        SipNameAddress from = CreateAddress(request.Sender, tokens.CreateTag());
        SipNameAddress to = CreateAddress(request.Target, null);
        string transport = request.Target != null
            && request.Target.Scheme == SipUriScheme.Sips ? "TLS" : "UDP";
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append("REFER ");
        text.Append(target);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/");
        text.Append(transport);
        text.Append(" refer.invalid;branch=");
        text.Append(tokens.CreateBranch());
        text.Append("\r\nMax-Forwards: 70\r\nFrom: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(from, options));
        text.Append("\r\nTo: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(to, options));
        text.Append("\r\nCall-ID: ");
        text.Append(tokens.CreateCallId());
        text.Append("\r\nCSeq: 1 REFER\r\nContent-Length: 0\r\n\r\n");
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(
            System.Text.Encoding.UTF8.GetBytes(text.ToString()),
            new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete
            || parsed.Message == null)
        {
            throw new System.ArgumentException(
                "The out-of-dialog REFER could not be constructed.",
                "request");
        }
        return parsed.Message;
    }

    static void AppendReferHeaders(
        SipMessage message, SipReferRequest request)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        string referTo = SipHeaderPrimitives.FormatNameAddressValue(
            request.ReferTo, options);
        SipReferCore.AppendHeader(message, SipHeaderNames.ReferTo, referTo);
        SipNameAddress contact = CreateAddress(request.Contact, null);
        SipReferCore.AppendHeader(message, SipHeaderNames.Contact,
            SipHeaderPrimitives.FormatNameAddressValue(contact, options));
        SipContactValue contactValue = new SipContactValue();
        contactValue.Address = contact;
        message.ContactValues = new SipContactValue[] { contactValue };
        if (request.ReferredBy != null)
        {
            SipReferCore.AppendHeader(message, SipHeaderNames.ReferredBy,
                SipHeaderPrimitives.FormatNameAddressValue(
                    request.ReferredBy, options));
        }
        if (request.NoSubscription)
        {
            SipReferCore.AppendHeader(message,
                SipHeaderNames.ReferSub, "false");
            SipReferCore.AppendHeader(message,
                request.RequireNoSubscription
                    ? SipHeaderNames.Require : SipHeaderNames.Supported,
                SipOptionTags.NoReferSub);
        }
        message.OriginalBytes = new byte[0];
    }

    SipReferEvent RetryAuthentication(
        SipReferClientContext context, SipMessage response, int index)
    {
        if (authenticationClient == null
            || context.AuthenticationAttempts
                >= context.Request.Options.MaxAuthenticationAttempts)
        {
            contexts.RemoveAt(index);
            SipReferEvent failed = CreateEvent(context,
                SipReferState.Failed, null, response, null,
                response.StatusCode, false, true,
                authenticationClient == null
                    ? "The REFER challenge requires an authentication client."
                    : "The REFER authentication retry limit was reached.");
            QueueEvent(failed);
            return CopyEvent(failed);
        }
        SipCredentialScope scope = response.StatusCode
            == SipStatusCodes.Unauthorized
            ? SipCredentialScope.Origin : SipCredentialScope.Proxy;
        SipDigestChallenge[] challenges = ReadChallenges(response, scope);
        if (challenges.Length == 0)
        {
            contexts.RemoveAt(index);
            SipReferEvent malformed = CreateEvent(context,
                SipReferState.Failed, null, response, null,
                response.StatusCode, false, true,
                "The REFER authentication challenge is malformed.");
            QueueEvent(malformed);
            return CopyEvent(malformed);
        }
        IncrementCSeq(context);
        context.SubscriptionId = context.CallId + ":"
            + context.CSeq.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        SipMessage retry = CreateRetryMessage(context);
        SipAuthenticationRequest authentication =
            new SipAuthenticationRequest();
        authentication.Message = retry;
        authentication.Challenges = challenges;
        authentication.Target = retry.RequestUri;
        authentication.Body = retry.Body;
        authentication.Scope = scope;
        authentication.Attempt = context.AuthenticationAttempts;
        authentication.CorrelationId =
            context.Request.AuthenticationContextId == null
            ? context.Request.CorrelationId
            : context.Request.AuthenticationContextId;
        SipAuthenticationResult authorized =
            authenticationClient.AuthorizeRequest(authentication);
        if (authorized.Status != SipAuthenticationStatus.Success
            || !authorized.RetryAllowed || authorized.Message == null)
        {
            contexts.RemoveAt(index);
            SipReferEvent denied = CreateEvent(context,
                SipReferState.Failed, null, response, null,
                response.StatusCode, false, true,
                authorized.ErrorMessage == null
                    ? "The REFER request could not be authorized."
                    : authorized.ErrorMessage);
            QueueEvent(denied);
            return CopyEvent(denied);
        }
        context.AuthenticationAttempts = context.AuthenticationAttempts + 1;
        context.AuthenticationScope = scope;
        context.Message = SipReferCore.CopyMessage(authorized.Message);
        SipReferEvent retried = CreateEvent(context,
            SipReferState.Sending, authorized.Message, response, null,
            response.StatusCode, false, false, null);
        QueueEvent(retried);
        return CopyEvent(retried);
    }

    static void ValidateRequest(SipReferRequest? request)
    {
        if (request == null || request.ReferTo == null
            || request.ReferTo.Uri == null || request.Contact == null
            || request.Body == null || request.Options == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (!SipReferCore.IsValidSipOrAbsoluteUri(request.ReferTo.Uri)
            || !SipReferCore.IsValidSipUri(request.Contact)
            || request.Options.MaxBodyBytes < 0
            || request.Options.MaxAuthenticationAttempts < 0
            || request.Body.Length > request.Options.MaxBodyBytes
            || request.Body.Length > 0
                && !SipReferCore.IsValidMediaType(request.ContentType)
            || request.ContentType != null
                && !SipReferCore.IsValidMediaType(request.ContentType))
        {
            throw new System.ArgumentException(
                "The REFER target, body, or limits are invalid.", "request");
        }
        if (request.DialogId == null
            && (!SipReferCore.IsValidSipUri(request.Target)
                || !SipReferCore.IsValidSipUri(request.Sender)))
        {
            throw new System.ArgumentException(
                "An out-of-dialog REFER requires valid sender and target URIs.",
                "request");
        }
        if (request.DialogId != null)
        {
            ValidateDialogId(request.DialogId);
        }
        if (request.RequireNoSubscription && !request.NoSubscription)
        {
            throw new System.ArgumentException(
                "Require norefersub requires Refer-Sub false.", "request");
        }
        if (request.Cancellation != null && request.Cancellation.Requested)
        {
            throw new System.OperationCanceledException(
                request.Cancellation.Reason);
        }
    }

    static void ValidateResponse(SipMessage? response)
    {
        if (response == null || response.Kind != SipMessageKind.Response
            || response.StatusCode < 100 || response.StatusCode > 699
            || response.CSeq == null
            || response.CSeq.Method != SipMethod.Refer
            || SipReferCore.ReadCallId(response) == null)
        {
            throw new System.ArgumentException(
                "A valid REFER response is required.", "message");
        }
    }

    static void ValidateNotification(SipMessage? message)
    {
        if (message == null || message.Kind != SipMessageKind.Request
            || message.Method != SipMethod.Notify
            || !SipAscii.EqualsIgnoreCase(
                message.MethodToken, SipMethods.Notify))
        {
            throw new System.ArgumentException(
                "A NOTIFY request is required.", "message");
        }
        SipValidationIssue[] issues =
            SipMessageValidator.ValidateMessageModel(message, true);
        if (SipMessageValidator.HasErrorIssues(issues))
        {
            throw new System.ArgumentException(
                "The refer-event NOTIFY is malformed.", "message");
        }
    }

    static void ValidateDialogId(SipDialogId dialogId)
    {
        if (dialogId.CallId == null || dialogId.CallId.Length == 0
            || dialogId.LocalTag == null || dialogId.LocalTag.Length == 0
            || dialogId.RemoteTag == null || dialogId.RemoteTag.Length == 0)
        {
            throw new System.ArgumentException(
                "A complete REFER dialog identifier is required.",
                "request");
        }
    }

    int FindResponseContext(SipMessage response)
    {
        string? callId = SipReferCore.ReadCallId(response);
        int index = 0;
        while (index < contexts.Count)
        {
            if (contexts[index].CallId == callId
                && response.CSeq != null
                && contexts[index].CSeq == response.CSeq.SequenceNumber)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    int FindNotificationContext(
        SipMessage notification, string? eventId)
    {
        string? callId = SipReferCore.ReadCallId(notification);
        int found = -1;
        int index = 0;
        while (index < contexts.Count)
        {
            bool idMatches = eventId == null || eventId.Length == 0
                || eventId == contexts[index].CSeq.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            if (contexts[index].CallId == callId && idMatches)
            {
                if (found >= 0 && eventId == null)
                {
                    return -1;
                }
                found = index;
            }
            index = index + 1;
        }
        return found;
    }

    void UpdateDialogFromResponse(
        SipReferClientContext context, SipMessage response)
    {
        if (context.DialogId != null)
        {
            return;
        }
        string? localTag = SipReferCore.ReadTag(response.From);
        string? remoteTag = SipReferCore.ReadTag(response.To);
        if (localTag != null && remoteTag != null)
        {
            SipDialogId id = new SipDialogId();
            id.CallId = context.CallId;
            id.LocalTag = localTag;
            id.RemoteTag = remoteTag;
            context.DialogId = id;
        }
    }

    void UpdateDialogFromNotification(
        SipReferClientContext context, SipMessage notification)
    {
        if (context.DialogId != null)
        {
            return;
        }
        string? localTag = SipReferCore.ReadTag(notification.To);
        string? remoteTag = SipReferCore.ReadTag(notification.From);
        if (localTag != null && remoteTag != null)
        {
            SipDialogId id = new SipDialogId();
            id.CallId = context.CallId;
            id.LocalTag = localTag;
            id.RemoteTag = remoteTag;
            context.DialogId = id;
        }
    }

    SipMessage CreateRetryMessage(SipReferClientContext context)
    {
        SipMessage retry = SipReferCore.CopyMessage(context.Message);
        if (retry.CSeq != null)
        {
            retry.CSeq.SequenceNumber = context.CSeq;
        }
        SetCSeqHeader(retry, context.CSeq);
        retry.OriginalBytes = new byte[0];
        return retry;
    }

    static void SetCSeqHeader(SipMessage message, int cseq)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.CSeq))
            {
                header.Value = cseq.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                    + " REFER";
                header.OriginalBytes = new byte[0];
                return;
            }
            index = index + 1;
        }
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
        while (index < response.Headers.Length)
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

    static SipReferState SelectProgressState(int status, bool terminated)
    {
        if (terminated)
        {
            return status >= 200 && status < 300
                ? SipReferState.Succeeded : SipReferState.Failed;
        }
        return status <= 100
            ? SipReferState.Pending : SipReferState.Active;
    }

    static SipReferEvent CreateEvent(
        SipReferClientContext context, SipReferState state,
        SipMessage? message, SipMessage? response,
        SipMessage? notification, int statusCode, bool success,
        bool finalValue, string? error)
    {
        SipReferEvent value = new SipReferEvent();
        value.State = state;
        value.Request = CopyRequest(context.Request);
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.Notification = SipSnapshotCopy.CopyMessageModel(notification);
        value.ReferTo = SipReferCore.CopyNameAddress(
            context.Request.ReferTo);
        value.DialogId = context.DialogId == null ? null
            : SipReferCore.CopyDialogId(context.DialogId);
        value.SubscriptionId = context.SubscriptionId;
        value.StatusCode = statusCode;
        value.Success = success;
        value.Final = finalValue;
        value.ErrorMessage = error;
        value.CorrelationId = context.Request.CorrelationId;
        return value;
    }

    void QueueEvent(SipReferEvent value)
    {
        if (events.Count >= SipDefaults.MaxQueueCount)
        {
            throw new System.InvalidOperationException(
                "The REFER event queue limit was reached.");
        }
        events.Add(CopyEvent(value));
    }

    static SipReferEvent CopyEvent(SipReferEvent source)
    {
        SipReferEvent copy = new SipReferEvent();
        copy.State = source.State;
        copy.Request = source.Request == null
            ? null : CopyRequest(source.Request);
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Response = SipSnapshotCopy.CopyMessageModel(source.Response);
        copy.Notification =
            SipSnapshotCopy.CopyMessageModel(source.Notification);
        copy.ReferTo = SipReferCore.CopyNameAddress(source.ReferTo);
        copy.DialogId = source.DialogId == null ? null
            : SipReferCore.CopyDialogId(source.DialogId);
        copy.SubscriptionId = source.SubscriptionId;
        copy.NoSubscription = source.NoSubscription;
        copy.StatusCode = source.StatusCode;
        copy.Success = source.Success;
        copy.Final = source.Final;
        copy.ErrorMessage = source.ErrorMessage;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipReferRequest CopyRequest(SipReferRequest source)
    {
        SipReferRequest copy = new SipReferRequest();
        copy.DialogId = source.DialogId == null ? null
            : SipReferCore.CopyDialogId(source.DialogId);
        copy.Target = source.Target == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Target);
        copy.Sender = source.Sender == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Sender);
        copy.ReferTo = SipReferCore.CopyNameAddress(source.ReferTo);
        copy.Contact = source.Contact == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Contact);
        copy.ReferredBy = SipReferCore.CopyNameAddress(source.ReferredBy);
        copy.NoSubscription = source.NoSubscription;
        copy.RequireNoSubscription = source.RequireNoSubscription;
        copy.Body = source.Body == null
            ? new byte[0] : (byte[])source.Body.Clone();
        copy.ContentType = SipReferCore.CopyMediaType(source.ContentType);
        copy.Options = CopyOptions(source.Options);
        copy.Cancellation = source.Cancellation;
        copy.CorrelationId = source.CorrelationId;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        return copy;
    }

    static SipReferOptions CopyOptions(SipReferOptions? source)
    {
        SipReferOptions copy = new SipReferOptions();
        if (source != null)
        {
            copy.MaxBodyBytes = source.MaxBodyBytes;
            copy.MaxAuthenticationAttempts =
                source.MaxAuthenticationAttempts;
        }
        return copy;
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

    static string FormatUri(
        SipUri? uri, SipWriteOptions options, string parameterName)
    {
        SipWriteResult result = SipUriFormatter.FormatUriModel(uri, options);
        if (!result.Success)
        {
            throw new System.ArgumentException(
                result.ErrorMessage == null
                    ? "The REFER URI is invalid."
                    : result.ErrorMessage, parameterName);
        }
        return System.Text.Encoding.ASCII.GetString(result.Bytes);
    }

    static void IncrementCSeq(SipReferClientContext context)
    {
        if (context.CSeq == int.MaxValue)
        {
            throw new System.InvalidOperationException(
                "The REFER CSeq is exhausted.");
        }
        context.CSeq = context.CSeq + 1;
    }
}
