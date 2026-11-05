namespace odf;

/// <summary>Describes one page in an explicit page sequence.</summary>
public class OdfPageSequenceEntry
{
    /// <summary>The referenced master-page name.</summary>
    public string MasterPageName = "";
    /// <summary>The uniquely resolved master page.</summary>
    public OdfMasterPage? MasterPage;
    /// <summary>The underlying text:page element.</summary>
    public OdfNode? Node;
}
