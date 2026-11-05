using System.Text.Json;

namespace jmap.Tests;

public class StandardSetProcessorTests
{
    [Test]
    public void ActionsRunInCreateUpdateDestroyOrderAndPopulateEveryPartition()
    {
        StandardSetProcessor processor = new StandardSetProcessor(10);
        JmapSetRequest request = Request("account-1");
        request.Create = new Dictionary<string, JsonElement>
        {
            { "create-ok", Json("{\"name\":\"New\"}") },
            { "create-fail", Json("{\"name\":\"Bad\"}") }
        };
        request.Update = new Dictionary<string, JmapPatchObject>
        {
            { "update-ok", Patch(("name", "\"Changed\"")) },
            { "update-fail", Patch(("name", "\"Missing\"")) }
        };
        request.Destroy = new List<string> { "destroy-ok", "destroy-fail" };
        FakeSetExecutor executor = new FakeSetExecutor("state-1", "state-2");
        executor.CreateResults.Add("create-ok", Success("{\"id\":\"new-1\",\"createdAt\":\"now\"}"));
        executor.CreateResults.Add("create-fail", Failure(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties));
        executor.UpdateResults.Add("update-ok", Success("null"));
        executor.UpdateResults.Add("update-fail", Failure(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound));
        executor.DestroyResults.Add("destroy-ok", SuccessForDestroy());
        executor.DestroyResults.Add("destroy-fail", Failure(JmapErrorCodes.Forbidden, JmapSetErrorKind.Forbidden));

        StandardSetResult result = processor.ProcessSet(request, executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.AccountId, Is.EqualTo("account-1"));
            Assert.That(result.Response.OldState, Is.EqualTo("state-1"));
            Assert.That(result.Response.NewState, Is.EqualTo("state-2"));
            Assert.That(result.Response.Created!["create-ok"].GetProperty("id").GetString(), Is.EqualTo("new-1"));
            Assert.That(result.Response.NotCreated!["create-fail"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(result.Response.Updated!["update-ok"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(result.Response.NotUpdated!["update-fail"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(result.Response.Destroyed, Is.EqualTo(new[] { "destroy-ok" }));
            Assert.That(result.Response.NotDestroyed!["destroy-fail"].Kind, Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(executor.Calls, Is.EqualTo(new[]
            {
                "create:create-ok",
                "create:create-fail",
                "update:update-ok",
                "update:update-fail",
                "destroy:destroy-ok",
                "destroy:destroy-fail"
            }));
            Assert.That(executor.StateReads, Is.EqualTo(2));
        }
    }

    [Test]
    public void EmptySetReturnsStatesAndLeavesAllActionPartitionsNull()
    {
        StandardSetProcessor processor = new StandardSetProcessor(10);
        FakeSetExecutor executor = new FakeSetExecutor("same", "same");

        StandardSetResult result = processor.ProcessSet(Request("account-1"), executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.OldState, Is.EqualTo("same"));
            Assert.That(result.Response.NewState, Is.EqualTo("same"));
            Assert.That(result.Response.Created, Is.Null);
            Assert.That(result.Response.Updated, Is.Null);
            Assert.That(result.Response.Destroyed, Is.Null);
            Assert.That(result.Response.NotCreated, Is.Null);
            Assert.That(result.Response.NotUpdated, Is.Null);
            Assert.That(result.Response.NotDestroyed, Is.Null);
            Assert.That(executor.Calls, Is.Empty);
        }
    }

    [Test]
    public void StateMismatchAbortsTheWholeMethodBeforeAnyAction()
    {
        StandardSetProcessor processor = new StandardSetProcessor(10);
        JmapSetRequest request = Request("account-1");
        request.IfInState = "expected";
        request.Create = new Dictionary<string, JsonElement> { { "c1", Json("{}") } };
        FakeSetExecutor executor = new FakeSetExecutor("actual", "changed");
        executor.CreateResults.Add("c1", Success("{\"id\":\"m1\"}"));

        StandardSetResult result = processor.ProcessSet(request, executor);

        using (Assert.EnterMultipleScope())
        {
            AssertMethodError(result, JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch);
            Assert.That(executor.Calls, Is.Empty);
            Assert.That(executor.StateReads, Is.EqualTo(1));
        }
    }

    [Test]
    public void CombinedActionLimitAbortsBeforeStateOrActionsAreRead()
    {
        StandardSetProcessor processor = new StandardSetProcessor(2);
        JmapSetRequest request = Request("account-1");
        request.Create = new Dictionary<string, JsonElement> { { "c1", Json("{}") } };
        request.Update = new Dictionary<string, JmapPatchObject> { { "m1", Patch(("name", "\"Changed\"")) } };
        request.Destroy = new List<string> { "m2" };
        FakeSetExecutor executor = new FakeSetExecutor("s1", "s2");

        StandardSetResult result = processor.ProcessSet(request, executor);

        using (Assert.EnterMultipleScope())
        {
            AssertMethodError(result, JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge);
            Assert.That(executor.StateReads, Is.Zero);
            Assert.That(executor.Calls, Is.Empty);
        }
    }

    [Test]
    public void InvalidRecordsAndPatchesBecomePerRecordErrorsAndProcessingContinues()
    {
        StandardSetProcessor processor = new StandardSetProcessor(10);
        JmapSetRequest request = Request("account-1");
        request.Create = new Dictionary<string, JsonElement> { { "bad-create", Json("[]") } };
        request.Update = new Dictionary<string, JmapPatchObject>
        {
            { "bad-update", Patch(("settings", "{}"), ("settings/value", "1")) }
        };
        request.Destroy = new List<string> { "destroy-ok" };
        FakeSetExecutor executor = new FakeSetExecutor("s1", "s2");
        executor.DestroyResults.Add("destroy-ok", SuccessForDestroy());

        StandardSetResult result = processor.ProcessSet(request, executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.NotCreated!["bad-create"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(result.Response.NotUpdated!["bad-update"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidPatch));
            Assert.That(result.Response.Destroyed, Is.EqualTo(new[] { "destroy-ok" }));
            Assert.That(executor.Calls, Is.EqualTo(new[] { "destroy:destroy-ok" }));
        }
    }

    [Test]
    public void UpdateAlsoScheduledForDestroyMapsToWillDestroyThenDestroys()
    {
        StandardSetProcessor processor = new StandardSetProcessor(10);
        JmapSetRequest request = Request("account-1");
        request.Update = new Dictionary<string, JmapPatchObject> { { "m1", Patch(("name", "\"Changed\"")) } };
        request.Destroy = new List<string> { "m1" };
        FakeSetExecutor executor = new FakeSetExecutor("s1", "s2");
        executor.DestroyResults.Add("m1", SuccessForDestroy());

        StandardSetResult result = processor.ProcessSet(request, executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.Updated, Is.Null);
            Assert.That(result.Response.NotUpdated!["m1"].Type, Is.EqualTo(JmapErrorCodes.WillDestroy));
            Assert.That(result.Response.NotUpdated["m1"].Kind, Is.EqualTo(JmapSetErrorKind.WillDestroy));
            Assert.That(result.Response.Destroyed, Is.EqualTo(new[] { "m1" }));
            Assert.That(executor.Calls, Is.EqualTo(new[] { "destroy:m1" }));
        }
    }

    [Test]
    public void InvalidCommonArgumentsAbortWithoutCallingTheExecutor()
    {
        StandardSetProcessor processor = new StandardSetProcessor(10);
        FakeSetExecutor executor = new FakeSetExecutor("s1", "s2");
        JmapSetRequest invalidCreationId = Request("account-1");
        invalidCreationId.Create = new Dictionary<string, JsonElement> { { "#bad", Json("{}") } };
        JmapSetRequest invalidUpdateId = Request("account-1");
        invalidUpdateId.Update = new Dictionary<string, JmapPatchObject> { { "bad/id", Patch(("name", "null")) } };
        JmapSetRequest duplicateDestroy = Request("account-1");
        duplicateDestroy.Destroy = new List<string> { "m1", "m1" };

        StandardSetResult missingRequest = processor.ProcessSet(null, executor);
        StandardSetResult missingAccount = processor.ProcessSet(Request(null), executor);
        StandardSetResult badCreate = processor.ProcessSet(invalidCreationId, executor);
        StandardSetResult badUpdate = processor.ProcessSet(invalidUpdateId, executor);
        StandardSetResult badDestroy = processor.ProcessSet(duplicateDestroy, executor);

        using (Assert.EnterMultipleScope())
        {
            AssertMethodError(missingRequest, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertMethodError(missingAccount, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertMethodError(badCreate, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertMethodError(badUpdate, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertMethodError(badDestroy, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            Assert.That(executor.StateReads, Is.Zero);
            Assert.That(executor.Calls, Is.Empty);
        }
    }

    [Test]
    public void SetErrorsAndSuccessfulDataAreDetachedFromExecutorStorage()
    {
        StandardSetProcessor processor = new StandardSetProcessor(10);
        JmapSetRequest request = Request("account-1");
        request.Create = new Dictionary<string, JsonElement> { { "c1", Json("{}") } };
        request.Destroy = new List<string> { "m2" };
        FakeSetExecutor executor = new FakeSetExecutor("s1", "s2");
        JsonDocument createdDocument = JsonDocument.Parse("{\"id\":\"m1\",\"server\":true}");
        JsonDocument errorDocument = JsonDocument.Parse("{\"code\":17}");
        StandardSetActionResult created = new StandardSetActionResult();
        created.IsSuccess = true;
        created.Data = createdDocument.RootElement;
        JmapSetError error = new JmapSetError();
        error.Type = "vendorDenied";
        error.Kind = JmapSetErrorKind.Unknown;
        error.UnknownProperties.Add("details", errorDocument.RootElement);
        StandardSetActionResult destroyed = new StandardSetActionResult();
        destroyed.Error = error;
        executor.CreateResults.Add("c1", created);
        executor.DestroyResults.Add("m2", destroyed);

        StandardSetResult result = processor.ProcessSet(request, executor);
        createdDocument.Dispose();
        errorDocument.Dispose();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Response!.Created!["c1"].GetProperty("server").GetBoolean(), Is.True);
            Assert.That(result.Response.NotDestroyed!["m2"].Type, Is.EqualTo("vendorDenied"));
            Assert.That(result.Response.NotDestroyed["m2"].UnknownProperties["details"].GetProperty("code").GetInt32(), Is.EqualTo(17));
            Assert.That(result.Response.NotDestroyed["m2"], Is.Not.SameAs(error));
        }
    }

    [Test]
    public void InconsistentExecutorResultsAreProtocolFailures()
    {
        StandardSetProcessor processor = new StandardSetProcessor(10);
        JmapSetRequest request = Request("account-1");
        request.Create = new Dictionary<string, JsonElement> { { "c1", Json("{}") } };
        FakeSetExecutor executor = new FakeSetExecutor("s1", "s2");
        executor.CreateResults.Add("c1", new StandardSetActionResult { IsSuccess = true });

        JmapProtocolException exception = Assert.Throws<JmapProtocolException>(
            () => processor.ProcessSet(request, executor))!;

        Assert.That(exception.Path, Is.EqualTo("created/c1"));
    }

    private static JmapSetRequest Request(string? accountId)
    {
        JmapSetRequest request = new JmapSetRequest();
        request.AccountId = accountId;
        return request;
    }

    private static JmapPatchObject Patch(params (string Path, string Json)[] entries)
    {
        JmapPatchObject patch = new JmapPatchObject();
        foreach ((string path, string json) in entries)
        {
            patch.Values.Add(path, Json(json));
        }

        return patch;
    }

    private static StandardSetActionResult Success(string json)
    {
        StandardSetActionResult result = new StandardSetActionResult();
        result.IsSuccess = true;
        result.Data = Json(json);
        return result;
    }

    private static StandardSetActionResult SuccessForDestroy()
    {
        StandardSetActionResult result = new StandardSetActionResult();
        result.IsSuccess = true;
        return result;
    }

    private static StandardSetActionResult Failure(string type, JmapSetErrorKind kind)
    {
        StandardSetActionResult result = new StandardSetActionResult();
        result.Error = new JmapSetError
        {
            Type = type,
            Kind = kind,
            Description = "Rejected"
        };
        return result;
    }

    private static void AssertMethodError(StandardSetResult result, string type, JmapMethodErrorKind kind)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(type));
            Assert.That(result.Error.Kind, Is.EqualTo(kind));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

internal class FakeSetExecutor : IStandardSetExecutor
{
    internal string? OldState;
    internal string? NewState;
    internal int StateReads;
    internal List<string> Calls = new List<string>();
    internal Dictionary<string, StandardSetActionResult> CreateResults = new Dictionary<string, StandardSetActionResult>();
    internal Dictionary<string, StandardSetActionResult> UpdateResults = new Dictionary<string, StandardSetActionResult>();
    internal Dictionary<string, StandardSetActionResult> DestroyResults = new Dictionary<string, StandardSetActionResult>();

    internal FakeSetExecutor(string? oldState, string? newState)
    {
        OldState = oldState;
        NewState = newState;
    }

    public string? ReadSetState(string accountId)
    {
        string? result = StateReads == 0 ? OldState : NewState;
        StateReads = StateReads + 1;
        return result;
    }

    public StandardSetActionResult ExecuteSetCreate(string accountId, string creationId, JsonElement value)
    {
        Calls.Add("create:" + creationId);
        return CreateResults[creationId];
    }

    public StandardSetActionResult ExecuteSetUpdate(string accountId, string id, JmapPatchObject patch)
    {
        Calls.Add("update:" + id);
        return UpdateResults[id];
    }

    public StandardSetActionResult ExecuteSetDestroy(string accountId, string id)
    {
        Calls.Add("destroy:" + id);
        return DestroyResults[id];
    }
}
