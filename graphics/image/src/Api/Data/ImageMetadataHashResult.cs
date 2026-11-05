using System.Collections.Generic;

namespace image;

/// <summary>Contains the explicit outcome and digest of a metadata-only hash operation.</summary>
public class ImageMetadataHashResult
{
    /// <summary>The operation outcome.</summary>
    public ImageMetadataHashStatus Status;
    /// <summary>The canonical algorithm name used or requested.</summary>
    public string AlgorithmName;
    /// <summary>The lowercase hexadecimal digest, or empty when hashing did not succeed.</summary>
    public string HexDigest;
    /// <summary>The number of merged source ranges included in the digest.</summary>
    public int RangeCount;
    /// <summary>The number of source bytes included in the digest.</summary>
    public long HashedByteCount;
    /// <summary>The diagnostics describing a non-success outcome.</summary>
    public List<MetadataDiagnostic> Diagnostics;

    /// <summary>Creates an unknown result with empty text, zero counts, and initialized diagnostics.</summary>
    public ImageMetadataHashResult()
    {
        Status = ImageMetadataHashStatus.Unknown;
        AlgorithmName = "";
        HexDigest = "";
        RangeCount = 0;
        HashedByteCount = 0;
        Diagnostics = new List<MetadataDiagnostic>();
    }
}
