namespace geo;

/// <summary>Identifies how curved geometry linearization is bounded.</summary>
public enum CurveLinearizationMode
{
    /// <summary>Bound the maximum distance between each arc and chord.</summary>
    MaximumDeviation = 0,

    /// <summary>Bound the maximum length of generated line segments.</summary>
    MaximumSegmentLength = 1,

    /// <summary>Bound the angular sweep of generated line segments.</summary>
    MaximumAngle = 2
}
