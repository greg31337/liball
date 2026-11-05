namespace ftp;

/// <summary>
/// Selects entries for recursive transfer and synchronization.
/// </summary>
public interface IFtpTransferFilter
{
    /// <summary>
    /// Decides whether one entry is included.
    /// </summary>
    /// <param name="request">The entry, paths, direction, depth, and planned action.</param>
    /// <returns>True when the entry is included; otherwise false.</returns>
    bool IncludeTransferEntry(FtpTransferFilterRequest request);
}

