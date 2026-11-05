using System.Text.Json;

namespace jmap.Tests;

public class StandardGetProcessorTests
{
    [Test]
    public void RequestedIdsAreReturnedDeterministicallyWithDuplicatesRemoved()
    {
        StandardGetProcessor processor = Processor(10, true);
        JmapGetRequest request = Request("account-1", new List<string> { "m2", "missing", "m1", "m2", "missing" }, null);
        List<JsonElement> records = new List<JsonElement>
        {
            Record("m1", "First", true),
            Record("m2", "Second", false),
            Record("unrequested", "Extra", true)
        };

        StandardGetResult result = processor.ProcessGet(request, records, "state-7");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.AccountId, Is.EqualTo("account-1"));
            Assert.That(result.Response.State, Is.EqualTo("state-7"));
            Assert.That(result.Response.List.Select(ItemId), Is.EqualTo(new[] { "m2", "m1" }));
            Assert.That(result.Response.NotFound, Is.EqualTo(new[] { "missing" }));
        }
    }

    [Test]
    public void NullIdsReturnAllRecordsInServerOrderAndAnEmptyNotFoundList()
    {
        StandardGetProcessor processor = Processor(10, true);
        JmapGetRequest request = Request("account-1", null, null);
        List<JsonElement> records = new List<JsonElement>
        {
            Record("m3", "Third", true),
            Record("m1", "First", false)
        };

        StandardGetResult result = processor.ProcessGet(request, records, "state-all");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.List.Select(ItemId), Is.EqualTo(new[] { "m3", "m1" }));
            Assert.That(result.Response.NotFound, Is.Empty);
        }
    }

    [Test]
    public void EmptyIdsReturnNoRecordsEvenWhenRecordsExist()
    {
        StandardGetProcessor processor = Processor(10, true);
        JmapGetRequest request = Request("account-1", new List<string>(), null);

        StandardGetResult result = processor.ProcessGet(
            request,
            new List<JsonElement> { Record("m1", "First", true) },
            "state-empty");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.List, Is.Empty);
            Assert.That(result.Response.NotFound, Is.Empty);
            Assert.That(result.Response.State, Is.EqualTo("state-empty"));
        }
    }

    [Test]
    public void ProjectionReturnsIdAndOnlyRequestedProperties()
    {
        StandardGetProcessor processor = Processor(10, true);
        JmapGetRequest projectedRequest = Request(
            "account-1",
            new List<string> { "m1" },
            new List<string> { "name", "name" });
        JsonElement source = Json("{\"id\":\"m1\",\"name\":\"First\",\"enabled\":true,\"vendor\":{\"x\":1}}");

        StandardGetResult projected = processor.ProcessGet(
            projectedRequest,
            new List<JsonElement> { source },
            "s1");
        StandardGetResult complete = processor.ProcessGet(
            Request("account-1", new List<string> { "m1" }, null),
            new List<JsonElement> { source },
            "s1");
        JsonElement projectedRecord = projected.Response!.List[0];
        JsonElement completeRecord = complete.Response!.List[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(projectedRecord.GetProperty("id").GetString(), Is.EqualTo("m1"));
            Assert.That(projectedRecord.GetProperty("name").GetString(), Is.EqualTo("First"));
            Assert.That(projectedRecord.TryGetProperty("enabled", out JsonElement _), Is.False);
            Assert.That(projectedRecord.TryGetProperty("vendor", out JsonElement _), Is.False);
            Assert.That(completeRecord.GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(completeRecord.GetProperty("vendor").GetProperty("x").GetInt32(), Is.EqualTo(1));
            Assert.That(source.GetProperty("enabled").GetBoolean(), Is.True);
        }
    }

    [Test]
    public void EmptyProjectionStillIncludesTheIdProperty()
    {
        StandardGetProcessor processor = Processor(10, true);
        JmapGetRequest request = Request("account-1", new List<string> { "m1" }, new List<string>());

        StandardGetResult result = processor.ProcessGet(
            request,
            new List<JsonElement> { Record("m1", "First", true) },
            "s1");
        JsonElement record = result.Response!.List[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(record.EnumerateObject().Select(property => property.Name), Is.EqualTo(new[] { "id" }));
            Assert.That(record.GetProperty("id").GetString(), Is.EqualTo("m1"));
        }
    }

    [Test]
    public void InvalidCommonArgumentsMapToInvalidArguments()
    {
        StandardGetProcessor processor = Processor(10, true);
        StandardGetProcessor explicitOnly = Processor(10, false);

        StandardGetResult missingAccount = processor.ProcessGet(
            Request(null, new List<string>(), null), new List<JsonElement>(), "s1");
        StandardGetResult invalidId = processor.ProcessGet(
            Request("account-1", new List<string> { "bad/id" }, null), new List<JsonElement>(), "s1");
        StandardGetResult invalidProperty = processor.ProcessGet(
            Request("account-1", new List<string>(), new List<string> { "unsupported" }), new List<JsonElement>(), "s1");
        StandardGetResult unsupportedAll = explicitOnly.ProcessGet(
            Request("account-1", null, null), new List<JsonElement>(), "s1");

        using (Assert.EnterMultipleScope())
        {
            AssertInvalidArguments(missingAccount);
            AssertInvalidArguments(invalidId);
            AssertInvalidArguments(invalidProperty);
            AssertInvalidArguments(unsupportedAll);
        }
    }

    [Test]
    public void ExplicitAndAllRecordLimitFailuresMapToRequestTooLarge()
    {
        StandardGetProcessor processor = Processor(2, true);
        StandardGetResult explicitIds = processor.ProcessGet(
            Request("account-1", new List<string> { "m1", "m2", "m3" }, null),
            new List<JsonElement>(),
            "s1");
        StandardGetResult allRecords = processor.ProcessGet(
            Request("account-1", null, null),
            new List<JsonElement>
            {
                Record("m1", "First", true),
                Record("m2", "Second", true),
                Record("m3", "Third", true)
            },
            "s1");

        using (Assert.EnterMultipleScope())
        {
            AssertRequestTooLarge(explicitIds);
            AssertRequestTooLarge(allRecords);
        }
    }

    [Test]
    public void InvalidBackendRecordsAreProtocolFailures()
    {
        StandardGetProcessor processor = Processor(10, true);
        JmapGetRequest request = Request("account-1", null, null);

        JmapProtocolException missingId = Assert.Throws<JmapProtocolException>(
            () => processor.ProcessGet(request, new List<JsonElement> { Json("{\"name\":\"Missing\"}") }, "s1"))!;
        JmapProtocolException duplicateId = Assert.Throws<JmapProtocolException>(
            () => processor.ProcessGet(
                request,
                new List<JsonElement> { Record("m1", "First", true), Record("m1", "Duplicate", false) },
                "s1"))!;
        JmapProtocolException missingState = Assert.Throws<JmapProtocolException>(
            () => processor.ProcessGet(request, new List<JsonElement>(), null))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missingId.Path, Is.EqualTo("list/0/id"));
            Assert.That(duplicateId.Path, Is.EqualTo("list/1/id"));
            Assert.That(missingState.Path, Is.EqualTo("state"));
        }
    }

    [Test]
    public void ReturnedRecordsAreDetachedFromBackendJsonStorage()
    {
        StandardGetProcessor processor = Processor(10, true);
        StandardGetResult result;
        using (JsonDocument document = JsonDocument.Parse("{\"id\":\"m1\",\"name\":\"First\"}"))
        {
            result = processor.ProcessGet(
                Request("account-1", null, null),
                new List<JsonElement> { document.RootElement },
                "s1");
        }

        Assert.That(result.Response!.List[0].GetProperty("name").GetString(), Is.EqualTo("First"));
    }

    private static StandardGetProcessor Processor(int maximum, bool supportsAll)
    {
        return new StandardGetProcessor(
            maximum,
            new List<string> { "name", "enabled", "vendor" },
            supportsAll);
    }

    private static JmapGetRequest Request(string? accountId, List<string>? ids, List<string>? properties)
    {
        JmapGetRequest request = new JmapGetRequest();
        request.AccountId = accountId;
        request.Ids = ids;
        request.Properties = properties;
        return request;
    }

    private static JsonElement Record(string id, string name, bool enabled)
    {
        return Json(
            "{\"id\":\"" + id + "\",\"name\":\"" + name + "\",\"enabled\":" +
            (enabled ? "true" : "false") + "}");
    }

    private static string? ItemId(JsonElement item)
    {
        return item.GetProperty("id").GetString();
    }

    private static void AssertInvalidArguments(StandardGetResult result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(JmapErrorCodes.InvalidArguments));
            Assert.That(result.Error.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
        }
    }

    private static void AssertRequestTooLarge(StandardGetResult result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(JmapErrorCodes.RequestTooLarge));
            Assert.That(result.Error.Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
