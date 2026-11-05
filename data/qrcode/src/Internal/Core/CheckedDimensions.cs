namespace qrcode;

internal class CheckedDimensions
{
    internal static void ValidateDimensionBounds(
        int width,
        int height,
        int maximumWidth,
        int maximumHeight,
        int maximumArea)
    {
        if (width <= 0)
        {
            throw new QrInputException("The width must be positive.", "width");
        }

        if (height <= 0)
        {
            throw new QrInputException("The height must be positive.", "height");
        }

        if (maximumWidth <= 0)
        {
            throw new QrInputException("The maximum width must be positive.", "maximumWidth");
        }

        if (maximumHeight <= 0)
        {
            throw new QrInputException("The maximum height must be positive.", "maximumHeight");
        }

        if (maximumArea <= 0)
        {
            throw new QrInputException("The maximum area must be positive.", "maximumArea");
        }

        if (width > maximumWidth)
        {
            throw new QrResourceLimitException(
                "The width exceeds the configured limit.",
                "MaximumWidth",
                width,
                maximumWidth);
        }

        if (height > maximumHeight)
        {
            throw new QrResourceLimitException(
                "The height exceeds the configured limit.",
                "MaximumHeight",
                height,
                maximumHeight);
        }

        CalculateArea(width, height, maximumArea);
    }

    internal static int CalculateArea(int width, int height, int maximumArea)
    {
        if (width <= 0)
        {
            throw new QrInputException("The width must be positive.", "width");
        }

        if (height <= 0)
        {
            throw new QrInputException("The height must be positive.", "height");
        }

        return CalculateProduct(width, height, maximumArea, "MaximumArea");
    }

    internal static int CalculateMinimumStride(int width, int bytesPerPixel)
    {
        if (width <= 0)
        {
            throw new QrInputException("The width must be positive.", "width");
        }

        if (bytesPerPixel <= 0)
        {
            throw new QrInputException("Bytes per pixel must be positive.", "bytesPerPixel");
        }

        return CalculateProduct(width, bytesPerPixel, int.MaxValue, "MinimumStride");
    }

    internal static void ValidateStride(int stride, int minimumStride)
    {
        if (stride <= 0)
        {
            throw new QrInputException("The stride must be positive.", "stride");
        }

        if (minimumStride <= 0)
        {
            throw new QrInputException("The minimum stride must be positive.", "minimumStride");
        }

        if (stride < minimumStride)
        {
            throw new QrInputException("The stride is smaller than one logical row.", "stride");
        }
    }

    internal static int CalculateRequiredBufferLength(int height, int stride, int minimumStride)
    {
        if (height <= 0)
        {
            throw new QrInputException("The height must be positive.", "height");
        }

        ValidateStride(stride, minimumStride);
        int precedingRowBytes = CalculateProduct(height - 1, stride, int.MaxValue, "BufferLength");
        return CalculateSum(precedingRowBytes, minimumStride, int.MaxValue, "BufferLength");
    }

    internal static int CalculateAllocationBytes(
        int elementCount,
        int elementSize,
        int maximumAllocationBytes)
    {
        if (elementCount < 0)
        {
            throw new QrInputException("The element count cannot be negative.", "elementCount");
        }

        if (elementSize <= 0)
        {
            throw new QrInputException("The element size must be positive.", "elementSize");
        }

        if (maximumAllocationBytes < 0)
        {
            throw new QrInputException(
                "The maximum allocation byte count cannot be negative.",
                "maximumAllocationBytes");
        }

        return CalculateProduct(
            elementCount,
            elementSize,
            maximumAllocationBytes,
            "MaximumAllocationBytes");
    }

    internal static void ValidateBufferLength(int requiredLength, int actualLength)
    {
        if (requiredLength < 0)
        {
            throw new QrInputException("The required buffer length cannot be negative.", "requiredLength");
        }

        if (actualLength < 0)
        {
            throw new QrInputException("The actual buffer length cannot be negative.", "actualLength");
        }

        if (actualLength < requiredLength)
        {
            throw new QrInputException("The supplied buffer is too short for the declared layout.", "actualLength");
        }
    }

