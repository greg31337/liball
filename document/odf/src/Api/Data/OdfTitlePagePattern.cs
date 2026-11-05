namespace odf;

/// <summary>Describes a first/title-page master followed by a body-page master.</summary>
public class OdfTitlePagePattern
{
    /// <summary>The title-page master name.</summary>
    public string TitleMasterPageName = "";
    /// <summary>The following body-page master name.</summary>
    public string FollowingMasterPageName = "";
    /// <summary>The resolved title-page master.</summary>
    public OdfMasterPage? TitleMasterPage;
    /// <summary>The resolved following master page.</summary>
    public OdfMasterPage? FollowingMasterPage;
    /// <summary>Whether a first-page header variant is available.</summary>
    public bool UsesFirstHeader;
    /// <summary>Whether a first-page footer variant is available.</summary>
    public bool UsesFirstFooter;
    /// <summary>The explicit page sequence from which this pattern was inferred.</summary>
    public OdfPageSequence? PageSequence;
}
