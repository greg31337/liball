using ttf;

namespace ttf.Tests;

internal class FontConcurrencyTests
{
    [Test]
    public void SharedVariableGlyphCachePublishesOneStableResult()
    {
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(Tag("wght"), 0.5);
        FontInstance instance = FontInstanceTests
            .CreateVariableFace(false)
            .CreateInstance(coordinates);
        SfntVariedGlyph[] results = new SfntVariedGlyph[32];
        using CountdownEvent ready = new CountdownEvent(results.Length);
        using ManualResetEventSlim start = new ManualResetEventSlim(false);
        Thread[] threads = new Thread[results.Length];
        int index = 0;

        while (index < threads.Length)
        {
            int resultIndex = index;
            threads[index] = new Thread(() =>
            {
                ready.Signal();
                start.Wait();
                results[resultIndex] = instance.GetTrueTypeVariedGlyph(2);
            });
            threads[index].Start();
            index += 1;
        }

        ready.Wait();
        start.Set();
        index = 0;
        while (index < threads.Length)
        {
            threads[index].Join();
            index += 1;
        }

        SfntVariedGlyph expected = results[0];
        Assert.Multiple(() =>
        {
            Assert.That(results, Has.All.SameAs(expected));
            Assert.That(expected.GetResolvedGlyph().GetGlyphId(),
                Is.EqualTo(2));
            Assert.That(instance.GetGlyphOutline(2).GetGlyphId(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void SharedFaceSupportsConcurrentReadsShapingOutlinesAndBuilders()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        FontInstance instance = face.CreateDefaultInstance();
        byte[][] subsetBytes = new byte[16][];
        int[] shapedGlyphIds = new int[subsetBytes.Length];
        double[] outlineMaximums = new double[subsetBytes.Length];

        Parallel.For(0, subsetBytes.Length, index =>
        {
            FontDescription description = face.GetDescription();
            GlyphOutline outline = instance.GetGlyphOutline(2);
            ShapedRun shaped = TextShaper.Shape("AV", instance)
                .GetRun(0)
                .GetShapedRun();
            FontSubsetOptions options = FontSubsetOptions.CreateStrict();
            options.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);
            FontSubsetBuilder builder = new FontSubsetBuilder(
                instance,
                options);
            builder.AddCodePoint(0x41);
            builder.AddFeature(Tag("kern"));
            subsetBytes[index] = builder.BuildResult().CopyFontBytes();
            shapedGlyphIds[index] = shaped.GetGlyph(0).GetGlyphId();
            outlineMaximums[index] = outline.GetBounds()!.GetYMaximum();
            Assert.That(description.GetFaceIndex(), Is.Zero);
        });

        Assert.Multiple(() =>
        {
            Assert.That(shapedGlyphIds, Has.All.EqualTo(2));
            Assert.That(outlineMaximums,
                Has.All.EqualTo(outlineMaximums[0]));
            Assert.That(subsetBytes,
                Has.All.EqualTo(subsetBytes[0]));
            Assert.That(FontChecksum.VerifyFont(subsetBytes[0]), Is.True);
        });
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
