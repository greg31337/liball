namespace geo;

/// <summary>Stores a two-dimensional affine transformation matrix.</summary>
public class AffineTransform
{
    /// <summary>The X-to-X scale or rotation coefficient.</summary>
    public double M00 = 1.0;

    /// <summary>The Y-to-X skew or rotation coefficient.</summary>
    public double M01;

    /// <summary>The X translation coefficient.</summary>
    public double M02;

    /// <summary>The X-to-Y skew or rotation coefficient.</summary>
    public double M10;

    /// <summary>The Y-to-Y scale or rotation coefficient.</summary>
    public double M11 = 1.0;

    /// <summary>The Y translation coefficient.</summary>
    public double M12;
}
