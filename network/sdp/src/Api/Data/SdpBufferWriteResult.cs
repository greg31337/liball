using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains the exact outcome of writing UTF-8 SDP into a caller-provided buffer.
/// </summary>
public class SdpBufferWriteResult
{
    /// <summary>Indicates whether the complete output was written.</summary>
    public bool Success = false;

    /// <summary>Indicates that the caller-provided buffer was too small.</summary>
    public bool BufferTooSmall = false;

    /// <summary>Contains the number of bytes written to the caller-provided buffer.</summary>
    public int BytesWritten = 0;

    /// <summary>Contains the exact buffer capacity required for complete output.</summary>
    public int RequiredBytes = 0;

    /// <summary>Contains retained diagnostics in deterministic order.</summary>
    public List<SdpDiagnostic> Diagnostics = new List<SdpDiagnostic>();

    /// <summary>Indicates that the configured diagnostic capacity was exhausted.</summary>
    public bool DiagnosticLimitReached = false;
}
