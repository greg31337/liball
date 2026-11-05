using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace jmap;

internal class StandardCopyAccount
{
    internal bool Exists;
    internal bool SupportsMethod;
    internal string? State;
}

internal interface IStandardCopyExecutor
{
    StandardCopyAccount ReadCopyAccount(string accountId);
    StandardSetActionResult ExecuteCopyCreate(string fromAccountId, string accountId, string creationId, string sourceId, JsonElement value);
    StandardSetActionResult ExecuteCopyDestroy(string fromAccountId, string sourceId);
}

internal class StandardCopyResult
{
    internal bool IsSuccess;
    internal JmapCopyResponse? Response;
    internal JmapMethodError? Error;
    internal JmapSetResponse? DestroyResponse;
    internal JmapMethodError? DestroyError;
    internal List<JmapInvocation> MethodResponses = new List<JmapInvocation>();
}

internal class StandardCopyProcessor
{
    internal ValueValidator Values = new ValueValidator();
    internal StandardSetProcessor Sets = new StandardSetProcessor(int.MaxValue);
    internal JmapJsonReader Reader = new JmapJsonReader(int.MaxValue, 1000);
    internal StandardMethodJsonCodec Codec;
    internal ProtocolJsonCodec Errors;

    internal StandardCopyProcessor()
    {
        Codec = new StandardMethodJsonCodec(Reader);
        Errors = new ProtocolJsonCodec(Reader);
    }

    internal StandardCopyResult ProcessCopy(
        JmapCopyRequest? request,
        string? methodName,
        string? callId,
        IStandardCopyExecutor? executor)
    {
        if (request == null)
        {
            return ErrorResult(Sets.MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A copy request is required."));
        }

        JmapMethodError? requestError = ValidateRequest(request, methodName, callId);
        if (requestError != null)
        {
            return ErrorResult(requestError);
        }

        if (executor == null)
        {
            throw ProtocolFailure("A copy executor is required.", "", default(JsonElement));
        }

        string fromAccountId = request.FromAccountId == null ? "" : request.FromAccountId;
        string accountId = request.AccountId == null ? "" : request.AccountId;
        StandardCopyAccount source = RequireAccount(executor.ReadCopyAccount(fromAccountId), "fromAccountId");
        if (!source.Exists)
        {
            return ErrorResult(Sets.MethodError(JmapErrorCodes.FromAccountNotFound, JmapMethodErrorKind.FromAccountNotFound, "The source account was not found."));
        }

        if (!source.SupportsMethod)
        {
            return ErrorResult(Sets.MethodError(JmapErrorCodes.FromAccountNotSupportedByMethod, JmapMethodErrorKind.FromAccountNotSupportedByMethod, "The source account does not support this copy method."));
        }

        StandardCopyAccount target = RequireAccount(executor.ReadCopyAccount(accountId), "accountId");
        if (!target.Exists)
        {
            return ErrorResult(Sets.MethodError(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound, "The destination account was not found."));
        }

        if (!target.SupportsMethod)
        {
            return ErrorResult(Sets.MethodError(JmapErrorCodes.AccountNotSupportedByMethod, JmapMethodErrorKind.AccountNotSupportedByMethod, "The destination account does not support this copy method."));
        }

        if (request.IfFromInState != null && request.IfFromInState != source.State)
        {
            return ErrorResult(Sets.MethodError(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch, "The source ifFromInState value does not match the current state."));
        }

        if (request.IfInState != null && request.IfInState != target.State)
        {
            return ErrorResult(Sets.MethodError(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch, "The destination ifInState value does not match the current state."));
        }

        JmapCopyResponse response = new JmapCopyResponse();
        response.FromAccountId = request.FromAccountId;
        response.AccountId = request.AccountId;
        response.OldState = target.State;
        Dictionary<string, string> existingCopies = new Dictionary<string, string>(StringComparer.Ordinal);
        List<string> copiedSourceIds = new List<string>();
        ProcessCopyCreates(request, executor, fromAccountId, accountId, response, existingCopies, copiedSourceIds);

        StandardCopyAccount finalTarget = RequireAccount(executor.ReadCopyAccount(accountId), "newState");
        if (!finalTarget.Exists || !finalTarget.SupportsMethod || finalTarget.State == null)
        {
            throw ProtocolFailure("The destination account did not provide a valid final state.", "newState", default(JsonElement));
        }

        response.NewState = finalTarget.State;
        StandardCopyResult result = new StandardCopyResult();
        result.IsSuccess = true;
        result.Response = response;
        AddResponseInvocation(result.MethodResponses, methodName, callId, EncodeCopyArguments(response));

        if (request.OnSuccessDestroyOriginal && copiedSourceIds.Count != 0)
        {
            ProcessImplicitDestroy(request, methodName, callId, executor, fromAccountId, copiedSourceIds, result);
        }

        return result;
    }

    internal JmapMethodError? ValidateRequest(JmapCopyRequest request, string? methodName, string? callId)
    {
        if (!Values.IsMethodName(methodName) || methodName == null || !methodName.EndsWith("/copy", StringComparison.Ordinal))
        {
            return Sets.MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A standard copy method name is required.");
        }

        if (callId == null || callId.Length == 0 || !Values.IsValidUnicode(callId))
        {
            return Sets.MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A usable copy call identifier is required.");
        }

        if (!Values.IsId(request.FromAccountId) || !Values.IsId(request.AccountId) || request.FromAccountId == request.AccountId)
        {
            return Sets.MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "Distinct valid source and destination account ids are required.");
        }

        if ((request.IfFromInState != null && !Values.IsValidUnicode(request.IfFromInState)) ||
            (request.IfInState != null && !Values.IsValidUnicode(request.IfInState)) ||
            (request.DestroyFromIfInState != null && !Values.IsValidUnicode(request.DestroyFromIfInState)))
        {
            return Sets.MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A copy state precondition is not valid Unicode.");
        }

        if (request.Create == null)
        {
            return Sets.MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The copy create map is required.");
        }

        foreach (KeyValuePair<string, JsonElement> item in request.Create)
        {
            if (!Values.IsCreationId(item.Key))
            {
                return Sets.MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The copy create map contains an invalid creation id.");
            }
        }

        return null;
    }

