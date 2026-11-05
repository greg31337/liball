namespace tiff;

/// <summary>Specifies whether a random-access TIFF source remains source-backed or is buffered during open.</summary>
public enum TiffSourceLoadingMode
{
    /// <summary>Retains the random-access source and reads metadata and image ranges only when needed.</summary>
    Streaming = 0,
    /// <summary>Copies the complete source into memory during open after applying the allocation policy.</summary>
    Buffered = 1
}
