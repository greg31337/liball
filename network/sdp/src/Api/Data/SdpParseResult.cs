using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains the bounded result of parsing SDP input.
/// </summary>
public class SdpParseResult
{
    /// <summary>Indicates whether parsing completed without error or fatal diagnostics.</summary>
    public bool Success = false;

    /// <summary>Contains the parsed description when sufficient input could be represented.</summary>
    public SdpSessionDescription? Description = null;

    /// <summary>Contains the options applied to the parse.</summary>
    public SdpParseOptions AppliedOptions = new SdpParseOptions();

    /// <summary>Contains retained diagnostics in deterministic order.</summary>
    public List<SdpDiagnostic> Diagnostics = new List<SdpDiagnostic>();

    /// <summary>Indicates that the configured diagnostic capacity was exhausted.</summary>
    public bool DiagnosticLimitReached = false;

    /// <summary>Contains the number of input characters consumed by text parsing.</summary>
    public int CharactersConsumed = 0;

    /// <summary>Contains the number of input bytes consumed by UTF-8 parsing.</summary>
    public int BytesConsumed = 0;

    /// <summary>Contains the number of input lines consumed.</summary>
    public int LinesConsumed = 0;
}
