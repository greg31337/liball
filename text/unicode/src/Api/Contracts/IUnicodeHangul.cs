namespace unicode;

/// <summary>
/// Provides algorithmic Hangul decomposition and composition operations.
/// </summary>
public interface IUnicodeHangul
{
    /// <summary>
    /// Reports whether a code point is a precomposed modern Hangul syllable.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsHangulSyllable(int codePoint);

    /// <summary>
    /// Reports whether a code point is a modern leading Hangul Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsLeadingJamo(int codePoint);

    /// <summary>
    /// Reports whether a code point is a modern vowel Hangul Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsVowelJamo(int codePoint);

    /// <summary>
    /// Reports whether a code point is a modern trailing Hangul Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsTrailingJamo(int codePoint);

    /// <summary>
    /// Reports whether a precomposed Hangul syllable has no trailing Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsLvSyllable(int codePoint);

    /// <summary>
    /// Reports whether a precomposed Hangul syllable has a trailing Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsLvtSyllable(int codePoint);

    /// <summary>
    /// Algorithmically decomposes a precomposed Hangul syllable into leading, vowel, and optional trailing Jamo.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing the resulting values.</returns>
    int[] DecomposeSyllable(int codePoint);

    /// <summary>
    /// Algorithmically composes valid Hangul Jamo into a precomposed syllable, or returns <c>-1</c> when they do not compose.
    /// </summary>
    /// <param name="leadingJamo">A modern Hangul leading Jamo.</param>
    /// <param name="vowelJamo">A modern Hangul vowel Jamo.</param>
    /// <returns>The resulting integer value.</returns>
    int ComposeJamoPair(int leadingJamo, int vowelJamo);

    /// <summary>
    /// Algorithmically composes valid Hangul Jamo into a precomposed syllable, or returns <c>-1</c> when they do not compose.
    /// </summary>
    /// <param name="leadingJamo">A modern Hangul leading Jamo.</param>
    /// <param name="vowelJamo">A modern Hangul vowel Jamo.</param>
    /// <param name="trailingJamo">A modern Hangul trailing Jamo.</param>
    /// <returns>The resulting integer value.</returns>
    int ComposeJamoSequence(int leadingJamo, int vowelJamo, int trailingJamo);

    /// <summary>
    /// Algorithmically composes a Hangul pair when possible, or returns <c>-1</c>.
    /// </summary>
    /// <param name="first">The first value or normalized text segment.</param>
    /// <param name="second">The second value or text segment.</param>
    /// <returns>The resulting integer value.</returns>
    int ComposePair(int first, int second);
}
