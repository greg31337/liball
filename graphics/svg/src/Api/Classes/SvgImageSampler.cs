namespace svg;

/// <summary>Samples decoded image resources through normalized preserve-aspect-ratio mappings.</summary>
public class SvgImageSampler : ISvgImageSampler
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgImageSampler()
    {
    }

    /// <summary>Samples one scene image at a local-user-space point using its rendering hint.</summary>
    /// <param name="node">The normalized image scene node.</param>
    /// <param name="point">The finite point in the node's local user space.</param>
    /// <returns>The decoded image pixel or transparent uncovered success.</returns>
    public SvgImageSampleResult SampleWithNodeAndPoint(SvgSceneNode node, SvgPoint point)
    {
        return new SvgImageSampler().SampleWithNodeAndPointAndOptions(node, point, new SvgImageSampleOptions());
    }

    /// <summary>Samples one scene image with explicit interpolation and opacity.</summary>
    /// <param name="node">The normalized image scene node.</param>
    /// <param name="point">The finite point in the node's local user space.</param>
    /// <param name="options">The interpolation and opacity options.</param>
    /// <returns>The decoded image pixel or transparent uncovered success.</returns>
    public SvgImageSampleResult SampleWithNodeAndPointAndOptions(SvgSceneNode node, SvgPoint point, SvgImageSampleOptions options)
    {
        SvgImageSampleResult? validation = SvgImageSampleMath.Validate(node, point, options);
        if (validation != null) return validation;
        SvgSceneImageData? image = node.Image;
        SvgPixelBuffer? source = (((image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Resource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DecodedPixels;
        if (!SvgImageSampleMath.Contains(image.Bounds, point.X, point.Y)) return SvgImageSampleMath.NoPixel((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ColorSpace);
        SvgMatrixOperationResult inverse = image.ContentTransform.Invert();
        if (!inverse.Success) return SvgImageSampleMath.MatrixFailure(inverse.Error);
        SvgPointTransformResult mapped = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(point);
        if (!mapped.Success) return SvgImageSampleMath.Failure(SvgImageSampleError.NonFiniteResult);
        SvgImageSamplingMode mode = SvgImageSampleMath.Mode(options.SamplingMode, image.Rendering);
        return SvgImageSampleMath.Source(source, (mapped.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, mapped.Point.Y, mode, options.Opacity);
    }
}
