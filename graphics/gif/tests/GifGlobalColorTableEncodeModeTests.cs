using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifGlobalColorTableEncodeModeTests
{
    [Test]
    public void Apply_SetsLogicalScreenGlobalTableAndClearsFrameLocalTables()
    {
        GifPalette palette = CreatePalette();
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.HasLocalColorTable = true;
        frame.LocalColorTableSortFlag = true;
        frame.LocalColorTableSize = 2;
        frame.LocalColorTableSizeCode = 0;
        frame.LocalColorTable = palette;
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(1, 1);
        document.Frames = new GifFrame[] { frame };

        GifErrorDiagnostic result = new GifGlobalColorTableEncoder().Apply(document, palette);

        Assert.That(result.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(document.LogicalScreen.GlobalColorTableSize, Is.EqualTo(2));
        Assert.That(document.GlobalColorTable, Is.SameAs(palette));
        Assert.That(frame.HasLocalColorTable, Is.False);
        Assert.That(frame.LocalColorTableSortFlag, Is.False);
        Assert.That(frame.LocalColorTableSize, Is.EqualTo(0));
        Assert.That(frame.LocalColorTableSizeCode, Is.EqualTo(0));
        Assert.That(frame.LocalColorTable.IsEmpty(), Is.True);
    }

    [Test]
    public void EncodeIndexedAnimation_GlobalModeWritesOneGlobalTableAndNoLocalTables()
    {
        GifPalette palette = CreatePalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateIndexedFrame(GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 1 }, palette)),
            CreateIndexedFrame(GifBitmap.CreateIndexed(2, 1, new byte[] { 1, 0 }, palette))
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.ColorTableMode = GifColorTableMode.Global;

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(parsed.Document.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(parsed.Document.Frames[1].HasLocalColorTable, Is.False);
    }

    [Test]
    public void EncodeRgbaAnimation_GlobalModeQuantizesOneSharedGlobalTable()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateRgbaFrame(new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 })),
            CreateRgbaFrame(new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 }))
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.ColorTableMode = GifColorTableMode.Global;

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult first = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);
        GifRawFrameDecodeResult second = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[1]);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(parsed.Document.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(parsed.Document.Frames[1].HasLocalColorTable, Is.False);
        Assert.That(first.IndexedPixels, Is.EqualTo(new byte[] { 0 }));
        Assert.That(second.IndexedPixels, Is.EqualTo(new byte[] { 1 }));
    }

    [Test]
    public void Apply_RejectsInvalidGlobalPalette()
    {
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(1, 1);
        GifPalette invalid = new GifPalette(new GifColor[] { new GifColor(1, 2, 3) });

        GifErrorDiagnostic result = new GifGlobalColorTableEncoder().Apply(document, invalid);

        Assert.That(result.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(document.GlobalColorTable.IsEmpty(), Is.True);
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifAnimationFrame CreateIndexedFrame(GifBitmap bitmap)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        return frame;
    }

    static GifAnimationFrame CreateRgbaFrame(GifBitmap bitmap)
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
