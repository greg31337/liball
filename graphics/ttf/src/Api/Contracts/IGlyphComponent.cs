namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="GlyphComponent"/>.</summary>
public interface IGlyphComponent
{
    /// <summary>Gets the zero-based component index.</summary>
    /// <returns>The component index.</returns>
    int GetComponentIndex();

    /// <summary>Gets the original component flags.</summary>
    /// <returns>The unsigned sixteen-bit flags as an integer.</returns>
    int GetFlags();

    /// <summary>Gets the directly referenced glyph identifier.</summary>
    /// <returns>The component glyph identifier.</returns>
    int GetGlyphId();

    /// <summary>Gets the component placement form.</summary>
    /// <returns>The placement form.</returns>
    GlyphComponentPlacement GetPlacement();

    /// <summary>Gets the encoded x offset.</summary>
    /// <returns>The encoded x offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses point attachment.</exception>
    int GetXOffset();

    /// <summary>Gets the encoded y offset.</summary>
    /// <returns>The encoded y offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses point attachment.</exception>
    int GetYOffset();

    /// <summary>Gets the offset after applying its scaling rule.</summary>
    /// <returns>The effective x offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses point attachment.</exception>
    double GetEffectiveXOffset();

    /// <summary>Gets the y offset after applying its scaling rule.</summary>
    /// <returns>The effective y offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses point attachment.</exception>
    double GetEffectiveYOffset();

    /// <summary>Gets the accumulated parent point used for attachment.</summary>
    /// <returns>The parent point index.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses an offset.</exception>
    int GetParentPointIndex();

    /// <summary>Gets the child point used for attachment.</summary>
    /// <returns>The child point index.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses an offset.</exception>
    int GetChildPointIndex();

    /// <summary>Gets the component offset-scaling form.</summary>
    /// <returns>The offset-scaling form.</returns>
    GlyphComponentOffsetScaling GetOffsetScaling();

    /// <summary>Gets the encoded transform form.</summary>
    /// <returns>The transform form.</returns>
    GlyphComponentTransformKind GetTransformKind();

    /// <summary>Gets the transform x scale.</summary>
    /// <returns>The x scale.</returns>
    double GetXScale();

    /// <summary>Gets the coefficient multiplying x into transformed y.</summary>
    /// <returns>The transform coefficient.</returns>
    double GetScale01();

    /// <summary>Gets the coefficient multiplying y into transformed x.</summary>
    /// <returns>The transform coefficient.</returns>
    double GetScale10();

    /// <summary>Gets the transform y scale.</summary>
    /// <returns>The y scale.</returns>
    double GetYScale();

    /// <summary>Reports whether the resolved offset is rounded to the grid.</summary>
    /// <returns>True when grid rounding is requested.</returns>
    bool RoundsOffsetToGrid();

    /// <summary>Reports whether another direct component follows.</summary>
    /// <returns>True when the encoded continuation flag is set.</returns>
    bool HasMoreComponents();

    /// <summary>Reports whether composite instructions follow the components.</summary>
    /// <returns>True when the encoded instruction flag is set.</returns>
    bool HasCompositeInstructions();

    /// <summary>Reports whether this component supplies the composite metrics.</summary>
    /// <returns>True when component metrics are inherited.</returns>
    bool UsesMetrics();

    /// <summary>Reports whether this first component marks overlapping geometry.</summary>
    /// <returns>True when overlapping components are declared.</returns>
    bool HasOverlappingComponents();
}
