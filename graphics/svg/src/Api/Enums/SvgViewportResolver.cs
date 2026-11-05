namespace svg;

/// <summary>Identifies the viewport-specific interpretation of an SVG fragment identifier.</summary>
public enum SvgViewportFragmentKind
{
    /// <summary>No viewport-affecting fragment was supplied.</summary>
    None = 0,
    /// <summary>A bare identifier selected an ordinary SVG element.</summary>
    Element = 1,
    /// <summary>A bare identifier selected a <c>view</c> element.</summary>
    ViewElement = 2,
    /// <summary>An <c>svgView(...)</c> fragment supplied view parameters.</summary>
    SvgView = 3,
    /// <summary>An <c>xywh</c> media fragment supplied a view box.</summary>
    MediaFragment = 4,
    /// <summary>The supplied fragment was malformed or did not identify an element.</summary>
    Invalid = 5
}
