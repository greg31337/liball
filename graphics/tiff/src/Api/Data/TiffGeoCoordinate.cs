namespace tiff;

/// <summary>Represents geographic coordinate used by the public TIFF API.</summary>
public class TiffGeoCoordinate
{
    /// <summary>Contains the X.</summary>
    public double X;
    /// <summary>Contains the Y.</summary>
    public double Y;
    /// <summary>Contains the Z.</summary>
    public double Z;

    /// <summary>Creates a new instance of the TIFF geographic Coordinate class.</summary>
    public TiffGeoCoordinate()
    {
        X = 0;
        Y = 0;
        Z = 0;
    }

    /// <summary>Creates a new instance of the TIFF geographic Coordinate class.</summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <param name="z">The z.</param>
    internal static TiffGeoCoordinate Create(double x, double y, double z)
    {
        TiffGeoCoordinate value = new TiffGeoCoordinate();
        value.Initialize(x, y, z);
        return value;
    }

    internal void Initialize(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF geographic coordinate.</returns>
    internal TiffGeoCoordinate Copy()
    {
        return TiffGeoCoordinate.Create(X, Y, Z);
    }
}
