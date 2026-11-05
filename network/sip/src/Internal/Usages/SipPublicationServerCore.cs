namespace sip;

internal class SipPublicationServerCore
{
    const int InitialOperation = 1;
    const int ModifyOperation = 2;
    const int RefreshOperation = 3;
    const int RemoveOperation = 4;

    object gate = new object();
    System.Collections.Generic.List<SipPublicationEvent> events =
        new System.Collections.Generic.List<SipPublicationEvent>();
    SipProtocolTokenGenerator tokens;
    ISipPublicationStore publicationStore;
    ISipEventPackageRegistry eventPackageRegistry;
    ISipPublicationAuthorizationPolicy? authorizationPolicy;
    long nowMilliseconds;

    internal SipPublicationServerCore(ISipRandomSource randomSource)
    {
        if (randomSource == null) throw new System.ArgumentNullException("randomSource");
        tokens = new SipProtocolTokenGenerator(randomSource);
        publicationStore = new SipMemoryPublicationStore();
        eventPackageRegistry = new SipEventPackageRegistry();
    }

    internal void ConfigureStore(ISipPublicationStore configuredStore)
    {
        lock (gate)
        {
            if (configuredStore == null) throw new System.ArgumentNullException("publicationStore");
            publicationStore = configuredStore;
        }
    }

    internal void ConfigureAuthorization(
        ISipPublicationAuthorizationPolicy configuredPolicy)
    {
        lock (gate)
        {
            if (configuredPolicy == null) throw new System.ArgumentNullException("authorizationPolicy");
            authorizationPolicy = configuredPolicy;
        }
    }

    internal void ConfigureEventPackages(ISipEventPackageRegistry configuredRegistry)
    {
        lock (gate)
        {
            if (configuredRegistry == null) throw new System.ArgumentNullException("eventPackageRegistry");
            eventPackageRegistry = configuredRegistry;
        }
    }

    internal SipPublicationEvent Process(SipPublicationRequest request)
    {
        SipPublicationServerParse parsed = ParseOperation(request);
        if (parsed.ErrorMessage != null || parsed.Request == null)
            return Reject(request, parsed.StatusCode,
                parsed.ErrorMessage == null ? "The PUBLISH request is invalid." : parsed.ErrorMessage,
                null, parsed.MinimumExpires);
        SipPublicationRequest normalized = parsed.Request;
        SipPublication? current;
        ISipPublicationAuthorizationPolicy? policy;
        lock (gate)
        {
            current = parsed.Operation == InitialOperation
                ? null : FindConditionalPublication(normalized);
            policy = authorizationPolicy;
        }
        if (parsed.Operation != InitialOperation && current == null)
            return Reject(normalized, SipStatusCodes.ConditionalRequestFailed,
                "The SIP-If-Match entity-tag does not identify current event state.",
                null, -1);
        SipAuthorizationDecision? authorization = Authorize(
            policy, normalized, current, parsed.Operation);
        if (normalized.ServerOptions.RequireAuthorization && authorization == null)
            return Reject(normalized, SipStatusCodes.Forbidden,
                "Publication authorization is required.", null, -1);
        if (authorization != null && !authorization.Allowed)
        {
            int status = authorization.StatusCode >= 300
                && authorization.StatusCode <= 699
                ? authorization.StatusCode : SipStatusCodes.Forbidden;
            string error = authorization.Reason == null
                || authorization.Reason.Length == 0
                ? "Application policy denied the publication."
                : authorization.Reason;
            return Reject(normalized, status, error, authorization, -1);
        }
        lock (gate)
        {
            if (parsed.Operation != InitialOperation)
            {
                current = FindConditionalPublication(normalized);
                if (current == null)
                    return Reject(normalized, SipStatusCodes.ConditionalRequestFailed,
                        "The publication changed before the conditional update was applied.",
                        authorization, -1);
            }
            else if (GetStorePublications().Length
                >= normalized.ServerOptions.MaxPublications)
                return Reject(normalized, SipStatusCodes.ServiceUnavailable,
                    "The publication store limit was reached.", authorization, -1);
            return ApplyOperation(normalized, current, parsed.Operation, authorization);
        }
    }

