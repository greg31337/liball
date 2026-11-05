namespace docx;

/// <summary>
/// Controls separators and revision handling during plain-text extraction.
/// </summary>
public class DocxTextOptions
{
    internal DocxTextMode Mode;
    /// <summary>
    /// The paragraph separator value stored in the document model.
    /// </summary>
    public string ParagraphSeparator;
    /// <summary>
    /// The table cell separator value stored in the document model.
    /// </summary>
    public string TableCellSeparator;
    /// <summary>
    /// The table row separator value stored in the document model.
    /// </summary>
    public string TableRowSeparator;
    /// <summary>
    /// The tab separator value stored in the document model.
    /// </summary>
    public string TabSeparator;
    /// <summary>
    /// The line break separator value stored in the document model.
    /// </summary>
    public string LineBreakSeparator;
    /// <summary>
    /// The page break separator value stored in the document model.
    /// </summary>
    public string PageBreakSeparator;
    /// <summary>
    /// The column break separator value stored in the document model.
    /// </summary>
    public string ColumnBreakSeparator;
    /// <summary>
    /// The soft hyphen text value stored in the document model.
    /// </summary>
    public string SoftHyphenText;
    /// <summary>
    /// The no break hyphen text value stored in the document model.
    /// </summary>
    public string NoBreakHyphenText;
    /// <summary>
    /// The symbol text value stored in the document model.
    /// </summary>
    public string SymbolText;
    /// <summary>
    /// Whether the include headers and footers behavior is enabled.
    /// </summary>
    public bool IncludeHeadersAndFooters;
    /// <summary>
    /// Whether the include footnotes and endnotes behavior is enabled.
    /// </summary>
    public bool IncludeFootnotesAndEndnotes;
    /// <summary>
    /// Whether the include comments behavior is enabled.
    /// </summary>
    public bool IncludeComments;
    /// <summary>
    /// Whether the include field results behavior is enabled.
    /// </summary>
    public bool IncludeFieldResults;
    /// <summary>
    /// Whether the include inserted text behavior is enabled.
    /// </summary>
    public bool IncludeInsertedText;
    /// <summary>
    /// Whether the include deleted text behavior is enabled.
    /// </summary>
    public bool IncludeDeletedText;

    /// <summary>
    /// Creates a text options instance.
    /// </summary>
    public DocxTextOptions()
    {
        this.Mode = DocxTextMode.PlainText;
        this.ParagraphSeparator = "\n";
        this.TableCellSeparator = "\t";
        this.TableRowSeparator = "\n";
        this.TabSeparator = "\t";
        this.LineBreakSeparator = "\n";
        this.PageBreakSeparator = "\n";
        this.ColumnBreakSeparator = "\n";
        this.SoftHyphenText = "";
        this.NoBreakHyphenText = "-";
        this.SymbolText = "";
        this.IncludeHeadersAndFooters = true;
        this.IncludeFootnotesAndEndnotes = true;
        this.IncludeComments = false;
        this.IncludeFieldResults = true;
        this.IncludeInsertedText = true;
        this.IncludeDeletedText = false;
    }
}
