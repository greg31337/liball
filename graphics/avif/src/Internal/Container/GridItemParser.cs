using System;
using System.Buffers.Binary;

namespace avif;

internal class GridItemParseResult
{
    internal AvifResultCode Code;
    internal int Rows;
    internal int Columns;
    internal int OutputWidth;
    internal int OutputHeight;

    internal GridItemParseResult(AvifResultCode code)
    {
        Code = code;
    }
}

internal static class GridItemParser
{
    internal static GridItemParseResult Parse(
        byte[] payload,
        int dependencyCount,
        AvifSecurityLimits? limits)
    {
        if (payload == null || limits == null || dependencyCount < -1)
        {
            return new GridItemParseResult(AvifResultCode.InvalidArgument);
        }

        AvifResultCode limitCode = ResourceGuard.ValidateLimits(limits);
        if (limitCode != AvifResultCode.Success)
        {
            return new GridItemParseResult(limitCode);
        }
        if (payload.Length < 2)
        {
            return new GridItemParseResult(AvifResultCode.TruncatedData);
        }
        if (payload[0] != 0 || (payload[1] & 254) != 0)
        {
            return new GridItemParseResult(AvifResultCode.InvalidItem);
        }

        bool largeDimensions = (payload[1] & 1) != 0;
        int expectedLength = largeDimensions ? 12 : 8;
        if (payload.Length < expectedLength)
        {
            return new GridItemParseResult(AvifResultCode.TruncatedData);
        }
        if (payload.Length != expectedLength)
        {
            return new GridItemParseResult(AvifResultCode.InvalidItem);
        }

        int rows = payload[2] + 1;
        int columns = payload[3] + 1;
        int cellCount = rows * columns;
        if (dependencyCount >= 0 && dependencyCount != cellCount)
        {
            return new GridItemParseResult(AvifResultCode.InvalidItem);
        }
        if (cellCount > limits.MaxTileCount)
        {
            return new GridItemParseResult(AvifResultCode.LimitExceeded);
        }

        ulong outputWidth;
        ulong outputHeight;
        if (largeDimensions)
        {
            outputWidth = BinaryPrimitives.ReadUInt32BigEndian(
                payload.AsSpan(4, 4));
            outputHeight = BinaryPrimitives.ReadUInt32BigEndian(
                payload.AsSpan(8, 4));
        }
        else
        {
            outputWidth = BinaryPrimitives.ReadUInt16BigEndian(
                payload.AsSpan(4, 2));
            outputHeight = BinaryPrimitives.ReadUInt16BigEndian(
                payload.AsSpan(6, 2));
        }

        if (outputWidth == 0 || outputHeight == 0)
        {
            return new GridItemParseResult(AvifResultCode.InvalidItem);
        }
        if (outputWidth > int.MaxValue || outputHeight > int.MaxValue)
        {
            return new GridItemParseResult(AvifResultCode.LimitExceeded);
        }

        ResourceGuard guard = new ResourceGuard(limits);
        AvifResultCode dimensionsCode = guard.CheckImageDimensions(
            (int)outputWidth,
            (int)outputHeight);
        if (dimensionsCode != AvifResultCode.Success)
        {
            return new GridItemParseResult(dimensionsCode);
        }

        GridItemParseResult result = new GridItemParseResult(
            AvifResultCode.Success);
        result.Rows = rows;
        result.Columns = columns;
        result.OutputWidth = (int)outputWidth;
        result.OutputHeight = (int)outputHeight;
        return result;
    }
}
