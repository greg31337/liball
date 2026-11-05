using System.Text;
using htmlgui;

namespace htmlgui.Tests;

public class BridgeChunkCoreTests
{
    [Test]
    public void SplitsAndReassemblesBinaryChunksByIndexWithExactMetadata()
    {
        (BridgeChunkCore sender, BridgeChunkCore receiver) = ReadyChunkCores(4, 64, 4, 50);
        byte[] data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 254, 255 };

        BridgeChunkOperationResult split = sender.SplitBridgeMessage("chunk-1", data, 121);
        UiBridgeEnvelope thirdFirst = CopyEnvelope(split.Envelopes[2], 1);
        UiBridgeEnvelope firstSecond = CopyEnvelope(split.Envelopes[0], 2);
        UiBridgeEnvelope secondThird = CopyEnvelope(split.Envelopes[1], 3);
        BridgeChunkOperationResult first = receiver.AcceptBridgeChunk(thirdFirst, true, 122);
        BridgeChunkOperationResult second = receiver.AcceptBridgeChunk(firstSecond, true, 123);
        BridgeChunkOperationResult complete = receiver.AcceptBridgeChunk(secondThird, true, 124);

        Assert.Multiple(() =>
        {
            Assert.That(split.Success, Is.True);
            Assert.That(split.Envelopes.Length, Is.EqualTo(3));
            Assert.That(split.Envelopes.Select(item => item.Kind), Is.All.EqualTo(UiBridgeMessageKind.Chunk));
            Assert.That(split.Envelopes.Select(item => item.Sequence), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(split.Checksum, Has.Length.EqualTo(64));
            Assert.That(first.Complete, Is.False);
            Assert.That(second.Complete, Is.False);
            Assert.That(complete.Success, Is.True);
            Assert.That(complete.Complete, Is.True);
            Assert.That(complete.Data, Is.EqualTo(data));
            Assert.That(receiver.BufferedBytes, Is.Zero);
            Assert.That(receiver.Assemblies, Is.Empty);
        });
    }

    [Test]
    public void DetectsEnvelopeAndChunkDuplicatesWithoutDeliveringTwice()
    {
        (BridgeChunkCore sender, BridgeChunkCore receiver) = ReadyChunkCores(3, 32, 4, 50);
        BridgeChunkOperationResult split = sender.SplitBridgeMessage("chunk-1", Encoding.UTF8.GetBytes("abcdef"), 121);

        BridgeChunkOperationResult first = receiver.AcceptBridgeChunk(split.Envelopes[0], true, 122);
        BridgeChunkOperationResult sameEnvelope = receiver.AcceptBridgeChunk(split.Envelopes[0], true, 123);
        UiBridgeEnvelope repeatedChunk = CopyEnvelope(split.Envelopes[0], 2);
        BridgeChunkOperationResult sameChunk = receiver.AcceptBridgeChunk(repeatedChunk, true, 124);
        UiBridgeEnvelope final = CopyEnvelope(split.Envelopes[1], 3);
        BridgeChunkOperationResult complete = receiver.AcceptBridgeChunk(final, true, 125);
        UiBridgeEnvelope replayAfterCompletion = CopyEnvelope(split.Envelopes[1], 4);
        BridgeChunkOperationResult historical = receiver.AcceptBridgeChunk(replayAfterCompletion, true, 126);

        Assert.Multiple(() =>
        {
            Assert.That(first.Applied, Is.True);
            Assert.That(sameEnvelope.Duplicate, Is.True);
            Assert.That(receiver.Session.LastInboundSequence, Is.EqualTo(4));
            Assert.That(sameChunk.Duplicate, Is.True);
            Assert.That(complete.Complete, Is.True);
            Assert.That(Encoding.UTF8.GetString(complete.Data!), Is.EqualTo("abcdef"));
            Assert.That(historical.Duplicate, Is.True);
            Assert.That(historical.Data, Is.Null);
        });
    }

