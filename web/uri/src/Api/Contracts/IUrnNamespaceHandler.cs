namespace uri;

/// <summary>
/// Supplies optional application-defined validation, normalization, and equivalence rules for a URN namespace.
/// </summary>
public interface IUrnNamespaceHandler
{
    /// <summary>Validates a namespace-specific string using namespace-defined rules.</summary>
    /// <param name="namespaceIdentifier">The namespace identifier.</param>
    /// <param name="namespaceSpecificString">The namespace-specific string.</param>
    /// <returns>A failure diagnostic, or <c>null</c> when the value is valid.</returns>
    UriDiagnostic? ValidateNamespaceSpecificString(
        string namespaceIdentifier,
        string namespaceSpecificString);

    /// <summary>Normalizes a namespace-specific string using namespace-defined rules.</summary>
    /// <param name="namespaceIdentifier">The namespace identifier.</param>
    /// <param name="namespaceSpecificString">The namespace-specific string.</param>
    /// <returns>The normalization outcome.</returns>
    UriTextResult NormalizeNamespaceSpecificString(
        string namespaceIdentifier,
        string namespaceSpecificString);

    /// <summary>Compares two namespace-specific strings using namespace-defined rules.</summary>
    /// <param name="namespaceIdentifier">The namespace identifier.</param>
    /// <param name="leftNamespaceSpecificString">The left namespace-specific string.</param>
    /// <param name="rightNamespaceSpecificString">The right namespace-specific string.</param>
    /// <returns>The namespace-specific equivalence outcome.</returns>
    UrnEquivalenceResult CompareNamespaceSpecificStrings(
        string namespaceIdentifier,
        string leftNamespaceSpecificString,
        string rightNamespaceSpecificString);
}
