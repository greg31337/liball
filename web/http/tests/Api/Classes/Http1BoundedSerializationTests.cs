using System.Text;

namespace http.Tests;

public class Http1BoundedSerializationTests
{
    [Test]
    public void ExactRequestDestinationMatchesAllocatingSerialization()
    {
        HttpRequest request = Request("POST", "/upload");
        AddField(request.Headers, "Host", "example.test");
        AddField(request.Headers, "x-miXED-name", "value");
        request.Content = [0x00, 0x80, 0xff];
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.PreserveFieldNameCasing = false;
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Request(request, options);
        byte[] destination = new byte[allocated.RequiredSize];

        Http1SerializationResult bounded =
            serializer.WriteHttp1Request(request, options, destination);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(bounded.Bytes, Is.Empty);
            Assert.That(bounded.BytesWritten, Is.EqualTo(destination.Length));
            Assert.That(bounded.RequiredSize, Is.EqualTo(destination.Length));
            Assert.That(destination, Is.EqualTo(allocated.Bytes));
            Assert.That(bounded.FramingDecision!.BodyFraming,
                Is.EqualTo(allocated.FramingDecision!.BodyFraming));
            Assert.That(bounded.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(allocated.ConnectionDecision!.ConnectionMode));
            Assert.That(request.Headers.Fields, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ExactResponseDestinationPreservesOpaqueReasonAndFieldBytes()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.HasReasonPhrase = true;
        response.ReasonPhrase = [0x4f, 0x4b, 0x09, 0x80];
        AddRawField(response.Headers, "X-Opaque", [0x20, 0x80, 0xff]);
        response.Content = [0x01, 0x00, 0xfe];
        Http1SerializationOptions options = new Http1SerializationOptions();
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Response(response, options);
        byte[] destination = Enumerable.Repeat(
            (byte)0xee,
            allocated.RequiredSize + 4).ToArray();

        Http1SerializationResult bounded = serializer.WriteHttp1Response(
            response,
            options,
            destination.AsSpan(2, allocated.RequiredSize));

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(bounded.Bytes, Is.Empty);
            Assert.That(bounded.BytesWritten, Is.EqualTo(allocated.Bytes.Length));
            Assert.That(bounded.RequiredSize, Is.EqualTo(allocated.Bytes.Length));
            Assert.That(destination.Take(2), Is.All.EqualTo(0xee));
            Assert.That(destination.Skip(2).Take(allocated.RequiredSize),
                Is.EqualTo(allocated.Bytes));
            Assert.That(destination.Skip(allocated.RequiredSize + 2),
                Is.All.EqualTo(0xee));
            Assert.That(response.Headers.Fields, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void UndersizedRequestDestinationIsUntouchedAndReportsExactSize()
    {
        HttpRequest request = Request("POST", "/stream");
        AddField(request.Headers, "Host", "example.test");
        request.Content = "abcdefg"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = Http1BodyFraming.Chunked;
        options.ChunkSize = 3;
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Request(request, options);
        byte[] destination = Enumerable.Repeat(
            (byte)0xa5,
            allocated.RequiredSize - 1).ToArray();

        Http1SerializationResult bounded =
            serializer.WriteHttp1Request(request, options, destination);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(bounded.BytesWritten, Is.Zero);
            Assert.That(bounded.RequiredSize, Is.EqualTo(allocated.RequiredSize));
            Assert.That(bounded.Bytes, Is.Empty);
            Assert.That(destination, Is.All.EqualTo(0xa5));
            Assert.That(bounded.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.Chunked));
        });
    }

    [Test]
    public void EmptyResponseDestinationReportsRequiredSizeWithoutWriting()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.Content = "payload"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Response(response, options);

