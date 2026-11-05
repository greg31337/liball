namespace unicode;

/// <summary>
/// Provides Unicode character property and name lookup operations.
/// </summary>
public interface IUnicodeProperties
{
    /// <summary>
    /// Returns the Unicode General_Category value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeGeneralCategory GetGeneralCategory(int codePoint);

    /// <summary>
    /// Returns the canonical combining class for a code point; unassigned values use class zero.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    int GetCombiningClass(int codePoint);

    /// <summary>
    /// Returns the Unicode Bidi_Class value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeBidiClass GetBidiClass(int codePoint);

    /// <summary>
    /// Reports whether the Unicode bidirectional mirrored property is assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsBidiMirrored(int codePoint);

    /// <summary>
    /// Returns the bidirectional mirroring glyph, or the input code point when no mapping is assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The mapped code point or the input value.</returns>
    int GetBidiMirror(int codePoint);

    /// <summary>
    /// Returns the bidirectional paired bracket, or the input code point when none is assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The paired bracket code point or the input value.</returns>
    int GetBidiPairedBracket(int codePoint);

    /// <summary>
    /// Returns the bidirectional paired-bracket type.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeBidiPairedBracketType GetBidiPairedBracketType(int codePoint);

    /// <summary>
    /// Returns the decomposition type of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeDecompositionType GetDecompositionType(int codePoint);

    /// <summary>
    /// Returns a copy of the direct decomposition mapping, or an empty array.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing the direct mapping.</returns>
    int[] GetDecompositionMapping(int codePoint);

    /// <summary>
    /// Returns the primary Unicode Script value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeScript GetScript(int codePoint);

    /// <summary>
    /// Returns Script_Extensions values when present, otherwise the primary script as a one-item array.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing the resulting values.</returns>
    UnicodeScript[] GetScriptExtensions(int codePoint);

    /// <summary>
    /// Returns the Unicode block containing a code point, or NoBlock when none is assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeBlock GetBlock(int codePoint);

    /// <summary>
    /// Returns the Unicode East_Asian_Width value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeEastAsianWidth GetEastAsianWidth(int codePoint);

    /// <summary>
    /// Returns the exact integer or rational Unicode numeric value, or <c>double.NaN</c>.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A numeric Unicode property value, or <c>double.NaN</c> when the scalar has no numeric value.</returns>
    double GetNumericValue(int codePoint);

    /// <summary>
    /// Returns the Unicode numeric type of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeNumericType GetNumericType(int codePoint);

    /// <summary>
    /// Returns the exact optional integer or rational Unicode numeric value.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A value containing the exact numerator and denominator when assigned.</returns>
    UnicodeRationalValue GetNumericValueExact(int codePoint);

    /// <summary>
    /// Returns the Unicode joining type of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeJoiningType GetJoiningType(int codePoint);

    /// <summary>
    /// Returns the Unicode joining group of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeJoiningGroup GetJoiningGroup(int codePoint);

    /// <summary>
    /// Returns the Unicode version in which a code point was assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeAge GetAge(int codePoint);

    /// <summary>
    /// Returns the Indic conjunct-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeIndicConjunctBreak GetIndicConjunctBreak(int codePoint);

    /// <summary>
    /// Returns the Hangul syllable type of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeHangulSyllableType GetHangulSyllableType(int codePoint);

    /// <summary>
    /// Returns the grapheme-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeGraphemeBreakClass GetGraphemeBreakClass(int codePoint);

    /// <summary>
    /// Returns the word-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeWordBreakClass GetWordBreakClass(int codePoint);

    /// <summary>
    /// Returns the sentence-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeSentenceBreakClass GetSentenceBreakClass(int codePoint);

    /// <summary>
    /// Returns the line-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeLineBreakClass GetLineBreakClass(int codePoint);

    /// <summary>
    /// Returns the Unicode name of a code point, including supported algorithmic names, or an empty string.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    string GetName(int codePoint);

    /// <summary>
    /// Returns caller-owned character-name alias records for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing new alias records.</returns>
    UnicodeNameAlias[] GetNameAliases(int codePoint);

    /// <summary>
    /// Looks up a Unicode character name case-insensitively and returns its code point, or <c>-1</c>.
    /// </summary>
    /// <param name="name">The name to store or look up; name lookup is case-insensitive.</param>
    /// <returns>The resulting integer value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is <c>null</c>.</exception>
    int LookupName(string name);

    /// <summary>
    /// Looks up a Unicode named sequence case-insensitively and returns a copy of its scalars, or an empty array.
    /// </summary>
    /// <param name="name">The name to store or look up; name lookup is case-insensitive.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is <c>null</c>.</exception>
    int[] LookupNamedSequence(string name);

    /// <summary>
    /// Returns caller-owned records for every normative named sequence.
    /// </summary>
    /// <returns>A new array containing new named-sequence records.</returns>
    UnicodeNamedSequence[] GetNamedSequences();

    /// <summary>
    /// Resolves a loosely matched property alias to its canonical long name.
    /// </summary>
    /// <param name="alias">A short, long, or additional property alias.</param>
    /// <returns>The canonical long name, or an empty string when unresolved.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>alias</c> is <c>null</c>.</exception>
    string ResolvePropertyAlias(string alias);

    /// <summary>
    /// Resolves a loosely matched property-value alias to its canonical long name.
    /// </summary>
    /// <param name="propertyAlias">A short, long, or additional property alias.</param>
    /// <param name="valueAlias">A short, long, or additional value alias.</param>
    /// <returns>The canonical long value name, or an empty string when unresolved.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    string ResolvePropertyValueAlias(string propertyAlias, string valueAlias);

    /// <summary>
    /// Reports whether a code point has the selected binary Unicode property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <param name="property">The binary Unicode property to test.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool HasBinaryProperty(int codePoint, UnicodeBinaryProperty property);
}
