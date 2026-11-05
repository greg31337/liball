using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifLogicalScreenDescriptorWriterTests
{
    [Test]
    public void Create_WritesDescriptorBytesAndPackedFields()
    {
        GifLogicalScreen logicalScreen = new GifLogicalScreen(16, 32);
        logicalScreen.HasGlobalColorTable = true;
        logicalScreen.GlobalColorTableSize = 256;
        logicalScreen.ColorResolution = 8;
        logicalScreen.SortFlag = false;
        logicalScreen.BackgroundColorIndex = 5;
        logicalScreen.PixelAspectRatio = 10;
        GifLogicalScreenDescriptorWriter writer = new GifLogicalScreenDescriptorWriter();

        GifLogicalScreenDescriptorWriteResult result = writer.Create(logicalScreen);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x10, 0x00, 0x20, 0x00, 0xF7, 5, 10 }));
        Assert.That(result.PackedFields, Is.EqualTo(0xF7));
        Assert.That(result.BytesWritten, Is.EqualTo(7));
    }

    [Test]
    public void Create_WritesSortFlagAndMinimumGlobalColorTableSize()
    {
        GifLogicalScreen logicalScreen = new GifLogicalScreen(3, 4);
        logicalScreen.HasGlobalColorTable = true;
        logicalScreen.GlobalColorTableSize = 2;
        logicalScreen.ColorResolution = 1;
        logicalScreen.SortFlag = true;
        logicalScreen.BackgroundColorIndex = 1;
        GifLogicalScreenDescriptorWriter writer = new GifLogicalScreenDescriptorWriter();

        GifLogicalScreenDescriptorWriteResult result = writer.Create(logicalScreen);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 3, 0, 4, 0, 0x88, 1, 0 }));
    }

    [Test]
    public void Create_RoundTripsThroughParser()
    {
        GifLogicalScreen logicalScreen = new GifLogicalScreen(100, 50);
        logicalScreen.HasGlobalColorTable = true;
        logicalScreen.GlobalColorTableSize = 16;
        logicalScreen.ColorResolution = 5;
        logicalScreen.SortFlag = true;
        logicalScreen.BackgroundColorIndex = 3;
        logicalScreen.PixelAspectRatio = 7;
        GifLogicalScreenDescriptorWriter writer = new GifLogicalScreenDescriptorWriter();
        GifLogicalScreenDescriptorParser parser = new GifLogicalScreenDescriptorParser();

        GifLogicalScreenDescriptorWriteResult writeResult = writer.Create(logicalScreen);
        GifLogicalScreenDescriptorReadResult readResult = parser.Parse(writeResult.EncodedData);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.LogicalScreen.Width, Is.EqualTo(100));
        Assert.That(readResult.LogicalScreen.Height, Is.EqualTo(50));
        Assert.That(readResult.LogicalScreen.GlobalColorTableSize, Is.EqualTo(16));
        Assert.That(readResult.LogicalScreen.ColorResolution, Is.EqualTo(5));
        Assert.That(readResult.LogicalScreen.SortFlag, Is.True);
        Assert.That(readResult.LogicalScreen.BackgroundColorIndex, Is.EqualTo(3));
        Assert.That(readResult.LogicalScreen.PixelAspectRatio, Is.EqualTo(7));
    }

    [Test]
    public void Write_WritesToProvidedStreamAndAdvancesOffset()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifLogicalScreen logicalScreen = new GifLogicalScreen(1, 2);
        GifLogicalScreenDescriptorWriter writer = new GifLogicalScreenDescriptorWriter();

        GifLogicalScreenDescriptorWriteResult result = writer.Write(byteWriter, logicalScreen);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 0, 2, 0, 0, 0, 0 }));
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(7));
    }

    [Test]
    public void Create_RejectsInvalidGlobalColorTableSize()
    {
        GifLogicalScreen logicalScreen = new GifLogicalScreen(1, 1);
        logicalScreen.HasGlobalColorTable = true;
        logicalScreen.GlobalColorTableSize = 3;
        GifLogicalScreenDescriptorWriter writer = new GifLogicalScreenDescriptorWriter();

        GifLogicalScreenDescriptorWriteResult result = writer.Create(logicalScreen);

        Assert.That(result.Success, Is.False);
        Assert.That(result.EncodedData, Is.Empty);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void Create_RejectsUnencodableDimensionOrBackgroundIndex()
    {
        GifLogicalScreen badWidth = new GifLogicalScreen(70000, 1);
        GifLogicalScreen badBackground = new GifLogicalScreen(1, 1);
        badBackground.BackgroundColorIndex = 256;
        GifLogicalScreenDescriptorWriter writer = new GifLogicalScreenDescriptorWriter();

        Assert.That(writer.Create(badWidth).Error.Code, Is.EqualTo(GifErrorCode.DimensionLimitExceeded));
        Assert.That(writer.Create(badBackground).Error.Code, Is.EqualTo(GifErrorCode.DimensionLimitExceeded));
    }

    [Test]
    public void CreatePackedFields_ReturnsMinusOneForInvalidColorResolution()
    {
        GifLogicalScreen logicalScreen = new GifLogicalScreen(1, 1);
        logicalScreen.ColorResolution = 9;
        GifLogicalScreenDescriptorWriter writer = new GifLogicalScreenDescriptorWriter();

        Assert.That(writer.CreatePackedFields(logicalScreen), Is.EqualTo(-1));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifLogicalScreenDescriptorWriter writer = new GifLogicalScreenDescriptorWriter();

        GifLogicalScreenDescriptorWriteResult result = writer.Write(null!, new GifLogicalScreen(1, 1));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }
}
