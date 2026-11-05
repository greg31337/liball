namespace docx;

/// <summary>
/// Describes one ordered unit produced during text traversal.
/// </summary>
public class DocxTextToken
{
    /// <summary>
    /// The token kind.
    /// </summary>
    public DocxTextTokenKind Kind;
    /// <summary>
    /// The extracted token text after applying text options.
    /// </summary>
    public string Text;
    /// <summary>
    /// Whether the source text node requests XML space preservation.
    /// </summary>
    public bool PreserveSpace;
    /// <summary>
    /// The story containing the token.
    /// </summary>
    public DocxStoryType StoryType;
    /// <summary>
    /// The source paragraph when available, or null.
    /// </summary>
    public DocxParagraph? Paragraph;
    /// <summary>
    /// The source run when available, or null.
    /// </summary>
    public DocxRun? Run;
    /// <summary>
    /// The source text node when available, or null.
    /// </summary>
    public DocxText? TextNode;

    /// <summary>
    /// Creates a text token.
    /// </summary>
    /// <param name="kind">The token kind.</param>
    /// <param name="text">The extracted token text.</param>
    /// <param name="preserveSpace">Whether XML space preservation is requested.</param>
    /// <param name="storyType">The containing story type.</param>
    /// <param name="paragraph">The source paragraph, or null.</param>
    /// <param name="run">The source run, or null.</param>
    /// <param name="textNode">The source text node, or null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    public DocxTextToken(DocxTextTokenKind kind, string text, bool preserveSpace, DocxStoryType storyType, DocxParagraph? paragraph, DocxRun? run, DocxText? textNode)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        this.Kind = kind;
        this.Text = text;
        this.PreserveSpace = preserveSpace;
        this.StoryType = storyType;
        this.Paragraph = paragraph;
        this.Run = run;
        this.TextNode = textNode;
    }
}
