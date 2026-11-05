using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Describes the effective serialized entries in a PowerPoint package.
/// </summary>
public class PptxPackageManifest
{
    /// <summary>
    /// Contains entries in ordinal ZIP entry-name order.
    /// </summary>
    public List<PptxPackageManifestEntry> Entries;
    /// <summary>
    /// Stores the combined uncompressed byte count of all entries.
    /// </summary>
    public long TotalBytes;

    /// <summary>
    /// Initializes an empty package manifest.
    /// </summary>
    public PptxPackageManifest()
    {
        Entries = new List<PptxPackageManifestEntry>();
        TotalBytes = 0L;
    }
}
