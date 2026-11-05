namespace svg;

/// <summary>Samples solid, transformed gradient, and bounded repeating-pattern scene paints.</summary>
public class SvgPaintSampler : ISvgPaintSampler
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPaintSampler()
    {
    }

    /// <summary>Samples one paint at a target-user-space point using deterministic defaults.</summary>
    /// <param name="resource">The solid or gradient scene resource.</param>
    /// <param name="point">The finite point in the paint target's user space.</param>
    /// <returns>The premultiplied sampled pixel or a stable failure.</returns>
    public SvgPaintSampleResult SampleWithResourceAndPoint(SvgSceneResource resource, SvgPoint point)
    {
        return new SvgPaintSampler().SampleWithResourceAndPointAndOptions(resource, point, new SvgPaintSampleOptions());
    }

    /// <summary>Samples one paint with explicit opacity, output encoding, gradient, and pattern bounds.</summary>
    /// <param name="resource">The solid or gradient scene resource.</param>
    /// <param name="point">The finite point in the paint target's user space.</param>
    /// <param name="options">The bounded sampling options.</param>
    /// <returns>The premultiplied sampled pixel or a stable failure.</returns>
    public SvgPaintSampleResult SampleWithResourceAndPointAndOptions(SvgSceneResource resource, SvgPoint point, SvgPaintSampleOptions options)
    {
        return new SvgPaintSampler().SampleWithResourceAndPointAndOptionsAndContext(resource, point, options, new SvgPatternSampleContext());
    }

    internal SvgPaintSampleResult SampleWithResourceAndPointAndOptionsAndContext(SvgSceneResource resource, SvgPoint? point, SvgPaintSampleOptions options, SvgPatternSampleContext context)
    {
        if (!SvgPaintSampleMath.Valid(resource, point, options)) return SvgPaintSampleMath.Failure(SvgPaintSampleError.InvalidArgument);
        if (resource.Kind == SvgSceneResourceKind.SolidPaint) return SvgPaintSampleMath.Solid(resource.SolidPaint, options);
        if (resource.Kind == SvgSceneResourceKind.PatternPaint) return SvgPatternPaintSampler.Sample(resource, point, options, context);
        if (resource.Kind != SvgSceneResourceKind.GradientPaint) return SvgPaintSampleMath.Failure(SvgPaintSampleError.UnsupportedPaint);
        SvgGradientSampleState state = new SvgGradientSampleState(resource.GradientPaint, point, options);
        return state.Run();
    }
}
