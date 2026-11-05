using websocket;

namespace websocket.Tests;

public class WebSocketInboundFramePumpTests
{
    [Test]
    public void EmptyInputCompletesWithoutProducingAFrame()
    {
        var consumer = new FramePumpTestConsumer();
        var pump = CreatePump(consumer);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames([]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.FrameCount, Is.Zero);
            Assert.That(result.ChunkCount, Is.Zero);
            Assert.That(consumer.Chunks, Is.Empty);
        });
    }

    [Test]
    public void PumpsOneFrameAtEveryTwoCallSplitBoundary()
    {
        byte[] frame = Frame(WebSocketOpcode.Binary, [1, 2, 3, 4, 5]);
        int split = 0;
        while (split <= frame.Length)
        {
            var consumer = new FramePumpTestConsumer();
            var pump = CreatePump(consumer);

            WebSocketInboundFramePumpResult first = pump.PumpInboundFrames(
                frame.AsSpan(0, split));
            WebSocketInboundFramePumpResult second = pump.PumpInboundFrames(
                frame.AsSpan(split));

            Assert.That(first.ConsumedCount, Is.EqualTo(split));
            Assert.That(second.ConsumedCount, Is.EqualTo(frame.Length - split));
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(JoinPayload(consumer.Chunks),
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(consumer.Chunks[^1].EndOfFrame, Is.True);
            split++;
        }
    }

    [Test]
    public void PumpsManyFramesAndRetainsAPartialTrailingHeader()
    {
        byte[] first = Frame(WebSocketOpcode.Text, [65]);
        byte[] second = Frame(WebSocketOpcode.Binary, [2, 3]);
        byte[] third = Frame(WebSocketOpcode.Ping, [9]);
        byte[] initial = Combine(first, second, third.AsSpan(0, 1).ToArray());
        var consumer = new FramePumpTestConsumer();
        var pump = CreatePump(consumer);

        WebSocketInboundFramePumpResult partial =
            pump.PumpInboundFrames(initial);
        WebSocketInboundFramePumpResult completed =
            pump.PumpInboundFrames(third.AsSpan(1));

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status,
                Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(partial.ConsumedCount, Is.EqualTo(initial.Length));
            Assert.That(partial.FrameCount, Is.EqualTo(2));
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(completed.ConsumedCount, Is.EqualTo(third.Length - 1));
            Assert.That(completed.FrameCount, Is.EqualTo(1));
            Assert.That(consumer.Chunks.Select(chunk => chunk.Header.Opcode),
                Is.EqualTo(new[]
                {
                    WebSocketOpcode.Text,
                    WebSocketOpcode.Binary,
                    WebSocketOpcode.Ping
                }));
            Assert.That(JoinPayload(consumer.Chunks),
                Is.EqualTo(new byte[] { 65, 2, 3, 9 }));
        });
    }

    [TestCase(WebSocketParseStatus.NeedEventCapacity)]
    [TestCase(WebSocketParseStatus.NeedOutputCapacity)]
    public void CapacityBackpressureStopsBeforePayloadConsumption(
        WebSocketParseStatus blockedStatus)
    {
        byte[] frame = Frame(WebSocketOpcode.Binary, [1, 2, 3]);
        var consumer = new FramePumpTestConsumer();
        consumer.CapacityStatus = blockedStatus;
        var pump = CreatePump(consumer);

        WebSocketInboundFramePumpResult blocked = pump.PumpInboundFrames(frame);
        consumer.CapacityStatus = WebSocketParseStatus.Complete;
        WebSocketInboundFramePumpResult resumed = pump.PumpInboundFrames(
            frame.AsSpan(blocked.ConsumedCount));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status, Is.EqualTo(blockedStatus));
            Assert.That(blocked.ConsumedCount, Is.EqualTo(2));
            Assert.That(blocked.FrameCount, Is.Zero);
            Assert.That(blocked.ChunkCount, Is.Zero);
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.ConsumedCount, Is.EqualTo(3));
            Assert.That(JoinPayload(consumer.Chunks),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void EmptyFrameWaitsForCapacityAfterConsumingOnlyItsHeader()
    {
        byte[] frame = Frame(WebSocketOpcode.Pong, []);
        var consumer = new FramePumpTestConsumer();
        consumer.CapacityStatus = WebSocketParseStatus.NeedOutputCapacity;
        var pump = CreatePump(consumer);

        WebSocketInboundFramePumpResult blocked = pump.PumpInboundFrames(frame);
        consumer.CapacityStatus = WebSocketParseStatus.Complete;
        WebSocketInboundFramePumpResult resumed = pump.PumpInboundFrames([]);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.ConsumedCount, Is.EqualTo(2));
            Assert.That(resumed.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.FrameCount, Is.EqualTo(1));
            Assert.That(consumer.Chunks, Has.Count.EqualTo(1));
            Assert.That(consumer.Chunks[0].FirstChunk, Is.True);
            Assert.That(consumer.Chunks[0].EndOfFrame, Is.True);
            Assert.That(consumer.Chunks[0].PayloadCount, Is.Zero);
        });
    }

    [Test]
    public void ConsumerCapacityBoundsEveryDeliveredPayloadChunk()
    {
        var consumer = new FramePumpTestConsumer();
        consumer.MaximumPayloadCount = 2;
        var pump = CreatePump(consumer);
        byte[] frame = Frame(WebSocketOpcode.Binary, [1, 2, 3, 4, 5]);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(frame.Length));
            Assert.That(result.FrameCount, Is.EqualTo(1));
            Assert.That(result.ChunkCount, Is.EqualTo(3));
            Assert.That(consumer.Chunks.Select(chunk => chunk.PayloadCount),
                Is.EqualTo(new[] { 2, 2, 1 }));
            Assert.That(consumer.Chunks.Select(chunk => chunk.FirstChunk),
                Is.EqualTo(new[] { true, false, false }));
            Assert.That(consumer.Chunks.Select(chunk => chunk.EndOfFrame),
                Is.EqualTo(new[] { false, false, true }));
        });
    }

    [Test]
    public void ExtendedLengthFrameIsPumpedWithoutWholePayloadBuffering()
    {
        byte[] payload = new byte[130];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = (byte)index;
            index++;
        }

        byte[] frame = new byte[payload.Length + 4];
        frame[0] = 0x82;
        frame[1] = 126;
        frame[2] = 0;
        frame[3] = 130;
        payload.CopyTo(frame, 4);
        var consumer = new FramePumpTestConsumer();
        consumer.MaximumPayloadCount = 17;
        var pump = CreatePump(consumer);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(frame.Length));
            Assert.That(result.FrameCount, Is.EqualTo(1));
            Assert.That(result.ChunkCount, Is.EqualTo(8));
            Assert.That(JoinPayload(consumer.Chunks), Is.EqualTo(payload));
            Assert.That(consumer.Chunks.All(
                chunk => chunk.PayloadCount <= 17), Is.True);
        });
    }

    [Test]
    public void UnexpectedConsumerBackpressureRetainsTheConsumedChunkForRetry()
    {
        byte[] first = Frame(WebSocketOpcode.Binary, [1, 2]);
        byte[] second = Frame(WebSocketOpcode.Text, [65]);
        byte[] source = Combine(first, second);
        var consumer = new FramePumpTestConsumer();
        consumer.RejectNextConsumeStatus =
            WebSocketParseStatus.NeedEventCapacity;
        var pump = CreatePump(consumer);

        WebSocketInboundFramePumpResult blocked = pump.PumpInboundFrames(source);
        WebSocketInboundFramePumpResult resumed = pump.PumpInboundFrames(
            source.AsSpan(blocked.ConsumedCount));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(blocked.ConsumedCount, Is.EqualTo(first.Length));
            Assert.That(blocked.FrameCount, Is.Zero);
            Assert.That(consumer.RejectedConsumeCount, Is.EqualTo(1));
            Assert.That(resumed.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.ConsumedCount, Is.EqualTo(second.Length));
            Assert.That(resumed.FrameCount, Is.EqualTo(2));
            Assert.That(consumer.Chunks, Has.Count.EqualTo(2));
            Assert.That(JoinPayload(consumer.Chunks),
                Is.EqualTo(new byte[] { 1, 2, 65 }));
        });
    }

    [Test]
    public void FrameFailureLatchesWithoutConsumingTrailingBytesAndResetRecovers()
    {
        byte[] invalidMaskedEmptyFrame =
            [0x82, 0x80, 1, 2, 3, 4, 99, 100];
        var consumer = new FramePumpTestConsumer();
        var pump = CreatePump(consumer);

        WebSocketInboundFramePumpResult failed =
            pump.PumpInboundFrames(invalidMaskedEmptyFrame);
        WebSocketInboundFramePumpResult repeated =
            pump.PumpInboundFrames([7, 8]);
        pump.ResetInboundFramePump();
        byte[] valid = Frame(WebSocketOpcode.Binary, [7]);
        WebSocketInboundFramePumpResult recovered =
            pump.PumpInboundFrames(valid);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failed.ConsumedCount, Is.EqualTo(6));
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("frame-mask-forbidden"));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Error.Identifier,
                Is.EqualTo("frame-mask-forbidden"));
            Assert.That(consumer.ResetCount, Is.EqualTo(1));
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(JoinPayload(consumer.Chunks),
                Is.EqualTo(new byte[] { 7 }));
        });
    }

    [Test]
    public void ConsumerFailureLatchesAndDoesNotDeliverLaterFrames()
    {
        var consumer = new FramePumpTestConsumer();
        consumer.FailNextConsume = true;
        var pump = CreatePump(consumer);
        byte[] source = Combine(
            Frame(WebSocketOpcode.Binary, [1]),
            Frame(WebSocketOpcode.Binary, [2]));

        WebSocketInboundFramePumpResult failed = pump.PumpInboundFrames(source);
        WebSocketInboundFramePumpResult repeated = pump.PumpInboundFrames(source);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failed.ConsumedCount, Is.EqualTo(3));
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("test-frame-consumer-failure"));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Error.Identifier,
                Is.EqualTo("test-frame-consumer-failure"));
            Assert.That(consumer.Chunks, Is.Empty);
        });
    }

    static WebSocketInboundFramePump CreatePump(
        FramePumpTestConsumer consumer)
    {
        IWebSocketFrameCodec codec = new WebSocketFrameCodec(
            WebSocketRole.Client,
            1000,
            []);
        return new WebSocketInboundFramePump(codec, consumer);
    }

    static byte[] Frame(WebSocketOpcode opcode, byte[] payload)
    {
        byte[] frame = new byte[payload.Length + 2];
        frame[0] = (byte)(128 | (int)opcode);
        frame[1] = (byte)payload.Length;
        payload.CopyTo(frame, 2);
        return frame;
    }

    static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    static byte[] Combine(byte[] first, byte[] second, byte[] third)
    {
        return Combine(Combine(first, second), third);
    }

    static byte[] JoinPayload(List<WebSocketInboundFrameChunk> chunks)
    {
        var payload = new List<byte>();
        int chunkIndex = 0;
        while (chunkIndex < chunks.Count)
        {
            int payloadIndex = 0;
            while (payloadIndex < chunks[chunkIndex].PayloadCount)
            {
                payload.Add(chunks[chunkIndex].Payload[
                    chunks[chunkIndex].PayloadOffset + payloadIndex]);
                payloadIndex++;
            }

            chunkIndex++;
        }

        return payload.ToArray();
    }
}

