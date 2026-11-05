using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class ResponseReaderTests
{
    [Test]
    public void JsonResponseIsBoundedDecodedAndCapturedWithSafeMetadata()
    {
        const string secret = "response-secret";
        const string json = "{\"apiKey\":\"response-secret\",\"count\":2}";
        byte[] body = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(json)).ToArray();
        using HttpResponseMessage response = CreateResponse(body, UsptoMediaTypes.Json, "utf-8");
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "request-123");
        response.Headers.TryAddWithoutValidation("Set-Cookie", "session=server-secret");
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.test/data?api_key=uri-secret&view=full");
        RequestContext context = new RequestContext
        {
            Service = ApiService.Odp,
            RetainRawResponse = true,
            MaximumRawResponseBytes = 1024
        };

        BufferedHttpResponse result = ResponseReader.ReadTextResponse(response, DocumentFormat.Json, context, new UsptoLimits(), new string[] { secret });

        Assert.Multiple(() =>
        {
            Assert.That(result.Text, Is.EqualTo(json));
            Assert.That(result.BodyBytes, Is.EqualTo(body.Length));
            Assert.That(result.ExpectedFormat, Is.EqualTo(DocumentFormat.Json));
            Assert.That(result.Metadata.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(result.Metadata.StatusCode, Is.EqualTo(200));
            Assert.That(result.Metadata.RequestIdentifier, Is.EqualTo("request-123"));
            Assert.That(result.Metadata.ContentLength, Is.EqualTo(body.Length));
            Assert.That(result.Metadata.HasContentLength, Is.True);
            Assert.That(result.Metadata.Headers["Set-Cookie"].Single(), Is.EqualTo(SecretRedactor.Replacement));
            Assert.That(result.Metadata.RequestUri?.AbsoluteUri, Does.Not.Contain("uri-secret"));
            Assert.That(result.HasRawText, Is.True);
            Assert.That(result.IsRawTextTruncated, Is.False);
            Assert.That(result.RawText, Does.Contain(SecretRedactor.Replacement));
            Assert.That(result.RawText, Does.Not.Contain(secret));
        });
    }

    [Test]
    public void XmlBomOverridesCharsetAndRawRetentionUsesTheSmallerLimit()
    {
        const string xml = "<root>café and additional text</root>";
        byte[] preamble = Encoding.BigEndianUnicode.GetPreamble();
        byte[] encoded = Encoding.BigEndianUnicode.GetBytes(xml);
        byte[] body = preamble.Concat(encoded).ToArray();
        using HttpResponseMessage response = CreateResponse(body, UsptoMediaTypes.Xml, "utf-8");
        RequestContext context = new RequestContext
        {
            Service = ApiService.Tsdr,
            RetainRawResponse = true,
            MaximumRawResponseBytes = 100
        };
        UsptoLimits limits = new UsptoLimits { MaximumRawResponseBytes = 12 };

        BufferedHttpResponse result = ResponseReader.ReadTextResponse(response, DocumentFormat.Xml, context, limits, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Text, Is.EqualTo(xml));
            Assert.That(result.RawTextBytes, Is.EqualTo(12));
            Assert.That(result.IsRawTextTruncated, Is.True);
            Assert.That(Encoding.UTF8.GetByteCount(result.RawText!), Is.LessThanOrEqualTo(12));
        });
    }

    [TestCase(DocumentFormat.Html, UsptoMediaTypes.Html)]
    [TestCase(DocumentFormat.Csv, UsptoMediaTypes.Csv)]
    [TestCase(DocumentFormat.Text, UsptoMediaTypes.Text)]
    [TestCase(DocumentFormat.Text, UsptoMediaTypes.Html)]
    public void SupportedTextRepresentationsUseTheirDeclaredMediaType(DocumentFormat format, string mediaType)
    {
        byte[] body = Encoding.UTF8.GetBytes("sample text");
        using HttpResponseMessage response = CreateResponse(body, mediaType, null);

        BufferedHttpResponse result = ResponseReader.ReadTextResponse(response, format, new RequestContext(), new UsptoLimits(), null);

        Assert.That(result.Text, Is.EqualTo("sample text"));
    }

    [Test]
    public void EmptyJsonXmlAndBinaryBodiesRemainSuccessfulTransportResults()
    {
        using HttpResponseMessage json = CreateResponse(Array.Empty<byte>(), UsptoMediaTypes.Json, "utf-8");
        using HttpResponseMessage xml = CreateResponse(Array.Empty<byte>(), UsptoMediaTypes.Xml, "utf-8");
        using HttpResponseMessage binary = CreateResponse(Array.Empty<byte>(), UsptoMediaTypes.Zip, null);
        RequestContext context = new RequestContext { RetainRawResponse = true, MaximumRawResponseBytes = 32 };
        using MemoryStream destination = new MemoryStream();

        BufferedHttpResponse jsonResult = ResponseReader.ReadTextResponse(json, DocumentFormat.Json, context, new UsptoLimits(), null);
        BufferedHttpResponse xmlResult = ResponseReader.ReadTextResponse(xml, DocumentFormat.Xml, context, new UsptoLimits(), null);
        StreamedHttpResponse binaryResult = ResponseReader.CopyBinaryResponse(binary, destination, 8, new RequestContext(), null);

        Assert.Multiple(() =>
        {
            Assert.That(jsonResult.Text, Is.Empty);
            Assert.That(jsonResult.RawText, Is.Empty);
            Assert.That(jsonResult.HasRawText, Is.True);
            Assert.That(jsonResult.IsRawTextTruncated, Is.False);
            Assert.That(xmlResult.Text, Is.Empty);
            Assert.That(xmlResult.Metadata.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(binaryResult.BytesCopied, Is.Zero);
            Assert.That(binaryResult.Metadata.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(destination.Length, Is.Zero);
        });
    }

    [Test]
    public void BinaryDeclaredLengthMismatchIsReportedAfterBoundedStreaming()
    {
        byte[] body = Encoding.ASCII.GetBytes("short");
        CountingReadStream input = new CountingReadStream(body, 2);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StreamContent(input);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Pdf);
        response.Content.Headers.ContentLength = body.Length + 1;
        using MemoryStream destination = new MemoryStream();

        ApiException failure = Assert.Throws<ApiException>(() => ResponseReader.CopyBinaryResponse(
            response,
            destination,
            3,
            new RequestContext { Service = ApiService.TsdrPdfAndZip },
            null))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorCode, Is.EqualTo("RESPONSE_LENGTH_MISMATCH"));
            Assert.That(destination.ToArray(), Is.EqualTo(body));
            Assert.That(input.MaximumChunkSize, Is.EqualTo(2));
        });
    }

    [Test]
    public void RawRetentionDoesNotSplitUtf8CharactersAtTheByteLimit()
    {
        byte[] body = Encoding.UTF8.GetBytes("éclair");
        using HttpResponseMessage response = CreateResponse(body, UsptoMediaTypes.Text, "utf-8");
        RequestContext context = new RequestContext
        {
            RetainRawResponse = true,
            MaximumRawResponseBytes = 1
        };

        BufferedHttpResponse result = ResponseReader.ReadTextResponse(response, DocumentFormat.Text, context, new UsptoLimits(), null);

        Assert.Multiple(() =>
        {
            Assert.That(result.RawText, Is.Empty);
            Assert.That(result.RawTextBytes, Is.Zero);
            Assert.That(result.IsRawTextTruncated, Is.True);
        });
    }

    [Test]
    public void DeclaredOversizedResponseIsRejectedBeforeItsStreamIsRead()
    {
        CountingReadStream stream = new CountingReadStream(Encoding.UTF8.GetBytes("0123456789"), 10);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StreamContent(stream);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Json);
        response.Content.Headers.ContentLength = 10;
        UsptoLimits limits = new UsptoLimits { MaximumResponseBytes = 5 };

        ApiException exception = Assert.Throws<ApiException>(() => ResponseReader.ReadTextResponse(
            response,
            DocumentFormat.Json,
            new RequestContext { Service = ApiService.Odp },
            limits,
            null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("RESPONSE_TOO_LARGE"));
            Assert.That(stream.ReadCount, Is.Zero);
        });
    }

    [Test]
    public void UnknownLengthResponseIsStoppedAsSoonAsItCrossesTheLimit()
    {
        CountingReadStream stream = new CountingReadStream(Encoding.UTF8.GetBytes("0123456789"), 2);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StreamingTestContent(stream, UsptoMediaTypes.Text);
        UsptoLimits limits = new UsptoLimits { MaximumResponseBytes = 5 };

        ApiException exception = Assert.Throws<ApiException>(() => ResponseReader.ReadTextResponse(
            response,
            DocumentFormat.Text,
            new RequestContext { Service = ApiService.Odp },
            limits,
            null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("RESPONSE_TOO_LARGE"));
            Assert.That(stream.BytesRead, Is.EqualTo(6));
        });
    }

    [Test]
    public void DeclaredLengthMismatchAndMediaMismatchHaveStructuredErrors()
    {
        using HttpResponseMessage truncated = CreateResponse(Encoding.UTF8.GetBytes("short"), UsptoMediaTypes.Json, null);
        truncated.Content.Headers.ContentLength = 20;
        using HttpResponseMessage wrongMedia = CreateResponse(Encoding.UTF8.GetBytes("{}"), UsptoMediaTypes.Xml, null);
        wrongMedia.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "request-media");

        ApiException lengthException = Assert.Throws<ApiException>(() => ResponseReader.ReadTextResponse(
            truncated,
            DocumentFormat.Json,
            new RequestContext { Service = ApiService.Odp },
            new UsptoLimits(),
            null));
        ApiException mediaException = Assert.Throws<ApiException>(() => ResponseReader.ReadTextResponse(
            wrongMedia,
            DocumentFormat.Json,
            new RequestContext { Service = ApiService.Odp },
            new UsptoLimits(),
            null));

        Assert.Multiple(() =>
        {
            Assert.That(lengthException.ErrorCode, Is.EqualTo("RESPONSE_LENGTH_MISMATCH"));
            Assert.That(mediaException.ErrorCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(mediaException.ErrorDetails, Is.EqualTo(UsptoMediaTypes.Xml));
            Assert.That(mediaException.RequestIdentifier, Is.EqualTo("request-media"));
        });
    }

    [Test]
    public void BinaryContentStreamsThroughTheConfiguredBufferAndPreservesMetadata()
    {
        byte[] body = Enumerable.Range(0, 257).Select(value => (byte)(value % 251)).ToArray();
        CountingReadStream input = new CountingReadStream(body, 31);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.PartialContent);
        response.Content = new StreamContent(input);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Zip);
        response.Content.Headers.ContentLength = body.Length;
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "binary-request");
        response.Headers.TryAddWithoutValidation("Set-Cookie", "binary-secret");
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.test/archive.zip?sig=signed-secret");
        TrackingWriteStream destination = new TrackingWriteStream();

        StreamedHttpResponse result = ResponseReader.CopyBinaryResponse(
            response,
            destination,
            17,
            new RequestContext { Service = ApiService.TsdrPdfAndZip },
            null);

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(body));
            Assert.That(destination.MaximumWriteCount, Is.LessThanOrEqualTo(17));
            Assert.That(result.BytesCopied, Is.EqualTo(body.Length));
            Assert.That(result.Metadata.StatusCode, Is.EqualTo(206));
            Assert.That(result.Metadata.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(result.Metadata.RequestIdentifier, Is.EqualTo("binary-request"));
            Assert.That(result.Metadata.Headers["Set-Cookie"].Single(), Is.EqualTo(SecretRedactor.Replacement));
            Assert.That(result.Metadata.RequestUri?.AbsoluteUri, Does.Not.Contain("signed-secret"));
            Assert.That(destination.CanWrite, Is.True);
        });
    }

    [Test]
    public void UnsupportedCharsetAndInvalidReaderInputsFailClearly()
    {
        using HttpResponseMessage charsetResponse = CreateResponse(Encoding.UTF8.GetBytes("text"), UsptoMediaTypes.Text, "made-up-charset");
        using HttpResponseMessage response = CreateResponse(Encoding.UTF8.GetBytes("text"), UsptoMediaTypes.Text, null);
        using MemoryStream readOnly = new MemoryStream(new byte[4], false);

        ParseException charset = Assert.Throws<ParseException>(() => ResponseReader.ReadTextResponse(
            charsetResponse,
            DocumentFormat.Text,
            new RequestContext(),
            new UsptoLimits(),
            null));
        ValidationException format = Assert.Throws<ValidationException>(() => ResponseReader.ReadTextResponse(
            response,
            DocumentFormat.Pdf,
            new RequestContext(),
            new UsptoLimits(),
            null));
        ValidationException destination = Assert.Throws<ValidationException>(() => ResponseReader.CopyBinaryResponse(
            response,
            readOnly,
            16,
            new RequestContext(),
            null));

        Assert.Multiple(() =>
        {
            Assert.That(charset.Format, Is.EqualTo(DocumentFormat.Text));
            Assert.That(format.ErrorCode, Is.EqualTo("UNSUPPORTED_FORMAT"));
            Assert.That(destination.ErrorCode, Is.EqualTo("STREAM_NOT_WRITABLE"));
        });
    }

    private static HttpResponseMessage CreateResponse(byte[] body, string mediaType, string? characterSet)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        response.Content.Headers.ContentType.CharSet = characterSet;
        return response;
    }
}

