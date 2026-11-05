namespace pptx;

/// <summary>
/// Describes one effective ZIP entry in a PowerPoint package manifest.
/// </summary>
public class PptxPackageManifestEntry
{
    /// <summary>
    /// Stores the ZIP entry name without a leading slash.
    /// </summary>
    public string EntryName;
    /// <summary>
    /// Stores the normalized package part name when one applies; otherwise, an empty string.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the effective MIME content type.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Stores the uncompressed byte count.
    /// </summary>
    public int ByteCount;
    /// <summary>
    /// Stores the lowercase SHA-256 fingerprint of the effective entry bytes.
    /// </summary>
    public string Sha256;

    /// <summary>
    /// Initializes an empty package manifest entry.
    /// </summary>
    public PptxPackageManifestEntry()
    {
        EntryName = "";
        PartName = "";
        ContentType = "";
        ByteCount = 0;
        Sha256 = "";
    }
}
