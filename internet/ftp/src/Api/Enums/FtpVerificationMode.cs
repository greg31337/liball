namespace ftp;

/// <summary>
/// Specifies post-transfer verification.
/// </summary>
public enum FtpVerificationMode
{
    /// <summary>
    /// Does not verify transferred content.
    /// </summary>
    None = 0,
    /// <summary>
    /// Verifies the transferred size.
    /// </summary>
    Size = 1,
    /// <summary>
    /// Verifies the modification time.
    /// </summary>
    ModifiedTime = 2,
    /// <summary>
    /// Verifies both size and modification time.
    /// </summary>
    SizeAndModifiedTime = 3,
    /// <summary>
    /// Verifies a cryptographic or CRC checksum.
    /// </summary>
    Checksum = 4
}
