using tftp;

namespace tftp.Tests;

public class NetasciiDecoderTests
{
    [Test]
    public void DecodesCarriageReturnPairsAndPreservesOtherBytes()
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        byte[] source =
        {
            (byte)'A', 13, 10,
            (byte)'B', 13, 0,
            (byte)'C', 10, 255
        };
        byte[] destination = new byte[7];

        NetasciiDecodeResult result = decoder.DecodeBytes(source, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(source.Length));
            Assert.That(result.BytesWritten, Is.EqualTo(destination.Length));
            Assert.That(result.HasPendingCarriageReturn, Is.False);
            Assert.That(result.NeedsMoreOutput, Is.False);
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                (byte)'A', 10, (byte)'B', 13, (byte)'C', 10, 255
            }));
        });
    }

    [TestCase(10, 10)]
    [TestCase(0, 13)]
    public void CompletesCarriageReturnPairAcrossInputBoundaries(
        byte pairedValue,
        byte expectedValue)
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        byte[] firstDestination = { 99 };

        NetasciiDecodeResult first = decoder.DecodeBytes(
            new byte[] { 13 },
            firstDestination);
        byte[] secondDestination = new byte[1];
        NetasciiDecodeResult second = decoder.DecodeBytes(
            new byte[] { pairedValue },
            secondDestination);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.BytesConsumed, Is.EqualTo(1));
            Assert.That(first.BytesWritten, Is.Zero);
            Assert.That(first.HasPendingCarriageReturn, Is.True);
            Assert.That(first.NeedsMoreOutput, Is.False);
            Assert.That(firstDestination[0], Is.EqualTo(99));
            Assert.That(second.Success, Is.True);
            Assert.That(second.BytesConsumed, Is.EqualTo(1));
            Assert.That(second.BytesWritten, Is.EqualTo(1));
            Assert.That(secondDestination[0], Is.EqualTo(expectedValue));
            Assert.That(second.HasPendingCarriageReturn, Is.False);
        });
    }

    [Test]
    public void RetainsTrailingCarriageReturnAfterDestinationFills()
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        byte[] firstDestination = new byte[1];

        NetasciiDecodeResult first = decoder.DecodeBytes(
            new byte[] { (byte)'A', 13 },
            firstDestination);
        byte[] secondDestination = new byte[1];
        NetasciiDecodeResult second = decoder.DecodeBytes(
            new byte[] { 10 },
            secondDestination);

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesConsumed, Is.EqualTo(2));
            Assert.That(first.BytesWritten, Is.EqualTo(1));
            Assert.That(firstDestination[0], Is.EqualTo((byte)'A'));
            Assert.That(first.HasPendingCarriageReturn, Is.True);
            Assert.That(second.BytesConsumed, Is.EqualTo(1));
            Assert.That(second.BytesWritten, Is.EqualTo(1));
            Assert.That(secondDestination[0], Is.EqualTo(10));
        });
    }

    [TestCase(1)]
    [TestCase(13)]
    [TestCase((byte)'A')]
    [TestCase(255)]
    public void RejectsInvalidCarriageReturnPairs(byte pairedValue)
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        byte[] destination = new byte[1];

        NetasciiDecodeResult result = decoder.DecodeBytes(
            new byte[] { 13, pairedValue },
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesConsumed, Is.EqualTo(1));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.HasPendingCarriageReturn, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("line feed or NUL"));
            Assert.That(result.HasFailureOffset, Is.True);
            Assert.That(result.FailureOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsInvalidPairThatStartsInPreviousInputChunk()
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        decoder.DecodeBytes(new byte[] { 13 }, Span<byte>.Empty);

        NetasciiDecodeResult result = decoder.DecodeBytes(
            new byte[] { (byte)'A' },
            new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesConsumed, Is.Zero);
            Assert.That(result.HasFailureOffset, Is.True);
            Assert.That(result.FailureOffset, Is.Zero);
        });
    }

    [Test]
    public void WaitsForOutputSpaceBeforeConsumingPairByte()
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        NetasciiDecodeResult blocked = decoder.DecodeBytes(
            new byte[] { 13, 10 },
            Span<byte>.Empty);
        byte[] destination = new byte[1];
        NetasciiDecodeResult resumed = decoder.DecodeBytes(
            new byte[] { 10 },
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Success, Is.True);
            Assert.That(blocked.BytesConsumed, Is.EqualTo(1));
            Assert.That(blocked.BytesWritten, Is.Zero);
            Assert.That(blocked.HasPendingCarriageReturn, Is.True);
            Assert.That(blocked.NeedsMoreOutput, Is.True);
            Assert.That(resumed.Success, Is.True);
            Assert.That(resumed.BytesConsumed, Is.EqualTo(1));
            Assert.That(resumed.BytesWritten, Is.EqualTo(1));
            Assert.That(destination[0], Is.EqualTo(10));
        });
    }

    [Test]
    public void ReportsUnconsumedOrdinaryInputWhenDestinationIsFull()
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        byte[] source = { (byte)'A', (byte)'B' };
        byte[] firstDestination = new byte[1];

        NetasciiDecodeResult first = decoder.DecodeBytes(source, firstDestination);
        byte[] secondDestination = new byte[1];
        NetasciiDecodeResult second = decoder.DecodeBytes(
            source.AsSpan(first.BytesConsumed),
            secondDestination);

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesConsumed, Is.EqualTo(1));
            Assert.That(first.BytesWritten, Is.EqualTo(1));
            Assert.That(first.NeedsMoreOutput, Is.True);
            Assert.That(second.BytesConsumed, Is.EqualTo(1));
            Assert.That(second.BytesWritten, Is.EqualTo(1));
            Assert.That(second.NeedsMoreOutput, Is.False);
            Assert.That(new byte[] { firstDestination[0], secondDestination[0] },
                Is.EqualTo(source));
        });
    }

    [Test]
    public void FinalizationRejectsDanglingCarriageReturn()
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        decoder.DecodeBytes(new byte[] { 13 }, Span<byte>.Empty);

        NetasciiDecodeResult result = decoder.FinishDecoding();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.HasPendingCarriageReturn, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("incomplete"));
        });
    }

    [Test]
    public void FinalizationSucceedsAfterCompleteSequence()
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
        decoder.DecodeBytes(new byte[] { 13, 0 }, new byte[1]);

        NetasciiDecodeResult result = decoder.FinishDecoding();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasPendingCarriageReturn, Is.False);
            Assert.That(result.FailureMessage, Is.Empty);
        });
    }

    [Test]
    public void OctetModeCopiesContentWithoutTransformation()
    {
        NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Octet);
        byte[] source = { 13, 10, 13, 0, 10, 128, 255 };
        byte[] firstDestination = new byte[4];

        NetasciiDecodeResult first = decoder.DecodeBytes(source, firstDestination);
        byte[] secondDestination = new byte[3];
        NetasciiDecodeResult second = decoder.DecodeBytes(
            source.AsSpan(first.BytesConsumed),
            secondDestination);
        NetasciiDecodeResult finished = decoder.FinishDecoding();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.BytesConsumed, Is.EqualTo(4));
            Assert.That(first.BytesWritten, Is.EqualTo(4));
            Assert.That(first.NeedsMoreOutput, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(second.BytesConsumed, Is.EqualTo(3));
            Assert.That(second.BytesWritten, Is.EqualTo(3));
            Assert.That(second.NeedsMoreOutput, Is.False);
            Assert.That(firstDestination.Concat(secondDestination), Is.EqualTo(source));
            Assert.That(finished.Success, Is.True);
        });
    }

    [Test]
    public void RejectsInvalidTransferMode()
    {
        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => new NetasciiDecoder((TftpTransferMode)99));

        Assert.That(exception.Message, Does.Contain("transfer mode is invalid"));
    }
}
