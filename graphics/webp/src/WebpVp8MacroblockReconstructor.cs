using System;

namespace webp;

internal static class WebpVp8MacroblockReconstructor
{
    internal static void ReconstructLumaMacroblock(byte[] destination, int macroblockX, int macroblockY, int stride, byte[] prediction, int[][] residualBlocks)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(prediction);
        ArgumentNullException.ThrowIfNull(residualBlocks);
        ValidateMacroblockPosition(macroblockX, macroblockY);
        ValidateResidualBlockArray(residualBlocks, 16, "residualBlocks");
        ValidatePrediction(prediction, 16, "prediction");

        int blockIndex = 0;
        while (blockIndex < 16)
        {
            int blockX = blockIndex & 3;
            int blockY = blockIndex >> 2;
            int destinationOffset = ((macroblockY * 16) + (blockY * 4)) * stride + (macroblockX * 16) + (blockX * 4);
            int predictionOffset = (blockY * 4 * 16) + (blockX * 4);
            WebpVp8InverseTransform.AddResidualToBlock(destination, destinationOffset, stride, prediction, predictionOffset, 16, residualBlocks[blockIndex]);
            blockIndex = blockIndex + 1;
        }
    }

    internal static void ReconstructChromaMacroblock(byte[] destination, int macroblockX, int macroblockY, int stride, byte[] prediction, int[][] residualBlocks)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(prediction);
        ArgumentNullException.ThrowIfNull(residualBlocks);
        ValidateMacroblockPosition(macroblockX, macroblockY);
        ValidateResidualBlockArray(residualBlocks, 4, "residualBlocks");
        ValidatePrediction(prediction, 8, "prediction");

        int blockIndex = 0;
        while (blockIndex < 4)
        {
            int blockX = blockIndex & 1;
            int blockY = blockIndex >> 1;
            int destinationOffset = ((macroblockY * 8) + (blockY * 4)) * stride + (macroblockX * 8) + (blockX * 4);
            int predictionOffset = (blockY * 4 * 8) + (blockX * 4);
            WebpVp8InverseTransform.AddResidualToBlock(destination, destinationOffset, stride, prediction, predictionOffset, 8, residualBlocks[blockIndex]);
            blockIndex = blockIndex + 1;
        }
    }

    internal static void PadPlaneEdges(byte[] plane, int width, int height, int stride, int paddedWidth, int paddedHeight)
    {
        ArgumentNullException.ThrowIfNull(plane);
        ValidatePlaneDimensions(width, height, stride, paddedWidth, paddedHeight);
        ValidatePlaneBounds(plane, stride, paddedHeight, "plane");

        int row = 0;
        while (row < height)
        {
            byte rightPixel = plane[(row * stride) + width - 1];
            int column = width;
            while (column < paddedWidth)
            {
                plane[(row * stride) + column] = rightPixel;
                column = column + 1;
            }

            row = row + 1;
        }

        int sourceRowOffset = (height - 1) * stride;
        row = height;
        while (row < paddedHeight)
        {
            int destinationRowOffset = row * stride;
            int column = 0;
            while (column < paddedWidth)
            {
                plane[destinationRowOffset + column] = plane[sourceRowOffset + column];
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static int MacroblockAlignedLumaSize(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        return ((value + 15) / 16) * 16;
    }

    internal static int MacroblockAlignedChromaSize(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        return ((value + 7) / 8) * 8;
    }

    internal static void ValidateMacroblockPosition(int macroblockX, int macroblockY)
    {
        if (macroblockX < 0)
        {
            throw new ArgumentOutOfRangeException("macroblockX");
        }

        if (macroblockY < 0)
        {
            throw new ArgumentOutOfRangeException("macroblockY");
        }
    }

    internal static void ValidateResidualBlockArray(int[][] residualBlocks, int expectedLength, string name)
    {
        if (residualBlocks.Length != expectedLength)
        {
            throw new ArgumentException("VP8 macroblock residual block count is invalid", name);
        }

        int index = 0;
        while (index < residualBlocks.Length)
        {
            if (residualBlocks[index] == null || residualBlocks[index].Length != 16)
            {
                throw new ArgumentException("VP8 residual blocks must contain 16 coefficients", name);
            }

            index = index + 1;
        }
    }

    internal static void ValidatePrediction(byte[] prediction, int size, string name)
    {
        int required = size * size;
        if (prediction.Length < required)
        {
            throw new ArgumentException("VP8 prediction block is too small", name);
        }
    }

    internal static void ValidatePlaneDimensions(int width, int height, int stride, int paddedWidth, int paddedHeight)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        if (paddedWidth < width)
        {
            throw new ArgumentOutOfRangeException("paddedWidth");
        }

        if (paddedHeight < height)
        {
            throw new ArgumentOutOfRangeException("paddedHeight");
        }

        if (stride < paddedWidth)
        {
            throw new ArgumentOutOfRangeException("stride");
        }
    }

    internal static void ValidatePlaneBounds(byte[] plane, int stride, int paddedHeight, string name)
    {
        int required = stride * paddedHeight;
        if (plane.Length < required)
        {
            throw new ArgumentException("VP8 plane buffer is too small", name);
        }
    }
}
