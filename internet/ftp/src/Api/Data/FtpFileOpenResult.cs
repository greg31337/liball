using System.IO;

namespace ftp;

/// <summary>
/// Carries a virtual file-system stream-open result.
/// </summary>
public class FtpFileOpenResult
{
    /// <summary>
    /// Indicates that the file was opened successfully.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// The optional opened stream.
    /// </summary>
    public Stream? Stream;

    /// <summary>
    /// Indicates that the provider retains stream ownership after the operation.
    /// </summary>
    public bool LeaveStreamOpen;

    /// <summary>
    /// The optional opened entry metadata.
    /// </summary>
    public FtpEntry? Entry;

    /// <summary>
    /// Indicates whether <see cref="Length"/> is available.
    /// </summary>
    public bool HasLength;

    /// <summary>
    /// The stream content length when <see cref="HasLength"/> is true.
    /// </summary>
    public long Length;

    /// <summary>
    /// The optional public failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an unsuccessful result without a stream.
    /// </summary>
    public FtpFileOpenResult()
    {
        Succeeded = false;
        Stream = null;
        LeaveStreamOpen = false;
        Entry = null;
        HasLength = false;
        Length = 0;
        FailureMessage = null;
    }
}

