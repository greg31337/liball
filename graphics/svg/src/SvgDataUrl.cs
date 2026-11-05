namespace svg;

internal class SvgDataMetadata
{
    internal bool Success;
    internal SvgDataUrlError Error;
    internal int ErrorOffset;
    internal SvgMediaType? MediaType;
    internal bool IsBase64;

    internal SvgDataMetadata()
    {
        Success = false;
        Error = SvgDataUrlError.InvalidMediaType;
        ErrorOffset = -1;
        MediaType = null;
        IsBase64 = false;
    }
}

internal class SvgDataBytes
{
    internal bool Success;
    internal SvgDataUrlError Error;
    internal int ErrorOffset;
    internal byte[] Bytes;

    internal SvgDataBytes()
    {
        Success = false;
        Error = SvgDataUrlError.InvalidPayload;
        ErrorOffset = -1;
        Bytes = System.Array.Empty<byte>();
    }
}

internal class SvgDataText
{
    internal bool Success;
    internal SvgDataUrlError Error;
    internal int ErrorOffset;
    internal string Value;

    internal SvgDataText()
    {
        Success = false;
        Error = SvgDataUrlError.InvalidMediaType;
        ErrorOffset = -1;
        Value = "";
    }
}
