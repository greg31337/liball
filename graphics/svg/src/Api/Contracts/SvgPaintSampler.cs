namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPaintSampler"/>.</summary>
public interface ISvgPaintSampler
{
    /// <summary>Samples one paint at a target-user-space point using deterministic defaults.</summary>
    /// <param name="resource">The solid or gradient scene resource.</param>
    /// <param name="point">The finite point in the paint target's user space.</param>
    /// <returns>The premultiplied sampled pixel or a stable failure.</returns>
    SvgPaintSampleResult SampleWithResourceAndPoint(SvgSceneResource resource, SvgPoint point);
    /// <summary>Samples one paint with explicit opacity, output encoding, gradient, and pattern bounds.</summary>
    /// <param name="resource">The solid or gradient scene resource.</param>
    /// <param name="point">The finite point in the paint target's user space.</param>
    /// <param name="options">The bounded sampling options.</param>
    /// <returns>The premultiplied sampled pixel or a stable failure.</returns>
    SvgPaintSampleResult SampleWithResourceAndPointAndOptions(SvgSceneResource resource, SvgPoint point, SvgPaintSampleOptions options);
}
