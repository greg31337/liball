namespace uspto;

internal class PtabInterferenceDocumentFields
{
    internal PtabDocument Document = new PtabDocument();
    internal PtabDecision Decision = new PtabDecision();
}

internal static class OdpPtabInterferenceDecisionParser
{
    internal static PtabInterferenceDecisionResponse ParseSearch(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP PTAB interference decision response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("The ODP PTAB interference decision search request is required.", "request", "REQUIRED", null);
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
                throw ParseFailure("The ODP PTAB interference decision response has an invalid count.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentInterferenceDataBag");
            if (!records.IsValid || records.IsNull || count.Int32Value != 0 && !records.IsPresent)
            {
                throw ParseFailure("The ODP PTAB interference decision response has an invalid record collection.", records.Path);
            }

            if (count.Int32Value != records.ArrayItems.Length)
            {
                throw ParseFailure("The ODP PTAB interference decision count does not match its records.", records.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, responseMetadata);
            PtabDataProvenance provenance = CreateProvenance(responseMetadata, requestIdentifier);
            PtabInterferenceDecision[] decisions = new PtabInterferenceDecision[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                decisions[index] = ParseRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PtabInterferenceDecisionResponse result = new PtabInterferenceDecisionResponse();
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

    internal static PtabInterferenceDecision ParseSingle(string json, string expectedDocumentIdentifier, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedDocumentIdentifier))
        {
            throw new ValidationException("An expected PTAB interference decision document identifier is required.", "expectedDocumentIdentifier", "REQUIRED", null);
        }

        PtabInterferenceDecisionResponse response = ParseSearch(json, new SearchRequest(), responseMetadata, null);
        if (response.Count != 1)
        {
            throw ParseFailure("The ODP PTAB interference decision response must contain exactly one record.", "$.count");
        }

        PtabInterferenceDecision result = response.Decisions[0];
        if (!global::System.String.Equals(result.Document.DocumentIdentifier, expectedDocumentIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw ParseFailure("The ODP PTAB interference decision document identifier does not match the requested identifier.", "$.patentInterferenceDataBag[0].decisionDocumentData.documentIdentifier");
        }

        return result;
    }

    internal static PtabInterferenceDecisionResponse ParseByInterference(string json, string expectedInterferenceNumber, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedInterferenceNumber))
        {
            throw new ValidationException("An expected PTAB interference number is required.", "expectedInterferenceNumber", "REQUIRED", null);
        }

        PtabInterferenceDecisionResponse result = ParseSearch(json, new SearchRequest(), responseMetadata, null);
        int index;
        for (index = 0; index < result.Decisions.Length; index += 1)
        {
            if (!global::System.String.Equals(result.Decisions[index].InterferenceNumber, expectedInterferenceNumber, global::System.StringComparison.OrdinalIgnoreCase))
            {
                string path = "$.patentInterferenceDataBag[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].interferenceNumber";
                throw ParseFailure("The ODP PTAB decision interference number does not match the requested interference.", path);
            }
        }

        return result;
    }

