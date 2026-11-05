namespace rtp;

/// <summary>
/// Contains one RFC 5104 temporal-spatial trade-off entry.
/// </summary>
public class RtcpTemporalSpatialTradeoff
{
    /// <summary>Whether the entry is a TSTN notification rather than a TSTR request.</summary>
    public bool IsNotification;

    /// <summary>The SSRC of the target media sender.</summary>
    public uint SourceIdentifier;

    /// <summary>The command sequence number.</summary>
    public byte SequenceNumber;

    /// <summary>The five-bit temporal-spatial trade-off index.</summary>
    public byte TradeoffIndex;

    internal RtcpTemporalSpatialTradeoff Copy()
    {
        RtcpTemporalSpatialTradeoff copy = new RtcpTemporalSpatialTradeoff();
        copy.IsNotification = IsNotification;
        copy.SourceIdentifier = SourceIdentifier;
        copy.SequenceNumber = SequenceNumber;
        copy.TradeoffIndex = TradeoffIndex;
        return copy;
    }
}
