using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentsViewTextOperationTests
{
    [Test]
    public void GrantGetAndPublicationPostQueriesCoverEveryLongTextResource()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(GrantSummaryBody());
        handler.BodiesToReturn.Enqueue(PublicationSummaryBody());
        handler.BodiesToReturn.Enqueue(GrantClaimBody());
        handler.BodiesToReturn.Enqueue(PublicationClaimBody());
        handler.BodiesToReturn.Enqueue(GrantDescriptionBody());
        handler.BodiesToReturn.Enqueue(PublicationDescriptionBody());
        handler.BodiesToReturn.Enqueue(GrantDrawingBody());
        handler.BodiesToReturn.Enqueue(PublicationDrawingBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewTextOperation operation = new PatentsViewTextOperation(client, CreateOptions(client));

        PatentsViewSummary grantSummary = operation.QueryGrantSummariesGet(CreateGrantQuery("patent_id")).Summaries.Single();
        PatentsViewSummary publicationSummary = operation.QueryPublicationSummariesPost(CreatePublicationQuery("document_number")).Summaries.Single();
        PatentsViewClaim grantClaim = operation.QueryGrantClaimsGet(CreateGrantQuery("claim_sequence")).Claims.Single();
        PatentsViewClaim publicationClaim = operation.QueryPublicationClaimsPost(CreatePublicationQuery("claim_sequence")).Claims.Single();
        PatentsViewDescription grantDescription = operation.QueryGrantDescriptionsGet(CreateGrantQuery("patent_id")).Descriptions.Single();
        PatentsViewDescription publicationDescription = operation.QueryPublicationDescriptionsPost(CreatePublicationQuery("document_number")).Descriptions.Single();
        PatentsViewDrawingDescription grantDrawing = operation.QueryGrantDrawingDescriptionsGet(CreateGrantQuery("draw_desc_sequence")).DrawingDescriptions.Single();
        PatentsViewDrawingDescription publicationDrawing = operation.QueryPublicationDrawingDescriptionsPost(CreatePublicationQuery("draw_desc_sequence")).DrawingDescriptions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/g_brf_sum_text",
                "/custom/api/v1/pg_brf_sum_text",
                "/custom/api/v1/g_claim",
                "/custom/api/v1/pg_claim",
                "/custom/api/v1/g_detail_desc_text",
                "/custom/api/v1/pg_detail_desc_text",
                "/custom/api/v1/g_draw_desc_text",
                "/custom/api/v1/pg_draw_desc_text"
            }));
            Assert.That(handler.Methods, Is.EqualTo(new[]
            {
                HttpMethod.Get, HttpMethod.Post, HttpMethod.Get, HttpMethod.Post,
                HttpMethod.Get, HttpMethod.Post, HttpMethod.Get, HttpMethod.Post
            }));
            Assert.That(Uri.UnescapeDataString(handler.Uris[0]!.Query), Does.Contain("o={\"size\":10,\"pad_patent_id\":true}"));
            Assert.That(Uri.UnescapeDataString(handler.Uris[2]!.Query), Does.Contain("pad_patent_id"));
            Assert.That(handler.Bodies[1], Does.Not.Contain("pad_patent_id"));
            Assert.That(handler.Bodies[3], Does.Not.Contain("pad_patent_id"));
            Assert.That(grantSummary.PatentIdentifier, Is.EqualTo("12000000"));
            Assert.That(grantSummary.Text, Is.EqualTo("Grant summary text."));
            Assert.That(grantSummary.AdditionalFields["future_summary"], Is.EqualTo("true"));
            Assert.That(grantSummary.Provenance.DatasetIdentifier, Is.EqualTo("g_brf_sum_texts"));
            Assert.That(publicationSummary.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(publicationSummary.PatentIdentifier, Is.Null);
            Assert.That(grantClaim.ClaimNumber, Is.EqualTo("00002"));
            Assert.That(grantClaim.Sequence, Is.EqualTo(1));
            Assert.That(grantClaim.HasSequence, Is.True);
            Assert.That(grantClaim.IsExemplary, Is.True);
            Assert.That(grantClaim.HasIsExemplary, Is.True);
            Assert.That(grantClaim.DependsOnClaim, Is.EqualTo("00001"));
            Assert.That(grantClaim.AdditionalFields["future_claim"], Is.EqualTo("{\"rank\":1}"));
            Assert.That(publicationClaim.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(publicationClaim.HasIsExemplary, Is.False);
            Assert.That(grantDescription.Length, Is.EqualTo(31));
            Assert.That(grantDescription.HasLength, Is.True);
            Assert.That(publicationDescription.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(publicationDescription.Length, Is.EqualTo(37));
            Assert.That(grantDrawing.Sequence, Is.EqualTo(1));
            Assert.That(grantDrawing.HasSequence, Is.True);
            Assert.That(publicationDrawing.Text, Is.EqualTo("FIG. 2 shows the publication."));
            Assert.That(publicationDrawing.Provenance.DatasetIdentifier, Is.EqualTo("pg_draw_desc_texts"));
        });
    }

    [Test]
    public void GrantPostQueriesRetainOnlyTheDocumentedPatentLinkedOptions()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(GrantSummaryBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewTextOperation operation = new PatentsViewTextOperation(client, CreateOptions(client));
        PatentsViewQueryRequest request = CreateGrantQuery("patent_id");
        request.ExcludeWithdrawnPatents = false;

        operation.QueryGrantSummariesPost(request);
        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single()!);
        JsonElement options = body.RootElement.GetProperty("o");

        Assert.Multiple(() =>
        {
            Assert.That(options.GetProperty("size").GetInt32(), Is.EqualTo(10));
            Assert.That(options.GetProperty("pad_patent_id").GetBoolean(), Is.True);
            Assert.That(options.TryGetProperty("exclude_withdrawn", out _), Is.False);
            Assert.That(handler.ContentTypes.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("text-secret"));
        });
    }

    [Test]
    public void IdentifierLookupsUseExactBoundedQueriesInsteadOfUndocumentedRoutes()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(GrantSummaryBody());
        handler.BodiesToReturn.Enqueue(PublicationSummaryBody());
        handler.BodiesToReturn.Enqueue(GrantClaimBody());
        handler.BodiesToReturn.Enqueue(PublicationClaimBody());
        handler.BodiesToReturn.Enqueue(GrantDescriptionBody());
        handler.BodiesToReturn.Enqueue(PublicationDescriptionBody());
        handler.BodiesToReturn.Enqueue(GrantDrawingBody());
        handler.BodiesToReturn.Enqueue(PublicationDrawingBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewTextOperation operation = new PatentsViewTextOperation(client, CreateOptions(client));

        operation.GetGrantSummary("12000000", new RequestContext());
        operation.GetPublicationSummary("US20240001234A1", new RequestContext());
        operation.GetGrantClaims("12000000", new RequestContext());
        operation.GetPublicationClaims("US20240001234A1", new RequestContext());
        operation.GetGrantDescription("12000000", new RequestContext());
        operation.GetPublicationDescription("US20240001234A1", new RequestContext());
        operation.GetGrantDrawingDescriptions("12000000", new RequestContext());
        operation.GetPublicationDrawingDescriptions("US20240001234A1", new RequestContext());
        string grantSummaryQuery = Uri.UnescapeDataString(handler.Uris[0]!.Query);
        string publicationSummaryQuery = Uri.UnescapeDataString(handler.Uris[1]!.Query);
        string grantClaimQuery = Uri.UnescapeDataString(handler.Uris[2]!.Query);
        string publicationDrawingQuery = Uri.UnescapeDataString(handler.Uris[7]!.Query);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/g_brf_sum_text",
                "/custom/api/v1/pg_brf_sum_text",
                "/custom/api/v1/g_claim",
                "/custom/api/v1/pg_claim",
                "/custom/api/v1/g_detail_desc_text",
                "/custom/api/v1/pg_detail_desc_text",
                "/custom/api/v1/g_draw_desc_text",
                "/custom/api/v1/pg_draw_desc_text"
            }));
            Assert.That(grantSummaryQuery, Does.Contain("q={\"_eq\":{\"patent_id\":\"12000000\"}}"));
            Assert.That(grantSummaryQuery, Does.Contain("o={\"size\":2,\"pad_patent_id\":false}"));
            Assert.That(publicationSummaryQuery, Does.Contain("q={\"_eq\":{\"document_number\":\"US20240001234A1\"}}"));
            Assert.That(publicationSummaryQuery, Does.Contain("o={\"size\":2}"));
            Assert.That(grantClaimQuery, Does.Contain("s=[{\"patent_id\":\"asc\"},{\"claim_sequence\":\"asc\"}]"));
            Assert.That(grantClaimQuery, Does.Contain("o={\"size\":1000,\"pad_patent_id\":false}"));
            Assert.That(publicationDrawingQuery, Does.Contain("s=[{\"document_number\":\"asc\"},{\"draw_desc_sequence\":\"asc\"}]"));
            Assert.That(publicationDrawingQuery, Does.Contain("o={\"size\":1000}"));
        });
    }

    [Test]
    public void SparseAvailabilityReturnsEmptyCollectionsAndClearSingularAbsence()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(EmptyBody("g_claims"));
        handler.BodiesToReturn.Enqueue(EmptyBody("pg_draw_desc_texts"));
        handler.BodiesToReturn.Enqueue(EmptyBody("g_brf_sum_texts"));
        using HttpClient client = new HttpClient(handler);
        PatentsViewTextOperation operation = new PatentsViewTextOperation(client, CreateOptions(client));

        PatentsViewClaimResponse claims = operation.GetGrantClaims("100", new RequestContext());
        PatentsViewDrawingDescriptionResponse drawings = operation.GetPublicationDrawingDescriptions("US20020000001A1", new RequestContext());
        NotFoundException missing = Assert.Throws<NotFoundException>(() => operation.GetGrantSummary("100", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(claims.Claims, Is.Empty);
            Assert.That(claims.Metadata.IsComplete, Is.True);
            Assert.That(drawings.DrawingDescriptions, Is.Empty);
            Assert.That(missing!.ResourceIdentifier, Is.EqualTo("100"));
            Assert.That(missing.RequestIdentifier, Is.EqualTo("pv-request"));
            Assert.That(missing.Message, Does.Contain("coverage is limited"));
        });
    }

    [Test]
    public void MismatchedAndDuplicateLookupRecordsFailClearly()
    {
        PatentsViewPatentHandler mismatchHandler = new PatentsViewPatentHandler(PublicationClaimBody("US20249999999A1"));
        using HttpClient mismatchClient = new HttpClient(mismatchHandler);
        PatentsViewTextOperation mismatchOperation = new PatentsViewTextOperation(mismatchClient, CreateOptions(mismatchClient));
        ParseException mismatch = Assert.Throws<ParseException>(() => mismatchOperation.GetPublicationClaims("US20240001234A1", new RequestContext()));

        string duplicateBody = "{\"error\":false,\"count\":2,\"total_hits\":2,\"g_brf_sum_texts\":[{\"patent_id\":\"12000000\",\"summary_text\":\"one\"},{\"patent_id\":\"12000000\",\"summary_text\":\"two\"}]}";
        PatentsViewPatentHandler duplicateHandler = new PatentsViewPatentHandler(duplicateBody);
        using HttpClient duplicateClient = new HttpClient(duplicateHandler);
        PatentsViewTextOperation duplicateOperation = new PatentsViewTextOperation(duplicateClient, CreateOptions(duplicateClient));
        ParseException duplicate = Assert.Throws<ParseException>(() => duplicateOperation.GetGrantSummary("12000000", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(mismatch!.DataPath, Is.EqualTo("$.pg_claims[0]"));
            Assert.That(duplicate!.DataPath, Is.EqualTo("$.g_brf_sum_texts"));
        });
    }

    [Test]
    public void InvalidClaimDescriptionAndDrawingValuesReportTheirSourcePaths()
    {
        string claimBody = "{\"error\":false,\"count\":1,\"total_hits\":1,\"g_claims\":[{\"patent_id\":\"12000000\",\"claim_sequence\":-1}]}";
        PatentsViewPatentHandler claimHandler = new PatentsViewPatentHandler(claimBody);
        using HttpClient claimClient = new HttpClient(claimHandler);
        PatentsViewTextOperation claimOperation = new PatentsViewTextOperation(claimClient, CreateOptions(claimClient));
        ParseException claim = Assert.Throws<ParseException>(() => claimOperation.QueryGrantClaimsGet(CreateGrantQuery("claim_sequence")));

        string descriptionBody = "{\"error\":false,\"count\":1,\"total_hits\":1,\"pg_detail_desc_texts\":[{\"document_number\":\"US20240001234A1\",\"description_length\":-2}]}";
        PatentsViewPatentHandler descriptionHandler = new PatentsViewPatentHandler(descriptionBody);
        using HttpClient descriptionClient = new HttpClient(descriptionHandler);
        PatentsViewTextOperation descriptionOperation = new PatentsViewTextOperation(descriptionClient, CreateOptions(descriptionClient));
        ParseException description = Assert.Throws<ParseException>(() => descriptionOperation.QueryPublicationDescriptionsGet(CreatePublicationQuery("description_length")));

        string drawingBody = "{\"error\":false,\"count\":1,\"total_hits\":1,\"g_draw_desc_texts\":[{\"patent_id\":\"12000000\",\"draw_desc_sequence\":0}]}";
        PatentsViewPatentHandler drawingHandler = new PatentsViewPatentHandler(drawingBody);
        using HttpClient drawingClient = new HttpClient(drawingHandler);
        PatentsViewTextOperation drawingOperation = new PatentsViewTextOperation(drawingClient, CreateOptions(drawingClient));
        ParseException drawing = Assert.Throws<ParseException>(() => drawingOperation.QueryGrantDrawingDescriptionsGet(CreateGrantQuery("draw_desc_sequence")));

        Assert.Multiple(() =>
        {
            Assert.That(claim!.DataPath, Is.EqualTo("$.g_claims[0].claim_sequence"));
            Assert.That(description!.DataPath, Is.EqualTo("$.pg_detail_desc_texts[0].description_length"));
            Assert.That(drawing!.DataPath, Is.EqualTo("$.g_draw_desc_texts[0].draw_desc_sequence"));
        });
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            PatentsViewBaseUri = new Uri("https://example.test/custom/"),
            PatentsViewApiKey = "text-secret"
        };
        options.Retry.AttemptCount = 1;
        return options;
    }

    private static PatentsViewQueryRequest CreateGrantQuery(string field)
    {
        PatentsViewQueryRequest request = CreatePublicationQuery(field);
        request.Search.Filters[0].Field = "patent_id";
        request.Search.Filters[0].Value = "12000000";
        request.PadPatentIdentifiers = true;
        return request;
    }

    private static PatentsViewQueryRequest CreatePublicationQuery(string field)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new[] { new SearchFilter { Field = "document_number", Operator = SearchOperator.Equal, Value = "US20240001234A1" } };
        request.Search.FieldSelection.Fields = new[] { field };
        request.Search.Sorts = new[] { new SearchSort { Field = field, Direction = SortDirection.Ascending } };
        request.Search.CursorPage.Size = 10;
        return request;
    }

    private static string GrantSummaryBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"g_brf_sum_texts\":[{\"patent_id\":\"12000000\",\"summary_text\":\"Grant summary text.\",\"future_summary\":true}]}";
    }

    private static string PublicationSummaryBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"pg_brf_sum_texts\":[{\"document_number\":\"US20240001234A1\",\"summary_text\":\"Publication summary text.\"}]}";
    }

    private static string GrantClaimBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"g_claims\":[{\"patent_id\":\"12000000\",\"claim_sequence\":1,\"claim_number\":\"00002\",\"claim_text\":\"2. The apparatus of claim 1.\",\"exemplary\":1,\"claim_dependent\":\"00001\",\"future_claim\":{\"rank\":1}}]}";
    }

    private static string PublicationClaimBody(string documentNumber = "US20240001234A1")
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"pg_claims\":[{\"document_number\":\"" + documentNumber + "\",\"claim_sequence\":0,\"claim_number\":\"00001\",\"claim_text\":\"1. A publication apparatus.\",\"claim_dependent\":null}]}";
    }

    private static string GrantDescriptionBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"g_detail_desc_texts\":[{\"patent_id\":\"12000000\",\"description_text\":\"Detailed grant description text.\",\"description_length\":31}]}";
    }

    private static string PublicationDescriptionBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"pg_detail_desc_texts\":[{\"document_number\":\"US20240001234A1\",\"description_text\":\"Detailed publication description text.\",\"description_length\":37}]}";
    }

    private static string GrantDrawingBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"g_draw_desc_texts\":[{\"patent_id\":\"12000000\",\"draw_desc_sequence\":1,\"draw_desc_text\":\"FIG. 1 shows the grant.\"}]}";
    }

    private static string PublicationDrawingBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"pg_draw_desc_texts\":[{\"document_number\":\"US20240001234A1\",\"draw_desc_sequence\":2,\"draw_desc_text\":\"FIG. 2 shows the publication.\"}]}";
    }

    private static string EmptyBody(string collectionName)
    {
        return "{\"error\":false,\"count\":0,\"total_hits\":0,\"" + collectionName + "\":[]}";
    }
}
