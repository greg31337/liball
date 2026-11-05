namespace http;

/// <summary>
/// Maps HTTP/2 field sections and common HTTP messages in both directions.
/// </summary>
public interface IHttp2MessageCodec
{
    /// <summary>Decodes and validates an HTTP/2 request field section.</summary>
    /// <param name="streamId">The request stream identifier.</param>
    /// <param name="headers">The decoded HPACK fields.</param>
    /// <param name="endStream">Whether the field section ends the stream.</param>
    /// <param name="extendedConnectEnabled">Whether extended CONNECT was negotiated.</param>
    /// <returns>The mapped request or malformed-message result.</returns>
    Http2MessageResult DecodeHttp2RequestHeaders(
        int streamId,
        HttpHeaders headers,
        bool endStream,
        bool extendedConnectEnabled);

    /// <summary>Decodes and validates an HTTP/2 response field section.</summary>
    /// <param name="streamId">The response stream identifier.</param>
    /// <param name="headers">The decoded HPACK fields.</param>
    /// <param name="endStream">Whether the field section ends the stream.</param>
    /// <returns>The mapped response or malformed-message result.</returns>
    Http2MessageResult DecodeHttp2ResponseHeaders(
        int streamId,
        HttpHeaders headers,
        bool endStream);

    /// <summary>Decodes HTTP/2 DATA content for a mapped request or response.</summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="messageKind">Whether the content belongs to a request or final response.</param>
    /// <param name="content">The DATA payload octets.</param>
    /// <param name="endStream">Whether the DATA frame ends the stream.</param>
    /// <returns>The updated message or malformed-message result.</returns>
    Http2MessageResult DecodeHttp2Content(
        int streamId,
        HttpMessageKind messageKind,
        byte[] content,
        bool endStream);

    /// <summary>Decodes and validates an HTTP/2 trailer field section.</summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="headers">The decoded HPACK fields.</param>
    /// <param name="endStream">Whether the trailers end the stream.</param>
    /// <returns>The mapped trailers or malformed-message result.</returns>
    Http2MessageResult DecodeHttp2Trailers(int streamId, HttpHeaders headers, bool endStream);

    /// <summary>Encodes a common request as legal HTTP/2 fields.</summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="request">The request to map.</param>
    /// <param name="endStream">Whether the field section ends the stream.</param>
    /// <returns>The HPACK-ready field section.</returns>
    Http2MessageResult EncodeHttp2RequestHeaders(int streamId, HttpRequest request, bool endStream);

    /// <summary>Encodes a common response as legal HTTP/2 fields.</summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="response">The response to map.</param>
    /// <param name="endStream">Whether the field section ends the stream.</param>
    /// <returns>The HPACK-ready field section.</returns>
    Http2MessageResult EncodeHttp2ResponseHeaders(int streamId, HttpResponse response, bool endStream);

    /// <summary>Encodes common trailer fields as legal HTTP/2 trailers.</summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="trailers">The trailer fields.</param>
    /// <returns>The HPACK-ready trailer section.</returns>
    Http2MessageResult EncodeHttp2Trailers(int streamId, HttpHeaders trailers);

    /// <summary>Converts a common HTTP/1 message into HTTP/2 mapping metadata.</summary>
    /// <param name="messageKind">The message kind.</param>
    /// <param name="request">The request, or <c>null</c> for a response.</param>
    /// <param name="response">The response, or <c>null</c> for a request.</param>
    /// <returns>The converted message and lossy-mapping metadata.</returns>
    Http2MessageResult ConvertHttp1ToHttp2(
        HttpMessageKind messageKind,
        HttpRequest? request,
        HttpResponse? response);

    /// <summary>Converts HTTP/2 mapping metadata into a common HTTP/1 message.</summary>
    /// <param name="message">The mapped HTTP/2 message.</param>
    /// <returns>The converted common message and lossy-mapping metadata.</returns>
    Http2MessageResult ConvertHttp2ToHttp1(Http2MessageResult message);
}
