using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class ProtocolJsonCodec
{
    internal JmapJsonReader Reader;
    internal SessionJsonCodec Values;

    internal ProtocolJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Reader = reader;
        Values = new SessionJsonCodec(reader);
    }

    internal JmapRequest DecodeRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapRequest request = new JmapRequest();
        request.Using = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.Using), "/using");
        request.MethodCalls = DecodeInvocationList(Values.GetMember(value, JmapProperties.MethodCalls), "/methodCalls");

        JsonElement createdIds = Values.GetMember(value, JmapProperties.CreatedIds);
        if (createdIds.ValueKind != JsonValueKind.Undefined)
        {
            request.CreatedIds = DecodeCreatedIds(createdIds, "/createdIds");
        }

        Values.CaptureUnknownMembers(
            value,
            request.UnknownProperties,
            new List<string> { JmapProperties.Using, JmapProperties.MethodCalls, JmapProperties.CreatedIds });
        return request;
    }

    internal void EncodeRequest(JmapRequest request, JmapJsonWriter writer)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        writer.WriteObjectStart();
        writer.WriteMemberName(JmapProperties.Using);
        Values.EncodeStringArray(request.Using, writer, "/using");
        writer.WriteMemberName(JmapProperties.MethodCalls);
        EncodeInvocationList(request.MethodCalls, writer, "/methodCalls");
        if (request.CreatedIds != null)
        {
            writer.WriteMemberName(JmapProperties.CreatedIds);
            EncodeCreatedIds(request.CreatedIds, writer, "/createdIds");
        }

        Values.WriteUnknownMembers(
            request.UnknownProperties,
            new List<string> { JmapProperties.Using, JmapProperties.MethodCalls, JmapProperties.CreatedIds },
            writer);
        writer.WriteObjectEnd();
    }

    internal JmapResponse DecodeResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapResponse response = new JmapResponse();
        response.MethodResponses = DecodeInvocationList(Values.GetMember(value, JmapProperties.MethodResponses), "/methodResponses");

        JsonElement createdIds = Values.GetMember(value, JmapProperties.CreatedIds);
        if (createdIds.ValueKind != JsonValueKind.Undefined)
        {
            response.CreatedIds = DecodeCreatedIds(createdIds, "/createdIds");
        }

        response.SessionState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.SessionState), "/sessionState");
        Values.CaptureUnknownMembers(
            value,
            response.UnknownProperties,
            new List<string> { JmapProperties.MethodResponses, JmapProperties.CreatedIds, JmapProperties.SessionState });
        return response;
    }

    internal void EncodeResponse(JmapResponse response, JmapJsonWriter writer)
    {
        if (response == null)
        {
            throw new System.ArgumentNullException("response");
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        writer.WriteObjectStart();
        writer.WriteMemberName(JmapProperties.MethodResponses);
        EncodeInvocationList(response.MethodResponses, writer, "/methodResponses");
        if (response.CreatedIds != null)
        {
            writer.WriteMemberName(JmapProperties.CreatedIds);
            EncodeCreatedIds(response.CreatedIds, writer, "/createdIds");
        }

        Values.WriteRequiredStringMember(writer, JmapProperties.SessionState, response.SessionState, "/sessionState");
        Values.WriteUnknownMembers(
            response.UnknownProperties,
            new List<string> { JmapProperties.MethodResponses, JmapProperties.CreatedIds, JmapProperties.SessionState },
            writer);
        writer.WriteObjectEnd();
    }

    internal JmapInvocation DecodeInvocation(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 3)
        {
            throw new JmapProtocolException("A JMAP Invocation must be an array containing exactly three elements.", path, Values.CloneContext(value));
        }

        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = Values.ReadRequiredString(value[0], path + "/0");

        JsonElement arguments = value[1];
        Values.RequireObject(arguments, path + "/1");
        invocation.Arguments = Reader.CloneUnknownValue(arguments);
        invocation.CallId = Values.ReadRequiredString(value[2], path + "/2");
        return invocation;
    }

    internal void EncodeInvocation(JmapInvocation invocation, JmapJsonWriter writer, string path)
    {
        if (invocation == null)
        {
            throw new JmapValidationException("A JMAP Invocation cannot be null.", new List<string> { path }, default(JsonElement));
        }

        if (invocation.Name == null)
        {
            throw new JmapValidationException("A JMAP Invocation name is required.", new List<string> { path + "/0" }, default(JsonElement));
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw new JmapValidationException("JMAP Invocation arguments must be a JSON object.", new List<string> { path + "/1" }, Values.CloneContext(invocation.Arguments));
        }

        if (invocation.CallId == null)
        {
            throw new JmapValidationException("A JMAP Invocation call ID is required.", new List<string> { path + "/2" }, default(JsonElement));
        }

        writer.WriteArrayStart();
        writer.WriteStringValue(invocation.Name);
        writer.WriteElementValue(invocation.Arguments);
        writer.WriteStringValue(invocation.CallId);
        writer.WriteArrayEnd();
    }

    internal List<JmapInvocation> DecodeInvocationList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A JSON array of JMAP Invocations is required.", path, Values.CloneContext(value));
        }

        List<JmapInvocation> invocations = new List<JmapInvocation>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            invocations.Add(DecodeInvocation(item, path + "/" + index));
            index = index + 1;
        }

        return invocations;
    }

    internal void EncodeInvocationList(List<JmapInvocation> invocations, JmapJsonWriter writer, string path)
    {
        if (invocations == null)
        {
            throw new JmapValidationException("A JMAP Invocation list cannot be null.", new List<string> { path }, default(JsonElement));
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (JmapInvocation invocation in invocations)
        {
            EncodeInvocation(invocation, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, string> DecodeCreatedIds(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Dictionary<string, string> createdIds = new Dictionary<string, string>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            createdIds.Add(property.Name, Values.ReadRequiredString(property.Value, path + "/" + property.Name));
        }

        return createdIds;
    }

    internal void EncodeCreatedIds(Dictionary<string, string> createdIds, JmapJsonWriter writer, string path)
    {
        writer.WriteObjectStart();
        List<string> keys = Values.SortNames(createdIds.Keys);
        foreach (string key in keys)
        {
            string? id = createdIds[key];
            Values.WriteRequiredStringMember(writer, key, id, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal JmapProblem DecodeProblem(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapProblem problem = new JmapProblem();
        problem.Type = ReadOptionalString(Values.GetMember(value, JmapProperties.Type), "/type");
        if (problem.Type != null)
        {
            problem.Kind = ClassifyProblemType(problem.Type);
        }

        problem.Title = ReadOptionalString(Values.GetMember(value, JmapProperties.Title), "/title");
        JsonElement status = Values.GetMember(value, JmapProperties.Status);
        if (status.ValueKind != JsonValueKind.Undefined)
        {
            if (status.ValueKind != JsonValueKind.Number)
            {
                throw new JmapProtocolException("A problem status must be an integer.", "/status", Values.CloneContext(status));
            }

            problem.Status = status.GetInt32();
            problem.HasStatus = true;
        }

        problem.Detail = ReadOptionalString(Values.GetMember(value, JmapProperties.Detail), "/detail");
        problem.Instance = ReadOptionalString(Values.GetMember(value, JmapProperties.Instance), "/instance");
        problem.Limit = ReadOptionalString(Values.GetMember(value, JmapProperties.Limit), "/limit");
        Values.CaptureUnknownMembers(
            value,
            problem.UnknownProperties,
            new List<string>
            {
                JmapProperties.Type,
                JmapProperties.Title,
                JmapProperties.Status,
                JmapProperties.Detail,
                JmapProperties.Instance,
                JmapProperties.Limit
            });
        return problem;
    }

    internal void EncodeProblem(JmapProblem problem, JmapJsonWriter writer)
    {
        if (problem == null)
        {
            throw new System.ArgumentNullException("problem");
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Type, problem.Type, false);
        writer.WriteStringMember(JmapProperties.Title, problem.Title, false);
        if (problem.HasStatus)
        {
            writer.WriteIntegerMember(JmapProperties.Status, problem.Status, true);
        }

        writer.WriteStringMember(JmapProperties.Detail, problem.Detail, false);
        writer.WriteStringMember(JmapProperties.Instance, problem.Instance, false);
        writer.WriteStringMember(JmapProperties.Limit, problem.Limit, false);
        Values.WriteUnknownMembers(
            problem.UnknownProperties,
            new List<string>
            {
                JmapProperties.Type,
                JmapProperties.Title,
                JmapProperties.Status,
                JmapProperties.Detail,
                JmapProperties.Instance,
                JmapProperties.Limit
            },
            writer);
        writer.WriteObjectEnd();
    }

    internal JmapMethodError DecodeMethodError(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapMethodError error = new JmapMethodError();
        error.Type = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Type), "/type");
        error.Kind = ClassifyMethodErrorType(error.Type);
        error.Description = ReadOptionalString(Values.GetMember(value, JmapProperties.Description), "/description");
        Values.CaptureUnknownMembers(
            value,
            error.UnknownProperties,
            new List<string> { JmapProperties.Type, JmapProperties.Description });
        return error;
    }

    internal void EncodeMethodError(JmapMethodError error, JmapJsonWriter writer)
    {
        if (error == null)
        {
            throw new System.ArgumentNullException("error");
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Type, error.Type, "/type");
        writer.WriteStringMember(JmapProperties.Description, error.Description, false);
        Values.WriteUnknownMembers(
            error.UnknownProperties,
            new List<string> { JmapProperties.Type, JmapProperties.Description },
            writer);
        writer.WriteObjectEnd();
    }

    internal JmapSetError DecodeSetError(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapSetError error = new JmapSetError();
        error.Type = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Type), "/type");
        error.Kind = ClassifySetErrorType(error.Type);
        error.Description = ReadOptionalString(Values.GetMember(value, JmapProperties.Description), "/description");
        error.Properties = ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Properties), "/properties");
        error.ExistingId = ReadOptionalString(Values.GetMember(value, JmapProperties.ExistingId), "/existingId");

        JsonElement maxSize = Values.GetMember(value, JmapProperties.MaxSize);
        if (maxSize.ValueKind != JsonValueKind.Undefined)
        {
            error.MaxSize = Values.ReadRequiredUnsignedInteger(maxSize, "/maxSize");
            error.HasMaxSize = true;
        }

        JsonElement maxRecipients = Values.GetMember(value, JmapProperties.MaxRecipients);
        if (maxRecipients.ValueKind != JsonValueKind.Undefined)
        {
            error.MaxRecipients = Values.ReadRequiredUnsignedInteger(maxRecipients, "/maxRecipients");
            error.HasMaxRecipients = true;
        }

        error.InvalidRecipients = ReadOptionalStringArray(Values.GetMember(value, JmapProperties.InvalidRecipients), "/invalidRecipients");
        error.NotFound = ReadOptionalStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(
            value,
            error.UnknownProperties,
            new List<string>
            {
                JmapProperties.Type,
                JmapProperties.Description,
                JmapProperties.Properties,
                JmapProperties.ExistingId,
                JmapProperties.MaxSize,
                JmapProperties.MaxRecipients,
                JmapProperties.InvalidRecipients,
                JmapProperties.NotFound
            });
        return error;
    }

    internal void EncodeSetError(JmapSetError error, JmapJsonWriter writer)
    {
        if (error == null)
        {
            throw new System.ArgumentNullException("error");
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Type, error.Type, "/type");
        writer.WriteStringMember(JmapProperties.Description, error.Description, false);
        WriteOptionalStringArray(JmapProperties.Properties, error.Properties, writer, "/properties");
        writer.WriteStringMember(JmapProperties.ExistingId, error.ExistingId, false);
        if (error.HasMaxSize)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.MaxSize, error.MaxSize, "/maxSize");
        }

        if (error.HasMaxRecipients)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.MaxRecipients, error.MaxRecipients, "/maxRecipients");
        }

        WriteOptionalStringArray(JmapProperties.InvalidRecipients, error.InvalidRecipients, writer, "/invalidRecipients");
        WriteOptionalStringArray(JmapProperties.NotFound, error.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(
            error.UnknownProperties,
            new List<string>
            {
                JmapProperties.Type,
                JmapProperties.Description,
                JmapProperties.Properties,
                JmapProperties.ExistingId,
                JmapProperties.MaxSize,
                JmapProperties.MaxRecipients,
                JmapProperties.InvalidRecipients,
                JmapProperties.NotFound
            },
            writer);
        writer.WriteObjectEnd();
    }

    internal string? ReadOptionalString(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return Values.ReadRequiredString(value, path);
    }

    internal List<string>? ReadOptionalStringArray(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return Values.DecodeStringArray(value, path);
    }

    internal void WriteOptionalStringArray(string name, List<string>? values, JmapJsonWriter writer, string path)
    {
        if (values == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        Values.EncodeStringArray(values, writer, path);
    }

    internal JmapProblemKind ClassifyProblemType(string type)
    {
        switch (type)
        {
            case JmapErrorCodes.UnknownCapabilityProblemType:
                return JmapProblemKind.UnknownCapability;
            case JmapErrorCodes.NotJsonProblemType:
                return JmapProblemKind.NotJson;
            case JmapErrorCodes.NotRequestProblemType:
                return JmapProblemKind.NotRequest;
            case JmapErrorCodes.LimitProblemType:
                return JmapProblemKind.Limit;
            default:
                return JmapProblemKind.Unknown;
        }
    }

    internal JmapMethodErrorKind ClassifyMethodErrorType(string type)
    {
        switch (type)
        {
            case JmapErrorCodes.ServerUnavailable:
                return JmapMethodErrorKind.ServerUnavailable;
            case JmapErrorCodes.ServerFail:
                return JmapMethodErrorKind.ServerFail;
            case JmapErrorCodes.ServerPartialFail:
                return JmapMethodErrorKind.ServerPartialFail;
            case JmapErrorCodes.UnknownMethod:
                return JmapMethodErrorKind.UnknownMethod;
            case JmapErrorCodes.InvalidArguments:
                return JmapMethodErrorKind.InvalidArguments;
            case JmapErrorCodes.InvalidResultReference:
                return JmapMethodErrorKind.InvalidResultReference;
            case JmapErrorCodes.Forbidden:
                return JmapMethodErrorKind.Forbidden;
            case JmapErrorCodes.AccountNotFound:
                return JmapMethodErrorKind.AccountNotFound;
            case JmapErrorCodes.AccountNotSupportedByMethod:
                return JmapMethodErrorKind.AccountNotSupportedByMethod;
            case JmapErrorCodes.AccountReadOnly:
                return JmapMethodErrorKind.AccountReadOnly;
            case JmapErrorCodes.RequestTooLarge:
                return JmapMethodErrorKind.RequestTooLarge;
            case JmapErrorCodes.CannotCalculateChanges:
                return JmapMethodErrorKind.CannotCalculateChanges;
            case JmapErrorCodes.StateMismatch:
                return JmapMethodErrorKind.StateMismatch;
            case JmapErrorCodes.AnchorNotFound:
                return JmapMethodErrorKind.AnchorNotFound;
            case JmapErrorCodes.UnsupportedFilter:
                return JmapMethodErrorKind.UnsupportedFilter;
            case JmapErrorCodes.UnsupportedSort:
                return JmapMethodErrorKind.UnsupportedSort;
            case JmapErrorCodes.TooManyChanges:
                return JmapMethodErrorKind.TooManyChanges;
            case JmapErrorCodes.FromAccountNotFound:
                return JmapMethodErrorKind.FromAccountNotFound;
            case JmapErrorCodes.FromAccountNotSupportedByMethod:
                return JmapMethodErrorKind.FromAccountNotSupportedByMethod;
            default:
                return JmapMethodErrorKind.Unknown;
        }
    }

    internal JmapSetErrorKind ClassifySetErrorType(string type)
    {
        switch (type)
        {
            case JmapErrorCodes.Forbidden:
                return JmapSetErrorKind.Forbidden;
            case JmapErrorCodes.OverQuota:
                return JmapSetErrorKind.OverQuota;
            case JmapErrorCodes.TooLarge:
                return JmapSetErrorKind.TooLarge;
            case JmapErrorCodes.RateLimit:
                return JmapSetErrorKind.RateLimit;
            case JmapErrorCodes.NotFound:
                return JmapSetErrorKind.NotFound;
            case JmapErrorCodes.InvalidPatch:
                return JmapSetErrorKind.InvalidPatch;
            case JmapErrorCodes.WillDestroy:
                return JmapSetErrorKind.WillDestroy;
            case JmapErrorCodes.InvalidProperties:
                return JmapSetErrorKind.InvalidProperties;
            case JmapErrorCodes.Singleton:
                return JmapSetErrorKind.Singleton;
            case JmapErrorCodes.AlreadyExists:
                return JmapSetErrorKind.AlreadyExists;
            case JmapErrorCodes.MailboxHasChild:
                return JmapSetErrorKind.MailboxHasChild;
            case JmapErrorCodes.MailboxHasEmail:
                return JmapSetErrorKind.MailboxHasEmail;
            case JmapErrorCodes.BlobNotFound:
                return JmapSetErrorKind.BlobNotFound;
            case JmapErrorCodes.TooManyKeywords:
                return JmapSetErrorKind.TooManyKeywords;
            case JmapErrorCodes.TooManyMailboxes:
                return JmapSetErrorKind.TooManyMailboxes;
            case JmapErrorCodes.ForbiddenFrom:
                return JmapSetErrorKind.ForbiddenFrom;
            case JmapErrorCodes.InvalidEmail:
                return JmapSetErrorKind.InvalidEmail;
            case JmapErrorCodes.TooManyRecipients:
                return JmapSetErrorKind.TooManyRecipients;
            case JmapErrorCodes.NoRecipients:
                return JmapSetErrorKind.NoRecipients;
            case JmapErrorCodes.InvalidRecipients:
                return JmapSetErrorKind.InvalidRecipients;
            case JmapErrorCodes.ForbiddenMailFrom:
                return JmapSetErrorKind.ForbiddenMailFrom;
            case JmapErrorCodes.ForbiddenToSend:
                return JmapSetErrorKind.ForbiddenToSend;
            case JmapErrorCodes.CannotUnsend:
                return JmapSetErrorKind.CannotUnsend;
            default:
                return JmapSetErrorKind.Unknown;
        }
    }
}
