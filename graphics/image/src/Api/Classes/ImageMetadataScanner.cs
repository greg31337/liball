namespace image;

/// <summary>Scans directories for image metadata with stable ordering and optional filtering.</summary>
public class ImageMetadataScanner : IImageMetadataScanner
{
    /// <summary>Creates a stateless image metadata scanner.</summary>
    public ImageMetadataScanner()
    {
    }

    /// <summary>Scans a directory using caller-supplied traversal, filtering, and reader options.</summary>
    /// <param name="directoryPath">The directory to scan, or null.</param>
    /// <param name="options">The scan options, or null to use non-recursive defaults.</param>
    /// <returns>The ordered per-file results and directory-level diagnostics.</returns>
    public ImageMetadataScanResult ScanDirectory(string? directoryPath, ImageMetadataScanOptions? options)
    {
        return ImageMetadataScannerCore.ScanDirectory(directoryPath, options);
    }
}
