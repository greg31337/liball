namespace uspto;

internal static class OdpPtabTrialDecisionParser
{
    internal static PtabTrialDecisionResponse ParseSearch(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP PTAB trial decision response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("The ODP PTAB trial decision search request is required.", "request", "REQUIRED", null);
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
                throw ParseFailure("The ODP PTAB trial decision response has an invalid count.", count.Path);
            }

            JsonFieldValue records = OdpPtabCompatibilityParser.ReadAliasedArray(root, new string[] { "patentTrialDecisionDataBag", "patentTrialDocumentDataBag" });
            if (!records.IsValid || records.IsNull || count.Int32Value != 0 && !records.IsPresent)
            {
                throw ParseFailure("The ODP PTAB trial decision response has an invalid record collection.", records.Path);
            }

            if (count.Int32Value != records.ArrayItems.Length)
            {
                throw ParseFailure("The ODP PTAB trial decision count does not match its records.", records.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, responseMetadata);
            PtabDataProvenance provenance = CreateProvenance(responseMetadata, requestIdentifier);
            PtabTrialDecision[] decisions = new PtabTrialDecision[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                decisions[index] = ParseRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PtabTrialDecisionResponse result = new PtabTrialDecisionResponse();
            result.Request = request;
            result.Decisions = decisions;
            result.Facets = ParseFacets(root);
            result.Count = decisions.Length;
            ApplyTotal(root, result);
            result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
            result.RequestIdentifier = requestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = true;
            return result;
        }
    }

