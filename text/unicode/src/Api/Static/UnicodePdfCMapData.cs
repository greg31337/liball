namespace unicode;

/// <summary>
/// Provides constants and stable diagnostic rule identifiers for PDF CMaps.
/// </summary>
public static class UnicodePdfCMapData
{
    /// <summary>
    /// The maximum number of entries emitted in one PDF CMap mapping section.
    /// </summary>
    public const int MaxCMapSectionEntries = UnicodePdfCMap.MaxCMapSectionEntries;
    /// <summary>
    /// The stable diagnostic rule identifier for empty Map.
    /// </summary>
    public const string EmptyMapRule = UnicodePdfCMap.EmptyMapRule;
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Mapping.
    /// </summary>
    public const string InvalidMappingRule = UnicodePdfCMap.InvalidMappingRule;
    /// <summary>
    /// The stable diagnostic rule identifier for duplicate Character Code.
    /// </summary>
    public const string DuplicateCharacterCodeRule = UnicodePdfCMap.DuplicateCharacterCodeRule;
    /// <summary>
    /// The stable diagnostic rule identifier for missing Unicode Scalars.
    /// </summary>
    public const string MissingUnicodeScalarsRule = UnicodePdfCMap.MissingUnicodeScalarsRule;
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Unicode Scalar.
    /// </summary>
    public const string InvalidUnicodeScalarRule = UnicodePdfCMap.InvalidUnicodeScalarRule;
    /// <summary>
    /// The stable diagnostic rule identifier for replacement Character.
    /// </summary>
    public const string ReplacementCharacterRule = UnicodePdfCMap.ReplacementCharacterRule;
    /// <summary>
    /// The stable diagnostic rule identifier for an invalid CMap font name.
    /// </summary>
    public const string InvalidFontNameRule = UnicodePdfCMap.InvalidFontNameRule;
}
