namespace tiff;

/// <summary>Contains a normalized strip layout and its diagnostics.</summary>
public class TiffStripLayoutResult
{
    /// <summary>Indicates whether the layout was created without errors.</summary>
    public bool Success;
    /// <summary>Contains the normalized strip layout.</summary>
    public TiffStripLayout Layout;
    /// <summary>Contains warnings and errors produced while creating the layout.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty strip-layout result.</summary>
    public TiffStripLayoutResult()
    {
        Success = false;
        Layout = new TiffStripLayout();
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffStripLayoutResult Create(TiffStripLayout layout, TiffDiagnostics diagnostics)
    {
        TiffStripLayoutResult result = new TiffStripLayoutResult();
        result.Layout = layout.Copy();
        result.Diagnostics = diagnostics;
        result.Success = !diagnostics.HasErrors();
        return result;
    }
}
