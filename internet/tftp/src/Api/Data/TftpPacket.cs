using System.Collections.Generic;

namespace tftp;

/// <summary>
/// Holds the fields of a decoded packet or a packet to be encoded.
/// </summary>
public class TftpPacket
{
    /// <summary>
    /// The operation represented by the packet.
    /// </summary>
    public TftpOpcode Opcode = TftpOpcode.ReadRequest;

    /// <summary>
    /// The requested filename for a read or write request.
    /// </summary>
    public string Filename = "";

    /// <summary>
    /// The wire mode name for a read or write request.
    /// </summary>
    public string Mode = TftpProtocolValues.OctetMode;

    /// <summary>
    /// The block number carried by a DATA or ACK packet.
    /// </summary>
    public ushort BlockNumber = 0;

    /// <summary>
    /// The bytes carried by a DATA packet.
    /// </summary>
    public byte[] Payload = [];

    /// <summary>
    /// The error code carried by an ERROR packet.
    /// </summary>
    public TftpErrorCode ErrorCode = TftpErrorCode.Undefined;

    /// <summary>
    /// The message carried by an ERROR packet.
    /// </summary>
    public string ErrorMessage = "";

    /// <summary>
    /// The ordered options carried by a request or OACK packet.
    /// </summary>
    public List<TftpOption> Options = [];
}
