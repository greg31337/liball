namespace uspto;

/// <summary>
/// Defines a recognized bulk-file compression or archive format.
/// </summary>
public enum CompressionFormat
{
    /// <summary>The input is not compressed or archived.</summary>
    None = 0,

    /// <summary>A ZIP archive.</summary>
    Zip = 1,

    /// <summary>A gzip-compressed stream.</summary>
    Gzip = 2,

    /// <summary>A tar archive.</summary>
    Tar = 3,

    /// <summary>A gzip-compressed tar archive.</summary>
    TarGzip = 4,

    /// <summary>The input uses a recognized but unsupported format.</summary>
    Unsupported = 5,

    /// <summary>The input format could not be determined.</summary>
    Unknown = 6
}
