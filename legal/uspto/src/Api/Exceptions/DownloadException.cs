namespace uspto;

/// <summary>
/// Reports a failed streamed or file-backed download.
/// </summary>
public class DownloadException : global::System.Exception
{
    /// <summary>The credential-free source URI, when available.</summary>
    public global::System.Uri? SourceUri;

    /// <summary>The caller-selected destination path, when applicable.</summary>
    public string? DestinationPath;

    /// <summary>The download state at the time of failure.</summary>
    public DownloadState State;

    /// <summary>The number of verified bytes transferred before failure.</summary>
    public long BytesTransferred;

    /// <summary>The HTTP status code when one was received.</summary>
    public int StatusCode;

    /// <summary>Whether <see cref="StatusCode"/> contains a received HTTP status code.</summary>
    public bool HasStatusCode;

    /// <summary>Whether validated resume or retry may recover the transfer.</summary>
    public bool IsRetryable;

    /// <summary>Initializes a download exception and its transfer context.</summary>
    /// <param name="message">The credential-free exception message.</param>
    /// <param name="sourceUri">The credential-free source URI.</param>
    /// <param name="destinationPath">The caller-selected destination path.</param>
    /// <param name="state">The download state at failure.</param>
    /// <param name="bytesTransferred">The number of verified bytes transferred.</param>
    /// <param name="statusCode">The HTTP status code, if present.</param>
    /// <param name="hasStatusCode">Whether <c>statusCode</c> is present.</param>
    /// <param name="isRetryable">Whether the transfer may be retried or resumed.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public DownloadException(string message, global::System.Uri? sourceUri, string? destinationPath, DownloadState state, long bytesTransferred, int statusCode, bool hasStatusCode, bool isRetryable, global::System.Exception? innerException)
        : base(message, innerException)
    {
        SourceUri = sourceUri;
        DestinationPath = destinationPath;
        State = state;
        BytesTransferred = bytesTransferred;
        StatusCode = statusCode;
        HasStatusCode = hasStatusCode;
        IsRetryable = isRetryable;
    }
}
