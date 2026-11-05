using System;

namespace websocket;

/// <summary>
/// Composes bounded HTTP/1 request and response parsing with canonical handshake writing.
/// </summary>
public class WebSocketHttp1Codec : IWebSocketHttp1Codec
{
    internal Http1RequestParser RequestParser;
    internal Http1ResponseParser ResponseParser;
    internal bool RequestWriteActive;
    internal WebSocketHttpRequest? RequestWriteOwner;
    internal byte[] RequestWriteBytes = [];
    internal int RequestWriteOffset;
    internal bool ResponseWriteActive;
    internal WebSocketHttpResponse? ResponseWriteOwner;
    internal byte[] ResponseWriteBytes = [];
    internal int ResponseWriteOffset;

    /// <summary>
    /// Initializes an HTTP/1 codec with finite header byte, field count, and line limits.
    /// </summary>
    /// <param name="maximumHeaderBytes">The maximum bytes accepted for one request or response header block.</param>
    /// <param name="maximumHeaderCount">The maximum fields accepted in one request or response.</param>
    /// <param name="maximumLineLength">The maximum request, status, or field line content length.</param>
    /// <exception cref="WebSocketLimitException">Thrown when a limit is not positive or the line limit exceeds the header byte limit.</exception>
    public WebSocketHttp1Codec(
        int maximumHeaderBytes,
        int maximumHeaderCount,
        int maximumLineLength)
    {
        RequestParser = new Http1RequestParser(
            maximumHeaderBytes,
            maximumHeaderCount,
            maximumLineLength);
        ResponseParser = new Http1ResponseParser(
            maximumHeaderBytes,
            maximumHeaderCount,
            maximumLineLength);
    }

    /// <summary>
    /// Reads an HTTP/1 client request from the supplied source bytes.
    /// </summary>
    /// <param name="source">The next contiguous request bytes.</param>
    /// <returns>The incremental request read result.</returns>
    public WebSocketHttp1ReadResult ReadClientRequest(ReadOnlySpan<byte> source)
    {
        return RequestParser.ReadRequest(source);
    }

    /// <summary>
    /// Reads an HTTP/1 server response from the supplied source bytes.
    /// </summary>
    /// <param name="source">The next contiguous response bytes.</param>
    /// <returns>The incremental response read result.</returns>
    public WebSocketHttp1ReadResult ReadServerResponse(ReadOnlySpan<byte> source)
    {
        return ResponseParser.ReadResponse(source);
    }

    /// <summary>
    /// Measures the canonical encoded length of an HTTP/1 client request.
    /// </summary>
    /// <param name="request">The request metadata to validate and measure.</param>
    /// <returns>The complete encoded request length in bytes.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when the request metadata is invalid or contradictory.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded length exceeds the supported limit.</exception>
    public int MeasureClientRequest(WebSocketHttpRequest request)
    {
        Http1WriteResult result = Http1Writer.MeasureRequest(request);
        if (result.Status != WebSocketParseStatus.Complete)
        {
            throw CreateWriteException(result.Status);
        }

        return result.RequiredLength;
    }

    /// <summary>
    /// Measures the canonical encoded length of an HTTP/1 server response.
    /// </summary>
    /// <param name="response">The response metadata to validate and measure.</param>
    /// <returns>The complete encoded response length in bytes.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when the response metadata is invalid or contradictory.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded length exceeds the supported limit.</exception>
    public int MeasureServerResponse(WebSocketHttpResponse response)
    {
        Http1WriteResult result = Http1Writer.MeasureResponse(response);
        if (result.Status != WebSocketParseStatus.Complete)
        {
            throw CreateWriteException(result.Status);
        }

        return result.RequiredLength;
    }

