using System.Text;

namespace http.Tests;

public class HpackDynamicTableTests
{
    [Test]
    public void TablesMaintainIndependentEncoderAndDecoderState()
    {
        HpackDynamicTable encoder = new HpackDynamicTable(256);
        HpackDynamicTable decoder = new HpackDynamicTable(256);

        encoder.Insert("x-encoder"u8, "one"u8);
        decoder.Insert("x-decoder"u8, "two"u8);

        HpackTableEntry encoderEntry = encoder.LookupByIndex(62);
        HpackTableEntry decoderEntry = decoder.LookupByIndex(62);

        Assert.Multiple(() =>
        {
            Assert.That(encoder.GetCount(), Is.EqualTo(1));
            Assert.That(decoder.GetCount(), Is.EqualTo(1));
            Assert.That(encoderEntry.Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("x-encoder")));
            Assert.That(decoderEntry.Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("x-decoder")));
            Assert.That(encoder.FindName("x-decoder"u8).HasIndex, Is.False);
            Assert.That(decoder.FindName("x-encoder"u8).HasIndex, Is.False);
        });
    }

    [Test]
    public void InsertionUsesRfcSizeAndNewestFirstIndexes()
    {
        HpackDynamicTable table = new HpackDynamicTable(256);

        HpackDynamicTableResult first = table.Insert(
            ":authority"u8, "www.example.com"u8);
        HpackDynamicTableResult second = table.Insert(
            "cache-control"u8, "no-cache"u8);

        HpackTableEntry newest = table.LookupByIndex(62);
        HpackTableEntry oldest = table.LookupByIndex(63);

        Assert.Multiple(() =>
        {
            Assert.That(first.WasInserted, Is.True);
            Assert.That(first.Entry.Size, Is.EqualTo(57));
            Assert.That(first.CurrentSize, Is.EqualTo(57));
            Assert.That(second.Entry.Size, Is.EqualTo(53));
            Assert.That(second.CurrentSize, Is.EqualTo(110));
            Assert.That(second.Count, Is.EqualTo(2));
            Assert.That(newest.Index, Is.EqualTo(62));
            Assert.That(newest.Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("cache-control")));
            Assert.That(oldest.Index, Is.EqualTo(63));
            Assert.That(oldest.Name,
                Is.EqualTo(Encoding.ASCII.GetBytes(":authority")));
            Assert.That(newest.IsStatic, Is.False);
        });
    }

    [Test]
    public void InsertionEvictsOldestEntriesUntilTheNewEntryFits()
    {
        HpackDynamicTable table = new HpackDynamicTable(110);
        table.Insert(":authority"u8, "www.example.com"u8);
        table.Insert("cache-control"u8, "no-cache"u8);

        HpackDynamicTableResult inserted = table.Insert(
            "custom-key"u8, "custom-value"u8);
        HpackDynamicTableResult inspected = table.Inspect(10);

        Assert.Multiple(() =>
        {
            Assert.That(inserted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(inserted.WasInserted, Is.True);
            Assert.That(inserted.EvictedCount, Is.EqualTo(1));
            Assert.That(inserted.CurrentSize, Is.EqualTo(107));
            Assert.That(inspected.Entries, Has.Count.EqualTo(2));
            Assert.That(inspected.Entries[0].Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("custom-key")));
            Assert.That(inspected.Entries[1].Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("cache-control")));
            Assert.That(table.FindName(":authority"u8).Index, Is.EqualTo(1));
        });
    }

    [Test]
    public void OversizedInsertionClearsTheTableWithoutInserting()
    {
        HpackDynamicTable table = new HpackDynamicTable(64);
        table.Insert("a"u8, "b"u8);
        byte[] oversizedName = new byte[33];

        HpackDynamicTableResult result = table.Insert(
            oversizedName, ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.WasOversized, Is.True);
            Assert.That(result.WasInserted, Is.False);
            Assert.That(result.EvictedCount, Is.EqualTo(1));
            Assert.That(result.CurrentSize, Is.Zero);
            Assert.That(result.Count, Is.Zero);
            Assert.That(table.LookupByIndex(62).HasIndex, Is.False);
        });
    }

    [Test]
    public void DuplicateEntriesArePreservedAndNewestMatchWins()
    {
        HpackDynamicTable table = new HpackDynamicTable(256);
        table.Insert("x-name"u8, "first"u8);
        table.Insert("x-name"u8, "second"u8);
        table.Insert("x-name"u8, "second"u8);

        HpackTableEntry newestExact = table.FindExact(
            "x-name"u8, "second"u8);
        HpackTableEntry olderExact = table.FindExact(
            "x-name"u8, "first"u8);
        HpackTableEntry nameMatch = table.FindName("x-name"u8);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetCount(), Is.EqualTo(3));
            Assert.That(newestExact.Index, Is.EqualTo(62));
            Assert.That(olderExact.Index, Is.EqualTo(64));
            Assert.That(nameMatch.Index, Is.EqualTo(62));
            Assert.That(table.LookupByIndex(63).Value,
                Is.EqualTo(Encoding.ASCII.GetBytes("second")));
        });
    }

    [Test]
    public void ResizeEvictsOldestEntriesAndSupportsZeroThenRestore()
    {
        HpackDynamicTable table = new HpackDynamicTable(110);
        table.Insert(":authority"u8, "www.example.com"u8);
        table.Insert("cache-control"u8, "no-cache"u8);

        HpackDynamicTableResult reduced = table.Resize(60);
        HpackDynamicTableResult expanded = table.Resize(200);
        HpackDynamicTableResult zero = table.Resize(0);
        HpackDynamicTableResult atZero = table.Insert(
            ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);
        HpackDynamicTableResult restored = table.Resize(128);
        HpackDynamicTableResult emptyEntry = table.Insert(
            ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(reduced.EvictedCount, Is.EqualTo(1));
            Assert.That(reduced.CurrentSize, Is.EqualTo(53));
            Assert.That(reduced.Count, Is.EqualTo(1));
            Assert.That(expanded.EvictedCount, Is.Zero);
            Assert.That(expanded.CurrentSize, Is.EqualTo(53));
            Assert.That(expanded.Count, Is.EqualTo(1));
            Assert.That(expanded.Limit, Is.EqualTo(200));
            Assert.That(zero.EvictedCount, Is.EqualTo(1));
            Assert.That(zero.CurrentSize, Is.Zero);
            Assert.That(zero.Limit, Is.Zero);
            Assert.That(atZero.WasOversized, Is.True);
            Assert.That(atZero.WasInserted, Is.False);
            Assert.That(atZero.Count, Is.Zero);
            Assert.That(restored.Limit, Is.EqualTo(128));
            Assert.That(emptyEntry.WasInserted, Is.True);
            Assert.That(emptyEntry.Entry.Size, Is.EqualTo(32));
            Assert.That(emptyEntry.CurrentSize, Is.EqualTo(32));
        });
    }

    [Test]
    public void CombinedLookupPrefersStaticThenUsesDynamicEntries()
    {
        HpackDynamicTable table = new HpackDynamicTable(256);
        table.Insert(":method"u8, "GET"u8);
        table.Insert("x-name"u8, "value"u8);

        HpackTableEntry indexedStatic = table.LookupByIndex(2);
        HpackTableEntry indexedDynamic = table.LookupByIndex(62);
        HpackTableEntry exactStatic = table.FindExact(":method"u8, "GET"u8);
        HpackTableEntry nameStatic = table.FindName(":method"u8);

        Assert.Multiple(() =>
        {
            Assert.That(indexedStatic.Index, Is.EqualTo(2));
            Assert.That(indexedStatic.IsStatic, Is.True);
            Assert.That(indexedDynamic.Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("x-name")));
            Assert.That(exactStatic.Index, Is.EqualTo(2));
            Assert.That(nameStatic.Index, Is.EqualTo(2));
            Assert.That(table.LookupByIndex(63).IsStatic, Is.False);
            Assert.That(table.LookupByIndex(64).HasIndex, Is.False);
            Assert.That(table.LookupByIndex(0).HasIndex, Is.False);
            Assert.That(table.LookupByIndex(int.MaxValue).HasIndex, Is.False);
        });
    }

    [Test]
    public void InspectionIsBoundedNewestFirstAndReturnsOwnedSnapshots()
    {
        HpackDynamicTable table = new HpackDynamicTable(256);
        byte[] originalName = Encoding.ASCII.GetBytes("x-one");
        byte[] originalValue = Encoding.ASCII.GetBytes("one");
        HpackDynamicTableResult inserted = table.Insert(originalName, originalValue);
        table.Insert("x-two"u8, "two"u8);
        table.Insert("x-three"u8, "three"u8);

        originalName[0] = (byte)'Z';
        originalValue[0] = (byte)'Z';
        inserted.Entry.Name[0] = (byte)'Z';
        HpackDynamicTableResult two = table.Inspect(2);
        two.Entries[0].Name[0] = (byte)'Z';
        HpackDynamicTableResult zero = table.Inspect(0);
        HpackDynamicTableResult negative = table.Inspect(-1);
        HpackDynamicTableResult again = table.Inspect(3);

        Assert.Multiple(() =>
        {
            Assert.That(two.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(two.Entries, Has.Count.EqualTo(2));
            Assert.That(two.Count, Is.EqualTo(3));
            Assert.That(zero.Entries, Is.Empty);
            Assert.That(zero.Count, Is.EqualTo(3));
            Assert.That(negative.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(negative.ErrorCode,
                Is.EqualTo("invalid-hpack-dynamic-table-inspection-limit"));
            Assert.That(again.Entries[0].Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("x-three")));
            Assert.That(again.Entries[2].Name,
                Is.EqualTo(Encoding.ASCII.GetBytes("x-one")));
            Assert.That(again.Entries[0].Index, Is.EqualTo(62));
            Assert.That(again.Entries[2].Index, Is.EqualTo(64));
        });
    }

    [Test]
    public void InvalidResizeAndConstructionDoNotCreateInvalidState()
    {
        HpackDynamicTable table = new HpackDynamicTable(64);
        table.Insert("a"u8, "b"u8);

        HpackDynamicTableResult invalid = table.Resize(-1);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalid.ErrorCode,
                Is.EqualTo("invalid-hpack-dynamic-table-size"));
            Assert.That(invalid.Limit, Is.EqualTo(64));
            Assert.That(table.GetLimit(), Is.EqualTo(64));
            Assert.That(table.GetCount(), Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HpackDynamicTable(-1));
        });
    }
}
