namespace uspto;

internal static class OdpPetitionDecisionParser
{
    internal static PetitionDecisionResponse ParseSearch(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP petition decision search response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("The ODP petition decision search request is required.", "request", "REQUIRED", null);
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
                throw ParseFailure("The ODP petition decision search response has an invalid count.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("petitionDecisionDataBag");
            if (!records.IsValid || records.IsNull || count.Int32Value != 0 && !records.IsPresent)
            {
                throw ParseFailure("The ODP petition decision search response has an invalid record collection.", records.Path);
            }

            if (count.Int32Value != records.ArrayItems.Length)
            {
                throw ParseFailure("The ODP petition decision search count does not match its records.", records.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, responseMetadata);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            PetitionDecision[] decisions = new PetitionDecision[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                decisions[index] = ParseDecision(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PetitionDecisionResponse result = new PetitionDecisionResponse();
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

    internal static PetitionDecision ParseSingle(string json, string expectedRecordIdentifier, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP petition decision response is required.", "json", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(expectedRecordIdentifier))
        {
            throw new ValidationException("An expected petition decision record identifier is required.", "expectedRecordIdentifier", "REQUIRED", null);
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
                throw ParseFailure("The ODP petition decision response must contain exactly one record.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("petitionDecisionDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw ParseFailure("The ODP petition decision response has an invalid record collection.", records.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, responseMetadata);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            PetitionDecision result = ParseDecision(records.ArrayItems[0], records.ArrayItemPaths[0], provenance);
            if (!global::System.String.Equals(result.RecordIdentifier, expectedRecordIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
            {
                throw ParseFailure("The ODP petition decision record identifier does not match the requested identifier.", records.ArrayItemPaths[0] + ".petitionDecisionRecordIdentifier");
            }

            return result;
        }
    }

    internal static PetitionDecision ParseDecision(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP petition decision record is not an object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PetitionDecision result = new PetitionDecision();
        result.RecordIdentifier = ReadOptionalString(reader, "petitionDecisionRecordIdentifier");
        result.ApplicationNumber = ReadOptionalString(reader, "applicationNumberText");
        result.PatentNumber = ReadOptionalString(reader, "patentNumber");
        result.FirstApplicantName = ReadOptionalString(reader, "firstApplicantName");
        result.InventionTitle = ReadOptionalString(reader, "inventionTitle");
        result.CustomerNumber = ReadOptionalString(reader, "customerNumber");
        result.BusinessEntityStatusCategory = ReadOptionalString(reader, "businessEntityStatusCategory");
        result.DecisionTypeCode = ReadOptionalString(reader, "decisionTypeCode");
        result.PetitionTypeDescription = ReadOptionalString(reader, "decisionPetitionTypeCodeDescriptionText");
        result.DecisionTypeDescription = ReadOptionalString(reader, "decisionTypeCodeDescriptionText");
        result.FinalDecidingOfficeName = ReadOptionalString(reader, "finalDecidingOfficeName");
        result.ArtUnit = ReadOptionalString(reader, "groupArtUnitNumber");
        result.TechnologyCenter = ReadOptionalString(reader, "technologyCenter");
        result.CourtActionName = ReadOptionalString(reader, "actionTakenByCourtName");
        result.ProsecutionStatusDescription = ReadOptionalString(reader, "prosecutionStatusCodeDescriptionText");
        ApplyDates(reader, result);
        ApplyOptionalValues(reader, result);
        result.Inventors = ReadTextArray(reader, "inventorBag", "inventorNameText");
        result.IssuesConsidered = ReadTextArray(reader, "petitionIssueConsideredTextBag", "petitionIssueConsideredText");
        result.Rules = ReadTextArray(reader, "ruleBag", "ruleText");
        result.Statutes = ReadTextArray(reader, "statuteBag", "statuteText");
        result.Documents = ParseDocuments(reader);
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        CaptureUnknownDecisionFields(reader, result);
        return result;
    }

    internal static void ApplyDates(JsonFieldReader reader, PetitionDecision result)
    {
        JsonFieldValue decisionDate = reader.ReadDate("decisionDate");
        ValidateOptional(decisionDate, "The ODP petition decision date is invalid.");
        if (decisionDate.IsPresent && !decisionDate.IsNull)
        {
            result.DecisionDate = decisionDate.DateValue;
            result.HasDecisionDate = true;
        }

        JsonFieldValue decisionMailDate = reader.ReadDate("decisionMailDate");
        ValidateOptional(decisionMailDate, "The ODP petition decision mailing date is invalid.");
        if (decisionMailDate.IsPresent && !decisionMailDate.IsNull)
        {
            result.DecisionMailDate = decisionMailDate.DateValue;
            result.HasDecisionMailDate = true;
        }

        JsonFieldValue petitionMailDate = reader.ReadDate("petitionMailDate");
        ValidateOptional(petitionMailDate, "The ODP petition mailing date is invalid.");
        if (petitionMailDate.IsPresent && !petitionMailDate.IsNull)
        {
            result.PetitionMailDate = petitionMailDate.DateValue;
            result.HasPetitionMailDate = true;
        }

        JsonFieldValue lastIngested = reader.ReadDate("lastIngestionDateTime");
        ValidateOptional(lastIngested, "The ODP petition decision ingestion time is invalid.");
        if (lastIngested.IsPresent && !lastIngested.IsNull)
        {
            result.LastIngestedAt = lastIngested.DateValue;
            result.HasLastIngestedAt = true;
        }
    }

    internal static void ApplyOptionalValues(JsonFieldReader reader, PetitionDecision result)
    {
        JsonFieldValue petitionType = reader.ReadInt32("decisionPetitionTypeCode");
        ValidateOptional(petitionType, "The ODP petition type code is invalid.");
        if (petitionType.IsPresent && !petitionType.IsNull)
        {
            result.PetitionTypeCode = petitionType.Int32Value;
            result.HasPetitionTypeCode = true;
        }

        JsonFieldValue firstInventor = reader.ReadBoolean("firstInventorToFileIndicator");
        ValidateOptional(firstInventor, "The ODP petition first-inventor-to-file indicator is invalid.");
        if (firstInventor.IsPresent && !firstInventor.IsNull)
        {
            result.IsFirstInventorToFile = firstInventor.BooleanValue;
            result.HasIsFirstInventorToFile = true;
        }

        JsonFieldValue courtAction = reader.ReadBoolean("courtActionIndicator");
        ValidateOptional(courtAction, "The ODP petition court-action indicator is invalid.");
        if (courtAction.IsPresent && !courtAction.IsNull)
        {
            result.CourtActionIndicator = courtAction.BooleanValue;
            result.HasCourtActionIndicator = true;
        }
    }

    internal static string[] ReadTextArray(JsonFieldReader reader, string name, string itemName)
    {
        JsonFieldValue values = reader.ReadArray(name);
        ValidateOptional(values, "The ODP petition decision text collection is invalid.");
        if (!values.IsPresent || values.IsNull)
        {
            return new string[0];
        }

        string[] result = new string[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement item = values.ArrayItems[index];
            if (item.ValueKind == global::System.Text.Json.JsonValueKind.String)
            {
                result[index] = item.GetString() ?? string.Empty;
            }
            else if (item.ValueKind == global::System.Text.Json.JsonValueKind.Number || item.ValueKind == global::System.Text.Json.JsonValueKind.True || item.ValueKind == global::System.Text.Json.JsonValueKind.False)
            {
                result[index] = item.GetRawText();
            }
            else if (item.ValueKind == global::System.Text.Json.JsonValueKind.Object)
            {
                JsonFieldReader itemReader = new JsonFieldReader(item, values.ArrayItemPaths[index]);
                string? text = ReadOptionalString(itemReader, itemName);
                if (text == null)
                {
                    result[index] = item.GetRawText();
                }
                else
                {
                    result[index] = text;
                }
            }
            else
            {
                throw ParseFailure("The ODP petition decision text entry is invalid.", values.ArrayItemPaths[index]);
            }
        }

        return result;
    }

    internal static PetitionDecisionDocument[] ParseDocuments(JsonFieldReader reader)
    {
        JsonFieldValue documents = reader.ReadArray("documentBag");
        ValidateOptional(documents, "The ODP petition decision document collection is invalid.");
        if (!documents.IsPresent || documents.IsNull)
        {
            return new PetitionDecisionDocument[0];
        }

        PetitionDecisionDocument[] result = new PetitionDecisionDocument[documents.ArrayItems.Length];
        int index;
        for (index = 0; index < documents.ArrayItems.Length; index += 1)
        {
            result[index] = ParseDocument(documents.ArrayItems[index], documents.ArrayItemPaths[index]);
        }

        return result;
    }

    internal static PetitionDecisionDocument ParseDocument(global::System.Text.Json.JsonElement element, string path)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP petition decision document is invalid.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PetitionDecisionDocument result = new PetitionDecisionDocument();
        result.ApplicationNumber = ReadOptionalString(reader, "applicationNumberText");
        result.DocumentIdentifier = ReadOptionalString(reader, "documentIdentifier");
        result.Code = ReadOptionalString(reader, "documentCode");
        result.Description = ReadOptionalString(reader, "documentCodeDescriptionText");
        result.DirectionCategory = ReadOptionalString(reader, "documentDirectionCategory");
        JsonFieldValue officialDate = reader.ReadDate("officialDate");
        ValidateOptional(officialDate, "The ODP petition decision document date is invalid.");
        if (officialDate.IsPresent && !officialDate.IsNull)
        {
            result.OfficialDate = officialDate.DateValue;
            result.HasOfficialDate = true;
        }

        result.DownloadAlternatives = ParseDownloadAlternatives(reader);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "applicationNumberText", "documentIdentifier", "documentCode", "documentCodeDescriptionText", "documentDirectionCategory", "officialDate", "downloadOptionBag" });
        CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static PetitionDocumentDownloadAlternative[] ParseDownloadAlternatives(JsonFieldReader reader)
    {
        JsonFieldValue alternatives = reader.ReadArray("downloadOptionBag");
        ValidateOptional(alternatives, "The ODP petition decision download options are invalid.");
        if (!alternatives.IsPresent || alternatives.IsNull)
        {
            return new PetitionDocumentDownloadAlternative[0];
        }

        PetitionDocumentDownloadAlternative[] result = new PetitionDocumentDownloadAlternative[alternatives.ArrayItems.Length];
        int index;
        for (index = 0; index < alternatives.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement element = alternatives.ArrayItems[index];
            string path = alternatives.ArrayItemPaths[index];
            if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ParseFailure("The ODP petition decision download option is invalid.", path);
            }

            JsonFieldReader optionReader = new JsonFieldReader(element, path);
            PetitionDocumentDownloadAlternative option = new PetitionDocumentDownloadAlternative();
            option.MimeTypeIdentifier = ReadOptionalString(optionReader, "mimeTypeIdentifier");
            ApplyDownloadFormat(option);
            string? uriText = ReadOptionalString(optionReader, "documentURI");
            if (uriText != null)
            {
                if (!global::System.Uri.IsWellFormedUriString(uriText, global::System.UriKind.Absolute))
                {
                    throw ParseFailure("The ODP petition decision document URI is invalid.", path + ".documentURI");
                }

                option.DownloadUri = new global::System.Uri(uriText, global::System.UriKind.Absolute);
            }

            JsonFieldValue pageCount = optionReader.ReadInt32("pageTotalQuantity");
            ValidateOptional(pageCount, "The ODP petition decision document page count is invalid.");
            if (pageCount.IsPresent && !pageCount.IsNull)
            {
                if (pageCount.Int32Value < 0)
                {
                    throw ParseFailure("The ODP petition decision document page count cannot be negative.", pageCount.Path);
                }

                option.PageCount = pageCount.Int32Value;
                option.HasPageCount = true;
            }

            JsonUnknownField[] unknown = optionReader.CaptureUnknownFields(new string[] { "mimeTypeIdentifier", "documentURI", "pageTotalQuantity" });
            CopyUnknownFields(unknown, option.UnknownFields);
            result[index] = option;
        }

        return result;
    }

    internal static void ApplyDownloadFormat(PetitionDocumentDownloadAlternative option)
    {
        string identifier = option.MimeTypeIdentifier ?? string.Empty;
        string? mediaType = OdpPatentDocumentParser.NormalizeMediaType(identifier);
        if (mediaType != null)
        {
            option.MediaType = mediaType;
            option.Format = ResponseReader.DetectDocumentFormat(mediaType);
        }
    }

    internal static PetitionDecisionFacet[] ParseFacets(JsonFieldReader root)
    {
        JsonFieldValue facets = root.ReadArray("facets");
        ValidateOptional(facets, "The ODP petition decision facets are invalid.");
        if (!facets.IsPresent || facets.IsNull)
        {
            return new PetitionDecisionFacet[0];
        }

        global::System.Collections.Generic.List<PetitionDecisionFacet> results = new global::System.Collections.Generic.List<PetitionDecisionFacet>();
        int index;
        for (index = 0; index < facets.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement item = facets.ArrayItems[index];
            string itemPath = facets.ArrayItemPaths[index];
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ParseFailure("The ODP petition decision facet entry is invalid.", itemPath);
            }

            foreach (global::System.Text.Json.JsonProperty property in item.EnumerateObject())
            {
                string propertyPath = JsonFieldReader.AppendPropertyPath(itemPath, property.Name);
                results.Add(ParseFacet(property.Name, property.Value, propertyPath));
            }
        }

        return results.ToArray();
    }

    internal static PetitionDecisionFacet ParseFacet(string field, global::System.Text.Json.JsonElement element, string path)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw ParseFailure("The ODP petition decision facet values are invalid.", path);
        }

        global::System.Collections.Generic.List<PetitionDecisionFacetValue> values = new global::System.Collections.Generic.List<PetitionDecisionFacetValue>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in element.EnumerateArray())
        {
            string itemPath = path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            JsonFieldReader itemReader = new JsonFieldReader(item, itemPath);
            JsonFieldValue value = itemReader.ReadString("value");
            JsonFieldValue count = itemReader.ReadInt64("count");
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object || !value.IsPresent || !value.IsValid || value.IsNull)
            {
                throw ParseFailure("The ODP petition decision facet value is invalid.", value.Path);
            }

            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int64Value < 0L)
            {
                throw ParseFailure("The ODP petition decision facet count is invalid.", count.Path);
            }

            PetitionDecisionFacetValue facetValue = new PetitionDecisionFacetValue();
            facetValue.Value = value.StringValue;
            facetValue.Count = count.Int64Value;
            values.Add(facetValue);
            index += 1;
        }

