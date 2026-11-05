using System;

namespace websocket;

/// <summary>
/// Defines incremental HTTP/1 opening-handshake parsing and writing operations.
/// </summary>
public interface IWebSocketHttp1Codec
{
    /// <summary>
    /// Reads an HTTP/1 client request from the supplied source bytes.
    /// </summary>
    /// <param name="source">The next contiguous request bytes.</param>
    /// <returns>The incremental request read result.</returns>
    WebSocketHttp1ReadResult ReadClientRequest(ReadOnlySpan<byte> source);

    /// <summary>
    /// Reads an HTTP/1 server response from the supplied source bytes.
    /// </summary>
    /// <param name="source">The next contiguous response bytes.</param>
    /// <returns>The incremental response read result.</returns>
    WebSocketHttp1ReadResult ReadServerResponse(ReadOnlySpan<byte> source);

    /// <summary>
    /// Measures the canonical encoded length of an HTTP/1 client request.
    /// </summary>
    /// <param name="request">The request metadata to validate and measure.</param>
    /// <returns>The complete encoded request length in bytes.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when the request metadata is invalid or contradictory.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded length exceeds the supported limit.</exception>
    int MeasureClientRequest(WebSocketHttpRequest request);

    /// <summary>
    /// Measures the canonical encoded length of an HTTP/1 server response.
    /// </summary>
    /// <param name="response">The response metadata to validate and measure.</param>
    /// <returns>The complete encoded response length in bytes.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when the response metadata is invalid or contradictory.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded length exceeds the supported limit.</exception>
    int MeasureServerResponse(WebSocketHttpResponse response);

    /// <summary>
    /// Writes the next canonical bytes of an HTTP/1 client request.
    /// </summary>
    /// <param name="request">The request instance retained until all measured bytes have been written.</param>
    /// <param name="destination">The bounded output destination.</param>
    /// <returns>The number of bytes produced in this call.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when a new request is invalid or contradictory.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded request length exceeds the supported limit.</exception>
    /// <exception cref="WebSocketStateException">Thrown when a different request is supplied during a partial write.</exception>
    int WriteClientRequest(WebSocketHttpRequest request, Span<byte> destination);

    /// <summary>
    /// Writes the next canonical bytes of an HTTP/1 server response.
    /// </summary>
    /// <param name="response">The response instance retained until all measured bytes have been written.</param>
    /// <param name="destination">The bounded output destination.</param>
    /// <returns>The number of bytes produced in this call.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when a new response is invalid or contradictory.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded response length exceeds the supported limit.</exception>
    /// <exception cref="WebSocketStateException">Thrown when a different response is supplied during a partial write.</exception>
    int WriteServerResponse(WebSocketHttpResponse response, Span<byte> destination);

    /// <summary>
    /// Resets both incremental HTTP/1 read states for reuse.
    /// </summary>
    void ResetHttpRead();
}