internal class CountingReadStream : MemoryStream
{
    internal int MaximumChunkSize;
    internal int ReadCount = 0;
    internal long BytesRead = 0L;

    internal CountingReadStream(byte[] bytes, int maximumChunkSize)
        : base(bytes, false)
    {
        MaximumChunkSize = maximumChunkSize;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int count = buffer.Length;
        if (count > MaximumChunkSize)
        {
            count = MaximumChunkSize;
        }

        ReadCount++;
        int read = base.Read(buffer.Span.Slice(0, count));
        BytesRead += read;
        return ValueTask.FromResult(read);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (count > MaximumChunkSize)
        {
            count = MaximumChunkSize;
        }

        ReadCount++;
        int read = base.Read(buffer, offset, count);
        BytesRead += read;
        return Task.FromResult(read);
    }
}

internal class TrackingWriteStream : MemoryStream
{
    internal int MaximumWriteCount = 0;

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (count > MaximumWriteCount)
        {
            MaximumWriteCount = count;
        }

        return base.WriteAsync(buffer, offset, count, cancellationToken);
    }
}

internal class StreamingTestContent : HttpContent
{
    internal Stream ContentStream;

    internal StreamingTestContent(Stream contentStream, string mediaType)
    {
        ContentStream = contentStream;
        Headers.ContentType = new MediaTypeHeaderValue(mediaType);
    }

    protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
    {
        return ContentStream;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return ContentStream.CopyToAsync(stream);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0L;
        return false;
    }
}
