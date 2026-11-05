namespace svg;

/// <summary>Draws decoded scene images directly into clipped premultiplied destinations.</summary>
public class SvgImageRasterizer : ISvgImageRasterizer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgImageRasterizer()
    {
    }

    /// <summary>Draws one image using its rendering hint and bounded defaults.</summary>
    /// <param name="node">The normalized image scene node.</param>
    /// <param name="destination">The mutable premultiplied destination.</param>
    /// <returns>The clipped direct drawing result.</returns>
    public SvgImageDrawResult DrawWithNodeAndDestination(SvgSceneNode node, SvgPixelBuffer destination)
    {
        return new SvgImageRasterizer().DrawWithNodeAndDestinationAndOptions(node, destination, new SvgImageDrawOptions());
    }

    /// <summary>Draws one image with explicit interpolation, opacity, clip, and work bound.</summary>
    /// <param name="node">The normalized image scene node.</param>
    /// <param name="destination">The mutable premultiplied destination.</param>
    /// <param name="options">The drawing options.</param>
    /// <returns>The clipped direct drawing result.</returns>
    public SvgImageDrawResult DrawWithNodeAndDestinationAndOptions(SvgSceneNode node, SvgPixelBuffer destination, SvgImageDrawOptions options)
    {
        SvgImageDrawState state = new SvgImageDrawState(node, destination, options);
        return state.Run();
    }
}
