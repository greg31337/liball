namespace sdp;

/// <summary>
/// Configures bounded parsing and input-preservation behavior.
/// </summary>
public class SdpParseOptions
{
    /// <summary>Specifies the parser's strictness and preservation mode.</summary>
    public SdpParseMode Mode = SdpParseMode.Strict;

    /// <summary>Indicates whether a leading UTF-8 byte-order mark may be accepted.</summary>
    public bool AllowByteOrderMark = false;

    /// <summary>Indicates whether bare line-feed endings may be accepted.</summary>
    public bool AllowLfLineEndings = false;

    /// <summary>Indicates whether bare carriage-return endings may be accepted.</summary>
    public bool AllowCrLineEndings = false;

    /// <summary>Indicates whether original source lines should be retained.</summary>
    public bool PreserveRawLines = false;

    /// <summary>Indicates whether recoverable malformed lines should be retained.</summary>
    public bool PreserveMalformedLines = false;

    /// <summary>Contains the maximum number of accepted input characters.</summary>
    public int MaximumInputCharacters = SdpLimits.DefaultMaximumInputCharacters;

    /// <summary>Contains the maximum number of accepted UTF-8 input bytes.</summary>
    public int MaximumInputBytes = SdpLimits.DefaultMaximumInputBytes;

    /// <summary>Contains the maximum number of characters or bytes in one line.</summary>
    public int MaximumLineLength = SdpLimits.DefaultMaximumLineLength;

    /// <summary>Contains the maximum number of input lines.</summary>
    public int MaximumLineCount = SdpLimits.DefaultMaximumLineCount;

    /// <summary>Contains the maximum number of media descriptions.</summary>
    public int MaximumMediaCount = SdpLimits.DefaultMaximumMediaCount;

    /// <summary>Contains the maximum number of attributes in one scope.</summary>
    public int MaximumAttributesPerScope = SdpLimits.DefaultMaximumAttributesPerScope;

    /// <summary>Contains the maximum number of characters or bytes in one token.</summary>
    public int MaximumTokenLength = SdpLimits.DefaultMaximumTokenLength;

    /// <summary>Contains the maximum number of ICE candidates in one media scope.</summary>
    public int MaximumCandidateCount = SdpLimits.DefaultMaximumCandidateCount;

    /// <summary>Contains the maximum number of diagnostics retained by one parse.</summary>
    public int MaximumDiagnosticCount = SdpLimits.DefaultMaximumDiagnosticCount;
}
