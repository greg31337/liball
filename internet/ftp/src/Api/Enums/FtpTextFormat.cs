namespace ftp;

/// <summary>
/// Specifies the format control used by a text representation.
/// </summary>
public enum FtpTextFormat
{
    /// <summary>
    /// Uses non-print format control.
    /// </summary>
    NonPrint = 0,
    /// <summary>
    /// Uses Telnet format control.
    /// </summary>
    Telnet = 1,
    /// <summary>
    /// Uses ASA carriage-control characters.
    /// </summary>
    CarriageControl = 2
}
