namespace odf;

/// <summary>
/// Provides typed editing and lookup for document metadata and RDF graphs.
/// </summary>
public class OdfMetadataEditor : IOdfMetadataEditor
{
    /// <summary>Initializes a metadata editor.</summary>
    public OdfMetadataEditor()
    {
    }

    /// <summary>Gets the live standard metadata model.</summary>
    public OdfDocumentMetadata GetDocumentMetadata(OdfDocument document)
    {
        return MetadataEditingModel.GetMetadata(document);
    }

    /// <summary>Copies simple standard metadata fields into a document.</summary>
    public OdfDocumentMetadata SetSimpleMetadata(
        OdfDocument document,
        OdfDocumentMetadata metadata)
    {
        return MetadataEditingModel.SetSimpleMetadata(document, metadata);
    }

    /// <summary>Finds a user-defined metadata value by exact name.</summary>
    public OdfUserDefinedMetadata? FindUserMetadata(
        OdfDocument document,
        string name)
    {
        return MetadataEditingModel.FindUserMetadata(document, name);
    }

    /// <summary>Adds or replaces a named user-defined metadata value.</summary>
    public OdfUserDefinedMetadata SetUserMetadata(
        OdfDocument document,
        OdfUserDefinedMetadata value)
    {
        return MetadataEditingModel.SetUserMetadata(document, value);
    }

    /// <summary>Removes a named user-defined metadata value.</summary>
    public bool RemoveUserMetadata(
        OdfDocument document,
        string name)
    {
        return MetadataEditingModel.RemoveUserMetadata(document, name);
    }

    /// <summary>Copies document statistics into the metadata model.</summary>
    public OdfDocumentStatistics SetDocumentStatistics(
        OdfDocument document,
        OdfDocumentStatistics statistics)
    {
        return MetadataEditingModel.SetStatistics(document, statistics);
    }

    /// <summary>Removes standard document statistics.</summary>
    public void ClearDocumentStatistics(OdfDocument document)
    {
        MetadataEditingModel.ClearStatistics(document);
    }

    /// <summary>Adds an RDFa or RDF/XML triple.</summary>
    public OdfRdfTriple AddRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple)
    {
        return MetadataEditingModel.AddRdfTriple(document, triple);
    }

    /// <summary>Modifies an existing RDF triple while retaining its source identity.</summary>
    public OdfRdfTriple ModifyRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple,
        OdfRdfTriple replacement)
    {
        return MetadataEditingModel.ModifyRdfTriple(
            document, triple, replacement);
    }

    /// <summary>Removes an RDF triple by identity.</summary>
    public bool RemoveRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple)
    {
        return MetadataEditingModel.RemoveRdfTriple(document, triple);
    }

    /// <summary>Finds triples using nullable wildcard terms.</summary>
    public OdfRdfTriple[] FindRdfTriples(
        OdfDocument document,
        string? subject,
        string? predicate,
        string? objectValue)
    {
        return RdfXmlDocumentModel.QueryTriples(
            document, subject, predicate, objectValue);
    }

    /// <summary>Adds a triple to the package metadata manifest graph.</summary>
    public OdfRdfTriple AddPackageMetadataTriple(
        OdfDocument document,
        OdfRdfTriple triple)
    {
        return MetadataEditingModel.AddPackageMetadata(document, triple);
    }

    /// <summary>Gets all triples from the package metadata manifest.</summary>
    public OdfRdfTriple[] GetPackageMetadataTriples(OdfDocument document)
    {
        return MetadataEditingModel.GetPackageMetadata(document);
    }
}
