namespace ftp;

/// <summary>
/// Classifies an FTP reply by its first digit.
/// </summary>
public enum FtpReplyCategory
{
    /// <summary>
    /// Represents a reply whose category is unavailable or invalid.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Indicates that an operation has started and another reply will follow.
    /// </summary>
    PositivePreliminary = 1,
    /// <summary>
    /// Indicates that an operation completed successfully.
    /// </summary>
    PositiveCompletion = 2,
    /// <summary>
    /// Indicates that more information is required.
    /// </summary>
    PositiveIntermediate = 3,
    /// <summary>
    /// Indicates a temporary failure that may succeed when retried.
    /// </summary>
    TransientNegativeCompletion = 4,
    /// <summary>
    /// Indicates a permanent failure for the current request.
    /// </summary>
    PermanentNegativeCompletion = 5
}
