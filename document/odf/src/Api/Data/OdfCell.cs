namespace odf;

/// <summary>Describes one physical table-cell or covered-cell run.</summary>
public class OdfCell
{
    /// <summary>The zero-based first logical column represented by this run.</summary>
    public long StartColumnIndex;
    /// <summary>The number of logical cells represented by this run.</summary>
    public long RepeatCount = 1;
    /// <summary>Whether this is a covered-table-cell run.</summary>
    public bool IsCovered;
    /// <summary>The number of columns occupied by an ordinary merged-cell origin.</summary>
    public long ColumnSpan = 1;
    /// <summary>The number of rows occupied by an ordinary merged-cell origin.</summary>
    public long RowSpan = 1;
    /// <summary>The number of columns in a matrix or array formula range.</summary>
    public long MatrixColumnSpan = 1;
    /// <summary>The number of rows in a matrix or array formula range.</summary>
    public long MatrixRowSpan = 1;
    /// <summary>Whether this cell is the origin of an ordinary merged range.</summary>
    public bool IsMergeOrigin;
    /// <summary>Whether this cell is the origin of a matrix or array formula range.</summary>
    public bool IsMatrixFormula;
    /// <summary>Whether the preserved span and formula attributes form a valid local structure.</summary>
    public bool HasValidStructure = true;
    /// <summary>The referenced table-cell style name.</summary>
    public string StyleName = "";
    /// <summary>The resolved referenced table-cell style.</summary>
    public OdfStyle? Style;
    /// <summary>The default table-cell style.</summary>
    public OdfStyle? DefaultStyle;
    /// <summary>The effective explicit or row/column default cell style.</summary>
    public OdfStyle? EffectiveStyle;
    /// <summary>Whether the cell is protected.</summary>
    public bool IsProtected;
    /// <summary>The content-validation name referenced by this cell.</summary>
    public string ValidationName = "";
    /// <summary>The resolved content-validation rule, when found.</summary>
    public OdfContentValidation? Validation;
    /// <summary>The physical row containing this cell run.</summary>
    public OdfRow? Row;
    /// <summary>Tables nested within this cell.</summary>
    public OdfTable[] NestedTables = [];
    /// <summary>The typed value stored by this cell run.</summary>
    public OdfCellValue Value = new OdfCellValue();
    /// <summary>The preserved formula expression, when the cell has one.</summary>
    public string Formula = "";
    /// <summary>The underlying cell element.</summary>
    public OdfNode? Node;
    internal string OriginalFormula = "";
    internal OdfCellValue? OriginalValue;
    internal string OriginalStyleName = "";
    internal bool OriginalIsProtected;
    internal string OriginalValidationName = "";
    internal bool OriginalIsCovered;
    internal long OriginalColumnSpan = 1;
    internal long OriginalRowSpan = 1;
    internal long OriginalMatrixColumnSpan = 1;
    internal long OriginalMatrixRowSpan = 1;
}
