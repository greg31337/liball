namespace ttf;

/// <summary>Identifies the encoded form of a component transform.</summary>
public enum GlyphComponentTransformKind
{
    /// <summary>The component has an identity transform.</summary>
    Identity = 0,

    /// <summary>The component has one uniform scale.</summary>
    UniformScale = 1,

    /// <summary>The component has separate horizontal and vertical scales.</summary>
    SeparateScale = 2,

    /// <summary>The component has a full two-by-two transform.</summary>
    TwoByTwo = 3
}
