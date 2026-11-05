namespace uri;

/// <summary>
/// Analyzes and normalizes strict URI-reference text.
/// </summary>
public interface IUriNormalizer
{
    /// <summary>Reports generic and explicitly profiled transformations without changing the input.</summary>
    /// <param name="text">The complete URI-reference text.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The required syntax and explicit-profile transformations in order.</returns>
    /// <exception cref="UriNormalizationException">Validation or a configured limit fails.</exception>
    UriNormalizationChange[] AnalyzeNormalization(
        string text,
        UriNormalizationOptions options);

    /// <summary>Applies generic syntax normalization.</summary>
    /// <param name="text">The complete URI-reference text.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized URI-reference text.</returns>
    /// <exception cref="UriNormalizationException">Validation or a configured limit fails.</exception>
    string NormalizeSyntax(string text, UriNormalizationOptions options);

    /// <summary>Applies generic normalization and an explicit or built-in scheme profile.</summary>
    /// <param name="text">The complete URI-reference text.</param>
    /// <param name="options">The normalization options and optional profile.</param>
    /// <returns>The normalized URI-reference text.</returns>
    /// <exception cref="UriNormalizationException">Validation, profile selection, or a configured limit fails.</exception>
    string NormalizeWithProfile(string text, UriNormalizationOptions options);
}
