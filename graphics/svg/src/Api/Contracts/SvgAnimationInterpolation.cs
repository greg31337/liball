namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgAnimationInterpolator"/>.</summary>
public interface ISvgAnimationInterpolator
{
    /// <summary>Parses authored text as one typed animation value.</summary>
    /// <param name="kind">The requested value family.</param>
    /// <param name="text">The authored value.</param>
    /// <returns>The parsed typed value.</returns>
    SvgAnimationValueParseResult ParseWithKindAndText(SvgAnimationValueKind kind, string text);
    /// <summary>Parses authored text with explicit bounds.</summary>
    /// <param name="kind">The requested value family.</param>
    /// <param name="text">The authored value.</param>
    /// <param name="options">The parsing and interpolation options.</param>
    /// <returns>The parsed typed value.</returns>
    SvgAnimationValueParseResult ParseWithKindAndTextAndOptions(SvgAnimationValueKind kind, string? text, SvgAnimationInterpolationOptions options);
    /// <summary>Interpolates two typed values with default conversion options.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="progress">The finite progress in the unit interval.</param>
    /// <param name="mode">The interpolation mode.</param>
    /// <returns>The typed interpolated value.</returns>
    SvgAnimationInterpolationResult InterpolateWithFromAndToAndProgressAndMode(SvgAnimationValue from, SvgAnimationValue to, double progress, SvgAnimationCalcMode mode);
    /// <summary>Interpolates two typed values with explicit conversion, color-space, spline, and bounds.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="progress">The finite progress in the unit interval.</param>
    /// <param name="mode">The interpolation mode.</param>
    /// <param name="options">The interpolation options.</param>
    /// <returns>The typed interpolated value.</returns>
    SvgAnimationInterpolationResult InterpolateWithFromAndToAndProgressAndModeAndOptions(SvgAnimationValue from, SvgAnimationValue to, double progress, SvgAnimationCalcMode mode, SvgAnimationInterpolationOptions options);
    /// <summary>Interpolates an evenly spaced or paced sequence of typed values.</summary>
    /// <param name="values">At least two compatible typed values.</param>
    /// <param name="progress">The finite overall progress in the unit interval.</param>
    /// <param name="mode">The interpolation mode.</param>
    /// <param name="splines">One spline per segment for spline mode, otherwise an empty list.</param>
    /// <param name="options">The interpolation options.</param>
    /// <returns>The selected segment and typed interpolated value.</returns>
    SvgAnimationInterpolationResult InterpolateSequence(System.Collections.Generic.List<SvgAnimationValue>? values, double progress, SvgAnimationCalcMode mode, System.Collections.Generic.List<SvgKeySpline>? splines, SvgAnimationInterpolationOptions options);
    /// <summary>Computes the finite typed distance used by paced interpolation.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="options">The conversion and color-space options.</param>
    /// <returns>A successful result whose Distance field contains the typed distance.</returns>
    SvgAnimationInterpolationResult Distance(SvgAnimationValue? from, SvgAnimationValue? to, SvgAnimationInterpolationOptions options);
}