        Http1SerializationResult bounded = serializer.WriteHttp1Response(
            response,
            options,
            Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(bounded.BytesWritten, Is.Zero);
            Assert.That(bounded.RequiredSize, Is.EqualTo(allocated.Bytes.Length));
            Assert.That(bounded.Bytes, Is.Empty);
            Assert.That(bounded.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Persistent));
        });
    }

    [Test]
    public void LargerDestinationWritesOnlyTheCompleteResponsePrefix()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.Content = "until-close"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.AddContentLength = false;
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Response(response, options);
        byte[] destination = Enumerable.Repeat(
            (byte)0xcc,
            allocated.RequiredSize + 5).ToArray();

        Http1SerializationResult bounded =
            serializer.WriteHttp1Response(response, options, destination);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(bounded.BytesWritten, Is.EqualTo(allocated.RequiredSize));
            Assert.That(destination.Take(allocated.RequiredSize),
                Is.EqualTo(allocated.Bytes));
            Assert.That(destination.Skip(allocated.RequiredSize), Is.All.EqualTo(0xcc));
            Assert.That(bounded.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.CloseDelimited));
            Assert.That(bounded.ConnectionDecision!.MustClose, Is.True);
        });
    }

    [Test]
    public void BoundedChunkedResponseMatchesAllocatingChunkBoundaries()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.Content = "abcdefghij"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = Http1BodyFraming.Chunked;
        options.ChunkSize = 4;
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Response(response, options);
        byte[] destination = new byte[allocated.RequiredSize];

        Http1SerializationResult bounded =
            serializer.WriteHttp1Response(response, options, destination);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(destination, Is.EqualTo(allocated.Bytes));
            Assert.That(Encoding.ASCII.GetString(destination), Does.EndWith(
                "4\r\nabcd\r\n4\r\nefgh\r\n2\r\nij\r\n0\r\n\r\n"));
        });
    }

    [Test]
    public void NoBodyAndTunnelResponsesHaveExactBoundedOutput()
    {
        HttpResponse noContent = Response(HttpStatusCodes.NoContent);
        HttpResponse connected = Response(HttpStatusCodes.Ok);
        Http1SerializationOptions noContentOptions = new Http1SerializationOptions();
        Http1SerializationOptions connectOptions = new Http1SerializationOptions();
        connectOptions.ResponseRequestMethod = HttpMethods.Connect;
        Http1Serializer serializer = new Http1Serializer();
        byte[] noContentOutput = new byte[27];
        byte[] connectOutput = new byte[19];

        Http1SerializationResult noContentResult = serializer.WriteHttp1Response(
            noContent,
            noContentOptions,
            noContentOutput);
        Http1SerializationResult connectResult = serializer.WriteHttp1Response(
            connected,
            connectOptions,
            connectOutput);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(noContentOutput), Is.EqualTo(
                "HTTP/1.1 204 No Content\r\n\r\n"));
            Assert.That(noContentResult.FramingDecision!.HasNoBody, Is.True);
            Assert.That(Encoding.ASCII.GetString(connectOutput), Is.EqualTo(
                "HTTP/1.1 200 OK\r\n\r\n"));
            Assert.That(connectResult.FramingDecision!.IsTunnel, Is.True);
            Assert.That(connectResult.ConnectionDecision!.IsTunnel, Is.True);
        });
    }

    [Test]
    public void InvalidMessagesDoNotModifyBoundedDestinations()
    {
        HttpRequest request = Request("BAD METHOD", "/");
        AddField(request.Headers, "Host", "example.test");
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.HasReasonPhrase = true;
        response.ReasonPhrase = "bad\nreason"u8.ToArray();
        byte[] requestOutput = Enumerable.Repeat((byte)0x71, 64).ToArray();
        byte[] responseOutput = Enumerable.Repeat((byte)0x72, 64).ToArray();
        Http1Serializer serializer = new Http1Serializer();

        Http1SerializationResult requestResult = serializer.WriteHttp1Request(
            request,
            new Http1SerializationOptions(),
            requestOutput);
        Http1SerializationResult responseResult = serializer.WriteHttp1Response(
            response,
            new Http1SerializationOptions(),
            responseOutput);

        Assert.Multiple(() =>
        {
            Assert.That(requestResult.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(responseResult.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(requestResult.BytesWritten, Is.Zero);
            Assert.That(responseResult.BytesWritten, Is.Zero);
            Assert.That(requestOutput, Is.All.EqualTo(0x71));
            Assert.That(responseOutput, Is.All.EqualTo(0x72));
        });
    }

    static HttpRequest Request(string method, string target)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = method;
        request.Target = target;
        return request;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = statusCode;
        response.IsInformational = statusCode >= 100 && statusCode < 200;
        return response;
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
}
