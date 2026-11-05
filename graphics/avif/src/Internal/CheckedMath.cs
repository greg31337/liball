namespace avif;

internal class CheckedIntegerResult
{
    internal bool Success;
    internal long Value;
    internal AvifResultCode Code;

    internal CheckedIntegerResult(bool success, long value, AvifResultCode code)
    {
        Success = success;
        Value = value;
        Code = code;
    }
}

internal class CheckedRangeResult
{
    internal bool Success;
    internal long Start;
    internal long Length;
    internal long End;
    internal AvifResultCode Code;

    internal CheckedRangeResult(
        bool success,
        long start,
        long length,
        long end,
        AvifResultCode code)
    {
        Success = success;
        Start = start;
        Length = length;
        End = end;
        Code = code;
    }
}

internal static class CheckedMath
{
    internal static CheckedIntegerResult AddInt32(int left, int right)
    {
        long value = (long)left + right;
        if (value < int.MinValue || value > int.MaxValue)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return new CheckedIntegerResult(true, value, AvifResultCode.Success);
    }

    internal static CheckedIntegerResult SubtractInt32(int left, int right)
    {
        long value = (long)left - right;
        if (value < int.MinValue || value > int.MaxValue)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return new CheckedIntegerResult(true, value, AvifResultCode.Success);
    }

    internal static CheckedIntegerResult MultiplyInt32(int left, int right)
    {
        long value = (long)left * right;
        if (value < int.MinValue || value > int.MaxValue)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return new CheckedIntegerResult(true, value, AvifResultCode.Success);
    }

    internal static CheckedIntegerResult AddInt64(long left, long right)
    {
        if (right > 0 && left > long.MaxValue - right)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        if (right < 0 && left < long.MinValue - right)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return new CheckedIntegerResult(true, left + right, AvifResultCode.Success);
    }

    internal static CheckedIntegerResult SubtractInt64(long left, long right)
    {
        if (right > 0 && left < long.MinValue + right)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        if (right < 0 && left > long.MaxValue + right)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return new CheckedIntegerResult(true, left - right, AvifResultCode.Success);
    }

    internal static CheckedIntegerResult MultiplyInt64(long left, long right)
    {
        if (left == 0 || right == 0)
        {
            return new CheckedIntegerResult(true, 0, AvifResultCode.Success);
        }

        if (left > 0)
        {
            if (right > 0 && left > long.MaxValue / right)
            {
                return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
            }

            if (right < 0 && right < long.MinValue / left)
            {
                return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
            }
        }
        else
        {
            if (right > 0 && left < long.MinValue / right)
            {
                return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
            }

            if (right < 0 && left < long.MaxValue / right)
            {
                return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
            }
        }

        return new CheckedIntegerResult(true, left * right, AvifResultCode.Success);
    }

    internal static CheckedRangeResult ResolveRange(
        long baseOffset,
        long relativeOffset,
        long length,
        long sourceLength)
    {
        if (baseOffset < 0 || relativeOffset < 0 || length < 0 || sourceLength < 0)
        {
            return new CheckedRangeResult(false, 0, 0, 0, AvifResultCode.InvalidArgument);
        }

        CheckedIntegerResult start = AddInt64(baseOffset, relativeOffset);
        if (!start.Success)
        {
            return new CheckedRangeResult(false, 0, 0, 0, start.Code);
        }

        CheckedIntegerResult end = AddInt64(start.Value, length);
        if (!end.Success)
        {
            return new CheckedRangeResult(false, 0, 0, 0, end.Code);
        }

        if (start.Value > sourceLength || end.Value > sourceLength)
        {
            return new CheckedRangeResult(false, start.Value, length, end.Value, AvifResultCode.TruncatedData);
        }

        return new CheckedRangeResult(true, start.Value, length, end.Value, AvifResultCode.Success);
    }

    internal static CheckedIntegerResult ComputeStride(
        int width,
        int samplesPerPixel,
        int bytesPerSample,
        int alignment,
        long maxBytes)
    {
        if (width <= 0 || samplesPerPixel <= 0 || bytesPerSample <= 0 || alignment <= 0 || maxBytes < 0)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.InvalidArgument);
        }

        CheckedIntegerResult sampleCount = MultiplyInt64(width, samplesPerPixel);
        if (!sampleCount.Success)
        {
            return sampleCount;
        }

        CheckedIntegerResult packedBytes = MultiplyInt64(sampleCount.Value, bytesPerSample);
        if (!packedBytes.Success)
        {
            return packedBytes;
        }

        CheckedIntegerResult alignmentInput = AddInt64(packedBytes.Value, alignment - 1);
        if (!alignmentInput.Success)
        {
            return alignmentInput;
        }

        long value = alignmentInput.Value / alignment * alignment;
        if (value > int.MaxValue || value > maxBytes)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return new CheckedIntegerResult(true, value, AvifResultCode.Success);
    }

    internal static CheckedIntegerResult ComputePlaneSize(int stride, int height, long maxBytes)
    {
        if (stride <= 0 || height <= 0 || maxBytes < 0)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.InvalidArgument);
        }

        CheckedIntegerResult size = MultiplyInt64(stride, height);
        if (!size.Success)
        {
            return size;
        }

        if (size.Value > maxBytes)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return size;
    }

    internal static CheckedIntegerResult ComputePixelCount(int width, int height, long maxPixels)
    {
        if (width <= 0 || height <= 0 || maxPixels < 0)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.InvalidArgument);
        }

        CheckedIntegerResult count = MultiplyInt64(width, height);
        if (!count.Success)
        {
            return count;
        }

        if (count.Value > maxPixels)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return count;
    }

    internal static CheckedIntegerResult ComputeDuration(
        long unitCount,
        long durationPerUnit,
        long maxDuration)
    {
        if (unitCount < 0 || durationPerUnit < 0 || maxDuration < 0)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.InvalidArgument);
        }

        CheckedIntegerResult duration = MultiplyInt64(unitCount, durationPerUnit);
        if (!duration.Success)
        {
            return duration;
        }

        if (duration.Value > maxDuration)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return duration;
    }

    internal static CheckedIntegerResult ComputeAllocationSize(
        long elementCount,
        int elementSize,
        long maxBytes)
    {
        if (elementCount < 0 || elementSize <= 0 || maxBytes < 0)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.InvalidArgument);
        }

        CheckedIntegerResult size = MultiplyInt64(elementCount, elementSize);
        if (!size.Success)
        {
            return size;
        }

        if (size.Value > System.Array.MaxLength || size.Value > maxBytes)
        {
            return new CheckedIntegerResult(false, 0, AvifResultCode.LimitExceeded);
        }

        return size;
    }
}
