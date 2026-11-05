namespace ttf;

/// <summary>Identifies how a composite component offset is interpreted.</summary>
public enum GlyphComponentOffsetScaling
{
    /// <summary>The font uses the default unscaled-offset interpretation.</summary>
    Default = 0,

    /// <summary>The component transform also transforms the offset.</summary>
    Scaled = 1,

    /// <summary>The offset is applied after the component transform.</summary>
    Unscaled = 2
}
