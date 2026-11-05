using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class VacationResponseSetPreparation
{
    internal VacationResponseSetRequest Backend = new VacationResponseSetRequest();
    internal VacationResponseSetResponse Local = new VacationResponseSetResponse();
}

internal class VacationResponseMethod
{
    internal const string SingletonId = "singleton";
    internal ValueValidator Values = new ValueValidator();
    internal SubmissionValidator VacationValues = new SubmissionValidator();
    internal EmailSetValidator SetValues = new EmailSetValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal VacationResponseJsonCodec Codec;
    internal ProtocolJsonCodec Protocol;
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);

    internal VacationResponseMethod()
    {
        Codec = new VacationResponseJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessGetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IVacationResponseService vacationService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessGetInvocation(invocation, session, vacationService, context, options,
                cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessGetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IVacationResponseService vacationService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.VacationResponseGet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsGetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The VacationResponse/get arguments are invalid.", callId);
        }

        VacationResponseGetRequest request = Codec.DecodeVacationGetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, vacationService, context, options);
        JmapMethodError? requestError = ValidateGetRequest(request, options.MaxObjectsInGet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateVacationAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        VacationResponseGetRequest backendRequest = BackendGetRequest(request.AccountId, request.Properties);
        CopyUnknown(request.UnknownProperties, backendRequest.UnknownProperties);
        cancellationToken.ThrowIfCancellationRequested();
        VacationResponseGetResponse stored = vacationService
            .GetStoredVacationResponseAsync(backendRequest, context, cancellationToken).GetAwaiter().GetResult();
        VacationResponse singleton = ValidateStoredSingleton(stored, backendRequest);
        VacationResponseGetResponse response = ClientGetResponse(stored, singleton, request);
        return GetSuccessInvocation(response, request.Properties, callId);
    }

    internal Task<JmapInvocation> ProcessSetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IVacationResponseService vacationService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessSetInvocation(invocation, session, vacationService, context, options,
                cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessSetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IVacationResponseService vacationService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.VacationResponseSet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsSetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The VacationResponse/set arguments are invalid.", callId);
        }

        VacationResponseSetRequest request = Codec.DecodeVacationSetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, vacationService, context, options);
        JmapMethodError? requestError = ValidateSetRequest(request, options.MaxObjectsInSet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateVacationAccount(session, request.AccountId);
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

        VacationResponseGetRequest snapshotRequest = BackendGetRequest(request.AccountId, null);
        cancellationToken.ThrowIfCancellationRequested();
        VacationResponseGetResponse stored = vacationService
            .GetStoredVacationResponseAsync(snapshotRequest, context, cancellationToken).GetAwaiter().GetResult();
        VacationResponse singleton = ValidateStoredSingleton(stored, snapshotRequest);
        if (request.IfInState != null && request.IfInState != stored.State)
        {
            return ErrorInvocation(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch,
                "The ifInState value does not match the current VacationResponse state.", callId);
        }

        VacationResponseSetPreparation preparation = PrepareSetRequest(request, singleton, stored.State);
        VacationResponseSetResponse response = preparation.Local;
        if (CountSetActions(preparation.Backend) != 0L)
        {
            cancellationToken.ThrowIfCancellationRequested();
            response = vacationService.SetStoredVacationResponseAsync(
                preparation.Backend, context, cancellationToken).GetAwaiter().GetResult();
            ValidateSetResponse(response, preparation.Backend);
            if (response.OldState != stored.State)
            {
                throw ProtocolFailure("VacationResponse/set returned an unexpected oldState.",
                    JmapProperties.OldState, default(JsonElement));
            }

            MergeSetFailures(response, preparation.Local);
        }

        ValidateSetResponse(response, request);
        return SetSuccessInvocation(response, callId);
    }

    internal JmapInvocation CreateGetInvocation(VacationResponseGetRequest request, string callId)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid VacationResponse/get arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeVacationGetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.VacationResponseGet, callId, stream.ToArray());
    }

    internal JmapInvocation CreateSetInvocation(VacationResponseSetRequest request, string callId)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid VacationResponse/set arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeVacationSetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.VacationResponseSet, callId, stream.ToArray());
    }

    internal VacationResponseGetResponse MapGetResponse(
        JmapResponse response,
        string callId,
        VacationResponseGetRequest request)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original VacationResponse/get arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.VacationResponseGet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.VacationResponseGet);
        ValidateGetProjection(arguments, request.Properties);
        VacationResponseGetResponse decoded = Codec.DecodeVacationGetResponse(arguments);
        ValidateClientGetResponse(decoded, request);
        return decoded;
    }

    internal VacationResponseSetResponse MapSetResponse(
        JmapResponse response,
        string callId,
        VacationResponseSetRequest request)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original VacationResponse/set arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.VacationResponseSet);
        JsonElement arguments = Responses.RequireSuccessfulStateArguments(
            result, JmapMethods.VacationResponseSet, request.AccountId, request.IfInState);
        VacationResponseSetResponse decoded = Codec.DecodeVacationSetResponse(arguments);
        ValidateSetResponse(decoded, request);
        return decoded;
    }

    internal JmapMethodError? ValidateGetRequest(
        VacationResponseGetRequest? request,
        int maximumObjectsInGet)
    {
        if (request == null || !Values.IsId(request.AccountId) || maximumObjectsInGet < 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId and get object limit are required.");
        }

        if (request.Ids == null && maximumObjectsInGet == 0)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "Returning the singleton VacationResponse would exceed the get object limit.");
        }

        if (request.Ids != null)
        {
            if (request.Ids.Count > maximumObjectsInGet)
            {
                return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                    "The VacationResponse/get request contains too many identifiers.");
            }

            foreach (string id in request.Ids)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The ids argument contains an invalid VacationResponse identifier.");
                }
            }
        }

        if (!ArePropertiesValid(request.Properties))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The properties argument contains an unsupported VacationResponse property.");
        }

        return null;
    }

    internal JmapMethodError? ValidateSetRequest(
        VacationResponseSetRequest? request,
        int maximumObjectsInSet)
    {
        if (request == null || !Values.IsId(request.AccountId) || maximumObjectsInSet < 0 ||
            request.IfInState != null && !Values.IsValidUnicode(request.IfInState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The VacationResponse/set account, state, or object limit is invalid.");
        }

        if (CountSetActions(request) > maximumObjectsInSet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "The VacationResponse/set request contains too many object actions.");
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
                        "The update map contains an invalid VacationResponse identifier.");
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
                        "The destroy list contains an invalid or duplicate VacationResponse identifier.");
                }
            }
        }

        return null;
    }

    internal VacationResponseGetRequest BackendGetRequest(string? accountId, List<string>? properties)
    {
        VacationResponseGetRequest result = new VacationResponseGetRequest();
        result.AccountId = accountId;
        result.Properties = EffectiveProperties(properties);
        return result;
    }

    internal VacationResponse ValidateStoredSingleton(
        VacationResponseGetResponse? response,
        VacationResponseGetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.State == null ||
            !Values.IsValidUnicode(response.State) || response.List == null || response.NotFound == null ||
            response.List.Count != 1 || response.NotFound.Count != 0 || response.UnknownProperties == null)
        {
            throw ProtocolFailure("The stored VacationResponse singleton result is invalid.",
                "methodResponses/1", default(JsonElement));
        }

        VacationResponse singleton = response.List[0];
        List<string> invalid = ValidateProjectedVacation(singleton, request.Properties);
        if (singleton == null || singleton.Id != SingletonId || invalid.Count != 0)
        {
            throw ProtocolFailure("VacationResponse/get did not return the valid singleton object.",
                JmapProperties.List + "/0", default(JsonElement));
        }

        return singleton;
    }

    internal VacationResponseGetResponse ClientGetResponse(
        VacationResponseGetResponse stored,
        VacationResponse singleton,
        VacationResponseGetRequest request)
    {
        VacationResponseGetResponse result = new VacationResponseGetResponse();
        result.AccountId = stored.AccountId;
        result.State = stored.State;
        CopyUnknown(stored.UnknownProperties, result.UnknownProperties);
        if (request.Ids == null)
        {
            result.List.Add(singleton);
            return result;
        }

        HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in request.Ids)
        {
            if (added.Add(id))
            {
                if (id == SingletonId)
                {
                    result.List.Add(singleton);
                }
                else
                {
                    result.NotFound.Add(id);
                }
            }
        }

        return result;
    }

    internal void ValidateClientGetResponse(
        VacationResponseGetResponse? response,
        VacationResponseGetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.State == null ||
            !Values.IsValidUnicode(response.State) || response.List == null || response.NotFound == null ||
            response.UnknownProperties == null || response.List.Count > 1)
        {
            throw ProtocolFailure("The VacationResponse/get response metadata is invalid.",
                "methodResponses/1", default(JsonElement));
        }

        bool hasSingleton = false;
        foreach (VacationResponse item in response.List)
        {
            if (hasSingleton || item == null || item.Id != SingletonId ||
                ValidateProjectedVacation(item, request.Properties).Count != 0)
            {
                throw ProtocolFailure("VacationResponse/get returned an invalid singleton.",
                    JmapProperties.List, default(JsonElement));
            }

            hasSingleton = true;
        }

        HashSet<string> notFound = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in response.NotFound)
        {
            if (!Values.IsId(id) || id == SingletonId || !notFound.Add(id))
            {
                throw ProtocolFailure("The VacationResponse/get notFound partition is invalid.",
                    JmapProperties.NotFound, default(JsonElement));
            }
        }

        if (request.Ids == null)
        {
            if (!hasSingleton || notFound.Count != 0)
            {
                throw ProtocolFailure("VacationResponse/get must return its singleton.",
                    "methodResponses/1", default(JsonElement));
            }

            return;
        }

        HashSet<string> requested = new HashSet<string>(request.Ids, StringComparer.Ordinal);
        if (hasSingleton != requested.Contains(SingletonId))
        {
            throw ProtocolFailure("VacationResponse/get classified the singleton incorrectly.",
                "methodResponses/1", default(JsonElement));
        }

        foreach (string id in requested)
        {
            if (id != SingletonId && !notFound.Contains(id))
            {
                throw ProtocolFailure("VacationResponse/get did not classify every requested identifier.",
                    "methodResponses/1", default(JsonElement));
            }
        }

        if (notFound.Count + (hasSingleton ? 1 : 0) != requested.Count)
        {
            throw ProtocolFailure("VacationResponse/get returned an unrequested identifier.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal List<string> ValidateProjectedVacation(
        VacationResponse? vacation,
        List<string>? properties)
    {
        List<string> invalid = new List<string>();
        if (vacation == null || vacation.Id != SingletonId)
        {
            invalid.Add(JmapProperties.Id);
            return invalid;
        }

        if (vacation.UnknownProperties == null)
        {
            invalid.Add("unknownProperties");
            return invalid;
        }

        HashSet<string> included = new HashSet<string>(EffectiveProperties(properties), StringComparer.Ordinal);
        VacationResponse candidate = CloneVacation(vacation);
        candidate.Id = null;
        if (!included.Contains(JmapProperties.FromDate))
        {
            candidate.FromDate = null;
        }

        if (!included.Contains(JmapProperties.ToDate))
        {
            candidate.ToDate = null;
        }

        if (!included.Contains(JmapProperties.Subject))
        {
            candidate.Subject = null;
        }

        if (!included.Contains(JmapProperties.TextBody))
        {
            candidate.TextBody = null;
        }

        if (!included.Contains(JmapProperties.HtmlBody))
        {
            candidate.HtmlBody = null;
        }

        List<string> values = VacationValues.ValidateVacationResponse(candidate);
        foreach (string path in values)
        {
            if (included.Contains(path))
            {
                invalid.Add(path);
            }
        }

        return UniquePaths(invalid);
    }

    internal VacationResponseSetPreparation PrepareSetRequest(
        VacationResponseSetRequest request,
        VacationResponse singleton,
        string? state)
    {
        VacationResponseSetPreparation result = new VacationResponseSetPreparation();
        result.Backend.AccountId = request.AccountId;
        result.Backend.IfInState = state;
        CopyUnknown(request.UnknownProperties, result.Backend.UnknownProperties);
        result.Local.AccountId = request.AccountId;
        result.Local.OldState = state;
        result.Local.NewState = state;
        PrepareCreates(request, result);
        PrepareUpdates(request, singleton, result);
        PrepareDestroys(request, result);
        return result;
    }

    internal void PrepareCreates(
        VacationResponseSetRequest request,
        VacationResponseSetPreparation preparation)
    {
        if (request.Create == null)
        {
            return;
        }

        foreach (string creationId in request.Create.Keys)
        {
            AddNotCreated(preparation.Local, creationId, SetError(JmapErrorCodes.Singleton,
                JmapSetErrorKind.Singleton, "The VacationResponse singleton already exists.", null));
        }
    }

    internal void PrepareUpdates(
        VacationResponseSetRequest request,
        VacationResponse singleton,
        VacationResponseSetPreparation preparation)
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
                    JmapSetErrorKind.WillDestroy,
                    "The VacationResponse will be destroyed by this request.", null));
            }
            else if (item.Key != SingletonId)
            {
                AddNotUpdated(preparation.Local, item.Key, SetError(JmapErrorCodes.NotFound,
                    JmapSetErrorKind.NotFound, "The VacationResponse was not found.", null));
            }
            else if (!SetValues.IsPatchStructureValid(item.Value))
            {
                AddNotUpdated(preparation.Local, item.Key, SetError(JmapErrorCodes.InvalidPatch,
                    JmapSetErrorKind.InvalidPatch, "The VacationResponse patch is invalid.", null));
            }
            else
            {
                List<string> invalid = ValidateVacationPatch(item.Value, singleton);
                if (invalid.Count != 0)
                {
                    AddNotUpdated(preparation.Local, item.Key, SetError(JmapErrorCodes.InvalidProperties,
                        JmapSetErrorKind.InvalidProperties,
                        "The VacationResponse patch changes invalid properties.", invalid));
                }
                else
                {
                    preparation.Backend.Update.Add(item.Key, item.Value);
                }
            }
        }
    }

    internal void PrepareDestroys(
        VacationResponseSetRequest request,
        VacationResponseSetPreparation preparation)
    {
        if (request.Destroy == null)
        {
            return;
        }

        foreach (string id in request.Destroy)
        {
            if (id == SingletonId)
            {
                AddNotDestroyed(preparation.Local, id, SetError(JmapErrorCodes.Singleton,
                    JmapSetErrorKind.Singleton, "The VacationResponse singleton cannot be destroyed.", null));
            }
            else
            {
                AddNotDestroyed(preparation.Local, id, SetError(JmapErrorCodes.NotFound,
                    JmapSetErrorKind.NotFound, "The VacationResponse was not found.", null));
            }
        }
    }

    internal List<string> ValidateVacationPatch(JmapPatchObject? patch, VacationResponse current)
    {
        List<string> invalid = VacationValues.ValidateVacationResponseWritablePatch(patch);
        if (patch == null)
        {
            return invalid;
        }

        if (patch.Values.ContainsKey(JmapProperties.Id) &&
            patch.Values[JmapProperties.Id].ValueKind == JsonValueKind.String &&
            patch.Values[JmapProperties.Id].GetString() == SingletonId)
        {
            invalid.Remove(JmapProperties.Id);
        }

        VacationResponse candidate = CloneVacation(current);
        candidate.Id = null;
        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (item.Key == JmapProperties.IsEnabled)
            {
                if (item.Value.ValueKind == JsonValueKind.Null)
                {
                    candidate.IsEnabled = false;
                }
                else if (item.Value.ValueKind == JsonValueKind.True ||
                    item.Value.ValueKind == JsonValueKind.False)
                {
                    candidate.IsEnabled = item.Value.GetBoolean();
                }
                else
                {
                    invalid.Add(item.Key);
                }
            }
            else if (IsOptionalStringProperty(item.Key))
            {
                if (item.Value.ValueKind != JsonValueKind.Null &&
                    item.Value.ValueKind != JsonValueKind.String)
                {
                    invalid.Add(item.Key);
                }
                else
                {
                    ApplyOptionalStringPatch(candidate, item.Key, item.Value);
                }
            }
        }

        foreach (string path in VacationValues.ValidateVacationResponse(candidate))
        {
            invalid.Add(path);
        }

        return UniquePaths(invalid);
    }

    internal bool IsOptionalStringProperty(string property)
    {
        return property == JmapProperties.FromDate || property == JmapProperties.ToDate ||
            property == JmapProperties.Subject || property == JmapProperties.TextBody ||
            property == JmapProperties.HtmlBody;
    }

    internal void ApplyOptionalStringPatch(
        VacationResponse vacation,
        string property,
        JsonElement value)
    {
        string? text = value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        if (property == JmapProperties.FromDate)
        {
            vacation.FromDate = text;
        }
        else if (property == JmapProperties.ToDate)
        {
            vacation.ToDate = text;
        }
        else if (property == JmapProperties.Subject)
        {
            vacation.Subject = text;
        }
        else if (property == JmapProperties.TextBody)
        {
            vacation.TextBody = text;
        }
        else if (property == JmapProperties.HtmlBody)
        {
            vacation.HtmlBody = text;
        }
    }

    internal VacationResponse CloneVacation(VacationResponse source)
    {
        VacationResponse result = new VacationResponse();
        result.Id = source.Id;
        result.IsEnabled = source.IsEnabled;
        result.FromDate = source.FromDate;
        result.ToDate = source.ToDate;
        result.Subject = source.Subject;
        result.TextBody = source.TextBody;
        result.HtmlBody = source.HtmlBody;
        CopyUnknown(source.UnknownProperties, result.UnknownProperties);
        return result;
    }

    internal void ValidateSetResponse(
        VacationResponseSetResponse? response,
        VacationResponseSetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.NewState == null ||
            !Values.IsValidUnicode(response.NewState) ||
            response.OldState != null && !Values.IsValidUnicode(response.OldState) ||
            request.IfInState != null && response.OldState != request.IfInState ||
            response.UnknownProperties == null)
        {
            throw ProtocolFailure("The VacationResponse/set response has invalid account or state metadata.",
                "methodResponses/1", default(JsonElement));
        }

        ValidateCreatePartitions(response, request.Create);
        ValidateUpdatePartitions(response, request.Update);
        ValidateDestroyPartitions(response, request.Destroy);
    }

    internal void ValidateCreatePartitions(
        VacationResponseSetResponse response,
        Dictionary<string, VacationResponse>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Created != null && response.Created.Count != 0)
        {
            throw ProtocolFailure("VacationResponse/set cannot create the singleton.",
                JmapProperties.Created, default(JsonElement));
        }

        if (response.NotCreated != null)
        {
            ValidateSetErrors(response.NotCreated, expected, seen, JmapProperties.NotCreated, "create");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("VacationResponse/set did not classify every create action.",
                JmapProperties.NotCreated, default(JsonElement));
        }
    }

    internal void ValidateUpdatePartitions(
        VacationResponseSetResponse response,
        Dictionary<string, JmapPatchObject>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Updated != null)
        {
            foreach (KeyValuePair<string, JsonElement> item in response.Updated)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Key != SingletonId ||
                    item.Value.ValueKind != JsonValueKind.Null && item.Value.ValueKind != JsonValueKind.Object)
                {
                    throw ProtocolFailure("The VacationResponse/set updated map is invalid.",
                        JmapProperties.Updated, default(JsonElement));
                }
            }
        }

        if (response.NotUpdated != null)
        {
            ValidateSetErrors(response.NotUpdated, expected, seen, JmapProperties.NotUpdated, "update");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("VacationResponse/set did not classify every update action.",
                JmapProperties.Updated, default(JsonElement));
        }
    }

    internal void ValidateDestroyPartitions(
        VacationResponseSetResponse response,
        List<string>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Destroyed != null && response.Destroyed.Count != 0)
        {
            throw ProtocolFailure("VacationResponse/set cannot destroy the singleton.",
                JmapProperties.Destroyed, default(JsonElement));
        }

        if (response.NotDestroyed != null)
        {
            ValidateSetErrors(response.NotDestroyed, expected, seen, JmapProperties.NotDestroyed, "destroy");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("VacationResponse/set did not classify every destroy action.",
                JmapProperties.NotDestroyed, default(JsonElement));
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
                throw ProtocolFailure("A VacationResponse/set error partition is invalid.", path,
                    default(JsonElement));
            }

            JmapSetError normalized = Errors.NormalizeSetError(item.Value);
            if (!IsAllowedSetError(normalized.Kind, action, item.Key))
            {
                throw ProtocolFailure("VacationResponse/set returned an invalid SetError type.",
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

    internal bool IsAllowedSetError(JmapSetErrorKind kind, string action, string id)
    {
        if (kind == JmapSetErrorKind.Unknown)
        {
            return true;
        }

        if (action == "create")
        {
            return kind == JmapSetErrorKind.Singleton;
        }

        if (action == "update")
        {
            if (id != SingletonId)
            {
                return kind == JmapSetErrorKind.NotFound || kind == JmapSetErrorKind.WillDestroy;
            }

            return kind == JmapSetErrorKind.Forbidden || kind == JmapSetErrorKind.InvalidPatch ||
                kind == JmapSetErrorKind.WillDestroy || kind == JmapSetErrorKind.InvalidProperties;
        }

        return id == SingletonId ? kind == JmapSetErrorKind.Singleton :
            kind == JmapSetErrorKind.NotFound;
    }

    internal void MergeSetFailures(
        VacationResponseSetResponse target,
        VacationResponseSetResponse source)
    {
        MergeSetErrorMap(target, source.NotCreated, "create");
        MergeSetErrorMap(target, source.NotUpdated, "update");
        MergeSetErrorMap(target, source.NotDestroyed, "destroy");
    }

    internal void MergeSetErrorMap(
        VacationResponseSetResponse target,
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

    internal void AddNotCreated(
        VacationResponseSetResponse response,
        string creationId,
        JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void AddNotUpdated(
        VacationResponseSetResponse response,
        string id,
        JmapSetError error)
    {
        if (response.NotUpdated == null)
        {
            response.NotUpdated = new Dictionary<string, JmapSetError>();
        }

        response.NotUpdated.Add(id, error);
    }

    internal void AddNotDestroyed(
        VacationResponseSetResponse response,
        string id,
        JmapSetError error)
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

    internal long CountSetActions(VacationResponseSetRequest request)
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
        List<string> source = properties == null ? VacationProperties() : properties;
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

    internal List<string> VacationProperties()
    {
        return new List<string>
        {
            JmapProperties.Id, JmapProperties.IsEnabled, JmapProperties.FromDate,
            JmapProperties.ToDate, JmapProperties.Subject, JmapProperties.TextBody,
            JmapProperties.HtmlBody
        };
    }

    internal bool ArePropertiesValid(List<string>? properties)
    {
        if (properties == null)
        {
            return true;
        }

        List<string> known = VacationProperties();
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
            throw ProtocolFailure("The VacationResponse/get result list is invalid.",
                JmapProperties.List, list);
        }

        HashSet<string> included = new HashSet<string>(EffectiveProperties(properties), StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement vacation in list.EnumerateArray())
        {
            if (vacation.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolFailure("A VacationResponse/get result item is invalid.",
                    JmapProperties.List + "/" + index, vacation);
            }

            foreach (JsonProperty property in vacation.EnumerateObject())
            {
                if (VacationProperties().Contains(property.Name) && !included.Contains(property.Name))
                {
                    throw ProtocolFailure("VacationResponse/get returned an unrequested property.",
                        JmapProperties.List + "/" + index + "/" + property.Name, vacation);
                }
            }

            foreach (string property in included)
            {
                if (Codec.Values.GetMember(vacation, property).ValueKind == JsonValueKind.Undefined)
                {
                    throw ProtocolFailure("VacationResponse/get omitted a requested property.",
                        JmapProperties.List + "/" + index + "/" + property, vacation);
                }
            }

            index = index + 1;
        }
    }

    internal JmapInvocation GetSuccessInvocation(
        VacationResponseGetResponse response,
        List<string>? properties,
        string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        WriteProjectedGetResponse(response, properties, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.VacationResponseGet, callId, stream.ToArray());
    }

    internal void WriteProjectedGetResponse(
        VacationResponseGetResponse response,
        List<string>? properties,
        JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, response.AccountId,
            "/accountId");
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.State, response.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        writer.WriteArrayStart();
        int index = 0;
        foreach (VacationResponse vacation in response.List)
        {
            WriteProjectedVacation(vacation, properties, writer, "/list/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
        writer.WriteMemberName(JmapProperties.NotFound);
        Codec.Values.EncodeStringArray(response.NotFound, writer, "/notFound");
        Codec.Values.WriteUnknownMembers(response.UnknownProperties, Codec.GetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void WriteProjectedVacation(
        VacationResponse vacation,
        List<string>? properties,
        JmapJsonWriter writer,
        string path)
    {
        writer.WriteObjectStart();
        foreach (string property in EffectiveProperties(properties))
        {
            if (property == JmapProperties.Id)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, vacation.Id, path + "/id");
            }
            else if (property == JmapProperties.IsEnabled)
            {
                writer.WriteBooleanMember(property, vacation.IsEnabled, true);
            }
            else if (property == JmapProperties.FromDate)
            {
                writer.WriteStringMember(property, vacation.FromDate, true);
            }
            else if (property == JmapProperties.ToDate)
            {
                writer.WriteStringMember(property, vacation.ToDate, true);
            }
            else if (property == JmapProperties.Subject)
            {
                writer.WriteStringMember(property, vacation.Subject, true);
            }
            else if (property == JmapProperties.TextBody)
            {
                writer.WriteStringMember(property, vacation.TextBody, true);
            }
            else if (property == JmapProperties.HtmlBody)
            {
                writer.WriteStringMember(property, vacation.HtmlBody, true);
            }
        }

        writer.WriteObjectEnd();
    }

    internal JmapInvocation SetSuccessInvocation(VacationResponseSetResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeVacationSetResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.VacationResponseSet, callId, stream.ToArray());
    }

    internal JmapMethodError? ValidateVacationAccount(JmapSession session, string? accountId)
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
            throw ProtocolFailure("The Session contains invalid VacationResponse account data.",
                "session/accounts/" + id, default(JsonElement));
        }

        if (!account.AccountCapabilities.ContainsKey(JmapCapabilities.VacationResponse))
        {
            return MethodError(JmapErrorCodes.AccountNotSupportedByMethod,
                JmapMethodErrorKind.AccountNotSupportedByMethod,
                "The account does not support VacationResponse.");
        }

        JmapCapability capability = account.AccountCapabilities[JmapCapabilities.VacationResponse];
        if (capability == null || capability.VacationResponse == null)
        {
            throw ProtocolFailure("The account contains invalid VacationResponse capability data.",
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

    internal void ValidateDispatchDependencies(
        JmapSession session,
        IVacationResponseService vacationService,
        JmapRequestContext context,
        JmapServerOptions options)
    {
        if (session == null || vacationService == null || context == null || options == null ||
            session.Accounts == null || session.Capabilities == null)
        {
            throw ProtocolFailure("VacationResponse dispatch dependencies are required.",
                "vacationService", default(JsonElement));
        }
    }

    internal void ValidateInvocation(JmapInvocation? invocation, string expectedName)
    {
        if (invocation == null)
        {
            throw ValidationFailure("A VacationResponse invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != expectedName)
        {
            throw ValidationFailure("The invocation has the wrong VacationResponse method name.",
                "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("VacationResponse arguments must be a JSON object.",
                "methodCalls/0/1");
        }

        if (!IsValidCallId(invocation.CallId))
        {
            throw ValidationFailure("A usable VacationResponse call identifier is required.",
                "methodCalls/0/2");
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

    internal void CopyUnknown(
        Dictionary<string, JsonElement> source,
        Dictionary<string, JsonElement> target)
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
