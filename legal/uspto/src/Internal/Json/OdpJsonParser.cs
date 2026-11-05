namespace uspto;

internal static class OdpJsonParser
{
    internal static PatentApplicationResponse ParsePatentApplicationSearch(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent search response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("The ODP patent search request is required.", "request", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("ODP response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue count = root.ReadInt32("count");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int32Value < 0)
            {
                throw ParseFailure("The ODP patent search response has an invalid count.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsValid || records.IsNull || count.Int32Value != 0 && !records.IsPresent)
            {
                throw ParseFailure("The ODP patent search response has an invalid record collection.", records.Path);
            }

            if (count.Int32Value != records.ArrayItems.Length)
            {
                throw ParseFailure("The ODP patent search count does not match its records.", records.Path);
            }

            JsonFieldValue requestIdentifier = root.ReadString("requestIdentifier");
            ValidateOptionalValue(requestIdentifier, "The ODP patent search request identifier is invalid.");
            string? resolvedRequestIdentifier = responseMetadata.RequestIdentifier;
            if (requestIdentifier.IsPresent && !requestIdentifier.IsNull)
            {
                resolvedRequestIdentifier = requestIdentifier.StringValue;
            }

            PatentDataProvenance provenance = CreateProvenance(responseMetadata, resolvedRequestIdentifier);
            PatentApplicationSummary[] summaries = new PatentApplicationSummary[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                summaries[index] = ParsePatentApplicationSummary(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentApplicationResponse result = new PatentApplicationResponse();
            result.Request = request;
            result.Summaries = summaries;
            result.Count = count.Int32Value;
            result.Page = CopyOffsetPage(request.OffsetPage);
            result.RequestIdentifier = resolvedRequestIdentifier;
            result.Provenance = provenance;
            result.Facets = ParsePatentApplicationFacets(root);
            result.RawResponse = rawResponse;
            result.IsComplete = true;
            return result;
        }
    }

    internal static PatentFileWrapper ParsePatentApplication(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent application response is required.", "json", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(expectedApplicationNumber))
        {
            throw new ValidationException("An expected application number is required.", "expectedApplicationNumber", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("ODP response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue count = root.ReadInt32("count");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int32Value != 1)
            {
                throw ParseApplicationFailure("The ODP patent application response must contain exactly one record.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw ParseApplicationFailure("The ODP patent application response has an invalid record collection.", records.Path);
            }

            JsonFieldValue requestIdentifier = root.ReadString("requestIdentifier");
            ValidateOptionalValue(requestIdentifier, "The ODP patent application request identifier is invalid.");
            string? resolvedRequestIdentifier = responseMetadata.RequestIdentifier;
            if (requestIdentifier.IsPresent && !requestIdentifier.IsNull)
            {
                resolvedRequestIdentifier = requestIdentifier.StringValue;
            }

            PatentDataProvenance provenance = CreateProvenance(responseMetadata, resolvedRequestIdentifier);
            return OdpPatentApplicationParser.ParseWrapper(records.ArrayItems[0], records.ArrayItemPaths[0], expectedApplicationNumber, provenance);
        }
    }

    internal static PatentApplicationSummary ParsePatentApplicationSummary(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        JsonFieldReader record = new JsonFieldReader(element, path);
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP patent search record is not an object.", path);
        }

        PatentApplicationSummary summary = new PatentApplicationSummary();
        summary.Provenance = CopyProvenance(provenance);
        JsonFieldValue applicationNumber = record.ReadString("applicationNumberText");
        ValidateOptionalValue(applicationNumber, "The ODP patent search application number is invalid.");
        if (applicationNumber.IsPresent && !applicationNumber.IsNull)
        {
            PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber.StringValue ?? string.Empty);
            if (!parsed.IsValid)
            {
                throw ParseFailure("The ODP patent search application number is invalid.", applicationNumber.Path);
            }

            summary.ApplicationNumber = parsed.CanonicalNumber;
        }

        JsonFieldValue metadataValue = record.ReadObject("applicationMetaData");
        if (!metadataValue.IsValid)
        {
            throw ParseFailure("The ODP patent search application metadata is invalid.", metadataValue.Path);
        }

        if (metadataValue.IsPresent && !metadataValue.IsNull)
        {
            JsonFieldReader metadata = new JsonFieldReader(metadataValue.ElementValue, metadataValue.Path);
            ApplySummaryMetadata(summary, metadata);
            CaptureSummaryMetadataFields(summary, metadata);
        }

        JsonUnknownField[] recordUnknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "applicationMetaData" });
        int unknownIndex;
        for (unknownIndex = 0; unknownIndex < recordUnknown.Length; unknownIndex += 1)
        {
            summary.AdditionalFields[recordUnknown[unknownIndex].Name] = recordUnknown[unknownIndex].RawJson;
        }

        return summary;
    }

    internal static void ApplySummaryMetadata(PatentApplicationSummary summary, JsonFieldReader metadata)
    {
        summary.PatentNumber = ReadOptionalString(metadata, "patentNumber");
        summary.PublicationNumber = ReadOptionalString(metadata, "earliestPublicationNumber");
        summary.InventionTitle = ReadOptionalString(metadata, "inventionTitle");
        summary.ApplicationTypeCode = ReadOptionalString(metadata, "applicationTypeCode");
        summary.ApplicationTypeLabel = ReadOptionalString(metadata, "applicationTypeLabelName");
        summary.ApplicationStatusDescription = ReadOptionalString(metadata, "applicationStatusDescriptionText");
        summary.FirstApplicantName = ReadOptionalString(metadata, "firstApplicantName");
        summary.FirstInventorName = ReadOptionalString(metadata, "firstInventorName");
        summary.ArtUnit = ReadOptionalString(metadata, "groupArtUnitNumber");

        JsonFieldValue filingDate = metadata.ReadDate("filingDate");
        ValidateOptionalValue(filingDate, "The ODP patent search filing date is invalid.");
        if (filingDate.IsPresent && !filingDate.IsNull)
        {
            summary.FilingDate = filingDate.DateValue;
            summary.HasFilingDate = true;
        }

        JsonFieldValue publicationDate = metadata.ReadDate("earliestPublicationDate");
        ValidateOptionalValue(publicationDate, "The ODP patent search publication date is invalid.");
        if (publicationDate.IsPresent && !publicationDate.IsNull)
        {
            summary.PublicationDate = publicationDate.DateValue;
            summary.HasPublicationDate = true;
        }

        JsonFieldValue grantDate = metadata.ReadDate("grantDate");
        ValidateOptionalValue(grantDate, "The ODP patent search grant date is invalid.");
        if (grantDate.IsPresent && !grantDate.IsNull)
        {
            summary.GrantDate = grantDate.DateValue;
            summary.HasGrantDate = true;
        }

        JsonFieldValue statusCode = metadata.ReadInt32("applicationStatusCode");
        ValidateOptionalValue(statusCode, "The ODP patent search status code is invalid.");
        if (statusCode.IsPresent && !statusCode.IsNull)
        {
            summary.ApplicationStatusCode = statusCode.Int32Value;
            summary.HasApplicationStatusCode = true;
        }
    }

    internal static void CaptureSummaryMetadataFields(PatentApplicationSummary summary, JsonFieldReader metadata)
    {
        string[] knownNames = new string[]
        {
            "patentNumber",
            "earliestPublicationNumber",
            "inventionTitle",
            "applicationTypeCode",
            "applicationTypeLabelName",
            "applicationStatusDescriptionText",
            "firstApplicantName",
            "firstInventorName",
            "groupArtUnitNumber",
            "filingDate",
            "earliestPublicationDate",
            "grantDate",
            "applicationStatusCode"
        };
        JsonUnknownField[] unknown = metadata.CaptureUnknownFields(knownNames);
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            summary.AdditionalFields["applicationMetaData." + unknown[index].Name] = unknown[index].RawJson;
        }
    }

    internal static PatentApplicationFacet[] ParsePatentApplicationFacets(JsonFieldReader root)
    {
        JsonFieldValue facets = root.ReadArray("facets");
        if (!facets.IsValid)
        {
            throw ParseFailure("The ODP patent search facets are invalid.", facets.Path);
        }

        if (!facets.IsPresent || facets.IsNull)
        {
            return new PatentApplicationFacet[0];
        }

        global::System.Collections.Generic.List<PatentApplicationFacet> results = new global::System.Collections.Generic.List<PatentApplicationFacet>();
        int index;
        for (index = 0; index < facets.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement item = facets.ArrayItems[index];
            string itemPath = facets.ArrayItemPaths[index];
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ParseFailure("The ODP patent search facet entry is invalid.", itemPath);
            }

            foreach (global::System.Text.Json.JsonProperty property in item.EnumerateObject())
            {
                string propertyPath = JsonFieldReader.AppendPropertyPath(itemPath, property.Name);
                results.Add(ParsePatentApplicationFacet(property.Name, property.Value, propertyPath));
            }
        }

        return results.ToArray();
    }

    internal static PatentApplicationFacet ParsePatentApplicationFacet(string field, global::System.Text.Json.JsonElement element, string path)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw ParseFailure("The ODP patent search facet values are invalid.", path);
        }

        global::System.Collections.Generic.List<PatentApplicationFacetValue> values = new global::System.Collections.Generic.List<PatentApplicationFacetValue>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in element.EnumerateArray())
        {
            string itemPath = path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            JsonFieldReader valueReader = new JsonFieldReader(item, itemPath);
            JsonFieldValue value = valueReader.ReadString("value");
            JsonFieldValue count = valueReader.ReadInt64("count");
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object || !value.IsPresent || !value.IsValid || value.IsNull)
            {
                throw ParseFailure("The ODP patent search facet value is invalid.", value.Path);
            }

            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int64Value < 0L)
            {
                throw ParseFailure("The ODP patent search facet count is invalid.", count.Path);
            }

            PatentApplicationFacetValue facetValue = new PatentApplicationFacetValue();
            facetValue.Value = value.StringValue;
            facetValue.Count = count.Int64Value;
            values.Add(facetValue);
            index += 1;
        }

        PatentApplicationFacet result = new PatentApplicationFacet();
        result.Field = field;
        result.Values = values.ToArray();
        return result;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptionalValue(value, "The ODP patent search field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static void ValidateOptionalValue(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw ParseFailure(message, value.Path);
        }
    }

    internal static PatentDataProvenance CreateProvenance(HttpResponseMetadata responseMetadata, string? requestIdentifier)
    {
        PatentDataProvenance provenance = new PatentDataProvenance();
        provenance.Source = DataProvenance.Odp;
        provenance.SourceUri = responseMetadata.RequestUri;
        provenance.RequestIdentifier = requestIdentifier;
        provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        provenance.HasRetrievedAt = true;
        provenance.IsAuthoritative = true;
        ProvenanceLabeler.AddPatentField(provenance, "*", provenance);
        return provenance;
    }

    internal static PatentDataProvenance CopyProvenance(PatentDataProvenance source)
    {
        return ProvenanceLabeler.CopyPatent(source);
    }

    internal static OffsetPage CopyOffsetPage(OffsetPage source)
    {
        if (source == null)
        {
            throw new ValidationException("ODP offset-page coordinates are required.", "OffsetPage", "REQUIRED", null);
        }

        OffsetPage copy = new OffsetPage();
        copy.Offset = source.Offset;
        copy.Limit = source.Limit;
        return copy;
    }

    internal static ParseException ParseFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent application search", path, 0L, false, 0, false, null);
    }

    internal static ParseException ParseApplicationFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent application retrieval", path, 0L, false, 0, false, null);
    }
}
