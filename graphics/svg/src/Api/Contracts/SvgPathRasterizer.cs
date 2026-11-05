namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathRasterizer"/>.</summary>
public interface ISvgPathRasterizer
{
    /// <summary>Paints one path using explicit colors, opacity, order, stroke geometry, and sampling hints.</summary>
    /// <param name="path">The valid path in local user coordinates.</param>
    /// <param name="surface">The mutable destination surface.</param>
    /// <param name="options">The complete path-painting options.</param>
    /// <returns>The ordered fill and stroke results.</returns>
    SvgPathRasterResult DrawWithPathAndSurfaceAndOptions(SvgPathData path, SvgPixelBuffer surface, SvgPathRasterOptions options);
    /// <summary>Paints one normalized scene path using caller-supplied premultiplied colors.</summary>
    /// <param name="node">The normalized scene path carrying transform and computed painting semantics.</param>
    /// <param name="surface">The mutable destination surface.</param>
    /// <param name="fillColor">The fill color used when the node has a fill resource.</param>
    /// <param name="strokeColor">The stroke color used when the node has a stroke resource.</param>
    /// <param name="rasterOptions">The clip and scan-conversion limits; the node supplies the transform and rendering hint.</param>
    /// <returns>The ordered fill and stroke results.</returns>
    SvgPathRasterResult DrawWithNodeAndSurfaceAndFillColorAndStrokeColorAndRasterOptions(SvgSceneNode node, SvgPixelBuffer surface, SvgPremultipliedRgba8 fillColor, SvgPremultipliedRgba8 strokeColor, SvgRasterOptions rasterOptions);
}
