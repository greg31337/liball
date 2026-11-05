namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgAnimationComposer"/>.</summary>
public interface ISvgAnimationComposer
{
    /// <summary>Builds a value plan using the animation target's interpolation category.</summary>
    /// <param name="animation">The target-resolved animation.</param>
    /// <returns>The validated value plan.</returns>
    SvgAnimationValuePlan ResolveWithAnimation(SvgAnimation animation);
    /// <summary>Builds a value plan for an explicit value family and bounded options.</summary>
    /// <param name="animation">The target-resolved animation.</param>
    /// <param name="kind">The requested typed value family.</param>
    /// <param name="options">The parsing and composition options.</param>
    /// <returns>The validated value plan.</returns>
    SvgAnimationValuePlan ResolveWithAnimationAndKindAndOptions(SvgAnimation? animation, SvgAnimationValueKind kind, SvgAnimationCompositionOptions options);
    /// <summary>Samples and composes a validated plan at normalized simple progress.</summary>
    /// <param name="plan">The validated value plan.</param>
    /// <param name="underlying">The current underlying typed value, or <c>null</c> when unnecessary.</param>
    /// <param name="progress">The finite simple progress in the unit interval.</param>
    /// <param name="repeatIteration">The nonnegative zero-based repeat iteration.</param>
    /// <param name="options">The composition options.</param>
    /// <returns>The effect and final composed value.</returns>
    SvgAnimationCompositionResult SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(SvgAnimationValuePlan? plan, SvgAnimationValue? underlying, double progress, int repeatIteration, SvgAnimationCompositionOptions options);
    /// <summary>Samples using the current simple progress and repeat iteration of a timeline entry.</summary>
    /// <param name="plan">The validated value plan.</param>
    /// <param name="underlying">The current underlying typed value, or <c>null</c>.</param>
    /// <param name="timeline">The active or frozen timeline entry.</param>
    /// <param name="options">The composition options.</param>
    /// <returns>The effect and final composed value.</returns>
    SvgAnimationCompositionResult SampleWithPlanAndUnderlyingAndTimelineAndOptions(SvgAnimationValuePlan plan, SvgAnimationValue? underlying, SvgAnimationTimelineEntry? timeline, SvgAnimationCompositionOptions options);
}
