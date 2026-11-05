namespace uspto;

internal static class OdpPtabAppealDecisionParser
{
    internal static PtabAppealDecisionResponse ParseSearch(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP PTAB appeal decision response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("The ODP PTAB appeal decision search request is required.", "request", "REQUIRED", null);
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
                throw ParseFailure("The ODP PTAB appeal decision response has an invalid count.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentAppealDataBag");
            if (!records.IsValid || records.IsNull || count.Int32Value != 0 && !records.IsPresent)
            {
                throw ParseFailure("The ODP PTAB appeal decision response has an invalid record collection.", records.Path);
            }

            if (count.Int32Value != records.ArrayItems.Length)
            {
                throw ParseFailure("The ODP PTAB appeal decision count does not match its records.", records.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, responseMetadata);
            PtabDataProvenance provenance = CreateProvenance(responseMetadata, requestIdentifier);
            PtabAppealDecision[] decisions = new PtabAppealDecision[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                decisions[index] = ParseRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PtabAppealDecisionResponse result = new PtabAppealDecisionResponse();
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

    internal static PtabAppealDecision ParseSingle(string json, string expectedDocumentIdentifier, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedDocumentIdentifier))
        {
            throw new ValidationException("An expected PTAB appeal decision document identifier is required.", "expectedDocumentIdentifier", "REQUIRED", null);
        }

        PtabAppealDecisionResponse response = ParseSearch(json, new SearchRequest(), responseMetadata, null);
        if (response.Count != 1)
        {
            throw ParseFailure("The ODP PTAB appeal decision response must contain exactly one record.", "$.count");
        }

        PtabAppealDecision result = response.Decisions[0];
        if (!global::System.String.Equals(result.Document.DocumentIdentifier, expectedDocumentIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw ParseFailure("The ODP PTAB appeal decision document identifier does not match the requested identifier.", "$.patentAppealDataBag[0].documentData.documentIdentifier");
        }

        return result;
    }

    internal static PtabAppealDecisionResponse ParseByAppeal(string json, string expectedAppealNumber, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedAppealNumber))
        {
            throw new ValidationException("An expected PTAB appeal number is required.", "expectedAppealNumber", "REQUIRED", null);
        }

        PtabAppealDecisionResponse result = ParseSearch(json, new SearchRequest(), responseMetadata, null);
        int index;
        for (index = 0; index < result.Decisions.Length; index += 1)
        {
            if (!global::System.String.Equals(result.Decisions[index].AppealNumber, expectedAppealNumber, global::System.StringComparison.OrdinalIgnoreCase))
            {
                string path = "$.patentAppealDataBag[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].appealNumber";
                throw ParseFailure("The ODP PTAB decision appeal number does not match the requested appeal.", path);
            }
        }

        return result;
    }

