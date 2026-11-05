using System;

namespace tiff;

/// <summary>Contains a parsed TIFF date/time value and validation diagnostics.</summary>
public class TiffDateTimeParseResult
{
    /// <summary>Indicates whether the text was a valid TIFF date/time value.</summary>
    public bool Success;
    /// <summary>Contains the parsed value with <c>Unspecified</c> kind.</summary>
    public DateTime Value;
    /// <summary>Contains the exact input text.</summary>
    public string OriginalText;
    /// <summary>Contains validation diagnostics.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an unsuccessful empty parse result.</summary>
    public TiffDateTimeParseResult()
    {
        Success = false;
        Value = default(DateTime);
        OriginalText = "";
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffDateTimeParseResult Create(DateTime value, string originalText, TiffDiagnostics diagnostics)
    {
        TiffDateTimeParseResult result = new TiffDateTimeParseResult();
        result.Value = value;
        result.OriginalText = originalText == null ? "" : originalText;
        result.Diagnostics = diagnostics;
        result.Success = !diagnostics.HasErrors();
        return result;
    }
}
