namespace png;

/// <summary>
/// Contains chunks read from a PNG datastream plus end-marker and trailing-byte information.
/// </summary>
public class PngChunkListResult
{
    /// <summary>
    /// Initializes a new instance of the PngChunkListResult class with its documented default values.
    /// </summary>
    public PngChunkListResult()
    {
    }

    /// <summary>
    /// Stores chunks in datastream order, excluding the PNG signature.
    /// </summary>
    public PngChunk[] Chunks = new PngChunk[0];
    /// <summary>
    /// Stores bytes found after IEND.
    /// </summary>
    public byte[] TrailingBytes = new byte[0];
    /// <summary>
    /// Indicates whether an IEND chunk terminated the chunk sequence.
    /// </summary>
    public bool FoundIend;
    /// <summary>
    /// Stores non-fatal chunk-list issues, such as a missing IEND marker.
    /// </summary>
    public PngWarning[] Warnings = new PngWarning[0];
}
