namespace jpeg;

internal class JpegCoefficientOrderConverter
{
    internal int[] NaturalToZigZagOrder = new int[64];

    internal JpegCoefficientOrderConverter()
    {
        BuildNaturalToZigZagOrder();
    }

    internal int[] NaturalToZigZag(int[] naturalCoefficients)
    {
        ValidateBlock(naturalCoefficients, "naturalCoefficients");
        int[] zigZagCoefficients = new int[JpegConstants.BlockSampleCount];
        CopyNaturalToZigZag(naturalCoefficients, zigZagCoefficients);
        return zigZagCoefficients;
    }

    internal int[] ZigZagToNatural(int[] zigZagCoefficients)
    {
        ValidateBlock(zigZagCoefficients, "zigZagCoefficients");
        int[] naturalCoefficients = new int[JpegConstants.BlockSampleCount];
        CopyZigZagToNatural(zigZagCoefficients, naturalCoefficients);
        return naturalCoefficients;
    }

    internal void CopyNaturalToZigZag(int[] naturalCoefficients, int[] zigZagCoefficients)
    {
        ValidateBlock(naturalCoefficients, "naturalCoefficients");
        ValidateBlock(zigZagCoefficients, "zigZagCoefficients");

        int zigZagIndex = 0;
        while (zigZagIndex < JpegConstants.BlockSampleCount)
        {
            int naturalIndex = ToNaturalIndex(zigZagIndex);
            zigZagCoefficients[zigZagIndex] = naturalCoefficients[naturalIndex];
            zigZagIndex = zigZagIndex + 1;
        }
    }

    internal void CopyZigZagToNatural(int[] zigZagCoefficients, int[] naturalCoefficients)
    {
        ValidateBlock(zigZagCoefficients, "zigZagCoefficients");
        ValidateBlock(naturalCoefficients, "naturalCoefficients");

        int zigZagIndex = 0;
        while (zigZagIndex < JpegConstants.BlockSampleCount)
        {
            int naturalIndex = ToNaturalIndex(zigZagIndex);
            naturalCoefficients[naturalIndex] = zigZagCoefficients[zigZagIndex];
            zigZagIndex = zigZagIndex + 1;
        }
    }

    internal void ConvertNaturalToZigZagInPlace(int[] coefficients)
    {
        ValidateBlock(coefficients, "coefficients");
        int[] copy = NaturalToZigZag(coefficients);
        CopyFirstBlock(copy, coefficients);
    }

    internal void ConvertZigZagToNaturalInPlace(int[] coefficients)
    {
        ValidateBlock(coefficients, "coefficients");
        int[] copy = ZigZagToNatural(coefficients);
        CopyFirstBlock(copy, coefficients);
    }

    internal int ToNaturalIndex(int zigZagIndex)
    {
        ValidateBlockIndex(zigZagIndex, "zigZagIndex");
        return JpegConstants.ZigZagOrder[zigZagIndex];
    }

    internal int ToZigZagIndex(int naturalIndex)
    {
        ValidateBlockIndex(naturalIndex, "naturalIndex");
        return NaturalToZigZagOrder[naturalIndex];
    }

    internal void BuildNaturalToZigZagOrder()
    {
        int zigZagIndex = 0;
        while (zigZagIndex < JpegConstants.BlockSampleCount)
        {
            int naturalIndex = JpegConstants.ZigZagOrder[zigZagIndex];
            NaturalToZigZagOrder[naturalIndex] = zigZagIndex;
            zigZagIndex = zigZagIndex + 1;
        }
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

    internal void ValidateBlockIndex(int index, string parameterName)
    {
        if (index < 0 || index >= JpegConstants.BlockSampleCount)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
