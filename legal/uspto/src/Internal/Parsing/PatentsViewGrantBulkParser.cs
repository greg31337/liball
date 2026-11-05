namespace uspto;

internal enum PatentsViewGrantTable
{
    Unknown = 0,
    Patent = 1,
    Application = 2,
    Inventor = 3,
    Assignee = 4,
    Applicant = 5,
    Location = 6,
    Attorney = 7,
    Cpc = 8,
    Ipc = 9,
    Uspc = 10,
    Wipo = 11,
    UsPatentCitation = 12,
    UsApplicationCitation = 13,
    ForeignCitation = 14,
    OtherReference = 15,
    RelatedApplication = 16,
    GovernmentInterest = 17,
    Claim = 18,
    Summary = 19,
    Description = 20,
    DrawingDescription = 21,
    EntityCrosswalk = 22,
    GrantPublicationCrosswalk = 23
}

internal static class PatentsViewGrantBulkParser
{
    internal static object ParseRow(DelimitedRow row, string tableHint, PatentDataProvenance provenance)
    {
        ValidateRow(row, provenance);
        PatentsViewGrantTable table = ResolveTable(tableHint, row);
        switch (table)
        {
            case PatentsViewGrantTable.Patent:
                return ParsePatent(row, tableHint, provenance);
            case PatentsViewGrantTable.Application:
                return ParseApplication(row, tableHint, provenance);
            case PatentsViewGrantTable.Inventor:
                return ParseInventor(row, tableHint, provenance);
            case PatentsViewGrantTable.Assignee:
                return ParseAssignee(row, tableHint, provenance);
            case PatentsViewGrantTable.Applicant:
                return ParseApplicant(row, tableHint, provenance);
            case PatentsViewGrantTable.Location:
                return ParseLocation(row, tableHint, provenance);
            case PatentsViewGrantTable.Attorney:
                return ParseAttorney(row, tableHint, provenance);
            case PatentsViewGrantTable.Cpc:
                return ParseCpc(row, tableHint, provenance);
            case PatentsViewGrantTable.Ipc:
                return ParseIpc(row, tableHint, provenance);
            case PatentsViewGrantTable.Uspc:
                return ParseUspc(row, tableHint, provenance);
            case PatentsViewGrantTable.Wipo:
                return ParseWipo(row, tableHint, provenance);
            case PatentsViewGrantTable.UsPatentCitation:
                return ParseCitation(row, tableHint, provenance, "us_patent");
            case PatentsViewGrantTable.UsApplicationCitation:
                return ParseCitation(row, tableHint, provenance, "us_application");
            case PatentsViewGrantTable.ForeignCitation:
                return ParseCitation(row, tableHint, provenance, "foreign");
            case PatentsViewGrantTable.OtherReference:
                return ParseCitation(row, tableHint, provenance, "other_reference");
            case PatentsViewGrantTable.RelatedApplication:
                return ParseRelatedApplication(row, tableHint, provenance);
            case PatentsViewGrantTable.GovernmentInterest:
                return ParseGovernmentInterest(row, tableHint, provenance);
            case PatentsViewGrantTable.Claim:
                return ParseClaim(row, tableHint, provenance);
            case PatentsViewGrantTable.Summary:
                return ParseSummary(row, tableHint, provenance);
            case PatentsViewGrantTable.Description:
                return ParseDescription(row, tableHint, provenance);
            case PatentsViewGrantTable.DrawingDescription:
                return ParseDrawingDescription(row, tableHint, provenance);
            case PatentsViewGrantTable.EntityCrosswalk:
                return ParseEntityCrosswalk(row, tableHint, provenance);
            case PatentsViewGrantTable.GrantPublicationCrosswalk:
                return ParseGrantPublicationCrosswalk(row, tableHint, provenance);
            default:
                throw ParseFailure("The PatentsView grant TSV table could not be identified.", tableHint, "table", row);
        }
    }

    internal static bool CanParseRow(DelimitedRow row, string tableHint)
    {
        if (row == null || row.Headers == null || row.Headers.Length == 0)
        {
            return false;
        }

        return ResolveTable(tableHint, row) != PatentsViewGrantTable.Unknown;
    }

