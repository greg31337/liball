namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPolynomialRoots"/>.</summary>
public interface ISvgPolynomialRoots
{
    /// <summary>Solves <c>a*x + b = 0</c> for its isolated real root.</summary>
    /// <param name="a">The linear coefficient.</param><param name="b">The constant coefficient.</param>
    /// <returns>The finite isolated roots; an indeterminate equation has none.</returns>
    SvgPolynomialRootResult SolveLinear(double a, double b);
    /// <summary>Solves <c>a*x*x + b*x + c = 0</c> for distinct real roots.</summary>
    /// <param name="a">The quadratic coefficient.</param><param name="b">The linear coefficient.</param><param name="c">The constant coefficient.</param>
    /// <returns>The ascending finite real roots or a stable failure.</returns>
    SvgPolynomialRootResult SolveQuadratic(double a, double b, double c);
}

/// <summary>Defines the public operations exposed by <see cref="SvgCurveGeometry"/>.</summary>
public interface ISvgCurveGeometry
{
    /// <summary>Evaluates a line.</summary><param name="line">The line.</param><param name="amount">The unit parameter.</param><returns>The point or failure.</returns>
    SvgPointOperationResult EvaluateWithLineAndAmount(SvgLineSegment line, double amount);
    /// <summary>Evaluates a quadratic.</summary><param name="quadratic">The quadratic.</param><param name="amount">The unit parameter.</param><returns>The point or failure.</returns>
    SvgPointOperationResult EvaluateWithQuadraticAndAmount(SvgQuadraticBezier quadratic, double amount);
    /// <summary>Evaluates a cubic.</summary><param name="cubic">The cubic.</param><param name="amount">The unit parameter.</param><returns>The point or failure.</returns>
    SvgPointOperationResult EvaluateWithCubicAndAmount(SvgCubicBezier cubic, double amount);
    /// <summary>Evaluates a line derivative.</summary><param name="line">The line.</param><param name="amount">The unit parameter.</param><returns>The vector or failure.</returns>
    SvgVectorOperationResult DerivativeWithLineAndAmount(SvgLineSegment line, double amount);
    /// <summary>Evaluates a quadratic derivative.</summary><param name="quadratic">The quadratic.</param><param name="amount">The unit parameter.</param><returns>The vector or failure.</returns>
    SvgVectorOperationResult DerivativeWithQuadraticAndAmount(SvgQuadraticBezier quadratic, double amount);
    /// <summary>Evaluates a cubic derivative.</summary><param name="cubic">The cubic.</param><param name="amount">The unit parameter.</param><returns>The vector or failure.</returns>
    SvgVectorOperationResult DerivativeWithCubicAndAmount(SvgCubicBezier cubic, double amount);
    /// <summary>Evaluates an arc derivative.</summary><param name="arc">The arc.</param><param name="amount">The unit parameter.</param><returns>The vector or failure.</returns>
    SvgVectorOperationResult DerivativeWithArcAndAmount(SvgArc arc, double amount);
    /// <summary>Splits a line.</summary><param name="line">The line.</param><param name="amount">The unit parameter.</param><returns>The portions or failure.</returns>
    SvgLineSplitResult SplitWithLineAndAmount(SvgLineSegment line, double amount);
    /// <summary>Splits a quadratic.</summary><param name="quadratic">The quadratic.</param><param name="amount">The unit parameter.</param><returns>The portions or failure.</returns>
    SvgQuadraticSplitResult SplitWithQuadraticAndAmount(SvgQuadraticBezier quadratic, double amount);
    /// <summary>Splits a cubic.</summary><param name="cubic">The cubic.</param><param name="amount">The unit parameter.</param><returns>The portions or failure.</returns>
    SvgCubicSplitResult SplitWithCubicAndAmount(SvgCubicBezier cubic, double amount);
    /// <summary>Reverses a line.</summary><param name="line">The line.</param><returns>The reversed line or failure.</returns>
    SvgLineOperationResult ReverseWithLine(SvgLineSegment line);
    /// <summary>Reverses a quadratic.</summary><param name="quadratic">The quadratic.</param><returns>The reversed quadratic or failure.</returns>
    SvgQuadraticOperationResult ReverseWithQuadratic(SvgQuadraticBezier quadratic);
    /// <summary>Reverses a cubic.</summary><param name="cubic">The cubic.</param><returns>The reversed cubic or failure.</returns>
    SvgCubicOperationResult ReverseWithCubic(SvgCubicBezier cubic);
    /// <summary>Reverses an arc.</summary><param name="arc">The arc.</param><returns>The reversed arc or failure.</returns>
    SvgArcOperationResult ReverseWithArc(SvgArc arc);
    /// <summary>Computes tight line bounds.</summary><param name="line">The line.</param><returns>The bounds or failure.</returns>
    SvgRectOperationResult GetBoundsWithLine(SvgLineSegment line);
    /// <summary>Computes tight quadratic bounds.</summary><param name="quadratic">The quadratic.</param><returns>The bounds or failure.</returns>
    SvgRectOperationResult GetBoundsWithQuadratic(SvgQuadraticBezier quadratic);
    /// <summary>Computes tight cubic bounds.</summary><param name="cubic">The cubic.</param><returns>The bounds or failure.</returns>
    SvgRectOperationResult GetBoundsWithCubic(SvgCubicBezier cubic);
    /// <summary>Computes tight arc bounds.</summary><param name="arc">The arc.</param><returns>The bounds or failure.</returns>
    SvgRectOperationResult GetBoundsWithArc(SvgArc arc);
}
