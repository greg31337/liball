namespace geo;

/// <summary>Stores a measured distance and the locations that realize it.</summary>
public class DistanceResult
{
    /// <summary>Whether a finite witness pair was found.</summary>
    public bool HasResult;

    /// <summary>The measured distance, or positive infinity when no result exists.</summary>
    public double Distance = double.PositiveInfinity;

    /// <summary>The nearest coordinate on the first geometry, or null when unavailable.</summary>
    public Coordinate? FirstCoordinate;

    /// <summary>The nearest coordinate on the second geometry, or null when unavailable.</summary>
    public Coordinate? SecondCoordinate;

    /// <summary>The location on the first linear geometry, or null when unavailable.</summary>
    public LinearLocation? FirstLocation;

    /// <summary>The location on the second linear geometry, or null when unavailable.</summary>
    public LinearLocation? SecondLocation;
}
