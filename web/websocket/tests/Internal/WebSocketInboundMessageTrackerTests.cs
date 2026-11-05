using websocket;

namespace websocket.Tests;

public class WebSocketInboundMessageTrackerTests
{
    [Test]
    public void TracksAnUnfragmentedMessageAcrossPayloadChunks()
    {
        var tracker = new WebSocketInboundMessageTracker(4);
        WebSocketFrameHeader header = Header(
            WebSocketOpcode.Binary,
            true,
            4);
        byte[] payload = [9, 1, 2, 3, 4, 8];

        WebSocketInboundMessageTrackResult first =
            tracker.TrackInboundFrameChunk(
                Chunk(header, true, false, payload, 1, 2));
        WebSocketInboundMessageTrackResult last =
            tracker.TrackInboundFrameChunk(
                Chunk(header, false, true, payload, 3, 2));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first.Accepted, Is.True);
            Assert.That(first.FrameStarted, Is.True);
            Assert.That(first.FrameEnded, Is.False);
            Assert.That(first.DataChunk, Is.True);
            Assert.That(first.ControlChunk, Is.False);
            Assert.That(first.HasMessageType, Is.True);
            Assert.That(first.MessageType,
                Is.EqualTo(WebSocketMessageType.Binary));
            Assert.That(first.MessageStarted, Is.True);
            Assert.That(first.MessageEnded, Is.False);
            Assert.That(first.FragmentCount, Is.EqualTo(1));
            Assert.That(first.Payload, Is.SameAs(payload));
            Assert.That(first.PayloadOffset, Is.EqualTo(1));
            Assert.That(first.PayloadCount, Is.EqualTo(2));
            Assert.That(last.FrameStarted, Is.False);
            Assert.That(last.FrameEnded, Is.True);
            Assert.That(last.MessageStarted, Is.False);
            Assert.That(last.MessageEnded, Is.True);
            Assert.That(last.MessageType,
                Is.EqualTo(WebSocketMessageType.Binary));
        });

        WebSocketInboundMessageTrackResult next = TrackEmptyFrame(
            tracker,
            WebSocketOpcode.Text,
            true);
        Assert.That(next.MessageStarted, Is.True);
        Assert.That(next.MessageEnded, Is.True);
    }

    [Test]
    public void RetainsMessageTypeAndFragmentCountAcrossInterleavedControls()
    {
        var tracker = new WebSocketInboundMessageTracker(3);

        WebSocketInboundMessageTrackResult start = TrackFrame(
            tracker,
            WebSocketOpcode.Text,
            false,
            [65]);
        WebSocketInboundMessageTrackResult ping = TrackFrame(
            tracker,
            WebSocketOpcode.Ping,
            true,
            [1]);
        WebSocketInboundMessageTrackResult middle = TrackFrame(
            tracker,
            WebSocketOpcode.Continuation,
            false,
            [66]);
        WebSocketInboundMessageTrackResult pong = TrackFrame(
            tracker,
            WebSocketOpcode.Pong,
            true,
            [2]);
        WebSocketInboundMessageTrackResult end = TrackFrame(
            tracker,
            WebSocketOpcode.Continuation,
            true,
            [67]);

        Assert.Multiple(() =>
        {
            Assert.That(start.MessageStarted, Is.True);
            Assert.That(start.MessageEnded, Is.False);
            Assert.That(start.FragmentCount, Is.EqualTo(1));
            Assert.That(ping.ControlChunk, Is.True);
            Assert.That(ping.InterleavedControl, Is.True);
            Assert.That(ping.HasMessageType, Is.False);
            Assert.That(ping.FragmentCount, Is.Zero);
            Assert.That(middle.MessageStarted, Is.False);
            Assert.That(middle.MessageEnded, Is.False);
            Assert.That(middle.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(middle.FragmentCount, Is.EqualTo(2));
            Assert.That(pong.InterleavedControl, Is.True);
            Assert.That(end.MessageEnded, Is.True);
            Assert.That(end.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(end.FragmentCount, Is.EqualTo(3));
        });

        WebSocketInboundMessageTrackResult after = TrackFrame(
            tracker,
            WebSocketOpcode.Ping,
            true,
            [3]);
        Assert.That(after.InterleavedControl, Is.False);
    }

    [Test]
    public void RejectsAnOverlappingDataMessageAndLatchesTheFailure()
    {
        var tracker = new WebSocketInboundMessageTracker(4);
        TrackFrame(tracker, WebSocketOpcode.Text, false, [65]);

        WebSocketInboundMessageTrackResult overlap = TrackFrame(
            tracker,
            WebSocketOpcode.Binary,
            true,
            [1]);
        WebSocketInboundMessageTrackResult latched = TrackFrame(
            tracker,
            WebSocketOpcode.Continuation,
            true,
            [66]);

        AssertProtocolFailure(overlap, "message-fragment-overlap");
        Assert.Multiple(() =>
        {
            Assert.That(latched.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(latched.Accepted, Is.False);
            Assert.That(latched.Error.Identifier,
                Is.EqualTo("message-fragment-overlap"));
        });
    }

    [Test]
    public void RejectsContinuationWithoutAnActiveFragmentedMessage()
    {
        var beforeStart = new WebSocketInboundMessageTracker(4);
        WebSocketInboundMessageTrackResult orphan = TrackFrame(
            beforeStart,
            WebSocketOpcode.Continuation,
            true,
            [1]);

        var afterEnd = new WebSocketInboundMessageTracker(4);
        TrackFrame(afterEnd, WebSocketOpcode.Text, true, [65]);
        WebSocketInboundMessageTrackResult late = TrackFrame(
            afterEnd,
            WebSocketOpcode.Continuation,
            true,
            [66]);

        AssertProtocolFailure(orphan, "message-continuation-unexpected");
        AssertProtocolFailure(late, "message-continuation-unexpected");
    }

    [Test]
    public void EnforcesFragmentLimitWithoutCountingControlFrames()
    {
        var tracker = new WebSocketInboundMessageTracker(2);
        TrackFrame(tracker, WebSocketOpcode.Binary, false, [1]);
        TrackFrame(tracker, WebSocketOpcode.Ping, true, [2]);
        WebSocketInboundMessageTrackResult second = TrackFrame(
            tracker,
            WebSocketOpcode.Continuation,
            false,
            [3]);
        TrackFrame(tracker, WebSocketOpcode.Pong, true, [4]);

        WebSocketInboundMessageTrackResult excessive = TrackFrame(
            tracker,
            WebSocketOpcode.Continuation,
            true,
            [5]);

        Assert.Multiple(() =>
        {
            Assert.That(second.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.FragmentCount, Is.EqualTo(2));
            Assert.That(excessive.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(excessive.Accepted, Is.False);
            Assert.That(excessive.HasError, Is.True);
            Assert.That(excessive.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(excessive.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
            Assert.That(excessive.Error.KnownCloseCode,
                Is.EqualTo(WebSocketCloseCode.MessageTooBig));
            Assert.That(excessive.Error.Identifier,
                Is.EqualTo("message-fragment-limit-exceeded"));
        });
    }

    [Test]
    public void RejectsFragmentedControlFrames()
    {
        var tracker = new WebSocketInboundMessageTracker(4);

        WebSocketInboundMessageTrackResult result = TrackFrame(
            tracker,
            WebSocketOpcode.Ping,
            false,
            [1]);

        AssertProtocolFailure(result, "message-control-fragmented");
    }

    [Test]
    public void RequiresFrameChunksToContinueTheSameHeaderAndLength()
    {
        WebSocketFrameHeader header = Header(
            WebSocketOpcode.Binary,
            true,
            2);
        var changedHeaderTracker = new WebSocketInboundMessageTracker(4);
        changedHeaderTracker.TrackInboundFrameChunk(
            Chunk(header, true, false, [1], 0, 1));
        WebSocketFrameHeader changed = Header(
            WebSocketOpcode.Text,
            true,
            2);
        WebSocketInboundMessageTrackResult changedResult =
            changedHeaderTracker.TrackInboundFrameChunk(
                Chunk(changed, false, true, [2], 0, 1));

        var shortTracker = new WebSocketInboundMessageTracker(4);
        WebSocketFrameHeader longHeader = Header(
            WebSocketOpcode.Binary,
            true,
            3);
        WebSocketInboundMessageTrackResult shortResult =
            shortTracker.TrackInboundFrameChunk(
                Chunk(longHeader, true, true, [1, 2], 0, 2));

        var extraTracker = new WebSocketInboundMessageTracker(4);
        WebSocketInboundMessageTrackResult extraResult =
            extraTracker.TrackInboundFrameChunk(
                Chunk(header, true, true, [1, 2, 3], 0, 3));

        AssertProtocolFailure(
            changedResult,
            "message-frame-continuation-invalid");
        AssertProtocolFailure(
            shortResult,
            "message-frame-payload-length-invalid");
        AssertProtocolFailure(
            extraResult,
            "message-frame-payload-length-invalid");
    }

    [Test]
    public void RejectsChunkLifecycleOverlapAndOrphanChunks()
    {
        WebSocketFrameHeader header = Header(
            WebSocketOpcode.Binary,
            true,
            2);
        var overlapTracker = new WebSocketInboundMessageTracker(4);
        overlapTracker.TrackInboundFrameChunk(
            Chunk(header, true, false, [1], 0, 1));
        WebSocketInboundMessageTrackResult overlap =
            overlapTracker.TrackInboundFrameChunk(
                Chunk(header, true, true, [2], 0, 1));

        var orphanTracker = new WebSocketInboundMessageTracker(4);
        WebSocketInboundMessageTrackResult orphan =
            orphanTracker.TrackInboundFrameChunk(
                Chunk(header, false, true, [1, 2], 0, 2));

        AssertProtocolFailure(overlap, "message-frame-overlap");
        AssertProtocolFailure(orphan, "message-frame-continuation-invalid");
    }

    [Test]
    public void EmptyMessageStartsAndEndsInOneChunk()
    {
        var tracker = new WebSocketInboundMessageTracker(1);

        WebSocketInboundMessageTrackResult result = TrackEmptyFrame(
            tracker,
            WebSocketOpcode.Text,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.FrameStarted, Is.True);
            Assert.That(result.FrameEnded, Is.True);
            Assert.That(result.MessageStarted, Is.True);
            Assert.That(result.MessageEnded, Is.True);
            Assert.That(result.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(result.PayloadCount, Is.Zero);
        });
    }

    [Test]
    public void ResetClearsFailureAndFragmentationState()
    {
        var tracker = new WebSocketInboundMessageTracker(2);
        TrackFrame(tracker, WebSocketOpcode.Text, false, [65]);
        WebSocketInboundMessageTrackResult failed = TrackFrame(
            tracker,
            WebSocketOpcode.Binary,
            true,
            [1]);

        tracker.ResetInboundMessageTracker();
        WebSocketInboundMessageTrackResult recovered = TrackFrame(
            tracker,
            WebSocketOpcode.Binary,
            true,
            [2]);

        Assert.Multiple(() =>
        {
            Assert.That(failed.HasError, Is.True);
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(recovered.Accepted, Is.True);
            Assert.That(recovered.MessageStarted, Is.True);
            Assert.That(recovered.MessageEnded, Is.True);
            Assert.That(recovered.MessageType,
                Is.EqualTo(WebSocketMessageType.Binary));
        });
    }

    [Test]
    public void ConstructorRejectsNonPositiveFragmentLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketInboundMessageTracker(0));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketInboundMessageTracker(-1));
        });
    }

    static WebSocketInboundMessageTrackResult TrackFrame(
        WebSocketInboundMessageTracker tracker,
        WebSocketOpcode opcode,
        bool final,
        byte[] payload)
    {
        WebSocketFrameHeader header = Header(opcode, final, payload.Length);
        return tracker.TrackInboundFrameChunk(
            Chunk(header, true, true, payload, 0, payload.Length));
    }

    static WebSocketInboundMessageTrackResult TrackEmptyFrame(
        WebSocketInboundMessageTracker tracker,
        WebSocketOpcode opcode,
        bool final)
    {
        return TrackFrame(tracker, opcode, final, []);
    }

    static WebSocketFrameHeader Header(
        WebSocketOpcode opcode,
        bool final,
        long payloadLength)
    {
        return new WebSocketFrameHeader
        {
            Final = final,
            Opcode = opcode,
            PayloadLength = payloadLength
        };
    }

    static WebSocketInboundFrameChunk Chunk(
        WebSocketFrameHeader header,
        bool first,
        bool end,
        byte[] payload,
        int offset,
        int count)
    {
        return new WebSocketInboundFrameChunk
        {
            Header = header,
            FirstChunk = first,
            EndOfFrame = end,
            Payload = payload,
            PayloadOffset = offset,
            PayloadCount = count
        };
    }

    static void AssertProtocolFailure(
        WebSocketInboundMessageTrackResult result,
        string identifier)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Protocol));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(result.Error.KnownCloseCode,
                Is.EqualTo(WebSocketCloseCode.ProtocolError));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        });
    }
}
