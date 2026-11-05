using System.Net;
using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentsViewPatentOperationTests
{
    [Test]
    public void PatentGetWritesOptionsAndParsesTypedRecordMetadataAndCursor()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PatentBody(2, 3));
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        PatentsViewQueryRequest request = CreateQuery("patent_id", 2);
        request.PadPatentIdentifiers = true;
        request.ExcludeWithdrawnPatents = false;
        request.Search.Context.RetainRawResponse = true;
        request.Search.Context.MaximumRawResponseBytes = 10000L;
        PatentsViewPatentOperation operation = new PatentsViewPatentOperation(client, options);

        PatentsViewPatentResponse result = operation.QueryPatentsGet(request);
        string decoded = Uri.UnescapeDataString(handler.Uris.Single()!.Query);
        PatentsViewPatent first = result.Patents[0];

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods.Single(), Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.Uris.Single()!.AbsolutePath, Is.EqualTo("/custom/api/v1/patent"));
            Assert.That(decoded, Does.Contain("q={\"_eq\":{\"patent_type\":\"utility\"}}"));
            Assert.That(decoded, Does.Contain("f=[\"patent_id\",\"patent_title\",\"patent_date\"]"));
            Assert.That(decoded, Does.Contain("s=[{\"patent_id\":\"asc\"}]"));
            Assert.That(decoded, Does.Contain("o={\"size\":2,\"pad_patent_id\":true,\"exclude_withdrawn\":false}"));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("pv-secret"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(result.Patents, Has.Length.EqualTo(2));
            Assert.That(first.PatentIdentifier, Is.EqualTo("100"));
            Assert.That(first.Title, Is.EqualTo("First invention"));
            Assert.That(first.GrantDate, Is.EqualTo(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(first.HasGrantDate, Is.True);
            Assert.That(first.GrantYear, Is.EqualTo(2024));
            Assert.That(first.HasGrantYear, Is.True);
            Assert.That(first.IsWithdrawn, Is.False);
            Assert.That(first.HasIsWithdrawn, Is.True);
            Assert.That(first.Applications.Single().ApplicationIdentifier, Is.EqualTo("17123456"));
            Assert.That(first.Applications.Single().HasFilingDate, Is.True);
            Assert.That(first.Inventors.Single().InventorIdentifier, Is.EqualTo("inv-1"));
            Assert.That(first.Inventors.Single().LastKnownLocation!.Name, Is.EqualTo("Alexandria"));
            Assert.That(first.Inventors.Single().AdditionalFields["inventor_sequence"], Is.EqualTo("1"));
            Assert.That(first.GovernmentInterests.Single().Statement, Is.EqualTo("Government-supported research"));
            Assert.That(first.GovernmentInterests.Single().AwardNumbers, Is.EqualTo(new[] { "ABC-123" }));
            Assert.That(first.GovernmentInterests.Single().Organizations.Single().FederalAgencyName, Is.EqualTo("Example Agency"));
            Assert.That(first.CpcClassifications.Single().GroupIdentifier, Is.EqualTo("G06F3/01"));
            Assert.That(first.AdditionalFields["future_field"], Is.EqualTo("{\"value\":1}"));
            Assert.That(first.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(first.Provenance.DatasetIdentifier, Is.EqualTo("g_patent"));
            Assert.That(first.Provenance.IsAuthoritative, Is.False);
            Assert.That(result.Metadata.Count, Is.EqualTo(2));
            Assert.That(result.Metadata.TotalHits, Is.EqualTo(3));
            Assert.That(result.Metadata.HasTotalHits, Is.True);
            Assert.That(result.Metadata.HasNextCursorPage, Is.True);
            Assert.That(result.Metadata.NextCursorPage.AfterValues, Is.EqualTo(new[] { "101" }));
            Assert.That(result.Metadata.RequestIdentifier, Is.EqualTo("pv-request"));
            Assert.That(result.Metadata.RawResponse, Is.EqualTo(PatentBody(2, 3)));
            Assert.That(result.Metadata.IsComplete, Is.False);
            Assert.That(operation.RateLimiter.GetObservation(ApiService.PatentsView).Used, Is.EqualTo(1));
        });
    }

    [Test]
    public void PatentPostUsesJsonBodyAndMemoryCacheAvoidsSecondRequest()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PatentBody(1, 1));
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.Cache.Mode = CacheMode.Memory;
        options.Cache.LifetimeSeconds = 60;
        options.Cache.MaximumMemoryEntries = 4;
        options.Cache.MaximumEntryBytes = 10000L;
        PatentsViewQueryRequest request = CreateQuery("patent_id", 1);
        PatentsViewPatentOperation operation = new PatentsViewPatentOperation(client, options);

        PatentsViewPatentResponse first = operation.QueryPatentsPost(request);
        PatentsViewPatentResponse second = operation.QueryPatentsPost(request);

        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single()!);
        JsonElement root = body.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(handler.Methods.Single(), Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.ContentTypes.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(root.GetProperty("q").GetProperty("_eq").GetProperty("patent_type").GetString(), Is.EqualTo("utility"));
            Assert.That(root.GetProperty("o").GetProperty("size").GetInt32(), Is.EqualTo(1));
            Assert.That(root.GetProperty("o").GetProperty("pad_patent_id").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("o").GetProperty("exclude_withdrawn").GetBoolean(), Is.True);
            Assert.That(first.Metadata.Provenance.FromCache, Is.False);
            Assert.That(second.Metadata.Provenance.FromCache, Is.True);
            Assert.That(second.Patents.Single().PatentIdentifier, Is.EqualTo("100"));
            Assert.That(second.Metadata.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(second.Metadata.Provenance.FieldSources.Single().Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(second.Metadata.Provenance.FieldSources.Single().Delivery, Is.EqualTo(DataProvenance.Cache));
            Assert.That(second.Patents.Single().Provenance.FieldSources.Single().Delivery, Is.EqualTo(DataProvenance.Cache));
            Assert.That(second.Patents.Single().Provenance.FieldSources, Is.Not.SameAs(second.Metadata.Provenance.FieldSources));
            Assert.That(operation.RateLimiter.GetObservation(ApiService.PatentsView).Used, Is.EqualTo(1));
        });
    }

    [Test]
    public void PublicationGetAndPostUsePublicationOptionsAndParseAliases()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PublicationBody());
        handler.BodiesToReturn.Enqueue(PublicationBody());
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        PatentsViewQueryRequest request = CreateQuery("document_number", 10);
        PatentsViewPatentOperation operation = new PatentsViewPatentOperation(client, options);

        PatentsViewPublicationResponse get = operation.QueryPublicationsGet(request);
        PatentsViewPublicationResponse post = operation.QueryPublicationsPost(request);
        string decoded = Uri.UnescapeDataString(handler.Uris[0]!.Query);
        using JsonDocument postBody = JsonDocument.Parse(handler.Bodies[1]!);
        PatentsViewPublication publication = get.Publications.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris[0]!.AbsolutePath, Is.EqualTo("/custom/api/v1/publication"));
            Assert.That(decoded, Does.Contain("o={\"size\":10}"));
            Assert.That(decoded, Does.Not.Contain("pad_patent_id"));
            Assert.That(postBody.RootElement.GetProperty("o").EnumerateObject().Select(property => property.Name), Is.EqualTo(new[] { "size" }));
            Assert.That(publication.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(publication.Title, Is.EqualTo("Energy storage controller"));
            Assert.That(publication.PublicationYear, Is.EqualTo(2024));
            Assert.That(publication.HasPublicationYear, Is.True);
            Assert.That(publication.SeriesCode, Is.EqualTo("09"));
            Assert.That(publication.Rule47, Is.True);
            Assert.That(publication.HasRule47, Is.True);
            Assert.That(publication.Applicants.Single().FirstName, Is.EqualTo("Ada"));
            Assert.That(publication.Applicants.Single().Authority, Is.EqualTo("inventor"));
            Assert.That(publication.GrantCrosswalks.Single().PatentIdentifier, Is.EqualTo("12000000"));
            Assert.That(publication.GrantCrosswalks.Single().IsCurrentPatentIdentifier, Is.True);
            Assert.That(publication.Provenance.DatasetIdentifier, Is.EqualTo("pg_publication"));
            Assert.That(get.Metadata.IsComplete, Is.True);
            Assert.That(post.Publications.Single().DocumentNumber, Is.EqualTo("US20240001234A1"));
        });
    }

    [Test]
    public void ByIdentifierRoutesEscapeSegmentsAndRequireMatchingSingleRecords()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PatentBody(1, 1));
        handler.BodiesToReturn.Enqueue(PublicationBody("US2024/001234A1"));
        using HttpClient client = new HttpClient(handler);
        PatentsViewPatentOperation operation = new PatentsViewPatentOperation(client, CreateOptions(client));

        PatentsViewPatent patent = operation.GetPatent("100", new RequestContext());
        PatentsViewPublication publication = operation.GetPublication("US2024/001234A1", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris[0]!.AbsolutePath, Is.EqualTo("/custom/api/v1/patent/100"));
            Assert.That(handler.Uris[1]!.AbsolutePath, Is.EqualTo("/custom/api/v1/publication/US2024%2F001234A1"));
            Assert.That(patent.PatentIdentifier, Is.EqualTo("100"));
            Assert.That(publication.DocumentNumber, Is.EqualTo("US2024/001234A1"));
        });
    }

    [Test]
    public void EmptyAndMismatchedIdentifierResponsesFailClearly()
    {
        PatentsViewPatentHandler emptyHandler = new PatentsViewPatentHandler("{\"error\":false,\"count\":0,\"total_hits\":0,\"patents\":[]}");
        using HttpClient emptyClient = new HttpClient(emptyHandler);
        PatentsViewPatentOperation emptyOperation = new PatentsViewPatentOperation(emptyClient, CreateOptions(emptyClient));
        NotFoundException notFound = Assert.Throws<NotFoundException>(() => emptyOperation.GetPatent("999", new RequestContext()));

        PatentsViewPatentHandler mismatchHandler = new PatentsViewPatentHandler(PatentBody(1, 1));
        using HttpClient mismatchClient = new HttpClient(mismatchHandler);
        PatentsViewPatentOperation mismatchOperation = new PatentsViewPatentOperation(mismatchClient, CreateOptions(mismatchClient));
        ParseException mismatch = Assert.Throws<ParseException>(() => mismatchOperation.GetPatent("101", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(notFound!.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(notFound.ResourceIdentifier, Is.EqualTo("999"));
            Assert.That(mismatch!.DataPath, Is.EqualTo("$.patents"));
        });
    }

    [Test]
    public void InvalidResponseShapeAndBlankRequestsAreRejected()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler("{\"error\":false,\"count\":1,\"total_hits\":1,\"patents\":[]}");
        using HttpClient client = new HttpClient(handler);
        PatentsViewPatentOperation operation = new PatentsViewPatentOperation(client, CreateOptions(client));
        ParseException count = Assert.Throws<ParseException>(() => operation.QueryPatentsGet(CreateQuery("patent_id", 1)));
        ValidationException blank = Assert.Throws<ValidationException>(() => operation.GetPublication("   ", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(count!.DataPath, Is.EqualTo("$.count"));
            Assert.That(blank!.FieldName, Is.EqualTo("publicationIdentifier"));
        });
    }

    [Test]
    public void CompleteGetAndRetrievalUrisHonorConfiguredLimitBeforeSending()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(PatentBody(1, 1));
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.PatentsViewBaseUri = new Uri("https://example.test/a/long/configured/base/path/");
        PatentsViewPatentOperation operation = new PatentsViewPatentOperation(client, options);
        PatentsViewQueryRequest query = CreateQuery("patent_id", 1);
        using HttpRequestMessage completeQuery = operation.CreateQueryMessage("patent", query, false);
        options.Limits.MaximumUriCharacters = completeQuery.RequestUri!.OriginalString.Length - 1;

        ValidationException queryFailure = Assert.Throws<ValidationException>(() => operation.QueryPatentsGet(query))!;
        Uri completeRetrieval = RequestBuilder.BuildUri(options.PatentsViewBaseUri, new[] { "api", "v1", "publication", "US2024/001234A1" }, new Dictionary<string, string?>());
        options.Limits.MaximumUriCharacters = completeRetrieval.OriginalString.Length - 1;
        ValidationException retrievalFailure = Assert.Throws<ValidationException>(() => operation.GetPublication("US2024/001234A1", new RequestContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(queryFailure.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
            Assert.That(retrievalFailure.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            PatentsViewBaseUri = new Uri("https://example.test/custom/"),
            PatentsViewApiKey = "pv-secret"
        };
        options.Retry.AttemptCount = 1;
        return options;
    }

    private static PatentsViewQueryRequest CreateQuery(string stableField, int size)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new[]
        {
            new SearchFilter { Field = "patent_type", Operator = SearchOperator.Equal, Value = "utility" }
        };
        request.Search.FieldSelection.Fields = stableField == "patent_id"
            ? new[] { "patent_id", "patent_title", "patent_date" }
            : new[] { "document_number", "publication_title", "publication_date" };
        request.Search.Sorts = new[]
        {
            new SearchSort { Field = stableField, Direction = SortDirection.Ascending }
        };
        request.Search.CursorPage.Size = size;
        return request;
    }

    private static string PatentBody(int count, int total)
    {
        string records = "{\"patent_id\":\"100\",\"patent_title\":\"First invention\",\"patent_abstract\":\"Abstract\",\"patent_date\":\"2024-01-02\",\"patent_year\":2024,\"patent_type\":\"utility\",\"wipo_kind\":\"B2\",\"withdrawn\":false,\"patent_num_total_documents_cited\":7,\"application\":[{\"application_id\":\"17123456\",\"application_type\":\"utility\",\"filing_date\":\"2020-03-04\",\"filing_type\":\"regular\",\"series_code\":\"17\",\"rule_47_flag\":false}],\"inventors\":[{\"inventor_id\":\"inv-1\",\"inventor_name_first\":\"Grace\",\"inventor_name_last\":\"Hopper\",\"inventor_city\":\"Alexandria\",\"inventor_state\":\"VA\",\"inventor_country\":\"US\",\"inventor_location_id\":\"loc-1\",\"inventor_sequence\":1}],\"gov_interest_statement\":\"Government-supported research\",\"gov_interest_contract_award_numbers\":[{\"award_number\":\"ABC-123\"}],\"gov_interest_organizations\":[{\"fedagency_name\":\"Example Agency\",\"level_one\":\"Department\"}],\"cpc_current\":[{\"cpc_section\":\"G\",\"cpc_class\":\"G06\",\"cpc_group_id\":\"G06F3/01\",\"cpc_sequence\":1}],\"future_field\":{\"value\":1}}";
        if (count == 2)
        {
            records += ",{\"patent_id\":\"101\",\"patent_title\":\"Second invention\",\"patent_date\":\"2024-01-03\"}";
        }

        return "{\"error\":false,\"count\":" + count + ",\"total_hits\":" + total + ",\"patents\":[" + records + "]}";
    }

    private static string PublicationBody(string documentNumber = "US20240001234A1")
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"publications\":[{\"document_number\":\"" + documentNumber + "\",\"publication_title\":\"Energy storage controller\",\"publication_abstract\":\"Abstract\",\"publication_date\":\"2024-01-04\",\"publication_year\":2024,\"publication_type\":\"utility\",\"series_code\":9,\"rule_47_flag\":true,\"us_parties\":[{\"us_party_name_first\":\"Ada\",\"us_party_name_last\":\"Lovelace\",\"us_party_type\":\"person\",\"applicant_authority\":\"inventor\",\"us_party_sequence\":1}],\"granted_pregrant_crosswalk\":[{\"patent_id\":12000000,\"application_number\":\"18123456\",\"current_patent_id_flag\":true}]}]}";
    }
}