    /// <summary>
    /// Writes the next canonical bytes of an HTTP/1 client request.
    /// </summary>
    /// <param name="request">The request instance retained until all measured bytes have been written.</param>
    /// <param name="destination">The bounded output destination.</param>
    /// <returns>The number of bytes produced in this call.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when a new request is invalid or contradictory.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded request length exceeds the supported limit.</exception>
    /// <exception cref="WebSocketStateException">Thrown when a different request is supplied during a partial write.</exception>
    public int WriteClientRequest(WebSocketHttpRequest request, Span<byte> destination)
    {
        if (RequestWriteActive)
        {
            if (RequestWriteOwner != request)
            {
                throw new WebSocketStateException();
            }
        }
        else
        {
            StartRequestWrite(request);
        }

        int copyCount = destination.Length;
        int remainingCount = RequestWriteBytes.Length - RequestWriteOffset;
        if (copyCount > remainingCount)
        {
            copyCount = remainingCount;
        }

        RequestWriteBytes.AsSpan(RequestWriteOffset, copyCount).CopyTo(destination);
        RequestWriteOffset = RequestWriteOffset + copyCount;
        if (RequestWriteOffset == RequestWriteBytes.Length)
        {
            RequestWriteActive = false;
            RequestWriteOwner = null;
            RequestWriteBytes = [];
            RequestWriteOffset = 0;
        }

        return copyCount;
    }

    /// <summary>
    /// Writes the next canonical bytes of an HTTP/1 server response.
    /// </summary>
    /// <param name="response">The response instance retained until all measured bytes have been written.</param>
    /// <param name="destination">The bounded output destination.</param>
    /// <returns>The number of bytes produced in this call.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when a new response is invalid or contradictory.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded response length exceeds the supported limit.</exception>
    /// <exception cref="WebSocketStateException">Thrown when a different response is supplied during a partial write.</exception>
    public int WriteServerResponse(WebSocketHttpResponse response, Span<byte> destination)
    {
        if (ResponseWriteActive)
        {
            if (ResponseWriteOwner != response)
            {
                throw new WebSocketStateException();
            }
        }
        else
        {
            StartResponseWrite(response);
        }

        int copyCount = destination.Length;
        int remainingCount = ResponseWriteBytes.Length - ResponseWriteOffset;
        if (copyCount > remainingCount)
        {
            copyCount = remainingCount;
        }

        ResponseWriteBytes.AsSpan(ResponseWriteOffset, copyCount).CopyTo(destination);
        ResponseWriteOffset = ResponseWriteOffset + copyCount;
        if (ResponseWriteOffset == ResponseWriteBytes.Length)
        {
            ResponseWriteActive = false;
            ResponseWriteOwner = null;
            ResponseWriteBytes = [];
            ResponseWriteOffset = 0;
        }

        return copyCount;
    }

    /// <summary>
    /// Resets both incremental HTTP/1 read states for reuse.
    /// </summary>
    public void ResetHttpRead()
    {
        RequestParser.ResetRequest();
        ResponseParser.ResetResponse();
    }

    internal void StartRequestWrite(WebSocketHttpRequest request)
    {
        Http1WriteResult measurement = Http1Writer.MeasureRequest(request);
        if (measurement.Status != WebSocketParseStatus.Complete)
        {
            throw CreateWriteException(measurement.Status);
        }

        byte[] encoded = new byte[measurement.RequiredLength];
        Http1WriteResult write = Http1Writer.WriteRequest(request, encoded);
        if (write.Status != WebSocketParseStatus.Complete)
        {
            throw CreateWriteException(write.Status);
        }

        RequestWriteBytes = encoded;
        RequestWriteOffset = 0;
        RequestWriteOwner = request;
        RequestWriteActive = true;
    }

    internal void StartResponseWrite(WebSocketHttpResponse response)
    {
        Http1WriteResult measurement = Http1Writer.MeasureResponse(response);
        if (measurement.Status != WebSocketParseStatus.Complete)
        {
            throw CreateWriteException(measurement.Status);
        }

        byte[] encoded = new byte[measurement.RequiredLength];
        Http1WriteResult write = Http1Writer.WriteResponse(response, encoded);
        if (write.Status != WebSocketParseStatus.Complete)
        {
            throw CreateWriteException(write.Status);
        }

        ResponseWriteBytes = encoded;
        ResponseWriteOffset = 0;
        ResponseWriteOwner = response;
        ResponseWriteActive = true;
    }

    internal static System.Exception CreateWriteException(WebSocketParseStatus status)
    {
        if (status == WebSocketParseStatus.LimitRejected)
        {
            return new WebSocketLimitException();
        }

        if (status == WebSocketParseStatus.ProtocolFailure)
        {
            return new WebSocketHandshakeException();
        }

        return new WebSocketStateException();
    }
}
