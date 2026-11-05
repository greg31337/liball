using System.Text;

namespace http.Tests;

public class Http2ResponseMessageTests
{
    [Test]
    public void FinalResponseMapsStatusAndPreservesRegularFieldsWithoutReasonPhrase()
    {
        HttpHeaderField regular = Field("content-type", "text/plain");
        regular.IsSensitive = true;
        regular.NeverIndex = true;
        HttpHeaders headers = Headers(Field(":status", "200"), regular);
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2ResponseHeaders(1, headers, false);
        HttpResponse response = result.Response!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.StreamId, Is.EqualTo(1));
            Assert.That(result.MessageKind,
                Is.EqualTo(HttpMessageKind.Response));
            Assert.That(result.Headers, Is.SameAs(headers));
            Assert.That(result.EndStream, Is.False);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.IsMalformed, Is.False);
            Assert.That(response.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http2));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(response.HasReasonPhrase, Is.False);
            Assert.That(response.ReasonPhrase, Is.Empty);
            Assert.That(response.IsInformational, Is.False);
            Assert.That(response.IsComplete, Is.False);
            Assert.That(response.Content, Is.Empty);
            Assert.That(response.Headers.Fields, Has.Count.EqualTo(1));
            Assert.That(response.Headers.Fields[0].Name,
                Is.EqualTo("content-type"));
            Assert.That(response.Headers.Fields[0].IsSensitive, Is.True);
            Assert.That(response.Headers.Fields[0].NeverIndex, Is.True);
        });

        regular.Value[0] = (byte)'X';
        Assert.That(
            Encoding.ASCII.GetString(response.Headers.Fields[0].Value),
            Is.EqualTo("text/plain"));
    }

    [Test]
    public void InformationalResponsesAreCompleteButLeaveStreamOpenForFinalResponse()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult continuing = codec.DecodeHttp2ResponseHeaders(
            3, Headers(Field(":status", "100")), false);
        Http2MessageResult hints = codec.DecodeHttp2ResponseHeaders(
            3,
            Headers(Field(":status", "103"), Field("link", "</style.css>")),
            false);
        Http2MessageResult final = codec.DecodeHttp2ResponseHeaders(
            3, Headers(Field(":status", "204")), true);

        Assert.Multiple(() =>
        {
            Assert.That(continuing.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(continuing.MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(continuing.EndStream, Is.False);
            Assert.That(continuing.IsComplete, Is.True);
            Assert.That(continuing.Response!.IsInformational, Is.True);
            Assert.That(continuing.Response.IsComplete, Is.True);
            Assert.That(hints.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(hints.MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(hints.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.EarlyHints));
            Assert.That(final.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(final.MessageKind,
                Is.EqualTo(HttpMessageKind.Response));
            Assert.That(final.EndStream, Is.True);
            Assert.That(final.IsComplete, Is.True);
            Assert.That(final.Response!.IsInformational, Is.False);
            Assert.That(final.Response.IsComplete, Is.True);
            Assert.That(final.Response.Content, Is.Empty);
        });
    }

    [Test]
    public void InformationalEndStreamAndSwitchingProtocolsAreMalformed()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult ended = codec.DecodeHttp2ResponseHeaders(
            5, Headers(Field(":status", "103")), true);
        Http2MessageResult switching = codec.DecodeHttp2ResponseHeaders(
            7, Headers(Field(":status", "101")), false);
        Http2MessageResult laterFinal = codec.DecodeHttp2ResponseHeaders(
            5, Headers(Field(":status", "200")), true);

        Assert.Multiple(() =>
        {
            AssertMalformed(ended, "http2-informational-end-stream");
            AssertMalformed(
                switching, "http2-switching-protocols-not-supported");
            Assert.That(laterFinal.Status,
                Is.EqualTo(HttpOperationStatus.Done));
        });
    }

    [Test]
    public void StatusMustBePresentUniqueAndThreeDigits()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult missing = codec.DecodeHttp2ResponseHeaders(
            9, Headers(Field("server", "example")), true);
        Http2MessageResult duplicate = codec.DecodeHttp2ResponseHeaders(
            11,
            Headers(Field(":status", "200"), Field(":status", "204")),
            true);
        string[] invalidValues = { "", "99", "099", "20a", "2000" };

        Assert.Multiple(() =>
        {
            AssertMalformed(missing, "missing-http2-response-status");
            AssertMalformed(duplicate, "duplicate-http2-response-status");
            int index = 0;
            while (index < invalidValues.Length)
            {
                Http2MessageResult invalid = codec.DecodeHttp2ResponseHeaders(
                    13 + index * 2,
                    Headers(Field(":status", invalidValues[index])),
                    true);
                AssertMalformed(invalid, "invalid-http2-response-status");
                index = index + 1;
            }
        });
    }

    [Test]
    public void UnknownThreeDigitExtensionStatusIsPreserved()
    {
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2ResponseHeaders(
                25, Headers(Field(":status", "699")), true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Response!.StatusCode, Is.EqualTo(699));
            Assert.That(result.Response.IsInformational, Is.False);
            Assert.That(result.Response.HasReasonPhrase, Is.False);
        });
    }

    [Test]
    public void ResponsePseudoHeadersMustPrecedeRegularFieldsAndOnlyUseStatus()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult ordered = codec.DecodeHttp2ResponseHeaders(
            27,
            Headers(Field("server", "example"), Field(":status", "200")),
            true);
        Http2MessageResult requestPseudo = codec.DecodeHttp2ResponseHeaders(
            29,
            Headers(Field(":status", "200"), Field(":method", "GET")),
            true);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                ordered, "http2-response-pseudo-header-order");
            AssertMalformed(
                requestPseudo, "invalid-http2-response-pseudo-header");
        });
    }

    [Test]
    public void AStreamRejectsEveryResponseHeaderBlockAfterItsFinalResponse()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult first = codec.DecodeHttp2ResponseHeaders(
            31, Headers(Field(":status", "200")), false);
        Http2MessageResult secondFinal = codec.DecodeHttp2ResponseHeaders(
            31, Headers(Field(":status", "204")), true);
        Http2MessageResult lateInformational = codec.DecodeHttp2ResponseHeaders(
            31, Headers(Field(":status", "103")), false);
        Http2MessageResult otherStream = codec.DecodeHttp2ResponseHeaders(
            33, Headers(Field(":status", "200")), true);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            AssertMalformed(
                secondFinal, "http2-final-response-already-received");
            AssertMalformed(
                lateInformational, "http2-final-response-already-received");
            Assert.That(otherStream.Status,
                Is.EqualTo(HttpOperationStatus.Done));
        });
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
        Assert.That(result.Response, Is.Null);
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
