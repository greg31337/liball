namespace svg;

/// <summary>Stores one tagged typed value used by declarative animation.</summary>
public class SvgAnimationValue
{
    /// <summary>The active value family.</summary>
    public SvgAnimationValueKind Kind;
    /// <summary>The scalar number payload.</summary>
    public double Number;
    /// <summary>The typed length payload.</summary>
    public SvgLength? Length;
    /// <summary>The color payload.</summary>
    public SvgColor? Color;
    /// <summary>The normalized path payload.</summary>
    public SvgPathData? Path;
    /// <summary>The ordered point payload.</summary>
    public System.Collections.Generic.List<SvgPoint> Points;
    /// <summary>The transform-list payload.</summary>
    public SvgTransformList? Transform;
    /// <summary>The view-box payload.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The paint payload.</summary>
    public SvgPaint? Paint;
    /// <summary>The string payload.</summary>
    public string Text;

    internal SvgAnimationValue(SvgAnimationValueKind kind)
    {
        Kind = kind;
        Number = 0;
        Length = null;
        Color = null;
        Path = null;
        Points = new System.Collections.Generic.List<SvgPoint>();
        Transform = null;
        ViewBox = null;
        Paint = null;
        Text = "";
    }

    /// <summary>Creates a finite number value.</summary>
    /// <param name="value">The finite scalar.</param>
    /// <returns>The tagged number value.</returns>
    internal static SvgAnimationValue FromNumber(double value)
    {
        if (!SvgFiniteMath.IsFinite(value)) throw new System.ArgumentOutOfRangeException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.Number);
        result.Number = SvgFiniteMath.NormalizeZero(value);
        return result;
    }

    /// <summary>Creates a typed length value.</summary>
    /// <param name="value">The typed length.</param>
    /// <returns>The tagged length value.</returns>
    internal static SvgAnimationValue FromLength(SvgLength value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.Length);
        result.Length = value;
        return result;
    }

    /// <summary>Creates a color value.</summary>
    /// <param name="value">The valid color.</param>
    /// <returns>The tagged color value.</returns>
    internal static SvgAnimationValue FromColor(SvgColor value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.Color);
        result.Color = value;
        return result;
    }

    /// <summary>Creates a path value.</summary>
    /// <param name="value">The valid path.</param>
    /// <returns>The tagged path value.</returns>
    internal static SvgAnimationValue FromPath(SvgPathData value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.Path);
        result.Path = value;
        return result;
    }

    /// <summary>Creates a point-list value and copies its collection.</summary>
    /// <param name="value">The ordered finite points.</param>
    /// <returns>The tagged point-list value.</returns>
    internal static SvgAnimationValue FromPoints(System.Collections.Generic.List<SvgPoint> value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.Points);
        int index = 0;
        while (index < value.Count)
        {
            if (value[index] == null) throw new System.ArgumentOutOfRangeException("value");
            result.Points.Add(value[index]);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Creates a transform-list value.</summary>
    /// <param name="value">The transform list.</param>
    /// <returns>The tagged transform value.</returns>
    internal static SvgAnimationValue FromTransform(SvgTransformList value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.Transform);
        result.Transform = value;
        return result;
    }

    /// <summary>Creates a view-box value.</summary>
    /// <param name="value">The finite view box.</param>
    /// <returns>The tagged view-box value.</returns>
    internal static SvgAnimationValue FromViewBox(SvgViewBox value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.ViewBox);
        result.ViewBox = value;
        return result;
    }

    /// <summary>Creates a paint value.</summary>
    /// <param name="value">The valid paint.</param>
    /// <returns>The tagged paint value.</returns>
    internal static SvgAnimationValue FromPaint(SvgPaint value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.Paint);
        result.Paint = value;
        return result;
    }

    /// <summary>Creates a discrete string value.</summary>
    /// <param name="value">The non-null string.</param>
    /// <returns>The tagged string value.</returns>
    internal static SvgAnimationValue FromString(string value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        SvgAnimationValue result = new SvgAnimationValue(SvgAnimationValueKind.String);
        result.Text = value;
        return result;
    }

    /// <summary>Formats the active payload canonically.</summary>
    /// <returns>The canonical typed text, or an empty string for invalid mutable state.</returns>
    internal string ToText()
    {
        if (Kind == SvgAnimationValueKind.Number) return SvgNumberFormatter.FormatShortest(Number).Text;
        if (Kind == SvgAnimationValueKind.Length) return Length == null ? "" : Length.ToText();
        if (Kind == SvgAnimationValueKind.Color) return Color == null ? "" : Color.ToText();
        if (Kind == SvgAnimationValueKind.Path) return Path == null ? "" : Path.ToText();
        if (Kind == SvgAnimationValueKind.Points) return PointsText(Points);
        if (Kind == SvgAnimationValueKind.Transform) return Transform == null ? "" : Transform.ToText();
        if (Kind == SvgAnimationValueKind.ViewBox) return ViewBox == null ? "" : ViewBox.ToText();
        if (Kind == SvgAnimationValueKind.Paint) return Paint == null ? "" : Paint.ToText();
        if (Kind == SvgAnimationValueKind.String) return Text == null ? "" : Text;
        return "";
    }

    internal static string PointsText(System.Collections.Generic.List<SvgPoint>? points)
    {
        if (points == null) return "";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < points.Count)
        {
            if (points[index] == null) return "";
            if (index != 0) builder.Append(' ');
            builder.Append(SvgNumberFormatter.FormatShortest(points[index].X).Text);
            builder.Append(',');
            builder.Append(SvgNumberFormatter.FormatShortest(points[index].Y).Text);
            index = index + 1;
        }
        return builder.ToString();
    }
}

