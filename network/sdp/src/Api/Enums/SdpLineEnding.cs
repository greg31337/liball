namespace sdp;

/// <summary>
/// Identifies a line ending encountered in or selected for SDP text.
/// </summary>
public enum SdpLineEnding
{
    /// <summary>The line ending is unknown.</summary>
    Unknown = 0,

    /// <summary>No line ending follows the final line.</summary>
    None = 1,

    /// <summary>The line ends with carriage return followed by line feed.</summary>
    CrLf = 2,

    /// <summary>The line ends with line feed.</summary>
    Lf = 3,

    /// <summary>The line ends with carriage return.</summary>
    Cr = 4
}
