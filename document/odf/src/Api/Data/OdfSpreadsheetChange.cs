namespace odf;

/// <summary>Describes a tracked spreadsheet cell-content, insertion, deletion, or movement change.</summary>
public class OdfSpreadsheetChange
{
    /// <summary>The change identifier.</summary>
    public string Id = "";
    /// <summary>The change kind: cell-content, insertion, deletion, or movement.</summary>
    public string Kind = "";
    /// <summary>The acceptance state.</summary>
    public string AcceptanceState = "pending";
    /// <summary>The identifier of the change that rejected this change.</summary>
    public string RejectingChangeId = "";
    /// <summary>The change author.</summary>
    public string Creator = "";
    /// <summary>The change date or date-time lexical value.</summary>
    public string Date = "";
    /// <summary>The plain-text change comment.</summary>
    public string Comment = "";
    /// <summary>The insertion or deletion type: row, column, or table.</summary>
    public string Axis = "";
    /// <summary>The zero-based insertion or deletion position.</summary>
    public long Position;
    /// <summary>The insertion count.</summary>
    public long Count = 1;
    /// <summary>The zero-based table index associated with the change.</summary>
    public long TableIndex;
    /// <summary>The number of deletions spanned by a multi-deletion entry.</summary>
    public long MultiDeletionSpanned;
    /// <summary>The changed cell column index.</summary>
    public long CellColumn;
    /// <summary>The changed cell row index.</summary>
    public long CellRow;
    /// <summary>The changed cell table index.</summary>
    public long CellTable;
    /// <summary>The movement source start column.</summary>
    public long SourceStartColumn;
    /// <summary>The movement source start row.</summary>
    public long SourceStartRow;
    /// <summary>The movement source start table index.</summary>
    public long SourceStartTable;
    /// <summary>The movement source end column.</summary>
    public long SourceEndColumn;
    /// <summary>The movement source end row.</summary>
    public long SourceEndRow;
    /// <summary>The movement source end table index.</summary>
    public long SourceEndTable;
    /// <summary>The movement target start column.</summary>
    public long TargetStartColumn;
    /// <summary>The movement target start row.</summary>
    public long TargetStartRow;
    /// <summary>The movement target start table index.</summary>
    public long TargetStartTable;
    /// <summary>The movement target end column.</summary>
    public long TargetEndColumn;
    /// <summary>The movement target end row.</summary>
    public long TargetEndRow;
    /// <summary>The movement target end table index.</summary>
    public long TargetEndTable;
    /// <summary>The changes on which this change depends.</summary>
    public OdfSpreadsheetChangeDependency[] Dependencies = [];
    /// <summary>The deleted change references and deleted cell snapshots.</summary>
    public OdfSpreadsheetChangeDeletion[] Deletions = [];
    /// <summary>The cut-offs associated with a structural deletion.</summary>
    public OdfSpreadsheetChangeCutOff[] CutOffs = [];
    /// <summary>The identifier associated with the previous cell snapshot.</summary>
    public string PreviousId = "";
    /// <summary>The previous value of a changed cell.</summary>
    public OdfSpreadsheetChangeCell? PreviousCell;
    /// <summary>The underlying tracked-change element.</summary>
    public OdfNode? Node;
}
