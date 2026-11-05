using System.Text;

namespace http.Tests;

public class Http1ResponseSerializationTests
{
    [Test]
    public void EmptyResponseUsesRegisteredReasonAndExactLengthFraming()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);

        Http1SerializationResult result = Serialize(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
                "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n"));
            Assert.That(result.BytesWritten, Is.EqualTo(result.Bytes.Length));
            Assert.That(result.RequiredSize, Is.EqualTo(result.Bytes.Length));
            Assert.That(result.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(result.FramingDecision.DeclaredLength, Is.Zero);
            Assert.That(result.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Persistent));
            Assert.That(response.Headers.Fields, Is.Empty);
        });
    }

    [Test]
    public void ExplicitReasonPhraseIsPreservedByteForByte()
    {
        HttpResponse response = Response(299);
        response.HasReasonPhrase = true;
        response.ReasonPhrase = [0x4f, 0x4b, 0x09, 0x80];

        Http1SerializationResult result = Serialize(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes.Take(17), Is.EqualTo(new byte[]
            {
                0x48, 0x54, 0x54, 0x50, 0x2f, 0x31, 0x2e, 0x31,
                0x20, 0x32, 0x39, 0x39, 0x20, 0x4f, 0x4b, 0x09, 0x80
            }));
            Assert.That(result.Bytes.Skip(17).Take(2), Is.EqualTo("\r\n"u8.ToArray()));
        });
    }

    [Test]
    public void InvalidStatusReasonVersionAndInformationalFlagAreRejected()
    {
        HttpResponse badStatus = Response(99);
        HttpResponse badReason = Response(HttpStatusCodes.Ok);
        badReason.HasReasonPhrase = true;
        badReason.ReasonPhrase = "OK\rInjected"u8.ToArray();
        HttpResponse badVersion = Response(HttpStatusCodes.Ok);
        badVersion.ProtocolVersion = HttpProtocolVersion.Http2;
        HttpResponse badFlag = Response(HttpStatusCodes.EarlyHints);
        badFlag.IsInformational = false;

        Http1SerializationResult statusResult = Serialize(badStatus);
        Http1SerializationResult reasonResult = Serialize(badReason);
        Http1SerializationResult versionResult = Serialize(badVersion);
        Http1SerializationResult flagResult = Serialize(badFlag);

        Assert.Multiple(() =>
        {
            Assert.That(statusResult.ErrorCode,
                Is.EqualTo("invalid-http1-status-code"));
            Assert.That(reasonResult.ErrorCode,
                Is.EqualTo("invalid-http1-reason-phrase"));
            Assert.That(versionResult.ErrorCode,
                Is.EqualTo("invalid-http1-version"));
            Assert.That(flagResult.ErrorCode,
                Is.EqualTo("inconsistent-http1-informational-status"));
            Assert.That(statusResult.Bytes, Is.Empty);
            Assert.That(reasonResult.Bytes, Is.Empty);
        });
    }

    [Test]
    public void BinaryContentGetsContentLengthAndRoundTrips()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        AddField(response.Headers, "X-Origin", "one");
        response.Content = [0x00, 0x80, 0xff, 0x0a];

        Http1SerializationResult result = Serialize(response);
        Http1ParseResult parsed = ParseResponse(result.Bytes, HttpMethods.Get);
        response.Content[0] = 0x7f;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Does.StartWith(
                "HTTP/1.1 200 OK\r\nX-Origin: one\r\nContent-Length: 4\r\n\r\n"));
            Assert.That(result.Bytes.TakeLast(4),
                Is.EqualTo(new byte[] { 0x00, 0x80, 0xff, 0x0a }));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Response!.Content,
                Is.EqualTo(new byte[] { 0x00, 0x80, 0xff, 0x0a }));
            Assert.That(response.Headers.Fields, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ExistingContentLengthIsVerifiedAndConflictsAreRejected()
    {
        HttpResponse valid = Response(HttpStatusCodes.Ok);
        AddField(valid.Headers, "Content-Length", "3");
        valid.Content = "abc"u8.ToArray();
        HttpResponse mismatch = Response(HttpStatusCodes.Ok);
        AddField(mismatch.Headers, "Content-Length", "4");
        mismatch.Content = "abc"u8.ToArray();
        HttpResponse conflict = Response(HttpStatusCodes.Ok);
        AddField(conflict.Headers, "Transfer-Encoding", "chunked");
        AddField(conflict.Headers, "Content-Length", "3");
        conflict.Content = "abc"u8.ToArray();

        Http1SerializationResult accepted = Serialize(valid);
        Http1SerializationResult mismatched = Serialize(mismatch);
        Http1SerializationResult conflicted = Serialize(conflict);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(CountText(accepted.Bytes, "Content-Length: 3"), Is.EqualTo(1));
            Assert.That(mismatched.ErrorCode,
                Is.EqualTo("mismatched-http1-content-length"));
            Assert.That(conflicted.ErrorCode,
                Is.EqualTo("http1-transfer-encoding-content-length-conflict"));
        });
    }

    [Test]
    public void ChunkedResponseIsPartitionedAndParsable()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.Content = "abcdefg"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = Http1BodyFraming.Chunked;
        options.ChunkSize = 3;

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Response(response, options);
        Http1ParseResult parsed = ParseResponse(result.Bytes, HttpMethods.Get);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
                "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n" +
                "3\r\nabc\r\n3\r\ndef\r\n1\r\ng\r\n0\r\n\r\n"));
            Assert.That(result.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Response!.Content,
                Is.EqualTo("abcdefg"u8.ToArray()));
        });
    }

    [TestCase("gzip")]
    [TestCase("gzip, chunked")]
    [TestCase("chunked, gzip")]
    public void UnsupportedResponseTransferCodingsAreNotApplied(string coding)
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        AddField(response.Headers, "Transfer-Encoding", coding);
        response.Content = "content"u8.ToArray();

        Http1SerializationResult result = Serialize(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.Not.Null);
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    [Test]
    public void InformationalAndNoContentResponsesNeverEmitFramingOrContent()
    {
        HttpResponse hints = Response(HttpStatusCodes.EarlyHints);
        AddField(hints.Headers, "Link", "</style.css>; rel=preload");
        HttpResponse noContent = Response(HttpStatusCodes.NoContent);
        HttpResponse resetContent = Response(HttpStatusCodes.ResetContent);
        HttpResponse invalid = Response(HttpStatusCodes.NoContent);
        AddField(invalid.Headers, "Content-Length", "0");

        Http1SerializationResult hintsResult = Serialize(hints);
        Http1SerializationResult noContentResult = Serialize(noContent);
        Http1SerializationResult resetContentResult = Serialize(resetContent);
        Http1SerializationResult invalidResult = Serialize(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(hintsResult.Bytes), Is.EqualTo(
                "HTTP/1.1 103 Early Hints\r\nLink: </style.css>; rel=preload\r\n\r\n"));
            Assert.That(hintsResult.FramingDecision!.HasNoBody, Is.True);
            Assert.That(Encoding.ASCII.GetString(noContentResult.Bytes), Is.EqualTo(
                "HTTP/1.1 204 No Content\r\n\r\n"));
            Assert.That(Encoding.ASCII.GetString(resetContentResult.Bytes), Is.EqualTo(
                "HTTP/1.1 205 Reset Content\r\nContent-Length: 0\r\n\r\n"));
            Assert.That(invalidResult.ErrorCode,
                Is.EqualTo("http1-response-framing-field-not-allowed"));
        });
    }

    [Test]
    public void HeadAndNotModifiedPreserveLengthMetadataWithoutBodyBytes()
    {
        HttpResponse head = Response(HttpStatusCodes.Ok);
        AddField(head.Headers, "Content-Length", "123");
        Http1SerializationOptions headOptions = new Http1SerializationOptions();
        headOptions.ResponseRequestMethod = HttpMethods.Head;
        HttpResponse notModified = Response(HttpStatusCodes.NotModified);
        AddField(notModified.Headers, "Content-Length", "456");

        Http1SerializationResult headResult =
            new Http1Serializer().SerializeHttp1Response(head, headOptions);
        Http1SerializationResult notModifiedResult = Serialize(notModified);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(headResult.Bytes), Is.EqualTo(
                "HTTP/1.1 200 OK\r\nContent-Length: 123\r\n\r\n"));
            Assert.That(headResult.FramingDecision!.HasNoBody, Is.True);
            Assert.That(Encoding.ASCII.GetString(notModifiedResult.Bytes), Is.EqualTo(
                "HTTP/1.1 304 Not Modified\r\nContent-Length: 456\r\n\r\n"));
            Assert.That(notModifiedResult.FramingDecision!.HasNoBody, Is.True);
        });
    }

    [Test]
    public void SuccessfulConnectEstablishesTunnelButFailureHasNormalContent()
    {
        HttpResponse connected = Response(HttpStatusCodes.Ok);
        Http1SerializationOptions connectOptions = new Http1SerializationOptions();
        connectOptions.ResponseRequestMethod = HttpMethods.Connect;
        HttpResponse denied = Response(HttpStatusCodes.Forbidden);
        denied.Content = "no"u8.ToArray();

        Http1SerializationResult connectedResult =
            new Http1Serializer().SerializeHttp1Response(connected, connectOptions);
        Http1SerializationResult deniedResult =
            new Http1Serializer().SerializeHttp1Response(denied, connectOptions);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(connectedResult.Bytes), Is.EqualTo(
                "HTTP/1.1 200 OK\r\n\r\n"));
            Assert.That(connectedResult.FramingDecision!.IsTunnel, Is.True);
            Assert.That(connectedResult.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Tunneled));
            Assert.That(Encoding.ASCII.GetString(deniedResult.Bytes), Is.EqualTo(
                "HTTP/1.1 403 Forbidden\r\nContent-Length: 2\r\n\r\nno"));
            Assert.That(deniedResult.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Persistent));
        });
    }

    [Test]
    public void CloseDelimitedResponseAddsCloseAndCompletesAtEndOfInput()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.Content = "until-close"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.AddContentLength = false;

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Response(response, options);
        Http1Parser parser = ResponseParser(HttpMethods.Get);
        Http1ParseResult partial = parser.ParseHttp1Bytes(result.Bytes);
        Http1ParseResult completed = parser.CompleteHttp1Input();

        Assert.Multiple(() =>
        {
            Assert.That(result.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.CloseDelimited));
            Assert.That(result.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Close));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
                "HTTP/1.1 200 OK\r\nConnection: close\r\n\r\nuntil-close"));
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Events[0].Response!.Content,
                Is.EqualTo("until-close"u8.ToArray()));
        });
    }

    [Test]
    public void CloseOptionAndUpgradeFieldsDriveConnectionDisposition()
    {
        HttpResponse closing = Response(HttpStatusCodes.Ok);
        Http1SerializationOptions closingOptions = new Http1SerializationOptions();
        closingOptions.CloseConnection = true;
        HttpResponse switching = Response(HttpStatusCodes.SwitchingProtocols);
        AddField(switching.Headers, "Connection", "Upgrade");
        AddField(switching.Headers, "Upgrade", "websocket");
        HttpResponse invalidSwitch = Response(HttpStatusCodes.SwitchingProtocols);

        Http1SerializationResult closeResult =
            new Http1Serializer().SerializeHttp1Response(closing, closingOptions);
        Http1SerializationResult upgradeResult = Serialize(switching);
        Http1SerializationResult invalidUpgradeResult = Serialize(invalidSwitch);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(closeResult.Bytes), Does.Contain(
                "Connection: close\r\n"));
            Assert.That(closeResult.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Close));
            Assert.That(upgradeResult.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Upgraded));
            Assert.That(upgradeResult.ConnectionDecision.ProtocolName,
                Is.EqualTo("websocket"));
            Assert.That(invalidUpgradeResult.ErrorCode,
                Is.EqualTo("invalid-http1-switching-protocols-response"));
        });
    }

    [Test]
    public void ResponseFieldsAreInjectionSafeAndCanBeCanonicallyCased()
    {
        HttpResponse badName = Response(HttpStatusCodes.Ok);
        AddField(badName.Headers, "Bad Name", "value");
        HttpResponse badValue = Response(HttpStatusCodes.Ok);
        AddRawField(badValue.Headers, "X-Test", "ok\r\nInjected: yes"u8.ToArray());
        HttpResponse canonical = Response(HttpStatusCodes.Ok);
        AddField(canonical.Headers, "x-miXED-name", "value");
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.PreserveFieldNameCasing = false;

        Http1SerializationResult nameResult = Serialize(badName);
        Http1SerializationResult valueResult = Serialize(badValue);
        Http1SerializationResult canonicalResult =
            new Http1Serializer().SerializeHttp1Response(canonical, options);

        Assert.Multiple(() =>
        {
            Assert.That(nameResult.ErrorCode, Is.EqualTo("invalid-http1-field"));
            Assert.That(valueResult.ErrorCode, Is.EqualTo("invalid-http1-field"));
            Assert.That(Encoding.ASCII.GetString(canonicalResult.Bytes), Does.Contain(
                "X-Mixed-Name: value\r\n"));
        });
    }

    static Http1SerializationResult Serialize(HttpResponse response)
    {
        return new Http1Serializer().SerializeHttp1Response(
            response,
            new Http1SerializationOptions());
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = statusCode;
        response.IsInformational = statusCode >= 100 && statusCode < 200;
        return response;
    }

    static Http1ParseResult ParseResponse(byte[] message, string requestMethod)
    {
        return ResponseParser(requestMethod).ParseHttp1Bytes(message);
    }

    static Http1Parser ResponseParser(string requestMethod)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = requestMethod;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser;
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
