namespace docx;

/// <summary>
/// Separates direct, inherited, and effective table-cell formatting.
/// </summary>
public class DocxEffectiveTableCellFormatting
{
    /// <summary>
    /// The formatting applied directly to the cell.
    /// </summary>
    public DocxTableCellFormat Direct;

    /// <summary>
    /// The formatting inherited from its table and table styles.
    /// </summary>
    public DocxTableCellFormat Inherited;

    /// <summary>
    /// The inherited formatting with direct formatting applied.
    /// </summary>
    public DocxTableCellFormat Effective;

    /// <summary>
    /// Creates an empty effective table-cell-format result.
    /// </summary>
    public DocxEffectiveTableCellFormatting()
    {
        this.Direct = new DocxTableCellFormat();
        this.Inherited = new DocxTableCellFormat();
        this.Effective = new DocxTableCellFormat();
    }
}
