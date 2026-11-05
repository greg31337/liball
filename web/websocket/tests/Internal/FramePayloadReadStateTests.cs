using websocket;

namespace websocket.Tests;

public class FramePayloadReadStateTests
{
    [Test]
    public void UnmaskedPayloadCompletesWithoutConsumingTrailingBytes()
    {
        var state = CreateState("8205");
        byte[] source = [1, 2, 3, 4, 5, 0xaa, 0xbb];

        var result = state.ReadPayload(source, 5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(5));
            Assert.That(result.Payload, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(result.PayloadCount, Is.EqualTo(5));
            Assert.That(result.EndOfFrame, Is.True);
            Assert.That(result.HasError, Is.False);
            Assert.That(source, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 0xaa, 0xbb }));
        });
    }

    [Test]
    public void MaskingContinuesCorrectlyAtEverySplitAlignment()
    {
        byte[] maskedPayload = [0x7f, 0x9f, 0x4d, 0x51, 0x58];
        byte[] expected = [0x48, 0x65, 0x6c, 0x6c, 0x6f];

        for (var split = 0; split <= maskedPayload.Length; split++)
        {
            var state = CreateState("818537fa213d");
            var first = state.ReadPayload(maskedPayload.AsSpan(0, split), maskedPayload.Length);
            var remaining = maskedPayload.Length - split;
            var secondSource = new byte[remaining + 2];
            maskedPayload.AsSpan(split).CopyTo(secondSource);
            secondSource[remaining] = 0xaa;
            secondSource[remaining + 1] = 0xbb;

            var second = state.ReadPayload(secondSource, maskedPayload.Length);
            var combined = first.Payload.Concat(second.Payload).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(first.ConsumedCount, Is.EqualTo(split), $"First call at split {split}");
                Assert.That(second.ConsumedCount, Is.EqualTo(remaining), $"Second call at split {split}");
                Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete), $"Second call at split {split}");
                Assert.That(second.EndOfFrame, Is.True, $"Second call at split {split}");
                Assert.That(combined, Is.EqualTo(expected), $"Decoded bytes at split {split}");
            });
        }

        Assert.That(maskedPayload, Is.EqualTo(new byte[] { 0x7f, 0x9f, 0x4d, 0x51, 0x58 }));
    }

    [Test]
    public void PartialPayloadReturnsOwnedBytesAndNeedsMoreInput()
    {
        var state = CreateState("8204");
        byte[] source = [1, 2];

        var result = state.ReadPayload(source, 4);
        source[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(result.ConsumedCount, Is.EqualTo(2));
            Assert.That(result.Payload, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(result.PayloadCount, Is.EqualTo(2));
            Assert.That(result.EndOfFrame, Is.False);
        });
    }

    [Test]
    public void EmptyInputLeavesPendingPayloadUnchanged()
    {
        var state = CreateState("8201");

        var result = state.ReadPayload([], 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Payload, Is.Empty);
            Assert.That(result.PayloadCount, Is.Zero);
            Assert.That(result.EndOfFrame, Is.False);
        });
    }

    [Test]
    public void ZeroLengthPayloadCompletesWithoutConsumingNextFrame()
    {
        var state = CreateState("8100");
        byte[] nextFrame = [0x82, 0x01, 0xaa];

        var result = state.ReadPayload(nextFrame, 1);
        var repeated = state.ReadPayload(nextFrame, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Payload, Is.Empty);
            Assert.That(result.EndOfFrame, Is.True);
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.EndOfFrame, Is.True);
        });
    }

    [Test]
    public void DeclaredLengthIsRejectedBeforeInputIsConsumedOrCopied()
    {
        var state = CreateState("827e007e");
        byte[] source = [1, 2, 3];

        var result = state.ReadPayload(source, 125);
        var repeated = state.ReadPayload(source, 1024);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Payload, Is.Empty);
            Assert.That(result.PayloadCount, Is.Zero);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(result.Error.CloseCode, Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
            Assert.That(result.Error.Identifier, Is.EqualTo("frame-payload-too-large"));
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(source, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void HugeDeclaredLengthIsRejectedWithoutDependingOnSourceSize()
    {
        var state = CreateState("827f7fffffffffffffff");

        var result = state.ReadPayload(new byte[] { 0xaa }, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Payload, Is.Empty);
        });
    }

    [Test]
    public void PayloadLengthEqualToConfiguredLimitIsAcceptedIncrementally()
    {
        var state = CreateState("827e007e");

        var result = state.ReadPayload(new byte[] { 1, 2, 3 }, 126);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(result.ConsumedCount, Is.EqualTo(3));
            Assert.That(result.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.HasError, Is.False);
        });
    }

    [Test]
    public void NonpositiveConfiguredLimitFailsBeforePayloadAllocation()
    {
        var state = CreateState("8201");

        var result = state.ReadPayload(new byte[] { 1 }, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Payload, Is.Empty);
            Assert.That(result.Error.Identifier, Is.EqualTo("frame-limit-invalid"));
        });
    }

    [Test]
    public void HeaderAndPayloadConsumptionComposeWithoutLosingFollowingBytes()
    {
        var state = new FrameReadState();
        byte[] source = [0x82, 0x03, 10, 20, 30, 0x81, 0x00];

        var header = state.ReadHeader(source);
        var payload = state.ReadPayload(source.AsSpan(header.ConsumedCount), 3);

        Assert.Multiple(() =>
        {
            Assert.That(header.ConsumedCount, Is.EqualTo(2));
            Assert.That(payload.ConsumedCount, Is.EqualTo(3));
            Assert.That(payload.Payload, Is.EqualTo(new byte[] { 10, 20, 30 }));
            Assert.That(header.ConsumedCount + payload.ConsumedCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void PayloadReadWaitsForACompleteHeader()
    {
        var state = new FrameReadState();
        state.ReadHeader(new byte[] { 0x82 });

        var result = state.ReadPayload(new byte[] { 1, 2, 3 }, 3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Payload, Is.Empty);
        });
    }

    [Test]
    public void ResetDiscardsPartialPayloadProgress()
    {
        var state = CreateState("8203");
        state.ReadPayload(new byte[] { 1 }, 3);

        state.ResetHeader();
        var header = state.ReadHeader(Convert.FromHexString("8100"));
        var payload = state.ReadPayload(new byte[] { 0xaa }, 1);

        Assert.Multiple(() =>
        {
            Assert.That(header.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(header.Header.Opcode, Is.EqualTo(WebSocketOpcode.Text));
            Assert.That(payload.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(payload.ConsumedCount, Is.Zero);
            Assert.That(payload.EndOfFrame, Is.True);
        });
    }

    static FrameReadState CreateState(string hexadecimalHeader)
    {
        var state = new FrameReadState();
        var result = state.ReadHeader(Convert.FromHexString(hexadecimalHeader));
        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        return state;
    }
}
