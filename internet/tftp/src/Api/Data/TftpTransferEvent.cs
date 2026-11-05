using System.Collections.Generic;

namespace tftp;

/// <summary>
/// Holds an immutable-by-convention snapshot of transfer classification and state.
/// </summary>
public class TftpTransferEvent
{
    /// <summary>
    /// The direction of the transfer relative to the server.
    /// </summary>
    public TftpTransferDirection Direction = TftpTransferDirection.Read;

    /// <summary>
    /// The current transfer lifecycle state.
    /// </summary>
    public TftpTransferState State = TftpTransferState.Created;

    /// <summary>
    /// The content representation used by the transfer.
    /// </summary>
    public TftpTransferMode Mode = TftpTransferMode.Octet;

    /// <summary>
    /// The peer endpoint associated with the event.
    /// </summary>
    public TftpEndpoint Peer = new TftpEndpoint();

    /// <summary>
    /// The block number associated with the event.
    /// </summary>
    public ushort BlockNumber = 0;

    /// <summary>
    /// The number of decoded content bytes transferred so far.
    /// </summary>
    public long BytesTransferred = 0;

    /// <summary>
    /// The number of encoded content bytes transferred on the wire so far.
    /// </summary>
    public long EncodedBytesTransferred = 0;

    /// <summary>
    /// The number of packet retransmissions so far.
    /// </summary>
    public int RetransmissionCount = 0;

    /// <summary>
    /// The number of duplicate packets observed so far.
    /// </summary>
    public int DuplicatePacketCount = 0;

    /// <summary>
    /// The ordered options accepted during negotiation.
    /// </summary>
    public List<TftpOption> NegotiatedOptions = [];

    /// <summary>
    /// Indicates whether the event reports an error.
    /// </summary>
    public bool HasError = false;

    /// <summary>
    /// The protocol error code when <see cref="HasError"/> is true.
    /// </summary>
    public TftpErrorCode ErrorCode = TftpErrorCode.Undefined;

    /// <summary>
    /// The diagnostic or lifecycle message associated with the event.
    /// </summary>
    public string Message = "";

    /// <summary>
    /// The server request associated with a server-side event, when applicable.
    /// </summary>
    public TftpServerRequest? ServerRequest = null;
}
