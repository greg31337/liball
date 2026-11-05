using System.Buffers;
using email;

namespace email.Tests;

public class EmailBufferTests
{
    [Test]
    public void DisposableInterfaceSupportsUsingStatements()
    {
        EmailBuffer buffer = new EmailBufferOperations().CreateWithInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(0, 16, false);

        using (buffer)
        {
            buffer.AppendByte(1);
        }

        Assert.That(buffer.IsClosed(), Is.True);
        Assert.Throws<ObjectDisposedException>(() => buffer.AppendByte(2));
    }

    [Test]
    public void AppendCopiesInputAndSnapshotsDoNotExposeStorage()
    {
        TrackingBytePool pool = new TrackingBytePool();
        EmailBuffer buffer = new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 0, 16, false);
        byte[] input = new byte[] { 1, 2, 3 };

        buffer.Append(input);
        input[0] = 9;
        byte[] snapshot = buffer.ToArray();
        snapshot[1] = 9;

        Assert.That(buffer.GetLength(), Is.EqualTo(3));
        Assert.That(buffer.GetByte(0), Is.EqualTo(1));
        Assert.That(buffer.GetByte(1), Is.EqualTo(2));
        Assert.That(buffer.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(pool.GetRentCount(), Is.EqualTo(1));
        buffer.Close();
    }

    [Test]
    public void BufferGrowsReusesAndCopiesToCallerSpan()
    {
        TrackingBytePool pool = new TrackingBytePool();
        EmailBuffer buffer = new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 2, 8, false);
        buffer.Append(new byte[] { 1, 2 });

        buffer.AppendByte(3);
        byte[] destination = new byte[] { 9, 9, 9, 9, 9 };
        buffer.CopyTo(destination);

