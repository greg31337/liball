namespace icc;

/// <summary>Reports the result of attempting to invert an ICC curve at one output value.</summary>
public class IccCurveInversionResult
{
    /// <summary>Gets or sets whether a corresponding input was found.</summary>
    public bool HasInput;

    /// <summary>Gets or sets the corresponding input when <see cref="HasInput"/> is true.</summary>
    public double Input;

    /// <summary>Gets or sets the operation outcome.</summary>
    public IccOperationResult Result;

    /// <summary>Initializes an inversion result with no input value.</summary>
    public IccCurveInversionResult()
    {
        HasInput = false;
        Input = 0.0;
        Result = new IccOperationResult();
    }
}
