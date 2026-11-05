namespace odf;

/// <summary>Describes a generated text index and its generated body.</summary>
public class OdfTextIndex
{
    /// <summary>The index family.</summary>
    public OdfTextIndexKind Kind;
    /// <summary>The index name.</summary>
    public string Name = "";
    /// <summary>The index section style.</summary>
    public string StyleName = "";
    /// <summary>The preserved protected value.</summary>
    public string Protected = "";
    /// <summary>The index source definition.</summary>
    public OdfTextIndexSource? Source;
    /// <summary>The paragraphs and headings in the generated body.</summary>
    public OdfTextBlock[] BodyBlocks = [];
    /// <summary>All qualified index attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The XML part containing the index.</summary>
    public string PartPath = "";
    /// <summary>The underlying index element.</summary>
    public OdfNode? Node;
    /// <summary>The underlying generated index-body element.</summary>
    public OdfNode? BodyNode;
}
