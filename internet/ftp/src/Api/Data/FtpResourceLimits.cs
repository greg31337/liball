namespace ftp;

/// <summary>
/// Carries optional resource ceilings for one authenticated FTP identity.
/// </summary>
public class FtpResourceLimits
{
    /// <summary>
    /// Indicates whether <see cref="StorageQuotaBytes"/> is configured.
    /// </summary>
    public bool HasStorageQuotaBytes;

    /// <summary>
    /// The maximum stored bytes owned by the identity.
    /// </summary>
    public long StorageQuotaBytes;

    /// <summary>
    /// Indicates whether <see cref="TransferByteCeiling"/> is configured.
    /// </summary>
    public bool HasTransferByteCeiling;

    /// <summary>
    /// The maximum wire bytes accepted by one transfer.
    /// </summary>
    public long TransferByteCeiling;

    /// <summary>
    /// Indicates whether <see cref="ReadBytesPerSecond"/> is configured.
    /// </summary>
    public bool HasReadBytesPerSecond;

    /// <summary>
    /// The maximum download and listing throughput in bytes per second.
    /// </summary>
    public long ReadBytesPerSecond;

    /// <summary>
    /// Indicates whether <see cref="WriteBytesPerSecond"/> is configured.
    /// </summary>
    public bool HasWriteBytesPerSecond;

    /// <summary>
    /// The maximum upload throughput in bytes per second.
    /// </summary>
    public long WriteBytesPerSecond;

    /// <summary>
    /// Indicates whether <see cref="ListingEntryCap"/> is configured.
    /// </summary>
    public bool HasListingEntryCap;

    /// <summary>
    /// The maximum entries emitted by one directory listing.
    /// </summary>
    public int ListingEntryCap;

    /// <summary>
    /// Indicates whether <see cref="ListingByteCap"/> is configured.
    /// </summary>
    public bool HasListingByteCap;

    /// <summary>
    /// The maximum encoded bytes emitted by one directory listing.
    /// </summary>
    public long ListingByteCap;

    /// <summary>
    /// Creates a resource policy with every optional ceiling disabled.
    /// </summary>
    public FtpResourceLimits()
    {
        HasStorageQuotaBytes = false;
        StorageQuotaBytes = 0;
        HasTransferByteCeiling = false;
        TransferByteCeiling = 0;
        HasReadBytesPerSecond = false;
        ReadBytesPerSecond = 0;
        HasWriteBytesPerSecond = false;
        WriteBytesPerSecond = 0;
        HasListingEntryCap = false;
        ListingEntryCap = 0;
        HasListingByteCap = false;
        ListingByteCap = 0;
    }
}
