using System.Text;

namespace http.Tests;

public class Http1RequestSerializationTests
{
    [Test]
    public void ContentlessRequestUsesCanonicalWireDelimiters()
    {
        HttpRequest request = Request("GET", "/items?q=1", HttpProtocolVersion.Http11);
        AddField(request.Headers, "Host", "example.test");
        AddField(request.Headers, "X-Test", "value");

        Http1SerializationResult result = Serialize(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
                "GET /items?q=1 HTTP/1.1\r\n" +
                "Host: example.test\r\n" +
                "X-Test: value\r\n\r\n"));
            Assert.That(result.BytesWritten, Is.EqualTo(result.Bytes.Length));
            Assert.That(result.RequiredSize, Is.EqualTo(result.Bytes.Length));
            Assert.That(result.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.None));
            Assert.That(result.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Persistent));
            Assert.That(request.Headers.Fields.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void ContentLengthIsGeneratedAndBinaryContentRoundTrips()
    {
        HttpRequest request = Request("POST", "/upload", HttpProtocolVersion.Http11);
        AddField(request.Headers, "Host", "example.test");
        request.Content = [0x00, 0x80, 0xff, 0x0a];

        Http1SerializationResult result = Serialize(request);
        Http1ParseResult parsed = new Http1Parser().ParseHttp1Bytes(result.Bytes);
        request.Content[0] = 0x7f;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(result.FramingDecision.DeclaredLength, Is.EqualTo(4));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Does.StartWith(
                "POST /upload HTTP/1.1\r\nHost: example.test\r\n" +
                "Content-Length: 4\r\n\r\n"));
            Assert.That(result.Bytes.TakeLast(4),
                Is.EqualTo(new byte[] { 0x00, 0x80, 0xff, 0x0a }));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Request!.Content,
                Is.EqualTo(new byte[] { 0x00, 0x80, 0xff, 0x0a }));
            Assert.That(request.Headers.Fields.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExistingContentLengthIsVerifiedWithoutRewritingFields()
    {
        HttpRequest valid = Request("POST", "/", HttpProtocolVersion.Http11);
        AddField(valid.Headers, "Host", "example.test");
        AddField(valid.Headers, "Content-Length", "3");
        valid.Content = "abc"u8.ToArray();
        HttpRequest invalid = Request("POST", "/", HttpProtocolVersion.Http11);
        AddField(invalid.Headers, "Host", "example.test");
        AddField(invalid.Headers, "Content-Length", "4");
        invalid.Content = "abc"u8.ToArray();

        Http1SerializationResult accepted = Serialize(valid);
        Http1SerializationResult rejected = Serialize(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(CountText(accepted.Bytes, "Content-Length: 3"), Is.EqualTo(1));
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("mismatched-http1-content-length"));
            Assert.That(rejected.Bytes, Is.Empty);
        });
    }

    [Test]
    public void ExplicitChunkedFramingProducesAParsableRequest()
    {
        HttpRequest request = Request("POST", "/stream", HttpProtocolVersion.Http11);
        AddField(request.Headers, "Host", "example.test");
        request.Content = "abcdefg"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = Http1BodyFraming.Chunked;
        options.ChunkSize = 3;

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(request, options);
        Http1ParseResult parsed = new Http1Parser().ParseHttp1Bytes(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
                "POST /stream HTTP/1.1\r\n" +
                "Host: example.test\r\n" +
                "Transfer-Encoding: chunked\r\n\r\n" +
                "3\r\nabc\r\n3\r\ndef\r\n1\r\ng\r\n0\r\n\r\n"));
            Assert.That(result.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Request!.Content,
                Is.EqualTo("abcdefg"u8.ToArray()));
        });
    }

    [TestCase("gzip")]
    [TestCase("gzip, chunked")]
    [TestCase("chunked, gzip")]
    public void UnsupportedTransferCodingsAreNeverEmittedImplicitly(string coding)
    {
        HttpRequest request = Request("POST", "/", HttpProtocolVersion.Http11);
        AddField(request.Headers, "Host", "example.test");
        AddField(request.Headers, "Transfer-Encoding", coding);
        request.Content = "content"u8.ToArray();

        Http1SerializationResult result = Serialize(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.ErrorCode, Is.Not.Null);
        });
    }

    [Test]
    public void TransferEncodingAndContentLengthConflictIsRejected()
    {
        HttpRequest request = Request("POST", "/", HttpProtocolVersion.Http11);
        AddField(request.Headers, "Host", "example.test");
        AddField(request.Headers, "Transfer-Encoding", "chunked");
        AddField(request.Headers, "Content-Length", "3");
        request.Content = "abc"u8.ToArray();

        Http1SerializationResult result = Serialize(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-transfer-encoding-content-length-conflict"));
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    [Test]
    public void StartLineAndFieldsRejectInjectionAndUnsupportedVersions()
    {
        HttpRequest badMethod = Request(
            "GET\r\nInjected:", "/", HttpProtocolVersion.Http11);
        AddField(badMethod.Headers, "Host", "example.test");
        HttpRequest badTarget = Request(
            "GET", "/one two", HttpProtocolVersion.Http11);
        AddField(badTarget.Headers, "Host", "example.test");
        HttpRequest nonAsciiMethod = Request(
            "GÉT", "/", HttpProtocolVersion.Http11);
        AddField(nonAsciiMethod.Headers, "Host", "example.test");
        HttpRequest badValue = Request("GET", "/", HttpProtocolVersion.Http11);
        AddField(badValue.Headers, "Host", "example.test");
        AddRawField(badValue.Headers, "X-Test", "ok\r\nInjected: yes"u8.ToArray());
        HttpRequest http2 = Request("GET", "/", HttpProtocolVersion.Http2);
        AddField(http2.Headers, "Host", "example.test");

        Http1SerializationResult methodResult = Serialize(badMethod);
        Http1SerializationResult targetResult = Serialize(badTarget);
        Http1SerializationResult nonAsciiMethodResult = Serialize(nonAsciiMethod);
        Http1SerializationResult valueResult = Serialize(badValue);
        Http1SerializationResult versionResult = Serialize(http2);

        Assert.Multiple(() =>
        {
            Assert.That(methodResult.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(targetResult.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(nonAsciiMethodResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(valueResult.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(versionResult.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(methodResult.Bytes, Is.Empty);
            Assert.That(targetResult.Bytes, Is.Empty);
            Assert.That(nonAsciiMethodResult.Bytes, Is.Empty);
            Assert.That(valueResult.Bytes, Is.Empty);
            Assert.That(versionResult.Bytes, Is.Empty);
        });
    }

    [Test]
    public void Http11RequiresOneValidHostThatMatchesTheTargetAuthority()
    {
        HttpRequest missing = Request("GET", "/", HttpProtocolVersion.Http11);
        HttpRequest duplicate = Request("GET", "/", HttpProtocolVersion.Http11);
        AddField(duplicate.Headers, "Host", "one.test");
        AddField(duplicate.Headers, "host", "two.test");
        HttpRequest conflict = Request(
            "GET", "http://one.test/path", HttpProtocolVersion.Http11);
        AddField(conflict.Headers, "Host", "two.test");
        HttpRequest equivalent = Request(
            "GET", "http://example.test:80/path", HttpProtocolVersion.Http11);
        AddField(equivalent.Headers, "Host", "EXAMPLE.TEST");

        Http1SerializationResult missingResult = Serialize(missing);
        Http1SerializationResult duplicateResult = Serialize(duplicate);
        Http1SerializationResult conflictResult = Serialize(conflict);
        Http1SerializationResult equivalentResult = Serialize(equivalent);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.ErrorCode,
                Is.EqualTo("missing-http1-host-field"));
            Assert.That(duplicateResult.ErrorCode,
                Is.EqualTo("multiple-http1-host-fields"));
            Assert.That(conflictResult.ErrorCode,
                Is.EqualTo("conflicting-http1-request-authority"));
            Assert.That(equivalentResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
        });
    }

    [Test]
    public void RequestTargetOptionsAndHttp10AreSerializedExplicitly()
    {
        HttpRequest proxy = Request("GET", "/resource", HttpProtocolVersion.Http11);
        proxy.Scheme = "https";
        proxy.Authority = "example.test";
        AddField(proxy.Headers, "Host", "example.test");
        Http1SerializationOptions proxyOptions = new Http1SerializationOptions();
        proxyOptions.UseAbsoluteRequestTarget = true;
        HttpRequest legacy = Request("GET", "/legacy", HttpProtocolVersion.Http10);

        Http1SerializationResult absolute = new Http1Serializer()
            .SerializeHttp1Request(proxy, proxyOptions);
        Http1SerializationResult http10 = Serialize(legacy);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(absolute.Bytes), Does.StartWith(
                "GET https://example.test/resource HTTP/1.1\r\n"));
            Assert.That(Encoding.ASCII.GetString(http10.Bytes),
                Is.EqualTo("GET /legacy HTTP/1.0\r\n\r\n"));
            Assert.That(http10.ConnectionDecision!.MustClose, Is.True);
        });
    }

    [Test]
    public void Http10RejectsExplicitChunkedFraming()
    {
        HttpRequest request = Request("POST", "/legacy", HttpProtocolVersion.Http10);
        request.Content = "body"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = Http1BodyFraming.Chunked;

        Http1SerializationResult result = new Http1Serializer()
            .SerializeHttp1Request(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-chunked-requires-http11"));
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    [Test]
    public void CasingAndCloseOptionsAffectOutputWithoutMutatingInput()
    {
        HttpRequest request = Request("GET", "/", HttpProtocolVersion.Http11);
        AddField(request.Headers, "hOsT", "example.test");
        AddField(request.Headers, "x-CUSTOM-name", "value");
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.PreserveFieldNameCasing = false;
        options.CloseConnection = true;

        Http1SerializationResult result = new Http1Serializer()
            .SerializeHttp1Request(request, options);
        string text = Encoding.ASCII.GetString(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Host: example.test\r\n"));
            Assert.That(text, Does.Contain("X-Custom-Name: value\r\n"));
            Assert.That(text, Does.Contain("Connection: close\r\n"));
            Assert.That(result.ConnectionDecision!.MustClose, Is.True);
            Assert.That(request.Headers.Fields[0].Name, Is.EqualTo("hOsT"));
            Assert.That(request.Headers.Fields[1].Name, Is.EqualTo("x-CUSTOM-name"));
            Assert.That(request.Headers.Fields.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void SerializationHonorsStartLineFieldAndContentLimits()
    {
        HttpRequest request = Request("POST", "/long", HttpProtocolVersion.Http11);
        AddField(request.Headers, "Host", "example.test");
        request.Content = "abc"u8.ToArray();
        Http1SerializationOptions startOptions = new Http1SerializationOptions();
        startOptions.Limits.MaximumStartLineSize = 10;
        Http1SerializationOptions fieldOptions = new Http1SerializationOptions();
        fieldOptions.Limits.MaximumFieldCount = 1;
        Http1SerializationOptions contentOptions = new Http1SerializationOptions();
        contentOptions.Limits.MaximumContentSize = 2;
        Http1Serializer serializer = new Http1Serializer();

        Http1SerializationResult start =
            serializer.SerializeHttp1Request(request, startOptions);
        Http1SerializationResult fields =
            serializer.SerializeHttp1Request(request, fieldOptions);
        Http1SerializationResult content =
            serializer.SerializeHttp1Request(request, contentOptions);

        Assert.Multiple(() =>
        {
            Assert.That(start.ErrorCode,
                Is.EqualTo("http1-start-line-limit-exceeded"));
            Assert.That(fields.ErrorCode,
                Is.EqualTo("http1-field-count-limit-exceeded"));
            Assert.That(content.ErrorCode,
                Is.EqualTo("http1-content-limit-exceeded"));
        });
    }

    [Test]
    public void FramingAndConnectionAnalysisMatchAllocatingSerialization()
    {
        HttpRequest request = Request("POST", "/", HttpProtocolVersion.Http11);
        AddField(request.Headers, "Host", "example.test");
        request.Content = "data"u8.ToArray();
        Http1Serializer serializer = new Http1Serializer();

        Http1FramingDecision framing = serializer.AnalyzeHttp1Framing(request);
        Http1ConnectionDecision connection =
            serializer.AnalyzeHttp1Connection(request);
        Http1SerializationResult serialized = serializer.SerializeHttp1Request(
            request,
            new Http1SerializationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(framing.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(framing.DeclaredLength, Is.EqualTo(4));
            Assert.That(connection.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Persistent));
            Assert.That(serialized.FramingDecision!.BodyFraming,
                Is.EqualTo(framing.BodyFraming));
            Assert.That(serialized.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(connection.ConnectionMode));
        });
    }

    static Http1SerializationResult Serialize(HttpRequest request)
    {
        return new Http1Serializer().SerializeHttp1Request(
            request,
            new Http1SerializationOptions());
    }

    static HttpRequest Request(
        string method,
        string target,
        HttpProtocolVersion version)
    {
        HttpRequest request = new HttpRequest();
        request.Method = method;
        request.Target = target;
        request.ProtocolVersion = version;
        return request;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        AddRawField(headers, name, Encoding.ASCII.GetBytes(value));
    }

    static void AddRawField(HttpHeaders headers, string name, byte[] value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = value;
        headers.Fields.Add(field);
    }

    static int CountText(byte[] bytes, string value)
    {
        string text = Encoding.ASCII.GetString(bytes);
        int count = 0;
        int position = 0;
        while (position <= text.Length - value.Length)
        {
            int found = text.IndexOf(value, position, StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }

            count = count + 1;
            position = found + value.Length;
        }

        return count;
    }
}
