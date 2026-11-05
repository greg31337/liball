namespace raw;

/// <summary>Contains typed identity and creation fields from the fixed ICC profile header.</summary>
public class RawIccHeader
{
    /// <summary>The exact declared profile size.</summary>
    public uint ProfileSize;
    /// <summary>The preferred color-management-module signature, or an empty string.</summary>
    public string PreferredCmmType;
    /// <summary>The major ICC profile version.</summary>
    public int VersionMajor;
    /// <summary>The minor ICC profile version.</summary>
    public int VersionMinor;
    /// <summary>The ICC profile bug-fix version.</summary>
    public int VersionBugFix;
    /// <summary>The profile or device class signature.</summary>
    public string DeviceClass;
    /// <summary>The data color-space signature.</summary>
    public string DataColorSpace;
    /// <summary>The profile connection-space signature.</summary>
    public string ProfileConnectionSpace;
    /// <summary>The creation year.</summary>
    public int CreatedYear;
    /// <summary>The creation month.</summary>
    public int CreatedMonth;
    /// <summary>The creation day.</summary>
    public int CreatedDay;
    /// <summary>The creation hour.</summary>
    public int CreatedHour;
    /// <summary>The creation minute.</summary>
    public int CreatedMinute;
    /// <summary>The creation second.</summary>
    public int CreatedSecond;
    /// <summary>Whether the creation fields form a valid UTC date and time.</summary>
    public bool CreatedValid;
    /// <summary>The primary platform signature, or an empty string.</summary>
    public string PrimaryPlatform;
    /// <summary>The profile flags.</summary>
    public uint Flags;
    /// <summary>The device manufacturer signature, or an empty string.</summary>
    public string DeviceManufacturer;
    /// <summary>The device model signature, or an empty string.</summary>
    public string DeviceModel;
    /// <summary>The device attributes bit field.</summary>
    public ulong DeviceAttributes;
    /// <summary>The declared rendering intent from zero through three.</summary>
    public uint RenderingIntent;
    /// <summary>The three normalized PCS illuminant XYZ values.</summary>
    public double[] PcsIlluminant;
    /// <summary>The profile creator signature, or an empty string.</summary>
    public string Creator;

    internal RawIccHeader(uint profileSize, string preferredCmmType, int versionMajor, int versionMinor, int versionBugFix, string deviceClass, string dataColorSpace, string profileConnectionSpace, int createdYear, int createdMonth, int createdDay, int createdHour, int createdMinute, int createdSecond, bool createdValid, string primaryPlatform, uint flags, string deviceManufacturer, string deviceModel, ulong deviceAttributes, uint renderingIntent, double[] pcsIlluminant, string creator)
    {
        ProfileSize = profileSize;
        PreferredCmmType = preferredCmmType;
        VersionMajor = versionMajor;
        VersionMinor = versionMinor;
        VersionBugFix = versionBugFix;
        DeviceClass = deviceClass;
        DataColorSpace = dataColorSpace;
        ProfileConnectionSpace = profileConnectionSpace;
        CreatedYear = createdYear;
        CreatedMonth = createdMonth;
        CreatedDay = createdDay;
        CreatedHour = createdHour;
        CreatedMinute = createdMinute;
        CreatedSecond = createdSecond;
        CreatedValid = createdValid;
        PrimaryPlatform = primaryPlatform;
        Flags = flags;
        DeviceManufacturer = deviceManufacturer;
        DeviceModel = deviceModel;
        DeviceAttributes = deviceAttributes;
        RenderingIntent = renderingIntent;
        PcsIlluminant = pcsIlluminant;
        Creator = creator;
    }
}

/// <summary>Represents one exact entry from an ICC profile tag table.</summary>
public class RawIccTag
{
    /// <summary>The four-character tag signature.</summary>
    public string Signature;
    /// <summary>The zero-based offset from the start of the ICC profile.</summary>
    public uint Offset;
    /// <summary>The exact tag data size without alignment padding.</summary>
    public uint Size;
    /// <summary>The four-character tag type signature.</summary>
    public string TypeSignature;
    /// <summary>The exact tag data bytes without alignment padding.</summary>
    public byte[] RawBytes;

    internal RawIccTag(string signature, uint offset, uint size, string typeSignature, byte[] rawBytes)
    {
        Signature = signature;
        Offset = offset;
        Size = size;
        TypeSignature = typeSignature;
        RawBytes = rawBytes;
    }

    /// <summary>Returns an independent copy of the exact tag data bytes.</summary>
    /// <returns>The copied tag bytes.</returns>
    internal byte[] CopyBytes()
    {
        return RawTextValue.Copy(RawBytes);
    }
}

