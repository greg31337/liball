namespace icc;

/// <summary>
/// Represents every common, ICC.1, and ICC.2 field in the 128-byte profile header.
/// </summary>
public class IccProfileHeader
{
    /// <summary>
    /// Gets or sets the validated declared profile size in bytes.
    /// </summary>
    public int DeclaredSize;

    /// <summary>
    /// Gets or sets the preferred colour management module signature.
    /// </summary>
    public uint PreferredCmmSignature;

    /// <summary>
    /// Gets or sets the profile and subclass version information.
    /// </summary>
    public IccVersion Version;

    /// <summary>
    /// Gets or sets the decoded profile class.
    /// </summary>
    public IccProfileClass ProfileClass;

    /// <summary>
    /// Gets or sets the profile or device class signature.
    /// </summary>
    public uint ProfileClassSignature;

    /// <summary>
    /// Gets or sets the data colour-space signature.
    /// </summary>
    public uint DataColorSpaceSignature;

    /// <summary>
    /// Gets or sets the decoded data colour-space family.
    /// </summary>
    public IccColorSpaceKind DataColorSpace;

    /// <summary>
    /// Gets or sets the colorimetric Profile Connection Space signature.
    /// </summary>
    public uint ProfileConnectionSpaceSignature;

    /// <summary>
    /// Gets or sets the decoded Profile Connection Space family.
    /// </summary>
    public IccPcsKind ProfileConnectionSpace;

    /// <summary>
    /// Gets or sets the profile creation date and time.
    /// </summary>
    public IccDateTime CreationDateTime;

    /// <summary>
    /// Gets or sets the profile file signature, normally <c>acsp</c>.
    /// </summary>
    public uint ProfileFileSignature;

    /// <summary>
    /// Gets or sets the primary platform signature.
    /// </summary>
    public uint PrimaryPlatformSignature;

    /// <summary>
    /// Gets or sets the profile flags bit field.
    /// </summary>
    public uint ProfileFlags;

    /// <summary>Gets or sets whether the header marks the profile as embedded.</summary>
    public bool IsEmbedded;

    /// <summary>Gets or sets whether the profile requires its embedded colour data.</summary>
    public bool RequiresEmbeddedData;

    /// <summary>Gets or sets whether ICC.2 MCS connections require a channel superset.</summary>
    public bool RequiresMcsSubset;

    /// <summary>Gets or sets whether the ICC.2 colorimetric PCS uses an extended HDR range.</summary>
    public bool UsesExtendedPcsRange;

    /// <summary>
    /// Gets or sets the device manufacturer signature.
    /// </summary>
    public uint DeviceManufacturerSignature;

    /// <summary>
    /// Gets or sets the device model signature.
    /// </summary>
    public uint DeviceModelSignature;

    /// <summary>
    /// Gets or sets the device attributes bit field.
    /// </summary>
    public ulong DeviceAttributes;

    /// <summary>
    /// Gets or sets the rendering-intent integer.
    /// </summary>
    public int RenderingIntent;

    /// <summary>
    /// Gets or sets the decoded rendering intent.
    /// </summary>
    public IccRenderingIntent RenderingIntentKind;

    /// <summary>
    /// Gets or sets the raw s15Fixed16 X component of the PCS illuminant.
    /// </summary>
    public int PcsIlluminantX;

    /// <summary>
    /// Gets or sets the raw s15Fixed16 Y component of the PCS illuminant.
    /// </summary>
    public int PcsIlluminantY;

    /// <summary>
    /// Gets or sets the raw s15Fixed16 Z component of the PCS illuminant.
    /// </summary>
    public int PcsIlluminantZ;

    /// <summary>
    /// Gets or sets the profile creator signature.
    /// </summary>
    public uint ProfileCreatorSignature;

    /// <summary>
    /// Gets or sets the Profile ID field.
    /// </summary>
    public IccProfileId ProfileId;

    /// <summary>
    /// Gets or sets the ICC.2 spectral PCS signature.
    /// </summary>
    public uint SpectralPcsSignature;

    /// <summary>Gets or sets the decoded ICC.2 spectral PCS family.</summary>
    public IccColorSpaceKind SpectralPcs;

