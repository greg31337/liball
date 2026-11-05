namespace png;

internal class PngApngAnimationLimitEnforcer
{
    internal static void Enforce(PngAnimation animation, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(animation.Frames);
        ArgumentNullException.ThrowIfNull(animation.ComposedFrames);
        ArgumentNullException.ThrowIfNull(limits);

        EnforceFrameArray(animation, limits);
        EnforceFrameByteLimits(animation, limits);
        EnforceAggregateBytes(animation, limits);
    }

    internal static void Enforce(PngAnimation animation, PngIhdr ihdr, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(animation.Frames);
        ArgumentNullException.ThrowIfNull(animation.ComposedFrames);
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(limits);

        EnforceFrameArray(animation, limits);
        EnforceFrameByteLimits(animation, limits);
        EnforceAggregateBytes(animation, limits);
    }

    internal static void EnforceFrameCount(uint frameCount, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        if (frameCount == 0)
        {
            throw new ArgumentOutOfRangeException("FrameCount");
        }

        if (frameCount > int.MaxValue)
        {
            throw new OverflowException();
        }

        if (limits.MaxAnimationFrames > 0 && frameCount > (uint)limits.MaxAnimationFrames)
        {
            throw new OverflowException();
        }
    }

    internal static long CalculateDecodedPixelBytes(PngAnimation animation, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(animation.Frames);
        ArgumentNullException.ThrowIfNull(animation.ComposedFrames);
        ArgumentNullException.ThrowIfNull(ihdr);

        long totalBytes = 0;
        int index = 0;
        while (index < animation.Frames.Length)
        {
            PngFrame frame = animation.Frames[index];
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(frame.Pixels);
            long frameBytes = CalculateFramePixelBytes(frame);
            if (totalBytes > long.MaxValue - frameBytes)
            {
                throw new OverflowException();
            }

            totalBytes += frameBytes;
            index += 1;
        }

        index = 0;
        while (index < animation.ComposedFrames.Length)
        {
            PngPixelBuffer buffer = animation.ComposedFrames[index];
            ArgumentNullException.ThrowIfNull(buffer);
            long bufferBytes = PngSizeCalculator.CalculateFrameBytes(buffer.Width, buffer.Height, buffer.Format);
            if (totalBytes > long.MaxValue - bufferBytes)
            {
                throw new OverflowException();
            }

            totalBytes += bufferBytes;
            index += 1;
        }

        return totalBytes;
    }

    internal static void EnforceFrameByteLimits(PngAnimation animation, PngSecurityLimits limits)
    {
        int index = 0;
        while (index < animation.Frames.Length)
        {
            PngFrame frame = animation.Frames[index];
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(frame.Pixels);
            int width = GetFrameDimension(frame.Width, frame.Pixels.Width);
            int height = GetFrameDimension(frame.Height, frame.Pixels.Height);
            PngSecurityLimitEnforcer.EnforceDimensions(width, height, limits);
            long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(width, frame.Pixels.Format);
            long frameBytes = PngSizeCalculator.CalculateFrameBytes(width, height, frame.Pixels.Format);
            PngSecurityLimitEnforcer.EnforceRowBytes(rowBytes, limits);
            PngSecurityLimitEnforcer.EnforceFrameBytes(frameBytes, limits);
            PngSecurityLimitEnforcer.EnforceDecompressedBytes(frameBytes, limits);
            index += 1;
        }

        index = 0;
        while (index < animation.ComposedFrames.Length)
        {
            PngPixelBuffer buffer = animation.ComposedFrames[index];
            ArgumentNullException.ThrowIfNull(buffer);
            PngSecurityLimitEnforcer.EnforcePixelBuffer(buffer, limits);
            index += 1;
        }
    }

    internal static void EnforceAggregateBytes(PngAnimation animation, PngSecurityLimits limits)
    {
        long decodedPixelBytes = CalculateDecodedPixelBytesWithoutIhdr(animation);
        PngSecurityLimitEnforcer.EnforceDecompressedBytes(decodedPixelBytes, limits);
        if (limits.MaxAnimationBytes > 0 && decodedPixelBytes > limits.MaxAnimationBytes)
        {
            throw new OverflowException();
        }
    }

    internal static long CalculateDecodedPixelBytesWithoutIhdr(PngAnimation animation)
    {
        PngIhdr ihdr = new PngIhdr();
        return CalculateDecodedPixelBytes(animation, ihdr);
    }

    internal static void EnforceFrameArray(PngAnimation animation, PngSecurityLimits limits)
    {
        if (animation.Frames.Length == 0)
        {
            throw new ArgumentOutOfRangeException("Frames");
        }

        if (animation.FrameCount != 0 && animation.FrameCount != animation.Frames.Length)
        {
            throw new ArgumentOutOfRangeException("FrameCount");
        }

        EnforceFrameCount((uint)animation.Frames.Length, limits);
    }

    internal static long CalculateFramePixelBytes(PngFrame frame)
    {
        int width = GetFrameDimension(frame.Width, frame.Pixels.Width);
        int height = GetFrameDimension(frame.Height, frame.Pixels.Height);
        return PngSizeCalculator.CalculateFrameBytes(width, height, frame.Pixels.Format);
    }

    internal static int GetFrameDimension(int frameDimension, int pixelDimension)
    {
        if (frameDimension == 0)
        {
            return pixelDimension;
        }

        return frameDimension;
    }
}
