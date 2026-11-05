namespace svg;

/// <summary>Contains a decoded RFC 2397 data URL and its media metadata.</summary>
public class SvgDataUrlResult
{
    /// <summary>Whether parsing completed successfully.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgDataUrlError Error;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The parsed or default media type.</summary>
    public SvgMediaType? MediaType;
    /// <summary>The effective charset parameter, or an empty string.</summary>
    public string Charset;
    /// <summary>Whether the payload used base64 transfer encoding.</summary>
    public bool IsBase64;
    /// <summary>The decoded payload bytes.</summary>
    public byte[] Bytes;
    /// <summary>The original data URL.</summary>
    public string Source;
    /// <summary>The payload starting offset after the comma.</summary>
    public int DataOffset;

    internal SvgDataUrlResult()
    {
        Success = false;
        Error = SvgDataUrlError.InvalidArgument;
        ErrorOffset = -1;
        MediaType = null;
        Charset = "";
        IsBase64 = false;
        Bytes = System.Array.Empty<byte>();
        Source = "";
        DataOffset = -1;
    }
}

/// <summary>Contains a deterministically encoded data URL or a stable failure.</summary>
public class SvgDataUrlEncodeResult
{
    /// <summary>Whether encoding completed successfully.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgDataUrlError Error;
    /// <summary>The encoded data URL.</summary>
    public string DataUrl;
    /// <summary>The parsed or default media type.</summary>
    public SvgMediaType? MediaType;
    /// <summary>Whether base64 transfer encoding was used.</summary>
    public bool IsBase64;

    internal SvgDataUrlEncodeResult()
    {
        Success = false;
        Error = SvgDataUrlError.InvalidArgument;
        DataUrl = "";
        MediaType = null;
        IsBase64 = false;
    }
}
