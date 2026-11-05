namespace uri;

/// <summary>
/// Builds URI references from components and creates validated edited copies.
/// </summary>
public interface IUriEditor
{
    /// <summary>Builds validated URI-reference text from a component model.</summary>
    /// <param name="components">The component model to build.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The built URI-reference text.</returns>
    /// <exception cref="UriValidationException">Component validation or a configured limit fails.</exception>
    string BuildFromComponents(
        UriReference components,
        UriValidationOptions options);

    /// <summary>Builds URI-reference text without throwing for expected failure.</summary>
    /// <param name="components">The component model to build.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The build outcome.</returns>
    UriTextResult TryBuildFromComponents(
        UriReference components,
        UriValidationOptions options);

    /// <summary>Creates validated URI-reference text by applying explicit component changes.</summary>
    /// <param name="source">The source URI-reference text.</param>
    /// <param name="changes">The component changes; unselected components are kept.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The edited URI-reference text.</returns>
    /// <exception cref="UriValidationException">Source or edited component validation, or a configured limit, fails.</exception>
    string CopyWithChanges(
        string source,
        UriReferenceChanges changes,
        UriValidationOptions options);
}
