namespace ftp;

/// <summary>
/// Configures explicit compatibility behavior for nonconforming FTP servers.
/// </summary>
public class FtpClientQuirkOptions
{
    /// <summary>
    /// Indicates that compact replies and mixed line endings are accepted.
    /// </summary>
    public bool LenientReplies;

    /// <summary>
    /// Indicates that the address in a PASV reply may be used instead of the control peer.
    /// </summary>
    public bool HonorPassiveAdvertisedAddress;

    /// <summary>
    /// Indicates that an advertised UTF8 feature is treated as unavailable.
    /// </summary>
    public bool IgnoreUtf8FeatureClaim;

    /// <summary>
    /// Indicates that an advertised MLST or MLSD feature is treated as unavailable.
    /// </summary>
    public bool IgnoreMlstFeatureClaim;

    /// <summary>
    /// Indicates that an advertised SIZE feature is treated as unavailable.
    /// </summary>
    public bool IgnoreSizeFeatureClaim;

    /// <summary>
    /// Indicates that an advertised MDTM feature is treated as unavailable.
    /// </summary>
    public bool IgnoreMdtmFeatureClaim;

    /// <summary>
    /// Indicates that a failed MLSD operation may fall back to another listing form.
    /// </summary>
    public bool AllowMlsdFallback;

    /// <summary>
    /// Indicates that a failed SIZE operation may use a safe metadata fallback.
    /// </summary>
    public bool AllowSizeFallback;

    /// <summary>
    /// Indicates that a failed MDTM operation may use a safe metadata fallback.
    /// </summary>
    public bool AllowMdtmFallback;

    /// <summary>
    /// Indicates that unsupported standard directory commands may use historic X-prefixed aliases.
    /// </summary>
    public bool AllowHistoricAliasFallback;

    /// <summary>
    /// Commands sent on the clear control channel immediately before AUTH TLS.
    /// </summary>
    public FtpCommand[] CommandsBeforeAuthorization;

    /// <summary>
    /// Commands sent immediately after the AUTH result and any accepted TLS handshake.
    /// </summary>
    public FtpCommand[] CommandsAfterAuthorization;

    /// <summary>
    /// Creates strict defaults with safe operation fallbacks and no extra commands.
    /// </summary>
    public FtpClientQuirkOptions()
    {
        LenientReplies = false;
        HonorPassiveAdvertisedAddress = false;
        IgnoreUtf8FeatureClaim = false;
        IgnoreMlstFeatureClaim = false;
        IgnoreSizeFeatureClaim = false;
        IgnoreMdtmFeatureClaim = false;
        AllowMlsdFallback = true;
        AllowSizeFallback = true;
        AllowMdtmFallback = true;
        AllowHistoricAliasFallback = false;
        CommandsBeforeAuthorization = new FtpCommand[0];
        CommandsAfterAuthorization = new FtpCommand[0];
    }
}
