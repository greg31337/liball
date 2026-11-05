namespace unicode;

/// <summary>
/// Provides constants used by Unicode normalization.
/// </summary>
public static class UnicodeNormalizationData
{
    /// <summary>
    /// U+034F COMBINING GRAPHEME JOINER, inserted to make text stream-safe.
    /// </summary>
    public const int CombiningGraphemeJoiner = UnicodeNormalization.CombiningGraphemeJoiner;
    /// <summary>
    /// The maximum consecutive non-starters permitted by Unicode stream-safe text format.
    /// </summary>
    public const int StreamSafeMaxNonStarters = UnicodeNormalization.StreamSafeMaxNonStarters;
}

