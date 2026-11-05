namespace odf;

/// <summary>
/// Describes one column in a text-flow column set.
/// </summary>
public class OdfTextColumn
{
    /// <summary>
    /// The relative column width.
    /// </summary>
    public string RelativeWidth = "";
    /// <summary>
    /// The column start indent.
    /// </summary>
    public string StartIndent = "";
    /// <summary>
    /// The column end indent.
    /// </summary>
    public string EndIndent = "";
    /// <summary>
    /// The underlying <c>style:column</c> element, when present.
    /// </summary>
    public OdfNode? Node;
}
