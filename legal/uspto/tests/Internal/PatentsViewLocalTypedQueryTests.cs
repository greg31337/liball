using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentsViewLocalTypedQueryTests
{
    [Test]
    public void PatentQueryReturnsFilteredTypedRecordsWithLocalProvenance()
    {
        string path = CreateTable(
            "patent_id\tpatent_title\tpatent_date\tpatent_year\tfuture_metric\n" +
            "001\tBattery controller\t2024-01-02\t2024\talpha\n" +
            "002\tOptical system\t2025-03-04\t2025\tbeta\n");
        try
        {
            PatentsViewLocalRowQueryRequest request = CreateRequest(path, "g_patent.tsv", DatasetFamily.PatentsViewGrant, "patent_id");
            request.Search.Filters = new[] { new SearchFilter { Field = "patent_title", Operator = SearchOperator.Contains, Value = "battery" } };

            PatentsViewLocalTypedQueryResult result = PatentsViewLocalTypedQuery.QueryPatents(request);
            PatentsViewPatent patent = (PatentsViewPatent)result.Records[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.Records, Has.Length.EqualTo(1));
                Assert.That(patent.PatentIdentifier, Is.EqualTo("001"));
                Assert.That(patent.Title, Is.EqualTo("Battery controller"));
                Assert.That(patent.GrantYear, Is.EqualTo(2024));
                Assert.That(patent.HasGrantDate, Is.True);
                Assert.That(patent.AdditionalFields["future_metric"], Is.EqualTo("alpha"));
                Assert.That(patent.Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
                Assert.That(patent.Provenance.DatasetVersion, Is.EqualTo("2026-09"));
                Assert.That(patent.Provenance.IsAuthoritative, Is.False);
                Assert.That(result.Metadata.Provenance, Is.EqualTo(DataProvenance.LocalMirror));
                Assert.That(result.Metadata.HasTotal, Is.True);
                Assert.That(result.IsComplete, Is.True);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void PublicationQueryUsesThePreGrantParser()
    {
        string path = CreateTable(
            "document_number\tpublication_title\tpublication_date\tpublication_year\tseries_code\trule_47_flag\n" +
            "US20240001234A1\tStorage controller\t2024-01-04\t2024\t9\tY\n");
        try
        {
            PatentsViewLocalRowQueryRequest request = CreateRequest(path, "pg_publication.tsv", DatasetFamily.PatentsViewPreGrant, "document_number");

            PatentsViewLocalTypedQueryResult result = PatentsViewLocalTypedQuery.QueryPublications(request);
            PatentsViewPublication publication = (PatentsViewPublication)result.Records[0];

            Assert.Multiple(() =>
            {
                Assert.That(publication.DocumentNumber, Is.EqualTo("US20240001234A1"));
                Assert.That(publication.Title, Is.EqualTo("Storage controller"));
                Assert.That(publication.PublicationYear, Is.EqualTo(2024));
                Assert.That(publication.SeriesCode, Is.EqualTo("09"));
                Assert.That(publication.Rule47, Is.True);
                Assert.That(publication.Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
                Assert.That(result.ScannedRowCount, Is.EqualTo(1));
                Assert.That(result.MatchedRowCount, Is.EqualTo(1));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void EntityQueriesSupportGrantAndPreGrantMirrorTables()
    {
        string inventorPath = CreateTable(
            "inventor_id\tinventor_first_name\tinventor_last_name\tpatent_count\n" +
            "inv-7\tAda\tLovelace\t12\n");
        string assigneePath = CreateTable(
            "document_number\tassignee_id\tassignee_organization\tassignee_type\n" +
            "US20240001234A1\tasg-8\tExample Labs\t2\n");
        try
        {
            PatentsViewLocalTypedQueryResult inventorResult = PatentsViewLocalTypedQuery.QueryInventors(
                CreateRequest(inventorPath, "g_inventor_disambiguated.tsv", DatasetFamily.PatentsViewGrant, "inventor_id"));
            PatentsViewLocalTypedQueryResult assigneeResult = PatentsViewLocalTypedQuery.QueryAssignees(
                CreateRequest(assigneePath, "pg_publication_assignee.tsv", DatasetFamily.PatentsViewPreGrant, "assignee_id"));
            PatentsViewInventor inventor = (PatentsViewInventor)inventorResult.Records[0];
            PatentsViewAssignee assignee = (PatentsViewAssignee)assigneeResult.Records[0];

            Assert.Multiple(() =>
            {
                Assert.That(inventor.InventorIdentifier, Is.EqualTo("inv-7"));
                Assert.That(inventor.FirstName, Is.EqualTo("Ada"));
                Assert.That(inventor.PatentCount, Is.EqualTo(12));
                Assert.That(inventor.Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
                Assert.That(assignee.AssigneeIdentifier, Is.EqualTo("asg-8"));
                Assert.That(assignee.Organization, Is.EqualTo("Example Labs"));
                Assert.That(assignee.AdditionalFields["document_number"], Is.EqualTo("US20240001234A1"));
                Assert.That(assignee.Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
            });
        }
        finally
        {
            File.Delete(inventorPath);
            File.Delete(assigneePath);
        }
    }

    [Test]
    public void ClassificationQueryProducesEverySupportedTypedClassification()
    {
        string cpcPath = CreateTable("cpc_id\tcpc_section\tcpc_group\tcpc_sequence\n" + "cpc-1\tG\tG06F3/01\t1\n");
        string ipcPath = CreateTable("ipc_id\tipc_section\tipc_main_group\tipc_subgroup\tipc_sequence\n" + "ipc-1\tG\t3\t01\t2\n");
        string uspcPath = CreateTable("uspc_id\tuspc_mainclass\tuspc_subclass\tuspc_sequence\n" + "uspc-1\t345\t156\t3\n");
        string wipoPath = CreateTable("wipo_id\twipo_sector_title\twipo_field_title\twipo_sequence\n" + "wipo-1\tElectrical engineering\tComputer technology\t4\n");
        try
        {
            object cpcValue = PatentsViewLocalTypedQuery.QueryClassifications(CreateRequest(cpcPath, "g_cpc_current.tsv", DatasetFamily.PatentsViewGrant, "cpc_id")).Records[0];
            object ipcValue = PatentsViewLocalTypedQuery.QueryClassifications(CreateRequest(ipcPath, "pg_ipc_at_issue.tsv", DatasetFamily.PatentsViewPreGrant, "ipc_id")).Records[0];
            object uspcValue = PatentsViewLocalTypedQuery.QueryClassifications(CreateRequest(uspcPath, "g_uspc_at_issue.tsv", DatasetFamily.PatentsViewGrant, "uspc_id")).Records[0];
            object wipoValue = PatentsViewLocalTypedQuery.QueryClassifications(CreateRequest(wipoPath, "pg_wipo_technology.tsv", DatasetFamily.PatentsViewPreGrant, "wipo_id")).Records[0];

            Assert.Multiple(() =>
            {
                Assert.That(cpcValue, Is.TypeOf<PatentsViewCpcClassification>());
                Assert.That(((PatentsViewCpcClassification)cpcValue).Group, Is.EqualTo("G06F3/01"));
                Assert.That(ipcValue, Is.TypeOf<PatentsViewIpcClassification>());
                Assert.That(((PatentsViewIpcClassification)ipcValue).MainGroup, Is.EqualTo("3"));
                Assert.That(uspcValue, Is.TypeOf<PatentsViewUspcClassification>());
                Assert.That(((PatentsViewUspcClassification)uspcValue).Subclass, Is.EqualTo("156"));
                Assert.That(wipoValue, Is.TypeOf<PatentsViewWipoClassification>());
                Assert.That(((PatentsViewWipoClassification)wipoValue).FieldTitle, Is.EqualTo("Computer technology"));
                Assert.That(((PatentsViewWipoClassification)wipoValue).Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
            });
        }
        finally
        {
            File.Delete(cpcPath);
            File.Delete(ipcPath);
            File.Delete(uspcPath);
            File.Delete(wipoPath);
        }
    }

    [Test]
    public void CitationQueryRetainsGrantAndPublicationCitationKinds()
    {
        string grantPath = CreateTable(
            "patent_id\tcitation_patent_id\tcitation_category\tcitation_date\tcitation_sequence\n" +
            "001\t999\texaminer\t2024-01-01\t1\n");
        string publicationPath = CreateTable(
            "document_number\treference_text\treference_sequence\n" +
            "US20240001234A1\tTechnical journal article\t3\n");
        try
        {
            PatentsViewCitation grant = (PatentsViewCitation)PatentsViewLocalTypedQuery.QueryCitations(
                CreateRequest(grantPath, "g_us_patent_citation.tsv", DatasetFamily.PatentsViewGrant, "citation_patent_id")).Records[0];
            PatentsViewCitation publication = (PatentsViewCitation)PatentsViewLocalTypedQuery.QueryCitations(
                CreateRequest(publicationPath, "pg_other_reference.tsv", DatasetFamily.PatentsViewPreGrant, "reference_sequence")).Records[0];

            Assert.Multiple(() =>
            {
                Assert.That(grant.CitationType, Is.EqualTo("us_patent"));
                Assert.That(grant.CitedPatentIdentifier, Is.EqualTo("999"));
                Assert.That(grant.HasCitationDate, Is.True);
                Assert.That(publication.CitationType, Is.EqualTo("other_reference"));
                Assert.That(publication.ReferenceText, Is.EqualTo("Technical journal article"));
                Assert.That(publication.Sequence, Is.EqualTo(3));
                Assert.That(publication.Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
            });
        }
        finally
        {
            File.Delete(grantPath);
            File.Delete(publicationPath);
        }
    }

    [Test]
    public void TypedQueriesRejectMismatchedTablesAndUnsupportedFamilies()
    {
        string path = CreateTable("patent_id\tpatent_title\n" + "001\tWidget\n");
        try
        {
            PatentsViewLocalRowQueryRequest wrongType = CreateRequest(path, "g_patent.tsv", DatasetFamily.PatentsViewGrant, "patent_id");
            ValidationException tableFailure = Assert.Throws<ValidationException>(() => PatentsViewLocalTypedQuery.QueryInventors(wrongType))!;
            PatentsViewLocalRowQueryRequest wrongFamily = CreateRequest(path, "annual_patent.tsv", DatasetFamily.PatentsViewAnnualized, "patent_id");
            ValidationException familyFailure = Assert.Throws<ValidationException>(() => PatentsViewLocalTypedQuery.QueryPatents(wrongFamily))!;

            Assert.Multiple(() =>
            {
                Assert.That(tableFailure.ErrorCode, Is.EqualTo("LOCAL_TABLE_TYPE_MISMATCH"));
                Assert.That(familyFailure.ErrorCode, Is.EqualTo("UNSUPPORTED_DATASET_FAMILY"));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static PatentsViewLocalRowQueryRequest CreateRequest(string path, string tableIdentifier, DatasetFamily family, string sortField)
    {
        return new PatentsViewLocalRowQueryRequest
        {
            Table = new PatentsViewLocalTableInfo
            {
                Family = family,
                TableIdentifier = tableIdentifier,
                FilePath = path,
                Delimiter = '\t',
                FileExists = true,
                Provenance = new PatentDataProvenance
                {
                    Source = DataProvenance.LocalMirror,
                    DatasetIdentifier = tableIdentifier,
                    DatasetVersion = "2026-09",
                    IsAuthoritative = false
                }
            },
            Search = new SearchRequest
            {
                Sorts = new[] { new SearchSort { Field = sortField, Direction = SortDirection.Ascending } },
                OffsetPage = new OffsetPage { Limit = 100 },
                MaximumResults = 100,
                Context = new RequestContext()
            },
            Limits = new UsptoLimits { MaximumResults = 100 },
            MaximumScannedRows = 100
        };
    }

    private static string CreateTable(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-typed-local-" + Guid.NewGuid().ToString("N") + ".tsv");
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }
}
