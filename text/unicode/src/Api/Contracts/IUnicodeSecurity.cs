namespace unicode;

/// <summary>
/// Provides Unicode spoofing and identifier security operations.
/// </summary>
public interface IUnicodeSecurity
{
    /// <summary>
    /// Returns the NFD-normalized UTS #39 confusable skeleton for text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string GetSkeleton(string text);

    /// <summary>
    /// Reports whether two strings have the same normalized UTS #39 confusable skeleton.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    bool AreConfusable(string left, string right);

    /// <summary>
    /// Reports whether two confusable strings have disjoint resolved script sets.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns><c>true</c> when the strings are UTS #39 mixed-script confusables; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    bool AreMixedScriptConfusable(string left, string right);

    /// <summary>
    /// Reports whether two strings are mixed-script confusables and each string is single-script.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns><c>true</c> when the strings are UTS #39 whole-script confusables; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    bool AreWholeScriptConfusable(string left, string right);

    /// <summary>
    /// Returns confusable flags derived from comparing two distinct strings.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns>The confusable flags produced by the comparison.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    UnicodeSecurityFlag GetConfusableFlags(string left, string right);

    /// <summary>
    /// Returns the UTS #39 Identifier_Status value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The Identifier_Status classification.</returns>
    UnicodeIdentifierSecurityStatus GetIdentifierStatus(int codePoint);

    /// <summary>
    /// Returns every UTS #39 Identifier_Type value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing the Identifier_Type values.</returns>
    UnicodeIdentifierType[] GetIdentifierTypes(int codePoint);

    /// <summary>
    /// Reports whether nonempty text is permitted by the UTS #39 General Security Profile, including canonical equivalents.
    /// </summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <returns><c>true</c> when the text is allowed; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool IsAllowedByGeneralSecurityProfile(string text);

    /// <summary>
    /// Returns distinct significant scripts, excluding Common and Inherited.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeScript[] GetScriptSet(string text);

    /// <summary>
    /// Reports whether text uses more than one significant script.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool IsMixedScript(string text);

    /// <summary>
    /// Classifies text with the UTS #39 restriction-level rules.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting classification or status.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeRestrictionLevel GetRestrictionLevel(string text);

    /// <summary>
    /// Reports invisible and control scalars together with identifier bidi-safety violations.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeSecurityDiagnostic[] GetSecurityDiagnostics(string text);

    /// <summary>
    /// Reports RFC 5893 Bidi Rule violations when an identifier contains right-to-left text.
    /// </summary>
    /// <param name="text">The UTF-16 identifier to inspect.</param>
    /// <returns>A new array containing identifier bidi-safety diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeSecurityDiagnostic[] GetIdentifierBidiDiagnostics(string text);

    /// <summary>
    /// Reports violations of the six RFC 5893 Bidi Rule conditions for a label.
    /// </summary>
    /// <param name="label">The UTF-16 label to inspect.</param>
    /// <returns>A new array containing label bidi-safety diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>label</c> is <c>null</c>.</exception>
    UnicodeSecurityDiagnostic[] GetLabelBidiDiagnostics(string label);

    /// <summary>
    /// Runs behavioral identifier-profile validation and Unicode security checks as one operation.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    UnicodeSecurityCheckResult CheckIdentifier(string text, IUnicodeIdentifierProfile profile);

    /// <summary>
    /// Runs identifier checks and compares the identifier with a protected reference string.
    /// </summary>
    /// <param name="text">The UTF-16 identifier to process.</param>
    /// <param name="reference">The distinct protected string to compare.</param>
    /// <param name="profile">The behavioral profile that defines preparation and validation rules.</param>
    /// <returns>An identifier result whose confusable flags reflect the comparison.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>reference</c>, <c>profile</c> is <c>null</c>.</exception>
    UnicodeSecurityCheckResult CheckIdentifierAgainst(
        string text,
        string reference,
        IUnicodeIdentifierProfile profile);
}
