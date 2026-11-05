using System;
using System.Numerics;

namespace avif;

internal class ImageTransformGeometry
{
    internal AvifResultCode Code;
    internal int CropX;
    internal int CropY;
    internal int CropWidth;
    internal int CropHeight;
    internal int Rotation;
    internal int RotatedWidth;
    internal int RotatedHeight;
    internal int MirrorAxis;
    internal int ExifOrientation;
    internal int OutputWidth;
    internal int OutputHeight;

    internal ImageTransformGeometry(AvifResultCode code)
    {
        Code = code;
        MirrorAxis = -1;
        ExifOrientation = 1;
    }
}

internal class ImageTransformCoordinate
{
    internal int X;
    internal int Y;

    internal ImageTransformCoordinate(int x, int y)
    {
        X = x;
        Y = y;
    }
}

internal static class ImageTransformer
{
    internal static AvifPixelBuffer Transform(
        AvifPixelBuffer source,
        AvifTransform transform,
        AvifRenderOptions options)
    {
        if (source == null || transform == null || options == null
            || options.Limits == null)
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }

        AvifResultCode sourceCode = source.GetCode();
        if (sourceCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(sourceCode);
        }

        ImageTransformGeometry geometry = ResolveGeometry(
            source.GetWidth(),
            source.GetHeight(),
            transform,
            options);
        if (geometry.Code != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(geometry.Code);
        }

        AvifPixelBuffer output = AvifPixelBuffer.Create(
            geometry.OutputWidth,
            geometry.OutputHeight,
            source.GetPixelFormat(),
            source.GetBitDepth(),
            source.IsFloatingPoint(),
            1,
            options.Limits);
        if (output.GetCode() != AvifResultCode.Success)
        {
            return output;
        }

        int bytesPerPixel = source.GetBytesPerPixel();
        int outputY = 0;
        while (outputY < geometry.OutputHeight)
        {
            Span<byte> outputRow = output.GetRowSpan(outputY);
            int outputX = 0;
            while (outputX < geometry.OutputWidth)
            {
                ImageTransformCoordinate sourceCoordinate = MapToSource(
                    outputX,
                    outputY,
                    geometry);
                source.GetRowSpan(sourceCoordinate.Y).Slice(
                    sourceCoordinate.X * bytesPerPixel,
                    bytesPerPixel).CopyTo(outputRow.Slice(
                        outputX * bytesPerPixel,
                        bytesPerPixel));
                outputX++;
            }

            outputY++;
        }

