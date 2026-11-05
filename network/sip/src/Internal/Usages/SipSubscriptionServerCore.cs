namespace sip;

internal class SipSubscriptionServerCore
{
    object gate = new object();
    System.Collections.Generic.List<SipSubscriptionServerContext> contexts =
        new System.Collections.Generic.List<SipSubscriptionServerContext>();
    System.Collections.Generic.List<SipSubscriptionEvent> events =
        new System.Collections.Generic.List<SipSubscriptionEvent>();
    SipProtocolTokenGenerator tokens;
    ISipSubscriptionAuthorizationPolicy? authorizationPolicy;
    long nowMilliseconds;

    internal SipSubscriptionServerCore(ISipRandomSource randomSource)
    {
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }
        tokens = new SipProtocolTokenGenerator(randomSource);
    }

    internal void ConfigureAuthorization(
        ISipSubscriptionAuthorizationPolicy configuredAuthorizationPolicy)
    {
        lock (gate)
        {
            if (configuredAuthorizationPolicy == null)
            {
                throw new System.ArgumentNullException("authorizationPolicy");
            }
            authorizationPolicy = configuredAuthorizationPolicy;
        }
    }

    internal SipSubscriptionEvent Process(SipSubscriptionRequest request)
    {
        ValidateOperation(request);
        SipMessage? suppliedMessage = request.Message;
        SipSubscriptionServerOptions? suppliedOptions =
            request.ServerOptions;
        if (suppliedMessage == null || suppliedOptions == null)
        {
            throw new System.ArgumentNullException("request");
        }
        SipMessage message = suppliedMessage;
        SipSubscriptionServerOptions options = suppliedOptions;
        SipSubscriptionServerParse parsed = ParseSubscribe(message, options);
        if (parsed.ErrorMessage != null)
        {
            lock (gate)
            {
                return Reject(request, message, parsed.StatusCode,
                    parsed.ErrorMessage, null, parsed.ExpiresSeconds);
            }
        }
        if (parsed.Event == null || parsed.Event.Token == null)
        {
            lock (gate)
            {
                return Reject(request, message, SipStatusCodes.BadEvent,
                    "A valid Event header field is required.", null, -1);
            }
        }
        int expires = parsed.HasExpires
            ? parsed.ExpiresSeconds : options.DefaultExpiresSeconds;
        if (expires > options.MaxExpiresSeconds)
        {
            expires = options.MaxExpiresSeconds;
        }
        if (request.Forked && !options.AllowForking)
        {
            lock (gate)
            {
                return Reject(request, message, SipStatusCodes.Forbidden,
                    "Forked subscriptions are not allowed for this event.",
                    null, -1);
            }
        }
        if (expires > 0 && expires < options.MinExpiresSeconds)
        {
            lock (gate)
            {
                return Reject(request, message,
                    SipStatusCodes.IntervalTooBrief,
                    "The requested subscription interval is too brief.",
                    null, options.MinExpiresSeconds);
            }
        }
        string? localTag = SipReferCore.ReadTag(message.To);
        bool refresh = localTag != null;
        SipDialogId? dialogId = CreateInboundDialogId(message);
        SipAuthorizationDecision? authorization = Authorize(
            request, message, parsed.Event, dialogId,
            expires, refresh);
        if (options.RequireAuthorization && authorization == null)
        {
            lock (gate)
            {
                return Reject(request, message, SipStatusCodes.Forbidden,
                    "Subscription authorization is required.", null, -1);
            }
        }
        if (authorization != null && !authorization.Allowed)
        {
            int status = authorization.StatusCode >= 300
                && authorization.StatusCode <= 699
                ? authorization.StatusCode : SipStatusCodes.Forbidden;
            string reason = authorization.Reason == null
                || authorization.Reason.Length == 0
                ? "Application policy denied the subscription."
                : authorization.Reason;
            lock (gate)
            {
                return Reject(request, message, status, reason,
                    authorization, -1);
            }
        }

        lock (gate)
        {
            if (refresh)
            {
                return ProcessRefresh(request, message, parsed.Event,
                    expires, authorization);
            }
            return ProcessInitial(request, message, parsed.Event,
                expires, authorization);
        }
    }

    internal SipSubscriptionEvent SendNotification(
        SipSubscriptionEvent subscriptionEvent)
    {
        lock (gate)
        {
            if (subscriptionEvent == null
                || subscriptionEvent.SubscriptionId == null)
            {
                throw new System.ArgumentNullException("subscriptionEvent");
            }
            int index = FindSubscription(subscriptionEvent.SubscriptionId);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The server subscription was not found.",
                    "subscriptionEvent");
            }
            SipSubscriptionServerContext context = contexts[index];
            if (context.OutstandingNotification != null)
            {
                throw new System.InvalidOperationException(
                    "The preceding NOTIFY delivery is still outstanding.");
            }
            ValidateNotificationUpdate(subscriptionEvent, context);
            context.State = subscriptionEvent.State;
            context.Reason = subscriptionEvent.Reason;
            context.RetryAfterSeconds =
                subscriptionEvent.RetryAfterSeconds;
            if (context.State == SipSubscriptionState.Terminated)
            {
                context.ExpiresSeconds = 0;
                context.ExpiresAtMilliseconds = -1;
            }
            else if (subscriptionEvent.ExpiresSeconds >= 0)
            {
                int expires = subscriptionEvent.ExpiresSeconds;
                if (context.Options != null
                    && expires > context.Options.MaxExpiresSeconds)
                {
                    expires = context.Options.MaxExpiresSeconds;
                }
                SetLifetime(context, expires);
            }
            SipMessage notification = BuildNotification(
                context, subscriptionEvent.Body,
                subscriptionEvent.ContentType);
            context.OutstandingNotification =
                SipReferCore.CopyMessage(notification);
            SipSubscriptionEvent value = CreateEvent(context,
                notification, null, notification, 0, true,
                context.State == SipSubscriptionState.Terminated,
                null, null);
            QueueEvent(value, context.Options);
            return CopyEvent(value);
        }
    }

    internal SipSubscriptionEvent ReceiveNotificationResponse(
        SipMessage response)
    {
        lock (gate)
        {
            ValidateNotifyResponse(response);
            int index = FindNotifyResponseContext(response);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The response does not match an outstanding NOTIFY.",
                    "message");
            }
            SipSubscriptionServerContext context = contexts[index];
            SipMessage? delivered = context.OutstandingNotification;
            if (response.StatusCode < 200)
            {
                SipSubscriptionEvent provisional = CreateEvent(
                    context, response, response, delivered,
                    response.StatusCode, false, false, null, null);
                QueueEvent(provisional, context.Options);
                return CopyEvent(provisional);
            }
            context.OutstandingNotification = null;
            bool accepted = response.StatusCode < 300;
            if (!accepted)
            {
                context.State = SipSubscriptionState.Terminated;
                context.Reason = "delivery-failed";
                context.ExpiresAtMilliseconds = -1;
            }
            bool finalValue = context.State
                == SipSubscriptionState.Terminated;
            SipSubscriptionEvent value = CreateEvent(
                context, response, response, delivered,
                response.StatusCode, accepted, finalValue,
                accepted ? null : "The peer rejected the NOTIFY request.",
                null);
            QueueEvent(value, context.Options);
            if (!accepted || finalValue)
            {
                contexts.RemoveAt(index);
            }
            else if (context.PendingExpiry)
            {
                context.PendingExpiry = false;
                QueueExpiryNotification(context);
            }
            return CopyEvent(value);
        }
    }

    internal void Expire(long nextMilliseconds)
    {
        lock (gate)
        {
            if (nextMilliseconds < nowMilliseconds)
            {
                throw new System.ArgumentException(
                    "Subscription time cannot move backwards.",
                    "nowMilliseconds");
            }
            nowMilliseconds = nextMilliseconds;
            int index = 0;
            while (index < contexts.Count)
            {
                SipSubscriptionServerContext context = contexts[index];
                if (context.State != SipSubscriptionState.Terminated
                    && context.ExpiresAtMilliseconds >= 0
                    && nowMilliseconds >= context.ExpiresAtMilliseconds)
                {
                    if (context.OutstandingNotification == null)
                    {
                        QueueExpiryNotification(context);
                    }
                    else
                    {
                        context.PendingExpiry = true;
                    }
                }
                index = index + 1;
            }
        }
    }

    internal SipSubscriptionEvent PollEvent()
    {
        lock (gate)
        {
            if (events.Count == 0)
            {
                return new SipSubscriptionEvent();
            }
            SipSubscriptionEvent value = events[0];
            events.RemoveAt(0);
            return CopyEvent(value);
        }
    }

    SipSubscriptionEvent ProcessInitial(
        SipSubscriptionRequest request, SipMessage message,
        SipExtensionHeaderValue eventValue, int expires,
        SipAuthorizationDecision? authorization)
    {
        if (contexts.Count >= request.ServerOptions.MaxSubscriptions)
        {
            return Reject(request, message,
                SipStatusCodes.ServiceUnavailable,
                "The server subscription limit was reached.",
                authorization, -1);
        }
        SipMessage? response = SipUasResponseCore.CreateResponseMessage(
            message, SipStatusCodes.Ok, SipReasonPhrases.Ok);
        if (response == null)
        {
            return Reject(request, message, SipStatusCodes.BadRequest,
                "A response cannot be created for the SUBSCRIBE request.",
                authorization, -1);
        }
        SipReferCore.AppendHeader(response, SipHeaderNames.Expires,
            expires.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        SipSubscriptionServerContext context = CreateContext(
            request, message, response, eventValue, expires);
        contexts.Add(context);
        SipMessage notification = BuildNotification(
            context, new byte[0], null);
        context.OutstandingNotification =
            SipReferCore.CopyMessage(notification);
        SipSubscriptionEvent value = CreateEvent(context,
            message, response, notification, SipStatusCodes.Ok,
            true, context.State == SipSubscriptionState.Terminated,
            null, authorization);
        QueueEvent(value, context.Options);
        return CopyEvent(value);
    }

    SipSubscriptionEvent ProcessRefresh(
        SipSubscriptionRequest request, SipMessage message,
        SipExtensionHeaderValue eventValue, int expires,
        SipAuthorizationDecision? authorization)
    {
        int dialogIndex = FindInboundDialog(message);
        if (dialogIndex < 0)
        {
            return Reject(request, message,
                SipStatusCodes.CallOrTransactionDoesNotExist,
                "The SUBSCRIBE does not match a server subscription.",
                authorization, -1);
        }
        SipSubscriptionServerContext context = contexts[dialogIndex];
        if (!SipAscii.EqualsIgnoreCase(
                context.EventPackage, eventValue.Token)
            || context.EventId != eventValue.SecondaryToken)
        {
            return Reject(request, message, SipStatusCodes.BadEvent,
                "The refresh Event value does not match the subscription.",
                authorization, -1);
        }
        int cseq = message.CSeq == null
            ? -1 : message.CSeq.SequenceNumber;
        if (cseq < context.LastSubscribeCSeq)
        {
            return Reject(request, message,
                SipStatusCodes.ServerInternalError,
                "The SUBSCRIBE CSeq is out of order.", authorization, -1);
        }
        if (cseq == context.LastSubscribeCSeq
            && context.Response != null)
        {
            SipSubscriptionEvent retransmission = CreateEvent(
                context, message, context.Response,
                context.OutstandingNotification,
                context.Response.StatusCode, true,
                context.State == SipSubscriptionState.Terminated,
                null, authorization);
            retransmission.Reason = "retransmission";
            QueueEvent(retransmission, context.Options);
            return CopyEvent(retransmission);
        }
        if (context.OutstandingNotification != null)
        {
            return Reject(request, message,
                SipStatusCodes.RequestPending,
                "A previous NOTIFY delivery is still outstanding.",
                authorization, -1);
        }
        context.LastSubscribeCSeq = cseq;
        context.Request = CopyRequest(request);
        context.Subscribe = SipReferCore.CopyMessage(message);
        if (expires == 0)
        {
            context.State = SipSubscriptionState.Terminated;
            context.Reason = "timeout";
            context.RetryAfterSeconds = -1;
            context.ExpiresSeconds = 0;
            context.ExpiresAtMilliseconds = -1;
        }
        else
        {
            SetLifetime(context, expires);
        }
        SipMessage? response = SipUasResponseCore.CreateResponseMessage(
            message, SipStatusCodes.Ok, SipReasonPhrases.Ok);
        if (response == null)
        {
            return Reject(request, message, SipStatusCodes.BadRequest,
                "A response cannot be created for the refresh.",
                authorization, -1);
        }
        SipReferCore.AppendHeader(response, SipHeaderNames.Expires,
            expires.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        context.Response = SipReferCore.CopyMessage(response);
        SipMessage notification = BuildNotification(
            context, new byte[0], null);
        context.OutstandingNotification =
            SipReferCore.CopyMessage(notification);
        SipSubscriptionEvent value = CreateEvent(context,
            message, response, notification, SipStatusCodes.Ok,
            true, context.State == SipSubscriptionState.Terminated,
            null, authorization);
        QueueEvent(value, context.Options);
        return CopyEvent(value);
    }

    SipSubscriptionServerContext CreateContext(
        SipSubscriptionRequest request, SipMessage message,
        SipMessage response, SipExtensionHeaderValue eventValue,
        int expires)
    {
        string? callId = SipReferCore.ReadCallId(message);
        string? localTag = SipReferCore.ReadTag(response.To);
        string? remoteTag = SipReferCore.ReadTag(message.From);
        if (callId == null || localTag == null || remoteTag == null
            || message.CSeq == null)
        {
            throw new System.InvalidOperationException(
                "The server subscription identity is unavailable.");
        }
        SipSubscriptionServerContext context =
            new SipSubscriptionServerContext();
        context.Request = CopyRequest(request);
        context.Options = CopyOptions(request.ServerOptions);
        context.Subscribe = SipReferCore.CopyMessage(message);
        context.Response = SipReferCore.CopyMessage(response);
        context.CallId = callId;
        context.LocalTag = localTag;
        context.RemoteTag = remoteTag;
        context.EventPackage = eventValue.Token;
        context.EventId = eventValue.SecondaryToken;
        context.LastSubscribeCSeq = message.CSeq.SequenceNumber;
        context.State = expires == 0
            ? SipSubscriptionState.Terminated
            : request.ServerOptions.InitialState;
        context.Reason = expires == 0 ? "timeout" : null;
        context.Forked = request.Forked;
        context.SubscriptionId = callId + ":" + localTag + ":"
            + remoteTag + ":" + context.EventPackage + ":"
            + context.EventId;
        SipDialogId dialogId = new SipDialogId();
        dialogId.CallId = callId;
        dialogId.LocalTag = localTag;
        dialogId.RemoteTag = remoteTag;
        context.DialogId = dialogId;
        SetLifetime(context, expires);
        return context;
    }

    SipMessage BuildNotification(
        SipSubscriptionServerContext context, byte[]? body,
        SipMediaType? contentType)
    {
        if (context.Subscribe == null || context.Request == null)
        {
            throw new System.InvalidOperationException(
                "The server subscription state is incomplete.");
        }
        SipMessage subscribe = context.Subscribe;
        SipUri? target = ReadSubscriberTarget(subscribe);
        SipUri? localContact = context.Request.Target == null
            ? subscribe.RequestUri : context.Request.Target;
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        string requestTarget = FormatUri(target, options);
        SipNameAddress from = CopyAddressWithTag(
            subscribe.To, context.LocalTag);
        SipNameAddress to = CopyAddressWithTag(
            subscribe.From, context.RemoteTag);
        SipNameAddress contact = CreateAddress(localContact);
        if (context.NotifyCSeq == int.MaxValue)
        {
            throw new System.InvalidOperationException(
                "The NOTIFY CSeq is exhausted.");
        }
        context.NotifyCSeq = context.NotifyCSeq + 1;
        context.Version = context.Version + 1;
        string eventText = context.EventPackage == null
            ? "" : context.EventPackage;
        if (context.EventId != null && context.EventId.Length > 0)
        {
            eventText = eventText + ";id=" + context.EventId;
        }
        string stateText = StateToken(context.State);
        if (context.State == SipSubscriptionState.Terminated)
        {
            if (context.Reason != null && context.Reason.Length > 0)
            {
                stateText = stateText + ";reason=" + context.Reason;
            }
            if (context.RetryAfterSeconds >= 0)
            {
                stateText = stateText + ";retry-after="
                    + context.RetryAfterSeconds.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        else
        {
            stateText = stateText + ";expires="
                + RemainingExpires(context).ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
        }
        byte[] notificationBody = body == null
            ? new byte[0] : (byte[])body.Clone();
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append("NOTIFY ");
        text.Append(requestTarget);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/UDP subscription.invalid;branch=");
        text.Append(tokens.CreateBranch());
        text.Append("\r\nMax-Forwards: 70\r\nFrom: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(from, options));
        text.Append("\r\nTo: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(to, options));
        text.Append("\r\nCall-ID: ");
        text.Append(context.CallId);
        text.Append("\r\nCSeq: ");
        text.Append(context.NotifyCSeq.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append(" NOTIFY\r\nContact: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(
            contact, options));
        text.Append("\r\nEvent: ");
        text.Append(eventText);
        text.Append("\r\nSubscription-State: ");
        text.Append(stateText);
        text.Append("\r\nContent-Length: 0\r\n\r\n");
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(
            System.Text.Encoding.UTF8.GetBytes(text.ToString()),
            new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete
            || parsed.Message == null)
        {
            throw new System.InvalidOperationException(
                "The NOTIFY request could not be constructed.");
        }
        parsed.Message.Body = notificationBody;
        parsed.Message.ContentType = SipReferCore.CopyMediaType(contentType);
        SipReferCore.ReplaceBodyHeaders(parsed.Message);
        return parsed.Message;
    }

    void QueueExpiryNotification(SipSubscriptionServerContext context)
    {
        context.State = SipSubscriptionState.Terminated;
        context.Reason = "timeout";
        context.RetryAfterSeconds = -1;
        context.ExpiresSeconds = 0;
        context.ExpiresAtMilliseconds = -1;
        SipMessage notification = BuildNotification(
            context, new byte[0], null);
        context.OutstandingNotification =
            SipReferCore.CopyMessage(notification);
        SipSubscriptionEvent value = CreateEvent(
            context, notification, null, notification, 0,
            true, true, null, null);
        QueueEvent(value, context.Options);
    }

    SipAuthorizationDecision? Authorize(
        SipSubscriptionRequest request, SipMessage message,
        SipExtensionHeaderValue eventValue, SipDialogId? dialogId,
        int expires, bool refresh)
    {
        ISipSubscriptionAuthorizationPolicy? policy;
        lock (gate)
        {
            policy = authorizationPolicy;
        }
        if (policy == null)
        {
            return null;
        }
        SipSubscriptionAuthorizationContext context =
            new SipSubscriptionAuthorizationContext();
        context.Message = SipSnapshotCopy.CopyMessageModel(message);
        context.EventPackage = eventValue.Token;
        context.EventId = eventValue.SecondaryToken;
        context.DialogId = dialogId == null ? null
            : SipReferCore.CopyDialogId(dialogId);
        context.ExpiresSeconds = expires;
        context.Refresh = refresh;
        context.Unsubscribe = expires == 0;
        context.Forked = request.Forked;
        context.CorrelationId = request.CorrelationId;
        return policy.AuthorizeSubscription(context);
    }

    SipSubscriptionEvent Reject(
        SipSubscriptionRequest request, SipMessage message,
        int statusCode, string error,
        SipAuthorizationDecision? authorization, int minimumExpires)
    {
        SipMessage? response = SipUasResponseCore.CreateResponseMessage(
            message, statusCode, ReasonPhrase(statusCode));
        if (response != null && minimumExpires >= 0)
        {
            SipReferCore.AppendHeader(response, SipHeaderNames.MinExpires,
                minimumExpires.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
        }
        SipSubscriptionEvent value = new SipSubscriptionEvent();
        value.State = SipSubscriptionState.Terminated;
        value.Request = CopyRequest(request);
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.StatusCode = statusCode;
        value.Final = true;
        value.Forked = request.Forked;
        value.ErrorMessage = error;
        value.Authorization = CopyAuthorization(authorization);
        value.CorrelationId = request.CorrelationId;
        QueueEvent(value, request.ServerOptions);
        return CopyEvent(value);
    }

    static SipSubscriptionServerParse ParseSubscribe(
        SipMessage message, SipSubscriptionServerOptions options)
    {
        SipSubscriptionServerParse result =
            new SipSubscriptionServerParse();
        if (message.Kind != SipMessageKind.Request
            || message.Method != SipMethod.Subscribe
            || !SipAscii.EqualsIgnoreCase(
                message.MethodToken, SipMethods.Subscribe)
            || message.CSeq == null
            || message.CSeq.Method != SipMethod.Subscribe
            || !SipAscii.EqualsIgnoreCase(
                message.CSeq.MethodToken, SipMethods.Subscribe))
        {
            result.StatusCode = SipStatusCodes.MethodNotAllowed;
            result.ErrorMessage = "A SUBSCRIBE request is required.";
            return result;
        }
        if (message.Body == null || message.Body.Length > options.MaxBodyBytes
            || message.Body.Length > 0
                && !SipReferCore.IsValidMediaType(message.ContentType))
        {
            result.StatusCode = message.Body != null
                && message.Body.Length > options.MaxBodyBytes
                ? SipStatusCodes.RequestEntityTooLarge
                : SipStatusCodes.UnsupportedMediaType;
            result.ErrorMessage = "The SUBSCRIBE body is not acceptable.";
            return result;
        }
        int eventCount = 0;
        int expiresCount = 0;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.Event))
            {
                eventCount = eventCount + 1;
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.Event, header.Value,
                        new SipParseOptions());
                if (!parsed.Success || parsed.Values.Length != 1)
                {
                    result.StatusCode = SipStatusCodes.BadEvent;
                    result.ErrorMessage = "The Event value is malformed.";
                    return result;
                }
                result.Event = parsed.Values[0];
            }
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.Expires))
            {
                expiresCount = expiresCount + 1;
                SipNumericParseResult parsed =
                    SipNumericParser.ParseNonNegativeInt32Text(
                        header.Value, int.MaxValue);
                if (!parsed.Success)
                {
                    result.StatusCode = SipStatusCodes.BadRequest;
                    result.ErrorMessage = "The Expires value is malformed.";
                    return result;
                }
                result.ExpiresSeconds = parsed.Value;
                result.HasExpires = true;
            }
            index = index + 1;
        }
        if (eventCount != 1)
        {
            result.StatusCode = SipStatusCodes.BadEvent;
            result.ErrorMessage =
                "A SUBSCRIBE request requires exactly one Event value.";
        }
        else if (expiresCount > 1)
        {
            result.StatusCode = SipStatusCodes.BadRequest;
            result.ErrorMessage =
                "A SUBSCRIBE request can contain only one Expires value.";
        }
        return result;
    }

    static void ValidateOperation(SipSubscriptionRequest? request)
    {
        if (request == null || request.Message == null
            || request.ServerOptions == null)
        {
            throw new System.ArgumentNullException("request");
        }
        SipSubscriptionServerOptions options = request.ServerOptions;
        if (options.DefaultExpiresSeconds < 0
            || options.MinExpiresSeconds < 0
            || options.MaxExpiresSeconds < options.MinExpiresSeconds
            || options.DefaultExpiresSeconds > options.MaxExpiresSeconds
            || options.MaxBodyBytes < 0
            || options.MaxSubscriptions < 1
            || options.MaxEventCount < 1
            || options.InitialState != SipSubscriptionState.Active
                && options.InitialState != SipSubscriptionState.Pending)
        {
            throw new System.ArgumentException(
                "The subscription server options are invalid.", "request");
        }
    }

    static void ValidateNotificationUpdate(
        SipSubscriptionEvent value,
        SipSubscriptionServerContext context)
    {
        if (value.Body == null || context.Options == null
            || value.Body.Length > context.Options.MaxBodyBytes
            || value.State != SipSubscriptionState.Active
                && value.State != SipSubscriptionState.Pending
                && value.State != SipSubscriptionState.Terminated
            || value.State != SipSubscriptionState.Terminated
                && value.ExpiresSeconds < 0
            || value.Body.Length > 0
                && !SipReferCore.IsValidMediaType(value.ContentType)
            || value.ContentType != null
                && !SipReferCore.IsValidMediaType(value.ContentType)
            || value.RetryAfterSeconds < -1)
        {
            throw new System.ArgumentException(
                "The subscription notification update is invalid.",
                "subscriptionEvent");
        }
    }

    static void ValidateNotifyResponse(SipMessage? response)
    {
        if (response == null || response.Kind != SipMessageKind.Response
            || response.StatusCode < 100 || response.StatusCode > 699
            || response.CSeq == null
            || response.CSeq.Method != SipMethod.Notify
            || !SipAscii.EqualsIgnoreCase(
                response.CSeq.MethodToken, SipMethods.Notify)
            || SipReferCore.ReadCallId(response) == null)
        {
            throw new System.ArgumentException(
                "A valid NOTIFY response is required.", "message");
        }
    }

    int FindInboundDialog(SipMessage message)
    {
        string? callId = SipReferCore.ReadCallId(message);
        string? localTag = SipReferCore.ReadTag(message.To);
        string? remoteTag = SipReferCore.ReadTag(message.From);
        int index = 0;
        while (index < contexts.Count)
        {
            SipSubscriptionServerContext context = contexts[index];
            if (context.CallId == callId && context.LocalTag == localTag
                && context.RemoteTag == remoteTag)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    int FindSubscription(string subscriptionId)
    {
        int index = 0;
        while (index < contexts.Count)
        {
            if (contexts[index].SubscriptionId == subscriptionId)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    int FindNotifyResponseContext(SipMessage response)
    {
        string? callId = SipReferCore.ReadCallId(response);
        int cseq = response.CSeq == null
            ? -1 : response.CSeq.SequenceNumber;
        int index = 0;
        while (index < contexts.Count)
        {
            SipMessage? notification =
                contexts[index].OutstandingNotification;
            if (notification != null
                && contexts[index].CallId == callId
                && notification.CSeq != null
                && notification.CSeq.SequenceNumber == cseq)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static SipDialogId? CreateInboundDialogId(SipMessage message)
    {
        string? callId = SipReferCore.ReadCallId(message);
        string? localTag = SipReferCore.ReadTag(message.To);
        string? remoteTag = SipReferCore.ReadTag(message.From);
        if (callId == null || localTag == null || remoteTag == null)
        {
            return null;
        }
        SipDialogId id = new SipDialogId();
        id.CallId = callId;
        id.LocalTag = localTag;
        id.RemoteTag = remoteTag;
        return id;
    }

    static SipUri? ReadSubscriberTarget(SipMessage message)
    {
        SipContactValue? contact = message.ContactValues != null
            && message.ContactValues.Length == 1
            ? message.ContactValues[0] : null;
        SipNameAddress? address = contact == null
            ? null : contact.Address;
        if (address != null
            && SipReferCore.IsValidSipUri(address.Uri))
        {
            return address.Uri;
        }
        return message.From == null ? null : message.From.Uri;
    }

    static SipNameAddress CopyAddressWithTag(
        SipNameAddress? source, string? tag)
    {
        SipNameAddress? copy = SipReferCore.CopyNameAddress(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The subscription address is unavailable.");
        }
        System.Collections.Generic.List<SipUriParameter> parameters =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (copy.Parameters != null && index < copy.Parameters.Length)
        {
            SipUriParameter? parameter = copy.Parameters[index];
            if (parameter != null && !SipAscii.EqualsIgnoreCase(
                    parameter.Name, "tag"))
            {
                parameters.Add(parameter);
            }
            index = index + 1;
        }
        if (tag != null)
        {
            SipUriParameter added = new SipUriParameter();
            added.Name = "tag";
            added.Value = tag;
            added.HasValue = true;
            parameters.Add(added);
        }
        copy.Parameters = parameters.ToArray();
        return copy;
    }

    static SipNameAddress CreateAddress(SipUri? uri)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = SipUriModelCopy.CreateNormalizedCopy(uri);
        return address;
    }

    static string FormatUri(SipUri? uri, SipWriteOptions options)
    {
        SipWriteResult formatted = SipUriFormatter.FormatUriModel(
            uri, options);
        if (!formatted.Success)
        {
            throw new System.InvalidOperationException(
                "The NOTIFY target URI is invalid.");
        }
        return System.Text.Encoding.ASCII.GetString(formatted.Bytes);
    }

    static string StateToken(SipSubscriptionState state)
    {
        if (state == SipSubscriptionState.Active)
        {
            return "active";
        }
        if (state == SipSubscriptionState.Pending)
        {
            return "pending";
        }
        return "terminated";
    }

    void SetLifetime(SipSubscriptionServerContext context, int expires)
    {
        context.ExpiresSeconds = expires;
        context.ExpiresAtMilliseconds = expires == 0 ? -1
            : AddMilliseconds(nowMilliseconds, (long)expires * 1000);
    }

    int RemainingExpires(SipSubscriptionServerContext context)
    {
        if (context.ExpiresAtMilliseconds < 0)
        {
            return context.ExpiresSeconds;
        }
        long remaining = context.ExpiresAtMilliseconds - nowMilliseconds;
        if (remaining <= 0)
        {
            return 0;
        }
        long seconds = (remaining + 999) / 1000;
        return seconds > int.MaxValue ? int.MaxValue : (int)seconds;
    }

    SipSubscriptionEvent CreateEvent(
        SipSubscriptionServerContext context, SipMessage? message,
        SipMessage? response, SipMessage? notification,
        int statusCode, bool accepted, bool finalValue,
        string? error, SipAuthorizationDecision? authorization)
    {
        SipSubscriptionEvent value = new SipSubscriptionEvent();
        value.SubscriptionId = context.SubscriptionId;
        value.State = context.State;
        value.Request = context.Request == null
            ? null : CopyRequest(context.Request);
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.Notification =
            SipSnapshotCopy.CopyMessageModel(notification);
        value.StatusCode = statusCode;
        value.Accepted = accepted;
        value.Final = finalValue;
        value.Forked = context.Forked;
        value.ExpiresSeconds = RemainingExpires(context);
        value.Reason = context.Reason;
        value.DialogId = context.DialogId == null ? null
            : SipReferCore.CopyDialogId(context.DialogId);
        value.NotifyCSeq = context.NotifyCSeq;
        value.Version = context.Version;
        value.ExpiresAtMilliseconds = context.ExpiresAtMilliseconds;
        value.RetryAfterSeconds = context.RetryAfterSeconds;
        SipMessage? bodySource = notification == null
            ? message : notification;
        value.Body = bodySource == null || bodySource.Body == null
            ? new byte[0] : (byte[])bodySource.Body.Clone();
        value.ContentType = bodySource == null
            ? null : SipReferCore.CopyMediaType(bodySource.ContentType);
        value.ErrorMessage = error;
        value.Authorization = CopyAuthorization(authorization);
        value.CorrelationId = context.Request == null
            ? null : context.Request.CorrelationId;
        return value;
    }

    void QueueEvent(
        SipSubscriptionEvent value, SipSubscriptionServerOptions? options)
    {
        int maximum = options == null
            ? SipDefaults.MaxQueueCount : options.MaxEventCount;
        if (events.Count >= maximum)
        {
            throw new System.InvalidOperationException(
                "The subscription server event queue limit was reached.");
        }
        events.Add(CopyEvent(value));
    }

    static SipSubscriptionEvent CopyEvent(SipSubscriptionEvent source)
    {
        SipSubscriptionEvent copy = new SipSubscriptionEvent();
        copy.SubscriptionId = source.SubscriptionId;
        copy.State = source.State;
        copy.Request = source.Request == null
            ? null : CopyRequest(source.Request);
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Response = SipSnapshotCopy.CopyMessageModel(source.Response);
        copy.Notification =
            SipSnapshotCopy.CopyMessageModel(source.Notification);
        copy.StatusCode = source.StatusCode;
        copy.Accepted = source.Accepted;
        copy.Final = source.Final;
        copy.Forked = source.Forked;
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.Reason = source.Reason;
        copy.DialogId = source.DialogId == null ? null
            : SipReferCore.CopyDialogId(source.DialogId);
        copy.NotifyCSeq = source.NotifyCSeq;
        copy.Version = source.Version;
        copy.RefreshAtMilliseconds = source.RefreshAtMilliseconds;
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        copy.RetryAtMilliseconds = source.RetryAtMilliseconds;
        copy.RetryAfterSeconds = source.RetryAfterSeconds;
        copy.Body = source.Body == null
            ? new byte[0] : (byte[])source.Body.Clone();
        copy.ContentType = SipReferCore.CopyMediaType(source.ContentType);
        copy.ErrorMessage = source.ErrorMessage;
        copy.Authorization = CopyAuthorization(source.Authorization);
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static SipSubscriptionRequest CopyRequest(
        SipSubscriptionRequest source)
    {
        SipSubscriptionRequest copy = new SipSubscriptionRequest();
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Sender = source.Sender == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Sender);
        copy.Target = source.Target == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Target);
        copy.Contact = source.Contact == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Contact);
        copy.EventPackage = source.EventPackage;
        copy.EventId = source.EventId;
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.Body = source.Body == null
            ? new byte[0] : (byte[])source.Body.Clone();
        copy.ContentType = SipReferCore.CopyMediaType(source.ContentType);
        copy.DialogId = source.DialogId == null ? null
            : SipReferCore.CopyDialogId(source.DialogId);
        copy.Options = CopyClientOptions(source.Options);
        copy.ServerOptions = CopyOptions(source.ServerOptions);
        copy.Forked = source.Forked;
        copy.Cancellation = source.Cancellation;
        copy.CorrelationId = source.CorrelationId;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        return copy;
    }

    static SipSubscriptionOptions CopyClientOptions(
        SipSubscriptionOptions? source)
    {
        SipSubscriptionOptions copy = new SipSubscriptionOptions();
        if (source != null)
        {
            copy.MinExpiresSeconds = source.MinExpiresSeconds;
            copy.MaxExpiresSeconds = source.MaxExpiresSeconds;
            copy.RefreshBeforeSeconds = source.RefreshBeforeSeconds;
            copy.RetryDelaySeconds = source.RetryDelaySeconds;
            copy.MaxForks = source.MaxForks;
            copy.MaxBodyBytes = source.MaxBodyBytes;
            copy.MaxAuthenticationAttempts = source.MaxAuthenticationAttempts;
            copy.StrictNotifyOrdering = source.StrictNotifyOrdering;
        }
        return copy;
    }

    static SipSubscriptionServerOptions CopyOptions(
        SipSubscriptionServerOptions? source)
    {
        SipSubscriptionServerOptions copy =
            new SipSubscriptionServerOptions();
        if (source != null)
        {
            copy.DefaultExpiresSeconds = source.DefaultExpiresSeconds;
            copy.MinExpiresSeconds = source.MinExpiresSeconds;
            copy.MaxExpiresSeconds = source.MaxExpiresSeconds;
            copy.MaxBodyBytes = source.MaxBodyBytes;
            copy.MaxSubscriptions = source.MaxSubscriptions;
            copy.MaxEventCount = source.MaxEventCount;
            copy.AllowForking = source.AllowForking;
            copy.RequireAuthorization = source.RequireAuthorization;
            copy.InitialState = source.InitialState;
        }
        return copy;
    }

    static SipAuthorizationDecision? CopyAuthorization(
        SipAuthorizationDecision? source)
    {
        if (source == null)
        {
            return null;
        }
        SipAuthorizationDecision copy = new SipAuthorizationDecision();
        copy.Allowed = source.Allowed;
        copy.Identity = source.Identity;
        copy.Reason = source.Reason;
        copy.StatusCode = source.StatusCode;
        copy.Scopes = source.Scopes == null
            ? new string[0] : (string[])source.Scopes.Clone();
        return copy;
    }

    static string ReasonPhrase(int statusCode)
    {
        switch (statusCode)
        {
            case SipStatusCodes.BadRequest:
                return SipReasonPhrases.BadRequest;
            case SipStatusCodes.Forbidden:
                return SipReasonPhrases.Forbidden;
            case SipStatusCodes.MethodNotAllowed:
                return SipReasonPhrases.MethodNotAllowed;
            case SipStatusCodes.RequestEntityTooLarge:
                return SipReasonPhrases.RequestEntityTooLarge;
            case SipStatusCodes.UnsupportedMediaType:
                return SipReasonPhrases.UnsupportedMediaType;
            case SipStatusCodes.IntervalTooBrief:
                return SipReasonPhrases.IntervalTooBrief;
            case SipStatusCodes.CallOrTransactionDoesNotExist:
                return SipReasonPhrases.CallOrTransactionDoesNotExist;
            case SipStatusCodes.BadEvent:
                return SipReasonPhrases.BadEvent;
            case SipStatusCodes.RequestPending:
                return SipReasonPhrases.RequestPending;
            case SipStatusCodes.ServiceUnavailable:
                return SipReasonPhrases.ServiceUnavailable;
            default:
                return statusCode >= 500
                    ? SipReasonPhrases.ServerInternalError
                    : SipReasonPhrases.Forbidden;
        }
    }

    static long AddMilliseconds(long baseline, long delta)
    {
        if (delta > long.MaxValue - baseline)
        {
            return long.MaxValue;
        }
        return baseline + delta;
    }
}
