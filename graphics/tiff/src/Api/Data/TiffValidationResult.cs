namespace tiff;

/// <summary>Contains the values and diagnostics produced by a validation operation.</summary>
public class TiffValidationResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Specifies the strict or permissive policy used by validation.</summary>
    public TiffValidationMode Mode;
    /// <summary>Contains the number of error diagnostics.</summary>
    public int ErrorCount;
    /// <summary>Contains the number of warning diagnostics.</summary>
    public int WarningCount;
    /// <summary>Contains the parsed TIFF header.</summary>
    public TiffHeader Header;
    /// <summary>Contains the image count.</summary>
    public int ImageCount;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty validation result.</summary>
    public TiffValidationResult()
    {
        Success = false;
        Mode = TiffValidationMode.Strict;
        ErrorCount = 0;
        WarningCount = 0;
        Header = new TiffHeader();
        ImageCount = 0;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffValidationResult Create(TiffHeader header, int imageCount, TiffValidationMode mode, TiffDiagnostics diagnostics)
    {
        TiffValidationResult value = new TiffValidationResult();
        value.Initialize(header, imageCount, mode, diagnostics);
        return value;
    }

    internal void Initialize(TiffHeader header, int imageCount, TiffValidationMode mode, TiffDiagnostics diagnostics)
    {
        Header = TiffFile.CopyHeader(header);
        ImageCount = imageCount;
        Mode = mode;
        Diagnostics = diagnostics;
        ErrorCount = diagnostics.ErrorCount();
        WarningCount = diagnostics.WarningCount();
        Success = !Diagnostics.HasErrors();
    }
}
