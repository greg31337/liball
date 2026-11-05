namespace odf;

/// <summary>
/// Describes an image stored in the package thumbnail directory.
/// </summary>
public class OdfThumbnail
{
    /// <summary>The thumbnail package path.</summary>
    public string Path = "";
    /// <summary>The declared thumbnail media type.</summary>
    public string MediaType = "";
    /// <summary>The safely detected thumbnail format.</summary>
    public OdfImageFormat DetectedFormat;
    /// <summary>The safely detected thumbnail media type.</summary>
    public string DetectedMediaType = "";
    /// <summary>The thumbnail bytes when locally available.</summary>
    public byte[] Data = [];
    /// <summary>Whether thumbnail bytes are available without decryption.</summary>
    public bool IsContentAvailable;
    /// <summary>Whether the thumbnail package entry is encrypted.</summary>
    public bool IsEncrypted;
    /// <summary>Whether the thumbnail has a recognized image signature.</summary>
    public bool IsValid;
    /// <summary>Whether declared and detected media types disagree.</summary>
    public bool HasMediaTypeMismatch;
    internal byte[] OriginalData = [];
}
