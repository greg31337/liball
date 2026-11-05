namespace svg;

/// <summary>Provides finite algorithms for line, quadratic, cubic, and arc segments.</summary>
public class SvgCurveGeometry : ISvgCurveGeometry
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCurveGeometry()
    {
    }

    /// <summary>Evaluates a line.</summary><param name="line">The line.</param><param name="amount">The unit parameter.</param><returns>The point or failure.</returns>
    public SvgPointOperationResult EvaluateWithLineAndAmount(SvgLineSegment line, double amount) { return SvgCurveMath.Evaluate(line, amount); }
    /// <summary>Evaluates a quadratic.</summary><param name="quadratic">The quadratic.</param><param name="amount">The unit parameter.</param><returns>The point or failure.</returns>
    public SvgPointOperationResult EvaluateWithQuadraticAndAmount(SvgQuadraticBezier quadratic, double amount) { return SvgCurveMath.Evaluate(quadratic, amount); }
    /// <summary>Evaluates a cubic.</summary><param name="cubic">The cubic.</param><param name="amount">The unit parameter.</param><returns>The point or failure.</returns>
    public SvgPointOperationResult EvaluateWithCubicAndAmount(SvgCubicBezier cubic, double amount) { return SvgCurveMath.Evaluate(cubic, amount); }
    /// <summary>Evaluates a line derivative.</summary><param name="line">The line.</param><param name="amount">The unit parameter.</param><returns>The vector or failure.</returns>
    public SvgVectorOperationResult DerivativeWithLineAndAmount(SvgLineSegment line, double amount) { return SvgCurveMath.Derivative(line, amount); }
    /// <summary>Evaluates a quadratic derivative.</summary><param name="quadratic">The quadratic.</param><param name="amount">The unit parameter.</param><returns>The vector or failure.</returns>
    public SvgVectorOperationResult DerivativeWithQuadraticAndAmount(SvgQuadraticBezier quadratic, double amount) { return SvgCurveMath.Derivative(quadratic, amount); }
    /// <summary>Evaluates a cubic derivative.</summary><param name="cubic">The cubic.</param><param name="amount">The unit parameter.</param><returns>The vector or failure.</returns>
    public SvgVectorOperationResult DerivativeWithCubicAndAmount(SvgCubicBezier cubic, double amount) { return SvgCurveMath.Derivative(cubic, amount); }
    /// <summary>Evaluates an arc derivative.</summary><param name="arc">The arc.</param><param name="amount">The unit parameter.</param><returns>The vector or failure.</returns>
    public SvgVectorOperationResult DerivativeWithArcAndAmount(SvgArc arc, double amount) { return SvgCurveMath.Derivative(arc, amount); }
    /// <summary>Splits a line.</summary><param name="line">The line.</param><param name="amount">The unit parameter.</param><returns>The portions or failure.</returns>
    public SvgLineSplitResult SplitWithLineAndAmount(SvgLineSegment line, double amount) { return SvgCurveMath.Split(line, amount); }
    /// <summary>Splits a quadratic.</summary><param name="quadratic">The quadratic.</param><param name="amount">The unit parameter.</param><returns>The portions or failure.</returns>
    public SvgQuadraticSplitResult SplitWithQuadraticAndAmount(SvgQuadraticBezier quadratic, double amount) { return SvgCurveMath.Split(quadratic, amount); }
    /// <summary>Splits a cubic.</summary><param name="cubic">The cubic.</param><param name="amount">The unit parameter.</param><returns>The portions or failure.</returns>
    public SvgCubicSplitResult SplitWithCubicAndAmount(SvgCubicBezier cubic, double amount) { return SvgCurveMath.Split(cubic, amount); }
    /// <summary>Reverses a line.</summary><param name="line">The line.</param><returns>The reversed line or failure.</returns>
    public SvgLineOperationResult ReverseWithLine(SvgLineSegment line) { return SvgCurveMath.Reverse(line); }
    /// <summary>Reverses a quadratic.</summary><param name="quadratic">The quadratic.</param><returns>The reversed quadratic or failure.</returns>
    public SvgQuadraticOperationResult ReverseWithQuadratic(SvgQuadraticBezier quadratic) { return SvgCurveMath.Reverse(quadratic); }
    /// <summary>Reverses a cubic.</summary><param name="cubic">The cubic.</param><returns>The reversed cubic or failure.</returns>
    public SvgCubicOperationResult ReverseWithCubic(SvgCubicBezier cubic) { return SvgCurveMath.Reverse(cubic); }
    /// <summary>Reverses an arc.</summary><param name="arc">The arc.</param><returns>The reversed arc or failure.</returns>
    public SvgArcOperationResult ReverseWithArc(SvgArc arc) { return SvgCurveMath.Reverse(arc); }
    /// <summary>Computes tight line bounds.</summary><param name="line">The line.</param><returns>The bounds or failure.</returns>
    public SvgRectOperationResult GetBoundsWithLine(SvgLineSegment line) { return SvgCurveMath.GetBounds(line); }
    /// <summary>Computes tight quadratic bounds.</summary><param name="quadratic">The quadratic.</param><returns>The bounds or failure.</returns>
    public SvgRectOperationResult GetBoundsWithQuadratic(SvgQuadraticBezier quadratic) { return SvgCurveMath.GetBounds(quadratic); }
    /// <summary>Computes tight cubic bounds.</summary><param name="cubic">The cubic.</param><returns>The bounds or failure.</returns>
    public SvgRectOperationResult GetBoundsWithCubic(SvgCubicBezier cubic) { return SvgCurveMath.GetBounds(cubic); }
    /// <summary>Computes tight arc bounds.</summary><param name="arc">The arc.</param><returns>The bounds or failure.</returns>
    public SvgRectOperationResult GetBoundsWithArc(SvgArc arc) { return SvgCurveMath.GetBounds(arc); }
}
