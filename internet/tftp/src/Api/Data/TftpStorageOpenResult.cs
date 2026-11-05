using System.IO;

namespace tftp;

/// <summary>
/// Holds the result of opening readable content or a pending write destination.
/// </summary>
public class TftpStorageOpenResult
{
    /// <summary>
    /// Indicates whether the storage operation succeeded.
    /// </summary>
    public bool Success = false;

    /// <summary>
    /// The protocol error code to report when <see cref="Success"/> is false.
    /// </summary>
    public TftpErrorCode ErrorCode = TftpErrorCode.Undefined;

    /// <summary>
    /// The error message to report when <see cref="Success"/> is false.
    /// </summary>
    public string ErrorMessage = "";

    /// <summary>
    /// The opened readable stream or pending writable stream when successful.
    /// </summary>
    public Stream? Content = null;

    /// <summary>
    /// Indicates whether the server owns and must dispose <see cref="Content"/>.
    /// </summary>
    public bool OwnsContent = false;

    /// <summary>
    /// Indicates whether <see cref="KnownSize"/> contains an exact content size.
    /// </summary>
    public bool HasKnownSize = false;

    /// <summary>
    /// The exact content size in bytes when <see cref="HasKnownSize"/> is true.
    /// </summary>
    public long KnownSize = 0;

    /// <summary>
    /// The opaque token used to commit or abort a successful pending write.
    /// </summary>
    public object? CompletionToken = null;
}
