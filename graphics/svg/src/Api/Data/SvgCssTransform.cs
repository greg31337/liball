namespace svg;

/// <summary>Contains one optional finite transform reference box.</summary>
public class SvgCssTransformReferenceBox
{
    /// <summary>Whether the box coordinates and dimensions are available.</summary>
    public bool Available;
    /// <summary>The box x coordinate.</summary>
    public double X;
    /// <summary>The box y coordinate.</summary>
    public double Y;
    /// <summary>The nonnegative box width.</summary>
    public double Width;
    /// <summary>The nonnegative box height.</summary>
    public double Height;

    /// <summary>Creates an optional transform reference box.</summary>
    /// <param name="available">Whether the box is available.</param>
    /// <param name="x">The finite x coordinate.</param>
    /// <param name="y">The finite y coordinate.</param>
    /// <param name="width">The finite nonnegative width.</param>
    /// <param name="height">The finite nonnegative height.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for invalid available box values.</exception>
    public SvgCssTransformReferenceBox(bool available, double x, double y, double width, double height)
    {
        if (available && (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y) || !SvgFiniteMath.IsFinite(width) || !SvgFiniteMath.IsFinite(height) || width < 0 || height < 0))
        {
            throw new System.ArgumentOutOfRangeException("width");
        }
        Available = available;
        X = SvgFiniteMath.NormalizeZero(x);
        Y = SvgFiniteMath.NormalizeZero(y);
        Width = SvgFiniteMath.NormalizeZero(width);
        Height = SvgFiniteMath.NormalizeZero(height);
    }
}

/// <summary>Contains length inputs and every transform-box reference candidate.</summary>
public class SvgCssTransformContext
{
    /// <summary>The optional viewport and font length context.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The optional content box.</summary>
    public SvgCssTransformReferenceBox ContentBox;
    /// <summary>The optional border box.</summary>
    public SvgCssTransformReferenceBox BorderBox;
    /// <summary>The optional fill box.</summary>
    public SvgCssTransformReferenceBox FillBox;
    /// <summary>The optional stroke box.</summary>
    public SvgCssTransformReferenceBox StrokeBox;
    /// <summary>The optional view box.</summary>
    public SvgCssTransformReferenceBox ViewBox;

    /// <summary>Creates a context with every reference box unavailable.</summary>
    public SvgCssTransformContext()
    {
        LengthContext = new SvgLengthContext();
        ContentBox = new SvgCssTransformReferenceBox(false, 0, 0, 0, 0);
        BorderBox = new SvgCssTransformReferenceBox(false, 0, 0, 0, 0);
        FillBox = new SvgCssTransformReferenceBox(false, 0, 0, 0, 0);
        StrokeBox = new SvgCssTransformReferenceBox(false, 0, 0, 0, 0);
        ViewBox = new SvgCssTransformReferenceBox(false, 0, 0, 0, 0);
    }
}

/// <summary>Represents one typed two-dimensional CSS transform function.</summary>
public class SvgCssTransform
{
    /// <summary>The CSS transform function kind.</summary>
    public SvgCssTransformKind Kind;
    /// <summary>The finite unitless numeric arguments.</summary>
    public double[] Numbers;
    /// <summary>The typed length-percentage arguments.</summary>
    public SvgLength[] Lengths;
    /// <summary>The typed angle arguments.</summary>
    public SvgAngle[] Angles;

    /// <summary>Creates a validated CSS transform and copies its argument arrays.</summary>
    /// <param name="kind">The function kind.</param>
    /// <param name="numbers">The unitless numeric arguments.</param>
    /// <param name="lengths">The length-percentage arguments.</param>
    /// <param name="angles">The angle arguments.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for invalid kind, arity, null arrays, null typed values, or non-finite numbers.</exception>
    public SvgCssTransform(SvgCssTransformKind kind, double[] numbers, SvgLength[] lengths, SvgAngle[] angles)
    {
        if (!SvgCssTransformRules.ValidArguments(kind, numbers, lengths, angles))
        {
            throw new System.ArgumentOutOfRangeException("numbers");
        }
        Kind = kind;
        Numbers = SvgCssTransformRules.CopyNumbers(numbers);
        Lengths = SvgCssTransformRules.CopyLengths(lengths);
        Angles = SvgCssTransformRules.CopyAngles(angles);
    }

    /// <summary>Serializes the function using canonical CSS spelling and shortest numbers.</summary>
    /// <returns>Canonical CSS text, or an empty string for invalid mutated state.</returns>
    internal string ToText()
    {
        return SvgCssTransformFormatter.Format(this);
    }
}

