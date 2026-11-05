namespace fake;

/// <summary>Represents a correlated, explicitly synthetic file fixture.</summary>
/// <remarks>The record does not create content or claim that its checksum is a digest of content.</remarks>
public class FileRecord
{
    /// <summary>The generated base name without an extension.</summary>
    public string BaseName = string.Empty;

    /// <summary>The lowercase extension without a leading dot.</summary>
    public string Extension = string.Empty;

    /// <summary>The base name and extension joined by a dot.</summary>
    public string Name = string.Empty;

    /// <summary>The broad project-authored file category.</summary>
    public string Type = string.Empty;

    /// <summary>The registered media type correlated with <c>Extension</c>.</summary>
    public string MimeType = string.Empty;

    /// <summary>The nonnegative synthetic file size in bytes.</summary>
    public long Size;

    /// <summary>The lowercase 64-character SHA-256-shaped value.</summary>
    public string Checksum = string.Empty;

    /// <summary>The checksum-shape algorithm label, <c>SHA-256</c>.</summary>
    public string ChecksumAlgorithm = string.Empty;

    /// <summary>Whether <c>Checksum</c> is a digest of file content, always <c>false</c>.</summary>
    public bool ChecksumIsContentDigest;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    internal FileRecord()
    {
    }
}
