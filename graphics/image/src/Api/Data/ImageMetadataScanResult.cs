using System.Collections.Generic;

namespace image;

/// <summary>Contains the ordered file results and directory-level diagnostics produced by one scan.</summary>
public class ImageMetadataScanResult
{
    /// <summary>The included file results in stable ordinal path order.</summary>
    public List<ImageMetadataScanFileResult> Files;
    /// <summary>The directory-level validation and traversal diagnostics.</summary>
    public List<MetadataDiagnostic> Diagnostics;

    /// <summary>Creates an empty scan result with initialized collections.</summary>
    public ImageMetadataScanResult()
    {
        Files = new List<ImageMetadataScanFileResult>();
        Diagnostics = new List<MetadataDiagnostic>();
    }
}
