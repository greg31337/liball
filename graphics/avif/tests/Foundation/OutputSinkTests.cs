using System.IO;

namespace avif.Tests;

public class OutputSinkTests
{
    [Test]
    public void CallerBufferOutputWritesCanonicallyAndRejectsOverflowAtomically()
    {
        byte[] destination = [204, 204, 204, 204];
        MemoryOutput output = MemoryOutput.Create(destination);
        output.Write(new byte[] { 1, 2 });
        output.Write(new byte[] { 3, 4 });
        byte[] canonical = output.ToArray();

        AvifResultCode overflow = output.Write(new byte[] { 5 });

        Assert.Multiple(() =>
        {
            Assert.That(canonical, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(overflow, Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(output.Length, Is.EqualTo(4));
            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(output.ToArray(), Is.EqualTo(canonical));
        });
    }

    [Test]
    public void ExpandableMemoryOutputGrowsWithinLimitWithoutCapacityBytes()
    {
        MemoryOutput output = MemoryOutput.Create(1, 8);
        output.Write(new byte[] { 1, 2, 3 });
        output.Write(new byte[] { 4, 5 });

        byte[] canonical = output.ToArray();
        AvifResultCode overLimit = output.Write(new byte[] { 6, 7, 8, 9 });

        Assert.Multiple(() =>
        {
            Assert.That(canonical, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(canonical, Has.Length.EqualTo(5));
            Assert.That(overLimit, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(output.Length, Is.EqualTo(5));
            Assert.That(output.ToArray(), Is.EqualTo(canonical));
        });
    }

    [Test]
    public void MemoryOutputCloseRejectsFurtherWrites()
    {
        MemoryOutput output = MemoryOutput.Create(0, 8);
        output.Write(new byte[] { 1 });
        output.Close();

        AvifResultCode code = output.Write(new byte[] { 2 });

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Closed));
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 1 }));
        });
    }

    [Test]
    public void StreamOutputWritesFlushesAndHonorsOwnership()
    {
        MemoryStream borrowed = new MemoryStream();
        StreamOutput borrowedOutput = new StreamOutput(borrowed, true);
        borrowedOutput.Write(new byte[] { 1, 2 });
        borrowedOutput.Write(new byte[] { 3 });
        AvifResultCode flushCode = borrowedOutput.Flush();
        borrowedOutput.Close();

        MemoryStream owned = new MemoryStream();
        StreamOutput ownedOutput = new StreamOutput(owned, false);
        ownedOutput.Write(new byte[] { 4 });
        ownedOutput.Close();

        Assert.Multiple(() =>
        {
            Assert.That(flushCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(borrowedOutput.BytesWritten, Is.EqualTo(3));
            Assert.That(borrowed.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(borrowed.CanWrite, Is.True);
            Assert.That(borrowedOutput.Write(new byte[] { 4 }), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(owned.CanWrite, Is.False);
        });
    }
}
