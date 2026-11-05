namespace docx;

/// <summary>
/// Separates direct, inherited, and effective paragraph formatting.
/// </summary>
public class DocxEffectiveParagraphFormatting
{
    /// <summary>
    /// The formatting applied directly to the paragraph.
    /// </summary>
    public DocxParagraphFormat Direct;

    /// <summary>
    /// The formatting inherited from defaults and styles.
    /// </summary>
    public DocxParagraphFormat Inherited;

    /// <summary>
    /// The inherited formatting with direct formatting applied.
    /// </summary>
    public DocxParagraphFormat Effective;

    /// <summary>
    /// Creates an empty effective paragraph-format result.
    /// </summary>
    public DocxEffectiveParagraphFormatting()
    {
        this.Direct = new DocxParagraphFormat();
        this.Inherited = new DocxParagraphFormat();
        this.Effective = new DocxParagraphFormat();
    }
}
