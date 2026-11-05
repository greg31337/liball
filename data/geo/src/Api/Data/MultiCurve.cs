namespace geo;

/// <summary>Stores the curve components of a multi-curve payload.</summary>
public class MultiCurve
{
    /// <summary>The curve components, or an empty array when the multi-curve is empty.</summary>
    public Geometry[] Curves = [];
}
