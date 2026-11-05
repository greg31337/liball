using System.Text.Json;

namespace jmap.Tests;

public class ProtocolModelsTests
{
    [Test]
    public void InvocationStoresTupleValuesAndArbitraryArguments()
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = JmapMethods.CoreEcho;
        invocation.Arguments = Json("{\"answer\":42,\"enabled\":false}");
        invocation.CallId = "c1";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(invocation.Arguments.GetProperty("answer").GetInt32(), Is.EqualTo(42));
            Assert.That(invocation.Arguments.GetProperty("enabled").GetBoolean(), Is.False);
            Assert.That(invocation.CallId, Is.EqualTo("c1"));
        }
    }

    [Test]
    public void RequestPreservesCallOrderAndOptionalCreatedIdsState()
    {
        JmapRequest request = new JmapRequest();
        JmapInvocation first = Invocation("First/get", "c1");
        JmapInvocation second = Invocation("Second/set", "c2");
        request.Using.Add(JmapCapabilities.Core);
        request.MethodCalls.Add(first);
        request.MethodCalls.Add(second);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.MethodCalls, Is.EqualTo(new[] { first, second }));
            Assert.That(request.CreatedIds, Is.Null);
            Assert.That(request.UnknownProperties, Is.Empty);
        }

        request.CreatedIds = new Dictionary<string, string>();
        request.UnknownProperties.Add("vendorRequest", Json("null"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.CreatedIds, Is.Not.Null.And.Empty);
            Assert.That(request.UnknownProperties["vendorRequest"].ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    [Test]
    public void ResponsePreservesResponseOrderAndCreatedIds()
    {
        JmapResponse response = new JmapResponse();
        JmapInvocation first = Invocation("Email/copy", "c1");
        JmapInvocation implicitResponse = Invocation("Email/set", "c1");
        response.MethodResponses.Add(first);
        response.MethodResponses.Add(implicitResponse);
        response.CreatedIds = new Dictionary<string, string>();
        response.CreatedIds.Add("draft", "M123");
        response.SessionState = "state-2";
        response.UnknownProperties.Add("vendorResponse", Json("{\"ok\":true}"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses, Is.EqualTo(new[] { first, implicitResponse }));
            Assert.That(response.MethodResponses[0].CallId, Is.EqualTo("c1"));
            Assert.That(response.MethodResponses[1].CallId, Is.EqualTo("c1"));
            Assert.That(response.CreatedIds["draft"], Is.EqualTo("M123"));
            Assert.That(response.SessionState, Is.EqualTo("state-2"));
            Assert.That(response.UnknownProperties["vendorResponse"].GetProperty("ok").GetBoolean(), Is.True);
        }
    }

    [Test]
    public void ResultReferenceAndRawCallRetainLowLevelCallData()
    {
        JmapResultReference reference = new JmapResultReference();
        reference.ResultOf = "c1";
        reference.Name = JmapMethods.EmailQuery;
        reference.Path = "/ids";
        reference.UnknownProperties.Add("vendorReference", Json("\"value\""));

        JmapRawMethodCall call = new JmapRawMethodCall();
        call.Name = JmapMethods.EmailGet;
        call.Arguments = Json("{\"accountId\":\"a1\"}");
        call.RequiredCapabilities.Add(JmapCapabilities.Mail);
        call.ResultReferences.Add("#ids", reference);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(call.CallId, Is.Null);
            Assert.That(call.Arguments.GetProperty("accountId").GetString(), Is.EqualTo("a1"));
            Assert.That(call.RequiredCapabilities, Is.EqualTo(new[] { JmapCapabilities.Mail }));
            Assert.That(call.ResultReferences["#ids"], Is.SameAs(reference));
            Assert.That(reference.ResultOf, Is.EqualTo("c1"));
            Assert.That(reference.Name, Is.EqualTo(JmapMethods.EmailQuery));
            Assert.That(reference.Path, Is.EqualTo("/ids"));
            Assert.That(reference.UnknownProperties["vendorReference"].GetString(), Is.EqualTo("value"));
        }
    }

    [Test]
    public void ProblemDistinguishesMissingAndPresentStatus()
    {
        JmapProblem missingStatus = new JmapProblem();

        JmapProblem problem = new JmapProblem();
        problem.Type = JmapErrorCodes.LimitProblemType;
        problem.Kind = JmapProblemKind.Limit;
        problem.Title = "Request limit exceeded";
        problem.Status = 400;
        problem.HasStatus = true;
        problem.Detail = "The request is too large.";
        problem.Instance = "urn:request:123";
        problem.Limit = JmapProperties.MaxSizeRequest;
        problem.UnknownProperties.Add("retryable", Json("false"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missingStatus.HasStatus, Is.False);
            Assert.That(missingStatus.Kind, Is.EqualTo(JmapProblemKind.Unknown));
            Assert.That(problem.Type, Is.EqualTo(JmapErrorCodes.LimitProblemType));
            Assert.That(problem.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(problem.Title, Is.EqualTo("Request limit exceeded"));
            Assert.That(problem.Status, Is.EqualTo(400));
            Assert.That(problem.HasStatus, Is.True);
            Assert.That(problem.Detail, Is.EqualTo("The request is too large."));
            Assert.That(problem.Instance, Is.EqualTo("urn:request:123"));
            Assert.That(problem.Limit, Is.EqualTo(JmapProperties.MaxSizeRequest));
            Assert.That(problem.UnknownProperties["retryable"].GetBoolean(), Is.False);
        }
    }

    [Test]
    public void MethodErrorAndCallResultPreserveKnownAndUnknownData()
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = "vendorFailure";
        error.Kind = JmapMethodErrorKind.Unknown;
        error.Description = "A vendor-specific failure occurred.";
        error.UnknownProperties.Add("vendorCode", Json("17"));

        JmapCallResult result = new JmapCallResult();
        result.Name = "error";
        result.CallId = "c9";
        result.Arguments = Json("{\"type\":\"vendorFailure\",\"vendorCode\":17}");
        result.Error = error;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Name, Is.EqualTo("error"));
            Assert.That(result.CallId, Is.EqualTo("c9"));
            Assert.That(result.Arguments.GetProperty(JmapProperties.Type).GetString(), Is.EqualTo("vendorFailure"));
            Assert.That(result.Error, Is.SameAs(error));
            Assert.That(error.Type, Is.EqualTo("vendorFailure"));
            Assert.That(error.Kind, Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(error.UnknownProperties["vendorCode"].GetInt32(), Is.EqualTo(17));
        }
    }

    [Test]
    public void SetErrorStoresEveryStructuredCoreAndMailDetail()
    {
        JmapSetError error = new JmapSetError();
        error.Type = JmapErrorCodes.InvalidRecipients;
        error.Kind = JmapSetErrorKind.InvalidRecipients;
        error.Description = "Some recipients are invalid.";
        error.Properties = new List<string> { JmapProperties.Envelope };
        error.ExistingId = "existing-1";
        error.MaxSize = 50000000;
        error.HasMaxSize = true;
        error.MaxRecipients = 100;
        error.HasMaxRecipients = true;
        error.InvalidRecipients = new List<string> { "invalid" };
        error.NotFound = new List<string> { "blob-1" };
        error.UnknownProperties.Add("vendorSetError", Json("{\"code\":9}"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Type, Is.EqualTo(JmapErrorCodes.InvalidRecipients));
            Assert.That(error.Kind, Is.EqualTo(JmapSetErrorKind.InvalidRecipients));
            Assert.That(error.Description, Is.EqualTo("Some recipients are invalid."));
            Assert.That(error.Properties, Is.EqualTo(new[] { JmapProperties.Envelope }));
            Assert.That(error.ExistingId, Is.EqualTo("existing-1"));
            Assert.That(error.MaxSize, Is.EqualTo(50000000));
            Assert.That(error.HasMaxSize, Is.True);
            Assert.That(error.MaxRecipients, Is.EqualTo(100));
            Assert.That(error.HasMaxRecipients, Is.True);
            Assert.That(error.InvalidRecipients, Is.EqualTo(new[] { "invalid" }));
            Assert.That(error.NotFound, Is.EqualTo(new[] { "blob-1" }));
            Assert.That(error.UnknownProperties["vendorSetError"].GetProperty("code").GetInt32(), Is.EqualTo(9));
        }
    }

    [Test]
    public void NewProtocolModelsDoNotShareMutableCollections()
    {
        JmapRequest firstRequest = new JmapRequest();
        JmapRequest secondRequest = new JmapRequest();
        JmapRawMethodCall firstCall = new JmapRawMethodCall();
        JmapRawMethodCall secondCall = new JmapRawMethodCall();
        JmapSetError firstError = new JmapSetError();
        JmapSetError secondError = new JmapSetError();

        firstRequest.Using.Add(JmapCapabilities.Core);
        firstCall.RequiredCapabilities.Add(JmapCapabilities.Mail);
        firstError.UnknownProperties.Add("x", Json("1"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondRequest.Using, Is.Empty);
            Assert.That(secondCall.RequiredCapabilities, Is.Empty);
            Assert.That(secondError.UnknownProperties, Is.Empty);
            Assert.That(secondRequest.Using, Is.Not.SameAs(firstRequest.Using));
            Assert.That(secondCall.RequiredCapabilities, Is.Not.SameAs(firstCall.RequiredCapabilities));
            Assert.That(secondError.UnknownProperties, Is.Not.SameAs(firstError.UnknownProperties));
        }
    }

    private static JmapInvocation Invocation(string name, string callId)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.Arguments = Json("{}");
        invocation.CallId = callId;
        return invocation;
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
