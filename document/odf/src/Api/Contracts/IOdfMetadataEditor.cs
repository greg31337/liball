namespace odf;

/// <summary>
/// Provides typed editing and lookup for document metadata and RDF graphs.
/// </summary>
public interface IOdfMetadataEditor
{
    /// <summary>Gets the live standard metadata model.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The document metadata model.</returns>
    public OdfDocumentMetadata GetDocumentMetadata(OdfDocument document);
    /// <summary>Copies simple standard metadata fields into a document.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="metadata">The replacement simple metadata values.</param>
    /// <returns>The modified document metadata model.</returns>
    public OdfDocumentMetadata SetSimpleMetadata(
        OdfDocument document,
        OdfDocumentMetadata metadata);
    /// <summary>Finds a user-defined metadata value by exact name.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The exact user-defined field name.</param>
    /// <returns>The matching value, or <c>null</c>.</returns>
    public OdfUserDefinedMetadata? FindUserMetadata(
        OdfDocument document,
        string name);
    /// <summary>Adds or replaces a named user-defined metadata value.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="value">The typed value definition.</param>
    /// <returns>The added or modified document value.</returns>
    public OdfUserDefinedMetadata SetUserMetadata(
        OdfDocument document,
        OdfUserDefinedMetadata value);
    /// <summary>Removes a named user-defined metadata value.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="name">The exact field name.</param>
    /// <returns>Whether a value was removed.</returns>
    public bool RemoveUserMetadata(
        OdfDocument document,
        string name);
    /// <summary>Copies document statistics into the metadata model.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="statistics">The statistics to apply.</param>
    /// <returns>The modified statistics model.</returns>
    public OdfDocumentStatistics SetDocumentStatistics(
        OdfDocument document,
        OdfDocumentStatistics statistics);
    /// <summary>Removes standard document statistics.</summary>
    /// <param name="document">The document to modify.</param>
    public void ClearDocumentStatistics(OdfDocument document);
    /// <summary>Adds an RDFa or RDF/XML triple.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="triple">The triple definition.</param>
    /// <returns>The independent triple added to the document.</returns>
    public OdfRdfTriple AddRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple);
    /// <summary>Modifies an existing RDF triple while retaining its source identity.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="triple">The existing triple.</param>
    /// <param name="replacement">The replacement RDF terms.</param>
    /// <returns>The modified triple.</returns>
    public OdfRdfTriple ModifyRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple,
        OdfRdfTriple replacement);
    /// <summary>Removes an RDF triple by identity.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="triple">The triple to remove.</param>
    /// <returns>Whether the triple was present.</returns>
    public bool RemoveRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple);
    /// <summary>Finds triples using nullable wildcard terms.</summary>
    /// <param name="document">The document graph to query.</param>
    /// <param name="subject">The exact subject, or <c>null</c>.</param>
    /// <param name="predicate">The exact predicate, or <c>null</c>.</param>
    /// <param name="objectValue">The exact object, or <c>null</c>.</param>
    /// <returns>The matching triples in document order.</returns>
    public OdfRdfTriple[] FindRdfTriples(
        OdfDocument document,
        string? subject,
        string? predicate,
        string? objectValue);
    /// <summary>Adds a triple to the package metadata manifest graph.</summary>
    /// <param name="document">The package document to modify.</param>
    /// <param name="triple">The package-metadata triple definition.</param>
    /// <returns>The independent package-metadata triple.</returns>
    public OdfRdfTriple AddPackageMetadataTriple(
        OdfDocument document,
        OdfRdfTriple triple);
    /// <summary>Gets all triples from the package metadata manifest.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The package-metadata triples in document order.</returns>
    public OdfRdfTriple[] GetPackageMetadataTriples(OdfDocument document);
}
