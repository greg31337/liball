namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgUnitContextResolver"/>.</summary>
public interface ISvgUnitContextResolver
{
    /// <summary>Converts a typed time value to another time unit.</summary>
    /// <param name="time">The source time.</param>
    /// <param name="targetUnit">The target time unit.</param>
    /// <returns>The converted finite value or a stable failure.</returns>
    SvgUnitConversionResult ConvertTime(SvgTime? time, SvgTimeUnit targetUnit);
    /// <summary>Converts a typed frequency value to another frequency unit.</summary>
    /// <param name="frequency">The source frequency.</param>
    /// <param name="targetUnit">The target frequency unit.</param>
    /// <returns>The converted finite value or a stable failure.</returns>
    SvgUnitConversionResult ConvertFrequency(SvgFrequency? frequency, SvgFrequencyUnit targetUnit);
    /// <summary>Converts a typed resolution value to another resolution unit.</summary>
    /// <param name="resolution">The source resolution.</param>
    /// <param name="targetUnit">The target resolution unit.</param>
    /// <returns>The converted finite value or a stable failure.</returns>
    SvgUnitConversionResult ConvertResolution(SvgResolution? resolution, SvgResolutionUnit targetUnit);
    /// <summary>Evaluates a positive ratio as a finite numeric value.</summary>
    /// <param name="ratio">The source ratio.</param>
    /// <returns>The finite quotient or a stable failure.</returns>
    SvgUnitConversionResult EvaluateRatio(SvgRatio? ratio);
    /// <summary>Creates an independent inherited context for a non-element node.</summary>
    /// <param name="inherited">The containing node context.</param>
    /// <returns>An independent context without an inherited object bounding box.</returns>
    SvgLengthContext Inherit(SvgLengthContext inherited);
    /// <summary>Computes an element context from its computed font size and containing context.</summary>
    /// <param name="style">The element computed style, or <c>null</c>.</param>
    /// <param name="inherited">The containing node context.</param>
    /// <param name="rootElement">Whether this is the root element of its resolved document.</param>
    /// <returns>An independent context with computed em, ex, ch, and rem inputs.</returns>
    SvgLengthContext Resolve(SvgComputedStyle? style, SvgLengthContext inherited, bool rootElement);
    /// <summary>Adds an independent object-bounding-box basis to a computed node context.</summary>
    /// <param name="context">The node viewport and font context.</param>
    /// <param name="bounds">The finite nonempty local object bounds, or <c>null</c>.</param>
    /// <returns>An independent context with object-bounding-box dimensions when available.</returns>
    SvgLengthContext WithObjectBoundingBox(SvgLengthContext context, SvgRect? bounds);
    /// <summary>Creates an independent copy of a complete length context.</summary>
    /// <param name="source">The source context, or <c>null</c>.</param>
    /// <returns>An independent context with the same available inputs.</returns>
    SvgLengthContext Copy(SvgLengthContext? source);
}
