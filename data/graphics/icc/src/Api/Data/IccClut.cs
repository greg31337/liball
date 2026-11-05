namespace icc;

/// <summary>Represents an N-dimensional colour lookup table.</summary>
public class IccClut
{
    /// <summary>Gets or sets the number of input channels.</summary>
    public int InputChannels;
    /// <summary>Gets or sets the number of output channels.</summary>
    public int OutputChannels;
    /// <summary>Gets or sets grid-point counts for each input dimension.</summary>
    public int[] GridPoints;
    /// <summary>Gets or sets output values in ICC grid order.</summary>
    public double[] Values;
    /// <summary>Gets or sets the preferred interpolation.</summary>
    public IccInterpolationKind Interpolation;
    /// <summary>Gets or sets the ICC.2 interpolation-hint selector.</summary>
    public int InterpolationHint;
    /// <summary>Gets or sets the ICC.2 value-encoding selector.</summary>
    public int Encoding;

    /// <summary>Initializes an empty lookup table.</summary>
    public IccClut()
    {
        InputChannels = 0;
        OutputChannels = 0;
        GridPoints = new int[0];
        Values = new double[0];
        Interpolation = IccInterpolationKind.Multilinear;
        InterpolationHint = 0;
        Encoding = 0;
    }
}
