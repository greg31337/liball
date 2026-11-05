namespace rtp;

/// <summary>
/// Contains a complete RTP packet independent of caller-owned wire storage.
/// </summary>
public class RtpPacket
{
    /// <summary>The RTP version field.</summary>
    public byte Version = RtpProtocolConstants.Version;

    /// <summary>Whether the RTP padding flag is set.</summary>
    public bool HasPadding;

    /// <summary>Whether the RTP extension flag is set.</summary>
    public bool HasExtension;

    /// <summary>The RTP marker flag.</summary>
    public bool Marker;

    /// <summary>The RTP payload type.</summary>
    public byte PayloadType;

    /// <summary>The RTP sequence number.</summary>
    public ushort SequenceNumber;

    /// <summary>The RTP timestamp.</summary>
    public uint Timestamp;

    /// <summary>The RTP synchronization source identifier.</summary>
    public uint SynchronizationSource;

    /// <summary>The RTP contributing source identifiers.</summary>
    public uint[] ContributingSources = [];

    /// <summary>The RTP header extension when the extension flag is set.</summary>
    public RtpHeaderExtension? HeaderExtension;

    /// <summary>The media payload bytes excluding RTP padding.</summary>
    public byte[] Payload = [];

    /// <summary>The preserved RTP padding bytes, including the final count byte.</summary>
    public byte[] Padding = [];

    internal RtpPacket Copy()
    {
        RtpPacket copy = new RtpPacket();
        copy.Version = Version;
        copy.HasPadding = HasPadding;
        copy.HasExtension = HasExtension;
        copy.Marker = Marker;
        copy.PayloadType = PayloadType;
        copy.SequenceNumber = SequenceNumber;
        copy.Timestamp = Timestamp;
        copy.SynchronizationSource = SynchronizationSource;
        copy.ContributingSources = (uint[])ContributingSources.Clone();
        if (HeaderExtension != null)
        {
            copy.HeaderExtension = HeaderExtension.Copy();
        }

        copy.Payload = (byte[])Payload.Clone();
        copy.Padding = (byte[])Padding.Clone();
        return copy;
    }
}