/// <summary>Contains an extracted ICC tone reproduction curve without applying a color transform.</summary>
public class RawIccToneCurve
{
    /// <summary>The curve representation.</summary>
    public RawIccCurveKind Kind;
    /// <summary>The parametric function type from zero through four, or zero for non-parametric curves.</summary>
    public int FunctionType;
    /// <summary>The gamma exponent, or one for identity and sampled curves.</summary>
    public double Gamma;
    /// <summary>Exact sampled values, or an empty array.</summary>
    public ushort[] Samples;
    /// <summary>Parametric curve values in specification order, or an empty array.</summary>
    public double[] Parameters;

    /// <summary>Creates a typed ICC tone reproduction curve.</summary>
    /// <param name="kind">The curve representation.</param>
    /// <param name="functionType">The parametric function type.</param>
    /// <param name="gamma">The gamma exponent.</param>
    /// <param name="samples">The exact curve samples.</param>
    /// <param name="parameters">The parametric values.</param>
    public RawIccToneCurve(RawIccCurveKind kind, int functionType, double gamma, ushort[]? samples, double[]? parameters)
    {
        Kind = kind;
        FunctionType = functionType;
        Gamma = gamma;
        Samples = samples == null ? System.Array.Empty<ushort>() : samples;
        Parameters = parameters == null ? System.Array.Empty<double>() : parameters;
    }
}

/// <summary>Retains one exact ICC profile with its validated header, tag table, identity, and matrix/TRC data.</summary>
public class RawIccProfile
{
    /// <summary>The exact encoded ICC profile bytes.</summary>
    public byte[]? RawBytes;
    /// <summary>The parsed fixed ICC profile header.</summary>
    public RawIccHeader? Header;
    /// <summary>The validated ICC tag-table entries in table order.</summary>
    public RawIccTag[]? Tags;
    /// <summary>The localized profile description selected for reporting, or an empty string.</summary>
    public string? Description;
    /// <summary>A stable display identity using the description when available and the computed ID otherwise.</summary>
    public string? Identity;
    /// <summary>The exact embedded profile ID from the header.</summary>
    public byte[]? EmbeddedProfileId;
    /// <summary>The profile ID calculated from the preserved bytes.</summary>
    public byte[]? CalculatedProfileId;
    /// <summary>The uppercase hexadecimal embedded profile ID, or an empty string.</summary>
    public string? EmbeddedProfileIdHex;
    /// <summary>The uppercase hexadecimal calculated profile ID.</summary>
    public string? CalculatedProfileIdHex;
    /// <summary>Whether the header contains a nonzero profile ID.</summary>
    public bool HasEmbeddedProfileId;
    /// <summary>Whether the embedded and calculated profile IDs match.</summary>
    public bool ProfileIdMatches;
    /// <summary>Whether unique tag data elements form a contiguous padded sequence.</summary>
    public bool TagDataContiguous;
    /// <summary>The extracted row-major RGB-to-PCS XYZ matrix, or an empty array.</summary>
    public double[]? Matrix;
    /// <summary>The extracted media white point, or an empty array.</summary>
    public double[]? WhitePoint;
    /// <summary>The extracted red tone reproduction curve, or <c>null</c>.</summary>
    public RawIccToneCurve? RedTrc;
    /// <summary>The extracted green tone reproduction curve, or <c>null</c>.</summary>
    public RawIccToneCurve? GreenTrc;
    /// <summary>The extracted blue tone reproduction curve, or <c>null</c>.</summary>
    public RawIccToneCurve? BlueTrc;
    /// <summary>The absolute profile offset in the containing source, or <c>-1</c>.</summary>
    public long Offset;
    /// <summary>The containing stable IFD path, or an empty string.</summary>
    public string? IfdPath;

    /// <summary>Parses and validates a bounded standalone ICC v2 or v4 profile.</summary>
    /// <param name="rawBytes">The exact profile bytes.</param>
    /// <param name="limits">The resource limits.</param>
    /// <exception cref="RawFormatException">The header, tag table, or a supported matrix/TRC value is malformed.</exception>
    /// <exception cref="RawLimitException">The profile exceeds a configured resource limit.</exception>
    /// <returns>The parsed profile.</returns>
    internal static RawIccProfile Parse(byte[] rawBytes, RawLimits limits)
    {
        return IccCodec.Parse(rawBytes, limits, -1, string.Empty);
    }

