namespace odf;

/// <summary>Describes one component of a generated-index entry.</summary>
public class OdfIndexEntryComponent
{
    /// <summary>The component kind.</summary>
    public OdfIndexEntryComponentKind Kind;
    /// <summary>The component text style.</summary>
    public string StyleName = "";
    /// <summary>Literal span text.</summary>
    public string Text = "";
    /// <summary>The chapter display form.</summary>
    public string Display = "";
    /// <summary>The chapter outline level.</summary>
    public string OutlineLevel = "";
    /// <summary>The bibliography data-field name.</summary>
    public string BibliographyDataField = "";
    /// <summary>The tab-stop type.</summary>
    public string TabType = "";
    /// <summary>The tab-stop position.</summary>
    public string TabPosition = "";
    /// <summary>The tab-stop leader character.</summary>
    public string LeaderCharacter = "";
    /// <summary>All qualified component attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The underlying component element.</summary>
    public OdfNode? Node;
    internal string OriginalText = "";
}