    internal static int CalculateElementOffset(
        int row,
        int column,
        int width,
        int height,
        int stride,
        int elementSize,
        int bufferLength)
    {
        if (width <= 0)
        {
            throw new QrInputException("The width must be positive.", "width");
        }

        if (height <= 0)
        {
            throw new QrInputException("The height must be positive.", "height");
        }

        if (row < 0 || row >= height)
        {
            throw new QrInputException("The row is outside the declared layout.", "row");
        }

        if (column < 0 || column >= width)
        {
            throw new QrInputException("The column is outside the declared layout.", "column");
        }

        int minimumStride = CalculateMinimumStride(width, elementSize);
        ValidateStride(stride, minimumStride);
        int requiredLength = CalculateRequiredBufferLength(height, stride, minimumStride);
        ValidateBufferLength(requiredLength, bufferLength);

        int rowOffset = CalculateProduct(row, stride, int.MaxValue, "ElementOffset");
        int columnOffset = CalculateProduct(column, elementSize, int.MaxValue, "ElementOffset");
        int elementOffset = CalculateSum(rowOffset, columnOffset, int.MaxValue, "ElementOffset");
        int elementEnd = CalculateSum(elementOffset, elementSize, int.MaxValue, "ElementEnd");
        if (elementEnd > bufferLength)
        {
            throw new QrInputException("The element extends beyond the supplied buffer.", "bufferLength");
        }

        return elementOffset;
    }

    internal static int CalculateProduct(int left, int right, int maximumValue, string limitName)
    {
        if (left < 0)
        {
            throw new QrInputException("The left factor cannot be negative.", "left");
        }

        if (right < 0)
        {
            throw new QrInputException("The right factor cannot be negative.", "right");
        }

        if (maximumValue < 0)
        {
            throw new QrInputException("The maximum value cannot be negative.", "maximumValue");
        }

        long requestedValue = (long)left * (long)right;
        if (requestedValue > maximumValue)
        {
            int reportedValue = requestedValue > int.MaxValue ? int.MaxValue : (int)requestedValue;
            throw new QrResourceLimitException(
                "The product exceeds the permitted integer or configured limit.",
                limitName,
                reportedValue,
                maximumValue);
        }

        return (int)requestedValue;
    }

    internal static int CalculateSum(int left, int right, int maximumValue, string limitName)
    {
        if (left < 0)
        {
            throw new QrInputException("The left addend cannot be negative.", "left");
        }

        if (right < 0)
        {
            throw new QrInputException("The right addend cannot be negative.", "right");
        }

        if (maximumValue < 0)
        {
            throw new QrInputException("The maximum value cannot be negative.", "maximumValue");
        }

        long requestedValue = (long)left + (long)right;
        if (requestedValue > maximumValue)
        {
            int reportedValue = requestedValue > int.MaxValue ? int.MaxValue : (int)requestedValue;
            throw new QrResourceLimitException(
                "The sum exceeds the permitted integer or configured limit.",
                limitName,
                reportedValue,
                maximumValue);
        }

        return (int)requestedValue;
    }

    internal static int CalculateRequiredCapacityBits(
        int modeHeaderBitCount,
        int characterCountBitCount,
        int controlBitCount,
        int payloadBitCount)
    {
        int headerBitCount = CalculateSum(
            modeHeaderBitCount,
            characterCountBitCount,
            int.MaxValue,
            "CapacityBitCount");
        int completeHeaderBitCount = CalculateSum(
            headerBitCount,
            controlBitCount,
            int.MaxValue,
            "CapacityBitCount");
        return CalculateSum(
            completeHeaderBitCount,
            payloadBitCount,
            int.MaxValue,
            "CapacityBitCount");
    }

    internal static int CalculateCodewordCapacityBits(int codewordCount, int remainderBitCount)
    {
        if (remainderBitCount < 0 || remainderBitCount > 7)
        {
            throw new QrInputException(
                "The remainder bit count must be between zero and seven.",
                "remainderBitCount");
        }

        int codewordBitCount = CalculateProduct(
            codewordCount,
            8,
            int.MaxValue,
            "CapacityBitCount");
        return CalculateSum(
            codewordBitCount,
            remainderBitCount,
            int.MaxValue,
            "CapacityBitCount");
    }
}