internal class FramePumpTestConsumer : IWebSocketInboundFrameConsumer
{
    internal WebSocketParseStatus CapacityStatus = WebSocketParseStatus.Complete;
    internal int MaximumPayloadCount = 1024;
    internal WebSocketParseStatus RejectNextConsumeStatus =
        WebSocketParseStatus.Complete;
    internal bool FailNextConsume;
    internal int RejectedConsumeCount;
    internal int ResetCount;
    internal List<WebSocketInboundFrameChunk> Chunks = [];

    public WebSocketInboundFrameCapacity GetInboundFrameCapacity(
        WebSocketFrameHeader header,
        bool firstChunk)
    {
        var result = new WebSocketInboundFrameCapacity();
        result.Status = CapacityStatus;
        result.MaximumPayloadCount = MaximumPayloadCount;
        return result;
    }

    public WebSocketInboundFrameConsumeResult ConsumeInboundFrameChunk(
        WebSocketInboundFrameChunk chunk)
    {
        if (FailNextConsume)
        {
            FailNextConsume = false;
            var failure = new WebSocketInboundFrameConsumeResult();
            failure.Status = WebSocketParseStatus.ProtocolFailure;
            failure.Accepted = false;
            failure.HasError = true;
            failure.Error.Kind = WebSocketErrorKind.Protocol;
            failure.Error.Identifier = "test-frame-consumer-failure";
            return failure;
        }

        if (RejectNextConsumeStatus != WebSocketParseStatus.Complete)
        {
            WebSocketParseStatus status = RejectNextConsumeStatus;
            RejectNextConsumeStatus = WebSocketParseStatus.Complete;
            RejectedConsumeCount++;
            var blocked = new WebSocketInboundFrameConsumeResult();
            blocked.Status = status;
            blocked.Accepted = false;
            return blocked;
        }

        Chunks.Add(chunk);
        return new WebSocketInboundFrameConsumeResult();
    }

    public void ResetInboundFrameConsumer()
    {
        CapacityStatus = WebSocketParseStatus.Complete;
        RejectNextConsumeStatus = WebSocketParseStatus.Complete;
        FailNextConsume = false;
        Chunks.Clear();
        ResetCount++;
    }

    public void HandleInboundFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
    }
}
