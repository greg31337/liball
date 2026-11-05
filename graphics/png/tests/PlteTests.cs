using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PlteTests
{
    [Test]
    public void PngPaletteModels_UsePublicFieldsOnly()
    {
        Assert.That(typeof(PngPaletteEntry).GetProperties(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngPaletteEntry).GetEvents(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngPaletteEntry).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(PngPaletteEntry).GetFields(BindingFlags.Public | BindingFlags.Instance), Has.Length.EqualTo(3));

        Assert.That(typeof(PngPalette).GetProperties(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngPalette).GetEvents(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngPalette).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(PngPalette).GetFields(BindingFlags.Public | BindingFlags.Instance), Has.Length.EqualTo(1));
    }

    [Test]
    public void Parse_ReadsPaletteEntries()
    {
        byte[] data = new byte[] { 255, 0, 0, 0, 128, 255 };

        PngPalette palette = PngPlteParser.Parse(data);

        Assert.That(palette.Entries, Has.Length.EqualTo(2));
        AssertEntry(palette.Entries[0], 255, 0, 0);
        AssertEntry(palette.Entries[1], 0, 128, 255);
    }

    [Test]
    public void Parse_ReadsPayloadAtOffsetAndLength()
    {
        byte[] data = new byte[] { 9, 8, 1, 2, 3, 4, 5, 6, 7 };

        PngPalette palette = PngPlteParser.Parse(data, 2, 6);

        Assert.That(palette.Entries, Has.Length.EqualTo(2));
        AssertEntry(palette.Entries[0], 1, 2, 3);
        AssertEntry(palette.Entries[1], 4, 5, 6);
    }

    [Test]
    public void Parse_ReadsPayloadAtLastValidOffset()
    {
        byte[] data = new byte[] { 9, 8, 7, 11, 22, 33 };

        PngPalette palette = PngPlteParser.Parse(data, 3, 3);

        Assert.That(palette.Entries, Has.Length.EqualTo(1));
        AssertEntry(palette.Entries[0], 11, 22, 33);
    }

    [Test]
    public void Parse_ReadsPlteChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkPLTE;
        chunk.Data = new byte[] { 10, 20, 30 };

        PngPalette palette = PngPlteParser.Parse(chunk);

        Assert.That(palette.Entries, Has.Length.EqualTo(1));
        AssertEntry(palette.Entries[0], 10, 20, 30);
    }

    [Test]
    public void Parse_AllowsEmptyPayloadForParserOnly()
    {
        PngPalette palette = PngPlteParser.Parse(new byte[0]);

        Assert.That(palette.Entries, Is.Empty);
    }

    [Test]
    public void WriteData_EmitsPaletteBytes()
    {
        PngPalette palette = CreatePalette(
            CreateEntry(1, 2, 3),
            CreateEntry(250, 251, 252));

        byte[] data = PngPlteWriter.WriteData(palette);

        Assert.That(data, Is.EqualTo(new byte[] { 1, 2, 3, 250, 251, 252 }));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngPalette palette = CreatePalette(CreateEntry(7, 8, 9));
        byte[] data = new byte[] { 1, 1, 1, 1, 1 };

        PngPlteWriter.WriteData(data, 1, palette);

        Assert.That(data, Is.EqualTo(new byte[] { 1, 7, 8, 9, 1 }));
    }

    [Test]
    public void WriteData_WritesAtLastValidOffset()
    {
        PngPalette palette = CreatePalette(CreateEntry(7, 8, 9));
        byte[] data = new byte[] { 1, 1, 1, 1, 1, 1 };

        PngPlteWriter.WriteData(data, 3, palette);

        Assert.That(data, Is.EqualTo(new byte[] { 1, 1, 1, 7, 8, 9 }));
    }

    [Test]
    public void WriteChunk_CreatesPlteChunkModel()
    {
        PngPalette palette = CreatePalette(CreateEntry(3, 2, 1));

        PngChunk chunk = PngPlteWriter.WriteChunk(palette);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkPLTE));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 3, 2, 1 }));
        Assert.That(chunk.Length, Is.EqualTo(3));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.True);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkReaderAndParser()
    {
        PngPalette palette = CreatePalette(
            CreateEntry(0, 0, 0),
            CreateEntry(255, 255, 255),
            CreateEntry(12, 34, 56));

        PngChunk chunk = PngPlteWriter.WriteChunk(palette);
        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngPalette readPalette = PngPlteParser.Parse(readChunk);

        Assert.That(readChunk.Type, Is.EqualTo(PngConstants.ChunkPLTE));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkPLTE, readChunk.Data)));
        Assert.That(readPalette.Entries, Has.Length.EqualTo(3));
        AssertEntry(readPalette.Entries[0], 0, 0, 0);
        AssertEntry(readPalette.Entries[1], 255, 255, 255);
        AssertEntry(readPalette.Entries[2], 12, 34, 56);
    }

    [Test]
    public void Parse_RejectsInvalidInputs()
    {
        PngChunk wrongType = new PngChunk();
        wrongType.Type = PngConstants.ChunkIHDR;
        wrongType.Data = new byte[] { 1, 2, 3 };
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 1, 2, 3 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkPLTE;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngPlteParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteParser.Parse(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngPlteParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngPlteParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPlteParser.Parse(new byte[3], -1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPlteParser.Parse(new byte[3], 4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPlteParser.Parse(new byte[3], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPlteParser.Parse(new byte[3], 1, 3));
        Assert.Throws<InvalidDataException>(() => PngPlteParser.Parse(new byte[] { 1, 2 }));
        Assert.Throws<InvalidDataException>(() => PngPlteParser.Parse(wrongType));
    }

    [Test]
    public void WriteData_RejectsInvalidInputs()
    {
        PngPalette valid = CreatePalette(CreateEntry(1, 2, 3));
        PngPalette nullEntries = new PngPalette();
        nullEntries.Entries = null!;
        PngPalette nullEntry = new PngPalette();
        nullEntry.Entries = new PngPaletteEntry[] { null! };

        Assert.Throws<ArgumentNullException>(() => PngPlteWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteWriter.WriteData(null!, 0, valid));
        Assert.Throws<ArgumentNullException>(() => PngPlteWriter.WriteData(new byte[3], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteWriter.WriteData(nullEntries));
        Assert.Throws<ArgumentNullException>(() => PngPlteWriter.WriteData(nullEntry));
        Assert.Throws<ArgumentNullException>(() => PngPlteWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPlteWriter.WriteData(new byte[3], -1, valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPlteWriter.WriteData(new byte[3], 1, valid));
    }

    static PngPaletteEntry CreateEntry(int red, int green, int blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = (byte)red;
        entry.Green = (byte)green;
        entry.Blue = (byte)blue;
        return entry;
    }

    static PngPalette CreatePalette(params PngPaletteEntry[] entries)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = entries;
        return palette;
    }

    static void AssertEntry(PngPaletteEntry entry, int red, int green, int blue)
    {
        Assert.That(entry.Red, Is.EqualTo((byte)red));
        Assert.That(entry.Green, Is.EqualTo((byte)green));
        Assert.That(entry.Blue, Is.EqualTo((byte)blue));
    }
}
