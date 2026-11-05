namespace http;

/// <summary>
/// Reports an HTTP/2 stream-scoped protocol failure.
/// </summary>
public class Http2StreamException : System.Exception
{
    /// <summary>A stable machine-readable diagnostic identifier.</summary>
    public string DiagnosticCode;
    /// <summary>The stream identifier associated with the failure.</summary>
    public int StreamId;
    /// <summary>The recognized error code, or <see cref="Http2ErrorCode.Unknown"/>.</summary>
    public Http2ErrorCode ErrorCode;
    /// <summary>The exact unsigned 32-bit error code.</summary>
    public uint ErrorCodeValue;
    /// <summary>Indicates that <see cref="ErrorCodeValue"/> maps to <see cref="ErrorCode"/>.</summary>
    public bool IsKnownErrorCode;

    /// <summary>Creates an HTTP/2 stream exception.</summary>
    /// <param name="message">The caller-readable failure explanation.</param>
    /// <param name="diagnosticCode">The stable machine-readable diagnostic identifier.</param>
    /// <param name="streamId">The stream identifier associated with the failure.</param>
    /// <param name="errorCode">The recognized HTTP/2 error code.</param>
    /// <param name="errorCodeValue">The exact unsigned HTTP/2 error code.</param>
    /// <param name="isKnownErrorCode">Whether the raw code maps to the recognized code.</param>
    public Http2StreamException(
        string message,
        string diagnosticCode,
        int streamId,
        Http2ErrorCode errorCode,
        uint errorCodeValue,
        bool isKnownErrorCode) : base(message)
    {
        DiagnosticCode = diagnosticCode;
        StreamId = streamId;
        ErrorCode = errorCode;
        ErrorCodeValue = errorCodeValue;
        IsKnownErrorCode = isKnownErrorCode;
    }
}
