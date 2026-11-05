namespace odf;

/// <summary>
/// Represents one RDF subject, predicate, and literal or resource object.
/// </summary>
public class OdfRdfTriple
{
    /// <summary>The RDF representation that owns this triple.</summary>
    public OdfRdfSourceKind SourceKind;
    /// <summary>The resolved subject IRI or blank-node identifier.</summary>
    public string Subject = "";
    /// <summary>The preserved RDFa subject lexical form.</summary>
    public string SubjectLexical = "";
    /// <summary>The resolved predicate IRI.</summary>
    public string Predicate = "";
    /// <summary>The preserved predicate CURIE.</summary>
    public string PredicateLexical = "";
    /// <summary>The literal value or resolved resource IRI.</summary>
    public string Object = "";
    /// <summary>The preserved content or resource lexical form.</summary>
    public string ObjectLexical = "";
    /// <summary>The resolved datatype IRI for a typed literal.</summary>
    public string Datatype = "";
    /// <summary>The preserved datatype CURIE.</summary>
    public string DatatypeLexical = "";
    /// <summary>The inherited or directly declared literal language.</summary>
    public string Language = "";
    /// <summary>Whether the object is a literal instead of an RDF resource.</summary>
    public bool IsLiteral;
    /// <summary>Whether all required RDFa terms are valid and resolvable.</summary>
    public bool IsValid;
    /// <summary>The XML part containing the RDFa statement.</summary>
    public string PartPath = "";
    /// <summary>The content node carrying the RDFa attributes.</summary>
    public OdfNode? Node;
    internal string OriginalSubject = "";
    internal string OriginalSubjectLexical = "";
    internal string OriginalPredicate = "";
    internal string OriginalPredicateLexical = "";
    internal string OriginalObject = "";
    internal string OriginalObjectLexical = "";
    internal string OriginalDatatype = "";
    internal string OriginalDatatypeLexical = "";
    internal string OriginalLanguage = "";
    internal bool OriginalIsLiteral;
    internal bool OriginalHadContent;
    internal int RdfXmlRepresentation;
    internal OdfNode? RdfXmlSubjectNode;
    internal OdfQualifiedName? RdfXmlAttributeName;
}
