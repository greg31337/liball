namespace odf;

/// <summary>
/// Identifies the role of a drawing reference.
/// </summary>
public enum OdfDrawingReferenceKind
{
    /// <summary>The reference role is unrecognized.</summary>
    Unknown = 0,
    /// <summary>A frame copy source.</summary>
    CopyOf = 1,
    /// <summary>A caption target.</summary>
    Caption = 2,
    /// <summary>A connector start shape.</summary>
    ConnectorStart = 3,
    /// <summary>A connector end shape.</summary>
    ConnectorEnd = 4,
    /// <summary>A hyperlink or package-resource target.</summary>
    Hyperlink = 5,
    /// <summary>A page navigation-order entry.</summary>
    NavigationOrder = 6,
    /// <summary>A drawing layer reference.</summary>
    Layer = 7,
    /// <summary>A drawing or presentation style reference.</summary>
    Style = 8,
    /// <summary>A text style reference.</summary>
    TextStyle = 9,
    /// <summary>A master-page reference.</summary>
    MasterPage = 10,
    /// <summary>A presentation page-layout reference.</summary>
    PresentationPageLayout = 11,
    /// <summary>A form-control reference.</summary>
    Control = 12
}
