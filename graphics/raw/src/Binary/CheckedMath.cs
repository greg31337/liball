namespace raw;

internal static class CheckedMath
{
    internal static long Add(long left, long right)
    {
        if (left < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The offset cannot be negative.", left);
        }
        if (right < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The length cannot be negative.", left);
        }
        if (left > long.MaxValue - right)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "Offset and length addition overflowed.", left);
        }
        return left + right;
    }

    internal static long Multiply(long count, long elementSize)
    {
        if (count < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The count cannot be negative.", -1);
        }
        if (elementSize < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The element size cannot be negative.", -1);
        }
        if (count != 0 && elementSize > long.MaxValue / count)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "Count and element-size multiplication overflowed.", -1);
        }
        return count * elementSize;
    }

    internal static long RangeEnd(long offset, long length, long boundary)
    {
        if (boundary < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The range boundary cannot be negative.", boundary);
        }
        long end = Add(offset, length);
        if (end > boundary)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The range extends beyond its permitted boundary.", offset);
        }
        return end;
    }

    internal static long FileSize(long length, RawLimits? limits)
    {
        RequireLimits(limits);
        RequirePositiveLimit((limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxFileSizeBytes, "The configured file-size limit must be greater than zero.");
        if (length < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The file size cannot be negative.", -1);
        }
        if (length > limits.MaxFileSizeBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The file size exceeds the configured limit.", -1);
        }
        return length;
    }

    internal static long PixelCount(int width, int height, RawLimits? limits)
    {
        RequireLimits(limits);
        RequirePositiveLimit((limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxWidth, "The configured width limit must be greater than zero.");
        RequirePositiveLimit(limits.MaxHeight, "The configured height limit must be greater than zero.");
        RequirePositiveLimit(limits.MaxPixelCount, "The configured pixel-count limit must be greater than zero.");
        if (width <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The image width must be greater than zero.", -1);
        }
        if (height <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The image height must be greater than zero.", -1);
        }
        if (width > limits.MaxWidth || height > limits.MaxHeight)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The image dimensions exceed the configured limits.", -1);
        }
        long pixels = Multiply(width, height);
        if (pixels > limits.MaxPixelCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The image pixel count exceeds the configured limit.", -1);
        }
        return pixels;
    }

    internal static long DecodedSize(long elementCount, long elementSize, RawLimits? limits)
    {
        RequireLimits(limits);
        RequirePositiveLimit((limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxDecodedBytes, "The configured decoded-data limit must be greater than zero.");
        long bytes = Multiply(elementCount, elementSize);
        if (bytes > limits.MaxDecodedBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The decoded byte count exceeds the configured limit.", -1);
        }
        return bytes;
    }

    internal static long AllocationSize(long elementCount, long elementSize, RawLimits? limits)
    {
        RequireLimits(limits);
        RequirePositiveLimit((limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxAllocationBytes, "The configured single-allocation limit must be greater than zero.");
        RequirePositiveLimit(limits.MaxDecodedBytes, "The configured decoded-data limit must be greater than zero.");
        long bytes = Multiply(elementCount, elementSize);
        if (bytes > limits.MaxAllocationBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The allocation size exceeds the configured single-allocation limit.", -1);
        }
        if (bytes > limits.MaxDecodedBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The allocation size exceeds the configured decoded-data limit.", -1);
        }
        return bytes;
    }

    internal static int ToInt32(long value)
    {
        if (value < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The value cannot be negative.", -1);
        }
        if (value > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "The value cannot be represented as a 32-bit signed integer.", -1);
        }
        return (int)value;
    }

    internal static long ToInt64(ulong value)
    {
        if (value > long.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "The value cannot be represented as a 64-bit signed file position.", -1);
        }
        return (long)value;
    }

    internal static void RequireLimits(RawLimits? limits)
    {
        if (limits == null)
        {
            throw new RawLimitException(RawDiagnosticCode.InvalidArgument, "Resource limits are required.", -1);
        }
    }

    internal static void RequirePositiveLimit(long value, string message)
    {
        if (value <= 0)
        {
            throw new RawLimitException(RawDiagnosticCode.InvalidArgument, message, -1);
        }
    }
}
