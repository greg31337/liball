namespace version;

/// <summary>
/// Provides public Semantic Versioning parsing, coercion, comparison, identity, and formatting operations.
/// </summary>
/// <remarks>
/// This class is stateless and applies strict Semantic Versioning rules unless a method explicitly selects loose input or coercion.
/// </remarks>
public class SemVerOperations : ISemVerOperations
{
    /// <summary>
    /// Initializes a stateless Semantic Versioning operations service.
    /// </summary>
    public SemVerOperations()
    {
    }

    /// <summary>
    /// Parses version text using strict Semantic Versioning rules and throws when the text is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">The text is not a strict Semantic Versioning version.</exception>
    public UniversalVersion ParseStrict(string text)
    {
        return GetRequiredVersion(TryParseStrict(text), "Invalid strict SemVer version text.");
    }

    /// <summary>
    /// Attempts to parse version text using strict Semantic Versioning rules.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionParseResult TryParseStrict(string text)
    {
        return SemVerParser.ParseStrict(text);
    }

    /// <summary>
    /// Parses version text using the accepted loose-input rules and throws when the text is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">The text is not accepted by the loose-input rules.</exception>
    public UniversalVersion ParseLoose(string text)
    {
        return GetRequiredVersion(TryParseLoose(text), "Invalid loose SemVer version text.");
    }

    /// <summary>
    /// Attempts to parse version text using the accepted loose-input rules.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionParseResult TryParseLoose(string text)
    {
        return SemVerParser.ParseLoose(text);
    }

    /// <summary>
    /// Extracts the first numeric version tuple, fills omitted components with zero, and throws when no tuple is found.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">The text does not contain a coercible version.</exception>
    public UniversalVersion Coerce(string text)
    {
        return GetRequiredVersion(TryCoerce(text), "Input does not contain a coercible SemVer version.");
    }

    /// <summary>
    /// Attempts to extract the first numeric version tuple and fills omitted components with zero.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionParseResult TryCoerce(string text)
    {
        return SemVerParser.Coerce(text);
    }

    /// <summary>
    /// Parses and compares two strict versions using Semantic Versioning precedence rules.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is not a strict Semantic Versioning version.</exception>
    public int ComparePrecedence(string leftText, string rightText)
    {
        UniversalVersion left = ParseStrict(leftText);
        UniversalVersion right = ParseStrict(rightText);
        return SemVerVersionComparer.ComparePrecedence(left, right);
    }

    /// <summary>
    /// Parses and compares two strict versions using precedence and build metadata identity.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is not a strict Semantic Versioning version.</exception>
    public int CompareIdentity(string leftText, string rightText)
    {
        UniversalVersion left = ParseStrict(leftText);
        UniversalVersion right = ParseStrict(rightText);
        return SemVerVersionComparer.CompareIdentity(left, right);
    }

    /// <summary>
    /// Reports whether two strict versions have the same normalized package identity.
    /// </summary>
    /// <remarks>
    /// Semantic Versioning build metadata is excluded from normalized package identity.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the normalized package identities are equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is not a strict Semantic Versioning version.</exception>
    public bool EqualsNormalizedPackageIdentity(string leftText, string rightText)
    {
        UniversalVersion left = ParseStrict(leftText);
        UniversalVersion right = ParseStrict(rightText);
        return SemVerVersionComparer.EqualsByNormalizedIdentity(left, right);
    }

    /// <summary>
    /// Formats a parsed Semantic Versioning value in canonical form.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">The value does not use Semantic Versioning semantics.</exception>
    public string FormatCanonical(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        VersionDialect dialect = version.GetDialect();
        if (dialect != VersionDialect.SemVer && dialect != VersionDialect.NodeSemVer)
        {
            throw new System.ArgumentException("Version must use SemVer semantics.", "version");
        }

        return VersionFormatter.FormatCanonical(version);
    }

    internal UniversalVersion GetRequiredVersion(VersionParseResult result, string message)
    {
        if (result.IsFailure())
        {
            throw new System.ArgumentException(message, "text");
        }

        return result.GetVersion();
    }
}
