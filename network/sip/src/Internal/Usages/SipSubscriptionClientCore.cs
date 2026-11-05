namespace sip;

internal class SipSubscriptionClientCore
{
    object gate = new object();
    System.Collections.Generic.List<SipSubscriptionClientContext> contexts =
        new System.Collections.Generic.List<SipSubscriptionClientContext>();
    System.Collections.Generic.List<SipSubscriptionEvent> events =
        new System.Collections.Generic.List<SipSubscriptionEvent>();
    SipProtocolTokenGenerator tokens;
    ISipDialogLayer? dialogLayer;
    ISipAuthenticationClient? authenticationClient;
    long nowMilliseconds;

    internal SipSubscriptionClientCore(ISipRandomSource randomSource)
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

    internal SipSubscriptionEvent Create(SipSubscriptionRequest request)
    {
        lock (gate)
        {
            ValidateRequest(request, true);
            SipSubscriptionRequest copied = CopyRequest(request);
            SipMessage message = BuildInitialRequest(copied);
            SipSubscriptionClientContext context = CreateContext(
                copied, message);
            contexts.Add(context);
            SipSubscriptionEvent value = CreateEvent(context, message,
                null, 0, false, false, null, null);
            QueueEvent(value);
            return CopyEvent(value);
        }
    }

