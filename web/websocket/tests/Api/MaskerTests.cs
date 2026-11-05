using websocket;

namespace websocket.Tests;

public class MaskerTests
{
    [Test]
    public void RfcExampleMasksAndUnmasksInPlace()
    {
        var masker = new WebSocketMasker();
        byte[] payload = [0x48, 0x65, 0x6c, 0x6c, 0x6f];

        masker.ApplyMask(payload, 0x37fa213dU, 0);

        Assert.That(payload, Is.EqualTo(new byte[] { 0x7f, 0x9f, 0x4d, 0x51, 0x58 }));

        masker.ApplyMask(payload, 0x37fa213dU, 0);

        Assert.That(payload, Is.EqualTo(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f }));
    }

    [TestCase(0, 0x11)]
    [TestCase(1, 0x22)]
    [TestCase(2, 0x33)]
    [TestCase(3, 0x44)]
    [TestCase(17, 0x22)]
    [TestCase(int.MaxValue, 0x44)]
    public void StreamOffsetSelectsTheExpectedMaskAlignment(int streamOffset, byte expected)
    {
        var masker = new WebSocketMasker();
        byte[] payload = [0x00];

        masker.ApplyMask(payload, 0x11223344U, streamOffset);

        Assert.That(payload, Is.EqualTo(new byte[] { expected }));
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(31)]
    [TestCase(255)]
    [TestCase(1024)]
    [TestCase(1000003)]
    [TestCase(int.MaxValue)]
    public void ArbitraryPositiveOffsetsCycleNetworkOrderKeyBytes(
        int streamOffset)
    {
        IWebSocketMasker masker = new WebSocketMasker();
        byte[] payload =
            [0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80];
        byte[] expected = (byte[])payload.Clone();
        byte[] keyBytes = [0xa1, 0xb2, 0xc3, 0xd4];
        int keyIndex = streamOffset % keyBytes.Length;
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = (byte)(expected[index] ^ keyBytes[keyIndex]);
            keyIndex = keyIndex + 1;
            if (keyIndex == keyBytes.Length)
            {
                keyIndex = 0;
            }
        }

        masker.ApplyMask(payload, 0xa1b2c3d4U, streamOffset);

        Assert.That(payload, Is.EqualTo(expected));
    }

    [Test]
    public void ChunkedCallsMatchOneWholeCallAtEveryBoundary()
    {
        var masker = new WebSocketMasker();
        byte[] original = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        var expected = (byte[])original.Clone();
        masker.ApplyMask(expected, 0xa1b2c3d4U, 7);

        for (var split = 0; split <= original.Length; split++)
        {
            var actual = (byte[])original.Clone();
            masker.ApplyMask(actual.AsSpan(0, split), 0xa1b2c3d4U, 7);
            masker.ApplyMask(actual.AsSpan(split), 0xa1b2c3d4U, 7 + split);

            Assert.That(actual, Is.EqualTo(expected), $"Split boundary {split}");
        }
    }

    [Test]
    public void EmptySpansAcceptEveryNonnegativeOffset()
    {
        var masker = new WebSocketMasker();
        byte[] payload = [];

        Assert.DoesNotThrow(() => masker.ApplyMask(payload, 0x11223344U, 0));
        Assert.DoesNotThrow(() => masker.ApplyMask(payload, 0x11223344U, int.MaxValue));
        Assert.That(payload, Is.Empty);
    }

    [Test]
    public void OnlyBytesInsideTheSuppliedSpanAreModified()
    {
        var masker = new WebSocketMasker();
        byte[] payload = [0xaa, 0x00, 0x00, 0x00, 0xbb];

        masker.ApplyMask(payload.AsSpan(1, 3), 0x11223344U, 1);

        Assert.That(payload, Is.EqualTo(new byte[] { 0xaa, 0x22, 0x33, 0x44, 0xbb }));
    }

    [Test]
    public void ZeroMaskingKeyLeavesPayloadUnchanged()
    {
        var masker = new WebSocketMasker();
        byte[] payload = [0x01, 0x23, 0x45, 0x67, 0x89];
        var expected = (byte[])payload.Clone();

        masker.ApplyMask(payload, 0U, 3);

        Assert.That(payload, Is.EqualTo(expected));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(17)]
    [TestCase(int.MaxValue)]
    public void ApplyingTheSameMaskTwiceReversesAtEveryAlignment(
        int streamOffset)
    {
        IWebSocketMasker masker = new WebSocketMasker();
        byte[] payload =
            [0x00, 0x01, 0x7f, 0x80, 0xfe, 0xff, 0x55, 0xaa, 0x42];
        byte[] original = (byte[])payload.Clone();

        masker.ApplyMask(payload, 0x01234567U, streamOffset);
        Assert.That(payload, Is.Not.EqualTo(original));

        masker.ApplyMask(payload, 0x01234567U, streamOffset);

        Assert.That(payload, Is.EqualTo(original));
    }

    [Test]
    public void NegativeOffsetIsRejectedBeforePayloadChanges()
    {
        var masker = new WebSocketMasker();
        byte[] payload = [0x10, 0x20, 0x30];
        var expected = (byte[])payload.Clone();

        Assert.Throws<WebSocketLimitException>(() => masker.ApplyMask(payload, 0x11223344U, -1));
        Assert.That(payload, Is.EqualTo(expected));
    }
}
