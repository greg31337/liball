using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifImageDescriptorParserTests
{
    [Test]
    public void Read_FromMemoryParsesRectangleAndPackedFields()
    {
        byte[] data = new byte[] { 0x2C, 1, 0, 2, 0, 16, 0, 32, 0, 0xE4 };
        GifMemoryByteReader byteReader = new GifMemoryByteReader(data);
        GifImageDescriptorParser parser = new GifImageDescriptorParser();

        GifImageDescriptorReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Frame.Left, Is.EqualTo(1));
        Assert.That(result.Frame.Top, Is.EqualTo(2));
        Assert.That(result.Frame.Width, Is.EqualTo(16));
        Assert.That(result.Frame.Height, Is.EqualTo(32));
        Assert.That(result.Frame.HasLocalColorTable, Is.True);
        Assert.That(result.Frame.Interlace, Is.True);
        Assert.That(result.Frame.LocalColorTableSortFlag, Is.True);
        Assert.That(result.Frame.LocalColorTableSizeCode, Is.EqualTo(4));
        Assert.That(result.Frame.LocalColorTableSize, Is.EqualTo(32));
        Assert.That(result.PackedFields, Is.EqualTo(0xE4));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(10));
    }

    [Test]
    public void Read_FromStreamParsesDescriptorWithoutLocalColorTable()
    {
        byte[] data = new byte[] { 0x2C, 0, 0, 0, 0, 1, 0, 2, 0, 0 };
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(data));
        GifImageDescriptorParser parser = new GifImageDescriptorParser();

        GifImageDescriptorReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Frame.Width, Is.EqualTo(1));
        Assert.That(result.Frame.Height, Is.EqualTo(2));
        Assert.That(result.Frame.HasLocalColorTable, Is.False);
        Assert.That(result.Frame.Interlace, Is.False);
        Assert.That(result.Frame.LocalColorTableSortFlag, Is.False);
        Assert.That(result.Frame.LocalColorTableSizeCode, Is.EqualTo(0));
        Assert.That(result.Frame.LocalColorTableSize, Is.EqualTo(2));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(10));
    }

    [Test]
    public void Parse_RejectsDescriptorWithoutImageSeparator()
    {
        byte[] data = new byte[] { 0x21, 0, 0, 0, 0, 1, 0, 1, 0, 0 };
        GifImageDescriptorParser parser = new GifImageDescriptorParser();

        GifImageDescriptorReadResult result = parser.Parse(data);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("ImageDescriptor"));
    }

    [Test]
    public void Read_FromMemoryRejectsShortDescriptorWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 0x2C, 0, 0 });
        GifImageDescriptorParser parser = new GifImageDescriptorParser();

        GifImageDescriptorReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Read_FromStreamRejectsShortDescriptorAfterPartialConsumption()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { 0x2C, 0, 0 }));
        GifImageDescriptorParser parser = new GifImageDescriptorParser();

        GifImageDescriptorReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(3));
        Assert.That(byteReader.EndOfStream, Is.True);
    }

    [Test]
    public void ParseDescriptor_RejectsNullDescriptor()
    {
        GifImageDescriptorParser parser = new GifImageDescriptorParser();

        GifImageDescriptorReadResult result = parser.ParseDescriptor(null!, 6);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.ByteOffset, Is.EqualTo(6));
    }
}
