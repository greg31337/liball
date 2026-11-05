namespace ttf;

/// <summary>Identifies the decoder selected for an OpenType name record.</summary>
public enum FontNameEncodingKind
{
    /// <summary>No supported decoder is available.</summary>
    Unknown = 0,

    /// <summary>Big-endian UTF-16.</summary>
    Utf16BigEndian = 1,

    /// <summary>Macintosh Roman or a language-specific Roman override.</summary>
    MacintoshRoman = 2,

    /// <summary>A supported legacy Macintosh encoding.</summary>
    MacintoshLegacy = 3,

    /// <summary>Windows code page 936.</summary>
    WindowsCodePage936 = 4,

    /// <summary>Windows code page 950.</summary>
    WindowsCodePage950 = 5,

    /// <summary>Windows code page 949.</summary>
    WindowsCodePage949 = 6,

    /// <summary>Windows code page 932.</summary>
    WindowsCodePage932 = 7
}
