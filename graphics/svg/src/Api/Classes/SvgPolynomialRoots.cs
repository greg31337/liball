namespace svg;

/// <summary>Provides stable real roots for linear and quadratic polynomials.</summary>
public class SvgPolynomialRoots : ISvgPolynomialRoots
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPolynomialRoots()
    {
    }

    /// <summary>Solves <c>a*x + b = 0</c> for its isolated real root.</summary>
    /// <param name="a">The linear coefficient.</param><param name="b">The constant coefficient.</param>
    /// <returns>The finite isolated roots; an indeterminate equation has none.</returns>
    public SvgPolynomialRootResult SolveLinear(double a, double b) { return SvgCurveMath.SolveLinear(a, b); }
    /// <summary>Solves <c>a*x*x + b*x + c = 0</c> for distinct real roots.</summary>
    /// <param name="a">The quadratic coefficient.</param><param name="b">The linear coefficient.</param><param name="c">The constant coefficient.</param>
    /// <returns>The ascending finite real roots or a stable failure.</returns>
    public SvgPolynomialRootResult SolveQuadratic(double a, double b, double c) { return SvgCurveMath.SolveQuadratic(a, b, c); }
}
