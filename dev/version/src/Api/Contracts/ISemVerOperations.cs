namespace version;

/// <summary>
/// Defines Semantic Versioning parsing, coercion, comparison, identity, and formatting operations.
/// </summary>
/// <remarks>
/// Diagnostic methods represent malformed input with result objects, while their fail-fast counterparts throw for malformed input.
/// </remarks>
public interface ISemVerOperations
{
    /// <summary>
    /// Parses version text using strict Semantic Versioning rules and throws when the text is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting universal version.</returns>
    public UniversalVersion ParseStrict(string text);

    /// <summary>
    /// Attempts to parse version text using strict Semantic Versioning rules.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionParseResult TryParseStrict(string text);

    /// <summary>
    /// Parses version text using the accepted loose-input rules and throws when the text is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting universal version.</returns>
    public UniversalVersion ParseLoose(string text);

    /// <summary>
    /// Attempts to parse version text using the accepted loose-input rules.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionParseResult TryParseLoose(string text);

    /// <summary>
    /// Extracts the first numeric version tuple, fills omitted components with zero, and throws when no tuple is found.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting universal version.</returns>
    public UniversalVersion Coerce(string text);

    /// <summary>
    /// Attempts to extract the first numeric version tuple and fills omitted components with zero.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionParseResult TryCoerce(string text);

    /// <summary>
    /// Parses and compares two strict versions using Semantic Versioning precedence rules.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public int ComparePrecedence(string leftText, string rightText);

    /// <summary>
    /// Parses and compares two strict versions using precedence and build metadata identity.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public int CompareIdentity(string leftText, string rightText);

    /// <summary>
    /// Reports whether two strict versions have the same normalized package identity.
    /// </summary>
    /// <remarks>
    /// Semantic Versioning build metadata is excluded from normalized package identity.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the normalized package identities are equal; otherwise, <c>false</c>.</returns>
    public bool EqualsNormalizedPackageIdentity(string leftText, string rightText);

    /// <summary>
    /// Formats a parsed Semantic Versioning value in canonical form.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatCanonical(UniversalVersion version);
}
