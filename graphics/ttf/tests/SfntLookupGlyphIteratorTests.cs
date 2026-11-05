using ttf;

namespace ttf.Tests;

internal class SfntLookupGlyphIteratorTests
{
    [Test]
    public void IteratesForwardAndBackwardWhileSkippingIgnoredGlyphClasses()
    {
        SfntLayoutGlyphBuffer buffer = CreateBuffer();
        SfntGdefTable gdef = CreateGdef(true, true, true);
        SfntLayoutLookup lookup = CreateLookup(0x000E, 0);
        SfntLookupGlyphIterator forward = SfntLookupGlyphIterator.Create(
            buffer,
            lookup,
            gdef,
            SfntLayoutTraversalDirection.Forward);
        SfntLookupGlyphIterator backward = SfntLookupGlyphIterator.Create(
            buffer,
            lookup,
            gdef,
            SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(CollectIndices(forward), Is.EqualTo(
                new int[] { 3, 4 }));
            Assert.That(CollectIndices(backward), Is.EqualTo(
                new int[] { 4, 3 }));
            Assert.That(forward.HasCurrent(), Is.False);
            Assert.That(backward.HasCurrent(), Is.False);
        });
    }

    [Test]
    public void FiltersOnlyMarksOutsideTheRequestedAttachmentClass()
    {
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            CreateBuffer(),
            CreateLookup(0x0200, 0),
            CreateGdef(true, true, true),
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(CollectIndices(iterator), Is.EqualTo(
                new int[] { 0, 2, 3, 4, 5 }));
            iterator.ResetToBoundary();
            Assert.That(iterator.IsIgnored(1), Is.True);
            Assert.That(iterator.IsIgnored(5), Is.False);
            Assert.That(iterator.IsIgnored(0), Is.False);
        });
    }

    [Test]
    public void MarkFilteringSetSupersedesMarkAttachmentType()
    {
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            CreateBuffer(),
            CreateLookup(0x0210, 0),
            CreateGdef(true, true, true),
            SfntLayoutTraversalDirection.Forward);

        Assert.That(CollectIndices(iterator), Is.EqualTo(
            new int[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void IgnoreMarksSupersedesOtherMarkFilters()
    {
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            CreateBuffer(),
            CreateLookup(0x0218, 0),
            CreateGdef(true, true, true),
            SfntLayoutTraversalDirection.Forward);

        Assert.That(CollectIndices(iterator), Is.EqualTo(
            new int[] { 0, 2, 3, 4 }));
    }

    [Test]
    public void UsesExplicitTraversalDirectionInsteadOfRightToLeftFlag()
    {
        SfntLayoutLookup rightToLeftLookup = CreateLookup(0x0001, 0);
        SfntLookupGlyphIterator forward = SfntLookupGlyphIterator.Create(
            CreateBuffer(),
            rightToLeftLookup,
            null,
            SfntLayoutTraversalDirection.Forward);
        SfntLookupGlyphIterator backward = SfntLookupGlyphIterator.Create(
            CreateBuffer(),
            rightToLeftLookup,
            null,
            SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(rightToLeftLookup.IsRightToLeft(), Is.True);
            Assert.That(CollectIndices(forward), Is.EqualTo(
                new int[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(CollectIndices(backward), Is.EqualTo(
                new int[] { 5, 4, 3, 2, 1, 0 }));
        });
    }

    [Test]
    public void RestrictsTraversalToBoundsAndResetsAfterAnAnchor()
    {
        SfntLookupGlyphIterator forward = SfntLookupGlyphIterator.Create(
            CreateBuffer(),
            CreateLookup(0, 0),
            null!,
            SfntLayoutTraversalDirection.Forward,
            1,
            5);
        SfntLookupGlyphIterator backward = SfntLookupGlyphIterator.Create(
            CreateBuffer(),
            CreateLookup(0, 0),
            null!,
            SfntLayoutTraversalDirection.Backward,
            1,
            5);
        forward.Reset(2);
        backward.Reset(3);

        Assert.Multiple(() =>
        {
            Assert.That(CollectIndices(forward), Is.EqualTo(
                new int[] { 3, 4 }));
            Assert.That(CollectIndices(backward), Is.EqualTo(
                new int[] { 2, 1 }));
            Assert.That(forward.GetMinimumIndex(), Is.EqualTo(1));
            Assert.That(forward.GetMaximumIndex(), Is.EqualTo(5));
            Assert.That(forward.GetDirection(), Is.EqualTo(
                SfntLayoutTraversalDirection.Forward));
        });
    }

    [Test]
    public void ReadsCurrentBufferValuesWithoutRetainingCallerArray()
    {
        int[] glyphIds = new int[] { 1, 2 };
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(glyphIds);
        glyphIds[0] = 9;
        buffer.SetGlyphId(1, 7);
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            buffer,
            CreateLookup(0, 0),
            null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(iterator.MoveNext(), Is.True);
            Assert.That(iterator.GetGlyphId(), Is.EqualTo(1));
            Assert.That(iterator.GetIndex(), Is.Zero);
            Assert.That(iterator.MoveNext(), Is.True);
            Assert.That(iterator.GetGlyphId(), Is.EqualTo(7));
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(buffer.GetGlyphId(1), Is.EqualTo(7));
        });
    }

    [Test]
    public void RejectsMissingFilterDependencies()
    {
        SfntLayoutGlyphBuffer buffer = CreateBuffer();
        SfntGdefTable noClasses = CreateGdef(false, true, true);
        SfntGdefTable noAttachmentClasses = CreateGdef(true, false, true);
        SfntGdefTable noMarkSets = CreateGdef(true, true, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                CreateIterator(buffer, CreateLookup(0x0002, 0), null));
            Assert.Throws<FontFormatException>(() =>
                CreateIterator(buffer, CreateLookup(0x0008, 0), noClasses));
            Assert.Throws<FontFormatException>(() => CreateIterator(
                buffer,
                CreateLookup(0x0100, 0),
                noAttachmentClasses));
            Assert.Throws<FontFormatException>(() => CreateIterator(
                buffer,
                CreateLookup(0x0010, 0),
                noMarkSets));
            Assert.Throws<FontFormatException>(() => CreateIterator(
                buffer,
                CreateLookup(0x0010, 2),
                CreateGdef(true, true, true)));
        });
    }

    [Test]
    public void ValidatesIteratorBufferAndFilterArguments()
    {
        SfntLayoutGlyphBuffer buffer = CreateBuffer();
        SfntLayoutLookup lookup = CreateLookup(0, 0);
        SfntLookupGlyphIterator iterator = CreateIterator(
            buffer,
            lookup,
            null);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutGlyphBuffer.Create(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutGlyphBuffer.Create(new int[] { -1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutGlyphBuffer.Create(new int[] { 0x10000 }));
            Assert.Throws<ArgumentNullException>(() =>
                new SfntLookupGlyphFilter(null!, null));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLookupGlyphIterator.Create(
                    null!,
                    lookup,
                    null,
                    SfntLayoutTraversalDirection.Forward));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLookupGlyphIterator.Create(
                    buffer,
                    lookup,
                    null,
                    (SfntLayoutTraversalDirection)9));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLookupGlyphIterator.Create(
                    buffer,
                    lookup,
                    null!,
                    SfntLayoutTraversalDirection.Forward,
                    4,
                    3));
            Assert.Throws<InvalidOperationException>(() =>
                iterator.GetIndex());
            Assert.Throws<InvalidOperationException>(() =>
                iterator.GetGlyphId());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                iterator.Reset(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                iterator.IsIgnored(6));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.GetGlyphId(6));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetGlyphId(0, 0x10000));
        });
    }

    static SfntLookupGlyphIterator CreateIterator(
        SfntLayoutGlyphBuffer buffer,
        SfntLayoutLookup lookup,
        SfntGdefTable? gdef)
    {
        return SfntLookupGlyphIterator.Create(
            buffer,
            lookup,
            gdef,
            SfntLayoutTraversalDirection.Forward);
    }

    static int[] CollectIndices(SfntLookupGlyphIterator iterator)
    {
        List<int> indices = new List<int>();

        while (iterator.MoveNext())
        {
            indices.Add(iterator.GetIndex());
        }

        return indices.ToArray();
    }

    static SfntLayoutGlyphBuffer CreateBuffer()
    {
        return SfntLayoutGlyphBuffer.Create(
            new int[] { 1, 2, 3, 4, 5, 6 });
    }

    static SfntLayoutLookup CreateLookup(int flags, int markFilteringSet)
    {
        return new SfntLayoutLookup(
            1,
            flags,
            new int[] { 6 },
            new int[] { 10 },
            markFilteringSet);
    }

    static SfntGdefTable CreateGdef(
        bool includeGlyphClasses,
        bool includeMarkAttachmentClasses,
        bool includeMarkGlyphSets)
    {
        SfntClassDefinitionTable? glyphClasses = includeGlyphClasses ?
            new SfntClassDefinitionTable(
                1,
                1,
                new int[] { 1, 3, 2, 4, 0, 3 },
                new SfntClassRange[0],
                4) : null;
        SfntClassDefinitionTable? markAttachmentClasses =
            includeMarkAttachmentClasses ?
            new SfntClassDefinitionTable(
                1,
                1,
                new int[] { 0, 1, 0, 0, 0, 2 },
                new SfntClassRange[0],
                2) : null;
        SfntGdefMarkGlyphSets? markGlyphSets = includeMarkGlyphSets ?
            new SfntGdefMarkGlyphSets(
                1,
                new SfntCoverageTable[]
                {
                    new SfntCoverageTable(
                        1,
                        new int[] { 2 },
                        new SfntCoverageRange[0],
                        1),
                    new SfntCoverageTable(
                        1,
                        new int[] { 6 },
                        new SfntCoverageRange[0],
                        1)
                }) : null;
        return new SfntGdefTable(
            1,
            3,
            glyphClasses,
            null,
            null,
            markAttachmentClasses,
            markGlyphSets,
            null);
    }
}
