namespace chart.Tests;

[TestFixture]
public class RawBitmapCompletionTests
{
    [Test]
    public void IndexedBitmap_StoresPaletteIndicesAndReturnsPaletteColors()
    {
        ChartColor[] palette = new ChartColor[]
        {
            new ChartColor(0, 0, 0, 0),
            new ChartColor(240, 10, 20, 128),
            new ChartColor(10, 20, 230, 255)
        };
        RawBitmap bitmap = new RawBitmap(2, 1, ChartPixelFormat.Indexed8);

        bitmap.SetPalette(palette);
        bitmap.SetPaletteIndex(0, 0, 1);
        bitmap.SetPaletteIndex(1, 0, 2);
        palette[1].SetR(1);

        Assert.That(bitmap.GetPaletteCount(), Is.EqualTo(3));
        Assert.That(bitmap.GetPaletteIndex(0, 0), Is.EqualTo(1));
        Assert.That(bitmap.GetPaletteIndex(1, 0), Is.EqualTo(2));
        Assert.That(bitmap.CopyBuffer(), Is.EqualTo(new byte[] { 1, 2 }));
        AssertColor(bitmap.GetPixel(0, 0), 240, 10, 20, 128);
        AssertColor(bitmap.GetPixel(1, 0), 10, 20, 230, 255);
    }

