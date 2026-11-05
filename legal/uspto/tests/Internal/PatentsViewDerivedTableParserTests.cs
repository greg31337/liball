using uspto;

namespace uspto.Tests;

public class PatentsViewDerivedTableParserTests
{
    [TestCase(DatasetFamily.PatentsViewAnnualized)]
    [TestCase(DatasetFamily.PatentsViewSorted)]
    public void HeaderDiscoveryCoversEveryPlannedDerivedTable(DatasetFamily family)
    {
        string[][] headers = new string[][]
        {
            new[] { "source_entity_id", "persistent_entity_id" },
            new[] { "document_number", "publication_title" },
            new[] { "patent_id", "patent_title" },
            new[] { "citation_patent_id" },
            new[] { "citation_document_number" },
            new[] { "citation_country" },
            new[] { "reference_text" },
            new[] { "claim_text" },
            new[] { "summary_text" },
            new[] { "description_text" },
            new[] { "draw_desc_text" },
            new[] { "inventor_id" },
            new[] { "assignee_id" },
            new[] { "applicant_id" },
            new[] { "location_id" },
            new[] { "cpc_id" },
            new[] { "ipc_id" },
            new[] { "uspc_id" },
            new[] { "wipo_id" },
            new[] { "patent_id" },
            new[] { "document_number" }
        };
        string[] tableIdentifiers = new[]
        {
            "entity_crosswalk",
            "publication",
            "patent",
            "us_patent_citation",
            "us_application_citation",
            "foreign_citation",
            "other_reference",
            "claim",
            "brief_summary",
            "description",
            "drawing_description",
            "inventor",
            "assignee",
            "applicant",
            "location",
            "cpc",
            "ipc",
            "uspc",
            "wipo",
            "patent_relation",
            "publication_relation"
        };
        string genericHint = family == DatasetFamily.PatentsViewAnnualized ? "PV-ANNUAL" : "PV-SORTED";
        int index;
        for (index = 0; index < headers.Length; index += 1)
        {
            PatentsViewDerivedTableSchema schema = PatentsViewDerivedTableParser.DiscoverSchema(
                CreateRow(headers[index], new string[headers[index].Length]),
                genericHint,
                family);

            Assert.Multiple(() =>
            {
                Assert.That(schema.TableIdentifier, Is.EqualTo(tableIdentifiers[index]), tableIdentifiers[index]);
                Assert.That(schema.DetectionReason, Is.EqualTo("headers"), tableIdentifiers[index]);
                Assert.That(schema.DetectedFromHeaders, Is.True, tableIdentifiers[index]);
            });
        }
    }

