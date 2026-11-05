namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgClipMaskRenderer"/>.</summary>
public interface ISvgClipMaskRenderer
{
    /// <summary>Applies an explicit clip stack and optional mask to a mutable surface.</summary>
    /// <param name="surface">The premultiplied target surface.</param>
    /// <param name="options">The complete bounded clipping and masking options.</param>
    /// <returns>The completed atomic effect application or stable failure.</returns>
    SvgClipMaskResult Apply(SvgPixelBuffer surface, SvgClipMaskOptions options);
}