/// <summary>Configures parsing, conversion, color space, and bounded typed interpolation.</summary>
public class SvgAnimationInterpolationOptions
{
    /// <summary>The length context used when endpoint units differ.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The percentage basis used when resolving endpoint lengths.</summary>
    public SvgLengthPercentageBasis LengthPercentageBasis;
    /// <summary>The color space used for interpolation and distance.</summary>
    public SvgColorSpace ColorSpace;
    /// <summary>The optional cubic Bézier easing used by spline mode.</summary>
    public SvgKeySpline? Spline;
    /// <summary>The maximum path command, point, transform, or paint fallback count.</summary>
    public int MaxItems;
    /// <summary>The maximum numeric, keyword, or function token length.</summary>
    public int MaxTokenCharacters;

    /// <summary>Creates deterministic interpolation defaults.</summary>
    public SvgAnimationInterpolationOptions()
    {
        LengthContext = new SvgLengthContext();
        LengthPercentageBasis = SvgLengthPercentageBasis.None;
        ColorSpace = SvgColorSpace.Srgb;
        Spline = null;
        MaxItems = 1000000;
        MaxTokenCharacters = 4096;
    }
}

/// <summary>Contains a typed animation-value parse result.</summary>
public class SvgAnimationValueParseResult
{
    /// <summary>Whether the complete value parsed successfully.</summary>
    public bool Success;
    /// <summary>The stable parsing failure.</summary>
    public SvgAnimationInterpolationError Error;
    /// <summary>The parsed typed value, or <c>null</c>.</summary>
    public SvgAnimationValue? Value;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates one typed value parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="value">The parsed value, or <c>null</c>.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgAnimationValueParseResult(bool success, SvgAnimationInterpolationError error, SvgAnimationValue? value, int errorOffset)
    {
        Success = success;
        Error = error;
        Value = value;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains one typed interpolation or paced-sequence result.</summary>
public class SvgAnimationInterpolationResult
{
    /// <summary>Whether interpolation succeeded.</summary>
    public bool Success;
    /// <summary>The stable interpolation failure.</summary>
    public SvgAnimationInterpolationError Error;
    /// <summary>The interpolated value, or <c>null</c>.</summary>
    public SvgAnimationValue? Value;
    /// <summary>The effective progress after spline or paced mapping.</summary>
    public double EffectiveProgress;
    /// <summary>The selected zero-based sequence segment.</summary>
    public int SegmentIndex;
    /// <summary>The typed distance between the selected endpoints.</summary>
    public double Distance;

    /// <summary>Creates one typed interpolation result.</summary>
    /// <param name="success">Whether interpolation succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="value">The interpolated value, or <c>null</c>.</param>
    /// <param name="effectiveProgress">The mapped progress.</param>
    /// <param name="segmentIndex">The selected segment.</param>
    /// <param name="distance">The endpoint distance.</param>
    public SvgAnimationInterpolationResult(bool success, SvgAnimationInterpolationError error, SvgAnimationValue? value, double effectiveProgress, int segmentIndex, double distance)
    {
        Success = success;
        Error = error;
        Value = value;
        EffectiveProgress = effectiveProgress;
        SegmentIndex = segmentIndex;
        Distance = distance;
    }
}