    [Test]
    public void AnnualizedRoutePreservesEveryColumnAndSourceCoordinates()
    {
        DelimitedRow row = CreateRow(
            new[] { "inventor_id", "year", "patent_count", "new_collaboration_score" },
            new[] { "inv-001", "2025", "17", "0.875" });
        BulkParserContext context = new BulkParserContext
        {
            ProductIdentifier = "PV-ANNUAL",
            FileName = "/mirror/annualized_inventor_2025.tsv.gz"
        };

        BulkParserRoute route = BulkParserRouter.RouteDelimitedRow(row, '\t', context, CreateProvenance());
        PatentsViewDerivedTableRecord record = (PatentsViewDerivedTableRecord)route.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(route.Family, Is.EqualTo(DatasetFamily.PatentsViewAnnualized));
            Assert.That(route.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(route.ParserName, Is.EqualTo("patentsview-annualized-delimited"));
            Assert.That(route.IsTyped, Is.True);
            Assert.That(record.Schema.Family, Is.EqualTo(DatasetFamily.PatentsViewAnnualized));
            Assert.That(record.Schema.TableIdentifier, Is.EqualTo("annualized_inventor_2025"));
            Assert.That(record.Schema.DetectionReason, Is.EqualTo("table-hint"));
            Assert.That(record.Schema.DetectedFromHint, Is.True);
            Assert.That(record.Schema.Headers, Is.EqualTo(new[] { "inventor_id", "year", "patent_count", "new_collaboration_score" }));
            Assert.That(record.Schema.Fingerprint, Is.EqualTo("inventorid|year|patentcount|newcollaborationscore"));
            Assert.That(record.Fields["inventor_id"], Is.EqualTo("inv-001"));
            Assert.That(record.Fields["new_collaboration_score"], Is.EqualTo("0.875"));
            Assert.That(record.ReadValue("New Collaboration Score"), Is.EqualTo("0.875"));
            Assert.That(record.RowNumber, Is.EqualTo(7));
            Assert.That(record.StartLineNumber, Is.EqualTo(9));
            Assert.That(record.StartOffset, Is.EqualTo(120));
            Assert.That(record.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(record.Provenance.DatasetIdentifier, Is.EqualTo("annualized_inventor_2025"));
            Assert.That(record.Provenance.DatasetVersion, Is.EqualTo("2026-09"));
            Assert.That(record.Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void SortedRouteSupportsCsvAndRetainsExactScalarText()
    {
        DelimitedRow row = CreateRow(
            new[] { "patent_id", "patent_title", "sort_rank", "empty_value", "leading_zero_code" },
            new[] { "0012345", "A sorted patent", "0007", "", "009" });
        BulkParserContext context = new BulkParserContext
        {
            ProductIdentifier = "PV-SORTED",
            FileName = "sorted_patent.csv"
        };

        BulkParserRoute route = BulkParserRouter.RouteDelimitedRow(row, ',', context, CreateProvenance());
        PatentsViewDerivedTableRecord record = (PatentsViewDerivedTableRecord)route.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(route.Family, Is.EqualTo(DatasetFamily.PatentsViewSorted));
            Assert.That(route.Format, Is.EqualTo(DocumentFormat.Csv));
            Assert.That(route.ParserName, Is.EqualTo("patentsview-sorted-delimited"));
            Assert.That(record.Schema.TableIdentifier, Is.EqualTo("sorted_patent"));
            Assert.That(record.Fields["sort_rank"], Is.EqualTo("0007"));
            Assert.That(record.Fields["empty_value"], Is.EqualTo(string.Empty));
            Assert.That(record.Fields["leading_zero_code"], Is.EqualTo("009"));
            Assert.That(record.ReadValue("missing_column"), Is.Null);
        });
    }

    [Test]
    public void GenericProductHintFallsBackToHeaderTableDiscovery()
    {
        DelimitedRow inventorRow = CreateRow(
            new[] { "inventor_id", "year", "patent_count" },
            new[] { "inv-2", "2024", "3" });
        BulkParserContext annualContext = new BulkParserContext { ProductIdentifier = "PV-ANNUAL" };
        PatentsViewDerivedTableRecord inventor = (PatentsViewDerivedTableRecord)BulkParserRouter.RouteDelimitedRow(inventorRow, '\t', annualContext, CreateProvenance()).Record!;

        DelimitedRow publicationRow = CreateRow(
            new[] { "document_number", "publication_title", "publication_year" },
            new[] { "US20240001234A1", "Discovered publication", "2024" });
        BulkParserContext sortedContext = new BulkParserContext { SchemaMarker = "PatentsView Sorted" };
        PatentsViewDerivedTableRecord publication = (PatentsViewDerivedTableRecord)BulkParserRouter.RouteDelimitedRow(publicationRow, '\t', sortedContext, CreateProvenance()).Record!;

        Assert.Multiple(() =>
        {
            Assert.That(inventor.Schema.TableIdentifier, Is.EqualTo("inventor"));
            Assert.That(inventor.Schema.DetectionReason, Is.EqualTo("headers"));
            Assert.That(inventor.Schema.DetectedFromHeaders, Is.True);
            Assert.That(inventor.ReadValue("Inventor Id"), Is.EqualTo("inv-2"));
            Assert.That(publication.Schema.Family, Is.EqualTo(DatasetFamily.PatentsViewSorted));
            Assert.That(publication.Schema.TableIdentifier, Is.EqualTo("publication"));
            Assert.That(publication.Provenance.DatasetIdentifier, Is.EqualTo("publication"));
        });
    }

    [Test]
    public void UnknownSchemasRemainGenericAndNewColumnsChangeTheFingerprint()
    {
        DelimitedRow firstRow = CreateRow(new[] { "experimental_key", "score" }, new[] { "A", "1.5" });
        DelimitedRow laterRow = CreateRow(new[] { "experimental_key", "score", "future_flag" }, new[] { "B", "2.5", "yes" });

        PatentsViewDerivedTableRecord first = PatentsViewDerivedTableParser.ParseRow(firstRow, "PV-ANNUAL", DatasetFamily.PatentsViewAnnualized, CreateProvenance());
        PatentsViewDerivedTableRecord later = PatentsViewDerivedTableParser.ParseRow(laterRow, "PV-ANNUAL", DatasetFamily.PatentsViewAnnualized, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(first.Schema.TableIdentifier, Is.EqualTo("table"));
            Assert.That(first.Schema.DetectionReason, Is.EqualTo("generic"));
            Assert.That(first.Fields["experimental_key"], Is.EqualTo("A"));
            Assert.That(later.Fields["future_flag"], Is.EqualTo("yes"));
            Assert.That(first.Schema.Fingerprint, Is.Not.EqualTo(later.Schema.Fingerprint));
            Assert.That(later.Schema.ColumnCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void MissingAndExtraRowValuesRemainObservable()
    {
        DelimitedRow shortRow = CreateRow(new[] { "assignee_id", "year", "patent_count" }, new[] { "asg-1", "2025" });
        shortRow.HasMissingColumns = true;
        shortRow.MissingColumnCount = 1;
        DelimitedRow wideRow = CreateRow(new[] { "location_id", "year" }, new[] { "loc-1", "2025", "unheaded", "tail" });
        wideRow.HasExtraColumns = true;
        wideRow.ExtraColumnCount = 2;

        PatentsViewDerivedTableRecord missing = PatentsViewDerivedTableParser.ParseRow(shortRow, "annualized_assignee.tsv", DatasetFamily.PatentsViewAnnualized, CreateProvenance());
        PatentsViewDerivedTableRecord extra = PatentsViewDerivedTableParser.ParseRow(wideRow, "sorted_location.tsv", DatasetFamily.PatentsViewSorted, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(missing.HasMissingColumns, Is.True);
            Assert.That(missing.Fields["patent_count"], Is.EqualTo(string.Empty));
            Assert.That(extra.HasExtraColumns, Is.True);
            Assert.That(extra.ExtraValues, Is.EqualTo(new[] { "unheaded", "tail" }));
            Assert.That(extra.Fields.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void AmbiguousHeadersAndUnsupportedFamiliesAreRejected()
    {
        DelimitedRow duplicate = CreateRow(new[] { "patent_id", "Patent Id" }, new[] { "1", "2" });
        ParseException duplicateFailure = Assert.Throws<ParseException>(() => PatentsViewDerivedTableParser.ParseRow(duplicate, "annualized.tsv", DatasetFamily.PatentsViewAnnualized, CreateProvenance()))!;
        DelimitedRow blank = CreateRow(new[] { "patent_id", "  " }, new[] { "1", "2" });
        ParseException blankFailure = Assert.Throws<ParseException>(() => PatentsViewDerivedTableParser.ParseRow(blank, "sorted.tsv", DatasetFamily.PatentsViewSorted, CreateProvenance()))!;
        ValidationException familyFailure = Assert.Throws<ValidationException>(() => PatentsViewDerivedTableParser.ParseRow(CreateRow(new[] { "patent_id" }, new[] { "1" }), "g_patent.tsv", DatasetFamily.PatentsViewGrant, CreateProvenance()))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicateFailure.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(duplicateFailure.DataPath, Is.EqualTo("Patent Id"));
            Assert.That(duplicateFailure.LineNumber, Is.EqualTo(9));
            Assert.That(blankFailure.DataPath, Is.EqualTo("header[1]"));
            Assert.That(familyFailure.FieldName, Is.EqualTo("family"));
            Assert.That(familyFailure.ErrorCode, Is.EqualTo("UNSUPPORTED_DATASET_FAMILY"));
        });
    }

    [Test]
    public void ProductIdentifiersSelectBothDerivedParsers()
    {
        BulkParserRoute annual = BulkParserRouter.SelectRoute(new BulkParserContext { ProductIdentifier = "PV-ANNUAL" });
        BulkParserRoute sorted = BulkParserRouter.SelectRoute(new BulkParserContext { ProductIdentifier = "PV-SORTED" });

        Assert.Multiple(() =>
        {
            Assert.That(annual.Family, Is.EqualTo(DatasetFamily.PatentsViewAnnualized));
            Assert.That(annual.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(annual.ParserName, Is.EqualTo("patentsview-annualized-delimited"));
            Assert.That(sorted.Family, Is.EqualTo(DatasetFamily.PatentsViewSorted));
            Assert.That(sorted.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(sorted.ParserName, Is.EqualTo("patentsview-sorted-delimited"));
        });
    }

    private static DelimitedRow CreateRow(string[] headers, string[] values)
    {
        return new DelimitedRow
        {
            RowNumber = 7,
            StartLineNumber = 9,
            EndLineNumber = 9,
            StartOffset = 120,
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
            RequestIdentifier = "derived-1",
            RetrievedAt = new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.Zero),
            HasRetrievedAt = true,
            IsAuthoritative = true
        };
    }
}
