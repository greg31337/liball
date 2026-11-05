using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal interface IStandardSetExecutor
{
    string? ReadSetState(string accountId);
    StandardSetActionResult ExecuteSetCreate(string accountId, string creationId, JsonElement value);
    StandardSetActionResult ExecuteSetUpdate(string accountId, string id, JmapPatchObject patch);
    StandardSetActionResult ExecuteSetDestroy(string accountId, string id);
}

internal class StandardSetActionResult
{
    internal bool IsSuccess;
    internal JsonElement Data;
    internal JmapSetError? Error;
}

internal class StandardSetResult
{
    internal bool IsSuccess;
    internal JmapSetResponse? Response;
    internal JmapMethodError? Error;
}

internal class StandardSetProcessor
{
    internal int MaximumObjectsInSet;
    internal ValueValidator Values = new ValueValidator();
    internal PatchValidator Patches = new PatchValidator();

    internal StandardSetProcessor(int maximumObjectsInSet)
    {
        if (maximumObjectsInSet < 0)
        {
            throw new ArgumentOutOfRangeException("maximumObjectsInSet");
        }

        MaximumObjectsInSet = maximumObjectsInSet;
    }

    internal StandardSetResult ProcessSet(JmapSetRequest? request, IStandardSetExecutor? executor)
    {
        if (request == null)
        {
            return ErrorResult(MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A set request is required."));
        }

        JmapMethodError? requestError = ValidateRequest(request);
        if (requestError != null)
        {
            return ErrorResult(requestError);
        }

        if (CountActions(request) > MaximumObjectsInSet)
        {
            return ErrorResult(MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "The set request contains too many object actions."));
        }

        if (executor == null)
        {
            throw ProtocolFailure("A set executor is required.", "", default(JsonElement));
        }

        string accountId = request.AccountId == null ? "" : request.AccountId;
        string? oldState = executor.ReadSetState(accountId);
        if (oldState != null && !Values.IsValidUnicode(oldState))
        {
            throw ProtocolFailure("The set executor returned an invalid current state.", "oldState", default(JsonElement));
        }

        if (request.IfInState != null && request.IfInState != oldState)
        {
            return ErrorResult(MethodError(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch, "The ifInState value does not match the current state."));
        }

        JmapSetResponse response = new JmapSetResponse();
        response.AccountId = request.AccountId;
        response.OldState = oldState;
        ProcessCreates(request, executor, accountId, response);
        ProcessUpdates(request, executor, accountId, response);
        ProcessDestroys(request, executor, accountId, response);

        string? newState = executor.ReadSetState(accountId);
        if (newState == null || !Values.IsValidUnicode(newState))
        {
            throw ProtocolFailure("The set executor returned an invalid new state.", "newState", default(JsonElement));
        }

        response.NewState = newState;
        StandardSetResult result = new StandardSetResult();
        result.IsSuccess = true;
        result.Response = response;
        return result;
    }

    internal JmapMethodError? ValidateRequest(JmapSetRequest request)
    {
        if (!Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (request.IfInState != null && !Values.IsValidUnicode(request.IfInState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The ifInState value is not valid Unicode.");
        }

        if (request.Create != null)
        {
            foreach (KeyValuePair<string, JsonElement> item in request.Create)
            {
                if (!Values.IsCreationId(item.Key))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The create map contains an invalid creation id.");
                }
            }
        }

        if (request.Update != null)
        {
            foreach (KeyValuePair<string, JmapPatchObject> item in request.Update)
            {
                if (!Values.IsId(item.Key))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The update map contains an invalid object id.");
                }
            }
        }

        if (request.Destroy != null)
        {
            HashSet<string> destroyIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.Destroy)
            {
                if (!Values.IsId(id) || !destroyIds.Add(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The destroy list contains an invalid or duplicate object id.");
                }
            }
        }

        return null;
    }

    internal long CountActions(JmapSetRequest request)
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