    internal StandardCopyAccount RequireAccount(StandardCopyAccount? account, string path)
    {
        if (account == null)
        {
            throw ProtocolFailure("The copy executor did not return account information.", path, default(JsonElement));
        }

        if (account.State != null && !Values.IsValidUnicode(account.State))
        {
            throw ProtocolFailure("The copy executor returned an invalid account state.", path, default(JsonElement));
        }

        return account;
    }

    internal void ProcessCopyCreates(
        JmapCopyRequest request,
        IStandardCopyExecutor executor,
        string fromAccountId,
        string accountId,
        JmapCopyResponse response,
        Dictionary<string, string> existingCopies,
        List<string> copiedSourceIds)
    {
        foreach (KeyValuePair<string, JsonElement> item in request.Create)
        {
            string? sourceId = ReadSourceId(item.Value);
            if (sourceId == null)
            {
                JmapSetError invalid = Sets.SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties, "A copied record requires a valid source id.");
                invalid.Properties = new List<string> { JmapProperties.Id };
                AddCopyFailure(response, item.Key, invalid);
            }
            else if (existingCopies.ContainsKey(sourceId))
            {
                AddCopyFailure(response, item.Key, AlreadyExistsError(existingCopies[sourceId]));
            }
            else
            {
                StandardSetActionResult action = executor.ExecuteCopyCreate(fromAccountId, accountId, item.Key, sourceId, item.Value.Clone());
                ConsumeCopyResult(response, item.Key, sourceId, action, existingCopies, copiedSourceIds);
            }
        }
    }

    internal string? ReadSourceId(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        JsonElement idValue;
        if (!value.TryGetProperty(JmapProperties.Id, out idValue) || idValue.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        string? id = idValue.GetString();
        if (!Values.IsId(id))
        {
            return null;
        }

        return id;
    }

    internal void ConsumeCopyResult(
        JmapCopyResponse response,
        string creationId,
        string sourceId,
        StandardSetActionResult? action,
        Dictionary<string, string> existingCopies,
        List<string> copiedSourceIds)
    {
        Sets.ValidateActionResult(action, "create/" + creationId);
        if (action == null)
        {
            return;
        }

        if (action.IsSuccess)
        {
            string? createdId = ReadSourceId(action.Data);
            if (createdId == null)
            {
                throw ProtocolFailure("A successful copy result must contain a valid id.", "created/" + creationId, action.Data);
            }

            if (response.Created == null)
            {
                response.Created = new Dictionary<string, JsonElement>();
            }

            response.Created.Add(creationId, action.Data.Clone());
            existingCopies.Add(sourceId, createdId);
            copiedSourceIds.Add(sourceId);
        }
        else if (action.Error != null)
        {
            JmapSetError error = Sets.CloneSetError(action.Error);
            AddCopyFailure(response, creationId, error);
            if (error.Kind == JmapSetErrorKind.AlreadyExists)
            {
                if (!Values.IsId(error.ExistingId) || error.ExistingId == null)
                {
                    throw ProtocolFailure("An alreadyExists copy error requires an existingId.", "notCreated/" + creationId + "/existingId", default(JsonElement));
                }

                existingCopies.Add(sourceId, error.ExistingId);
            }
        }
    }

    internal void ProcessImplicitDestroy(
        JmapCopyRequest request,
        string? copyMethodName,
        string? callId,
        IStandardCopyExecutor executor,
        string fromAccountId,
        List<string> sourceIds,
        StandardCopyResult result)
    {
        StandardCopyAccount source = RequireAccount(executor.ReadCopyAccount(fromAccountId), "destroyFromIfInState");
        if (!source.Exists)
        {
            AddImplicitError(
                result,
                callId,
                Sets.MethodError(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound, "The source account was not found for implicit destruction."));
            return;
        }

        if (!source.SupportsMethod)
        {
            AddImplicitError(
                result,
                callId,
                Sets.MethodError(JmapErrorCodes.AccountNotSupportedByMethod, JmapMethodErrorKind.AccountNotSupportedByMethod, "The source account does not support implicit destruction."));
            return;
        }

        if (request.DestroyFromIfInState != null && request.DestroyFromIfInState != source.State)
        {
            JmapMethodError error = Sets.MethodError(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch, "The implicit source destroy state does not match destroyFromIfInState.");
            AddImplicitError(result, callId, error);
            return;
        }

        JmapSetResponse response = new JmapSetResponse();
        response.AccountId = request.FromAccountId;
        response.OldState = source.State;
        foreach (string sourceId in sourceIds)
        {
            StandardSetActionResult action = executor.ExecuteCopyDestroy(fromAccountId, sourceId);
            Sets.ConsumeDestroyResult(response, sourceId, action);
        }

        StandardCopyAccount finalSource = RequireAccount(executor.ReadCopyAccount(fromAccountId), "newState");
        if (!finalSource.Exists || !finalSource.SupportsMethod || finalSource.State == null)
        {
            throw ProtocolFailure("The source account did not provide a valid state after implicit destruction.", "newState", default(JsonElement));
        }

        response.NewState = finalSource.State;
        result.DestroyResponse = response;
        AddResponseInvocation(result.MethodResponses, SetMethodName(copyMethodName), callId, EncodeSetArguments(response));
    }

    internal void AddImplicitError(StandardCopyResult result, string? callId, JmapMethodError error)
    {
        result.DestroyError = error;
        AddResponseInvocation(result.MethodResponses, "error", callId, EncodeErrorArguments(error));
    }

    internal string SetMethodName(string? copyMethodName)
    {
        if (copyMethodName == null)
        {
            return "";
        }

        return copyMethodName.Substring(0, copyMethodName.Length - 5) + "/set";
    }

    internal void AddCopyFailure(JmapCopyResponse response, string creationId, JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal JmapSetError AlreadyExistsError(string existingId)
    {
        JmapSetError error = Sets.SetError(JmapErrorCodes.AlreadyExists, JmapSetErrorKind.AlreadyExists, "The source record already has a copy in the destination account.");
        error.ExistingId = existingId;
        return error;
    }

    internal void AddResponseInvocation(List<JmapInvocation> responses, string? name, string? callId, JsonElement arguments)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = arguments;
        responses.Add(invocation);
    }

    internal JsonElement EncodeCopyArguments(JmapCopyResponse response)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeCopyResponse(response, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal JsonElement EncodeSetArguments(JmapSetResponse response)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSetResponse(response, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal JsonElement EncodeErrorArguments(JmapMethodError error)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Errors.EncodeMethodError(error, writer);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal StandardCopyResult ErrorResult(JmapMethodError error)
    {
        StandardCopyResult result = new StandardCopyResult();
        result.Error = error;
        return result;
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
