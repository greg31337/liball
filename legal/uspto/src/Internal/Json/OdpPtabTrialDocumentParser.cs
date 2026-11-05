namespace uspto;

internal static class OdpPtabTrialDocumentParser
{
    internal static PtabTrialDocumentResponse ParseSearch(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP PTAB trial document response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("The ODP PTAB trial document search request is required.", "request", "REQUIRED", null);
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
                throw ParseFailure("The ODP PTAB trial document response has an invalid count.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentTrialDocumentDataBag");
            if (!records.IsValid || records.IsNull || count.Int32Value != 0 && !records.IsPresent)
            {
                throw ParseFailure("The ODP PTAB trial document response has an invalid record collection.", records.Path);
            }

            if (count.Int32Value != records.ArrayItems.Length)
            {
                throw ParseFailure("The ODP PTAB trial document count does not match its records.", records.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, responseMetadata);
            PtabDataProvenance provenance = CreateProvenance(responseMetadata, requestIdentifier);
            PtabTrialDocument[] trialDocuments = new PtabTrialDocument[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                trialDocuments[index] = ParseRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PtabTrialDocumentResponse result = new PtabTrialDocumentResponse();
            result.Request = request;
            result.Documents = trialDocuments;
            result.Facets = ParseFacets(root);
            result.Count = trialDocuments.Length;
            ApplyTotal(root, result);
            result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
            result.RequestIdentifier = requestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = true;
            return result;
        }
    }

    internal static PtabTrialDocument ParseSingle(string json, string expectedDocumentIdentifier, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedDocumentIdentifier))
        {
            throw new ValidationException("An expected PTAB document identifier is required.", "expectedDocumentIdentifier", "REQUIRED", null);
        }

        PtabTrialDocumentResponse response = ParseSearch(json, new SearchRequest(), responseMetadata, null);
        if (response.Count != 1)
        {
            throw ParseFailure("The ODP PTAB trial document response must contain exactly one record.", "$.count");
        }

        PtabTrialDocument result = response.Documents[0];
        if (!global::System.String.Equals(result.Document.DocumentIdentifier, expectedDocumentIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw ParseFailure("The ODP PTAB document identifier does not match the requested identifier.", "$.patentTrialDocumentDataBag[0].documentData.documentIdentifier");
        }

        return result;
    }