    internal RawIccProfile(byte[] rawBytes, RawIccHeader header, RawIccTag[] tags, string description, string identity, byte[] embeddedProfileId, byte[] calculatedProfileId, string embeddedProfileIdHex, string calculatedProfileIdHex, bool hasEmbeddedProfileId, bool profileIdMatches, bool tagDataContiguous, double[] matrix, double[] whitePoint, RawIccToneCurve? redTrc, RawIccToneCurve? greenTrc, RawIccToneCurve? blueTrc, long offset, string ifdPath)
    {
        RawBytes = rawBytes;
        Header = header;
        Tags = tags;
        Description = description;
        Identity = identity;
        EmbeddedProfileId = embeddedProfileId;
        CalculatedProfileId = calculatedProfileId;
        EmbeddedProfileIdHex = embeddedProfileIdHex;
        CalculatedProfileIdHex = calculatedProfileIdHex;
        HasEmbeddedProfileId = hasEmbeddedProfileId;
        ProfileIdMatches = profileIdMatches;
        TagDataContiguous = tagDataContiguous;
        Matrix = matrix;
        WhitePoint = whitePoint;
        RedTrc = redTrc;
        GreenTrc = greenTrc;
        BlueTrc = blueTrc;
        Offset = offset;
        IfdPath = ifdPath;
    }

    /// <summary>Returns an independent copy of the complete preserved ICC profile.</summary>
    /// <returns>The copied profile bytes.</returns>
    internal byte[] CopyBytes()
    {
        return RawTextValue.Copy(RawBytes);
    }

    /// <summary>Creates a TIFF UNDEFINED value suitable for ICC profile tag 34675.</summary>
    /// <returns>An exact independent typed value.</returns>
    internal RawTagValue ToTagValue()
    {
        return RawTagValue.CreateUndefined(CopyBytes());
    }

    /// <summary>Finds a tag by its exact four-character signature.</summary>
    /// <param name="signature">The exact tag signature.</param>
    /// <returns>The matching tag, or <c>null</c>.</returns>
    internal RawIccTag? GetTag(string signature)
    {
        if (signature == null)
        {
            return null;
        }
        int index = 0;
        while (index < (Tags ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawIccTag tag = Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return tag;
            }
            index++;
        }
        return null;
    }

    /// <summary>Returns whether a complete RGB matrix and three tone curves were extracted.</summary>
    /// <returns><c>true</c> for a complete matrix/TRC profile.</returns>
    internal bool HasMatrixTrc()
    {
        return Matrix != null && Matrix.Length == 9 && RedTrc != null && GreenTrc != null && BlueTrc != null;
    }

    /// <summary>Validates the mutable public profile model after parsing.</summary>
    /// <returns>All model validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (RawBytes == null || RawBytes.Length < 132)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Icc.RawBytes", "An ICC profile requires its complete preserved byte stream.");
        }
        if (Header == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Icc.Header", "An ICC profile requires a parsed header.");
        }
        if (Tags == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Icc.Tags", "An ICC profile requires a tag table.");
        }
        if (EmbeddedProfileId == null || EmbeddedProfileId.Length != 16 || CalculatedProfileId == null || CalculatedProfileId.Length != 16)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Icc.ProfileId", "Embedded and calculated ICC profile IDs must each contain sixteen bytes.");
        }
        if (Matrix == null || Matrix.Length != 0 && Matrix.Length != 9 || Matrix != null && !RawBlackLevel.AllFinite(Matrix))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Icc.Matrix", "An extracted ICC matrix must be empty or contain nine finite values.");
        }
        if (WhitePoint == null || WhitePoint.Length != 0 && WhitePoint.Length != 3 || WhitePoint != null && !RawBlackLevel.AllFinite(WhitePoint))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Icc.WhitePoint", "An extracted ICC white point must be empty or contain three finite values.");
        }
        if (Offset < -1)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidOffset, "Icc.Offset", "An ICC profile offset must be nonnegative or minus one.");
        }
        if (Header != null && !Header.CreatedValid)
        {
            messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Warning, RawFormat.Unknown, Offset, "Icc.Header.Created", "The ICC creation date and time are not representable.", false));
        }
        if (HasEmbeddedProfileId && !ProfileIdMatches)
        {
            messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Warning, RawFormat.Unknown, Offset, "Icc.ProfileId", "The embedded ICC profile ID does not match the preserved profile bytes.", false));
        }
        if (!TagDataContiguous)
        {
            messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Warning, RawFormat.Unknown, Offset, "Icc.TagData", "ICC tag data elements are bounded but do not form the current contiguous layout.", false));
        }
        return new RawValidationReport(true, messages.ToArray());
    }

}
