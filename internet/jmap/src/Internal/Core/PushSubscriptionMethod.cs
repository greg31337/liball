using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal interface IPushVerificationSender
{
    Task SendPushVerificationAsync(PushSubscription subscription, PushVerification verification, JmapRequestContext context, CancellationToken cancellationToken);
}

internal class PushSubscriptionMethod
{
    internal ValueValidator Values = new ValueValidator();
    internal PushValidator Validator = new PushValidator();
    internal PushSubscriptionSecurity Security = new PushSubscriptionSecurity();
    internal PatchValidator Patches = new PatchValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal BlobPushJsonCodec Codec;
    internal ProtocolJsonCodec Protocol;
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);
    internal PushDeliveryManager? PushDelivery;
    internal Dictionary<string, PushSubscriptionSecurityState> SecurityStates = new Dictionary<string, PushSubscriptionSecurityState>(StringComparer.Ordinal);

    internal PushSubscriptionMethod()
    {
        Codec = new BlobPushJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessGetInvocationAsync(
        JmapInvocation invocation,
        IPushService pushService,
        JmapRequestContext context,
        JmapServerOptions options,
        IJmapClock? clock,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessGetInvocation(invocation, pushService, context, options, clock, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessGetInvocation(
        JmapInvocation invocation,
        IPushService pushService,
        JmapRequestContext context,
        JmapServerOptions options,
        IJmapClock? clock,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.PushSubscriptionGet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsGetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The PushSubscription/get arguments are invalid.", callId);
        }

        PushSubscriptionGetRequest request = Codec.DecodePushGetRequest(invocation.Arguments);
        JmapMethodError? requestError = ValidateGetRequest(request, options);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        if (HasPrivateProperty(request.Properties))
        {
            return ErrorInvocation(JmapErrorCodes.Forbidden, JmapMethodErrorKind.Forbidden, "The url and keys properties are private to the creating device.", callId);
        }

        ValidateDispatchDependencies(pushService, context, options);
        if (!IsCredentialActive(context, clock))
        {
            return ErrorInvocation(JmapErrorCodes.Forbidden, JmapMethodErrorKind.Forbidden, "The request credentials cannot access push subscriptions.", callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        PushSubscriptionGetResponse stored = pushService.GetStoredPushSubscriptionsAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        if (request.Ids == null && stored != null && stored.List != null && stored.List.Count > options.MaxObjectsInGet)
        {
            return ErrorInvocation(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "Returning all push subscriptions would exceed the object limit.", callId);
        }

        PushSubscriptionGetResponse response = NormalizeGetResponse(stored, request);
        return GetSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessSetInvocationAsync(
        JmapInvocation invocation,
        IPushService pushService,
        IPushVerificationSender verificationSender,
        JmapRequestContext context,
        JmapServerOptions options,
        IJmapPushUrlPolicy? urlPolicy,
        IJmapClock? clock,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessSetInvocation(invocation, pushService, verificationSender, context, options, urlPolicy, clock, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessSetInvocation(
        JmapInvocation invocation,
        IPushService pushService,
        IPushVerificationSender verificationSender,
        JmapRequestContext context,
        JmapServerOptions options,
        IJmapPushUrlPolicy? urlPolicy,
        IJmapClock? clock,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.PushSubscriptionSet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsSetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The PushSubscription/set arguments are invalid.", callId);
        }

        PushSubscriptionSetRequest request = Codec.DecodePushSetRequest(invocation.Arguments);
        JmapMethodError? requestError = ValidateSetRequest(request, options);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        ValidateSetDependencies(pushService, verificationSender, context, options);
        if (!IsCredentialActive(context, clock))
        {
            return ErrorInvocation(JmapErrorCodes.Forbidden, JmapMethodErrorKind.Forbidden, "The request credentials cannot modify push subscriptions.", callId);
        }

        PushSubscriptionSetResponse local = new PushSubscriptionSetResponse();
        PushSubscriptionSetRequest backend = PrepareSetRequest(request, local, context, options, urlPolicy, clock);
        Dictionary<string, string> verificationCodes = CaptureVerificationCodes(backend.Create);
        bool hasBackendActions = CountSetActions(backend) > 0;
        PushSubscriptionSetResponse response = new PushSubscriptionSetResponse();
        if (hasBackendActions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            response = pushService.SetStoredPushSubscriptionsAsync(backend, context, cancellationToken).GetAwaiter().GetResult();
            ValidateSetResponse(response, backend);
        }

        MergeSetFailures(response, local);
        RegisterCreatedSubscriptions(response, backend, verificationCodes, verificationSender, context, cancellationToken);
        ApplySuccessfulSecurityChanges(response, backend, context, clock);
        SanitizeCreated(response.Created);
        return SetSuccessInvocation(response, callId);
    }

    internal PushSubscriptionGetResponse MapGetResponse(JmapResponse response, string callId, PushSubscriptionGetRequest request)
    {
        if (request == null)
        {
            throw ValidationFailure("Original PushSubscription/get arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.PushSubscriptionGet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.PushSubscriptionGet);
        PushSubscriptionGetResponse decoded = Codec.DecodePushGetResponse(arguments);
        ValidateClientGetResponse(decoded, request);
        return decoded;
    }

    internal PushSubscriptionSetResponse MapSetResponse(JmapResponse response, string callId, PushSubscriptionSetRequest request)
    {
        if (request == null)
        {
            throw ValidationFailure("Original PushSubscription/set arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.PushSubscriptionSet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.PushSubscriptionSet);
        PushSubscriptionSetResponse decoded = Codec.DecodePushSetResponse(arguments);
        ValidateSetResponse(decoded, request);
        ValidateCreatedPrivacy(decoded.Created);
        return decoded;
    }

    internal JmapInvocation CreateGetInvocation(PushSubscriptionGetRequest request, string callId)
    {
        if (request == null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid PushSubscription/get arguments and call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodePushGetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.PushSubscriptionGet, callId, stream.ToArray());
    }

    internal JmapInvocation CreateSetInvocation(PushSubscriptionSetRequest request, string callId)
    {
        if (request == null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid PushSubscription/set arguments and call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodePushSetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.PushSubscriptionSet, callId, stream.ToArray());
    }

    internal bool CanSendStateChange(string? pushSubscriptionId, JmapRequestContext? context, IJmapClock? clock)
    {
        if (pushSubscriptionId == null)
        {
            return false;
        }

        lock (SecurityStates)
        {
            if (!SecurityStates.ContainsKey(pushSubscriptionId))
            {
                return false;
            }

            return Security.CanReceiveNotification(SecurityStates[pushSubscriptionId], context, clock);
        }
    }

    internal void ValidateInvocation(JmapInvocation invocation, string expectedName)
    {
        if (invocation == null)
        {
            throw ValidationFailure("A push-subscription invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != expectedName)
        {
            throw ValidationFailure("The invocation has the wrong push-subscription method name.", "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("Push-subscription arguments must be a JSON object.", "methodCalls/0/1");
        }

        if (!IsValidCallId(invocation.CallId))
        {
            throw ValidationFailure("A usable push-subscription call identifier is required.", "methodCalls/0/2");
        }
    }

    internal JmapMethodError? ValidateGetRequest(PushSubscriptionGetRequest request, JmapServerOptions options)
    {
        if (request.Ids != null)
        {
            if (request.Ids.Count > options.MaxObjectsInGet)
            {
                return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "The get request contains too many identifiers.");
            }

            foreach (string id in request.Ids)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The ids argument contains an invalid identifier.");
                }
            }
        }

        if (request.Properties != null)
        {
            foreach (string property in request.Properties)
            {
                if (property == null || property.Length == 0 || !Values.IsValidUnicode(property))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The properties argument contains an invalid name.");
                }
            }
        }

        return null;
    }

    internal JmapMethodError? ValidateSetRequest(PushSubscriptionSetRequest request, JmapServerOptions options)
    {
        if (CountSetActions(request) > options.MaxObjectsInSet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "The set request contains too many object actions.");
        }

        if (request.Create != null)
        {
            foreach (string creationId in request.Create.Keys)
            {
                if (!Values.IsCreationId(creationId))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The create map contains an invalid creation id.");
                }
            }
        }

        if (request.Update != null)
        {
            foreach (string id in request.Update.Keys)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The update map contains an invalid id.");
                }
            }
        }

        if (request.Destroy != null)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.Destroy)
            {
                if (!Values.IsId(id) || !ids.Add(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The destroy list contains an invalid or duplicate id.");
                }
            }
        }

        return null;
    }

    internal PushSubscriptionSetRequest PrepareSetRequest(
        PushSubscriptionSetRequest request,
        PushSubscriptionSetResponse local,
        JmapRequestContext context,
        JmapServerOptions options,
        IJmapPushUrlPolicy? urlPolicy,
        IJmapClock? clock)
    {
        PushSubscriptionSetRequest backend = new PushSubscriptionSetRequest();
        CopyUnknown(request.UnknownProperties, backend.UnknownProperties);
        PrepareCreates(request, backend, local, context, options, urlPolicy, clock);
        PrepareUpdates(request, backend, local, context, options, clock);
        if (request.Destroy != null)
        {
            backend.Destroy = new List<string>(request.Destroy);
        }

        return backend;
    }

    internal void PrepareCreates(
        PushSubscriptionSetRequest request,
        PushSubscriptionSetRequest backend,
        PushSubscriptionSetResponse local,
        JmapRequestContext context,
        JmapServerOptions options,
        IJmapPushUrlPolicy? urlPolicy,
        IJmapClock? clock)
    {
        if (request.Create == null)
        {
            return;
        }

        foreach (KeyValuePair<string, PushSubscription> item in request.Create)
        {
            List<string> invalid = Validator.ValidateCreatePolicy(item.Value, context, urlPolicy);
            if (invalid.Count > 0)
            {
                AddNotCreated(local, item.Key, SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties, "The push subscription contains invalid properties.", invalid));
            }
            else
            {
                Validator.ApplyExpiryPolicy(item.Value, context, options, clock);
                item.Value.VerificationCode = Security.GenerateVerificationCode();
                if (backend.Create == null)
                {
                    backend.Create = new Dictionary<string, PushSubscription>();
                }

                backend.Create.Add(item.Key, item.Value);
            }
        }
    }

    internal void PrepareUpdates(
        PushSubscriptionSetRequest request,
        PushSubscriptionSetRequest backend,
        PushSubscriptionSetResponse local,
        JmapRequestContext context,
        JmapServerOptions options,
        IJmapClock? clock)
    {
        if (request.Update == null)
        {
            return;
        }

        HashSet<string> destroyed = new HashSet<string>(StringComparer.Ordinal);
        if (request.Destroy != null)
        {
            foreach (string id in request.Destroy)
            {
                destroyed.Add(id);
            }
        }

        foreach (KeyValuePair<string, JmapPatchObject> item in request.Update)
        {
            JmapPatchObject patch = item.Value == null ? new JmapPatchObject() : item.Value;
            Dictionary<string, JsonElement> patchValues = patch.Values == null ? new Dictionary<string, JsonElement>() : patch.Values;
            List<string> patchConflicts = Patches.FindConflictingPaths(patch);
            bool invalidPatch = item.Value == null || patch.Values == null || patchConflicts.Count > 0;
            if (!invalidPatch)
            {
                foreach (KeyValuePair<string, JsonElement> value in patchValues)
                {
                    if (!Patches.IsPatchPath(value.Key) || value.Value.ValueKind == JsonValueKind.Undefined)
                    {
                        invalidPatch = true;
                    }
                }
            }

            if (destroyed.Contains(item.Key))
            {
                AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.WillDestroy, JmapSetErrorKind.WillDestroy, "The update is skipped because the subscription is destroyed.", null));
            }
            else if (invalidPatch)
            {
                AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.InvalidPatch, JmapSetErrorKind.InvalidPatch, "The update is not a valid PatchObject.", null));
            }
            else
            {
                List<string> invalid = Validator.ValidateWritablePatchValues(patch);
                if (invalid.Count == 0 && IsWrongLocalVerification(item.Key, patch, context, clock))
                {
                    invalid.Add(JmapProperties.VerificationCode);
                }

                if (invalid.Count > 0)
                {
                    AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties, "The push subscription patch contains invalid properties.", invalid));
                }
                else
                {
                    ApplyExpiryToPatch(patch, context, options, clock);
                    if (backend.Update == null)
                    {
                        backend.Update = new Dictionary<string, JmapPatchObject>();
                    }

                    backend.Update.Add(item.Key, patch);
                }
            }
        }
    }

    internal bool IsWrongLocalVerification(string id, JmapPatchObject patch, JmapRequestContext context, IJmapClock? clock)
    {
        if (!patch.Values.ContainsKey(JmapProperties.VerificationCode))
        {
            return false;
        }

        string? supplied = patch.Values[JmapProperties.VerificationCode].GetString();
        lock (SecurityStates)
        {
            if (!SecurityStates.ContainsKey(id))
            {
                return false;
            }

            PushSubscriptionSecurityState state = SecurityStates[id];
            if (state.CredentialId != context.CredentialId)
            {
                return false;
            }

            if (!Security.IsAccessible(state, context, clock))
            {
                return true;
            }

            return !Security.VerifyCode(state, supplied);
        }
    }

    internal void ApplyExpiryToPatch(JmapPatchObject patch, JmapRequestContext context, JmapServerOptions options, IJmapClock? clock)
    {
        if (!patch.Values.ContainsKey(JmapProperties.Expires) || context.HasCredentialExpiresUtc)
        {
            return;
        }

        JsonElement requested = patch.Values[JmapProperties.Expires];
        PushSubscription temporary = new PushSubscription();
        if (requested.ValueKind == JsonValueKind.String)
        {
            temporary.Expires = requested.GetString();
        }

        Validator.ApplyExpiryPolicy(temporary, context, options, clock);
        patch.Values[JmapProperties.Expires] = StringElement(temporary.Expires);
    }

    internal Dictionary<string, string> CaptureVerificationCodes(Dictionary<string, PushSubscription>? creates)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (creates != null)
        {
            foreach (KeyValuePair<string, PushSubscription> item in creates)
            {
                if (item.Value.VerificationCode != null)
                {
                    result.Add(item.Key, item.Value.VerificationCode);
                }
            }
        }

        return result;
    }

    internal void RegisterCreatedSubscriptions(
        PushSubscriptionSetResponse response,
        PushSubscriptionSetRequest request,
        Dictionary<string, string> verificationCodes,
        IPushVerificationSender sender,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        if (response.Created == null || request.Create == null)
        {
            return;
        }

        foreach (KeyValuePair<string, PushSubscription> item in response.Created)
        {
            PushSubscription source = request.Create[item.Key];
            string id = item.Value.Id == null ? "" : item.Value.Id;
            string code = verificationCodes[item.Key];
            source.Id = id;
            PushSubscriptionSecurityState state = Security.Capture(source, context, code);
            lock (SecurityStates)
            {
                SecurityStates.Add(id, state);
            }

            if (PushDelivery != null)
            {
                PushDelivery.RegisterTarget(id, state, context);
            }

            PushVerification verification = new PushVerification();
            verification.PushSubscriptionId = id;
            verification.VerificationCode = code;
            cancellationToken.ThrowIfCancellationRequested();
            sender.SendPushVerificationAsync(source, verification, context, cancellationToken).GetAwaiter().GetResult();
        }
    }

    internal void ApplySuccessfulSecurityChanges(PushSubscriptionSetResponse response, PushSubscriptionSetRequest request, JmapRequestContext context, IJmapClock? clock)
    {
        if (response.Updated != null && request.Update != null)
        {
            foreach (string id in response.Updated.Keys)
            {
                if (request.Update.ContainsKey(id))
                {
                    UpdateSecurityState(id, request.Update[id], context, clock);
                }
            }
        }

        if (response.Destroyed != null)
        {
            foreach (string id in response.Destroyed)
            {
                if (PushDelivery != null)
                {
                    PushDelivery.RemoveTarget(id);
                }

                lock (SecurityStates)
                {
                    if (SecurityStates.ContainsKey(id))
                    {
                        Security.Erase(SecurityStates[id]);
                        SecurityStates.Remove(id);
                    }
                }
            }
        }
    }

    internal void UpdateSecurityState(string id, JmapPatchObject patch, JmapRequestContext context, IJmapClock? clock)
    {
        lock (SecurityStates)
        {
            if (!SecurityStates.ContainsKey(id))
            {
                return;
            }

            PushSubscriptionSecurityState state = SecurityStates[id];
            if (!Security.IsAccessible(state, context, clock) || state.Subscription == null)
            {
                return;
            }

            if (patch.Values.ContainsKey(JmapProperties.VerificationCode))
            {
                Security.ConfirmVerification(state, patch.Values[JmapProperties.VerificationCode].GetString());
            }

            if (patch.Values.ContainsKey(JmapProperties.Expires))
            {
                JsonElement expires = patch.Values[JmapProperties.Expires];
                state.Subscription.Expires = expires.ValueKind == JsonValueKind.Null ? null : expires.GetString();
            }

            if (patch.Values.ContainsKey(JmapProperties.Types))
            {
                state.Subscription.Types = ReadTypes(patch.Values[JmapProperties.Types]);
            }
        }
    }

    internal PushSubscriptionGetResponse NormalizeGetResponse(PushSubscriptionGetResponse? stored, PushSubscriptionGetRequest request)
    {
        if (stored == null || stored.List == null || stored.NotFound == null)
        {
            throw ProtocolFailure("The push backend returned an invalid get response.", "methodResponses/1", default(JsonElement));
        }

        Dictionary<string, PushSubscription> records = new Dictionary<string, PushSubscription>(StringComparer.Ordinal);
        foreach (PushSubscription subscription in stored.List)
        {
            if (subscription == null || !Values.IsId(subscription.Id) || subscription.Id == null || records.ContainsKey(subscription.Id))
            {
                throw ProtocolFailure("The push backend returned an invalid or duplicate subscription id.", "list", default(JsonElement));
            }

            records.Add(subscription.Id, subscription);
        }

        PushSubscriptionGetResponse result = new PushSubscriptionGetResponse();
        CopyUnknown(stored.UnknownProperties, result.UnknownProperties);
        if (request.Ids == null)
        {
            foreach (PushSubscription subscription in stored.List)
            {
                result.List.Add(ProjectSubscription(subscription, request.Properties));
            }
        }
        else
        {
            HashSet<string> handled = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.Ids)
            {
                if (handled.Add(id))
                {
                    if (records.ContainsKey(id))
                    {
                        result.List.Add(ProjectSubscription(records[id], request.Properties));
                    }
                    else
                    {
                        result.NotFound.Add(id);
                    }
                }
            }
        }

        return result;
    }

    internal PushSubscription ProjectSubscription(PushSubscription source, List<string>? properties)
    {
        PushSubscription result = new PushSubscription();
        result.Id = source.Id;
        if (properties == null || properties.Contains(JmapProperties.DeviceClientId))
        {
            result.DeviceClientId = source.DeviceClientId;
        }

        if (properties == null || properties.Contains(JmapProperties.VerificationCode))
        {
            result.VerificationCode = source.VerificationCode;
        }

        if (properties == null || properties.Contains(JmapProperties.Expires))
        {
            result.Expires = source.Expires;
        }

        if (properties == null || properties.Contains(JmapProperties.Types))
        {
            result.Types = source.Types == null ? null : new List<string>(source.Types);
        }

        foreach (KeyValuePair<string, JsonElement> property in source.UnknownProperties)
        {
            if (properties == null || properties.Contains(property.Key))
            {
                result.UnknownProperties.Add(property.Key, property.Value.Clone());
            }
        }

        return result;
    }

    internal void ValidateClientGetResponse(PushSubscriptionGetResponse response, PushSubscriptionGetRequest request)
    {
        if (response == null || response.List == null || response.NotFound == null)
        {
            throw ProtocolFailure("The PushSubscription/get response is invalid.", "methodResponses/1", default(JsonElement));
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> accounted = new HashSet<string>(StringComparer.Ordinal);
        foreach (PushSubscription subscription in response.List)
        {
            if (subscription == null || !Values.IsId(subscription.Id) || subscription.Url != null || subscription.Keys != null || subscription.Id == null || !ids.Add(subscription.Id))
            {
                throw ProtocolFailure("The PushSubscription/get response exposes private data or invalid records.", "list", default(JsonElement));
            }

            accounted.Add(subscription.Id);
        }

        foreach (string id in response.NotFound)
        {
            if (!Values.IsId(id) || !accounted.Add(id))
            {
                throw ProtocolFailure("The PushSubscription/get notFound list is invalid.", "notFound", default(JsonElement));
            }
        }

        if (request.Ids != null)
        {
            HashSet<string> requested = new HashSet<string>(request.Ids, StringComparer.Ordinal);
            foreach (string id in accounted)
            {
                if (!requested.Contains(id))
                {
                    throw ProtocolFailure("The PushSubscription/get response contains an unrequested id.", "methodResponses/1", default(JsonElement));
                }
            }

            if (accounted.Count != requested.Count)
            {
                throw ProtocolFailure("The PushSubscription/get response does not account for every requested id.", "notFound", default(JsonElement));
            }
        }
    }

    internal void ValidateSetResponse(PushSubscriptionSetResponse? response, PushSubscriptionSetRequest request)
    {
        if (response == null)
        {
            throw ProtocolFailure("The push backend returned a null set response.", "methodResponses/1", default(JsonElement));
        }

        ValidateCreatePartitions(response, request.Create);
        ValidateUpdatePartitions(response, request.Update);
        ValidateDestroyPartitions(response, request.Destroy);
    }

    internal void ValidateCreatePartitions(PushSubscriptionSetResponse response, Dictionary<string, PushSubscription>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Created != null)
        {
            foreach (KeyValuePair<string, PushSubscription> item in response.Created)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Value == null || !Values.IsId(item.Value.Id))
                {
                    throw ProtocolFailure("The PushSubscription/set created map is invalid.", "created", default(JsonElement));
                }
            }
        }

        if (response.NotCreated != null)
        {
            ValidateSetErrors(response.NotCreated, expected, seen, "notCreated");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The PushSubscription/set response does not partition every create.", "created", default(JsonElement));
        }
    }

    internal void ValidateUpdatePartitions(PushSubscriptionSetResponse response, Dictionary<string, JmapPatchObject>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Updated != null)
        {
            foreach (KeyValuePair<string, JsonElement> item in response.Updated)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || (item.Value.ValueKind != JsonValueKind.Null && item.Value.ValueKind != JsonValueKind.Object))
                {
                    throw ProtocolFailure("The PushSubscription/set updated map is invalid.", "updated", default(JsonElement));
                }
            }
        }

        if (response.NotUpdated != null)
        {
            ValidateSetErrors(response.NotUpdated, expected, seen, "notUpdated");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The PushSubscription/set response does not partition every update.", "updated", default(JsonElement));
        }
    }

    internal void ValidateDestroyPartitions(PushSubscriptionSetResponse response, List<string>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(requested, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Destroyed != null)
        {
            foreach (string id in response.Destroyed)
            {
                if (!expected.Contains(id) || !seen.Add(id))
                {
                    throw ProtocolFailure("The PushSubscription/set destroyed list is invalid.", "destroyed", default(JsonElement));
                }
            }
        }

        if (response.NotDestroyed != null)
        {
            ValidateSetErrors(response.NotDestroyed, expected, seen, "notDestroyed");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The PushSubscription/set response does not partition every destroy.", "destroyed", default(JsonElement));
        }
    }

    internal void ValidateSetErrors(Dictionary<string, JmapSetError> errors, HashSet<string> expected, HashSet<string> seen, string path)
    {
        foreach (KeyValuePair<string, JmapSetError> item in errors)
        {
            if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Value == null)
            {
                throw ProtocolFailure("A PushSubscription/set error partition is invalid.", path, default(JsonElement));
            }

            JmapSetError normalized = Errors.NormalizeSetError(item.Value);
            item.Value.Kind = normalized.Kind;
        }
    }

    internal void MergeSetFailures(PushSubscriptionSetResponse target, PushSubscriptionSetResponse source)
    {
        if (source.NotCreated != null)
        {
            if (target.NotCreated == null)
            {
                target.NotCreated = new Dictionary<string, JmapSetError>();
            }

            foreach (KeyValuePair<string, JmapSetError> item in source.NotCreated)
            {
                target.NotCreated.Add(item.Key, item.Value);
            }
        }

        if (source.NotUpdated != null)
        {
            if (target.NotUpdated == null)
            {
                target.NotUpdated = new Dictionary<string, JmapSetError>();
            }

            foreach (KeyValuePair<string, JmapSetError> item in source.NotUpdated)
            {
                target.NotUpdated.Add(item.Key, item.Value);
            }
        }
    }

    internal void SanitizeCreated(Dictionary<string, PushSubscription>? created)
    {
        if (created == null)
        {
            return;
        }

        foreach (PushSubscription subscription in created.Values)
        {
            subscription.Url = null;
            subscription.Keys = null;
            subscription.VerificationCode = null;
        }
    }

    internal void ValidateCreatedPrivacy(Dictionary<string, PushSubscription>? created)
    {
        if (created == null)
        {
            return;
        }

        foreach (PushSubscription subscription in created.Values)
        {
            if (subscription.Url != null || subscription.Keys != null || subscription.VerificationCode != null)
            {
                throw ProtocolFailure("The PushSubscription/set response exposes private creation data.", "created", default(JsonElement));
            }
        }
    }

    internal void AddNotCreated(PushSubscriptionSetResponse response, string creationId, JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void AddNotUpdated(PushSubscriptionSetResponse response, string id, JmapSetError error)
    {
        if (response.NotUpdated == null)
        {
            response.NotUpdated = new Dictionary<string, JmapSetError>();
        }

        response.NotUpdated.Add(id, error);
    }

    internal JmapSetError SetError(string type, JmapSetErrorKind kind, string description, List<string>? properties)
    {
        JmapSetError error = new JmapSetError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        error.Properties = properties;
        return error;
    }

    internal JmapMethodError MethodError(string type, JmapMethodErrorKind kind, string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal JmapInvocation GetSuccessInvocation(PushSubscriptionGetResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodePushGetResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.PushSubscriptionGet, callId, stream.ToArray());
    }

    internal JmapInvocation SetSuccessInvocation(PushSubscriptionSetResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodePushSetResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.PushSubscriptionSet, callId, stream.ToArray());
    }

    internal JmapInvocation ErrorInvocation(string? type, JmapMethodErrorKind kind, string? description, string callId)
    {
        JmapMethodError error = Errors.CreateMethodError(type, description, false);
        error.Kind = kind;
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeMethodError(error, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(ResponseMapper.ErrorResponseName, callId, stream.ToArray());
    }

    internal JmapInvocation InvocationFromBytes(string name, string callId, byte[] bytes)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Reader.ReadUtf8Memory(bytes);
        return invocation;
    }

    internal bool IsGetRequestShape(JsonElement arguments)
    {
        JsonElement ids = Codec.Values.GetMember(arguments, JmapProperties.Ids);
        JsonElement properties = Codec.Values.GetMember(arguments, JmapProperties.Properties);
        return IsOptionalStringArray(ids) && IsOptionalStringArray(properties);
    }

    internal bool IsSetRequestShape(JsonElement arguments)
    {
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        JsonElement update = Codec.Values.GetMember(arguments, JmapProperties.Update);
        JsonElement destroy = Codec.Values.GetMember(arguments, JmapProperties.Destroy);
        if (!IsOptionalObject(create) || !IsOptionalObject(update) || !IsOptionalStringArray(destroy))
        {
            return false;
        }

        if (create.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty item in create.EnumerateObject())
            {
                if (!IsPushSubscriptionShape(item.Value))
                {
                    return false;
                }
            }
        }

        if (update.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty item in update.EnumerateObject())
            {
                if (item.Value.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal bool IsPushSubscriptionShape(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement id = Codec.Values.GetMember(value, JmapProperties.Id);
        JsonElement device = Codec.Values.GetMember(value, JmapProperties.DeviceClientId);
        JsonElement url = Codec.Values.GetMember(value, JmapProperties.Url);
        JsonElement verification = Codec.Values.GetMember(value, JmapProperties.VerificationCode);
        JsonElement expires = Codec.Values.GetMember(value, JmapProperties.Expires);
        JsonElement types = Codec.Values.GetMember(value, JmapProperties.Types);
        JsonElement keys = Codec.Values.GetMember(value, JmapProperties.Keys);
        if (!IsOptionalString(id) || !IsOptionalString(device) || !IsOptionalString(url) || !IsOptionalString(verification) || !IsOptionalString(expires) || !IsOptionalStringArray(types))
        {
            return false;
        }

        if (keys.ValueKind == JsonValueKind.Undefined || keys.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (keys.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement p256dh = Codec.Values.GetMember(keys, JmapProperties.P256Dh);
        JsonElement auth = Codec.Values.GetMember(keys, JmapProperties.Auth);
        return p256dh.ValueKind == JsonValueKind.String && auth.ValueKind == JsonValueKind.String;
    }

    internal bool IsOptionalString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.String;
    }

    internal bool IsOptionalObject(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Object;
    }

    internal bool IsOptionalStringArray(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }
        }

        return true;
    }

    internal bool HasPrivateProperty(List<string>? properties)
    {
        return properties != null && (properties.Contains(JmapProperties.Url) || properties.Contains(JmapProperties.Keys));
    }

    internal bool IsCredentialActive(JmapRequestContext? context, IJmapClock? clock)
    {
        if (context == null || context.CredentialId == null || context.CredentialId.Length == 0 || context.CredentialIsRevoked)
        {
            return false;
        }

        DateTimeOffset now = clock == null ? DateTimeOffset.UtcNow : clock.GetCurrentUtcDateTime();
        return !context.HasCredentialExpiresUtc || context.CredentialExpiresUtc > now;
    }

    internal bool IsValidCallId(string? callId)
    {
        return callId != null && callId.Length > 0 && Values.IsValidUnicode(callId);
    }

    internal long CountSetActions(PushSubscriptionSetRequest request)
    {
        long count = 0;
        if (request.Create != null)
        {
            count = count + request.Create.Count;
        }

        if (request.Update != null)
        {
            count = count + request.Update.Count;
        }

        if (request.Destroy != null)
        {
            count = count + request.Destroy.Count;
        }

        return count;
    }

    internal void ValidateDispatchDependencies(IPushService pushService, JmapRequestContext context, JmapServerOptions options)
    {
        if (pushService == null || context == null || options == null)
        {
            throw ProtocolFailure("Push dispatch dependencies are required.", "pushService", default(JsonElement));
        }
    }

    internal void ValidateSetDependencies(IPushService pushService, IPushVerificationSender sender, JmapRequestContext context, JmapServerOptions options)
    {
        if (pushService == null || sender == null || context == null || options == null)
        {
            throw ProtocolFailure("Push set dispatch dependencies are required.", "pushService", default(JsonElement));
        }
    }

    internal void CopyUnknown(Dictionary<string, JsonElement> source, Dictionary<string, JsonElement> target)
    {
        foreach (KeyValuePair<string, JsonElement> item in source)
        {
            target.Add(item.Key, item.Value.Clone());
        }
    }

    internal List<string>? ReadTypes(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        List<string> result = new List<string>();
        foreach (JsonElement item in value.EnumerateArray())
        {
            string? type = item.GetString();
            result.Add(type == null ? "" : type);
        }

        return result;
    }

    internal JsonElement StringElement(string? value)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        writer.WriteStringValue(value);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(message, new List<string> { path }, default(JsonElement));
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