    [Test]
    public void RejectsChecksumLengthBase64AndConflictingChunkData()
    {
        (BridgeChunkCore sender, BridgeChunkCore receiver) = ReadyChunkCores(4, 32, 4, 50);
        BridgeChunkOperationResult split = sender.SplitBridgeMessage("chunk-1", Encoding.UTF8.GetBytes("abcdefgh"), 121);
        SetPayloadText(split.Envelopes[0].Payload!, BridgeProtocolNames.Checksum, new string('0', 64));
        SetPayloadText(split.Envelopes[1].Payload!, BridgeProtocolNames.Checksum, new string('0', 64));
        BridgeChunkOperationResult partial = receiver.AcceptBridgeChunk(split.Envelopes[0], true, 122);
        UiBridgeEnvelope conflicting = CopyEnvelope(split.Envelopes[0], 2);
        SetPayloadText(conflicting.Payload!, BridgeProtocolNames.ChunkData, "aWprbA==");
        BridgeChunkOperationResult conflict = receiver.AcceptBridgeChunk(conflicting, true, 123);
        BridgeChunkOperationResult checksum = receiver.AcceptBridgeChunk(split.Envelopes[1], true, 124);
        UiValue malformedLength = receiver.CreateChunkPayload("chunk-x", 0, 2, 9, new string('0', 64), "YWJjZA==");
        UiValue malformedBase64 = receiver.CreateChunkPayload("chunk-y", 0, 1, 1, new string('0', 64), "AB==");

        Assert.Multiple(() =>
        {
            Assert.That(partial.Success, Is.True);
            Assert.That(conflict.Code, Is.EqualTo("bridge.chunk-data-conflict"));
            Assert.That(checksum.Code, Is.EqualTo("bridge.chunk-checksum-mismatch"));
            Assert.That(checksum.Success, Is.False);
            Assert.That(checksum.Applied, Is.True);
            Assert.That(receiver.ParseChunkPayload(malformedLength).Code, Is.EqualTo("bridge.invalid-chunk-shape"));
            Assert.That(receiver.ParseChunkPayload(malformedBase64).Code, Is.EqualTo("bridge.invalid-base64"));
            Assert.That(receiver.Assemblies, Is.Empty);
            Assert.That(receiver.BufferedBytes, Is.Zero);
        });
    }

