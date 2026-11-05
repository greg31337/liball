namespace svg;

/// <summary>Composites premultiplied RGBA8 pixels and creates bounded opacity groups.</summary>
public class SvgCompositor : ISvgCompositor
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCompositor()
    {
    }

    /// <summary>Composites a complete source over a destination at the origin.</summary>
    /// <param name="source">The premultiplied source surface.</param>
    /// <param name="destination">The mutable premultiplied destination surface.</param>
    /// <returns>The source-over compositing result.</returns>
    public SvgCompositeResult CompositeWithSourceAndDestination(SvgPixelBuffer source, SvgPixelBuffer destination)
    {
        return new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(source, destination, new SvgCompositeOptions());
    }

    /// <summary>Composites a clipped source rectangle with explicit operator, opacity, placement, and bounds.</summary>
    /// <param name="source">The premultiplied source surface.</param>
    /// <param name="destination">The mutable premultiplied destination surface.</param>
    /// <param name="options">The complete bounded compositing options.</param>
    /// <returns>The completed operation or stable failure.</returns>
    public SvgCompositeResult CompositeWithSourceAndDestinationAndOptions(SvgPixelBuffer source, SvgPixelBuffer destination, SvgCompositeOptions options)
    {
        SvgCompositeState state = new SvgCompositeState(source, destination, options);
        return state.Run();
    }

    /// <summary>Creates a transparent bounded offscreen group surface.</summary>
    /// <param name="bounds">The destination-local bounds represented by the group.</param>
    /// <param name="colorSpace">The concrete group color space.</param>
    /// <param name="opacity">The group opacity applied once during compositing.</param>
    /// <param name="limits">The offscreen allocation limits.</param>
    /// <returns>The created opacity group or stable failure.</returns>
    public SvgOpacityGroupResult CreateOpacityGroupWithBoundsAndColorSpaceAndOpacityAndLimits(SvgPixelRect bounds, SvgPixelColorSpace colorSpace, double opacity, SvgRasterLimits limits)
    {
        if (!SvgCompositeMath.ValidRectangle(bounds) || !SvgCompositeMath.Known(colorSpace) || !SvgFiniteMath.IsFinite(opacity) || opacity < 0 || opacity > 1 || !SvgCompositeMath.ValidLimits(limits)) return new SvgOpacityGroupResult(false, SvgCompositeError.InvalidArgument, null);
        SvgPixelBufferResult created = SvgPixelBuffer.Create(bounds.Width, bounds.Height, colorSpace, limits);
        if (!created.Success) return new SvgOpacityGroupResult(false, SvgCompositeMath.MapAllocation(created.Error), null);
        SvgOpacityGroup group = new SvgOpacityGroup(created.Buffer, new SvgPixelRect(bounds.X, bounds.Y, bounds.Width, bounds.Height), opacity, SvgCompositeMath.CopyLimits(limits));
        return new SvgOpacityGroupResult(true, SvgCompositeError.None, group);
    }

    /// <summary>Creates an offscreen group from a planned scene layer's pixel-enclosing bounds and opacity.</summary>
    /// <param name="layer">The planned compositing layer.</param>
    /// <param name="colorSpace">The concrete group color space.</param>
    /// <param name="limits">The offscreen allocation limits.</param>
    /// <returns>The created opacity group or stable failure.</returns>
    public SvgOpacityGroupResult CreateOpacityGroupWithLayerAndColorSpaceAndLimits(SvgSceneLayer? layer, SvgPixelColorSpace colorSpace, SvgRasterLimits limits)
    {
        if (layer == null || layer.PaintBounds == null || !SvgCompositeMath.ValidBounds(layer.PaintBounds)) return new SvgOpacityGroupResult(false, SvgCompositeError.InvalidArgument, null);
        SvgPixelRect? bounds = SvgCompositeMath.PixelBounds(layer.PaintBounds);
        if (bounds == null) return new SvgOpacityGroupResult(false, SvgCompositeError.InvalidArgument, null);
        SvgOpacityGroupResult result = new SvgCompositor().CreateOpacityGroupWithBoundsAndColorSpaceAndOpacityAndLimits(bounds, colorSpace, layer.Opacity, limits);
        if (!result.Success) return result;
        (result.Group ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).BlendMode = SvgBlendModeMath.Parse(layer.BlendMode);
        result.Group.BlendColorSpace = layer.ColorInterpolation == SvgColorSpace.SrgbLinear ? SvgBlendColorSpace.SrgbLinear : SvgBlendColorSpace.Srgb;
        result.Group.Isolated = layer.Isolated;
        return result;
    }
}
