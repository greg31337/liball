namespace odf;

/// <summary>
/// Describes the document and explicit stacking position of a drawing object.
/// </summary>
public class OdfDrawingZOrder
{
    /// <summary>The preserved <c>draw:z-index</c> value.</summary>
    public string LexicalValue = "";
    /// <summary>The parsed non-negative stacking index.</summary>
    public long Index;
    /// <summary>Whether an explicit stacking index is present.</summary>
    public bool IsSpecified;
    /// <summary>Whether the explicit stacking index is valid.</summary>
    public bool IsValid = true;
    /// <summary>The zero-based drawing-object order in the XML part.</summary>
    public long DocumentOrder;
    internal string OriginalLexicalValue = "";
    internal long OriginalIndex;
}
