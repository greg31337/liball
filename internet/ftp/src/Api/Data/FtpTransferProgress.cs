using System;

namespace ftp;

/// <summary>
/// Carries a point-in-time transfer progress snapshot.
/// </summary>
public class FtpTransferProgress
{
    /// <summary>
    /// The transfer direction.
    /// </summary>
    public FtpTransferDirection Direction;

    /// <summary>
    /// The optional current local or remote path.
    /// </summary>
    public string? Path;

    /// <summary>
    /// Indicates whether <see cref="ExpectedBytes"/> is available.
    /// </summary>
    public bool HasExpectedBytes;

    /// <summary>
    /// The expected logical byte count when <see cref="HasExpectedBytes"/> is true.
    /// </summary>
    public long ExpectedBytes;

    /// <summary>
    /// The logical bytes processed.
    /// </summary>
    public long LogicalBytes;

    /// <summary>
    /// The wire bytes processed after framing or transformation.
    /// </summary>
    public long WireBytes;

    /// <summary>
    /// The elapsed transfer time.
    /// </summary>
    public TimeSpan Elapsed;

    /// <summary>
    /// Indicates that the transfer reached a terminal state.
    /// </summary>
    public bool Completed;

    /// <summary>
    /// Creates an empty download progress snapshot.
    /// </summary>
    public FtpTransferProgress()
    {
        Direction = FtpTransferDirection.Download;
        Path = null;
        HasExpectedBytes = false;
        ExpectedBytes = 0;
        LogicalBytes = 0;
        WireBytes = 0;
        Elapsed = TimeSpan.Zero;
        Completed = false;
    }
}

