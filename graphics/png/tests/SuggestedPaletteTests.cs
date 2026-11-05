using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class SuggestedPaletteTests
{
    [Test]
    public void Parse_Reads8BitEntriesUsingOffset()
    {
        byte[] payload = Create8BitPayload();
        byte[] data = WrapPayload(payload);

        PngSuggestedPalette palette = PngSuggestedPaletteParser.Parse(data, 1, payload.Length);

        Assert8BitPalette(palette);
    }

    [Test]
    public void Parse_Reads16BitEntries()
    {
        PngSuggestedPalette palette = PngSuggestedPaletteParser.Parse(Create16BitPayload());

        Assert16BitPalette(palette);
    }

    [Test]
    public void Parse_AllowsPaletteWithNoEntries()
    {
        PngSuggestedPalette palette = PngSuggestedPaletteParser.Parse(new byte[] { 65, 0, 8 });

        Assert.That(palette.Name, Is.EqualTo("A"));
        Assert.That(palette.SampleDepth, Is.EqualTo(8));
        Assert.That(palette.Entries, Is.Empty);
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkSuggestedPalette, Create8BitPayload());

        PngSuggestedPalette palette = PngSuggestedPaletteParser.Parse(chunk);

        Assert8BitPalette(palette);
    }

    [Test]
    public void WriteData_Writes8BitEntries()
    {
        byte[] data = PngSuggestedPaletteWriter.WriteData(Create8BitPalette());

        Assert.That(data, Is.EqualTo(Create8BitPayload()));
    }

    [Test]
    public void WriteData_Writes16BitEntries()
    {
        byte[] data = PngSuggestedPaletteWriter.WriteData(Create16BitPalette());

        Assert.That(data, Is.EqualTo(Create16BitPayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        byte[] payload = Create8BitPayload();
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;

        PngSuggestedPaletteWriter.WriteData(data, 1, Create8BitPalette());

        Assert.That(data, Is.EqualTo(WrapPayload(payload)));
    }

    [Test]
    public void WriteChunk_CreatesKnownSpltChunk()
    {
        PngChunk chunk = PngSuggestedPaletteWriter.WriteChunk(Create8BitPalette());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkSuggestedPalette));
        Assert.That(chunk.Data, Is.EqualTo(Create8BitPayload()));
        Assert.That(chunk.Length, Is.EqualTo(Create8BitPayload().Length));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngSuggestedPaletteWriter.WriteChunk(Create16BitPalette());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngSuggestedPalette palette = PngSuggestedPaletteParser.Parse(readChunk);

        Assert16BitPalette(palette);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkSuggestedPalette, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = Create8BitPayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkSuggestedPalette;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, Create8BitPayload());

        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteParser.Parse(null!, 0, 3));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteParser.Parse(new byte[3], -1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteParser.Parse(new byte[3], 4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteParser.Parse(new byte[3], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteParser.Parse(new byte[3], 1, 3));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(new byte[] { 65, 8 }));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(new byte[] { 0, 8 }));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(new byte[] { 65, 0 }));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(CreateLongNamePayload()));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(new byte[] { 32, 65, 0, 8 }));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(new byte[] { 65, 0, 7 }));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(new byte[] { 65, 0, 8, 1 }));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(new byte[] { 65, 0, 16, 1, 2, 3, 4, 5, 6 }));
        Assert.Throws<InvalidDataException>(() => PngSuggestedPaletteParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngSuggestedPalette palette = Create8BitPalette();
        PngSuggestedPalette nullName = Create8BitPalette();
        nullName.Name = null!;
        PngSuggestedPalette nullEntries = Create8BitPalette();
        nullEntries.Entries = null!;
        PngSuggestedPalette nullEntry = Create8BitPalette();
        nullEntry.Entries = new PngSuggestedPaletteEntry[] { null! };
        PngSuggestedPalette emptyName = Create8BitPalette();
        emptyName.Name = "";
        PngSuggestedPalette longName = Create8BitPalette();
        longName.Name = new string('A', 80);
        PngSuggestedPalette invalidName = Create8BitPalette();
        invalidName.Name = "Bad  Name";
        PngSuggestedPalette invalidDepth = Create8BitPalette();
        invalidDepth.SampleDepth = 7;
        PngSuggestedPalette oversized8BitSample = Create8BitPalette();
        oversized8BitSample.Entries[0].Red = 256;

        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteWriter.WriteData(null!, 0, palette));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteWriter.WriteData(new byte[Create8BitPayload().Length], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteWriter.WriteChunk(null!));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteWriter.WriteData(nullName));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteWriter.WriteData(nullEntries));
        Assert.Throws<ArgumentNullException>(() => PngSuggestedPaletteWriter.WriteData(nullEntry));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteWriter.WriteData(emptyName));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteWriter.WriteData(longName));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteWriter.WriteData(invalidName));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteWriter.WriteData(invalidDepth));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteWriter.WriteData(oversized8BitSample));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteWriter.WriteData(new byte[Create8BitPayload().Length], -1, palette));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSuggestedPaletteWriter.WriteData(new byte[Create8BitPayload().Length], 1, palette));
    }

    static PngSuggestedPalette Create8BitPalette()
    {
        PngSuggestedPalette palette = new PngSuggestedPalette();
        palette.Name = "Web";
        palette.SampleDepth = PngSuggestedPaletteParser.SampleDepth8;
        palette.Entries = new PngSuggestedPaletteEntry[]
        {
            CreateEntry(1, 2, 3, 4, 0x1234),
            CreateEntry(255, 128, 64, 0, 0xFFFF)
        };
        return palette;
    }

    static PngSuggestedPalette Create16BitPalette()
    {
        PngSuggestedPalette palette = new PngSuggestedPalette();
        palette.Name = "Deep";
        palette.SampleDepth = PngSuggestedPaletteParser.SampleDepth16;
        palette.Entries = new PngSuggestedPaletteEntry[]
        {
            CreateEntry(0x0102, 0x0304, 0x0506, 0x0708, 0x090A)
        };
        return palette;
    }

    static PngSuggestedPaletteEntry CreateEntry(int red, int green, int blue, int alpha, int frequency)
    {
        PngSuggestedPaletteEntry entry = new PngSuggestedPaletteEntry();
        entry.Red = (ushort)red;
        entry.Green = (ushort)green;
        entry.Blue = (ushort)blue;
        entry.Alpha = (ushort)alpha;
        entry.Frequency = (ushort)frequency;
        return entry;
    }

    static byte[] Create8BitPayload()
    {
        return new byte[]
        {
            87, 101, 98, 0, 8,
            1, 2, 3, 4, 0x12, 0x34,
            255, 128, 64, 0, 0xFF, 0xFF
        };
    }

    static byte[] Create16BitPayload()
    {
        return new byte[]
        {
            68, 101, 101, 112, 0, 16,
            0x01, 0x02,
            0x03, 0x04,
            0x05, 0x06,
            0x07, 0x08,
            0x09, 0x0A
        };
    }

    static byte[] WrapPayload(byte[] payload)
    {
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;
        CopyBytes(payload, 0, data, 1, payload.Length);
        return data;
    }

    static byte[] CreateLongNamePayload()
    {
        byte[] data = new byte[82];
        int index = 0;
        while (index < 80)
        {
            data[index] = 65;
            index += 1;
        }

        data[80] = 0;
        data[81] = 8;
        return data;
    }

    static void Assert8BitPalette(PngSuggestedPalette palette)
    {
        Assert.That(palette.Name, Is.EqualTo("Web"));
        Assert.That(palette.SampleDepth, Is.EqualTo(8));
        Assert.That(palette.Entries, Has.Length.EqualTo(2));
        AssertEntry(palette.Entries[0], 1, 2, 3, 4, 0x1234);
        AssertEntry(palette.Entries[1], 255, 128, 64, 0, 0xFFFF);
    }

    static void Assert16BitPalette(PngSuggestedPalette palette)
    {
        Assert.That(palette.Name, Is.EqualTo("Deep"));
        Assert.That(palette.SampleDepth, Is.EqualTo(16));
        Assert.That(palette.Entries, Has.Length.EqualTo(1));
        AssertEntry(palette.Entries[0], 0x0102, 0x0304, 0x0506, 0x0708, 0x090A);
    }

    static void AssertEntry(PngSuggestedPaletteEntry entry, int red, int green, int blue, int alpha, int frequency)
    {
        Assert.That(entry.Red, Is.EqualTo(red));
        Assert.That(entry.Green, Is.EqualTo(green));
        Assert.That(entry.Blue, Is.EqualTo(blue));
        Assert.That(entry.Alpha, Is.EqualTo(alpha));
        Assert.That(entry.Frequency, Is.EqualTo(frequency));
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.IsKnown = true;
        return chunk;
    }

    static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
