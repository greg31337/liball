namespace uspto;

internal class PtabPartyFields
{
    internal string? ApplicationNumber = null;
    internal string? PatentNumber = null;
    internal string? PatentOwnerName = null;
    internal string? RealPartyInInterestName = null;
    internal string? InventorName = null;
    internal string? ArtUnit = null;
    internal string? TechnologyCenter = null;
    internal global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);
    internal bool HasGrantDate = false;
    internal PtabCounsel Counsel = new PtabCounsel();
    internal JsonUnknownField[] UnknownFields = new JsonUnknownField[0];
}

internal static class OdpPtabProceedingParser
{
    internal static PtabTrialProceedingResponse ParseSearch(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP PTAB proceeding search response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("The ODP PTAB proceeding search request is required.", "request", "REQUIRED", null);
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
                throw ParseFailure("The ODP PTAB proceeding response has an invalid count.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentTrialProceedingDataBag");
            if (!records.IsValid || records.IsNull || count.Int32Value != 0 && !records.IsPresent)
            {
                throw ParseFailure("The ODP PTAB proceeding response has an invalid record collection.", records.Path);
            }

            if (count.Int32Value != records.ArrayItems.Length)
            {
                throw ParseFailure("The ODP PTAB proceeding count does not match its records.", records.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, responseMetadata);
            PtabDataProvenance provenance = CreateProvenance(responseMetadata, requestIdentifier);
            PtabTrialProceeding[] proceedings = new PtabTrialProceeding[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                proceedings[index] = ParseProceeding(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PtabTrialProceedingResponse result = new PtabTrialProceedingResponse();
            result.Request = request;
            result.Proceedings = proceedings;
            result.Facets = ParseFacets(root);
            result.Count = proceedings.Length;
            ApplyTotal(root, result);
            result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
            result.RequestIdentifier = requestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = true;
            return result;
        }
    }

    internal static PtabTrialProceeding ParseSingle(string json, string expectedTrialNumber, HttpResponseMetadata responseMetadata)
    {
        if (global::System.String.IsNullOrWhiteSpace(expectedTrialNumber))
        {
            throw new ValidationException("An expected PTAB trial number is required.", "expectedTrialNumber", "REQUIRED", null);
        }

        SearchRequest request = new SearchRequest();
        PtabTrialProceedingResponse response = ParseSearch(json, request, responseMetadata, null);
        if (response.Count != 1)
        {
            throw ParseFailure("The ODP PTAB proceeding response must contain exactly one record.", "$.count");
        }

        PtabTrialProceeding result = response.Proceedings[0];
        if (!global::System.String.Equals(result.TrialNumber, expectedTrialNumber, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw ParseFailure("The ODP PTAB trial number does not match the requested trial.", "$.patentTrialProceedingDataBag[0].trialNumber");
        }

        return result;
    }

    internal static PtabTrialProceeding ParseProceeding(global::System.Text.Json.JsonElement element, string path, PtabDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP PTAB proceeding record is not an object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PtabTrialProceeding result = new PtabTrialProceeding();
        result.TrialNumber = ReadOptionalString(reader, "trialNumber");
        ApplyRecordModifiedDate(reader, result);
        result.Metadata = ParseMetadata(reader);
        result.PatentOwner = ParsePatentOwner(reader);
        result.RegularPetitioner = ParsePetitioner(reader, "regularPetitionerData", "Regular");
        result.Respondent = ParseRespondent(reader);
        result.DerivationPetitioner = ParsePetitioner(reader, "derivationPetitionerData", "Derivation");
        result.Provenance = CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "trialNumber", "lastModifiedDateTime", "trialMetaData", "patentOwnerData", "regularPetitionerData", "respondentData", "derivationPetitionerData" });
        CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyRecordModifiedDate(JsonFieldReader reader, PtabTrialProceeding result)
    {
        JsonFieldValue value = reader.ReadDate("lastModifiedDateTime");
        ValidateOptional(value, "The ODP PTAB proceeding modification time is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            result.LastModifiedAt = value.DateValue;
            result.HasLastModifiedAt = true;
        }
    }

    internal static PtabTrialMetadata ParseMetadata(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadObject("trialMetaData");
        ValidateOptional(value, "The ODP PTAB trial metadata is invalid.");
        PtabTrialMetadata result = new PtabTrialMetadata();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        result.TrialTypeCode = ReadOptionalString(reader, "trialTypeCode");
        result.StatusCategory = ReadOptionalString(reader, "trialStatusCategory");
        ApplyMetadataDates(reader, result);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "trialTypeCode", "trialStatusCategory", "petitionFilingDate", "accordedFilingDate", "institutionDecisionDate", "latestDecisionDate", "terminationDate", "trialLastModifiedDateTime", "trialLastModifiedDate" });
        CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ApplyMetadataDates(JsonFieldReader reader, PtabTrialMetadata result)
    {
        JsonFieldValue petition = reader.ReadDate("petitionFilingDate");
        ValidateOptional(petition, "The ODP PTAB petition filing date is invalid.");
        if (petition.IsPresent && !petition.IsNull)
        {
            result.PetitionFilingDate = petition.DateValue;
            result.HasPetitionFilingDate = true;
        }

        JsonFieldValue accorded = reader.ReadDate("accordedFilingDate");
        ValidateOptional(accorded, "The ODP PTAB accorded filing date is invalid.");
        if (accorded.IsPresent && !accorded.IsNull)
        {
            result.AccordedFilingDate = accorded.DateValue;
            result.HasAccordedFilingDate = true;
        }

        JsonFieldValue institution = reader.ReadDate("institutionDecisionDate");
        ValidateOptional(institution, "The ODP PTAB institution decision date is invalid.");
        if (institution.IsPresent && !institution.IsNull)
        {
            result.InstitutionDecisionDate = institution.DateValue;
            result.HasInstitutionDecisionDate = true;
        }

        JsonFieldValue latest = reader.ReadDate("latestDecisionDate");
        ValidateOptional(latest, "The ODP PTAB latest decision date is invalid.");
        if (latest.IsPresent && !latest.IsNull)
        {
            result.LatestDecisionDate = latest.DateValue;
            result.HasLatestDecisionDate = true;
        }

        JsonFieldValue termination = reader.ReadDate("terminationDate");
        ValidateOptional(termination, "The ODP PTAB termination date is invalid.");
        if (termination.IsPresent && !termination.IsNull)
        {
            result.TerminationDate = termination.DateValue;
            result.HasTerminationDate = true;
        }

        JsonFieldValue modifiedAt = reader.ReadDate("trialLastModifiedDateTime");
        ValidateOptional(modifiedAt, "The ODP PTAB trial modification time is invalid.");
        if (modifiedAt.IsPresent && !modifiedAt.IsNull)
        {
            result.LastModifiedAt = modifiedAt.DateValue;
            result.HasLastModifiedAt = true;
        }

        JsonFieldValue modifiedDate = reader.ReadDate("trialLastModifiedDate");
        ValidateOptional(modifiedDate, "The ODP PTAB trial modification date is invalid.");
        if (modifiedDate.IsPresent && !modifiedDate.IsNull)
        {
            result.LastModifiedDate = modifiedDate.DateValue;
            result.HasLastModifiedDate = true;
        }
    }

    internal static PtabPatentOwner ParsePatentOwner(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadObject("patentOwnerData");
        ValidateOptional(value, "The ODP PTAB patent owner data is invalid.");
        PtabPatentOwner result = new PtabPatentOwner();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        PtabPartyFields fields = ParsePartyFields(reader);
        result.ApplicationNumber = fields.ApplicationNumber;
        result.PatentNumber = fields.PatentNumber;
        result.Name = fields.PatentOwnerName;
        result.RealPartyInInterestName = fields.RealPartyInInterestName;
        result.InventorName = fields.InventorName;
        result.ArtUnit = fields.ArtUnit;
        result.TechnologyCenter = fields.TechnologyCenter;
        result.GrantDate = fields.GrantDate;
        result.HasGrantDate = fields.HasGrantDate;
        result.Counsel = fields.Counsel;
        CopyUnknownFields(fields.UnknownFields, result.UnknownFields);
        return result;
    }

    internal static PtabPetitioner ParsePetitioner(JsonFieldReader record, string fieldName, string category)
    {
        JsonFieldValue value = record.ReadObject(fieldName);
        ValidateOptional(value, "The ODP PTAB petitioner data is invalid.");
        PtabPetitioner result = new PtabPetitioner();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        PtabPartyFields fields = ParsePartyFields(reader);
        result.Category = category;
        result.ApplicationNumber = fields.ApplicationNumber;
        result.PatentNumber = fields.PatentNumber;
        result.PatentOwnerName = fields.PatentOwnerName;
        result.RealPartyInInterestName = fields.RealPartyInInterestName;
        result.InventorName = fields.InventorName;
        result.ArtUnit = fields.ArtUnit;
        result.TechnologyCenter = fields.TechnologyCenter;
        result.GrantDate = fields.GrantDate;
        result.HasGrantDate = fields.HasGrantDate;
        result.Counsel = fields.Counsel;
        CopyUnknownFields(fields.UnknownFields, result.UnknownFields);
        return result;
    }

    internal static PtabRespondent ParseRespondent(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadObject("respondentData");
        ValidateOptional(value, "The ODP PTAB respondent data is invalid.");
        PtabRespondent result = new PtabRespondent();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        PtabPartyFields fields = ParsePartyFields(reader);
        result.ApplicationNumber = fields.ApplicationNumber;
        result.PatentNumber = fields.PatentNumber;
        result.PatentOwnerName = fields.PatentOwnerName;
        result.RealPartyInInterestName = fields.RealPartyInInterestName;
        result.InventorName = fields.InventorName;
        result.ArtUnit = fields.ArtUnit;
        result.TechnologyCenter = fields.TechnologyCenter;
        result.GrantDate = fields.GrantDate;
        result.HasGrantDate = fields.HasGrantDate;
        result.Counsel = fields.Counsel;
        CopyUnknownFields(fields.UnknownFields, result.UnknownFields);
        return result;
    }

    internal static PtabPartyFields ParsePartyFields(JsonFieldReader reader)
    {
        PtabPartyFields result = new PtabPartyFields();
        result.ApplicationNumber = ReadOptionalString(reader, "applicationNumberText");
        result.PatentNumber = ReadOptionalString(reader, "patentNumber");
        result.PatentOwnerName = ReadOptionalString(reader, "patentOwnerName");
        result.RealPartyInInterestName = ReadOptionalAliasedString(reader, new string[] { "realPartyInInterestName", "realPartyName" });
        result.InventorName = ReadOptionalString(reader, "inventorName");
        result.ArtUnit = ReadOptionalString(reader, "groupArtUnitNumber");
        result.TechnologyCenter = ReadOptionalAliasedString(reader, new string[] { "technologyCenterNumber", "techCenterNumber" });
        result.Counsel.Name = ReadOptionalString(reader, "counselName");
        JsonFieldValue grantDate = reader.ReadDate("grantDate");
        ValidateOptional(grantDate, "The ODP PTAB party grant date is invalid.");
        if (grantDate.IsPresent && !grantDate.IsNull)
        {
            result.GrantDate = grantDate.DateValue;
            result.HasGrantDate = true;
        }

        result.UnknownFields = reader.CaptureUnknownFields(new string[] { "applicationNumberText", "patentNumber", "patentOwnerName", "realPartyInInterestName", "realPartyName", "inventorName", "groupArtUnitNumber", "technologyCenterNumber", "techCenterNumber", "counselName", "grantDate" });
        return result;
    }

    internal static PtabFacet[] ParseFacets(JsonFieldReader root)
    {
        JsonFieldValue facets = root.ReadArray("facets");
        ValidateOptional(facets, "The ODP PTAB proceeding facets are invalid.");
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
                throw ParseFailure("The ODP PTAB proceeding facet entry is invalid.", itemPath);
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
            throw ParseFailure("The ODP PTAB proceeding facet values are invalid.", path);
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
                throw ParseFailure("The ODP PTAB proceeding facet name is invalid.", name.Path);
            }

            if (!quantity.IsPresent || !quantity.IsValid || quantity.IsNull || quantity.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB proceeding facet quantity is invalid.", quantity.Path);
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

    internal static void ApplyTotal(JsonFieldReader root, PtabTrialProceedingResponse result)
    {
        JsonFieldValue total = root.ReadInt64("total");
        if (!total.IsPresent)
        {
            total = root.ReadInt64("totalRecordCount");
        }

        ValidateOptional(total, "The ODP PTAB proceeding total is invalid.");
        if (total.IsPresent && !total.IsNull)
        {
            if (total.Int64Value < 0L)
            {
                throw ParseFailure("The ODP PTAB proceeding total cannot be negative.", total.Path);
            }

            result.Total = total.Int64Value;
            result.HasTotal = true;
        }
    }

    internal static PtabDataProvenance CreateProvenance(HttpResponseMetadata metadata, string? requestIdentifier)
    {
        PtabDataProvenance result = new PtabDataProvenance();
        result.Source = DataProvenance.Odp;
        result.Collection = PtabCollection.TrialProceedings;
        result.HasCollection = true;
        result.SourceUri = metadata.RequestUri;
        result.RequestIdentifier = requestIdentifier;
        result.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.HasRetrievedAt = true;
        result.IsAuthoritative = true;
        ProvenanceLabeler.AddPtabField(result, "*", result);
        return result;
    }

    internal static PtabDataProvenance CopyProvenance(PtabDataProvenance source)
    {
        return ProvenanceLabeler.CopyPtab(source);
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, HttpResponseMetadata metadata)
    {
        JsonFieldValue value = root.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP PTAB proceeding request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return metadata.RequestIdentifier;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP PTAB proceeding field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string? ReadOptionalAliasedString(JsonFieldReader reader, string[] names)
    {
        JsonFieldValue value = OdpPtabCompatibilityParser.ReadAliasedString(reader, names);
        ValidateOptional(value, "The ODP PTAB proceeding field is invalid.");
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
        return new ParseException(message, DocumentFormat.Json, "ODP PTAB proceeding", path, 0L, false, 0, false, null);
    }
}
