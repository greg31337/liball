using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ArrayPoolLeaseTests
{
    [Test]
    public void NewLeaseExposesOnlyTheRequestedZeroedLength()
    {
        using (ArrayPoolLease lease = new ArrayPoolLease(7, 32))
        {
            Span<byte> span = lease.GetSpan();
            int spanLength = span.Length;
            byte[] snapshot = span.ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(lease.IsActive(), Is.True);
                Assert.That(lease.GetLength(), Is.EqualTo(7));
                Assert.That(spanLength, Is.EqualTo(7));
                Assert.That(snapshot, Is.EqualTo(new byte[7]));
            });
        }
    }

    [Test]
    public void SetBytesClearsUnusedLogicalTailAndCopyBytesPreservesDestinationTail()
    {
        using (ArrayPoolLease lease = new ArrayPoolLease(5, 5))
        {
            lease.GetSpan().Fill(0xCC);
            lease.SetBytes(new byte[] { 1, 2 });
            byte[] destination = new byte[] { 9, 9, 9, 9, 9, 0x77 };

            lease.CopyBytes(destination);

            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 0, 0, 0, 0x77 }));
        }
    }

    [Test]
    public void OwnedCopyRemainsIndependentAfterLeaseReturn()
    {
        ArrayPoolLease lease = new ArrayPoolLease(4, 4);
        lease.SetBytes(new byte[] { 10, 20, 30, 40 });
        byte[] owned = lease.CreateOwnedCopy();

        lease.Dispose();
        owned[0] = 99;

        using (ArrayPoolLease nextLease = new ArrayPoolLease(4, 4))
        {
            Assert.Multiple(() =>
            {
                Assert.That(owned, Is.EqualTo(new byte[] { 99, 20, 30, 40 }));
                Assert.That(nextLease.CreateOwnedCopy(), Is.EqualTo(new byte[4]));
            });
        }
    }

    [Test]
    public void ClearBufferZerosTheWholeLogicalLease()
    {
        using (ArrayPoolLease lease = new ArrayPoolLease(4, 4))
        {
            lease.SetBytes(new byte[] { 1, 2, 3, 4 });
            lease.ClearBuffer();

            Assert.That(lease.CreateOwnedCopy(), Is.EqualTo(new byte[4]));
        }
    }

    [Test]
    public void DisposeIsIdempotentAndPreventsFurtherAccess()
    {
        ArrayPoolLease lease = new ArrayPoolLease(8, 8);

        lease.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(lease.IsActive(), Is.False);
            Assert.DoesNotThrow(() => lease.Dispose());
            Assert.Throws<ObjectDisposedException>(() => lease.GetLength());
            Assert.Throws<ObjectDisposedException>(() => lease.CreateOwnedCopy());
        });
    }

    [Test]
    public void ConstructorRejectsInvalidAndOverLimitLengthsBeforeRenting()
    {
        QrInputException negativeMinimum = Assert.Throws<QrInputException>(() =>
            new ArrayPoolLease(-1, 8))!;
        QrInputException negativeMaximum = Assert.Throws<QrInputException>(() =>
            new ArrayPoolLease(0, -1))!;
        QrResourceLimitException overLimit = Assert.Throws<QrResourceLimitException>(() =>
            new ArrayPoolLease(9, 8))!;

        Assert.Multiple(() =>
        {
            Assert.That(negativeMinimum.InputName, Is.EqualTo("minimumLength"));
            Assert.That(negativeMaximum.InputName, Is.EqualTo("maximumLength"));
            Assert.That(overLimit.LimitName, Is.EqualTo("MaximumAllocationBytes"));
            Assert.That(overLimit.RequestedValue, Is.EqualTo(9));
            Assert.That(overLimit.MaximumValue, Is.EqualTo(8));
        });
    }

    [Test]
    public void SetAndCopyRejectBuffersOutsideTheLogicalLease()
    {
        using (ArrayPoolLease lease = new ArrayPoolLease(2, 2))
        {
            QrInputException source = Assert.Throws<QrInputException>(() =>
                lease.SetBytes(new byte[] { 1, 2, 3 }))!;
            QrInputException destination = Assert.Throws<QrInputException>(() =>
                lease.CopyBytes(new byte[1]))!;

            Assert.Multiple(() =>
            {
                Assert.That(source.InputName, Is.EqualTo("source"));
                Assert.That(destination.InputName, Is.EqualTo("destination"));
            });
        }
    }
}
