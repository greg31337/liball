using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class IdentitySetPreparation
{
    internal IdentitySetRequest Backend = new IdentitySetRequest();
    internal IdentitySetResponse Local = new IdentitySetResponse();
}

internal class IdentityMethod
{
    internal ValueValidator Values = new ValueValidator();
    internal SubmissionValidator IdentityValues = new SubmissionValidator();
    internal EmailSetValidator SetValues = new EmailSetValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal IdentityJsonCodec Codec;
    internal ProtocolJsonCodec Protocol;
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);

    internal IdentityMethod()
    {
        Codec = new IdentityJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessGetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IIdentityService identityService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessGetInvocation(invocation, session, identityService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessGetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IIdentityService identityService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.IdentityGet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsGetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Identity/get arguments are invalid.", callId);
        }

        IdentityGetRequest request = Codec.DecodeIdentityGetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, identityService, context, options);
        JmapMethodError? requestError = ValidateGetRequest(request, options.MaxObjectsInGet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateSubmissionAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        IdentityGetRequest backendRequest = CompleteGetRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        IdentityGetResponse stored = identityService
            .GetStoredIdentitiesAsync(backendRequest, context, cancellationToken).GetAwaiter().GetResult();
        if (request.Ids == null && stored != null && stored.List != null &&
            stored.List.Count > options.MaxObjectsInGet)
        {
            return ErrorInvocation(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "Returning all Identities would exceed the get object limit.", callId);
        }

        IdentityGetResponse response = NormalizeGetResponse(stored, backendRequest);
        return GetSuccessInvocation(response, request.Properties, callId);
    }

    internal Task<JmapInvocation> ProcessChangesInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IIdentityService identityService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessChangesInvocation(invocation, session, identityService, context, options,
                cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessChangesInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IIdentityService identityService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.IdentityChanges);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsChangesRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Identity/changes arguments are invalid.", callId);
        }

        IdentityChangesRequest request = Codec.DecodeIdentityChangesRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, identityService, context, options);
        JmapMethodError? requestError = ValidateChangesRequest(request);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateSubmissionAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        IdentityChangesResponse response = identityService
            .GetStoredIdentityChangesAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateChangesResponse(response, request);
        return ChangesSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessSetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IIdentityService identityService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessSetInvocation(invocation, session, identityService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessSetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IIdentityService identityService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.IdentitySet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsSetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Identity/set arguments are invalid.", callId);
        }

        IdentitySetRequest request = Codec.DecodeIdentitySetRequest(invocation.Arguments);
        ApplyCreateDefaults(invocation.Arguments, request);
        ValidateDispatchDependencies(session, identityService, context, options);
        JmapMethodError? requestError = ValidateSetRequest(request, options.MaxObjectsInSet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateSubmissionAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        string accountId = request.AccountId == null ? "" : request.AccountId;
        if (session.Accounts[accountId].IsReadOnly)
        {
            return ErrorInvocation(JmapErrorCodes.AccountReadOnly, JmapMethodErrorKind.AccountReadOnly,
                "The account is read-only.", callId);
        }

        IdentityGetRequest snapshotRequest = SetSnapshotRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        IdentityGetResponse stored = identityService
            .GetStoredIdentitiesAsync(snapshotRequest, context, cancellationToken).GetAwaiter().GetResult();
        IdentityGetResponse snapshot = NormalizeGetResponse(stored, snapshotRequest);
        if (request.IfInState != null && request.IfInState != snapshot.State)
        {
            return ErrorInvocation(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch,
                "The ifInState value does not match the current Identity state.", callId);
        }

        Dictionary<string, Identity> identities = IndexIdentities(snapshot.List, snapshotRequest.Properties);
        IdentitySetPreparation preparation = PrepareSetRequest(request, invocation.Arguments, identities,
            snapshot.NotFound, snapshot.State);
        IdentitySetResponse response = preparation.Local;
        if (CountSetActions(preparation.Backend) != 0L)
        {
            cancellationToken.ThrowIfCancellationRequested();
            response = identityService.SetStoredIdentitiesAsync(preparation.Backend, context, cancellationToken)
                .GetAwaiter().GetResult();
            ValidateSetResponse(response, preparation.Backend);
            if (response.OldState != snapshot.State)
            {
                throw ProtocolFailure("Identity/set returned an unexpected oldState.", JmapProperties.OldState,
                    default(JsonElement));
            }

            MergeSetFailures(response, preparation.Local);
        }

        ValidateSetResponse(response, request);
        return SetSuccessInvocation(response, callId);
    }

    internal JmapInvocation CreateGetInvocation(IdentityGetRequest request, string callId)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Identity/get arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeIdentityGetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.IdentityGet, callId, stream.ToArray());
    }

    internal JmapInvocation CreateChangesInvocation(IdentityChangesRequest request, string callId)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Identity/changes arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeIdentityChangesRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.IdentityChanges, callId, stream.ToArray());
    }

    internal JmapInvocation CreateSetInvocation(IdentitySetRequest request, string callId)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Identity/set arguments and a call identifier are required.",
                "request");
        }

        ApplyTypedCreateDefaults(request);
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeIdentitySetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.IdentitySet, callId, stream.ToArray());
    }

    internal IdentityGetResponse MapGetResponse(
        JmapResponse response,
        string callId,
        IdentityGetRequest request)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Identity/get arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.IdentityGet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.IdentityGet);
        ValidateGetProjection(arguments, request.Properties);
        IdentityGetResponse decoded = Codec.DecodeIdentityGetResponse(arguments);
        ValidateClientGetResponse(decoded, request);
        return decoded;
    }

    internal IdentityChangesResponse MapChangesResponse(
        JmapResponse response,
        string callId,
        IdentityChangesRequest request)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null)
        {
            throw ValidationFailure("Valid original Identity/changes arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.IdentityChanges);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.IdentityChanges);
        IdentityChangesResponse decoded = Codec.DecodeIdentityChangesResponse(arguments);
        ValidateChangesResponse(decoded, request);
        return decoded;
    }

    internal IdentitySetResponse MapSetResponse(
        JmapResponse response,
        string callId,
        IdentitySetRequest request)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Identity/set arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.IdentitySet);
        JsonElement arguments = Responses.RequireSuccessfulStateArguments(
            result, JmapMethods.IdentitySet, request.AccountId, request.IfInState);
        ValidateCreatedWire(arguments);
        IdentitySetResponse decoded = Codec.DecodeIdentitySetResponse(arguments);
        ValidateSetResponse(decoded, request);
        return decoded;
    }

    internal JmapMethodError? ValidateGetRequest(IdentityGetRequest? request, int maximumObjectsInGet)
    {
        if (request == null || !Values.IsId(request.AccountId) || maximumObjectsInGet < 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId and get object limit are required.");
        }

        if (request.Ids != null)
        {
            if (request.Ids.Count > maximumObjectsInGet)
            {
                return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                    "The Identity/get request contains too many identifiers.");
            }

            foreach (string id in request.Ids)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The ids argument contains an invalid Identity identifier.");
                }
            }
        }

        if (!ArePropertiesValid(request.Properties))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The properties argument contains an unsupported Identity property.");
        }

        return null;
    }

    internal JmapMethodError? ValidateChangesRequest(IdentityChangesRequest? request)
    {
        if (request == null || !Values.IsId(request.AccountId) || request.SinceState == null ||
            !Values.IsValidUnicode(request.SinceState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId and sinceState are required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges ||
            request.MaxChanges != 0L && !request.HasMaxChanges ||
            request.HasMaxChanges && !request.IsMaxChangesNull &&
            (request.MaxChanges <= 0L || !Values.IsUnsignedInt(request.MaxChanges)))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "maxChanges must be null or a positive UnsignedInt with consistent presence flags.");
        }

        return null;
    }

    internal JmapMethodError? ValidateSetRequest(IdentitySetRequest? request, int maximumObjectsInSet)
    {
        if (request == null || !Values.IsId(request.AccountId) || maximumObjectsInSet < 0 ||
            request.IfInState != null && !Values.IsValidUnicode(request.IfInState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Identity/set account, state, or object limit is invalid.");
        }

        if (CountSetActions(request) > maximumObjectsInSet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "The Identity/set request contains too many object actions.");
        }

        if (request.Create != null)
        {
            foreach (string creationId in request.Create.Keys)
            {
                if (!Values.IsCreationId(creationId))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The create map contains an invalid creation identifier.");
                }
            }
        }

        if (request.Update != null)
        {
            foreach (string id in request.Update.Keys)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The update map contains an invalid Identity identifier.");
                }
            }
        }

        if (request.Destroy != null)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.Destroy)
            {
                if (!Values.IsId(id) || !seen.Add(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The destroy list contains an invalid or duplicate Identity identifier.");
                }
            }
        }

        return null;
    }

    internal IdentityGetRequest CompleteGetRequest(IdentityGetRequest request)
    {
        IdentityGetRequest result = new IdentityGetRequest();
        result.AccountId = request.AccountId;
        if (request.Ids != null)
        {
            result.Ids = new List<string>(request.Ids);
        }

        if (request.Properties != null)
        {
            result.Properties = EffectiveProperties(request.Properties);
        }

        CopyUnknown(request.UnknownProperties, result.UnknownProperties);
        return result;
    }

    internal IdentityGetResponse NormalizeGetResponse(IdentityGetResponse? stored, IdentityGetRequest request)
    {
        ValidateGetEnvelope(stored, request);
        if (stored == null)
        {
            throw ProtocolFailure("An Identity/get response is required.", "methodResponses/1",
                default(JsonElement));
        }

        Dictionary<string, Identity> records = IndexIdentities(stored.List, request.Properties);
        ValidateGetPartition(stored, request, records);
        IdentityGetResponse result = new IdentityGetResponse();
        result.AccountId = stored.AccountId;
        result.State = stored.State;
        CopyUnknown(stored.UnknownProperties, result.UnknownProperties);
        if (request.Ids == null)
        {
            result.List.AddRange(stored.List);
            return result;
        }

        HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in request.Ids)
        {
            if (added.Add(id))
            {
                if (records.ContainsKey(id))
                {
                    result.List.Add(records[id]);
                }
                else
                {
                    result.NotFound.Add(id);
                }
            }
        }

        return result;
    }

    internal void ValidateClientGetResponse(IdentityGetResponse? response, IdentityGetRequest request)
    {
        ValidateGetEnvelope(response, request);
        if (response == null)
        {
            throw ProtocolFailure("An Identity/get response is required.", "methodResponses/1",
                default(JsonElement));
        }

        Dictionary<string, Identity> records = IndexIdentities(response.List, request.Properties);
        ValidateGetPartition(response, request, records);
    }

    internal void ValidateGetEnvelope(IdentityGetResponse? response, IdentityGetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.State == null ||
            !Values.IsValidUnicode(response.State) || response.List == null || response.NotFound == null)
        {
            throw ProtocolFailure("The Identity/get response has invalid account, state, or collection metadata.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal Dictionary<string, Identity> IndexIdentities(
        List<Identity> identities,
        List<string>? properties)
    {
        Dictionary<string, Identity> result = new Dictionary<string, Identity>(StringComparer.Ordinal);
        int index = 0;
        foreach (Identity identity in identities)
        {
            ApplyDefaults(identity, properties);
            List<string> invalid = ValidateProjectedIdentity(identity, properties);
            if (identity == null || identity.Id == null || invalid.Count != 0 || result.ContainsKey(identity.Id))
            {
                throw ProtocolFailure("Identity/get returned an invalid or duplicate Identity.",
                    JmapProperties.List + "/" + index, default(JsonElement));
            }

            result.Add(identity.Id, identity);
            index = index + 1;
        }

        return result;
    }

    internal List<string> ValidateProjectedIdentity(Identity? identity, List<string>? properties)
    {
        List<string> invalid = new List<string>();
        if (identity == null || !Values.IsId(identity.Id))
        {
            invalid.Add(JmapProperties.Id);
            return invalid;
        }

        HashSet<string> included = new HashSet<string>(EffectiveProperties(properties), StringComparer.Ordinal);
        if (included.Contains(JmapProperties.Name) &&
            (identity.Name == null || !Values.IsValidUnicode(identity.Name)))
        {
            invalid.Add(JmapProperties.Name);
        }

        if (included.Contains(JmapProperties.Email) &&
            (identity.Email == null || identity.Email.Length == 0 || !Values.IsValidUnicode(identity.Email)))
        {
            invalid.Add(JmapProperties.Email);
        }

        if (included.Contains(JmapProperties.ReplyTo))
        {
            IdentityValues.ValidateAddressList(identity.ReplyTo, JmapProperties.ReplyTo, invalid);
        }

        if (included.Contains(JmapProperties.Bcc))
        {
            IdentityValues.ValidateAddressList(identity.Bcc, JmapProperties.Bcc, invalid);
        }

        if (included.Contains(JmapProperties.TextSignature) &&
            (identity.TextSignature == null || !Values.IsValidUnicode(identity.TextSignature)))
        {
            invalid.Add(JmapProperties.TextSignature);
        }

        if (included.Contains(JmapProperties.HtmlSignature) &&
            (identity.HtmlSignature == null || !Values.IsValidUnicode(identity.HtmlSignature)))
        {
            invalid.Add(JmapProperties.HtmlSignature);
        }

        if (identity.UnknownProperties == null)
        {
            invalid.Add("unknownProperties");
        }

        return UniquePaths(invalid);
    }

    internal void ApplyDefaults(Identity? identity, List<string>? properties)
    {
        if (identity == null)
        {
            return;
        }

        HashSet<string> included = new HashSet<string>(EffectiveProperties(properties), StringComparer.Ordinal);
        if (included.Contains(JmapProperties.Name) && identity.Name == null)
        {
            identity.Name = "";
        }

        if (included.Contains(JmapProperties.TextSignature) && identity.TextSignature == null)
        {
            identity.TextSignature = "";
        }

        if (included.Contains(JmapProperties.HtmlSignature) && identity.HtmlSignature == null)
        {
            identity.HtmlSignature = "";
        }
    }

    internal void ValidateGetPartition(
        IdentityGetResponse response,
        IdentityGetRequest request,
        Dictionary<string, Identity> records)
    {
        HashSet<string> notFound = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in response.NotFound)
        {
            if (!Values.IsId(id) || !notFound.Add(id) || records.ContainsKey(id))
            {
                throw ProtocolFailure("The Identity/get notFound partition is invalid.",
                    JmapProperties.NotFound, default(JsonElement));
            }
        }

        if (request.Ids == null)
        {
            if (notFound.Count != 0)
            {
                throw ProtocolFailure("Identity/get cannot return notFound when all Identities were requested.",
                    JmapProperties.NotFound, default(JsonElement));
            }

            return;
        }

        HashSet<string> requested = new HashSet<string>(request.Ids, StringComparer.Ordinal);
        foreach (string id in records.Keys)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("Identity/get returned an unrequested Identity.", JmapProperties.List,
                    default(JsonElement));
            }
        }

        foreach (string id in notFound)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("Identity/get returned an unrequested notFound identifier.",
                    JmapProperties.NotFound, default(JsonElement));
            }
        }

        if (records.Count + notFound.Count != requested.Count)
        {
            throw ProtocolFailure("Identity/get did not classify every requested identifier.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateChangesResponse(IdentityChangesResponse? response, IdentityChangesRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.OldState != request.SinceState ||
            response.NewState == null || !Values.IsValidUnicode(response.NewState) || response.Created == null ||
            response.Updated == null || response.Destroyed == null)
        {
            throw ProtocolFailure("The Identity/changes response has invalid account, state, or collections.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> classified = new HashSet<string>(StringComparer.Ordinal);
        ValidateChangeIds(response.Created, classified, JmapProperties.Created);
        ValidateChangeIds(response.Updated, classified, JmapProperties.Updated);
        ValidateChangeIds(response.Destroyed, classified, JmapProperties.Destroyed);
        long count = (long)response.Created.Count + response.Updated.Count + response.Destroyed.Count;
        if (request.HasMaxChanges && !request.IsMaxChangesNull && count > request.MaxChanges)
        {
            throw ProtocolFailure("Identity/changes returned more identifiers than maxChanges permits.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateChangeIds(List<string> ids, HashSet<string> classified, string path)
    {
        foreach (string id in ids)
        {
            if (!Values.IsId(id) || !classified.Add(id))
            {
                throw ProtocolFailure("An Identity change identifier is invalid or multiply classified.", path,
                    default(JsonElement));
            }
        }
    }

    internal IdentityGetRequest SetSnapshotRequest(IdentitySetRequest request)
    {
        IdentityGetRequest result = new IdentityGetRequest();
        result.AccountId = request.AccountId;
        result.Ids = new List<string>();
        result.Properties = new List<string> { JmapProperties.Id, JmapProperties.MayDelete };
        HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
        if (request.Update != null)
        {
            foreach (string id in request.Update.Keys)
            {
                if (added.Add(id))
                {
                    result.Ids.Add(id);
                }
            }
        }

        if (request.Destroy != null)
        {
            foreach (string id in request.Destroy)
            {
                if (added.Add(id))
                {
                    result.Ids.Add(id);
                }
            }
        }

        return result;
    }

    internal IdentitySetPreparation PrepareSetRequest(
        IdentitySetRequest request,
        JsonElement arguments,
        Dictionary<string, Identity> identities,
        List<string> missing,
        string? state)
    {
        IdentitySetPreparation result = new IdentitySetPreparation();
        result.Backend.AccountId = request.AccountId;
        result.Backend.IfInState = request.IfInState;
        CopyUnknown(request.UnknownProperties, result.Backend.UnknownProperties);
        result.Local.AccountId = request.AccountId;
        result.Local.OldState = state;
        result.Local.NewState = state;
        HashSet<string> notFound = new HashSet<string>(missing, StringComparer.Ordinal);
        PrepareCreates(request, arguments, result);
        PrepareUpdates(request, result, identities, notFound);
        PrepareDestroys(request, result, identities, notFound);
        return result;
    }

    internal void PrepareCreates(
        IdentitySetRequest request,
        JsonElement arguments,
        IdentitySetPreparation preparation)
    {
        if (request.Create == null)
        {
            return;
        }

        preparation.Backend.Create = new Dictionary<string, Identity>();
        foreach (KeyValuePair<string, Identity> item in request.Create)
        {
            List<string> invalid = IdentityValues.ValidateIdentityCreate(item.Value);
            AddRawCreateInvalidProperties(arguments, item.Key, invalid);
            invalid = UniquePaths(invalid);
            if (invalid.Count != 0)
            {
                AddNotCreated(preparation.Local, item.Key, SetError(JmapErrorCodes.InvalidProperties,
                    JmapSetErrorKind.InvalidProperties, "The Identity has invalid properties.", invalid));
            }
            else
            {
                preparation.Backend.Create.Add(item.Key, item.Value);
            }
        }
    }

    internal void PrepareUpdates(
        IdentitySetRequest request,
        IdentitySetPreparation preparation,
        Dictionary<string, Identity> identities,
        HashSet<string> missing)
    {
        if (request.Update == null)
        {
            return;
        }

        preparation.Backend.Update = new Dictionary<string, JmapPatchObject>();
        HashSet<string> destroyed = request.Destroy == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(request.Destroy, StringComparer.Ordinal);
        foreach (KeyValuePair<string, JmapPatchObject> item in request.Update)
        {
            if (destroyed.Contains(item.Key))
            {
                AddNotUpdated(preparation.Local, item.Key, SetError(JmapErrorCodes.WillDestroy,
                    JmapSetErrorKind.WillDestroy, "The Identity will be destroyed by this request.", null));
            }
            else if (missing.Contains(item.Key) || !identities.ContainsKey(item.Key))
            {
                AddNotUpdated(preparation.Local, item.Key, SetError(JmapErrorCodes.NotFound,
                    JmapSetErrorKind.NotFound, "The Identity was not found.", null));
            }
            else if (!SetValues.IsPatchStructureValid(item.Value))
            {
                AddNotUpdated(preparation.Local, item.Key, SetError(JmapErrorCodes.InvalidPatch,
                    JmapSetErrorKind.InvalidPatch, "The Identity patch is invalid.", null));
            }
            else
            {
                List<string> invalid = ValidateIdentityPatch(item.Value);
                if (invalid.Count != 0)
                {
                    AddNotUpdated(preparation.Local, item.Key, SetError(JmapErrorCodes.InvalidProperties,
                        JmapSetErrorKind.InvalidProperties, "The Identity patch changes invalid properties.",
                        invalid));
                }
                else
                {
                    preparation.Backend.Update.Add(item.Key, item.Value);
                }
            }
        }
    }

    internal void PrepareDestroys(
        IdentitySetRequest request,
        IdentitySetPreparation preparation,
        Dictionary<string, Identity> identities,
        HashSet<string> missing)
    {
        if (request.Destroy == null)
        {
            return;
        }

        preparation.Backend.Destroy = new List<string>();
        foreach (string id in request.Destroy)
        {
            if (missing.Contains(id) || !identities.ContainsKey(id))
            {
                AddNotDestroyed(preparation.Local, id, SetError(JmapErrorCodes.NotFound,
                    JmapSetErrorKind.NotFound, "The Identity was not found.", null));
            }
            else if (!identities[id].MayDelete)
            {
                AddNotDestroyed(preparation.Local, id, SetError(JmapErrorCodes.Forbidden,
                    JmapSetErrorKind.Forbidden, "The Identity may not be deleted.", null));
            }
            else
            {
                preparation.Backend.Destroy.Add(id);
            }
        }
    }

    internal List<string> ValidateIdentityPatch(JmapPatchObject? patch)
    {
        List<string> invalid = IdentityValues.ValidateIdentityWritablePatch(patch);
        if (patch == null)
        {
            return invalid;
        }

        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (item.Key == JmapProperties.Name || item.Key == JmapProperties.TextSignature ||
                item.Key == JmapProperties.HtmlSignature)
            {
                if (item.Value.ValueKind != JsonValueKind.Null && item.Value.ValueKind != JsonValueKind.String)
                {
                    invalid.Add(item.Key);
                }
                else if (item.Value.ValueKind == JsonValueKind.String &&
                    !Values.IsValidUnicode(item.Value.GetString()))
                {
                    invalid.Add(item.Key);
                }
            }
            else if (item.Key == JmapProperties.ReplyTo || item.Key == JmapProperties.Bcc)
            {
                ValidateAddressPatchValue(item.Value, item.Key, invalid);
            }
        }

        return UniquePaths(invalid);
    }

    internal void ValidateAddressPatchValue(JsonElement value, string path, List<string> invalid)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            invalid.Add(path);
            return;
        }

        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                invalid.Add(path + "/" + index);
            }
            else
            {
                JsonElement name = Codec.Values.GetMember(item, JmapProperties.Name);
                JsonElement email = Codec.Values.GetMember(item, JmapProperties.Email);
                if (name.ValueKind != JsonValueKind.String && name.ValueKind != JsonValueKind.Null ||
                    email.ValueKind != JsonValueKind.String || email.GetString() == null ||
                    email.GetString() == "")
                {
                    invalid.Add(path + "/" + index);
                }
                else if (name.ValueKind == JsonValueKind.String && !Values.IsValidUnicode(name.GetString()) ||
                    email.ValueKind == JsonValueKind.String && !Values.IsValidUnicode(email.GetString()))
                {
                    invalid.Add(path + "/" + index);
                }
            }

            index = index + 1;
        }
    }

    internal void ApplyCreateDefaults(JsonElement arguments, IdentitySetRequest request)
    {
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        if (create.ValueKind != JsonValueKind.Object || request.Create == null)
        {
            return;
        }

        foreach (JsonProperty property in create.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object && request.Create.ContainsKey(property.Name))
            {
                Identity identity = request.Create[property.Name];
                if (Codec.Values.GetMember(property.Value, JmapProperties.Name).ValueKind == JsonValueKind.Undefined)
                {
                    identity.Name = "";
                }

                if (Codec.Values.GetMember(property.Value, JmapProperties.TextSignature).ValueKind ==
                    JsonValueKind.Undefined)
                {
                    identity.TextSignature = "";
                }

                if (Codec.Values.GetMember(property.Value, JmapProperties.HtmlSignature).ValueKind ==
                    JsonValueKind.Undefined)
                {
                    identity.HtmlSignature = "";
                }
            }
        }
    }

    internal void ApplyTypedCreateDefaults(IdentitySetRequest request)
    {
        if (request.Create == null)
        {
            return;
        }

        foreach (Identity identity in request.Create.Values)
        {
            if (identity != null)
            {
                if (identity.Name == null)
                {
                    identity.Name = "";
                }

                if (identity.TextSignature == null)
                {
                    identity.TextSignature = "";
                }

                if (identity.HtmlSignature == null)
                {
                    identity.HtmlSignature = "";
                }
            }
        }
    }

    internal void AddRawCreateInvalidProperties(
        JsonElement arguments,
        string creationId,
        List<string> invalid)
    {
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        JsonElement identity = Codec.Values.GetMember(create, creationId);
        if (identity.ValueKind != JsonValueKind.Object)
        {
            invalid.Add("");
            return;
        }

        if (Codec.Values.GetMember(identity, JmapProperties.Id).ValueKind != JsonValueKind.Undefined)
        {
            invalid.Add(JmapProperties.Id);
        }

        if (Codec.Values.GetMember(identity, JmapProperties.MayDelete).ValueKind != JsonValueKind.Undefined)
        {
            invalid.Add(JmapProperties.MayDelete);
        }

        AddInvalidStringShape(identity, JmapProperties.Name, false, invalid);
        AddInvalidStringShape(identity, JmapProperties.Email, true, invalid);
        AddInvalidStringShape(identity, JmapProperties.TextSignature, false, invalid);
        AddInvalidStringShape(identity, JmapProperties.HtmlSignature, false, invalid);
    }

    internal void AddInvalidStringShape(
        JsonElement identity,
        string property,
        bool required,
        List<string> invalid)
    {
        JsonElement value = Codec.Values.GetMember(identity, property);
        if (required && value.ValueKind == JsonValueKind.Undefined ||
            value.ValueKind != JsonValueKind.Undefined && value.ValueKind != JsonValueKind.String)
        {
            invalid.Add(property);
        }
    }

    internal void ValidateSetResponse(IdentitySetResponse? response, IdentitySetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.NewState == null ||
            !Values.IsValidUnicode(response.NewState) ||
            response.OldState != null && !Values.IsValidUnicode(response.OldState) ||
            request.IfInState != null && response.OldState != request.IfInState)
        {
            throw ProtocolFailure("The Identity/set response has invalid account or state metadata.",
                "methodResponses/1", default(JsonElement));
        }

        ValidateCreatePartitions(response, request.Create);
        ValidateUpdatePartitions(response, request.Update);
        ValidateDestroyPartitions(response, request.Destroy);
    }

    internal void ValidateCreatePartitions(
        IdentitySetResponse response,
        Dictionary<string, Identity>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Created != null)
        {
            foreach (KeyValuePair<string, Identity> item in response.Created)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || !IsValidCreatedIdentity(item.Value))
                {
                    throw ProtocolFailure("The Identity/set created map is invalid.", JmapProperties.Created,
                        default(JsonElement));
                }
            }
        }

        if (response.NotCreated != null)
        {
            ValidateSetErrors(response.NotCreated, expected, seen, JmapProperties.NotCreated, "create");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("Identity/set did not classify every create action.", JmapProperties.Created,
                default(JsonElement));
        }
    }

    internal void ValidateUpdatePartitions(
        IdentitySetResponse response,
        Dictionary<string, JmapPatchObject>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Updated != null)
        {
            foreach (KeyValuePair<string, JsonElement> item in response.Updated)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) ||
                    item.Value.ValueKind != JsonValueKind.Null && item.Value.ValueKind != JsonValueKind.Object)
                {
                    throw ProtocolFailure("The Identity/set updated map is invalid.", JmapProperties.Updated,
                        default(JsonElement));
                }
            }
        }

        if (response.NotUpdated != null)
        {
            ValidateSetErrors(response.NotUpdated, expected, seen, JmapProperties.NotUpdated, "update");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("Identity/set did not classify every update action.", JmapProperties.Updated,
                default(JsonElement));
        }
    }

    internal void ValidateDestroyPartitions(IdentitySetResponse response, List<string>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Destroyed != null)
        {
            foreach (string id in response.Destroyed)
            {
                if (!expected.Contains(id) || !seen.Add(id))
                {
                    throw ProtocolFailure("The Identity/set destroyed list is invalid.",
                        JmapProperties.Destroyed, default(JsonElement));
                }
            }
        }

        if (response.NotDestroyed != null)
        {
            ValidateSetErrors(response.NotDestroyed, expected, seen, JmapProperties.NotDestroyed, "destroy");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("Identity/set did not classify every destroy action.",
                JmapProperties.Destroyed, default(JsonElement));
        }
    }

    internal void ValidateSetErrors(
        Dictionary<string, JmapSetError> errors,
        HashSet<string> expected,
        HashSet<string> seen,
        string path,
        string action)
    {
        foreach (KeyValuePair<string, JmapSetError> item in errors)
        {
            if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Value == null)
            {
                throw ProtocolFailure("An Identity/set error partition is invalid.", path,
                    default(JsonElement));
            }

            JmapSetError normalized = Errors.NormalizeSetError(item.Value);
            if (!IsAllowedSetError(normalized.Kind, action))
            {
                throw ProtocolFailure("Identity/set returned an invalid SetError type.",
                    path + "/" + item.Key + "/type", default(JsonElement));
            }

            if (normalized.Kind == JmapSetErrorKind.InvalidProperties && normalized.Properties == null)
            {
                throw ProtocolFailure("An invalidProperties error must identify the invalid properties.",
                    path + "/" + item.Key + "/properties", default(JsonElement));
            }

            item.Value.Kind = normalized.Kind;
        }
    }

    internal bool IsAllowedSetError(JmapSetErrorKind kind, string action)
    {
        if (kind == JmapSetErrorKind.Unknown)
        {
            return true;
        }

        if (action == "create")
        {
            return kind == JmapSetErrorKind.Forbidden || kind == JmapSetErrorKind.OverQuota ||
                kind == JmapSetErrorKind.RateLimit || kind == JmapSetErrorKind.InvalidProperties ||
                kind == JmapSetErrorKind.ForbiddenFrom;
        }

        if (action == "update")
        {
            return kind == JmapSetErrorKind.Forbidden || kind == JmapSetErrorKind.NotFound ||
                kind == JmapSetErrorKind.InvalidPatch || kind == JmapSetErrorKind.WillDestroy ||
                kind == JmapSetErrorKind.InvalidProperties;
        }

        return kind == JmapSetErrorKind.Forbidden || kind == JmapSetErrorKind.NotFound;
    }

    internal bool IsValidCreatedIdentity(Identity? identity)
    {
        if (identity == null || !Values.IsId(identity.Id) || identity.UnknownProperties == null)
        {
            return false;
        }

        if (identity.Name != null && !Values.IsValidUnicode(identity.Name) ||
            identity.Email != null && (identity.Email.Length == 0 || !Values.IsValidUnicode(identity.Email)) ||
            identity.TextSignature != null && !Values.IsValidUnicode(identity.TextSignature) ||
            identity.HtmlSignature != null && !Values.IsValidUnicode(identity.HtmlSignature))
        {
            return false;
        }

        List<string> invalid = new List<string>();
        IdentityValues.ValidateAddressList(identity.ReplyTo, JmapProperties.ReplyTo, invalid);
        IdentityValues.ValidateAddressList(identity.Bcc, JmapProperties.Bcc, invalid);
        return invalid.Count == 0;
    }

    internal void ValidateCreatedWire(JsonElement arguments)
    {
        JsonElement created = Codec.Values.GetMember(arguments, JmapProperties.Created);
        if (created.ValueKind == JsonValueKind.Undefined || created.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (created.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolFailure("The Identity/set created map is invalid.", JmapProperties.Created, created);
        }

        foreach (JsonProperty item in created.EnumerateObject())
        {
            if (item.Value.ValueKind != JsonValueKind.Object ||
                Codec.Values.GetMember(item.Value, JmapProperties.Id).ValueKind != JsonValueKind.String)
            {
                throw ProtocolFailure("A created Identity must contain its id.",
                    JmapProperties.Created + "/" + item.Name, item.Value);
            }
        }
    }

    internal void MergeSetFailures(IdentitySetResponse target, IdentitySetResponse source)
    {
        MergeSetErrorMap(target, source.NotCreated, "create");
        MergeSetErrorMap(target, source.NotUpdated, "update");
        MergeSetErrorMap(target, source.NotDestroyed, "destroy");
    }

    internal void MergeSetErrorMap(
        IdentitySetResponse target,
        Dictionary<string, JmapSetError>? source,
        string action)
    {
        if (source == null)
        {
            return;
        }

        Dictionary<string, JmapSetError>? destination = action == "create" ? target.NotCreated :
            action == "update" ? target.NotUpdated : target.NotDestroyed;
        if (destination == null)
        {
            destination = new Dictionary<string, JmapSetError>();
            if (action == "create")
            {
                target.NotCreated = destination;
            }
            else if (action == "update")
            {
                target.NotUpdated = destination;
            }
            else
            {
                target.NotDestroyed = destination;
            }
        }

        foreach (KeyValuePair<string, JmapSetError> item in source)
        {
            destination.Add(item.Key, item.Value);
        }
    }

    internal void AddNotCreated(IdentitySetResponse response, string creationId, JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void AddNotUpdated(IdentitySetResponse response, string id, JmapSetError error)
    {
        if (response.NotUpdated == null)
        {
            response.NotUpdated = new Dictionary<string, JmapSetError>();
        }

        response.NotUpdated.Add(id, error);
    }

    internal void AddNotDestroyed(IdentitySetResponse response, string id, JmapSetError error)
    {
        if (response.NotDestroyed == null)
        {
            response.NotDestroyed = new Dictionary<string, JmapSetError>();
        }

        response.NotDestroyed.Add(id, error);
    }

    internal JmapSetError SetError(
        string type,
        JmapSetErrorKind kind,
        string description,
        List<string>? properties)
    {
        JmapSetError error = new JmapSetError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        error.Properties = properties;
        return error;
    }

    internal long CountSetActions(IdentitySetRequest request)
    {
        long count = 0L;
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

    internal List<string> EffectiveProperties(List<string>? properties)
    {
        List<string> source = properties == null ? IdentityProperties() : properties;
        List<string> result = new List<string> { JmapProperties.Id };
        foreach (string property in source)
        {
            if (!result.Contains(property))
            {
                result.Add(property);
            }
        }

        return result;
    }

    internal List<string> IdentityProperties()
    {
        return new List<string>
        {
            JmapProperties.Id, JmapProperties.Name, JmapProperties.Email, JmapProperties.ReplyTo,
            JmapProperties.Bcc, JmapProperties.TextSignature, JmapProperties.HtmlSignature,
            JmapProperties.MayDelete
        };
    }

    internal bool ArePropertiesValid(List<string>? properties)
    {
        if (properties == null)
        {
            return true;
        }

        List<string> known = IdentityProperties();
        foreach (string property in properties)
        {
            if (property == null || !known.Contains(property))
            {
                return false;
            }
        }

        return true;
    }

    internal List<string> UniquePaths(List<string> paths)
    {
        List<string> result = new List<string>();
        foreach (string path in paths)
        {
            if (!result.Contains(path))
            {
                result.Add(path);
            }
        }

        return result;
    }

    internal void ValidateGetProjection(JsonElement arguments, List<string>? properties)
    {
        JsonElement list = Codec.Values.GetMember(arguments, JmapProperties.List);
        if (list.ValueKind != JsonValueKind.Array)
        {
            throw ProtocolFailure("The Identity/get result list is invalid.", JmapProperties.List, list);
        }

        HashSet<string> included = new HashSet<string>(EffectiveProperties(properties), StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement identity in list.EnumerateArray())
        {
            if (identity.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolFailure("An Identity/get result item is invalid.",
                    JmapProperties.List + "/" + index, identity);
            }

            foreach (JsonProperty property in identity.EnumerateObject())
            {
                if (IdentityProperties().Contains(property.Name) && !included.Contains(property.Name))
                {
                    throw ProtocolFailure("Identity/get returned an unrequested property.",
                        JmapProperties.List + "/" + index + "/" + property.Name, identity);
                }
            }

            foreach (string property in included)
            {
                if (Codec.Values.GetMember(identity, property).ValueKind == JsonValueKind.Undefined)
                {
                    throw ProtocolFailure("Identity/get omitted a requested property.",
                        JmapProperties.List + "/" + index + "/" + property, identity);
                }
            }

            index = index + 1;
        }
    }

    internal JmapInvocation GetSuccessInvocation(
        IdentityGetResponse response,
        List<string>? properties,
        string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        WriteProjectedGetResponse(response, properties, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.IdentityGet, callId, stream.ToArray());
    }

    internal void WriteProjectedGetResponse(
        IdentityGetResponse response,
        List<string>? properties,
        JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, response.AccountId, "/accountId");
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.State, response.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        writer.WriteArrayStart();
        int index = 0;
        foreach (Identity identity in response.List)
        {
            WriteProjectedIdentity(identity, properties, writer, "/list/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
        writer.WriteMemberName(JmapProperties.NotFound);
        Codec.Values.EncodeStringArray(response.NotFound, writer, "/notFound");
        Codec.Values.WriteUnknownMembers(response.UnknownProperties, Codec.GetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void WriteProjectedIdentity(
        Identity identity,
        List<string>? properties,
        JmapJsonWriter writer,
        string path)
    {
        writer.WriteObjectStart();
        foreach (string property in EffectiveProperties(properties))
        {
            if (property == JmapProperties.Id)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, identity.Id, path + "/id");
            }
            else if (property == JmapProperties.Name)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, identity.Name, path + "/name");
            }
            else if (property == JmapProperties.Email)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, identity.Email, path + "/email");
            }
            else if (property == JmapProperties.ReplyTo)
            {
                WriteAddressListOrNull(property, identity.ReplyTo, writer, path + "/replyTo");
            }
            else if (property == JmapProperties.Bcc)
            {
                WriteAddressListOrNull(property, identity.Bcc, writer, path + "/bcc");
            }
            else if (property == JmapProperties.TextSignature)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, identity.TextSignature,
                    path + "/textSignature");
            }
            else if (property == JmapProperties.HtmlSignature)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, identity.HtmlSignature,
                    path + "/htmlSignature");
            }
            else if (property == JmapProperties.MayDelete)
            {
                writer.WriteBooleanMember(property, identity.MayDelete, true);
            }
        }

        writer.WriteObjectEnd();
    }

    internal void WriteAddressListOrNull(
        string name,
        List<EmailAddress>? value,
        JmapJsonWriter writer,
        string path)
    {
        writer.WriteMemberName(name);
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            Codec.Emails.EncodeAddressList(value, writer, path);
        }
    }

    internal JmapInvocation ChangesSuccessInvocation(IdentityChangesResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeIdentityChangesResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.IdentityChanges, callId, stream.ToArray());
    }

    internal JmapInvocation SetSuccessInvocation(IdentitySetResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeIdentitySetResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.IdentitySet, callId, stream.ToArray());
    }

    internal JmapMethodError? ValidateSubmissionAccount(JmapSession session, string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        if (!session.Accounts.ContainsKey(id))
        {
            return MethodError(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound,
                "The account was not found or is not accessible.");
        }

        JmapAccount account = session.Accounts[id];
        if (account == null || account.AccountCapabilities == null)
        {
            throw ProtocolFailure("The Session contains invalid Identity account data.",
                "session/accounts/" + id, default(JsonElement));
        }

        if (!account.AccountCapabilities.ContainsKey(JmapCapabilities.Submission))
        {
            return MethodError(JmapErrorCodes.AccountNotSupportedByMethod,
                JmapMethodErrorKind.AccountNotSupportedByMethod,
                "The account does not support JMAP Submission.");
        }

        JmapCapability capability = account.AccountCapabilities[JmapCapabilities.Submission];
        if (capability == null || capability.Submission == null)
        {
            throw ProtocolFailure("The account contains invalid Submission capability data.",
                "session/accounts/" + id, default(JsonElement));
        }

        return null;
    }

    internal bool IsGetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ids = Codec.Values.GetMember(arguments, JmapProperties.Ids);
        JsonElement properties = Codec.Values.GetMember(arguments, JmapProperties.Properties);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalStringArray(ids) &&
            IsOptionalStringArray(properties);
    }

    internal bool IsChangesRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement sinceState = Codec.Values.GetMember(arguments, JmapProperties.SinceState);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxChanges);
        return accountId.ValueKind == JsonValueKind.String && sinceState.ValueKind == JsonValueKind.String &&
            (maximum.ValueKind == JsonValueKind.Undefined || maximum.ValueKind == JsonValueKind.Null ||
                IsUnsignedIntegerShape(maximum));
    }

    internal bool IsSetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement state = Codec.Values.GetMember(arguments, JmapProperties.IfInState);
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        JsonElement update = Codec.Values.GetMember(arguments, JmapProperties.Update);
        JsonElement destroy = Codec.Values.GetMember(arguments, JmapProperties.Destroy);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalString(state) &&
            IsOptionalObjectMap(create) && IsOptionalObjectMap(update) && IsOptionalStringArray(destroy);
    }

    internal bool IsOptionalString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.String;
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

    internal bool IsOptionalObjectMap(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsUnsignedIntegerShape(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        string text = value.GetRawText();
        if (text.Length == 0 || text.Length > 16)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void ValidateDispatchDependencies(
        JmapSession session,
        IIdentityService identityService,
        JmapRequestContext context,
        JmapServerOptions options)
    {
        if (session == null || identityService == null || context == null || options == null ||
            session.Accounts == null || session.Capabilities == null)
        {
            throw ProtocolFailure("Identity dispatch dependencies are required.", "identityService",
                default(JsonElement));
        }
    }

    internal void ValidateInvocation(JmapInvocation? invocation, string expectedName)
    {
        if (invocation == null)
        {
            throw ValidationFailure("An Identity invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != expectedName)
        {
            throw ValidationFailure("The invocation has the wrong Identity method name.",
                "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("Identity arguments must be a JSON object.", "methodCalls/0/1");
        }

        if (!IsValidCallId(invocation.CallId))
        {
            throw ValidationFailure("A usable Identity call identifier is required.", "methodCalls/0/2");
        }
    }

    internal JmapInvocation ErrorInvocation(
        string? type,
        JmapMethodErrorKind kind,
        string? description,
        string callId)
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

    internal JmapMethodError MethodError(string type, JmapMethodErrorKind kind, string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal bool IsValidCallId(string? callId)
    {
        return callId != null && callId.Length != 0 && Values.IsValidUnicode(callId);
    }

    internal void CopyUnknown(Dictionary<string, JsonElement> source, Dictionary<string, JsonElement> target)
    {
        foreach (KeyValuePair<string, JsonElement> item in source)
        {
            target.Add(item.Key, item.Value.Clone());
        }
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
