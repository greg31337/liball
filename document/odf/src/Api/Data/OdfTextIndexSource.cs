namespace odf;

/// <summary>Describes selection, sorting, title, and templates for a text index.</summary>
public class OdfTextIndexSource
{
    /// <summary>The source family.</summary>
    public OdfTextIndexKind Kind;
    /// <summary>The maximum or template outline level.</summary>
    public string OutlineLevel = "";
    /// <summary>The index scope.</summary>
    public string IndexScope = "";
    /// <summary>The preserved relative-tab-stop-position value.</summary>
    public string RelativeTabStopPosition = "";
    /// <summary>The preserved use-index-marks value.</summary>
    public string UseIndexMarks = "";
    /// <summary>The preserved use-index-source-styles value.</summary>
    public string UseIndexSourceStyles = "";
    /// <summary>The user-index name.</summary>
    public string IndexName = "";
    /// <summary>The caption sequence name.</summary>
    public string CaptionSequenceName = "";
    /// <summary>The caption sequence format.</summary>
    public string CaptionSequenceFormat = "";
    /// <summary>The alphabetical main-entry text style.</summary>
    public string MainEntryStyleName = "";
    /// <summary>The sorting algorithm.</summary>
    public string SortAlgorithm = "";
    /// <summary>The source language.</summary>
    public string Language = "";
    /// <summary>The source country.</summary>
    public string Country = "";
    /// <summary>The generated title text.</summary>
    public string Title = "";
    /// <summary>The generated title paragraph style.</summary>
    public string TitleStyleName = "";
    /// <summary>The entry templates.</summary>
    public OdfIndexEntryTemplate[] Templates = [];
    /// <summary>The source-style groups.</summary>
    public OdfIndexSourceStyle[] SourceStyles = [];
    /// <summary>All qualified source attributes, including family-specific switches.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The underlying source element.</summary>
    public OdfNode? Node;
    /// <summary>The underlying title-template element.</summary>
    public OdfNode? TitleNode;
    internal string OriginalTitle = "";
}
