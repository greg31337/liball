namespace ftp;

/// <summary>
/// Describes whether a negotiated FTP feature is available.
/// </summary>
public enum FtpFeatureSupport
{
    /// <summary>
    /// Indicates that feature support has not been determined.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Indicates that the feature is unavailable.
    /// </summary>
    NotSupported = 1,
    /// <summary>
    /// Indicates that the feature is available.
    /// </summary>
    Supported = 2
}
