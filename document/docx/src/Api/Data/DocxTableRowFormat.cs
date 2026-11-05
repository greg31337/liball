namespace docx;

/// <summary>
/// Represents direct table-row formatting.
/// </summary>
public class DocxTableRowFormat
{
    /// <summary>
    /// The row height.
    /// </summary>
    public string Height;
    /// <summary>
    /// The rule used to interpret the row height.
    /// </summary>
    public string HeightRule;
    /// <summary>
    /// Whether row splitting was explicitly controlled.
    /// </summary>
    public bool HasCantSplit;
    /// <summary>
    /// Whether the row must remain on one page.
    /// </summary>
    public bool CantSplit;
    /// <summary>
    /// Whether repeating as a header was explicitly controlled.
    /// </summary>
    public bool HasRepeatHeader;
    /// <summary>
    /// Whether the row repeats as a header on each page.
    /// </summary>
    public bool RepeatHeader;

    /// <summary>
    /// Creates an empty table-row format.
    /// </summary>
    public DocxTableRowFormat()
    {
        this.Height = "";
        this.HeightRule = "";
        this.HasCantSplit = false;
        this.CantSplit = false;
        this.HasRepeatHeader = false;
        this.RepeatHeader = false;
    }
}
