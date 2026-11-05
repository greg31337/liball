namespace odf;

/// <summary>Describes one generated-index entry template.</summary>
public class OdfIndexEntryTemplate
{
    /// <summary>The template outline level or alphabetical separator level.</summary>
    public string OutlineLevel = "";
    /// <summary>The template bibliography type.</summary>
    public string BibliographyType = "";
    /// <summary>The generated entry paragraph style.</summary>
    public string StyleName = "";
    /// <summary>The ordered entry components.</summary>
    public OdfIndexEntryComponent[] Components = [];
    /// <summary>All qualified template attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The underlying template element.</summary>
    public OdfNode? Node;
}
