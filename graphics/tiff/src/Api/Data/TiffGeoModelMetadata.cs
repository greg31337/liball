namespace tiff;

/// <summary>Represents geographic model metadata extracted from or prepared for a TIFF file.</summary>
public class TiffGeoModelMetadata
{
    /// <summary>Indicates whether the pixel scale is present.</summary>
    public bool HasPixelScale;
    /// <summary>Indicates whether the tiepoints is present.</summary>
    public bool HasTiepoints;
    /// <summary>Indicates whether the transformation is present.</summary>
    public bool HasTransformation;
    /// <summary>Contains the pixel scale.</summary>
    public double[] PixelScale;
    /// <summary>Contains the tiepoints.</summary>
    public TiffGeoTiepoint[] Tiepoints;
    /// <summary>Contains the transformation.</summary>
    public double[] Transformation;

    /// <summary>Creates a new instance of the TIFF geographic Model Metadata class.</summary>
    public TiffGeoModelMetadata()
    {
        HasPixelScale = false;
        HasTiepoints = false;
        HasTransformation = false;
        PixelScale = new double[0];
        Tiepoints = new TiffGeoTiepoint[0];
        Transformation = new double[0];
    }

    /// <summary>Creates a new instance of the TIFF geographic Model Metadata class.</summary>
    /// <param name="hasPixelScale">The has pixel scale.</param>
    /// <param name="pixelScale">The pixel scale.</param>
    /// <param name="hasTiepoints">The has tiepoints.</param>
    /// <param name="tiepoints">The tiepoints.</param>
    /// <param name="hasTransformation">The has transformation.</param>
    /// <param name="transformation">The transformation.</param>
    internal static TiffGeoModelMetadata Create(bool hasPixelScale, double[] pixelScale, bool hasTiepoints, TiffGeoTiepoint[] tiepoints, bool hasTransformation, double[] transformation)
    {
        TiffGeoModelMetadata value = new TiffGeoModelMetadata();
        value.Initialize(hasPixelScale, pixelScale, hasTiepoints, tiepoints, hasTransformation, transformation);
        return value;
    }

    internal void Initialize(bool hasPixelScale, double[] pixelScale, bool hasTiepoints, TiffGeoTiepoint[] tiepoints, bool hasTransformation, double[] transformation)
    {
        HasPixelScale = hasPixelScale;
        HasTiepoints = hasTiepoints;
        HasTransformation = hasTransformation;
        PixelScale = CopyDoubleArray(pixelScale);
        Tiepoints = CopyTiepoints(tiepoints);
        Transformation = CopyDoubleArray(transformation);
    }

    /// <summary>Returns the number of available tiepoint entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int TiepointCount()
    {
        return Tiepoints.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF geographic model metadata.</returns>
    internal TiffGeoModelMetadata Copy()
    {
        return TiffGeoModelMetadata.Create(HasPixelScale, PixelScale, HasTiepoints, Tiepoints, HasTransformation, Transformation);
    }

    internal static double[] CopyDoubleArray(double[] values)
    {
        if (values == null)
        {
            return new double[0];
        }

        double[] copy = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static TiffGeoTiepoint[] CopyTiepoints(TiffGeoTiepoint[] tiepoints)
    {
        if (tiepoints == null)
        {
            return new TiffGeoTiepoint[0];
        }

        TiffGeoTiepoint[] copy = new TiffGeoTiepoint[tiepoints.Length];
        int index = 0;
        while (index < tiepoints.Length)
        {
            if (tiepoints[index] == null)
            {
                copy[index] = new TiffGeoTiepoint();
            }
            else
            {
                copy[index] = tiepoints[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
