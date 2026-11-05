using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class ThreadMethod
{
    internal ValueValidator Values = new ValueValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal ThreadJsonCodec Codec;
    internal ProtocolJsonCodec Protocol;
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);

    internal ThreadMethod()
    {
        Codec = new ThreadJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessGetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IThreadService threadService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessGetInvocation(invocation, session, threadService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessGetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IThreadService threadService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.ThreadGet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsGetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The Thread/get arguments are invalid.", callId);
        }

        ThreadGetRequest request = Codec.DecodeThreadGetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, threadService, context, options);
        JmapMethodError? requestError = ValidateGetRequest(request, options.MaxObjectsInGet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        ThreadGetRequest backendRequest = CompleteGetRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        ThreadGetResponse stored = threadService.GetStoredThreadsAsync(backendRequest, context, cancellationToken).GetAwaiter().GetResult();
        if (request.Ids == null && stored != null && stored.List != null && stored.List.Count > options.MaxObjectsInGet)
        {
            return ErrorInvocation(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "Returning all Threads would exceed the get object limit.", callId);
        }

        ThreadGetResponse response = NormalizeServerGetResponse(stored, request);
        return GetSuccessInvocation(response, request.Properties, callId);
    }

    internal Task<JmapInvocation> ProcessChangesInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IThreadService threadService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessChangesInvocation(invocation, session, threadService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessChangesInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IThreadService threadService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.ThreadChanges);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsChangesRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The Thread/changes arguments are invalid.", callId);
        }

        ThreadChangesRequest request = Codec.DecodeThreadChangesRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, threadService, context, options);
        JmapMethodError? requestError = ValidateChangesRequest(request);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        ThreadChangesResponse response = threadService.GetStoredThreadChangesAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateChangesResponse(response, request);
        return ChangesSuccessInvocation(response, callId);
    }

    internal JmapInvocation CreateGetInvocation(ThreadGetRequest request, string callId)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Thread/get arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeThreadGetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.ThreadGet, callId, stream.ToArray());
    }

    internal JmapInvocation CreateChangesInvocation(ThreadChangesRequest request, string callId)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Thread/changes arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeThreadChangesRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.ThreadChanges, callId, stream.ToArray());
    }

    internal ThreadGetResponse MapGetResponse(JmapResponse response, string callId, ThreadGetRequest request)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Thread/get arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.ThreadGet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.ThreadGet);
        ValidateGetProjection(arguments, request.Properties);
        ThreadGetResponse decoded = Codec.DecodeProjectedThreadGetResponse(arguments, request.Properties);
        ValidateClientGetResponse(decoded, request);
        return decoded;
    }

    internal ThreadChangesResponse MapChangesResponse(JmapResponse response, string callId, ThreadChangesRequest request)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null)
        {
            throw ValidationFailure("Valid original Thread/changes arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.ThreadChanges);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.ThreadChanges);
        ThreadChangesResponse decoded = Codec.DecodeThreadChangesResponse(arguments);
        ValidateChangesResponse(decoded, request);
        return decoded;
    }

    internal JmapMethodError? ValidateGetRequest(ThreadGetRequest? request, int maximumObjectsInGet)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (maximumObjectsInGet < 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid get object limit is required.");
        }

        if (request.Ids != null)
        {
            if (request.Ids.Count > maximumObjectsInGet)
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
            HashSet<string> supported = ThreadPropertySet();
            foreach (string property in request.Properties)
            {
                if (property == null || !supported.Contains(property))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The properties argument contains an unsupported property.");
                }
            }
        }

        return null;
    }

    internal JmapMethodError? ValidateChangesRequest(ThreadChangesRequest? request)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (request.SinceState == null || !Values.IsValidUnicode(request.SinceState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid sinceState is required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The maxChanges presence flags are inconsistent.");
        }

        if (request.HasMaxChanges && !request.IsMaxChangesNull &&
            (request.MaxChanges <= 0L || !Values.IsUnsignedInt(request.MaxChanges)))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "maxChanges must be null or a positive UnsignedInt.");
        }

        return null;
    }

    internal ThreadGetRequest CompleteGetRequest(ThreadGetRequest request)
    {
        ThreadGetRequest result = new ThreadGetRequest();
        result.AccountId = request.AccountId;
        if (request.Ids != null)
        {
            result.Ids = new List<string>(request.Ids);
        }

        CopyUnknown(request.UnknownProperties, result.UnknownProperties);
        return result;
    }

    internal ThreadGetResponse NormalizeServerGetResponse(ThreadGetResponse? stored, ThreadGetRequest request)
    {
        ValidateGetEnvelope(stored, request);
        if (stored == null)
        {
            throw ProtocolFailure("A Thread/get response is required.", "methodResponses/1", default(JsonElement));
        }

        Dictionary<string, Thread> records = IndexCompleteThreads(stored.List);
        ValidateGetPartition(stored, request, records);
        ThreadGetResponse result = new ThreadGetResponse();
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

    internal void ValidateClientGetResponse(ThreadGetResponse? response, ThreadGetRequest request)
    {
        ValidateGetEnvelope(response, request);
        if (response == null)
        {
            throw ProtocolFailure("A Thread/get response is required.", "methodResponses/1", default(JsonElement));
        }

        Dictionary<string, Thread> records = IndexProjectedThreads(response.List, request.Properties);
        ValidateGetPartition(response, request, records);
    }

    internal void ValidateGetEnvelope(ThreadGetResponse? response, ThreadGetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.State == null ||
            !Values.IsValidUnicode(response.State) || response.List == null || response.NotFound == null)
        {
            throw ProtocolFailure("The Thread/get response has invalid account, state, or collection metadata.", "methodResponses/1", default(JsonElement));
        }
    }

    internal Dictionary<string, Thread> IndexCompleteThreads(List<Thread> threads)
    {
        Dictionary<string, Thread> records = new Dictionary<string, Thread>(StringComparer.Ordinal);
        HashSet<string> memberships = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (Thread thread in threads)
        {
            if (!IsValidCompleteThread(thread, memberships) || thread.Id == null || records.ContainsKey(thread.Id))
            {
                throw ProtocolFailure("The Thread/get response contains an invalid Thread or membership.", "list/" + index, default(JsonElement));
            }

            records.Add(thread.Id, thread);
            index = index + 1;
        }

        return records;
    }

    internal Dictionary<string, Thread> IndexProjectedThreads(List<Thread> threads, List<string>? properties)
    {
        Dictionary<string, Thread> records = new Dictionary<string, Thread>(StringComparer.Ordinal);
        HashSet<string> memberships = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (Thread thread in threads)
        {
            if (!IsValidProjectedThread(thread, properties, memberships) || thread.Id == null || records.ContainsKey(thread.Id))
            {
                throw ProtocolFailure("The Thread/get response contains an invalid projected Thread.", "list/" + index, default(JsonElement));
            }

            records.Add(thread.Id, thread);
            index = index + 1;
        }

        return records;
    }

    internal bool IsValidCompleteThread(Thread? thread, HashSet<string> memberships)
    {
        if (thread == null || !Values.IsId(thread.Id) || thread.EmailIds == null || thread.EmailIds.Count == 0)
        {
            return false;
        }

        foreach (string emailId in thread.EmailIds)
        {
            if (!Values.IsId(emailId) || !memberships.Add(emailId))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsValidProjectedThread(Thread? thread, List<string>? properties, HashSet<string> memberships)
    {
        if (thread == null || !Values.IsId(thread.Id))
        {
            return false;
        }

        if (properties == null || properties.Contains(JmapProperties.EmailIds))
        {
            return IsValidCompleteThread(thread, memberships);
        }

        return true;
    }

    internal void ValidateGetPartition(
        ThreadGetResponse response,
        ThreadGetRequest request,
        Dictionary<string, Thread> records)
    {
        HashSet<string> notFound = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in response.NotFound)
        {
            if (!Values.IsId(id) || !notFound.Add(id) || records.ContainsKey(id))
            {
                throw ProtocolFailure("The Thread/get notFound partition is invalid.", "notFound", default(JsonElement));
            }
        }

        if (request.Ids == null)
        {
            if (notFound.Count != 0)
            {
                throw ProtocolFailure("Thread/get cannot return notFound identifiers when all Threads were requested.", "notFound", default(JsonElement));
            }

            return;
        }

        HashSet<string> requested = new HashSet<string>(request.Ids, StringComparer.Ordinal);
        foreach (string id in records.Keys)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("Thread/get returned an unrequested Thread.", "list", default(JsonElement));
            }
        }

        foreach (string id in notFound)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("Thread/get returned an unrequested notFound identifier.", "notFound", default(JsonElement));
            }
        }

        if (records.Count + notFound.Count != requested.Count)
        {
            throw ProtocolFailure("Thread/get did not classify every requested identifier.", "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateChangesResponse(ThreadChangesResponse? response, ThreadChangesRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.OldState != request.SinceState ||
            response.NewState == null || !Values.IsValidUnicode(response.NewState) || response.Created == null ||
            response.Updated == null || response.Destroyed == null)
        {
            throw ProtocolFailure("The Thread/changes response has invalid account, state, or collection metadata.", "methodResponses/1", default(JsonElement));
        }

        HashSet<string> classified = new HashSet<string>(StringComparer.Ordinal);
        ValidateChangeIds(response.Created, classified, "created");
        ValidateChangeIds(response.Updated, classified, "updated");
        ValidateChangeIds(response.Destroyed, classified, "destroyed");
        long changeCount = response.Created.Count + response.Updated.Count + response.Destroyed.Count;
        if (request.HasMaxChanges && !request.IsMaxChangesNull && changeCount > request.MaxChanges)
        {
            throw ProtocolFailure("Thread/changes returned more identifiers than maxChanges permits.", "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateChangeIds(List<string> ids, HashSet<string> classified, string path)
    {
        foreach (string id in ids)
        {
            if (!Values.IsId(id) || !classified.Add(id))
            {
                throw ProtocolFailure("A Thread change identifier is invalid or has multiple classifications.", path, default(JsonElement));
            }
        }
    }

    internal void ValidateGetProjection(JsonElement arguments, List<string>? properties)
    {
        JsonElement list = Codec.Values.GetMember(arguments, JmapProperties.List);
        if (list.ValueKind != JsonValueKind.Array)
        {
            throw ProtocolFailure("The Thread/get result list is invalid.", "list", list);
        }

        HashSet<string> included = properties == null ? ThreadPropertySet() : new HashSet<string>(properties, StringComparer.Ordinal);
        included.Add(JmapProperties.Id);
        int index = 0;
        foreach (JsonElement thread in list.EnumerateArray())
        {
            if (thread.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolFailure("A Thread/get result item is invalid.", "list/" + index, thread);
            }

            foreach (JsonProperty property in thread.EnumerateObject())
            {
                if ((property.Name == JmapProperties.Id || property.Name == JmapProperties.EmailIds) && !included.Contains(property.Name))
                {
                    throw ProtocolFailure("Thread/get returned an unrequested property.", "list/" + index + "/" + property.Name, thread);
                }
            }

            foreach (string property in included)
            {
                if (Codec.Values.GetMember(thread, property).ValueKind == JsonValueKind.Undefined)
                {
                    throw ProtocolFailure("Thread/get omitted a requested property.", "list/" + index + "/" + property, thread);
                }
            }

            index = index + 1;
        }
    }

    internal JmapMethodError? ValidateMailAccount(JmapSession session, string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        if (!session.Accounts.ContainsKey(id))
        {
            return MethodError(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound, "The account was not found or is not accessible.");
        }

        JmapAccount account = session.Accounts[id];
        if (account == null)
        {
            throw ProtocolFailure("The Session contains a null Thread account.", "session/accounts/" + id, default(JsonElement));
        }

        if (account.AccountCapabilities == null)
        {
            throw ProtocolFailure("The Session contains invalid account capability data.", "session/accounts/" + id, default(JsonElement));
        }

        if (!account.AccountCapabilities.ContainsKey(JmapCapabilities.Mail))
        {
            return MethodError(JmapErrorCodes.AccountNotSupportedByMethod, JmapMethodErrorKind.AccountNotSupportedByMethod, "The account does not support JMAP Mail.");
        }

        JmapCapability capability = account.AccountCapabilities[JmapCapabilities.Mail];
        if (capability == null || capability.Mail == null)
        {
            throw ProtocolFailure("The account contains invalid Mail capability data.", "session/accounts/" + id, default(JsonElement));
        }

        return null;
    }

    internal void ValidateDispatchDependencies(
        JmapSession session,
        IThreadService threadService,
        JmapRequestContext context,
        JmapServerOptions options)
    {
        if (session == null || threadService == null || context == null || options == null ||
            session.Accounts == null || session.Capabilities == null)
        {
            throw ProtocolFailure("Thread dispatch dependencies are required.", "threadService", default(JsonElement));
        }
    }

    internal void ValidateInvocation(JmapInvocation? invocation, string expectedName)
    {
        if (invocation == null)
        {
            throw ValidationFailure("A Thread invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != expectedName)
        {
            throw ValidationFailure("The invocation has the wrong Thread method name.", "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("Thread arguments must be a JSON object.", "methodCalls/0/1");
        }

        if (!IsValidCallId(invocation.CallId))
        {
            throw ValidationFailure("A usable Thread call identifier is required.", "methodCalls/0/2");
        }
    }

    internal bool IsGetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ids = Codec.Values.GetMember(arguments, JmapProperties.Ids);
        JsonElement properties = Codec.Values.GetMember(arguments, JmapProperties.Properties);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalStringArray(ids) && IsOptionalStringArray(properties);
    }

    internal bool IsChangesRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement sinceState = Codec.Values.GetMember(arguments, JmapProperties.SinceState);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxChanges);
        return accountId.ValueKind == JsonValueKind.String && sinceState.ValueKind == JsonValueKind.String &&
            (maximum.ValueKind == JsonValueKind.Undefined || maximum.ValueKind == JsonValueKind.Null || IsUnsignedIntegerShape(maximum));
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
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal HashSet<string> ThreadPropertySet()
    {
        return new HashSet<string>(new List<string> { JmapProperties.Id, JmapProperties.EmailIds }, StringComparer.Ordinal);
    }

    internal JmapInvocation GetSuccessInvocation(ThreadGetResponse response, List<string>? properties, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeProjectedThreadGetResponse(response, properties, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.ThreadGet, callId, stream.ToArray());
    }

    internal JmapInvocation ChangesSuccessInvocation(ThreadChangesResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeThreadChangesResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.ThreadChanges, callId, stream.ToArray());
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
        return callId != null && callId.Length > 0 && Values.IsValidUnicode(callId);
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
