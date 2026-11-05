namespace docx;

/// <summary>
/// Separates direct, inherited, and effective table formatting.
/// </summary>
public class DocxEffectiveTableFormatting
{
    /// <summary>
    /// The formatting applied directly to the table.
    /// </summary>
    public DocxTableFormat Direct;

    /// <summary>
    /// The formatting inherited from table styles.
    /// </summary>
    public DocxTableFormat Inherited;

    /// <summary>
    /// The inherited formatting with direct formatting applied.
    /// </summary>
    public DocxTableFormat Effective;

    /// <summary>
    /// Creates an empty effective table-format result.
    /// </summary>
    public DocxEffectiveTableFormatting()
    {
        this.Direct = new DocxTableFormat();
        this.Inherited = new DocxTableFormat();
        this.Effective = new DocxTableFormat();
    }
}
