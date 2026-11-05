namespace rtp;

/// <summary>
/// Contains the owned result of an RTP repair operation.
/// </summary>
public class RtpRepairResult
{
    /// <summary>Whether the repair operation completed successfully.</summary>
    public bool Success;

    /// <summary>The repair mechanism that produced the result.</summary>
    public RtpRepairKind Kind;

    /// <summary>The packets recovered by the repair operation.</summary>
    public RtpPacket[] RepairedPackets = [];

    /// <summary>The requested sequence numbers that remain unrepaired.</summary>
    public ushort[] UnrepairedSequenceNumbers = [];

    /// <summary>Application-defined result metadata.</summary>
    public byte[] Data = [];

    /// <summary>A description of a recoverable repair failure.</summary>
    public string? ErrorMessage;

    internal RtpRepairResult Copy()
    {
        RtpRepairResult copy = new RtpRepairResult();
        copy.Success = Success;
        copy.Kind = Kind;
        copy.RepairedPackets = new RtpPacket[RepairedPackets.Length];
        int packetIndex = 0;
        while (packetIndex < RepairedPackets.Length)
        {
            copy.RepairedPackets[packetIndex] = RepairedPackets[packetIndex].Copy();
            packetIndex = packetIndex + 1;
        }

        copy.UnrepairedSequenceNumbers = (ushort[])UnrepairedSequenceNumbers.Clone();
        copy.Data = (byte[])Data.Clone();
        copy.ErrorMessage = ErrorMessage;
        return copy;
    }
}
