namespace jpeg;

internal class JpegAccurateIntegerInverseDct : IJpegInverseDct
{
    internal const int BasisScaleBits = 12;
    internal const int FinalScaleBits = 26;
    internal const int SampleCenter = 128;

    internal int[] BasisValues = new int[]
    {
        2896, 4017, 3784, 3406, 2896, 2276, 1567, 799,
        2896, 3406, 1567, -799, -2896, -4017, -3784, -2276,
        2896, 2276, -1567, -4017, -2896, 799, 3784, 3406,
        2896, 799, -3784, -2276, 2896, 3406, -1567, -4017,
        2896, -799, -3784, 2276, 2896, -3406, -1567, 4017,
        2896, -2276, -1567, 4017, -2896, -799, 3784, -3406,
        2896, -3406, 1567, 799, -2896, 4017, -3784, 2276,
        2896, -4017, 3784, -3406, 2896, -2276, 1567, -799
    };

    /// <summary>Transforms one coefficient block to centered samples.</summary>
    /// <param name="coefficients">The natural-order coefficient block.</param>
    /// <returns>The centered sample block.</returns>
    public int[] InverseTransformBlock(int[] coefficients)
    {
        ValidateCoefficientBlock(coefficients, "coefficients");
        int[] samples = new int[JpegConstants.BlockSampleCount];
        CopyInverseTransformBlock(coefficients, samples);
        return samples;
    }

    /// <summary>Transforms one coefficient block to byte samples.</summary>
    /// <param name="coefficients">The natural-order coefficient block.</param>
    /// <returns>The byte sample block.</returns>
    public byte[] InverseTransformBlockToSamples(int[] coefficients)
    {
        ValidateCoefficientBlock(coefficients, "coefficients");
        byte[] samples = new byte[JpegConstants.BlockSampleCount];
        CopyInverseTransformBlockToSamples(coefficients, samples);
        return samples;
    }

    /// <summary>Transforms one coefficient block into a centered-sample destination.</summary>
    /// <param name="coefficients">The natural-order coefficient block.</param>
    /// <param name="destination">The centered-sample destination.</param>
    public void CopyInverseTransformBlock(int[] coefficients, int[] destination)
    {
        ValidateCoefficientBlock(coefficients, "coefficients");
        ValidateCoefficientBlock(destination, "destination");

        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                destination[y * JpegConstants.BlockWidth + x] = TransformCoefficientAt(coefficients, x, y);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    /// <summary>Transforms one coefficient block into a byte-sample destination.</summary>
    /// <param name="coefficients">The natural-order coefficient block.</param>
    /// <param name="destination">The byte-sample destination.</param>
    public void CopyInverseTransformBlockToSamples(int[] coefficients, byte[] destination)
    {
        ValidateCoefficientBlock(coefficients, "coefficients");
        ValidateSampleBlock(destination, "destination");

        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int transformedValue = TransformCoefficientAt(coefficients, x, y);
                destination[y * JpegConstants.BlockWidth + x] = ToSampleByte(transformedValue);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal int TransformCoefficientAt(int[] coefficients, int x, int y)
    {
        ValidateCoefficientBlock(coefficients, "coefficients");
        ValidateBlockPosition(x, "x");
        ValidateBlockPosition(y, "y");

        long total = 0;
        int verticalFrequency = 0;
        while (verticalFrequency < JpegConstants.BlockHeight)
        {
            int horizontalFrequency = 0;
            while (horizontalFrequency < JpegConstants.BlockWidth)
            {
                int coefficientIndex = verticalFrequency * JpegConstants.BlockWidth + horizontalFrequency;
                long coefficient = coefficients[coefficientIndex];
                long horizontalBasis = GetBasisValue(x, horizontalFrequency);
                long verticalBasis = GetBasisValue(y, verticalFrequency);
                total = total + coefficient * horizontalBasis * verticalBasis;
                horizontalFrequency = horizontalFrequency + 1;
            }

            verticalFrequency = verticalFrequency + 1;
        }

        return RoundScaledLong(total, FinalScaleBits);
    }

    internal int GetBasisValue(int position, int frequency)
    {
        ValidateBlockPosition(position, "position");
        ValidateBlockPosition(frequency, "frequency");
        return BasisValues[position * JpegConstants.BlockWidth + frequency];
    }

    internal int RoundScaledLong(long value, int shift)
    {
        long divisor = 1L << shift;
        long offset = divisor >> 1;
        long quotient = value / divisor;
        long remainder = value % divisor;
        if (remainder >= offset)
        {
            quotient = quotient + 1;
        }
        else if (remainder <= 0 - offset)
        {
            quotient = quotient - 1;
        }

        return ClampToInt32(quotient);
    }

    internal byte ToSampleByte(int transformedValue)
    {
        if (transformedValue < 0 - SampleCenter)
        {
            return 0;
        }

        if (transformedValue > 255 - SampleCenter)
        {
            return 255;
        }

        return (byte)(transformedValue + SampleCenter);
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

    internal void ValidateCoefficientBlock(int[] coefficients, string parameterName)
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

    internal void ValidateSampleBlock(byte[] samples, string parameterName)
    {
        if (samples == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }

        if (samples.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Sample block must contain 64 entries.");
        }
    }

    internal void ValidateBlockPosition(int value, string parameterName)
    {
        if (value < 0 || value >= JpegConstants.BlockWidth)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