internal class PatentsViewPatentHandler : HttpMessageHandler
{
    internal Queue<string> BodiesToReturn = new Queue<string>();
    internal List<HttpMethod> Methods = new List<HttpMethod>();
    internal List<Uri?> Uris = new List<Uri?>();
    internal List<string?> Bodies = new List<string?>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> ContentTypes = new List<string?>();
    internal int CallCount = 0;

    internal PatentsViewPatentHandler(string body)
    {
        BodiesToReturn.Enqueue(body);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount += 1;
        Methods.Add(request.Method);
        Uris.Add(request.RequestUri);
        Bodies.Add(request.Content == null ? null : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult());
        ApiKeys.Add(request.Headers.Contains(UsptoHeaders.PatentsViewApiKey) ? request.Headers.GetValues(UsptoHeaders.PatentsViewApiKey).Single() : null);
        Accepts.Add(request.Headers.Accept.SingleOrDefault()?.MediaType);
        ContentTypes.Add(request.Content?.Headers.ContentType?.MediaType);
        string body = BodiesToReturn.Count == 1 ? BodiesToReturn.Peek() : BodiesToReturn.Dequeue();
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "pv-request");
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { MaxAge = TimeSpan.FromSeconds(60) };
        return Task.FromResult(response);
    }
}
