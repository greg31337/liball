using System.Collections.Generic;

namespace image;

internal class MetadataByteRange
{
    internal long Offset;
    internal long Length;
    internal long EndOffset;
    internal bool IsValid;

    internal MetadataByteRange()
    {
        Offset = -1;
        Length = 0;
        EndOffset = -1;
        IsValid = false;
    }

    internal static MetadataByteRange Create(long offset, long length, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        if (offset < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "range.offset.negative", "Byte range offset was negative.", blockType, offset, length);
            return new MetadataByteRange();
        }

        if (length < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "range.length.negative", "Byte range length was negative.", blockType, offset, length);
            return new MetadataByteRange();
        }

        long endOffset = AddNonNegativeOffset(offset, length, diagnostics, "range.end", blockType);

        if (endOffset < 0)
        {
            return new MetadataByteRange();
        }

        MetadataByteRange range = new MetadataByteRange();
        range.Offset = offset;
        range.Length = length;
        range.EndOffset = endOffset;
        range.IsValid = true;
        return range;
    }

    internal static long AddNonNegativeOffset(long left, long right, List<MetadataDiagnostic> diagnostics, string operation, string blockType)
    {
        if (left < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, operation + ".left.negative", "Left offset operand was negative.", blockType, left, right);
            return -1;
        }

        if (right < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, operation + ".right.negative", "Right offset operand was negative.", blockType, left, right);
            return -1;
        }

        return AddOffset(left, right, diagnostics, operation, blockType);
    }

    internal static long AddOffset(long left, long right, List<MetadataDiagnostic> diagnostics, string operation, string blockType)
    {
        if (right > 0 && left > long.MaxValue - right)
        {
            MetadataDiagnostic.AddLimitExceeded(diagnostics, operation + ".overflow", "Offset addition overflowed.", blockType, left, right);
            return -1;
        }

        if (right < 0)
        {
            if (right == long.MinValue)
            {
                if (left < 0)
                {
                    MetadataDiagnostic.AddLimitExceeded(diagnostics, operation + ".underflow", "Offset addition underflowed.", blockType, left, right);
                    return -1;
                }
            }
            else
            {
                long magnitude = 0 - right;
                if (left < long.MinValue + magnitude)
                {
                    MetadataDiagnostic.AddLimitExceeded(diagnostics, operation + ".underflow", "Offset addition underflowed.", blockType, left, right);
                    return -1;
                }
            }
        }

        return left + right;
    }

    internal static bool IsRangeWithin(long containerOffset, long containerLength, long childOffset, long childLength, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        MetadataByteRange container = Create(containerOffset, containerLength, diagnostics, blockType);
        MetadataByteRange child = Create(childOffset, childLength, diagnostics, blockType);

        if (!container.IsValid || !child.IsValid)
        {
            return false;
        }

        if (child.Offset < container.Offset)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "range.child.before.container", "Child byte range started before the container byte range.", blockType, childOffset, childLength);
            return false;
        }

        if (child.EndOffset > container.EndOffset)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "range.child.after.container", "Child byte range exceeded the container byte range.", blockType, childOffset, childLength);
            return false;
        }

        return true;
    }

    internal static bool IsOffsetWithin(long containerOffset, long containerLength, long offset, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        MetadataByteRange container = Create(containerOffset, containerLength, diagnostics, blockType);

        if (!container.IsValid)
        {
            return false;
        }

        if (offset < container.Offset || offset >= container.EndOffset)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "range.offset.outside", "Offset was outside the container byte range.", blockType, offset, 0);
            return false;
        }

        return true;
    }

    internal static long ClampLengthToRange(long containerOffset, long containerLength, long childOffset, long childLength, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        MetadataByteRange container = Create(containerOffset, containerLength, diagnostics, blockType);
        MetadataByteRange child = Create(childOffset, childLength, diagnostics, blockType);

        if (!container.IsValid || !child.IsValid)
        {
            return 0;
        }

        if (child.Offset < container.Offset)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "range.clamp.before.container", "Cannot clamp a child range that starts before the container.", blockType, childOffset, childLength);
            return 0;
        }

        if (child.EndOffset <= container.EndOffset)
        {
            return child.Length;
        }

        MetadataDiagnostic.AddTruncated(diagnostics, "range.clamp.after.container", "Child byte range length was clamped to the container.", blockType, childOffset, childLength);
        return container.EndOffset - child.Offset;
    }
}
