namespace icc;

/// <summary>Represents one ICC parametric-curve function.</summary>
public class IccParametricCurve
{
    /// <summary>Gets or sets the ICC function selector.</summary>
    public int FunctionType;
    /// <summary>Gets or sets ordered function parameters.</summary>
    public double[] Parameters;

    /// <summary>Initializes function zero with no parameters.</summary>
    public IccParametricCurve()
    {
        FunctionType = 0;
        Parameters = new double[0];
    }
}
