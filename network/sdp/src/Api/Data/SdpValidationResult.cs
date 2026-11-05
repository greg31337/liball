using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains the bounded result of validating an SDP description.
/// </summary>
public class SdpValidationResult
{
    /// <summary>Indicates whether the description satisfies the selected validation profile.</summary>
    public bool Success = false;

    /// <summary>Contains the options applied to validation.</summary>
    public SdpValidationOptions AppliedOptions = new SdpValidationOptions();

    /// <summary>Contains retained validation issues in deterministic order.</summary>
    public List<SdpValidationIssue> Issues = new List<SdpValidationIssue>();

    /// <summary>Indicates that the configured issue capacity was exhausted.</summary>
    public bool DiagnosticLimitReached = false;

    /// <summary>Contains the number of retained warning issues.</summary>
    public int WarningCount = 0;

    /// <summary>Contains the number of retained error and fatal issues.</summary>
    public int ErrorCount = 0;
}
