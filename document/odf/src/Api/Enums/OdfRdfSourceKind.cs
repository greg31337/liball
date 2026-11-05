namespace odf;

/// <summary>
/// Identifies the document representation from which an RDF triple originated.
/// </summary>
public enum OdfRdfSourceKind
{
    /// <summary>The source representation is unknown.</summary>
    Unknown = 0,
    /// <summary>The triple is represented by RDFa attributes on a content node.</summary>
    Rdfa = 1,
    /// <summary>The triple is represented by an RDF/XML package part.</summary>
    RdfXml = 2,
    /// <summary>The triple is represented by the package metadata manifest.</summary>
    PackageMetadata = 3
}
