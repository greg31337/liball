namespace svg;

/// <summary>Represents one finite line segment.</summary>
public class SvgLineSegment
{
    /// <summary>The segment starting point.</summary>
    public SvgPoint Start;
    /// <summary>The segment ending point.</summary>
    public SvgPoint End;

    /// <summary>Creates a line by copying two finite points.</summary>
    /// <param name="start">The starting point.</param>
    /// <param name="end">The ending point.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a point is missing or non-finite.</exception>
    public SvgLineSegment(SvgPoint start, SvgPoint end)
    {
        if (!SvgCurveMath.ValidPoint(start) || !SvgCurveMath.ValidPoint(end)) throw new System.ArgumentOutOfRangeException("start");
        Start = new SvgPoint(start.X, start.Y);
        End = new SvgPoint(end.X, end.Y);
    }

    /// <summary>Evaluates the line at an inclusive unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite point or a stable failure.</returns>
    internal SvgPointOperationResult Evaluate(double amount) { return new SvgCurveGeometry().EvaluateWithLineAndAmount(this, amount); }
    /// <summary>Evaluates the constant first derivative.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite derivative vector or a stable failure.</returns>
    internal SvgVectorOperationResult Derivative(double amount) { return new SvgCurveGeometry().DerivativeWithLineAndAmount(this, amount); }
    /// <summary>Splits the line at an inclusive unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The ordered line portions or a stable failure.</returns>
    internal SvgLineSplitResult Split(double amount) { return new SvgCurveGeometry().SplitWithLineAndAmount(this, amount); }
    /// <summary>Reverses the line.</summary>
    /// <returns>The reversed line or a stable failure.</returns>
    internal SvgLineOperationResult Reverse() { return new SvgCurveGeometry().ReverseWithLine(this); }
    /// <summary>Computes the tight axis-aligned line bounds.</summary>
    /// <returns>The finite bounds or a stable failure.</returns>
    internal SvgRectOperationResult GetBounds() { return new SvgCurveGeometry().GetBoundsWithLine(this); }
    /// <summary>Flattens the line to parameter-ordered points.</summary>
    /// <param name="options">The tolerance and security limits.</param>
    /// <returns>The flattened points or a stable failure.</returns>
    internal SvgFlattenResult Flatten(SvgFlattenOptions options) { return new SvgCurveFlattener().FlattenWithLineAndOptions(this, options); }
}

/// <summary>Represents one finite quadratic Bézier segment.</summary>
public class SvgQuadraticBezier
{
    /// <summary>The segment starting point.</summary>
    public SvgPoint Start;
    /// <summary>The quadratic control point.</summary>
    public SvgPoint Control;
    /// <summary>The segment ending point.</summary>
    public SvgPoint End;

    /// <summary>Creates a quadratic by copying three finite points.</summary>
    /// <param name="start">The starting point.</param>
    /// <param name="control">The control point.</param>
    /// <param name="end">The ending point.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a point is missing or non-finite.</exception>
    public SvgQuadraticBezier(SvgPoint start, SvgPoint control, SvgPoint end)
    {
        if (!SvgCurveMath.ValidPoint(start) || !SvgCurveMath.ValidPoint(control) || !SvgCurveMath.ValidPoint(end)) throw new System.ArgumentOutOfRangeException("start");
        Start = new SvgPoint(start.X, start.Y);
        Control = new SvgPoint(control.X, control.Y);
        End = new SvgPoint(end.X, end.Y);
    }

    /// <summary>Evaluates the quadratic at an inclusive unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite point or a stable failure.</returns>
    internal SvgPointOperationResult Evaluate(double amount) { return new SvgCurveGeometry().EvaluateWithQuadraticAndAmount(this, amount); }
    /// <summary>Evaluates the first derivative.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite derivative vector or a stable failure.</returns>
    internal SvgVectorOperationResult Derivative(double amount) { return new SvgCurveGeometry().DerivativeWithQuadraticAndAmount(this, amount); }
    /// <summary>Splits the quadratic at an inclusive unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The ordered quadratic portions or a stable failure.</returns>
    internal SvgQuadraticSplitResult Split(double amount) { return new SvgCurveGeometry().SplitWithQuadraticAndAmount(this, amount); }
    /// <summary>Reverses the quadratic.</summary>
    /// <returns>The reversed quadratic or a stable failure.</returns>
    internal SvgQuadraticOperationResult Reverse() { return new SvgCurveGeometry().ReverseWithQuadratic(this); }
    /// <summary>Computes tight axis-aligned bounds including interior extrema.</summary>
    /// <returns>The finite bounds or a stable failure.</returns>
    internal SvgRectOperationResult GetBounds() { return new SvgCurveGeometry().GetBoundsWithQuadratic(this); }
    /// <summary>Adaptively flattens the quadratic to parameter-ordered points.</summary>
    /// <param name="options">The tolerance and security limits.</param>
    /// <returns>The flattened points or a stable failure.</returns>
    internal SvgFlattenResult Flatten(SvgFlattenOptions options) { return new SvgCurveFlattener().FlattenWithQuadraticAndOptions(this, options); }
}

