namespace svg;

/// <summary>Normalizes SVG dash arrays and signed offsets.</summary>
public class SvgStrokeDashes : ISvgStrokeDashes
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgStrokeDashes()
    {
    }

    /// <summary>Copies and normalizes a dash array, repeating odd-length arrays.</summary>
    /// <param name="dashArray">The finite nonnegative dash lengths.</param><param name="dashOffset">The finite signed offset.</param>
    /// <returns>The normalized even pattern or a stable failure.</returns>
    public SvgStrokeDashPatternResult Normalize(System.Collections.Generic.List<double>? dashArray, double dashOffset)
    {
        if (dashArray == null || !SvgFiniteMath.IsFinite(dashOffset)) return new SvgStrokeDashes().Failure();
        System.Collections.Generic.List<double> pattern = new System.Collections.Generic.List<double>();
        double authoredTotal = 0;
        int index = 0;
        while (index < dashArray.Count)
        {
            double value = dashArray[index];
            if (!SvgFiniteMath.IsFinite(value) || value < 0) return new SvgStrokeDashes().Failure();
            authoredTotal = authoredTotal + value;
            if (!SvgFiniteMath.IsFinite(authoredTotal)) return new SvgStrokeDashes().Failure();
            pattern.Add(SvgFiniteMath.NormalizeZero(value));
            index = index + 1;
        }
        if (pattern.Count == 0 || authoredTotal == 0) return new SvgStrokeDashPatternResult(true, SvgStrokeError.None, new System.Collections.Generic.List<double>(), 0, 0, true);
        if (pattern.Count % 2 != 0)
        {
            int count = pattern.Count;
            index = 0;
            while (index < count) { pattern.Add(pattern[index]); index = index + 1; }
        }
        double total = pattern.Count == dashArray.Count ? authoredTotal : authoredTotal * 2;
        if (!SvgFiniteMath.IsFinite(total)) return new SvgStrokeDashes().Failure();
        double offset = dashOffset % total;
        if (offset < 0) offset = offset + total;
        return new SvgStrokeDashPatternResult(true, SvgStrokeError.None, pattern, SvgFiniteMath.NormalizeZero(offset), total, false);
    }

    internal SvgStrokeDashPatternResult Failure()
    {
        return new SvgStrokeDashPatternResult(false, SvgStrokeError.InvalidArgument, null, 0, 0, false);
    }
}
