namespace image;

/// <summary>Identifies the outcome of a metadata-only hash operation.</summary>
public enum ImageMetadataHashStatus
{
    /// <summary>The operation has not produced an outcome.</summary>
    Unknown = 0,
    /// <summary>The selected metadata ranges were hashed successfully.</summary>
    Success = 1,
    /// <summary>No metadata matched the requested selection.</summary>
    EmptyMetadata = 2,
    /// <summary>Selected metadata existed but could not be safely separated from other source bytes.</summary>
    UnsupportedSeparation = 3,
    /// <summary>The requested hash algorithm was not supported.</summary>
    UnsupportedAlgorithm = 4,
    /// <summary>The source or metadata document was missing, unreadable, or inconsistent.</summary>
    InvalidInput = 5
}
