namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgImageSampler"/>.</summary>
public interface ISvgImageSampler
{
    /// <summary>Samples one scene image at a local-user-space point using its rendering hint.</summary>
    /// <param name="node">The normalized image scene node.</param>
    /// <param name="point">The finite point in the node's local user space.</param>
    /// <returns>The decoded image pixel or transparent uncovered success.</returns>
    SvgImageSampleResult SampleWithNodeAndPoint(SvgSceneNode node, SvgPoint point);
    /// <summary>Samples one scene image with explicit interpolation and opacity.</summary>
    /// <param name="node">The normalized image scene node.</param>
    /// <param name="point">The finite point in the node's local user space.</param>
    /// <param name="options">The interpolation and opacity options.</param>
    /// <returns>The decoded image pixel or transparent uncovered success.</returns>
    SvgImageSampleResult SampleWithNodeAndPointAndOptions(SvgSceneNode node, SvgPoint point, SvgImageSampleOptions options);
}

/// <summary>Defines the public operations exposed by <see cref="SvgImageRasterizer"/>.</summary>
public interface ISvgImageRasterizer
{
    /// <summary>Draws one image using its rendering hint and bounded defaults.</summary>
    /// <param name="node">The normalized image scene node.</param>
    /// <param name="destination">The mutable premultiplied destination.</param>
    /// <returns>The clipped direct drawing result.</returns>
    SvgImageDrawResult DrawWithNodeAndDestination(SvgSceneNode node, SvgPixelBuffer destination);
    /// <summary>Draws one image with explicit interpolation, opacity, clip, and work bound.</summary>
    /// <param name="node">The normalized image scene node.</param>
    /// <param name="destination">The mutable premultiplied destination.</param>
    /// <param name="options">The drawing options.</param>
    /// <returns>The clipped direct drawing result.</returns>
    SvgImageDrawResult DrawWithNodeAndDestinationAndOptions(SvgSceneNode node, SvgPixelBuffer destination, SvgImageDrawOptions options);
}
