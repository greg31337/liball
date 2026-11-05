namespace uspto;

internal class OfficeActionEnrichedBooleanValue
{
    internal bool Value = false;
    internal bool HasValue = false;
}

internal static class OfficeActionEnrichedCitationParser
{
    internal static OfficeActionEnrichedCitationResponse Parse(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An enriched Office Action citation response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("An enriched Office Action citation search request is required.", "request", "REQUIRED", null);
        }

        if (request.OffsetPage == null)
        {
            throw new ValidationException("Enriched Office Action citation page coordinates are required.", "OffsetPage", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("Enriched Office Action citation response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw Failure("The enriched Office Action citation response must be a JSON object.", "$");
            }

            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue envelope = root.ReadObject("response");
            if (!envelope.IsValid)
            {
                throw Failure("The enriched Office Action citation response envelope is invalid.", envelope.Path);
            }

            JsonFieldReader payload = root;
            bool hasEnvelope = false;
            if (envelope.IsPresent)
            {
                if (envelope.IsNull || !envelope.HasElementValue)
                {
                    throw Failure("The enriched Office Action citation response envelope is invalid.", envelope.Path);
                }

                payload = new JsonFieldReader(envelope.ElementValue, envelope.Path);
                hasEnvelope = true;
            }

            JsonFieldValue total = payload.ReadInt64("numFound");
            if (!total.IsPresent || !total.IsValid || total.IsNull || total.Int64Value < 0L)
            {
                throw Failure("The enriched Office Action citation response has an invalid total.", total.Path);
            }

            JsonFieldValue start = payload.ReadInt64("start");
            if (!start.IsPresent || !start.IsValid || start.IsNull || start.Int64Value < 0L)
            {
                throw Failure("The enriched Office Action citation response has an invalid start offset.", start.Path);
            }

            JsonFieldValue documents = payload.ReadArray("docs");
            if (!documents.IsPresent || !documents.IsValid || documents.IsNull)
            {
                throw Failure("The enriched Office Action citation response has an invalid document collection.", documents.Path);
            }

            if (documents.ArrayItems.Length != 0 && (start.Int64Value > total.Int64Value || documents.ArrayItems.Length > total.Int64Value - start.Int64Value))
            {
                throw Failure("The enriched Office Action citation page extends beyond the reported total.", documents.Path);
            }

            string? requestIdentifier = ResolveEnrichedRequestIdentifier(root, payload, hasEnvelope, responseMetadata);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            provenance.DatasetIdentifier = "enriched_cited_reference_metadata";
            provenance.DatasetVersion = "v3";
            OfficeActionEnrichedCitation[] citations = new OfficeActionEnrichedCitation[documents.ArrayItems.Length];
            int index;
            for (index = 0; index < documents.ArrayItems.Length; index += 1)
            {
                citations[index] = ParseEnrichedCitation(documents.ArrayItems[index], documents.ArrayItemPaths[index], provenance);
            }

            OfficeActionEnrichedCitationResponse result = new OfficeActionEnrichedCitationResponse();
            result.Request = request;
            result.Citations = citations;
            result.Count = citations.Length;
            result.Total = total.Int64Value;
            result.HasTotal = true;
            result.Start = start.Int64Value;
            result.Page.Offset = start.Int64Value;
            result.Page.Limit = request.OffsetPage.Limit;
            result.RequestIdentifier = requestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = start.Int64Value >= total.Int64Value || start.Int64Value + citations.Length >= total.Int64Value;
            CaptureEnrichedResponseUnknown(root, payload, hasEnvelope, result);
            return result;
        }
    }