    internal static PtabInterferenceDecision ParseRecord(global::System.Text.Json.JsonElement element, string path, PtabDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP PTAB interference decision record is not an object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PtabInterferenceDecision result = new PtabInterferenceDecision();
        result.InterferenceNumber = ReadOptionalString(reader, "interferenceNumber");
        result.RecordCategory = ReadOptionalString(reader, "interferenceDocumentCategory");
        ApplyRecordModifiedDate(reader, result);
        result.Metadata = ParseMetadata(reader);
        result.SeniorParty = ParseParty(reader, "seniorPartyData", "Senior");
        result.JuniorParty = ParseParty(reader, "juniorPartyData", "Junior");
        result.AdditionalParties = ParseAdditionalParties(reader);
        PtabInterferenceDocumentFields fields = ParseDecisionDocument(reader, provenance);
        result.Document = fields.Document;
        result.Decision = fields.Decision;
        result.Provenance = OdpPtabProceedingParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "interferenceNumber", "interferenceDocumentCategory", "lastModifiedDateTime", "interferenceMetaData", "seniorPartyData", "juniorPartyData", "additionalPartyDataBag", "decisionDocumentData", "documentData" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyRecordModifiedDate(JsonFieldReader reader, PtabInterferenceDecision result)
    {
        JsonFieldValue value = reader.ReadDate("lastModifiedDateTime");
        ValidateOptional(value, "The ODP PTAB interference decision modification time is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            result.LastModifiedAt = value.DateValue;
            result.HasLastModifiedAt = true;
        }
    }

    internal static PtabInterferenceMetadata ParseMetadata(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadObject("interferenceMetaData");
        ValidateOptional(value, "The ODP PTAB interference metadata is invalid.");
        PtabInterferenceMetadata result = new PtabInterferenceMetadata();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.StyleName = ReadOptionalString(reader, "interferenceStyleName");
        JsonFieldValue modified = reader.ReadDate("interferenceLastModifiedDate");
        ValidateOptional(modified, "The ODP PTAB interference modification date is invalid.");
        if (modified.IsPresent && !modified.IsNull)
        {
            result.LastModifiedDate = modified.DateValue;
            result.HasLastModifiedDate = true;
        }

        string? uriText = ReadOptionalAliasedString(reader, new string[] { "fileDownloadURI", "downloadURI" });
        if (uriText != null)
        {
            if (!global::System.Uri.IsWellFormedUriString(uriText, global::System.UriKind.RelativeOrAbsolute))
            {
                throw ParseFailure("The ODP PTAB interference file URI is invalid.", OdpPatentDocumentParser.FindPresentPath(reader, new string[] { "fileDownloadURI", "downloadURI" }));
            }

            result.FileDownloadUri = new global::System.Uri(uriText, global::System.UriKind.RelativeOrAbsolute);
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "interferenceStyleName", "interferenceLastModifiedDate", "fileDownloadURI", "downloadURI" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static PtabInterferenceParty ParseParty(JsonFieldReader record, string fieldName, string category)
    {
        JsonFieldValue value = record.ReadObject(fieldName);
        ValidateOptional(value, "The ODP PTAB interference party data is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            PtabInterferenceParty empty = new PtabInterferenceParty();
            empty.Category = category;
            return empty;
        }

        return ParsePartyElement(value.ElementValue, value.Path, category);
    }

    internal static PtabInterferenceParty[] ParseAdditionalParties(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadArray("additionalPartyDataBag");
        ValidateOptional(value, "The ODP PTAB additional party collection is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return new PtabInterferenceParty[0];
        }

        PtabInterferenceParty[] result = new PtabInterferenceParty[value.ArrayItems.Length];
        int index;
        for (index = 0; index < value.ArrayItems.Length; index += 1)
        {
            result[index] = ParsePartyElement(value.ArrayItems[index], value.ArrayItemPaths[index], "Additional");
        }

        return result;
    }

    internal static PtabInterferenceParty ParsePartyElement(global::System.Text.Json.JsonElement element, string path, string category)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP PTAB interference party entry is invalid.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PtabInterferenceParty result = new PtabInterferenceParty();
        result.Category = category;
        result.Name = ReadOptionalString(reader, "additionalPartyName");
        result.ApplicationNumber = ReadOptionalString(reader, "applicationNumberText");
        result.PatentNumber = ReadOptionalString(reader, "patentNumber");
        result.PublicationNumber = ReadOptionalString(reader, "publicationNumber");
        result.PatentOwnerName = ReadOptionalString(reader, "patentOwnerName");
        result.RealPartyInInterestName = ReadOptionalAliasedString(reader, new string[] { "realPartyInInterestName", "realPartyName" });
        result.InventorName = ReadOptionalString(reader, "inventorName");
        result.ArtUnit = ReadOptionalString(reader, "groupArtUnitNumber");
        result.TechnologyCenter = ReadOptionalAliasedString(reader, new string[] { "technologyCenterNumber", "techCenterNumber" });
        result.Counsel.Name = ReadOptionalString(reader, "counselName");
        ApplyPartyDates(reader, result);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "additionalPartyName", "applicationNumberText", "patentNumber", "publicationNumber", "patentOwnerName", "realPartyInInterestName", "realPartyName", "inventorName", "groupArtUnitNumber", "technologyCenterNumber", "techCenterNumber", "counselName", "publicationDate", "grantDate" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyPartyDates(JsonFieldReader reader, PtabInterferenceParty result)
    {
        JsonFieldValue publication = reader.ReadDate("publicationDate");
        ValidateOptional(publication, "The ODP PTAB interference party publication date is invalid.");
        if (publication.IsPresent && !publication.IsNull)
        {
            result.PublicationDate = publication.DateValue;
            result.HasPublicationDate = true;
        }

        JsonFieldValue grant = reader.ReadDate("grantDate");
        ValidateOptional(grant, "The ODP PTAB interference party grant date is invalid.");
        if (grant.IsPresent && !grant.IsNull)
        {
            result.GrantDate = grant.DateValue;
            result.HasGrantDate = true;
        }
    }

    internal static PtabInterferenceDocumentFields ParseDecisionDocument(JsonFieldReader record, PtabDataProvenance provenance)
    {
        JsonFieldValue value = OdpPtabCompatibilityParser.ReadAliasedObject(record, new string[] { "decisionDocumentData", "documentData" });
        ValidateOptional(value, "The ODP PTAB interference decision document data is invalid.");
        PtabInterferenceDocumentFields result = new PtabInterferenceDocumentFields();
        result.Document.Provenance = OdpPtabProceedingParser.CopyProvenance(provenance);
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.Document.DocumentIdentifier = ReadOptionalString(reader, "documentIdentifier");
        result.Document.Name = ReadOptionalString(reader, "documentName");
        result.Document.Title = ReadOptionalString(reader, "documentTitleText");
        result.Document.TypeDescription = ReadOptionalAliasedString(reader, new string[] { "documentTypeDescriptionText", "documentTypeCategory" });
        result.Document.MimeTypeIdentifier = ReadOptionalString(reader, "mimeTypeIdentifier");
        result.Document.OcrText = ReadOptionalString(reader, "documentOCRText");
        ApplyDecisionDocumentValues(reader, result.Document);
        OdpPtabTrialDecisionParser.ApplyDocumentFormat(result.Document);
        result.Decision.TypeCategory = ReadOptionalString(reader, "decisionTypeCategory");
        result.Decision.InterferenceOutcomeCategory = ReadOptionalString(reader, "interferenceOutcomeCategory");
        result.Decision.StatutesAndRules = OdpPtabTrialDecisionParser.ReadStringArray(reader, "statuteAndRuleBag", PtabCollection.InterferenceDecisions);
        result.Decision.IssueTypes = OdpPtabTrialDecisionParser.ReadStringArray(reader, "issueTypeBag", PtabCollection.InterferenceDecisions);
        JsonFieldValue issueDate = reader.ReadDate("decisionIssueDate");
        ValidateOptional(issueDate, "The ODP PTAB interference decision issue date is invalid.");
        if (issueDate.IsPresent && !issueDate.IsNull)
        {
            result.Decision.IssueDate = issueDate.DateValue;
            result.Decision.HasIssueDate = true;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "documentFilingDate", "documentIdentifier", "documentName", "documentSizeQuantity", "documentOCRText", "documentTitleText", "documentTypeDescriptionText", "documentTypeCategory", "mimeTypeIdentifier", "fileDownloadURI", "downloadURI", "statuteAndRuleBag", "decisionIssueDate", "decisionTypeCategory", "issueTypeBag", "interferenceOutcomeCategory" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.Document.UnknownFields);
        return result;
    }

    internal static void ApplyDecisionDocumentValues(JsonFieldReader reader, PtabDocument result)
    {
        JsonFieldValue size = reader.ReadInt64("documentSizeQuantity");
        ValidateOptional(size, "The ODP PTAB interference document size is invalid.");
        if (size.IsPresent && !size.IsNull)
        {
            if (size.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB interference document size cannot be negative.", size.Path);
            }

            result.Size = size.Int64Value;
            result.HasSize = true;
        }

        JsonFieldValue filingDate = reader.ReadDate("documentFilingDate");
        ValidateOptional(filingDate, "The ODP PTAB interference document filing date is invalid.");
        if (filingDate.IsPresent && !filingDate.IsNull)
        {
            result.FilingDate = filingDate.DateValue;
            result.HasFilingDate = true;
        }

        string? uriText = ReadOptionalAliasedString(reader, new string[] { "fileDownloadURI", "downloadURI" });
        if (uriText != null)
        {
            if (!global::System.Uri.IsWellFormedUriString(uriText, global::System.UriKind.Absolute))
            {
                throw ParseFailure("The ODP PTAB interference document download URI is invalid.", OdpPatentDocumentParser.FindPresentPath(reader, new string[] { "fileDownloadURI", "downloadURI" }));
            }

            result.DownloadUri = new global::System.Uri(uriText, global::System.UriKind.Absolute);
        }
    }

    internal static PtabFacet[] ParseFacets(JsonFieldReader root)
    {
        JsonFieldValue facets = root.ReadArray("facets");
        ValidateOptional(facets, "The ODP PTAB interference decision facets are invalid.");
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
                throw ParseFailure("The ODP PTAB interference decision facet entry is invalid.", itemPath);
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
            throw ParseFailure("The ODP PTAB interference decision facet values are invalid.", path);
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
                throw ParseFailure("The ODP PTAB interference decision facet name is invalid.", name.Path);
            }

            if (!quantity.IsPresent || !quantity.IsValid || quantity.IsNull || quantity.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB interference decision facet quantity is invalid.", quantity.Path);
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

    internal static void ApplyTotal(JsonFieldReader root, PtabInterferenceDecisionResponse result)
    {
        JsonFieldValue total = root.ReadInt64("total");
        if (!total.IsPresent)
        {
            total = root.ReadInt64("totalRecordCount");
        }

        ValidateOptional(total, "The ODP PTAB interference decision total is invalid.");
        if (total.IsPresent && !total.IsNull)
        {
            if (total.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB interference decision total cannot be negative.", total.Path);
            }

            result.Total = total.Int64Value;
            result.HasTotal = true;
        }
    }

    internal static PtabDataProvenance CreateProvenance(HttpResponseMetadata metadata, string? requestIdentifier)
    {
        PtabDataProvenance result = OdpPtabProceedingParser.CreateProvenance(metadata, requestIdentifier);
        result.Collection = PtabCollection.InterferenceDecisions;
        return result;
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, HttpResponseMetadata metadata)
    {
        JsonFieldValue value = root.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP PTAB interference decision request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return metadata.RequestIdentifier;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP PTAB interference decision field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string? ReadOptionalAliasedString(JsonFieldReader reader, string[] names)
    {
        JsonFieldValue value = OdpPtabCompatibilityParser.ReadAliasedString(reader, names);
        ValidateOptional(value, "The ODP PTAB interference decision field is invalid.");
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
        return new ParseException(message, DocumentFormat.Json, "ODP PTAB interference decision", path, 0L, false, 0, false, null);
    }
}
