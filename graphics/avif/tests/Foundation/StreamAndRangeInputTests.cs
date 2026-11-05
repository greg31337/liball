using System.IO;

namespace avif.Tests;

public class StreamAndRangeInputTests
{
    [Test]
    public void SeekableStreamSupportsRandomAndPreflightedExactReads()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        StreamInput input = new StreamInput(stream, true);
        byte[] random = new byte[3];
        byte[] exact = new byte[4];
        byte[] truncatedDestination = [204, 204, 204];

        InputReadResult randomResult = input.Read(5, random);
        InputReadResult exactResult = input.ReadExact(1, exact);
        InputReadResult truncated = input.ReadExact(8, truncatedDestination);
        input.Close();

        Assert.Multiple(() =>
        {
            Assert.That(input.Seekable, Is.True);
            Assert.That(input.HasKnownLength, Is.True);
            Assert.That(input.KnownLength, Is.EqualTo(10));
            Assert.That(randomResult.BytesRead, Is.EqualTo(3));
            Assert.That(random, Is.EqualTo(new byte[] { 5, 6, 7 }));
            Assert.That(exactResult.BytesRead, Is.EqualTo(4));
            Assert.That(exact, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncated.BytesRead, Is.Zero);
            Assert.That(truncatedDestination, Is.EqualTo(new byte[] { 204, 204, 204 }));
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void SeekableReadHonorsAdvertisedLengthAndEmptyOffsetBoundaries()
    {
        AdvertisedLengthMemoryStream stream = new AdvertisedLengthMemoryStream(
            new byte[] { 1, 2, 3, 4 },
            2);
        StreamInput input = new StreamInput(stream, true);
        byte[] destination = [204, 204, 204, 204];

        InputReadResult partial = input.Read(0, destination);
        InputReadResult emptyAtEnd = input.Read(2, Span<byte>.Empty);
        InputReadResult emptyBeyondEnd = input.Read(3, Span<byte>.Empty);

        RangeCache cache = new RangeCache(new MemoryStream(new byte[2]), true, 1, 1);
        InputReadResult cachedEmptyAtEnd = cache.Read(2, Span<byte>.Empty);
        InputReadResult cachedEmptyBeyondEnd = cache.Read(3, Span<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(partial.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(partial.BytesRead, Is.EqualTo(2));
            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 204, 204 }));
            Assert.That(emptyAtEnd.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(emptyBeyondEnd.Code, Is.EqualTo(AvifResultCode.EndOfData));
            Assert.That(cachedEmptyAtEnd.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(cachedEmptyBeyondEnd.Code, Is.EqualTo(AvifResultCode.EndOfData));
        });
    }

    [Test]
    public void SeekableExactReadReportsEarlyPhysicalEndAndRejectsNegativeLength()
    {
        AdvertisedLengthMemoryStream shortStream = new AdvertisedLengthMemoryStream(
            new byte[] { 1, 2, 3, 4 },
            8);
        StreamInput input = new StreamInput(shortStream, true);
        byte[] destination = [204, 204, 204, 204, 204, 204];

        InputReadResult result = input.ReadExact(0, destination);
        StreamInput invalid = new StreamInput(
            new AdvertisedLengthMemoryStream(System.Array.Empty<byte>(), -1),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(result.BytesRead, Is.EqualTo(4));
            Assert.That(result.MissingOffset, Is.EqualTo(4));
            Assert.That(result.MissingLength, Is.EqualTo(2));
            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4, 204, 204 }));
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidData));
        });
    }

    [Test]
    public void NonSeekableStreamRequiresCurrentSequentialOffset()
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4 });
        StreamInput input = new StreamInput(stream, true);
        byte[] first = new byte[2];

        InputReadResult firstResult = input.Read(0, first);
        InputReadResult wrongOffset = input.Read(0, new byte[1]);
        byte[] tail = new byte[3];
        InputReadResult truncated = input.ReadExact(2, tail);

        Assert.Multiple(() =>
        {
            Assert.That(input.Seekable, Is.False);
            Assert.That(firstResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(wrongOffset.Code, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncated.BytesRead, Is.EqualTo(2));
            Assert.That(tail, Is.EqualTo(new byte[] { 3, 4, 0 }));
        });
    }

    [Test]
    public void StreamCloseHonorsOwnership()
    {
        MemoryStream owned = new MemoryStream();
        StreamInput ownedInput = new StreamInput(owned, false);
        ownedInput.Close();

        MemoryStream borrowed = new MemoryStream();
        StreamInput borrowedInput = new StreamInput(borrowed, true);
        borrowedInput.Close();

        Assert.Multiple(() =>
        {
            Assert.That(owned.CanRead, Is.False);
            Assert.That(borrowed.CanRead, Is.True);
            Assert.That(borrowedInput.Read(0, new byte[1]).Code, Is.EqualTo(AvifResultCode.Closed));
        });
    }

    [Test]
    public void RangeCacheReusesBlocksAndEvictsLeastRecentlyUsedBlock()
    {
        byte[] source = new byte[16];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = (byte)index;
            index++;
        }

        CountingMemoryStream stream = new CountingMemoryStream(source);
        RangeCache cache = new RangeCache(stream, true, 4, 8);
        byte[] first = new byte[6];
        InputReadResult firstResult = cache.ReadExact(2, first);
        int afterFirst = stream.ReadCalls;

        byte[] reused = new byte[3];
        cache.ReadExact(1, reused);
        int afterReuse = stream.ReadCalls;

        cache.ReadExact(8, new byte[4]);
        int afterThirdBlock = stream.ReadCalls;
        cache.ReadExact(4, new byte[4]);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first, Is.EqualTo(new byte[] { 2, 3, 4, 5, 6, 7 }));
            Assert.That(afterFirst, Is.EqualTo(2));
            Assert.That(reused, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(afterReuse, Is.EqualTo(afterFirst));
            Assert.That(afterThirdBlock, Is.EqualTo(afterReuse + 1));
            Assert.That(stream.ReadCalls, Is.EqualTo(afterThirdBlock + 1));
            Assert.That(cache.Entries.Count, Is.EqualTo(2));
            Assert.That(cache.CachedBytes, Is.EqualTo(8));
        });
    }

    [Test]
    public void RangeCacheExactReadPreflightsSourceBounds()
    {
        CountingMemoryStream stream = new CountingMemoryStream(new byte[8]);
        RangeCache cache = new RangeCache(stream, true, 4, 8);
        byte[] destination = [204, 204, 204];

        InputReadResult result = cache.ReadExact(7, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(result.BytesRead, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 204, 204, 204 }));
            Assert.That(stream.ReadCalls, Is.Zero);
        });
    }

    [Test]
    public void RangeCachePreservesLruOrderWhenUseCounterReachesMaximum()
    {
        CountingMemoryStream stream = new CountingMemoryStream(new byte[16]);
        RangeCache cache = new RangeCache(stream, true, 4, 8);
        cache.ReadExact(0, new byte[4]);
        cache.ReadExact(4, new byte[4]);
        cache.UseCounter = long.MaxValue;

        cache.ReadExact(0, new byte[4]);
        int readsBeforeEviction = stream.ReadCalls;
        cache.ReadExact(8, new byte[4]);
        int readsAfterEviction = stream.ReadCalls;
        cache.ReadExact(0, new byte[4]);

        Assert.Multiple(() =>
        {
            Assert.That(readsBeforeEviction, Is.EqualTo(2));
            Assert.That(readsAfterEviction, Is.EqualTo(3));
            Assert.That(stream.ReadCalls, Is.EqualTo(3));
            Assert.That(cache.UseCounter, Is.GreaterThan(0));
            Assert.That(cache.Entries.Exists(entry => entry.Offset == 0), Is.True);
            Assert.That(cache.Entries.Exists(entry => entry.Offset == 4), Is.False);
            Assert.That(cache.Entries.Exists(entry => entry.Offset == 8), Is.True);
        });
    }
}

