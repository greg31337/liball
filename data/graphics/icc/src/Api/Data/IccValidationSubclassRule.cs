namespace icc;

/// <summary>Defines an accepted ICC.2 profile subclass, version range, and header context.</summary>
public class IccValidationSubclassRule
{
    /// <summary>Gets or sets the required profile-subclass signature.</summary>
    public uint ProfileSubclassSignature;
    /// <summary>Gets or sets the inclusive minimum subclass major version.</summary>
    public int MinimumMajorVersion;
    /// <summary>Gets or sets the inclusive minimum subclass minor version.</summary>
    public int MinimumMinorVersion;
    /// <summary>Gets or sets the inclusive maximum subclass major version.</summary>
    public int MaximumMajorVersion;
    /// <summary>Gets or sets the inclusive maximum subclass minor version.</summary>
    public int MaximumMinorVersion;
    /// <summary>Gets or sets the required profile class, or <see cref="IccProfileClass.Unknown"/> for any class.</summary>
    public IccProfileClass ProfileClass;
    /// <summary>Gets or sets the required data colour-space signature, or zero for any data space.</summary>
    public uint DataColorSpaceSignature;
    /// <summary>Gets or sets the required colorimetric PCS signature, or zero for any PCS.</summary>
    public uint ProfileConnectionSpaceSignature;
    /// <summary>Gets or sets the required spectral PCS signature, or zero for any spectral PCS.</summary>
    public uint SpectralPcsSignature;
    /// <summary>Gets or sets the required Material Connection Space signature, or zero for any MCS.</summary>
    public uint MaterialConnectionSpaceSignature;

    /// <summary>Initializes an unconstrained subclass context covering all encoded subclass versions.</summary>
    public IccValidationSubclassRule()
    {
        ProfileSubclassSignature = 0;
        MinimumMajorVersion = 0;
        MinimumMinorVersion = 0;
        MaximumMajorVersion = byte.MaxValue;
        MaximumMinorVersion = byte.MaxValue;
        ProfileClass = IccProfileClass.Unknown;
        DataColorSpaceSignature = 0;
        ProfileConnectionSpaceSignature = 0;
        SpectralPcsSignature = 0;
        MaterialConnectionSpaceSignature = 0;
    }
}