    internal static PtabTrialDocumentResponse ParseByTrial(string json, string expectedTrialNumber, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedTrialNumber))
        {
            throw new ValidationException("An expected PTAB trial number is required.", "expectedTrialNumber", "REQUIRED", null);
        }

        PtabTrialDocumentResponse result = ParseSearch(json, new SearchRequest(), responseMetadata, null);
        int index;
        for (index = 0; index < result.Documents.Length; index += 1)
        {
            if (!global::System.String.Equals(result.Documents[index].TrialNumber, expectedTrialNumber, global::System.StringComparison.OrdinalIgnoreCase))
            {
                string path = "$.patentTrialDocumentDataBag[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].trialNumber";
                throw ParseFailure("The ODP PTAB document trial number does not match the requested trial.", path);
            }
        }

        return result;
    }

    internal static PtabTrialDocument ParseRecord(global::System.Text.Json.JsonElement element, string path, PtabDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP PTAB trial document record is not an object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PtabTrialDocument result = new PtabTrialDocument();
        result.RecordCategory = ReadOptionalString(reader, "trialDocumentCategory");
        result.TrialNumber = ReadOptionalString(reader, "trialNumber");
        result.TrialTypeCode = ReadOptionalString(reader, "trialTypeCode");
        ApplyRecordModifiedDate(reader, result);
        result.Metadata = OdpPtabProceedingParser.ParseMetadata(reader);
        result.PatentOwner = OdpPtabProceedingParser.ParsePatentOwner(reader);
        result.RegularPetitioner = OdpPtabProceedingParser.ParsePetitioner(reader, "regularPetitionerData", "Regular");
        result.Respondent = OdpPtabProceedingParser.ParseRespondent(reader);
        result.DerivationPetitioner = OdpPtabProceedingParser.ParsePetitioner(reader, "derivationPetitionerData", "Derivation");
        result.Document = OdpPtabTrialDecisionParser.ParseDocument(reader, provenance, PtabCollection.TrialDocuments);
        result.Decision = OdpPtabTrialDecisionParser.ParseDecision(reader, PtabCollection.TrialDocuments);
        result.Provenance = OdpPtabProceedingParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "trialDocumentCategory", "lastModifiedDateTime", "trialNumber", "trialTypeCode", "trialMetaData", "patentOwnerData", "regularPetitionerData", "respondentData", "derivationPetitionerData", "documentData", "decisionData" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyRecordModifiedDate(JsonFieldReader reader, PtabTrialDocument result)
    {
        JsonFieldValue value = reader.ReadDate("lastModifiedDateTime");
        ValidateOptional(value, "The ODP PTAB trial document modification time is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            result.LastModifiedAt = value.DateValue;
            result.HasLastModifiedAt = true;
        }
    }

    internal static PtabFacet[] ParseFacets(JsonFieldReader root)
    {
        JsonFieldValue facets = root.ReadArray("facets");
        ValidateOptional(facets, "The ODP PTAB trial document facets are invalid.");
        if (!facets.IsPresent || facets.IsNull)
        {
            return new PtabFacet[0];
        }

        global::System.Collections.Generic.List<PtabFacet> results = new global::System.Collections.Generic.List<PtabFacet>();
        int index;
        for (index = 0; index < facets.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement item = facets.ArrayItems[index];
            string itemPath = facets.ArrayItemPaths[index];
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ParseFailure("The ODP PTAB trial document facet entry is invalid.", itemPath);
            }

            foreach (global::System.Text.Json.JsonProperty property in item.EnumerateObject())
            {
                string propertyPath = JsonFieldReader.AppendPropertyPath(itemPath, property.Name);
                results.Add(ParseFacet(property.Name, property.Value, propertyPath));
            }
        }

        return results.ToArray();
    }

    internal static PtabFacet ParseFacet(string field, global::System.Text.Json.JsonElement element, string path)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw ParseFailure("The ODP PTAB trial document facet values are invalid.", path);
        }

        global::System.Collections.Generic.List<PtabFacetValue> values = new global::System.Collections.Generic.List<PtabFacetValue>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in element.EnumerateArray())
        {
            string itemPath = path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            JsonFieldReader reader = new JsonFieldReader(item, itemPath);
            JsonFieldValue name = reader.ReadString("name");
            JsonFieldValue quantity = reader.ReadInt64("quantity");
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object || !name.IsPresent || !name.IsValid || name.IsNull)
            {
                throw ParseFailure("The ODP PTAB trial document facet name is invalid.", name.Path);
            }

            if (!quantity.IsPresent || !quantity.IsValid || quantity.IsNull || quantity.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB trial document facet quantity is invalid.", quantity.Path);
            }

            PtabFacetValue facetValue = new PtabFacetValue();
            facetValue.Name = name.StringValue;
            facetValue.Count = quantity.Int64Value;
            values.Add(facetValue);
            index += 1;
        }

        PtabFacet result = new PtabFacet();
        result.Field = field;
        result.Values = values.ToArray();
        return result;
    }

    internal static void ApplyTotal(JsonFieldReader root, PtabTrialDocumentResponse result)
    {
        JsonFieldValue total = root.ReadInt64("total");
        if (!total.IsPresent)
        {
            total = root.ReadInt64("totalRecordCount");
        }

        ValidateOptional(total, "The ODP PTAB trial document total is invalid.");
        if (total.IsPresent && !total.IsNull)
        {
            if (total.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB trial document total cannot be negative.", total.Path);
            }

            result.Total = total.Int64Value;
            result.HasTotal = true;
        }
    }

    internal static PtabDataProvenance CreateProvenance(HttpResponseMetadata metadata, string? requestIdentifier)
    {
        PtabDataProvenance result = OdpPtabProceedingParser.CreateProvenance(metadata, requestIdentifier);
        result.Collection = PtabCollection.TrialDocuments;
        return result;
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, HttpResponseMetadata metadata)
    {
        JsonFieldValue value = root.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP PTAB trial document request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return metadata.RequestIdentifier;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP PTAB trial document field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw ParseFailure(message, value.Path);
        }
    }

    internal static ParseException ParseFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP PTAB trial document", path, 0L, false, 0, false, null);
    }
}
