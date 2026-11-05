using System.Collections.Generic;

namespace png;

internal class PngApngEncoder
{
    internal static PngChunk[] EncodeAnimation(PngIhdr ihdr, PngPixelBuffer defaultPixels, PngAnimation animation, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(defaultPixels);
        ArgumentNullException.ThrowIfNull(defaultPixels.Data);
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(animation.Frames);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        if (options.MaxFdatChunkBytes < 0)
        {
            throw new ArgumentOutOfRangeException("MaxFdatChunkBytes");
        }

        PngApngAnimationLimitEnforcer.Enforce(animation, ihdr, options.Limits);
        ValidateDefaultImageSelection(animation.Frames);

        List<PngChunk> chunks = new List<PngChunk>();
        chunks.Add(PngAnimationControlWriter.WriteChunk(PngApngFrameRectangleEncoder.CreateAnimationControl(animation)));
        bool usesDefaultImageFrame = animation.Frames[0].UsesDefaultImage;
        if (!usesDefaultImageFrame)
        {
            byte[] compressedDefaultImage = PngEncoder.EncodePixels(defaultPixels, ihdr, options);
            if (HasBailedOut(options))
            {
                return new PngChunk[0];
            }

            AddChunks(chunks, PngEncoder.CreateIdatChunks(compressedDefaultImage, options));
        }

        uint sequenceNumber = 0;
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            bool frameUsesDefaultImage = usesDefaultImageFrame && frameIndex == 0;
            PngFrame encodedFrame = CreateEncodedFrame(animation.Frames[frameIndex], ihdr, frameUsesDefaultImage);
            PngFrameControl frameControl = PngApngFrameRectangleEncoder.CreateFrameControl(encodedFrame, sequenceNumber);
            chunks.Add(PngFrameControlWriter.WriteChunk(frameControl));
            sequenceNumber = IncrementSequenceNumber(sequenceNumber);

            byte[] compressedFrameData = EncodeFramePixels(encodedFrame, ihdr, options);
            if (HasBailedOut(options))
            {
                return new PngChunk[0];
            }

            if (frameUsesDefaultImage)
            {
                AddChunks(chunks, PngEncoder.CreateIdatChunks(compressedFrameData, options));
            }
            else
            {
                PngChunk[] frameDataChunks = CreateFrameDataChunks(compressedFrameData, sequenceNumber, options.MaxFdatChunkBytes);
                AddChunks(chunks, frameDataChunks);
                sequenceNumber = AddSequenceNumbers(sequenceNumber, frameDataChunks.Length);
            }

            frameIndex += 1;
        }

        return chunks.ToArray();
    }

    internal static bool HasBailedOut(PngEncoderOptions options)
    {
        return options.WasEarlyBailedOut;
    }

    internal static void ValidateDefaultImageSelection(PngFrame[] frames)
    {
        int index = 1;
        while (index < frames.Length)
        {
            PngFrame frame = frames[index];
            ArgumentNullException.ThrowIfNull(frame);
            if (frame.UsesDefaultImage)
            {
                throw new ArgumentOutOfRangeException("UsesDefaultImage");
            }

            index += 1;
        }
    }

    internal static PngFrame CreateEncodedFrame(PngFrame frame, PngIhdr ihdr, bool usesDefaultImage)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(frame.Pixels);
        int width = PngApngFrameRectangleEncoder.GetFrameDimension(frame.Width, frame.Pixels.Width);
        int height = PngApngFrameRectangleEncoder.GetFrameDimension(frame.Height, frame.Pixels.Height);
        if (usesDefaultImage)
        {
            PngApngFrameRectangleEncoder.ValidateDefaultFrame(width, height, frame.XOffset, frame.YOffset, ihdr);
        }
        else
        {
            PngApngFrameRectangleEncoder.ValidateFrameRectangle(width, height, frame.XOffset, frame.YOffset, ihdr);
        }

        PngFrame encodedFrame = new PngFrame();
        encodedFrame.Width = width;
        encodedFrame.Height = height;
        encodedFrame.XOffset = frame.XOffset;
        encodedFrame.YOffset = frame.YOffset;
        encodedFrame.DelayNumerator = frame.DelayNumerator;
        encodedFrame.DelayDenominator = frame.DelayDenominator;
        encodedFrame.DisposeOperation = frame.DisposeOperation;
        encodedFrame.BlendOperation = frame.BlendOperation;
        encodedFrame.Pixels = frame.Pixels;
        encodedFrame.UsesDefaultImage = usesDefaultImage;
        return encodedFrame;
    }

    internal static byte[] EncodeFramePixels(PngFrame frame, PngIhdr ihdr, PngEncoderOptions options)
    {
        PngIhdr frameIhdr = PngApngFrameRectangleEncoder.CreateFrameIhdr(ihdr, frame.Width, frame.Height);
        return PngApngFrameRectangleEncoder.EncodeFramePixels(frame.Pixels, frameIhdr, options);
    }

    internal static PngChunk[] CreateFrameDataChunks(byte[] compressedFrameData, uint firstSequenceNumber, int maxChunkBytes)
    {
        ArgumentNullException.ThrowIfNull(compressedFrameData);
        if (maxChunkBytes < 0)
        {
            throw new ArgumentOutOfRangeException("MaxFdatChunkBytes");
        }

        int chunkCount = PngEncoder.CalculateIdatChunkCount(compressedFrameData.Length, maxChunkBytes);
        PngChunk[] chunks = new PngChunk[chunkCount];
        int chunkIndex = 0;
        int dataOffset = 0;
        while (chunkIndex < chunkCount)
        {
            int chunkLength = PngEncoder.GetIdatChunkLength(compressedFrameData.Length, dataOffset, maxChunkBytes);
            PngFrameData frameData = new PngFrameData();
            frameData.SequenceNumber = AddSequenceNumbers(firstSequenceNumber, chunkIndex);
            frameData.Data = CopyBytes(compressedFrameData, dataOffset, chunkLength);
            chunks[chunkIndex] = PngFrameDataWriter.WriteChunk(frameData);
            dataOffset += chunkLength;
            chunkIndex += 1;
        }

        return chunks;
    }

    internal static uint IncrementSequenceNumber(uint sequenceNumber)
    {
        if (sequenceNumber == uint.MaxValue)
        {
            throw new OverflowException();
        }

        return sequenceNumber + 1;
    }

    internal static uint AddSequenceNumbers(uint sequenceNumber, int addition)
    {
        if (addition < 0 || (uint)addition > uint.MaxValue - sequenceNumber)
        {
            throw new OverflowException();
        }

        return sequenceNumber + (uint)addition;
    }

    internal static byte[] CopyBytes(byte[] source, int offset, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = source[offset + index];
            index += 1;
        }

        return result;
    }

    internal static void AddChunks(List<PngChunk> destination, PngChunk[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index += 1;
        }
    }
}
