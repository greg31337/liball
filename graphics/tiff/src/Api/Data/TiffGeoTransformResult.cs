namespace tiff;

/// <summary>Contains the values and diagnostics produced by a geographic transform operation.</summary>
public class TiffGeoTransformResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the coordinate.</summary>
    public TiffGeoCoordinate Coordinate;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF geographic Transform Result class.</summary>
    public TiffGeoTransformResult()
    {
        Success = false;
        Coordinate = new TiffGeoCoordinate();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF geographic Transform Result class.</summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffGeoTransformResult Create(TiffGeoCoordinate coordinate, TiffDiagnostics diagnostics)
    {
        TiffGeoTransformResult value = new TiffGeoTransformResult();
        value.Initialize(coordinate, diagnostics);
        return value;
    }

    internal void Initialize(TiffGeoCoordinate coordinate, TiffDiagnostics diagnostics)
    {
        if (coordinate == null)
        {
            Coordinate = new TiffGeoCoordinate();
        }
        else
        {
            Coordinate = coordinate.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
