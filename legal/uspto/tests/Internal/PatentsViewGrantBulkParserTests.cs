using uspto;

namespace uspto.Tests;

public class PatentsViewGrantBulkParserTests
{
    [TestCase("patent_title|patent_date", PatentsViewGrantTable.Patent)]
    [TestCase("application_id|filing_date", PatentsViewGrantTable.Application)]
    [TestCase("inventor_id", PatentsViewGrantTable.Inventor)]
    [TestCase("assignee_id", PatentsViewGrantTable.Assignee)]
    [TestCase("applicant_id", PatentsViewGrantTable.Applicant)]
    [TestCase("location_id|location_city", PatentsViewGrantTable.Location)]
    [TestCase("attorney_id", PatentsViewGrantTable.Attorney)]
    [TestCase("cpc_id", PatentsViewGrantTable.Cpc)]
    [TestCase("ipc_id", PatentsViewGrantTable.Ipc)]
    [TestCase("uspc_id", PatentsViewGrantTable.Uspc)]
    [TestCase("wipo_id", PatentsViewGrantTable.Wipo)]
    [TestCase("citation_patent_id", PatentsViewGrantTable.UsPatentCitation)]
    [TestCase("citation_document_number", PatentsViewGrantTable.UsApplicationCitation)]
    [TestCase("citation_country", PatentsViewGrantTable.ForeignCitation)]
    [TestCase("citation_text", PatentsViewGrantTable.OtherReference)]
    [TestCase("related_application_text", PatentsViewGrantTable.RelatedApplication)]
    [TestCase("government_interest_statement", PatentsViewGrantTable.GovernmentInterest)]
    [TestCase("claim_text", PatentsViewGrantTable.Claim)]
    [TestCase("summary_text", PatentsViewGrantTable.Summary)]
    [TestCase("description_text", PatentsViewGrantTable.Description)]
    [TestCase("drawing_description", PatentsViewGrantTable.DrawingDescription)]
    [TestCase("source_entity_id|persistent_entity_id", PatentsViewGrantTable.EntityCrosswalk)]
    [TestCase("patent_id|application_id|document_number", PatentsViewGrantTable.GrantPublicationCrosswalk)]
    public void HeadersDiscoverEveryPlannedGrantTable(string headerText, object expected)
    {
        string[] headers = headerText.Split('|');
        DelimitedRow row = CreateRow(headers, new string[headers.Length]);

        PatentsViewGrantTable actual = PatentsViewGrantBulkParser.ResolveTable("PV-GRANT", row);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RouterParsesPatentTsvWithTypedValuesProvenanceAndUnknownFields()
    {
        DelimitedRow row = CreateRow(
            new[] { "patent_id", "patent_title", "patent_date", "patent_year", "wipo_kind", "withdrawn", "patent_detail_desc_length", "future_metric" },
            new[] { "0012345", "Useful widget", "2026-01-02", "2026", "B2", "0", "418", "future-value" });
        PatentDataProvenance provenance = CreateProvenance();
        BulkParserContext context = new BulkParserContext { FileName = "g_patent.tsv" };

        BulkParserRoute route = BulkParserRouter.RouteDelimitedRow(row, '\t', context, provenance);
        PatentsViewPatent patent = (PatentsViewPatent)route.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(route.Family, Is.EqualTo(DatasetFamily.PatentsViewGrant));
            Assert.That(route.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(route.ParserName, Is.EqualTo("patentsview-grant-tsv"));
            Assert.That(route.IsTyped, Is.True);
            Assert.That(patent.PatentIdentifier, Is.EqualTo("0012345"));
            Assert.That(patent.Title, Is.EqualTo("Useful widget"));
            Assert.That(patent.GrantDate, Is.EqualTo(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(patent.HasGrantDate, Is.True);
            Assert.That(patent.GrantYear, Is.EqualTo(2026));
            Assert.That(patent.HasGrantYear, Is.True);
            Assert.That(patent.KindCode, Is.EqualTo("B2"));
            Assert.That(patent.IsWithdrawn, Is.False);
            Assert.That(patent.HasIsWithdrawn, Is.True);
            Assert.That(patent.DescriptionLength, Is.EqualTo(418));
            Assert.That(patent.AdditionalFields["future_metric"], Is.EqualTo("future-value"));
            Assert.That(patent.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(patent.Provenance.DatasetIdentifier, Is.EqualTo("g_patent"));
            Assert.That(patent.Provenance.DatasetVersion, Is.EqualTo("2026-09"));
            Assert.That(patent.Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void ParsesApplicationAndGrantEntityTables()
    {
        PatentsViewPatentApplication application = (PatentsViewPatentApplication)Parse(
            "g_application.tsv",
            new[] { "application_id", "application_type", "filing_date", "series_code", "rule_47", "patent_id" },
            new[] { "16123456", "utility", "2024-03-04", "16", "Y", "0012345" });
        PatentsViewInventor inventor = (PatentsViewInventor)Parse(
            "g_inventor_disambiguated.tsv",
            new[] { "inventor_id", "inventor_first_name", "inventor_last_name", "inventor_gender", "first_seen_date", "patent_count" },
            new[] { "inv-7", "Ada", "Lovelace", "F", "20200102", "12" });
        PatentsViewAssignee assignee = (PatentsViewAssignee)Parse(
            "g_patent_assignee.tsv",
            new[] { "assignee_id", "assignee_organization", "assignee_type", "inventor_count", "patent_count", "patent_id" },
            new[] { "asg-8", "Example Labs", "2", "4", "9", "0012345" });
        PatentsViewApplicant applicant = (PatentsViewApplicant)Parse(
            "g_applicant.tsv",
            new[] { "applicant_id", "patent_id", "application_id", "applicant_first_name", "location_id", "applicant_sequence" },
            new[] { "app-9", "0012345", "16123456", "Grace", "loc-1", "2" });
        PatentsViewLocation location = (PatentsViewLocation)Parse(
            "g_location_disambiguated.tsv",
            new[] { "location_id", "location_city", "location_state", "location_country", "location_latitude", "location_longitude", "patent_count" },
            new[] { "loc-1", "Seattle", "Washington", "US", "47.6062", "-122.3321", "31" });
        PatentsViewAttorney attorney = (PatentsViewAttorney)Parse(
            "g_patent_lawyer.tsv",
            new[] { "lawyer_id", "lawyer_first_name", "lawyer_last_name", "lawyer_organization", "years_active", "patent_id" },
            new[] { "law-3", "Lin", "Chen", "Example IP", "8", "0012345" });

        Assert.Multiple(() =>
        {
            Assert.That(application.ApplicationIdentifier, Is.EqualTo("16123456"));
            Assert.That(application.HasFilingDate, Is.True);
            Assert.That(application.Rule47, Is.True);
            Assert.That(application.AdditionalFields["patent_id"], Is.EqualTo("0012345"));
            Assert.That(inventor.InventorIdentifier, Is.EqualTo("inv-7"));
            Assert.That(inventor.FirstName, Is.EqualTo("Ada"));
            Assert.That(inventor.HasFirstSeenDate, Is.True);
            Assert.That(inventor.PatentCount, Is.EqualTo(12));
            Assert.That(assignee.AssigneeIdentifier, Is.EqualTo("asg-8"));
            Assert.That(assignee.Organization, Is.EqualTo("Example Labs"));
            Assert.That(assignee.AdditionalFields["patent_id"], Is.EqualTo("0012345"));
            Assert.That(applicant.ApplicationIdentifier, Is.EqualTo("16123456"));
            Assert.That(applicant.Sequence, Is.EqualTo(2));
            Assert.That(location.Name, Is.EqualTo("Seattle"));
            Assert.That(location.Latitude, Is.EqualTo(47.6062));
            Assert.That(location.Longitude, Is.EqualTo(-122.3321));
            Assert.That(attorney.AttorneyIdentifier, Is.EqualTo("law-3"));
            Assert.That(attorney.Organization, Is.EqualTo("Example IP"));
            Assert.That(attorney.YearsActive, Is.EqualTo(8));
        });
    }

    [Test]
    public void ParsesEveryGrantClassificationFamily()
    {
        PatentsViewCpcClassification cpc = (PatentsViewCpcClassification)Parse(
            "g_cpc_current.tsv",
            new[] { "cpc_id", "cpc_section", "cpc_class", "cpc_subclass", "cpc_group", "cpc_type", "cpc_sequence", "patent_id" },
            new[] { "cpc-1", "G", "G06", "G06F", "G06F3/01", "inventional", "1", "0012345" });
        PatentsViewIpcClassification ipc = (PatentsViewIpcClassification)Parse(
            "g_ipc_at_issue.tsv",
            new[] { "ipc_id", "ipc_section", "ipc_class", "ipc_subclass", "ipc_main_group", "ipc_subgroup", "ipc_action_date", "ipc_sequence" },
            new[] { "ipc-1", "G", "06", "F", "3", "01", "2025-02-03", "2" });
        PatentsViewUspcClassification uspc = (PatentsViewUspcClassification)Parse(
            "g_uspc_at_issue.tsv",
            new[] { "uspc_id", "uspc_level", "uspc_mainclass", "uspc_subclass", "uspc_sequence" },
            new[] { "uspc-1", "subclass", "345", "156", "3" });
        PatentsViewWipoClassification wipo = (PatentsViewWipoClassification)Parse(
            "g_wipo_technology.tsv",
            new[] { "wipo_id", "wipo_sector_title", "wipo_field_title", "wipo_sequence" },
            new[] { "wipo-1", "Electrical engineering", "Computer technology", "4" });

        Assert.Multiple(() =>
        {
            Assert.That(cpc.Section, Is.EqualTo("G"));
            Assert.That(cpc.Group, Is.EqualTo("G06F3/01"));
            Assert.That(cpc.Sequence, Is.EqualTo(1));
            Assert.That(cpc.AdditionalFields["patent_id"], Is.EqualTo("0012345"));
            Assert.That(ipc.MainGroup, Is.EqualTo("3"));
            Assert.That(ipc.HasActionDate, Is.True);
            Assert.That(ipc.Sequence, Is.EqualTo(2));
            Assert.That(uspc.MainClass, Is.EqualTo("345"));
            Assert.That(uspc.Subclass, Is.EqualTo("156"));
            Assert.That(wipo.SectorTitle, Is.EqualTo("Electrical engineering"));
            Assert.That(wipo.FieldTitle, Is.EqualTo("Computer technology"));
        });
    }

    [Test]
    public void ParsesGrantCitationRelatedApplicationAndGovernmentTables()
    {
        PatentsViewCitation patentCitation = (PatentsViewCitation)Parse(
            "g_us_patent_citation.tsv",
            new[] { "patent_id", "citation_patent_id", "citation_category", "citation_date", "citation_sequence" },
            new[] { "0012345", "0098765", "examiner", "2024-01-01", "1" });
        PatentsViewCitation applicationCitation = (PatentsViewCitation)Parse(
            "g_us_application_citation.tsv",
            new[] { "patent_id", "citation_document_number", "citation_sequence" },
            new[] { "0012345", "US20240001234A1", "2" });
        PatentsViewCitation foreignCitation = (PatentsViewCitation)Parse(
            "g_foreign_citation.tsv",
            new[] { "patent_id", "citation_number", "citation_country", "citation_kind" },
            new[] { "0012345", "EP123456", "EP", "A1" });
        PatentsViewCitation otherReference = (PatentsViewCitation)Parse(
            "g_other_reference.tsv",
            new[] { "patent_id", "citation_text", "citation_sequence" },
            new[] { "0012345", "Technical journal article", "3" });
        PatentsViewRelatedApplication related = (PatentsViewRelatedApplication)Parse(
            "g_rel_app_text.tsv",
            new[] { "patent_id", "related_application_text" },
            new[] { "0012345", "Continuation of application 16/123456." });
        PatentsViewGovernmentInterest interest = (PatentsViewGovernmentInterest)Parse(
            "g_gov_interest_organizations.tsv",
            new[] { "patent_id", "government_interest_statement", "award_number", "federal_agency_name", "level_one", "level_two" },
            new[] { "0012345", "Made with government support.", "ABC-123", "Department of Energy", "Office of Science", "Research" });

        Assert.Multiple(() =>
        {
            Assert.That(patentCitation.CitationType, Is.EqualTo("us_patent"));
            Assert.That(patentCitation.CitedPatentIdentifier, Is.EqualTo("0098765"));
            Assert.That(patentCitation.HasCitationDate, Is.True);
            Assert.That(applicationCitation.CitationType, Is.EqualTo("us_application"));
            Assert.That(applicationCitation.CitedDocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(foreignCitation.CitationType, Is.EqualTo("foreign"));
            Assert.That(foreignCitation.Country, Is.EqualTo("EP"));
            Assert.That(otherReference.CitationType, Is.EqualTo("other_reference"));
            Assert.That(otherReference.ReferenceText, Is.EqualTo("Technical journal article"));
            Assert.That(related.RelatedText, Does.StartWith("Continuation"));
            Assert.That(interest.Statement, Is.EqualTo("Made with government support."));
            Assert.That(interest.AwardNumbers, Is.EqualTo(new[] { "ABC-123" }));
            Assert.That(interest.Organizations.Single().FederalAgencyName, Is.EqualTo("Department of Energy"));
            Assert.That(interest.Organizations.Single().LevelOne, Is.EqualTo("Office of Science"));
        });
    }

    [Test]
    public void ParsesGrantLongTextAndCrosswalkTables()
    {
        PatentsViewClaim claim = (PatentsViewClaim)Parse(
            "g_claim.tsv",
            new[] { "patent_id", "claim_number", "claim_sequence", "claim_text", "exemplary", "depends_on_claim" },
            new[] { "0012345", "2", "2", "The system of claim 1...", "true", "1" });
        PatentsViewSummary summary = (PatentsViewSummary)Parse(
            "g_brf_sum_text.tsv", new[] { "patent_id", "summary_text" }, new[] { "0012345", "A concise summary." });
        PatentsViewDescription description = (PatentsViewDescription)Parse(
            "g_detail_desc_text.tsv", new[] { "patent_id", "description_text", "description_length" }, new[] { "0012345", "A detailed description.", "23" });
        PatentsViewDrawingDescription drawing = (PatentsViewDrawingDescription)Parse(
            "g_draw_desc_text.tsv", new[] { "patent_id", "drawing_sequence", "drawing_description" }, new[] { "0012345", "1", "Figure 1 is a plan view." });
        PatentsViewEntityCrosswalk entity = (PatentsViewEntityCrosswalk)Parse(
            "g_inventor_crosswalk.tsv",
            new[] { "source_entity_id", "persistent_entity_id", "disambiguation_version", "valid_from", "is_current", "legacy_label" },
            new[] { "old-inv", "inv-1", "v2", "2025-01-01", "1", "kept" });
        PatentsViewGrantPublicationCrosswalk publication = (PatentsViewGrantPublicationCrosswalk)Parse(
            "g_patent_publication.tsv",
            new[] { "patent_id", "application_id", "document_number", "is_current_document_number", "is_current_patent_id" },
            new[] { "0012345", "16123456", "US20240001234A1", "true", "false" });

        Assert.Multiple(() =>
        {
            Assert.That(claim.ClaimNumber, Is.EqualTo("2"));
            Assert.That(claim.Sequence, Is.EqualTo(2));
            Assert.That(claim.IsExemplary, Is.True);
            Assert.That(claim.DependsOnClaim, Is.EqualTo("1"));
            Assert.That(summary.Text, Is.EqualTo("A concise summary."));
            Assert.That(description.Length, Is.EqualTo(23));
            Assert.That(description.HasLength, Is.True);
            Assert.That(drawing.Sequence, Is.EqualTo(1));
            Assert.That(drawing.Text, Does.StartWith("Figure 1"));
            Assert.That(entity.EntityType, Is.EqualTo("inventor"));
            Assert.That(entity.SourceEntityIdentifier, Is.EqualTo("old-inv"));
            Assert.That(entity.PersistentEntityIdentifier, Is.EqualTo("inv-1"));
            Assert.That(entity.IsCurrent, Is.True);
            Assert.That(entity.AdditionalFields["legacy_label"], Is.EqualTo("kept"));
            Assert.That(publication.PatentIdentifier, Is.EqualTo("0012345"));
            Assert.That(publication.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(publication.IsCurrentDocumentNumber, Is.True);
            Assert.That(publication.IsCurrentPatentIdentifier, Is.False);
            Assert.That(publication.HasIsCurrentPatentIdentifier, Is.True);
        });
    }

    [Test]
    public void RejectsMalformedTypedRowsAndLeavesCsvGeneric()
    {
        DelimitedRow invalidValue = CreateRow(new[] { "patent_id", "patent_year" }, new[] { "0012345", "twenty" });
        ParseException invalidNumber = Assert.Throws<ParseException>(() => PatentsViewGrantBulkParser.ParseRow(invalidValue, "g_patent.tsv", CreateProvenance()))!;

        DelimitedRow missingColumn = CreateRow(new[] { "patent_id", "patent_title" }, new[] { "0012345" });
        missingColumn.HasMissingColumns = true;
        missingColumn.MissingColumnCount = 1;
        ParseException shape = Assert.Throws<ParseException>(() => PatentsViewGrantBulkParser.ParseRow(missingColumn, "g_patent.tsv", CreateProvenance()))!;

        DelimitedRow csvRow = CreateRow(new[] { "patent_id", "patent_title", "patent_date" }, new[] { "0012345", "CSV", "2026-01-02" });
        BulkParserRoute csv = BulkParserRouter.RouteDelimitedRow(csvRow, ',', new BulkParserContext { FileName = "g_patent.csv" }, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(invalidNumber.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(invalidNumber.SourceName, Is.EqualTo("g_patent.tsv"));
            Assert.That(invalidNumber.DataPath, Is.EqualTo("patent_year"));
            Assert.That(invalidNumber.LineNumber, Is.EqualTo(2));
            Assert.That(shape.DataPath, Is.EqualTo("row"));
            Assert.That(csv.IsTyped, Is.False);
            Assert.That(csv.ParserName, Is.EqualTo("generic-delimited"));
            Assert.That(csv.Record, Is.SameAs(csvRow));
        });
    }

    [Test]
    public void EmptyOptionalGrantValuesRemainMissingAndResearchProvenanceIsPreserved()
    {
        DelimitedRow row = CreateRow(
            new[] { "patent_id", "patent_title", "patent_date", "patent_year", "withdrawn", "future_metric" },
            new[] { "0012345", "", "", "", "", "" });

        PatentsViewPatent patent = (PatentsViewPatent)PatentsViewGrantBulkParser.ParseRow(row, "PV-GRANT", CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(patent.Title, Is.Null);
            Assert.That(patent.HasGrantDate, Is.False);
            Assert.That(patent.HasGrantYear, Is.False);
            Assert.That(patent.HasIsWithdrawn, Is.False);
            Assert.That(patent.AdditionalFields, Does.Not.ContainKey("future_metric"));
            Assert.That(patent.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(patent.Provenance.DatasetIdentifier, Is.EqualTo("PV-GRANT"));
            Assert.That(patent.Provenance.RequestIdentifier, Is.EqualTo("bulk-1"));
            Assert.That(patent.Provenance.HasRetrievedAt, Is.True);
            Assert.That(patent.Provenance.IsAuthoritative, Is.False);
        });
    }

    private static object Parse(string tableName, string[] headers, string[] values)
    {
        return PatentsViewGrantBulkParser.ParseRow(CreateRow(headers, values), tableName, CreateProvenance());
    }

    private static DelimitedRow CreateRow(string[] headers, string[] values)
    {
        return new DelimitedRow
        {
            RowNumber = 1,
            StartLineNumber = 2,
            EndLineNumber = 2,
            StartOffset = 100,
            Headers = headers,
            Values = values,
            ExpectedColumnCount = headers.Length,
            HasMissingColumns = values.Length < headers.Length,
            MissingColumnCount = Math.Max(0, headers.Length - values.Length),
            HasExtraColumns = values.Length > headers.Length,
            ExtraColumnCount = Math.Max(0, values.Length - headers.Length)
        };
    }

    private static PatentDataProvenance CreateProvenance()
    {
        return new PatentDataProvenance
        {
            Source = DataProvenance.Bulk,
            DatasetVersion = "2026-09",
            RequestIdentifier = "bulk-1",
            RetrievedAt = new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.Zero),
            HasRetrievedAt = true,
            IsAuthoritative = true
        };
    }
}
