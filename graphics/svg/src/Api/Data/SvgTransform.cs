namespace svg;

/// <summary>Contains a non-throwing transform matrix construction result.</summary>
public class SvgTransformMatrixResult
{
    /// <summary>Indicates whether a finite matrix was constructed.</summary>
    public bool Success;

    /// <summary>The matrix construction failure kind.</summary>
    public SvgTransformMatrixError Error;

    /// <summary>The constructed matrix, or <c>null</c> after failure.</summary>
    public SvgMatrix? Matrix;

    /// <summary>Creates a transform matrix result.</summary>
    /// <param name="success">Whether construction succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="matrix">The finite matrix, or <c>null</c>.</param>
    public SvgTransformMatrixResult(bool success, SvgTransformMatrixError error, SvgMatrix? matrix)
    {
        Success = success;
        Error = error;
        Matrix = matrix;
    }
}

/// <summary>Represents one validated SVG transform function and its authored parameters.</summary>
public class SvgTransform
{
    /// <summary>The transform function kind.</summary>
    public SvgTransformKind Kind;

    /// <summary>The finite parameters in grammar order.</summary>
    public double[] Values;

    /// <summary>Creates a validated SVG transform and copies its parameters.</summary>
    /// <param name="kind">The transform function kind.</param>
    /// <param name="values">The finite parameters in grammar order.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for an unknown kind, invalid arity, or non-finite parameter.</exception>
    public SvgTransform(SvgTransformKind kind, double[] values)
    {
        if (!SvgTransformRules.IsKind(kind) || values == null || !SvgTransformRules.IsParameterCount(kind, values.Length))
        {
            throw new System.ArgumentOutOfRangeException("values");
        }
        Values = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (!SvgFiniteMath.IsFinite(values[index]))
            {
                throw new System.ArgumentOutOfRangeException("values");
            }
            Values[index] = SvgFiniteMath.NormalizeZero(values[index]);
            index = index + 1;
        }
        Kind = kind;
    }

    /// <summary>Constructs the finite affine matrix for this transform.</summary>
    /// <returns>A finite matrix or a stable failure.</returns>
    internal SvgTransformMatrixResult ToMatrix()
    {
        return SvgTransformMatrixBuilder.Build(this);
    }

    /// <summary>Serializes this transform with canonical names, spacing, and shortest numbers.</summary>
    /// <returns>The canonical transform function text, or an empty string for invalid mutated state.</returns>
    internal string ToText()
    {
        return SvgTransformFormatter.Format(this);
    }
}

/// <summary>Represents an ordered SVG transform list.</summary>
public class SvgTransformList
{
    /// <summary>The ordered transform functions.</summary>
    public System.Collections.Generic.List<SvgTransform> Items;

    /// <summary>Creates a transform list and copies its collection.</summary>
    /// <param name="items">The ordered transform functions.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for a missing collection or null item.</exception>
    public SvgTransformList(System.Collections.Generic.List<SvgTransform> items)
    {
        if (items == null)
        {
            throw new System.ArgumentOutOfRangeException("items");
        }
        Items = new System.Collections.Generic.List<SvgTransform>();
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
    }

    /// <summary>Composes this list into one finite affine matrix in SVG transform order.</summary>
    /// <returns>The composed matrix or a stable failure.</returns>
    internal SvgTransformMatrixResult ToMatrix()
    {
        return SvgTransformMatrixBuilder.Compose(this);
    }

    /// <summary>Serializes this list with one space between canonical transform functions.</summary>
    /// <returns>The canonical transform-list text, or an empty string for invalid mutated state.</returns>
    internal string ToText()
    {
        return SvgTransformFormatter.FormatList(this);
    }
}
