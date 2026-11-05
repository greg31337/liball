namespace odf;

/// <summary>
/// Describes an inline or style-driven text-flow break.
/// </summary>
public class OdfTextBreak
{
    /// <summary>
    /// The typed break kind.
    /// </summary>
    public OdfTextBreakKind Kind;
    /// <summary>
    /// The preserved lexical break value.
    /// </summary>
    public string Value = "";
    /// <summary>
    /// Whether a style-driven break applies before its block.
    /// </summary>
    public bool IsBefore;
    /// <summary>
    /// The associated text block.
    /// </summary>
    public OdfTextBlock? Block;
    /// <summary>
    /// The associated inline run, when this is an explicit control.
    /// </summary>
    public OdfTextRun? Run;
    /// <summary>
    /// The underlying break or block node.
    /// </summary>
    public OdfNode? Node;
}
