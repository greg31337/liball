using ttf;

namespace ttf.Tests;

internal class PdfCidAllocatorTests
{
    [Test]
    public void AllocatesDenseCidsIndependentlyFromBothGlyphSpaces()
    {
        PdfCidAllocator allocator = PdfCidAllocator.Create(10);

        Assert.That(allocator.GetOrAddCid(9), Is.EqualTo(1));
        Assert.That(allocator.GetOrAddCid(3), Is.EqualTo(2));
        Assert.That(allocator.GetOrAddCid(9), Is.EqualTo(1));
        Assert.That(allocator.GetOrAddCid(0), Is.Zero);

        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            new int[] { 0, -1, -1, 1, -1, 2, -1, -1, -1, 3 },
            new int[] { 0, 3, 5, 9 });

        Assert.Multiple(() =>
        {
            Assert.That(allocation.GetCidCount(), Is.EqualTo(3));
            Assert.That(allocation.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 9, 3 }));
            Assert.That(allocation.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 3, 1 }));
            Assert.That(allocation.CopySourceGlyphToCids(),
                Is.EqualTo(new int[]
                {
                    0, -1, -1, 2, -1, -1, -1, -1, -1, 1
                }));
            Assert.That(allocation.CopySubsetGlyphToCids(),
                Is.EqualTo(new int[] { 0, 2, -1, 1 }));
            Assert.That(allocation.GetSourceGlyphId(1), Is.EqualTo(9));
            Assert.That(allocation.FindCidForSourceGlyph(3), Is.EqualTo(2));
            Assert.That(allocation.GetSubsetGlyphId(1), Is.EqualTo(3));
            Assert.That(allocation.FindCidForSubsetGlyph(1), Is.EqualTo(2));
        });
    }

    [Test]
    public void UsageFactoryPreservesDocumentFirstUseOrder()
    {
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            HarfBuzzLayoutDifferentialTests.Load().CreateDefaultInstance(),
            PdfFontOptions.Create());
        usage.AddGlyph(12, Array.Empty<int>());
        usage.AddGlyph(2, Array.Empty<int>());
        usage.AddGlyph(7, Array.Empty<int>());

        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);

        Assert.Multiple(() =>
        {
            Assert.That(allocator.GetCount(), Is.EqualTo(4));
            Assert.That(allocator.GetOrAddCid(0), Is.Zero);
            Assert.That(allocator.GetOrAddCid(12), Is.EqualTo(1));
            Assert.That(allocator.GetOrAddCid(2), Is.EqualTo(2));
            Assert.That(allocator.GetOrAddCid(7), Is.EqualTo(3));
        });
    }

    [Test]
    public void ReturnsDefensiveCopiesOfEveryMappingDirection()
    {
        PdfCidAllocator allocator = PdfCidAllocator.Create(4);
        allocator.GetOrAddCid(3);
        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            new int[] { 0, -1, -1, 1 },
            new int[] { 0, 3 });
        int[] cidToSource = allocation.CopyCidToSourceGlyphIds();
        int[] sourceToCid = allocation.CopySourceGlyphToCids();
        int[] cidToSubset = allocation.CopyCidToSubsetGlyphIds();
        int[] subsetToCid = allocation.CopySubsetGlyphToCids();
        int[] oldToNew = allocation.CopyOldToNewGlyphIds();
        int[] newToOld = allocation.CopyNewToOldGlyphIds();
        cidToSource[0] = 3;
        sourceToCid[0] = 1;
        cidToSubset[0] = 1;
        subsetToCid[0] = 1;
        oldToNew[0] = 1;
        newToOld[0] = 3;

        Assert.Multiple(() =>
        {
            Assert.That(allocation.CopyCidToSourceGlyphIds(),
                Is.EqualTo(new int[] { 0, 3 }));
            Assert.That(allocation.CopySourceGlyphToCids(),
                Is.EqualTo(new int[] { 0, -1, -1, 1 }));
            Assert.That(allocation.CopyCidToSubsetGlyphIds(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(allocation.CopySubsetGlyphToCids(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(allocation.CopyOldToNewGlyphIds(),
                Is.EqualTo(new int[] { 0, -1, -1, 1 }));
            Assert.That(allocation.CopyNewToOldGlyphIds(),
                Is.EqualTo(new int[] { 0, 3 }));
        });
    }

    [Test]
    public void RejectsInvalidGlyphsMapsAndMissingUsedGlyphs()
    {
        PdfCidAllocator allocator = PdfCidAllocator.Create(4);
        allocator.GetOrAddCid(3);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidAllocator.Create(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfCidAllocator.Create(ushort.MaxValue + 1));
            Assert.Throws<ArgumentNullException>(() =>
                PdfCidAllocator.Create((PdfFontUsage?)null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                allocator.GetOrAddCid(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                allocator.GetOrAddCid(4));
            Assert.Throws<ArgumentNullException>(() =>
                allocator.BindSubsetGlyphs(null!, new int[] { 0 }));
            Assert.Throws<ArgumentException>(() =>
                allocator.BindSubsetGlyphs(
                    new int[] { 1, -1, -1, 0 },
                    new int[] { 3, 0 }));
            Assert.Throws<ArgumentException>(() =>
                allocator.BindSubsetGlyphs(
                    new int[] { 0, -1, -1, -1 },
                    new int[] { 0 }));
            Assert.Throws<ArgumentException>(() =>
                allocator.BindSubsetGlyphs(
                    new int[] { 0, -1, -1, 1 },
                    new int[] { 0, 2 }));
        });
    }

    [Test]
    public void CheckedLookupsRejectIdentifiersOutsideTheirSpaces()
    {
        PdfCidAllocation allocation = PdfCidAllocator.Create(2)
            .BindSubsetGlyphs(
                new int[] { 0, 1 },
                new int[] { 0, 1 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                allocation.GetSourceGlyphId(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                allocation.GetSubsetGlyphId(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                allocation.FindCidForSourceGlyph(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                allocation.FindCidForSubsetGlyph(2));
        });
    }

    [Test]
    public void FrozenAllocationAllowsLookupsButRejectsNewCodes()
    {
        PdfCidAllocator allocator = PdfCidAllocator.Create(5);
        allocator.GetOrAddCid(3);
        allocator.Freeze();

        Assert.Multiple(() =>
        {
            Assert.That(allocator.IsFrozen(), Is.True);
            Assert.That(allocator.GetOrAddCid(3), Is.EqualTo(1));
            Assert.That(allocator.GetOrAddCids(
                new int[] { 0, 3 },
                ushort.MaxValue), Is.EqualTo(new int[] { 0, 1 }));
            Assert.Throws<InvalidOperationException>(() =>
                allocator.GetOrAddCid(4));
            Assert.Throws<InvalidOperationException>(() =>
                allocator.GetOrAddCids(
                    new int[] { 3, 4 },
                    ushort.MaxValue));
            Assert.That(allocator.GetCount(), Is.EqualTo(2));
        });
    }
}
