namespace ftp;

/// <summary>
/// Specifies the FTP representation type.
/// </summary>
public enum FtpTransferType
{
    /// <summary>
    /// Uses the ASCII representation type.
    /// </summary>
    Ascii = 0,
    /// <summary>
    /// Uses the EBCDIC representation type.
    /// </summary>
    Ebcdic = 1,
    /// <summary>
    /// Uses the image representation type.
    /// </summary>
    Image = 2,
    /// <summary>
    /// Uses a local-byte representation with an explicit byte size.
    /// </summary>
    LocalByte = 3
}
