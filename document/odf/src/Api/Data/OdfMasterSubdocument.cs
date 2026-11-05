namespace odf;

/// <summary>Describes a linked section used as a master-document subdocument.</summary>
public class OdfMasterSubdocument
{
    /// <summary>The local linked-section name.</summary>
    public string Name = "";
    /// <summary>The linked document IRI.</summary>
    public string Href = "";
    /// <summary>The linked source section name.</summary>
    public string SectionName = "";
    /// <summary>The linked source filter name.</summary>
    public string FilterName = "";
    /// <summary>The corresponding typed text section.</summary>
    public OdfTextSection? Section;
    /// <summary>The underlying linked section element.</summary>
    public OdfNode? Node;
    /// <summary>The underlying section-source element.</summary>
    public OdfNode? SourceNode;
    /// <summary>The XML part containing the link.</summary>
    public string PartPath = "";
}