    [Test]
    public void IndexedBitmap_CopiesPaletteAccessAndMapsColorsToNearestEntry()
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Indexed8);
        bitmap.SetPalette(new ChartColor[]
        {
            new ChartColor(0, 0, 0, 255),
            new ChartColor(255, 0, 0, 255),
            new ChartColor(255, 255, 255, 255)
        });

        bitmap.SetPixel(0, 0, new ChartColor(220, 15, 10, 255));
        ChartColor[] paletteCopy = bitmap.GetPalette();
        ChartColor colorCopy = bitmap.GetPaletteColor(1);
        paletteCopy[1].SetG(200);
        colorCopy.SetB(200);

        Assert.That(bitmap.GetPaletteIndex(0, 0), Is.EqualTo(1));
        AssertColor(bitmap.GetPixel(0, 0), 255, 0, 0, 255);

        bitmap.SetPaletteColor(1, new ChartColor(200, 30, 40, 100));

        AssertColor(bitmap.GetPixel(0, 0), 200, 30, 40, 100);
    }

    [Test]
    public void IndexedFactory_WrapsPaddedRowsAndExposesPixelAndFullRowSpans()
    {
        RawBitmapFactory factory = new RawBitmapFactory();
        byte[] buffer = new byte[] { 1, 0, 91, 92, 0, 1, 93, 94 };
        ChartColor[] palette = new ChartColor[]
        {
            new ChartColor(5, 6, 7, 255),
            new ChartColor(200, 201, 202, 50)
        };

        RawBitmap bitmap = factory.WrapIndexed(2, 2, 4, buffer, palette);
        RawBitmap allocated = factory.CreateIndexedWithStride(2, 2, 5, palette);
        System.Span<byte> completeRow = bitmap.GetRowSpan(1);
        System.Span<byte> pixelRow = bitmap.GetPixelRowSpan(1);
        System.ReadOnlySpan<byte> readOnlyRow = bitmap.GetReadOnlyRowSpan(0);
        System.ReadOnlySpan<byte> readOnlyPixelRow = bitmap.GetReadOnlyPixelRowSpan(0);

        Assert.That(completeRow.Length, Is.EqualTo(4));
        Assert.That(pixelRow.Length, Is.EqualTo(2));
        Assert.That(readOnlyRow.Length, Is.EqualTo(4));
        Assert.That(readOnlyPixelRow.Length, Is.EqualTo(2));
        Assert.That(readOnlyRow[2], Is.EqualTo(91));
        Assert.That(allocated.Stride, Is.EqualTo(5));
        Assert.That(allocated.GetPaletteCount(), Is.EqualTo(2));
        AssertColor(allocated.GetPixel(0, 0), 5, 6, 7, 255);
        AssertColor(bitmap.GetPixel(0, 0), 200, 201, 202, 50);
        AssertColor(bitmap.GetPixel(1, 1), 200, 201, 202, 50);

        pixelRow[0] = 1;

        Assert.That(buffer[4], Is.EqualTo(1));
        Assert.That(buffer[6], Is.EqualTo(93));
        AssertColor(bitmap.GetPixel(0, 1), 200, 201, 202, 50);
    }

    [Test]
    public void ConvertTo_HandlesEveryDirectPixelFormatAndAutomaticIndexedPalette()
    {
        RawBitmap source = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        source.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));
        source.SetPixel(1, 0, new ChartColor(200, 150, 100, 255));
        RawBitmapFactory factory = new RawBitmapFactory();

        RawBitmap bgra = factory.Convert(source, ChartPixelFormat.Bgra32);
        RawBitmap rgb = source.ConvertTo(ChartPixelFormat.Rgb24);
        RawBitmap grayscale = source.ConvertTo(ChartPixelFormat.Grayscale8);
        RawBitmap alpha = source.ConvertTo(ChartPixelFormat.Alpha8);
        RawBitmap indexed = source.ConvertTo(ChartPixelFormat.Indexed8);
        RawBitmap restored = indexed.ConvertTo(ChartPixelFormat.Rgba32);

        Assert.That(bgra.CopyBuffer(), Is.EqualTo(new byte[] { 30, 20, 10, 40, 100, 150, 200, 255 }));
        Assert.That(rgb.CopyBuffer(), Is.EqualTo(new byte[] { 10, 20, 30, 200, 150, 100 }));
        Assert.That(grayscale.CopyBuffer(), Is.EqualTo(new byte[] { 18, 159 }));
        Assert.That(alpha.CopyBuffer(), Is.EqualTo(new byte[] { 40, 255 }));
        Assert.That(indexed.GetPaletteCount(), Is.EqualTo(2));
        Assert.That(indexed.CopyBuffer(), Is.EqualTo(new byte[] { 0, 1 }));
        AssertColor(restored.GetPixel(0, 0), 10, 20, 30, 40);
        AssertColor(restored.GetPixel(1, 0), 200, 150, 100, 255);
    }

    [Test]
    public void ConvertToIndexed_UsesExplicitPaletteAndPreservesTransparentEntries()
    {
        RawBitmap source = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        source.SetPixel(0, 0, new ChartColor(1, 2, 3, 2));
        source.SetPixel(1, 0, new ChartColor(245, 4, 7, 250));
        ChartColor[] palette = new ChartColor[]
        {
            new ChartColor(0, 0, 0, 0),
            new ChartColor(255, 0, 0, 255)
        };

        RawBitmap indexed = new RawBitmapFactory().ConvertToIndexed(source, palette);

        Assert.That(indexed.CopyBuffer(), Is.EqualTo(new byte[] { 0, 1 }));
        AssertColor(indexed.GetPixel(0, 0), 0, 0, 0, 0);
        AssertColor(indexed.GetPixel(1, 0), 255, 0, 0, 255);
    }

    [Test]
    public void AutomaticIndexedConversion_QuantizesMoreThan256DistinctColors()
    {
        RawBitmap source = new RawBitmap(257, 1, ChartPixelFormat.Rgba32);
        int x = 0;
        while (x < source.Width)
        {
            source.SetPixel(x, 0, new ChartColor((byte)x, (byte)(x / 256), (byte)(255 - (x % 256)), (byte)(x % 255)));
            x = x + 1;
        }

        RawBitmap indexed = source.ConvertTo(ChartPixelFormat.Indexed8);

        Assert.That(indexed.GetPaletteCount(), Is.EqualTo(256));
        Assert.That(indexed.Stride, Is.EqualTo(257));
        Assert.That(indexed.GetPaletteColor(0).A, Is.EqualTo(0));
        Assert.That(indexed.GetPaletteColor(255).A, Is.EqualTo(255));
        Assert.That(indexed.GetPaletteIndex(256, 0), Is.InRange(0, 255));
    }

    [Test]
    public void CopyAndExport_PreserveStridePaddingAndPaletteIndependently()
    {
        RawBitmap source = new RawBitmapFactory().WrapIndexed(
            1,
            2,
            3,
            new byte[] { 1, 77, 78, 0, 79, 80 },
            new ChartColor[]
            {
                new ChartColor(10, 20, 30, 40),
                new ChartColor(50, 60, 70, 80)
            });

        RawBitmap copy = source.Copy();
        RawBitmap exported = ChartExportOperations.ToRawBitmap(source);
        ChartRenderStyle style = new ChartRenderStyle();
        style.SetFillPattern(source);
        RawBitmap pattern = style.GetFillPattern();
        source.SetPaletteColor(1, new ChartColor(1, 2, 3, 4));
        source.GetBufferSpan()[1] = 0;

        Assert.That(copy.Stride, Is.EqualTo(3));
        Assert.That(copy.CopyBuffer(), Is.EqualTo(new byte[] { 1, 77, 78, 0, 79, 80 }));
        Assert.That(exported.CopyBuffer(), Is.EqualTo(new byte[] { 1, 77, 78, 0, 79, 80 }));
        AssertColor(copy.GetPixel(0, 0), 50, 60, 70, 80);
        AssertColor(exported.GetPixel(0, 0), 50, 60, 70, 80);
        AssertColor(pattern.GetPixel(0, 0), 50, 60, 70, 80);
    }

    [Test]
    public void SameFormatConversion_PacksRowsWithoutChangingPaletteIndices()
    {
        RawBitmap source = new RawBitmapFactory().WrapIndexed(
            2,
            2,
            4,
            new byte[] { 2, 1, 77, 78, 0, 2, 79, 80 },
            new ChartColor[]
            {
                new ChartColor(1, 2, 3, 4),
                new ChartColor(5, 6, 7, 8),
                new ChartColor(9, 10, 11, 12)
            });

        RawBitmap converted = source.ConvertTo(ChartPixelFormat.Indexed8);

        Assert.That(converted.Stride, Is.EqualTo(2));
        Assert.That(converted.CopyBuffer(), Is.EqualTo(new byte[] { 2, 1, 0, 2 }));
        Assert.That(converted.GetPaletteCount(), Is.EqualTo(3));
        AssertColor(converted.GetPixel(0, 0), 9, 10, 11, 12);
    }

    [Test]
    public void PaletteAndRowValidation_RejectsInvalidOperations()
    {
        RawBitmap direct = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        RawBitmap indexed = new RawBitmap(1, 1, ChartPixelFormat.Indexed8);
        ChartColor[] tooLarge = new ChartColor[257];
        int index = 0;
        while (index < tooLarge.Length)
        {
            tooLarge[index] = new ChartColor(0, 0, 0, 255);
            index = index + 1;
        }

        Assert.Throws<System.InvalidOperationException>(() => direct.SetPalette(new ChartColor[] { ChartColor.Black() }));
        Assert.Throws<System.InvalidOperationException>(() => direct.GetPaletteIndex(0, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => indexed.SetPalette(new ChartColor[0]));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => indexed.SetPalette(tooLarge));
        Assert.Throws<System.ArgumentException>(() => indexed.SetPalette(new ChartColor[] { null! }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => indexed.SetPaletteIndex(0, 0, 256));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => indexed.GetPixelRowSpan(-1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => indexed.GetReadOnlyRowSpan(1));
        Assert.Throws<System.ArgumentException>(() => new RawBitmapFactory().WrapIndexed(
            1,
            1,
            1,
            new byte[] { 1 },
            new ChartColor[] { ChartColor.Black() }));
    }

    static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.R, Is.EqualTo((byte)red));
        Assert.That(color.G, Is.EqualTo((byte)green));
        Assert.That(color.B, Is.EqualTo((byte)blue));
        Assert.That(color.A, Is.EqualTo((byte)alpha));
    }
}
