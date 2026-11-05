namespace odf;

/// <summary>
/// Describes one query or search result.
/// </summary>
public class OdfSearchMatch
{
    /// <summary>
    /// The location of the match.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
    /// <summary>
    /// The matched lexical or text value.
    /// </summary>
    public string MatchedValue = "";
    /// <summary>
    /// Context surrounding the match, or an empty string.
    /// </summary>
    public string Context = "";
    /// <summary>
    /// The matched node, when the result is associated with a materialized node.
    /// </summary>
    public OdfNode? Node;
    /// <summary>
    /// The table containing a matched cell, when this is a table-range result.
    /// </summary>
    public OdfTable? Table;
    /// <summary>
    /// The matched cell, when this is a table-range result.
    /// </summary>
    public OdfCell? Cell;
    /// <summary>
    /// The zero-based logical row of a matched cell.
    /// </summary>
    public long RowIndex;
    /// <summary>
    /// The zero-based logical column of a matched cell.
    /// </summary>
    public long ColumnIndex;
    /// <summary>
    /// Whether the match occurred in a cell formula rather than its value.
    /// </summary>
    public bool IsFormula;
    /// <summary>
    /// Whether the result exactly matches the requested value rather than a derived criterion.
    /// </summary>
    public bool IsExact;
}
