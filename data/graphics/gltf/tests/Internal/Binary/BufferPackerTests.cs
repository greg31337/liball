namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class BufferPackerTests
{
    [Test]
    public void PackBuffers_GroupsTargetsStablyAndZeroFillsAlignmentPadding()
    {
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(CreateItem(
            new byte[] { 9, 10 },
            1,
            2,
            2,
            GltfBufferViewTarget.ElementArrayBuffer,
            -1));
        items.Add(CreateItem(
            new byte[] { 1, 2, 3 },
            1,
            3,
            1,
            GltfBufferViewTarget.None,
            -1));
        items.Add(CreateItem(
            new byte[] { 5, 6, 7, 8 },
            1,
            4,
            4,
            GltfBufferViewTarget.ArrayBuffer,
            -1));

        BufferPackResult result = new BufferPacker().PackBuffers(
            items,
            new BufferPackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Buffers, Has.Count.EqualTo(1));
            Assert.That(result.BufferViews, Has.Count.EqualTo(3));
            Assert.That(
                result.Buffers[0].Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 0, 5, 6, 7, 8, 9, 10 }));
            Assert.That(result.BufferViews[0].Target, Is.EqualTo(GltfBufferViewTarget.None));
            Assert.That(result.BufferViews[0].ByteOffset, Is.EqualTo(0));
            Assert.That(result.BufferViews[1].Target, Is.EqualTo(GltfBufferViewTarget.ArrayBuffer));
            Assert.That(result.BufferViews[1].ByteOffset, Is.EqualTo(4));
            Assert.That(result.BufferViews[2].Target, Is.EqualTo(GltfBufferViewTarget.ElementArrayBuffer));
            Assert.That(result.BufferViews[2].ByteOffset, Is.EqualTo(8));
            Assert.That(result.Placements[0].BufferView, Is.EqualTo(2));
            Assert.That(result.Placements[1].BufferView, Is.EqualTo(0));
            Assert.That(result.Placements[2].BufferView, Is.EqualTo(1));
        });
    }

    [Test]
    public void PackBuffers_InterleavesEqualCountArrayItemsWithAlignedOffsets()
    {
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(CreateItem(
            new byte[] { 1, 2, 3, 4, 5, 6 },
            2,
            3,
            1,
            GltfBufferViewTarget.ArrayBuffer,
            7));
        items.Add(CreateItem(
            new byte[] { 10, 0, 20, 0 },
            2,
            2,
            2,
            GltfBufferViewTarget.ArrayBuffer,
            7));

        BufferPackResult result = new BufferPacker().PackBuffers(
            items,
            new BufferPackOptions());

        BufferPackView view = result.BufferViews[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.BufferViews, Has.Count.EqualTo(1));
            Assert.That(view.HasByteStride, Is.True);
            Assert.That(view.ByteStride, Is.EqualTo(8));
            Assert.That(view.ByteLength, Is.EqualTo(16));
            Assert.That(result.Placements[0].AccessorByteOffset, Is.EqualTo(0));
            Assert.That(result.Placements[1].AccessorByteOffset, Is.EqualTo(4));
            Assert.That(
                result.Buffers[0].Bytes,
                Is.EqualTo(new byte[]
                {
                    1, 2, 3, 0, 10, 0, 0, 0,
                    4, 5, 6, 0, 20, 0, 0, 0
                }));
        });
    }

    [Test]
    public void PackBuffers_CanDisableRequestedInterleaving()
    {
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(CreateItem(
            new byte[] { 1, 2 },
            2,
            1,
            1,
            GltfBufferViewTarget.ArrayBuffer,
            3));
        items.Add(CreateItem(
            new byte[] { 3, 4 },
            2,
            1,
            1,
            GltfBufferViewTarget.ArrayBuffer,
            3));
        BufferPackOptions options = new BufferPackOptions();
        options.Interleave = false;

        BufferPackResult result = new BufferPacker().PackBuffers(items, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.BufferViews, Has.Count.EqualTo(2));
            Assert.That(result.BufferViews[0].HasByteStride, Is.True);
            Assert.That(result.BufferViews[1].HasByteStride, Is.True);
            Assert.That(result.BufferViews[0].ByteStride, Is.EqualTo(4));
            Assert.That(result.BufferViews[1].ByteStride, Is.EqualTo(4));
            Assert.That(result.BufferViews[0].ByteOffset, Is.EqualTo(0));
            Assert.That(result.BufferViews[1].ByteOffset, Is.EqualTo(8));
            Assert.That(
                result.Buffers[0].Bytes,
                Is.EqualTo(new byte[]
                {
                    1, 0, 0, 0,
                    2, 0, 0, 0,
                    3, 0, 0, 0,
                    4, 0, 0, 0
                }));
        });
    }

    [Test]
    public void PackBuffers_PartitionsOnlyAtWholeViewBoundaries()
    {
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(CreateItem(
            new byte[] { 1, 2, 3, 4 },
            1,
            4,
            4,
            GltfBufferViewTarget.None,
            -1));
        items.Add(CreateItem(
            new byte[] { 5, 6, 7, 8 },
            1,
            4,
            4,
            GltfBufferViewTarget.None,
            -1));
        BufferPackOptions options = new BufferPackOptions();
        options.MaximumBufferByteLength = 7;
        options.Partition = true;

        BufferPackResult result = new BufferPacker().PackBuffers(items, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Buffers, Has.Count.EqualTo(2));
            Assert.That(result.Buffers[0].Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(result.Buffers[1].Bytes, Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
            Assert.That(result.BufferViews[0].Buffer, Is.EqualTo(0));
            Assert.That(result.BufferViews[1].Buffer, Is.EqualTo(1));
            Assert.That(result.BufferViews[1].ByteOffset, Is.EqualTo(0));
            Assert.That(result.Placements[1].Buffer, Is.EqualTo(1));
        });
    }

    [Test]
    public void PackBuffers_RejectsLimitOverflowWhenPartitioningIsUnavailable()
    {
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(CreateItem(
            new byte[] { 1, 2, 3, 4 },
            1,
            4,
            4,
            GltfBufferViewTarget.None,
            -1));
        items.Add(CreateItem(
            new byte[] { 5, 6, 7, 8 },
            1,
            4,
            4,
            GltfBufferViewTarget.None,
            -1));
        BufferPackOptions options = new BufferPackOptions();
        options.MaximumBufferByteLength = 7;

        Assert.That(
            () => new BufferPacker().PackBuffers(items, options),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
    }

    [Test]
    public void PackBuffers_RejectsAnOversizedViewEvenWithPartitioning()
    {
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(CreateItem(
            new byte[] { 1, 2, 3, 4, 5 },
            1,
            5,
            1,
            GltfBufferViewTarget.None,
            -1));
        BufferPackOptions options = new BufferPackOptions();
        options.MaximumBufferByteLength = 4;
        options.Partition = true;

        Assert.That(
            () => new BufferPacker().PackBuffers(items, options),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("items"));
    }

    [Test]
    public void PackBuffers_RejectsInvalidInterleaveGroups()
    {
        List<BufferPackItem> mismatchedCounts = new List<BufferPackItem>();
        mismatchedCounts.Add(CreateItem(
            new byte[] { 1, 2 },
            2,
            1,
            1,
            GltfBufferViewTarget.ArrayBuffer,
            1));
        mismatchedCounts.Add(CreateItem(
            new byte[] { 3 },
            1,
            1,
            1,
            GltfBufferViewTarget.ArrayBuffer,
            1));
        List<BufferPackItem> wrongTarget = new List<BufferPackItem>();
        wrongTarget.Add(CreateItem(
            new byte[] { 1 },
            1,
            1,
            1,
            GltfBufferViewTarget.ElementArrayBuffer,
            1));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new BufferPacker().PackBuffers(
                    mismatchedCounts,
                    new BufferPackOptions()),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("items"));
            Assert.That(
                () => new BufferPacker().PackBuffers(
                    wrongTarget,
                    new BufferPackOptions()),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("items"));
        });
    }

    [Test]
    public void PackBuffers_RejectsInterleavedStridesBeyondTheCoreLimit()
    {
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(CreateItem(
            new byte[252],
            1,
            252,
            4,
            GltfBufferViewTarget.ArrayBuffer,
            2));
        items.Add(CreateItem(
            new byte[] { 1 },
            1,
            1,
            1,
            GltfBufferViewTarget.ArrayBuffer,
            2));

        Assert.That(
            () => new BufferPacker().PackBuffers(items, new BufferPackOptions()),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("items"));
    }

    [Test]
    public void PackBuffers_ProducesOwnedDeterministicResults()
    {
        byte[] source = { 1, 2, 3 };
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(CreateItem(
            source,
            1,
            3,
            1,
            GltfBufferViewTarget.None,
            -1));
        BufferPacker packer = new BufferPacker();

        BufferPackResult first = packer.PackBuffers(items, new BufferPackOptions());
        BufferPackResult second = packer.PackBuffers(items, new BufferPackOptions());
        source[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(first.Buffers[0].Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(first.Buffers[0].Bytes, Is.EqualTo(second.Buffers[0].Bytes));
            Assert.That(first.Buffers[0].Bytes, Is.Not.SameAs(second.Buffers[0].Bytes));
        });
    }

    [Test]
    public void PackBuffers_PreservesOpaqueStridedViewBytesAndMetadata()
    {
        BufferPackItem item = CreateItem(
            new byte[] { 1, 2, 3, 9, 4, 5, 6, 8 },
            1,
            8,
            1,
            GltfBufferViewTarget.ArrayBuffer,
            -1);
        item.PreserveLayout = true;
        item.HasByteStride = true;
        item.ByteStride = 4;
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(item);

        BufferPackResult result = new BufferPacker().PackBuffers(
            items,
            new BufferPackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Buffers[0].Bytes, Is.EqualTo(item.Bytes));
            Assert.That(result.BufferViews[0].HasByteStride, Is.True);
            Assert.That(result.BufferViews[0].ByteStride, Is.EqualTo(4));
            Assert.That(result.BufferViews[0].ByteLength, Is.EqualTo(8));
        });
    }

    [Test]
    public void PackBuffers_RejectsIncompleteItemLayouts()
    {
        BufferPackItem incomplete = CreateItem(
            new byte[] { 1, 2, 3 },
            2,
            2,
            2,
            GltfBufferViewTarget.None,
            -1);
        List<BufferPackItem> items = new List<BufferPackItem>();
        items.Add(incomplete);
        BufferPackItem misaligned = CreateItem(
            new byte[] { 1, 2, 3 },
            1,
            3,
            2,
            GltfBufferViewTarget.None,
            -1);
        List<BufferPackItem> misalignedItems = new List<BufferPackItem>();
        misalignedItems.Add(misaligned);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new BufferPacker().PackBuffers(items, new BufferPackOptions()),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("items"));
            Assert.That(
                () => new BufferPacker().PackBuffers(
                    misalignedItems,
                    new BufferPackOptions()),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("items"));
        });
    }

    private static BufferPackItem CreateItem(
        byte[] bytes,
        int elementCount,
        int elementByteLength,
        int elementAlignment,
        GltfBufferViewTarget target,
        int interleaveGroup)
    {
        BufferPackItem result = new BufferPackItem();
        result.Bytes = bytes;
        result.ElementCount = elementCount;
        result.ElementByteLength = elementByteLength;
        result.ElementAlignment = elementAlignment;
        result.Target = target;
        result.InterleaveGroup = interleaveGroup;
        return result;
    }
}
