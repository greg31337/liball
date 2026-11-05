using uspto;

namespace uspto.Tests;

public class PatentsViewPreGrantBulkParserTests
{
    [TestCase("publication_title|publication_date", PatentsViewPreGrantTable.Publication)]
    [TestCase("inventor_id", PatentsViewPreGrantTable.Inventor)]
    [TestCase("assignee_id", PatentsViewPreGrantTable.Assignee)]
    [TestCase("applicant_id", PatentsViewPreGrantTable.Applicant)]
    [TestCase("location_id|location_name", PatentsViewPreGrantTable.Location)]
    [TestCase("cpc_id", PatentsViewPreGrantTable.Cpc)]
    [TestCase("ipc_id", PatentsViewPreGrantTable.Ipc)]
    [TestCase("uspc_id", PatentsViewPreGrantTable.Uspc)]
    [TestCase("wipo_id", PatentsViewPreGrantTable.Wipo)]
    [TestCase("citation_patent_id", PatentsViewPreGrantTable.UsPatentCitation)]
    [TestCase("citation_document_number", PatentsViewPreGrantTable.UsApplicationCitation)]
    [TestCase("citation_country", PatentsViewPreGrantTable.ForeignCitation)]
    [TestCase("reference_text", PatentsViewPreGrantTable.OtherReference)]
    [TestCase("related_text", PatentsViewPreGrantTable.RelatedApplication)]
    [TestCase("claim_text", PatentsViewPreGrantTable.Claim)]
    [TestCase("summary_text", PatentsViewPreGrantTable.Summary)]
    [TestCase("description_text", PatentsViewPreGrantTable.Description)]
    [TestCase("draw_desc_text", PatentsViewPreGrantTable.DrawingDescription)]
    [TestCase("source_entity_id|persistent_entity_id", PatentsViewPreGrantTable.EntityCrosswalk)]
    [TestCase("patent_id|application_number|document_number", PatentsViewPreGrantTable.GrantPublicationCrosswalk)]
    public void HeadersDiscoverEveryPlannedPreGrantTable(string headerText, object expected)
    {
        string[] headers = headerText.Split('|');
        DelimitedRow row = CreateRow(headers, new string[headers.Length]);

        PatentsViewPreGrantTable actual = PatentsViewPreGrantBulkParser.ResolveTable("PV-PREGRANT", row);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RouterParsesPublicationTsvWithTypedValuesProvenanceAndUnknownFields()
    {
        DelimitedRow row = CreateRow(
            new[] { "document_number", "publication_title", "publication_abstract", "publication_date", "publication_year", "publication_type", "series_code", "rule_47_flag", "future_metric" },
            new[] { "US20240001234A1", "Storage controller", "A controller abstract.", "2024-01-04", "2024", "utility", "9", "Y", "future-value" });
        BulkParserContext context = new BulkParserContext { FileName = "pg_publication.tsv" };

        BulkParserRoute route = BulkParserRouter.RouteDelimitedRow(row, '\t', context, CreateProvenance());
        PatentsViewPublication publication = (PatentsViewPublication)route.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(route.Family, Is.EqualTo(DatasetFamily.PatentsViewPreGrant));
            Assert.That(route.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(route.ParserName, Is.EqualTo("patentsview-pregrant-tsv"));
            Assert.That(route.IsTyped, Is.True);
            Assert.That(publication.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(publication.Title, Is.EqualTo("Storage controller"));
            Assert.That(publication.Abstract, Is.EqualTo("A controller abstract."));
            Assert.That(publication.PublicationDate, Is.EqualTo(new DateTimeOffset(2024, 1, 4, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(publication.HasPublicationDate, Is.True);
            Assert.That(publication.PublicationYear, Is.EqualTo(2024));
            Assert.That(publication.HasPublicationYear, Is.True);
            Assert.That(publication.PublicationType, Is.EqualTo("utility"));
            Assert.That(publication.SeriesCode, Is.EqualTo("09"));
            Assert.That(publication.Rule47, Is.True);
            Assert.That(publication.HasRule47, Is.True);
            Assert.That(publication.AdditionalFields["future_metric"], Is.EqualTo("future-value"));
            Assert.That(publication.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(publication.Provenance.DatasetIdentifier, Is.EqualTo("pg_publication"));
            Assert.That(publication.Provenance.DatasetVersion, Is.EqualTo("2026-09"));
            Assert.That(publication.Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void ParsesPreGrantInventorAssigneeApplicantAndLocationTables()
    {
        PatentsViewInventor inventor = (PatentsViewInventor)Parse(
            "pg_publication_inventor.tsv",
            new[] { "document_number", "inventor_id", "inventor_name_first", "inventor_name_last", "inventor_gender_code", "patent_count" },
            new[] { "US20240001234A1", "inv-1", "Ada", "Lovelace", "F", "7" });
        PatentsViewAssignee assignee = (PatentsViewAssignee)Parse(
            "pg_publication_assignee.tsv",
            new[] { "document_number", "assignee_id", "assignee_individual_name_first", "assignee_individual_name_last", "assignee_organization", "assignee_type" },
            new[] { "US20240001234A1", "asg-1", "", "", "Example Labs", "2" });
        PatentsViewApplicant applicant = (PatentsViewApplicant)Parse(
            "pg_us_parties.tsv",
            new[] { "document_number", "application_id", "applicant", "us_party_name_first", "us_party_name_last", "us_party_type", "applicant_authority", "us_party_sequence" },
            new[] { "US20240001234A1", "18123456", "app-1", "Grace", "Hopper", "person", "inventor", "1" });
        PatentsViewLocation location = (PatentsViewLocation)Parse(
            "pg_location_disambiguated.tsv",
            new[] { "location_id", "location_name", "location_state", "location_country", "location_place_type", "location_latitude", "location_longitude" },
            new[] { "loc-1", "Arlington", "Virginia", "US", "city", "38.8816", "-77.0910" });

        Assert.Multiple(() =>
        {
            Assert.That(inventor.InventorIdentifier, Is.EqualTo("inv-1"));
            Assert.That(inventor.FirstName, Is.EqualTo("Ada"));
            Assert.That(inventor.LastName, Is.EqualTo("Lovelace"));
            Assert.That(inventor.GenderCode, Is.EqualTo("F"));
            Assert.That(inventor.AdditionalFields["document_number"], Is.EqualTo("US20240001234A1"));
            Assert.That(assignee.AssigneeIdentifier, Is.EqualTo("asg-1"));
            Assert.That(assignee.Organization, Is.EqualTo("Example Labs"));
            Assert.That(applicant.ApplicantIdentifier, Is.EqualTo("app-1"));
            Assert.That(applicant.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(applicant.ApplicationIdentifier, Is.EqualTo("18123456"));
            Assert.That(applicant.FirstName, Is.EqualTo("Grace"));
            Assert.That(applicant.Sequence, Is.EqualTo(1));
            Assert.That(location.LocationIdentifier, Is.EqualTo("loc-1"));
            Assert.That(location.Name, Is.EqualTo("Arlington"));
            Assert.That(location.PlaceType, Is.EqualTo("city"));
            Assert.That(location.Longitude, Is.EqualTo(-77.0910));
        });
    }

    [Test]
    public void ParsesEveryPreGrantClassificationFamily()
    {
        PatentsViewCpcClassification cpc = (PatentsViewCpcClassification)Parse(
            "pg_cpc_current.tsv",
            new[] { "document_number", "cpc_id", "cpc_section", "cpc_class", "cpc_subclass", "cpc_group", "cpc_type", "cpc_sequence" },
            new[] { "US20240001234A1", "cpc-1", "G", "G06", "G06F", "G06F3/01", "inventional", "1" });
        PatentsViewIpcClassification ipc = (PatentsViewIpcClassification)Parse(
            "pg_ipc_at_issue.tsv",
            new[] { "document_number", "ipc_id", "ipc_section", "ipc_class", "ipc_subclass", "ipc_main_group", "ipc_subgroup", "ipc_action_date", "ipc_sequence" },
            new[] { "US20240001234A1", "ipc-1", "G", "06", "F", "3", "01", "2023-04-05", "2" });
        PatentsViewUspcClassification uspc = (PatentsViewUspcClassification)Parse(
            "pg_uspc_at_issue.tsv",
            new[] { "document_number", "uspc_id", "uspc_level", "uspc_mainclass", "uspc_subclass", "uspc_sequence" },
            new[] { "US20240001234A1", "uspc-1", "subclass", "345", "156", "3" });
        PatentsViewWipoClassification wipo = (PatentsViewWipoClassification)Parse(
            "pg_wipo_technology.tsv",
            new[] { "document_number", "wipo_id", "wipo_sector_title", "wipo_field_title", "wipo_sequence" },
            new[] { "US20240001234A1", "wipo-1", "Electrical engineering", "Computer technology", "4" });

        Assert.Multiple(() =>
        {
            Assert.That(cpc.Group, Is.EqualTo("G06F3/01"));
            Assert.That(cpc.Sequence, Is.EqualTo(1));
            Assert.That(cpc.AdditionalFields["document_number"], Is.EqualTo("US20240001234A1"));
            Assert.That(ipc.MainGroup, Is.EqualTo("3"));
            Assert.That(ipc.HasActionDate, Is.True);
            Assert.That(uspc.MainClass, Is.EqualTo("345"));
            Assert.That(uspc.Subclass, Is.EqualTo("156"));
            Assert.That(wipo.SectorTitle, Is.EqualTo("Electrical engineering"));
            Assert.That(wipo.FieldTitle, Is.EqualTo("Computer technology"));
        });
    }

    [Test]
    public void ParsesPreGrantCitationsRelatedApplicationsAndLongText()
    {
        PatentsViewCitation patentCitation = (PatentsViewCitation)Parse(
            "pg_us_patent_citation.tsv",
            new[] { "document_number", "citation_patent_id", "citation_category", "citation_date", "citation_sequence" },
            new[] { "US20240001234A1", "0098765", "examiner", "2022-01-02", "1" });
        PatentsViewCitation applicationCitation = (PatentsViewCitation)Parse(
            "pg_us_application_citation.tsv",
            new[] { "document_number", "citation_document_number", "citation_sequence" },
            new[] { "US20240001234A1", "US20210001111A1", "2" });
        PatentsViewCitation foreignCitation = (PatentsViewCitation)Parse(
            "pg_foreign_citation.tsv",
            new[] { "document_number", "citation_number", "citation_country", "citation_wipo_kind" },
            new[] { "US20240001234A1", "EP123456", "EP", "A1" });
        PatentsViewCitation otherReference = (PatentsViewCitation)Parse(
            "pg_other_reference.tsv",
            new[] { "document_number", "reference_text", "reference_sequence" },
            new[] { "US20240001234A1", "Technical journal article", "3" });
        PatentsViewRelatedApplication related = (PatentsViewRelatedApplication)Parse(
            "pg_rel_app_text.tsv",
            new[] { "document_number", "related_text" },
            new[] { "US20240001234A1", "Claims priority to application 17/111111." });
        PatentsViewClaim claim = (PatentsViewClaim)Parse(
            "pg_claim.tsv",
            new[] { "document_number", "claim_sequence", "claim_number", "claim_text", "claim_dependent" },
            new[] { "US20240001234A1", "2", "00002", "The apparatus of claim 1...", "1" });
        PatentsViewSummary summary = (PatentsViewSummary)Parse(
            "pg_brf_sum_text.tsv", new[] { "document_number", "summary_text" }, new[] { "US20240001234A1", "Publication summary." });
        PatentsViewDescription description = (PatentsViewDescription)Parse(
            "pg_detail_desc_text.tsv", new[] { "document_number", "description_text", "description_length" }, new[] { "US20240001234A1", "Detailed publication text.", "26" });
        PatentsViewDrawingDescription drawing = (PatentsViewDrawingDescription)Parse(
            "pg_draw_desc_text.tsv", new[] { "document_number", "draw_desc_sequence", "draw_desc_text" }, new[] { "US20240001234A1", "2", "FIG. 2 shows the publication." });

        Assert.Multiple(() =>
        {
            Assert.That(patentCitation.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(patentCitation.CitationType, Is.EqualTo("us_patent"));
            Assert.That(patentCitation.CitedPatentIdentifier, Is.EqualTo("0098765"));
            Assert.That(applicationCitation.CitationType, Is.EqualTo("us_application"));
            Assert.That(applicationCitation.CitedDocumentNumber, Is.EqualTo("US20210001111A1"));
            Assert.That(foreignCitation.Country, Is.EqualTo("EP"));
            Assert.That(foreignCitation.KindCode, Is.EqualTo("A1"));
            Assert.That(otherReference.ReferenceText, Is.EqualTo("Technical journal article"));
            Assert.That(otherReference.Sequence, Is.EqualTo(3));
            Assert.That(related.RelatedText, Does.StartWith("Claims priority"));
            Assert.That(claim.DependsOnClaim, Is.EqualTo("1"));
            Assert.That(claim.Sequence, Is.EqualTo(2));
            Assert.That(summary.Text, Is.EqualTo("Publication summary."));
            Assert.That(description.Length, Is.EqualTo(26));
            Assert.That(drawing.Sequence, Is.EqualTo(2));
            Assert.That(drawing.Text, Is.EqualTo("FIG. 2 shows the publication."));
        });
    }

    [Test]
    public void ParsesPreGrantCrosswalksAndRejectsMalformedRows()
    {
        PatentsViewEntityCrosswalk entity = (PatentsViewEntityCrosswalk)Parse(
            "pg_assignee_crosswalk.tsv",
            new[] { "source_entity_id", "persistent_entity_id", "disambiguation_version", "is_current", "legacy_label" },
            new[] { "old-asg", "asg-1", "v3", "true", "kept" });
        PatentsViewGrantPublicationCrosswalk grant = (PatentsViewGrantPublicationCrosswalk)Parse(
            "granted_pregrant_crosswalk.tsv",
            new[] { "patent_id", "application_number", "document_number", "current_document_number_flag", "current_patent_id_flag" },
            new[] { "12000000", "18123456", "US20240001234A1", "true", "1" });

        DelimitedRow invalid = CreateRow(new[] { "document_number", "publication_date" }, new[] { "US20240001234A1", "not-a-date" });
        ParseException failure = Assert.Throws<ParseException>(() => PatentsViewPreGrantBulkParser.ParseRow(invalid, "pg_publication.tsv", CreateProvenance()))!;
        DelimitedRow invalidSequence = CreateRow(new[] { "document_number", "draw_desc_sequence", "draw_desc_text" }, new[] { "US20240001234A1", "0", "Invalid sequence" });
        ParseException sequenceFailure = Assert.Throws<ParseException>(() => PatentsViewPreGrantBulkParser.ParseRow(invalidSequence, "pg_draw_desc_text.tsv", CreateProvenance()))!;

        DelimitedRow csvRow = CreateRow(new[] { "document_number", "publication_title", "publication_date" }, new[] { "US20240001234A1", "CSV", "2024-01-04" });
        BulkParserRoute csv = BulkParserRouter.RouteDelimitedRow(csvRow, ',', new BulkParserContext { FileName = "pg_publication.csv" }, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(entity.EntityType, Is.EqualTo("assignee"));
            Assert.That(entity.SourceEntityIdentifier, Is.EqualTo("old-asg"));
            Assert.That(entity.PersistentEntityIdentifier, Is.EqualTo("asg-1"));
            Assert.That(entity.IsCurrent, Is.True);
            Assert.That(entity.AdditionalFields["legacy_label"], Is.EqualTo("kept"));
            Assert.That(grant.PatentIdentifier, Is.EqualTo("12000000"));
            Assert.That(grant.ApplicationIdentifier, Is.EqualTo("18123456"));
            Assert.That(grant.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(grant.IsCurrentDocumentNumber, Is.True);
            Assert.That(grant.IsCurrentPatentIdentifier, Is.True);
            Assert.That(failure.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(failure.SourceName, Is.EqualTo("pg_publication.tsv"));
            Assert.That(failure.DataPath, Is.EqualTo("publication_date"));
            Assert.That(failure.LineNumber, Is.EqualTo(2));
            Assert.That(sequenceFailure.DataPath, Is.EqualTo("draw_desc_sequence"));
            Assert.That(csv.IsTyped, Is.False);
            Assert.That(csv.Record, Is.SameAs(csvRow));
        });
    }

    [Test]
    public void ProductIdentifierSelectsPreGrantTsvParserAndHeadersResolveTheTable()
    {
        BulkParserContext context = new BulkParserContext { ProductIdentifier = "PV-PREGRANT" };
        BulkParserRoute selection = BulkParserRouter.SelectRoute(context);
        DelimitedRow row = CreateRow(
            new[] { "document_number", "publication_title", "publication_date" },
            new[] { "US20240001234A1", "Header discovered", "2024-01-04" });

        BulkParserRoute routed = BulkParserRouter.RouteDelimitedRow(row, '\t', context, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(selection.Family, Is.EqualTo(DatasetFamily.PatentsViewPreGrant));
            Assert.That(selection.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(selection.ParserName, Is.EqualTo("patentsview-pregrant-tsv"));
            Assert.That(((PatentsViewPublication)routed.Record!).Title, Is.EqualTo("Header discovered"));
        });
    }

    [Test]
    public void EmptyOptionalPublicationValuesRemainMissingAndResearchProvenanceIsPreserved()
    {
        DelimitedRow row = CreateRow(
            new[] { "document_number", "publication_title", "publication_date", "publication_year", "rule_47_flag", "future_metric" },
            new[] { "US20240001234A1", "", "", "", "", "" });

        PatentsViewPublication publication = (PatentsViewPublication)PatentsViewPreGrantBulkParser.ParseRow(row, "PV-PREGRANT", CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(publication.Title, Is.Null);
            Assert.That(publication.HasPublicationDate, Is.False);
            Assert.That(publication.HasPublicationYear, Is.False);
            Assert.That(publication.HasRule47, Is.False);
            Assert.That(publication.AdditionalFields, Does.Not.ContainKey("future_metric"));
            Assert.That(publication.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(publication.Provenance.DatasetIdentifier, Is.EqualTo("PV-PREGRANT"));
            Assert.That(publication.Provenance.RequestIdentifier, Is.EqualTo("pregrant-bulk-1"));
            Assert.That(publication.Provenance.HasRetrievedAt, Is.True);
            Assert.That(publication.Provenance.IsAuthoritative, Is.False);
        });
    }

    private static object Parse(string tableName, string[] headers, string[] values)
    {
        return PatentsViewPreGrantBulkParser.ParseRow(CreateRow(headers, values), tableName, CreateProvenance());
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
            RequestIdentifier = "pregrant-bulk-1",
            RetrievedAt = new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.Zero),
            HasRetrievedAt = true,
            IsAuthoritative = true
        };
    }
}
