using System.Collections.Generic;

namespace tftp;

/// <summary>
/// Holds the completed outcome and metrics of a TFTP transfer.
/// </summary>
public class TftpTransferResult
{
    /// <summary>
    /// The direction of the completed transfer.
    /// </summary>
    public TftpTransferDirection Direction = TftpTransferDirection.Read;

    /// <summary>
    /// The terminal transfer state.
    /// </summary>
    public TftpTransferState State = TftpTransferState.Created;

    /// <summary>
    /// The content representation used by the transfer.
    /// </summary>
    public TftpTransferMode Mode = TftpTransferMode.Octet;

    /// <summary>
    /// The final peer endpoint used by the transfer.
    /// </summary>
    public TftpEndpoint FinalEndpoint = new TftpEndpoint();

    /// <summary>
    /// The number of decoded content bytes transferred.
    /// </summary>
    public long BytesTransferred = 0;

    /// <summary>
    /// The number of encoded content bytes transferred on the wire.
    /// </summary>
    public long EncodedBytesTransferred = 0;

    /// <summary>
    /// The elapsed transfer duration in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds = 0;

    /// <summary>
    /// The number of packet retransmissions.
    /// </summary>
    public int RetransmissionCount = 0;

    /// <summary>
    /// The number of duplicate packets observed.
    /// </summary>
    public int DuplicatePacketCount = 0;

    /// <summary>
    /// The effective data block size.
    /// </summary>
    public int EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;

    /// <summary>
    /// The effective retransmission timeout in seconds.
    /// </summary>
    public int EffectiveTimeoutSeconds = 1;

    /// <summary>
    /// Indicates whether <see cref="TransferSize"/> contains a negotiated transfer size.
    /// </summary>
    public bool HasTransferSize = false;

    /// <summary>
    /// The negotiated transfer size when <see cref="HasTransferSize"/> is true.
    /// </summary>
    public long TransferSize = 0;

    /// <summary>
    /// The ordered options accepted during negotiation.
    /// </summary>
    public List<TftpOption> NegotiatedOptions = [];
}
