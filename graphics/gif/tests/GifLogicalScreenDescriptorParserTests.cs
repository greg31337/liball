using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifLogicalScreenDescriptorParserTests
{
    [Test]
    public void Read_FromMemoryParsesDimensionsAndPackedFields()
    {
        byte[] data = new byte[] { 0x10, 0x00, 0x20, 0x00, 0xF7, 5, 10 };
        GifMemoryByteReader byteReader = new GifMemoryByteReader(data);
        GifLogicalScreenDescriptorParser parser = new GifLogicalScreenDescriptorParser();

        GifLogicalScreenDescriptorReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LogicalScreen.Width, Is.EqualTo(16));
        Assert.That(result.LogicalScreen.Height, Is.EqualTo(32));
        Assert.That(result.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(result.LogicalScreen.ColorResolution, Is.EqualTo(8));
        Assert.That(result.LogicalScreen.SortFlag, Is.False);
        Assert.That(result.LogicalScreen.GlobalColorTableSize, Is.EqualTo(256));
        Assert.That(result.LogicalScreen.BackgroundColorIndex, Is.EqualTo(5));
        Assert.That(result.LogicalScreen.PixelAspectRatio, Is.EqualTo(10));
        Assert.That(result.PackedFields, Is.EqualTo(0xF7));
        Assert.That(result.GlobalColorTableSizeCode, Is.EqualTo(7));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(7));
    }

    [Test]
    public void Read_FromStreamParsesNoGlobalColorTableDescriptor()
    {
        byte[] data = new byte[] { 0x01, 0x00, 0x02, 0x00, 0x00, 0, 0 };
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(data));
        GifLogicalScreenDescriptorParser parser = new GifLogicalScreenDescriptorParser();

        GifLogicalScreenDescriptorReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LogicalScreen.Width, Is.EqualTo(1));
        Assert.That(result.LogicalScreen.Height, Is.EqualTo(2));
        Assert.That(result.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(result.LogicalScreen.ColorResolution, Is.EqualTo(1));
        Assert.That(result.LogicalScreen.SortFlag, Is.False);
        Assert.That(result.LogicalScreen.GlobalColorTableSize, Is.EqualTo(2));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(7));
    }

    [Test]
    public void Parse_DecodesSortFlagAndMinimumColorTableSize()
    {
        byte[] data = new byte[] { 0x03, 0x00, 0x04, 0x00, 0x88, 1, 0 };
        GifLogicalScreenDescriptorParser parser = new GifLogicalScreenDescriptorParser();

        GifLogicalScreenDescriptorReadResult result = parser.Parse(data);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(result.LogicalScreen.ColorResolution, Is.EqualTo(1));
        Assert.That(result.LogicalScreen.SortFlag, Is.True);
        Assert.That(result.LogicalScreen.GlobalColorTableSize, Is.EqualTo(2));
        Assert.That(result.GlobalColorTableSizeCode, Is.EqualTo(0));
    }

    [Test]
    public void Read_FromMemoryRejectsShortDescriptorWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 1, 0, 2 });
        GifLogicalScreenDescriptorParser parser = new GifLogicalScreenDescriptorParser();

        GifLogicalScreenDescriptorReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("LogicalScreenDescriptor"));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Read_FromStreamRejectsShortDescriptorAfterPartialConsumption()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { 1, 0, 2 }));
        GifLogicalScreenDescriptorParser parser = new GifLogicalScreenDescriptorParser();

        GifLogicalScreenDescriptorReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(3));
        Assert.That(byteReader.EndOfStream, Is.True);
    }

    [Test]
    public void ParseDescriptor_RejectsNullDescriptor()
    {
        GifLogicalScreenDescriptorParser parser = new GifLogicalScreenDescriptorParser();

        GifLogicalScreenDescriptorReadResult result = parser.ParseDescriptor(null!, 9);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.ByteOffset, Is.EqualTo(9));
    }
}
