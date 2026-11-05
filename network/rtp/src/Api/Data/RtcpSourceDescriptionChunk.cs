namespace rtp;

/// <summary>
/// Contains the source identifier and items of one RTCP SDES chunk.
/// </summary>
public class RtcpSourceDescriptionChunk
{
    /// <summary>The SSRC or CSRC described by the chunk.</summary>
    public uint SourceIdentifier;

    /// <summary>The source-description items preceding the END marker.</summary>
    public RtcpSourceDescriptionItem[] Items = [];

    internal RtcpSourceDescriptionChunk Copy()
    {
        RtcpSourceDescriptionChunk copy = new RtcpSourceDescriptionChunk();
        copy.SourceIdentifier = SourceIdentifier;
        copy.Items = new RtcpSourceDescriptionItem[Items.Length];
        int index = 0;
        while (index < Items.Length)
        {
            copy.Items[index] = Items[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
