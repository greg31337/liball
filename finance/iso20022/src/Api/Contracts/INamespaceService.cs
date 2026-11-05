namespace iso20022;

/// <summary>Parses, classifies, normalizes, and constructs ISO 20022 namespaces.</summary>
public interface INamespaceService
{
    /// <summary>Parses a namespace URI using the requested strict or lenient policy.</summary>
    /// <param name="namespaceUri">The namespace URI.</param>
    /// <param name="mode">The namespace-processing mode.</param>
    /// <returns>The namespace result and any ordered repairs.</returns>
    NamespaceServiceResult ParseNamespace(string? namespaceUri, NamespaceMode mode);

    /// <summary>Constructs the canonical namespace for a message identifier.</summary>
    /// <param name="identifier">The message identifier.</param>
    /// <returns>The namespace result.</returns>
    NamespaceServiceResult BuildNamespaceFromIdentifier(MessageIdentifier? identifier);
}
