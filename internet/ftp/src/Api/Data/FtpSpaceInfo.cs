namespace ftp;

/// <summary>
/// Carries optional virtual file-system capacity and quota values.
/// </summary>
public class FtpSpaceInfo
{
    /// <summary>
    /// Indicates whether <see cref="TotalBytes"/> is available.
    /// </summary>
    public bool HasTotalBytes;

    /// <summary>
    /// The total capacity in bytes when <see cref="HasTotalBytes"/> is true.
    /// </summary>
    public long TotalBytes;

    /// <summary>
    /// Indicates whether <see cref="AvailableBytes"/> is available.
    /// </summary>
    public bool HasAvailableBytes;

    /// <summary>
    /// The available capacity in bytes when <see cref="HasAvailableBytes"/> is true.
    /// </summary>
    public long AvailableBytes;

    /// <summary>
    /// Indicates whether <see cref="UsedBytes"/> is available.
    /// </summary>
    public bool HasUsedBytes;

    /// <summary>
    /// The used capacity in bytes when <see cref="HasUsedBytes"/> is true.
    /// </summary>
    public long UsedBytes;

    /// <summary>
    /// The optional provider-defined quota name.
    /// </summary>
    public string? QuotaName;

    /// <summary>
    /// Creates space information with all numeric values unavailable.
    /// </summary>
    public FtpSpaceInfo()
    {
        HasTotalBytes = false;
        TotalBytes = 0;
        HasAvailableBytes = false;
        AvailableBytes = 0;
        HasUsedBytes = false;
        UsedBytes = 0;
        QuotaName = null;
    }
}

