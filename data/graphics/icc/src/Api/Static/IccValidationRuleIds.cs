namespace icc;

/// <summary>
/// Defines stable identifiers for structural, semantic, security, and quality validation rules.
/// </summary>
public static class IccValidationRuleIds
{
    /// <summary>The general validation-diagnostic rule.</summary>
    public const string ValidationDiagnostic = "ICC.VALIDATION.DIAGNOSTIC";
    /// <summary>The retained-diagnostic limit rule.</summary>
    public const string ValidationLimit = "ICC.VALIDATION.LIMIT";
    /// <summary>The declared profile-size rule.</summary>
    public const string HeaderProfileSize = "ICC.HDR.SIZE";
    /// <summary>The profile file-signature rule.</summary>
    public const string HeaderFileSignature = "ICC.HDR.SIGNATURE";
    /// <summary>The profile-version rule.</summary>
    public const string HeaderVersion = "ICC.HDR.VERSION";
    /// <summary>The header reserved-bytes rule.</summary>
    public const string HeaderReserved = "ICC.HDR.RESERVED";
    /// <summary>The header date/time rule.</summary>
    public const string HeaderDateTime = "ICC.HDR.DATETIME";
    /// <summary>The profile-class rule.</summary>
    public const string HeaderProfileClass = "ICC.HDR.CLASS";
    /// <summary>The profile/device subclass rule.</summary>
    public const string HeaderProfileSubclass = "ICC.HDR.SUBCLASS";
    /// <summary>The data colour-space rule.</summary>
    public const string HeaderColorSpace = "ICC.HDR.COLORSPACE";
    /// <summary>The Profile Connection Space rule.</summary>
    public const string HeaderPcs = "ICC.HDR.PCS";
    /// <summary>The profile-flags rule.</summary>
    public const string HeaderFlags = "ICC.HDR.FLAGS";
    /// <summary>The primary-platform rule.</summary>
    public const string HeaderPlatform = "ICC.HDR.PLATFORM";
    /// <summary>The registered header-signature rule for CMM, manufacturer, model, and creator fields.</summary>
    public const string HeaderRegisteredSignature = "ICC.HDR.REGISTERED_SIGNATURE";
    /// <summary>The device-attributes rule.</summary>
    public const string HeaderDeviceAttributes = "ICC.HDR.ATTRIBUTES";
    /// <summary>The rendering-intent rule.</summary>
    public const string HeaderRenderingIntent = "ICC.HDR.INTENT";
    /// <summary>The PCS illuminant rule.</summary>
    public const string HeaderPcsIlluminant = "ICC.HDR.ILLUMINANT";
    /// <summary>The Profile ID rule.</summary>
    public const string HeaderProfileId = "ICC.HDR.PROFILE_ID";
    /// <summary>The ICC.2 spectral PCS and wavelength-range rule.</summary>
    public const string HeaderSpectralPcs = "ICC.HDR.SPECTRAL_PCS";
    /// <summary>The ICC.2 Material Connection Space rule.</summary>
    public const string HeaderMcs = "ICC.HDR.MCS";
    /// <summary>The tag-table count rule.</summary>
    public const string TagTableCount = "ICC.TABLE.COUNT";
    /// <summary>The tag-table bounds rule.</summary>
    public const string TagTableBounds = "ICC.TABLE.BOUNDS";
    /// <summary>The tag-table alignment rule.</summary>
    public const string TagTableAlignment = "ICC.TABLE.ALIGNMENT";
    /// <summary>The duplicate tag-signature rule.</summary>
    public const string TagTableDuplicateSignature = "ICC.TABLE.DUPLICATE_SIGNATURE";
    /// <summary>The overlapping tag-payload rule.</summary>
    public const string TagTableOverlap = "ICC.TABLE.OVERLAP";
    /// <summary>The profile-gap rule.</summary>
    public const string TagTableGap = "ICC.TABLE.GAP";
    /// <summary>The tag-padding rule.</summary>
    public const string TagTablePadding = "ICC.TABLE.PADDING";
    /// <summary>The exact profile-end rule.</summary>
    public const string ProfileExactEnd = "ICC.PROFILE.EXACT_END";
    /// <summary>The tag signature/type compatibility rule.</summary>
    public const string TagTypeCompatibility = "ICC.TAG.TYPE_COMPATIBILITY";
    /// <summary>The tag reserved-bytes rule.</summary>
    public const string TagReserved = "ICC.TAG.RESERVED";
    /// <summary>The tag-size rule.</summary>
    public const string TagSize = "ICC.TAG.SIZE";
    /// <summary>The nested-offset ownership rule.</summary>
    public const string TagNestedOffset = "ICC.TAG.NESTED_OFFSET";
    /// <summary>The text-encoding rule.</summary>
    public const string TextEncoding = "ICC.TAG.TEXT_ENCODING";
    /// <summary>The colour-channel-count rule.</summary>
    public const string ChannelCount = "ICC.DATA.CHANNEL_COUNT";
    /// <summary>The curve-validity rule.</summary>
    public const string CurveValidity = "ICC.DATA.CURVE";
    /// <summary>The CLUT-dimensions rule.</summary>
    public const string ClutDimensions = "ICC.DATA.CLUT_DIMENSIONS";
    /// <summary>The matrix-invertibility rule.</summary>
    public const string MatrixInvertibility = "ICC.DATA.MATRIX_INVERTIBILITY";
    /// <summary>The processing-pipeline channel-continuity rule.</summary>
    public const string PipelineChannelContinuity = "ICC.MPE.CHANNEL_CONTINUITY";
    /// <summary>The calculator safety rule.</summary>
    public const string CalculatorSafety = "ICC.MAX.CALCULATOR_SAFETY";
    /// <summary>The compressed-payload expansion rule.</summary>
    public const string CompressionLimit = "ICC.MAX.COMPRESSION_LIMIT";
    /// <summary>The required-tag rule.</summary>
    public const string RequiredTag = "ICC.SEMANTIC.REQUIRED_TAG";
    /// <summary>The forbidden-tag rule.</summary>
    public const string ForbiddenTag = "ICC.SEMANTIC.FORBIDDEN_TAG";
    /// <summary>The profile-class semantic rule.</summary>
    public const string ProfileClassSemantics = "ICC.SEMANTIC.PROFILE_CLASS";
    /// <summary>The ICC.2 hierarchy rule.</summary>
    public const string IccMaxHierarchy = "ICC.MAX.HIERARCHY";
    /// <summary>The ICC.2 spectral consistency rule.</summary>
    public const string IccMaxSpectral = "ICC.MAX.SPECTRAL";
    /// <summary>The ICC.2 profile connection conditions rule.</summary>
    public const string IccMaxPcc = "ICC.MAX.PCC";
    /// <summary>The ICC.2 Material Connection Space rule.</summary>
    public const string IccMaxMcs = "ICC.MAX.MCS";
    /// <summary>The resource-limit security rule.</summary>
    public const string SecurityResourceLimit = "ICC.SECURITY.RESOURCE_LIMIT";
    /// <summary>The round-trip quality rule.</summary>
    public const string QualityRoundTrip = "ICC.QUALITY.ROUND_TRIP";
    /// <summary>The transform-smoothness quality rule.</summary>
    public const string QualitySmoothness = "ICC.QUALITY.SMOOTHNESS";
    /// <summary>The embedded-profile carrier rule.</summary>
    public const string EmbeddedProfile = "ICC.EMBEDDED.PROFILE";
}