    internal void ProcessCreates(JmapSetRequest request, IStandardSetExecutor executor, string accountId, JmapSetResponse response)
    {
        if (request.Create == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonElement> item in request.Create)
        {
            if (item.Value.ValueKind != JsonValueKind.Object)
            {
                AddCreateFailure(response, item.Key, SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties, "A created record must be a JSON object."));
            }
            else
            {
                StandardSetActionResult action = executor.ExecuteSetCreate(accountId, item.Key, item.Value.Clone());
                ConsumeCreateResult(response, item.Key, action);
            }
        }
    }

    internal void ProcessUpdates(JmapSetRequest request, IStandardSetExecutor executor, string accountId, JmapSetResponse response)
    {
        if (request.Update == null)
        {
            return;
        }

        HashSet<string> destroyIds = DestroyIds(request.Destroy);
        foreach (KeyValuePair<string, JmapPatchObject> item in request.Update)
        {
            if (destroyIds.Contains(item.Key))
            {
                AddUpdateFailure(response, item.Key, SetError(JmapErrorCodes.WillDestroy, JmapSetErrorKind.WillDestroy, "The update was skipped because the record is destroyed by the same request."));
            }
            else if (!IsPatchStructurallyValid(item.Value))
            {
                AddUpdateFailure(response, item.Key, SetError(JmapErrorCodes.InvalidPatch, JmapSetErrorKind.InvalidPatch, "The update contains an invalid PatchObject."));
            }
            else
            {
                StandardSetActionResult action = executor.ExecuteSetUpdate(accountId, item.Key, ClonePatch(item.Value));
                ConsumeUpdateResult(response, item.Key, action);
            }
        }
    }

    internal void ProcessDestroys(JmapSetRequest request, IStandardSetExecutor executor, string accountId, JmapSetResponse response)
    {
        if (request.Destroy == null)
        {
            return;
        }

        foreach (string id in request.Destroy)
        {
            StandardSetActionResult action = executor.ExecuteSetDestroy(accountId, id);
            ConsumeDestroyResult(response, id, action);
        }
    }

    internal bool IsPatchStructurallyValid(JmapPatchObject? patch)
    {
        if (patch == null || patch.Values == null)
        {
            return false;
        }

        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (!Patches.IsPatchPath(item.Key) || item.Value.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }
        }

        return Patches.FindConflictingPaths(patch).Count == 0;
    }

    internal JmapPatchObject ClonePatch(JmapPatchObject patch)
    {
        JmapPatchObject clone = new JmapPatchObject();
        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            clone.Values.Add(item.Key, item.Value.Clone());
        }

        return clone;
    }

    internal HashSet<string> DestroyIds(List<string>? destroy)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        if (destroy != null)
        {
            foreach (string id in destroy)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    internal void ConsumeCreateResult(JmapSetResponse response, string creationId, StandardSetActionResult? action)
    {
        ValidateActionResult(action, "create/" + creationId);
        if (action == null)
        {
            return;
        }

        if (action.IsSuccess)
        {
            if (action.Data.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolFailure("A successful create result must contain an object.", "created/" + creationId, action.Data);
            }

            if (response.Created == null)
            {
                response.Created = new Dictionary<string, JsonElement>();
            }

            response.Created.Add(creationId, action.Data.Clone());
        }
        else if (action.Error != null)
        {
            AddCreateFailure(response, creationId, CloneSetError(action.Error));
        }
    }

    internal void ConsumeUpdateResult(JmapSetResponse response, string id, StandardSetActionResult? action)
    {
        ValidateActionResult(action, "update/" + id);
        if (action == null)
        {
            return;
        }

        if (action.IsSuccess)
        {
            if (action.Data.ValueKind != JsonValueKind.Object && action.Data.ValueKind != JsonValueKind.Null)
            {
                throw ProtocolFailure("A successful update result must contain an object or null.", "updated/" + id, action.Data);
            }

            if (response.Updated == null)
            {
                response.Updated = new Dictionary<string, JsonElement>();
            }

            response.Updated.Add(id, action.Data.Clone());
        }
        else if (action.Error != null)
        {
            AddUpdateFailure(response, id, CloneSetError(action.Error));
        }
    }

    internal void ConsumeDestroyResult(JmapSetResponse response, string id, StandardSetActionResult? action)
    {
        ValidateActionResult(action, "destroy/" + id);
        if (action == null)
        {
            return;
        }

        if (action.IsSuccess)
        {
            if (response.Destroyed == null)
            {
                response.Destroyed = new List<string>();
            }

            response.Destroyed.Add(id);
        }
        else if (action.Error != null)
        {
            AddDestroyFailure(response, id, CloneSetError(action.Error));
        }
    }

    internal void ValidateActionResult(StandardSetActionResult? action, string path)
    {
        if (action == null || (action.IsSuccess && action.Error != null) || (!action.IsSuccess && action.Error == null))
        {
            throw ProtocolFailure("A set executor returned an inconsistent action result.", path, default(JsonElement));
        }

        if (action.Error != null && (action.Error.Type == null || action.Error.Type.Length == 0 || !Values.IsValidUnicode(action.Error.Type)))
        {
            throw ProtocolFailure("A set executor returned an invalid SetError.", path, default(JsonElement));
        }
    }

    internal void AddCreateFailure(JmapSetResponse response, string creationId, JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void AddUpdateFailure(JmapSetResponse response, string id, JmapSetError error)
    {
        if (response.NotUpdated == null)
        {
            response.NotUpdated = new Dictionary<string, JmapSetError>();
        }

        response.NotUpdated.Add(id, error);
    }

    internal void AddDestroyFailure(JmapSetResponse response, string id, JmapSetError error)
    {
        if (response.NotDestroyed == null)
        {
            response.NotDestroyed = new Dictionary<string, JmapSetError>();
        }

        response.NotDestroyed.Add(id, error);
    }

    internal JmapSetError CloneSetError(JmapSetError source)
    {
        JmapSetError clone = new JmapSetError();
        clone.Type = source.Type;
        clone.Kind = source.Kind;
        clone.Description = source.Description;
        clone.ExistingId = source.ExistingId;
        clone.MaxSize = source.MaxSize;
        clone.HasMaxSize = source.HasMaxSize;
        clone.MaxRecipients = source.MaxRecipients;
        clone.HasMaxRecipients = source.HasMaxRecipients;
        if (source.Properties != null)
        {
            clone.Properties = new List<string>(source.Properties);
        }

        if (source.InvalidRecipients != null)
        {
            clone.InvalidRecipients = new List<string>(source.InvalidRecipients);
        }

        if (source.NotFound != null)
        {
            clone.NotFound = new List<string>(source.NotFound);
        }

        foreach (KeyValuePair<string, JsonElement> item in source.UnknownProperties)
        {
            clone.UnknownProperties.Add(item.Key, item.Value.Clone());
        }

        return clone;
    }

    internal JmapSetError SetError(string type, JmapSetErrorKind kind, string description)
    {
        JmapSetError error = new JmapSetError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal StandardSetResult ErrorResult(JmapMethodError error)
    {
        StandardSetResult result = new StandardSetResult();
        result.Error = error;
        return result;
    }

    internal JmapMethodError MethodError(string type, JmapMethodErrorKind kind, string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
