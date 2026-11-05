namespace sdp;

/// <summary>
/// Defines safe default limits for SDP parsing and generation.
/// </summary>
public static class SdpLimits
{
    /// <summary>The default maximum number of characters accepted from text input.</summary>
    public const int DefaultMaximumInputCharacters = 1048576;

    /// <summary>The default maximum number of bytes accepted from UTF-8 input.</summary>
    public const int DefaultMaximumInputBytes = 4194304;

    /// <summary>The default maximum number of characters produced as text output.</summary>
    public const int DefaultMaximumOutputCharacters = 4194304;

    /// <summary>The default maximum number of bytes produced as UTF-8 output.</summary>
    public const int DefaultMaximumOutputBytes = 16777216;

    /// <summary>The default maximum number of characters or bytes in one input line.</summary>
    public const int DefaultMaximumLineLength = 16384;

    /// <summary>The default maximum number of input lines.</summary>
    public const int DefaultMaximumLineCount = 16384;

    /// <summary>The default maximum number of media descriptions.</summary>
    public const int DefaultMaximumMediaCount = 256;

    /// <summary>The default maximum number of attributes in one session or media scope.</summary>
    public const int DefaultMaximumAttributesPerScope = 2048;

    /// <summary>The default maximum number of characters or bytes in one parsed token.</summary>
    public const int DefaultMaximumTokenLength = 4096;

    /// <summary>The default maximum number of ICE candidates in one media scope.</summary>
    public const int DefaultMaximumCandidateCount = 1024;

    /// <summary>The default maximum number of diagnostics retained by one operation.</summary>
    public const int DefaultMaximumDiagnosticCount = 256;
}
