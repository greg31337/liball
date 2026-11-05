namespace icc;

/// <summary>
/// Controls profile reading, input ownership, and resource consumption.
/// </summary>
public class IccReadOptions
{
    /// <summary>
    /// Gets or sets the error-handling mode used while reading.
    /// </summary>
    public IccReadMode Mode;

    /// <summary>
    /// Gets or sets the validation policy applied while reading.
    /// </summary>
    public IccValidationMode ValidationMode;

    /// <summary>
    /// Gets or sets the hard resource limits applied while reading.
    /// </summary>
    public IccResourceLimits ResourceLimits;

    /// <summary>
    /// Gets or sets whether input bytes are copied to provide immutable ownership.
    /// </summary>
    public bool CopyInputBytes;

    /// <summary>
    /// Gets or sets whether tag payloads are decoded only when requested.
    /// </summary>
    public bool DecodeTagsLazily;

    /// <summary>
    /// Gets or sets whether bytes after the declared profile must be rejected.
    /// </summary>
    public bool RequireExactProfileSize;

    /// <summary>
    /// Gets or sets whether original encoded bytes are retained for preservation writes.
    /// </summary>
    public bool PreserveOriginalBytes;

    /// <summary>
    /// Initializes safe defaults for reading untrusted profiles.
    /// </summary>
    public IccReadOptions()
    {
        Mode = IccReadMode.Strict;
        ValidationMode = IccValidationMode.SecurityStrict;
        ResourceLimits = new IccResourceLimits();
        CopyInputBytes = true;
        DecodeTagsLazily = true;
        RequireExactProfileSize = true;
        PreserveOriginalBytes = true;
    }
}
