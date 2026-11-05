using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifImageDescriptorWriterTests
{
    [Test]
    public void Create_WritesDescriptorBytesAndPackedFields()
    {
        GifFrame frame = new GifFrame(1, 2, 16, 32);
        frame.HasLocalColorTable = true;
        frame.LocalColorTableSize = 32;
        frame.Interlace = true;
        frame.LocalColorTableSortFlag = true;
        GifImageDescriptorWriter writer = new GifImageDescriptorWriter();

        GifImageDescriptorWriteResult result = writer.Create(frame);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x2C, 1, 0, 2, 0, 16, 0, 32, 0, 0xE4 }));
        Assert.That(result.PackedFields, Is.EqualTo(0xE4));
        Assert.That(result.BytesWritten, Is.EqualTo(10));
    }

    [Test]
    public void Create_WritesInterlacedDescriptorWithoutLocalColorTable()
    {
        GifFrame frame = new GifFrame(0, 0, 1, 2);
        frame.Interlace = true;
        GifImageDescriptorWriter writer = new GifImageDescriptorWriter();

        GifImageDescriptorWriteResult result = writer.Create(frame);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x2C, 0, 0, 0, 0, 1, 0, 2, 0, 0x40 }));
    }

    [Test]
    public void Create_RoundTripsThroughParser()
    {
        GifFrame frame = new GifFrame(5, 6, 7, 8);
        frame.HasLocalColorTable = true;
        frame.LocalColorTableSize = 16;
        frame.Interlace = true;
        GifImageDescriptorWriter writer = new GifImageDescriptorWriter();
        GifImageDescriptorParser parser = new GifImageDescriptorParser();

        GifImageDescriptorWriteResult writeResult = writer.Create(frame);
        GifImageDescriptorReadResult readResult = parser.Parse(writeResult.EncodedData);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Frame.Left, Is.EqualTo(5));
        Assert.That(readResult.Frame.Top, Is.EqualTo(6));
        Assert.That(readResult.Frame.Width, Is.EqualTo(7));
        Assert.That(readResult.Frame.Height, Is.EqualTo(8));
        Assert.That(readResult.Frame.HasLocalColorTable, Is.True);
        Assert.That(readResult.Frame.LocalColorTableSize, Is.EqualTo(16));
        Assert.That(readResult.Frame.Interlace, Is.True);
    }

    [Test]
    public void Write_WritesToProvidedStreamAndAdvancesOffset()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        GifImageDescriptorWriter writer = new GifImageDescriptorWriter();

        GifImageDescriptorWriteResult result = writer.Write(byteWriter, frame);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0 }));
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(10));
    }

    [Test]
    public void Create_RejectsInvalidLocalColorTableSize()
    {
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.HasLocalColorTable = true;
        frame.LocalColorTableSize = 3;
        GifImageDescriptorWriter writer = new GifImageDescriptorWriter();

        GifImageDescriptorWriteResult result = writer.Create(frame);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(result.EncodedData, Is.Empty);
    }

    [Test]
    public void Create_RejectsUnencodableRectangleFields()
    {
        GifImageDescriptorWriter writer = new GifImageDescriptorWriter();
        GifFrame negativeLeft = new GifFrame(-1, 0, 1, 1);
        GifFrame tooWide = new GifFrame(0, 0, 65536, 1);

        Assert.That(writer.Create(negativeLeft).Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(writer.Create(tooWide).Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifImageDescriptorWriter writer = new GifImageDescriptorWriter();

        GifImageDescriptorWriteResult result = writer.Write(null!, new GifFrame(0, 0, 1, 1));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[10], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifImageDescriptorWriter writer = new GifImageDescriptorWriter();

        GifImageDescriptorWriteResult result = writer.Write(byteWriter, new GifFrame(0, 0, 1, 1));

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }
}
