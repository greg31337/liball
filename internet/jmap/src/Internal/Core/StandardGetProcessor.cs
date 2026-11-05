using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace jmap;

internal class StandardGetResult
{
    internal bool IsSuccess;
    internal JmapGetResponse? Response;
    internal JmapMethodError? Error;
}

internal class StandardGetProcessor
{
    internal int MaximumObjectsInGet;
    internal bool SupportsAllRecords;
    internal HashSet<string> SupportedProperties = new HashSet<string>(StringComparer.Ordinal);
    internal ValueValidator Values = new ValueValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(int.MaxValue, 1000);

    internal StandardGetProcessor(int maximumObjectsInGet, List<string> supportedProperties, bool supportsAllRecords)
    {
        if (maximumObjectsInGet < 0)
        {
            throw new ArgumentOutOfRangeException("maximumObjectsInGet");
        }

        if (supportedProperties == null)
        {
            throw new ArgumentNullException("supportedProperties");
        }

        MaximumObjectsInGet = maximumObjectsInGet;
        SupportsAllRecords = supportsAllRecords;
        SupportedProperties.Add(JmapProperties.Id);
        foreach (string property in supportedProperties)
        {
            if (property == null || property.Length == 0 || !Values.IsValidUnicode(property))
            {
                throw new ArgumentException("A supported property name is not valid.", "supportedProperties");
            }

            SupportedProperties.Add(property);
        }
    }

    internal StandardGetResult ProcessGet(JmapGetRequest? request, List<JsonElement>? records, string? state)
    {
        if (request == null)
        {
            return ErrorResult(MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A get request is required."));
        }

        JmapMethodError? requestError = ValidateRequest(request);
        if (requestError != null)
        {
            return ErrorResult(requestError);
        }

        if (request.Ids != null && request.Ids.Count > MaximumObjectsInGet)
        {
            return ErrorResult(MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "The get request contains too many identifiers."));
        }

        if (records == null)
        {
            throw ProtocolFailure("A get record collection is required.", "list", default(JsonElement));
        }

        if (state == null || !Values.IsValidUnicode(state))
        {
            throw ProtocolFailure("A valid get state is required.", "state", default(JsonElement));
        }

        if (request.Ids == null && records.Count > MaximumObjectsInGet)
        {
            return ErrorResult(MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "Returning all records would exceed the get object limit."));
        }

        Dictionary<string, JsonElement> recordsById = IndexRecords(records);
        JmapGetResponse response = new JmapGetResponse();
        response.AccountId = request.AccountId;
        response.State = state;

        if (request.Ids == null)
        {
            foreach (JsonElement record in records)
            {
                response.List.Add(ProjectRecord(record, request.Properties));
            }
        }
        else
        {
            AddRequestedRecords(request.Ids, request.Properties, recordsById, response);
        }

        StandardGetResult result = new StandardGetResult();
        result.IsSuccess = true;
        result.Response = response;
        return result;
    }

    internal JmapMethodError? ValidateRequest(JmapGetRequest request)
    {
        if (!Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (request.Ids == null && !SupportsAllRecords)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "This get method requires an explicit ids list.");
        }

        if (request.Ids != null)
        {
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
                if (property == null || !SupportedProperties.Contains(property))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The properties argument contains an unsupported property.");
                }
            }
        }

        return null;
    }

    internal Dictionary<string, JsonElement> IndexRecords(List<JsonElement> records)
    {
        Dictionary<string, JsonElement> recordsById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement record in records)
        {
            if (record.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolFailure("A get record must be a JSON object.", "list/" + index, record);
            }

            JsonElement idValue;
            if (!record.TryGetProperty(JmapProperties.Id, out idValue) || idValue.ValueKind != JsonValueKind.String)
            {
                throw ProtocolFailure("A get record must contain an id string.", "list/" + index + "/id", record);
            }

            string? id = idValue.GetString();
            if (!Values.IsId(id) || id == null)
            {
                throw ProtocolFailure("A get record contains an invalid id.", "list/" + index + "/id", record);
            }

            if (recordsById.ContainsKey(id))
            {
                throw ProtocolFailure("A get record collection contains a duplicate id.", "list/" + index + "/id", record);
            }

            recordsById.Add(id, record);
            index = index + 1;
        }

        return recordsById;
    }

    internal void AddRequestedRecords(
        List<string> requestedIds,
        List<string>? requestedProperties,
        Dictionary<string, JsonElement> recordsById,
        JmapGetResponse response)
    {
        HashSet<string> handledIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in requestedIds)
        {
            if (handledIds.Add(id))
            {
                if (recordsById.ContainsKey(id))
                {
                    response.List.Add(ProjectRecord(recordsById[id], requestedProperties));
                }
                else
                {
                    response.NotFound.Add(id);
                }
            }
        }
    }

    internal JsonElement ProjectRecord(JsonElement record, List<string>? requestedProperties)
    {
        if (requestedProperties == null)
        {
            return record.Clone();
        }

        HashSet<string> includedProperties = new HashSet<string>(requestedProperties, StringComparer.Ordinal);
        includedProperties.Add(JmapProperties.Id);
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        writer.WriteObjectStart();
        foreach (JsonProperty property in record.EnumerateObject())
        {
            if (includedProperties.Contains(property.Name))
            {
                writer.WriteElementMember(property.Name, property.Value);
            }
        }

        writer.WriteObjectEnd();
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal StandardGetResult ErrorResult(JmapMethodError error)
    {
        StandardGetResult result = new StandardGetResult();
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
