namespace odf;

/// <summary>Describes a deleted change reference or deleted cell snapshot.</summary>
public class OdfSpreadsheetChangeDeletion
{
    /// <summary>The deletion kind: <c>cell-content</c> or <c>change</c>.</summary>
    public string Kind = "";
    /// <summary>The associated change identifier.</summary>
    public string Id = "";
    /// <summary>Whether a numeric cell address was stored.</summary>
    public bool HasCellAddress;
    /// <summary>The deleted cell column index.</summary>
    public long CellColumn;
    /// <summary>The deleted cell row index.</summary>
    public long CellRow;
    /// <summary>The deleted cell table index.</summary>
    public long CellTable;
    /// <summary>The optional deleted cell snapshot.</summary>
    public OdfSpreadsheetChangeCell? Cell;
    /// <summary>The underlying deletion element.</summary>
    public OdfNode? Node;
}