    /// <summary>
    /// Gets or sets the ICC.2 spectral PCS start wavelength in nanometres.
    /// </summary>
    public double SpectralPcsRangeStart;

    /// <summary>
    /// Gets or sets the ICC.2 spectral PCS end wavelength in nanometres.
    /// </summary>
    public double SpectralPcsRangeEnd;

    /// <summary>
    /// Gets or sets the ICC.2 spectral PCS sample count.
    /// </summary>
    public int SpectralPcsRangeSteps;

    /// <summary>
    /// Gets or sets the ICC.2 bi-spectral PCS start wavelength in nanometres.
    /// </summary>
    public double BiSpectralPcsRangeStart;

    /// <summary>
    /// Gets or sets the ICC.2 bi-spectral PCS end wavelength in nanometres.
    /// </summary>
    public double BiSpectralPcsRangeEnd;

    /// <summary>
    /// Gets or sets the ICC.2 bi-spectral PCS sample count.
    /// </summary>
    public int BiSpectralPcsRangeSteps;

    /// <summary>
    /// Gets or sets the ICC.2 Material Connection Space signature.
    /// </summary>
    public uint MaterialConnectionSpaceSignature;

    /// <summary>Gets or sets the decoded ICC.2 multiplex connection-space family.</summary>
    public IccColorSpaceKind MaterialConnectionSpace;

    /// <summary>
    /// Gets or sets the ICC.2 profile or device subclass signature.
    /// </summary>
    public uint ProfileSubclassSignature;

    /// <summary>
    /// Gets or sets the four ICC.2 reserved bytes at offsets 124 through 127 as a bit field.
    /// </summary>
    public uint IccMaxReservedValue;

    /// <summary>
    /// Gets or sets the original bytes at header offsets 100 through 127.
    /// </summary>
    public byte[] ContextualBytes;

    /// <summary>
    /// Gets or sets the complete original 128-byte header representation.
    /// </summary>
    public byte[] RawBytes;

    /// <summary>
    /// Gets or sets non-fatal issues discovered while decoding header fields.
    /// </summary>
    public IccValidationIssue[] ValidationIssues;

    /// <summary>
    /// Initializes an empty header with independent fixed-size raw buffers.
    /// </summary>
    public IccProfileHeader()
    {
        DeclaredSize = 0;
        PreferredCmmSignature = 0;
        Version = new IccVersion();
        ProfileClass = IccProfileClass.Unknown;
        ProfileClassSignature = 0;
        DataColorSpaceSignature = 0;
        DataColorSpace = IccColorSpaceKind.Unknown;
        ProfileConnectionSpaceSignature = 0;
        ProfileConnectionSpace = IccPcsKind.None;
        CreationDateTime = new IccDateTime();
        ProfileFileSignature = 0;
        PrimaryPlatformSignature = 0;
        ProfileFlags = 0;
        IsEmbedded = false;
        RequiresEmbeddedData = false;
        RequiresMcsSubset = false;
        UsesExtendedPcsRange = false;
        DeviceManufacturerSignature = 0;
        DeviceModelSignature = 0;
        DeviceAttributes = 0;
        RenderingIntent = 0;
        RenderingIntentKind = IccRenderingIntent.Perceptual;
        PcsIlluminantX = 0;
        PcsIlluminantY = 0;
        PcsIlluminantZ = 0;
        ProfileCreatorSignature = 0;
        ProfileId = new IccProfileId();
        SpectralPcsSignature = 0;
        SpectralPcs = IccColorSpaceKind.Unknown;
        SpectralPcsRangeStart = 0;
        SpectralPcsRangeEnd = 0;
        SpectralPcsRangeSteps = 0;
        BiSpectralPcsRangeStart = 0;
        BiSpectralPcsRangeEnd = 0;
        BiSpectralPcsRangeSteps = 0;
        MaterialConnectionSpaceSignature = 0;
        MaterialConnectionSpace = IccColorSpaceKind.Unknown;
        ProfileSubclassSignature = 0;
        IccMaxReservedValue = 0;
        ContextualBytes = new byte[28];
        RawBytes = new byte[128];
        ValidationIssues = new IccValidationIssue[0];
    }
}
