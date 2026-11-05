namespace odf;

/// <summary>Identifies a master-page header or footer variant.</summary>
public enum OdfMasterPageContentKind
{
    /// <summary>An unrecognized master-page content kind.</summary>
    Unknown = 0,
    /// <summary>The default header.</summary>
    Header = 1,
    /// <summary>The left-page header.</summary>
    HeaderLeft = 2,
    /// <summary>The first-page header.</summary>
    HeaderFirst = 3,
    /// <summary>The default footer.</summary>
    Footer = 4,
    /// <summary>The left-page footer.</summary>
    FooterLeft = 5,
    /// <summary>The first-page footer.</summary>
    FooterFirst = 6
}
