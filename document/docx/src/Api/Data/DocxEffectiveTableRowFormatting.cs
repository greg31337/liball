namespace docx;

/// <summary>
/// Separates direct, inherited, and effective table-row formatting.
/// </summary>
public class DocxEffectiveTableRowFormatting
{
    /// <summary>
    /// The formatting applied directly to the row.
    /// </summary>
    public DocxTableRowFormat Direct;

    /// <summary>
    /// The formatting inherited from table styles.
    /// </summary>
    public DocxTableRowFormat Inherited;

    /// <summary>
    /// The inherited formatting with direct formatting applied.
    /// </summary>
    public DocxTableRowFormat Effective;

    /// <summary>
    /// Creates an empty effective table-row-format result.
    /// </summary>
    public DocxEffectiveTableRowFormatting()
    {
        this.Direct = new DocxTableRowFormat();
        this.Inherited = new DocxTableRowFormat();
        this.Effective = new DocxTableRowFormat();
    }
}
