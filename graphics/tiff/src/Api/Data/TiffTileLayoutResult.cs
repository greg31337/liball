namespace tiff;

/// <summary>Contains a normalized tile layout and its diagnostics.</summary>
public class TiffTileLayoutResult
{
    /// <summary>Indicates whether the layout was created without errors.</summary>
    public bool Success;
    /// <summary>Contains the normalized tile layout.</summary>
    public TiffTileLayout Layout;
    /// <summary>Contains warnings and errors produced while creating the layout.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty tile-layout result.</summary>
    public TiffTileLayoutResult()
    {
        Success = false;
        Layout = new TiffTileLayout();
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffTileLayoutResult Create(TiffTileLayout layout, TiffDiagnostics diagnostics)
    {
        TiffTileLayoutResult result = new TiffTileLayoutResult();
        result.Layout = layout.Copy();
        result.Diagnostics = diagnostics;
        result.Success = !diagnostics.HasErrors();
        return result;
    }
}
