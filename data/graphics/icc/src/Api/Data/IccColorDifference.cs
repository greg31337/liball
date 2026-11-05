namespace icc;

/// <summary>Represents a colour-difference comparison and its inputs.</summary>
public class IccColorDifference
{
    /// <summary>Gets or sets the comparison formula.</summary>
    public IccColorDifferenceFormula Formula;
    /// <summary>Gets or sets the first Lab colour.</summary>
    public IccLabColor First;
    /// <summary>Gets or sets the second Lab colour.</summary>
    public IccLabColor Second;
    /// <summary>Gets or sets the calculated delta E value.</summary>
    public double DeltaE;

    /// <summary>Initializes an empty CIE76 comparison.</summary>
    public IccColorDifference()
    {
        Formula = IccColorDifferenceFormula.Cie76;
        First = new IccLabColor();
        Second = new IccLabColor();
        DeltaE = 0.0;
    }
}
