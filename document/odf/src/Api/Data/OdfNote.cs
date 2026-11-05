namespace odf;

/// <summary>
/// Describes a footnote or endnote, its citation, and its body content.
/// </summary>
public class OdfNote
{
    /// <summary>
    /// The note class.
    /// </summary>
    public OdfNoteKind Kind;
    /// <summary>
    /// The note identifier used by note references.
    /// </summary>
    public string Id = "";
    /// <summary>
    /// The explicit citation label attribute.
    /// </summary>
    public string CitationLabel = "";
    /// <summary>
    /// The preserved citation element text.
    /// </summary>
    public string CitationText = "";
    /// <summary>
    /// The resolved one-based ordinal within the configured numbering scope.
    /// </summary>
    public int Ordinal;
    /// <summary>
    /// The citation calculated from the ordinal and note configuration.
    /// </summary>
    public string ComputedCitation = "";
    /// <summary>
    /// Paragraphs and headings contained by the note body.
    /// </summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>
    /// The resolved note configuration.
    /// </summary>
    public OdfNoteConfiguration? Configuration;
    /// <summary>
    /// Whether multiple configurations make the note configuration ambiguous.
    /// </summary>
    public bool HasAmbiguousConfiguration;
    /// <summary>
    /// The paragraph or heading containing the note citation.
    /// </summary>
    public OdfTextBlock? CitationBlock;
    /// <summary>
    /// The XML part containing the note.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying note element.
    /// </summary>
    public OdfNode? Node;
    /// <summary>
    /// The underlying note-citation element.
    /// </summary>
    public OdfNode? CitationNode;
    /// <summary>
    /// The underlying note-body element.
    /// </summary>
    public OdfNode? BodyNode;
    internal OdfTextRun? Run;
    internal string OriginalCitationText = "";
}
