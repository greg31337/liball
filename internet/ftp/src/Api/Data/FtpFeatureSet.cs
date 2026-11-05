namespace ftp;

/// <summary>
/// Carries a bounded collection of normalized and unknown FTP features.
/// </summary>
public class FtpFeatureSet
{
    /// <summary>
    /// The advertised features in server reply order.
    /// </summary>
    public FtpFeature[] Features;

    /// <summary>
    /// The optional FEAT reply from which this set was parsed.
    /// </summary>
    public FtpReply? SourceReply;

    /// <summary>
    /// Indicates that feature negotiation completed successfully.
    /// </summary>
    public bool Negotiated;

    /// <summary>
    /// Creates an empty, unnegotiated feature set.
    /// </summary>
    public FtpFeatureSet()
    {
        Features = new FtpFeature[0];
        SourceReply = null;
        Negotiated = false;
    }
}