    internal SipSubscriptionEvent Refresh(SipSubscriptionRequest request)
    {
        lock (gate)
        {
            ValidateRequest(request, false);
            int index = FindRefreshContext(request);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The subscription branch is not active.", "request");
            }
            SipSubscriptionClientContext context = contexts[index];
            if (context.State == SipSubscriptionState.Terminated)
            {
                throw new System.ArgumentException(
                    "The subscription branch has terminated.", "request");
            }
            context.Request = CopyRequest(request);
            SipMessage message = BuildContinuationRequest(
                context, request.ExpiresSeconds);
            context.Message = SipReferCore.CopyMessage(message);
            context.SubscribeFinal = false;
            context.RefreshAtMilliseconds = -1;
            SipSubscriptionEvent value = CreateEvent(context, message,
                null, 0, false, false, null, null);
            QueueEvent(value);
            return CopyEvent(value);
        }
    }

    internal SipSubscriptionEvent ReceiveResponse(SipMessage response)
    {
        lock (gate)
        {
            ValidateResponse(response);
            int index = FindResponseContext(response);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The SUBSCRIBE response does not match an outstanding request.",
                    "message");
            }
            SipSubscriptionClientContext context = contexts[index];
            string? remoteTag = SipReferCore.ReadTag(response.To);
            if (response.StatusCode >= 200 && response.StatusCode < 300
                && remoteTag != null && context.RemoteTag != remoteTag)
            {
                index = SelectResponseFork(index, remoteTag);
                context = contexts[index];
            }
            if (response.StatusCode < 200)
            {
                SipSubscriptionEvent provisional = CreateEvent(
                    context, response, null, response.StatusCode,
                    false, false, null, null);
                QueueEvent(provisional);
                return CopyEvent(provisional);
            }
            if (response.StatusCode == SipStatusCodes.Unauthorized
                || response.StatusCode
                    == SipStatusCodes.ProxyAuthenticationRequired)
            {
                return RetryAuthentication(context, response, index);
            }
            if (response.StatusCode == SipStatusCodes.IntervalTooBrief)
            {
                return RetryMinimumExpiry(context, response, index);
            }
            context.SubscribeFinal = true;
            context.Response = SipReferCore.CopyMessage(response);
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                if (remoteTag == null)
                {
                    return FailResponse(context, response, index,
                        "A successful SUBSCRIBE response requires a remote tag.");
                }
                SetRemoteTag(context, remoteTag);
                int expires = ReadIntegerHeader(
                    response, SipHeaderNames.Expires);
                if (expires < 0 && context.Request != null)
                {
                    expires = context.Request.ExpiresSeconds;
                }
                if (!context.NotificationFinal)
                {
                    if (!context.Terminating)
                    {
                        SetLifetime(context, LimitExpires(context, expires));
                    }
                    if (context.State == SipSubscriptionState.None)
                    {
                        context.State = SipSubscriptionState.Pending;
                    }
                }
                SipSubscriptionEvent accepted = CreateEvent(
                    context, response, null, response.StatusCode,
                    true, context.NotificationFinal, null, null);
                QueueEvent(accepted);
                if (context.NotificationFinal
                    && context.RetryAtMilliseconds < 0)
                {
                    contexts.RemoveAt(index);
                }
                return CopyEvent(accepted);
            }
            return FailResponse(context, response, index,
                "The peer rejected the SUBSCRIBE request.");
        }
    }

    internal SipSubscriptionEvent ConsumeNotification(
        SipMessage notification)
    {
        lock (gate)
        {
            ValidateNotification(notification);
            SipExtensionHeaderValue? eventValue =
                SipReferCore.ParseEventHeader(notification);
            SipExtensionHeaderValue? subscription =
                SipReferCore.ParseSubscriptionStateHeader(notification);
            int responseStatus = SipStatusCodes.Ok;
            string? error = null;
            if (eventValue == null || subscription == null)
            {
                throw new System.ArgumentException(
                    "NOTIFY requires one valid Event and Subscription-State value.",
                    "message");
            }
            int index = FindNotificationContext(notification, eventValue);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The NOTIFY does not match a client subscription.",
                    "message");
            }
            SipSubscriptionClientContext context = contexts[index];
            int notifyCSeq = notification.CSeq == null
                ? -1 : notification.CSeq.SequenceNumber;
            if (context.Request != null
                && context.Request.Options.StrictNotifyOrdering
                && context.LastNotifyCSeq >= 0
                && notifyCSeq <= context.LastNotifyCSeq)
            {
                responseStatus = SipStatusCodes.ServerInternalError;
                error = "The NOTIFY CSeq is duplicate or out of order.";
                SipMessage? rejectedResponse = CreateNotifyResponse(
                    notification, responseStatus);
                SipSubscriptionEvent rejected = CreateEvent(
                    context, notification, rejectedResponse,
                    responseStatus, false, false, null, error);
                QueueEvent(rejected);
                return CopyEvent(rejected);
            }
            string? stateToken = subscription.Token;
            bool active = SipAscii.EqualsIgnoreCase(stateToken, "active");
            bool pending = SipAscii.EqualsIgnoreCase(stateToken, "pending");
            bool terminated = SipAscii.EqualsIgnoreCase(
                stateToken, "terminated");
            if (!active && !pending && !terminated)
            {
                throw new System.ArgumentException(
                    "The Subscription-State token is invalid.", "message");
            }
            if ((active || pending) && subscription.Number < 0)
            {
                throw new System.ArgumentException(
                    "An active or pending NOTIFY requires expires.", "message");
            }
            context.LastNotifyCSeq = notifyCSeq;
            context.Version = context.Version + 1;
            context.State = active ? SipSubscriptionState.Active
                : pending ? SipSubscriptionState.Pending
                : SipSubscriptionState.Terminated;
            context.NotificationFinal = terminated;
            string? reason = subscription.SecondaryToken;
            if (terminated)
            {
                context.RefreshAtMilliseconds = -1;
                context.ExpiresAtMilliseconds = -1;
                context.ExpiresSeconds = 0;
                SetRetry(context, subscription.SecondNumber, reason);
            }
            else
            {
                SetLifetime(context,
                    LimitExpires(context, subscription.Number));
            }
            SipMessage? response = CreateNotifyResponse(
                notification, responseStatus);
            SipSubscriptionEvent value = CreateEvent(
                context, notification, response, responseStatus,
                true, terminated, reason, null);
            QueueEvent(value);
            if (terminated && context.SubscribeFinal
                && context.RetryAtMilliseconds < 0)
            {
                contexts.RemoveAt(index);
            }
            return CopyEvent(value);
        }
    }

    internal void Terminate(SipCancellation cancellation)
    {
        lock (gate)
        {
            if (cancellation == null || !cancellation.Requested)
            {
                throw new System.ArgumentException(
                    "A requested subscription cancellation is required.",
                    "cancellation");
            }
            int index = FindCancellationContext(cancellation.Id);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The subscription branch was not found.", "cancellation");
            }
            SipSubscriptionClientContext context = contexts[index];
            SipMessage message = BuildContinuationRequest(context, 0);
            context.Message = SipReferCore.CopyMessage(message);
            context.SubscribeFinal = false;
            context.Terminating = true;
            context.RefreshAtMilliseconds = -1;
            SipSubscriptionEvent value = CreateEvent(
                context, message, null, 0, false, false,
                cancellation.Reason, null);
            QueueEvent(value);
        }
    }

    internal void AdvanceTime(long nextMilliseconds)
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
            int index = contexts.Count - 1;
            while (index >= 0)
            {
                SipSubscriptionClientContext context = contexts[index];
                if (context.RetryAtMilliseconds >= 0
                    && nowMilliseconds >= context.RetryAtMilliseconds)
                {
                    RetryTerminatedSubscription(context);
                }
                else if (context.ExpiresAtMilliseconds >= 0
                    && nowMilliseconds >= context.ExpiresAtMilliseconds)
                {
                    context.State = SipSubscriptionState.Terminated;
                    context.ExpiresSeconds = 0;
                    context.RefreshAtMilliseconds = -1;
                    context.ExpiresAtMilliseconds = -1;
                    context.NotificationFinal = true;
                    SipSubscriptionEvent expired = CreateEvent(
                        context, null, null, 0, false, true,
                        "timeout", "The subscription expired.");
                    QueueEvent(expired);
                    contexts.RemoveAt(index);
                }
                else if (context.RefreshAtMilliseconds >= 0
                    && nowMilliseconds >= context.RefreshAtMilliseconds)
                {
                    int expires = context.Request == null
                        ? SipDefaults.SubscriptionExpiresSeconds
                        : context.Request.ExpiresSeconds;
                    SipMessage message = BuildContinuationRequest(
                        context, expires);
                    context.Message = SipReferCore.CopyMessage(message);
                    context.SubscribeFinal = false;
                    context.RefreshAtMilliseconds = -1;
                    SipSubscriptionEvent refresh = CreateEvent(
                        context, message, null, 0, false, false,
                        "refresh", null);
                    QueueEvent(refresh);
                }
                index = index - 1;
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

    SipMessage BuildInitialRequest(SipSubscriptionRequest request)
    {
        SipMessage message;
        if (request.DialogId != null)
        {
            if (dialogLayer == null)
            {
                throw new System.InvalidOperationException(
                    "A subscription dialog layer has not been configured.");
            }
            message = dialogLayer.CreateRequest(
                request.DialogId, SipMethods.Subscribe);
        }
        else
        {
            message = BuildOutsideDialogRequest(request);
        }
        ApplySubscriptionContent(message, request,
            request.ExpiresSeconds);
        return message;
    }

    SipMessage BuildOutsideDialogRequest(SipSubscriptionRequest request)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        string target = FormatUri(request.Target, options, "target");
        SipNameAddress from = CreateAddress(request.Sender, tokens.CreateTag());
        SipNameAddress to = CreateAddress(request.Target, null);
        SipNameAddress contact = CreateAddress(request.Contact, null);
        string transport = request.Target != null
            && request.Target.Scheme == SipUriScheme.Sips ? "TLS" : "UDP";
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append("SUBSCRIBE ");
        text.Append(target);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/");
        text.Append(transport);
        text.Append(" subscription.invalid;branch=");
        text.Append(tokens.CreateBranch());
        text.Append("\r\nMax-Forwards: 70\r\nFrom: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(from, options));
        text.Append("\r\nTo: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(to, options));
        text.Append("\r\nCall-ID: ");
        text.Append(tokens.CreateCallId());
        text.Append("\r\nCSeq: 1 SUBSCRIBE\r\nContact: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(
            contact, options));
        text.Append("\r\nContent-Length: 0\r\n\r\n");
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(
            System.Text.Encoding.UTF8.GetBytes(text.ToString()),
            new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete
            || parsed.Message == null)
        {
            throw new System.ArgumentException(
                "The out-of-dialog SUBSCRIBE could not be constructed.",
                "request");
        }
        return parsed.Message;
    }

    SipMessage BuildContinuationRequest(
        SipSubscriptionClientContext context, int expires)
    {
        if (context.Message == null || context.Request == null)
        {
            throw new System.InvalidOperationException(
                "The subscription request state is incomplete.");
        }
        if (context.CSeq == int.MaxValue)
        {
            throw new System.InvalidOperationException(
                "The SUBSCRIBE CSeq is exhausted.");
        }
        context.CSeq = context.CSeq + 1;
        SipMessage message = SipReferCore.CopyMessage(context.Message);
        message.Method = SipMethod.Subscribe;
        message.MethodToken = SipMethods.Subscribe;
        if (message.CSeq != null)
        {
            message.CSeq.SequenceNumber = context.CSeq;
            message.CSeq.Method = SipMethod.Subscribe;
            message.CSeq.MethodToken = SipMethods.Subscribe;
        }
        ReplaceHeader(message, SipHeaderNames.CSeq,
            context.CSeq.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
                + " SUBSCRIBE");
        if (context.RemoteTag != null)
        {
            SetToTag(message, context.RemoteTag);
        }
        ApplySubscriptionContent(message, context.Request, expires);
        return message;
    }

    static void ApplySubscriptionContent(
        SipMessage message, SipSubscriptionRequest request, int expires)
    {
        message.Body = request.Body == null
            ? new byte[0] : (byte[])request.Body.Clone();
        message.ContentType = SipReferCore.CopyMediaType(request.ContentType);
        SipReferCore.ReplaceBodyHeaders(message);
        string eventValue = request.EventPackage == null
            ? "" : request.EventPackage;
        if (request.EventId != null && request.EventId.Length > 0)
        {
            eventValue = eventValue + ";id=" + request.EventId;
        }
        ReplaceHeader(message, SipHeaderNames.Event, eventValue);
        ReplaceHeader(message, SipHeaderNames.Expires,
            expires.ToString(System.Globalization.CultureInfo.InvariantCulture));
        message.OriginalBytes = new byte[0];
    }

    SipSubscriptionClientContext CreateContext(
        SipSubscriptionRequest request, SipMessage message)
    {
        string? callId = SipReferCore.ReadCallId(message);
        string? localTag = SipReferCore.ReadTag(message.From);
        if (callId == null || localTag == null || message.CSeq == null)
        {
            throw new System.InvalidOperationException(
                "The SUBSCRIBE protocol identity is unavailable.");
        }
        SipSubscriptionClientContext context =
            new SipSubscriptionClientContext();
        context.Request = request;
        context.Message = SipReferCore.CopyMessage(message);
        context.CallId = callId;
        context.LocalTag = localTag;
        context.CSeq = message.CSeq.SequenceNumber;
        context.State = SipSubscriptionState.Pending;
        context.DialogId = request.DialogId == null ? null
            : SipReferCore.CopyDialogId(request.DialogId);
        context.BaseId = callId + ":" + localTag + ":"
            + request.EventPackage + ":" + request.EventId;
        context.SubscriptionId = context.BaseId;
        context.ExpiresSeconds = request.ExpiresSeconds;
        if (request.DialogId != null && request.DialogId.RemoteTag != null)
        {
            context.RemoteTag = request.DialogId.RemoteTag;
            context.SubscriptionId = context.BaseId + ":"
                + request.DialogId.RemoteTag;
        }
        return context;
    }

    int SelectResponseFork(int index, string remoteTag)
    {
        SipSubscriptionClientContext source = contexts[index];
        int exact = FindFamilyRemoteTag(source, remoteTag);
        if (exact >= 0)
        {
            return exact;
        }
        if (source.RemoteTag == null)
        {
            SetRemoteTag(source, remoteTag);
            return index;
        }
        EnsureForkCapacity(source);
        SipSubscriptionClientContext fork = CopyContext(source);
        fork.RemoteTag = null;
        fork.DialogId = null;
        fork.Forked = true;
        fork.SubscribeFinal = false;
        fork.NotificationFinal = false;
        fork.LastNotifyCSeq = -1;
        fork.Version = 0;
        ResetForkState(fork);
        SetRemoteTag(fork, remoteTag);
        contexts.Add(fork);
        return contexts.Count - 1;
    }

    int FindNotificationContext(
        SipMessage notification, SipExtensionHeaderValue eventValue)
    {
        string? callId = SipReferCore.ReadCallId(notification);
        string? localTag = SipReferCore.ReadTag(notification.To);
        string? remoteTag = SipReferCore.ReadTag(notification.From);
        if (callId == null || localTag == null || remoteTag == null)
        {
            return -1;
        }
        int family = -1;
        int index = 0;
        while (index < contexts.Count)
        {
            SipSubscriptionClientContext candidate = contexts[index];
            if (candidate.CallId == callId
                && candidate.LocalTag == localTag
                && EventMatches(candidate.Request, eventValue))
            {
                if (candidate.RemoteTag == remoteTag)
                {
                    return index;
                }
                if (family < 0)
                {
                    family = index;
                }
            }
            index = index + 1;
        }
        if (family < 0)
        {
            return -1;
        }
        SipSubscriptionClientContext source = contexts[family];
        if (source.RemoteTag == null)
        {
            SetRemoteTag(source, remoteTag);
            return family;
        }
        EnsureForkCapacity(source);
        SipSubscriptionClientContext fork = CopyContext(source);
        fork.RemoteTag = null;
        fork.DialogId = null;
        fork.Forked = true;
        fork.SubscribeFinal = false;
        fork.NotificationFinal = false;
        fork.LastNotifyCSeq = -1;
        fork.Version = 0;
        ResetForkState(fork);
        SetRemoteTag(fork, remoteTag);
        contexts.Add(fork);
        return contexts.Count - 1;
    }

    int FindResponseContext(SipMessage response)
    {
        string? callId = SipReferCore.ReadCallId(response);
        string? remoteTag = SipReferCore.ReadTag(response.To);
        int responseCSeq = response.CSeq == null
            ? -1 : response.CSeq.SequenceNumber;
        int family = -1;
        int index = 0;
        while (index < contexts.Count)
        {
            SipSubscriptionClientContext candidate = contexts[index];
            if (candidate.CallId == callId
                && candidate.CSeq == responseCSeq)
            {
                if (remoteTag != null && candidate.RemoteTag == remoteTag)
                {
                    return index;
                }
                if (family < 0)
                {
                    family = index;
                }
            }
            index = index + 1;
        }
        return family;
    }

    int FindRefreshContext(SipSubscriptionRequest request)
    {
        int found = -1;
        int index = 0;
        while (index < contexts.Count)
        {
            SipSubscriptionClientContext candidate = contexts[index];
            if (candidate.Request != null
                && SipAscii.EqualsIgnoreCase(
                    candidate.Request.EventPackage, request.EventPackage)
                && candidate.Request.EventId == request.EventId
                && DialogIdsEqual(candidate.DialogId, request.DialogId))
            {
                if (found >= 0)
                {
                    return -1;
                }
                found = index;
            }
            index = index + 1;
        }
        return found;
    }

    int FindCancellationContext(string? subscriptionId)
    {
        if (subscriptionId != null && subscriptionId.Length > 0)
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
        return contexts.Count == 1 ? 0 : -1;
    }

    SipSubscriptionEvent RetryMinimumExpiry(
        SipSubscriptionClientContext context, SipMessage response, int index)
    {
        int minimum = ReadIntegerHeader(response, SipHeaderNames.MinExpires);
        if (context.Request == null || minimum < 0
            || minimum < context.Request.Options.MinExpiresSeconds
            || minimum > context.Request.Options.MaxExpiresSeconds)
        {
            return FailResponse(context, response, index,
                "The 423 response does not contain an acceptable Min-Expires value.");
        }
        context.Request.ExpiresSeconds = minimum;
        SipMessage retry = BuildContinuationRequest(context, minimum);
        context.Message = SipReferCore.CopyMessage(retry);
        SipSubscriptionEvent value = CreateEvent(context, retry, null,
            response.StatusCode, false, false, "minimum-expiry", null);
        QueueEvent(value);
        return CopyEvent(value);
    }

    SipSubscriptionEvent RetryAuthentication(
        SipSubscriptionClientContext context, SipMessage response, int index)
    {
        if (authenticationClient == null || context.Request == null
            || context.AuthenticationAttempts
                >= context.Request.Options.MaxAuthenticationAttempts)
        {
            return FailResponse(context, response, index,
                authenticationClient == null
                    ? "The SUBSCRIBE challenge requires an authentication client."
                    : "The SUBSCRIBE authentication retry limit was reached.");
        }
        SipCredentialScope scope = response.StatusCode
            == SipStatusCodes.Unauthorized
            ? SipCredentialScope.Origin : SipCredentialScope.Proxy;
        SipDigestChallenge[] challenges = ReadChallenges(response, scope);
        if (challenges.Length == 0)
        {
            return FailResponse(context, response, index,
                "The SUBSCRIBE authentication challenge is malformed.");
        }
        SipMessage retry = BuildContinuationRequest(
            context, context.Request.ExpiresSeconds);
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
            return FailResponse(context, response, index,
                authorized.ErrorMessage == null
                    ? "The SUBSCRIBE request could not be authorized."
                    : authorized.ErrorMessage);
        }
        context.AuthenticationAttempts = context.AuthenticationAttempts + 1;
        context.Message = SipReferCore.CopyMessage(authorized.Message);
        SipSubscriptionEvent value = CreateEvent(
            context, authorized.Message, null, response.StatusCode,
            false, false, "authentication-retry", null);
        QueueEvent(value);
        return CopyEvent(value);
    }

    SipSubscriptionEvent FailResponse(
        SipSubscriptionClientContext context, SipMessage response,
        int index, string error)
    {
        context.State = SipSubscriptionState.Terminated;
        context.NotificationFinal = true;
        context.RefreshAtMilliseconds = -1;
        context.ExpiresAtMilliseconds = -1;
        SipSubscriptionEvent failed = CreateEvent(
            context, response, null, response.StatusCode,
            false, true, null, error);
        QueueEvent(failed);
        contexts.RemoveAt(index);
        return CopyEvent(failed);
    }

    void RetryTerminatedSubscription(SipSubscriptionClientContext context)
    {
        if (context.Request == null)
        {
            return;
        }
        SipMessage message = BuildInitialRequest(context.Request);
        string? callId = SipReferCore.ReadCallId(message);
        string? localTag = SipReferCore.ReadTag(message.From);
        if (callId == null || localTag == null || message.CSeq == null)
        {
            throw new System.InvalidOperationException(
                "The retry SUBSCRIBE identity is unavailable.");
        }
        context.Message = SipReferCore.CopyMessage(message);
        context.CallId = callId;
        context.LocalTag = localTag;
        context.RemoteTag = null;
        context.CSeq = message.CSeq.SequenceNumber;
        context.BaseId = callId + ":" + localTag + ":"
            + context.Request.EventPackage + ":" + context.Request.EventId;
        context.SubscriptionId = context.BaseId;
        context.DialogId = context.Request.DialogId == null ? null
            : SipReferCore.CopyDialogId(context.Request.DialogId);
        context.State = SipSubscriptionState.Pending;
        context.SubscribeFinal = false;
        context.NotificationFinal = false;
        context.Terminating = false;
        context.LastNotifyCSeq = -1;
        context.Version = 0;
        context.RetryAtMilliseconds = -1;
        context.RetryAfterSeconds = -1;
        SipSubscriptionEvent retry = CreateEvent(
            context, message, null, 0, false, false,
            "retry", null);
        QueueEvent(retry);
    }

    void SetLifetime(SipSubscriptionClientContext context, int seconds)
    {
        if (seconds < 0)
        {
            return;
        }
        context.ExpiresSeconds = seconds;
        context.ExpiresAtMilliseconds = AddMilliseconds(
            nowMilliseconds, (long)seconds * 1000);
        int leadSeconds = context.Request == null
            ? 0 : context.Request.Options.RefreshBeforeSeconds;
        long refresh = context.ExpiresAtMilliseconds
            - (long)leadSeconds * 1000;
        context.RefreshAtMilliseconds = seconds == 0 ? -1
            : refresh < nowMilliseconds ? nowMilliseconds : refresh;
    }

    static int LimitExpires(
        SipSubscriptionClientContext context, int seconds)
    {
        if (context.Request != null
            && seconds > context.Request.ExpiresSeconds)
        {
            return context.Request.ExpiresSeconds;
        }
        return seconds;
    }

    static void ResetForkState(SipSubscriptionClientContext context)
    {
        context.State = SipSubscriptionState.Pending;
        context.ExpiresSeconds = context.Request == null
            ? -1 : context.Request.ExpiresSeconds;
        context.RefreshAtMilliseconds = -1;
        context.ExpiresAtMilliseconds = -1;
        context.RetryAtMilliseconds = -1;
        context.RetryAfterSeconds = -1;
    }

    void SetRetry(SipSubscriptionClientContext context,
        int retryAfter, string? reason)
    {
        context.RetryAtMilliseconds = -1;
        context.RetryAfterSeconds = retryAfter;
        if (context.Terminating
            || SipAscii.EqualsIgnoreCase(reason, "rejected")
            || SipAscii.EqualsIgnoreCase(reason, "noresource")
            || SipAscii.EqualsIgnoreCase(reason, "invariant"))
        {
            return;
        }
        int delay = retryAfter;
        if (SipAscii.EqualsIgnoreCase(reason, "deactivated")
            || SipAscii.EqualsIgnoreCase(reason, "timeout"))
        {
            delay = 0;
        }
        else if (delay < 0 && context.Request != null)
        {
            delay = context.Request.Options.RetryDelaySeconds;
        }
        if (delay >= 0)
        {
            context.RetryAtMilliseconds = AddMilliseconds(
                nowMilliseconds, (long)delay * 1000);
        }
    }

    void SetRemoteTag(
        SipSubscriptionClientContext context, string remoteTag)
    {
        context.RemoteTag = remoteTag;
        context.SubscriptionId = context.BaseId + ":" + remoteTag;
        SipDialogId id = new SipDialogId();
        id.CallId = context.CallId;
        id.LocalTag = context.LocalTag;
        id.RemoteTag = remoteTag;
        context.DialogId = id;
    }

    void EnsureForkCapacity(SipSubscriptionClientContext source)
    {
        int count = 0;
        int index = 0;
        while (index < contexts.Count)
        {
            if (contexts[index].BaseId == source.BaseId)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        int maximum = source.Request == null
            ? 1 : source.Request.Options.MaxForks;
        if (count >= maximum)
        {
            throw new System.InvalidOperationException(
                "The subscription fork limit was reached.");
        }
    }

    int FindFamilyRemoteTag(
        SipSubscriptionClientContext source, string remoteTag)
    {
        int index = 0;
        while (index < contexts.Count)
        {
            if (contexts[index].BaseId == source.BaseId
                && contexts[index].RemoteTag == remoteTag)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool EventMatches(
        SipSubscriptionRequest? request,
        SipExtensionHeaderValue eventValue)
    {
        return request != null
            && SipAscii.EqualsIgnoreCase(
                request.EventPackage, eventValue.Token)
            && request.EventId == eventValue.SecondaryToken;
    }

    static bool DialogIdsEqual(SipDialogId? first, SipDialogId? second)
    {
        return first != null && second != null
            && first.CallId == second.CallId
            && first.LocalTag == second.LocalTag
            && first.RemoteTag == second.RemoteTag;
    }

    static void ValidateRequest(
        SipSubscriptionRequest? request, bool initial)
    {
        if (request == null || request.Body == null
            || request.Options == null || request.EventPackage == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (!SipAscii.IsToken(request.EventPackage)
            || request.EventId != null && !SipAscii.IsToken(request.EventId)
            || request.ExpiresSeconds < 0
            || request.ExpiresSeconds < request.Options.MinExpiresSeconds
            || request.ExpiresSeconds > request.Options.MaxExpiresSeconds
            || request.Options.MinExpiresSeconds < 0
            || request.Options.MaxExpiresSeconds
                < request.Options.MinExpiresSeconds
            || request.Options.RefreshBeforeSeconds < 0
            || request.Options.RetryDelaySeconds < 0
            || request.Options.MaxForks < 1
            || request.Options.MaxBodyBytes < 0
            || request.Options.MaxAuthenticationAttempts < 0
            || request.Body.Length > request.Options.MaxBodyBytes
            || request.Body.Length > 0
                && !SipReferCore.IsValidMediaType(request.ContentType)
            || request.ContentType != null
                && !SipReferCore.IsValidMediaType(request.ContentType))
        {
            throw new System.ArgumentException(
                "The subscription event, expiry, body, or options are invalid.",
                "request");
        }
        if (initial && request.DialogId == null
            && (!SipReferCore.IsValidSipUri(request.Sender)
                || !SipReferCore.IsValidSipUri(request.Target)
                || !SipReferCore.IsValidSipUri(request.Contact)))
        {
            throw new System.ArgumentException(
                "An out-of-dialog subscription requires sender, target, and Contact URIs.",
                "request");
        }
        if (request.DialogId != null)
        {
            ValidateDialogId(request.DialogId);
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
            || response.CSeq.Method != SipMethod.Subscribe
            || !SipAscii.EqualsIgnoreCase(
                response.CSeq.MethodToken, SipMethods.Subscribe)
            || SipReferCore.ReadCallId(response) == null)
        {
            throw new System.ArgumentException(
                "A valid SUBSCRIBE response is required.", "message");
        }
    }

    static void ValidateNotification(SipMessage? message)
    {
        if (message == null || message.Kind != SipMessageKind.Request
            || message.Method != SipMethod.Notify
            || !SipAscii.EqualsIgnoreCase(
                message.MethodToken, SipMethods.Notify)
            || message.CSeq == null
            || message.CSeq.Method != SipMethod.Notify
            || !SipAscii.EqualsIgnoreCase(
                message.CSeq.MethodToken, SipMethods.Notify)
            || SipReferCore.ReadCallId(message) == null)
        {
            throw new System.ArgumentException(
                "A valid NOTIFY request is required.", "message");
        }
    }

    static void ValidateDialogId(SipDialogId id)
    {
        if (id.CallId == null || id.CallId.Length == 0
            || id.LocalTag == null || id.LocalTag.Length == 0
            || id.RemoteTag == null || id.RemoteTag.Length == 0)
        {
            throw new System.ArgumentException(
                "A complete subscription dialog identifier is required.",
                "request");
        }
    }

    static int ReadIntegerHeader(SipMessage message, string name)
    {
        int value = -1;
        int count = 0;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                count = count + 1;
                SipNumericParseResult parsed =
                    SipNumericParser.ParseNonNegativeInt32Text(
                        header.Value, int.MaxValue);
                if (!parsed.Success)
                {
                    return -1;
                }
                value = parsed.Value;
            }
            index = index + 1;
        }
        return count == 1 ? value : -1;
    }

    static void ReplaceHeader(
        SipMessage message, string name, string value)
    {
        RemoveHeader(message, name);
        SipReferCore.AppendHeader(message, name, value);
    }

    static void RemoveHeader(SipMessage message, string name)
    {
        System.Collections.Generic.List<SipHeader> kept =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && !SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                kept.Add(header);
            }
            index = index + 1;
        }
        message.Headers = kept.ToArray();
    }

    static void SetToTag(SipMessage message, string remoteTag)
    {
        if (message.To == null)
        {
            return;
        }
        SipNameAddress? to = SipReferCore.CopyNameAddress(message.To);
        if (to == null)
        {
            return;
        }
        SetAddressTag(to, remoteTag);
        message.To = to;
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        ReplaceHeader(message, SipHeaderNames.To,
            SipHeaderPrimitives.FormatNameAddressValue(to, options));
    }

    static void SetAddressTag(SipNameAddress address, string tag)
    {
        System.Collections.Generic.List<SipUriParameter> parameters =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (address.Parameters != null
            && index < address.Parameters.Length)
        {
            SipUriParameter? parameter = address.Parameters[index];
            if (parameter != null && !SipAscii.EqualsIgnoreCase(
                    parameter.Name, "tag"))
            {
                parameters.Add(parameter);
            }
            index = index + 1;
        }
        SipUriParameter added = new SipUriParameter();
        added.Name = "tag";
        added.Value = tag;
        added.HasValue = true;
        parameters.Add(added);
        address.Parameters = parameters.ToArray();
    }

    static SipMessage? CreateNotifyResponse(
        SipMessage notification, int statusCode)
    {
        string reason = statusCode == SipStatusCodes.Ok
            ? SipReasonPhrases.Ok : SipReasonPhrases.ServerInternalError;
        return SipUasResponseCore.CreateResponseMessage(
            notification, statusCode, reason);
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
                    ? "The subscription URI is invalid."
                    : result.ErrorMessage, parameterName);
        }
        return System.Text.Encoding.ASCII.GetString(result.Bytes);
    }

    SipSubscriptionEvent CreateEvent(
        SipSubscriptionClientContext context, SipMessage? message,
        SipMessage? response, int statusCode, bool accepted,
        bool finalValue, string? reason, string? error)
    {
        SipSubscriptionEvent value = new SipSubscriptionEvent();
        value.SubscriptionId = context.SubscriptionId;
        value.State = context.State;
        value.Request = context.Request == null
            ? null : CopyRequest(context.Request);
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.StatusCode = statusCode;
        value.Accepted = accepted;
        value.Final = finalValue;
        value.Forked = context.Forked;
        value.ExpiresSeconds = RemainingExpires(context);
        value.Reason = reason;
        value.DialogId = context.DialogId == null ? null
            : SipReferCore.CopyDialogId(context.DialogId);
        value.NotifyCSeq = context.LastNotifyCSeq;
        value.Version = context.Version == 0 ? -1 : context.Version;
        value.RefreshAtMilliseconds = context.RefreshAtMilliseconds;
        value.ExpiresAtMilliseconds = context.ExpiresAtMilliseconds;
        value.RetryAtMilliseconds = context.RetryAtMilliseconds;
        value.RetryAfterSeconds = context.RetryAfterSeconds;
        value.ErrorMessage = error;
        value.CorrelationId = context.Request == null
            ? null : context.Request.CorrelationId;
        return value;
    }

    int RemainingExpires(SipSubscriptionClientContext context)
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

    void QueueEvent(SipSubscriptionEvent value)
    {
        if (events.Count >= SipDefaults.MaxQueueCount)
        {
            throw new System.InvalidOperationException(
                "The subscription event queue limit was reached.");
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
        copy.Notification = SipSnapshotCopy.CopyMessageModel(
            source.Notification);
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
        copy.Authorization = CopyAuthorization(source.Authorization);
        copy.ErrorMessage = source.ErrorMessage;
        copy.CorrelationId = source.CorrelationId;
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
        copy.Options = CopyOptions(source.Options);
        copy.ServerOptions = CopyServerOptions(source.ServerOptions);
        copy.Forked = source.Forked;
        copy.Cancellation = source.Cancellation;
        copy.CorrelationId = source.CorrelationId;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        return copy;
    }

    static SipSubscriptionServerOptions CopyServerOptions(
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

    static SipSubscriptionOptions CopyOptions(
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

    static SipSubscriptionClientContext CopyContext(
        SipSubscriptionClientContext source)
    {
        SipSubscriptionClientContext copy =
            new SipSubscriptionClientContext();
        copy.Request = source.Request == null
            ? null : CopyRequest(source.Request);
        copy.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        copy.Response = SipSnapshotCopy.CopyMessageModel(source.Response);
        copy.BaseId = source.BaseId;
        copy.SubscriptionId = source.SubscriptionId;
        copy.CallId = source.CallId;
        copy.LocalTag = source.LocalTag;
        copy.RemoteTag = source.RemoteTag;
        copy.CSeq = source.CSeq;
        copy.LastNotifyCSeq = source.LastNotifyCSeq;
        copy.Version = source.Version;
        copy.State = source.State;
        copy.DialogId = source.DialogId == null ? null
            : SipReferCore.CopyDialogId(source.DialogId);
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.RefreshAtMilliseconds = source.RefreshAtMilliseconds;
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        copy.RetryAtMilliseconds = source.RetryAtMilliseconds;
        copy.RetryAfterSeconds = source.RetryAfterSeconds;
        copy.Forked = source.Forked;
        copy.SubscribeFinal = source.SubscribeFinal;
        copy.NotificationFinal = source.NotificationFinal;
        copy.Terminating = source.Terminating;
        copy.AuthenticationAttempts = source.AuthenticationAttempts;
        return copy;
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
