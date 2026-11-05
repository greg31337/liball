using System.Text.Json;

namespace jmap.Tests;

public class ResponseMapperTests
{
    [Test]
    public void CorrelationPreservesAllResponsesInWireOrderAndRawSelectsFirst()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation(JmapMethods.EmailCopy, "copy-1", "{\"copied\":{\"m1\":\"m2\"}}"));
        response.MethodResponses.Add(Invocation(JmapMethods.EmailGet, "other", "{\"list\":[]}"));
        response.MethodResponses.Add(Invocation(JmapMethods.EmailSet, "copy-1", "{\"destroyed\":[\"m1\"]}"));

        List<JmapCallResult> results = mapper.FindAllResults(response, "copy-1");
        JmapCallResult raw = mapper.FindRawResult(response, "copy-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results.Select(result => result.Name), Is.EqualTo(new[] { JmapMethods.EmailCopy, JmapMethods.EmailSet }));
            Assert.That(results.Select(result => result.CallId), Is.EqualTo(new[] { "copy-1", "copy-1" }));
            Assert.That(results[0].Arguments.GetProperty("copied").GetProperty("m1").GetString(), Is.EqualTo("m2"));
            Assert.That(results[1].Arguments.GetProperty("destroyed")[0].GetString(), Is.EqualTo("m1"));
            Assert.That(raw.Name, Is.EqualTo(JmapMethods.EmailCopy));
            Assert.That(raw.Error, Is.Null);
        }
    }

    [Test]
    public void TypedCorrelationCanSelectAnImplicitResponseFromOneCall()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation(JmapMethods.EmailCopy, "copy-1", "{\"copied\":{}}"));
        response.MethodResponses.Add(Invocation(JmapMethods.EmailSet, "copy-1", "{\"destroyed\":[\"source-1\"]}"));

        JmapCallResult result = mapper.FindTypedResult(response, "copy-1", JmapMethods.EmailSet);
        JsonElement arguments = mapper.RequireSuccessfulArguments(result, JmapMethods.EmailSet);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Name, Is.EqualTo(JmapMethods.EmailSet));
            Assert.That(result.CallId, Is.EqualTo("copy-1"));
            Assert.That(arguments.GetProperty("destroyed")[0].GetString(), Is.EqualTo("source-1"));
        }
    }

    [Test]
    public void ErrorMappingClassifiesTheErrorAndPreservesRawArguments()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation(
            "error",
            "get-1",
            "{\"type\":\"serverUnavailable\",\"description\":\"Try later\",\"retryAfter\":30}"));

        JmapCallResult result = mapper.FindRawResult(response, "get-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Name, Is.EqualTo("error"));
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(JmapErrorCodes.ServerUnavailable));
            Assert.That(result.Error.Kind, Is.EqualTo(JmapMethodErrorKind.ServerUnavailable));
            Assert.That(result.Error.Description, Is.EqualTo("Try later"));
            Assert.That(result.Error.UnknownProperties["retryAfter"].GetInt32(), Is.EqualTo(30));
            Assert.That(result.Arguments.GetProperty("retryAfter").GetInt32(), Is.EqualTo(30));
        }
    }

    [Test]
    public void TypedCorrelationReturnsTheCorrelatedMethodError()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation("error", "get-1", "{\"type\":\"accountNotFound\"}"));
        response.MethodResponses.Add(Invocation(JmapMethods.EmailGet, "other", "{\"list\":[]}"));

        JmapCallResult result = mapper.FindTypedResult(response, "get-1", JmapMethods.EmailGet);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Name, Is.EqualTo("error"));
            Assert.That(result.CallId, Is.EqualTo("get-1"));
            Assert.That(result.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
        }
    }

    [Test]
    public void RequiringSuccessConvertsPerCallErrorsToRichExceptions()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation(
            "error",
            "query-7",
            "{\"type\":\"serverUnavailable\",\"description\":\"Temporarily offline\",\"vendorTrace\":\"t-9\"}"));
        JmapCallResult result = mapper.FindRawResult(response, "query-7");

        JmapMethodException exception = Assert.Throws<JmapMethodException>(
            () => mapper.RequireSuccessfulArguments(result, JmapMethods.EmailQuery))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.MethodName, Is.EqualTo(JmapMethods.EmailQuery));
            Assert.That(exception.CallId, Is.EqualTo("query-7"));
            Assert.That(exception.ErrorToken, Is.EqualTo(JmapErrorCodes.ServerUnavailable));
            Assert.That(exception.Error, Is.SameAs(result.Error));
            Assert.That(exception.IsRetryable, Is.True);
            Assert.That(exception.StructuredData.GetProperty("vendorTrace").GetString(), Is.EqualTo("t-9"));
        }
    }

    [Test]
    public void UnknownMethodErrorsRemainUnknownAndAreNotRetryable()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation("error", "vendor-1", "{\"type\":\"vendorBusy\",\"wait\":5}"));
        JmapCallResult result = mapper.FindRawResult(response, "vendor-1");

        JmapMethodException exception = mapper.ConvertMethodError(result, "Vendor/run");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ErrorToken, Is.EqualTo("vendorBusy"));
            Assert.That(exception.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(exception.Error.UnknownProperties["wait"].GetInt32(), Is.EqualTo(5));
            Assert.That(exception.IsRetryable, Is.False);
        }
    }

    [Test]
    public void MappedArgumentsAreDetachedFromTheInputDocument()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapCallResult result;
        using (JsonDocument document = JsonDocument.Parse("{\"ids\":[\"m1\"]}"))
        {
            result = mapper.MapInvocation(new JmapInvocation
            {
                Name = JmapMethods.EmailGet,
                CallId = "get-1",
                Arguments = document.RootElement
            });
        }

        JsonElement successfulArguments = mapper.RequireSuccessfulArguments(result, JmapMethods.EmailGet);

        Assert.That(successfulArguments.GetProperty("ids")[0].GetString(), Is.EqualTo("m1"));
    }

    [Test]
    public void MissingCorrelationAndMismatchedTypedResponsesAreProtocolFailures()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation(JmapMethods.EmailGet, "get-1", "{\"list\":[]}"));

        JmapProtocolException missing = Assert.Throws<JmapProtocolException>(
            () => mapper.FindRawResult(response, "missing"))!;
        JmapProtocolException mismatched = Assert.Throws<JmapProtocolException>(
            () => mapper.FindTypedResult(response, "get-1", JmapMethods.EmailQuery))!;
        JmapProtocolException emptyCallId = Assert.Throws<JmapProtocolException>(
            () => mapper.FindRawResult(response, ""))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.Path, Is.EqualTo("methodResponses"));
            Assert.That(mismatched.Path, Is.EqualTo("methodResponses"));
            Assert.That(emptyCallId.Path, Is.EqualTo("methodResponses/2"));
        }
    }

    [Test]
    public void MalformedCorrelatedInvocationsAreRejected()
    {
        ResponseMapper mapper = new ResponseMapper();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(new JmapInvocation
        {
            Name = JmapMethods.EmailGet,
            CallId = "get-1",
            Arguments = Json("[]")
        });

        JmapProtocolException exception = Assert.Throws<JmapProtocolException>(
            () => mapper.FindRawResult(response, "get-1"))!;

        Assert.That(exception.Path, Is.EqualTo("methodResponses/1"));
    }

    private static JmapInvocation Invocation(string name, string callId, string arguments)
    {
        return new JmapInvocation
        {
            Name = name,
            CallId = callId,
            Arguments = Json(arguments)
        };
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
