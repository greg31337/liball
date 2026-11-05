using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifLocalColorTableEncodeModeTests
{
    [Test]
    public void Apply_ClearsGlobalTableAndAssignsFrameLocalTables()
    {
        GifPalette firstPalette = CreatePalette(new GifColor(0, 0, 0), new GifColor(255, 255, 255));
        GifPalette secondPalette = CreatePalette(new GifColor(255, 0, 0), new GifColor(0, 0, 255));
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(1, 1);
        document.LogicalScreen.HasGlobalColorTable = true;
        document.LogicalScreen.GlobalColorTableSize = 2;
        document.GlobalColorTable = firstPalette;
        document.Frames = new GifFrame[] { new GifFrame(0, 0, 1, 1), new GifFrame(0, 0, 1, 1) };

        GifErrorDiagnostic result = new GifLocalColorTableEncoder().Apply(document, new GifPalette[] { firstPalette, secondPalette });

        Assert.That(result.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[0].LocalColorTable.Colors[1].Red, Is.EqualTo(255));
        Assert.That(document.Frames[1].LocalColorTable.Colors[1].Blue, Is.EqualTo(255));
    }

    [Test]
    public void EncodeIndexedStill_LocalModeWritesLocalTableWithoutGlobalTable()
    {
        GifPalette palette = CreatePalette(new GifColor(0, 0, 0), new GifColor(255, 255, 255));
        GifEncodeOptions options = new GifEncodeOptions();
        options.ColorTableMode = GifColorTableMode.Local;
        GifBitmap bitmap = GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 1 }, palette);

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(parsed.Document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(parsed.Document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[0].LocalColorTable.Count, Is.EqualTo(2));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.IndexedPixels, Is.EqualTo(new byte[] { 0, 1 }));
    }

    [Test]
    public void EncodeIndexedAnimation_LocalModeAllowsDifferentFramePalettes()
    {
        GifPalette firstPalette = CreatePalette(new GifColor(0, 0, 0), new GifColor(255, 255, 255));
        GifPalette secondPalette = CreatePalette(new GifColor(255, 0, 0), new GifColor(0, 0, 255));
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, firstPalette)),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, secondPalette))
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.ColorTableMode = GifColorTableMode.Local;

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(parsed.Document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[0].LocalColorTable.Colors[1].Red, Is.EqualTo(255));
        Assert.That(parsed.Document.Frames[1].LocalColorTable.Colors[1].Blue, Is.EqualTo(255));
    }

    [Test]
    public void EncodeRgbaAnimation_LocalModeQuantizesEachFrameLocalPalette()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 })),
            CreateFrame(new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 }))
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.ColorTableMode = GifColorTableMode.Local;

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(parsed.Document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(parsed.Document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[0].LocalColorTable.Colors[0].Red, Is.EqualTo(255));
        Assert.That(parsed.Document.Frames[1].LocalColorTable.Colors[0].Blue, Is.EqualTo(255));
    }

    [Test]
    public void Apply_RejectsMissingPerFramePalette()
    {
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(1, 1);
        document.Frames = new GifFrame[] { new GifFrame(0, 0, 1, 1) };

        GifErrorDiagnostic result = new GifLocalColorTableEncoder().Apply(document, new GifPalette[0]);

        Assert.That(result.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(document.Frames[0].HasLocalColorTable, Is.False);
    }

    static GifPalette CreatePalette(GifColor first, GifColor second)
    {
        return new GifPalette(new GifColor[] { first, second });
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        return frame;
    }
}
