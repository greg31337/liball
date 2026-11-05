namespace uspto;

internal static class OdpPatentStatusCodeParser
{
    internal static PatentStatusCodeResponse Parse(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent status-code response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("The ODP patent status-code request is required.", "request", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("ODP response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue count = root.ReadInt64("count");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int64Value < 0L)
            {
                throw StatusCodeFailure("The ODP patent status-code response has an invalid count.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("statusCodeBag");
            ValidateOptional(records, "The ODP patent status-code collection is invalid.");
            if ((!records.IsPresent || records.IsNull) && count.Int64Value != 0L)
            {
                throw StatusCodeFailure("The ODP patent status-code collection is missing.", records.Path);
            }

            PatentStatusCode[] statusCodes = ParseRecords(records);
            if (statusCodes.Length != 0 && (request.OffsetPage.Offset > count.Int64Value || statusCodes.Length > count.Int64Value - request.OffsetPage.Offset))
            {
                throw StatusCodeFailure("The ODP patent status-code page extends beyond the reported total.", records.Path);
            }

            JsonFieldValue requestIdentifier = root.ReadString("requestIdentifier");
            ValidateOptional(requestIdentifier, "The ODP patent status-code request identifier is invalid.");
            string? resolvedRequestIdentifier = responseMetadata.RequestIdentifier;
            if (requestIdentifier.IsPresent && !requestIdentifier.IsNull)
            {
                resolvedRequestIdentifier = requestIdentifier.StringValue;
            }

            PatentStatusCodeResponse result = new PatentStatusCodeResponse();
            result.Request = request;
            result.StatusCodes = statusCodes;
            result.DescriptionsByCode = BuildDescriptionMap(statusCodes);
            result.Count = statusCodes.Length;
            result.Total = count.Int64Value;
            result.HasTotal = true;
            result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
            result.RequestIdentifier = resolvedRequestIdentifier;
            result.Provenance = OdpJsonParser.CreateProvenance(responseMetadata, resolvedRequestIdentifier);
            result.RawResponse = rawResponse;
            result.IsComplete = statusCodes.Length == 0 || request.OffsetPage.Offset + statusCodes.Length >= count.Int64Value;
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "count", "statusCodeBag", "requestIdentifier" });
            MergeUnknown(result.UnknownFields, unknown);
            return result;
        }
    }

    internal static PatentStatusCode[] ParseRecords(JsonFieldValue records)
    {
        if (!records.IsPresent || records.IsNull)
        {
            return new PatentStatusCode[0];
        }

        PatentStatusCode[] statusCodes = new PatentStatusCode[records.ArrayItems.Length];
        int index;
        for (index = 0; index < records.ArrayItems.Length; index += 1)
        {
            if (records.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw StatusCodeFailure("The ODP patent status-code entry is invalid.", records.ArrayItemPaths[index]);
            }

            JsonFieldReader reader = new JsonFieldReader(records.ArrayItems[index], records.ArrayItemPaths[index]);
            JsonFieldValue code = reader.ReadInt32("applicationStatusCode");
            if (!code.IsPresent || !code.IsValid || code.IsNull)
            {
                throw StatusCodeFailure("The ODP patent application status code is missing or invalid.", code.Path);
            }

            JsonFieldValue description = reader.ReadString("applicationStatusDescriptionText");
            if (!description.IsPresent || !description.IsValid || description.IsNull)
            {
                throw StatusCodeFailure("The ODP patent application status description is missing or invalid.", description.Path);
            }

            PatentStatusCode statusCode = new PatentStatusCode();
            statusCode.Code = code.Int32Value;
            statusCode.Description = description.StringValue;
            JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "applicationStatusCode", "applicationStatusDescriptionText" });
            MergeUnknown(statusCode.UnknownFields, unknown);
            statusCodes[index] = statusCode;
        }

        return statusCodes;
    }

    internal static global::System.Collections.Generic.Dictionary<int, string> BuildDescriptionMap(PatentStatusCode[] statusCodes)
    {
        global::System.Collections.Generic.Dictionary<int, string> descriptions = new global::System.Collections.Generic.Dictionary<int, string>();
        int index;
        for (index = 0; index < statusCodes.Length; index += 1)
        {
            PatentStatusCode statusCode = statusCodes[index];
            if (descriptions.ContainsKey(statusCode.Code))
            {
                throw StatusCodeFailure("The ODP patent status-code response contains a duplicate code.", "$.statusCodeBag[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].applicationStatusCode");
            }

            descriptions.Add(statusCode.Code, statusCode.Description ?? string.Empty);
        }

        return descriptions;
    }

    internal static void MergeUnknown(global::System.Collections.Generic.Dictionary<string, string> destination, JsonUnknownField[] fields)
    {
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            destination[fields[index].Name] = fields[index].RawJson;
        }
    }

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw StatusCodeFailure(message, value.Path);
        }
    }

    internal static ParseException StatusCodeFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent status-code search", path, 0L, false, 0, false, null);
    }
}
