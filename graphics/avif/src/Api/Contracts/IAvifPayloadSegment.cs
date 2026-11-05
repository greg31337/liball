namespace avif;

/// <summary>Defines the source mapping for one contiguous portion of extracted payload data.</summary>
public interface IAvifPayloadSegment
{
    /// <summary>Returns the offset of this segment in the extracted payload.</summary>
    /// <returns>The zero-based payload offset.</returns>
    long GetPayloadOffset();

    /// <summary>Returns the absolute offset of this segment in the AVIF source.</summary>
    /// <returns>The zero-based source offset.</returns>
    long GetSourceOffset();

    /// <summary>Returns the segment length.</summary>
    /// <returns>The byte count.</returns>
    long GetLength();
}
