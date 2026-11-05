using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains the bounded result of generating SDP text or UTF-8 data.
/// </summary>
public class SdpWriteResult
{
    /// <summary>Indicates whether generation completed successfully.</summary>
    public bool Success = false;

    /// <summary>Contains generated text when the text-writing entry point was used.</summary>
    public string Text = string.Empty;

    /// <summary>Contains generated UTF-8 data when the UTF-8 entry point was used.</summary>
    public byte[] Utf8Bytes = new byte[0];

    /// <summary>Contains the options applied to generation.</summary>
    public SdpWriteOptions AppliedOptions = new SdpWriteOptions();

    /// <summary>Contains retained diagnostics in deterministic order.</summary>
    public List<SdpDiagnostic> Diagnostics = new List<SdpDiagnostic>();

    /// <summary>Indicates that the configured diagnostic capacity was exhausted.</summary>
    public bool DiagnosticLimitReached = false;

    /// <summary>Contains the number of generated text characters.</summary>
    public int CharactersWritten = 0;

    /// <summary>Contains the number of generated UTF-8 bytes.</summary>
    public int BytesWritten = 0;
}
