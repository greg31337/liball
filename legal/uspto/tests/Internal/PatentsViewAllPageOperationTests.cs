using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentsViewAllPageOperationTests
{
    [Test]
    public void PatentGetAggregatesPagesWithoutMutatingCallerFieldsOrSorts()
    {
        string firstBody = PatentPage("100", "2024-01-03", "101", "2024-01-02", 3, true);
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(firstBody);
        handler.BodiesToReturn.Enqueue(PatentPage("102", "2024-01-01", null, null, 3, false));
        using HttpClient client = new HttpClient(handler);
        PatentsViewAllPageOperation operation = new PatentsViewAllPageOperation(client, CreateOptions(client));
        PatentsViewQueryRequest request = CreatePatentQuery(2);
        request.Search.Context.RetainRawResponse = true;

        PatentsViewPatentResponse result = operation.QueryPatentsAll(request, false);
        string firstQuery = Uri.UnescapeDataString(handler.Uris[0]!.Query);
        string secondQuery = Uri.UnescapeDataString(handler.Uris[1]!.Query);

        Assert.Multiple(() =>
        {
            Assert.That(handler.CallCount, Is.EqualTo(2));
            Assert.That(handler.Methods, Is.EqualTo(new[] { HttpMethod.Get, HttpMethod.Get }));
            Assert.That(result.Patents.Select(patent => patent.PatentIdentifier), Is.EqualTo(new[] { "100", "101", "102" }));
            Assert.That(result.Metadata.Count, Is.EqualTo(3));
            Assert.That(result.Metadata.TotalHits, Is.EqualTo(3));
            Assert.That(result.Metadata.HasTotalHits, Is.True);
            Assert.That(result.Metadata.IsComplete, Is.True);
            Assert.That(result.Metadata.HasNextCursorPage, Is.False);
            Assert.That(result.Metadata.Request, Is.SameAs(request));
            Assert.That(result.Metadata.RequestIdentifier, Is.EqualTo("pv-request"));
            Assert.That(result.Metadata.RawResponse, Is.EqualTo(firstBody));
            Assert.That(result.Metadata.AdditionalFields["future_response"], Is.EqualTo("true"));
            Assert.That(request.Search.FieldSelection.Fields, Is.EqualTo(new[] { "patent_title" }));
            Assert.That(request.Search.Sorts.Select(sort => sort.Field), Is.EqualTo(new[] { "patent_date", "patent_id" }));
            Assert.That(firstQuery, Does.Contain("f=[\"patent_title\",\"patent_date\",\"patent_id\"]"));
            Assert.That(secondQuery, Does.Contain("f=[\"patent_title\",\"patent_date\",\"patent_id\"]"));
            Assert.That(firstQuery, Does.Contain("s=[{\"patent_date\":\"desc\"},{\"patent_id\":\"asc\"}]"));
            Assert.That(secondQuery, Does.Contain("\"after\":[\"2024-01-02\",\"101\"]"));
        });
    }

    [Test]
    public void PublicationPostHonorsResultBoundAndReturnsContinuationCursor()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PublicationPage("US20240000001A1", "US20240000002A1", 5));
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        PatentsViewAllPageOperation operation = new PatentsViewAllPageOperation(client, options);
        PatentsViewQueryRequest request = CreateSingleQuery("document_number", 3);
        request.Search.FieldSelection.Fields = new[] { "publication_title" };
        request.Search.MaximumResults = 2;

        PatentsViewPublicationResponse result = operation.QueryPublicationsAll(request, true);
        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single()!);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods.Single(), Is.EqualTo(HttpMethod.Post));
            Assert.That(body.RootElement.GetProperty("f").EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { "publication_title", "document_number" }));
            Assert.That(body.RootElement.GetProperty("o").GetProperty("size").GetInt32(), Is.EqualTo(2));
            Assert.That(request.Search.CursorPage.Size, Is.EqualTo(3));
            Assert.That(request.Search.FieldSelection.Fields, Is.EqualTo(new[] { "publication_title" }));
            Assert.That(result.Publications, Has.Length.EqualTo(2));
            Assert.That(result.Metadata.IsComplete, Is.False);
            Assert.That(result.Metadata.HasNextCursorPage, Is.True);
            Assert.That(result.Metadata.NextCursorPage.AfterValues, Is.EqualTo(new[] { "US20240000002A1" }));
            Assert.That(result.Metadata.NextCursorPage.Size, Is.EqualTo(3));
        });
    }

    [Test]
    public void CompositeClaimIdentityAllowsMultipleRecordsForOneDocumentAcrossPages()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PublicationClaimPage(0, 1, 3));
        handler.BodiesToReturn.Enqueue(PublicationClaimPage(2, -1, 3));
        using HttpClient client = new HttpClient(handler);
        PatentsViewAllPageOperation operation = new PatentsViewAllPageOperation(client, CreateOptions(client));
        PatentsViewQueryRequest request = CreateCompositeQuery("document_number", "claim_sequence", 2);
        request.Search.FieldSelection.Fields = new[] { "claim_text" };

        PatentsViewClaimResponse result = operation.QueryPublicationClaimsAll(request, false);
        string firstQuery = Uri.UnescapeDataString(handler.Uris[0]!.Query);
        string secondQuery = Uri.UnescapeDataString(handler.Uris[1]!.Query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Claims.Select(claim => claim.Sequence), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(result.Claims.All(claim => claim.DocumentNumber == "US20240001234A1"), Is.True);
            Assert.That(result.Metadata.Count, Is.EqualTo(3));
            Assert.That(result.Metadata.IsComplete, Is.True);
            Assert.That(firstQuery, Does.Contain("f=[\"claim_text\",\"document_number\",\"claim_sequence\"]"));
            Assert.That(firstQuery, Does.Contain("s=[{\"document_number\":\"asc\"},{\"claim_sequence\":\"asc\"}]"));
            Assert.That(secondQuery, Does.Contain("\"after\":[\"US20240001234A1\",\"1\"]"));
        });
    }

    [Test]
    public void EveryQueryableEndpointCanProduceAnEmptyTypedAggregate()
    {
        string[] collections = new[]
        {
            "patents", "publications", "inventors", "assignees", "locations", "attorneys",
            "cpc_classes", "cpc_subclasses", "cpc_groups", "ipcr", "uspc_mainclasses", "uspc_subclasses", "wipo",
            "us_patent_citations", "us_application_citations", "foreign_citations", "other_references",
            "rel_app_texts", "rel_app_texts", "g_brf_sum_texts", "pg_brf_sum_texts", "g_claims", "pg_claims",
            "g_detail_desc_texts", "pg_detail_desc_texts", "g_draw_desc_texts", "pg_draw_desc_texts"
        };
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(EmptyPage(collections[0]));
        int bodyIndex;
        for (bodyIndex = 1; bodyIndex < collections.Length; bodyIndex += 1)
        {
            handler.BodiesToReturn.Enqueue(EmptyPage(collections[bodyIndex]));
        }

        using HttpClient client = new HttpClient(handler);
        PatentsViewAllPageOperation operation = new PatentsViewAllPageOperation(client, CreateOptions(client));

        operation.QueryPatentsAll(CreateSingleQuery("patent_id", 10), false);
        operation.QueryPublicationsAll(CreateSingleQuery("document_number", 10), true);
        operation.QueryInventorsAll(CreateSingleQuery("inventor_id", 10), false);
        operation.QueryAssigneesAll(CreateSingleQuery("assignee_id", 10), true);
        operation.QueryLocationsAll(CreateSingleQuery("location_id", 10), false);
        operation.QueryAttorneysAll(CreateSingleQuery("attorney_id", 10), true);
        operation.QueryCpcClassesAll(CreateSingleQuery("cpc_class_id", 10), false);
        operation.QueryCpcSubclassesAll(CreateSingleQuery("cpc_subclass_id", 10), true);
        operation.QueryCpcGroupsAll(CreateSingleQuery("cpc_group_id", 10), false);
        operation.QueryIpcAll(CreateSingleQuery("ipc_id", 10), true);
        operation.QueryUspcMainClassesAll(CreateSingleQuery("uspc_mainclass_id", 10), false);
        operation.QueryUspcSubclassesAll(CreateSingleQuery("uspc_subclass_id", 10), true);
        operation.QueryWipoAll(CreateSingleQuery("wipo_id", 10), false);
        operation.QueryUsPatentCitationsAll(CreateCompositeQuery("patent_id", "citation_sequence", 10), true);
        operation.QueryUsApplicationCitationsAll(CreateCompositeQuery("patent_id", "citation_sequence", 10), false);
        operation.QueryForeignCitationsAll(CreateCompositeQuery("patent_id", "citation_sequence", 10), true);
        operation.QueryOtherReferencesAll(CreateCompositeQuery("patent_id", "reference_sequence", 10), false);
        operation.QueryPatentRelatedApplicationsAll(CreateSingleQuery("patent_id", 10), true);
        operation.QueryPublicationRelatedApplicationsAll(CreateSingleQuery("document_number", 10), false);
        operation.QueryGrantSummariesAll(CreateSingleQuery("patent_id", 10), true);
        operation.QueryPublicationSummariesAll(CreateSingleQuery("document_number", 10), false);
        operation.QueryGrantClaimsAll(CreateCompositeQuery("patent_id", "claim_sequence", 10), true);
        operation.QueryPublicationClaimsAll(CreateCompositeQuery("document_number", "claim_sequence", 10), false);
        operation.QueryGrantDescriptionsAll(CreateSingleQuery("patent_id", 10), true);
        operation.QueryPublicationDescriptionsAll(CreateSingleQuery("document_number", 10), false);
        operation.QueryGrantDrawingDescriptionsAll(CreateCompositeQuery("patent_id", "draw_desc_sequence", 10), true);
        PatentsViewDrawingDescriptionResponse final = operation.QueryPublicationDrawingDescriptionsAll(CreateCompositeQuery("document_number", "draw_desc_sequence", 10), false);

        Assert.Multiple(() =>
        {
            Assert.That(handler.CallCount, Is.EqualTo(27));
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/patent", "/custom/api/v1/publication", "/custom/api/v1/inventor", "/custom/api/v1/assignee",
                "/custom/api/v1/location", "/custom/api/v1/patent/attorney", "/custom/api/v1/cpc_class", "/custom/api/v1/cpc_subclass",
                "/custom/api/v1/cpc_group", "/custom/api/v1/ipc", "/custom/api/v1/uspc_mainclass", "/custom/api/v1/uspc_subclass",
                "/custom/api/v1/wipo", "/custom/api/v1/patent/us_patent_citation", "/custom/api/v1/patent/us_application_citation",
                "/custom/api/v1/patent/foreign_citation", "/custom/api/v1/patent/other_reference", "/custom/api/v1/patent/rel_app_text",
                "/custom/api/v1/publication/rel_app_text", "/custom/api/v1/g_brf_sum_text", "/custom/api/v1/pg_brf_sum_text",
                "/custom/api/v1/g_claim", "/custom/api/v1/pg_claim", "/custom/api/v1/g_detail_desc_text",
                "/custom/api/v1/pg_detail_desc_text", "/custom/api/v1/g_draw_desc_text", "/custom/api/v1/pg_draw_desc_text"
            }));
            Assert.That(final.DrawingDescriptions, Is.Empty);
            Assert.That(final.Metadata.Count, Is.Zero);
            Assert.That(final.Metadata.IsComplete, Is.True);
        });
    }

    [Test]
    public void PageBoundPreservesContinuationAndStableIdentitySortIsRequired()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PatentPage("100", "2024-01-03", null, null, 3, false));
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.Limits.MaximumPages = 1;
        PatentsViewAllPageOperation operation = new PatentsViewAllPageOperation(client, options);
        PatentsViewQueryRequest bounded = CreatePatentQuery(1);

        PatentsViewPatentResponse result = operation.QueryPatentsAll(bounded, false);
        PatentsViewQueryRequest unstable = CreateCompositeQuery("claim_sequence", "document_number", 2);
        ValidationException validation = Assert.Throws<ValidationException>(() => operation.QueryPublicationClaimsAll(unstable, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Patents, Has.Length.EqualTo(1));
            Assert.That(result.Metadata.IsComplete, Is.False);
            Assert.That(result.Metadata.HasNextCursorPage, Is.True);
            Assert.That(result.Metadata.NextCursorPage.AfterValues, Is.EqualTo(new[] { "2024-01-03", "100" }));
            Assert.That(validation!.ErrorCode, Is.EqualTo("DETERMINISTIC_SORT_REQUIRED"));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void MissingWireCursorFieldsAndMissingRecordIdentitiesFailClearly()
    {
        string missingCursorBody = "{\"error\":false,\"count\":1,\"total_hits\":2,\"patents\":[{\"patent_id\":\"100\"}]}";
        PatentsViewPatentHandler cursorHandler = new PatentsViewPatentHandler(missingCursorBody);
        using HttpClient cursorClient = new HttpClient(cursorHandler);
        PatentsViewAllPageOperation cursorOperation = new PatentsViewAllPageOperation(cursorClient, CreateOptions(cursorClient));
        PatentsViewQueryRequest cursorRequest = CreatePatentQuery(1);
        ValidationException cursor = Assert.Throws<ValidationException>(() => cursorOperation.QueryPatentsAll(cursorRequest, false));

        string missingIdentityBody = "{\"error\":false,\"count\":1,\"total_hits\":1,\"inventors\":[{\"inventor_name_first\":\"Ada\"}]}";
        PatentsViewPatentHandler identityHandler = new PatentsViewPatentHandler(missingIdentityBody);
        using HttpClient identityClient = new HttpClient(identityHandler);
        PatentsViewAllPageOperation identityOperation = new PatentsViewAllPageOperation(identityClient, CreateOptions(identityClient));
        ValidationException identity = Assert.Throws<ValidationException>(() => identityOperation.QueryInventorsAll(CreateSingleQuery("inventor_id", 10), false));

        Assert.Multiple(() =>
        {
            Assert.That(cursor!.ErrorCode, Is.EqualTo("MISSING_CURSOR_FIELDS"));
            Assert.That(identity!.ErrorCode, Is.EqualTo("MISSING_RECORD_IDENTIFIER"));
        });
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            PatentsViewBaseUri = new Uri("https://example.test/custom/"),
            PatentsViewApiKey = "all-page-secret"
        };
        options.Retry.AttemptCount = 1;
        return options;
    }

    private static PatentsViewQueryRequest CreatePatentQuery(int size)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new[] { new SearchFilter { Field = "patent_type", Operator = SearchOperator.Equal, Value = "utility" } };
        request.Search.FieldSelection.Fields = new[] { "patent_title" };
        request.Search.Sorts = new[]
        {
            new SearchSort { Field = "patent_date", Direction = SortDirection.Descending },
            new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
        };
        request.Search.CursorPage.Size = size;
        request.Search.MaximumPages = 10;
        request.Search.MaximumResults = 100;
        return request;
    }

    private static PatentsViewQueryRequest CreateSingleQuery(string identityField, int size)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new[] { new SearchFilter { Field = identityField, Operator = SearchOperator.GreaterThan, Value = "0" } };
        request.Search.FieldSelection.Fields = new[] { "selected_field" };
        request.Search.Sorts = new[] { new SearchSort { Field = identityField, Direction = SortDirection.Ascending } };
        request.Search.CursorPage.Size = size;
        request.Search.MaximumPages = 10;
        request.Search.MaximumResults = 100;
        return request;
    }

    private static PatentsViewQueryRequest CreateCompositeQuery(string firstIdentityField, string secondIdentityField, int size)
    {
        PatentsViewQueryRequest request = CreateSingleQuery(firstIdentityField, size);
        request.Search.Sorts = new[]
        {
            new SearchSort { Field = firstIdentityField, Direction = SortDirection.Ascending },
            new SearchSort { Field = secondIdentityField, Direction = SortDirection.Ascending }
        };
        return request;
    }

    private static string PatentPage(string firstIdentifier, string firstDate, string? secondIdentifier, string? secondDate, int total, bool futureField)
    {
        string records = "{\"patent_id\":\"" + firstIdentifier + "\",\"patent_date\":\"" + firstDate + "\",\"patent_title\":\"Patent " + firstIdentifier + "\"}";
        int count = 1;
        if (secondIdentifier != null)
        {
            records += ",{\"patent_id\":\"" + secondIdentifier + "\",\"patent_date\":\"" + secondDate + "\",\"patent_title\":\"Patent " + secondIdentifier + "\"}";
            count = 2;
        }

        string future = futureField ? ",\"future_response\":true" : string.Empty;
        return "{\"error\":false,\"count\":" + count + ",\"total_hits\":" + total + ",\"patents\":[" + records + "]" + future + "}";
    }

    private static string PublicationPage(string first, string second, int total)
    {
        return "{\"error\":false,\"count\":2,\"total_hits\":" + total + ",\"publications\":[{\"document_number\":\"" + first + "\",\"publication_title\":\"First\"},{\"document_number\":\"" + second + "\",\"publication_title\":\"Second\"}]}";
    }

    private static string PublicationClaimPage(int firstSequence, int secondSequence, int total)
    {
        string records = "{\"document_number\":\"US20240001234A1\",\"claim_sequence\":" + firstSequence + ",\"claim_text\":\"Claim " + firstSequence + "\"}";
        int count = 1;
        if (secondSequence >= 0)
        {
            records += ",{\"document_number\":\"US20240001234A1\",\"claim_sequence\":" + secondSequence + ",\"claim_text\":\"Claim " + secondSequence + "\"}";
            count = 2;
        }

        return "{\"error\":false,\"count\":" + count + ",\"total_hits\":" + total + ",\"pg_claims\":[" + records + "]}";
    }

    private static string EmptyPage(string collectionName)
    {
        return "{\"error\":false,\"count\":0,\"total_hits\":0,\"" + collectionName + "\":[]}";
    }
}
