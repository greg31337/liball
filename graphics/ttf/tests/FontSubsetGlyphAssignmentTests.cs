using ttf;

namespace ttf.Tests;

internal class FontSubsetGlyphAssignmentTests
{
    [Test]
    public void CompactAssignmentUsesDenseDeterministicGlyphIdentifiers()
    {
        FontSubset subset = BuildSubset(
            FontSubsetEmbeddingMode.Subset,
            FontSubsetGlyphRetentionMode.Compact,
            3);

        int[] oldToNew = subset.GetOldToNewGlyphMap();
        int[] newToOld = subset.GetNewToOldGlyphMap();

        Assert.Multiple(() =>
        {
            Assert.That(oldToNew,
                Is.EqualTo(new int[] { 0, -1, -1, 1 }));
            Assert.That(newToOld, Is.EqualTo(new int[] { 0, 3 }));
            AssertBidirectionalMaps(oldToNew, newToOld);
        });
    }

    [Test]
    public void PreserveAssignmentKeepsGlyphIdentifiersAndRepresentsGaps()
    {
        FontSubset subset = BuildSubset(
            FontSubsetEmbeddingMode.Subset,
            FontSubsetGlyphRetentionMode.Preserve,
            3);

        int[] oldToNew = subset.GetOldToNewGlyphMap();
        int[] newToOld = subset.GetNewToOldGlyphMap();

        Assert.Multiple(() =>
        {
            Assert.That(oldToNew,
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
            Assert.That(newToOld,
                Is.EqualTo(new int[] { 0, -1, -1, 3 }));
            AssertBidirectionalMaps(oldToNew, newToOld);
        });
    }

    [Test]
    public void FullFontAssignmentIsIdentityForEveryOriginalGlyph()
    {
        FontSubset subset = BuildSubset(
            FontSubsetEmbeddingMode.FullFont,
            FontSubsetGlyphRetentionMode.Compact);

        Assert.Multiple(() =>
        {
            Assert.That(subset.GetOldToNewGlyphMap(),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(subset.GetNewToOldGlyphMap(),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        });
    }

    [Test]
    public void ReturnedGlyphMapsAreIndependentCopies()
    {
        FontSubset subset = BuildSubset(
            FontSubsetEmbeddingMode.Subset,
            FontSubsetGlyphRetentionMode.Compact,
            3);
        int[] oldToNew = subset.GetOldToNewGlyphMap();
        int[] newToOld = subset.GetNewToOldGlyphMap();
        oldToNew[0] = 99;
        newToOld[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(subset.GetOldToNewGlyphMap(),
                Is.EqualTo(new int[] { 0, -1, -1, 1 }));
            Assert.That(subset.GetNewToOldGlyphMap(),
                Is.EqualTo(new int[] { 0, 3 }));
        });
    }

    [Test]
    public void AssignmentBuilderRejectsInvalidRetainedGlyphSets()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                FontSubsetGlyphAssignmentBuilder.Create(
                    4,
                    new int[] { 1 },
                    FontSubsetEmbeddingMode.Subset,
                    FontSubsetGlyphRetentionMode.Compact));
            Assert.Throws<ArgumentException>(() =>
                FontSubsetGlyphAssignmentBuilder.Create(
                    4,
                    new int[] { 0, 1, 1 },
                    FontSubsetEmbeddingMode.Subset,
                    FontSubsetGlyphRetentionMode.Compact));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FontSubsetGlyphAssignmentBuilder.Create(
                    4,
                    new int[] { 0, 4 },
                    FontSubsetEmbeddingMode.Subset,
                    FontSubsetGlyphRetentionMode.Compact));
        });
    }

    static FontSubset BuildSubset(
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetGlyphRetentionMode retentionMode,
        params int[] glyphIds)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetEmbeddingMode(embeddingMode);
        options.SetGlyphRetentionMode(retentionMode);
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            options);
        int index = 0;

        while (index < glyphIds.Length)
        {
            builder.AddGlyph(glyphIds[index]);
            index++;
        }

        return builder.Build();
    }

    static void AssertBidirectionalMaps(
        int[] oldToNew,
        int[] newToOld)
    {
        int oldGlyphId = 0;

        while (oldGlyphId < oldToNew.Length)
        {
            int newGlyphId = oldToNew[oldGlyphId];

            if (newGlyphId >= 0)
            {
                Assert.That(newToOld[newGlyphId], Is.EqualTo(oldGlyphId));
            }

            oldGlyphId++;
        }

        int newGlyphIdIndex = 0;

        while (newGlyphIdIndex < newToOld.Length)
        {
            int mappedOldGlyphId = newToOld[newGlyphIdIndex];

            if (mappedOldGlyphId >= 0)
            {
                Assert.That(oldToNew[mappedOldGlyphId],
                    Is.EqualTo(newGlyphIdIndex));
            }

            newGlyphIdIndex++;
        }
    }
}
