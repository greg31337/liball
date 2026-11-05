namespace ttf;

/// <summary>Identifies the operation performed by a normalized COLR paint node.</summary>
public enum ColorPaintKind
{
    /// <summary>Paint children in order as layers.</summary>
    LayerList = 0,
    /// <summary>Fill a glyph outline with the child paint.</summary>
    Glyph = 1,
    /// <summary>Reference another COLR base glyph.</summary>
    ColrGlyph = 2,
    /// <summary>Paint a linear gradient.</summary>
    LinearGradient = 3,
    /// <summary>Paint a radial gradient.</summary>
    RadialGradient = 4,
    /// <summary>Paint a sweep gradient.</summary>
    SweepGradient = 5,
    /// <summary>Apply a normalized affine transform to the child paint.</summary>
    Transform = 6,
    /// <summary>Composite the two child paints.</summary>
    Composite = 7,
    /// <summary>Use a color resolved from the selected CPAL palette.</summary>
    PaletteColor = 8,
    /// <summary>Use the caller-selected foreground color.</summary>
    ForegroundColor = 9,
    /// <summary>Preserve an unsupported COLR paint as an explicit node.</summary>
    Unsupported = 10
}
