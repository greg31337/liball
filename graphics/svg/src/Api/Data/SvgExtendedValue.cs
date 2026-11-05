namespace svg;

/// <summary>Configures bounded parsing for every extended standalone value grammar.</summary>
public class SvgExtendedValueParseOptions
{
    /// <summary>The maximum list, transform, fallback, or function count.</summary>
    public int MaxItems;
    /// <summary>The maximum number, dimension, keyword, URL, or function token length.</summary>
    public int MaxTokenCharacters;
    /// <summary>The maximum CSS math nesting and substitution depth.</summary>
    public int MaxDepth;
    /// <summary>The maximum CSS math expression-node count.</summary>
    public int MaxNodes;
    /// <summary>Whether an SVG length, angle, or time may omit its unit.</summary>
    public bool AllowUnitless;
    /// <summary>Whether length tokens use CSS rather than SVG number-token boundaries.</summary>
    public bool UseCssNumberGrammar;
    /// <summary>Whether a length may use a percentage.</summary>
    public bool AllowPercentage;
    /// <summary>Whether malformed CSS transform functions or paint fallbacks may be skipped.</summary>
    public bool RecoverInvalidItems;
    /// <summary>The required CSS math result dimension.</summary>
    public SvgCalcDimension ExpectedCalcDimension;
    /// <summary>The optional custom properties used before CSS math parsing.</summary>
    public SvgCustomPropertySet? CustomProperties;

    /// <summary>Creates secure, SVG-compatible standalone parsing defaults.</summary>
    public SvgExtendedValueParseOptions()
    {
        MaxItems = 1024;
        MaxTokenCharacters = 128;
        MaxDepth = 32;
        MaxNodes = 4096;
        AllowUnitless = true;
        UseCssNumberGrammar = false;
        AllowPercentage = true;
        RecoverInvalidItems = false;
        ExpectedCalcDimension = SvgCalcDimension.Any;
        CustomProperties = null;
    }
}

/// <summary>Stores one tagged extended standalone SVG or CSS value.</summary>
public class SvgExtendedValue
{
    /// <summary>The active value grammar.</summary>
    public SvgExtendedValueKind Kind;
    /// <summary>The typed length payload.</summary>
    public SvgLength? Length;
    /// <summary>The typed angle payload.</summary>
    public SvgAngle? Angle;
    /// <summary>The typed time payload.</summary>
    public SvgTime? Time;
    /// <summary>The typed frequency payload.</summary>
    public SvgFrequency? Frequency;
    /// <summary>The typed resolution payload.</summary>
    public SvgResolution? Resolution;
    /// <summary>The ratio payload.</summary>
    public SvgRatio? Ratio;
    /// <summary>The pathLength payload.</summary>
    public SvgPathLength? PathLength;
    /// <summary>The viewBox payload.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The preserveAspectRatio payload.</summary>
    public SvgAspectRatio? AspectRatio;
    /// <summary>The SVG transform-list payload.</summary>
    public SvgTransformList? SvgTransform;
    /// <summary>The CSS transform-list payload.</summary>
    public SvgCssTransformList? CssTransform;
    /// <summary>The transform-origin payload.</summary>
    public SvgTransformOrigin? TransformOrigin;
    /// <summary>The transform-box payload.</summary>
    public SvgTransformBox TransformBox;
    /// <summary>The color payload.</summary>
    public SvgColor? Color;
    /// <summary>The paint payload.</summary>
    public SvgPaint? Paint;
    /// <summary>The CSS math payload.</summary>
    public SvgCalcExpression? CalcExpression;
    /// <summary>The length-list parse payload.</summary>
    public SvgLengthListParseResult? LengthList;
    /// <summary>The rotate-list parse payload.</summary>
    public SvgRotateListParseResult? RotateList;
    /// <summary>The font-family-list parse payload.</summary>
    public SvgFontFamilyParseResult? FontFamilyList;
    /// <summary>The paint-order parse payload.</summary>
    public SvgPaintOrderParseResult? PaintOrder;

    internal SvgExtendedValue(SvgExtendedValueKind kind)
    {
        Kind = kind;
        Length = null; Angle = null; Time = null; Frequency = null; Resolution = null; Ratio = null; PathLength = null;
        ViewBox = null; AspectRatio = null; SvgTransform = null; CssTransform = null; TransformOrigin = null; TransformBox = SvgTransformBox.ViewBox;
        Color = null; Paint = null; CalcExpression = null; LengthList = null; RotateList = null; FontFamilyList = null; PaintOrder = null;
    }
}

/// <summary>Contains a complete or recovered extended standalone value parse result.</summary>
public class SvgExtendedValueParseResult
{
    /// <summary>Whether a usable complete or recovered value is available.</summary>
    public bool Success;
    /// <summary>Whether invalid syntax was skipped by a compatible parser.</summary>
    public bool Recovered;
    /// <summary>The stable shared failure category.</summary>
    public SvgExtendedValueError Error;
    /// <summary>The parsed typed value, or <c>null</c>.</summary>
    public SvgExtendedValue? Value;
    /// <summary>The exact first failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates an extended typed parse result.</summary>
    /// <param name="success">Whether a usable value is available.</param><param name="recovered">Whether invalid syntax was skipped.</param><param name="error">The shared failure category.</param><param name="value">The parsed value.</param><param name="errorOffset">The exact failure offset.</param>
    public SvgExtendedValueParseResult(bool success, bool recovered, SvgExtendedValueError error, SvgExtendedValue? value, int errorOffset)
    {
        Success = success; Recovered = recovered; Error = error; Value = value; ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a typed extended-value interpolation result.</summary>
public class SvgExtendedValueOperationResult
{
    /// <summary>Whether interpolation succeeded.</summary>
    public bool Success;
    /// <summary>The stable interpolation failure.</summary>
    public SvgAnimationInterpolationError Error;
    /// <summary>The interpolated typed value, or <c>null</c>.</summary>
    public SvgExtendedValue? Value;

    /// <summary>Creates an extended interpolation result.</summary>
    /// <param name="success">Whether interpolation succeeded.</param><param name="error">The failure.</param><param name="value">The interpolated value.</param>
    public SvgExtendedValueOperationResult(bool success, SvgAnimationInterpolationError error, SvgExtendedValue? value)
    {
        Success = success; Error = error; Value = value;
    }
}

/// <summary>Contains a canonical-dimension CSS math evaluation result.</summary>
public class SvgCalcEvaluationResult
{
    /// <summary>Whether evaluation produced a finite value.</summary>
    public bool Success;
    /// <summary>The stable evaluation failure.</summary>
    public SvgCalcEvaluationError Error;
    /// <summary>The evaluated result dimension.</summary>
    public SvgCalcDimension Dimension;
    /// <summary>The finite canonical value, or zero after failure.</summary>
    public double Value;

    /// <summary>Creates a CSS math evaluation result.</summary>
    /// <param name="success">Whether evaluation succeeded.</param><param name="error">The failure.</param><param name="dimension">The evaluated dimension.</param><param name="value">The finite result.</param>
    public SvgCalcEvaluationResult(bool success, SvgCalcEvaluationError error, SvgCalcDimension dimension, double value)
    {
        Success = success; Error = error; Dimension = dimension; Value = value;
    }
}
