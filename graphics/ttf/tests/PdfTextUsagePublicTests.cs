using ttf;

namespace ttf.Tests;

internal class PdfTextUsagePublicTests
{
    [Test]
    public void ShapesFallbackTextIntoStableIndependentResources()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = FontClusterFallbackTests.Clone(
            source,
            "Primary",
            new int[] { 0x0041 });
        FontFace fallback = FontClusterFallbackTests.Clone(
            source,
            "Fallback",
            new int[] { 0x0042 });
        FontSet set = FontClusterFallbackTests.Set(primary, fallback);
        PdfTextUsageBuilder builder = new PdfTextUsageBuilder(
            set,
            new PdfFontOptions());

        PdfTextUsageResult result = builder.AddText(
            "ABBA",
            new FontRequest("Primary"));
        PdfTextUsageRun first = result.GetRun(0);
        PdfTextUsageRun middle = result.GetRun(1);
        PdfTextUsageRun last = result.GetRun(2);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(3));
            Assert.That(builder.GetResourceCount(), Is.EqualTo(2));
            Assert.That(first.GetResourceIndex(), Is.Zero);
            Assert.That(middle.GetResourceIndex(), Is.EqualTo(1));
            Assert.That(last.GetResourceIndex(), Is.Zero);
            Assert.That(first.GetFontInstance().GetFace(),
                Is.SameAs(primary));
            Assert.That(middle.GetFontInstance().GetFace(),
                Is.SameAs(fallback));
            Assert.That(first.GetSourceUtf16Start(), Is.Zero);
            Assert.That(first.GetSourceUtf16Length(), Is.EqualTo(1));
            Assert.That(middle.GetSourceUtf16Start(), Is.EqualTo(1));
            Assert.That(middle.GetSourceUtf16Length(), Is.EqualTo(2));
            Assert.That(last.GetSourceUtf16Start(), Is.EqualTo(3));
            Assert.That(first.GetCodes().Length,
                Is.EqualTo(first.GetEncodedRun().GetGlyphCount() * 2));
            Assert.That(middle.GetPositionedGlyphs().Length,
                Is.EqualTo(middle.GetEncodedRun().GetGlyphCount()));
            Assert.That(middle.GetPositionedGlyphs()[0].GetUtf16Start(),
                Is.EqualTo(1));
            Assert.That(middle.GetTjAdjustments(), Is.Not.Null);
        });

        PdfFontResource[] resources = builder.FinalizeFonts();

        Assert.Multiple(() =>
        {
            Assert.That(resources.Length, Is.EqualTo(2));
            Assert.That(builder.IsFinalized(), Is.True);
            Assert.That(builder.GetFinalizedResource(0),
                Is.SameAs(resources[0]));
            Assert.That(builder.GetFinalizedResource(1),
                Is.SameAs(resources[1]));
            Assert.That(resources[0].GetCharacterCodeCount(),
                Is.EqualTo(2));
            Assert.That(resources[1].GetCharacterCodeCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void ConsumesFallbackShapedRunsWithoutChangingTheirIdentity()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        FontSet set = new FontSet();
        set.Add(face);
        string family = face.GetNames().GetFamilyName()!.GetValue();
        TextShapingResult shaped = set.ShapeText(
            "AV",
            new FontRequest(family));
        PdfTextUsageBuilder builder = new PdfTextUsageBuilder(
            set,
            new PdfFontOptions());

        PdfTextUsageResult first = builder.AddShapingResult(shaped);
        PdfTextUsageResult second = builder.AddShapingResult(shaped);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetShapingResult(), Is.SameAs(shaped));
            Assert.That(first.GetRun(0).GetFontInstance(),
                Is.SameAs(shaped.GetRun(0).GetFontInstance()));
            Assert.That(second.GetRun(0).GetResourceIndex(),
                Is.EqualTo(first.GetRun(0).GetResourceIndex()));
            Assert.That(builder.GetResourceCount(), Is.EqualTo(1));
            Assert.That(first.GetRun(0).GetEncodedRun().GetXAdvance(),
                Is.EqualTo(second.GetRun(0).GetEncodedRun().GetXAdvance()));
            Assert.That(first.GetRun(0).GetTjAdjustments().Length,
                Is.EqualTo(1));
            Assert.That(first.GetRun(0).GetTjAdjustments()[0].GetValue(),
                Is.EqualTo(80));
        });
    }

    [Test]
    public void ReturnsOccurrenceActualTextForOneCodeWithTwoSourceStrings()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace face = FontMatchingTests.CloneFaceWithCmap(
            source,
            "Shared Glyph",
            BuildSharedGlyphCmap());
        FontSet set = new FontSet();
        set.Add(face);
        PdfTextUsageBuilder builder = new PdfTextUsageBuilder(
            set,
            new PdfFontOptions());
        FontRequest request = new FontRequest("Shared Glyph");

        PdfTextUsageResult first = builder.AddText("B", request);
        Assert.That(first.GetActualTextRecommendationCount(), Is.Zero);

        PdfTextUsageResult second = builder.AddText("A", request);
        PdfTextActualTextRecommendation recommendation =
            first.GetActualTextRecommendation(0);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetActualTextRecommendationCount(),
                Is.EqualTo(1));
            Assert.That(second.GetActualTextRecommendationCount(), Is.Zero);
            Assert.That(recommendation.GetResourceIndex(), Is.Zero);
            Assert.That(recommendation.GetRunIndex(), Is.Zero);
            Assert.That(recommendation.GetGlyphIndex(), Is.Zero);
            Assert.That(recommendation.GetSourceUtf16Start(), Is.Zero);
            Assert.That(recommendation.GetSourceUtf16Length(), Is.EqualTo(1));
            Assert.That(recommendation.GetRecommendation()
                .GetCharacterCode(),
                Is.EqualTo(first.GetRun(0).GetEncodedRun().GetCid(0)));
            Assert.That(recommendation.GetRecommendation().GetText(),
                Is.EqualTo("B"));
            Assert.That(recommendation.GetRecommendation().GetReason(),
                Is.EqualTo(PdfActualTextReason.MultipleSequencesForCode));
        });

        PdfFontResource resource = builder.FinalizeFonts()[0];

        Assert.That(resource.GetToUnicodeMappings()
            .GetActualTextRecommendation(0).GetText(), Is.EqualTo("B"));
    }

    [Test]
    public void ValidatesInputsAndRejectsOperationsAfterFinalization()
    {
        FontSet set = new FontSet();
        FontRequest request = new FontRequest("Missing");
        PdfTextUsageBuilder builder = new PdfTextUsageBuilder(
            set,
            new PdfFontOptions());
        PdfFontResource[] resources = builder.FinalizeFonts();

        Assert.Multiple(() =>
        {
            Assert.That(resources, Is.Empty);
            Assert.That(builder.IsFinalized(), Is.True);
            Assert.Throws<ArgumentNullException>(() =>
                new PdfTextUsageBuilder(null!, new PdfFontOptions()));
            Assert.Throws<ArgumentNullException>(() =>
                new PdfTextUsageBuilder(set, null!));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddText("A", request));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddShapingResult(
                    set.ShapeText("", request)));
            Assert.Throws<InvalidOperationException>(() =>
                builder.FinalizeFonts());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.GetFinalizedResource(0));
        });
    }

    static byte[] BuildSharedGlyphCmap()
    {
        byte[] format = new TestBinaryBuilder()
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32(40)
            .WriteUInt32(0)
            .WriteUInt32(2)
            .WriteUInt32(0x0041)
            .WriteUInt32(0x0041)
            .WriteUInt32(2)
            .WriteUInt32(0x0042)
            .WriteUInt32(0x0042)
            .WriteUInt32(2)
            .CopyBytes();
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt32(12)
            .WriteBytes(format)
            .CopyBytes();
    }
}
