namespace svg;

/// <summary>Contains viewport, font, root-font, and object-bounding-box length inputs.</summary>
public class SvgLengthContext
{
    /// <summary>Indicates whether viewport dimensions are available.</summary>
    public bool HasViewport;

    /// <summary>The viewport width in user units.</summary>
    public double ViewportWidth;

    /// <summary>The viewport height in user units.</summary>
    public double ViewportHeight;

    /// <summary>Indicates whether the current font size is available.</summary>
    public bool HasFontSize;

    /// <summary>The current font size in user units.</summary>
    public double FontSize;

    /// <summary>Indicates whether the current font x-height is available.</summary>
    public bool HasXHeight;

    /// <summary>The current font x-height in user units.</summary>
    public double XHeight;

    /// <summary>Indicates whether the current font zero-glyph advance is available.</summary>
    public bool HasZeroAdvance;

    /// <summary>The current font zero-glyph advance in user units.</summary>
    public double ZeroAdvance;

    /// <summary>Indicates whether the root font size is available.</summary>
    public bool HasRootFontSize;

    /// <summary>The root font size in user units.</summary>
    public double RootFontSize;

    /// <summary>Indicates whether object-bounding-box dimensions are available.</summary>
    public bool HasObjectBoundingBox;

    /// <summary>The object bounding-box width in user units.</summary>
    public double ObjectBoundingBoxWidth;

    /// <summary>The object bounding-box height in user units.</summary>
    public double ObjectBoundingBoxHeight;

    /// <summary>Creates an evaluation context with every optional input unavailable.</summary>
    public SvgLengthContext()
    {
        HasViewport = false;
        ViewportWidth = 0;
        ViewportHeight = 0;
        HasFontSize = false;
        FontSize = 0;
        HasXHeight = false;
        XHeight = 0;
        HasZeroAdvance = false;
        ZeroAdvance = 0;
        HasRootFontSize = false;
        RootFontSize = 0;
        HasObjectBoundingBox = false;
        ObjectBoundingBoxWidth = 0;
        ObjectBoundingBoxHeight = 0;
    }
}

/// <summary>Contains a non-throwing typed-length evaluation result.</summary>
public class SvgLengthEvaluationResult
{
    /// <summary>Indicates whether evaluation produced a finite user-unit value.</summary>
    public bool Success;

    /// <summary>The evaluation failure kind.</summary>
    public SvgLengthEvaluationError Error;

    /// <summary>The finite user-unit value, or zero after failure.</summary>
    public double Value;

    /// <summary>Creates a typed-length evaluation result.</summary>
    /// <param name="success">Whether evaluation succeeded.</param>
    /// <param name="error">The evaluation failure kind.</param>
    /// <param name="value">The finite user-unit value, or zero.</param>
    public SvgLengthEvaluationResult(bool success, SvgLengthEvaluationError error, double value)
    {
        Success = success;
        Error = error;
        Value = value;
    }
}

/// <summary>Represents a finite SVG or CSS length, percentage, or unitless user-unit value.</summary>
public class SvgLength
{
    /// <summary>The finite numeric component.</summary>
    public double Value;

    /// <summary>The typed length unit.</summary>
    public SvgLengthUnit Unit;

    /// <summary>Creates a typed finite length.</summary>
    /// <param name="value">The finite numeric component.</param>
    /// <param name="unit">The typed length unit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the value is non-finite or the unit is unknown.</exception>
    public SvgLength(double value, SvgLengthUnit unit)
    {
        if (!SvgFiniteMath.IsFinite(value))
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        if (!SvgLengthUnits.IsKnown(unit))
        {
            throw new System.ArgumentOutOfRangeException("unit");
        }
        Value = SvgFiniteMath.NormalizeZero(value);
        Unit = unit;
    }

    /// <summary>Evaluates this length to finite SVG user units.</summary>
    /// <param name="context">The optional viewport, font, root-font, and bounding-box context.</param>
    /// <param name="percentageBasis">The basis used by percentages and object-bounding-box unitless values.</param>
    /// <returns>A finite user-unit value or a stable failure.</returns>
    internal SvgLengthEvaluationResult Evaluate(SvgLengthContext context, SvgLengthPercentageBasis percentageBasis)
    {
        return SvgLengthEvaluator.Evaluate(this, context, percentageBasis);
    }

    /// <summary>Serializes this length with shortest invariant number formatting and canonical unit text.</summary>
    /// <returns>The typed length text.</returns>
    internal string ToText()
    {
        SvgNumberFormatResult number = SvgNumberFormatter.FormatShortest(Value);
        return number.Text + SvgLengthUnits.GetText(Unit);
    }
}