/// <summary>Contains a non-throwing line operation result.</summary>
public class SvgLineOperationResult
{
    /// <summary>Whether the operation produced a finite line.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite line, or <c>null</c> after failure.</summary>
    public SvgLineSegment? Line;
    /// <summary>Creates a line operation result.</summary>
    /// <param name="success">Whether the operation succeeded.</param><param name="error">The failure kind.</param><param name="line">The finite line.</param>
    public SvgLineOperationResult(bool success, SvgGeometryError error, SvgLineSegment? line) { Success = success; Error = error; Line = line; }
}

/// <summary>Contains two ordered portions of a split line.</summary>
public class SvgLineSplitResult
{
    /// <summary>Whether splitting succeeded.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The first portion.</summary>
    public SvgLineSegment? First;
    /// <summary>The second portion.</summary>
    public SvgLineSegment? Second;
    /// <summary>Creates a line split result.</summary>
    /// <param name="success">Whether splitting succeeded.</param><param name="error">The failure kind.</param><param name="first">The first portion.</param><param name="second">The second portion.</param>
    public SvgLineSplitResult(bool success, SvgGeometryError error, SvgLineSegment? first, SvgLineSegment? second) { Success = success; Error = error; First = first; Second = second; }
}

/// <summary>Contains a non-throwing quadratic operation result.</summary>
public class SvgQuadraticOperationResult
{
    /// <summary>Whether the operation produced a finite quadratic.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite quadratic, or <c>null</c> after failure.</summary>
    public SvgQuadraticBezier? Quadratic;
    /// <summary>Creates a quadratic operation result.</summary>
    /// <param name="success">Whether the operation succeeded.</param><param name="error">The failure kind.</param><param name="quadratic">The finite quadratic.</param>
    public SvgQuadraticOperationResult(bool success, SvgGeometryError error, SvgQuadraticBezier? quadratic) { Success = success; Error = error; Quadratic = quadratic; }
}

/// <summary>Contains two ordered portions of a split quadratic.</summary>
public class SvgQuadraticSplitResult
{
    /// <summary>Whether splitting succeeded.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The first portion.</summary>
    public SvgQuadraticBezier? First;
    /// <summary>The second portion.</summary>
    public SvgQuadraticBezier? Second;
    /// <summary>Creates a quadratic split result.</summary>
    /// <param name="success">Whether splitting succeeded.</param><param name="error">The failure kind.</param><param name="first">The first portion.</param><param name="second">The second portion.</param>
    public SvgQuadraticSplitResult(bool success, SvgGeometryError error, SvgQuadraticBezier? first, SvgQuadraticBezier? second) { Success = success; Error = error; First = first; Second = second; }
}

/// <summary>Contains a non-throwing cubic operation result.</summary>
public class SvgCubicOperationResult
{
    /// <summary>Whether the operation produced a finite cubic.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite cubic, or <c>null</c> after failure.</summary>
    public SvgCubicBezier? Cubic;
    /// <summary>Creates a cubic operation result.</summary>
    /// <param name="success">Whether the operation succeeded.</param><param name="error">The failure kind.</param><param name="cubic">The finite cubic.</param>
    public SvgCubicOperationResult(bool success, SvgGeometryError error, SvgCubicBezier? cubic) { Success = success; Error = error; Cubic = cubic; }
}

/// <summary>Contains two ordered portions of a split cubic.</summary>
public class SvgCubicSplitResult
{
    /// <summary>Whether splitting succeeded.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The first portion.</summary>
    public SvgCubicBezier? First;
    /// <summary>The second portion.</summary>
    public SvgCubicBezier? Second;
    /// <summary>Creates a cubic split result.</summary>
    /// <param name="success">Whether splitting succeeded.</param><param name="error">The failure kind.</param><param name="first">The first portion.</param><param name="second">The second portion.</param>
    public SvgCubicSplitResult(bool success, SvgGeometryError error, SvgCubicBezier? first, SvgCubicBezier? second) { Success = success; Error = error; First = first; Second = second; }
}

/// <summary>Contains ordered distinct real polynomial roots.</summary>
public class SvgPolynomialRootResult
{
    /// <summary>Whether root solving completed with finite results.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The ascending distinct real roots.</summary>
    public System.Collections.Generic.List<double>? Roots;
    /// <summary>Creates a polynomial root result.</summary>
    /// <param name="success">Whether solving succeeded.</param><param name="error">The failure kind.</param><param name="roots">The ordered roots.</param>
    public SvgPolynomialRootResult(bool success, SvgGeometryError error, System.Collections.Generic.List<double>? roots) { Success = success; Error = error; Roots = roots; }
}
