namespace docx;

/// <summary>
/// Separates direct, inherited, and effective run formatting.
/// </summary>
public class DocxEffectiveRunFormatting
{
    /// <summary>
    /// The formatting applied directly to the run.
    /// </summary>
    public DocxRunFormat Direct;

    /// <summary>
    /// The formatting inherited from defaults and styles.
    /// </summary>
    public DocxRunFormat Inherited;

    /// <summary>
    /// The inherited formatting with direct formatting applied.
    /// </summary>
    public DocxRunFormat Effective;

    /// <summary>
    /// Creates an empty effective run-format result.
    /// </summary>
    public DocxEffectiveRunFormatting()
    {
        this.Direct = new DocxRunFormat();
        this.Inherited = new DocxRunFormat();
        this.Effective = new DocxRunFormat();
    }
}
