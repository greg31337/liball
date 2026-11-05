namespace uspto;

internal static class OfficeActionFieldParser
{
    internal static OfficeActionFieldResponse Parse(string json, string expectedDataset, string expectedVersion, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An Office Action field response is required.", "json", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(expectedDataset))
        {
            throw new ValidationException("An Office Action dataset name is required.", "expectedDataset", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(expectedVersion))
        {
            throw new ValidationException("An Office Action dataset version is required.", "expectedVersion", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("Office Action response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            if (document.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw Failure("The Office Action field response must be a JSON object.", "$");
            }

            JsonFieldValue dataset = ReadRequiredString(root, "apiKey", "The Office Action dataset name is missing or invalid.");
            JsonFieldValue version = ReadRequiredString(root, "apiVersionNumber", "The Office Action dataset version is missing or invalid.");
            JsonFieldValue status = ReadRequiredString(root, "apiStatus", "The Office Action dataset status is missing or invalid.");
            if (!global::System.String.Equals(dataset.StringValue, expectedDataset, global::System.StringComparison.Ordinal))
            {
                throw Failure("The Office Action field response identifies a different dataset.", dataset.Path);
            }

            if (!global::System.String.Equals(version.StringValue, expectedVersion, global::System.StringComparison.Ordinal))
            {
                throw Failure("The Office Action field response identifies a different dataset version.", version.Path);
            }

            global::System.Uri serviceUri = ReadRequiredUri(root, "apiUrl", "The Office Action dataset URI is missing or invalid.");
            global::System.Uri schemaUri = ReadRequiredUri(root, "apiDocumentationUrl", "The Office Action schema URI is missing or invalid.");
            JsonFieldValue count = root.ReadInt32("fieldCount");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int32Value < 0)
            {
                throw Failure("The Office Action field count is missing or invalid.", count.Path);
            }

            JsonFieldValue fieldValues = root.ReadArray("fields");
            if (!fieldValues.IsPresent || !fieldValues.IsValid || fieldValues.IsNull)
            {
                throw Failure("The Office Action searchable-field collection is missing or invalid.", fieldValues.Path);
            }

            string[] fields = ReadFields(fieldValues);
            if (fields.Length != count.Int32Value)
            {
                throw Failure("The Office Action searchable-field count does not match the collection.", count.Path);
            }

            JsonFieldValue lastUpdated = root.ReadString("lastDataUpdatedDate");
            if (!lastUpdated.IsValid)
            {
                throw Failure("The Office Action dataset last-update value is invalid.", lastUpdated.Path);
            }

            JsonFieldValue requestIdentifier = root.ReadString("requestIdentifier");
            if (!requestIdentifier.IsValid)
            {
                throw Failure("The Office Action request identifier is invalid.", requestIdentifier.Path);
            }

            string? resolvedRequestIdentifier = responseMetadata.RequestIdentifier;
            if (requestIdentifier.IsPresent && !requestIdentifier.IsNull)
            {
                resolvedRequestIdentifier = requestIdentifier.StringValue;
            }

            OfficeActionDatasetMetadata metadata = new OfficeActionDatasetMetadata();
            metadata.DatasetName = dataset.StringValue;
            metadata.Version = version.StringValue;
            metadata.Status = status.StringValue;
            metadata.ServiceUri = serviceUri;
            metadata.SchemaUri = schemaUri;
            if (lastUpdated.IsPresent && !lastUpdated.IsNull)
            {
                metadata.LastUpdatedText = lastUpdated.StringValue;
                JsonDateParseResult parsedDate = JsonFieldReader.ParseFlexibleDate((lastUpdated.StringValue ?? string.Empty).Trim());
                if (parsedDate.IsValid)
                {
                    metadata.LastUpdatedAt = parsedDate.Value;
                    metadata.HasLastUpdatedAt = true;
                }
            }

            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, resolvedRequestIdentifier);
            provenance.DatasetIdentifier = dataset.StringValue;
            provenance.DatasetVersion = version.StringValue;
            provenance.LastUpdatedAt = metadata.LastUpdatedAt;
            provenance.HasLastUpdatedAt = metadata.HasLastUpdatedAt;

            OfficeActionFieldResponse result = new OfficeActionFieldResponse();
            result.Metadata = metadata;
            result.Fields = fields;
            result.Count = fields.Length;
            result.RequestIdentifier = resolvedRequestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = true;
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "apiKey", "apiVersionNumber", "apiUrl", "apiDocumentationUrl", "apiStatus", "fieldCount", "fields", "lastDataUpdatedDate", "requestIdentifier" });
            MergeUnknown(result.UnknownFields, unknown);
            return result;
        }
    }

    internal static JsonFieldValue ReadRequiredString(JsonFieldReader reader, string name, string message)
    {
        JsonFieldValue value = reader.ReadString(name);
        if (!value.IsPresent || !value.IsValid || value.IsNull || global::System.String.IsNullOrWhiteSpace(value.StringValue))
        {
            throw Failure(message, value.Path);
        }

        return value;
    }

    internal static global::System.Uri ReadRequiredUri(JsonFieldReader reader, string name, string message)
    {
        JsonFieldValue value = ReadRequiredString(reader, name, message);
        string uriText = value.StringValue ?? string.Empty;
        if (!global::System.Uri.IsWellFormedUriString(uriText, global::System.UriKind.Absolute))
        {
            throw Failure(message, value.Path);
        }

        return new global::System.Uri(uriText, global::System.UriKind.Absolute);
    }

    internal static string[] ReadFields(JsonFieldValue fieldValues)
    {
        string[] fields = new string[fieldValues.ArrayItems.Length];
        int index;
        for (index = 0; index < fieldValues.ArrayItems.Length; index += 1)
        {
            if (fieldValues.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.String)
            {
                throw Failure("An Office Action searchable field name is invalid.", fieldValues.ArrayItemPaths[index]);
            }

            string? field = fieldValues.ArrayItems[index].GetString();
            if (global::System.String.IsNullOrWhiteSpace(field))
            {
                throw Failure("An Office Action searchable field name is invalid.", fieldValues.ArrayItemPaths[index]);
            }

            fields[index] = field ?? string.Empty;
        }

        return fields;
    }

    internal static void MergeUnknown(global::System.Collections.Generic.Dictionary<string, string> destination, JsonUnknownField[] fields)
    {
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            destination[fields[index].Name] = fields[index].RawJson;
        }
    }

    internal static ParseException Failure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "Office Action field discovery", path, 0L, false, 0, false, null);
    }
}
