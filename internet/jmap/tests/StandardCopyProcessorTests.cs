using System.Text.Json;

namespace jmap.Tests;

public class StandardCopyProcessorTests
{
    [Test]
    public void CopyPartitionsResultsDetectsDuplicatesAndBuildsThePrimaryResponse()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest request = Request();
        request.Create.Add("copy-1", CopyValue("source-1", "One"));
        request.Create.Add("copy-2", CopyValue("source-2", "Two"));
        request.Create.Add("copy-3", CopyValue("source-1", "Duplicate"));
        FakeCopyExecutor executor = ReadyExecutor("source-state", "target-old", "target-new");
        executor.CopyResults.Add("copy-1", Success("{\"id\":\"target-1\",\"serverValue\":7}"));
        executor.CopyResults.Add("copy-2", AlreadyExists("existing-2"));

        StandardCopyResult result = processor.ProcessCopy(request, JmapMethods.EmailCopy, "call-1", executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Response!.FromAccountId, Is.EqualTo("source"));
            Assert.That(result.Response.AccountId, Is.EqualTo("target"));
            Assert.That(result.Response.OldState, Is.EqualTo("target-old"));
            Assert.That(result.Response.NewState, Is.EqualTo("target-new"));
            Assert.That(result.Response.Created!["copy-1"].GetProperty("id").GetString(), Is.EqualTo("target-1"));
            Assert.That(result.Response.NotCreated!["copy-2"].ExistingId, Is.EqualTo("existing-2"));
            Assert.That(result.Response.NotCreated["copy-3"].Kind, Is.EqualTo(JmapSetErrorKind.AlreadyExists));
            Assert.That(result.Response.NotCreated["copy-3"].ExistingId, Is.EqualTo("target-1"));
            Assert.That(executor.Calls.Where(value => value.StartsWith("copy:", StringComparison.Ordinal)), Is.EqualTo(new[] { "copy:copy-1", "copy:copy-2" }));
            Assert.That(result.MethodResponses, Has.Count.EqualTo(1));
            Assert.That(result.MethodResponses[0].Name, Is.EqualTo(JmapMethods.EmailCopy));
            Assert.That(result.MethodResponses[0].CallId, Is.EqualTo("call-1"));
            Assert.That(result.MethodResponses[0].Arguments.GetProperty("created").GetProperty("copy-1").GetProperty("id").GetString(), Is.EqualTo("target-1"));
        }
    }

    [Test]
    public void OptionalSourceDestructionFollowsTheCopyResponseAndIsNotAtomicWithCopy()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest request = Request();
        request.OnSuccessDestroyOriginal = true;
        request.HasOnSuccessDestroyOriginal = true;
        request.Create.Add("copy-1", CopyValue("source-1", "One"));
        request.Create.Add("copy-2", CopyValue("source-2", "Two"));
        request.Create.Add("copy-fail", CopyValue("source-3", "Three"));
        FakeCopyExecutor executor = ReadyExecutorForDestroy();
        executor.CopyResults.Add("copy-1", Success("{\"id\":\"target-1\"}"));
        executor.CopyResults.Add("copy-2", Success("{\"id\":\"target-2\"}"));
        executor.CopyResults.Add("copy-fail", Failure(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound));
        executor.DestroyResults.Add("source-1", DestroySuccess());
        executor.DestroyResults.Add("source-2", Failure(JmapErrorCodes.Forbidden, JmapSetErrorKind.Forbidden));

        StandardCopyResult result = processor.ProcessCopy(request, JmapMethods.EmailCopy, "call-2", executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.Created!.Keys, Is.EqualTo(new[] { "copy-1", "copy-2" }));
            Assert.That(result.Response.NotCreated!.ContainsKey("copy-fail"), Is.True);
            Assert.That(result.DestroyResponse, Is.Not.Null);
            Assert.That(result.DestroyResponse!.AccountId, Is.EqualTo("source"));
            Assert.That(result.DestroyResponse.OldState, Is.EqualTo("source-before-destroy"));
            Assert.That(result.DestroyResponse.NewState, Is.EqualTo("source-after-destroy"));
            Assert.That(result.DestroyResponse.Destroyed, Is.EqualTo(new[] { "source-1" }));
            Assert.That(result.DestroyResponse.NotDestroyed!["source-2"].Kind, Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(result.MethodResponses.Select(response => response.Name), Is.EqualTo(new[] { JmapMethods.EmailCopy, JmapMethods.EmailSet }));
            Assert.That(result.MethodResponses.Select(response => response.CallId), Is.EqualTo(new[] { "call-2", "call-2" }));
            Assert.That(result.MethodResponses[1].Arguments.GetProperty("destroyed")[0].GetString(), Is.EqualTo("source-1"));
            Assert.That(executor.Calls, Is.EqualTo(new[]
            {
                "read:source",
                "read:target",
                "copy:copy-1",
                "copy:copy-2",
                "copy:copy-fail",
                "read:target",
                "read:source",
                "destroy:source-1",
                "destroy:source-2",
                "read:source"
            }));
        }
    }

    [Test]
    public void DestroyStateMismatchIsAnImplicitErrorAfterTheSuccessfulCopyResponse()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest request = Request();
        request.OnSuccessDestroyOriginal = true;
        request.DestroyFromIfInState = "expected-source";
        request.Create.Add("copy-1", CopyValue("source-1", "One"));
        FakeCopyExecutor executor = new FakeCopyExecutor();
        executor.AddAccounts("source", Account(true, true, "read-state"), Account(true, true, "changed-source"));
        executor.AddAccounts("target", Account(true, true, "target-old"), Account(true, true, "target-new"));
        executor.CopyResults.Add("copy-1", Success("{\"id\":\"target-1\"}"));

        StandardCopyResult result = processor.ProcessCopy(request, JmapMethods.EmailCopy, "call-3", executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.Created!.ContainsKey("copy-1"), Is.True);
            Assert.That(result.DestroyResponse, Is.Null);
            Assert.That(result.DestroyError!.Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(result.MethodResponses.Select(response => response.Name), Is.EqualTo(new[] { JmapMethods.EmailCopy, "error" }));
            Assert.That(result.MethodResponses[1].CallId, Is.EqualTo("call-3"));
            Assert.That(result.MethodResponses[1].Arguments.GetProperty("type").GetString(), Is.EqualTo(JmapErrorCodes.StateMismatch));
            Assert.That(executor.Calls.Any(value => value.StartsWith("destroy:", StringComparison.Ordinal)), Is.False);
        }
    }

    [Test]
    public void SourceAccountLossBeforeDestroyIsAnImplicitErrorAfterCopy()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest request = Request();
        request.OnSuccessDestroyOriginal = true;
        request.Create.Add("copy-1", CopyValue("source-1", "One"));
        FakeCopyExecutor executor = new FakeCopyExecutor();
        executor.AddAccounts("source", Account(true, true, "read-state"), Account(false, false, null));
        executor.AddAccounts("target", Account(true, true, "target-old"), Account(true, true, "target-new"));
        executor.CopyResults.Add("copy-1", Success("{\"id\":\"target-1\"}"));

        StandardCopyResult result = processor.ProcessCopy(request, JmapMethods.EmailCopy, "call-4", executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.Created!.ContainsKey("copy-1"), Is.True);
            Assert.That(result.DestroyError!.Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(result.MethodResponses.Select(response => response.Name), Is.EqualTo(new[] { JmapMethods.EmailCopy, "error" }));
            Assert.That(result.MethodResponses[1].Arguments.GetProperty("type").GetString(), Is.EqualTo(JmapErrorCodes.AccountNotFound));
            Assert.That(executor.Calls.Any(value => value.StartsWith("destroy:", StringComparison.Ordinal)), Is.False);
        }
    }

    [Test]
    public void SourceAndTargetAccountFailuresMapToTheCorrectMethodErrors()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest request = Request();

        StandardCopyResult missingSource = processor.ProcessCopy(
            request, JmapMethods.EmailCopy, "c1", AccountExecutor(Account(false, false, null), Account(true, true, "t")));
        StandardCopyResult unsupportedSource = processor.ProcessCopy(
            request, JmapMethods.EmailCopy, "c1", AccountExecutor(Account(true, false, "s"), Account(true, true, "t")));
        StandardCopyResult missingTarget = processor.ProcessCopy(
            request, JmapMethods.EmailCopy, "c1", AccountExecutor(Account(true, true, "s"), Account(false, false, null)));
        StandardCopyResult unsupportedTarget = processor.ProcessCopy(
            request, JmapMethods.EmailCopy, "c1", AccountExecutor(Account(true, true, "s"), Account(true, false, "t")));

        using (Assert.EnterMultipleScope())
        {
            AssertMethodError(missingSource, JmapErrorCodes.FromAccountNotFound, JmapMethodErrorKind.FromAccountNotFound);
            AssertMethodError(unsupportedSource, JmapErrorCodes.FromAccountNotSupportedByMethod, JmapMethodErrorKind.FromAccountNotSupportedByMethod);
            AssertMethodError(missingTarget, JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound);
            AssertMethodError(unsupportedTarget, JmapErrorCodes.AccountNotSupportedByMethod, JmapMethodErrorKind.AccountNotSupportedByMethod);
        }
    }

    [Test]
    public void SourceAndTargetStatePreconditionsAbortBeforeCopying()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest sourceRequest = Request();
        sourceRequest.IfFromInState = "wrong";
        JmapCopyRequest targetRequest = Request();
        targetRequest.IfInState = "wrong";
        FakeCopyExecutor sourceExecutor = ReadyExecutor("source-state", "target-state", "target-new");
        FakeCopyExecutor targetExecutor = ReadyExecutor("source-state", "target-state", "target-new");

        StandardCopyResult source = processor.ProcessCopy(sourceRequest, JmapMethods.EmailCopy, "c1", sourceExecutor);
        StandardCopyResult target = processor.ProcessCopy(targetRequest, JmapMethods.EmailCopy, "c2", targetExecutor);

        using (Assert.EnterMultipleScope())
        {
            AssertMethodError(source, JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch);
            AssertMethodError(target, JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch);
            Assert.That(sourceExecutor.Calls.Any(value => value.StartsWith("copy:", StringComparison.Ordinal)), Is.False);
            Assert.That(targetExecutor.Calls.Any(value => value.StartsWith("copy:", StringComparison.Ordinal)), Is.False);
        }
    }

    [Test]
    public void MalformedCopyRecordsBecomeInvalidPropertiesAndOtherRecordsContinue()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest request = Request();
        request.Create.Add("bad-shape", Json("[]"));
        request.Create.Add("missing-id", Json("{\"name\":\"Missing\"}"));
        request.Create.Add("valid", CopyValue("source-1", "Valid"));
        FakeCopyExecutor executor = ReadyExecutor("source-state", "target-old", "target-new");
        executor.CopyResults.Add("valid", Success("{\"id\":\"target-1\"}"));

        StandardCopyResult result = processor.ProcessCopy(request, JmapMethods.EmailCopy, "c1", executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.NotCreated!["bad-shape"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(result.Response.NotCreated["missing-id"].Properties, Is.EqualTo(new[] { JmapProperties.Id }));
            Assert.That(result.Response.Created!["valid"].GetProperty("id").GetString(), Is.EqualTo("target-1"));
            Assert.That(executor.Calls.Where(value => value.StartsWith("copy:", StringComparison.Ordinal)), Is.EqualTo(new[] { "copy:valid" }));
        }
    }

    [Test]
    public void InvalidCommonInputsFailBeforeAccountLookup()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest sameAccount = Request();
        sameAccount.AccountId = sameAccount.FromAccountId;
        JmapCopyRequest badCreation = Request();
        badCreation.Create.Add("#bad", CopyValue("source-1", "Bad"));
        FakeCopyExecutor executor = new FakeCopyExecutor();

        StandardCopyResult missingRequest = processor.ProcessCopy(null, JmapMethods.EmailCopy, "c1", executor);
        StandardCopyResult wrongMethod = processor.ProcessCopy(Request(), JmapMethods.EmailGet, "c1", executor);
        StandardCopyResult emptyCallId = processor.ProcessCopy(Request(), JmapMethods.EmailCopy, "", executor);
        StandardCopyResult same = processor.ProcessCopy(sameAccount, JmapMethods.EmailCopy, "c1", executor);
        StandardCopyResult creation = processor.ProcessCopy(badCreation, JmapMethods.EmailCopy, "c1", executor);

        using (Assert.EnterMultipleScope())
        {
            AssertMethodError(missingRequest, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertMethodError(wrongMethod, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertMethodError(emptyCallId, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertMethodError(same, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertMethodError(creation, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            Assert.That(executor.Calls, Is.Empty);
        }
    }

    [Test]
    public void CopyResultsAndErrorsAreDetachedFromExecutorStorage()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest request = Request();
        request.Create.Add("success", CopyValue("source-1", "One"));
        request.Create.Add("failure", CopyValue("source-2", "Two"));
        FakeCopyExecutor executor = ReadyExecutor("source-state", "target-old", "target-new");
        JsonDocument successDocument = JsonDocument.Parse("{\"id\":\"target-1\",\"value\":7}");
        JsonDocument errorDocument = JsonDocument.Parse("{\"code\":9}");
        StandardSetActionResult success = new StandardSetActionResult();
        success.IsSuccess = true;
        success.Data = successDocument.RootElement;
        JmapSetError setError = new JmapSetError();
        setError.Type = "vendorFailure";
        setError.Kind = JmapSetErrorKind.Unknown;
        setError.UnknownProperties.Add("detail", errorDocument.RootElement);
        StandardSetActionResult failure = new StandardSetActionResult();
        failure.Error = setError;
        executor.CopyResults.Add("success", success);
        executor.CopyResults.Add("failure", failure);

        StandardCopyResult result = processor.ProcessCopy(request, JmapMethods.EmailCopy, "c1", executor);
        successDocument.Dispose();
        errorDocument.Dispose();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Response!.Created!["success"].GetProperty("value").GetInt32(), Is.EqualTo(7));
            Assert.That(result.Response.NotCreated!["failure"].Type, Is.EqualTo("vendorFailure"));
            Assert.That(result.Response.NotCreated["failure"].UnknownProperties["detail"].GetProperty("code").GetInt32(), Is.EqualTo(9));
            Assert.That(result.Response.NotCreated["failure"], Is.Not.SameAs(setError));
        }
    }

    [Test]
    public void NoSuccessfulCopiesDoNotProduceAnImplicitSetResponse()
    {
        StandardCopyProcessor processor = new StandardCopyProcessor();
        JmapCopyRequest request = Request();
        request.OnSuccessDestroyOriginal = true;
        request.Create.Add("failed", CopyValue("source-1", "One"));
        FakeCopyExecutor executor = ReadyExecutor("source-state", "target-old", "target-old");
        executor.CopyResults.Add("failed", Failure(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound));

        StandardCopyResult result = processor.ProcessCopy(request, JmapMethods.EmailCopy, "c1", executor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.Created, Is.Null);
            Assert.That(result.Response.NotCreated!.ContainsKey("failed"), Is.True);
            Assert.That(result.DestroyResponse, Is.Null);
            Assert.That(result.MethodResponses, Has.Count.EqualTo(1));
            Assert.That(executor.Calls.Any(value => value.StartsWith("destroy:", StringComparison.Ordinal)), Is.False);
        }
    }

    private static JmapCopyRequest Request()
    {
        JmapCopyRequest request = new JmapCopyRequest();
        request.FromAccountId = "source";
        request.AccountId = "target";
        return request;
    }

    private static JsonElement CopyValue(string sourceId, string name)
    {
        return Json("{\"id\":\"" + sourceId + "\",\"name\":\"" + name + "\"}");
    }

    private static StandardCopyAccount Account(bool exists, bool supports, string? state)
    {
        StandardCopyAccount account = new StandardCopyAccount();
        account.Exists = exists;
        account.SupportsMethod = supports;
        account.State = state;
        return account;
    }

    private static FakeCopyExecutor AccountExecutor(StandardCopyAccount source, StandardCopyAccount target)
    {
        FakeCopyExecutor executor = new FakeCopyExecutor();
        executor.AddAccounts("source", source);
        executor.AddAccounts("target", target);
        return executor;
    }

    private static FakeCopyExecutor ReadyExecutor(string sourceState, string targetOld, string targetNew)
    {
        FakeCopyExecutor executor = new FakeCopyExecutor();
        executor.AddAccounts("source", Account(true, true, sourceState));
        executor.AddAccounts("target", Account(true, true, targetOld), Account(true, true, targetNew));
        return executor;
    }

    private static FakeCopyExecutor ReadyExecutorForDestroy()
    {
        FakeCopyExecutor executor = new FakeCopyExecutor();
        executor.AddAccounts(
            "source",
            Account(true, true, "source-read"),
            Account(true, true, "source-before-destroy"),
            Account(true, true, "source-after-destroy"));
        executor.AddAccounts("target", Account(true, true, "target-old"), Account(true, true, "target-new"));
        return executor;
    }

    private static StandardSetActionResult Success(string json)
    {
        StandardSetActionResult result = new StandardSetActionResult();
        result.IsSuccess = true;
        result.Data = Json(json);
        return result;
    }

    private static StandardSetActionResult DestroySuccess()
    {
        StandardSetActionResult result = new StandardSetActionResult();
        result.IsSuccess = true;
        return result;
    }

    private static StandardSetActionResult Failure(string type, JmapSetErrorKind kind)
    {
        StandardSetActionResult result = new StandardSetActionResult();
        result.Error = new JmapSetError { Type = type, Kind = kind, Description = "Rejected" };
        return result;
    }

    private static StandardSetActionResult AlreadyExists(string existingId)
    {
        StandardSetActionResult result = Failure(JmapErrorCodes.AlreadyExists, JmapSetErrorKind.AlreadyExists);
        result.Error!.ExistingId = existingId;
        return result;
    }

    private static void AssertMethodError(StandardCopyResult result, string type, JmapMethodErrorKind kind)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(type));
            Assert.That(result.Error.Kind, Is.EqualTo(kind));
            Assert.That(result.MethodResponses, Is.Empty);
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

