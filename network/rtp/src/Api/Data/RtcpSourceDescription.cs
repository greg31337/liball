namespace rtp;

/// <summary>
/// Contains the chunks of an RTCP source-description packet.
/// </summary>
public class RtcpSourceDescription
{
    /// <summary>The source-description chunks.</summary>
    public RtcpSourceDescriptionChunk[] Chunks = [];

    internal RtcpSourceDescription Copy()
    {
        RtcpSourceDescription copy = new RtcpSourceDescription();
        copy.Chunks = new RtcpSourceDescriptionChunk[Chunks.Length];
        int index = 0;
        while (index < Chunks.Length)
        {
            copy.Chunks[index] = Chunks[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
