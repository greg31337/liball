namespace unicode;

/// <summary>
/// Provides constants used by algorithmic Hangul processing.
/// </summary>
public static class UnicodeHangulData
{
    /// <summary>
    /// The first precomposed Hangul syllable, U+AC00.
    /// </summary>
    public const int SBase = UnicodeHangul.SBase;
    /// <summary>
    /// The first modern leading Jamo, U+1100.
    /// </summary>
    public const int LBase = UnicodeHangul.LBase;
    /// <summary>
    /// The first modern vowel Jamo, U+1161.
    /// </summary>
    public const int VBase = UnicodeHangul.VBase;
    /// <summary>
    /// One less than the first modern trailing Jamo, used by the Unicode composition formula.
    /// </summary>
    public const int TBase = UnicodeHangul.TBase;
    /// <summary>
    /// The number of modern leading Jamo used by algorithmic composition.
    /// </summary>
    public const int LCount = UnicodeHangul.LCount;
    /// <summary>
    /// The number of modern vowel Jamo used by algorithmic composition.
    /// </summary>
    public const int VCount = UnicodeHangul.VCount;
    /// <summary>
    /// The number of trailing-Jamo states, including the no-trailing-Jamo state.
    /// </summary>
    public const int TCount = UnicodeHangul.TCount;
    /// <summary>
    /// The number of syllables for each leading Jamo.
    /// </summary>
    public const int NCount = UnicodeHangul.NCount;
    /// <summary>
    /// The total number of algorithmically composed modern Hangul syllables.
    /// </summary>
    public const int SCount = UnicodeHangul.SCount;
}

