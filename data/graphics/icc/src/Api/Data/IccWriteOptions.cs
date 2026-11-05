namespace icc;

/// <summary>
/// Controls profile layout, preservation behavior, and write-time resource consumption.
/// </summary>
public class IccWriteOptions
{
    /// <summary>
    /// Gets or sets the profile layout mode used while writing.
    /// </summary>
    public IccWriteMode Mode;

    /// <summary>
    /// Gets or sets the hard resource limits applied while writing.
    /// </summary>
    public IccResourceLimits ResourceLimits;

    /// <summary>
    /// Gets or sets whether byte-identical tag payloads share one encoded payload.
    /// </summary>
    public bool InternIdenticalTagPayloads;

    /// <summary>
    /// Gets or sets whether the Profile ID is recomputed when the profile version supports it.
    /// </summary>
    public bool ComputeProfileId;

    /// <summary>
    /// Gets or sets whether the original encoded profile creation time is retained.
    /// </summary>
    public bool PreserveOriginalCreationDateTime;

    /// <summary>
    /// Gets or sets whether unsupported and private payloads are retained.
    /// </summary>
    public bool PreserveUnknownData;

    /// <summary>
    /// Gets or sets whether writing refuses a profile known to be invalid.
    /// </summary>
    public bool RejectInvalidProfile;

    /// <summary>
    /// Initializes deterministic and lossless defaults for writing profiles.
    /// </summary>
    public IccWriteOptions()
    {
        Mode = IccWriteMode.Canonical;
        ResourceLimits = new IccResourceLimits();
        InternIdenticalTagPayloads = true;
        ComputeProfileId = true;
        PreserveOriginalCreationDateTime = true;
        PreserveUnknownData = true;
        RejectInvalidProfile = true;
    }
}
