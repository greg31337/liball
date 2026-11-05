namespace svg;

/// <summary>Represents one typed CSS math expression node.</summary>
public class SvgCalcExpression
{
    /// <summary>The expression node kind.</summary>
    public SvgCalcExpressionKind Kind;
    /// <summary>The validated result dimension.</summary>
    public SvgCalcDimension Dimension;
    /// <summary>The finite numeric component for a value node.</summary>
    public double Value;
    /// <summary>The exact unit suffix for a value node.</summary>
    public string Unit;
    /// <summary>The ordered child expressions.</summary>
    public System.Collections.Generic.List<SvgCalcExpression> Children;
    /// <summary>The starting offset in substituted text.</summary>
    public int StartOffset;
    /// <summary>The exclusive ending offset in substituted text.</summary>
    public int EndOffset;
    /// <summary>Whether the mutable expression state was valid when created.</summary>
    public bool IsValid;

    /// <summary>Creates a typed CSS math node and copies its children.</summary>
    /// <param name="kind">The expression kind.</param>
    /// <param name="dimension">The result dimension.</param>
    /// <param name="value">The leaf numeric component.</param>
    /// <param name="unit">The leaf unit text.</param>
    /// <param name="children">The child nodes.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The ending offset.</param>
    public SvgCalcExpression(SvgCalcExpressionKind kind, SvgCalcDimension dimension, double value, string unit, System.Collections.Generic.List<SvgCalcExpression> children, int startOffset, int endOffset)
    {
        if (!SvgFiniteMath.IsFinite(value) || unit == null || children == null || startOffset < 0 || endOffset <= startOffset) throw new System.ArgumentOutOfRangeException("kind");
        Kind = kind;
        Dimension = dimension;
        Value = SvgFiniteMath.NormalizeZero(value);
        Unit = unit;
        Children = new System.Collections.Generic.List<SvgCalcExpression>(children);
        StartOffset = startOffset;
        EndOffset = endOffset;
        IsValid = true;
    }
}

/// <summary>Contains a substituted and dimensionally validated CSS math parse result.</summary>
public class SvgCalcParseResult
{
    /// <summary>Whether parsing and dimensional validation succeeded.</summary>
    public bool Success;
    /// <summary>The stable math failure kind.</summary>
    public SvgCalcParseError Error;
    /// <summary>The variable failure kind, when applicable.</summary>
    public SvgCssVariableError VariableError;
    /// <summary>The parsed expression tree.</summary>
    public SvgCalcExpression? Expression;
    /// <summary>The text after custom-property substitution.</summary>
    public string ResolvedText;
    /// <summary>The referenced custom-property names.</summary>
    public System.Collections.Generic.List<string> Dependencies;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a CSS math parse result and copies dependencies.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The math failure kind.</param>
    /// <param name="variableError">The variable failure kind.</param>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="resolvedText">The substituted text.</param>
    /// <param name="dependencies">The referenced names.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgCalcParseResult(bool success, SvgCalcParseError error, SvgCssVariableError variableError, SvgCalcExpression? expression, string resolvedText, System.Collections.Generic.List<string> dependencies, int errorOffset)
    {
        Success = success;
        Error = error;
        VariableError = variableError;
        Expression = expression;
        ResolvedText = resolvedText;
        Dependencies = new System.Collections.Generic.List<string>(dependencies);
        ErrorOffset = errorOffset;
    }
}
