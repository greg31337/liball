namespace uri;

/// <summary>
/// Compares strict URI references at exact, syntax, and scheme-profile levels.
/// </summary>
public interface IUriComparer
{
    /// <summary>Compares serialized component text and delimiter presence exactly.</summary>
    /// <param name="left">The first URI-reference text.</param>
    /// <param name="right">The second URI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    UriComparisonResult CompareExact(
        string left,
        string right,
        UriComparisonOptions options);

    /// <summary>Compares after generic syntax normalization.</summary>
    /// <param name="left">The first URI-reference text.</param>
    /// <param name="right">The second URI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    UriComparisonResult CompareSyntax(
        string left,
        string right,
        UriComparisonOptions options);

    /// <summary>Compares after generic and explicit or built-in profile normalization.</summary>
    /// <param name="left">The first URI-reference text.</param>
    /// <param name="right">The second URI-reference text.</param>
    /// <param name="options">The comparison options and optional profile.</param>
    /// <returns>The comparison outcome.</returns>
    UriComparisonResult CompareWithProfile(
        string left,
        string right,
        UriComparisonOptions options);
}
