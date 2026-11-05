namespace ftp;

/// <summary>
/// Specifies the logical structure of transferred data.
/// </summary>
public enum FtpFileStructure
{
    /// <summary>
    /// Treats transferred data as an unstructured file.
    /// </summary>
    File = 0,
    /// <summary>
    /// Preserves record boundaries.
    /// </summary>
    Record = 1,
    /// <summary>
    /// Transfers indexed pages with page headers.
    /// </summary>
    Page = 2
}