    internal static PatentsViewGrantTable ResolveTable(string tableHint, DelimitedRow row)
    {
        string value = NormalizeName(tableHint ?? string.Empty);
        if (ContainsName(value, "patentpublication") || ContainsName(value, "publicationcrosswalk") || ContainsName(value, "grantpublicationcrosswalk"))
        {
            return PatentsViewGrantTable.GrantPublicationCrosswalk;
        }

        if (ContainsName(value, "crosswalk") || ContainsName(value, "notdisambiguated"))
        {
            return PatentsViewGrantTable.EntityCrosswalk;
        }

        if (ContainsName(value, "usapplicationcitation"))
        {
            return PatentsViewGrantTable.UsApplicationCitation;
        }

        if (ContainsName(value, "uspatentcitation"))
        {
            return PatentsViewGrantTable.UsPatentCitation;
        }

        if (ContainsName(value, "foreigncitation"))
        {
            return PatentsViewGrantTable.ForeignCitation;
        }

        if (ContainsName(value, "otherreference"))
        {
            return PatentsViewGrantTable.OtherReference;
        }

        if (ContainsName(value, "relapp") || ContainsName(value, "relatedapplication"))
        {
            return PatentsViewGrantTable.RelatedApplication;
        }

        if (ContainsName(value, "govinterest") || ContainsName(value, "governmentinterest"))
        {
            return PatentsViewGrantTable.GovernmentInterest;
        }

        if (ContainsName(value, "drawdesc") || ContainsName(value, "drawingdescription"))
        {
            return PatentsViewGrantTable.DrawingDescription;
        }

        if (ContainsName(value, "detaildesc") || ContainsName(value, "descriptiontext"))
        {
            return PatentsViewGrantTable.Description;
        }

        if (ContainsName(value, "brfsum") || ContainsName(value, "briefsummary"))
        {
            return PatentsViewGrantTable.Summary;
        }

        if (ContainsName(value, "claim"))
        {
            return PatentsViewGrantTable.Claim;
        }

        if (ContainsName(value, "cpc"))
        {
            return PatentsViewGrantTable.Cpc;
        }

        if (ContainsName(value, "ipc"))
        {
            return PatentsViewGrantTable.Ipc;
        }

        if (ContainsName(value, "uspc"))
        {
            return PatentsViewGrantTable.Uspc;
        }

        if (ContainsName(value, "wipo"))
        {
            return PatentsViewGrantTable.Wipo;
        }

        if (ContainsName(value, "applicant"))
        {
            return PatentsViewGrantTable.Applicant;
        }

        if (ContainsName(value, "inventor"))
        {
            return PatentsViewGrantTable.Inventor;
        }

        if (ContainsName(value, "assignee"))
        {
            return PatentsViewGrantTable.Assignee;
        }

        if (ContainsName(value, "location"))
        {
            return PatentsViewGrantTable.Location;
        }

        if (ContainsName(value, "attorney") || ContainsName(value, "lawyer"))
        {
            return PatentsViewGrantTable.Attorney;
        }

        if (ContainsName(value, "application"))
        {
            return PatentsViewGrantTable.Application;
        }

        if (ContainsName(value, "gpatent") || value.Equals("patent", global::System.StringComparison.Ordinal))
        {
            return PatentsViewGrantTable.Patent;
        }

        if (HasHeader(row, "patent_title") && HasHeader(row, "patent_date"))
        {
            return PatentsViewGrantTable.Patent;
        }

        if (HasHeader(row, "claim_text"))
        {
            return PatentsViewGrantTable.Claim;
        }

        if (HasHeader(row, "cpc_id"))
        {
            return PatentsViewGrantTable.Cpc;
        }

        if (HasHeader(row, "ipc_id"))
        {
            return PatentsViewGrantTable.Ipc;
        }

        if (HasHeader(row, "uspc_id"))
        {
            return PatentsViewGrantTable.Uspc;
        }

        if (HasHeader(row, "wipo_id"))
        {
            return PatentsViewGrantTable.Wipo;
        }

        if (HasHeader(row, "source_entity_id") && HasHeader(row, "persistent_entity_id"))
        {
            return PatentsViewGrantTable.EntityCrosswalk;
        }

        if (HasHeader(row, "patent_id") && HasHeader(row, "application_id") && HasHeader(row, "document_number"))
        {
            return PatentsViewGrantTable.GrantPublicationCrosswalk;
        }

        if (HasHeader(row, "inventor_id"))
        {
            return PatentsViewGrantTable.Inventor;
        }

        if (HasHeader(row, "assignee_id"))
        {
            return PatentsViewGrantTable.Assignee;
        }

        if (HasHeader(row, "applicant_id"))
        {
            return PatentsViewGrantTable.Applicant;
        }

        if (HasHeader(row, "location_id") && HasHeader(row, "location_city"))
        {
            return PatentsViewGrantTable.Location;
        }

        if (HasHeader(row, "attorney_id") || HasHeader(row, "lawyer_id"))
        {
            return PatentsViewGrantTable.Attorney;
        }

        if (HasHeader(row, "application_id") && HasHeader(row, "filing_date"))
        {
            return PatentsViewGrantTable.Application;
        }

        if (HasHeader(row, "citation_patent_id"))
        {
            return PatentsViewGrantTable.UsPatentCitation;
        }

        if (HasHeader(row, "citation_document_number"))
        {
            return PatentsViewGrantTable.UsApplicationCitation;
        }

        if (HasHeader(row, "citation_country"))
        {
            return PatentsViewGrantTable.ForeignCitation;
        }

        if (HasHeader(row, "citation_text"))
        {
            return PatentsViewGrantTable.OtherReference;
        }

        if (HasHeader(row, "related_application_text"))
        {
            return PatentsViewGrantTable.RelatedApplication;
        }

        if (HasHeader(row, "government_interest_statement"))
        {
            return PatentsViewGrantTable.GovernmentInterest;
        }

        if (HasHeader(row, "drawing_description"))
        {
            return PatentsViewGrantTable.DrawingDescription;
        }

        if (HasHeader(row, "description_text"))
        {
            return PatentsViewGrantTable.Description;
        }

        if (HasHeader(row, "summary_text"))
        {
            return PatentsViewGrantTable.Summary;
        }

        return PatentsViewGrantTable.Unknown;
    }

