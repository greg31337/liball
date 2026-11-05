using System.Text;
using websocket;

namespace websocket.Tests;

public class Http1WriterTests
{
    [Test]
    public void MeasuresAndWritesCanonicalRequestInFieldOrder()
    {
        var request = Request();
        request.RequestTarget = "/chat?room=blue%20sky";
        request.Fields = new[]
        {
            Field("Host", "example.test"),
            Field("Upgrade", "websocket"),
            Field("Connection", "Upgrade"),
            Field("X-Empty", ""),
            Field("Cookie", "first=1"),
            Field("Cookie", "second=2")
        };
        const string expected =
            "GET /chat?room=blue%20sky HTTP/1.1\r\n" +
            "Host: example.test\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "X-Empty: \r\n" +
            "Cookie: first=1\r\n" +
            "Cookie: second=2\r\n" +
            "\r\n";
        var destination = Enumerable.Repeat((byte)0xAA, expected.Length + 1).ToArray();

        var measurement = Http1Writer.MeasureRequest(request);
        var write = Http1Writer.WriteRequest(request, destination);

        Assert.Multiple(() =>
        {
            Assert.That(measurement.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(measurement.RequiredLength, Is.EqualTo(expected.Length));
            Assert.That(measurement.ProducedCount, Is.Zero);
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(write.RequiredLength, Is.EqualTo(expected.Length));
            Assert.That(write.ProducedCount, Is.EqualTo(expected.Length));
            Assert.That(Encoding.Latin1.GetString(destination, 0, write.ProducedCount), Is.EqualTo(expected));
            Assert.That(destination[expected.Length], Is.EqualTo(0xAA));
        });
    }

    [Test]
    public void WritesCanonicalResponseWithRequiredStatusSpaceAndNoReason()
    {
        var response = Response(101);
        response.Fields = new[]
        {
            Field("Upgrade", "websocket"),
            Field("Connection", "Upgrade")
        };
        const string expected =
            "HTTP/1.1 101 \r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "\r\n";
        var destination = new byte[expected.Length];

        var measurement = Http1Writer.MeasureResponse(response);
        var write = Http1Writer.WriteResponse(response, destination);

        Assert.Multiple(() =>
        {
            Assert.That(measurement.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(measurement.RequiredLength, Is.EqualTo(expected.Length));
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(write.ProducedCount, Is.EqualTo(expected.Length));
            Assert.That(Encoding.ASCII.GetString(destination), Is.EqualTo(expected));
        });
    }

    [Test]
    public void WritesReasonFieldsAndLengthDelimitedRejectionBody()
    {
        var response = Response(403);
        response.HasReasonPhrase = true;
        response.ReasonPhrase = "Forbidden";
        response.HasBody = true;
        response.Body = Encoding.ASCII.GetBytes("nope");
        response.Fields = new[]
        {
            Field("Content-Length", "4"),
            Field("Content-Type", "text/plain")
        };
        const string expected =
            "HTTP/1.1 403 Forbidden\r\n" +
            "Content-Length: 4\r\n" +
            "Content-Type: text/plain\r\n" +
            "\r\n" +
            "nope";
        var destination = new byte[expected.Length];

        var write = Http1Writer.WriteResponse(response, destination);

        Assert.Multiple(() =>
        {
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(write.RequiredLength, Is.EqualTo(expected.Length));
            Assert.That(write.ProducedCount, Is.EqualTo(expected.Length));
            Assert.That(Encoding.ASCII.GetString(destination), Is.EqualTo(expected));
        });
    }

    [Test]
    public void PreservesLatin1ReasonAndFieldOctets()
    {
        var response = Response(400);
        response.HasReasonPhrase = true;
        response.ReasonPhrase = "Bad\u0080";
        response.Fields = new[] { Field("X-Opaque", "\u00ff") };
        var destination = new byte[Http1Writer.MeasureResponse(response).RequiredLength];

        var result = Http1Writer.WriteResponse(response, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(destination, Does.Contain((byte)0x80));
            Assert.That(destination, Does.Contain((byte)0xFF));
        });
    }

    [Test]
    public void InsufficientDestinationsDoNotReceivePartialOutput()
    {
        var request = Request();
        request.Fields = new[] { Field("Host", "example.test") };
        var response = Response(101);
        var requestLength = Http1Writer.MeasureRequest(request).RequiredLength;
        var responseLength = Http1Writer.MeasureResponse(response).RequiredLength;
        var requestDestination = Enumerable.Repeat((byte)0xAA, requestLength - 1).ToArray();
        var responseDestination = Enumerable.Repeat((byte)0xBB, responseLength - 1).ToArray();

        var requestWrite = Http1Writer.WriteRequest(request, requestDestination);
        var responseWrite = Http1Writer.WriteResponse(response, responseDestination);

        Assert.Multiple(() =>
        {
            Assert.That(requestWrite.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(requestWrite.RequiredLength, Is.EqualTo(requestLength));
            Assert.That(requestWrite.ProducedCount, Is.Zero);
            Assert.That(requestDestination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, requestLength - 1)));
            Assert.That(responseWrite.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(responseWrite.RequiredLength, Is.EqualTo(responseLength));
            Assert.That(responseWrite.ProducedCount, Is.Zero);
            Assert.That(responseDestination, Is.EqualTo(Enumerable.Repeat((byte)0xBB, responseLength - 1)));
        });
    }

    [Test]
    public void WrittenRequestRoundTripsThroughIncrementalParser()
    {
        var request = Request();
        request.RequestTarget = "/chat?x=1";
        request.Fields = new[]
        {
            Field("Host", "example.test"),
            Field("Cookie", "one"),
            Field("Cookie", "two")
        };
        var destination = new byte[Http1Writer.MeasureRequest(request).RequiredLength];
        Http1Writer.WriteRequest(request, destination);
        var parser = new Http1RequestParser(1024, 8, 256);

        var parsed = parser.ReadRequest(destination);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(parsed.Request.Method, Is.EqualTo(request.Method));
            Assert.That(parsed.Request.RequestTarget, Is.EqualTo(request.RequestTarget));
            Assert.That(parsed.Request.HttpVersion, Is.EqualTo(request.HttpVersion));
            Assert.That(parsed.Request.Fields.Select(field => field.Name),
                Is.EqualTo(request.Fields.Select(field => field.Name)));
            Assert.That(parsed.Request.Fields.Select(field => field.Value),
                Is.EqualTo(request.Fields.Select(field => field.Value)));
        });
    }

    [Test]
    public void WrittenResponseHeaderRoundTripsAndLeavesBodyTrailing()
    {
        var response = Response(404);
        response.HasReasonPhrase = true;
        response.ReasonPhrase = "Not Found";
        response.HasBody = true;
        response.Body = Encoding.ASCII.GetBytes("missing");
        response.Fields = new[] { Field("Content-Length", "7") };
        var destination = new byte[Http1Writer.MeasureResponse(response).RequiredLength];
        Http1Writer.WriteResponse(response, destination);
        var parser = new Http1ResponseParser(1024, 8, 256);

        var parsed = parser.ReadResponse(destination);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(parsed.Response.StatusCode, Is.EqualTo(404));
            Assert.That(parsed.Response.ReasonPhrase, Is.EqualTo("Not Found"));
            Assert.That(destination.AsSpan(parsed.ConsumedCount).ToArray(), Is.EqualTo(response.Body));
        });
    }

    [Test]
    public void RejectsNullOrInconsistentRequestMetadata()
    {
        var wrongTransport = Request();
        wrongTransport.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        var invalidMethod = Request();
        invalidMethod.Method = "bad method";
        var nullMethod = Request();
        nullMethod.Method = null!;
        var missingTarget = Request();
        missingTarget.HasRequestTarget = false;
        var invalidTarget = Request();
        invalidTarget.RequestTarget = "*";
        var missingVersion = Request();
        missingVersion.HasHttpVersion = false;
        var invalidVersion = Request();
        invalidVersion.HttpVersion = "HTTP/1.10";
        var extendedMetadata = Request();
        extendedMetadata.HasScheme = true;
        extendedMetadata.Scheme = "https";
        var staleExtendedMetadata = Request();
        staleExtendedMetadata.Authority = "example.test";
        var nullFields = Request();
        nullFields.Fields = null!;

        var invalid = new WebSocketHttpRequest[]
        {
            null!,
            wrongTransport,
            invalidMethod,
            nullMethod,
            missingTarget,
            invalidTarget,
            missingVersion,
            invalidVersion,
            extendedMetadata,
            staleExtendedMetadata,
            nullFields
        };

        Assert.Multiple(() =>
        {
            foreach (var request in invalid)
            {
                var result = Http1Writer.MeasureRequest(request);
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
                Assert.That(result.RequiredLength, Is.Zero);
                Assert.That(result.Error.Identifier, Is.EqualTo("http-write-request-invalid"));
            }
        });
    }

    [Test]
    public void RejectsNullOrInconsistentResponseMetadata()
    {
        var wrongTransport = Response(200);
        wrongTransport.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        var lowStatus = Response(99);
        var highStatus = Response(600);
        var emptyPresentReason = Response(200);
        emptyPresentReason.HasReasonPhrase = true;
        var staleReason = Response(200);
        staleReason.ReasonPhrase = "OK";
        var unsafeReason = Response(200);
        unsafeReason.HasReasonPhrase = true;
        unsafeReason.ReasonPhrase = "bad\rreason";
        var nullReason = Response(200);
        nullReason.ReasonPhrase = null!;
        var nullFields = Response(200);
        nullFields.Fields = null!;
        var nullBody = Response(200);
        nullBody.Body = null!;
        var staleBody = Response(200);
        staleBody.Body = new byte[] { 1 };

        var invalid = new WebSocketHttpResponse[]
        {
            null!,
            wrongTransport,
            lowStatus,
            highStatus,
            emptyPresentReason,
            staleReason,
            unsafeReason,
            nullReason,
            nullFields,
            nullBody,
            staleBody
        };

        Assert.Multiple(() =>
        {
            foreach (var response in invalid)
            {
                var result = Http1Writer.MeasureResponse(response);
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
                Assert.That(result.Error.Identifier, Is.EqualTo("http-write-response-invalid"));
            }
        });
    }

    [Test]
    public void RejectsUnsafeFieldNamesValuesAndOuterWhitespaceWithoutWriting()
    {
        var nullEntry = RequestWithField(null!);
        var nullNameField = Field("X-Test", "value");
        nullNameField.Name = null!;
        var nullName = RequestWithField(nullNameField);
        var nullValueField = Field("X-Test", "value");
        nullValueField.Value = null!;
        var nullValue = RequestWithField(nullValueField);
        var emptyName = RequestWithField(Field("", "value"));
        var delimitedName = RequestWithField(Field("Bad Name", "value"));
        var injectedValue = RequestWithField(Field("X-Test", "safe\r\nInjected: yes"));
        var deleteValue = RequestWithField(Field("X-Test", "bad\u007fvalue"));
        var nonOctetValue = RequestWithField(Field("X-Test", "bad\u0100value"));
        var leadingWhitespace = RequestWithField(Field("X-Test", " value"));
        var trailingWhitespace = RequestWithField(Field("X-Test", "value\t"));
        var invalid = new[]
        {
            nullEntry,
            nullName,
            nullValue,
            emptyName,
            delimitedName,
            injectedValue,
            deleteValue,
            nonOctetValue,
            leadingWhitespace,
            trailingWhitespace
        };
        var destination = Enumerable.Repeat((byte)0xAA, 256).ToArray();

        Assert.Multiple(() =>
        {
            foreach (var request in invalid)
            {
                var result = Http1Writer.WriteRequest(request, destination);
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
                Assert.That(result.Error.Identifier, Is.EqualTo("http-write-field-invalid"));
            }

            Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, 256)));
        });
    }

    [TestCase("Host")]
    [TestCase("Sec-WebSocket-Key")]
    [TestCase("Sec-WebSocket-Version")]
    [TestCase("Origin")]
    public void RejectsRepeatedRequestSingletonFieldsCaseInsensitively(string fieldName)
    {
        var request = Request();
        request.Fields = new[]
        {
            Field(fieldName, "one"),
            Field(fieldName.ToUpperInvariant(), "two")
        };

        AssertContradiction(Http1Writer.MeasureRequest(request));
    }

    [TestCase("Sec-WebSocket-Accept")]
    [TestCase("Sec-WebSocket-Protocol")]
    public void RejectsRepeatedResponseSingletonFieldsCaseInsensitively(string fieldName)
    {
        var response = Response(101);
        response.Fields = new[]
        {
            Field(fieldName, "one"),
            Field(fieldName.ToUpperInvariant(), "two")
        };

        AssertContradiction(Http1Writer.MeasureResponse(response));
    }

    [Test]
    public void AllowsRepeatedListFieldsInTheirValidDirections()
    {
        var request = Request();
        request.Fields = new[]
        {
            Field("Sec-WebSocket-Protocol", "chat"),
            Field("sec-websocket-protocol", "superchat")
        };
        var response = Response(426);
        response.Fields = new[]
        {
            Field("Sec-WebSocket-Version", "13"),
            Field("sec-websocket-version", "14")
        };

        Assert.Multiple(() =>
        {
            Assert.That(Http1Writer.MeasureRequest(request).Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(Http1Writer.MeasureResponse(response).Status, Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [TestCase("Sec-WebSocket-Accept")]
    public void RejectsResponseOnlyFieldsInRequests(string fieldName)
    {
        var request = RequestWithField(Field(fieldName, "value"));

        AssertContradiction(Http1Writer.MeasureRequest(request));
    }

    [TestCase("Host")]
    [TestCase("Sec-WebSocket-Key")]
    [TestCase("Origin")]
    public void RejectsRequestOnlyFieldsInResponses(string fieldName)
    {
        var response = Response(101);
        response.Fields = new[] { Field(fieldName, "value") };

        AssertContradiction(Http1Writer.MeasureResponse(response));
    }

    [Test]
    public void RejectsTransferEncodingInEitherDirection()
    {
        var request = RequestWithField(Field("Transfer-Encoding", "chunked"));
        var response = Response(400);
        response.Fields = new[] { Field("transfer-encoding", "chunked") };

        AssertContradiction(Http1Writer.MeasureRequest(request));
        AssertContradiction(Http1Writer.MeasureResponse(response));
    }

    [TestCase("")]
    [TestCase("00")]
    [TestCase("01")]
    [TestCase("-1")]
    [TestCase("1, 1")]
    [TestCase("2147483648")]
    public void RejectsNonCanonicalContentLengths(string value)
    {
        var request = RequestWithField(Field("Content-Length", value));

        AssertContradiction(Http1Writer.MeasureRequest(request));
    }

    [Test]
    public void RequestAllowsOnlyZeroOrAbsentContentLength()
    {
        var absent = Request();
        var zero = RequestWithField(Field("Content-Length", "0"));
        var nonzero = RequestWithField(Field("Content-Length", "1"));
        var duplicate = Request();
        duplicate.Fields = new[]
        {
            Field("Content-Length", "0"),
            Field("content-length", "0")
        };

        Assert.Multiple(() =>
        {
            Assert.That(Http1Writer.MeasureRequest(absent).Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(Http1Writer.MeasureRequest(zero).Status, Is.EqualTo(WebSocketParseStatus.Complete));
        });
        AssertContradiction(Http1Writer.MeasureRequest(nonzero));
        AssertContradiction(Http1Writer.MeasureRequest(duplicate));
    }

    [Test]
    public void ResponseContentLengthMustExactlyDescribeBodyPresenceAndSize()
    {
        var missing = ResponseWithBody("data");
        var mismatch = ResponseWithBody("data");
        mismatch.Fields = new[] { Field("Content-Length", "3") };
        var exact = ResponseWithBody("data");
        exact.Fields = new[] { Field("Content-Length", "4") };
        var declaredWithoutBody = Response(400);
        declaredWithoutBody.Fields = new[] { Field("Content-Length", "4") };
        var zeroWithoutBody = Response(400);
        zeroWithoutBody.Fields = new[] { Field("Content-Length", "0") };

        AssertContradiction(Http1Writer.MeasureResponse(missing));
        AssertContradiction(Http1Writer.MeasureResponse(mismatch));
        Assert.That(Http1Writer.MeasureResponse(exact).Status, Is.EqualTo(WebSocketParseStatus.Complete));
        AssertContradiction(Http1Writer.MeasureResponse(declaredWithoutBody));
        Assert.That(Http1Writer.MeasureResponse(zeroWithoutBody).Status, Is.EqualTo(WebSocketParseStatus.Complete));
    }

    [TestCase(100)]
    [TestCase(101)]
    [TestCase(199)]
    [TestCase(204)]
    [TestCase(304)]
    public void RejectsBodiesForStatusesWithoutHttpMessageContent(int statusCode)
    {
        var response = ResponseWithBody("x");
        response.StatusCode = statusCode;
        response.Fields = new[] { Field("Content-Length", "1") };

        AssertContradiction(Http1Writer.MeasureResponse(response));
    }

    static WebSocketHttpRequest Request()
    {
        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Method = "GET",
            HasRequestTarget = true,
            RequestTarget = "/",
            HasHttpVersion = true,
            HttpVersion = "HTTP/1.1"
        };
    }

    static WebSocketHttpRequest RequestWithField(WebSocketHeaderField field)
    {
        var request = Request();
        request.Fields = new[] { field };
        return request;
    }

    static WebSocketHttpResponse Response(int statusCode)
    {
        return new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = statusCode
        };
    }

    static WebSocketHttpResponse ResponseWithBody(string body)
    {
        var response = Response(400);
        response.HasBody = true;
        response.Body = Encoding.ASCII.GetBytes(body);
        return response;
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }

    static void AssertContradiction(Http1WriteResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.RequiredLength, Is.Zero);
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.Identifier, Is.EqualTo("http-write-field-contradictory"));
            Assert.That(result.Error.Diagnostic, Does.Not.Contain("\r").And.Not.Contain("\n"));
        });
    }
}
