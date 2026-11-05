namespace png;

internal class PngApngDefaultImageFrameHandler
{
    internal static bool UsesDefaultImageFrame(PngChunkListResult chunkList)
    {
        ArgumentNullException.ThrowIfNull(chunkList);
        return UsesDefaultImageFrame(chunkList.Chunks);
    }

    internal static bool UsesDefaultImageFrame(PngChunk[] chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        int firstFrameControlIndex = -1;
        int firstIdatIndex = -1;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);

            if (chunk.Type == PngConstants.ChunkFrameControl && firstFrameControlIndex < 0)
            {
                firstFrameControlIndex = index;
            }

            if (chunk.Type == PngConstants.ChunkIDAT && firstIdatIndex < 0)
            {
                firstIdatIndex = index;
            }

            index += 1;
        }

        return firstFrameControlIndex >= 0 && firstIdatIndex >= 0 && firstFrameControlIndex < firstIdatIndex;
    }

    internal static PngFrame CreateDefaultImageFrame(PngFrameControl frameControl, PngPixelBuffer pixels)
    {
        ArgumentNullException.ThrowIfNull(frameControl);
        ArgumentNullException.ThrowIfNull(pixels);

        PngFrame frame = new PngFrame();
        frame.Width = ConvertToInt(frameControl.Width, "Width");
        frame.Height = ConvertToInt(frameControl.Height, "Height");
        frame.XOffset = ConvertToInt(frameControl.XOffset, "XOffset");
        frame.YOffset = ConvertToInt(frameControl.YOffset, "YOffset");
        frame.DelayNumerator = frameControl.DelayNumerator;
        frame.DelayDenominator = frameControl.DelayDenominator;
        frame.DisposeOperation = frameControl.DisposeOperation;
        frame.BlendOperation = frameControl.BlendOperation;
        frame.Pixels = pixels;
        frame.UsesDefaultImage = true;
        return frame;
    }

    internal static PngAnimation PrependDefaultImageFrame(PngAnimation animation, PngFrame defaultFrame)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(animation.Frames);
        ArgumentNullException.ThrowIfNull(defaultFrame);

        PngAnimation result = new PngAnimation();
        result.FrameCount = animation.FrameCount;
        result.PlayCount = animation.PlayCount;
        result.Frames = new PngFrame[animation.Frames.Length + 1];
        result.Frames[0] = defaultFrame;

        int index = 0;
        while (index < animation.Frames.Length)
        {
            PngFrame frame = animation.Frames[index];
            ArgumentNullException.ThrowIfNull(frame);
            result.Frames[index + 1] = frame;
            index += 1;
        }

        return result;
    }

    internal static int ConvertToInt(uint value, string name)
    {
        if (value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return (int)value;
    }
}
