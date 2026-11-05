namespace svg;

internal class SvgOpenTypeReadState
{
    internal byte[]? Bytes;
    internal SvgOpenTypeReadOptions Options;
    internal SvgOpenTypeReadResult Result;

    internal SvgOpenTypeReadState(byte[]? bytes, SvgOpenTypeReadOptions options, SvgOpenTypeReadResult result)
    {
        Bytes = bytes;
        Options = options;
        Result = result;
    }
}