    [Test]
    public void ExpiresIncompleteAssembliesAndRejectsLateChunks()
    {
        (BridgeChunkCore sender, BridgeChunkCore receiver) = ReadyChunkCores(4, 8, 1, 10);
        BridgeChunkOperationResult firstMessage = sender.SplitBridgeMessage("chunk-a", Encoding.UTF8.GetBytes("abcdefgh"), 121);
        BridgeChunkOperationResult secondMessage = sender.SplitBridgeMessage("chunk-b", Encoding.UTF8.GetBytes("ijklmnop"), 122);
        receiver.AcceptBridgeChunk(firstMessage.Envelopes[0], true, 121);
        UiBridgeEnvelope blockedFirst = CopyEnvelope(secondMessage.Envelopes[0], 2);
        BridgeChunkOperationResult blocked = receiver.AcceptBridgeChunk(blockedFirst, true, 122);
        BridgeChunkOperationResult early = receiver.SweepExpiredBridgeChunks(130);
        BridgeChunkOperationResult expired = receiver.SweepExpiredBridgeChunks(131);
        BridgeChunkOperationResult resumed = receiver.AcceptBridgeChunk(blockedFirst, true, 132);
        UiBridgeEnvelope lateFirst = CopyEnvelope(firstMessage.Envelopes[1], 3);
        BridgeChunkOperationResult late = receiver.AcceptBridgeChunk(lateFirst, true, 133);
        BridgeChunkOperationResult completed = receiver.AcceptBridgeChunk(secondMessage.Envelopes[1], true, 134);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Backpressured, Is.True);
            Assert.That(blocked.Code, Is.EqualTo("bridge.chunk-assembly-backpressure"));
            Assert.That(early.Expired, Is.False);
            Assert.That(expired.Expired, Is.True);
            Assert.That(expired.ExpiredChunkIds, Is.EqualTo(new[] { "chunk-a" }));
            Assert.That(resumed.Success, Is.True);
            Assert.That(late.Expired, Is.True);
            Assert.That(late.Success, Is.False);
            Assert.That(late.Applied, Is.True);
            Assert.That(late.Code, Is.EqualTo("bridge.chunk-expired"));
            Assert.That(completed.Complete, Is.True);
            Assert.That(Encoding.UTF8.GetString(completed.Data!), Is.EqualTo("ijklmnop"));
            Assert.That(receiver.BufferedBytes, Is.Zero);
        });
    }

    [Test]
    public void EnforcesGlobalBufferConfigurationCapabilityAndEnvelopeLimits()
    {
        (BridgeChunkCore sender, BridgeChunkCore receiver) = ReadyChunkCores(4, 6, 2, 10);
        BridgeChunkOperationResult first = sender.SplitBridgeMessage("chunk-a", Encoding.UTF8.GetBytes("abcdef"), 121);
        BridgeChunkOperationResult second = sender.SplitBridgeMessage("chunk-b", Encoding.UTF8.GetBytes("ghijkl"), 122);
        receiver.AcceptBridgeChunk(first.Envelopes[0], true, 123);
        UiBridgeEnvelope competing = CopyEnvelope(second.Envelopes[0], 2);
        BridgeChunkOperationResult bufferBlocked = receiver.AcceptBridgeChunk(competing, true, 124);
        BridgeChunkCore invalid = new BridgeChunkCore(receiver.Session, 0, 8, 1, 10);
        BridgeChunkOperationResult invalidConfiguration = invalid.SplitBridgeMessage("chunk", new byte[] { 1 }, 125);
        receiver.Session.NegotiatedCapabilities!.SupportsChunking = false;
        BridgeChunkOperationResult unsupported = receiver.SplitBridgeMessage("chunk", new byte[] { 1 }, 126);
        (BridgeChunkCore smallSender, _) = ReadyChunkCores(4, 8, 1, 10, 128);
        BridgeChunkOperationResult oversizedEnvelope = smallSender.SplitBridgeMessage(new string('x', 100), new byte[] { 1 }, 121);

        Assert.Multiple(() =>
        {
            Assert.That(bufferBlocked.Backpressured, Is.True);
            Assert.That(bufferBlocked.Code, Is.EqualTo("bridge.chunk-buffer-backpressure"));
            Assert.That(receiver.Assemblies.Count, Is.EqualTo(1));
            Assert.That(receiver.BufferedBytes, Is.EqualTo(4));
            Assert.That(invalidConfiguration.Code, Is.EqualTo("bridge.invalid-chunk-configuration"));
            Assert.That(unsupported.Code, Is.EqualTo("bridge.chunking-unsupported"));
            Assert.That(oversizedEnvelope.Code, Is.EqualTo("bridge.chunk-envelope-too-large"));
            Assert.That(smallSender.Session.NextOutboundSequence, Is.EqualTo(1));
        });
    }

    private static (BridgeChunkCore Sender, BridgeChunkCore Receiver) ReadyChunkCores(int maximumChunkBytes, int maximumAssemblyBytes, int maximumAssemblyCount, int expiryMilliseconds, int maximumMessageLength = 4096)
    {
        BridgeSession senderSession = Session(BridgeProtocolNames.BrowserOrigin, maximumMessageLength);
        BridgeSession receiverSession = Session(BridgeProtocolNames.HostOrigin, maximumMessageLength);
        BridgeSessionOperation initialize = senderSession.BeginBridgeHandshake("session-1", "nonce-1", 100);
        BridgeSessionOperation ready = receiverSession.AcceptBridgeInitialize(initialize.Envelope, true, 110);
        BridgeSessionOperation accepted = senderSession.AcceptBridgeReady(ready.Envelope, true, 120);
        Assert.That(accepted.Success, Is.True);
        return (
            new BridgeChunkCore(senderSession, maximumChunkBytes, maximumAssemblyBytes, maximumAssemblyCount, expiryMilliseconds),
            new BridgeChunkCore(receiverSession, maximumChunkBytes, maximumAssemblyBytes, maximumAssemblyCount, expiryMilliseconds));
    }

    private static BridgeSession Session(string expectedOrigin, int maximumMessageLength)
    {
        UiBridgeOptions options = new UiBridgeOptions
        {
            Version = BridgeProtocolVersions.Current,
            Origin = expectedOrigin,
            RequireMainFrame = true,
            MaximumMessageLength = maximumMessageLength,
            MaximumPendingRequests = 8,
            TimeoutMilliseconds = 1000
        };
        UiTransportCapabilities capabilities = new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current,
            SupportsBinary = true,
            SupportsChunking = true
        };
        return new BridgeSession(options, capabilities);
    }

    private static UiBridgeEnvelope CopyEnvelope(UiBridgeEnvelope source, int sequence)
    {
        return new UiBridgeEnvelope
        {
            Version = source.Version,
            Session = source.Session,
            Sequence = sequence,
            CorrelationId = source.CorrelationId,
            Origin = source.Origin,
            Kind = source.Kind,
            Payload = CloneValue(source.Payload),
            Request = source.Request,
            Response = source.Response,
            Error = source.Error
        };
    }

    private static UiValue? CloneValue(UiValue? value)
    {
        return value == null ? null : ValueJsonCodec.CloneNode(value);
    }

    private static void SetPayloadText(UiValue payload, string key, string value)
    {
        payload.ObjectValue!.Single(item => item.Key == key).Value.StringValue = value;
    }
}
