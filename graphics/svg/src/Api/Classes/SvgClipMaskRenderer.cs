namespace svg;

/// <summary>Intersects nested clip entries and applies normalized alpha or luminance masks to premultiplied pixels.</summary>
public class SvgClipMaskRenderer : ISvgClipMaskRenderer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgClipMaskRenderer()
    {
    }

    /// <summary>Applies an explicit clip stack and optional mask to a mutable surface.</summary>
    /// <param name="surface">The premultiplied target surface.</param>
    /// <param name="options">The complete bounded clipping and masking options.</param>
    /// <returns>The completed atomic effect application or stable failure.</returns>
    public SvgClipMaskResult Apply(SvgPixelBuffer surface, SvgClipMaskOptions options)
    {
        SvgClipMaskState state = new SvgClipMaskState(surface, options);
        return state.Run();
    }
}