        PetitionDecisionFacet result = new PetitionDecisionFacet();
        result.Field = field;
        result.Values = values.ToArray();
        return result;
    }

    internal static void ApplyTotal(JsonFieldReader root, PetitionDecisionResponse result)
    {
        JsonFieldValue total = root.ReadInt64("total");
        if (!total.IsPresent)
        {
            total = root.ReadInt64("totalRecordCount");
        }

        ValidateOptional(total, "The ODP petition decision total is invalid.");
        if (total.IsPresent && !total.IsNull)
        {
            if (total.Int64Value < 0L)
            {
                throw ParseFailure("The ODP petition decision total cannot be negative.", total.Path);
            }

            result.Total = total.Int64Value;
            result.HasTotal = true;
        }
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, HttpResponseMetadata responseMetadata)
    {
        JsonFieldValue requestIdentifier = root.ReadString("requestIdentifier");
        ValidateOptional(requestIdentifier, "The ODP petition decision request identifier is invalid.");
        if (requestIdentifier.IsPresent && !requestIdentifier.IsNull)
        {
            return requestIdentifier.StringValue;
        }

        return responseMetadata.RequestIdentifier;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP petition decision field is invalid.");
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

    internal static void CaptureUnknownDecisionFields(JsonFieldReader reader, PetitionDecision result)
    {
        string[] knownNames = new string[]
        {
            "petitionDecisionRecordIdentifier",
            "applicationNumberText",
            "patentNumber",
            "firstApplicantName",
            "inventionTitle",
            "customerNumber",
            "businessEntityStatusCategory",
            "decisionDate",
            "decisionMailDate",
            "petitionMailDate",
            "decisionPetitionTypeCode",
            "decisionTypeCode",
            "decisionPetitionTypeCodeDescriptionText",
            "decisionTypeCodeDescriptionText",
            "finalDecidingOfficeName",
            "firstInventorToFileIndicator",
            "groupArtUnitNumber",
            "technologyCenter",
            "inventorBag",
            "actionTakenByCourtName",
            "courtActionIndicator",
            "petitionIssueConsideredTextBag",
            "prosecutionStatusCodeDescriptionText",
            "ruleBag",
            "statuteBag",
            "lastIngestionDateTime",
            "documentBag"
        };
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(knownNames);
        CopyUnknownFields(unknown, result.UnknownFields);
    }

    internal static void CopyUnknownFields(JsonUnknownField[] source, global::System.Collections.Generic.Dictionary<string, string> destination)
    {
        int index;
        for (index = 0; index < source.Length; index += 1)
        {
            destination[source[index].Name] = source[index].RawJson;
        }
    }

    internal static ParseException ParseFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP petition decision", path, 0L, false, 0, false, null);
    }
}
