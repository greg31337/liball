namespace unicode;

/// <summary>
/// Defines the behavioral policy used to prepare, compare, and validate Unicode identifiers.
/// </summary>
/// <remarks>
/// Implement this interface to supply application-specific identifier rules without deriving from
/// <see cref="UnicodeIdentifierProfile"/> or exposing mutable configuration fields.
/// </remarks>
public interface IUnicodeIdentifierProfile
{
    /// <summary>
    /// Reports whether a code point may start an identifier.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the code point is allowed; otherwise, <c>false</c>.</returns>
    bool AllowsStart(int codePoint);

    /// <summary>
    /// Reports whether a code point may continue an identifier.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the code point is allowed; otherwise, <c>false</c>.</returns>
    bool AllowsContinue(int codePoint);

    /// <summary>
    /// Reports whether unassigned Unicode code points are accepted when other character rules reject them.
    /// </summary>
    /// <returns><c>true</c> when unassigned code points are accepted; otherwise, <c>false</c>.</returns>
    bool AllowsUnassignedCodePoints();

    /// <summary>
    /// Reports whether code points with the Pattern_Syntax property are rejected.
    /// </summary>
    /// <returns><c>true</c> when Pattern_Syntax code points are rejected; otherwise, <c>false</c>.</returns>
    bool DisallowsPatternSyntax();

    /// <summary>
    /// Reports whether code points with the Pattern_White_Space property are rejected.
    /// </summary>
    /// <returns><c>true</c> when Pattern_White_Space code points are rejected; otherwise, <c>false</c>.</returns>
    bool DisallowsPatternWhiteSpace();

    /// <summary>
    /// Reports whether text satisfies the profile's normalization requirement.
    /// </summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <returns><c>true</c> when the normalization requirement is satisfied; otherwise, <c>false</c>.</returns>
    bool IsNormalizationSatisfied(string text);

    /// <summary>
    /// Prepares text using the profile's normalization policy.
    /// </summary>
    /// <param name="text">The UTF-16 text to prepare.</param>
    /// <returns>The prepared UTF-16 text.</returns>
    string Normalize(string text);

    /// <summary>
    /// Produces the profile-specific key used for identifier comparison.
    /// </summary>
    /// <param name="text">The UTF-16 text to prepare for comparison.</param>
    /// <returns>The comparison key.</returns>
    string GetCompareKey(string text);
}
