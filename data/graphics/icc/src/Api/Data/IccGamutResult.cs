namespace icc;

/// <summary>Reports gamut membership and nearest-boundary information.</summary>
public class IccGamutResult
{
    /// <summary>Gets or sets the gamut status.</summary>
    public IccGamutStatus Status;
    /// <summary>Gets or sets the tested colour.</summary>
    public IccColorVector Color;
    /// <summary>Gets or sets the nearest boundary colour when available.</summary>
    public IccColorVector? NearestBoundaryColor;
    /// <summary>Gets or sets distance to the boundary.</summary>
    public double Distance;
    /// <summary>Gets or sets the operation outcome.</summary>
    public IccOperationResult Result;

    /// <summary>Initializes an unknown gamut result.</summary>
    public IccGamutResult()
    {
        Status = IccGamutStatus.Unknown;
        Color = new IccColorVector();
        NearestBoundaryColor = null;
        Distance = 0.0;
        Result = new IccOperationResult();
    }
}
