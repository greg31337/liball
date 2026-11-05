namespace uri;

/// <summary>
/// Applies generic and explicitly supplied namespace-specific normalization to RFC 8141 URNs.
/// </summary>
public interface IUrnNormalizer
{
    /// <summary>Normalizes a complete URN while preserving its optional components.</summary>
    /// <param name="text">The complete URN text.</param>
    /// <param name="options">The validation, namespace-handler, and processing options.</param>
    /// <returns>The normalized complete URN text.</returns>
    /// <exception cref="UrnEquivalenceException">Validation, normalization, or a configured limit fails.</exception>
    string NormalizeUrn(string text, UrnParseOptions options);

    /// <summary>Normalizes and returns only the assigned-name portion of a URN.</summary>
    /// <param name="text">The complete URN text.</param>
    /// <param name="options">The validation, namespace-handler, and processing options.</param>
    /// <returns>The normalized assigned-name text without optional components.</returns>
    /// <exception cref="UrnEquivalenceException">Validation, normalization, or a configured limit fails.</exception>
    string NormalizeAssignedName(string text, UrnParseOptions options);
}
