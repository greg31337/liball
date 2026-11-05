namespace avif;

/// <summary>Maps one contiguous portion of extracted payload data to its exact source range.</summary>
public class AvifPayloadSegment : IAvifPayloadSegment
{
    long payloadOffset;
    long sourceOffset;
    long length;

    internal AvifPayloadSegment(
        long payloadOffset,
        long sourceOffset,
        long length)
    {
        this.payloadOffset = payloadOffset;
        this.sourceOffset = sourceOffset;
        this.length = length;
    }

    /// <summary>Returns the offset of this segment in the extracted payload.</summary>
    /// <returns>The zero-based payload offset.</returns>
    public long GetPayloadOffset()
    {
        return payloadOffset;
    }

    /// <summary>Returns the absolute offset of this segment in the AVIF source.</summary>
    /// <returns>The zero-based source offset.</returns>
    public long GetSourceOffset()
    {
        return sourceOffset;
    }

    /// <summary>Returns the segment length.</summary>
    /// <returns>The byte count.</returns>
    public long GetLength()
    {
        return length;
    }
}
