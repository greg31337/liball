namespace odf;

/// <summary>Describes one bibliography mark and its standard citation data.</summary>
public class OdfBibliographyEntry
{
    /// <summary>The entry identifier.</summary>
    public string Identifier = "";
    /// <summary>The bibliography type.</summary>
    public string BibliographyType = "";
    /// <summary>The author value.</summary>
    public string Author = "";
    /// <summary>The title value.</summary>
    public string Title = "";
    /// <summary>The year value.</summary>
    public string Year = "";
    /// <summary>The URL value.</summary>
    public string Url = "";
    /// <summary>The displayed citation text.</summary>
    public string DisplayText = "";
    /// <summary>All qualified bibliography-mark attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The containing text block.</summary>
    public OdfTextBlock? TextBlock;
    /// <summary>The corresponding structured text run.</summary>
    public OdfTextRun? Run;
    /// <summary>The XML part containing the entry.</summary>
    public string PartPath = "";
    /// <summary>The underlying bibliography-mark element.</summary>
    public OdfNode? Node;
    internal string OriginalDisplayText = "";
}
