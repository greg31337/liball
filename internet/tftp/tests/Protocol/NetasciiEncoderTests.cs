using tftp;

namespace tftp.Tests;

public class NetasciiEncoderTests
{
    [Test]
    public void EncodesLineFeedAndCarriageReturnSequences()
    {
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        byte[] source =
        {
            (byte)'A', 10, (byte)'B', 13, (byte)'C', 13, 10
        };
        byte[] destination = new byte[11];

        NetasciiEncodeResult result = encoder.EncodeBytes(source, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.BytesConsumed, Is.EqualTo(source.Length));
            Assert.That(result.BytesWritten, Is.EqualTo(destination.Length));
            Assert.That(result.HasPendingOutput, Is.False);
            Assert.That(result.NeedsMoreOutput, Is.False);
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                (byte)'A', 13, 10,
                (byte)'B', 13, 0,
                (byte)'C', 13, 0, 13, 10
            }));
        });
    }

    [Test]
    public void PreservesBytesOtherThanLineFeedAndCarriageReturn()
    {
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        byte[] source = { 0, 1, 9, 11, 12, 14, 127, 128, 255 };
        byte[] destination = new byte[source.Length];

        NetasciiEncodeResult result = encoder.EncodeBytes(source, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.BytesConsumed, Is.EqualTo(source.Length));
            Assert.That(result.BytesWritten, Is.EqualTo(source.Length));
            Assert.That(destination, Is.EqualTo(source));
            Assert.That(result.NeedsMoreOutput, Is.False);
        });
    }

    [TestCase(10, 10)]
    [TestCase(13, 0)]
    public void RetainsSecondExpansionByteAcrossOutputBoundaries(
        byte sourceValue,
        byte expectedPendingValue)
    {
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        byte[] firstDestination = new byte[1];

        NetasciiEncodeResult first = encoder.EncodeBytes(
            new byte[] { sourceValue },
            firstDestination);
        byte[] secondDestination = new byte[1];
        NetasciiEncodeResult second = encoder.EncodeBytes(
            ReadOnlySpan<byte>.Empty,
            secondDestination);

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesConsumed, Is.EqualTo(1));
            Assert.That(first.BytesWritten, Is.EqualTo(1));
            Assert.That(firstDestination[0], Is.EqualTo(13));
            Assert.That(first.HasPendingOutput, Is.True);
            Assert.That(first.NeedsMoreOutput, Is.True);
            Assert.That(second.BytesConsumed, Is.Zero);
            Assert.That(second.BytesWritten, Is.EqualTo(1));
            Assert.That(secondDestination[0], Is.EqualTo(expectedPendingValue));
            Assert.That(second.HasPendingOutput, Is.False);
            Assert.That(second.NeedsMoreOutput, Is.False);
        });
    }

    [Test]
    public void FlushesPendingOutputBeforeConsumingNewInput()
    {
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        byte[] firstDestination = new byte[1];
        encoder.EncodeBytes(new byte[] { 10 }, firstDestination);
        byte[] secondDestination = new byte[2];

        NetasciiEncodeResult result = encoder.EncodeBytes(
            new byte[] { (byte)'A' },
            secondDestination);

        Assert.Multiple(() =>
        {
            Assert.That(result.BytesConsumed, Is.EqualTo(1));
            Assert.That(result.BytesWritten, Is.EqualTo(2));
            Assert.That(secondDestination, Is.EqualTo(new byte[] { 10, (byte)'A' }));
            Assert.That(result.HasPendingOutput, Is.False);
            Assert.That(result.NeedsMoreOutput, Is.False);
        });
    }

    [Test]
    public void DoesNotConsumeInputUntilPendingOutputCanBeFlushed()
    {
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        encoder.EncodeBytes(new byte[] { 10 }, new byte[1]);

        NetasciiEncodeResult blocked = encoder.EncodeBytes(
            new byte[] { (byte)'A' },
            Span<byte>.Empty);
        byte[] destination = new byte[2];
        NetasciiEncodeResult resumed = encoder.EncodeBytes(
            new byte[] { (byte)'A' },
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.BytesConsumed, Is.Zero);
            Assert.That(blocked.BytesWritten, Is.Zero);
            Assert.That(blocked.HasPendingOutput, Is.True);
            Assert.That(blocked.NeedsMoreOutput, Is.True);
            Assert.That(resumed.BytesConsumed, Is.EqualTo(1));
            Assert.That(resumed.BytesWritten, Is.EqualTo(2));
            Assert.That(destination, Is.EqualTo(new byte[] { 10, (byte)'A' }));
        });
    }

    [Test]
    public void ReportsUnconsumedInputWhenDestinationIsFull()
    {
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        byte[] source = { (byte)'A', (byte)'B', (byte)'C' };
        byte[] firstDestination = new byte[2];

        NetasciiEncodeResult first = encoder.EncodeBytes(source, firstDestination);
        byte[] secondDestination = new byte[1];
        NetasciiEncodeResult second = encoder.EncodeBytes(
            source.AsSpan(first.BytesConsumed),
            secondDestination);

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesConsumed, Is.EqualTo(2));
            Assert.That(first.BytesWritten, Is.EqualTo(2));
            Assert.That(first.HasPendingOutput, Is.False);
            Assert.That(first.NeedsMoreOutput, Is.True);
            Assert.That(firstDestination, Is.EqualTo(new byte[] { (byte)'A', (byte)'B' }));
            Assert.That(second.BytesConsumed, Is.EqualTo(1));
            Assert.That(second.BytesWritten, Is.EqualTo(1));
            Assert.That(secondDestination[0], Is.EqualTo((byte)'C'));
            Assert.That(second.NeedsMoreOutput, Is.False);
        });
    }

    [Test]
    public void StreamsMixedContentThroughSingleByteDestinations()
    {
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        byte[] source = { (byte)'A', 10, 13, (byte)'B' };
        List<byte> encoded = new List<byte>();
        int sourceOffset = 0;
        bool needsMoreOutput = true;

        while (needsMoreOutput)
        {
            byte[] destination = new byte[1];
            NetasciiEncodeResult result = encoder.EncodeBytes(
                source.AsSpan(sourceOffset),
                destination);
            sourceOffset = sourceOffset + result.BytesConsumed;
            if (result.BytesWritten == 1)
            {
                encoded.Add(destination[0]);
            }

            needsMoreOutput = result.NeedsMoreOutput;
        }

        Assert.Multiple(() =>
        {
            Assert.That(sourceOffset, Is.EqualTo(source.Length));
            Assert.That(encoded, Is.EqualTo(new byte[]
            {
                (byte)'A', 13, 10, 13, 0, (byte)'B'
            }));
        });
    }

    [Test]
    public void EmptyInputCompletesWithoutOutput()
    {
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        byte[] destination = { 99 };

        NetasciiEncodeResult result = encoder.EncodeBytes(
            ReadOnlySpan<byte>.Empty,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.BytesConsumed, Is.Zero);
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.HasPendingOutput, Is.False);
            Assert.That(result.NeedsMoreOutput, Is.False);
            Assert.That(destination[0], Is.EqualTo(99));
        });
    }
}
