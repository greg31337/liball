using System;

namespace http;

/// <summary>
/// Analyzes and serializes HTTP/1 messages.
/// </summary>
public interface IHttp1Serializer
{
    /// <summary>
    /// Serializes an HTTP/1 request into a newly allocated byte array.
    /// </summary>
    /// <param name="request">The request to serialize.</param>
    /// <param name="options">The serialization and framing options.</param>
    /// <returns>The serialization result and selected protocol metadata.</returns>
    Http1SerializationResult SerializeHttp1Request(HttpRequest request, Http1SerializationOptions options);

    /// <summary>
    /// Serializes an HTTP/1 response into a newly allocated byte array.
    /// </summary>
    /// <param name="response">The response to serialize.</param>
    /// <param name="options">The serialization and framing options.</param>
    /// <returns>The serialization result and selected protocol metadata.</returns>
    Http1SerializationResult SerializeHttp1Response(HttpResponse response, Http1SerializationOptions options);

    /// <summary>Writes an HTTP/1 request into bounded caller-provided storage.</summary>
    /// <param name="request">The request to serialize.</param>
    /// <param name="options">The serialization and framing options.</param>
    /// <param name="output">The destination storage.</param>
    /// <returns>The status and exact written or required size.</returns>
    Http1SerializationResult WriteHttp1Request(
        HttpRequest request,
        Http1SerializationOptions options,
        Span<byte> output);

    /// <summary>Writes an HTTP/1 response into bounded caller-provided storage.</summary>
    /// <param name="response">The response to serialize.</param>
    /// <param name="options">The serialization and framing options.</param>
    /// <param name="output">The destination storage.</param>
    /// <returns>The status and exact written or required size.</returns>
    Http1SerializationResult WriteHttp1Response(
        HttpResponse response,
        Http1SerializationOptions options,
        Span<byte> output);

    /// <summary>
    /// Determines the body framing for a request.
    /// </summary>
    /// <param name="request">The request to analyze.</param>
    /// <returns>The framing decision.</returns>
    Http1FramingDecision AnalyzeHttp1Framing(HttpRequest request);

    /// <summary>
    /// Determines the connection disposition for a request.
    /// </summary>
    /// <param name="request">The request to analyze.</param>
    /// <returns>The connection decision.</returns>
    Http1ConnectionDecision AnalyzeHttp1Connection(HttpRequest request);
}
