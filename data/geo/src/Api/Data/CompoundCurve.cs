namespace geo;

/// <summary>Stores the connected linear and circular components of a compound curve payload.</summary>
public class CompoundCurve
{
    /// <summary>The curve components, or an empty array when the compound curve is empty.</summary>
    public Geometry[] Curves = [];
}
