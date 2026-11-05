namespace image;

/// <summary>Defines stable, filtered directory scanning for image metadata.</summary>
public interface IImageMetadataScanner
{
    /// <summary>Scans a directory using caller-supplied traversal, filtering, and reader options.</summary>
    /// <param name="directoryPath">The directory to scan, or null.</param>
    /// <param name="options">The scan options, or null to use non-recursive defaults.</param>
    /// <returns>The ordered per-file results and directory-level diagnostics.</returns>
    ImageMetadataScanResult ScanDirectory(string? directoryPath, ImageMetadataScanOptions? options);
}
