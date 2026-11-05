namespace tftp;

/// <summary>
/// Holds the outcome of exception-less packet decoding.
/// </summary>
public class TftpPacketDecodeResult
{
    /// <summary>
    /// Indicates whether the packet was decoded successfully.
    /// </summary>
    public bool Success = false;

    /// <summary>
    /// The decoded packet when decoding succeeds.
    /// </summary>
    public TftpPacket Packet = new TftpPacket();

    /// <summary>
    /// The number of source bytes consumed when decoding succeeds.
    /// </summary>
    public int ConsumedLength = 0;

    /// <summary>
    /// Describes the validation failure when decoding does not succeed.
    /// </summary>
    public string FailureMessage = "";
}
