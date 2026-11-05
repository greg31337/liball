namespace odf;

/// <summary>
/// Describes the columns and optional separator applied to a text flow.
/// </summary>
public class OdfTextColumnSet
{
    /// <summary>
    /// The declared number of columns, or zero when it is absent or invalid.
    /// </summary>
    public int Count;
    /// <summary>
    /// The uniform gap between columns.
    /// </summary>
    public string Gap = "";
    /// <summary>
    /// The individually configured columns.
    /// </summary>
    public OdfTextColumn[] Columns = [];
    /// <summary>
    /// The separator line style.
    /// </summary>
    public string SeparatorStyle = "";
    /// <summary>
    /// The separator line width.
    /// </summary>
    public string SeparatorWidth = "";
    /// <summary>
    /// The separator line color.
    /// </summary>
    public string SeparatorColor = "";
    /// <summary>
    /// The separator height percentage.
    /// </summary>
    public string SeparatorHeight = "";
    /// <summary>
    /// The separator vertical alignment.
    /// </summary>
    public string SeparatorVerticalAlignment = "";
    /// <summary>
    /// The underlying <c>style:columns</c> element.
    /// </summary>
    public OdfNode? Node;
    /// <summary>
    /// The underlying <c>style:column-sep</c> element, when present.
    /// </summary>
    public OdfNode? SeparatorNode;
}
