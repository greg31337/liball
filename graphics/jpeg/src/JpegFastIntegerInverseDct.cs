namespace jpeg;

internal class JpegFastIntegerInverseDct : IJpegInverseDct
{
    internal JpegAccurateIntegerInverseDct Accurate = new JpegAccurateIntegerInverseDct();
    internal const int PassScaleBits = JpegAccurateIntegerInverseDct.BasisScaleBits + 1;

    /// <summary>Transforms one coefficient block to centered samples.</summary>
    /// <param name="coefficients">The natural-order coefficient block.</param>
    /// <returns>The centered sample block.</returns>
    public int[] InverseTransformBlock(int[] coefficients)
    {
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");
        int[] samples = new int[JpegConstants.BlockSampleCount];
        CopyInverseTransformBlock(coefficients, samples);
        return samples;
    }

    /// <summary>Transforms one coefficient block to byte samples.</summary>
    /// <param name="coefficients">The natural-order coefficient block.</param>
    /// <returns>The byte sample block.</returns>
    public byte[] InverseTransformBlockToSamples(int[] coefficients)
    {
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");
        byte[] samples = new byte[JpegConstants.BlockSampleCount];
        CopyInverseTransformBlockToSamples(coefficients, samples);
        return samples;
    }

    /// <summary>Transforms one coefficient block into a centered-sample destination.</summary>
    /// <param name="coefficients">The natural-order coefficient block.</param>
    /// <param name="destination">The centered-sample destination.</param>
    public void CopyInverseTransformBlock(int[] coefficients, int[] destination)
    {
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");
        Accurate.ValidateCoefficientBlock(destination, "destination");

        long[] workspace = BuildRowWorkspace(coefficients);
        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                destination[y * JpegConstants.BlockWidth + x] = TransformWorkspaceAt(workspace, x, y);
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
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");
        Accurate.ValidateSampleBlock(destination, "destination");

        long[] workspace = BuildRowWorkspace(coefficients);
        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int transformedValue = TransformWorkspaceAt(workspace, x, y);
                destination[y * JpegConstants.BlockWidth + x] = Accurate.ToSampleByte(transformedValue);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal long[] BuildRowWorkspace(int[] coefficients)
    {
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");

        long[] workspace = new long[JpegConstants.BlockSampleCount];
        int verticalFrequency = 0;
        while (verticalFrequency < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                long total = 0;
                int horizontalFrequency = 0;
                while (horizontalFrequency < JpegConstants.BlockWidth)
                {
                    int coefficientIndex = verticalFrequency * JpegConstants.BlockWidth + horizontalFrequency;
                    long coefficient = coefficients[coefficientIndex];
                    long basis = Accurate.GetBasisValue(x, horizontalFrequency);
                    total = total + coefficient * basis;
                    horizontalFrequency = horizontalFrequency + 1;
                }

                workspace[verticalFrequency * JpegConstants.BlockWidth + x] = RoundScaledLongToLong(total, PassScaleBits);
                x = x + 1;
            }

            verticalFrequency = verticalFrequency + 1;
        }

        return workspace;
    }

    internal int TransformWorkspaceAt(long[] workspace, int x, int y)
    {
        ValidateWorkspaceBlock(workspace, "workspace");
        Accurate.ValidateBlockPosition(x, "x");
        Accurate.ValidateBlockPosition(y, "y");

        long total = 0;
        int verticalFrequency = 0;
        while (verticalFrequency < JpegConstants.BlockHeight)
        {
            long workspaceValue = workspace[verticalFrequency * JpegConstants.BlockWidth + x];
            long basis = Accurate.GetBasisValue(y, verticalFrequency);
            total = total + workspaceValue * basis;
            verticalFrequency = verticalFrequency + 1;
        }

        return Accurate.ClampToInt32(RoundScaledLongToLong(total, PassScaleBits));
    }

    internal long RoundScaledLongToLong(long value, int shift)
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

        return quotient;
    }

    internal void ValidateWorkspaceBlock(long[] workspace, string parameterName)
    {
        if (workspace == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }

        if (workspace.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("IDCT workspace block must contain 64 entries.");
        }
    }
}