internal class NonSeekableReadStream : Stream
{
    internal byte[] Data;
    internal int Offset;
    internal bool IsClosed;

    internal NonSeekableReadStream(byte[] data)
    {
        Data = data;
        Offset = 0;
        IsClosed = false;
    }

    public override bool CanRead => !IsClosed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => Offset;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int available = Data.Length - Offset;
        int actual = count < available ? count : available;
        Data.AsSpan(Offset, actual).CopyTo(buffer.AsSpan(offset, actual));
        Offset += actual;
        return actual;
    }

    public override int Read(Span<byte> buffer)
    {
        int available = Data.Length - Offset;
        int actual = buffer.Length < available ? buffer.Length : available;
        Data.AsSpan(Offset, actual).CopyTo(buffer);
        Offset += actual;
        return actual;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        IsClosed = true;
        base.Dispose(disposing);
    }
}

internal class CountingMemoryStream : MemoryStream
{
    internal int ReadCalls;

    internal CountingMemoryStream(byte[] data)
        : base(data)
    {
        ReadCalls = 0;
    }

    public override int Read(Span<byte> buffer)
    {
        ReadCalls++;
        return base.Read(buffer);
    }
}

internal class AdvertisedLengthMemoryStream : MemoryStream
{
    internal long AdvertisedLength;

    internal AdvertisedLengthMemoryStream(byte[] data, long advertisedLength)
        : base(data)
    {
        AdvertisedLength = advertisedLength;
    }

    public override long Length => AdvertisedLength;
}
