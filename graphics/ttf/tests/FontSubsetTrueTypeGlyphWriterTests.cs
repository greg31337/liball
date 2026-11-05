using ttf;

namespace ttf.Tests;

internal class FontSubsetTrueTypeGlyphWriterTests
{
    [Test]
    public void RewritesRetainedGlyphsWithOffsetsBoundsAndDroppedHints()
    {
        FontSubset subset = BuildSubset(
            FontSubsetGlyphRetentionMode.Compact,
            FontSubsetHintPolicy.Drop,
            2);
        SfntInstancedGlyfData data = subset.CreateTrueTypeGlyphData();
        int[] offsets = data.CopyOffsets();
        SfntSimpleGlyph? simple = ParseSimple(data, offsets, 0);
        SfntCompositeGlyph? composite = ParseComposite(data, offsets, 2);

        Assert.Multiple(() =>
        {
            Assert.That(offsets, Has.Length.EqualTo(4));
            Assert.That(offsets[0], Is.Zero);
            Assert.That(offsets[3], Is.EqualTo(data.GetGlyf().Length));
            Assert.That(offsets[1] % 4, Is.Zero);
            Assert.That(offsets[2] % 4, Is.Zero);
            Assert.That(offsets[3] % 4, Is.Zero);
            AssertLongLocaMatchesOffsets(data.GetLoca(), offsets);
            Assert.That(data.GetBounds(0),
                Is.EqualTo(new int[] { 0, 0, 20, 20 }));
            Assert.That(data.GetBounds(2),
                Is.EqualTo(new int[] { 10, 4, 20, 14 }));
            Assert.That(simple!.GetInstructionLength(), Is.Zero);
            Assert.That(composite!.GetInstructionLength(), Is.Zero);
            Assert.That(composite.GetComponent(0).GetGlyphId(), Is.Zero);
            Assert.That(composite.GetComponent(1).GetGlyphId(), Is.EqualTo(1));
            Assert.That(composite.HasMetricsComponent(), Is.True);
        });
    }

    [Test]
    public void RetainsSimpleAndCompositeInstructionsWhenRequested()
    {
        FontSubset subset = BuildSubset(
            FontSubsetGlyphRetentionMode.Compact,
            FontSubsetHintPolicy.Retain,
            2);
        SfntInstancedGlyfData data = subset.CreateTrueTypeGlyphData();
        int[] offsets = data.CopyOffsets();

        Assert.Multiple(() =>
        {
            Assert.That(ParseSimple(data, offsets, 0)!.CopyInstructions(),
                Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(ParseComposite(data, offsets, 2)!.CopyInstructions(),
                Is.EqualTo(new byte[] { 0xCC }));
        });
    }

    [Test]
    public void RemapsCompositeComponentGlyphIdentifiers()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        SfntInstancedGlyfData data =
            SfntStaticTrueTypeInstancer.CreateSubsetGlyphData(
                instance,
                new int[] { 1, 0, 2, -1 },
                new int[] { 1, 0, 2 },
                false);
        int[] offsets = data.CopyOffsets();
        SfntCompositeGlyph? composite = ParseComposite(data, offsets, 2);

        Assert.Multiple(() =>
        {
            Assert.That(composite!.GetComponent(0).GetGlyphId(), Is.EqualTo(1));
            Assert.That(composite.GetComponent(1).GetGlyphId(), Is.Zero);
            Assert.That(data.GetBounds(2),
                Is.EqualTo(new int[] { 10, 4, 20, 14 }));
        });
    }

    [Test]
    public void PreserveModeEmitsEmptyRecordsForUnassignedGlyphGaps()
    {
        FontSubset subset = BuildSubset(
            FontSubsetGlyphRetentionMode.Preserve,
            FontSubsetHintPolicy.Drop,
            3);
        SfntInstancedGlyfData data = subset.CreateTrueTypeGlyphData();
        int[] offsets = data.CopyOffsets();

        Assert.Multiple(() =>
        {
            Assert.That(subset.GetNewToOldGlyphMap(),
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
            Assert.That(offsets, Has.Length.EqualTo(5));
            Assert.That(offsets[1], Is.EqualTo(offsets[2]));
            Assert.That(offsets[2], Is.EqualTo(offsets[3]));
            Assert.That(offsets[3], Is.EqualTo(offsets[4]));
            Assert.That(offsets[4], Is.EqualTo(data.GetGlyf().Length));
            AssertLongLocaMatchesOffsets(data.GetLoca(), offsets);
        });
    }

    [Test]
    public void RejectsRetainedCompositeWithOmittedComponent()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();

        Assert.Throws<FontFormatException>(() =>
            SfntStaticTrueTypeInstancer.CreateSubsetGlyphData(
                instance,
                new int[] { 0, -1, 1, -1 },
                new int[] { 0, 2 },
                false));
    }

    static FontSubset BuildSubset(
        FontSubsetGlyphRetentionMode retentionMode,
        FontSubsetHintPolicy hintPolicy,
        int glyphId)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetGlyphRetentionMode(retentionMode);
        options.SetHintPolicy(hintPolicy);
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            options);
        builder.AddGlyph(glyphId);
        return builder.Build();
    }

    static SfntSimpleGlyph? ParseSimple(
        SfntInstancedGlyfData data,
        int[] offsets,
        int glyphId)
    {
        return SfntSimpleGlyphParser.Parse(
            CreateRecord(data.GetGlyf(), offsets, glyphId),
            FontInstanceTests.CreateFace().GetMaxpTable()!,
            FontLimits.CreateCompatible());
    }

    static SfntCompositeGlyph? ParseComposite(
        SfntInstancedGlyfData data,
        int[] offsets,
        int glyphId)
    {
        return SfntCompositeGlyphParser.Parse(
            CreateRecord(data.GetGlyf(), offsets, glyphId),
            FontInstanceTests.CreateFace().GetMaxpTable()!,
            FontLimits.CreateCompatible());
    }

    static SfntGlyfRecord CreateRecord(
        byte[] glyf,
        int[] offsets,
        int glyphId)
    {
        FontData data = FontData.Create(glyf);
        int offset = offsets[glyphId];
        int length = offsets[glyphId + 1] - offset;
        return new SfntGlyfRecord(
            glyphId,
            offset,
            data.GetSlice(offset, length));
    }

    static void AssertLongLocaMatchesOffsets(byte[] loca, int[] offsets)
    {
        Assert.That(loca, Has.Length.EqualTo(offsets.Length * 4));
        int index = 0;

        while (index < offsets.Length)
        {
            Assert.That(ReadInt32(loca, index * 4),
                Is.EqualTo(offsets[index]));
            index++;
        }
    }

    static int ReadInt32(byte[] data, int offset)
    {
        return (data[offset] << 24) |
            (data[offset + 1] << 16) |
            (data[offset + 2] << 8) |
            data[offset + 3];
    }
}
