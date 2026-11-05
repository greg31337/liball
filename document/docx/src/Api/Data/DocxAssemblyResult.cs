namespace docx;

/// <summary>
/// Reports imported content, remapped identifiers, and assembly conflicts.
/// </summary>
public class DocxAssemblyResult
{
    /// <summary>The number of body blocks inserted.</summary>
    public int AffectedBlockCount;
    /// <summary>The number of sections imported.</summary>
    public int ImportedSectionCount;
    /// <summary>The number of styles imported.</summary>
    public int ImportedStyleCount;
    /// <summary>The number of abstract numbering definitions imported.</summary>
    public int ImportedAbstractNumberingCount;
    /// <summary>The number of numbering instances imported.</summary>
    public int ImportedNumberingCount;
    /// <summary>The number of media relationships imported.</summary>
    public int ImportedMediaCount;
    /// <summary>The number of relationships remapped.</summary>
    public int RemappedRelationshipCount;
    /// <summary>The number of bookmark identifiers remapped.</summary>
    public int RemappedBookmarkCount;
    /// <summary>The number of comment identifiers remapped.</summary>
    public int RemappedCommentCount;
    /// <summary>The number of footnote identifiers remapped.</summary>
    public int RemappedFootnoteCount;
    /// <summary>The number of endnote identifiers remapped.</summary>
    public int RemappedEndnoteCount;
    /// <summary>The number of drawing identifiers remapped.</summary>
    public int RemappedDrawingCount;
    /// <summary>Non-fatal conflicts discovered before assembly.</summary>
    public List<DocxAssemblyConflict> Conflicts;

    /// <summary>Creates an empty assembly result.</summary>
    public DocxAssemblyResult()
    {
        this.AffectedBlockCount = 0;
        this.ImportedSectionCount = 0;
        this.ImportedStyleCount = 0;
        this.ImportedAbstractNumberingCount = 0;
        this.ImportedNumberingCount = 0;
        this.ImportedMediaCount = 0;
        this.RemappedRelationshipCount = 0;
        this.RemappedBookmarkCount = 0;
        this.RemappedCommentCount = 0;
        this.RemappedFootnoteCount = 0;
        this.RemappedEndnoteCount = 0;
        this.RemappedDrawingCount = 0;
        this.Conflicts = new List<DocxAssemblyConflict>();
    }
}
