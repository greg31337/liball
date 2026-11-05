using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifSubBlockWriterTests
{
    [Test]
    public void Create_WritesEmptyPayloadAsOnlyTerminator()
    {
        GifSubBlockWriter writer = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockWriteResult result = writer.Create(new byte[0]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(0));
        Assert.That(result.TotalBytes, Is.EqualTo(0));
    }

    [Test]
    public void Create_WritesSingleSmallSubBlockAndTerminator()
    {
        GifSubBlockWriter writer = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockWriteResult result = writer.Create(new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 3, 1, 2, 3, 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
    }

    [Test]
    public void Create_ChunksPayloadAtMaximumSubBlockSize()
    {
        byte[] payload = new byte[256];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = (byte)index;
            index = index + 1;
        }

        GifSubBlockWriter writer = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockWriteResult result = writer.Create(payload);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(256));
        Assert.That(result.EncodedData.Length, Is.EqualTo(259));
        Assert.That(result.EncodedData[0], Is.EqualTo(255));
        Assert.That(result.EncodedData[256], Is.EqualTo(1));
        Assert.That(result.EncodedData[257], Is.EqualTo(255));
        Assert.That(result.EncodedData[258], Is.EqualTo(0));
    }

    [Test]
    public void Write_WritesToProvidedStreamAndTracksByteOffset()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifSubBlockWriter writer = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockWriteResult result = writer.Write(byteWriter, new byte[] { 7, 8 });

        Assert.That(result.Success, Is.True);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 2, 7, 8, 0 }));
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(4));
    }

    [Test]
    public void Write_TreatsNullPayloadAsEmptyPayload()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifSubBlockWriter writer = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockWriteResult result = writer.Write(byteWriter, null!);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(0));
        Assert.That(result.TotalBytes, Is.EqualTo(0));
    }

    [Test]
    public void Write_RejectsPayloadAboveByteLimitBeforeWriting()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifSubBlockWriter writer = new GifSubBlockWriter(2);

        GifSubBlockWriteResult result = writer.Write(byteWriter, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.LimitExceeded, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
        Assert.That(stream.ToArray(), Is.Empty);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifSubBlockWriter writer = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockWriteResult result = writer.Write(null!, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[8], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifSubBlockWriter writer = new GifSubBlockWriter(GifConstants.DefaultMaxSubBlockBytes);

        GifSubBlockWriteResult result = writer.Write(byteWriter, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }
}
