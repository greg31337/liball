namespace jpeg;

internal class JpegScaledIntegerInverseDct
{
    internal JpegAccurateIntegerInverseDct Accurate = new JpegAccurateIntegerInverseDct();

    internal int[] InverseTransformBlock(int[] coefficients, int scaleNumerator, int scaleDenominator)
    {
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");
        ValidateScale(scaleNumerator, scaleDenominator);

        int outputSize = GetScaledBlockSize(scaleNumerator, scaleDenominator);
        int[] samples = new int[outputSize * outputSize];
        CopyInverseTransformBlock(coefficients, samples, scaleNumerator, scaleDenominator);
        return samples;
    }

    internal byte[] InverseTransformBlockToSamples(int[] coefficients, int scaleNumerator, int scaleDenominator)
    {
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");
        ValidateScale(scaleNumerator, scaleDenominator);

        int outputSize = GetScaledBlockSize(scaleNumerator, scaleDenominator);
        byte[] samples = new byte[outputSize * outputSize];
        CopyInverseTransformBlockToSamples(coefficients, samples, scaleNumerator, scaleDenominator);
        return samples;
    }

    internal void CopyInverseTransformBlock(int[] coefficients, int[] destination, int scaleNumerator, int scaleDenominator)
    {
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");
        ValidateScale(scaleNumerator, scaleDenominator);
        ValidateScaledIntBlock(destination, "destination", scaleNumerator, scaleDenominator);

        int[] fullBlock = Accurate.InverseTransformBlock(coefficients);
        int outputSize = GetScaledBlockSize(scaleNumerator, scaleDenominator);
        int outputY = 0;
        while (outputY < outputSize)
        {
            int outputX = 0;
            while (outputX < outputSize)
            {
                int destinationIndex = outputY * outputSize + outputX;
                destination[destinationIndex] = AverageOutputRegion(fullBlock, outputX, outputY, outputSize);
                outputX = outputX + 1;
            }

            outputY = outputY + 1;
        }
    }

    internal void CopyInverseTransformBlockToSamples(int[] coefficients, byte[] destination, int scaleNumerator, int scaleDenominator)
    {
        Accurate.ValidateCoefficientBlock(coefficients, "coefficients");
        ValidateScale(scaleNumerator, scaleDenominator);
        ValidateScaledByteBlock(destination, "destination", scaleNumerator, scaleDenominator);

        int[] scaledBlock = InverseTransformBlock(coefficients, scaleNumerator, scaleDenominator);
        int outputSampleCount = GetOutputSampleCount(scaleNumerator, scaleDenominator);
        int index = 0;
        while (index < outputSampleCount)
        {
            destination[index] = Accurate.ToSampleByte(scaledBlock[index]);
            index = index + 1;
        }
    }

    internal int AverageRegion(int[] fullBlock, int startX, int startY, int size)
    {
        Accurate.ValidateCoefficientBlock(fullBlock, "fullBlock");
        ValidateRegion(startX, startY, size);

        long total = 0;
        int y = 0;
        while (y < size)
        {
            int x = 0;
            while (x < size)
            {
                int sourceIndex = (startY + y) * JpegConstants.BlockWidth + startX + x;
                total = total + fullBlock[sourceIndex];
                x = x + 1;
            }

            y = y + 1;
        }

        return RoundAverage(total, size * size);
    }

    internal int AverageOutputRegion(int[] fullBlock, int outputX, int outputY, int outputSize)
    {
        Accurate.ValidateCoefficientBlock(fullBlock, "fullBlock");
        ValidateOutputPosition(outputX, outputY, outputSize);

        int startX = GetRegionStart(outputX, outputSize);
        int endX = GetRegionEnd(outputX, outputSize);
        int startY = GetRegionStart(outputY, outputSize);
        int endY = GetRegionEnd(outputY, outputSize);
        return AverageRectangle(fullBlock, startX, startY, endX - startX, endY - startY);
    }

    internal int AverageRectangle(int[] fullBlock, int startX, int startY, int width, int height)
    {
        Accurate.ValidateCoefficientBlock(fullBlock, "fullBlock");
        ValidateRectangle(startX, startY, width, height);

        long total = 0;
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int sourceIndex = (startY + y) * JpegConstants.BlockWidth + startX + x;
                total = total + fullBlock[sourceIndex];
                x = x + 1;
            }

