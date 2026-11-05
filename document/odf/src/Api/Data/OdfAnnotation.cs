namespace odf;

/// <summary>
/// Describes an annotation, its content, anchor range, and reply relationships.
/// </summary>
public class OdfAnnotation
{
    /// <summary>The name that pairs a range start with its end marker.</summary>
    public string Name = "";
    /// <summary>The XML identifier on the annotation element.</summary>
    public string XmlId = "";
    /// <summary>The annotation author.</summary>
    public string Creator = "";
    /// <summary>The annotation creation date or date-time.</summary>
    public string Date = "";
    /// <summary>Whether <see cref="Date"/> has a valid XML Schema lexical form.</summary>
    public bool IsDateValid;
    /// <summary>The deprecated display-oriented date string.</summary>
    public string DateString = "";
    /// <summary>The annotation author's initials.</summary>
    public string Initials = "";
    /// <summary>The plain-text projection of the annotation body.</summary>
    public string Text = "";
    /// <summary>The paragraphs and headings contained by the annotation.</summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>The plain text covered by a resolved range annotation.</summary>
    public string AnnotatedText = "";
    /// <summary>Whether a corresponding range-end marker exists.</summary>
    public bool IsRange;
    /// <summary>Whether one unique, ordered range-end marker was resolved.</summary>
    public bool IsRangeResolved;
    /// <summary>Whether a range-end marker precedes this annotation.</summary>
    public bool IsReversed;
    /// <summary>Whether duplicate starts or ends make the range ambiguous.</summary>
    public bool HasDuplicateEndpoint;
    /// <summary>Whether a resolution extension was present in the source.</summary>
    public bool HasExplicitState;
    /// <summary>
    /// The annotation state.
    /// </summary>
    public OdfAnnotationState State;
    /// <summary>The extension name of the parent annotation in a reply thread.</summary>
    public string ParentName = "";
    /// <summary>The resolved parent annotation, or <c>null</c> for a thread root.</summary>
    public OdfAnnotation? Parent;
    /// <summary>The directly linked replies in document order.</summary>
    public OdfAnnotation[] Replies = [];
    /// <summary>The preserved <c>office:display</c> value.</summary>
    public string Display = "";
    /// <summary>The horizontal annotation position.</summary>
    public string X = "";
    /// <summary>The vertical annotation position.</summary>
    public string Y = "";
    /// <summary>The annotation width.</summary>
    public string Width = "";
    /// <summary>The annotation height.</summary>
    public string Height = "";
    /// <summary>The drawing style name.</summary>
    public string StyleName = "";
    /// <summary>The text style name.</summary>
    public string TextStyleName = "";
    /// <summary>The text anchor type.</summary>
    public string AnchorType = "";
    /// <summary>The lexical anchor page number.</summary>
    public string AnchorPageNumber = "";
    /// <summary>The spreadsheet range end-cell address.</summary>
    public string EndCellAddress = "";
    /// <summary>The horizontal range end position in a spreadsheet cell.</summary>
    public string EndX = "";
    /// <summary>The vertical range end position in a spreadsheet cell.</summary>
    public string EndY = "";
    /// <summary>The XML part containing the annotation.</summary>
    public string PartPath = "";
    /// <summary>The paragraph or heading containing the range start.</summary>
    public OdfTextBlock? StartBlock;
    /// <summary>The paragraph or heading containing the range end.</summary>
    public OdfTextBlock? EndBlock;
    /// <summary>The underlying annotation element.</summary>
    public OdfNode? Node;
    /// <summary>The underlying range-end element.</summary>
    public OdfNode? EndNode;
    internal OdfTextRun? StartRun;
    internal OdfTextRun? EndRun;
    internal string OriginalName = "";
    internal string OriginalText = "";
}
