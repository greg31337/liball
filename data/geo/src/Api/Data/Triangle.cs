namespace geo;

/// <summary>Stores the single ring of a triangular surface payload.</summary>
public class Triangle
{
    /// <summary>The triangle ring, which is empty when the triangle is empty.</summary>
    public LinearRing Shell = new LinearRing();
}