internal class FakeCopyExecutor : IStandardCopyExecutor
{
    internal Dictionary<string, Queue<StandardCopyAccount>> Accounts = new Dictionary<string, Queue<StandardCopyAccount>>();
    internal Dictionary<string, StandardSetActionResult> CopyResults = new Dictionary<string, StandardSetActionResult>();
    internal Dictionary<string, StandardSetActionResult> DestroyResults = new Dictionary<string, StandardSetActionResult>();
    internal List<string> Calls = new List<string>();

    internal void AddAccounts(string accountId, params StandardCopyAccount[] accounts)
    {
        Accounts.Add(accountId, new Queue<StandardCopyAccount>(accounts));
    }

    public StandardCopyAccount ReadCopyAccount(string accountId)
    {
        Calls.Add("read:" + accountId);
        return Accounts[accountId].Dequeue();
    }

    public StandardSetActionResult ExecuteCopyCreate(string fromAccountId, string accountId, string creationId, string sourceId, JsonElement value)
    {
        Calls.Add("copy:" + creationId);
        return CopyResults[creationId];
    }

    public StandardSetActionResult ExecuteCopyDestroy(string fromAccountId, string sourceId)
    {
        Calls.Add("destroy:" + sourceId);
        return DestroyResults[sourceId];
    }
}
