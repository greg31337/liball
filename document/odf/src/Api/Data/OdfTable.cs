namespace odf;

/// <summary>Describes a table using compact repeated row and column runs.</summary>
public class OdfTable
{
    /// <summary>The table name.</summary>
    public string Name = "";
    /// <summary>The referenced table style name.</summary>
    public string StyleName = "";
    /// <summary>The resolved referenced table style.</summary>
    public OdfStyle? Style;
    /// <summary>The default table style, when declared.</summary>
    public OdfStyle? DefaultStyle;
    /// <summary>The referenced table-template name.</summary>
    public string TemplateName = "";
    /// <summary>The resolved table template.</summary>
    public OdfTableTemplate? Template;
    /// <summary>Whether first-row template styles are enabled.</summary>
    public bool UseFirstRowStyles;
    /// <summary>Whether last-row template styles are enabled.</summary>
    public bool UseLastRowStyles;
    /// <summary>Whether first-column template styles are enabled.</summary>
    public bool UseFirstColumnStyles;
    /// <summary>Whether last-column template styles are enabled.</summary>
    public bool UseLastColumnStyles;
    /// <summary>Whether alternating row template styles are enabled.</summary>
    public bool UseBandingRowStyles;
    /// <summary>Whether alternating column template styles are enabled.</summary>
    public bool UseBandingColumnStyles;
    /// <summary>Whether the table is protected.</summary>
    public bool IsProtected;
    /// <summary>The preserved table protection key.</summary>
    public string ProtectionKey = "";
    /// <summary>The protection-key digest algorithm IRI.</summary>
    public string ProtectionKeyDigestAlgorithm = "";
    /// <summary>Whether the table is included when printing.</summary>
    public bool IsPrinted = true;
    /// <summary>The preserved print-range list.</summary>
    public string PrintRangeText = "";
    /// <summary>The parsed print ranges.</summary>
    public OdfCellRange[] PrintRanges = [];
    /// <summary>The zero-based first print-title row.</summary>
    public long PrintTitleRowStart;
    /// <summary>The logical number of print-title rows.</summary>
    public long PrintTitleRowCount;
    /// <summary>The zero-based first print-title column.</summary>
    public long PrintTitleColumnStart;
    /// <summary>The logical number of print-title columns.</summary>
    public long PrintTitleColumnCount;
    /// <summary>The table title text.</summary>
    public string Title = "";
    /// <summary>The table description text.</summary>
    public string Description = "";
    /// <summary>The linked source used to populate this table.</summary>
    public OdfTableSource Source = new OdfTableSource();
    /// <summary>The logical row count, saturated at the largest signed 64-bit value.</summary>
    public long RowCount;
    /// <summary>The logical column count, saturated at the largest signed 64-bit value.</summary>
    public long ColumnCount;
    /// <summary>The compact physical row runs in logical order.</summary>
    public OdfRow[] Rows = [];
    /// <summary>The compact physical column runs in logical order.</summary>
    public OdfColumn[] Columns = [];
    /// <summary>The row-group logical ranges in source order.</summary>
    public OdfTableGroup[] RowGroups = [];
    /// <summary>The column-group logical ranges in source order.</summary>
    public OdfTableGroup[] ColumnGroups = [];
    /// <summary>The containing table for a nested table, when present.</summary>
    public OdfTable? ParentTable;
    /// <summary>The containing cell for a nested table, when present.</summary>
    public OdfCell? ParentCell;
    /// <summary>The XML part containing the table.</summary>
    public string PartPath = "";
    /// <summary>The underlying table element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalStyleName = "";
    internal string OriginalTemplateName = "";
    internal bool OriginalUseFirstRowStyles;
    internal bool OriginalUseLastRowStyles;
    internal bool OriginalUseFirstColumnStyles;
    internal bool OriginalUseLastColumnStyles;
    internal bool OriginalUseBandingRowStyles;
    internal bool OriginalUseBandingColumnStyles;
    internal bool OriginalIsProtected;
    internal string OriginalProtectionKey = "";
    internal string OriginalProtectionKeyDigestAlgorithm = "";
    internal bool OriginalIsPrinted = true;
    internal string OriginalPrintRangeText = "";
    internal int OriginalPrintRangeCount;
    internal string OriginalTitle = "";
    internal string OriginalDescription = "";
}
