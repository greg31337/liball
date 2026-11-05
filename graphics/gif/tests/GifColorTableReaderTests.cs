using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifColorTableReaderTests
{
    [Test]
    public void Read_FromMemoryReadsRgbTriplesIntoPalette()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 1, 2, 3, 4, 5, 6, 99 });
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableReadResult result = reader.Read(byteReader, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ColorCount, Is.EqualTo(2));
        Assert.That(result.BytesRead, Is.EqualTo(6));
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        Assert.That(result.Palette.Colors[0].Red, Is.EqualTo(1));
        Assert.That(result.Palette.Colors[0].Green, Is.EqualTo(2));
        Assert.That(result.Palette.Colors[0].Blue, Is.EqualTo(3));
        Assert.That(result.Palette.Colors[1].Red, Is.EqualTo(4));
        Assert.That(result.Palette.Colors[1].Green, Is.EqualTo(5));
        Assert.That(result.Palette.Colors[1].Blue, Is.EqualTo(6));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void Read_FromStreamReadsRgbTriplesIntoPalette()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { 10, 11, 12, 20, 21, 22 }));
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableReadResult result = reader.Read(byteReader, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        Assert.That(result.Palette.Colors[0].Red, Is.EqualTo(10));
        Assert.That(result.Palette.Colors[1].Blue, Is.EqualTo(22));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void ParseColorTable_ReadsMaximumValidPalette()
    {
        byte[] data = new byte[GifConstants.MaximumColorTableSize * GifConstants.ColorTableEntrySize];
        data[0] = 1;
        data[1] = 2;
        data[2] = 3;
        data[data.Length - 3] = 4;
        data[data.Length - 2] = 5;
        data[data.Length - 1] = 6;
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableReadResult result = reader.ParseColorTable(data, GifConstants.MaximumColorTableSize, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Palette.Count, Is.EqualTo(256));
        Assert.That(result.Palette.Colors[0].Red, Is.EqualTo(1));
        Assert.That(result.Palette.Colors[255].Blue, Is.EqualTo(6));
    }

    [Test]
    public void Read_RejectsInvalidColorCountsBeforeReading()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 1, 2, 3, 4, 5, 6 });
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableReadResult tooSmall = reader.Read(byteReader, 1);
        GifColorTableReadResult tooLarge = reader.Read(byteReader, 257);

        Assert.That(tooSmall.Success, Is.False);
        Assert.That(tooSmall.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(tooLarge.Success, Is.False);
        Assert.That(tooLarge.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
    }

    [Test]
    public void Read_FromMemoryRejectsShortColorTableWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 1, 2, 3 });
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableReadResult result = reader.Read(byteReader, 2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Read_FromStreamRejectsShortColorTableAfterPartialConsumption()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { 1, 2, 3 }));
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableReadResult result = reader.Read(byteReader, 2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(3));
        Assert.That(byteReader.EndOfStream, Is.True);
    }

    [Test]
    public void ParseColorTable_RejectsMismatchedByteCount()
    {
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableReadResult result = reader.ParseColorTable(new byte[] { 1, 2, 3 }, 2, 12);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.ByteOffset, Is.EqualTo(12));
    }
}