    internal static PtabTrialDecision ParseSingle(string json, string expectedDocumentIdentifier, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedDocumentIdentifier))
        {
            throw new ValidationException("An expected PTAB decision document identifier is required.", "expectedDocumentIdentifier", "REQUIRED", null);
        }

        PtabTrialDecisionResponse response = ParseSearch(json, new SearchRequest(), responseMetadata, null);
        if (response.Count != 1)
        {
            throw ParseFailure("The ODP PTAB trial decision response must contain exactly one record.", "$.count");
        }

        PtabTrialDecision result = response.Decisions[0];
        if (!global::System.String.Equals(result.Document.DocumentIdentifier, expectedDocumentIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw ParseFailure("The ODP PTAB decision document identifier does not match the requested identifier.", "$.patentTrialDecisionDataBag[0].documentData.documentIdentifier");
        }

        return result;
    }

    internal static PtabTrialDecisionResponse ParseByTrial(string json, string expectedTrialNumber, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedTrialNumber))
        {
            throw new ValidationException("An expected PTAB trial number is required.", "expectedTrialNumber", "REQUIRED", null);
        }

        PtabTrialDecisionResponse result = ParseSearch(json, new SearchRequest(), responseMetadata, null);
        int index;
        for (index = 0; index < result.Decisions.Length; index += 1)
        {
            if (!global::System.String.Equals(result.Decisions[index].TrialNumber, expectedTrialNumber, global::System.StringComparison.OrdinalIgnoreCase))
            {
                string path = "$.patentTrialDecisionDataBag[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].trialNumber";
                throw ParseFailure("The ODP PTAB decision trial number does not match the requested trial.", path);
            }
        }

        return result;
    }

    internal static PtabTrialDecision ParseRecord(global::System.Text.Json.JsonElement element, string path, PtabDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP PTAB trial decision record is not an object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PtabTrialDecision result = new PtabTrialDecision();
        result.RecordCategory = ReadOptionalString(reader, "trialDocumentCategory");
        result.TrialNumber = ReadOptionalString(reader, "trialNumber");
        result.TrialTypeCode = ReadOptionalString(reader, "trialTypeCode");
        ApplyRecordModifiedDate(reader, result);
        result.Metadata = OdpPtabProceedingParser.ParseMetadata(reader);
        result.PatentOwner = OdpPtabProceedingParser.ParsePatentOwner(reader);
        result.RegularPetitioner = OdpPtabProceedingParser.ParsePetitioner(reader, "regularPetitionerData", "Regular");
        result.Respondent = OdpPtabProceedingParser.ParseRespondent(reader);
        result.DerivationPetitioner = OdpPtabProceedingParser.ParsePetitioner(reader, "derivationPetitionerData", "Derivation");
        result.Document = ParseDocument(reader, provenance, PtabCollection.TrialDecisions);
        result.Decision = ParseDecision(reader, PtabCollection.TrialDecisions);
        result.Provenance = OdpPtabProceedingParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "trialDocumentCategory", "lastModifiedDateTime", "trialNumber", "trialTypeCode", "trialMetaData", "patentOwnerData", "regularPetitionerData", "respondentData", "derivationPetitionerData", "documentData", "decisionData" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyRecordModifiedDate(JsonFieldReader reader, PtabTrialDecision result)
    {
        JsonFieldValue value = reader.ReadDate("lastModifiedDateTime");
        ValidateOptional(value, "The ODP PTAB trial decision modification time is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            result.LastModifiedAt = value.DateValue;
            result.HasLastModifiedAt = true;
        }
    }

    internal static PtabDocument ParseDocument(JsonFieldReader record, PtabDataProvenance provenance, PtabCollection collection)
    {
        JsonFieldValue value = record.ReadObject("documentData");
        ValidateDocumentOptional(value, "The ODP PTAB document data is invalid.", collection);
        PtabDocument result = new PtabDocument();
        result.Provenance = OdpPtabProceedingParser.CopyProvenance(provenance);
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.DocumentIdentifier = ReadOptionalDocumentString(reader, "documentIdentifier", collection);
        result.Name = ReadOptionalDocumentString(reader, "documentName", collection);
        result.Category = ReadOptionalDocumentString(reader, "documentCategory", collection);
        result.FilingPartyCategory = ReadOptionalDocumentString(reader, "filingPartyCategory", collection);
        result.Title = ReadOptionalDocumentString(reader, "documentTitleText", collection);
        result.TypeDescription = ReadOptionalAliasedDocumentString(reader, new string[] { "documentTypeDescriptionText", "documentTypeCategory" }, collection);
        result.MimeTypeIdentifier = ReadOptionalDocumentString(reader, "mimeTypeIdentifier", collection);
        result.OcrText = ReadOptionalDocumentString(reader, "documentOCRText", collection);
        ApplyDocumentValues(reader, result, collection);
        ApplyDocumentFormat(result);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "documentIdentifier", "documentName", "documentCategory", "filingPartyCategory", "documentTitleText", "documentTypeDescriptionText", "documentTypeCategory", "mimeTypeIdentifier", "documentOCRText", "documentNumber", "documentSizeQuantity", "documentFilingDate", "fileDownloadURI", "downloadURI" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyDocumentValues(JsonFieldReader reader, PtabDocument result, PtabCollection collection)
    {
        JsonFieldValue number = reader.ReadInt32("documentNumber");
        ValidateDocumentOptional(number, "The ODP PTAB document number is invalid.", collection);
        if (number.IsPresent && !number.IsNull)
        {
            if (number.Int32Value < 0)
            {
                throw ParseDocumentFailure("The ODP PTAB document number cannot be negative.", number.Path, collection);
            }

            result.DocumentNumber = number.Int32Value;
            result.HasDocumentNumber = true;
        }

        JsonFieldValue size = reader.ReadInt64("documentSizeQuantity");
        ValidateDocumentOptional(size, "The ODP PTAB document size is invalid.", collection);
        if (size.IsPresent && !size.IsNull)
        {
            if (size.Int64Value < 0L)
            {
                throw ParseDocumentFailure("The ODP PTAB document size cannot be negative.", size.Path, collection);
            }

            result.Size = size.Int64Value;
            result.HasSize = true;
        }

        JsonFieldValue filingDate = reader.ReadDate("documentFilingDate");
        ValidateDocumentOptional(filingDate, "The ODP PTAB document filing date is invalid.", collection);
        if (filingDate.IsPresent && !filingDate.IsNull)
        {
            result.FilingDate = filingDate.DateValue;
            result.HasFilingDate = true;
        }

        string? uriText = ReadOptionalAliasedDocumentString(reader, new string[] { "fileDownloadURI", "downloadURI" }, collection);
        if (uriText != null)
        {
            if (!global::System.Uri.IsWellFormedUriString(uriText, global::System.UriKind.Absolute))
            {
                throw ParseDocumentFailure("The ODP PTAB document download URI is invalid.", OdpPatentDocumentParser.FindPresentPath(reader, new string[] { "fileDownloadURI", "downloadURI" }), collection);
            }

            result.DownloadUri = new global::System.Uri(uriText, global::System.UriKind.Absolute);
        }
    }

    internal static string CreateValuePath(JsonFieldReader reader, string name)
    {
        return JsonFieldReader.AppendPropertyPath(reader.Path, name);
    }

    internal static string? ReadOptionalDocumentString(JsonFieldReader reader, string name, PtabCollection collection)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateDocumentOptional(value, "The ODP PTAB document field is invalid.", collection);
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string? ReadOptionalAliasedDocumentString(JsonFieldReader reader, string[] names, PtabCollection collection)
    {
        JsonFieldValue value = OdpPtabCompatibilityParser.ReadAliasedString(reader, names);
        ValidateDocumentOptional(value, "The ODP PTAB document field is invalid.", collection);
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static void ValidateDocumentOptional(JsonFieldValue value, string message, PtabCollection collection)
    {
        if (!value.IsValid)
        {
            throw ParseDocumentFailure(message, value.Path, collection);
        }
    }

    internal static ParseException ParseDocumentFailure(string message, string path, PtabCollection collection)
    {
        string sourceName = "ODP PTAB trial decision";
        if (collection == PtabCollection.TrialDocuments)
        {
            sourceName = "ODP PTAB trial document";
        }
        else if (collection == PtabCollection.AppealDecisions)
        {
            sourceName = "ODP PTAB appeal decision";
        }
        else if (collection == PtabCollection.InterferenceDecisions)
        {
            sourceName = "ODP PTAB interference decision";
        }

        return new ParseException(message, DocumentFormat.Json, sourceName, path, 0L, false, 0, false, null);
    }

    internal static void ApplyDocumentFormat(PtabDocument result)
    {
        string identifier = result.MimeTypeIdentifier ?? string.Empty;
        result.MediaType = result.MimeTypeIdentifier;
        if (identifier.Equals(UsptoMediaTypes.Pdf, global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("PDF", global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Pdf;
            result.MediaType = UsptoMediaTypes.Pdf;
        }
        else if (identifier.Equals(UsptoMediaTypes.Xml, global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals(UsptoMediaTypes.TextXml, global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("XML", global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Xml;
        }
        else if (identifier.Equals(UsptoMediaTypes.Json, global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("JSON", global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Json;
            result.MediaType = UsptoMediaTypes.Json;
        }
        else if (identifier.Equals(UsptoMediaTypes.Zip, global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("ZIP", global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Zip;
            result.MediaType = UsptoMediaTypes.Zip;
        }
        else if (identifier.Equals(UsptoMediaTypes.Text, global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Text;
        }
    }

    internal static PtabDecision ParseDecision(JsonFieldReader record, PtabCollection collection)
    {
        JsonFieldValue value = record.ReadObject("decisionData");
        ValidateDecisionOptional(value, "The ODP PTAB decision data is invalid.", collection);
        PtabDecision result = new PtabDecision();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.TypeCategory = ReadOptionalDecisionString(reader, "decisionTypeCategory", collection);
        result.TrialOutcomeCategory = ReadOptionalDecisionString(reader, "trialOutcomeCategory", collection);
        result.AppealOutcomeCategory = ReadOptionalDecisionString(reader, "appealOutcomeCategory", collection);
        result.StatutesAndRules = ReadStringArray(reader, "statuteAndRuleBag", collection);
        result.IssueTypes = ReadStringArray(reader, "issueTypeBag", collection);
        JsonFieldValue issueDate = reader.ReadDate("decisionIssueDate");
        ValidateDecisionOptional(issueDate, "The ODP PTAB decision issue date is invalid.", collection);
        if (issueDate.IsPresent && !issueDate.IsNull)
        {
            result.IssueDate = issueDate.DateValue;
            result.HasIssueDate = true;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "decisionTypeCategory", "trialOutcomeCategory", "appealOutcomeCategory", "statuteAndRuleBag", "issueTypeBag", "decisionIssueDate" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static string[] ReadStringArray(JsonFieldReader reader, string name, PtabCollection collection)
    {
        JsonFieldValue value = OdpPtabCompatibilityParser.ReadStringCollection(reader, new string[] { name });
        ValidateDecisionOptional(value, "The ODP PTAB decision text collection is invalid.", collection);
        if (!value.IsPresent || value.IsNull)
        {
            return new string[0];
        }

        string[] result = new string[value.ArrayItems.Length];
        int index;
        for (index = 0; index < value.ArrayItems.Length; index += 1)
        {
            if (value.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.String)
            {
                throw ParseDecisionFailure("The ODP PTAB decision text entry is invalid.", value.ArrayItemPaths[index], collection);
            }

            result[index] = value.ArrayItems[index].GetString() ?? string.Empty;
        }

        return result;
    }

    internal static string? ReadOptionalDecisionString(JsonFieldReader reader, string name, PtabCollection collection)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateDecisionOptional(value, "The ODP PTAB decision field is invalid.", collection);
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static void ValidateDecisionOptional(JsonFieldValue value, string message, PtabCollection collection)
    {
        if (!value.IsValid)
        {
            throw ParseDecisionFailure(message, value.Path, collection);
        }
    }

    internal static ParseException ParseDecisionFailure(string message, string path, PtabCollection collection)
    {
        string sourceName = "ODP PTAB trial decision";
        if (collection == PtabCollection.TrialDocuments)
        {
            sourceName = "ODP PTAB trial document";
        }
        else if (collection == PtabCollection.AppealDecisions)
        {
            sourceName = "ODP PTAB appeal decision";
        }
        else if (collection == PtabCollection.InterferenceDecisions)
        {
            sourceName = "ODP PTAB interference decision";
        }

        return new ParseException(message, DocumentFormat.Json, sourceName, path, 0L, false, 0, false, null);
    }

    internal static PtabFacet[] ParseFacets(JsonFieldReader root)
    {
        JsonFieldValue facets = root.ReadArray("facets");
        ValidateOptional(facets, "The ODP PTAB trial decision facets are invalid.");
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
                throw ParseFailure("The ODP PTAB trial decision facet entry is invalid.", itemPath);
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
            throw ParseFailure("The ODP PTAB trial decision facet values are invalid.", path);
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
                throw ParseFailure("The ODP PTAB trial decision facet name is invalid.", name.Path);
            }

            if (!quantity.IsPresent || !quantity.IsValid || quantity.IsNull || quantity.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB trial decision facet quantity is invalid.", quantity.Path);
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

    internal static void ApplyTotal(JsonFieldReader root, PtabTrialDecisionResponse result)
    {
        JsonFieldValue total = root.ReadInt64("total");
        if (!total.IsPresent)
        {
            total = root.ReadInt64("totalRecordCount");
        }

        ValidateOptional(total, "The ODP PTAB trial decision total is invalid.");
        if (total.IsPresent && !total.IsNull)
        {
            if (total.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB trial decision total cannot be negative.", total.Path);
            }

            result.Total = total.Int64Value;
            result.HasTotal = true;
        }
    }

    internal static PtabDataProvenance CreateProvenance(HttpResponseMetadata metadata, string? requestIdentifier)
    {
        PtabDataProvenance result = OdpPtabProceedingParser.CreateProvenance(metadata, requestIdentifier);
        result.Collection = PtabCollection.TrialDecisions;
        return result;
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, HttpResponseMetadata metadata)
    {
        JsonFieldValue value = root.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP PTAB trial decision request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return metadata.RequestIdentifier;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP PTAB trial decision field is invalid.");
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
        return new ParseException(message, DocumentFormat.Json, "ODP PTAB trial decision", path, 0L, false, 0, false, null);
    }
}
