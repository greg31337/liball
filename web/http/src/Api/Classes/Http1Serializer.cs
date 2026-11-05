using System;

namespace http;

/// <summary>
/// Validates, analyzes, and serializes HTTP/1 requests and responses.
/// </summary>
public class Http1Serializer : IHttp1Serializer
{
    /// <summary>Creates an HTTP/1 serializer.</summary>
    public Http1Serializer()
    {
    }

    /// <summary>Serializes an HTTP/1 request into owned output bytes.</summary>
    /// <param name="request">The request to serialize.</param>
    /// <param name="options">The serialization and framing options.</param>
    /// <returns>The serialization result and selected protocol metadata.</returns>
    public Http1SerializationResult SerializeHttp1Request(
        HttpRequest request,
        Http1SerializationOptions options)
    {
        return Http1RequestSerializer.Serialize(request, options);
    }

    /// <summary>Serializes an HTTP/1 response into owned output bytes.</summary>
    /// <param name="response">The response to serialize.</param>
    /// <param name="options">The serialization and framing options.</param>
    /// <returns>The serialization result and selected protocol metadata.</returns>
    public Http1SerializationResult SerializeHttp1Response(
        HttpResponse response,
        Http1SerializationOptions options)
    {
        return Http1ResponseSerializer.SerializeResponse(response, options);
    }

    /// <summary>Writes an HTTP/1 request into bounded caller-provided storage.</summary>
    /// <param name="request">The request to serialize.</param>
    /// <param name="options">The serialization and framing options.</param>
    /// <param name="output">The destination storage.</param>
    /// <returns>The status and exact written or required size.</returns>
    public Http1SerializationResult WriteHttp1Request(
        HttpRequest request,
        Http1SerializationOptions options,
        Span<byte> output)
    {
        return Http1RequestSerializer.Write(request, options, output);
    }

    /// <summary>Writes an HTTP/1 response into bounded caller-provided storage.</summary>
    /// <param name="response">The response to serialize.</param>
    /// <param name="options">The serialization and framing options.</param>
    /// <param name="output">The destination storage.</param>
    /// <returns>The status and exact written or required size.</returns>
    public Http1SerializationResult WriteHttp1Response(
        HttpResponse response,
        Http1SerializationOptions options,
        Span<byte> output)
    {
        return Http1ResponseSerializer.WriteResponse(response, options, output);
    }

    /// <summary>Determines the selected framing for an HTTP/1 request.</summary>
    /// <param name="request">The request to analyze.</param>
    /// <returns>The validated framing decision.</returns>
    public Http1FramingDecision AnalyzeHttp1Framing(HttpRequest request)
    {
        return Http1RequestSerializer.AnalyzeFraming(request);
    }

    /// <summary>Determines the connection disposition for an HTTP/1 request.</summary>
    /// <param name="request">The request to analyze.</param>
    /// <returns>The validated connection decision.</returns>
    public Http1ConnectionDecision AnalyzeHttp1Connection(HttpRequest request)
    {
        return Http1RequestSerializer.AnalyzeConnection(request);
    }
}
