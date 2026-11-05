using System;

namespace avif;

internal class IsoFullBox
{
    internal AvifResultCode Code;
    internal int Version;
    internal int Flags;
    internal long PayloadOffset;
    internal long PayloadSize;

    internal IsoFullBox(
        AvifResultCode code,
        int version,
        int flags,
        long payloadOffset,
        long payloadSize)
    {
        Code = code;
        Version = version;
        Flags = flags;
        PayloadOffset = payloadOffset;
        PayloadSize = payloadSize;
    }

    internal static IsoFullBox Parse(ReadOnlySpan<byte> source, IsoBox? box)
    {
        if (box == null)
        {
            return new IsoFullBox(AvifResultCode.InvalidArgument, 0, 0, 0, 0);
        }

        if (box.PayloadOffset < 0 || box.PayloadSize < 4)
        {
            return new IsoFullBox(AvifResultCode.InvalidContainer, 0, 0, 0, 0);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            box.PayloadOffset,
            0,
            4,
            source.Length);
        if (!range.Success)
        {
            return new IsoFullBox(range.Code, 0, 0, 0, 0);
        }

        int offset = (int)box.PayloadOffset;
        int version = source[offset];
        int flags = source[offset + 1] << 16
            | source[offset + 2] << 8
            | source[offset + 3];
        return new IsoFullBox(
            AvifResultCode.Success,
            version,
            flags,
            box.PayloadOffset + 4,
            box.PayloadSize - 4);
    }
}
