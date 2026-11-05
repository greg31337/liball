namespace jpeg;

internal class JpegCoefficientQuantizer
{
    internal int[] QuantizeBlock(int[] coefficients, JpegQuantizationTable table)
    {
        ValidateBlock(coefficients, "coefficients");
        ValidateNaturalTable(table);

        int[] quantizedCoefficients = new int[JpegConstants.BlockSampleCount];
        CopyQuantizedBlock(coefficients, table, quantizedCoefficients);
        return quantizedCoefficients;
    }

    internal int[] DequantizeBlock(int[] coefficients, JpegQuantizationTable table)
    {
        ValidateBlock(coefficients, "coefficients");
        ValidateNaturalTable(table);

        int[] dequantizedCoefficients = new int[JpegConstants.BlockSampleCount];
        CopyDequantizedBlock(coefficients, table, dequantizedCoefficients);
        return dequantizedCoefficients;
    }

    internal int[] QuantizeZigZagBlock(int[] coefficients, JpegQuantizationTable table)
    {
        ValidateBlock(coefficients, "coefficients");
        ValidateTable(table);

        int[] quantizedCoefficients = new int[JpegConstants.BlockSampleCount];
        CopyQuantizedZigZagBlock(coefficients, table, quantizedCoefficients);
        return quantizedCoefficients;
    }

    internal int[] DequantizeZigZagBlock(int[] coefficients, JpegQuantizationTable table)
    {
        ValidateBlock(coefficients, "coefficients");
        ValidateTable(table);

        int[] dequantizedCoefficients = new int[JpegConstants.BlockSampleCount];
        CopyDequantizedZigZagBlock(coefficients, table, dequantizedCoefficients);
        return dequantizedCoefficients;
    }

    internal void CopyQuantizedBlock(int[] source, JpegQuantizationTable table, int[] destination)
    {
        ValidateBlock(source, "source");
        ValidateBlock(destination, "destination");
        ValidateNaturalTable(table);

        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            int quantizationValue = GetNaturalQuantizationValue(table, index);
            destination[index] = QuantizeCoefficient(source[index], quantizationValue);
            index = index + 1;
        }
    }

    internal void CopyDequantizedBlock(int[] source, JpegQuantizationTable table, int[] destination)
    {
        ValidateBlock(source, "source");
        ValidateBlock(destination, "destination");
        ValidateNaturalTable(table);

        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            int quantizationValue = GetNaturalQuantizationValue(table, index);
            destination[index] = DequantizeCoefficient(source[index], quantizationValue);
            index = index + 1;
        }
    }

    internal void CopyQuantizedZigZagBlock(int[] source, JpegQuantizationTable table, int[] destination)
    {
        ValidateBlock(source, "source");
        ValidateBlock(destination, "destination");
        ValidateTable(table);

        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            int quantizationValue = GetZigZagQuantizationValue(table, index);
            destination[index] = QuantizeCoefficient(source[index], quantizationValue);
            index = index + 1;
        }
    }

    internal void CopyDequantizedZigZagBlock(int[] source, JpegQuantizationTable table, int[] destination)
    {
        ValidateBlock(source, "source");
        ValidateBlock(destination, "destination");
        ValidateTable(table);

        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            int quantizationValue = GetZigZagQuantizationValue(table, index);
            destination[index] = DequantizeCoefficient(source[index], quantizationValue);
            index = index + 1;
        }
    }

    internal void QuantizeBlockInPlace(int[] coefficients, JpegQuantizationTable table)
    {
        int[] quantizedCoefficients = QuantizeBlock(coefficients, table);
        CopyFirstBlock(quantizedCoefficients, coefficients);
    }

    internal void DequantizeBlockInPlace(int[] coefficients, JpegQuantizationTable table)
    {
        int[] dequantizedCoefficients = DequantizeBlock(coefficients, table);
        CopyFirstBlock(dequantizedCoefficients, coefficients);
    }

    internal void QuantizeZigZagBlockInPlace(int[] coefficients, JpegQuantizationTable table)
    {
        int[] quantizedCoefficients = QuantizeZigZagBlock(coefficients, table);
        CopyFirstBlock(quantizedCoefficients, coefficients);
    }

    internal void DequantizeZigZagBlockInPlace(int[] coefficients, JpegQuantizationTable table)
    {
        int[] dequantizedCoefficients = DequantizeZigZagBlock(coefficients, table);
        CopyFirstBlock(dequantizedCoefficients, coefficients);
    }

    internal int QuantizeCoefficient(int coefficient, int quantizationValue)
    {
        ValidateQuantizationValue(quantizationValue);

        long magnitude = coefficient;
        bool isNegative = false;
        if (magnitude < 0)
        {
            isNegative = true;
            magnitude = 0 - magnitude;
        }

        long quantizedValue = (magnitude + quantizationValue / 2) / quantizationValue;
        if (isNegative)
        {
            quantizedValue = 0 - quantizedValue;
        }

        return ClampToInt32(quantizedValue);
    }

    internal int DequantizeCoefficient(int coefficient, int quantizationValue)
    {
        ValidateQuantizationValue(quantizationValue);
        long dequantizedValue = (long)coefficient * (long)quantizationValue;
        return ClampToInt32(dequantizedValue);
    }

    internal int GetNaturalQuantizationValue(JpegQuantizationTable table, int naturalIndex)
    {
        ValidateBlockIndex(naturalIndex, "naturalIndex");
        int value = table.Values[naturalIndex];
        ValidateQuantizationValue(value);
        return value;
    }

    internal int GetZigZagQuantizationValue(JpegQuantizationTable table, int zigZagIndex)
    {
        ValidateBlockIndex(zigZagIndex, "zigZagIndex");
        if (HasZigZagValues(table))
        {
            int zigZagValue = table.ZigZagValues[zigZagIndex];
            ValidateQuantizationValue(zigZagValue);
            return zigZagValue;
        }

        int naturalIndex = JpegConstants.ZigZagOrder[zigZagIndex];
        return GetNaturalQuantizationValue(table, naturalIndex);
    }

    internal bool HasZigZagValues(JpegQuantizationTable table)
    {
        if (table.ZigZagValues == null || table.ZigZagValues.Length < JpegConstants.BlockSampleCount)
        {
            return false;
        }

        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            if (table.ZigZagValues[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int ClampToInt32(long value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (value < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)value;
    }

    internal void CopyFirstBlock(int[] source, int[] destination)
    {
        ValidateBlock(source, "source");
        ValidateBlock(destination, "destination");

        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            destination[index] = source[index];
            index = index + 1;
        }
    }

    internal void ValidateBlock(int[] coefficients, string parameterName)
    {
        if (coefficients == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }

        if (coefficients.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Coefficient block must contain 64 entries.");
        }
    }

    internal void ValidateTable(JpegQuantizationTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.Values == null || table.Values.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Quantization table values must contain 64 entries.");
        }
    }

    internal void ValidateNaturalTable(JpegQuantizationTable table)
    {
        ValidateTable(table);

        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            ValidateQuantizationValue(table.Values[index]);
            index = index + 1;
        }
    }

    internal void ValidateBlockIndex(int index, string parameterName)
    {
        if (index < 0 || index >= JpegConstants.BlockSampleCount)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    internal void ValidateQuantizationValue(int quantizationValue)
    {
        if (quantizationValue <= 0)
        {
            throw JpegInvalidDataException.Create("Quantization values must be positive.");
        }
    }
}
