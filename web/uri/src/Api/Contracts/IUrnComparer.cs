namespace uri;

/// <summary>
/// Compares RFC 8141 URNs exactly, by assigned name, or with explicit namespace rules.
/// </summary>
public interface IUrnComparer
{
    /// <summary>Compares complete URN component text and delimiter presence exactly.</summary>
    /// <param name="left">The first complete URN text.</param>
    /// <param name="right">The second complete URN text.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The exact comparison outcome.</returns>
    UrnEquivalenceResult CompareUrnExact(
        string left,
        string right,
        UrnParseOptions options);

    /// <summary>Compares assigned names using generic RFC 8141 equivalence.</summary>
    /// <param name="left">The first complete URN text.</param>
    /// <param name="right">The second complete URN text.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The assigned-name comparison outcome.</returns>
    UrnEquivalenceResult CompareUrnAssignedName(
        string left,
        string right,
        UrnParseOptions options);

    /// <summary>Compares assigned names with an explicitly supplied namespace handler.</summary>
    /// <param name="left">The first complete URN text.</param>
    /// <param name="right">The second complete URN text.</param>
    /// <param name="options">The validation, namespace-handler, and processing options.</param>
    /// <returns>The namespace-specific comparison outcome.</returns>
    UrnEquivalenceResult CompareUrnNamespaceSpecific(
        string left,
        string right,
        UrnParseOptions options);
}
