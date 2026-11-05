namespace svg;

/// <summary>Contains a bounded web-font reconstruction outcome.</summary>
public class SvgWebFontDecodeResult
{
    /// <summary>Whether a complete SFNT font was reconstructed.</summary>
    public bool Success;
    /// <summary>The stable OpenType or container failure.</summary>
    public SvgOpenTypeError Error;
    /// <summary>The detected web-font format.</summary>
    public SvgWebFontFormat Format;
    /// <summary>The reconstructed SFNT bytes, or <c>null</c>.</summary>
    public byte[]? Bytes;
    /// <summary>Whether a capable host decoder may handle the unsupported feature.</summary>
    public bool RequiresFallback;
    /// <summary>The unsupported table tag, or an empty string.</summary>
    public string UnsupportedTable;
    /// <summary>A stable human-readable outcome message.</summary>
    public string Message;

    internal SvgWebFontDecodeResult()
    {
        Success = false;
        Error = SvgOpenTypeError.None;
        Format = SvgWebFontFormat.Unknown;
        Bytes = null;
        RequiresFallback = false;
        UnsupportedTable = "";
        Message = "";
    }
}
