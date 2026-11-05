using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifColorTableWriterTests
{
    [Test]
    public void Create_WritesPaletteAsRgbTriples()
    {
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(1, 2, 3), new GifColor(4, 5, 6) });
        GifColorTableWriter writer = new GifColorTableWriter();

        GifColorTableWriteResult result = writer.Create(palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ColorCount, Is.EqualTo(2));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Write_WritesToProvidedStreamAndAdvancesOffset()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(10, 11, 12), new GifColor(20, 21, 22) });
        GifColorTableWriter writer = new GifColorTableWriter();

        GifColorTableWriteResult result = writer.Write(byteWriter, palette);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 10, 11, 12, 20, 21, 22 }));
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void Create_RoundTripsThroughColorTableReader()
    {
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(7, 8, 9), new GifColor(17, 18, 19) });
        GifColorTableWriter writer = new GifColorTableWriter();
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableWriteResult writeResult = writer.Create(palette);
        GifColorTableReadResult readResult = reader.ParseColorTable(writeResult.EncodedData, 2, 0);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Palette.Colors[0].Red, Is.EqualTo(7));
        Assert.That(readResult.Palette.Colors[1].Blue, Is.EqualTo(19));
    }

    [Test]
    public void Create_WritesOnlyPaletteCountEntries()
    {
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(1, 2, 3), new GifColor(4, 5, 6), new GifColor(7, 8, 9) });
        palette.Count = 2;
        GifColorTableWriter writer = new GifColorTableWriter();

        GifColorTableWriteResult result = writer.Create(palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Create_RejectsMissingOrInvalidPalette()
    {
        GifPalette tooSmall = new GifPalette(new GifColor[] { new GifColor(1, 2, 3) });
        GifPalette tooLarge = new GifPalette(new GifColor[257]);
        GifPalette missingColors = new GifPalette(null);
        missingColors.Count = 2;
        GifColorTableWriter writer = new GifColorTableWriter();

        Assert.That(writer.Create(null!).Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(writer.Create(tooSmall).Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(writer.Create(tooLarge).Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(writer.Create(missingColors).Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void Create_RejectsNullColorEntry()
    {
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(1, 2, 3), null! });
        GifColorTableWriter writer = new GifColorTableWriter();

        GifColorTableWriteResult result = writer.Create(palette);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifColorTableWriter writer = new GifColorTableWriter();
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(1, 2, 3), new GifColor(4, 5, 6) });

        GifColorTableWriteResult result = writer.Write(null!, palette);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[6], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(1, 2, 3), new GifColor(4, 5, 6) });
        GifColorTableWriter writer = new GifColorTableWriter();

        GifColorTableWriteResult result = writer.Write(byteWriter, palette);

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }
}
