namespace svg;

/// <summary>Computes per-node viewport, font-relative, root-font, and object-bounding-box length contexts.</summary>
public class SvgUnitContextResolver : ISvgUnitContextResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgUnitContextResolver()
    {
    }

    /// <summary>Converts a typed time value to another time unit.</summary>
    /// <param name="time">The source time.</param>
    /// <param name="targetUnit">The target time unit.</param>
    /// <returns>The converted finite value or a stable failure.</returns>
    public SvgUnitConversionResult ConvertTime(SvgTime? time, SvgTimeUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertTime(time, targetUnit);
    }

    /// <summary>Converts a typed frequency value to another frequency unit.</summary>
    /// <param name="frequency">The source frequency.</param>
    /// <param name="targetUnit">The target frequency unit.</param>
    /// <returns>The converted finite value or a stable failure.</returns>
    public SvgUnitConversionResult ConvertFrequency(SvgFrequency? frequency, SvgFrequencyUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertFrequency(frequency, targetUnit);
    }

    /// <summary>Converts a typed resolution value to another resolution unit.</summary>
    /// <param name="resolution">The source resolution.</param>
    /// <param name="targetUnit">The target resolution unit.</param>
    /// <returns>The converted finite value or a stable failure.</returns>
    public SvgUnitConversionResult ConvertResolution(SvgResolution? resolution, SvgResolutionUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertResolution(resolution, targetUnit);
    }

    /// <summary>Evaluates a positive ratio as a finite numeric value.</summary>
    /// <param name="ratio">The source ratio.</param>
    /// <returns>The finite quotient or a stable failure.</returns>
    public SvgUnitConversionResult EvaluateRatio(SvgRatio? ratio)
    {
        if (ratio == null) return SvgScalarUnitConverter.Failure(SvgUnitConversionError.InvalidArgument);
        return ratio.ToValue();
    }

    /// <summary>Creates an independent inherited context for a non-element node.</summary>
    /// <param name="inherited">The containing node context.</param>
    /// <returns>An independent context without an inherited object bounding box.</returns>
    public SvgLengthContext Inherit(SvgLengthContext inherited)
    {
        SvgLengthContext result = new SvgUnitContextResolver().Copy(inherited);
        new SvgUnitContextResolver().ClearObjectBoundingBox(result);
        return result;
    }

    /// <summary>Computes an element context from its computed font size and containing context.</summary>
    /// <param name="style">The element computed style, or <c>null</c>.</param>
    /// <param name="inherited">The containing node context.</param>
    /// <param name="rootElement">Whether this is the root element of its resolved document.</param>
    /// <returns>An independent context with computed em, ex, ch, and rem inputs.</returns>
    public SvgLengthContext Resolve(SvgComputedStyle? style, SvgLengthContext inherited, bool rootElement)
    {
        SvgLengthContext result = new SvgUnitContextResolver().Inherit(inherited);
        double inheritedFontSize = result.HasFontSize ? result.FontSize : result.HasRootFontSize ? result.RootFontSize : 16;
        double fontSize = new SvgUnitContextResolver().ResolveFontSize(style, result, inheritedFontSize);
        double xHeightRatio = new SvgUnitContextResolver().MetricRatio(result.HasXHeight, result.XHeight, result.HasFontSize, result.FontSize);
        double zeroAdvanceRatio = new SvgUnitContextResolver().MetricRatio(result.HasZeroAdvance, result.ZeroAdvance, result.HasFontSize, result.FontSize);
        result.HasFontSize = true;
        result.FontSize = fontSize;
        result.HasXHeight = true;
        result.XHeight = fontSize * xHeightRatio;
        result.HasZeroAdvance = true;
        result.ZeroAdvance = fontSize * zeroAdvanceRatio;
        if (!result.HasRootFontSize)
        {
            result.HasRootFontSize = true;
            result.RootFontSize = inheritedFontSize;
        }
        if (rootElement) result.RootFontSize = fontSize;
        return result;
    }

    /// <summary>Adds an independent object-bounding-box basis to a computed node context.</summary>
    /// <param name="context">The node viewport and font context.</param>
    /// <param name="bounds">The finite nonempty local object bounds, or <c>null</c>.</param>
    /// <returns>An independent context with object-bounding-box dimensions when available.</returns>
    public SvgLengthContext WithObjectBoundingBox(SvgLengthContext context, SvgRect? bounds)
    {
        SvgLengthContext result = new SvgUnitContextResolver().Copy(context);
        new SvgUnitContextResolver().ClearObjectBoundingBox(result);
        if (bounds != null && !bounds.IsEmpty && SvgFiniteMath.IsFinite(bounds.Width) && SvgFiniteMath.IsFinite(bounds.Height) && bounds.Width >= 0 && bounds.Height >= 0)
        {
            result.HasObjectBoundingBox = true;
            result.ObjectBoundingBoxWidth = bounds.Width;
            result.ObjectBoundingBoxHeight = bounds.Height;
        }
        return result;
    }

    /// <summary>Creates an independent copy of a complete length context.</summary>
    /// <param name="source">The source context, or <c>null</c>.</param>
    /// <returns>An independent context with the same available inputs.</returns>
    public SvgLengthContext Copy(SvgLengthContext? source)
    {
        SvgLengthContext result = new SvgLengthContext();
        if (source == null) return result;
        result.HasViewport = source.HasViewport;
        result.ViewportWidth = source.ViewportWidth;
        result.ViewportHeight = source.ViewportHeight;
        result.HasFontSize = source.HasFontSize;
        result.FontSize = source.FontSize;
        result.HasXHeight = source.HasXHeight;
        result.XHeight = source.XHeight;
        result.HasZeroAdvance = source.HasZeroAdvance;
        result.ZeroAdvance = source.ZeroAdvance;
        result.HasRootFontSize = source.HasRootFontSize;
        result.RootFontSize = source.RootFontSize;
        result.HasObjectBoundingBox = source.HasObjectBoundingBox;
        result.ObjectBoundingBoxWidth = source.ObjectBoundingBoxWidth;
        result.ObjectBoundingBoxHeight = source.ObjectBoundingBoxHeight;
        return result;
    }
