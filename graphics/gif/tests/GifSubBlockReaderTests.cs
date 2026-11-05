using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifSubBlockReaderTests
{
    [Test]
    public void Read_FromMemoryConcatenatesBlocksUntilTerminator()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 2, 10, 11, 3, 12, 13, 14, 0, 99 });
        GifSubBlockReader reader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.EqualTo(new byte[] { 10, 11, 12, 13, 14 }));
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(5));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(8));
    }

    [Test]
    public void Read_FromMemoryAcceptsEmptyTerminatedChain()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 0, 99 });
        GifSubBlockReader reader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Empty);
        Assert.That(result.BlockCount, Is.EqualTo(0));
        Assert.That(result.TotalBytes, Is.EqualTo(0));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(1));
    }

    [Test]
    public void Read_FromMemoryReportsUnterminatedChainWhenTerminatorIsMissing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 1, 42 });
        GifSubBlockReader reader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Unterminated, Is.True);
        Assert.That(result.Data, Is.EqualTo(new byte[] { 42 }));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Read_FromMemoryReportsUnterminatedChainWhenBlockPayloadIsShort()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 2, 42 });
        GifSubBlockReader reader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Unterminated, Is.True);
        Assert.That(result.Data, Is.Empty);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Read_StopsBeforeAllocationWhenByteLimitWouldBeExceeded()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 2, 1, 2, 2, 3, 4, 0 });
        GifSubBlockReader reader = new GifSubBlockReader(10, 3);

        GifSubBlockReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.LimitExceeded, Is.True);
        Assert.That(result.Data, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(2));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.SubBlockByteLimitExceeded));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(4));
    }

    [Test]
    public void Read_StopsBeforeDataReadWhenBlockCountLimitWouldBeExceeded()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 1, 1, 1, 2, 0 });
        GifSubBlockReader reader = new GifSubBlockReader(1, 10);

        GifSubBlockReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.LimitExceeded, Is.True);
        Assert.That(result.Data, Is.EqualTo(new byte[] { 1 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(1));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.SubBlockCountLimitExceeded));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(3));
    }

    [Test]
    public void Read_FromStreamConcatenatesBlocksUntilTerminator()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { 1, 7, 2, 8, 9, 0 }));
        GifSubBlockReader reader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.EqualTo(new byte[] { 7, 8, 9 }));
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void Read_FromStreamReportsUnterminatedPayloadAndTracksPartialConsumption()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { 3, 1, 2 }));
        GifSubBlockReader reader = new GifSubBlockReader(GifConstants.DefaultMaxSubBlocks, GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Unterminated, Is.True);
        Assert.That(result.Data, Is.Empty);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(3));
        Assert.That(byteReader.EndOfStream, Is.True);
    }
}
