using ttf;

namespace ttf.Tests;

internal class FontSubsetCff2ConversionTests
{
    [Test]
    public void MaterializesSelectedCff2InstanceBeforeCff1Subsetting()
    {
        FontFace sourceFace =
            FontInstanceCffTests.CreateReopenableVariableOutlineFace();
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontSubset subset = BuildSubset(
            sourceFace.CreateInstance(coordinates),
            true);
        byte[] first = subset.CreateCffData().CopyData();
        byte[] second = subset.CreateCffData().CopyData();
        FontFace outputFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            first,
            2);
        SfntCffTable? cff = outputFace.GetCffTable();
        GlyphOutline outline = outputFace.CreateDefaultInstance()
            .GetGlyphOutline(1);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(subset.CopyCffFontDictionaryIndices(), Is.Empty);
            Assert.That(subset.CopyCffGlobalSubroutineIndices(), Is.Empty);
            Assert.That(subset.CopyCffLocalSubroutines(), Is.Empty);
            Assert.That(outputFace.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(cff!.GetCharStringsIndex().GetCount(), Is.EqualTo(2));
            Assert.That(cff.GetGlobalSubroutineIndex().GetCount(), Is.Zero);
            Assert.That(cff.GetCharset().GetName(1), Is.EqualTo("gid0001"));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(4));
            Assert.That(outline.GetCommand(1).GetEndX(), Is.EqualTo(146.5));
            Assert.That(outline.GetCommand(2).GetEndX(), Is.EqualTo(176.5));
            Assert.That(cff.GetTopDictionary().CopyFontBoundingBox(),
                Is.EqualTo(new double[] { 0, 0, 177, 10 }));
        });
    }

    [Test]
    public void SubsetsConvertedProgramAndVariesOutputBySelectedInstance()
    {
        FontFace sourceFace =
            FontInstanceCffTests.CreateReopenableVariableOutlineFace();
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        byte[] defaultProgram = BuildSubset(
            sourceFace.CreateDefaultInstance(),
            true).CreateCffData().CopyData();
        byte[] selectedProgram = BuildSubset(
            sourceFace.CreateInstance(coordinates),
            true).CreateCffData().CopyData();
        byte[] notdefOnly = BuildSubset(
            sourceFace.CreateInstance(coordinates),
            false).CreateCffData().CopyData();
        FontFace outputFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            notdefOnly,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(selectedProgram, Is.Not.EqualTo(defaultProgram));
            Assert.That(outputFace.GetCffTable()!
                .GetCharStringsIndex().GetCount(), Is.EqualTo(1));
            Assert.That(outputFace.GetCffTable()!
                .GetStringIndex().GetCount(), Is.Zero);
            Assert.That(outputFace.CreateDefaultInstance()
                .GetGlyphOutline(0).IsEmpty(), Is.True);
        });
    }

    static FontSubset BuildSubset(
        FontInstance instance,
        bool includeGlyphOne)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        FontSubsetBuilder builder = new FontSubsetBuilder(instance, options);

        if (includeGlyphOne)
        {
            builder.AddGlyph(1);
        }

        return builder.Build();
    }
}