double ResolveFontSize(SvgComputedStyle? style, SvgLengthContext inherited, double fallback)
    {
        if (style == null) return fallback;
        SvgCascadedValue? cascaded = style.Cascade == null ? null : style.Cascade.Find(SvgCssPropertyNames.FontSize);
        if (cascaded != null && cascaded.Kind == SvgCascadedValueKind.Inherited) return fallback;
        SvgComputedValue? value = style.Find(SvgCssPropertyNames.FontSize);
        if (value == null) return fallback;
        if (value.Kind == SvgComputedValueKind.Length && value.Length != null)
        {
            SvgLengthEvaluationResult evaluated = value.Length.Evaluate(inherited, SvgLengthPercentageBasis.FontSize);
            if (evaluated.Success && evaluated.Value >= 0) return evaluated.Value;
            return fallback;
        }
        if (value.Kind == SvgComputedValueKind.Keyword) return new SvgUnitContextResolver().KeywordFontSize(value.Keyword, fallback, inherited);
        return fallback;
    }
double KeywordFontSize(string keyword, double inheritedSize, SvgLengthContext inherited)
    {
        double medium = inherited.HasRootFontSize ? inherited.RootFontSize : 16;
        if (keyword == "xx-small") return medium * 0.6;
        if (keyword == "x-small") return medium * 0.75;
        if (keyword == "small") return medium * 0.8888888888888888;
        if (keyword == "medium") return medium;
        if (keyword == "large") return medium * 1.2;
        if (keyword == "x-large") return medium * 1.5;
        if (keyword == "xx-large") return medium * 2;
        if (keyword == "xxx-large") return medium * 3;
        if (keyword == "larger" || keyword == "math") return inheritedSize * 1.2;
        if (keyword == "smaller") return inheritedSize / 1.2;
        return inheritedSize;
    }
double MetricRatio(bool hasMetric, double metric, bool hasFontSize, double fontSize)
    {
        if (hasMetric && hasFontSize && fontSize > 0 && SvgFiniteMath.IsFinite(metric) && metric >= 0) return metric / fontSize;
        return 0.5;
    }
void ClearObjectBoundingBox(SvgLengthContext context)
    {
        context.HasObjectBoundingBox = false;
        context.ObjectBoundingBoxWidth = 0;
        context.ObjectBoundingBoxHeight = 0;
    }
}