    internal static PatentsViewPatent ParsePatent(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "patent_id", "patent_title", "patent_abstract", "patent_date", "patent_year", "patent_type", "patent_kind", "wipo_kind", "withdrawn", "patent_withdrawn", "patent_earliest_application_date", "patent_detail_desc_length", "patent_num_us_patents_cited", "patent_num_us_applications_cited", "patent_num_foreign_documents_cited", "patent_num_total_documents_cited", "patent_num_times_cited_by_us_patents", "patent_processing_days", "patent_term_extension" };
        PatentsViewPatent result = new PatentsViewPatent();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id", "patent_number" });
        result.Title = ReadText(row, new string[] { "patent_title", "title" });
        result.Abstract = ReadText(row, new string[] { "patent_abstract", "abstract" });
        ApplyDate(ReadText(row, new string[] { "patent_date", "grant_date" }), sourceName, "patent_date", row, result, 1);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_year", "grant_year" }), sourceName, "patent_year", row, result, 1);
        result.PatentType = ReadText(row, new string[] { "patent_type", "type" });
        result.KindCode = ReadText(row, new string[] { "patent_kind", "wipo_kind", "kind_code" });
        ApplyPatentBoolean(ReadText(row, new string[] { "patent_withdrawn", "withdrawn" }), sourceName, "patent_withdrawn", row, result);
        ApplyDate(ReadText(row, new string[] { "patent_earliest_application_date", "earliest_application_date" }), sourceName, "patent_earliest_application_date", row, result, 2);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_detail_desc_length", "description_length" }), sourceName, "patent_detail_desc_length", row, result, 2);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_num_us_patents_cited", "patent_num_us_patent_citations" }), sourceName, "patent_num_us_patents_cited", row, result, 3);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_num_us_applications_cited", "patent_num_us_application_citations" }), sourceName, "patent_num_us_applications_cited", row, result, 4);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_num_foreign_documents_cited", "patent_num_foreign_citations" }), sourceName, "patent_num_foreign_documents_cited", row, result, 5);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_num_total_documents_cited", "patent_num_total_documents" }), sourceName, "patent_num_total_documents_cited", row, result, 6);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_num_times_cited_by_us_patents" }), sourceName, "patent_num_times_cited_by_us_patents", row, result, 7);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_processing_days" }), sourceName, "patent_processing_days", row, result, 8);
        ApplyPatentInteger(ReadText(row, new string[] { "patent_term_extension" }), sourceName, "patent_term_extension", row, result, 9);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewPatentApplication ParseApplication(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "application_id", "application_number", "application_type", "filing_date", "application_date", "filing_type", "series_code", "rule_47", "rule_47_flag" };
        PatentsViewPatentApplication result = new PatentsViewPatentApplication();
        result.ApplicationIdentifier = ReadText(row, new string[] { "application_id", "application_number" });
        result.ApplicationType = ReadText(row, new string[] { "application_type" });
        ApplyApplicationDate(ReadText(row, new string[] { "filing_date", "application_date" }), sourceName, "filing_date", row, result);
        result.FilingType = ReadText(row, new string[] { "filing_type" });
        result.SeriesCode = ReadText(row, new string[] { "series_code" });
        ApplyApplicationBoolean(ReadText(row, new string[] { "rule_47", "rule_47_flag" }), sourceName, "rule_47_flag", row, result);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewInventor ParseInventor(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "inventor_id", "inventor_first_name", "inventor_name_first", "inventor_last_name", "inventor_name_last", "inventor_gender", "inventor_gender_code", "first_seen_date", "last_seen_date", "assignee_count", "patent_count", "years_active", "resource_reference", "inventor" };
        PatentsViewInventor result = new PatentsViewInventor();
        result.InventorIdentifier = ReadText(row, new string[] { "inventor_id", "inventor_identifier" });
        result.ResourceReference = ReadText(row, new string[] { "resource_reference", "inventor_href", "inventor" });
        result.FirstName = ReadText(row, new string[] { "inventor_first_name", "inventor_name_first", "first_name" });
        result.LastName = ReadText(row, new string[] { "inventor_last_name", "inventor_name_last", "last_name" });
        result.GenderCode = ReadText(row, new string[] { "inventor_gender", "inventor_gender_code", "gender_code", "gender" });
        ApplyEntityDate(ReadText(row, new string[] { "first_seen_date", "inventor_first_seen_date" }), sourceName, "first_seen_date", row, result, null, null, 1);
        ApplyEntityDate(ReadText(row, new string[] { "last_seen_date", "inventor_last_seen_date" }), sourceName, "last_seen_date", row, result, null, null, 2);
        ApplyInventorInteger(ReadText(row, new string[] { "assignee_count" }), sourceName, "assignee_count", row, result, 1);
        ApplyInventorInteger(ReadText(row, new string[] { "patent_count" }), sourceName, "patent_count", row, result, 2);
        ApplyInventorInteger(ReadText(row, new string[] { "years_active" }), sourceName, "years_active", row, result, 3);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewAssignee ParseAssignee(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "assignee_id", "assignee_first_name", "assignee_individual_name_first", "assignee_last_name", "assignee_individual_name_last", "assignee_organization", "assignee_type", "first_seen_date", "last_seen_date", "inventor_count", "patent_count", "years_active", "resource_reference", "assignee" };
        PatentsViewAssignee result = new PatentsViewAssignee();
        result.AssigneeIdentifier = ReadText(row, new string[] { "assignee_id", "assignee_identifier" });
        result.ResourceReference = ReadText(row, new string[] { "resource_reference", "assignee_href", "assignee" });
        result.FirstName = ReadText(row, new string[] { "assignee_first_name", "assignee_individual_name_first", "first_name" });
        result.LastName = ReadText(row, new string[] { "assignee_last_name", "assignee_individual_name_last", "last_name" });
        result.Organization = ReadText(row, new string[] { "assignee_organization", "organization" });
        result.AssigneeType = ReadText(row, new string[] { "assignee_type", "type" });
        ApplyEntityDate(ReadText(row, new string[] { "first_seen_date", "assignee_first_seen_date" }), sourceName, "first_seen_date", row, null, result, null, 1);
        ApplyEntityDate(ReadText(row, new string[] { "last_seen_date", "assignee_last_seen_date" }), sourceName, "last_seen_date", row, null, result, null, 2);
        ApplyAssigneeInteger(ReadText(row, new string[] { "inventor_count" }), sourceName, "inventor_count", row, result, 1);
        ApplyAssigneeInteger(ReadText(row, new string[] { "patent_count" }), sourceName, "patent_count", row, result, 2);
        ApplyAssigneeInteger(ReadText(row, new string[] { "years_active" }), sourceName, "years_active", row, result, 3);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewApplicant ParseApplicant(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "applicant_id", "applicant", "patent_id", "document_number", "application_id", "applicant_first_name", "applicant_name_first", "us_party_name_first", "applicant_last_name", "applicant_name_last", "us_party_name_last", "applicant_organization", "us_party_organization", "applicant_type", "us_party_type", "applicant_designation", "us_party_designation", "applicant_authority", "location_id", "applicant_sequence", "us_party_sequence" };
        PatentsViewApplicant result = new PatentsViewApplicant();
        result.ApplicantIdentifier = ReadText(row, new string[] { "applicant_id", "applicant_identifier", "applicant" });
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        result.ApplicationIdentifier = ReadText(row, new string[] { "application_id", "application_number" });
        result.FirstName = ReadText(row, new string[] { "applicant_first_name", "applicant_name_first", "us_party_name_first", "first_name" });
        result.LastName = ReadText(row, new string[] { "applicant_last_name", "applicant_name_last", "us_party_name_last", "last_name" });
        result.Organization = ReadText(row, new string[] { "applicant_organization", "us_party_organization", "organization" });
        result.ApplicantType = ReadText(row, new string[] { "applicant_type", "us_party_type", "type" });
        result.Designation = ReadText(row, new string[] { "applicant_designation", "us_party_designation", "designation" });
        result.Authority = ReadText(row, new string[] { "applicant_authority", "authority" });
        result.LocationIdentifier = ReadText(row, new string[] { "location_id", "location_identifier" });
        ApplyApplicantInteger(ReadText(row, new string[] { "applicant_sequence", "us_party_sequence", "sequence" }), sourceName, "applicant_sequence", row, result);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewLocation ParseLocation(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "location_id", "location_city", "location_name", "location_county", "location_county_fips", "location_state", "location_state_fips", "location_country", "location_type", "location_place_type", "location_latitude", "location_longitude", "assignee_count", "patent_count", "inventor_count" };
        PatentsViewLocation result = new PatentsViewLocation();
        result.LocationIdentifier = ReadText(row, new string[] { "location_id", "location_identifier" });
        result.Name = ReadText(row, new string[] { "location_city", "location_name", "city", "name" });
        result.County = ReadText(row, new string[] { "location_county", "county" });
        result.CountyFipsCode = ReadText(row, new string[] { "location_county_fips", "county_fips" });
        result.State = ReadText(row, new string[] { "location_state", "state" });
        result.StateFipsCode = ReadText(row, new string[] { "location_state_fips", "state_fips" });
        result.Country = ReadText(row, new string[] { "location_country", "country" });
        result.PlaceType = ReadText(row, new string[] { "location_type", "location_place_type", "place_type" });
        ApplyLocationDouble(ReadText(row, new string[] { "location_latitude", "latitude" }), sourceName, "location_latitude", row, result, 1);
        ApplyLocationDouble(ReadText(row, new string[] { "location_longitude", "longitude" }), sourceName, "location_longitude", row, result, 2);
        ApplyLocationInteger(ReadText(row, new string[] { "assignee_count" }), sourceName, "assignee_count", row, result, 1);
        ApplyLocationInteger(ReadText(row, new string[] { "patent_count" }), sourceName, "patent_count", row, result, 2);
        ApplyLocationInteger(ReadText(row, new string[] { "inventor_count" }), sourceName, "inventor_count", row, result, 3);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewAttorney ParseAttorney(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "attorney_id", "lawyer_id", "attorney_first_name", "attorney_last_name", "attorney_organization", "first_seen_date", "last_seen_date", "inventor_count", "patent_count", "years_active" };
        PatentsViewAttorney result = new PatentsViewAttorney();
        result.AttorneyIdentifier = ReadText(row, new string[] { "attorney_id", "lawyer_id", "attorney_identifier" });
        result.FirstName = ReadText(row, new string[] { "attorney_first_name", "lawyer_first_name", "first_name" });
        result.LastName = ReadText(row, new string[] { "attorney_last_name", "lawyer_last_name", "last_name" });
        result.Organization = ReadText(row, new string[] { "attorney_organization", "lawyer_organization", "organization" });
        ApplyEntityDate(ReadText(row, new string[] { "first_seen_date" }), sourceName, "first_seen_date", row, null, null, result, 1);
        ApplyEntityDate(ReadText(row, new string[] { "last_seen_date" }), sourceName, "last_seen_date", row, null, null, result, 2);
        ApplyAttorneyInteger(ReadText(row, new string[] { "inventor_count" }), sourceName, "inventor_count", row, result, 1);
        ApplyAttorneyInteger(ReadText(row, new string[] { "patent_count" }), sourceName, "patent_count", row, result, 2);
        ApplyAttorneyInteger(ReadText(row, new string[] { "years_active" }), sourceName, "years_active", row, result, 3);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewCpcClassification ParseCpc(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "cpc_id", "cpc_level", "cpc_title", "cpc_section", "cpc_class", "cpc_class_id", "cpc_subclass", "cpc_subclass_id", "cpc_group", "cpc_group_id", "cpc_type", "cpc_sequence" };
        PatentsViewCpcClassification result = new PatentsViewCpcClassification();
        result.ClassificationIdentifier = ReadText(row, new string[] { "cpc_id", "classification_id" });
        result.Level = ReadText(row, new string[] { "cpc_level", "level" });
        result.Title = ReadText(row, new string[] { "cpc_title", "title" });
        result.Section = ReadText(row, new string[] { "cpc_section", "section_id", "section" });
        result.Class = ReadText(row, new string[] { "cpc_class", "class" });
        result.ClassIdentifier = ReadText(row, new string[] { "cpc_class_id", "class_id" });
        result.Subclass = ReadText(row, new string[] { "cpc_subclass", "subclass" });
        result.SubclassIdentifier = ReadText(row, new string[] { "cpc_subclass_id", "subclass_id" });
        result.Group = ReadText(row, new string[] { "cpc_group", "group" });
        result.GroupIdentifier = ReadText(row, new string[] { "cpc_group_id", "group_id" });
        result.ClassificationType = ReadText(row, new string[] { "cpc_type", "classification_type" });
        ApplyCpcInteger(ReadText(row, new string[] { "cpc_sequence", "sequence" }), sourceName, "cpc_sequence", row, result);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewIpcClassification ParseIpc(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "ipc_id", "ipc_section", "ipc_class", "ipc_subclass", "ipc_main_group", "ipc_subgroup", "ipc_data_source", "ipc_classification_value", "ipc_symbol_position", "ipc_classification_level", "ipc_classification_status", "ipc_action_date", "ipc_sequence" };
        PatentsViewIpcClassification result = new PatentsViewIpcClassification();
        result.ClassificationIdentifier = ReadText(row, new string[] { "ipc_id", "classification_id" });
        result.Section = ReadText(row, new string[] { "ipc_section", "section" });
        result.Class = ReadText(row, new string[] { "ipc_class", "class" });
        result.Subclass = ReadText(row, new string[] { "ipc_subclass", "subclass" });
        result.MainGroup = ReadText(row, new string[] { "ipc_main_group", "main_group" });
        result.Subgroup = ReadText(row, new string[] { "ipc_subgroup", "subgroup" });
        result.DataSource = ReadText(row, new string[] { "ipc_data_source", "data_source" });
        result.ClassificationValue = ReadText(row, new string[] { "ipc_classification_value", "classification_value" });
        result.SymbolPosition = ReadText(row, new string[] { "ipc_symbol_position", "symbol_position" });
        result.ClassificationLevel = ReadText(row, new string[] { "ipc_classification_level", "classification_level" });
        result.ClassificationStatus = ReadText(row, new string[] { "ipc_classification_status", "classification_status" });
        ApplyIpcDate(ReadText(row, new string[] { "ipc_action_date", "action_date" }), sourceName, "ipc_action_date", row, result);
        ApplyIpcInteger(ReadText(row, new string[] { "ipc_sequence", "sequence" }), sourceName, "ipc_sequence", row, result);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewUspcClassification ParseUspc(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "uspc_id", "uspc_level", "uspc_title", "uspc_mainclass", "uspc_mainclass_id", "uspc_subclass", "uspc_subclass_id", "uspc_sequence" };
        PatentsViewUspcClassification result = new PatentsViewUspcClassification();
        result.ClassificationIdentifier = ReadText(row, new string[] { "uspc_id", "classification_id" });
        result.Level = ReadText(row, new string[] { "uspc_level", "level" });
        result.Title = ReadText(row, new string[] { "uspc_title", "title" });
        result.MainClass = ReadText(row, new string[] { "uspc_mainclass", "mainclass_id", "main_class" });
        result.MainClassIdentifier = ReadText(row, new string[] { "uspc_mainclass_id", "main_class_id" });
        result.Subclass = ReadText(row, new string[] { "uspc_subclass", "subclass_id", "subclass" });
        result.SubclassIdentifier = ReadText(row, new string[] { "uspc_subclass_id", "subclass_identifier" });
        ApplyUspcInteger(ReadText(row, new string[] { "uspc_sequence", "sequence" }), sourceName, "uspc_sequence", row, result);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewWipoClassification ParseWipo(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "wipo_id", "wipo_sector_title", "sector_title", "wipo_field_title", "field_title", "wipo_sequence" };
        PatentsViewWipoClassification result = new PatentsViewWipoClassification();
        result.ClassificationIdentifier = ReadText(row, new string[] { "wipo_id", "classification_id" });
        result.SectorTitle = ReadText(row, new string[] { "wipo_sector_title", "sector_title" });
        result.FieldTitle = ReadText(row, new string[] { "wipo_field_title", "field_title" });
        ApplyWipoInteger(ReadText(row, new string[] { "wipo_sequence", "sequence" }), sourceName, "wipo_sequence", row, result);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewCitation ParseCitation(DelimitedRow row, string sourceName, PatentDataProvenance provenance, string citationType)
    {
        string[] known = new string[] { "patent_id", "document_number", "source_resource_reference", "patent", "citation_type", "citation_patent_id", "cited_patent_id", "citation_document_number", "cited_document_number", "citation_number", "cited_resource_reference", "citation_patent", "citation_category", "citation_date", "citation_name", "citation_country", "citation_kind", "citation_wipo_kind", "citation_sequence", "reference_sequence", "citation_text", "reference_text" };
        PatentsViewCitation result = new PatentsViewCitation();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        result.SourceResourceReference = ReadText(row, new string[] { "source_resource_reference", "source_href", "patent" });
        result.CitationType = ReadText(row, new string[] { "citation_type" }) ?? citationType;
        result.CitedPatentIdentifier = ReadText(row, new string[] { "citation_patent_id", "cited_patent_id" });
        result.CitedDocumentNumber = ReadText(row, new string[] { "citation_document_number", "cited_document_number" });
        result.CitationNumber = ReadText(row, new string[] { "citation_number" });
        result.CitedResourceReference = ReadText(row, new string[] { "cited_resource_reference", "citation_href", "citation_patent" });
        result.CitationCategory = ReadText(row, new string[] { "citation_category", "category" });
        ApplyCitationDate(ReadText(row, new string[] { "citation_date" }), sourceName, "citation_date", row, result);
        result.CitationName = ReadText(row, new string[] { "citation_name", "name" });
        result.Country = ReadText(row, new string[] { "citation_country", "country" });
        result.KindCode = ReadText(row, new string[] { "citation_kind", "citation_wipo_kind", "kind_code" });
        ApplyCitationInteger(ReadText(row, new string[] { "citation_sequence", "reference_sequence", "sequence" }), sourceName, "citation_sequence", row, result);
        result.ReferenceText = ReadText(row, new string[] { "citation_text", "reference_text" });
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewRelatedApplication ParseRelatedApplication(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "patent_id", "document_number", "related_application_text", "related_text", "relation_text" };
        PatentsViewRelatedApplication result = new PatentsViewRelatedApplication();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        result.RelatedText = ReadText(row, new string[] { "related_application_text", "related_text", "relation_text", "text" });
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewGovernmentInterest ParseGovernmentInterest(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "patent_id", "document_number", "government_interest", "government_interest_statement", "award_number", "federal_agency_name", "level_one", "level_two", "level_three" };
        PatentsViewGovernmentInterest result = new PatentsViewGovernmentInterest();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        result.Statement = ReadText(row, new string[] { "government_interest", "government_interest_statement", "statement" });
        string? award = ReadText(row, new string[] { "award_number", "contract_award_number" });
        if (award != null)
        {
            result.AwardNumbers = new string[] { award };
        }

        PatentsViewGovernmentOrganization organization = new PatentsViewGovernmentOrganization();
        organization.FederalAgencyName = ReadText(row, new string[] { "federal_agency_name", "organization_name" });
        organization.LevelOne = ReadText(row, new string[] { "level_one", "organization_level_one" });
        organization.LevelTwo = ReadText(row, new string[] { "level_two", "organization_level_two" });
        organization.LevelThree = ReadText(row, new string[] { "level_three", "organization_level_three" });
        if (organization.FederalAgencyName != null || organization.LevelOne != null || organization.LevelTwo != null || organization.LevelThree != null)
        {
            result.Organizations = new PatentsViewGovernmentOrganization[] { organization };
        }

        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewClaim ParseClaim(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "patent_id", "document_number", "claim_number", "claim_sequence", "claim_text", "exemplary", "depends_on_claim", "claim_dependent" };
        PatentsViewClaim result = new PatentsViewClaim();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        result.ClaimNumber = ReadText(row, new string[] { "claim_number", "claim_id" });
        ApplyClaimInteger(ReadText(row, new string[] { "claim_sequence", "sequence" }), sourceName, "claim_sequence", row, result);
        result.Text = ReadText(row, new string[] { "claim_text", "text" });
        ApplyClaimBoolean(ReadText(row, new string[] { "exemplary", "is_exemplary" }), sourceName, "exemplary", row, result);
        result.DependsOnClaim = ReadText(row, new string[] { "depends_on_claim", "claim_dependent", "dependent" });
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewSummary ParseSummary(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        PatentsViewSummary result = new PatentsViewSummary();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        result.Text = ReadText(row, new string[] { "summary_text", "brief_summary_text", "text" });
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, new string[] { "patent_id", "document_number", "summary_text", "brief_summary_text" });
        return result;
    }

    internal static PatentsViewDescription ParseDescription(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        PatentsViewDescription result = new PatentsViewDescription();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        result.Text = ReadText(row, new string[] { "description_text", "detail_description_text", "text" });
        ApplyDescriptionInteger(ReadText(row, new string[] { "description_length", "length" }), sourceName, "description_length", row, result);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, new string[] { "patent_id", "document_number", "description_text", "detail_description_text", "description_length" });
        return result;
    }

    internal static PatentsViewDrawingDescription ParseDrawingDescription(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        PatentsViewDrawingDescription result = new PatentsViewDrawingDescription();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        ApplyDrawingInteger(ReadText(row, new string[] { "drawing_sequence", "draw_desc_sequence", "sequence", "figure_number" }), sourceName, "draw_desc_sequence", row, result);
        result.Text = ReadText(row, new string[] { "drawing_description", "drawing_description_text", "draw_desc_text", "text" });
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, new string[] { "patent_id", "document_number", "drawing_sequence", "draw_desc_sequence", "figure_number", "drawing_description", "drawing_description_text", "draw_desc_text" });
        return result;
    }

    internal static PatentsViewEntityCrosswalk ParseEntityCrosswalk(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "entity_type", "source_entity_id", "persistent_entity_id", "old_id", "original_id", "source_id", "inventor_id", "assignee_id", "applicant_id", "lawyer_id", "attorney_id", "new_id", "disambiguated_id", "entity_id", "persistent_inventor_id", "persistent_assignee_id", "persistent_applicant_id", "persistent_lawyer_id", "persistent_attorney_id", "new_inventor_id", "new_assignee_id", "new_applicant_id", "new_lawyer_id", "new_attorney_id", "disambiguation_version", "valid_from", "valid_to", "is_current" };
        PatentsViewEntityCrosswalk result = new PatentsViewEntityCrosswalk();
        result.EntityType = ReadText(row, new string[] { "entity_type" }) ?? InferEntityType(sourceName);
        result.SourceEntityIdentifier = ReadText(row, new string[] { "source_entity_id", "old_id", "original_id", "source_id", "inventor_id", "assignee_id", "applicant_id", "lawyer_id", "attorney_id" });
        result.PersistentEntityIdentifier = ReadText(row, new string[] { "persistent_entity_id", "new_id", "disambiguated_id", "entity_id", "persistent_inventor_id", "persistent_assignee_id", "persistent_applicant_id", "persistent_lawyer_id", "persistent_attorney_id", "new_inventor_id", "new_assignee_id", "new_applicant_id", "new_lawyer_id", "new_attorney_id" });
        result.DisambiguationVersion = ReadText(row, new string[] { "disambiguation_version", "version" });
        ApplyCrosswalkDate(ReadText(row, new string[] { "valid_from" }), sourceName, "valid_from", row, result, 1);
        ApplyCrosswalkDate(ReadText(row, new string[] { "valid_to" }), sourceName, "valid_to", row, result, 2);
        ApplyCrosswalkBoolean(ReadText(row, new string[] { "is_current" }), sourceName, "is_current", row, result);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static PatentsViewGrantPublicationCrosswalk ParseGrantPublicationCrosswalk(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "patent_id", "application_id", "application_number", "document_number", "publication_number", "is_current_document_number", "current_document_number_flag", "is_current_patent_id", "current_patent_id_flag" };
        PatentsViewGrantPublicationCrosswalk result = new PatentsViewGrantPublicationCrosswalk();
        result.PatentIdentifier = ReadText(row, new string[] { "patent_id", "patent_number" });
        result.ApplicationIdentifier = ReadText(row, new string[] { "application_id", "application_number" });
        result.DocumentNumber = ReadText(row, new string[] { "document_number", "publication_number" });
        ApplyGrantCrosswalkBoolean(ReadText(row, new string[] { "is_current_document_number", "current_document_number_flag" }), sourceName, "current_document_number_flag", row, result, 1);
        ApplyGrantCrosswalkBoolean(ReadText(row, new string[] { "is_current_patent_id", "current_patent_id_flag" }), sourceName, "current_patent_id_flag", row, result, 2);
        result.Provenance = CreateProvenance(provenance, sourceName);
        CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static string? ReadText(DelimitedRow row, string[] names)
    {
        int headerIndex;
        for (headerIndex = 0; headerIndex < row.Headers.Length; headerIndex += 1)
        {
            string normalizedHeader = NormalizeName(row.Headers[headerIndex]);
            int nameIndex;
            for (nameIndex = 0; nameIndex < names.Length; nameIndex += 1)
            {
                if (normalizedHeader.Equals(NormalizeName(names[nameIndex]), global::System.StringComparison.Ordinal))
                {
                    if (headerIndex >= row.Values.Length || global::System.String.IsNullOrWhiteSpace(row.Values[headerIndex]))
                    {
                        return null;
                    }

                    return row.Values[headerIndex].Trim();
                }
            }
        }

        return null;
    }

    internal static bool HasHeader(DelimitedRow row, string name)
    {
        string expected = NormalizeName(name);
        int index;
        for (index = 0; index < row.Headers.Length; index += 1)
        {
            if (NormalizeName(row.Headers[index]).Equals(expected, global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeName(string value)
    {
        global::System.Text.StringBuilder result = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            if (global::System.Char.IsLetterOrDigit(value[index]))
            {
                result.Append(global::System.Char.ToLowerInvariant(value[index]));
            }
        }

        return result.ToString();
    }

    internal static bool ContainsName(string value, string expected)
    {
        return value.IndexOf(NormalizeName(expected), global::System.StringComparison.Ordinal) >= 0;
    }

    internal static void CopyAdditionalFields(DelimitedRow row, global::System.Collections.Generic.Dictionary<string, string> destination, string[] knownNames)
    {
        int index;
        for (index = 0; index < row.Headers.Length && index < row.Values.Length; index += 1)
        {
            string value = row.Values[index];
            if (!global::System.String.IsNullOrWhiteSpace(value) && !IsKnownName(row.Headers[index], knownNames))
            {
                destination[row.Headers[index]] = value;
            }
        }
    }

    internal static bool IsKnownName(string header, string[] knownNames)
    {
        string value = NormalizeName(header);
        int index;
        for (index = 0; index < knownNames.Length; index += 1)
        {
            if (value.Equals(NormalizeName(knownNames[index]), global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static PatentDataProvenance CreateProvenance(PatentDataProvenance source, string tableHint)
    {
        PatentDataProvenance result = OdpJsonParser.CopyProvenance(source);
        result.Source = DataProvenance.PatentsView;
        result.IsAuthoritative = false;
        result.DatasetIdentifier = NormalizeDatasetIdentifier(tableHint);
        return result;
    }

    internal static string NormalizeDatasetIdentifier(string tableHint)
    {
        string fileName = global::System.IO.Path.GetFileName(tableHint ?? string.Empty);
        string lower = fileName.ToLowerInvariant();
        if (lower.EndsWith(".gz", global::System.StringComparison.Ordinal))
        {
            fileName = fileName.Substring(0, fileName.Length - 3);
            lower = fileName.ToLowerInvariant();
        }

        if (lower.EndsWith(".tsv", global::System.StringComparison.Ordinal) || lower.EndsWith(".txt", global::System.StringComparison.Ordinal) || lower.EndsWith(".csv", global::System.StringComparison.Ordinal))
        {
            fileName = fileName.Substring(0, fileName.Length - 4);
        }

        return fileName;
    }

    internal static string? InferEntityType(string sourceName)
    {
        string value = NormalizeName(sourceName);
        if (ContainsName(value, "inventor"))
        {
            return "inventor";
        }

        if (ContainsName(value, "assignee"))
        {
            return "assignee";
        }

        if (ContainsName(value, "applicant"))
        {
            return "applicant";
        }

        if (ContainsName(value, "attorney") || ContainsName(value, "lawyer"))
        {
            return "attorney";
        }

        return null;
    }

    internal static JsonIntegerParseResult ParseIntegerValue(string? text, string sourceName, string path, DelimitedRow row)
    {
        JsonIntegerParseResult result = new JsonIntegerParseResult();
        if (text == null)
        {
            return result;
        }

        result = JsonFieldReader.ParseSignedInteger(text);
        if (!result.IsValid || result.Value < global::System.Int32.MinValue || result.Value > global::System.Int32.MaxValue)
        {
            throw ParseFailure("The PatentsView grant TSV integer is invalid.", sourceName, path, row);
        }

        return result;
    }

    internal static JsonDateParseResult ParseDateValue(string? text, string sourceName, string path, DelimitedRow row)
    {
        JsonDateParseResult result = new JsonDateParseResult();
        if (text == null)
        {
            return result;
        }

        result = JsonFieldReader.ParseFlexibleDate(text);
        if (!result.IsValid)
        {
            throw ParseFailure("The PatentsView grant TSV date is invalid.", sourceName, path, row);
        }

        return result;
    }

    internal static int ParseBooleanValue(string? text, string sourceName, string path, DelimitedRow row)
    {
        if (text == null)
        {
            return -1;
        }

        int result = TsdrXmlParser.ParseBoolean(text);
        if (result < 0)
        {
            throw ParseFailure("The PatentsView grant TSV Boolean is invalid.", sourceName, path, row);
        }

        return result;
    }

    internal static double ParseDoubleValue(string? text, string sourceName, string path, DelimitedRow row)
    {
        if (text == null)
        {
            return 0.0;
        }

        int index = 0;
        int digitCount = 0;
        int decimalCount = 0;
        if (text.Length > 0 && (text[0] == '-' || text[0] == '+'))
        {
            index = 1;
        }

        while (index < text.Length)
        {
            char character = text[index];
            if (character >= '0' && character <= '9')
            {
                digitCount += 1;
            }
            else if (character == '.')
            {
                decimalCount += 1;
            }
            else
            {
                throw ParseFailure("The PatentsView grant TSV decimal is invalid.", sourceName, path, row);
            }

            index += 1;
        }

        if (digitCount == 0 || decimalCount > 1)
        {
            throw ParseFailure("The PatentsView grant TSV decimal is invalid.", sourceName, path, row);
        }

        return global::System.Convert.ToDouble(text, global::System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void ApplyDate(string? text, string sourceName, string path, DelimitedRow row, PatentsViewPatent target, int field)
    {
        JsonDateParseResult parsed = ParseDateValue(text, sourceName, path, row);
        if (!parsed.IsValid)
        {
            return;
        }

        if (field == 1)
        {
            target.GrantDate = parsed.Value;
            target.HasGrantDate = true;
        }
        else
        {
            target.EarliestApplicationDate = parsed.Value;
            target.HasEarliestApplicationDate = true;
        }
    }

    internal static void ApplyPatentInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewPatent target, int field)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (!parsed.IsValid)
        {
            return;
        }

        int value = (int)parsed.Value;
        switch (field)
        {
            case 1: target.GrantYear = value; target.HasGrantYear = true; break;
            case 2: target.DescriptionLength = value; target.HasDescriptionLength = true; break;
            case 3: target.UnitedStatesPatentCitationCount = value; target.HasUnitedStatesPatentCitationCount = true; break;
            case 4: target.UnitedStatesApplicationCitationCount = value; target.HasUnitedStatesApplicationCitationCount = true; break;
            case 5: target.ForeignDocumentCitationCount = value; target.HasForeignDocumentCitationCount = true; break;
            case 6: target.TotalDocumentCitationCount = value; target.HasTotalDocumentCitationCount = true; break;
            case 7: target.CitedByUnitedStatesPatentCount = value; target.HasCitedByUnitedStatesPatentCount = true; break;
            case 8: target.ProcessingDays = value; target.HasProcessingDays = true; break;
            case 9: target.TermExtensionDays = value; target.HasTermExtensionDays = true; break;
        }
    }

    internal static void ApplyPatentBoolean(string? text, string sourceName, string path, DelimitedRow row, PatentsViewPatent target)
    {
        int parsed = ParseBooleanValue(text, sourceName, path, row);
        if (parsed >= 0)
        {
            target.IsWithdrawn = parsed == 1;
            target.HasIsWithdrawn = true;
        }
    }

    internal static void ApplyApplicationDate(string? text, string sourceName, string path, DelimitedRow row, PatentsViewPatentApplication target)
    {
        JsonDateParseResult parsed = ParseDateValue(text, sourceName, path, row);
        if (parsed.IsValid)
        {
            target.FilingDate = parsed.Value;
            target.HasFilingDate = true;
        }
    }

    internal static void ApplyApplicationBoolean(string? text, string sourceName, string path, DelimitedRow row, PatentsViewPatentApplication target)
    {
        int parsed = ParseBooleanValue(text, sourceName, path, row);
        if (parsed >= 0)
        {
            target.Rule47 = parsed == 1;
            target.HasRule47 = true;
        }
    }

    internal static void ApplyEntityDate(string? text, string sourceName, string path, DelimitedRow row, PatentsViewInventor? inventor, PatentsViewAssignee? assignee, PatentsViewAttorney? attorney, int field)
    {
        JsonDateParseResult parsed = ParseDateValue(text, sourceName, path, row);
        if (!parsed.IsValid)
        {
            return;
        }

        if (inventor != null && field == 1) { inventor.FirstSeenDate = parsed.Value; inventor.HasFirstSeenDate = true; }
        if (inventor != null && field == 2) { inventor.LastSeenDate = parsed.Value; inventor.HasLastSeenDate = true; }
        if (assignee != null && field == 1) { assignee.FirstSeenDate = parsed.Value; assignee.HasFirstSeenDate = true; }
        if (assignee != null && field == 2) { assignee.LastSeenDate = parsed.Value; assignee.HasLastSeenDate = true; }
        if (attorney != null && field == 1) { attorney.FirstSeenDate = parsed.Value; attorney.HasFirstSeenDate = true; }
        if (attorney != null && field == 2) { attorney.LastSeenDate = parsed.Value; attorney.HasLastSeenDate = true; }
    }

    internal static void ApplyInventorInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewInventor target, int field)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (!parsed.IsValid) { return; }
        int value = (int)parsed.Value;
        if (field == 1) { target.AssigneeCount = value; target.HasAssigneeCount = true; }
        if (field == 2) { target.PatentCount = value; target.HasPatentCount = true; }
        if (field == 3) { target.YearsActive = value; target.HasYearsActive = true; }
    }

    internal static void ApplyAssigneeInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewAssignee target, int field)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (!parsed.IsValid) { return; }
        int value = (int)parsed.Value;
        if (field == 1) { target.InventorCount = value; target.HasInventorCount = true; }
        if (field == 2) { target.PatentCount = value; target.HasPatentCount = true; }
        if (field == 3) { target.YearsActive = value; target.HasYearsActive = true; }
    }

    internal static void ApplyAttorneyInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewAttorney target, int field)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (!parsed.IsValid) { return; }
        int value = (int)parsed.Value;
        if (field == 1) { target.InventorCount = value; target.HasInventorCount = true; }
        if (field == 2) { target.PatentCount = value; target.HasPatentCount = true; }
        if (field == 3) { target.YearsActive = value; target.HasYearsActive = true; }
    }

    internal static void ApplyApplicantInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewApplicant target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid) { target.Sequence = (int)parsed.Value; target.HasSequence = true; }
    }

    internal static void ApplyLocationDouble(string? text, string sourceName, string path, DelimitedRow row, PatentsViewLocation target, int field)
    {
        if (text == null) { return; }
        double value = ParseDoubleValue(text, sourceName, path, row);
        if (field == 1) { target.Latitude = value; target.HasLatitude = true; }
        if (field == 2) { target.Longitude = value; target.HasLongitude = true; }
    }

    internal static void ApplyLocationInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewLocation target, int field)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (!parsed.IsValid) { return; }
        int value = (int)parsed.Value;
        if (field == 1) { target.AssigneeCount = value; target.HasAssigneeCount = true; }
        if (field == 2) { target.PatentCount = value; target.HasPatentCount = true; }
        if (field == 3) { target.InventorCount = value; target.HasInventorCount = true; }
    }

    internal static void ApplyCpcInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewCpcClassification target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid) { target.Sequence = (int)parsed.Value; target.HasSequence = true; }
    }

    internal static void ApplyIpcDate(string? text, string sourceName, string path, DelimitedRow row, PatentsViewIpcClassification target)
    {
        JsonDateParseResult parsed = ParseDateValue(text, sourceName, path, row);
        if (parsed.IsValid) { target.ActionDate = parsed.Value; target.HasActionDate = true; }
    }

    internal static void ApplyIpcInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewIpcClassification target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid) { target.Sequence = (int)parsed.Value; target.HasSequence = true; }
    }

    internal static void ApplyUspcInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewUspcClassification target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid) { target.Sequence = (int)parsed.Value; target.HasSequence = true; }
    }

    internal static void ApplyWipoInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewWipoClassification target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid) { target.Sequence = (int)parsed.Value; target.HasSequence = true; }
    }

    internal static void ApplyCitationDate(string? text, string sourceName, string path, DelimitedRow row, PatentsViewCitation target)
    {
        JsonDateParseResult parsed = ParseDateValue(text, sourceName, path, row);
        if (parsed.IsValid) { target.CitationDate = parsed.Value; target.HasCitationDate = true; }
    }

    internal static void ApplyCitationInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewCitation target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid)
        {
            if (parsed.Value < 0L)
            {
                throw ParseFailure("The PatentsView citation sequence cannot be negative.", sourceName, path, row);
            }

            target.Sequence = (int)parsed.Value;
            target.HasSequence = true;
        }
    }

    internal static void ApplyClaimInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewClaim target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid)
        {
            if (parsed.Value < 0L)
            {
                throw ParseFailure("The PatentsView claim sequence cannot be negative.", sourceName, path, row);
            }

            target.Sequence = (int)parsed.Value;
            target.HasSequence = true;
        }
    }

    internal static void ApplyClaimBoolean(string? text, string sourceName, string path, DelimitedRow row, PatentsViewClaim target)
    {
        int parsed = ParseBooleanValue(text, sourceName, path, row);
        if (parsed >= 0) { target.IsExemplary = parsed == 1; target.HasIsExemplary = true; }
    }

    internal static void ApplyDescriptionInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewDescription target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid)
        {
            if (parsed.Value < 0L)
            {
                throw ParseFailure("The PatentsView description length cannot be negative.", sourceName, path, row);
            }

            target.Length = (int)parsed.Value;
            target.HasLength = true;
        }
    }

    internal static void ApplyDrawingInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewDrawingDescription target)
    {
        JsonIntegerParseResult parsed = ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid)
        {
            if (parsed.Value < 1L)
            {
                throw ParseFailure("The PatentsView drawing-description sequence must be positive.", sourceName, path, row);
            }

            target.Sequence = (int)parsed.Value;
            target.HasSequence = true;
        }
    }

    internal static void ApplyCrosswalkDate(string? text, string sourceName, string path, DelimitedRow row, PatentsViewEntityCrosswalk target, int field)
    {
        JsonDateParseResult parsed = ParseDateValue(text, sourceName, path, row);
        if (!parsed.IsValid) { return; }
        if (field == 1) { target.ValidFrom = parsed.Value; target.HasValidFrom = true; }
        if (field == 2) { target.ValidTo = parsed.Value; target.HasValidTo = true; }
    }

    internal static void ApplyCrosswalkBoolean(string? text, string sourceName, string path, DelimitedRow row, PatentsViewEntityCrosswalk target)
    {
        int parsed = ParseBooleanValue(text, sourceName, path, row);
        if (parsed >= 0) { target.IsCurrent = parsed == 1; target.HasIsCurrent = true; }
    }

    internal static void ApplyGrantCrosswalkBoolean(string? text, string sourceName, string path, DelimitedRow row, PatentsViewGrantPublicationCrosswalk target, int field)
    {
        int parsed = ParseBooleanValue(text, sourceName, path, row);
        if (parsed < 0) { return; }
        if (field == 1) { target.IsCurrentDocumentNumber = parsed == 1; target.HasIsCurrentDocumentNumber = true; }
        if (field == 2) { target.IsCurrentPatentIdentifier = parsed == 1; target.HasIsCurrentPatentIdentifier = true; }
    }

    internal static void ValidateRow(DelimitedRow row, PatentDataProvenance provenance)
    {
        if (row == null)
        {
            throw new ValidationException("A PatentsView grant TSV row is required.", "row", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("PatentsView grant TSV provenance is required.", "provenance", "REQUIRED", null);
        }

        if (row.Headers == null || row.Headers.Length == 0)
        {
            throw new ValidationException("PatentsView grant TSV headers are required.", "Headers", "REQUIRED", null);
        }

        if (row.Values == null)
        {
            throw new ValidationException("PatentsView grant TSV values are required.", "Values", "REQUIRED", null);
        }

        if (row.HasMissingColumns || row.HasExtraColumns || row.Values.Length != row.Headers.Length)
        {
            throw ParseFailure("The PatentsView grant TSV row does not match its header column count.", string.Empty, "row", row);
        }
    }

    internal static ParseException ParseFailure(string message, string sourceName, string path, DelimitedRow row)
    {
        return new ParseException(message, DocumentFormat.Text, sourceName, path, row.StartOffset, true, (int)row.StartLineNumber, row.StartLineNumber > 0L, null);
    }
}
