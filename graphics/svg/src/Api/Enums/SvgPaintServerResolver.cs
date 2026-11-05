namespace svg;

/// <summary>Identifies a resolved SVG gradient or pattern paint server.</summary>
public enum SvgPaintServerKind
{
    /// <summary>No supported paint server was resolved.</summary>
    None = 0,
    /// <summary>A linear gradient was resolved.</summary>
    LinearGradient = 1,
    /// <summary>A radial gradient was resolved.</summary>
    RadialGradient = 2,
    /// <summary>A repeating pattern was resolved.</summary>
    Pattern = 3
}

/// <summary>Identifies the coordinate system used by paint-server geometry.</summary>
public enum SvgPaintServerUnits
{
    /// <summary>Coordinates use the referencing element's current user space.</summary>
    UserSpaceOnUse = 0,
    /// <summary>Coordinates use a normalized object bounding box.</summary>
    ObjectBoundingBox = 1
}

/// <summary>Identifies gradient extension behavior outside its stop interval.</summary>
public enum SvgGradientSpreadMethod
{
    /// <summary>Terminal colors extend beyond the gradient interval.</summary>
    Pad = 0,
    /// <summary>The gradient interval alternates direction.</summary>
    Reflect = 1,
    /// <summary>The gradient interval repeats in its original direction.</summary>
    Repeat = 2
}

/// <summary>Identifies whether a resolved gradient paints nothing, one color, or an interpolation.</summary>
public enum SvgGradientPaintKind
{
    /// <summary>The gradient has no effective paint.</summary>
    None = 0,
    /// <summary>The gradient paints one effective stop color.</summary>
    Solid = 1,
    /// <summary>The gradient interpolates two or more stops.</summary>
    Gradient = 2
}