/// <summary>Represents a CSS transform list or the none keyword.</summary>
public class SvgCssTransformList
{
    /// <summary>Whether the value is the none keyword.</summary>
    public bool IsNone;
    /// <summary>The ordered CSS transform functions.</summary>
    public System.Collections.Generic.List<SvgCssTransform> Items;

    /// <summary>Creates a CSS transform list and copies its collection.</summary>
    /// <param name="isNone">Whether the value is none.</param>
    /// <param name="items">The ordered transform functions.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for a missing/null item or functions accompanying none.</exception>
    public SvgCssTransformList(bool isNone, System.Collections.Generic.List<SvgCssTransform> items)
    {
        if (items == null || isNone && items.Count != 0)
        {
            throw new System.ArgumentOutOfRangeException("items");
        }
        Items = new System.Collections.Generic.List<SvgCssTransform>();
        int index = 0;
        while (index < items.Count)
        {
            if (items[index] == null)
            {
                throw new System.ArgumentOutOfRangeException("items");
            }
            Items.Add(items[index]);
            index = index + 1;
        }
        IsNone = isNone;
    }

    /// <summary>Serializes the CSS transform list with canonical function spacing.</summary>
    /// <returns>The canonical list text, or an empty string for invalid mutated state.</returns>
    internal string ToText()
    {
        return SvgCssTransformFormatter.FormatList(this);
    }

    /// <summary>Composes the transform list around a resolved transform origin.</summary>
    /// <param name="context">The explicit length and reference-box context.</param>
    /// <param name="origin">The typed transform origin.</param>
    /// <param name="transformBox">The selected reference box.</param>
    /// <returns>A finite composed matrix or stable failure.</returns>
    internal SvgCssTransformMatrixResult ToMatrix(SvgCssTransformContext context, SvgTransformOrigin origin, SvgTransformBox transformBox)
    {
        return SvgCssTransformMatrixBuilder.Compose(this, context, origin, transformBox);
    }
}

/// <summary>Represents a typed CSS transform-origin value.</summary>
public class SvgTransformOrigin
{
    /// <summary>The horizontal length-percentage position.</summary>
    public SvgLength? X;
    /// <summary>The vertical length-percentage position.</summary>
    public SvgLength? Y;
    /// <summary>The optional depth length.</summary>
    public SvgLength? Z;
    /// <summary>Whether a depth component was authored.</summary>
    public bool HasZ;

    /// <summary>Creates a validated typed transform origin.</summary>
    /// <param name="x">The horizontal length-percentage.</param>
    /// <param name="y">The vertical length-percentage.</param>
    /// <param name="z">The optional non-percentage depth length.</param>
    /// <param name="hasZ">Whether the depth component is present.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for missing or invalid component kinds.</exception>
    public SvgTransformOrigin(SvgLength x, SvgLength y, SvgLength? z, bool hasZ)
    {
        if (!SvgCssTransformRules.ValidPositionLength(x) || !SvgCssTransformRules.ValidPositionLength(y) || hasZ && (z == null || !SvgCssTransformRules.ValidDepthLength(z)) || !hasZ && z != null)
        {
            throw new System.ArgumentOutOfRangeException("x");
        }
        X = x;
        Y = y;
        Z = z;
        HasZ = hasZ;
    }

    /// <summary>Serializes the origin with canonical typed components.</summary>
    /// <returns>Canonical transform-origin text, or an empty string for invalid mutated state.</returns>
    internal string ToText()
    {
        if (!SvgCssTransformRules.ValidPositionLength(X) || !SvgCssTransformRules.ValidPositionLength(Y) || HasZ && !SvgCssTransformRules.ValidDepthLength(Z) || !HasZ && Z != null)
        {
            return "";
        }
        string text = (X ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText() + " " + (Y ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText();
        if (HasZ)
        {
            text = text + " " + (Z ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText();
        }
        return text;
    }
}

/// <summary>Contains a non-throwing CSS transform matrix result.</summary>
public class SvgCssTransformMatrixResult
{
    /// <summary>Indicates whether matrix evaluation succeeded.</summary>
    public bool Success;
    /// <summary>The evaluation failure kind.</summary>
    public SvgCssTransformError Error;
    /// <summary>The finite matrix, or <c>null</c> after failure.</summary>
    public SvgMatrix? Matrix;

    /// <summary>Creates a CSS transform matrix result.</summary>
    /// <param name="success">Whether evaluation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="matrix">The finite matrix, or <c>null</c>.</param>
    public SvgCssTransformMatrixResult(bool success, SvgCssTransformError error, SvgMatrix? matrix)
    {
        Success = success;
        Error = error;
        Matrix = matrix;
    }
}
