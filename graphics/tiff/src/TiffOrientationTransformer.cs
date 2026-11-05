using System;

namespace tiff;

internal class TiffOrientationTransformer
{
    internal static TiffOrientationTransformResult Transform(TiffImageInfo info, ReadOnlySpan<byte> source, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        return Transform(info, source, bytesPerPixel, new TiffLimits(), diagnostics);
    }

    internal static TiffOrientationTransformResult Transform(TiffImageInfo info, ReadOnlySpan<byte> source, int bytesPerPixel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for orientation transform.");
            return TiffOrientationTransformResult.Create(0, 0, bytesPerPixel, 0, 0, new byte[0], diagnostics);
        }

        return TransformInterleavedBytes(info.Width, info.Length, bytesPerPixel, info.Orientation, source, limits, diagnostics);
    }

    internal static TiffOrientationTransformResult TransformInterleavedBytes(ulong width, ulong length, int bytesPerPixel, TiffOrientation orientation, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TransformInterleavedBytes(width, length, bytesPerPixel, orientation, source, new TiffLimits(), diagnostics);
    }

    internal static TiffOrientationTransformResult TransformInterleavedBytes(ulong width, ulong length, int bytesPerPixel, TiffOrientation orientation, ReadOnlySpan<byte> source, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (!ValidateOrientationRequest(width, length, bytesPerPixel, orientation, source.Length, limits, diagnostics))
        {
            return TiffOrientationTransformResult.Create(CalculateOrientedWidth(width, length, orientation), CalculateOrientedLength(width, length, orientation), bytesPerPixel, 0, 0, new byte[0], diagnostics);
        }

        ulong outputWidth = CalculateOrientedWidth(width, length, orientation);
        ulong outputLength = CalculateOrientedLength(width, length, orientation);
        ulong outputRowByteCount = outputWidth * (ulong)bytesPerPixel;
        ulong outputByteCount = outputRowByteCount * outputLength;
        byte[] destination = new byte[(int)outputByteCount];

        ulong destinationY = 0;
        while (destinationY < outputLength)
        {
            ulong destinationX = 0;
            while (destinationX < outputWidth)
            {
                ulong sourceX = CalculateSourceX(width, length, destinationX, destinationY, orientation);
                ulong sourceY = CalculateSourceY(width, length, destinationX, destinationY, orientation);
                CopyPixel(source, destination, width, outputWidth, bytesPerPixel, sourceX, sourceY, destinationX, destinationY);
                destinationX = destinationX + 1;
            }

            destinationY = destinationY + 1;
        }

        return TiffOrientationTransformResult.Create(outputWidth, outputLength, bytesPerPixel, outputRowByteCount, outputByteCount, destination, diagnostics);
    }

    internal static bool ValidateOrientationRequest(ulong width, ulong length, int bytesPerPixel, TiffOrientation orientation, int sourceByteCount, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (width == 0 || length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Orientation transform requires positive image dimensions.");
            return false;
        }

        if (bytesPerPixel <= 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Orientation transform requires a positive byte count per pixel.");
            return false;
        }

        if (!IsSupportedOrientation(orientation))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Orientation transform requires a valid TIFF orientation value.", TiffDiagnostic.NoImageIndex, TiffTagId.Orientation);
            return false;
        }

        ulong byteCount = CalculateRequiredByteCount(width, length, bytesPerPixel, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (byteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Orientation transform exceeds the supported array range.");
            return false;
        }

        if ((ulong)sourceByteCount != byteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Orientation transform source byte count does not match dimensions and bytes per pixel.");
            return false;
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Orientation transform output exceeds the configured allocation limit.");
            return false;
        }

        return true;
    }

    internal static ulong CalculateRequiredByteCount(ulong width, ulong length, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        if ((ulong)bytesPerPixel != 0 && width > ulong.MaxValue / (ulong)bytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Orientation transform row byte count overflows the supported range.");
            return 0;
        }

        ulong rowByteCount = width * (ulong)bytesPerPixel;
        if (rowByteCount != 0 && length > ulong.MaxValue / rowByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Orientation transform byte count overflows the supported range.");
            return 0;
        }

        return rowByteCount * length;
    }

    internal static bool IsSupportedOrientation(TiffOrientation orientation)
    {
        if (orientation == TiffOrientation.TopLeft)
        {
            return true;
        }

        if (orientation == TiffOrientation.TopRight)
        {
            return true;
        }

        if (orientation == TiffOrientation.BottomRight)
        {
            return true;
        }

        if (orientation == TiffOrientation.BottomLeft)
        {
            return true;
        }

        if (orientation == TiffOrientation.LeftTop)
        {
            return true;
        }

        if (orientation == TiffOrientation.RightTop)
        {
            return true;
        }

        if (orientation == TiffOrientation.RightBottom)
        {
            return true;
        }

        if (orientation == TiffOrientation.LeftBottom)
        {
            return true;
        }

        return false;
    }

    internal static ulong CalculateOrientedWidth(ulong width, ulong length, TiffOrientation orientation)
    {
        if (SwapsAxes(orientation))
        {
            return length;
        }

        return width;
    }

    internal static ulong CalculateOrientedLength(ulong width, ulong length, TiffOrientation orientation)
    {
        if (SwapsAxes(orientation))
        {
            return width;
        }

        return length;
    }

    internal static bool SwapsAxes(TiffOrientation orientation)
    {
        if (orientation == TiffOrientation.LeftTop)
        {
            return true;
        }

        if (orientation == TiffOrientation.RightTop)
        {
            return true;
        }

        if (orientation == TiffOrientation.RightBottom)
        {
            return true;
        }

        if (orientation == TiffOrientation.LeftBottom)
        {
            return true;
        }

        return false;
    }

    internal static ulong CalculateSourceX(ulong width, ulong length, ulong destinationX, ulong destinationY, TiffOrientation orientation)
    {
        if (orientation == TiffOrientation.TopRight)
        {
            return width - 1 - destinationX;
        }

        if (orientation == TiffOrientation.BottomRight)
        {
            return width - 1 - destinationX;
        }

        if (orientation == TiffOrientation.LeftTop)
        {
            return destinationY;
        }

        if (orientation == TiffOrientation.RightTop)
        {
            return destinationY;
        }

        if (orientation == TiffOrientation.RightBottom)
        {
            return width - 1 - destinationY;
        }

        if (orientation == TiffOrientation.LeftBottom)
        {
            return width - 1 - destinationY;
        }

        return destinationX;
    }

    internal static ulong CalculateSourceY(ulong width, ulong length, ulong destinationX, ulong destinationY, TiffOrientation orientation)
    {
        if (orientation == TiffOrientation.BottomRight)
        {
            return length - 1 - destinationY;
        }

        if (orientation == TiffOrientation.BottomLeft)
        {
            return length - 1 - destinationY;
        }

        if (orientation == TiffOrientation.LeftTop)
        {
            return destinationX;
        }

        if (orientation == TiffOrientation.RightTop)
        {
            return length - 1 - destinationX;
        }

        if (orientation == TiffOrientation.RightBottom)
        {
            return length - 1 - destinationX;
        }

        if (orientation == TiffOrientation.LeftBottom)
        {
            return destinationX;
        }

        return destinationY;
    }

    internal static void CopyPixel(ReadOnlySpan<byte> source, byte[] destination, ulong sourceWidth, ulong destinationWidth, int bytesPerPixel, ulong sourceX, ulong sourceY, ulong destinationX, ulong destinationY)
    {
        ulong sourceOffset = (sourceY * sourceWidth + sourceX) * (ulong)bytesPerPixel;
        ulong destinationOffset = (destinationY * destinationWidth + destinationX) * (ulong)bytesPerPixel;
        int byteIndex = 0;
        while (byteIndex < bytesPerPixel)
        {
            destination[(int)destinationOffset + byteIndex] = source[(int)sourceOffset + byteIndex];
            byteIndex = byteIndex + 1;
        }
    }
}