    internal static OfficeActionEnrichedCitation ParseEnrichedCitation(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw Failure("The enriched Office Action citation record must be a JSON object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        OfficeActionEnrichedCitation result = new OfficeActionEnrichedCitation();
        result.Identifier = ReadEnrichedString(reader, "id");
        result.ApplicationNumber = ReadEnrichedString(reader, "patentApplicationNumber");
        result.CitedDocumentIdentifier = ReadEnrichedString(reader, "citedDocumentIdentifier");
        result.PublicationNumber = ReadEnrichedString(reader, "publicationNumber");
        result.KindCode = ReadEnrichedString(reader, "kindCode");
        result.CountryCode = ReadEnrichedString(reader, "countryCode");
        result.InventorNameText = ReadEnrichedString(reader, "inventorNameText");
        result.OfficeActionCategory = ReadEnrichedString(reader, "officeActionCategory");
        result.CitationCategoryCode = ReadEnrichedString(reader, "citationCategoryCode");
        result.RelatedClaimNumberText = ReadEnrichedString(reader, "relatedClaimNumberText");
        result.RelatedClaimNumbers = SplitRelatedClaims(result.RelatedClaimNumberText);
        result.WorkGroupNumber = ReadEnrichedString(reader, "workGroupNumber");
        result.GroupArtUnitNumber = ReadEnrichedString(reader, "groupArtUnitNumber");
        result.TechnologyCenter = ReadEnrichedString(reader, "techCenter");
        result.QualitySummaryText = ReadEnrichedString(reader, "qualitySummaryText");
        result.PassageLocations = ReadPassageLocations(reader);
        result.ObsoleteDocumentIdentifier = ReadEnrichedString(reader, "obsoleteDocumentIdentifier");
        result.CreateUserIdentifier = ReadEnrichedString(reader, "createUserIdentifier");
        ApplyEnrichedDates(reader, result);
        ApplyEnrichedIndicators(reader, result);
        result.CitedIdentifier = CitedIdentifierNormalizer.NormalizeCitedIdentifier(result.CitedDocumentIdentifier, null, result.PublicationNumber, result.KindCode, result.CountryCode, result.NonPatentLiteratureIndicator, result.HasNonPatentLiteratureIndicator);
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownEnrichedFields());
        MergeEnrichedUnknown(result.UnknownFields, unknown, string.Empty);
        return result;
    }

    internal static void ApplyEnrichedDates(JsonFieldReader reader, OfficeActionEnrichedCitation result)
    {
        JsonFieldValue officeAction = reader.ReadDate("officeActionDate");
        if (!officeAction.IsValid)
        {
            throw Failure("The enriched citation Office Action date is invalid.", officeAction.Path);
        }

        if (officeAction.IsPresent && !officeAction.IsNull)
        {
            result.OfficeActionDate = officeAction.DateValue;
            result.HasOfficeActionDate = true;
        }

        JsonFieldValue created = reader.ReadDate("createDateTime");
        if (!created.IsValid)
        {
            throw Failure("The enriched citation creation time is invalid.", created.Path);
        }

        if (created.IsPresent && !created.IsNull)
        {
            result.CreatedAt = created.DateValue;
            result.HasCreatedAt = true;
        }
    }

    internal static void ApplyEnrichedIndicators(JsonFieldReader reader, OfficeActionEnrichedCitation result)
    {
        OfficeActionEnrichedBooleanValue examiner = ReadEnrichedBoolean(reader, "examinerCitedReferenceIndicator");
        result.ExaminerCitedReferenceIndicator = examiner.Value;
        result.HasExaminerCitedReferenceIndicator = examiner.HasValue;
        OfficeActionEnrichedBooleanValue applicant = ReadEnrichedBoolean(reader, "applicantCitedExaminerReferenceIndicator");
        result.ApplicantCitedExaminerReferenceIndicator = applicant.Value;
        result.HasApplicantCitedExaminerReferenceIndicator = applicant.HasValue;
        OfficeActionEnrichedBooleanValue npl = ReadEnrichedBoolean(reader, "nplIndicator");
        result.NonPatentLiteratureIndicator = npl.Value;
        result.HasNonPatentLiteratureIndicator = npl.HasValue;
    }

    internal static string[] SplitRelatedClaims(string? text)
    {
        if (text == null)
        {
            return new string[0];
        }

        string[] parts = text.Split(',', global::System.StringSplitOptions.None);
        global::System.Collections.Generic.List<string> claims = new global::System.Collections.Generic.List<string>();
        int index;
        for (index = 0; index < parts.Length; index += 1)
        {
            string claim = parts[index].Trim();
            if (claim.Length != 0)
            {
                claims.Add(claim);
            }
        }

        return claims.ToArray();
    }

    internal static string[] ReadPassageLocations(JsonFieldReader reader)
    {
        JsonFieldValue value = reader.ReadRaw("passageLocationText");
        if (!value.IsPresent || value.IsNull)
        {
            return new string[0];
        }

        global::System.Collections.Generic.List<string> locations = new global::System.Collections.Generic.List<string>();
        if (value.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            int index = 0;
            foreach (global::System.Text.Json.JsonElement item in value.ElementValue.EnumerateArray())
            {
                AddPassageLocation(locations, item, value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]");
                index += 1;
            }
        }
        else
        {
            AddPassageLocation(locations, value.ElementValue, value.Path);
        }

        return locations.ToArray();
    }

