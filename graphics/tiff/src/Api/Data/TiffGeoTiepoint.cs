namespace tiff;

/// <summary>Represents geographic tiepoint used by the public TIFF API.</summary>
public class TiffGeoTiepoint
{
    /// <summary>Contains the raster I.</summary>
    public double RasterI;
    /// <summary>Contains the raster J.</summary>
    public double RasterJ;
    /// <summary>Contains the raster K.</summary>
    public double RasterK;
    /// <summary>Contains the model X.</summary>
    public double ModelX;
    /// <summary>Contains the model Y.</summary>
    public double ModelY;
    /// <summary>Contains the model Z.</summary>
    public double ModelZ;

    /// <summary>Creates a new instance of the TIFF geographic Tiepoint class.</summary>
    public TiffGeoTiepoint()
    {
        RasterI = 0;
        RasterJ = 0;
        RasterK = 0;
        ModelX = 0;
        ModelY = 0;
        ModelZ = 0;
    }

    /// <summary>Creates a new instance of the TIFF geographic Tiepoint class.</summary>
    /// <param name="rasterI">The raster I.</param>
    /// <param name="rasterJ">The raster J.</param>
    /// <param name="rasterK">The raster K.</param>
    /// <param name="modelX">The model X.</param>
    /// <param name="modelY">The model Y.</param>
    /// <param name="modelZ">The model Z.</param>
    internal static TiffGeoTiepoint Create(double rasterI, double rasterJ, double rasterK, double modelX, double modelY, double modelZ)
    {
        TiffGeoTiepoint value = new TiffGeoTiepoint();
        value.Initialize(rasterI, rasterJ, rasterK, modelX, modelY, modelZ);
        return value;
    }

    internal void Initialize(double rasterI, double rasterJ, double rasterK, double modelX, double modelY, double modelZ)
    {
        RasterI = rasterI;
        RasterJ = rasterJ;
        RasterK = rasterK;
        ModelX = modelX;
        ModelY = modelY;
        ModelZ = modelZ;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF geographic tiepoint.</returns>
    internal TiffGeoTiepoint Copy()
    {
        return TiffGeoTiepoint.Create(RasterI, RasterJ, RasterK, ModelX, ModelY, ModelZ);
    }
}
