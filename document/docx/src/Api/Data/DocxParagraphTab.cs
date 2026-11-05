namespace docx;

/// <summary>
/// Describes one custom paragraph tab stop.
/// </summary>
public class DocxParagraphTab
{
    /// <summary>
    /// The tab alignment or clear operation.
    /// </summary>
    public string Alignment;
    /// <summary>
    /// The tab position in twentieths of a point.
    /// </summary>
    public string Position;
    /// <summary>
    /// The optional leader style.
    /// </summary>
    public string Leader;

    /// <summary>
    /// Creates a paragraph tab stop.
    /// </summary>
    /// <param name="alignment">The tab alignment or clear operation.</param>
    /// <param name="position">The tab position.</param>
    /// <param name="leader">The optional leader style.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxParagraphTab(string alignment, string position, string leader)
    {
        if (alignment == null)
        {
            throw new ArgumentNullException("alignment");
        }

        if (position == null)
        {
            throw new ArgumentNullException("position");
        }

        if (leader == null)
        {
            throw new ArgumentNullException("leader");
        }

        this.Alignment = alignment;
        this.Position = position;
        this.Leader = leader;
    }
}
