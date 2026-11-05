namespace uspto;

internal static class PatentsViewPatentParser
{
    internal static PatentsViewPatentResponse ParsePatentResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = ValidateResponseRoot(document.RootElement, "patents");
            PatentDataProvenance provenance = CreatePatentsViewProvenance(responseMetadata, "g_patent", fromCache);
            JsonFieldValue records = root.ReadArray("patents");
            ValidateRequiredValue(records, "The PatentsView patents collection is missing or invalid.");
            PatentsViewPatent[] patents = new PatentsViewPatent[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                patents[index] = ParsePatentRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentsViewPatentResponse result = new PatentsViewPatentResponse();
            result.Patents = patents;
            result.Metadata = ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "patents", provenance);
            return result;
        }
    }

    internal static PatentsViewPublicationResponse ParsePublicationResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = ValidateResponseRoot(document.RootElement, "publications");
            PatentDataProvenance provenance = CreatePatentsViewProvenance(responseMetadata, "pg_publication", fromCache);
            JsonFieldValue records = root.ReadArray("publications");
            ValidateRequiredValue(records, "The PatentsView publications collection is missing or invalid.");
            PatentsViewPublication[] publications = new PatentsViewPublication[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                publications[index] = ParsePublicationRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentsViewPublicationResponse result = new PatentsViewPublicationResponse();
            result.Publications = publications;
            result.Metadata = ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "publications", provenance);
            return result;
        }
    }

    internal static JsonFieldReader ValidateResponseRoot(global::System.Text.Json.JsonElement element, string collectionName)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParsePatentSearchFailure("The PatentsView response must be a JSON object.", "$");
        }

        JsonFieldReader root = new JsonFieldReader(element, "$");
        JsonFieldValue error = root.ReadBoolean("error");
        ValidateRequiredValue(error, "The PatentsView error indicator is missing or invalid.");
        if (error.BooleanValue)
        {
            throw ParsePatentSearchFailure("The PatentsView success response reported an error.", error.Path);
        }

        JsonFieldValue collection = root.ReadArray(collectionName);
        ValidateRequiredValue(collection, "The PatentsView response collection is missing or invalid.");
        return root;
    }

    internal static PatentsViewResponseMetadata ParseResponseMetadata(JsonFieldReader root, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, JsonFieldValue records, string collectionName, PatentDataProvenance provenance)
    {
        JsonFieldValue count = root.ReadInt32("count");
        JsonFieldValue total = root.ReadInt64("total_hits");
        ValidateRequiredValue(count, "The PatentsView response count is missing or invalid.");
        ValidateRequiredValue(total, "The PatentsView total hit count is missing or invalid.");
        if (count.Int32Value < 0 || total.Int64Value < 0L || count.Int32Value != records.ArrayItems.Length)
        {
            throw ParsePatentSearchFailure("The PatentsView response counts are inconsistent.", "$.count");
        }

        PatentsViewResponseMetadata metadata = new PatentsViewResponseMetadata();
        metadata.Request = request;
        metadata.IsError = false;
        metadata.Count = count.Int32Value;
        metadata.TotalHits = total.Int64Value;
        metadata.HasTotalHits = true;
        metadata.RequestIdentifier = responseMetadata.RequestIdentifier;
        metadata.Provenance = OdpJsonParser.CopyProvenance(provenance);
        metadata.RawResponse = rawResponse;
        metadata.IsComplete = count.Int32Value == 0 || count.Int32Value < request.Search.CursorPage.Size || total.Int64Value <= count.Int32Value;
        ApplyNextCursor(metadata, request, records);
        JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "error", "count", "total_hits", collectionName });
        CopyUnknownFields(unknown, metadata.AdditionalFields);
        metadata.Provenance.FromCache = fromCache;
        return metadata;
    }

    internal static void ApplyNextCursor(PatentsViewResponseMetadata metadata, PatentsViewQueryRequest request, JsonFieldValue records)
    {
        if (metadata.IsComplete || records.ArrayItems.Length == 0 || request.Search.Sorts.Length == 0)
        {
            return;
        }

        global::System.Text.Json.JsonElement last = records.ArrayItems[records.ArrayItems.Length - 1];
        string[] values = new string[request.Search.Sorts.Length];
        int index;
        for (index = 0; index < request.Search.Sorts.Length; index += 1)
        {
            string? value = ReadSortValue(last, request.Search.Sorts[index].Field);
            if (value == null)
            {
                return;
            }

            values[index] = value;
        }

        metadata.NextCursorPage.Size = request.Search.CursorPage.Size;
        metadata.NextCursorPage.AfterValues = values;
        metadata.HasNextCursorPage = true;
    }

    internal static string? ReadSortValue(global::System.Text.Json.JsonElement element, string? field)
    {
        if (field == null)
        {
            return null;
        }

        string[] segments = field.Split('.');
        global::System.Text.Json.JsonElement current = element;
        int index;
        for (index = 0; index < segments.Length; index += 1)
        {
            if (current.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            bool found = false;
            global::System.Text.Json.JsonElement next = default(global::System.Text.Json.JsonElement);
            foreach (global::System.Text.Json.JsonProperty property in current.EnumerateObject())
            {
                if (property.NameEquals(segments[index]))
                {
                    next = property.Value;
                    found = true;
                }
            }

            if (!found)
            {
                return null;
            }

            current = next;
        }

        if (current.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            return current.GetString();
        }

        if (current.ValueKind == global::System.Text.Json.JsonValueKind.Number || current.ValueKind == global::System.Text.Json.JsonValueKind.True || current.ValueKind == global::System.Text.Json.JsonValueKind.False)
        {
            return current.GetRawText();
        }

        return null;
    }

    internal static PatentsViewPatent ParsePatentRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        ValidateRecordObject(element, path, "patent");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewPatent patent = new PatentsViewPatent();
        patent.PatentIdentifier = ReadOptionalText(reader, "patent_id");
        patent.Title = ReadOptionalText(reader, "patent_title");
        patent.Abstract = ReadOptionalText(reader, "patent_abstract");
        patent.PatentType = ReadOptionalText(reader, "patent_type");
        patent.KindCode = ReadOptionalText(reader, "wipo_kind");
        ApplyOptionalDate(reader, "patent_date", patent, 1);
        ApplyOptionalDate(reader, "patent_earliest_application_date", patent, 2);
        ApplyPatentInteger(reader, "patent_year", patent, 1);
        ApplyPatentInteger(reader, "patent_detail_desc_length", patent, 2);
        ApplyPatentInteger(reader, "patent_num_us_patents_cited", patent, 3);
        ApplyPatentInteger(reader, "patent_num_us_applications_cited", patent, 4);
        ApplyPatentInteger(reader, "patent_num_foreign_documents_cited", patent, 5);
        ApplyPatentInteger(reader, "patent_num_total_documents_cited", patent, 6);
        ApplyPatentInteger(reader, "patent_num_times_cited_by_us_patents", patent, 7);
        ApplyPatentInteger(reader, "patent_processing_days", patent, 8);
        ApplyPatentInteger(reader, "patent_term_extension", patent, 9);
        JsonFieldValue withdrawn = reader.ReadBoolean("withdrawn");
        ValidateOptionalValue(withdrawn, "The PatentsView withdrawn indicator is invalid.");
        if (withdrawn.IsPresent && !withdrawn.IsNull)
        {
            patent.IsWithdrawn = withdrawn.BooleanValue;
            patent.HasIsWithdrawn = true;
        }

        patent.Applications = ParseApplications(reader, provenance);
        patent.Applicants = ParseApplicants(reader, "applicants", provenance);
        patent.Inventors = ParseInventors(reader, provenance);
        patent.Assignees = ParseAssignees(reader, provenance);
        patent.Attorneys = ParseAttorneys(reader, provenance);
        patent.GovernmentInterests = ParseGovernmentInterests(reader, patent.PatentIdentifier, null, provenance);
        patent.PublicationCrosswalks = ParseCrosswalks(reader, provenance);
        patent.CpcClassifications = ParseCpcClassifications(reader, provenance);
        patent.IpcClassifications = ParseIpcClassifications(reader, provenance);
        patent.UspcClassifications = ParseUspcClassifications(reader, provenance);
        patent.WipoClassifications = ParseWipoClassifications(reader, provenance);
        patent.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(PatentKnownFields());
        CopyUnknownFields(unknown, patent.AdditionalFields);
        return patent;
    }

    internal static PatentsViewPublication ParsePublicationRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        ValidateRecordObject(element, path, "publication");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewPublication publication = new PatentsViewPublication();
        publication.DocumentNumber = ReadOptionalText(reader, "document_number");
        publication.Title = ReadOptionalText(reader, "publication_title");
        publication.Abstract = ReadOptionalText(reader, "publication_abstract");
        publication.PublicationType = ReadOptionalText(reader, "publication_type");
        publication.SeriesCode = NormalizeSeriesCode(ReadOptionalText(reader, "series_code"));
        JsonFieldValue date = reader.ReadDate("publication_date");
        ValidateOptionalValue(date, "The PatentsView publication date is invalid.");
        if (date.IsPresent && !date.IsNull)
        {
            publication.PublicationDate = date.DateValue;
            publication.HasPublicationDate = true;
        }

        JsonFieldValue year = reader.ReadInt32("publication_year");
        ValidateOptionalValue(year, "The PatentsView publication year is invalid.");
        if (year.IsPresent && !year.IsNull)
        {
            publication.PublicationYear = year.Int32Value;
            publication.HasPublicationYear = true;
        }

        JsonFieldValue rule47 = reader.ReadBoolean("rule_47_flag");
        ValidateOptionalValue(rule47, "The PatentsView Rule 47 indicator is invalid.");
        if (rule47.IsPresent && !rule47.IsNull)
        {
            publication.Rule47 = rule47.BooleanValue;
            publication.HasRule47 = true;
        }

        publication.Applicants = ParsePublicationApplicants(reader, provenance);
        publication.Inventors = ParseInventors(reader, provenance);
        publication.Assignees = ParseAssignees(reader, provenance);
        publication.GovernmentInterests = ParseGovernmentInterests(reader, null, publication.DocumentNumber, provenance);
        publication.GrantCrosswalks = ParseCrosswalks(reader, provenance);
        publication.CpcClassifications = ParseCpcClassifications(reader, provenance);
        publication.IpcClassifications = ParseIpcClassifications(reader, provenance);
        publication.UspcClassifications = ParseUspcClassifications(reader, provenance);
        publication.WipoClassifications = ParseWipoClassifications(reader, provenance);
        publication.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(PublicationKnownFields());
        CopyUnknownFields(unknown, publication.AdditionalFields);
        return publication;
    }

    internal static PatentsViewPatentApplication[] ParseApplications(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("application");
        ValidateOptionalValue(values, "The PatentsView application collection is invalid.");
        PatentsViewPatentApplication[] result = new PatentsViewPatentApplication[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "application");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewPatentApplication application = new PatentsViewPatentApplication();
            application.ApplicationIdentifier = ReadOptionalText(item, "application_id");
            application.ApplicationType = ReadOptionalText(item, "application_type");
            application.FilingType = ReadOptionalText(item, "filing_type");
            application.SeriesCode = NormalizeSeriesCode(ReadOptionalText(item, "series_code"));
            JsonFieldValue filingDate = item.ReadDate("filing_date");
            ValidateOptionalValue(filingDate, "The PatentsView application filing date is invalid.");
            if (filingDate.IsPresent && !filingDate.IsNull)
            {
                application.FilingDate = filingDate.DateValue;
                application.HasFilingDate = true;
            }

            JsonFieldValue rule47 = item.ReadBoolean("rule_47_flag");
            ValidateOptionalValue(rule47, "The PatentsView application Rule 47 indicator is invalid.");
            if (rule47.IsPresent && !rule47.IsNull)
            {
                application.Rule47 = rule47.BooleanValue;
                application.HasRule47 = true;
            }

            application.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "application_id", "application_type", "filing_date", "filing_type", "series_code", "rule_47_flag" }), application.AdditionalFields);
            result[index] = application;
        }

        return result;
    }

    internal static PatentsViewApplicant[] ParsePublicationApplicants(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue direct = reader.ReadArray("applicants");
        if (direct.IsPresent)
        {
            return ParseApplicants(reader, "applicants", provenance);
        }

        return ParseApplicants(reader, "us_parties", provenance);
    }

    internal static PatentsViewApplicant[] ParseApplicants(JsonFieldReader reader, string fieldName, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray(fieldName);
        ValidateOptionalValue(values, "The PatentsView applicant collection is invalid.");
        PatentsViewApplicant[] result = new PatentsViewApplicant[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "applicant");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewApplicant applicant = new PatentsViewApplicant();
            applicant.ApplicantIdentifier = ReadAliasedText(item, new string[] { "applicant_id", "applicant" });
            applicant.PatentIdentifier = ReadOptionalText(item, "patent_id");
            applicant.DocumentNumber = ReadOptionalText(item, "document_number");
            applicant.ApplicationIdentifier = ReadOptionalText(item, "application_id");
            applicant.FirstName = ReadAliasedText(item, new string[] { "applicant_name_first", "us_party_name_first" });
            applicant.LastName = ReadAliasedText(item, new string[] { "applicant_name_last", "us_party_name_last" });
            applicant.Organization = ReadAliasedText(item, new string[] { "applicant_organization", "us_party_organization" });
            applicant.ApplicantType = ReadAliasedText(item, new string[] { "applicant_type", "us_party_type" });
            applicant.Designation = ReadAliasedText(item, new string[] { "applicant_designation", "us_party_designation" });
            applicant.Authority = ReadOptionalText(item, "applicant_authority");
            applicant.LocationIdentifier = ReadOptionalText(item, "location_id");
            ApplyApplicantSequence(item, applicant);
            applicant.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "applicant_id", "applicant", "patent_id", "document_number", "application_id", "applicant_name_first", "us_party_name_first", "applicant_name_last", "us_party_name_last", "applicant_organization", "us_party_organization", "applicant_type", "us_party_type", "applicant_designation", "us_party_designation", "applicant_authority", "location_id", "applicant_sequence", "us_party_sequence" }), applicant.AdditionalFields);
            result[index] = applicant;
        }

        return result;
    }

    internal static void ApplyApplicantSequence(JsonFieldReader reader, PatentsViewApplicant applicant)
    {
        JsonFieldValue sequence = ReadAliasedInt32(reader, new string[] { "applicant_sequence", "us_party_sequence" });
        ValidateOptionalValue(sequence, "The PatentsView applicant sequence is invalid.");
        if (sequence.IsPresent && !sequence.IsNull)
        {
            applicant.Sequence = sequence.Int32Value;
            applicant.HasSequence = true;
        }
    }

    internal static PatentsViewInventor[] ParseInventors(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("inventors");
        ValidateOptionalValue(values, "The PatentsView inventor collection is invalid.");
        PatentsViewInventor[] result = new PatentsViewInventor[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "inventor");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewInventor inventor = new PatentsViewInventor();
            inventor.InventorIdentifier = ReadAliasedText(item, new string[] { "inventor_id", "inventor" });
            inventor.ResourceReference = ReadOptionalText(item, "inventor");
            inventor.FirstName = ReadOptionalText(item, "inventor_name_first");
            inventor.LastName = ReadOptionalText(item, "inventor_name_last");
            inventor.GenderCode = ReadOptionalText(item, "inventor_gender_code");
            inventor.LastKnownLocation = ParseEmbeddedLocation(item, "inventor", provenance);
            inventor.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "inventor_id", "inventor", "inventor_name_first", "inventor_name_last", "inventor_gender_code", "inventor_city", "inventor_state", "inventor_country", "inventor_location_id" }), inventor.AdditionalFields);
            result[index] = inventor;
        }

        return result;
    }

    internal static PatentsViewAssignee[] ParseAssignees(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("assignees");
        ValidateOptionalValue(values, "The PatentsView assignee collection is invalid.");
        PatentsViewAssignee[] result = new PatentsViewAssignee[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "assignee");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewAssignee assignee = new PatentsViewAssignee();
            assignee.AssigneeIdentifier = ReadAliasedText(item, new string[] { "assignee_id", "assignee" });
            assignee.ResourceReference = ReadOptionalText(item, "assignee");
            assignee.FirstName = ReadOptionalText(item, "assignee_individual_name_first");
            assignee.LastName = ReadOptionalText(item, "assignee_individual_name_last");
            assignee.Organization = ReadOptionalText(item, "assignee_organization");
            assignee.AssigneeType = ReadOptionalText(item, "assignee_type");
            assignee.LastKnownLocation = ParseEmbeddedLocation(item, "assignee", provenance);
            assignee.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "assignee_id", "assignee", "assignee_individual_name_first", "assignee_individual_name_last", "assignee_organization", "assignee_type", "assignee_city", "assignee_state", "assignee_country", "assignee_location_id" }), assignee.AdditionalFields);
            result[index] = assignee;
        }

        return result;
    }

    internal static PatentsViewLocation? ParseEmbeddedLocation(JsonFieldReader reader, string prefix, PatentDataProvenance provenance)
    {
        string? identifier = ReadOptionalText(reader, prefix + "_location_id");
        string? city = ReadOptionalText(reader, prefix + "_city");
        string? state = ReadOptionalText(reader, prefix + "_state");
        string? country = ReadOptionalText(reader, prefix + "_country");
        if (identifier == null && city == null && state == null && country == null)
        {
            return null;
        }

        PatentsViewLocation location = new PatentsViewLocation();
        location.LocationIdentifier = identifier;
        location.Name = city;
        location.State = state;
        location.Country = country;
        location.Provenance = OdpJsonParser.CopyProvenance(provenance);
        return location;
    }

    internal static PatentsViewAttorney[] ParseAttorneys(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("attorneys");
        ValidateOptionalValue(values, "The PatentsView attorney collection is invalid.");
        PatentsViewAttorney[] result = new PatentsViewAttorney[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "attorney");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewAttorney attorney = new PatentsViewAttorney();
            attorney.AttorneyIdentifier = ReadOptionalText(item, "attorney_id");
            attorney.FirstName = ReadOptionalText(item, "attorney_name_first");
            attorney.LastName = ReadOptionalText(item, "attorney_name_last");
            attorney.Organization = ReadOptionalText(item, "attorney_organization");
            attorney.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "attorney_id", "attorney_name_first", "attorney_name_last", "attorney_organization" }), attorney.AdditionalFields);
            result[index] = attorney;
        }

        return result;
    }

    internal static PatentsViewGovernmentInterest[] ParseGovernmentInterests(JsonFieldReader reader, string? patentIdentifier, string? documentNumber, PatentDataProvenance provenance)
    {
        string? statement = ReadOptionalText(reader, "gov_interest_statement");
        JsonFieldValue awards = reader.ReadArray("gov_interest_contract_award_numbers");
        JsonFieldValue organizations = reader.ReadArray("gov_interest_organizations");
        ValidateOptionalValue(awards, "The PatentsView government award collection is invalid.");
        ValidateOptionalValue(organizations, "The PatentsView government organization collection is invalid.");
        if (statement == null && awards.ArrayItems.Length == 0 && organizations.ArrayItems.Length == 0)
        {
            return new PatentsViewGovernmentInterest[0];
        }

        PatentsViewGovernmentInterest interest = new PatentsViewGovernmentInterest();
        interest.PatentIdentifier = patentIdentifier;
        interest.DocumentNumber = documentNumber;
        interest.Statement = statement;
        interest.AwardNumbers = ParseAwardNumbers(awards);
        interest.Organizations = ParseGovernmentOrganizations(organizations);
        interest.Provenance = OdpJsonParser.CopyProvenance(provenance);
        return new PatentsViewGovernmentInterest[] { interest };
    }

    internal static string[] ParseAwardNumbers(JsonFieldValue values)
    {
        global::System.Collections.Generic.List<string> result = new global::System.Collections.Generic.List<string>();
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "government award");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            string? award = ReadOptionalText(item, "award_number");
            if (award != null)
            {
                result.Add(award);
            }
        }

        return result.ToArray();
    }

    internal static PatentsViewGovernmentOrganization[] ParseGovernmentOrganizations(JsonFieldValue values)
    {
        PatentsViewGovernmentOrganization[] result = new PatentsViewGovernmentOrganization[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "government organization");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewGovernmentOrganization organization = new PatentsViewGovernmentOrganization();
            organization.FederalAgencyName = ReadOptionalText(item, "fedagency_name");
            organization.LevelOne = ReadOptionalText(item, "level_one");
            organization.LevelTwo = ReadOptionalText(item, "level_two");
            organization.LevelThree = ReadOptionalText(item, "level_three");
            result[index] = organization;
        }

        return result;
    }

    internal static PatentsViewGrantPublicationCrosswalk[] ParseCrosswalks(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("granted_pregrant_crosswalk");
        ValidateOptionalValue(values, "The PatentsView grant-publication crosswalk is invalid.");
        PatentsViewGrantPublicationCrosswalk[] result = new PatentsViewGrantPublicationCrosswalk[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "grant-publication crosswalk");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewGrantPublicationCrosswalk crosswalk = new PatentsViewGrantPublicationCrosswalk();
            crosswalk.PatentIdentifier = ReadOptionalText(item, "patent_id");
            crosswalk.ApplicationIdentifier = ReadOptionalText(item, "application_number");
            crosswalk.DocumentNumber = ReadOptionalText(item, "document_number");
            ApplyCrosswalkIndicators(item, crosswalk);
            crosswalk.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "patent_id", "application_number", "document_number", "current_document_number_flag", "current_patent_id_flag" }), crosswalk.AdditionalFields);
            result[index] = crosswalk;
        }

        return result;
    }

    internal static void ApplyCrosswalkIndicators(JsonFieldReader reader, PatentsViewGrantPublicationCrosswalk crosswalk)
    {
        JsonFieldValue currentDocument = reader.ReadBoolean("current_document_number_flag");
        JsonFieldValue currentPatent = reader.ReadBoolean("current_patent_id_flag");
        ValidateOptionalValue(currentDocument, "The PatentsView current-document indicator is invalid.");
        ValidateOptionalValue(currentPatent, "The PatentsView current-patent indicator is invalid.");
        if (currentDocument.IsPresent && !currentDocument.IsNull)
        {
            crosswalk.IsCurrentDocumentNumber = currentDocument.BooleanValue;
            crosswalk.HasIsCurrentDocumentNumber = true;
        }

        if (currentPatent.IsPresent && !currentPatent.IsNull)
        {
            crosswalk.IsCurrentPatentIdentifier = currentPatent.BooleanValue;
            crosswalk.HasIsCurrentPatentIdentifier = true;
        }
    }

    internal static PatentsViewCpcClassification[] ParseCpcClassifications(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("cpc_current");
        if (!values.IsPresent)
        {
            values = reader.ReadArray("cpc_at_issue");
        }

        ValidateOptionalValue(values, "The PatentsView CPC collection is invalid.");
        PatentsViewCpcClassification[] result = new PatentsViewCpcClassification[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "CPC classification");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewCpcClassification classification = new PatentsViewCpcClassification();
            classification.Section = ReadOptionalText(item, "cpc_section");
            classification.Class = ReadOptionalText(item, "cpc_class");
            classification.ClassIdentifier = ReadOptionalText(item, "cpc_class_id");
            classification.Subclass = ReadOptionalText(item, "cpc_subclass");
            classification.SubclassIdentifier = ReadOptionalText(item, "cpc_subclass_id");
            classification.Group = ReadOptionalText(item, "cpc_group");
            classification.GroupIdentifier = ReadOptionalText(item, "cpc_group_id");
            classification.ClassificationType = ReadOptionalText(item, "cpc_type");
            ApplyCpcSequence(item, classification);
            classification.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "cpc_section", "cpc_class", "cpc_class_id", "cpc_subclass", "cpc_subclass_id", "cpc_group", "cpc_group_id", "cpc_type", "cpc_sequence" }), classification.AdditionalFields);
            result[index] = classification;
        }

        return result;
    }

    internal static void ApplyCpcSequence(JsonFieldReader reader, PatentsViewCpcClassification classification)
    {
        JsonFieldValue sequence = reader.ReadInt32("cpc_sequence");
        ValidateOptionalValue(sequence, "The PatentsView CPC sequence is invalid.");
        if (sequence.IsPresent && !sequence.IsNull)
        {
            classification.Sequence = sequence.Int32Value;
            classification.HasSequence = true;
        }
    }

    internal static PatentsViewIpcClassification[] ParseIpcClassifications(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("ipcr");
        ValidateOptionalValue(values, "The PatentsView IPC collection is invalid.");
        PatentsViewIpcClassification[] result = new PatentsViewIpcClassification[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "IPC classification");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewIpcClassification classification = new PatentsViewIpcClassification();
            classification.ClassificationIdentifier = ReadOptionalText(item, "ipc_id");
            classification.Section = ReadOptionalText(item, "ipc_section");
            classification.Class = ReadOptionalText(item, "ipc_class");
            classification.Subclass = ReadOptionalText(item, "ipc_subclass");
            classification.MainGroup = ReadOptionalText(item, "ipc_main_group");
            classification.Subgroup = ReadOptionalText(item, "ipc_subgroup");
            classification.DataSource = ReadOptionalText(item, "ipc_classification_data_source");
            classification.ClassificationValue = ReadOptionalText(item, "ipc_classification_value");
            classification.SymbolPosition = ReadOptionalText(item, "ipc_symbol_position");
            classification.ClassificationLevel = ReadOptionalText(item, "ipc_class_level");
            classification.ClassificationStatus = ReadOptionalText(item, "ipc_class_status");
            ApplyIpcValues(item, classification);
            classification.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "ipc_id", "ipc_section", "ipc_class", "ipc_subclass", "ipc_main_group", "ipc_subgroup", "ipc_classification_data_source", "ipc_classification_value", "ipc_symbol_position", "ipc_class_level", "ipc_class_status", "ipc_action_date", "ipc_sequence" }), classification.AdditionalFields);
            result[index] = classification;
        }

        return result;
    }

    internal static void ApplyIpcValues(JsonFieldReader reader, PatentsViewIpcClassification classification)
    {
        JsonFieldValue date = reader.ReadDate("ipc_action_date");
        JsonFieldValue sequence = reader.ReadInt32("ipc_sequence");
        ValidateOptionalValue(date, "The PatentsView IPC action date is invalid.");
        ValidateOptionalValue(sequence, "The PatentsView IPC sequence is invalid.");
        if (date.IsPresent && !date.IsNull)
        {
            classification.ActionDate = date.DateValue;
            classification.HasActionDate = true;
        }

        if (sequence.IsPresent && !sequence.IsNull)
        {
            classification.Sequence = sequence.Int32Value;
            classification.HasSequence = true;
        }
    }

    internal static PatentsViewUspcClassification[] ParseUspcClassifications(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("uspc_at_issue");
        ValidateOptionalValue(values, "The PatentsView USPC collection is invalid.");
        PatentsViewUspcClassification[] result = new PatentsViewUspcClassification[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "USPC classification");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewUspcClassification classification = new PatentsViewUspcClassification();
            classification.MainClass = ReadOptionalText(item, "uspc_mainclass");
            classification.MainClassIdentifier = ReadOptionalText(item, "uspc_mainclass_id");
            classification.Subclass = ReadOptionalText(item, "uspc_subclass");
            classification.SubclassIdentifier = ReadOptionalText(item, "uspc_subclass_id");
            JsonFieldValue sequence = item.ReadInt32("uspc_sequence");
            ValidateOptionalValue(sequence, "The PatentsView USPC sequence is invalid.");
            if (sequence.IsPresent && !sequence.IsNull)
            {
                classification.Sequence = sequence.Int32Value;
                classification.HasSequence = true;
            }

            classification.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "uspc_mainclass", "uspc_mainclass_id", "uspc_subclass", "uspc_subclass_id", "uspc_sequence" }), classification.AdditionalFields);
            result[index] = classification;
        }

        return result;
    }

    internal static PatentsViewWipoClassification[] ParseWipoClassifications(JsonFieldReader reader, PatentDataProvenance provenance)
    {
        JsonFieldValue values = reader.ReadArray("wipo");
        ValidateOptionalValue(values, "The PatentsView WIPO collection is invalid.");
        PatentsViewWipoClassification[] result = new PatentsViewWipoClassification[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "WIPO classification");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            PatentsViewWipoClassification classification = new PatentsViewWipoClassification();
            classification.ClassificationIdentifier = ReadOptionalText(item, "wipo_field_id");
            classification.SectorTitle = ReadOptionalText(item, "wipo_sector_title");
            classification.FieldTitle = ReadOptionalText(item, "wipo_field");
            JsonFieldValue sequence = item.ReadInt32("wipo_sequence");
            ValidateOptionalValue(sequence, "The PatentsView WIPO sequence is invalid.");
            if (sequence.IsPresent && !sequence.IsNull)
            {
                classification.Sequence = sequence.Int32Value;
                classification.HasSequence = true;
            }

            classification.Provenance = OdpJsonParser.CopyProvenance(provenance);
            CopyUnknownFields(item.CaptureUnknownFields(new string[] { "wipo_field_id", "wipo_sector_title", "wipo_field", "wipo_sequence" }), classification.AdditionalFields);
            result[index] = classification;
        }

        return result;
    }

    internal static void ApplyOptionalDate(JsonFieldReader reader, string fieldName, PatentsViewPatent patent, int destination)
    {
        JsonFieldValue value = reader.ReadDate(fieldName);
        ValidateOptionalValue(value, "A PatentsView patent date is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return;
        }

        if (destination == 1)
        {
            patent.GrantDate = value.DateValue;
            patent.HasGrantDate = true;
        }
        else
        {
            patent.EarliestApplicationDate = value.DateValue;
            patent.HasEarliestApplicationDate = true;
        }
    }

    internal static void ApplyPatentInteger(JsonFieldReader reader, string fieldName, PatentsViewPatent patent, int destination)
    {
        JsonFieldValue value = reader.ReadInt32(fieldName);
        ValidateOptionalValue(value, "A PatentsView patent numeric field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return;
        }

        switch (destination)
        {
            case 1:
                patent.GrantYear = value.Int32Value;
                patent.HasGrantYear = true;
                break;
            case 2:
                patent.DescriptionLength = value.Int32Value;
                patent.HasDescriptionLength = true;
                break;
            case 3:
                patent.UnitedStatesPatentCitationCount = value.Int32Value;
                patent.HasUnitedStatesPatentCitationCount = true;
                break;
            case 4:
                patent.UnitedStatesApplicationCitationCount = value.Int32Value;
                patent.HasUnitedStatesApplicationCitationCount = true;
                break;
            case 5:
                patent.ForeignDocumentCitationCount = value.Int32Value;
                patent.HasForeignDocumentCitationCount = true;
                break;
            case 6:
                patent.TotalDocumentCitationCount = value.Int32Value;
                patent.HasTotalDocumentCitationCount = true;
                break;
            case 7:
                patent.CitedByUnitedStatesPatentCount = value.Int32Value;
                patent.HasCitedByUnitedStatesPatentCount = true;
                break;
            case 8:
                patent.ProcessingDays = value.Int32Value;
                patent.HasProcessingDays = true;
                break;
            case 9:
                patent.TermExtensionDays = value.Int32Value;
                patent.HasTermExtensionDays = true;
                break;
            default:
                throw ParsePatentSearchFailure("The PatentsView numeric destination is invalid.", fieldName);
        }
    }

    internal static string? ReadOptionalText(JsonFieldReader reader, string fieldName)
    {
        JsonFieldValue value = reader.ReadString(fieldName);
        ValidateOptionalValue(value, "A PatentsView text field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string? ReadAliasedText(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadString(names[index]);
            if (value.IsPresent)
            {
                ValidateOptionalValue(value, "A PatentsView text field is invalid.");
                return value.IsNull ? null : value.StringValue;
            }
        }

        return null;
    }

    internal static string? NormalizeSeriesCode(string? value)
    {
        if (value != null && value.Length == 1 && value[0] >= '0' && value[0] <= '9')
        {
            return "0" + value;
        }

        return value;
    }

    internal static JsonFieldValue ReadAliasedInt32(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadInt32(names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return reader.ReadInt32(names[0]);
    }

    internal static PatentDataProvenance CreatePatentsViewProvenance(HttpResponseMetadata responseMetadata, string datasetIdentifier, bool fromCache)
    {
        PatentDataProvenance provenance = new PatentDataProvenance();
        provenance.Source = DataProvenance.PatentsView;
        provenance.SourceUri = responseMetadata.RequestUri;
        provenance.DatasetIdentifier = datasetIdentifier;
        provenance.RequestIdentifier = responseMetadata.RequestIdentifier;
        provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        provenance.HasRetrievedAt = true;
        provenance.FromCache = fromCache;
        provenance.IsAuthoritative = false;
        ProvenanceLabeler.AddPatentField(provenance, "*", provenance);
        return provenance;
    }

    internal static void ValidateRecordObject(global::System.Text.Json.JsonElement element, string path, string recordName)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParsePatentSearchFailure("The PatentsView " + recordName + " record is not an object.", path);
        }
    }

    internal static void ValidateRequiredValue(JsonFieldValue value, string message)
    {
        if (!value.IsPresent || !value.IsValid || value.IsNull)
        {
            throw ParsePatentSearchFailure(message, value.Path);
        }
    }

    internal static void ValidateOptionalValue(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw ParsePatentSearchFailure(message, value.Path);
        }
    }

    internal static void CopyUnknownFields(JsonUnknownField[] unknown, global::System.Collections.Generic.Dictionary<string, string> destination)
    {
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            destination[unknown[index].Name] = unknown[index].RawJson;
        }
    }

    internal static string[] PatentKnownFields()
    {
        return new string[] { "patent_id", "patent_title", "patent_abstract", "patent_date", "patent_year", "patent_type", "wipo_kind", "withdrawn", "patent_earliest_application_date", "patent_detail_desc_length", "patent_num_us_patents_cited", "patent_num_us_applications_cited", "patent_num_foreign_documents_cited", "patent_num_total_documents_cited", "patent_num_times_cited_by_us_patents", "patent_processing_days", "patent_term_extension", "application", "applicants", "inventors", "assignees", "attorneys", "gov_interest_statement", "gov_interest_contract_award_numbers", "gov_interest_organizations", "granted_pregrant_crosswalk", "cpc_current", "cpc_at_issue", "ipcr", "uspc_at_issue", "wipo" };
    }

    internal static string[] PublicationKnownFields()
    {
        return new string[] { "document_number", "publication_title", "publication_abstract", "publication_date", "publication_year", "publication_type", "series_code", "rule_47_flag", "applicants", "us_parties", "inventors", "assignees", "gov_interest_statement", "gov_interest_contract_award_numbers", "gov_interest_organizations", "granted_pregrant_crosswalk", "cpc_current", "cpc_at_issue", "ipcr", "uspc_at_issue", "wipo" };
    }

    internal static ParseException ParsePatentSearchFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "PatentsView PatentSearch", path, 0L, false, 0, false, null);
    }
}
