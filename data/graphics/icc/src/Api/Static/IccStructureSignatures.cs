namespace icc;

/// <summary>
/// Defines registered ICC.2 tag-structure signatures.
/// </summary>
public static class IccStructureSignatures
{
    /// <summary>The BRDF-transform structure <c>brdf</c>.</summary>
    public const uint BrdfTransform = 0x62726466;
    /// <summary>The colour-encoding-parameters structure <c>cept</c>.</summary>
    public const uint ColorEncodingParameters = 0x63657074;
    /// <summary>The colorant-information structure <c>cinf</c>.</summary>
    public const uint ColorantInformation = 0x63696e66;
    /// <summary>The measurement-information structure <c>meas</c>.</summary>
    public const uint MeasurementInformation = 0x6d656173;
    /// <summary>The named-colour structure <c>nmcl</c>.</summary>
    public const uint NamedColor = 0x6e6d636c;
    /// <summary>The profile-connection-conditions structure <c>pcc </c>.</summary>
    public const uint ProfileConnectionConditions = 0x70636320;
    /// <summary>The profile-information structure <c>pinf</c>.</summary>
    public const uint ProfileInformation = 0x70696e66;
    /// <summary>The tint-zero structure <c>tnt0</c>.</summary>
    public const uint TintZero = 0x746e7430;
}