        return output;
    }

    static ImageTransformGeometry ResolveGeometry(
        int sourceWidth,
        int sourceHeight,
        AvifTransform transform,
        AvifRenderOptions options)
    {
        ImageTransformGeometry geometry = new ImageTransformGeometry(
            AvifResultCode.Success);
        geometry.CropX = 0;
        geometry.CropY = 0;
        geometry.CropWidth = sourceWidth;
        geometry.CropHeight = sourceHeight;
        if (options.ApplyCleanAperture
            && transform.GetCleanApertureValue() != null)
        {
            AvifResultCode cropCode = ResolveCrop(
                geometry,
                sourceWidth,
                sourceHeight,
                transform.GetCleanApertureValue());
            if (cropCode != AvifResultCode.Success)
            {
                geometry.Code = cropCode;
                return geometry;
            }
        }

        geometry.Rotation = options.ApplyRotation
            && transform.GetRotationValue() >= 0
            ? transform.GetRotationValue()
            : 0;
        bool rotationExchangesAxes = (geometry.Rotation & 1) != 0;
        geometry.RotatedWidth = rotationExchangesAxes
            ? geometry.CropHeight
            : geometry.CropWidth;
        geometry.RotatedHeight = rotationExchangesAxes
            ? geometry.CropWidth
            : geometry.CropHeight;
        geometry.MirrorAxis = options.ApplyMirror
            ? transform.GetMirrorValue()
            : -1;
        geometry.ExifOrientation = options.ApplyExifOrientation
            && transform.GetExifOrientationValue() != 0
            ? transform.GetExifOrientationValue()
            : 1;
        bool exifExchangesAxes = geometry.ExifOrientation >= 5
            && geometry.ExifOrientation <= 8;
        geometry.OutputWidth = exifExchangesAxes
            ? geometry.RotatedHeight
            : geometry.RotatedWidth;
        geometry.OutputHeight = exifExchangesAxes
            ? geometry.RotatedWidth
            : geometry.RotatedHeight;
        return geometry;
    }

    static AvifResultCode ResolveCrop(
        ImageTransformGeometry geometry,
        int sourceWidth,
        int sourceHeight,
        AvifCrop? crop)
    {
        BigInteger widthNumerator = (crop ?? throw new InvalidOperationException("Invalid internal state.")).GetWidthNumerator();
        BigInteger widthDenominator = crop.GetWidthDenominator();
        BigInteger heightNumerator = crop.GetHeightNumerator();
        BigInteger heightDenominator = crop.GetHeightDenominator();
        if (widthNumerator % widthDenominator != 0
            || heightNumerator % heightDenominator != 0)
        {
            return AvifResultCode.InvalidMetadata;
        }

        BigInteger cropWidth = widthNumerator / widthDenominator;
        BigInteger cropHeight = heightNumerator / heightDenominator;
        BigInteger horizontalOffsetDenominator =
            crop.GetHorizontalOffsetDenominator();
        BigInteger verticalOffsetDenominator =
            crop.GetVerticalOffsetDenominator();
        BigInteger leftDenominator = 2
            * widthDenominator
            * horizontalOffsetDenominator;
        BigInteger topDenominator = 2
            * heightDenominator
            * verticalOffsetDenominator;
        BigInteger leftNumerator =
            ((BigInteger)sourceWidth * widthDenominator - widthNumerator)
                * horizontalOffsetDenominator
            + 2
                * crop.GetHorizontalOffsetNumerator()
                * widthDenominator;
        BigInteger topNumerator =
            ((BigInteger)sourceHeight * heightDenominator - heightNumerator)
                * verticalOffsetDenominator
            + 2
                * crop.GetVerticalOffsetNumerator()
                * heightDenominator;
        if (leftNumerator % leftDenominator != 0
            || topNumerator % topDenominator != 0)
        {
            return AvifResultCode.InvalidMetadata;
        }

        BigInteger left = leftNumerator / leftDenominator;
        BigInteger top = topNumerator / topDenominator;
        if (cropWidth <= 0
            || cropHeight <= 0
            || left < 0
            || top < 0
            || cropWidth > int.MaxValue
            || cropHeight > int.MaxValue
            || left > int.MaxValue
            || top > int.MaxValue
            || left + cropWidth > sourceWidth
            || top + cropHeight > sourceHeight)
        {
            return AvifResultCode.InvalidMetadata;
        }

        geometry.CropX = (int)left;
        geometry.CropY = (int)top;
        geometry.CropWidth = (int)cropWidth;
        geometry.CropHeight = (int)cropHeight;
        return AvifResultCode.Success;
    }

    static ImageTransformCoordinate MapToSource(
        int outputX,
        int outputY,
        ImageTransformGeometry geometry)
    {
        ImageTransformCoordinate coordinate = UndoExifOrientation(
            outputX,
            outputY,
            geometry.RotatedWidth,
            geometry.RotatedHeight,
            geometry.ExifOrientation);
        if (geometry.MirrorAxis == 0)
        {
            coordinate.Y = geometry.RotatedHeight - 1 - coordinate.Y;
        }
        else if (geometry.MirrorAxis == 1)
        {
            coordinate.X = geometry.RotatedWidth - 1 - coordinate.X;
        }

        int rotatedX = coordinate.X;
        int rotatedY = coordinate.Y;
        if (geometry.Rotation == 1)
        {
            coordinate.X = geometry.CropWidth - 1 - rotatedY;
            coordinate.Y = rotatedX;
        }
        else if (geometry.Rotation == 2)
        {
            coordinate.X = geometry.CropWidth - 1 - rotatedX;
            coordinate.Y = geometry.CropHeight - 1 - rotatedY;
        }
        else if (geometry.Rotation == 3)
        {
            coordinate.X = rotatedY;
            coordinate.Y = geometry.CropHeight - 1 - rotatedX;
        }

        coordinate.X += geometry.CropX;
        coordinate.Y += geometry.CropY;
        return coordinate;
    }

    static ImageTransformCoordinate UndoExifOrientation(
        int outputX,
        int outputY,
        int sourceWidth,
        int sourceHeight,
        int orientation)
    {
        if (orientation == 2)
        {
            return new ImageTransformCoordinate(
                sourceWidth - 1 - outputX,
                outputY);
        }
        if (orientation == 3)
        {
            return new ImageTransformCoordinate(
                sourceWidth - 1 - outputX,
                sourceHeight - 1 - outputY);
        }
        if (orientation == 4)
        {
            return new ImageTransformCoordinate(
                outputX,
                sourceHeight - 1 - outputY);
        }
        if (orientation == 5)
        {
            return new ImageTransformCoordinate(outputY, outputX);
        }
        if (orientation == 6)
        {
            return new ImageTransformCoordinate(
                outputY,
                sourceHeight - 1 - outputX);
        }
        if (orientation == 7)
        {
            return new ImageTransformCoordinate(
                sourceWidth - 1 - outputY,
                sourceHeight - 1 - outputX);
        }
        if (orientation == 8)
        {
            return new ImageTransformCoordinate(
                sourceWidth - 1 - outputY,
                outputX);
        }
        return new ImageTransformCoordinate(outputX, outputY);
    }
}
