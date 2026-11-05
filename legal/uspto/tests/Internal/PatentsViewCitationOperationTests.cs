using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentsViewCitationOperationTests
{
    [Test]
    public void UnitedStatesCitationQueriesUseNestedRoutesAndMapDistinctFields()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(UsPatentBody());
        handler.BodiesToReturn.Enqueue(UsApplicationBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewCitationOperation operation = new PatentsViewCitationOperation(client, CreateOptions(client));

        PatentsViewCitation patent = operation.QueryUsPatentCitationsGet(CreateQuery("citation_patent_id")).Citations.Single();
        PatentsViewCitation application = operation.QueryUsApplicationCitationsPost(CreateQuery("citation_document_number")).Citations.Single();
        using JsonDocument postBody = JsonDocument.Parse(handler.Bodies[1]!);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/patent/us_patent_citation",
                "/custom/api/v1/patent/us_application_citation"
            }));
            Assert.That(handler.Methods, Is.EqualTo(new[] { HttpMethod.Get, HttpMethod.Post }));
            Assert.That(handler.ApiKeys, Is.EqualTo(new[] { "citation-secret", "citation-secret" }));
            Assert.That(postBody.RootElement.GetProperty("f")[0].GetString(), Is.EqualTo("citation_document_number"));
            Assert.That(patent.PatentIdentifier, Is.EqualTo("100"));
            Assert.That(patent.SourceResourceReference, Is.EqualTo("/api/v1/patent/100/"));
            Assert.That(patent.CitationType, Is.EqualTo("us_patent"));
            Assert.That(patent.CitedPatentIdentifier, Is.EqualTo("50"));
            Assert.That(patent.CitedResourceReference, Is.EqualTo("/api/v1/patent/50/"));
            Assert.That(patent.KindCode, Is.EqualTo("B2"));
            Assert.That(patent.CitationCategory, Is.EqualTo("examiner"));
            Assert.That(patent.CitationName, Is.EqualTo("Example Inventor"));
            Assert.That(patent.CitationDate, Is.EqualTo(new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(patent.HasCitationDate, Is.True);
            Assert.That(patent.Sequence, Is.EqualTo(0));
            Assert.That(patent.HasSequence, Is.True);
            Assert.That(patent.AdditionalFields["future_field"], Is.EqualTo("true"));
            Assert.That(patent.Provenance.DatasetIdentifier, Is.EqualTo("us_patent_citations"));
            Assert.That(application.CitationType, Is.EqualTo("us_application"));
            Assert.That(application.CitedDocumentNumber, Is.EqualTo("US20200012345A1"));
            Assert.That(application.KindCode, Is.EqualTo("A1"));
            Assert.That(application.Sequence, Is.EqualTo(4));
        });
    }

    [Test]
    public void ForeignAndOtherReferenceQueriesMapCategorySpecificValues()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(ForeignBody());
        handler.BodiesToReturn.Enqueue(OtherReferenceBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewCitationOperation operation = new PatentsViewCitationOperation(client, CreateOptions(client));

        PatentsViewCitation foreign = operation.QueryForeignCitationsGet(CreateQuery("citation_number")).Citations.Single();
        PatentsViewCitation other = operation.QueryOtherReferencesPost(CreateQuery("reference_sequence")).Citations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/patent/foreign_citation",
                "/custom/api/v1/patent/other_reference"
            }));
            Assert.That(foreign.CitationType, Is.EqualTo("foreign"));
            Assert.That(foreign.CitationNumber, Is.EqualTo("EP1234567"));
            Assert.That(foreign.Country, Is.EqualTo("EP"));
            Assert.That(foreign.CitationCategory, Is.EqualTo("applicant"));
            Assert.That(foreign.Sequence, Is.EqualTo(2));
            Assert.That(other.CitationType, Is.EqualTo("other"));
            Assert.That(other.Sequence, Is.EqualTo(3));
            Assert.That(other.HasSequence, Is.True);
            Assert.That(other.ReferenceText, Is.EqualTo("Non-patent literature reference"));
            Assert.That(other.Provenance.DatasetIdentifier, Is.EqualTo("other_references"));
        });
    }

    [Test]
    public void PatentAndPublicationRelationQueriesKeepTheirIdentifiersSeparate()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PatentRelationBody());
        handler.BodiesToReturn.Enqueue(PublicationRelationBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewCitationOperation operation = new PatentsViewCitationOperation(client, CreateOptions(client));

        PatentsViewRelatedApplication patent = operation.QueryPatentRelatedApplicationsGet(CreateQuery("patent_id")).RelatedApplications.Single();
        PatentsViewRelatedApplication publication = operation.QueryPublicationRelatedApplicationsPost(CreateQuery("document_number")).RelatedApplications.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/patent/rel_app_text",
                "/custom/api/v1/publication/rel_app_text"
            }));
            Assert.That(patent.PatentIdentifier, Is.EqualTo("100"));
            Assert.That(patent.DocumentNumber, Is.Null);
            Assert.That(patent.RelatedText, Does.Contain("continuation"));
            Assert.That(patent.Provenance.DatasetIdentifier, Is.EqualTo("g_rel_app_text"));
            Assert.That(patent.AdditionalFields["future_relation"], Is.EqualTo("7"));
            Assert.That(publication.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(publication.PatentIdentifier, Is.Null);
            Assert.That(publication.RelatedText, Does.Contain("priority"));
            Assert.That(publication.Provenance.DatasetIdentifier, Is.EqualTo("pg_rel_app_text"));
        });
    }

    [Test]
    public void UnitedStatesCitationRetrievalUsesDocumentedIdentifierRoutes()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(UsPatentBody("US10/0"));
        handler.BodiesToReturn.Enqueue(UsApplicationBody("US10/0"));
        using HttpClient client = new HttpClient(handler);
        PatentsViewCitationOperation operation = new PatentsViewCitationOperation(client, CreateOptions(client));

        PatentsViewCitationResponse patents = operation.GetUsPatentCitations("US10/0", new RequestContext());
        PatentsViewCitationResponse applications = operation.GetUsApplicationCitations("US10/0", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/patent/us_patent_citation/US10%2F0",
                "/custom/api/v1/patent/us_application_citation/US10%2F0"
            }));
            Assert.That(patents.Citations.Single().PatentIdentifier, Is.EqualTo("US10/0"));
            Assert.That(applications.Citations.Single().PatentIdentifier, Is.EqualTo("US10/0"));
        });
    }

    [Test]
    public void ResourcesWithoutIdentifierRoutesUseExactBoundedQueries()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(ForeignBody());
        handler.BodiesToReturn.Enqueue(OtherReferenceBody());
        handler.BodiesToReturn.Enqueue(PatentRelationBody());
        handler.BodiesToReturn.Enqueue(PublicationRelationBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewCitationOperation operation = new PatentsViewCitationOperation(client, CreateOptions(client));

        operation.GetForeignCitations("100", new RequestContext());
        operation.GetOtherReferences("100", new RequestContext());
        operation.GetPatentRelatedApplications("100", new RequestContext());
        operation.GetPublicationRelatedApplications("US20240001234A1", new RequestContext());
        string foreignQuery = Uri.UnescapeDataString(handler.Uris[0]!.Query);
        string otherQuery = Uri.UnescapeDataString(handler.Uris[1]!.Query);
        string patentRelationQuery = Uri.UnescapeDataString(handler.Uris[2]!.Query);
        string publicationRelationQuery = Uri.UnescapeDataString(handler.Uris[3]!.Query);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/patent/foreign_citation",
                "/custom/api/v1/patent/other_reference",
                "/custom/api/v1/patent/rel_app_text",
                "/custom/api/v1/publication/rel_app_text"
            }));
            Assert.That(foreignQuery, Does.Contain("q={\"_eq\":{\"patent_id\":\"100\"}}"));
            Assert.That(foreignQuery, Does.Contain("s=[{\"patent_id\":\"asc\"},{\"citation_sequence\":\"asc\"}]"));
            Assert.That(otherQuery, Does.Contain("s=[{\"patent_id\":\"asc\"},{\"reference_sequence\":\"asc\"}]"));
            Assert.That(patentRelationQuery, Does.Contain("q={\"_eq\":{\"patent_id\":\"100\"}}"));
            Assert.That(publicationRelationQuery, Does.Contain("q={\"_eq\":{\"document_number\":\"US20240001234A1\"}}"));
            Assert.That(publicationRelationQuery, Does.Contain("o={\"size\":1000}"));
        });
    }

    [Test]
    public void InvalidSequencesAndMismatchedRetrievedOwnersFailClearly()
    {
        string negativeBody = "{\"error\":false,\"count\":1,\"total_hits\":1,\"foreign_citations\":[{\"patent_id\":\"100\",\"citation_sequence\":-1}]}";
        PatentsViewPatentHandler negativeHandler = new PatentsViewPatentHandler(negativeBody);
        using HttpClient negativeClient = new HttpClient(negativeHandler);
        PatentsViewCitationOperation negativeOperation = new PatentsViewCitationOperation(negativeClient, CreateOptions(negativeClient));
        ParseException negative = Assert.Throws<ParseException>(() => negativeOperation.QueryForeignCitationsGet(CreateQuery("patent_id")));

        PatentsViewPatentHandler mismatchHandler = new PatentsViewPatentHandler(UsPatentBody());
        using HttpClient mismatchClient = new HttpClient(mismatchHandler);
        PatentsViewCitationOperation mismatchOperation = new PatentsViewCitationOperation(mismatchClient, CreateOptions(mismatchClient));
        ParseException mismatch = Assert.Throws<ParseException>(() => mismatchOperation.GetUsPatentCitations("101", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(negative!.DataPath, Is.EqualTo("$.foreign_citations[0].citation_sequence"));
            Assert.That(mismatch!.DataPath, Is.EqualTo("$.us_patent_citations[0].patent_id"));
        });
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            PatentsViewBaseUri = new Uri("https://example.test/custom/"),
            PatentsViewApiKey = "citation-secret"
        };
        options.Retry.AttemptCount = 1;
        return options;
    }

    private static PatentsViewQueryRequest CreateQuery(string field)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new[] { new SearchFilter { Field = "patent_id", Operator = SearchOperator.Equal, Value = "100" } };
        request.Search.FieldSelection.Fields = new[] { field };
        request.Search.Sorts = new[] { new SearchSort { Field = field, Direction = SortDirection.Ascending } };
        request.Search.CursorPage.Size = 10;
        return request;
    }

    private static string UsPatentBody(string patentIdentifier = "100")
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"us_patent_citations\":[{\"patent_id\":\"" + patentIdentifier + "\",\"patent\":\"/api/v1/patent/100/\",\"citation_sequence\":0,\"citation_patent_id\":\"50\",\"citation_patent\":\"/api/v1/patent/50/\",\"citation_wipo_kind\":\"B2\",\"citation_category\":\"examiner\",\"citation_date\":\"2020-01-02\",\"citation_name\":\"Example Inventor\",\"future_field\":true}]}";
    }

    private static string UsApplicationBody(string patentIdentifier = "100")
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"us_application_citations\":[{\"patent_id\":\"" + patentIdentifier + "\",\"patent\":\"/api/v1/patent/100/\",\"citation_sequence\":4,\"citation_document_number\":\"US20200012345A1\",\"citation_wipo_kind\":\"A1\",\"citation_category\":\"applicant\",\"citation_date\":\"2020-02-03\",\"citation_name\":\"Example Applicant\"}]}";
    }

    private static string ForeignBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"foreign_citations\":[{\"patent_id\":\"100\",\"patent\":\"/api/v1/patent/100/\",\"citation_number\":\"EP1234567\",\"citation_sequence\":2,\"citation_date\":\"2019-04-05\",\"citation_category\":\"applicant\",\"citation_country\":\"EP\"}]}";
    }

    private static string OtherReferenceBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"other_references\":[{\"patent_id\":\"100\",\"patent\":\"/api/v1/patent/100/\",\"reference_sequence\":\"3\",\"reference_text\":\"Non-patent literature reference\"}]}";
    }

    private static string PatentRelationBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"rel_app_texts\":[{\"patent_id\":\"100\",\"related_text\":\"This is a continuation of application 12/345,678.\",\"future_relation\":7}]}";
    }

    private static string PublicationRelationBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"rel_app_texts\":[{\"document_number\":\"US20240001234A1\",\"related_text\":\"This application claims priority to an earlier filing.\"}]}";
    }
}
