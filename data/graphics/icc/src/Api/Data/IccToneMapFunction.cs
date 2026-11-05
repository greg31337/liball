namespace icc;

/// <summary>Represents one ICC.2 tone-mapping function.</summary>
public class IccToneMapFunction
{
    /// <summary>Gets or sets the mapping-function selector.</summary>
    public int FunctionType;
    /// <summary>Gets or sets the function parameters in encoded order.</summary>
    public double[] Parameters;

    /// <summary>Initializes the standard tone-mapping function with empty parameters.</summary>
    public IccToneMapFunction()
    {
        FunctionType = 0;
        Parameters = new double[0];
    }
}
