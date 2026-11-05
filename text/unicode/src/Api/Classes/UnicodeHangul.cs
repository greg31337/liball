namespace unicode;

/// <summary>
/// Provides the algorithmic Hangul decomposition and composition operations defined by Unicode.
/// </summary>
public class UnicodeHangul : IUnicodeHangul
{
    internal static readonly UnicodeHangul Instance = new UnicodeHangul();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeHangul()
    {
    }
    /// <summary>
    /// The first precomposed Hangul syllable, U+AC00.
    /// </summary>
    internal const int SBase = 0xAC00;
    /// <summary>
    /// The first modern leading Jamo, U+1100.
    /// </summary>
    internal const int LBase = 0x1100;
    /// <summary>
    /// The first modern vowel Jamo, U+1161.
    /// </summary>
    internal const int VBase = 0x1161;
    /// <summary>
    /// One less than the first modern trailing Jamo, used by the Unicode composition formula.
    /// </summary>
    internal const int TBase = 0x11A7;
    /// <summary>
    /// The number of modern leading Jamo used by algorithmic composition.
    /// </summary>
    internal const int LCount = 19;
    /// <summary>
    /// The number of modern vowel Jamo used by algorithmic composition.
    /// </summary>
    internal const int VCount = 21;
    /// <summary>
    /// The number of trailing-Jamo states, including the no-trailing-Jamo state.
    /// </summary>
    internal const int TCount = 28;
    /// <summary>
    /// The number of syllables for each leading Jamo.
    /// </summary>
    internal const int NCount = 588;
    /// <summary>
    /// The total number of algorithmically composed modern Hangul syllables.
    /// </summary>
    internal const int SCount = 11172;

    /// <summary>
    /// Reports whether a code point is a precomposed modern Hangul syllable.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsHangulSyllable(int codePoint)
    {
        if (codePoint < SBase)
        {
            return false;
        }

        return codePoint < SBase + SCount;
    }

    /// <summary>
    /// Reports whether a code point is a modern leading Hangul Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsLeadingJamo(int codePoint)
    {
        if (codePoint < LBase)
        {
            return false;
        }

        return codePoint < LBase + LCount;
    }

    /// <summary>
    /// Reports whether a code point is a modern vowel Hangul Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsVowelJamo(int codePoint)
    {
        if (codePoint < VBase)
        {
            return false;
        }

        return codePoint < VBase + VCount;
    }

    /// <summary>
    /// Reports whether a code point is a modern trailing Hangul Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsTrailingJamo(int codePoint)
    {
        if (codePoint <= TBase)
        {
            return false;
        }

        return codePoint < TBase + TCount;
    }

    /// <summary>
    /// Reports whether a precomposed Hangul syllable has no trailing Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsLvSyllable(int codePoint)
    {
        if (!IsHangulSyllable(codePoint))
        {
            return false;
        }

        return ((codePoint - SBase) % TCount) == 0;
    }

    /// <summary>
    /// Reports whether a precomposed Hangul syllable has a trailing Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsLvtSyllable(int codePoint)
    {
        if (!IsHangulSyllable(codePoint))
        {
            return false;
        }

        return !IsLvSyllable(codePoint);
    }

    /// <summary>
    /// Algorithmically decomposes a precomposed Hangul syllable into leading, vowel, and optional trailing Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing the resulting values.</returns>
    public int[] DecomposeSyllable(int codePoint)
    {
        if (!IsHangulSyllable(codePoint))
        {
            return new int[]
            {
            };
        }

        int syllableIndex = codePoint - SBase;
        int leading = LBase + (syllableIndex / NCount);
        int vowel = VBase + ((syllableIndex % NCount) / TCount);
        int trailingIndex = syllableIndex % TCount;

        if (trailingIndex == 0)
        {
            return new int[]
            {
                leading,
                vowel,
            };
        }

        return new int[]
        {
            leading,
            vowel,
            TBase + trailingIndex,
        };
    }

    /// <summary>
    /// Algorithmically composes valid Hangul Jamo into a precomposed syllable, or returns <c>-1</c> when they do not compose.
    /// </summary>
    /// <param name="leadingJamo">A modern Hangul leading Jamo.</param>
    /// <param name="vowelJamo">A modern Hangul vowel Jamo.</param>
    /// <returns>The resulting integer value.</returns>
    public int ComposeJamoPair(int leadingJamo, int vowelJamo)
    {
        if (!IsLeadingJamo(leadingJamo))
        {
            return -1;
        }

        if (!IsVowelJamo(vowelJamo))
        {
            return -1;
        }

        int leadingIndex = leadingJamo - LBase;
        int vowelIndex = vowelJamo - VBase;
        return SBase + ((leadingIndex * VCount) + vowelIndex) * TCount;
    }

    /// <summary>
    /// Algorithmically composes valid Hangul Jamo into a precomposed syllable, or returns <c>-1</c> when they do not compose.
    /// </summary>
    /// <param name="leadingJamo">A modern Hangul leading Jamo.</param>
    /// <param name="vowelJamo">A modern Hangul vowel Jamo.</param>
    /// <param name="trailingJamo">A modern Hangul trailing Jamo.</param>
    /// <returns>The resulting integer value.</returns>
    public int ComposeJamoSequence(int leadingJamo, int vowelJamo, int trailingJamo)
    {
        int syllable = ComposeJamoPair(leadingJamo, vowelJamo);
        if (syllable < 0)
        {
            return -1;
        }

        if (!IsTrailingJamo(trailingJamo))
        {
            return -1;
        }

        return syllable + (trailingJamo - TBase);
    }

    /// <summary>
    /// Algorithmically composes a Hangul pair when possible, or returns <c>-1</c>.
    /// </summary>
    /// <param name="first">The first value or normalized text segment.</param>
    /// <param name="second">The second value or text segment.</param>
    /// <returns>The resulting integer value.</returns>
    public int ComposePair(int first, int second)
    {
        int syllable = ComposeJamoPair(first, second);
        if (syllable >= 0)
        {
            return syllable;
        }

        if (!IsLvSyllable(first))
        {
            return -1;
        }

        if (!IsTrailingJamo(second))
        {
            return -1;
        }

        return first + (second - TBase);
    }
}
