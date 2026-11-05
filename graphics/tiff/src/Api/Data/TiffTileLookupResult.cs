namespace tiff;

/// <summary>Contains the values and diagnostics produced by a tile lookup operation.</summary>
public class TiffTileLookupResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the lookup.</summary>
    public TiffTileLookup Lookup;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Tile Lookup Result class.</summary>
    public TiffTileLookupResult()
    {
        Success = false;
        Lookup = new TiffTileLookup();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Tile Lookup Result class.</summary>
    /// <param name="lookup">The lookup.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffTileLookupResult Create(TiffTileLookup lookup, TiffDiagnostics diagnostics)
    {
        TiffTileLookupResult value = new TiffTileLookupResult();
        value.Initialize(lookup, diagnostics);
        return value;
    }

    internal void Initialize(TiffTileLookup lookup, TiffDiagnostics diagnostics)
    {
        if (lookup == null)
        {
            Lookup = new TiffTileLookup();
        }
        else
        {
            Lookup = lookup.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
