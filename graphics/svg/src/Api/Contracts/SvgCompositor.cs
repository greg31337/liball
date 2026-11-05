namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCompositor"/>.</summary>
public interface ISvgCompositor
{
    /// <summary>Composites a complete source over a destination at the origin.</summary>
    /// <param name="source">The premultiplied source surface.</param>
    /// <param name="destination">The mutable premultiplied destination surface.</param>
    /// <returns>The source-over compositing result.</returns>
    SvgCompositeResult CompositeWithSourceAndDestination(SvgPixelBuffer source, SvgPixelBuffer destination);
    /// <summary>Composites a clipped source rectangle with explicit operator, opacity, placement, and bounds.</summary>
    /// <param name="source">The premultiplied source surface.</param>
    /// <param name="destination">The mutable premultiplied destination surface.</param>
    /// <param name="options">The complete bounded compositing options.</param>
    /// <returns>The completed operation or stable failure.</returns>
    SvgCompositeResult CompositeWithSourceAndDestinationAndOptions(SvgPixelBuffer source, SvgPixelBuffer destination, SvgCompositeOptions options);
    /// <summary>Creates a transparent bounded offscreen group surface.</summary>
    /// <param name="bounds">The destination-local bounds represented by the group.</param>
    /// <param name="colorSpace">The concrete group color space.</param>
    /// <param name="opacity">The group opacity applied once during compositing.</param>
    /// <param name="limits">The offscreen allocation limits.</param>
    /// <returns>The created opacity group or stable failure.</returns>
    SvgOpacityGroupResult CreateOpacityGroupWithBoundsAndColorSpaceAndOpacityAndLimits(SvgPixelRect bounds, SvgPixelColorSpace colorSpace, double opacity, SvgRasterLimits limits);
    /// <summary>Creates an offscreen group from a planned scene layer's pixel-enclosing bounds and opacity.</summary>
    /// <param name="layer">The planned compositing layer.</param>
    /// <param name="colorSpace">The concrete group color space.</param>
    /// <param name="limits">The offscreen allocation limits.</param>
    /// <returns>The created opacity group or stable failure.</returns>
    SvgOpacityGroupResult CreateOpacityGroupWithLayerAndColorSpaceAndLimits(SvgSceneLayer? layer, SvgPixelColorSpace colorSpace, SvgRasterLimits limits);
}
