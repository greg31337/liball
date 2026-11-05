namespace jpeg;

internal class JpegIntegerForwardDct
{
    internal JpegAccurateIntegerInverseDct Accurate = new JpegAccurateIntegerInverseDct();

    internal int[] ForwardTransformBlock(int[] centeredSamples)
    {
        ValidateCenteredSampleBlock(centeredSamples, "centeredSamples");
        int[] coefficients = new int[JpegConstants.BlockSampleCount];
        CopyForwardTransformBlock(centeredSamples, coefficients);
        return coefficients;
    }

    internal int[] ForwardTransformSamples(byte[] samples)
    {
        Accurate.ValidateSampleBlock(samples, "samples");
        int[] centeredSamples = new int[JpegConstants.BlockSampleCount];
        CopyCenteredSamples(samples, centeredSamples);
        return ForwardTransformBlock(centeredSamples);
    }

    internal void CopyForwardTransformBlock(int[] centeredSamples, int[] destination)
    {
        ValidateCenteredSampleBlock(centeredSamples, "centeredSamples");
        Accurate.ValidateCoefficientBlock(destination, "destination");

        int verticalFrequency = 0;
        while (verticalFrequency < JpegConstants.BlockHeight)
        {
            int horizontalFrequency = 0;
            while (horizontalFrequency < JpegConstants.BlockWidth)
            {
                int coefficientIndex = verticalFrequency * JpegConstants.BlockWidth + horizontalFrequency;
                destination[coefficientIndex] = TransformCoefficientAt(centeredSamples, horizontalFrequency, verticalFrequency);
                horizontalFrequency = horizontalFrequency + 1;
            }

            verticalFrequency = verticalFrequency + 1;
        }
    }

    internal void CopyForwardTransformSamples(byte[] samples, int[] destination)
    {
        Accurate.ValidateSampleBlock(samples, "samples");
        Accurate.ValidateCoefficientBlock(destination, "destination");

        int[] centeredSamples = new int[JpegConstants.BlockSampleCount];
        CopyCenteredSamples(samples, centeredSamples);
        CopyForwardTransformBlock(centeredSamples, destination);
    }

    internal int TransformCoefficientAt(int[] centeredSamples, int horizontalFrequency, int verticalFrequency)
    {
        ValidateCenteredSampleBlock(centeredSamples, "centeredSamples");
        Accurate.ValidateBlockPosition(horizontalFrequency, "horizontalFrequency");
        Accurate.ValidateBlockPosition(verticalFrequency, "verticalFrequency");

        long total = 0;
        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int sampleIndex = y * JpegConstants.BlockWidth + x;
                long centeredSample = centeredSamples[sampleIndex];
                long horizontalBasis = Accurate.GetBasisValue(x, horizontalFrequency);
                long verticalBasis = Accurate.GetBasisValue(y, verticalFrequency);
                total = total + centeredSample * horizontalBasis * verticalBasis;
                x = x + 1;
            }

            y = y + 1;
        }

        return Accurate.RoundScaledLong(total, JpegAccurateIntegerInverseDct.FinalScaleBits);
    }

    internal void CopyCenteredSamples(byte[] samples, int[] destination)
    {
        Accurate.ValidateSampleBlock(samples, "samples");
        Accurate.ValidateCoefficientBlock(destination, "destination");

        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            destination[index] = CenterSampleByte(samples[index]);
            index = index + 1;
        }
    }

    internal int CenterSampleByte(byte sample)
    {
        return sample - JpegAccurateIntegerInverseDct.SampleCenter;
    }

    internal void ValidateCenteredSampleBlock(int[] centeredSamples, string parameterName)
    {
        if (centeredSamples == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }

        if (centeredSamples.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Centered sample block must contain 64 entries.");
        }
    }
}
