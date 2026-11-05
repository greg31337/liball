using System.Threading.Tasks;

namespace avif.Tests;

public class PooledBufferTests
{
    [Test]
    public void ByteBufferStartsClearedAndSupportsBoundedCopies()
    {
        PooledByteBuffer buffer = new PooledByteBuffer(6, 6, true);
        byte[] destination = new byte[3];

        AvifResultCode write = buffer.CopyFrom(new byte[] { 2, 4, 6 }, 2);
        AvifResultCode read = buffer.CopyTo(2, destination);
        byte[] beforeRejectedWrite = buffer.GetSpan().ToArray();
        AvifResultCode rejected = buffer.CopyFrom(new byte[] { 8, 10 }, 5);
        AvifResultCode invalidOffset = buffer.CopyTo(-1, destination);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(beforeRejectedWrite, Is.EqualTo(new byte[] { 0, 0, 2, 4, 6, 0 }));
            Assert.That(destination, Is.EqualTo(new byte[] { 2, 4, 6 }));
            Assert.That(write, Is.EqualTo(AvifResultCode.Success));
            Assert.That(read, Is.EqualTo(AvifResultCode.Success));
            Assert.That(rejected, Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(invalidOffset, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(buffer.GetSpan().ToArray(), Is.EqualTo(beforeRejectedWrite));
        });

        buffer.Close();
    }

    [Test]
    public void ByteViewSharesStorageButCopyHasIndependentOwnership()
    {
        PooledByteBuffer owner = new PooledByteBuffer(5, 5, true);
        owner.CopyFrom(new byte[] { 1, 2, 3, 4, 5 }, 0);
        ByteBufferView view = owner.CreateView(1, 3);
        PooledByteBuffer copy = owner.CreateCopy();

        view.CopyFrom(new byte[] { 9, 8 }, 1);
        copy.GetSpan()[0] = 7;

        Assert.Multiple(() =>
        {
            Assert.That(view.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(view.GetSpan().ToArray(), Is.EqualTo(new byte[] { 2, 9, 8 }));
            Assert.That(owner.GetSpan().ToArray(), Is.EqualTo(new byte[] { 1, 2, 9, 8, 5 }));
            Assert.That(copy.GetSpan().ToArray(), Is.EqualTo(new byte[] { 7, 2, 3, 4, 5 }));
        });

        view.Close();
        Assert.That(owner.GetSpan().Length, Is.EqualTo(5));
        owner.Close();
        copy.Close();
    }

    [Test]
    public void ClosingByteOwnerInvalidatesViewsAndIsIdempotent()
    {
        PooledByteBuffer owner = new PooledByteBuffer(4, 4, true);
        ByteBufferView view = owner.CreateView(0, 4);

        AvifResultCode firstClose = owner.Close();
        AvifResultCode secondClose = owner.Close();
        AvifResultCode viewWrite = view.CopyFrom(new byte[] { 1 }, 0);

        Assert.Multiple(() =>
        {
            Assert.That(firstClose, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondClose, Is.EqualTo(AvifResultCode.Success));
            Assert.That(owner.Data, Is.Empty);
            Assert.That(owner.Length, Is.Zero);
            Assert.That(view.GetSpan().Length, Is.Zero);
            Assert.That(viewWrite, Is.EqualTo(AvifResultCode.Closed));
        });

        view.Close();
    }

    [Test]
    public void SampleBufferSupportsViewsCopiesAndCleanup()
    {
        PooledSampleBuffer owner = new PooledSampleBuffer(4, 8, true);
        owner.CopyFrom(new ushort[] { 100, 200, 300, 400 }, 0);
        SampleBufferView view = owner.CreateView(1, 2);
        PooledSampleBuffer copy = owner.CreateCopy();

        view.GetSpan()[0] = 250;
        copy.GetSpan()[2] = 350;

        Assert.Multiple(() =>
        {
            Assert.That(owner.GetSpan().ToArray(), Is.EqualTo(new ushort[] { 100, 250, 300, 400 }));
            Assert.That(view.GetSpan().ToArray(), Is.EqualTo(new ushort[] { 250, 300 }));
            Assert.That(copy.GetSpan().ToArray(), Is.EqualTo(new ushort[] { 100, 200, 350, 400 }));
        });

        owner.Close();
        Assert.That(view.CopyTo(0, new ushort[2]), Is.EqualTo(AvifResultCode.Closed));
        Assert.That(owner.Close(), Is.EqualTo(AvifResultCode.Success));
        view.Close();
        copy.Close();
    }

    [Test]
    public void BuffersRejectInvalidOrOverLimitLengthsWithoutRenting()
    {
        PooledByteBuffer invalid = new PooledByteBuffer(-1, 10, true);
        PooledByteBuffer byteLimit = new PooledByteBuffer(11, 10, true);
        PooledSampleBuffer sampleLimit = new PooledSampleBuffer(4, 7, true);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(byteLimit.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(sampleLimit.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(invalid.Pooled, Is.False);
            Assert.That(byteLimit.Pooled, Is.False);
            Assert.That(sampleLimit.Pooled, Is.False);
        });
    }

    [Test]
    public void ConcurrentCleanupLeavesPooledOwnersClosedAndEmpty()
    {
        PooledByteBuffer bytes = new PooledByteBuffer(4096, 4096, true);
        PooledSampleBuffer samples = new PooledSampleBuffer(2048, 4096, true);
        Task[] tasks = new Task[16];
        int index = 0;
        while (index < tasks.Length)
        {
            tasks[index] = Task.Run(() =>
            {
                bytes.Close();
                samples.Close();
            });
            index++;
        }

        Task.WaitAll(tasks);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Closed, Is.True);
            Assert.That(bytes.Pooled, Is.False);
            Assert.That(bytes.Data, Is.Empty);
            Assert.That(bytes.Length, Is.Zero);
            Assert.That(samples.Closed, Is.True);
            Assert.That(samples.Pooled, Is.False);
            Assert.That(samples.Data, Is.Empty);
            Assert.That(samples.Length, Is.Zero);
            Assert.That(bytes.Close(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(samples.Close(), Is.EqualTo(AvifResultCode.Success));
        });
    }
}
