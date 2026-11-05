using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentsViewEntityOperationTests
{
    [Test]
    public void InventorGetParsesIdentityLocationDatesCountsActivityAndCursor()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(InventorBody(1, 2));
        using HttpClient client = new HttpClient(handler);
        PatentsViewEntityOperation operation = new PatentsViewEntityOperation(client, CreateOptions(client));
        PatentsViewQueryRequest request = CreateQuery("inventor_id", 1);

        PatentsViewInventorResponse result = operation.QueryInventorsGet(request);
        PatentsViewInventor inventor = result.Inventors.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods.Single(), Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.Uris.Single()!.AbsolutePath, Is.EqualTo("/custom/api/v1/inventor"));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("entity-secret"));
            Assert.That(inventor.InventorIdentifier, Is.EqualTo("inv-1"));
            Assert.That(inventor.ResourceReference, Is.EqualTo("/api/v1/inventor/inv-1/"));
            Assert.That(inventor.FirstName, Is.EqualTo("Grace"));
            Assert.That(inventor.GenderCode, Is.EqualTo("F"));
            Assert.That(inventor.LastKnownLocation!.LocationIdentifier, Is.EqualTo("loc-1"));
            Assert.That(inventor.LastKnownLocation.Latitude, Is.EqualTo(38.8048).Within(0.00001));
            Assert.That(inventor.LastKnownLocation.HasLatitude, Is.True);
            Assert.That(inventor.LastKnownLocation.Longitude, Is.EqualTo(-77.0469).Within(0.00001));
            Assert.That(inventor.HasFirstSeenDate, Is.True);
            Assert.That(inventor.FirstSeenDate, Is.EqualTo(new DateTimeOffset(1990, 1, 2, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(inventor.AssigneeCount, Is.EqualTo(4));
            Assert.That(inventor.HasAssigneeCount, Is.True);
            Assert.That(inventor.PatentCount, Is.EqualTo(12));
            Assert.That(inventor.YearsActive, Is.EqualTo(8));
            Assert.That(inventor.ActivityYears.Single().Year, Is.EqualTo(2024));
            Assert.That(inventor.ActivityYears.Single().PatentCount, Is.EqualTo(2));
            Assert.That(inventor.AdditionalFields["future_rank"], Is.EqualTo("7"));
            Assert.That(inventor.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(inventor.Provenance.DatasetIdentifier, Is.EqualTo("g_inventor_disambiguated"));
            Assert.That(inventor.Provenance.IsAuthoritative, Is.False);
            Assert.That(result.Metadata.HasNextCursorPage, Is.True);
            Assert.That(result.Metadata.NextCursorPage.AfterValues, Is.EqualTo(new[] { "inv-1" }));
        });
    }

    [Test]
    public void AssigneePostUsesGenericOptionsAndParsesOrganizationActivity()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(AssigneeBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewEntityOperation operation = new PatentsViewEntityOperation(client, CreateOptions(client));
        PatentsViewQueryRequest request = CreateQuery("assignee_id", 25);

        PatentsViewAssigneeResponse result = operation.QueryAssigneesPost(request);
        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single()!);
        PatentsViewAssignee assignee = result.Assignees.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods.Single(), Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Uris.Single()!.AbsolutePath, Is.EqualTo("/custom/api/v1/assignee"));
            Assert.That(handler.ContentTypes.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(body.RootElement.GetProperty("o").GetProperty("size").GetInt32(), Is.EqualTo(25));
            Assert.That(body.RootElement.GetProperty("o").TryGetProperty("pad_patent_id", out _), Is.False);
            Assert.That(assignee.AssigneeIdentifier, Is.EqualTo("asg-1"));
            Assert.That(assignee.Organization, Is.EqualTo("Analytical Engines, Inc."));
            Assert.That(assignee.AssigneeType, Is.EqualTo("organization"));
            Assert.That(assignee.LastKnownLocation!.Name, Is.EqualTo("Boston"));
            Assert.That(assignee.InventorCount, Is.EqualTo(9));
            Assert.That(assignee.PatentCount, Is.EqualTo(20));
            Assert.That(assignee.YearsActive, Is.EqualTo(11));
            Assert.That(assignee.ActivityYears.Single().HasYear, Is.True);
            Assert.That(result.Metadata.Provenance.DatasetIdentifier, Is.EqualTo("g_assignee_disambiguated"));
            Assert.That(result.Metadata.IsComplete, Is.True);
        });
    }

    [Test]
    public void LocationGetParsesIdentifiersCoordinatesAndCounts()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(LocationBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewEntityOperation operation = new PatentsViewEntityOperation(client, CreateOptions(client));

        PatentsViewLocationResponse result = operation.QueryLocationsGet(CreateQuery("location_id", 10));
        PatentsViewLocation location = result.Locations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Single()!.AbsolutePath, Is.EqualTo("/custom/api/v1/location"));
            Assert.That(location.LocationIdentifier, Is.EqualTo("loc-1"));
            Assert.That(location.Name, Is.EqualTo("Alexandria"));
            Assert.That(location.County, Is.EqualTo("Alexandria city"));
            Assert.That(location.CountyFipsCode, Is.EqualTo("510"));
            Assert.That(location.StateFipsCode, Is.EqualTo("51"));
            Assert.That(location.Country, Is.EqualTo("US"));
            Assert.That(location.PlaceType, Is.EqualTo("city"));
            Assert.That(location.Latitude, Is.EqualTo(38.8048).Within(0.00001));
            Assert.That(location.Longitude, Is.EqualTo(-77.0469).Within(0.00001));
            Assert.That(location.AssigneeCount, Is.EqualTo(3));
            Assert.That(location.PatentCount, Is.EqualTo(30));
            Assert.That(location.InventorCount, Is.EqualTo(14));
            Assert.That(location.Provenance.DatasetIdentifier, Is.EqualTo("g_location_disambiguated"));
        });
    }

    [Test]
    public void AttorneyGetAndPostUseNestedPatentRouteAndParseMetrics()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(AttorneyBody());
        handler.BodiesToReturn.Enqueue(AttorneyBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewEntityOperation operation = new PatentsViewEntityOperation(client, CreateOptions(client));
        PatentsViewQueryRequest request = CreateQuery("attorney_id", 10);

        PatentsViewAttorneyResponse get = operation.QueryAttorneysGet(request);
        PatentsViewAttorneyResponse post = operation.QueryAttorneysPost(request);
        PatentsViewAttorney attorney = get.Attorneys.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[] { "/custom/api/v1/patent/attorney", "/custom/api/v1/patent/attorney" }));
            Assert.That(handler.Methods, Is.EqualTo(new[] { HttpMethod.Get, HttpMethod.Post }));
            Assert.That(attorney.AttorneyIdentifier, Is.EqualTo("aty-1"));
            Assert.That(attorney.FirstName, Is.EqualTo("Katherine"));
            Assert.That(attorney.Organization, Is.EqualTo("Example LLP"));
            Assert.That(attorney.HasFirstSeenDate, Is.True);
            Assert.That(attorney.HasLastSeenDate, Is.True);
            Assert.That(attorney.InventorCount, Is.EqualTo(18));
            Assert.That(attorney.PatentCount, Is.EqualTo(42));
            Assert.That(attorney.YearsActive, Is.EqualTo(15));
            Assert.That(attorney.Provenance.DatasetIdentifier, Is.EqualTo("g_attorney"));
            Assert.That(post.Attorneys.Single().AttorneyIdentifier, Is.EqualTo("aty-1"));
        });
    }

    [Test]
    public void AlternateQueryVerbsPreserveAuthenticationRawDataAndProvenance()
    {
        string inventorBody = InventorBody(1, 1);
        string assigneeBody = AssigneeBody();
        string locationBody = LocationBody();
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(inventorBody);
        handler.BodiesToReturn.Enqueue(assigneeBody);
        handler.BodiesToReturn.Enqueue(locationBody);
        using HttpClient client = new HttpClient(handler);
        PatentsViewEntityOperation operation = new PatentsViewEntityOperation(client, CreateOptions(client));
        PatentsViewQueryRequest inventorRequest = CreateQuery("inventor_id", 1000);
        inventorRequest.Search.Context.RetainRawResponse = true;
        PatentsViewQueryRequest assigneeRequest = CreateQuery("assignee_id", 1000);
        assigneeRequest.Search.Context.RetainRawResponse = true;
        PatentsViewQueryRequest locationRequest = CreateQuery("location_id", 1000);
        locationRequest.Search.Context.RetainRawResponse = true;

        PatentsViewInventorResponse inventors = operation.QueryInventorsPost(inventorRequest);
        PatentsViewAssigneeResponse assignees = operation.QueryAssigneesGet(assigneeRequest);
        PatentsViewLocationResponse locations = operation.QueryLocationsPost(locationRequest);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Is.EqualTo(new[] { HttpMethod.Post, HttpMethod.Get, HttpMethod.Post }));
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/inventor", "/custom/api/v1/assignee", "/custom/api/v1/location"
            }));
            Assert.That(handler.ApiKeys, Is.EqualTo(new[] { "entity-secret", "entity-secret", "entity-secret" }));
            Assert.That(handler.Accepts, Is.EqualTo(new[] { UsptoMediaTypes.Json, UsptoMediaTypes.Json, UsptoMediaTypes.Json }));
            Assert.That(inventors.Metadata.RawResponse, Is.EqualTo(inventorBody));
            Assert.That(assignees.Metadata.RawResponse, Is.EqualTo(assigneeBody));
            Assert.That(locations.Metadata.RawResponse, Is.EqualTo(locationBody));
            Assert.That(inventors.Metadata.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(assignees.Metadata.Provenance.IsAuthoritative, Is.False);
            Assert.That(locations.Locations.Single().Provenance.DatasetIdentifier, Is.EqualTo("g_location_disambiguated"));
        });

        using JsonDocument inventorPost = JsonDocument.Parse(handler.Bodies[0]!);
        using JsonDocument locationPost = JsonDocument.Parse(handler.Bodies[2]!);
        Assert.Multiple(() =>
        {
            Assert.That(inventorPost.RootElement.GetProperty("f")[0].GetString(), Is.EqualTo("inventor_id"));
            Assert.That(inventorPost.RootElement.GetProperty("o").GetProperty("size").GetInt32(), Is.EqualTo(1000));
            Assert.That(locationPost.RootElement.GetProperty("f")[0].GetString(), Is.EqualTo("location_id"));
            Assert.That(locationPost.RootElement.GetProperty("o").GetProperty("size").GetInt32(), Is.EqualTo(1000));
        });
    }

    [Test]
    public void ByIdentifierRetrievalUsesEveryDocumentedEntityRoute()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(InventorBody(1, 1));
        handler.BodiesToReturn.Enqueue(AssigneeBody());
        handler.BodiesToReturn.Enqueue(LocationBody());
        handler.BodiesToReturn.Enqueue(AttorneyBody("aty/1"));
        using HttpClient client = new HttpClient(handler);
        PatentsViewEntityOperation operation = new PatentsViewEntityOperation(client, CreateOptions(client));

        PatentsViewInventor inventor = operation.GetInventor("inv-1", new RequestContext());
        PatentsViewAssignee assignee = operation.GetAssignee("asg-1", new RequestContext());
        PatentsViewLocation location = operation.GetLocation("loc-1", new RequestContext());
        PatentsViewAttorney attorney = operation.GetAttorney("aty/1", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/inventor/inv-1",
                "/custom/api/v1/assignee/asg-1",
                "/custom/api/v1/location/loc-1",
                "/custom/api/v1/patent/attorney/aty%2F1"
            }));
            Assert.That(inventor.InventorIdentifier, Is.EqualTo("inv-1"));
            Assert.That(assignee.AssigneeIdentifier, Is.EqualTo("asg-1"));
            Assert.That(location.LocationIdentifier, Is.EqualTo("loc-1"));
            Assert.That(attorney.AttorneyIdentifier, Is.EqualTo("aty/1"));
        });
    }

    [Test]
    public void EmptyMismatchedAndInvalidEntityValuesFailClearly()
    {
        PatentsViewPatentHandler emptyHandler = new PatentsViewPatentHandler("{\"error\":false,\"count\":0,\"total_hits\":0,\"inventors\":[]}");
        using HttpClient emptyClient = new HttpClient(emptyHandler);
        PatentsViewEntityOperation emptyOperation = new PatentsViewEntityOperation(emptyClient, CreateOptions(emptyClient));
        NotFoundException missing = Assert.Throws<NotFoundException>(() => emptyOperation.GetInventor("missing", new RequestContext()));

        PatentsViewPatentHandler mismatchHandler = new PatentsViewPatentHandler(AssigneeBody());
        using HttpClient mismatchClient = new HttpClient(mismatchHandler);
        PatentsViewEntityOperation mismatchOperation = new PatentsViewEntityOperation(mismatchClient, CreateOptions(mismatchClient));
        ParseException mismatch = Assert.Throws<ParseException>(() => mismatchOperation.GetAssignee("other", new RequestContext()));

        string invalidBody = "{\"error\":false,\"count\":1,\"total_hits\":1,\"locations\":[{\"location_id\":\"bad\",\"location_latitude\":\"91.0\"}]}";
        PatentsViewPatentHandler invalidHandler = new PatentsViewPatentHandler(invalidBody);
        using HttpClient invalidClient = new HttpClient(invalidHandler);
        PatentsViewEntityOperation invalidOperation = new PatentsViewEntityOperation(invalidClient, CreateOptions(invalidClient));
        ParseException coordinate = Assert.Throws<ParseException>(() => invalidOperation.QueryLocationsGet(CreateQuery("location_id", 10)));

        Assert.Multiple(() =>
        {
            Assert.That(missing!.ResourceIdentifier, Is.EqualTo("missing"));
            Assert.That(missing.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(mismatch!.DataPath, Is.EqualTo("$.assignees"));
            Assert.That(coordinate!.DataPath, Is.EqualTo("$.locations[0].location_latitude"));
        });
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            PatentsViewBaseUri = new Uri("https://example.test/custom/"),
            PatentsViewApiKey = "entity-secret"
        };
        options.Retry.AttemptCount = 1;
        return options;
    }

    private static PatentsViewQueryRequest CreateQuery(string field, int size)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new[] { new SearchFilter { Field = field, Operator = SearchOperator.GreaterThan, Value = "0" } };
        request.Search.FieldSelection.Fields = new[] { field };
        request.Search.Sorts = new[] { new SearchSort { Field = field } };
        request.Search.CursorPage.Size = size;
        return request;
    }

    private static string InventorBody(int count, int total)
    {
        return "{\"error\":false,\"count\":" + count + ",\"total_hits\":" + total + ",\"inventors\":[{\"inventor_id\":\"inv-1\",\"inventor\":\"/api/v1/inventor/inv-1/\",\"inventor_name_first\":\"Grace\",\"inventor_name_last\":\"Hopper\",\"inventor_gender_code\":\"F\",\"inventor_lastknown_city\":\"Alexandria\",\"inventor_lastknown_state\":\"VA\",\"inventor_lastknown_country\":\"US\",\"inventor_lastknown_latitude\":\"38.8048\",\"inventor_lastknown_longitude\":\"-77.0469\",\"inventor_lastknown_location\":\"loc-1\",\"inventor_first_seen_date\":\"1990-01-02\",\"inventor_last_seen_date\":\"2024-05-06\",\"inventor_num_assignees\":4,\"inventor_num_patents\":12,\"inventor_years_active\":8,\"inventor_years\":[{\"year\":2024,\"num_patents\":2}],\"future_rank\":7}]}";
    }

    private static string AssigneeBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"assignees\":[{\"assignee_id\":\"asg-1\",\"assignee_organization\":\"Analytical Engines, Inc.\",\"assignee_type\":\"organization\",\"assignee_lastknown_city\":\"Boston\",\"assignee_lastknown_state\":\"MA\",\"assignee_lastknown_country\":\"US\",\"assignee_num_inventors\":9,\"assignee_num_patents\":20,\"assignee_years_active\":11,\"assignee_years\":[{\"year\":2023,\"num_patents\":3}]}]}";
    }

    private static string LocationBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"locations\":[{\"location_id\":\"loc-1\",\"location_name\":\"Alexandria\",\"location_county\":\"Alexandria city\",\"location_county_fips\":\"510\",\"location_state\":\"VA\",\"location_state_fips\":\"51\",\"location_country\":\"US\",\"location_place_type\":\"city\",\"location_latitude\":38.8048,\"location_longitude\":\"-77.0469\",\"location_num_assignees\":3,\"location_num_patents\":30,\"location_num_inventors\":14}]}";
    }

    private static string AttorneyBody(string identifier = "aty-1")
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"attorneys\":[{\"attorney_id\":\"" + identifier + "\",\"attorney_name_first\":\"Katherine\",\"attorney_name_last\":\"Johnson\",\"attorney_organization\":\"Example LLP\",\"attorney_first_seen_date\":\"2009-02-03\",\"attorney_last_seen_date\":\"2024-04-05\",\"attorney_num_inventors\":18,\"attorney_num_patents\":42,\"attorney_years_active\":15}]}";
    }
}
