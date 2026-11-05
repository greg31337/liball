namespace icc;

/// <summary>
/// Represents the packed ICC profile version and ICC.2 profile subclass version.
/// </summary>
public class IccVersion
{
    /// <summary>
    /// Gets or sets the decoded profile generation.
    /// </summary>
    public IccVersionKind Kind;

    /// <summary>
    /// Gets or sets the profile major version encoded in byte 8 of the header.
    /// </summary>
    public int Major;

    /// <summary>
    /// Gets or sets the profile minor version encoded in the high nibble of byte 9.
    /// </summary>
    public int Minor;

    /// <summary>
    /// Gets or sets the profile bug-fix version encoded in the low nibble of byte 9.
    /// </summary>
    public int BugFix;

    /// <summary>
    /// Gets or sets the ICC.2 profile subclass major version encoded in byte 10.
    /// </summary>
    public int SubclassMajor;

    /// <summary>
    /// Gets or sets the ICC.2 profile subclass minor version encoded in byte 11.
    /// </summary>
    public int SubclassMinor;

    /// <summary>
    /// Gets or sets whether the profile version is directly supported by this library.
    /// </summary>
    public bool IsSupported;

    /// <summary>
    /// Gets or sets whether the profile uses a recognized generation with a newer minor version.
    /// </summary>
    public bool IsFutureMinor;

    /// <summary>
    /// Gets or sets the original packed 32-bit version value.
    /// </summary>
    public uint PackedValue;

    /// <summary>
    /// Initializes an unspecified profile version.
    /// </summary>
    public IccVersion()
    {
        Kind = IccVersionKind.Unknown;
        Major = 0;
        Minor = 0;
        BugFix = 0;
        SubclassMajor = 0;
        SubclassMinor = 0;
        IsSupported = false;
        IsFutureMinor = false;
        PackedValue = 0;
    }
}
