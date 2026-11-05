using ttf;

namespace ttf.Tests;

internal class SfntLayoutCoverageCompletionTests
{
    [Test]
    public void ExecutesExtensionWrappedMultipleSubstitutionWithMetadata()
    {
        SfntCoverageTable coverage = CreateCoverage(new int[] { 5 });
        SfntGsubMultipleSubstitution multiple =
            new SfntGsubMultipleSubstitution(
                coverage,
                new SfntGsubGlyphSequence[]
                {
                    new SfntGsubGlyphSequence(new int[] { 6, 7 })
                });
        SfntLayoutLookup layoutLookup = new SfntLayoutLookup(
            7,
            0,
            new int[] { 8 },
            new int[] { 8 },
            0);
        SfntGsubLookup lookup = new SfntGsubLookup(
            layoutLookup,
            Array.Empty<SfntGsubSingleSubstitution>(),
            new SfntGsubMultipleSubstitution[] { multiple },
            Array.Empty<SfntGsubAlternateSubstitution>(),
            Array.Empty<SfntGsubLigatureSubstitution>(),
            Array.Empty<SfntGsubContextSubstitution>(),
            Array.Empty<SfntGsubChainedContextSubstitution>(),
            new SfntExtensionLookup[]
            {
                new SfntExtensionLookup(2, 8, 8)
            },
            Array.Empty<SfntGsubReverseSubstitution>(),
            2);
        SfntGsubTable table = new SfntGsubTable(
            null!,
            new SfntGsubLookup[] { lookup },
            FontLimits.CreateCompatible());
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 5 },
            new int[] { 4 },
            new int[] { 3 },
            new int[] { 2 },
            new int[] { 1 });

        int count = SfntGsubExecutor.ApplyLookup(
            table,
            0,
            buffer,
            null!,
            SfntLayoutTraversalDirection.Forward,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(lookup.GetLayoutLookup().GetLookupType(),
                Is.EqualTo(7));
            Assert.That(lookup.GetExtension(0).GetLookupType(),
                Is.EqualTo(2));
            Assert.That(lookup.GetLookupType(), Is.EqualTo(2));
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(6));
            Assert.That(buffer.GetGlyphId(1), Is.EqualTo(7));
            Assert.That(buffer.GetClusterStart(0), Is.EqualTo(4));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(4));
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(3));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(3));
            Assert.That(buffer.GetLigatureComponentIndex(0), Is.EqualTo(1));
            Assert.That(buffer.GetLigatureComponentIndex(1), Is.EqualTo(2));
        });
    }

    [Test]
    public void AppliesDeviceAndVariationValuesThroughGposExecution()
    {
        SfntDeviceOrVariationIndex device =
            SfntDeviceOrVariationIndex.Create(new SfntDeviceTable(
                20,
                20,
                1,
                new int[] { 0x4000 }));
        SfntDeviceOrVariationIndex variation =
            SfntDeviceOrVariationIndex.Create(
                new SfntVariationIndex(0, 0));
        SfntGposValueRecord deviceRecord = CreateValueRecord(
            10,
            0,
            device,
            null);
        SfntGposValueRecord variationRecord = CreateValueRecord(
            0,
            -20,
            null,
            variation);
        SfntGposSingleAdjustment single = new SfntGposSingleAdjustment(
            2,
            CreateCoverage(new int[] { 1, 2 }),
            0x0091,
            new SfntGposValueRecord[]
            {
                deviceRecord,
                variationRecord
            });
        SfntLayoutLookup layoutLookup = new SfntLayoutLookup(
            1,
            0,
            new int[] { 8 },
            new int[] { 8 },
            0);
        SfntGposLookup lookup = new SfntGposLookup(
            layoutLookup,
            new SfntGposSingleAdjustment[] { single },
            Array.Empty<SfntGposPairAdjustment>(),
            Array.Empty<SfntGposCursiveAdjustment>(),
            Array.Empty<SfntGposMarkAttachment>(),
            Array.Empty<SfntGposMarkLigatureAdjustment>(),
            Array.Empty<SfntGposMarkAttachment>(),
            Array.Empty<SfntGsubContextSubstitution>(),
            Array.Empty<SfntGsubChainedContextSubstitution>(),
            Array.Empty<SfntExtensionLookup>(),
            1);
        SfntGposTable table = new SfntGposTable(
            null!,
            new SfntGposLookup[] { lookup },
            FontLimits.CreateCompatible());
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 2 });
        SfntItemVariationStore store = CreateVariationStore();
        SfntGposAdjustmentContext context =
            new SfntGposAdjustmentContext(
                1000,
                20,
                20,
                store.CreateLocation(new double[] { 0.5 }));

        int count = SfntGposExecutor.ApplyLookup(
            table,
            0,
            buffer,
            null!,
            SfntLayoutTraversalDirection.Forward,
            context);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(60));
            Assert.That(buffer.GetYAdvance(0), Is.Zero);
            Assert.That(buffer.GetXPlacement(1), Is.Zero);
            Assert.That(buffer.GetYAdvance(1), Is.EqualTo(30));
        });
    }

    static SfntCoverageTable CreateCoverage(int[] glyphIds)
    {
        return new SfntCoverageTable(
            1,
            glyphIds,
            Array.Empty<SfntCoverageRange>(),
            glyphIds.Length);
    }

    static SfntGposValueRecord CreateValueRecord(
        int xPlacement,
        int yAdvance,
        SfntDeviceOrVariationIndex? xPlacementAdjustment,
        SfntDeviceOrVariationIndex? yAdvanceAdjustment)
    {
        return new SfntGposValueRecord(
            0x0091,
            xPlacement,
            0,
            0,
            yAdvance,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            xPlacementAdjustment,
            null,
            null,
            yAdvanceAdjustment);
    }

    static SfntItemVariationStore CreateVariationStore()
    {
        SfntVariationRegion region = new SfntVariationRegion(
            new SfntVariationRegionAxis[]
            {
                new SfntVariationRegionAxis(0, 1, 1)
            });
        SfntItemVariationData data = new SfntItemVariationData(
            1,
            1,
            false,
            new int[] { 0 },
            new int[][]
            {
                new int[] { 100 }
            });
        return new SfntItemVariationStore(
            0,
            1,
            1,
            new SfntVariationRegion[] { region },
            new SfntItemVariationData[] { data });
    }
}
