namespace odf;

/// <summary>
/// Queries RDFa, RDF/XML, and package-metadata triples as one graph.
/// </summary>
public class OdfRdfQuery : IOdfRdfQuery
{
    /// <summary>
    /// Initializes an RDF graph query service.
    /// </summary>
    public OdfRdfQuery()
    {
    }

    /// <summary>
    /// Finds triples matching the supplied terms, treating <c>null</c> terms as wildcards.
    /// </summary>
    /// <param name="document">The document graph to query.</param>
    /// <param name="subject">The exact subject, or <c>null</c> for any subject.</param>
    /// <param name="predicate">The exact predicate, or <c>null</c> for any predicate.</param>
    /// <param name="objectValue">The exact object, or <c>null</c> for any object.</param>
    /// <returns>The matching triples in document order.</returns>
    public OdfRdfTriple[] FindRdfTriples(
        OdfDocument document,
        string? subject,
        string? predicate,
        string? objectValue)
    {
        return RdfXmlDocumentModel.QueryTriples(
            document, subject, predicate, objectValue);
    }
}
