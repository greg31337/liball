namespace tftp;

/// <summary>
/// Defines standard TFTP protocol sizes, limits, and wire names.
/// </summary>
public static class TftpProtocolValues
{
    /// <summary>
    /// The well-known UDP port on which a TFTP server receives requests.
    /// </summary>
    public const int DefaultPort = 69;

    /// <summary>
    /// The length of the opcode field in bytes.
    /// </summary>
    public const int OpcodeLength = 2;

    /// <summary>
    /// The length of a DATA packet header in bytes.
    /// </summary>
    public const int DataHeaderLength = 4;

    /// <summary>
    /// The fixed length of an ACK packet in bytes.
    /// </summary>
    public const int AckPacketLength = 4;

    /// <summary>
    /// The maximum length of an RRQ or WRQ packet in bytes.
    /// </summary>
    public const int MaximumRequestPacketLength = 512;

    /// <summary>
    /// The default number of data bytes carried by a DATA packet.
    /// </summary>
    public const int DefaultBlockSize = 512;

    /// <summary>
    /// The smallest block size permitted by the block-size option.
    /// </summary>
    public const int MinimumBlockSize = 8;

    /// <summary>
    /// The largest block size permitted by the block-size option.
    /// </summary>
    public const int MaximumBlockSize = 65464;

    /// <summary>
    /// The maximum length of a DATA packet, including its header, in bytes.
    /// </summary>
    public const int MaximumDataPacketLength = MaximumBlockSize + DataHeaderLength;

    /// <summary>
    /// The smallest timeout value permitted by the timeout option, in seconds.
    /// </summary>
    public const int MinimumTimeoutSeconds = 1;

    /// <summary>
    /// The largest timeout value permitted by the timeout option, in seconds.
    /// </summary>
    public const int MaximumTimeoutSeconds = 255;

    /// <summary>
    /// The canonical wire name for netascii transfer mode.
    /// </summary>
    public const string NetasciiMode = "netascii";

    /// <summary>
    /// The canonical wire name for octet transfer mode.
    /// </summary>
    public const string OctetMode = "octet";

    /// <summary>
    /// The canonical wire name for mail transfer mode.
    /// </summary>
    public const string MailMode = "mail";

    /// <summary>
    /// The canonical wire name for the block-size option.
    /// </summary>
    public const string BlockSizeOption = "blksize";

    /// <summary>
    /// The canonical wire name for the timeout option.
    /// </summary>
    public const string TimeoutOption = "timeout";

    /// <summary>
    /// The canonical wire name for the transfer-size option.
    /// </summary>
    public const string TransferSizeOption = "tsize";
}
