using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifAllocationSmokeTests
{
    const int LargeTrailingPayloadLength = 1024 * 1024;
    const long AllocationGrowthAllowanceBytes = 96 * 1024;

    [Test]
    public void Identify_DoesNotAllocateProportionallyToCompressedImagePayload()
    {
        byte[] small = CreateTwoFrameGifWithLargeSecondPayload(0);
        byte[] large = CreateTwoFrameGifWithLargeSecondPayload(LargeTrailingPayloadLength);

        long smallAllocations = MeasureIdentifyAllocations(small, out GifInfo smallInfo);
        long largeAllocations = MeasureIdentifyAllocations(large, out GifInfo largeInfo);

        Assert.That(smallInfo.Success, Is.True);
        Assert.That(largeInfo.Success, Is.True);
        Assert.That(largeInfo.FrameCount, Is.EqualTo(2));
        Assert.That(largeAllocations, Is.LessThanOrEqualTo(smallAllocations + AllocationGrowthAllowanceBytes));
        Assert.That(largeAllocations, Is.LessThan(LargeTrailingPayloadLength / 4));
    }

    [Test]
    public void DecodeAnimation_FirstFrameOnlyDoesNotAllocateProportionallyToTrailingFrames()
    {
        byte[] small = CreateTwoFrameGifWithLargeSecondPayload(0);
        byte[] large = CreateTwoFrameGifWithLargeSecondPayload(LargeTrailingPayloadLength);

        long smallAllocations = MeasureFirstFrameDecodeAllocations(small, out GifAnimation smallAnimation);
        long largeAllocations = MeasureFirstFrameDecodeAllocations(large, out GifAnimation largeAnimation);

        Assert.That(smallAnimation.Frames.Length, Is.EqualTo(1));
        Assert.That(largeAnimation.Frames.Length, Is.EqualTo(1));
        Assert.That(largeAnimation.Width, Is.EqualTo(1));
        Assert.That(largeAnimation.Height, Is.EqualTo(1));
        Assert.That(largeAllocations, Is.LessThanOrEqualTo(smallAllocations + AllocationGrowthAllowanceBytes));
        Assert.That(largeAllocations, Is.LessThan(LargeTrailingPayloadLength / 4));
    }

    static long MeasureIdentifyAllocations(byte[] data, out GifInfo info)
    {
        Gif.Identify(data);

        long before = GC.GetAllocatedBytesForCurrentThread();
        info = Gif.Identify(data);
        long after = GC.GetAllocatedBytesForCurrentThread();
        GC.KeepAlive(info);
        return after - before;
    }

    static long MeasureFirstFrameDecodeAllocations(byte[] data, out GifAnimation animation)
    {
        GifDecodeOptions warmOptions = new GifDecodeOptions();
        warmOptions.FirstFrameOnly = true;
        Gif.DecodeAnimation(data, warmOptions);

        GifDecodeOptions options = new GifDecodeOptions();
        options.FirstFrameOnly = true;
        long before = GC.GetAllocatedBytesForCurrentThread();
        animation = Gif.DecodeAnimation(data, options);
        long after = GC.GetAllocatedBytesForCurrentThread();
        GC.KeepAlive(animation);
        return after - before;
    }

    static byte[] CreateTwoFrameGifWithLargeSecondPayload(int secondPayloadLength)
    {
        int subBlockCount = CalculateSubBlockCount(secondPayloadLength);
        int length = 19 + 15 + 11 + subBlockCount + secondPayloadLength + 2;
        byte[] data = new byte[length];
        WriteHeaderLogicalScreenAndPalette(data);

        int offset = 19;
        offset = WriteOnePixelImage(data, offset, 68, 1);
        offset = WriteSecondImagePrefix(data, offset);
        offset = WritePayloadSubBlocks(data, offset, secondPayloadLength);
        data[offset] = 0;
        data[offset + 1] = 0x3B;
        return data;
    }

    static int CalculateSubBlockCount(int payloadLength)
    {
        if (payloadLength == 0)
        {
            return 0;
        }

        return ((payloadLength - 1) / 255) + 1;
    }

    static void WriteHeaderLogicalScreenAndPalette(byte[] data)
    {
        data[0] = 71;
        data[1] = 73;
        data[2] = 70;
        data[3] = 56;
        data[4] = 57;
        data[5] = 97;
        data[6] = 1;
        data[7] = 0;
        data[8] = 1;
        data[9] = 0;
        data[10] = 0x80;
        data[11] = 0;
        data[12] = 0;
        data[13] = 0;
        data[14] = 0;
        data[15] = 0;
        data[16] = 255;
        data[17] = 255;
        data[18] = 255;
    }

    static int WriteOnePixelImage(byte[] data, int offset, byte compressedByte0, byte compressedByte1)
    {
        offset = WriteImageDescriptor(data, offset);
        data[offset] = 2;
        data[offset + 1] = 2;
        data[offset + 2] = compressedByte0;
        data[offset + 3] = compressedByte1;
        data[offset + 4] = 0;
        return offset + 5;
    }

    static int WriteSecondImagePrefix(byte[] data, int offset)
    {
        offset = WriteImageDescriptor(data, offset);
        data[offset] = 2;
        return offset + 1;
    }

    static int WriteImageDescriptor(byte[] data, int offset)
    {
        data[offset] = 0x2C;
        data[offset + 1] = 0;
        data[offset + 2] = 0;
        data[offset + 3] = 0;
        data[offset + 4] = 0;
        data[offset + 5] = 1;
        data[offset + 6] = 0;
        data[offset + 7] = 1;
        data[offset + 8] = 0;
        data[offset + 9] = 0;
        return offset + 10;
    }

    static int WritePayloadSubBlocks(byte[] data, int offset, int payloadLength)
    {
        int remaining = payloadLength;
        while (remaining > 0)
        {
            int blockLength = 255;
            if (remaining < blockLength)
            {
                blockLength = remaining;
            }

            data[offset] = (byte)blockLength;
            offset = offset + 1;
            int index = 0;
            while (index < blockLength)
            {
                data[offset + index] = 77;
                index = index + 1;
            }

            offset = offset + blockLength;
            remaining = remaining - blockLength;
        }

        return offset;
    }
}
