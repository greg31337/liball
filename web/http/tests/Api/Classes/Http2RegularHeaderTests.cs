using System.Text;

namespace http.Tests;

public class Http2RegularHeaderTests
{
    [Test]
    public void RegularNamesAndValuesUseHttp2WireSyntax()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult uppercase = DecodeRequest(
            codec, Field("X-Test", "value"));
        Http2MessageResult badName = DecodeRequest(
            codec, Field("bad name", "value"));
        Http2MessageResult leadingSpace = DecodeRequest(
            codec, Field("x-test", " value"));
        Http2MessageResult trailingTab = DecodeRequest(
            codec, Field("x-test", "value\t"));
        HttpHeaderField nulField = Field("x-test", "value");
        nulField.Value = new byte[] { (byte)'a', 0, (byte)'b' };
        Http2MessageResult nul = DecodeRequest(codec, nulField);

        Assert.Multiple(() =>
        {
            AssertMalformed(uppercase, "invalid-http2-field-name");
            AssertMalformed(badName, "invalid-http2-field-name");
            AssertMalformed(leadingSpace, "invalid-http2-field-value");
            AssertMalformed(trailingTab, "invalid-http2-field-value");
            AssertMalformed(nul, "invalid-http2-field-value");
        });
    }

    [Test]
    public void ConnectionSpecificFieldsAreRejectedInRequestsAndResponses()
    {
        string[] names =
        {
            "connection",
            "proxy-connection",
            "keep-alive",
            "transfer-encoding",
            "upgrade"
        };
        Http2MessageCodec codec = new Http2MessageCodec();

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < names.Length)
            {
                Http2MessageResult request = DecodeRequest(
                    codec, Field(names[index], "value"));
                Http2MessageResult response = codec.DecodeHttp2ResponseHeaders(
                    3 + index * 2,
                    Headers(
                        Field(":status", "200"),
                        Field(names[index], "value")),
                    true);
                AssertMalformed(
                    request, "http2-connection-specific-field");
                AssertMalformed(
                    response, "http2-connection-specific-field");
                index = index + 1;
            }
        });
    }

    [Test]
    public void TeAllowsOnlyTrailersInRequestsAndIsForbiddenInResponses()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult trailers = DecodeRequest(
            codec, Field("te", "Trailers"));
        string[] invalidValues =
        {
            "gzip",
            "trailers, gzip",
            "trailers;q=1",
            ""
        };
        Http2MessageResult response = codec.DecodeHttp2ResponseHeaders(
            21,
            Headers(Field(":status", "200"), Field("te", "trailers")),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(trailers.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            int index = 0;
            while (index < invalidValues.Length)
            {
                Http2MessageResult invalid = DecodeRequest(
                    codec, Field("te", invalidValues[index]));
                AssertMalformed(invalid, "invalid-http2-te-field");
                index = index + 1;
            }

            AssertMalformed(response, "invalid-http2-te-field");
        });
    }

    [Test]
    public void EquivalentHostAndAuthorityValuesAreAcceptedAfterNormalization()
    {
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2RequestHeaders(
                1,
                Headers(
                    Field(":method", "GET"),
                    Field(":scheme", "https"),
                    Field(":authority", "EXAMPLE.test:443"),
                    Field(":path", "/"),
                    Field("host", "example.test")),
                true,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Request!.Authority,
                Is.EqualTo("EXAMPLE.test:443"));
            Assert.That(result.Request.Headers.Fields, Has.Count.EqualTo(1));
            Assert.That(result.Request.Headers.Fields[0].Name,
                Is.EqualTo("host"));
        });
    }

    [Test]
    public void HostCanSupplyMissingAuthorityButCannotConflictOrRepeat()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult hostFallback = codec.DecodeHttp2RequestHeaders(
            1,
            Headers(
                Field(":method", "GET"),
                Field(":scheme", "https"),
                Field(":path", "/"),
                Field("host", "example.test")),
            true,
            false);
        Http2MessageResult missing = codec.DecodeHttp2RequestHeaders(
            3,
            Headers(
                Field(":method", "GET"),
                Field(":scheme", "https"),
                Field(":path", "/")),
            true,
            false);
        Http2MessageResult conflicting = codec.DecodeHttp2RequestHeaders(
            5,
            Headers(
                Field(":method", "GET"),
                Field(":scheme", "https"),
                Field(":authority", "one.example"),
                Field(":path", "/"),
                Field("host", "two.example")),
            true,
            false);
        Http2MessageResult repeated = codec.DecodeHttp2RequestHeaders(
            7,
            Headers(
                Field(":method", "GET"),
                Field(":scheme", "https"),
                Field(":authority", "example.test"),
                Field(":path", "/"),
                Field("host", "example.test"),
                Field("host", "example.test")),
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(hostFallback.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(hostFallback.Request!.Authority, Is.Null);
            AssertMalformed(missing, "missing-http2-request-authority");
            AssertMalformed(conflicting, "conflicting-http2-authority");
            AssertMalformed(repeated, "multiple-http2-host-fields");
        });
    }

    [Test]
    public void MalformedHostValuesAreRejected()
    {
        string[] values =
        {
            "",
            "user@example.test",
            "example.test:",
            "example.test:70000"
        };
        Http2MessageCodec codec = new Http2MessageCodec();

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < values.Length)
            {
                Http2MessageResult result = codec.DecodeHttp2RequestHeaders(
                    1,
                    Headers(
                        Field(":method", "GET"),
                        Field(":scheme", "https"),
                        Field(":path", "/"),
                        Field("host", values[index])),
                    true,
                    false);
                AssertMalformed(result, "invalid-http2-host-field");
                index = index + 1;
            }
        });
    }

    [Test]
    public void HeaderCountValueAndRfcListSizeLimitsAreEnforced()
    {
        Http2MessageCodec exactCodec = new Http2MessageCodec();
        exactCodec.limits.MaximumHeaderListSize = 42;
        exactCodec.limits.MaximumFieldCount = 1;
        exactCodec.limits.MaximumFieldValueSize = 3;
        Http2MessageResult exact = exactCodec.DecodeHttp2ResponseHeaders(
            41, Headers(Field(":status", "200")), true);

        Http2MessageCodec listCodec = new Http2MessageCodec();
        listCodec.limits.MaximumHeaderListSize = 41;
        Http2MessageResult list = listCodec.DecodeHttp2ResponseHeaders(
            43, Headers(Field(":status", "200")), true);

        Http2MessageCodec countCodec = new Http2MessageCodec();
        countCodec.limits.MaximumFieldCount = 0;
        Http2MessageResult count = countCodec.DecodeHttp2ResponseHeaders(
            45, Headers(Field(":status", "200")), true);

        Http2MessageCodec valueCodec = new Http2MessageCodec();
        valueCodec.limits.MaximumFieldValueSize = 2;
        Http2MessageResult value = valueCodec.DecodeHttp2ResponseHeaders(
            47, Headers(Field(":status", "200")), true);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(HttpOperationStatus.Done));
            AssertMalformed(
                list, "http2-header-list-size-limit-exceeded");
            AssertMalformed(
                count, "http2-header-count-limit-exceeded");
            AssertMalformed(
                value, "http2-field-value-limit-exceeded");
        });
    }

    static Http2MessageResult DecodeRequest(
        Http2MessageCodec codec,
        HttpHeaderField field)
    {
        return codec.DecodeHttp2RequestHeaders(
            1,
            Headers(
                Field(":method", "GET"),
                Field(":scheme", "https"),
                Field(":authority", "example.test"),
                Field(":path", "/"),
                field),
            true,
            false);
    }

    static void AssertMalformed(
        Http2MessageResult result,
        string errorCodeName)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
        Assert.That(result.IsMalformed, Is.True);
        Assert.That(result.ErrorScope, Is.EqualTo(Http2ErrorScope.Stream));
        Assert.That(result.HasErrorCode, Is.True);
        Assert.That(result.ErrorCode,
            Is.EqualTo(Http2ErrorCode.ProtocolError));
        Assert.That(result.ErrorCodeValue,
            Is.EqualTo((uint)Http2ErrorCode.ProtocolError));
        Assert.That(result.IsKnownErrorCode, Is.True);
        Assert.That(result.ErrorCodeName, Is.EqualTo(errorCodeName));
    }

    static HttpHeaders Headers(params HttpHeaderField[] fields)
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.AddRange(fields);
        return headers;
    }

    static HttpHeaderField Field(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
    }
}
