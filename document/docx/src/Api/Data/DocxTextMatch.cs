namespace docx;

/// <summary>
/// Describes a text match and the model objects that contain it.
/// </summary>
public class DocxTextMatch
{
    /// <summary>
    /// The paragraph value stored in the document model.
    /// </summary>
    public DocxParagraph Paragraph;
    /// <summary>
    /// The paragraph index value stored in the document model.
    /// </summary>
    public int ParagraphIndex;
    /// <summary>
    /// The match index value stored in the document model.
    /// </summary>
    public int MatchIndex;
    /// <summary>
    /// The length value stored in the document model.
    /// </summary>
    public int Length;
    /// <summary>
    /// The start run index value stored in the document model.
    /// </summary>
    public int StartRunIndex;
    /// <summary>
    /// The start run offset value stored in the document model.
    /// </summary>
    public int StartRunOffset;
    /// <summary>
    /// The end run index value stored in the document model.
    /// </summary>
    public int EndRunIndex;
    /// <summary>
    /// The end run offset value stored in the document model.
    /// </summary>
    public int EndRunOffset;
    /// <summary>
    /// The text value stored in the document model.
    /// </summary>
    public string Text;
    /// <summary>
    /// The start run value stored in the document model.
    /// </summary>
    public DocxRun StartRun;
    /// <summary>
    /// The end run value stored in the document model.
    /// </summary>
    public DocxRun EndRun;

    /// <summary>
    /// Creates a text match instance.
    /// </summary>
    /// <param name="paragraph">The paragraph used by the operation.</param>
    /// <param name="paragraphIndex">The paragraph index used by the operation.</param>
    /// <param name="matchIndex">The match index used by the operation.</param>
    /// <param name="length">The length used by the operation.</param>
    /// <param name="startRunIndex">The start run index used by the operation.</param>
    /// <param name="startRunOffset">The start run offset used by the operation.</param>
    /// <param name="endRunIndex">The end run index used by the operation.</param>
    /// <param name="endRunOffset">The end run offset used by the operation.</param>
    /// <param name="text">The text to store or process.</param>
    /// <param name="startRun">The start run used by the operation.</param>
    /// <param name="endRun">The end run used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxTextMatch(DocxParagraph paragraph, int paragraphIndex, int matchIndex, int length, int startRunIndex, int startRunOffset, int endRunIndex, int endRunOffset, string text, DocxRun startRun, DocxRun endRun)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (startRun == null)
        {
            throw new ArgumentNullException("startRun");
        }

        if (endRun == null)
        {
            throw new ArgumentNullException("endRun");
        }

        this.Paragraph = paragraph;
        this.ParagraphIndex = paragraphIndex;
        this.MatchIndex = matchIndex;
        this.Length = length;
        this.StartRunIndex = startRunIndex;
        this.StartRunOffset = startRunOffset;
        this.EndRunIndex = endRunIndex;
        this.EndRunOffset = endRunOffset;
        this.Text = text;
        this.StartRun = startRun;
        this.EndRun = endRun;
    }
}
