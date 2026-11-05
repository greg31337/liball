namespace tiff;

/// <summary>Contains the values and diagnostics produced by a geographic key directory read operation.</summary>
public class TiffGeoKeyDirectoryReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the directory.</summary>
    public TiffGeoKeyDirectory Directory;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF geographic Key Directory Read Result class.</summary>
    public TiffGeoKeyDirectoryReadResult()
    {
        Success = true;
        Directory = new TiffGeoKeyDirectory();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF geographic Key Directory Read Result class.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffGeoKeyDirectoryReadResult Create(TiffGeoKeyDirectory directory, TiffDiagnostics diagnostics)
    {
        TiffGeoKeyDirectoryReadResult value = new TiffGeoKeyDirectoryReadResult();
        value.Initialize(directory, diagnostics);
        return value;
    }

    internal void Initialize(TiffGeoKeyDirectory directory, TiffDiagnostics diagnostics)
    {
        if (directory == null)
        {
            Directory = new TiffGeoKeyDirectory();
        }
        else
        {
            Directory = directory.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