    internal static PtabAppealDecision ParseRecord(global::System.Text.Json.JsonElement element, string path, PtabDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP PTAB appeal decision record is not an object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PtabAppealDecision result = new PtabAppealDecision();
        result.AppealNumber = ReadOptionalString(reader, "appealNumber");
        result.RecordCategory = ReadOptionalString(reader, "appealDocumentCategory");
        ApplyRecordModifiedDate(reader, result);
        result.Metadata = ParseMetadata(reader);
        result.Appellant = ParseAppellant(reader);
        result.ThirdPartyRequester = ParseThirdPartyRequester(reader);
        result.Examiner = ParseExaminer(reader);
        result.Document = OdpPtabTrialDecisionParser.ParseDocument(reader, provenance, PtabCollection.AppealDecisions);
        result.Decision = OdpPtabTrialDecisionParser.ParseDecision(reader, PtabCollection.AppealDecisions);
        result.Provenance = OdpPtabProceedingParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "appealNumber", "lastModifiedDateTime", "appealDocumentCategory", "appealMetaData", "appellantData", "appelantData", "thirdPartyRequesterData", "requestorData", "examinerData", "documentData", "decisionData" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyRecordModifiedDate(JsonFieldReader reader, PtabAppealDecision result)
    {
        JsonFieldValue value = reader.ReadDate("lastModifiedDateTime");
        ValidateOptional(value, "The ODP PTAB appeal decision modification time is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            result.LastModifiedAt = value.DateValue;
            result.HasLastModifiedAt = true;
        }
    }

    internal static PtabAppealMetadata ParseMetadata(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadObject("appealMetaData");
        ValidateOptional(value, "The ODP PTAB appeal metadata is invalid.");
        PtabAppealMetadata result = new PtabAppealMetadata();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.ApplicationTypeCategory = ReadOptionalString(reader, "applicationTypeCategory");
        ApplyMetadataDates(reader, result);
        string? uriText = ReadOptionalAliasedString(reader, new string[] { "fileDownloadURI", "downloadURI" });
        if (uriText != null)
        {
            if (!global::System.Uri.IsWellFormedUriString(uriText, global::System.UriKind.RelativeOrAbsolute))
            {
                throw ParseFailure("The ODP PTAB appeal file URI is invalid.", OdpPatentDocumentParser.FindPresentPath(reader, new string[] { "fileDownloadURI", "downloadURI" }));
            }

            result.FileDownloadUri = new global::System.Uri(uriText, global::System.UriKind.RelativeOrAbsolute);
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "docketNoticeMailedDate", "appealFilingDate", "appealLastModifiedDate", "appealLastModifiedDateTime", "applicationTypeCategory", "fileDownloadURI", "downloadURI" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyMetadataDates(JsonFieldReader reader, PtabAppealMetadata result)
    {
        JsonFieldValue docket = reader.ReadDate("docketNoticeMailedDate");
        ValidateOptional(docket, "The ODP PTAB appeal docket notice date is invalid.");
        if (docket.IsPresent && !docket.IsNull)
        {
            result.DocketNoticeMailedDate = docket.DateValue;
            result.HasDocketNoticeMailedDate = true;
        }

        JsonFieldValue filing = reader.ReadDate("appealFilingDate");
        ValidateOptional(filing, "The ODP PTAB appeal filing date is invalid.");
        if (filing.IsPresent && !filing.IsNull)
        {
            result.FilingDate = filing.DateValue;
            result.HasFilingDate = true;
        }

        JsonFieldValue modifiedDate = reader.ReadDate("appealLastModifiedDate");
        ValidateOptional(modifiedDate, "The ODP PTAB appeal modification date is invalid.");
        if (modifiedDate.IsPresent && !modifiedDate.IsNull)
        {
            result.LastModifiedDate = modifiedDate.DateValue;
            result.HasLastModifiedDate = true;
        }

        JsonFieldValue modifiedAt = reader.ReadDate("appealLastModifiedDateTime");
        ValidateOptional(modifiedAt, "The ODP PTAB appeal modification time is invalid.");
        if (modifiedAt.IsPresent && !modifiedAt.IsNull)
        {
            result.LastModifiedAt = modifiedAt.DateValue;
            result.HasLastModifiedAt = true;
        }
    }

    internal static PtabAppellant ParseAppellant(JsonFieldReader record)
    {
        JsonFieldValue value = OdpPtabCompatibilityParser.ReadAliasedObject(record, new string[] { "appellantData", "appelantData" });
        ValidateOptional(value, "The ODP PTAB appellant data is invalid.");
        PtabAppellant result = new PtabAppellant();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.ApplicationNumber = ReadOptionalString(reader, "applicationNumberText");
        result.PatentNumber = ReadOptionalString(reader, "patentNumber");
        result.PublicationNumber = ReadOptionalString(reader, "publicationNumber");
        result.PatentOwnerName = ReadOptionalString(reader, "patentOwnerName");
        result.RealPartyInInterestName = ReadOptionalAliasedString(reader, new string[] { "realPartyInInterestName", "realPartyName" });
        result.InventorName = ReadOptionalString(reader, "inventorName");
        result.ArtUnit = ReadOptionalString(reader, "groupArtUnitNumber");
        result.TechnologyCenter = ReadOptionalAliasedString(reader, new string[] { "technologyCenterNumber", "techCenterNumber" });
        result.Counsel.Name = ReadOptionalString(reader, "counselName");
        ApplyAppellantDates(reader, result);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "applicationNumberText", "patentNumber", "publicationNumber", "patentOwnerName", "realPartyInInterestName", "realPartyName", "inventorName", "groupArtUnitNumber", "technologyCenterNumber", "techCenterNumber", "counselName", "publicationDate", "grantDate" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyAppellantDates(JsonFieldReader reader, PtabAppellant result)
    {
        JsonFieldValue publication = reader.ReadDate("publicationDate");
        ValidateOptional(publication, "The ODP PTAB appellant publication date is invalid.");
        if (publication.IsPresent && !publication.IsNull)
        {
            result.PublicationDate = publication.DateValue;
            result.HasPublicationDate = true;
        }

        JsonFieldValue grant = reader.ReadDate("grantDate");
        ValidateOptional(grant, "The ODP PTAB appellant grant date is invalid.");
        if (grant.IsPresent && !grant.IsNull)
        {
            result.GrantDate = grant.DateValue;
            result.HasGrantDate = true;
        }
    }

    internal static PtabThirdPartyRequester ParseThirdPartyRequester(JsonFieldReader record)
    {
        JsonFieldValue value = OdpPtabCompatibilityParser.ReadAliasedObject(record, new string[] { "thirdPartyRequesterData", "requestorData" });
        ValidateOptional(value, "The ODP PTAB third-party requester data is invalid.");
        PtabThirdPartyRequester result = new PtabThirdPartyRequester();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.Name = ReadOptionalString(reader, "thirdPartyName");
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "thirdPartyName" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static PtabAppealExaminer ParseExaminer(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadObject("examinerData");
        ValidateOptional(value, "The ODP PTAB examiner data is invalid.");
        PtabAppealExaminer result = new PtabAppealExaminer();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.Name = ReadOptionalString(reader, "examinerName");
        result.Identifier = ReadOptionalString(reader, "examinerIdentifier");
        result.ArtUnit = ReadOptionalString(reader, "groupArtUnitNumber");
        result.TechnologyCenter = ReadOptionalAliasedString(reader, new string[] { "technologyCenterNumber", "techCenterNumber" });
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "examinerName", "examinerIdentifier", "groupArtUnitNumber", "technologyCenterNumber", "techCenterNumber" });
        OdpPtabProceedingParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static PtabFacet[] ParseFacets(JsonFieldReader root)
    {
        JsonFieldValue facets = root.ReadArray("facets");
        ValidateOptional(facets, "The ODP PTAB appeal decision facets are invalid.");
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
                throw ParseFailure("The ODP PTAB appeal decision facet entry is invalid.", itemPath);
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
            throw ParseFailure("The ODP PTAB appeal decision facet values are invalid.", path);
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
                throw ParseFailure("The ODP PTAB appeal decision facet name is invalid.", name.Path);
            }

            if (!quantity.IsPresent || !quantity.IsValid || quantity.IsNull || quantity.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB appeal decision facet quantity is invalid.", quantity.Path);
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

    internal static void ApplyTotal(JsonFieldReader root, PtabAppealDecisionResponse result)
    {
        JsonFieldValue total = root.ReadInt64("total");
        if (!total.IsPresent)
        {
            total = root.ReadInt64("totalRecordCount");
        }

        ValidateOptional(total, "The ODP PTAB appeal decision total is invalid.");
        if (total.IsPresent && !total.IsNull)
        {
            if (total.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB appeal decision total cannot be negative.", total.Path);
            }

            result.Total = total.Int64Value;
            result.HasTotal = true;
        }
    }

    internal static PtabDataProvenance CreateProvenance(HttpResponseMetadata metadata, string? requestIdentifier)
    {
        PtabDataProvenance result = OdpPtabProceedingParser.CreateProvenance(metadata, requestIdentifier);
        result.Collection = PtabCollection.AppealDecisions;
        return result;
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, HttpResponseMetadata metadata)
    {
        JsonFieldValue value = root.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP PTAB appeal decision request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return metadata.RequestIdentifier;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP PTAB appeal decision field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string? ReadOptionalAliasedString(JsonFieldReader reader, string[] names)
    {
        JsonFieldValue value = OdpPtabCompatibilityParser.ReadAliasedString(reader, names);
        ValidateOptional(value, "The ODP PTAB appeal decision field is invalid.");
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
        return new ParseException(message, DocumentFormat.Json, "ODP PTAB appeal decision", path, 0L, false, 0, false, null);
    }
}
