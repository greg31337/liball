using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class ProtocolJsonCodecTests
{
    [Test]
    public void DecodeRequestPreservesInvocationOrderArbitraryArgumentsAndUnknownData()
    {
        ProtocolJsonCodec codec = Codec();
        JmapRequest request;

        using (JsonDocument document = JsonDocument.Parse(
            "{\"using\":[\"urn:ietf:params:jmap:core\",\"https://example.test/vendor\"]," +
            "\"methodCalls\":[" +
            "[\"Core/echo\",{\"answer\":42,\"nested\":{\"enabled\":false}},\"c1\"]," +
            "[\"Vendor/run\",{\"items\":[null,0,\"\"]},\"c2\"]]," +
            "\"vendorRequest\":{\"code\":17}}"))
        {
            request = codec.DecodeRequest(document.RootElement);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Using, Is.EqualTo(new[] { JmapCapabilities.Core, "https://example.test/vendor" }));
            Assert.That(request.MethodCalls, Has.Count.EqualTo(2));
            Assert.That(request.MethodCalls[0].Name, Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(request.MethodCalls[0].CallId, Is.EqualTo("c1"));
            Assert.That(request.MethodCalls[0].Arguments.GetProperty("answer").GetInt32(), Is.EqualTo(42));
            Assert.That(request.MethodCalls[0].Arguments.GetProperty("nested").GetProperty("enabled").GetBoolean(), Is.False);
            Assert.That(request.MethodCalls[1].Name, Is.EqualTo("Vendor/run"));
            Assert.That(request.MethodCalls[1].Arguments.GetProperty("items")[0].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(request.CreatedIds, Is.Null);
            Assert.That(request.UnknownProperties["vendorRequest"].GetProperty("code").GetInt32(), Is.EqualTo(17));
        }
    }

    [Test]
    public void CreatedIdsDistinguishOmissionEmptyAndPopulatedMaps()
    {
        JmapRequest omitted = DecodeRequest("{\"using\":[],\"methodCalls\":[]}");
        JmapRequest empty = DecodeRequest("{\"using\":[],\"methodCalls\":[],\"createdIds\":{}}");
        JmapRequest populated = DecodeRequest("{\"using\":[],\"methodCalls\":[],\"createdIds\":{\"draft\":\"M1\"}}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(omitted.CreatedIds, Is.Null);
            Assert.That(empty.CreatedIds, Is.Not.Null.And.Empty);
            Assert.That(populated.CreatedIds, Is.EqualTo(new Dictionary<string, string> { { "draft", "M1" } }));
        }

        using JsonDocument omittedJson = JsonDocument.Parse(EncodeRequest(omitted));
        using JsonDocument emptyJson = JsonDocument.Parse(EncodeRequest(empty));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(omittedJson.RootElement.TryGetProperty(JmapProperties.CreatedIds, out JsonElement _), Is.False);
            Assert.That(emptyJson.RootElement.GetProperty(JmapProperties.CreatedIds).GetRawText(), Is.EqualTo("{}"));
        }
    }

    [Test]
    public void DecodeResponseKeepsMultipleResponsesForTheOriginatingCallId()
    {
        JmapResponse response = DecodeResponse(
            "{\"methodResponses\":[" +
            "[\"Email/copy\",{\"created\":{\"k1\":{\"id\":\"M1\"}}},\"copy-1\"]," +
            "[\"Email/set\",{\"destroyed\":[\"M0\"]},\"copy-1\"]," +
            "[\"error\",{\"type\":\"serverFail\",\"vendorCode\":9},\"get-2\"]]," +
            "\"createdIds\":{\"draft\":\"M1\"},\"sessionState\":\"s2\",\"vendorResponse\":true}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses, Has.Count.EqualTo(3));
            Assert.That(response.MethodResponses.Select(item => item.Name), Is.EqualTo(new[] { JmapMethods.EmailCopy, JmapMethods.EmailSet, "error" }));
            Assert.That(response.MethodResponses[0].CallId, Is.EqualTo("copy-1"));
            Assert.That(response.MethodResponses[1].CallId, Is.EqualTo("copy-1"));
            Assert.That(response.MethodResponses[2].CallId, Is.EqualTo("get-2"));
            Assert.That(response.MethodResponses[2].Arguments.GetProperty("vendorCode").GetInt32(), Is.EqualTo(9));
            Assert.That(response.CreatedIds!["draft"], Is.EqualTo("M1"));
            Assert.That(response.SessionState, Is.EqualTo("s2"));
            Assert.That(response.UnknownProperties["vendorResponse"].GetBoolean(), Is.True);
        }
    }

    [Test]
    public void RequestEncodingIsDeterministicWhilePreservingCallAndCapabilityOrder()
    {
        JmapRequest request = new JmapRequest();
        request.Using.Add("https://z.example/capability");
        request.Using.Add(JmapCapabilities.Core);
        request.MethodCalls.Add(Invocation("Z/run", "c2", "{\"z\":2,\"a\":1}"));
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c1", "{\"text\":\"hello\"}"));
        request.CreatedIds = new Dictionary<string, string>();
        request.CreatedIds.Add("z", "M2");
        request.CreatedIds.Add("a", "M1");
        request.UnknownProperties.Add("zVendor", Json("2"));
        request.UnknownProperties.Add("aVendor", Json("1"));

        byte[] first = EncodeRequest(request);
        byte[] second = EncodeRequest(request);
        string json = Encoding.UTF8.GetString(first);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(json, Does.StartWith("{\"using\":[\"https://z.example/capability\",\"urn:ietf:params:jmap:core\"],\"methodCalls\":[[\"Z/run\",{\"a\":1,\"z\":2},\"c2\"],[\"Core/echo\",{\"text\":\"hello\"},\"c1\"]]"));
            Assert.That(json.IndexOf("\"a\":\"M1\"", StringComparison.Ordinal), Is.LessThan(json.IndexOf("\"z\":\"M2\"", StringComparison.Ordinal)));
            Assert.That(json.IndexOf("\"aVendor\"", StringComparison.Ordinal), Is.LessThan(json.IndexOf("\"zVendor\"", StringComparison.Ordinal)));
        }

        JmapRequest roundTrip = DecodeRequest(json);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.Using, Is.EqualTo(request.Using));
            Assert.That(roundTrip.MethodCalls.Select(item => item.Name), Is.EqualTo(new[] { "Z/run", JmapMethods.CoreEcho }));
            Assert.That(roundTrip.CreatedIds, Is.EqualTo(request.CreatedIds));
            Assert.That(roundTrip.UnknownProperties["zVendor"].GetInt32(), Is.EqualTo(2));
        }
    }

    [Test]
    public void ResponseEncodingRoundTripsRepeatedCallIdsAndOptionalCreatedIds()
    {
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(Invocation(JmapMethods.EmailCopy, "copy-1", "{\"copied\":{}}"));
        response.MethodResponses.Add(Invocation(JmapMethods.EmailSet, "copy-1", "{\"destroyed\":[]}"));
        response.SessionState = "s3";
        response.UnknownProperties.Add("extension", Json("{\"ok\":true}"));

        byte[] utf8 = EncodeResponse(response);
        JmapResponse roundTrip = DecodeResponse(Encoding.UTF8.GetString(utf8));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.MethodResponses, Has.Count.EqualTo(2));
            Assert.That(roundTrip.MethodResponses[0].CallId, Is.EqualTo("copy-1"));
            Assert.That(roundTrip.MethodResponses[1].CallId, Is.EqualTo("copy-1"));
            Assert.That(roundTrip.CreatedIds, Is.Null);
            Assert.That(roundTrip.SessionState, Is.EqualTo("s3"));
            Assert.That(roundTrip.UnknownProperties["extension"].GetProperty("ok").GetBoolean(), Is.True);
        }
    }

    [TestCase("[]", "/methodCalls/0")]
    [TestCase("[\"Core/echo\",{}]", "/methodCalls/0")]
    [TestCase("[\"Core/echo\",{},\"c1\",null]", "/methodCalls/0")]
    [TestCase("[1,{},\"c1\"]", "/methodCalls/0/0")]
    [TestCase("[\"Core/echo\",[],\"c1\"]", "/methodCalls/0/1")]
    [TestCase("[\"Core/echo\",{},null]", "/methodCalls/0/2")]
    public void DecodeRejectsInvalidInvocationTupleShapes(string invocation, string expectedPath)
    {
        JmapProtocolException exception = Assert.Throws<JmapProtocolException>(
            () => DecodeRequest("{\"using\":[],\"methodCalls\":[" + invocation + "]}"))!;

        Assert.That(exception.Path, Is.EqualTo(expectedPath));
    }

    [Test]
    public void DecodeRejectsInvalidRequiredCollectionsCreatedIdsAndSessionState()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => DecodeRequest("{\"using\":{},\"methodCalls\":[]}"),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(
                () => DecodeRequest("{\"using\":[],\"methodCalls\":{},\"createdIds\":{}}"),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(
                () => DecodeRequest("{\"using\":[],\"methodCalls\":[],\"createdIds\":null}"),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(
                () => DecodeResponse("{\"methodResponses\":[],\"sessionState\":null}"),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void EncodeRejectsIncompleteInvocationsAndResponses()
    {
        JmapInvocation missingName = new JmapInvocation();
        missingName.Arguments = Json("{}");
        missingName.CallId = "c1";

        JmapInvocation invalidArguments = new JmapInvocation();
        invalidArguments.Name = JmapMethods.CoreEcho;
        invalidArguments.Arguments = Json("[]");
        invalidArguments.CallId = "c1";

        JmapInvocation missingCallId = new JmapInvocation();
        missingCallId.Name = JmapMethods.CoreEcho;
        missingCallId.Arguments = Json("{}");

        JmapResponse missingState = new JmapResponse();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => EncodeInvocation(missingName), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => EncodeInvocation(invalidArguments), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => EncodeInvocation(missingCallId), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => EncodeResponse(missingState), Throws.TypeOf<JmapValidationException>());
        }
    }

    private static ProtocolJsonCodec Codec()
    {
        return new ProtocolJsonCodec(new JmapJsonReader(100000, 32));
    }

    private static JmapRequest DecodeRequest(string json)
    {
        JmapJsonReader reader = new JmapJsonReader(100000, 32);
        ProtocolJsonCodec codec = new ProtocolJsonCodec(reader);
        return codec.DecodeRequest(reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)));
    }

    private static JmapResponse DecodeResponse(string json)
    {
        JmapJsonReader reader = new JmapJsonReader(100000, 32);
        ProtocolJsonCodec codec = new ProtocolJsonCodec(reader);
        return codec.DecodeResponse(reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)));
    }

    private static byte[] EncodeRequest(JmapRequest request)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec().EncodeRequest(request, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    private static byte[] EncodeResponse(JmapResponse response)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec().EncodeResponse(response, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    private static byte[] EncodeInvocation(JmapInvocation invocation)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec().EncodeInvocation(invocation, writer, "/invocation");
        writer.DisposeWriter();
        return stream.ToArray();
    }

    private static JmapInvocation Invocation(string name, string callId, string arguments)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.Arguments = Json(arguments);
        invocation.CallId = callId;
        return invocation;
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
