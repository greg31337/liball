namespace uspto;

internal enum PatentsViewPreGrantTable
{
    Unknown = 0,
    Publication = 1,
    Inventor = 2,
    Assignee = 3,
    Applicant = 4,
    Location = 5,
    Cpc = 6,
    Ipc = 7,
    Uspc = 8,
    Wipo = 9,
    UsPatentCitation = 10,
    UsApplicationCitation = 11,
    ForeignCitation = 12,
    OtherReference = 13,
    RelatedApplication = 14,
    Claim = 15,
    Summary = 16,
    Description = 17,
    DrawingDescription = 18,
    EntityCrosswalk = 19,
    GrantPublicationCrosswalk = 20
}

internal static class PatentsViewPreGrantBulkParser
{
    internal static object ParseRow(DelimitedRow row, string tableHint, PatentDataProvenance provenance)
    {
        PatentsViewGrantBulkParser.ValidateRow(row, provenance);
        PatentsViewPreGrantTable table = ResolveTable(tableHint, row);
        switch (table)
        {
            case PatentsViewPreGrantTable.Publication:
                return ParsePublication(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Inventor:
                return PatentsViewGrantBulkParser.ParseInventor(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Assignee:
                return PatentsViewGrantBulkParser.ParseAssignee(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Applicant:
                return PatentsViewGrantBulkParser.ParseApplicant(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Location:
                return PatentsViewGrantBulkParser.ParseLocation(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Cpc:
                return PatentsViewGrantBulkParser.ParseCpc(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Ipc:
                return PatentsViewGrantBulkParser.ParseIpc(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Uspc:
                return PatentsViewGrantBulkParser.ParseUspc(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Wipo:
                return PatentsViewGrantBulkParser.ParseWipo(row, tableHint, provenance);
            case PatentsViewPreGrantTable.UsPatentCitation:
                return PatentsViewGrantBulkParser.ParseCitation(row, tableHint, provenance, "us_patent");
            case PatentsViewPreGrantTable.UsApplicationCitation:
                return PatentsViewGrantBulkParser.ParseCitation(row, tableHint, provenance, "us_application");
            case PatentsViewPreGrantTable.ForeignCitation:
                return PatentsViewGrantBulkParser.ParseCitation(row, tableHint, provenance, "foreign");
            case PatentsViewPreGrantTable.OtherReference:
                return PatentsViewGrantBulkParser.ParseCitation(row, tableHint, provenance, "other_reference");
            case PatentsViewPreGrantTable.RelatedApplication:
                return PatentsViewGrantBulkParser.ParseRelatedApplication(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Claim:
                return PatentsViewGrantBulkParser.ParseClaim(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Summary:
                return PatentsViewGrantBulkParser.ParseSummary(row, tableHint, provenance);
            case PatentsViewPreGrantTable.Description:
                return PatentsViewGrantBulkParser.ParseDescription(row, tableHint, provenance);
            case PatentsViewPreGrantTable.DrawingDescription:
                return PatentsViewGrantBulkParser.ParseDrawingDescription(row, tableHint, provenance);
            case PatentsViewPreGrantTable.EntityCrosswalk:
                return PatentsViewGrantBulkParser.ParseEntityCrosswalk(row, tableHint, provenance);
            case PatentsViewPreGrantTable.GrantPublicationCrosswalk:
                return PatentsViewGrantBulkParser.ParseGrantPublicationCrosswalk(row, tableHint, provenance);
            default:
                throw PatentsViewGrantBulkParser.ParseFailure("The PatentsView pre-grant TSV table could not be identified.", tableHint, "table", row);
        }
    }

    internal static bool CanParseRow(DelimitedRow row, string tableHint)
    {
        if (row == null || row.Headers == null || row.Headers.Length == 0)
        {
            return false;
        }

        return ResolveTable(tableHint, row) != PatentsViewPreGrantTable.Unknown;
    }

    internal static PatentsViewPreGrantTable ResolveTable(string tableHint, DelimitedRow row)
    {
        string value = PatentsViewGrantBulkParser.NormalizeName(tableHint ?? string.Empty);
        if (HasName(value, "grantedpregrantcrosswalk") || HasName(value, "publicationgrantcrosswalk") || HasName(value, "grantcrosswalk"))
        {
            return PatentsViewPreGrantTable.GrantPublicationCrosswalk;
        }

        if (HasName(value, "crosswalk") || HasName(value, "notdisambiguated"))
        {
            return PatentsViewPreGrantTable.EntityCrosswalk;
        }

        if (HasName(value, "usapplicationcitation"))
        {
            return PatentsViewPreGrantTable.UsApplicationCitation;
        }

        if (HasName(value, "uspatentcitation"))
        {
            return PatentsViewPreGrantTable.UsPatentCitation;
        }

        if (HasName(value, "foreigncitation"))
        {
            return PatentsViewPreGrantTable.ForeignCitation;
        }

        if (HasName(value, "otherreference"))
        {
            return PatentsViewPreGrantTable.OtherReference;
        }

        if (HasName(value, "relapp") || HasName(value, "relatedapplication"))
        {
            return PatentsViewPreGrantTable.RelatedApplication;
        }

        if (HasName(value, "drawdesc") || HasName(value, "drawingdescription"))
        {
            return PatentsViewPreGrantTable.DrawingDescription;
        }

        if (HasName(value, "detaildesc") || HasName(value, "descriptiontext"))
        {
            return PatentsViewPreGrantTable.Description;
        }

        if (HasName(value, "brfsum") || HasName(value, "briefsummary"))
        {
            return PatentsViewPreGrantTable.Summary;
        }

        if (HasName(value, "claim"))
        {
            return PatentsViewPreGrantTable.Claim;
        }

        if (HasName(value, "cpc"))
        {
            return PatentsViewPreGrantTable.Cpc;
        }

        if (HasName(value, "ipc"))
        {
            return PatentsViewPreGrantTable.Ipc;
        }

        if (HasName(value, "uspc"))
        {
            return PatentsViewPreGrantTable.Uspc;
        }

        if (HasName(value, "wipo"))
        {
            return PatentsViewPreGrantTable.Wipo;
        }

        if (HasName(value, "applicant") || HasName(value, "usparty"))
        {
            return PatentsViewPreGrantTable.Applicant;
        }

        if (HasName(value, "inventor"))
        {
            return PatentsViewPreGrantTable.Inventor;
        }

        if (HasName(value, "assignee"))
        {
            return PatentsViewPreGrantTable.Assignee;
        }

        if (HasName(value, "location"))
        {
            return PatentsViewPreGrantTable.Location;
        }

        if (HasName(value, "pgpublication") || HasName(value, "pgpublished") || value.Equals("pgpub", global::System.StringComparison.Ordinal) || value.Equals("publication", global::System.StringComparison.Ordinal))
        {
            return PatentsViewPreGrantTable.Publication;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "publication_title") && PatentsViewGrantBulkParser.HasHeader(row, "publication_date"))
        {
            return PatentsViewPreGrantTable.Publication;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "cpc_id"))
        {
            return PatentsViewPreGrantTable.Cpc;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "ipc_id"))
        {
            return PatentsViewPreGrantTable.Ipc;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "uspc_id"))
        {
            return PatentsViewPreGrantTable.Uspc;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "wipo_id"))
        {
            return PatentsViewPreGrantTable.Wipo;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "source_entity_id") && PatentsViewGrantBulkParser.HasHeader(row, "persistent_entity_id"))
        {
            return PatentsViewPreGrantTable.EntityCrosswalk;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "patent_id") && PatentsViewGrantBulkParser.HasHeader(row, "application_number") && PatentsViewGrantBulkParser.HasHeader(row, "document_number"))
        {
            return PatentsViewPreGrantTable.GrantPublicationCrosswalk;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "inventor_id"))
        {
            return PatentsViewPreGrantTable.Inventor;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "assignee_id"))
        {
            return PatentsViewPreGrantTable.Assignee;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "applicant_id") || PatentsViewGrantBulkParser.HasHeader(row, "us_party_name_first"))
        {
            return PatentsViewPreGrantTable.Applicant;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "location_id") && PatentsViewGrantBulkParser.HasHeader(row, "location_name"))
        {
            return PatentsViewPreGrantTable.Location;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "citation_patent_id"))
        {
            return PatentsViewPreGrantTable.UsPatentCitation;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "citation_document_number"))
        {
            return PatentsViewPreGrantTable.UsApplicationCitation;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "citation_country"))
        {
            return PatentsViewPreGrantTable.ForeignCitation;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "reference_text"))
        {
            return PatentsViewPreGrantTable.OtherReference;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "related_text"))
        {
            return PatentsViewPreGrantTable.RelatedApplication;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "claim_text"))
        {
            return PatentsViewPreGrantTable.Claim;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "draw_desc_text"))
        {
            return PatentsViewPreGrantTable.DrawingDescription;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "description_text"))
        {
            return PatentsViewPreGrantTable.Description;
        }

        if (PatentsViewGrantBulkParser.HasHeader(row, "summary_text"))
        {
            return PatentsViewPreGrantTable.Summary;
        }

        return PatentsViewPreGrantTable.Unknown;
    }

    internal static PatentsViewPublication ParsePublication(DelimitedRow row, string sourceName, PatentDataProvenance provenance)
    {
        string[] known = new string[] { "document_number", "publication_number", "publication_title", "publication_abstract", "publication_date", "publication_year", "publication_type", "series_code", "rule_47_flag" };
        PatentsViewPublication result = new PatentsViewPublication();
        result.DocumentNumber = PatentsViewGrantBulkParser.ReadText(row, new string[] { "document_number", "publication_number" });
        result.Title = PatentsViewGrantBulkParser.ReadText(row, new string[] { "publication_title", "title" });
        result.Abstract = PatentsViewGrantBulkParser.ReadText(row, new string[] { "publication_abstract", "abstract" });
        ApplyPublicationDate(PatentsViewGrantBulkParser.ReadText(row, new string[] { "publication_date" }), sourceName, "publication_date", row, result);
        ApplyPublicationInteger(PatentsViewGrantBulkParser.ReadText(row, new string[] { "publication_year" }), sourceName, "publication_year", row, result);
        result.PublicationType = PatentsViewGrantBulkParser.ReadText(row, new string[] { "publication_type", "type" });
        result.SeriesCode = NormalizeSeriesCode(PatentsViewGrantBulkParser.ReadText(row, new string[] { "series_code" }));
        ApplyPublicationBoolean(PatentsViewGrantBulkParser.ReadText(row, new string[] { "rule_47_flag", "rule_47" }), sourceName, "rule_47_flag", row, result);
        result.Provenance = PatentsViewGrantBulkParser.CreateProvenance(provenance, sourceName);
        PatentsViewGrantBulkParser.CopyAdditionalFields(row, result.AdditionalFields, known);
        return result;
    }

    internal static bool HasName(string value, string expected)
    {
        return value.IndexOf(PatentsViewGrantBulkParser.NormalizeName(expected), global::System.StringComparison.Ordinal) >= 0;
    }

    internal static string? NormalizeSeriesCode(string? value)
    {
        if (value != null && value.Length == 1 && value[0] >= '0' && value[0] <= '9')
        {
            return "0" + value;
        }

        return value;
    }

    internal static void ApplyPublicationDate(string? text, string sourceName, string path, DelimitedRow row, PatentsViewPublication target)
    {
        JsonDateParseResult parsed = PatentsViewGrantBulkParser.ParseDateValue(text, sourceName, path, row);
        if (parsed.IsValid)
        {
            target.PublicationDate = parsed.Value;
            target.HasPublicationDate = true;
        }
    }

    internal static void ApplyPublicationInteger(string? text, string sourceName, string path, DelimitedRow row, PatentsViewPublication target)
    {
        JsonIntegerParseResult parsed = PatentsViewGrantBulkParser.ParseIntegerValue(text, sourceName, path, row);
        if (parsed.IsValid)
        {
            target.PublicationYear = (int)parsed.Value;
            target.HasPublicationYear = true;
        }
    }

    internal static void ApplyPublicationBoolean(string? text, string sourceName, string path, DelimitedRow row, PatentsViewPublication target)
    {
        int parsed = PatentsViewGrantBulkParser.ParseBooleanValue(text, sourceName, path, row);
        if (parsed >= 0)
        {
            target.Rule47 = parsed == 1;
            target.HasRule47 = true;
        }
    }
}
