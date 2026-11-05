namespace docx;

/// <summary>
/// Represents one explicitly sized section column.
/// </summary>
public class DocxSectionColumn
{
    /// <summary>
    /// The column width in twentieths of a point.
    /// </summary>
    public string Width;

    /// <summary>
    /// The spacing after the column in twentieths of a point.
    /// </summary>
    public string Space;

    /// <summary>
    /// Creates an empty section column.
    /// </summary>
    public DocxSectionColumn()
    {
        this.Width = "";
        this.Space = "";
    }
}
