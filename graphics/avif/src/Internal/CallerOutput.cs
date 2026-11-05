using System;
using System.IO;

namespace avif;

internal static class CallerOutput
{
    internal static AvifResultCode Copy(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (destination.Length < source.Length)
        {
            return AvifResultCode.BufferTooSmall;
        }
        source.CopyTo(destination);
        return AvifResultCode.Success;
    }

    internal static AvifResultCode Write(
        ReadOnlySpan<byte> source,
        Stream destination)
    {
        StreamOutput output = new StreamOutput(destination, true);
        AvifResultCode code = output.Code;
        if (code == AvifResultCode.Success)
        {
            code = output.Write(source);
        }
        if (code == AvifResultCode.Success)
        {
            code = output.Flush();
        }
        output.Close();
        return code;
    }
}
