namespace ftp;

/// <summary>
/// Represents one normalized feature advertised by an FTP server.
/// </summary>
public class FtpFeature
{
    /// <summary>
    /// The normalized, case-insensitive feature name.
    /// </summary>
    public string Name;

    /// <summary>
    /// The optional feature parameters; null means no parameter section was present.
    /// </summary>
    public string? Parameters;

    /// <summary>
    /// The original feature line without its line terminator.
    /// </summary>
    public string RawLine;

    /// <summary>
    /// The known support state for this feature.
    /// </summary>
    public FtpFeatureSupport Support;

    /// <summary>
    /// Creates an unnamed feature whose support is unknown.
    /// </summary>
    public FtpFeature()
    {
        Name = "";
        Parameters = null;
        RawLine = "";
        Support = FtpFeatureSupport.Unknown;
    }
}

