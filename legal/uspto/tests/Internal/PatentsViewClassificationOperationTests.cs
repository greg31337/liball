using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentsViewClassificationOperationTests
{
    [Test]
    public void CpcClassGetAndSubclassPostParseLevelsMetricsAndMetadata()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(CpcClassBody(2));
        handler.BodiesToReturn.Enqueue(CpcSubclassBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewClassificationOperation operation = new PatentsViewClassificationOperation(client, CreateOptions(client));

        PatentsViewCpcClassificationResponse classes = operation.QueryCpcClassesGet(CreateQuery("cpc_class_id", 1));
        PatentsViewCpcClassificationResponse subclasses = operation.QueryCpcSubclassesPost(CreateQuery("cpc_subclass_id", 10));
        PatentsViewCpcClassification classification = classes.Classifications.Single();
        PatentsViewCpcClassification subclass = subclasses.Classifications.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[] { "/custom/api/v1/cpc_class", "/custom/api/v1/cpc_subclass" }));
            Assert.That(handler.Methods, Is.EqualTo(new[] { HttpMethod.Get, HttpMethod.Post }));
            Assert.That(classification.ClassificationIdentifier, Is.EqualTo("G06"));
            Assert.That(classification.Level, Is.EqualTo("class"));
            Assert.That(classification.Class, Is.EqualTo("G06"));
            Assert.That(classification.Title, Is.EqualTo("Computing; calculating or counting"));
            Assert.That(classification.HasFirstSeenDate, Is.True);
            Assert.That(classification.LastSeenDate, Is.EqualTo(new DateTimeOffset(2024, 5, 7, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(classification.YearsActive, Is.EqualTo(49));
            Assert.That(classification.PatentCount, Is.EqualTo(150000));
            Assert.That(classification.AssigneeCount, Is.EqualTo(12000));
            Assert.That(classification.InventorCount, Is.EqualTo(45000));
            Assert.That(classification.AdditionalFields["future_note"], Is.EqualTo("\"retained\""));
            Assert.That(classification.Provenance.DatasetIdentifier, Is.EqualTo("cpc_classes"));
            Assert.That(classification.Provenance.IsAuthoritative, Is.False);
            Assert.That(classes.Metadata.HasNextCursorPage, Is.True);
            Assert.That(classes.Metadata.NextCursorPage.AfterValues, Is.EqualTo(new[] { "G06" }));
            Assert.That(subclass.ClassificationIdentifier, Is.EqualTo("G06F"));
            Assert.That(subclass.Level, Is.EqualTo("subclass"));
            Assert.That(subclass.ClassIdentifier, Is.EqualTo("G06"));
            Assert.That(subclass.Subclass, Is.EqualTo("G06F"));
            Assert.That(subclass.Title, Is.EqualTo("Electric digital data processing"));
        });
    }

    [Test]
    public void CpcGroupQueriesAndLookupUseDocumentedColonPathSubstitution()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(CpcGroupBody());
        handler.BodiesToReturn.Enqueue(CpcGroupBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewClassificationOperation operation = new PatentsViewClassificationOperation(client, CreateOptions(client));

        PatentsViewCpcClassificationResponse query = operation.QueryCpcGroupsPost(CreateQuery("cpc_group_id", 10));
        PatentsViewCpcClassification retrieved = operation.GetCpcGroup("A61K2039/892", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris[0]!.AbsolutePath, Is.EqualTo("/custom/api/v1/cpc_group"));
            Assert.That(handler.Uris[1]!.AbsolutePath, Is.EqualTo("/custom/api/v1/cpc_group/A61K2039%3A892"));
            Assert.That(query.Classifications.Single().Level, Is.EqualTo("group"));
            Assert.That(query.Classifications.Single().ClassificationIdentifier, Is.EqualTo("A61K2039/892"));
            Assert.That(query.Classifications.Single().ClassIdentifier, Is.EqualTo("A61"));
            Assert.That(query.Classifications.Single().SubclassIdentifier, Is.EqualTo("A61K"));
            Assert.That(retrieved.GroupIdentifier, Is.EqualTo("A61K2039/892"));
        });
    }

    [Test]
    public void IpcGetPostAndByIdentifierUseTheQueryEndpoint()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(IpcBody());
        handler.BodiesToReturn.Enqueue(IpcBody());
        handler.BodiesToReturn.Enqueue(IpcBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewClassificationOperation operation = new PatentsViewClassificationOperation(client, CreateOptions(client));

        PatentsViewIpcClassificationResponse get = operation.QueryIpcGet(CreateQuery("ipc_id", 10));
        PatentsViewIpcClassificationResponse post = operation.QueryIpcPost(CreateQuery("ipc_id", 10));
        PatentsViewIpcClassification retrieved = operation.GetIpc("ipc-1", new RequestContext());
        string lookupQuery = Uri.UnescapeDataString(handler.Uris[2]!.Query);
        PatentsViewIpcClassification classification = get.Classifications.Single();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[] { "/custom/api/v1/ipc", "/custom/api/v1/ipc", "/custom/api/v1/ipc" }));
            Assert.That(handler.Methods, Is.EqualTo(new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Get }));
            Assert.That(lookupQuery, Does.Contain("q={\"_eq\":{\"ipc_id\":\"ipc-1\"}}"));
            Assert.That(lookupQuery, Does.Contain("o={\"size\":2}"));
            Assert.That(classification.ClassificationIdentifier, Is.EqualTo("ipc-1"));
            Assert.That(classification.Section, Is.EqualTo("G"));
            Assert.That(classification.Class, Is.EqualTo("G06"));
            Assert.That(classification.Subclass, Is.EqualTo("G06F"));
            Assert.That(classification.MainGroup, Is.EqualTo("3"));
            Assert.That(classification.Subgroup, Is.EqualTo("01"));
            Assert.That(classification.HasActionDate, Is.True);
            Assert.That(classification.Sequence, Is.EqualTo(2));
            Assert.That(classification.HasSequence, Is.True);
            Assert.That(classification.AdditionalFields["future_ipc"], Is.EqualTo("true"));
            Assert.That(post.Classifications.Single().ClassificationIdentifier, Is.EqualTo("ipc-1"));
            Assert.That(retrieved.ClassificationIdentifier, Is.EqualTo("ipc-1"));
        });
    }

    [Test]
    public void UspcMainAndSubclassQueriesAndLookupsMapDistinctLevels()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(UspcMainBody());
        handler.BodiesToReturn.Enqueue(UspcSubclassBody());
        handler.BodiesToReturn.Enqueue(UspcMainBody());
        handler.BodiesToReturn.Enqueue(UspcSubclassBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewClassificationOperation operation = new PatentsViewClassificationOperation(client, CreateOptions(client));

        PatentsViewUspcClassificationResponse mainQuery = operation.QueryUspcMainClassesGet(CreateQuery("uspc_mainclass_id", 10));
        PatentsViewUspcClassificationResponse subclassQuery = operation.QueryUspcSubclassesPost(CreateQuery("uspc_subclass_id", 10));
        PatentsViewUspcClassification main = operation.GetUspcMainClass("427", new RequestContext());
        PatentsViewUspcClassification subclass = operation.GetUspcSubclass("427/286", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/uspc_mainclass",
                "/custom/api/v1/uspc_subclass",
                "/custom/api/v1/uspc_mainclass/427",
                "/custom/api/v1/uspc_subclass/427%3A286"
            }));
            Assert.That(mainQuery.Classifications.Single().Level, Is.EqualTo("mainclass"));
            Assert.That(mainQuery.Classifications.Single().Title, Is.EqualTo("Coating processes"));
            Assert.That(mainQuery.Classifications.Single().HasFirstSeenDate, Is.True);
            Assert.That(mainQuery.Classifications.Single().PatentCount, Is.EqualTo(9000));
            Assert.That(subclassQuery.Classifications.Single().Level, Is.EqualTo("subclass"));
            Assert.That(subclassQuery.Classifications.Single().MainClassIdentifier, Is.EqualTo("427"));
            Assert.That(main.ClassificationIdentifier, Is.EqualTo("427"));
            Assert.That(subclass.ClassificationIdentifier, Is.EqualTo("427/286"));
        });
    }

    [Test]
    public void WipoGetPostAndByIdentifierUseTypedResearchResults()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(WipoBody());
        handler.BodiesToReturn.Enqueue(WipoBody());
        handler.BodiesToReturn.Enqueue(WipoBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewClassificationOperation operation = new PatentsViewClassificationOperation(client, CreateOptions(client));

        PatentsViewWipoClassificationResponse get = operation.QueryWipoGet(CreateQuery("wipo_id", 10));
        PatentsViewWipoClassificationResponse post = operation.QueryWipoPost(CreateQuery("wipo_id", 10));
        PatentsViewWipoClassification retrieved = operation.GetWipo("wipo-1", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[] { "/custom/api/v1/wipo", "/custom/api/v1/wipo", "/custom/api/v1/wipo/wipo-1" }));
            Assert.That(get.Classifications.Single().ClassificationIdentifier, Is.EqualTo("wipo-1"));
            Assert.That(get.Classifications.Single().SectorTitle, Is.EqualTo("Electrical engineering"));
            Assert.That(get.Classifications.Single().FieldTitle, Is.EqualTo("Computer technology"));
            Assert.That(get.Classifications.Single().AdditionalFields["future_wipo"], Is.EqualTo("{\"rank\":1}"));
            Assert.That(get.Metadata.Provenance.DatasetIdentifier, Is.EqualTo("wipo"));
            Assert.That(post.Classifications.Single().ClassificationIdentifier, Is.EqualTo("wipo-1"));
            Assert.That(retrieved.ClassificationIdentifier, Is.EqualTo("wipo-1"));
        });
    }

    [Test]
    public void AlternateClassificationVerbsAndRemainingCpcIdentifierRoutesAreCovered()
    {
        PatentsViewPatentHandler handler = new PatentsViewPatentHandler(CpcClassBody(1));
        handler.BodiesToReturn.Enqueue(CpcSubclassBody());
        handler.BodiesToReturn.Enqueue(UspcMainBody());
        handler.BodiesToReturn.Enqueue(UspcSubclassBody());
        handler.BodiesToReturn.Enqueue(CpcClassBody(1));
        handler.BodiesToReturn.Enqueue(CpcSubclassBody());
        using HttpClient client = new HttpClient(handler);
        PatentsViewClassificationOperation operation = new PatentsViewClassificationOperation(client, CreateOptions(client));

        PatentsViewCpcClassification cpcClassQuery = operation.QueryCpcClassesPost(CreateQuery("cpc_class_id", 1000)).Classifications.Single();
        PatentsViewCpcClassification cpcSubclassQuery = operation.QueryCpcSubclassesGet(CreateQuery("cpc_subclass_id", 1000)).Classifications.Single();
        PatentsViewUspcClassification uspcMainQuery = operation.QueryUspcMainClassesPost(CreateQuery("uspc_mainclass_id", 1000)).Classifications.Single();
        PatentsViewUspcClassification uspcSubclassQuery = operation.QueryUspcSubclassesGet(CreateQuery("uspc_subclass_id", 1000)).Classifications.Single();
        PatentsViewCpcClassification cpcClass = operation.GetCpcClass("G06", new RequestContext());
        PatentsViewCpcClassification cpcSubclass = operation.GetCpcSubclass("G06F", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Is.EqualTo(new[]
            {
                HttpMethod.Post, HttpMethod.Get, HttpMethod.Post, HttpMethod.Get, HttpMethod.Get, HttpMethod.Get
            }));
            Assert.That(handler.Uris.Select(uri => uri!.AbsolutePath), Is.EqualTo(new[]
            {
                "/custom/api/v1/cpc_class", "/custom/api/v1/cpc_subclass", "/custom/api/v1/uspc_mainclass",
                "/custom/api/v1/uspc_subclass", "/custom/api/v1/cpc_class/G06", "/custom/api/v1/cpc_subclass/G06F"
            }));
            Assert.That(handler.ApiKeys, Is.All.EqualTo("classification-secret"));
            Assert.That(cpcClassQuery.ClassificationIdentifier, Is.EqualTo("G06"));
            Assert.That(cpcSubclassQuery.ClassificationIdentifier, Is.EqualTo("G06F"));
            Assert.That(uspcMainQuery.ClassificationIdentifier, Is.EqualTo("427"));
            Assert.That(uspcSubclassQuery.ClassificationIdentifier, Is.EqualTo("427/286"));
            Assert.That(cpcClass.ClassificationIdentifier, Is.EqualTo("G06"));
            Assert.That(cpcSubclass.ClassificationIdentifier, Is.EqualTo("G06F"));
        });

        using JsonDocument classPost = JsonDocument.Parse(handler.Bodies[0]!);
        using JsonDocument mainPost = JsonDocument.Parse(handler.Bodies[2]!);
        Assert.Multiple(() =>
        {
            Assert.That(classPost.RootElement.GetProperty("o").GetProperty("size").GetInt32(), Is.EqualTo(1000));
            Assert.That(mainPost.RootElement.GetProperty("o").GetProperty("size").GetInt32(), Is.EqualTo(1000));
        });
    }

    [Test]
    public void EmptyMismatchedAndNegativeClassificationValuesFailClearly()
    {
        PatentsViewPatentHandler emptyHandler = new PatentsViewPatentHandler("{\"error\":false,\"count\":0,\"total_hits\":0,\"cpc_classes\":[]}");
        using HttpClient emptyClient = new HttpClient(emptyHandler);
        PatentsViewClassificationOperation emptyOperation = new PatentsViewClassificationOperation(emptyClient, CreateOptions(emptyClient));
        NotFoundException missing = Assert.Throws<NotFoundException>(() => emptyOperation.GetCpcClass("A01", new RequestContext()));

        PatentsViewPatentHandler mismatchHandler = new PatentsViewPatentHandler(WipoBody());
        using HttpClient mismatchClient = new HttpClient(mismatchHandler);
        PatentsViewClassificationOperation mismatchOperation = new PatentsViewClassificationOperation(mismatchClient, CreateOptions(mismatchClient));
        ParseException mismatch = Assert.Throws<ParseException>(() => mismatchOperation.GetWipo("other", new RequestContext()));

        string negativeBody = "{\"error\":false,\"count\":1,\"total_hits\":1,\"cpc_classes\":[{\"cpc_class_id\":\"G06\",\"cpc_class_num_patents\":-1}]}";
        PatentsViewPatentHandler negativeHandler = new PatentsViewPatentHandler(negativeBody);
        using HttpClient negativeClient = new HttpClient(negativeHandler);
        PatentsViewClassificationOperation negativeOperation = new PatentsViewClassificationOperation(negativeClient, CreateOptions(negativeClient));
        ParseException negative = Assert.Throws<ParseException>(() => negativeOperation.QueryCpcClassesGet(CreateQuery("cpc_class_id", 10)));

        Assert.Multiple(() =>
        {
            Assert.That(missing!.ResourceIdentifier, Is.EqualTo("A01"));
            Assert.That(mismatch!.DataPath, Is.EqualTo("$.wipo"));
            Assert.That(negative!.DataPath, Is.EqualTo("$.cpc_classes[0].cpc_class_num_patents"));
        });
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            PatentsViewBaseUri = new Uri("https://example.test/custom/"),
            PatentsViewApiKey = "classification-secret"
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

    private static string CpcClassBody(int total)
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":" + total + ",\"cpc_classes\":[{\"cpc_class_id\":\"G06\",\"cpc_class_title\":\"Computing; calculating or counting\",\"cpc_class_first_seen_date\":\"1976-01-06\",\"cpc_class_last_seen_date\":\"2024-05-07\",\"cpc_class_years_active\":49,\"cpc_class_num_patents\":150000,\"cpc_class_num_assignees\":12000,\"cpc_class_num_inventors\":45000,\"future_note\":\"retained\"}]}";
    }

    private static string CpcSubclassBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"cpc_subclasses\":[{\"cpc_subclass_id\":\"G06F\",\"cpc_subclass_title\":\"Electric digital data processing\",\"cpc_class_id\":\"G06\",\"cpc_class\":\"Computing\",\"cpc_subclass_years_active\":45,\"cpc_subclass_num_patents\":100000,\"cpc_subclass_num_assignees\":9000,\"cpc_subclass_num_inventors\":30000}]}";
    }

    private static string CpcGroupBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"cpc_groups\":[{\"cpc_group_id\":\"A61K2039/892\",\"cpc_group_title\":\"Medicinal preparations\",\"cpc_class_id\":\"A61\",\"cpc_class\":\"Medical science\",\"cpc_subclass_id\":\"A61K\",\"cpc_subclass\":\"Preparations\"}]}";
    }

    private static string IpcBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"ipcr\":[{\"ipc_id\":\"ipc-1\",\"ipc_section\":\"G\",\"ipc_class\":\"G06\",\"ipc_subclass\":\"G06F\",\"ipc_main_group\":\"3\",\"ipc_subgroup\":\"01\",\"ipc_classification_data_source\":\"H\",\"ipc_classification_value\":\"I\",\"ipc_symbol_position\":\"F\",\"ipc_class_level\":\"A\",\"ipc_class_status\":\"B\",\"ipc_action_date\":\"2024-01-02\",\"ipc_sequence\":2,\"future_ipc\":true}]}";
    }

    private static string UspcMainBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"uspc_mainclasses\":[{\"uspc_mainclass_id\":\"427\",\"uspc_mainclass_title\":\"Coating processes\",\"uspc_mainclass_first_seen_date\":\"1976-01-06\",\"uspc_mainclass_last_seen_date\":\"2015-06-02\",\"uspc_mainclass_years_active\":40,\"uspc_mainclass_num_patents\":9000,\"uspc_mainclass_num_assignees\":1000,\"uspc_mainclass_num_inventors\":3500}]}";
    }

    private static string UspcSubclassBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"uspc_subclasses\":[{\"uspc_subclass_id\":\"427/286\",\"uspc_subclass_title\":\"With specified material\",\"uspc_mainclass_id\":\"427\",\"uspc_mainclass\":\"Coating processes\"}]}";
    }

    private static string WipoBody()
    {
        return "{\"error\":false,\"count\":1,\"total_hits\":1,\"wipo\":[{\"wipo_id\":\"wipo-1\",\"sector_title\":\"Electrical engineering\",\"field_title\":\"Computer technology\",\"future_wipo\":{\"rank\":1}}]}";
    }
}
