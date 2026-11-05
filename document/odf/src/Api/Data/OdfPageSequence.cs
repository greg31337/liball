namespace odf;

/// <summary>Describes an explicit sequence of master pages.</summary>
public class OdfPageSequence
{
    /// <summary>The ordered pages.</summary>
    public OdfPageSequenceEntry[] Pages = [];
    /// <summary>The XML part containing the sequence.</summary>
    public string PartPath = "";
    /// <summary>The underlying page-sequence element.</summary>
    public OdfNode? Node;
}
