namespace tftp;

/// <summary>
/// Identifies the representation used to transfer file content.
/// </summary>
public enum TftpTransferMode
{
    /// <summary>
    /// Transfers content as unmodified eight-bit bytes.
    /// </summary>
    Octet = 0,

    /// <summary>
    /// Transfers text using the TFTP netascii representation.
    /// </summary>
    Netascii = 1
}
