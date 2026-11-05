using websocket;

namespace websocket.Tests;

public class FrameReadStateTests
{
    [TestCase("827d", 125L, false, 0U)]
    [TestCase("82fd11223344", 125L, true, 0x11223344U)]
    [TestCase("827e007e", 126L, false, 0U)]
    [TestCase("82feffffdeadbeef", 65535L, true, 0xdeadbeefU)]
    [TestCase("827f0000000000010000", 65536L, false, 0U)]
    [TestCase("82ff0000000100000000cafebabe", 4294967296L, true, 0xcafebabeU)]
    public void EveryLegalHeaderShapeDecodesWithoutConsumingTrailingBytes(
        string hexadecimalHeader,
        long expectedPayloadLength,
        bool expectedMasked,
        uint expectedMaskingKey)
    {
        var state = new FrameReadState();
        var headerBytes = Convert.FromHexString(hexadecimalHeader);
        var source = new byte[headerBytes.Length + 3];
        headerBytes.CopyTo(source, 0);
        source[headerBytes.Length] = 0xaa;
        source[headerBytes.Length + 1] = 0xbb;
        source[headerBytes.Length + 2] = 0xcc;

        var result = state.ReadHeader(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(headerBytes.Length));
            Assert.That(result.HasHeader, Is.True);
            Assert.That(result.Header.Opcode, Is.EqualTo(WebSocketOpcode.Binary));
            Assert.That(result.Header.PayloadLength, Is.EqualTo(expectedPayloadLength));
            Assert.That(result.Header.Masked, Is.EqualTo(expectedMasked));
            Assert.That(result.Header.MaskingKey, Is.EqualTo(expectedMaskingKey));
        });
    }

    [Test]
    public void BaseFlagsOpcodeAndNetworkOrderMaskAreDecoded()
    {
        var state = new FrameReadState();

        var result = state.ReadHeader(Convert.FromHexString("ff80a1b2c3d4"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Header.Final, Is.True);
            Assert.That(result.Header.Rsv1, Is.True);
            Assert.That(result.Header.Rsv2, Is.True);
            Assert.That(result.Header.Rsv3, Is.True);
            Assert.That(result.Header.Opcode, Is.EqualTo(WebSocketOpcode.ReservedControl15));
            Assert.That(result.Header.Masked, Is.True);
            Assert.That(result.Header.MaskingKey, Is.EqualTo(0xa1b2c3d4U));
            Assert.That(result.Header.PayloadLength, Is.Zero);
        });
    }

    [Test]
    public void MaximumSigned63BitLengthIsAccepted()
    {
        var state = new FrameReadState();

        var result = state.ReadHeader(Convert.FromHexString("827f7fffffffffffffff"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(10));
            Assert.That(result.Header.PayloadLength, Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void MaximumHeaderCanArriveOneByteAtATime()
    {
        var state = new FrameReadState();
        var header = Convert.FromHexString("82ff000000010000000011223344");

        for (var index = 0; index < header.Length; index++)
        {
            var result = state.ReadHeader(header.AsSpan(index, 1));

            Assert.That(result.ConsumedCount, Is.EqualTo(1), $"Byte index {index}");
            if (index + 1 < header.Length)
            {
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput), $"Byte index {index}");
                Assert.That(result.HasHeader, Is.False, $"Byte index {index}");
            }
            else
            {
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(result.HasHeader, Is.True);
                Assert.That(result.Header.PayloadLength, Is.EqualTo(4294967296L));
                Assert.That(result.Header.MaskingKey, Is.EqualTo(0x11223344U));
            }
        }
    }

    [Test]
    public void PartialMaskBytesRemainPendingAcrossCalls()
    {
        var state = new FrameReadState();

        var baseResult = state.ReadHeader(Convert.FromHexString("8280"));
        var firstMaskResult = state.ReadHeader(Convert.FromHexString("a1b2"));
        var finalMaskResult = state.ReadHeader(Convert.FromHexString("c3d4aabb"));

        Assert.Multiple(() =>
        {
            Assert.That(baseResult.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(baseResult.ConsumedCount, Is.EqualTo(2));
            Assert.That(firstMaskResult.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(firstMaskResult.ConsumedCount, Is.EqualTo(2));
            Assert.That(finalMaskResult.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finalMaskResult.ConsumedCount, Is.EqualTo(2));
            Assert.That(finalMaskResult.Header.MaskingKey, Is.EqualTo(0xa1b2c3d4U));
        });
    }

    [Test]
    public void EmptyInputDoesNotChangeAnIncompleteHeader()
    {
        var state = new FrameReadState();

        var first = state.ReadHeader(new byte[] { 0x82 });
        var empty = state.ReadHeader([]);
        var final = state.ReadHeader(new byte[] { 0x00, 0xaa });

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(first.ConsumedCount, Is.EqualTo(1));
            Assert.That(empty.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.ConsumedCount, Is.EqualTo(1));
            Assert.That(final.Header.PayloadLength, Is.Zero);
        });
    }

    [Test]
    public void CompletedHeaderConsumesNothingUntilReset()
    {
        var state = new FrameReadState();
        var first = state.ReadHeader(Convert.FromHexString("8100"));

        var repeated = state.ReadHeader(Convert.FromHexString("8201"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Header.Opcode, Is.EqualTo(WebSocketOpcode.Text));
        });
    }

    [Test]
    public void ResetAllowsTheStateToReadAnotherHeader()
    {
        var state = new FrameReadState();
        state.ReadHeader(Convert.FromHexString("818011223344"));

        state.ResetHeader();
        var result = state.ReadHeader(Convert.FromHexString("0201aa"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(2));
            Assert.That(result.Header.Final, Is.False);
            Assert.That(result.Header.Opcode, Is.EqualTo(WebSocketOpcode.Binary));
            Assert.That(result.Header.Masked, Is.False);
            Assert.That(result.Header.PayloadLength, Is.EqualTo(1));
        });
    }

    [TestCase("827e007d", 4)]
    [TestCase("82fe007d11223344", 4)]
    [TestCase("827f000000000000ffff", 10)]
    [TestCase("82ff000000000000ffff11223344", 10)]
    [TestCase("827f8000000000000000", 10)]
    public void NonCanonicalOrSignedExtendedLengthsFailAtTheLengthBoundary(
        string hexadecimalSource,
        int expectedConsumedCount)
    {
        var state = new FrameReadState();

        var result = state.ReadHeader(Convert.FromHexString(hexadecimalSource));
        var repeated = state.ReadHeader(new byte[] { 0xaa });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.ConsumedCount, Is.EqualTo(expectedConsumedCount));
            Assert.That(result.HasHeader, Is.False);
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(repeated.ConsumedCount, Is.Zero);
        });
    }
}
