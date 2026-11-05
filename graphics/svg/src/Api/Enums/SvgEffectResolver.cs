namespace svg;

/// <summary>Identifies a resolved reusable SVG effect resource.</summary>
public enum SvgEffectKind
{
    /// <summary>No supported effect was resolved.</summary>
    None = 0,
    /// <summary>A marker resource was resolved.</summary>
    Marker = 1,
    /// <summary>A clipping-path resource was resolved.</summary>
    ClipPath = 2,
    /// <summary>A mask resource was resolved.</summary>
    Mask = 3
}

/// <summary>Identifies the coordinate scale applied to marker dimensions and content.</summary>
public enum SvgMarkerUnits
{
    /// <summary>One marker unit equals the referencing element's used stroke width.</summary>
    StrokeWidth = 0,
    /// <summary>Marker units equal the referencing element's current user units.</summary>
    UserSpaceOnUse = 1
}

/// <summary>Identifies how a marker obtains its placement angle.</summary>
public enum SvgMarkerOrientKind
{
    /// <summary>The marker uses an authored fixed angle.</summary>
    Angle = 0,
    /// <summary>The marker follows the path direction.</summary>
    Auto = 1,
    /// <summary>The marker follows the path direction and reverses at a start vertex.</summary>
    AutoStartReverse = 2
}

/// <summary>Identifies the vertex role of a marker instance.</summary>
public enum SvgMarkerPlacement
{
    /// <summary>The marker is placed at the beginning of a subpath.</summary>
    Start = 0,
    /// <summary>The marker is placed at an interior vertex.</summary>
    Mid = 1,
    /// <summary>The marker is placed at the end of a subpath.</summary>
    End = 2
}

/// <summary>Identifies how rendered mask colors become mask values.</summary>
public enum SvgMaskType
{
    /// <summary>Mask values are derived from color luminance and alpha.</summary>
    Luminance = 0,
    /// <summary>Mask values are derived from alpha only.</summary>
    Alpha = 1
}
