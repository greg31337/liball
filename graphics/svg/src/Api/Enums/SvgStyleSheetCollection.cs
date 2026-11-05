namespace svg;

/// <summary>Defines how a stylesheet entered a collected style environment.</summary>
public enum SvgStyleSheetSourceKind
{
    /// <summary>The stylesheet came from an SVG style element.</summary>
    Embedded = 0,
    /// <summary>The stylesheet was supplied directly by the caller.</summary>
    Caller = 1,
    /// <summary>The stylesheet was loaded through a CSS import rule.</summary>
    Imported = 2
}
