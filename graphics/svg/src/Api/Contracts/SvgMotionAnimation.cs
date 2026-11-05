namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgMotionAnimation"/>.</summary>
public interface ISvgMotionAnimation
{
    /// <summary>Resolves one target-resolved animateMotion element with secure defaults.</summary>
    /// <param name="animation">The target-resolved motion animation.</param>
    /// <returns>The resolved bounded motion plan.</returns>
    SvgMotionAnimationPlan ResolveWithAnimation(SvgAnimation animation);
    /// <summary>Resolves one target-resolved animateMotion element.</summary>
    /// <param name="animation">The target-resolved motion animation.</param>
    /// <param name="options">The reference, geometry, interpolation, and security options.</param>
    /// <returns>The resolved bounded motion plan.</returns>
    SvgMotionAnimationPlan ResolveWithAnimationAndOptions(SvgAnimation animation, SvgMotionAnimationOptions options);
    /// <summary>Samples a resolved motion plan around the coordinate origin.</summary>
    /// <param name="plan">The resolved motion plan.</param>
    /// <param name="progress">The finite simple progress in the unit interval.</param>
    /// <returns>The sampled supplemental motion transform.</returns>
    SvgMotionAnimationResult SampleWithPlanAndProgress(SvgMotionAnimationPlan plan, double progress);
    /// <summary>Samples a resolved motion plan and composes it after a base transform.</summary>
    /// <param name="plan">The resolved motion plan.</param>
    /// <param name="progress">The finite simple progress in the unit interval.</param>
    /// <param name="repeatIteration">The nonnegative zero-based repeat iteration.</param>
    /// <param name="origin">The finite target point mapped onto the motion path.</param>
    /// <param name="baseTransform">The base target transform, or <c>null</c>.</param>
    /// <param name="options">The sampling and repeat bounds.</param>
    /// <returns>The sampled and composed motion transform.</returns>
    SvgMotionAnimationResult SampleWithPlanAndProgressAndRepeatIterationAndOriginAndBaseTransformAndOptions(SvgMotionAnimationPlan? plan, double progress, int repeatIteration, SvgPoint? origin, SvgTransformList? baseTransform, SvgMotionAnimationOptions options);
    /// <summary>Samples using a timeline entry's simple progress and repeat iteration.</summary>
    /// <param name="plan">The resolved motion plan.</param>
    /// <param name="timeline">The active or frozen timeline entry.</param>
    /// <param name="origin">The finite target point mapped onto the motion path.</param>
    /// <param name="baseTransform">The base target transform, or <c>null</c>.</param>
    /// <param name="options">The sampling and repeat bounds.</param>
    /// <returns>The sampled and composed motion transform.</returns>
    SvgMotionAnimationResult SampleWithPlanAndTimelineAndOriginAndBaseTransformAndOptions(SvgMotionAnimationPlan plan, SvgAnimationTimelineEntry? timeline, SvgPoint origin, SvgTransformList? baseTransform, SvgMotionAnimationOptions options);
}