    internal void Expire(long nextMilliseconds)
    {
        lock (gate)
        {
            if (nextMilliseconds < nowMilliseconds)
                throw new System.ArgumentException(
                    "Publication time cannot move backwards.", "nowMilliseconds");
            nowMilliseconds = nextMilliseconds;
            SipPublication[] publications = GetStorePublications();
            int index = 0;
            while (index < publications.Length)
            {
                SipPublication publication = publications[index];
                if (publication.PublicationId != null
                    && publication.ExpiresAtMilliseconds >= 0
                    && publication.ExpiresAtMilliseconds <= nowMilliseconds)
                {
                    publicationStore.RemovePublication(publication.PublicationId);
                    eventPackageRegistry.ClearEventPackageState(publication.PublicationId);
                    SipPublicationEvent expired = CreateStoredEvent(publication,
                        SipPublicationState.Terminated, 0,
                        true, true, "expired", null, null, null);
                    QueueEvent(expired, new SipPublicationServerOptions());
                }
                index = index + 1;
            }
        }
    }

    internal SipPublication[] GetPublications()
    {
        lock (gate)
        {
            return GetStorePublications();
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

    SipPublicationEvent ApplyOperation(
        SipPublicationRequest request, SipPublication? current,
        int operation, SipAuthorizationDecision? authorization)
    {
        string publicationId = current == null || current.PublicationId == null
            ? CreatePublicationId() : current.PublicationId;
        if ((operation == InitialOperation || operation == ModifyOperation)
            && request.ServerOptions.ValidateEventBodies)
        {
            SipEventPackageResult validation = ValidateEventBody(
                request, publicationId);
            if (!validation.Accepted)
                return Reject(request,
                    validation.StatusCode < 300 ? SipStatusCodes.BadRequest
                        : validation.StatusCode,
                    validation.ErrorMessage == null
                        ? "The event package body is invalid."
                        : validation.ErrorMessage,
                    authorization, -1);
        }
        string entityTag = CreateEntityTag(request);
        int expires = operation == RemoveOperation ? 0 : request.ExpiresSeconds;
        SipPublication? stored = null;
        bool aggregate = operation != RefreshOperation;
        if (operation == RemoveOperation)
        {
            if (current == null || current.PublicationId == null)
                return Reject(request, SipStatusCodes.ConditionalRequestFailed,
                    "The publication no longer exists.", authorization, -1);
            publicationStore.RemovePublication(current.PublicationId);
            eventPackageRegistry.ClearEventPackageState(current.PublicationId);
        }
        else
        {
            stored = BuildStoredPublication(
                request, current, publicationId, entityTag, operation, expires);
            publicationStore.StorePublication(stored);
        }
        SipMessage? response = CreateResponse(request.Message,
            SipStatusCodes.Ok, entityTag, expires, -1);
        SipPublicationEvent value = new SipPublicationEvent();
        value.PublicationId = publicationId;
        value.State = operation == RemoveOperation
            ? SipPublicationState.Terminated : SipPublicationState.Published;
        value.Request = CopyRequest(request);
        value.Publication = stored == null ? null
            : SipPublicationModelCopy.CopyPublication(stored);
        value.Message = SipSnapshotCopy.CopyMessageModel(request.Message);
        value.Response = SipSnapshotCopy.CopyMessageModel(response);
        value.StatusCode = SipStatusCodes.Ok;
        value.Accepted = true;
        value.Final = true;
        value.AggregationRequired = aggregate;
        value.EntityTag = entityTag;
        value.ExpiresSeconds = expires;
        value.ExpiresAtMilliseconds = stored == null
            ? -1 : stored.ExpiresAtMilliseconds;
        value.Reason = OperationReason(operation);
        value.Authorization = SipPublicationModelCopy.CopyAuthorization(authorization);
        value.CorrelationId = request.CorrelationId;
        QueueEvent(value, request.ServerOptions);
        return CopyEvent(value);
    }

    SipPublication BuildStoredPublication(
        SipPublicationRequest request, SipPublication? current,
        string publicationId, string entityTag, int operation, int expires)
    {
        SipPublication publication = current == null
            ? new SipPublication() : SipPublicationModelCopy.CopyPublication(current);
        publication.PublicationId = publicationId;
        publication.Target = SipUriModelCopy.CreateNormalizedCopy(request.Target);
        publication.Sender = SipUriModelCopy.CreateNormalizedCopy(request.Sender);
        publication.EventPackage = request.EventPackage;
        publication.EntityTag = entityTag;
        if (operation == InitialOperation || operation == ModifyOperation)
        {
            publication.Body = (byte[])request.Body.Clone();
            publication.ContentType = SipReferCore.CopyMediaType(request.ContentType);
        }
        publication.UpdatedAtMilliseconds = nowMilliseconds;
        publication.ExpiresAtMilliseconds = AddTime(
            nowMilliseconds, (long)expires * 1000);
        publication.CorrelationId = request.CorrelationId;
        return publication;
    }

    SipEventPackageResult ValidateEventBody(
        SipPublicationRequest request, string publicationId)
    {
        SipEventPackageRequest bodyRequest = new SipEventPackageRequest();
        bodyRequest.SubscriptionId = publicationId;
        bodyRequest.EventPackage = request.EventPackage;
        bodyRequest.Body = (byte[])request.Body.Clone();
        bodyRequest.ContentType = SipReferCore.CopyMediaType(request.ContentType);
        bodyRequest.Options = new SipParseOptions();
        bodyRequest.Options.MaxBodyBytes = request.ServerOptions.MaxBodyBytes;
        bodyRequest.Options.MaxMessageBytes = request.ServerOptions.MaxBodyBytes;
        bodyRequest.CorrelationId = request.CorrelationId;
        return eventPackageRegistry.ProcessEventBody(bodyRequest);
    }

    SipPublicationServerParse ParseOperation(SipPublicationRequest? request)
    {
        SipPublicationServerParse result = new SipPublicationServerParse();
        if (request == null || request.ServerOptions == null)
        {
            result.StatusCode = SipStatusCodes.BadRequest;
            result.ErrorMessage = "Publication request and server options are required.";
            return result;
        }
        string? optionsError = ValidateOptions(request.ServerOptions);
        if (optionsError != null)
        {
            result.StatusCode = SipStatusCodes.BadRequest;
            result.ErrorMessage = optionsError;
            return result;
        }
        SipPublicationRequest normalized;
        if (request.Message != null)
        {
            SipPublicationServerParse messageResult = ParseMessageRequest(request);
            if (messageResult.ErrorMessage != null) return messageResult;
            if (messageResult.Request == null) return messageResult;
            normalized = messageResult.Request;
        }
        else normalized = CopyRequest(request);
        result.Request = normalized;
        if (normalized.Body == null || normalized.Body.Length > normalized.ServerOptions.MaxBodyBytes)
        {
            result.StatusCode = normalized.Body != null
                && normalized.Body.Length > normalized.ServerOptions.MaxBodyBytes
                ? SipStatusCodes.RequestEntityTooLarge : SipStatusCodes.BadRequest;
            result.ErrorMessage = "The publication body is unavailable or too large.";
            return result;
        }
        if (!SipReferCore.IsValidSipUri(normalized.Target)
            || !SipReferCore.IsValidSipUri(normalized.Sender)
            || normalized.EventPackage == null
            || !SipAscii.IsToken(normalized.EventPackage)
            || normalized.EntityTag != null && !SipAscii.IsToken(normalized.EntityTag)
            || normalized.ContentType != null
                && !SipReferCore.IsValidMediaType(normalized.ContentType))
        {
            result.StatusCode = SipStatusCodes.BadRequest;
            result.ErrorMessage = "The publication identity, event, entity-tag, or media type is invalid.";
            return result;
        }
        if (normalized.ExpiresSeconds > normalized.ServerOptions.MaxExpiresSeconds)
            normalized.ExpiresSeconds = normalized.ServerOptions.MaxExpiresSeconds;
        if (normalized.ExpiresSeconds > 0
            && normalized.ExpiresSeconds < normalized.ServerOptions.MinExpiresSeconds)
        {
            result.StatusCode = SipStatusCodes.IntervalTooBrief;
            result.MinimumExpires = normalized.ServerOptions.MinExpiresSeconds;
            result.ErrorMessage = "The requested publication interval is too brief.";
            return result;
        }
        result.Operation = ClassifyOperation(normalized);
        if (result.Operation == 0)
        {
            result.StatusCode = normalized.Body.Length > 0
                && normalized.ContentType == null
                ? SipStatusCodes.UnsupportedMediaType : SipStatusCodes.BadRequest;
            result.ErrorMessage = "The PUBLISH body, SIP-If-Match, and Expires combination is invalid.";
        }
        return result;
    }

    SipPublicationServerParse ParseMessageRequest(SipPublicationRequest supplied)
    {
        SipPublicationServerParse result = new SipPublicationServerParse();
        SipMessage message = supplied.Message == null
            ? throw new System.InvalidOperationException("The PUBLISH message is unavailable.")
            : supplied.Message;
        if (message.Kind != SipMessageKind.Request
            || message.Method != SipMethod.Publish || message.CSeq == null
            || message.CSeq.Method != SipMethod.Publish
            || SipReferCore.ReadCallId(message) == null)
        {
            result.StatusCode = SipStatusCodes.MethodNotAllowed;
            result.ErrorMessage = "A valid PUBLISH request is required.";
            return result;
        }
        if (message.Body == null || message.Body.Length > supplied.ServerOptions.MaxBodyBytes)
        {
            result.StatusCode = message.Body != null
                && message.Body.Length > supplied.ServerOptions.MaxBodyBytes
                ? SipStatusCodes.RequestEntityTooLarge : SipStatusCodes.BadRequest;
            result.ErrorMessage = "The PUBLISH body is unavailable or too large.";
            return result;
        }
        string? eventPackage = null;
        string? entityTag = null;
        int expires = supplied.ServerOptions.DefaultExpiresSeconds;
        int eventCount = 0;
        int tagCount = 0;
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
                        SipHeaderNames.Event, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.Values.Length != 1)
                {
                    result.StatusCode = SipStatusCodes.BadEvent;
                    result.ErrorMessage = "The Event header field is malformed.";
                    return result;
                }
                eventPackage = parsed.Values[0].Token;
            }
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.SipIfMatch))
            {
                tagCount = tagCount + 1;
                if (!SipAscii.IsToken(header.Value))
                {
                    result.StatusCode = SipStatusCodes.BadRequest;
                    result.ErrorMessage = "The SIP-If-Match value is malformed.";
                    return result;
                }
                entityTag = header.Value;
            }
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.Expires))
            {
                expiresCount = expiresCount + 1;
                SipNumericParseResult parsed = SipNumericParser.ParseNonNegativeInt32Text(
                    header.Value, int.MaxValue);
                if (!parsed.Success)
                {
                    result.StatusCode = SipStatusCodes.BadRequest;
                    result.ErrorMessage = "The Expires value is malformed.";
                    return result;
                }
                expires = parsed.Value;
            }
            index = index + 1;
        }
        if (eventCount != 1)
        {
            result.StatusCode = SipStatusCodes.BadEvent;
            result.ErrorMessage = "PUBLISH requires exactly one Event header field.";
            return result;
        }
        if (tagCount > 1 || expiresCount > 1)
        {
            result.StatusCode = SipStatusCodes.BadRequest;
            result.ErrorMessage = "PUBLISH permits at most one SIP-If-Match and Expires value.";
            return result;
        }
        SipPublicationRequest normalized = CopyRequest(supplied);
        normalized.Message = SipSnapshotCopy.CopyMessageModel(message);
        normalized.Sender = message.From == null || message.From.Uri == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(message.From.Uri);
        normalized.Target = message.RequestUri == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(message.RequestUri);
        normalized.EventPackage = eventPackage;
        normalized.EntityTag = entityTag;
        normalized.ExpiresSeconds = expires;
        normalized.Body = (byte[])message.Body.Clone();
        normalized.ContentType = SipReferCore.CopyMediaType(message.ContentType);
        if (normalized.CorrelationId == null)
            normalized.CorrelationId = SipReferCore.ReadCallId(message);
        result.Request = normalized;
        return result;
    }

    static int ClassifyOperation(SipPublicationRequest request)
    {
        bool body = request.Body.Length > 0;
        bool conditional = request.EntityTag != null;
        if (body && !conditional && request.ExpiresSeconds > 0
            && request.ContentType != null) return InitialOperation;
        if (body && conditional && request.ExpiresSeconds > 0
            && request.ContentType != null) return ModifyOperation;
        if (!body && conditional && request.ExpiresSeconds > 0
            && request.ContentType == null) return RefreshOperation;
        if (!body && conditional && request.ExpiresSeconds == 0
            && request.ContentType == null) return RemoveOperation;
        return 0;
    }

    static string? ValidateOptions(SipPublicationServerOptions options)
    {
        if (options.DefaultExpiresSeconds < 0
            || options.MinExpiresSeconds < 0
            || options.MaxExpiresSeconds < options.MinExpiresSeconds
            || options.DefaultExpiresSeconds > options.MaxExpiresSeconds
            || options.MaxBodyBytes < 0 || options.MaxPublications < 1
            || options.MaxEventCount < 1)
            return "Valid publication expiry, size, store, and queue limits are required.";
        return null;
    }

    SipPublication? FindConditionalPublication(SipPublicationRequest request)
    {
        SipPublication[] publications = GetStorePublications();
        int index = 0;
        while (index < publications.Length)
        {
            SipPublication publication = publications[index];
            if (publication.EntityTag == request.EntityTag
                && publication.EventPackage == request.EventPackage
                && publication.Target != null && request.Target != null
                && UrisEqual(publication.Target, request.Target))
                return SipPublicationModelCopy.CopyPublication(publication);
            index = index + 1;
        }
        return null;
    }

    SipPublication[] GetStorePublications()
    {
        SipPublication[] source = publicationStore.GetPublications();
        if (source == null) throw new System.InvalidOperationException(
            "The publication store returned no snapshot array.");
        SipPublication[] result = new SipPublication[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipPublication? publication = source[index];
            if (publication == null) throw new System.InvalidOperationException(
                "The publication store returned a null record.");
            result[index] = SipPublicationModelCopy.CopyPublication(publication);
            index = index + 1;
        }
        return result;
    }

    string CreatePublicationId()
    {
        int attempt = 0;
        while (attempt < 16)
        {
            string candidate = tokens.CreateCallId();
            if (!ContainsPublicationId(candidate)) return candidate;
            attempt = attempt + 1;
        }
        throw new System.InvalidOperationException(
            "A unique publication identifier could not be generated.");
    }

    string CreateEntityTag(SipPublicationRequest request)
    {
        SipPublication[] publications = GetStorePublications();
        int attempt = 0;
        while (attempt < 16)
        {
            string candidate = tokens.CreateTag();
            bool used = false;
            int index = 0;
            while (index < publications.Length)
            {
                SipPublication publication = publications[index];
                if (publication.EntityTag == candidate
                    && publication.Target != null && request.Target != null
                    && UrisEqual(publication.Target, request.Target)) used = true;
                index = index + 1;
            }
            if (!used) return candidate;
            attempt = attempt + 1;
        }
        throw new System.InvalidOperationException(
            "A unique publication entity-tag could not be generated.");
    }

    bool ContainsPublicationId(string publicationId)
    {
        SipPublication[] publications = GetStorePublications();
        int index = 0;
        while (index < publications.Length)
        {
            if (publications[index].PublicationId == publicationId) return true;
            index = index + 1;
        }
        return false;
    }

    static SipAuthorizationDecision? Authorize(
        ISipPublicationAuthorizationPolicy? policy,
        SipPublicationRequest request, SipPublication? current, int operation)
    {
        if (policy == null) return null;
        SipPublicationAuthorizationContext context =
            new SipPublicationAuthorizationContext();
        context.Message = SipSnapshotCopy.CopyMessageModel(request.Message);
        context.Request = CopyRequest(request);
        context.CurrentPublication = current == null ? null
            : SipPublicationModelCopy.CopyPublication(current);
        context.Initial = operation == InitialOperation;
        context.Modify = operation == ModifyOperation;
        context.Refresh = operation == RefreshOperation;
        context.Remove = operation == RemoveOperation;
        context.CorrelationId = request.CorrelationId;
        return policy.AuthorizePublication(context);
    }

    SipPublicationEvent Reject(
        SipPublicationRequest? request, int statusCode, string error,
        SipAuthorizationDecision? authorization, int minimumExpires)
    {
        lock (gate)
        {
            SipMessage? message = request == null ? null : request.Message;
            SipMessage? response = CreateResponse(message, statusCode,
                null, -1, minimumExpires);
            AppendErrorCapabilities(response, statusCode,
                request == null ? null : request.EventPackage);
            SipPublicationEvent value = new SipPublicationEvent();
            value.State = SipPublicationState.Failed;
            value.Request = request == null ? null : CopyRequest(request);
            value.Message = SipSnapshotCopy.CopyMessageModel(message);
            value.Response = SipSnapshotCopy.CopyMessageModel(response);
            value.StatusCode = statusCode;
            value.Final = true;
            value.Reason = "rejected";
            value.Authorization = SipPublicationModelCopy.CopyAuthorization(authorization);
            value.ErrorMessage = error;
            value.CorrelationId = request == null ? null : request.CorrelationId;
            QueueEvent(value, request == null ? null : request.ServerOptions);
            return CopyEvent(value);
        }
    }

    static SipMessage? CreateResponse(
        SipMessage? request, int statusCode, string? entityTag,
        int expires, int minimumExpires)
    {
        if (request == null) return null;
        SipMessage? response = SipUasResponseCore.CreateResponseMessage(
            request, statusCode, ReasonPhrase(statusCode));
        if (response != null && entityTag != null)
            SipReferCore.AppendHeader(response, SipHeaderNames.SipETag, entityTag);
        if (response != null && expires >= 0)
            SipReferCore.AppendHeader(response, SipHeaderNames.Expires,
                expires.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (response != null && minimumExpires >= 0)
            SipReferCore.AppendHeader(response, SipHeaderNames.MinExpires,
                minimumExpires.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return response;
    }

    void AppendErrorCapabilities(
        SipMessage? response, int statusCode, string? eventPackage)
    {
        if (response == null) return;
        if (statusCode == SipStatusCodes.MethodNotAllowed)
            SipReferCore.AppendHeader(response, SipHeaderNames.Allow, SipMethods.Publish);
        if (statusCode == SipStatusCodes.BadEvent)
        {
            string packages = JoinEventPackages();
            if (packages.Length > 0)
                SipReferCore.AppendHeader(response, SipHeaderNames.AllowEvents, packages);
        }
        if (statusCode == SipStatusCodes.UnsupportedMediaType)
        {
            string contentTypes = JoinContentTypes(eventPackage);
            if (contentTypes.Length > 0)
                SipReferCore.AppendHeader(response, SipHeaderNames.Accept, contentTypes);
        }
    }

    string JoinEventPackages()
    {
        SipEventPackageRegistration[] registrations =
            eventPackageRegistry.GetEventPackageRegistrations();
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < registrations.Length)
        {
            string? eventPackage = registrations[index].EventPackage;
            if (eventPackage != null && eventPackage.Length > 0)
            {
                if (result.Length > 0) result.Append(", ");
                result.Append(eventPackage);
            }
            index = index + 1;
        }
        return result.ToString();
    }

    string JoinContentTypes(string? eventPackage)
    {
        SipEventPackageRegistration[] registrations =
            eventPackageRegistry.GetEventPackageRegistrations();
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < registrations.Length)
        {
            SipEventPackageRegistration registration = registrations[index];
            if (registration.EventPackage == eventPackage)
            {
                int typeIndex = 0;
                while (typeIndex < registration.ContentTypes.Length)
                {
                    SipMediaType mediaType = registration.ContentTypes[typeIndex];
                    if (mediaType.Type != null && mediaType.Subtype != null)
                    {
                        if (result.Length > 0) result.Append(", ");
                        result.Append(mediaType.Type);
                        result.Append('/');
                        result.Append(mediaType.Subtype);
                    }
                    typeIndex = typeIndex + 1;
                }
            }
            index = index + 1;
        }
        return result.ToString();
    }

    static string ReasonPhrase(int statusCode)
    {
        switch (statusCode)
        {
            case SipStatusCodes.Ok: return SipReasonPhrases.Ok;
            case SipStatusCodes.BadRequest: return SipReasonPhrases.BadRequest;
            case SipStatusCodes.Forbidden: return SipReasonPhrases.Forbidden;
            case SipStatusCodes.MethodNotAllowed: return SipReasonPhrases.MethodNotAllowed;
            case SipStatusCodes.ConditionalRequestFailed:
                return SipReasonPhrases.ConditionalRequestFailed;
            case SipStatusCodes.RequestEntityTooLarge:
                return SipReasonPhrases.RequestEntityTooLarge;
            case SipStatusCodes.UnsupportedMediaType:
                return SipReasonPhrases.UnsupportedMediaType;
            case SipStatusCodes.IntervalTooBrief: return SipReasonPhrases.IntervalTooBrief;
            case SipStatusCodes.BadEvent: return SipReasonPhrases.BadEvent;
            case SipStatusCodes.ServiceUnavailable: return SipReasonPhrases.ServiceUnavailable;
            default: return "Publication Error";
        }
    }

    static string OperationReason(int operation)
    {
        if (operation == InitialOperation) return "initial";
        if (operation == ModifyOperation) return "modified";
        if (operation == RefreshOperation) return "refreshed";
        return "removed";
    }

    SipPublicationEvent CreateStoredEvent(
        SipPublication publication, SipPublicationState state,
        int statusCode, bool finalValue, bool aggregate,
        string reason, string? error, SipMessage? message,
        SipAuthorizationDecision? authorization)
    {
        SipPublicationEvent value = new SipPublicationEvent();
        value.PublicationId = publication.PublicationId;
        value.State = state;
        value.Request = CreateRequestFromPublication(publication);
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.StatusCode = statusCode;
        value.Accepted = statusCode >= 200 && statusCode < 300;
        value.Final = finalValue;
        value.AggregationRequired = aggregate;
        value.EntityTag = publication.EntityTag;
        value.ExpiresSeconds = state == SipPublicationState.Terminated
            ? 0 : RemainingExpires(publication);
        value.ExpiresAtMilliseconds = state == SipPublicationState.Terminated
            ? -1 : publication.ExpiresAtMilliseconds;
        value.Reason = reason;
        value.Authorization = SipPublicationModelCopy.CopyAuthorization(authorization);
        value.ErrorMessage = error;
        value.CorrelationId = publication.CorrelationId;
        return value;
    }

    int RemainingExpires(SipPublication publication)
    {
        if (publication.ExpiresAtMilliseconds < 0) return -1;
        long remaining = publication.ExpiresAtMilliseconds - nowMilliseconds;
        if (remaining <= 0) return 0;
        long seconds = (remaining + 999) / 1000;
        return seconds > int.MaxValue ? int.MaxValue : (int)seconds;
    }

    void QueueEvent(SipPublicationEvent value, SipPublicationServerOptions? options)
    {
        int maximum = options == null ? SipDefaults.MaxQueueCount : options.MaxEventCount;
        if (events.Count >= maximum)
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
        copy.Options = CopyClientOptions(source.Options);
        copy.ServerOptions = SipPublicationModelCopy.CopyServerOptions(source.ServerOptions);
        copy.Cancellation = source.Cancellation;
        copy.CorrelationId = source.CorrelationId;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        return copy;
    }

    static SipPublicationRequest CreateRequestFromPublication(SipPublication publication)
    {
        SipPublicationRequest request = new SipPublicationRequest();
        request.Sender = publication.Sender == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(publication.Sender);
        request.Target = publication.Target == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(publication.Target);
        request.EventPackage = publication.EventPackage;
        request.EntityTag = publication.EntityTag;
        request.Body = publication.Body == null
            ? new byte[0] : (byte[])publication.Body.Clone();
        request.ContentType = SipReferCore.CopyMediaType(publication.ContentType);
        request.CorrelationId = publication.CorrelationId;
        return request;
    }

    static SipPublicationOptions CopyClientOptions(SipPublicationOptions? source)
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

    static long AddTime(long value, long duration)
    {
        return duration > long.MaxValue - value ? long.MaxValue : value + duration;
    }
}