        Assert.That(buffer.GetCapacity(), Is.EqualTo(4));
        Assert.That(buffer.GetLength(), Is.EqualTo(3));
        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 9, 9 }));
        Assert.That(pool.GetRentCount(), Is.EqualTo(2));
        Assert.That(pool.GetReturnCount(), Is.EqualTo(1));
        Assert.Throws<ArgumentException>(() => buffer.CopyTo(new byte[2]));
        buffer.Close();
    }

    [Test]
    public void DiscardPrefixSupportsIncrementalReuse()
    {
        EmailBuffer buffer = new EmailBufferOperations().CreateWithInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(2, 10, false);
        buffer.Append(new byte[] { 1, 2, 3, 4, 5 });

        buffer.DiscardPrefix(2);
        buffer.Append(new byte[] { 6, 7 });

        Assert.That(buffer.ToArray(), Is.EqualTo(new byte[] { 3, 4, 5, 6, 7 }));
        buffer.DiscardPrefix(0);
        Assert.That(buffer.GetLength(), Is.EqualTo(5));
        buffer.DiscardPrefix(5);
        Assert.That(buffer.GetLength(), Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DiscardPrefix(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DiscardPrefix(1));
        buffer.Close();
    }

    [Test]
    public void MaximumCapacityIsEnforcedBeforeMutation()
    {
        EmailBuffer buffer = new EmailBufferOperations().CreateWithInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(0, 3, false);
        buffer.Append(new byte[] { 1, 2, 3 });

        EmailLimitException appendException = Assert.Throws<EmailLimitException>(() => buffer.AppendByte(4));
        Assert.That(appendException.GetLimitName(), Is.EqualTo("MaxBufferBytes"));
        Assert.That(appendException.GetLimit(), Is.EqualTo(3));
        Assert.That(appendException.GetObserved(), Is.EqualTo(4));
        Assert.That(buffer.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));

        EmailLimitException capacityException = Assert.Throws<EmailLimitException>(() => buffer.EnsureCapacity(4));
        Assert.That(capacityException.GetObserved(), Is.EqualTo(4));
        Assert.That(buffer.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        buffer.Close();
    }

    [Test]
    public void SensitiveBuffersAreFullyClearedBeforeGrowthAndCloseReturns()
    {
        TrackingBytePool pool = new TrackingBytePool();
        EmailBuffer buffer = new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 2, 8, true);
        buffer.Append(new byte[] { 10, 11 });

        buffer.AppendByte(12);

        Assert.That(pool.GetReturnCount(), Is.EqualTo(1));
        Assert.That(pool.GetReturnedAt(0), Is.All.Zero);
        buffer.Close();
        Assert.That(pool.GetReturnCount(), Is.EqualTo(2));
        Assert.That(pool.GetReturnedAt(1), Is.All.Zero);
        Assert.That(pool.GetClearRequestAt(0), Is.False);
        Assert.That(pool.GetClearRequestAt(1), Is.False);
    }

    [Test]
    public void SensitiveClearAndDiscardDoNotLeaveLogicalDataForLaterReturn()
    {
        TrackingBytePool pool = new TrackingBytePool();
        EmailBuffer buffer = new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 8, 8, true);
        buffer.Append(new byte[] { 1, 2, 3, 4, 5 });
        buffer.DiscardPrefix(2);
        Assert.That(buffer.ToArray(), Is.EqualTo(new byte[] { 3, 4, 5 }));
        buffer.Clear();
        Assert.That(buffer.GetLength(), Is.Zero);
        buffer.Append(new byte[] { 6, 7 });

        buffer.Close();

        Assert.That(pool.GetReturnedAt(0), Is.All.Zero);
    }

    [Test]
    public void NonSensitiveReturnDoesNotRequestOrPerformClearing()
    {
        TrackingBytePool pool = new TrackingBytePool();
        EmailBuffer buffer = new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 4, 4, false);
        buffer.Append(new byte[] { 1, 2 });

        buffer.Close();

        Assert.That(pool.GetReturnCount(), Is.EqualTo(1));
        Assert.That(pool.GetReturnedAt(0)[0], Is.EqualTo(1));
        Assert.That(pool.GetReturnedAt(0)[1], Is.EqualTo(2));
        Assert.That(pool.GetClearRequestAt(0), Is.False);
    }

    [Test]
    public void CloseAndDisposeAreIdempotentAndReleaseExactlyOnce()
    {
        TrackingBytePool pool = new TrackingBytePool();
        EmailBuffer buffer = new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 4, 10, true);
        buffer.AppendByte(1);

        buffer.Dispose();
        buffer.Close();
        buffer.Dispose();

        Assert.That(buffer.IsClosed(), Is.True);
        Assert.That(buffer.GetMaximumCapacity(), Is.EqualTo(10));
        Assert.That(buffer.GetClearsSensitiveBytes(), Is.True);
        Assert.That(pool.GetReturnCount(), Is.EqualTo(1));
        Assert.Throws<ObjectDisposedException>(() => buffer.GetLength());
        Assert.Throws<ObjectDisposedException>(() => buffer.GetCapacity());
        Assert.Throws<ObjectDisposedException>(() => buffer.AppendByte(2));
        Assert.Throws<ObjectDisposedException>(() => buffer.ToArray());
        Assert.Throws<ObjectDisposedException>(() => buffer.Clear());
    }

    [Test]
    public void ZeroInitialCapacityDefersRentalAndEmptyAppendDoesNotRent()
    {
        TrackingBytePool pool = new TrackingBytePool();
        EmailBuffer buffer = new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 0, 10, false);

        buffer.Append(Array.Empty<byte>());
        buffer.EnsureCapacity(0);
        Assert.That(buffer.GetCapacity(), Is.Zero);
        Assert.That(pool.GetRentCount(), Is.Zero);

        buffer.EnsureCapacity(3);
        Assert.That(buffer.GetCapacity(), Is.EqualTo(3));
        Assert.That(pool.GetRentCount(), Is.EqualTo(1));
        buffer.Close();
    }

    [Test]
    public void ConstructorAndIndexesValidateArguments()
    {
        TrackingBytePool pool = new TrackingBytePool();
        Assert.Throws<ArgumentNullException>(() => new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(null!, 0, 1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, -1, 1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 0, 0, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 2, 1, false));

        EmailBuffer buffer = new EmailBufferOperations().CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(pool, 0, 2, false);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.EnsureCapacity(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(0));
        buffer.AppendByte(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(1));
        buffer.Close();
    }
}

public class TrackingBytePool : ArrayPool<byte>
{
    List<byte[]> rented;
    List<byte[]> returned;
    List<bool> clearRequests;

    public TrackingBytePool()
    {
        rented = new List<byte[]>();
        returned = new List<byte[]>();
        clearRequests = new List<bool>();
    }

    public override byte[] Rent(int minimumLength)
    {
        byte[] value = new byte[minimumLength];
        int index = 0;
        while (index < value.Length)
        {
            value[index] = 204;
            index++;
        }

        rented.Add(value);
        return value;
    }

    public override void Return(byte[] array, bool clearArray)
    {
        if (clearArray)
        {
            Array.Clear(array, 0, array.Length);
        }

        returned.Add(array);
        clearRequests.Add(clearArray);
    }

    public int GetRentCount()
    {
        return rented.Count;
    }

    public int GetReturnCount()
    {
        return returned.Count;
    }

    public byte[] GetReturnedAt(int index)
    {
        return returned[index];
    }

    public bool GetClearRequestAt(int index)
    {
        return clearRequests[index];
    }
}
