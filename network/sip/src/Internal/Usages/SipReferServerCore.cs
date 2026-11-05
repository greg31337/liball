namespace sip;

internal class SipReferServerCore
{
    object gate = new object();
    System.Collections.Generic.List<SipReferServerContext> contexts =
        new System.Collections.Generic.List<SipReferServerContext>();
    System.Collections.Generic.List<SipReferEvent> events =
        new System.Collections.Generic.List<SipReferEvent>();
    SipProtocolTokenGenerator tokens;
    ISipAuthenticationServer? authenticationServer;
    SipNonceOptions? nonceOptions;

    internal SipReferServerCore(ISipRandomSource randomSource)
    {
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }
        tokens = new SipProtocolTokenGenerator(randomSource);
    }

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

    internal SipReferEvent Receive(
        SipDialogId? dialogId, SipMessage message,
        SipReferServerOptions options)
    {
        lock (gate)
        {
            if (message == null)
            {
                throw new System.ArgumentNullException("message");
            }
            ValidateOptions(options);
            if (!IsReferRequest(message))
            {
                return Reject(message, dialogId,
                    SipStatusCodes.MethodNotAllowed,
                    "A REFER request is required.", null);
            }
            SipValidationIssue[] issues =
                SipMessageValidator.ValidateMessageModel(message, true);
            if (SipMessageValidator.HasErrorIssues(issues))
            {
                return Reject(message, dialogId,
                    SipStatusCodes.BadRequest,
                    "The REFER request is malformed.", null);
            }
            bool inDialog = SipReferCore.ReadTag(message.To) != null;
            if (inDialog && (!options.AllowInDialog || dialogId == null)
                || !inDialog && (!options.AllowOutOfDialog
                    || dialogId != null)
                || dialogId != null
                    && !SipReferCore.MessageMatchesInboundDialog(
                        message, dialogId))
            {
                return Reject(message, dialogId,
                    SipStatusCodes.CallOrTransactionDoesNotExist,
                    "The REFER request does not match dialog policy.", null);
            }
            SipContactValue? referContact =
                message.ContactValues == null
                    || message.ContactValues.Length != 1
                    ? null : message.ContactValues[0];
            SipNameAddress? referContactAddress = referContact == null
                ? null : referContact.Address;
            if (referContactAddress == null
                || !SipReferCore.IsValidSipUri(referContactAddress.Uri))
            {
                return Reject(message, dialogId,
                    SipStatusCodes.BadRequest,
                    "A REFER request requires exactly one valid Contact.",
                    null);
            }
            if (message.Body.Length > options.MaxBodyBytes)
            {
                return Reject(message, dialogId,
                    SipStatusCodes.RequestEntityTooLarge,
                    "The REFER body exceeds the configured size policy.",
                    null);
            }
            if (message.Body.Length > 0
                && !SipReferCore.IsValidMediaType(message.ContentType))
            {
                return Reject(message, dialogId,
                    SipStatusCodes.UnsupportedMediaType,
                    "A valid Content-Type is required for the REFER body.",
                    null);
            }
            SipReferHeaderParse headers =
                SipReferCore.ParseReferHeaders(message);
            if (!headers.Success || headers.ReferTo == null
                || headers.ReferTo.Uri == null)
            {
                return Reject(message, dialogId,
                    SipStatusCodes.BadRequest,
                    headers.ErrorMessage == null
                        ? "The Refer-To value is malformed."
                        : headers.ErrorMessage, null);
            }
            if (!SchemeAllowed(
                    headers.ReferTo.Uri, options.AllowedReferToSchemes))
            {
                return Reject(message, dialogId,
                    SipStatusCodes.Forbidden,
                    "The Refer-To URI scheme is not allowed.",
                    headers.ReferTo);
            }
            bool requestedNoSubscription = headers.ReferSubPresent
                && !headers.ReferSub;
            bool requiresNoSubscription = SipReferCore.HasOptionTag(
                message, SipHeaderNames.Require,
                SipOptionTags.NoReferSub);
            if (requiresNoSubscription && !options.AllowNoSubscription)
            {
                SipReferEvent unsupported = Reject(message, dialogId,
                    SipStatusCodes.BadExtension,
                    "The norefersub extension is not supported.",
                    headers.ReferTo);
                if (unsupported.Response != null)
                {
                    SipReferCore.AppendHeader(unsupported.Response,
                        SipHeaderNames.Unsupported,
                        SipOptionTags.NoReferSub);
                }
                return unsupported;
            }
            SipReferEvent? authenticationFailure = Authenticate(
                message, dialogId, options, headers.ReferTo);
            if (authenticationFailure != null)
            {
                QueueEvent(authenticationFailure);
                return CopyEvent(authenticationFailure);
            }
            bool noSubscription = requestedNoSubscription
                && options.AllowNoSubscription;
            SipMessage? response = SipUasResponseCore.CreateResponseMessage(
                message, SipStatusCodes.Accepted,
                SipReasonPhrases.Accepted);
            AddResponseContact(response, message.RequestUri);
            if (response != null && noSubscription)
            {
                SipReferCore.AppendHeader(response,
                    SipHeaderNames.ReferSub, "false");
                SipReferCore.AppendHeader(response,
                    SipHeaderNames.Supported,
                    SipOptionTags.NoReferSub);
            }
            if (noSubscription)
            {
                SipReferEvent acceptedWithoutSubscription =
                    CreateServerEvent(null, headers.ReferTo,
                        SipReferState.Succeeded, message, response,
                        null, SipStatusCodes.Accepted, true, true,
                        true, null, dialogId);
                QueueEvent(acceptedWithoutSubscription);
                return CopyEvent(acceptedWithoutSubscription);
            }
            SipReferServerContext context = CreateContext(
                message, response, headers.ReferTo, dialogId, options);
            contexts.Add(context);
            SipMessage initial = BuildNotification(
                context, SipStatusCodes.Trying,
                SipReasonPhrases.Trying, false,
                options.SubscriptionExpiresSeconds);
            SipReferEvent accepted = CreateServerEvent(context,
                headers.ReferTo, SipReferState.Pending, message,
                response, initial, SipStatusCodes.Accepted,
                true, false, false, null, context.DialogId);
            QueueEvent(accepted);
            return CopyEvent(accepted);
        }
    }

    internal SipReferEvent ReportProgress(SipReferProgress progress)
    {
        lock (gate)
        {
            if (progress == null || progress.SubscriptionId == null
                || progress.StatusCode < 100
                || progress.StatusCode > 699
                || progress.ExpiresSeconds < 0)
            {
                throw new System.ArgumentException(
                    "Valid REFER progress and subscription state are required.",
                    "progress");
            }
            int index = FindContext(progress.SubscriptionId);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    "The REFER subscription is not active.", "progress");
            }
            SipReferServerContext context = contexts[index];
            bool finalValue = progress.Final
                || progress.StatusCode >= 200;
            string reason = SanitizeReason(
                progress.ReasonPhrase,
                SipReferCore.ReasonPhrase(progress.StatusCode));
            SipMessage notification = BuildNotification(context,
                progress.StatusCode, reason, finalValue,
                progress.ExpiresSeconds);
            SipReferState state = finalValue
                ? progress.StatusCode >= 200
                    && progress.StatusCode < 300
                    ? SipReferState.Succeeded : SipReferState.Failed
                : progress.StatusCode <= 100
                    ? SipReferState.Pending : SipReferState.Active;
            SipReferEvent value = CreateServerEvent(context,
                context.ReferTo, state, null, null, notification,
                progress.StatusCode,
                finalValue && progress.StatusCode >= 200
                    && progress.StatusCode < 300,
                finalValue, false,
                finalValue && progress.StatusCode >= 300
                    ? "The referred operation failed." : null,
                context.DialogId);
            value.CorrelationId = progress.CorrelationId == null
                ? context.CorrelationId : progress.CorrelationId;
            if (finalValue)
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

    SipReferEvent? Authenticate(
        SipMessage message, SipDialogId? dialogId,
        SipReferServerOptions options, SipNameAddress referTo)
    {
        if (!options.RequireAuthentication)
        {
            return null;
        }
        if (authenticationServer == null || nonceOptions == null)
        {
            SipMessage? response = SipUasResponseCore.CreateResponseMessage(
                message, SipStatusCodes.ServerInternalError,
                SipReferCore.ReasonPhrase(
                    SipStatusCodes.ServerInternalError));
            return CreateServerEvent(null, referTo,
                SipReferState.Failed, message, response, null,
                SipStatusCodes.ServerInternalError, false, true, false,
                "REFER authentication is required but is not configured.",
                dialogId);
        }
        SipAuthenticationRequest request =
            new SipAuthenticationRequest();
        request.Message = SipReferCore.CopyMessage(message);
        request.Target = message.RequestUri;
        request.Body = (byte[])message.Body.Clone();
        request.Scope = SipCredentialScope.Origin;
        request.CorrelationId = SipReferCore.ReadCallId(message);
        SipAuthenticationResult authentication =
            authenticationServer.VerifyAuthentication(request);
        if (authentication.Status == SipAuthenticationStatus.Success)
        {
            return null;
        }
        if (authentication.Message == null)
        {
            authentication = authenticationServer
                .CreateAuthenticationChallenge(request, nonceOptions);
        }
        SipReferEvent challenged = CreateServerEvent(null, referTo,
            SipReferState.Failed, message, authentication.Message,
            null, authentication.Message == null ? 0
                : authentication.Message.StatusCode,
            false, true, false,
            authentication.ErrorMessage == null
                ? "REFER authentication is required."
                : authentication.ErrorMessage, dialogId);
        return challenged;
    }

    SipReferServerContext CreateContext(
        SipMessage message, SipMessage? response, SipNameAddress referTo,
        SipDialogId? dialogId, SipReferServerOptions options)
    {
        if (message.CSeq == null)
        {
            throw new System.InvalidOperationException(
                "The REFER CSeq is unavailable.");
        }
        SipReferServerContext context = new SipReferServerContext();
        context.Refer = SipReferCore.CopyMessage(message);
        SipNameAddress? copiedReferTo =
            SipReferCore.CopyNameAddress(referTo);
        context.ReferTo = copiedReferTo == null
            ? new SipNameAddress() : copiedReferTo;
        context.ReferCSeq = message.CSeq.SequenceNumber;
        context.SubscriptionId = SipReferCore.ReadCallId(message) + ":"
            + context.ReferCSeq.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        context.ExpiresSeconds = options.SubscriptionExpiresSeconds;
        context.CorrelationId = SipReferCore.ReadCallId(message);
        if (dialogId != null)
        {
            context.DialogId = SipReferCore.CopyDialogId(dialogId);
        }
        else
        {
            SipDialogId created = new SipDialogId();
            created.CallId = SipReferCore.ReadCallId(message);
            created.LocalTag = response == null
                ? null : SipReferCore.ReadTag(response.To);
            created.RemoteTag = SipReferCore.ReadTag(message.From);
            context.DialogId = created;
        }
        return context;
    }

    SipMessage BuildNotification(
        SipReferServerContext context, int statusCode, string reason,
        bool finalValue, int expiresSeconds)
    {
        SipContactValue? contact = context.Refer.ContactValues.Length == 0
            ? null : context.Refer.ContactValues[0];
        SipNameAddress? contactAddress = contact == null
            ? null : contact.Address;
        SipUri? target = contactAddress == null
            ? null : contactAddress.Uri;
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipNameAddress from = CreateNotificationAddress(
            context.Refer.To, context.DialogId.LocalTag);
        SipNameAddress to = CreateNotificationAddress(
            context.Refer.From, context.DialogId.RemoteTag);
        string targetText = FormatNotificationUri(target, options);
        string bodyText = "SIP/2.0 " + statusCode.ToString(
            System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n";
        byte[] body = System.Text.Encoding.UTF8.GetBytes(bodyText);
        context.NotificationCSeq = context.NotificationCSeq + 1;
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append("NOTIFY ");
        text.Append(targetText);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/UDP refer.invalid;branch=");
        text.Append(tokens.CreateBranch());
        text.Append("\r\nMax-Forwards: 70\r\nFrom: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(from, options));
        text.Append("\r\nTo: ");
        text.Append(SipHeaderPrimitives.FormatNameAddressValue(to, options));
        text.Append("\r\nCall-ID: ");
        text.Append(context.DialogId.CallId);
        text.Append("\r\nCSeq: ");
        text.Append(context.NotificationCSeq.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append(" NOTIFY\r\nEvent: refer;id=");
        text.Append(context.ReferCSeq.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append("\r\nSubscription-State: ");
        text.Append(finalValue ? "terminated;reason=noresource" :
            statusCode <= 100 ? "pending;expires=" : "active;expires=");
        if (!finalValue)
        {
            text.Append(expiresSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        }
        text.Append("\r\nContent-Type: message/sipfrag\r\nContent-Length: ");
        text.Append(body.Length.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append("\r\n\r\n");
        byte[] header = System.Text.Encoding.UTF8.GetBytes(text.ToString());
        byte[] wire = new byte[header.Length + body.Length];
        System.Array.Copy(header, wire, header.Length);
        System.Array.Copy(body, 0, wire, header.Length, body.Length);
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(
            wire, new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete
            || parsed.Message == null)
        {
            throw new System.InvalidOperationException(
                "The refer-event NOTIFY could not be constructed.");
        }
        return parsed.Message;
    }

    SipReferEvent Reject(
        SipMessage message, SipDialogId? dialogId, int statusCode,
        string error, SipNameAddress? referTo)
    {
        SipMessage? response = SipUasResponseCore.CreateResponseMessage(
            message, statusCode, SipReferCore.ReasonPhrase(statusCode));
        if (response != null && statusCode == SipStatusCodes.MethodNotAllowed)
        {
            SipReferCore.AppendHeader(response,
                SipHeaderNames.Allow, SipMethods.Refer);
        }
        SipReferEvent rejected = CreateServerEvent(null, referTo,
            SipReferState.Failed, message, response, null,
            statusCode, false, true, false, error, dialogId);
        QueueEvent(rejected);
        return CopyEvent(rejected);
    }

    static void ValidateOptions(SipReferServerOptions? options)
    {
        if (options == null || options.MaxBodyBytes < 0
            || options.SubscriptionExpiresSeconds < 0
            || options.AllowedReferToSchemes == null)
        {
            throw new System.ArgumentException(
                "Valid REFER server policy is required.", "options");
        }
        int index = 0;
        while (index < options.AllowedReferToSchemes.Length)
        {
            if (!SipUriSyntax.IsScheme(
                    options.AllowedReferToSchemes[index]))
            {
                throw new System.ArgumentException(
                    "Every allowed Refer-To scheme must be valid.",
                    "options");
            }
            index = index + 1;
        }
    }

    static bool IsReferRequest(SipMessage message)
    {
        return message.Kind == SipMessageKind.Request
            && message.Method == SipMethod.Refer
            && SipAscii.EqualsIgnoreCase(
                message.MethodToken, SipMethods.Refer);
    }

    static bool SchemeAllowed(SipUri uri, string[] allowed)
    {
        if (!SipReferCore.IsValidSipOrAbsoluteUri(uri))
        {
            return false;
        }
        if (allowed.Length == 0)
        {
            return true;
        }
        string? scheme = uri.SchemeToken;
        int index = 0;
        while (index < allowed.Length)
        {
            if (SipAscii.EqualsIgnoreCase(allowed[index], scheme))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void AddResponseContact(SipMessage? response, SipUri? uri)
    {
        if (response == null || !SipReferCore.IsValidSipUri(uri))
        {
            return;
        }
        SipNameAddress address = new SipNameAddress();
        address.Uri = SipUriModelCopy.CreateNormalizedCopy(uri);
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipReferCore.AppendHeader(response, SipHeaderNames.Contact,
            SipHeaderPrimitives.FormatNameAddressValue(address, options));
        SipContactValue contact = new SipContactValue();
        contact.Address = address;
        response.ContactValues = new SipContactValue[] { contact };
    }

    static SipNameAddress CreateNotificationAddress(
        SipNameAddress? source, string? tag)
    {
        SipNameAddress? copied = SipReferCore.CopyNameAddress(source);
        SipNameAddress address = copied == null
            ? new SipNameAddress() : copied;
        System.Collections.Generic.List<SipUriParameter> parameters =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (address.Parameters != null
            && index < address.Parameters.Length)
        {
            SipUriParameter parameter = address.Parameters[index];
            if (!SipAscii.EqualsIgnoreCase(parameter.Name, "tag"))
            {
                parameters.Add(parameter);
            }
            index = index + 1;
        }
        if (tag != null)
        {
            SipUriParameter tagParameter = new SipUriParameter();
            tagParameter.Name = "tag";
            tagParameter.Value = tag;
            tagParameter.HasValue = true;
            parameters.Add(tagParameter);
        }
        address.Parameters = parameters.ToArray();
        return address;
    }

    static string FormatNotificationUri(
        SipUri? uri, SipWriteOptions options)
    {
        SipWriteResult formatted = SipUriFormatter.FormatUriModel(uri, options);
        if (!formatted.Success)
        {
            throw new System.InvalidOperationException(
                "The REFER Contact URI is invalid.");
        }
        return System.Text.Encoding.ASCII.GetString(formatted.Bytes);
    }

    static string SanitizeReason(string? supplied, string fallback)
    {
        if (supplied == null || supplied.Length == 0
            || supplied.IndexOf('\r') >= 0
            || supplied.IndexOf('\n') >= 0)
        {
            return fallback;
        }
        return supplied;
    }

    int FindContext(string subscriptionId)
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

    static SipReferEvent CreateServerEvent(
        SipReferServerContext? context, SipNameAddress? referTo,
        SipReferState state, SipMessage? message, SipMessage? response,
        SipMessage? notification, int statusCode, bool success,
        bool finalValue, bool noSubscription, string? error,
        SipDialogId? dialogId)
    {
        SipReferEvent value = new SipReferEvent();
        value.State = state;
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.Notification = SipSnapshotCopy.CopyMessageModel(notification);
        value.ReferTo = SipReferCore.CopyNameAddress(referTo);
        value.DialogId = dialogId == null ? null
            : SipReferCore.CopyDialogId(dialogId);
        value.SubscriptionId = context == null
            ? null : context.SubscriptionId;
        value.NoSubscription = noSubscription;
        value.StatusCode = statusCode;
        value.Success = success;
        value.Final = finalValue;
        value.ErrorMessage = error;
        value.CorrelationId = context == null
            ? SipReferCore.ReadCallId(message == null
                ? new SipMessage() : message)
            : context.CorrelationId;
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
        copy.Request = source.Request;
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
}
