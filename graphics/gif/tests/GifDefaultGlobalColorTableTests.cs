using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifDefaultGlobalColorTableTests
{
    [Test]
    public void Resolve_UsesDefaultGlobalColorTableWhenDocumentGlobalTableIsMissing()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.DefaultGlobalColorTable = new byte[] { 1, 2, 3, 4, 5, 6 };
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult result = resolver.ResolveFrameWithOptions(frame, new GifPalette(null), options, 7);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedGlobalColorTable, Is.True);
        Assert.That(result.UsedDefaultGlobalColorTable, Is.True);
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        Assert.That(result.Palette.Colors[0].Red, Is.EqualTo(1));
        Assert.That(result.Palette.Colors[1].Blue, Is.EqualTo(6));
    }

    [Test]
    public void Resolve_PrefersLocalAndExistingGlobalTablesOverDefaultGlobalTable()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.DefaultGlobalColorTable = new byte[] { 1, 2, 3, 4, 5, 6 };
        GifPalette global = CreatePalette(10, 20, 30, 40, 50, 60);
        GifPalette local = CreatePalette(70, 80, 90, 100, 110, 120);
        GifFrame globalFrame = new GifFrame(0, 0, 1, 1);
        GifFrame localFrame = new GifFrame(0, 0, 1, 1);
        localFrame.HasLocalColorTable = true;
        localFrame.LocalColorTable = local;
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult globalResult = resolver.ResolveFrameWithOptions(globalFrame, global, options, 0);
        GifActiveColorTableResult localResult = resolver.ResolveFrameWithOptions(localFrame, global, options, 1);

        Assert.That(globalResult.Success, Is.True);
        Assert.That(globalResult.Palette, Is.SameAs(global));
        Assert.That(globalResult.UsedDefaultGlobalColorTable, Is.False);
        Assert.That(localResult.Success, Is.True);
        Assert.That(localResult.Palette, Is.SameAs(local));
        Assert.That(localResult.UsedDefaultGlobalColorTable, Is.False);
    }

    [Test]
    public void Resolve_RejectsInvalidDefaultGlobalColorTableWhenNeeded()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.DefaultGlobalColorTable = new byte[] { 1, 2, 3 };
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult result = resolver.ResolveFrameWithOptions(frame, new GifPalette(null), options, 5);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(5));
    }

    [Test]
    public void BlockReader_AppliesDefaultGlobalColorTableWhenStreamHasNoGlobalTable()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.DefaultGlobalColorTable = new byte[] { 9, 8, 7, 6, 5, 4 };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateNoGlobalColorTableGif(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(result.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(result.Document.GlobalColorTable.Colors[0].Red, Is.EqualTo(9));
        Assert.That(result.Document.GlobalColorTable.Colors[1].Blue, Is.EqualTo(4));
        Assert.That(options.DefaultGlobalColorTable, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5, 4 }));
    }

    [Test]
    public void BlockReader_IgnoresInvalidDefaultGlobalColorTableDuringStructuralRead()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.DefaultGlobalColorTable = new byte[] { 1, 2, 3 };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateNoGlobalColorTableGif(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(options.DefaultGlobalColorTable, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    static byte[] CreateNoGlobalColorTableGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x3B
        };
    }

    static GifPalette CreatePalette(int r0, int g0, int b0, int r1, int g1, int b1)
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor((byte)r0, (byte)g0, (byte)b0),
            new GifColor((byte)r1, (byte)g1, (byte)b1)
        });
    }
}
