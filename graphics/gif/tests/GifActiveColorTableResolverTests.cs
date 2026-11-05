using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifActiveColorTableResolverTests
{
    [Test]
    public void Resolve_UsesLocalColorTableWhenFrameHasOne()
    {
        GifPalette global = CreatePalette(1, 2, 3, 4, 5, 6);
        GifPalette local = CreatePalette(10, 20, 30, 40, 50, 60);
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.HasLocalColorTable = true;
        frame.LocalColorTable = local;
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult result = resolver.ResolveFrameAtIndex(frame, global, 3);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Palette, Is.SameAs(local));
        Assert.That(result.UsedLocalColorTable, Is.True);
        Assert.That(result.UsedGlobalColorTable, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.None));
    }

    [Test]
    public void Resolve_UsesGlobalColorTableWhenFrameHasNoLocalTable()
    {
        GifPalette global = CreatePalette(1, 2, 3, 4, 5, 6);
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult result = resolver.ResolveFrame(frame, global);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Palette, Is.SameAs(global));
        Assert.That(result.UsedLocalColorTable, Is.False);
        Assert.That(result.UsedGlobalColorTable, Is.True);
    }

    [Test]
    public void Resolve_DoesNotFallBackWhenLocalColorTableFlagIsSetButMissing()
    {
        GifPalette global = CreatePalette(1, 2, 3, 4, 5, 6);
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.HasLocalColorTable = true;
        frame.LocalColorTable = new GifPalette(null);
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult result = resolver.ResolveFrameAtIndex(frame, global, 2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(2));
    }

    [Test]
    public void Resolve_RejectsMissingGlobalColorTableWhenNoLocalTableExists()
    {
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult result = resolver.ResolveFrameAtIndex(frame, new GifPalette(null), 4);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("ColorTable"));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(4));
    }

    [Test]
    public void Resolve_RejectsInvalidLocalOrGlobalColorTables()
    {
        GifPalette invalid = new GifPalette(new GifColor[] { new GifColor(1, 2, 3), new GifColor(4, 5, 6), new GifColor(7, 8, 9) });
        GifFrame localFrame = new GifFrame(0, 0, 1, 1);
        localFrame.HasLocalColorTable = true;
        localFrame.LocalColorTable = invalid;
        GifFrame globalFrame = new GifFrame(0, 0, 1, 1);
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult localResult = resolver.ResolveFrame(localFrame, CreatePalette(1, 2, 3, 4, 5, 6));
        GifActiveColorTableResult globalResult = resolver.ResolveFrame(globalFrame, invalid);

        Assert.That(localResult.Success, Is.False);
        Assert.That(localResult.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(globalResult.Success, Is.False);
        Assert.That(globalResult.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void Resolve_FromDocumentUsesRequestedFrameAndGlobalTable()
    {
        GifDocument document = new GifDocument();
        document.GlobalColorTable = CreatePalette(1, 2, 3, 4, 5, 6);
        document.Frames = new GifFrame[] { new GifFrame(0, 0, 1, 1), new GifFrame(0, 0, 1, 1) };
        GifPalette local = CreatePalette(10, 20, 30, 40, 50, 60);
        document.Frames[1].HasLocalColorTable = true;
        document.Frames[1].LocalColorTable = local;
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult first = resolver.Resolve(document, 0);
        GifActiveColorTableResult second = resolver.Resolve(document, 1);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Palette, Is.SameAs(document.GlobalColorTable));
        Assert.That(second.Success, Is.True);
        Assert.That(second.Palette, Is.SameAs(local));
    }

    [Test]
    public void Resolve_FromDocumentRejectsMissingDocumentOrFrameIndex()
    {
        GifDocument document = new GifDocument();
        document.Frames = new GifFrame[] { new GifFrame(0, 0, 1, 1) };
        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();

        GifActiveColorTableResult missingDocument = resolver.Resolve(null!, 0);
        GifActiveColorTableResult missingFrame = resolver.Resolve(document, 1);

        Assert.That(missingDocument.Success, Is.False);
        Assert.That(missingDocument.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(missingFrame.Success, Is.False);
        Assert.That(missingFrame.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(missingFrame.Error.Position.FrameIndex, Is.EqualTo(1));
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