    internal static void AddPassageLocation(global::System.Collections.Generic.List<string> locations, global::System.Text.Json.JsonElement item, string path)
    {
        string? text = null;
        if (item.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            text = item.GetString();
        }
        else if (item.ValueKind == global::System.Text.Json.JsonValueKind.Number || item.ValueKind == global::System.Text.Json.JsonValueKind.True || item.ValueKind == global::System.Text.Json.JsonValueKind.False)
        {
            text = item.GetRawText();
        }

        if (text == null)
        {
            throw Failure("An enriched citation passage location is invalid.", path);
        }

        locations.Add(text);
    }

    internal static string? ReadEnrichedString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        if (!value.IsValid)
        {
            throw Failure("An enriched Office Action citation field is invalid.", value.Path);
        }

        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static OfficeActionEnrichedBooleanValue ReadEnrichedBoolean(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadBoolean(name);
        if (!value.IsValid)
        {
            throw Failure("An enriched Office Action citation indicator is invalid.", value.Path);
        }

        OfficeActionEnrichedBooleanValue result = new OfficeActionEnrichedBooleanValue();
        if (value.IsPresent && !value.IsNull)
        {
            result.Value = value.BooleanValue;
            result.HasValue = true;
        }

        return result;
    }

    internal static string? ResolveEnrichedRequestIdentifier(JsonFieldReader root, JsonFieldReader payload, bool hasEnvelope, HttpResponseMetadata responseMetadata)
    {
        string? result = responseMetadata.RequestIdentifier;
        JsonFieldValue payloadIdentifier = payload.ReadString("requestIdentifier");
        if (!payloadIdentifier.IsValid)
        {
            throw Failure("The enriched Office Action citation request identifier is invalid.", payloadIdentifier.Path);
        }

        if (payloadIdentifier.IsPresent && !payloadIdentifier.IsNull)
        {
            result = payloadIdentifier.StringValue;
        }

        if (hasEnvelope)
        {
            JsonFieldValue rootIdentifier = root.ReadString("requestIdentifier");
            if (!rootIdentifier.IsValid)
            {
                throw Failure("The enriched Office Action citation request identifier is invalid.", rootIdentifier.Path);
            }

            if (rootIdentifier.IsPresent && !rootIdentifier.IsNull)
            {
                result = rootIdentifier.StringValue;
            }
        }

        return result;
    }

    internal static void CaptureEnrichedResponseUnknown(JsonFieldReader root, JsonFieldReader payload, bool hasEnvelope, OfficeActionEnrichedCitationResponse result)
    {
        JsonUnknownField[] payloadUnknown = payload.CaptureUnknownFields(new string[] { "numFound", "start", "docs", "requestIdentifier" });
        MergeEnrichedUnknown(result.UnknownFields, payloadUnknown, string.Empty);
        if (hasEnvelope)
        {
            JsonUnknownField[] envelopeUnknown = root.CaptureUnknownFields(new string[] { "response", "requestIdentifier" });
            MergeEnrichedUnknown(result.UnknownFields, envelopeUnknown, "envelope.");
        }
    }

    internal static void MergeEnrichedUnknown(global::System.Collections.Generic.Dictionary<string, string> destination, JsonUnknownField[] fields, string prefix)
    {
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            destination[prefix + fields[index].Name] = fields[index].RawJson;
        }
    }

    internal static string[] KnownEnrichedFields()
    {
        return new string[]
        {
            "id",
            "patentApplicationNumber",
            "citedDocumentIdentifier",
            "publicationNumber",
            "kindCode",
            "countryCode",
            "inventorNameText",
            "officeActionDate",
            "officeActionCategory",
            "citationCategoryCode",
            "relatedClaimNumberText",
            "examinerCitedReferenceIndicator",
            "applicantCitedExaminerReferenceIndicator",
            "nplIndicator",
            "workGroupNumber",
            "groupArtUnitNumber",
            "techCenter",
            "qualitySummaryText",
            "passageLocationText",
            "obsoleteDocumentIdentifier",
            "createUserIdentifier",
            "createDateTime"
        };
    }

    internal static ParseException Failure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "enriched Office Action citation search", path, 0L, false, 0, false, null);
    }
}
