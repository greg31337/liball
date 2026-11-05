namespace odf;

/// <summary>
/// Holds the standard document statistics; negative values mean absent or invalid.
/// </summary>
public class OdfDocumentStatistics
{
    /// <summary>Whether a document-statistic element is present.</summary>
    public bool IsPresent;
    /// <summary>The page count.</summary>
    public long PageCount = -1;
    /// <summary>The table count.</summary>
    public long TableCount = -1;
    /// <summary>The drawing count.</summary>
    public long DrawCount = -1;
    /// <summary>The image count.</summary>
    public long ImageCount = -1;
    /// <summary>The object count.</summary>
    public long ObjectCount = -1;
    /// <summary>The OLE object count.</summary>
    public long OleObjectCount = -1;
    /// <summary>The paragraph count.</summary>
    public long ParagraphCount = -1;
    /// <summary>The word count.</summary>
    public long WordCount = -1;
    /// <summary>The character count.</summary>
    public long CharacterCount = -1;
    /// <summary>The non-whitespace character count.</summary>
    public long NonWhitespaceCharacterCount = -1;
    /// <summary>The row count.</summary>
    public long RowCount = -1;
    /// <summary>The cell count.</summary>
    public long CellCount = -1;
    /// <summary>The frame count.</summary>
    public long FrameCount = -1;
    /// <summary>The sentence count.</summary>
    public long SentenceCount = -1;
    /// <summary>The syllable count.</summary>
    public long SyllableCount = -1;
    internal OdfNode? Node;
}
