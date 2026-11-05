using System.Text.Json;

namespace jmap.Tests;

public class JmapRequestBuilderTests
{
    [Test]
    public void BuilderPreservesCallOrderAndDeduplicatesCapabilitiesByFirstUse()
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddRequestCapability(JmapCapabilities.Core);
        builder.AddRequestCapability(JmapCapabilities.Core);
        JmapRawMethodCall raw = RawCall("Vendor/run", "explicit", "{\"value\":1}");
        raw.RequiredCapabilities.Add("https://example.test/vendor");
        raw.RequiredCapabilities.Add(JmapCapabilities.Core);

        string firstId = builder.AddRawMethodCall(raw);
        string secondId = builder.AddTypedMethodCall(
            JmapMethods.EmailGet,
            Json("{\"accountId\":\"a\"}"),
            new List<string> { JmapCapabilities.Mail, JmapCapabilities.Core },
            null);
        JmapRequest request = builder.BuildJmapRequest();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstId, Is.EqualTo("explicit"));
            Assert.That(secondId, Is.EqualTo("s0"));
            Assert.That(request.Using, Is.EqualTo(new[]
            {
                JmapCapabilities.Core,
                "https://example.test/vendor",
                JmapCapabilities.Mail
            }));
            Assert.That(request.MethodCalls.Select(call => call.Name), Is.EqualTo(new[] { "Vendor/run", JmapMethods.EmailGet }));
            Assert.That(request.MethodCalls.Select(call => call.CallId), Is.EqualTo(new[] { "explicit", "s0" }));
        }
    }

    [Test]
    public void RawCallMergesResultReferencesAndPreservesUnknownReferenceData()
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddTypedMethodCall(
            JmapMethods.EmailQuery,
            Json("{\"accountId\":\"a\"}"),
            new List<string> { JmapCapabilities.Core, JmapCapabilities.Mail },
            "query-call");
        JmapRawMethodCall raw = RawCall(JmapMethods.EmailGet, null, "{\"accountId\":\"a\",\"properties\":[\"id\"]}");
        JmapResultReference reference = builder.CreateResultReference("query-call", JmapMethods.EmailQuery, "/ids");
        reference.UnknownProperties.Add("vendor", Json("{\"mode\":2}"));
        raw.ResultReferences.Add("#ids", reference);

        string callId = builder.AddRawMethodCall(raw);
        reference.Path = "/changed-after-add";
        raw.Arguments = Json("{\"accountId\":\"changed\"}");
        JmapRequest request = builder.BuildJmapRequest();
        JsonElement arguments = request.MethodCalls[1].Arguments;
        JsonElement encodedReference = arguments.GetProperty("#ids");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(callId, Is.EqualTo("s0"));
            Assert.That(request.MethodCalls.Select(call => call.CallId),
                Is.EqualTo(new[] { "query-call", "s0" }));
            Assert.That(arguments.GetProperty("accountId").GetString(), Is.EqualTo("a"));
            Assert.That(arguments.GetProperty("properties")[0].GetString(), Is.EqualTo("id"));
            Assert.That(encodedReference.GetProperty("resultOf").GetString(), Is.EqualTo("query-call"));
            Assert.That(encodedReference.GetProperty("name").GetString(), Is.EqualTo(JmapMethods.EmailQuery));
            Assert.That(encodedReference.GetProperty("path").GetString(), Is.EqualTo("/ids"));
            Assert.That(encodedReference.GetProperty("vendor").GetProperty("mode").GetInt32(), Is.EqualTo(2));
        }
    }

    [Test]
    public void GeneratedIdsSkipExplicitCollisionsAndResetToInitialState()
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        List<string> capabilities = new List<string> { JmapCapabilities.Core };

        string explicitId = builder.AddTypedMethodCall(JmapMethods.CoreEcho, Json("{}"), capabilities, "s0");
        string generatedId = builder.AddTypedMethodCall(JmapMethods.CoreEcho, Json("{}"), capabilities, null);

        Assert.That(
            () => builder.AddTypedMethodCall(JmapMethods.CoreEcho, Json("{}"), capabilities, "s0"),
            Throws.TypeOf<JmapValidationException>());
        Assert.That(builder.BuildJmapRequest().MethodCalls, Has.Count.EqualTo(2));

        builder.AddRequestCreatedId("draft", "M1");
        builder.ResetJmapRequestBuilder();
        JmapRequest reset = builder.BuildJmapRequest();
        string resetId = builder.AddTypedMethodCall(JmapMethods.CoreEcho, Json("{}"), capabilities, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(explicitId, Is.EqualTo("s0"));
            Assert.That(generatedId, Is.EqualTo("s1"));
            Assert.That(reset.Using, Is.Empty);
            Assert.That(reset.MethodCalls, Is.Empty);
            Assert.That(reset.CreatedIds, Is.Null);
            Assert.That(resetId, Is.EqualTo("s0"));
            Assert.That(builder.BuildJmapRequest().MethodCalls, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void BuildReturnsDetachedSnapshotsIncludingOptionalCreatedIds()
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddRequestCapability(JmapCapabilities.Core);
        builder.AddTypedMethodCall(JmapMethods.CoreEcho, Json("{\"value\":1}"), new List<string>(), null);
        JmapRequest withoutCreatedIds = builder.BuildJmapRequest();

        builder.AddRequestCreatedId("draft", "M1");
        builder.AddRequestCreatedId("draft", "M2");
        JmapRequest first = builder.BuildJmapRequest();
        first.Using.Clear();
        first.MethodCalls.Clear();
        first.CreatedIds!["draft"] = "changed";
        builder.AddTypedMethodCall(JmapMethods.CoreEcho, Json("{\"value\":2}"), new List<string>(), null);
        JmapRequest second = builder.BuildJmapRequest();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutCreatedIds.CreatedIds, Is.Null);
            Assert.That(withoutCreatedIds.MethodCalls, Has.Count.EqualTo(1));
            Assert.That(second.Using, Is.EqualTo(new[] { JmapCapabilities.Core }));
            Assert.That(second.MethodCalls, Has.Count.EqualTo(2));
            Assert.That(second.MethodCalls[0].Arguments.GetProperty("value").GetInt32(), Is.EqualTo(1));
            Assert.That(second.CreatedIds, Is.EqualTo(new Dictionary<string, string> { { "draft", "M2" } }));
        }
    }

    [Test]
    public void InvalidInputsFailWithoutMutatingTheBuilder()
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        JmapRawMethodCall conflicting = RawCall(JmapMethods.EmailGet, null, "{\"ids\":[]}");
        JmapResultReference conflictReference = new JmapResultReference
        {
            ResultOf = "query",
            Name = JmapMethods.EmailQuery,
            Path = "/ids"
        };
        conflicting.ResultReferences.Add(
            "#ids",
            conflictReference);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => builder.AddRawMethodCall(null!), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRequestCapability("relative"), Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => builder.AddTypedMethodCall("bad", Json("{}"), new List<string>(), null),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => builder.AddTypedMethodCall(JmapMethods.CoreEcho, Json("[]"), new List<string>(), null),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => builder.AddTypedMethodCall(
                    JmapMethods.CoreEcho, Json("{}"), new List<string> { "relative" }, null),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => builder.AddTypedMethodCall(
                    JmapMethods.CoreEcho, Json("{}"), new List<string>(), ""),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRawMethodCall(conflicting), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRequestCreatedId("#draft", "M1"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRequestCreatedId("draft", "bad id"), Throws.TypeOf<JmapValidationException>());
        }

        JmapRequest request = builder.BuildJmapRequest();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Using, Is.Empty);
            Assert.That(request.MethodCalls, Is.Empty);
            Assert.That(request.CreatedIds, Is.Null);
        }
    }

    [Test]
    public void ReferenceHelperValidatesCallNameAndPointer()
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddTypedMethodCall(
            JmapMethods.EmailQuery,
            Json("{}"),
            new List<string> { JmapCapabilities.Core, JmapCapabilities.Mail },
            "c1");

        JmapResultReference reference = builder.CreateResultReference("c1", JmapMethods.EmailQuery, "/ids/*");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reference.ResultOf, Is.EqualTo("c1"));
            Assert.That(reference.Name, Is.EqualTo(JmapMethods.EmailQuery));
            Assert.That(reference.Path, Is.EqualTo("/ids/*"));
            Assert.That(() => builder.CreateResultReference("", JmapMethods.EmailQuery, "/ids"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.CreateResultReference("missing", JmapMethods.EmailQuery, "/ids"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.CreateResultReference("c1", "bad", "/ids"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.CreateResultReference("c1", JmapMethods.EmailQuery, "ids"), Throws.TypeOf<JmapValidationException>());
        }
    }

    [Test]
    public void RawCallValidationRejectsMalformedCapabilityAndReferenceStorage()
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddTypedMethodCall(
            JmapMethods.EmailQuery, Json("{}"),
            new List<string> { JmapCapabilities.Core }, "query");
        JmapResultReference valid = builder.CreateResultReference(
            "query", JmapMethods.EmailQuery, "/ids");

        JmapRawMethodCall nullReferences = RawCall(JmapMethods.EmailGet, null, "{}");
        nullReferences.ResultReferences = null!;
        JmapRawMethodCall duplicateArguments = RawCall(
            JmapMethods.EmailGet, null, "{\"ids\":[],\"ids\":[]}");
        JmapRawMethodCall malformedName = RawCall(JmapMethods.EmailGet, null, "{}");
        malformedName.ResultReferences["ids"] = valid;
        JmapRawMethodCall directConflict = RawCall(
            JmapMethods.EmailGet, null, "{\"#ids\":null}");
        directConflict.ResultReferences["#ids"] = valid;
        JmapRawMethodCall nullUnknown = RawCall(JmapMethods.EmailGet, null, "{}");
        nullUnknown.ResultReferences["#ids"] = ReferenceWithUnknown(null, default(JsonElement));
        string[] standardNames =
        {
            JmapProperties.ResultOf, JmapProperties.Name, JmapProperties.Path
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => builder.AddTypedMethodCall(
                JmapMethods.CoreEcho, Json("{}"), null!, null),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.ValidateReferenceArgument(
                null!, valid, new Dictionary<string, JsonElement>()),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRawMethodCall(nullReferences),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRawMethodCall(duplicateArguments),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRawMethodCall(malformedName),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRawMethodCall(directConflict),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => builder.AddRawMethodCall(nullUnknown),
                Throws.TypeOf<JmapValidationException>());
            foreach (string standardName in standardNames)
            {
                JmapRawMethodCall conflict = RawCall(JmapMethods.EmailGet, null, "{}");
                conflict.ResultReferences["#ids"] = ReferenceWithUnknown(
                    standardName, Json("true"));
                Assert.That(() => builder.AddRawMethodCall(conflict),
                    Throws.TypeOf<JmapValidationException>());
            }
            Assert.That(builder.BuildJmapRequest().MethodCalls, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void RawResultReferencesMustTargetEarlierCallsAndStayWithinTheSafetyLimit()
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddTypedMethodCall(
            JmapMethods.EmailQuery,
            Json("{}"),
            new List<string> { JmapCapabilities.Core },
            "query");
        JmapRawMethodCall missing = RawCall(JmapMethods.EmailGet, null, "{}");
        missing.ResultReferences.Add("#ids", new JmapResultReference
        {
            ResultOf = "later",
            Name = JmapMethods.EmailQuery,
            Path = "/ids"
        });

        JmapValidationException missingError = Assert.Throws<JmapValidationException>(
            () => builder.AddRawMethodCall(missing))!;

        JmapRawMethodCall bounded = RawCall(JmapMethods.EmailGet, null, "{}");
        JmapResultReference reference = builder.CreateResultReference(
            "query", JmapMethods.EmailQuery, "/ids");
        int index = 0;
        while (index < 256)
        {
            bounded.ResultReferences.Add("#value" + index, reference);
            index = index + 1;
        }

        string boundedId = builder.AddRawMethodCall(bounded);
        bounded.ResultReferences.Add("#value256", reference);
        JmapValidationException limitError = Assert.Throws<JmapValidationException>(
            () => builder.AddRawMethodCall(bounded))!;
        JmapRequest request = builder.BuildJmapRequest();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missingError.InvalidProperties,
                Is.EqualTo(new[] { "methodCalls/resultReferences/#ids" }));
            Assert.That(limitError.InvalidProperties,
                Is.EqualTo(new[] { "methodCalls/resultReferences" }));
            Assert.That(boundedId, Is.EqualTo("s0"));
            Assert.That(request.MethodCalls, Has.Count.EqualTo(2));
            Assert.That(request.MethodCalls[1].Arguments.EnumerateObject().Count(), Is.EqualTo(256));
        }
    }

    private static JmapRawMethodCall RawCall(string name, string? callId, string arguments)
    {
        JmapRawMethodCall call = new JmapRawMethodCall();
        call.Name = name;
        call.CallId = callId;
        call.Arguments = Json(arguments);
        return call;
    }

    private static JmapResultReference ReferenceWithUnknown(
        string? name, JsonElement value)
    {
        JmapResultReference reference = new JmapResultReference
        {
            ResultOf = "query",
            Name = JmapMethods.EmailQuery,
            Path = "/ids"
        };
        if (name == null)
        {
            reference.UnknownProperties = null!;
        }
        else
        {
            reference.UnknownProperties[name] = value;
        }
        return reference;
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
