namespace odf;

/// <summary>Describes structured text in one master-page header or footer.</summary>
public class OdfMasterPageContent
{
    /// <summary>The header or footer variant.</summary>
    public OdfMasterPageContentKind Kind;
    /// <summary>The containing master page.</summary>
    public OdfMasterPage? MasterPage;
    /// <summary>The contained paragraphs and headings.</summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>The underlying header or footer element.</summary>
    public OdfNode? Node;
}
