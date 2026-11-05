using System;

namespace http;

/// <summary>
/// Represents a complete or incrementally assembled HTTP response.
/// </summary>
public class HttpResponse
{
    /// <summary>
    /// The protocol version used by the response.
    /// </summary>
    public HttpProtocolVersion ProtocolVersion;

    /// <summary>
    /// The numeric HTTP status code.
    /// </summary>
    public int StatusCode;

    /// <summary>
    /// Indicates that an HTTP/1 reason phrase was present, including an empty phrase.
    /// </summary>
    public bool HasReasonPhrase;

    /// <summary>
    /// The byte-exact HTTP/1 reason phrase without its leading separator.
    /// </summary>
    public byte[] ReasonPhrase = Array.Empty<byte>();

    /// <summary>
    /// The ordered response header fields.
    /// </summary>
    public HttpHeaders Headers = new HttpHeaders();

    /// <summary>
    /// The owned response content bytes.
    /// </summary>
    public byte[] Content = Array.Empty<byte>();

    /// <summary>
    /// The ordered response trailer fields.
    /// </summary>
    public HttpHeaders Trailers = new HttpHeaders();

    /// <summary>
    /// Indicates that this is an informational response preceding a final response.
    /// </summary>
    public bool IsInformational;

    /// <summary>
    /// Indicates that the entire response, including content and trailers, is available.
    /// </summary>
    public bool IsComplete;
}
