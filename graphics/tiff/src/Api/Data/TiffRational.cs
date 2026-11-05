namespace tiff;

/// <summary>Represents rational used by the public TIFF API.</summary>
public class TiffRational
{
    /// <summary>Contains the numerator.</summary>
    public long Numerator;
    /// <summary>Contains the denominator.</summary>
    public long Denominator;

    /// <summary>Creates a new instance of the TIFF Rational class.</summary>
    public TiffRational()
    {
        Numerator = 0;
        Denominator = 1;
    }

    /// <summary>Creates a new instance of the TIFF Rational class.</summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    internal static TiffRational Create(long numerator, long denominator)
    {
        TiffRational value = new TiffRational();
        value.Initialize(numerator, denominator);
        return value;
    }

    internal void Initialize(long numerator, long denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF rational.</returns>
    internal TiffRational Copy()
    {
        return TiffRational.Create(Numerator, Denominator);
    }

    /// <summary>Determines whether has valid denominator under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasValidDenominator()
    {
        return Denominator != 0;
    }

    /// <summary>Converts the numerator and denominator to a floating-point value.</summary>
    /// <returns>The resulting double.</returns>
    internal double ToDouble()
    {
        if (Denominator == 0)
        {
            return 0.0;
        }

        return (double)Numerator / (double)Denominator;
    }
}
