namespace odf;

/// <summary>Describes a cell, row, column, or rectangular table range.</summary>
public class OdfCellRange
{
    /// <summary>The preserved ODF range-address lexical value.</summary>
    public string LexicalValue = "";
    /// <summary>The table name on the first endpoint, when declared.</summary>
    public string StartTableName = "";
    /// <summary>The table name on the second endpoint, when declared.</summary>
    public string EndTableName = "";
    /// <summary>The zero-based first column, when columns are present.</summary>
    public long StartColumnIndex;
    /// <summary>The zero-based last column, when columns are present.</summary>
    public long EndColumnIndex;
    /// <summary>The zero-based first row, when rows are present.</summary>
    public long StartRowIndex;
    /// <summary>The zero-based last row, when rows are present.</summary>
    public long EndRowIndex;
    /// <summary>Whether the range carries column coordinates.</summary>
    public bool HasColumns;
    /// <summary>Whether the range carries row coordinates.</summary>
    public bool HasRows;
    /// <summary>Whether a distinct second endpoint is present.</summary>
    public bool HasEnd;
    /// <summary>Whether the lexical range is valid ODF address syntax.</summary>
    public bool IsValid;
    /// <summary>The resolved table for the range, when unambiguous.</summary>
    public OdfTable? Table;
    internal string OriginalLexicalValue = "";
    internal string OriginalStartTableName = "";
    internal string OriginalEndTableName = "";
    internal long OriginalStartColumnIndex;
    internal long OriginalEndColumnIndex;
    internal long OriginalStartRowIndex;
    internal long OriginalEndRowIndex;
    internal bool OriginalHasColumns;
    internal bool OriginalHasRows;
    internal bool OriginalHasEnd;
}
