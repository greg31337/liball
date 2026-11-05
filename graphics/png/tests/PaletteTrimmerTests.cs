using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PaletteTrimmerTests
{
    [Test]
    public void Trim_TrimsIndexed8PaletteToHighestUsedIndex()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed8, 4, 1, 4, new byte[] { 0, 2, 1, 2 });
        PngPalette palette = CreatePalette(5);

        PngPalette trimmed = PngPaletteTrimmer.Trim(buffer, palette);

        Assert.That(trimmed.Entries, Has.Length.EqualTo(3));
        AssertEntry(trimmed.Entries[0], 0, 1, 2);
        AssertEntry(trimmed.Entries[1], 1, 2, 3);
        AssertEntry(trimmed.Entries[2], 2, 3, 4);
    }

    [Test]
    public void Trim_ExpandsPackedIndexedPixelsBeforeFindingHighestIndex()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed4, 3, 2, 3, new byte[]
        {
            0x12, 0x30, 99,
            0x01, 0x20, 88
        });
        PngPalette palette = CreatePalette(6);

        PngPalette trimmed = PngPaletteTrimmer.Trim(buffer, palette);

        Assert.That(trimmed.Entries, Has.Length.EqualTo(4));
        AssertEntry(trimmed.Entries[3], 3, 4, 5);
    }

    [Test]
    public void Trim_ReturnsIndependentPaletteEntries()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed8, 2, 1, 2, new byte[] { 0, 1 });
        PngPalette palette = CreatePalette(3);

        PngPalette trimmed = PngPaletteTrimmer.Trim(buffer, palette);
        palette.Entries[0].Red = 99;

        Assert.That(trimmed.Entries, Has.Length.EqualTo(2));
        Assert.That(trimmed.Entries[0], Is.Not.SameAs(palette.Entries[0]));
        AssertEntry(trimmed.Entries[0], 0, 1, 2);
    }

    [Test]
    public void Trim_AllowsUnusedTrailingNullEntries()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed8, 2, 1, 2, new byte[] { 0, 1 });
        PngPalette palette = CreatePalette(3);
        palette.Entries[2] = null!;

        PngPalette trimmed = PngPaletteTrimmer.Trim(buffer, palette);

        Assert.That(trimmed.Entries, Has.Length.EqualTo(2));
        AssertEntry(trimmed.Entries[1], 1, 2, 3);
    }

    [Test]
    public void Trim_RejectsUsedIndexOutsidePalette()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed8, 3, 1, 3, new byte[] { 0, 1, 2 });
        PngPalette palette = CreatePalette(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteTrimmer.Trim(buffer, palette));
    }

    [Test]
    public void Trim_RejectsInvalidInputs()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 });
        PngPalette palette = CreatePalette(1);
        PngPalette nullEntries = new PngPalette();
        nullEntries.Entries = null!;
        PngPalette nullUsedEntry = new PngPalette();
        nullUsedEntry.Entries = new PngPaletteEntry[] { null! };

        Assert.Throws<ArgumentNullException>(() => PngPaletteTrimmer.Trim(null!, palette));
        Assert.Throws<ArgumentNullException>(() => PngPaletteTrimmer.Trim(buffer, null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteTrimmer.Trim(buffer, nullEntries));
        Assert.Throws<ArgumentNullException>(() => PngPaletteTrimmer.Trim(buffer, nullUsedEntry));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteTrimmer.Trim(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 }), palette));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteTrimmer.Trim(CreateBuffer(PngPixelFormat.Indexed8, 0, 1, 1, new byte[] { 0 }), palette));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteTrimmer.Trim(CreateBuffer(PngPixelFormat.Indexed8, 2, 1, 1, new byte[] { 0, 1 }), palette));
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    static PngPalette CreatePalette(int entryCount)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[entryCount];
        int entryIndex = 0;
        while (entryIndex < entryCount)
        {
            palette.Entries[entryIndex] = CreateEntry(entryIndex, entryIndex + 1, entryIndex + 2);
            entryIndex += 1;
        }

        return palette;
    }

    static PngPaletteEntry CreateEntry(int red, int green, int blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = (byte)red;
        entry.Green = (byte)green;
        entry.Blue = (byte)blue;
        return entry;
    }

    static void AssertEntry(PngPaletteEntry entry, int red, int green, int blue)
    {
        Assert.That(entry.Red, Is.EqualTo(red));
        Assert.That(entry.Green, Is.EqualTo(green));
        Assert.That(entry.Blue, Is.EqualTo(blue));
    }
}