            y = y + 1;
        }

        return RoundAverage(total, width * height);
    }

    internal int GetRegionStart(int outputPosition, int outputSize)
    {
        ValidateOutputAxis(outputPosition, outputSize, "outputPosition");
        return outputPosition * JpegConstants.BlockWidth / outputSize;
    }

    internal int GetRegionEnd(int outputPosition, int outputSize)
    {
        ValidateOutputAxis(outputPosition, outputSize, "outputPosition");
        return (outputPosition + 1) * JpegConstants.BlockWidth / outputSize;
    }

    internal int RoundAverage(long total, int count)
    {
        if (count <= 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        long quotient = total / count;
        long remainder = total % count;
        int offset = count / 2;
        int roundingThreshold = count - offset;
        if (remainder >= roundingThreshold)
        {
            quotient = quotient + 1;
        }
        else if (remainder <= 0 - roundingThreshold)
        {
            quotient = quotient - 1;
        }

        return Accurate.ClampToInt32(quotient);
    }

    internal int GetScaledBlockSize(int scaleNumerator, int scaleDenominator)
    {
        ValidateScale(scaleNumerator, scaleDenominator);
        long scaledNumerator = (long)JpegConstants.BlockWidth * (long)scaleNumerator;
        return (int)(scaledNumerator / scaleDenominator);
    }

    internal int GetOutputSampleCount(int scaleNumerator, int scaleDenominator)
    {
        int outputSize = GetScaledBlockSize(scaleNumerator, scaleDenominator);
        return outputSize * outputSize;
    }

    internal void ValidateScale(int scaleNumerator, int scaleDenominator)
    {
        if (scaleNumerator < 1)
        {
            throw new System.ArgumentOutOfRangeException("scaleNumerator");
        }

        if (scaleDenominator < 1)
        {
            throw new System.ArgumentOutOfRangeException("scaleDenominator");
        }

        if (scaleNumerator > scaleDenominator)
        {
            throw new System.ArgumentOutOfRangeException("scaleNumerator");
        }

        long scaledNumerator = (long)JpegConstants.BlockWidth * (long)scaleNumerator;
        if (scaledNumerator % scaleDenominator != 0)
        {
            throw new System.ArgumentOutOfRangeException("scaleDenominator");
        }

        long outputSize = scaledNumerator / scaleDenominator;
        if (outputSize < 1 || outputSize > JpegConstants.BlockWidth)
        {
            throw new System.ArgumentOutOfRangeException("scaleDenominator");
        }
    }

    internal void ValidateScaledIntBlock(int[] samples, string parameterName, int scaleNumerator, int scaleDenominator)
    {
        if (samples == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }

        int outputSampleCount = GetOutputSampleCount(scaleNumerator, scaleDenominator);
        if (samples.Length < outputSampleCount)
        {
            throw JpegInvalidDataException.Create("Scaled sample block is too small.");
        }
    }

    internal void ValidateScaledByteBlock(byte[] samples, string parameterName, int scaleNumerator, int scaleDenominator)
    {
        if (samples == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }

        int outputSampleCount = GetOutputSampleCount(scaleNumerator, scaleDenominator);
        if (samples.Length < outputSampleCount)
        {
            throw JpegInvalidDataException.Create("Scaled sample block is too small.");
        }
    }

    internal void ValidateRegion(int startX, int startY, int size)
    {
        if (startX < 0 || startX >= JpegConstants.BlockWidth)
        {
            throw new System.ArgumentOutOfRangeException("startX");
        }

        if (startY < 0 || startY >= JpegConstants.BlockHeight)
        {
            throw new System.ArgumentOutOfRangeException("startY");
        }

        if (size < 1 || startX + size > JpegConstants.BlockWidth || startY + size > JpegConstants.BlockHeight)
        {
            throw new System.ArgumentOutOfRangeException("size");
        }
    }

    internal void ValidateRectangle(int startX, int startY, int width, int height)
    {
        if (startX < 0 || startX >= JpegConstants.BlockWidth)
        {
            throw new System.ArgumentOutOfRangeException("startX");
        }

        if (startY < 0 || startY >= JpegConstants.BlockHeight)
        {
            throw new System.ArgumentOutOfRangeException("startY");
        }

        if (width < 1 || startX + width > JpegConstants.BlockWidth)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height < 1 || startY + height > JpegConstants.BlockHeight)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }
    }

    internal void ValidateOutputPosition(int outputX, int outputY, int outputSize)
    {
        ValidateOutputAxis(outputX, outputSize, "outputX");
        ValidateOutputAxis(outputY, outputSize, "outputY");
    }

    internal void ValidateOutputAxis(int outputPosition, int outputSize, string parameterName)
    {
        if (outputSize < 1 || outputSize > JpegConstants.BlockWidth)
        {
            throw new System.ArgumentOutOfRangeException("outputSize");
        }

        if (outputPosition < 0 || outputPosition >= outputSize)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
