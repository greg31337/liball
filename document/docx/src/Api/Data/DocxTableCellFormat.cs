namespace docx;

/// <summary>
/// Represents direct table-cell formatting.
/// </summary>
public class DocxTableCellFormat
{
    /// <summary>
    /// The preferred cell width.
    /// </summary>
    public string Width;
    /// <summary>
    /// The preferred cell-width unit.
    /// </summary>
    public string WidthType;
    /// <summary>
    /// The number of grid columns spanned by the cell.
    /// </summary>
    public string GridSpan;
    /// <summary>
    /// The horizontal merge state.
    /// </summary>
    public string HorizontalMerge;
    /// <summary>
    /// The vertical merge state.
    /// </summary>
    public string VerticalMerge;
    /// <summary>
    /// The vertical content alignment.
    /// </summary>
    public string VerticalAlignment;
    /// <summary>
    /// The cell text-flow direction.
    /// </summary>
    public string TextDirection;
    /// <summary>
    /// The top cell-margin width.
    /// </summary>
    public string MarginTopWidth;
    /// <summary>
    /// The top cell-margin unit.
    /// </summary>
    public string MarginTopType;
    /// <summary>
    /// The left cell-margin width.
    /// </summary>
    public string MarginLeftWidth;
    /// <summary>
    /// The left cell-margin unit.
    /// </summary>
    public string MarginLeftType;
    /// <summary>
    /// The bottom cell-margin width.
    /// </summary>
    public string MarginBottomWidth;
    /// <summary>
    /// The bottom cell-margin unit.
    /// </summary>
    public string MarginBottomType;
    /// <summary>
    /// The right cell-margin width.
    /// </summary>
    public string MarginRightWidth;
    /// <summary>
    /// The right cell-margin unit.
    /// </summary>
    public string MarginRightType;
    /// <summary>
    /// The cell borders.
    /// </summary>
    public DocxBorders Borders;
    /// <summary>
    /// The cell shading.
    /// </summary>
    public DocxShading Shading;

    /// <summary>
    /// Creates an empty table-cell format.
    /// </summary>
    public DocxTableCellFormat()
    {
        this.Width = "";
        this.WidthType = "";
        this.GridSpan = "";
        this.HorizontalMerge = "";
        this.VerticalMerge = "";
        this.VerticalAlignment = "";
        this.TextDirection = "";
        this.MarginTopWidth = "";
        this.MarginTopType = "";
        this.MarginLeftWidth = "";
        this.MarginLeftType = "";
        this.MarginBottomWidth = "";
        this.MarginBottomType = "";
        this.MarginRightWidth = "";
        this.MarginRightType = "";
        this.Borders = new DocxBorders();
        this.Shading = new DocxShading();
    }
}
