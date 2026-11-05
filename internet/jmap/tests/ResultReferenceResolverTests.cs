using System.Text.Json;

namespace jmap.Tests;

public class ResultReferenceResolverTests
{
    [Test]
    public void ResolverSubstitutesReferencedArgumentsAndPreservesNormalArguments()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement arguments = Json("""
            {
              "accountId": "a1",
              "#ids": {
                "resultOf": "c1",
                "name": "Email/query",
                "path": "/ids"
              },
              "properties": ["subject"]
            }
            """);
        List<JmapInvocation> previous = new List<JmapInvocation>
        {
            Response("Email/query", "c1", "{\"ids\":[\"m1\",\"m2\"]}")
        };

        ResultReferenceResolution resolution = resolver.ResolveArguments(arguments, previous);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution.IsSuccess, Is.True);
            Assert.That(resolution.Error, Is.Null);
            Assert.That(resolution.Arguments.GetProperty("accountId").GetString(), Is.EqualTo("a1"));
            Assert.That(resolution.Arguments.GetProperty("ids").EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { "m1", "m2" }));
            Assert.That(resolution.Arguments.GetProperty("properties")[0].GetString(), Is.EqualTo("subject"));
            Assert.That(resolution.Arguments.TryGetProperty("#ids", out _), Is.False);
            Assert.That(arguments.TryGetProperty("#ids", out _), Is.True);
        }
    }

    [Test]
    public void ResolverSupportsWildcardProjectionFromPreviousResponseArguments()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement arguments = ReferenceArguments("threadIds", "c1", "Email/get", "/list/*/threadId");
        List<JmapInvocation> previous = new List<JmapInvocation>
        {
            Response("Email/get", "c1", "{\"list\":[{\"threadId\":\"t2\"},{\"threadId\":\"t1\"}]}")
        };

        ResultReferenceResolution resolution = resolver.ResolveArguments(arguments, previous);

        Assert.That(
            resolution.Arguments.GetProperty("threadIds").EnumerateArray().Select(value => value.GetString()),
            Is.EqualTo(new[] { "t2", "t1" }));
    }

    [Test]
    public void FirstResponseWithMatchingCallIdMustHaveTheExpectedName()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement arguments = ReferenceArguments("ids", "c1", "Email/query", "/ids");
        List<JmapInvocation> previous = new List<JmapInvocation>
        {
            Response("Email/set", "c1", "{\"ids\":[\"wrong\"]}"),
            Response("Email/query", "c1", "{\"ids\":[\"would-match\"]}")
        };

        ResultReferenceResolution resolution = resolver.ResolveArguments(arguments, previous);

        AssertInvalidResultReference(resolution);
    }

    [Test]
    public void FirstMatchingResponseSuppliesTheReferencedValue()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement arguments = ReferenceArguments("ids", "c1", "Email/query", "/ids");
        List<JmapInvocation> previous = new List<JmapInvocation>
        {
            Response("Email/query", "c1", "{\"ids\":[\"first\"]}"),
            Response("Email/query", "c1", "{\"ids\":[\"second\"]}")
        };

        ResultReferenceResolution resolution = resolver.ResolveArguments(arguments, previous);

        Assert.That(resolution.Arguments.GetProperty("ids")[0].GetString(), Is.EqualTo("first"));
    }

    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{\"resultOf\":\"c1\",\"name\":\"Email/query\"}")]
    [TestCase("{\"resultOf\":1,\"name\":\"Email/query\",\"path\":\"/ids\"}")]
    [TestCase("{\"resultOf\":\"c1\",\"name\":false,\"path\":\"/ids\"}")]
    [TestCase("{\"resultOf\":\"c1\",\"name\":\"Email/query\",\"path\":null}")]
    public void InvalidReferenceShapesGenerateInvalidResultReference(string referenceJson)
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement arguments = Json("{\"#ids\":" + referenceJson + "}");
        List<JmapInvocation> previous = new List<JmapInvocation>
        {
            Response("Email/query", "c1", "{\"ids\":[\"m1\"]}")
        };

        ResultReferenceResolution resolution = resolver.ResolveArguments(arguments, previous);

        AssertInvalidResultReference(resolution);
    }

    [Test]
    public void MissingCallNameMismatchAndBadPathGenerateInvalidResultReference()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        List<JmapInvocation> previous = new List<JmapInvocation>
        {
            Response("Email/query", "c1", "{\"ids\":[\"m1\"]}")
        };

        ResultReferenceResolution missingCall = resolver.ResolveArguments(
            ReferenceArguments("ids", "missing", "Email/query", "/ids"), previous);
        ResultReferenceResolution wrongName = resolver.ResolveArguments(
            ReferenceArguments("ids", "c1", "Email/get", "/ids"), previous);
        ResultReferenceResolution badPath = resolver.ResolveArguments(
            ReferenceArguments("ids", "c1", "Email/query", "/missing"), previous);
        ResultReferenceResolution malformedPath = resolver.ResolveArguments(
            ReferenceArguments("ids", "c1", "Email/query", "bad-path"), previous);

        using (Assert.EnterMultipleScope())
        {
            AssertInvalidResultReference(missingCall);
            AssertInvalidResultReference(wrongName);
            AssertInvalidResultReference(badPath);
            AssertInvalidResultReference(malformedPath);
        }
    }

    [Test]
    public void NormalAndReferencedFormsGenerateInvalidArguments()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement arguments = Json("""
            {
              "ids": ["direct"],
              "#ids": { "resultOf": "c1", "name": "Email/query", "path": "/ids" }
            }
            """);

        ResultReferenceResolution resolution = resolver.ResolveArguments(arguments, new List<JmapInvocation>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution.IsSuccess, Is.False);
            Assert.That(resolution.Error, Is.Not.Null);
            Assert.That(resolution.Error!.Type, Is.EqualTo(JmapErrorCodes.InvalidArguments));
            Assert.That(resolution.Error.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(resolution.Arguments.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
        }
    }

    [Test]
    public void AnyFailedReferencePreventsAllSubstitution()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement arguments = Json("""
            {
              "#ids": { "resultOf": "c1", "name": "Email/query", "path": "/ids" },
              "#threadIds": { "resultOf": "missing", "name": "Thread/query", "path": "/ids" }
            }
            """);
        List<JmapInvocation> previous = new List<JmapInvocation>
        {
            Response("Email/query", "c1", "{\"ids\":[\"m1\"]}")
        };

        ResultReferenceResolution resolution = resolver.ResolveArguments(arguments, previous);

        using (Assert.EnterMultipleScope())
        {
            AssertInvalidResultReference(resolution);
            Assert.That(resolution.Arguments.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
            Assert.That(arguments.TryGetProperty("#ids", out _), Is.True);
            Assert.That(arguments.TryGetProperty("#threadIds", out _), Is.True);
        }
    }

    [Test]
    public void ReferenceUnknownMembersArePreservedWhenDecoded()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement value = Json("""
            {
              "resultOf": "c1",
              "name": "Email/query",
              "path": "/ids",
              "vendorData": { "key": 7 }
            }
            """);

        JmapResultReference reference = resolver.DecodeReference(value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reference.ResultOf, Is.EqualTo("c1"));
            Assert.That(reference.Name, Is.EqualTo("Email/query"));
            Assert.That(reference.Path, Is.EqualTo("/ids"));
            Assert.That(reference.UnknownProperties["vendorData"].GetProperty("key").GetInt32(), Is.EqualTo(7));
        }
    }

    [Test]
    public void ResolvedArgumentsAreDetachedFromResponseStorage()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        ResultReferenceResolution resolution;
        using (JsonDocument document = JsonDocument.Parse("{\"ids\":[\"m1\"]}"))
        {
            JmapInvocation response = new JmapInvocation
            {
                Name = "Email/query",
                CallId = "c1",
                Arguments = document.RootElement
            };
            resolution = resolver.ResolveArguments(
                ReferenceArguments("ids", "c1", "Email/query", "/ids"),
                new List<JmapInvocation> { response });
        }

        Assert.That(resolution.Arguments.GetProperty("ids")[0].GetString(), Is.EqualTo("m1"));
    }

    [Test]
    public void NonObjectArgumentsAndUnavailableResponseHistoryReturnErrors()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();

        ResultReferenceResolution invalidArguments = resolver.ResolveArguments(Json("[]"), new List<JmapInvocation>());
        ResultReferenceResolution noHistory = resolver.ResolveArguments(
            ReferenceArguments("ids", "c1", "Email/query", "/ids"), null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalidArguments.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            AssertInvalidResultReference(noHistory);
        }
    }

    [Test]
    public void PlainArgumentsNeedNoResponseHistory()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        ResultReferenceResolution resolution = resolver.ResolveArguments(Json("{\"accountId\":\"a\"}"), null);

        Assert.That(resolution.IsSuccess, Is.True);
        Assert.That(resolution.Arguments.GetProperty("accountId").GetString(), Is.EqualTo("a"));
    }

    [Test]
    public void DuplicateArgumentMembersGenerateInvalidArguments()
    {
        ResultReferenceResolver resolver = new ResultReferenceResolver();
        JsonElement arguments = Json("{\"accountId\":\"a\",\"accountId\":\"b\"}");

        ResultReferenceResolution resolution = resolver.ResolveArguments(arguments, null);

        Assert.That(resolution.IsSuccess, Is.False);
        Assert.That(resolution.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
    }

    private static void AssertInvalidResultReference(ResultReferenceResolution resolution)
    {
        Assert.That(resolution.IsSuccess, Is.False);
        Assert.That(resolution.Error, Is.Not.Null);
        Assert.That(resolution.Error!.Type, Is.EqualTo(JmapErrorCodes.InvalidResultReference));
        Assert.That(resolution.Error.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidResultReference));
    }

    private static JmapInvocation Response(string name, string callId, string arguments)
    {
        return new JmapInvocation
        {
            Name = name,
            CallId = callId,
            Arguments = Json(arguments)
        };
    }

    private static JsonElement ReferenceArguments(string argumentName, string resultOf, string name, string path)
    {
        return Json(
            "{\"#" + argumentName + "\":{" +
            "\"resultOf\":\"" + resultOf + "\"," +
            "\"name\":\"" + name + "\"," +
            "\"path\":\"" + path + "\"}}");
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
