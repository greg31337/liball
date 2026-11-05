using System.Text.Json;

namespace jmap.Tests;

public class CoreEchoMethodTests
{
    [Test]
    public void ProcessingEchoesArbitraryArgumentsAndInvocationIdentity()
    {
        CoreEchoMethod method = new CoreEchoMethod();
        JsonElement arguments = Json(
            "{\"text\":\"hello\",\"enabled\":true,\"nothing\":null," +
            "\"number\":1.2300e+4,\"items\":[0,false,{\"vendor:key\":\"value\"}]}");
        JmapInvocation call = Invocation(JmapMethods.CoreEcho, "echo-7", arguments);

        JmapInvocation response = method.ProcessInvocation(call);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Name, Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(response.CallId, Is.EqualTo("echo-7"));
            Assert.That(JsonElement.DeepEquals(response.Arguments, arguments), Is.True);
            Assert.That(response.Arguments.GetProperty("number").GetRawText(), Is.EqualTo("1.2300e+4"));
            Assert.That(response.Arguments.GetProperty("items")[2].GetProperty("vendor:key").GetString(), Is.EqualTo("value"));
        }
    }

    [Test]
    public void ProcessingReturnsArgumentsDetachedFromRequestStorage()
    {
        CoreEchoMethod method = new CoreEchoMethod();
        JmapInvocation response;
        using (JsonDocument document = JsonDocument.Parse("{\"nested\":{\"answer\":42}}"))
        {
            response = method.ProcessInvocation(Invocation(JmapMethods.CoreEcho, "echo-1", document.RootElement));
        }

        Assert.That(response.Arguments.GetProperty("nested").GetProperty("answer").GetInt32(), Is.EqualTo(42));
    }

    [Test]
    public void EmptyArgumentsAreEchoedWithoutAddingProtocolMembers()
    {
        CoreEchoMethod method = new CoreEchoMethod();

        JmapInvocation response = method.ProcessInvocation(
            Invocation(JmapMethods.CoreEcho, "echo-empty", Json("{}")));

        Assert.That(response.Arguments.EnumerateObject(), Is.Empty);
    }

    [Test]
    public void ClientMappingSelectsTheEchoResponseByCallIdAndName()
    {
        CoreEchoMethod method = new CoreEchoMethod();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation(JmapMethods.EmailSet, "echo-1", Json("{\"destroyed\":[]}")));
        response.MethodResponses.Add(Invocation(JmapMethods.CoreEcho, "other", Json("{\"wrong\":true}")));
        response.MethodResponses.Add(Invocation(JmapMethods.CoreEcho, "echo-1", Json("{\"answer\":42}")));

        JsonElement result = method.MapResponse(response, "echo-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetProperty("answer").GetInt32(), Is.EqualTo(42));
            Assert.That(result.TryGetProperty("destroyed", out JsonElement _), Is.False);
        }
    }

    [Test]
    public void ClientMappingConvertsACorrelatedMethodError()
    {
        CoreEchoMethod method = new CoreEchoMethod();
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation(
            "error",
            "echo-1",
            Json("{\"type\":\"serverUnavailable\",\"description\":\"Try later\",\"retryAfter\":10}")));

        JmapMethodException exception = Assert.Throws<JmapMethodException>(
            () => method.MapResponse(response, "echo-1"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.MethodName, Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(exception.CallId, Is.EqualTo("echo-1"));
            Assert.That(exception.ErrorToken, Is.EqualTo(JmapErrorCodes.ServerUnavailable));
            Assert.That(exception.Error!.Description, Is.EqualTo("Try later"));
            Assert.That(exception.StructuredData.GetProperty("retryAfter").GetInt32(), Is.EqualTo(10));
            Assert.That(exception.IsRetryable, Is.True);
        }
    }

    [Test]
    public void InvalidInvocationShapeIsRejectedAtTheRelevantTuplePosition()
    {
        CoreEchoMethod method = new CoreEchoMethod();
        JmapInvocation wrongName = Invocation(JmapMethods.EmailGet, "echo-1", Json("{}"));
        JmapInvocation wrongArguments = Invocation(JmapMethods.CoreEcho, "echo-1", Json("[]"));
        JmapInvocation missingCallId = Invocation(JmapMethods.CoreEcho, null, Json("{}"));

        JmapValidationException nullCall = Assert.Throws<JmapValidationException>(
            () => method.ProcessInvocation(null))!;
        JmapValidationException name = Assert.Throws<JmapValidationException>(
            () => method.ProcessInvocation(wrongName))!;
        JmapValidationException arguments = Assert.Throws<JmapValidationException>(
            () => method.ProcessInvocation(wrongArguments))!;
        JmapValidationException callId = Assert.Throws<JmapValidationException>(
            () => method.ProcessInvocation(missingCallId))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nullCall.InvalidProperties, Is.EqualTo(new[] { "methodCalls/0" }));
            Assert.That(name.InvalidProperties, Is.EqualTo(new[] { "methodCalls/0/0" }));
            Assert.That(arguments.InvalidProperties, Is.EqualTo(new[] { "methodCalls/0/1" }));
            Assert.That(callId.InvalidProperties, Is.EqualTo(new[] { "methodCalls/0/2" }));
            Assert.That(arguments.StructuredData.ValueKind, Is.EqualTo(JsonValueKind.Array));
        }
    }

    private static JmapInvocation Invocation(string name, string? callId, JsonElement arguments)
    {
        return new JmapInvocation
        {
            Name = name,
            CallId = callId,
            Arguments = arguments
        };
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
