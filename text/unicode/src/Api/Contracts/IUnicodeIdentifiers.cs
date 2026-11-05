namespace unicode;

/// <summary>
/// Provides Unicode identifier classification, preparation, and validation operations.
/// </summary>
public interface IUnicodeIdentifiers
{
    /// <summary>
    /// Reports whether a code point has the Unicode ID_Start property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsIdStart(int codePoint);

    /// <summary>
    /// Reports whether a code point has the Unicode ID_Continue property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsIdContinue(int codePoint);

    /// <summary>
    /// Reports whether a code point has the Unicode XID_Start property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsXidStart(int codePoint);

    /// <summary>
    /// Reports whether a code point has the Unicode XID_Continue property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsXidContinue(int codePoint);

    /// <summary>
    /// Reports whether a code point has the Unicode Pattern_Syntax property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsPatternSyntax(int codePoint);

    /// <summary>
    /// Reports whether a code point has the Unicode Pattern_White_Space property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsPatternWhiteSpace(int codePoint);

    /// <summary>
    /// Returns the Unicode code-point ranges with the ID_Start property.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    UnicodeRangeSet GetIdStartRangeSet();

    /// <summary>
    /// Reports whether text is a valid default XID identifier.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool IsIdentifier(string text);

    /// <summary>
    /// Applies the normalization policy supplied by an identifier profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    string NormalizeIdentifier(string text, IUnicodeIdentifierProfile profile);

    /// <summary>
    /// Produces an identifier comparison key using a behavioral profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    string GetIdentifierCompareKey(string text, IUnicodeIdentifierProfile profile);

    /// <summary>
    /// Reports whether two identifiers produce the same comparison key under a behavioral profile.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c>, <c>profile</c> is <c>null</c>.</exception>
    bool EqualsIdentifier(string left, string right, IUnicodeIdentifierProfile profile);

    /// <summary>
    /// Validates every scalar against a behavioral identifier profile and returns ordered diagnostics.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    UnicodeIdentifierValidationResult ValidateIdentifier(string text, IUnicodeIdentifierProfile profile);
}
