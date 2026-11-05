namespace ttf;

/// <summary>Describes one direct component of a composite glyph.</summary>
public class GlyphComponent : IGlyphComponent
{
    int componentIndex;
    int flags;
    int glyphId;
    GlyphComponentPlacement placement;
    int argument1;
    int argument2;
    double effectiveXOffset;
    double effectiveYOffset;
    GlyphComponentOffsetScaling offsetScaling;
    GlyphComponentTransformKind transformKind;
    double xScale;
    double scale01;
    double scale10;
    double yScale;

    internal GlyphComponent(
        SfntCompositeComponent component,
        double variedEffectiveXOffset,
        double variedEffectiveYOffset)
    {
        Initialize(component);

        if (placement == GlyphComponentPlacement.Offset)
        {
            effectiveXOffset = variedEffectiveXOffset;
            effectiveYOffset = variedEffectiveYOffset;
        }
    }

    internal static GlyphComponent Create(SfntCompositeComponent component)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        double effectiveXOffset = 0;
        double effectiveYOffset = 0;
        if (component.GetPlacementKind() ==
            SfntCompositePlacementKind.Offset)
        {
            effectiveXOffset = component.GetEffectiveXOffset();
            effectiveYOffset = component.GetEffectiveYOffset();
        }

        return new GlyphComponent(
            component,
            effectiveXOffset,
            effectiveYOffset);
    }

    internal static GlyphComponent Create(
        SfntCompositeComponent component,
        double variedEffectiveXOffset,
        double variedEffectiveYOffset)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        return new GlyphComponent(
            component,
            variedEffectiveXOffset,
            variedEffectiveYOffset);
    }

    void Initialize(SfntCompositeComponent component)
    {
        componentIndex = component.GetComponentIndex();
        flags = component.GetFlags();
        glyphId = component.GetGlyphId();
        placement = component.GetPlacementKind() == SfntCompositePlacementKind.Offset
            ? GlyphComponentPlacement.Offset
            : GlyphComponentPlacement.PointAttachment;

        if (placement == GlyphComponentPlacement.Offset)
        {
            argument1 = component.GetXOffset();
            argument2 = component.GetYOffset();
        }
        else
        {
            argument1 = component.GetParentPointIndex();
            argument2 = component.GetChildPointIndex();
        }

        offsetScaling = ConvertOffsetScaling(component.GetOffsetScaling());
        SfntCompositeTransform transform = component.GetTransform();
        transformKind = ConvertTransformKind(transform.GetKind());
        xScale = transform.GetXScale();
        scale01 = transform.GetScale01();
        scale10 = transform.GetScale10();
        yScale = transform.GetYScale();
    }

    /// <summary>Gets the zero-based component index.</summary>
    /// <returns>The component index.</returns>
    public int GetComponentIndex() { return componentIndex; }

    /// <summary>Gets the original component flags.</summary>
    /// <returns>The unsigned sixteen-bit flags as an integer.</returns>
    public int GetFlags() { return flags; }

    /// <summary>Gets the directly referenced glyph identifier.</summary>
    /// <returns>The component glyph identifier.</returns>
    public int GetGlyphId() { return glyphId; }

    /// <summary>Gets the component placement form.</summary>
    /// <returns>The placement form.</returns>
    public GlyphComponentPlacement GetPlacement() { return placement; }

    /// <summary>Gets the encoded x offset.</summary>
    /// <returns>The encoded x offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses point attachment.</exception>
    public int GetXOffset() { EnsureOffset(); return argument1; }

    /// <summary>Gets the encoded y offset.</summary>
    /// <returns>The encoded y offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses point attachment.</exception>
    public int GetYOffset() { EnsureOffset(); return argument2; }

    /// <summary>Gets the offset after applying its scaling rule.</summary>
    /// <returns>The effective x offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses point attachment.</exception>
    public double GetEffectiveXOffset() { EnsureOffset(); return effectiveXOffset; }

    /// <summary>Gets the y offset after applying its scaling rule.</summary>
    /// <returns>The effective y offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses point attachment.</exception>
    public double GetEffectiveYOffset() { EnsureOffset(); return effectiveYOffset; }

    /// <summary>Gets the accumulated parent point used for attachment.</summary>
    /// <returns>The parent point index.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses an offset.</exception>
    public int GetParentPointIndex() { EnsureAttachment(); return argument1; }

    /// <summary>Gets the child point used for attachment.</summary>
    /// <returns>The child point index.</returns>
    /// <exception cref="System.InvalidOperationException">The component uses an offset.</exception>
    public int GetChildPointIndex() { EnsureAttachment(); return argument2; }

    /// <summary>Gets the component offset-scaling form.</summary>
    /// <returns>The offset-scaling form.</returns>
    public GlyphComponentOffsetScaling GetOffsetScaling() { return offsetScaling; }

    /// <summary>Gets the encoded transform form.</summary>
    /// <returns>The transform form.</returns>
    public GlyphComponentTransformKind GetTransformKind() { return transformKind; }

    /// <summary>Gets the transform x scale.</summary>
    /// <returns>The x scale.</returns>
    public double GetXScale() { return xScale; }

    /// <summary>Gets the coefficient multiplying x into transformed y.</summary>
    /// <returns>The transform coefficient.</returns>
    public double GetScale01() { return scale01; }

    /// <summary>Gets the coefficient multiplying y into transformed x.</summary>
    /// <returns>The transform coefficient.</returns>
    public double GetScale10() { return scale10; }

    /// <summary>Gets the transform y scale.</summary>
    /// <returns>The y scale.</returns>
    public double GetYScale() { return yScale; }

    /// <summary>Reports whether the resolved offset is rounded to the grid.</summary>
    /// <returns>True when grid rounding is requested.</returns>
    public bool RoundsOffsetToGrid()
    {
        return placement == GlyphComponentPlacement.Offset &&
            (flags & 0x0004) != 0;
    }

    /// <summary>Reports whether another direct component follows.</summary>
    /// <returns>True when the encoded continuation flag is set.</returns>
    public bool HasMoreComponents() { return (flags & 0x0020) != 0; }

    /// <summary>Reports whether composite instructions follow the components.</summary>
    /// <returns>True when the encoded instruction flag is set.</returns>
    public bool HasCompositeInstructions() { return (flags & 0x0100) != 0; }

    /// <summary>Reports whether this component supplies the composite metrics.</summary>
    /// <returns>True when component metrics are inherited.</returns>
    public bool UsesMetrics() { return (flags & 0x0200) != 0; }

    /// <summary>Reports whether this first component marks overlapping geometry.</summary>
    /// <returns>True when overlapping components are declared.</returns>
    public bool HasOverlappingComponents() { return (flags & 0x0400) != 0; }

    void EnsureOffset()
    {
        if (placement != GlyphComponentPlacement.Offset)
        {
            throw new System.InvalidOperationException(
                "A point-attached component has no XY offset.");
        }
    }

    void EnsureAttachment()
    {
        if (placement != GlyphComponentPlacement.PointAttachment)
        {
            throw new System.InvalidOperationException(
                "An offset component has no point attachment.");
        }
    }

    static GlyphComponentOffsetScaling ConvertOffsetScaling(
        SfntCompositeOffsetScaling scaling)
    {
        if (scaling == SfntCompositeOffsetScaling.Scaled)
        {
            return GlyphComponentOffsetScaling.Scaled;
        }

        if (scaling == SfntCompositeOffsetScaling.Unscaled)
        {
            return GlyphComponentOffsetScaling.Unscaled;
        }

        return GlyphComponentOffsetScaling.Default;
    }

    static GlyphComponentTransformKind ConvertTransformKind(
        SfntCompositeTransformKind kind)
    {
        if (kind == SfntCompositeTransformKind.UniformScale)
        {
            return GlyphComponentTransformKind.UniformScale;
        }

        if (kind == SfntCompositeTransformKind.SeparateScale)
        {
            return GlyphComponentTransformKind.SeparateScale;
        }

        if (kind == SfntCompositeTransformKind.TwoByTwo)
        {
            return GlyphComponentTransformKind.TwoByTwo;
        }

        return GlyphComponentTransformKind.Identity;
    }
}
