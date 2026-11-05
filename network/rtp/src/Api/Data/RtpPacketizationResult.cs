namespace rtp;

/// <summary>
/// Contains the owned result of splitting media data into RTP payload fragments.
/// </summary>
public class RtpPacketizationResult
{
    /// <summary>Whether packetization completed successfully.</summary>
    public bool Success;

    /// <summary>The number of media payload bytes consumed.</summary>
    public int ConsumedCount;

    /// <summary>The payload fragments produced without RTP header fields.</summary>
    public byte[][] Payloads = [];

    /// <summary>Whether each corresponding payload fragment ends an access unit.</summary>
    public bool[] Markers = [];

    /// <summary>A description of a recoverable packetization failure.</summary>
    public string? ErrorMessage;

    internal RtpPacketizationResult Copy()
    {
        RtpPacketizationResult copy = new RtpPacketizationResult();
        copy.Success = Success;
        copy.ConsumedCount = ConsumedCount;
        copy.Payloads = new byte[Payloads.Length][];
        int payloadIndex = 0;
        while (payloadIndex < Payloads.Length)
        {
            copy.Payloads[payloadIndex] = (byte[])Payloads[payloadIndex].Clone();
            payloadIndex = payloadIndex + 1;
        }

        copy.Markers = (bool[])Markers.Clone();
        copy.ErrorMessage = ErrorMessage;
        return copy;
    }
}
