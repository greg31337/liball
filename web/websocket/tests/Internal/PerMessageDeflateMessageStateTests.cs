using websocket;

namespace websocket.Tests;

public class PerMessageDeflateMessageStateTests
{
    [Test]
    public void OutgoingCompressionHonorsOptOutAndInclusiveThreshold()
    {
        PerMessageDeflateMessageState state = CreateState(10, 1000);

        PerMessageDeflateMessageResult below = state.BeginOutgoingMessage(
            WebSocketMessageType.Binary,
            true,
            9,
            true);
        byte[] belowPayload = RandomBytes(9, 1);
        List<byte> belowOutput = TransformOutgoing(
            state,
            belowPayload,
            2);
        PerMessageDeflateMessageResult belowEnd = FinishOutgoing(state, 2);
        PerMessageDeflateMessageResult optedOut = state.BeginOutgoingMessage(
            WebSocketMessageType.Binary,
            false,
            20,
            true);
        byte[] optedPayload = RandomBytes(20, 2);
        List<byte> optedOutput = TransformOutgoing(
            state,
            optedPayload,
            3);
        PerMessageDeflateMessageResult optedEnd = FinishOutgoing(state, 3);
        PerMessageDeflateMessageResult threshold = state.BeginOutgoingMessage(
            WebSocketMessageType.Binary,
            true,
            10,
            true);
        TransformOutgoing(state, RandomBytes(10, 3), 2);
        PerMessageDeflateMessageResult thresholdEnd = FinishOutgoing(state, 2);

        Assert.Multiple(() =>
        {
            Assert.That(below.Compressed, Is.False);
            Assert.That(below.SetRsv1, Is.False);
            Assert.That(belowOutput, Is.EqualTo(belowPayload));
            Assert.That(belowEnd.MessageComplete, Is.True);
            Assert.That(optedOut.Compressed, Is.False);
            Assert.That(optedOut.SetRsv1, Is.False);
            Assert.That(optedOutput, Is.EqualTo(optedPayload));
            Assert.That(optedEnd.MessageComplete, Is.True);
            Assert.That(threshold.Compressed, Is.True);
            Assert.That(threshold.SetRsv1, Is.True);
            Assert.That(thresholdEnd.MessageComplete, Is.True);
        });
    }

    [Test]
    public void OutgoingRsv1AppearsOnlyOnFirstFragmentAndControlsInterleave()
    {
        byte[] source = new byte[40000];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = (byte)('a' + (index % 11));
            index++;
        }

        PerMessageDeflateMessageState state = CreateState(0, source.Length);
        PerMessageDeflateMessageResult first = state.BeginOutgoingMessage(
            WebSocketMessageType.Text,
            true,
            source.Length,
            false);
        List<byte> compressed = TransformOutgoing(
            state,
            source.AsSpan(0, 33000).ToArray(),
            7);
        PerMessageDeflateMessageResult firstEnd = FinishOutgoing(state, 7);
        PerMessageDeflateMessageResult control = state.ObserveOutgoingControl(
            WebSocketOpcode.Ping,
            false);
        PerMessageDeflateMessageResult continuation =
            state.BeginOutgoingContinuation(true);
        compressed.AddRange(TransformOutgoing(
            state,
            source.AsSpan(33000).ToArray(),
            5));
        PerMessageDeflateMessageResult finalEnd = FinishOutgoingTo(
            state,
            3,
            compressed);
        byte[] decoded = DecompressPayload(
            compressed.ToArray(),
            source.Length,
            15);

        Assert.Multiple(() =>
        {
            Assert.That(first.SetRsv1, Is.True);
            Assert.That(first.Compressed, Is.True);
            Assert.That(firstEnd.MessageComplete, Is.False);
            Assert.That(control.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(state.GetOutgoingMessageCompressed(), Is.False);
            Assert.That(continuation.SetRsv1, Is.False);
            Assert.That(continuation.Compressed, Is.True);
            Assert.That(finalEnd.MessageComplete, Is.True);
            Assert.That(decoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void IncomingCompressedFragmentsSurviveControlInterleaving()
    {
        byte[] source = "A\u20acB across fragments"u8.ToArray();
        byte[] compressed = CompressPayload(source, 15);
        int firstCount = compressed.Length / 3;
        int secondCount = compressed.Length / 3;
        int thirdOffset = firstCount + secondCount;
        PerMessageDeflateMessageState state = CreateState(0, 1000);
        var output = new List<byte>();

        PerMessageDeflateMessageResult first = state.BeginIncomingDataFrame(
            WebSocketOpcode.Text,
            true,
            false);
        output.AddRange(TransformIncoming(
            state,
            compressed.AsSpan(0, firstCount).ToArray(),
            1));
        PerMessageDeflateMessageResult firstEnd = FinishIncoming(state, 1);
        PerMessageDeflateMessageResult ping = state.ObserveIncomingControl(
            WebSocketOpcode.Ping,
            false);
        PerMessageDeflateMessageResult second = state.BeginIncomingDataFrame(
            WebSocketOpcode.Continuation,
            false,
            false);
        output.AddRange(TransformIncoming(
            state,
            compressed.AsSpan(firstCount, secondCount).ToArray(),
            1));
        FinishIncoming(state, 1);
        PerMessageDeflateMessageResult pong = state.ObserveIncomingControl(
            WebSocketOpcode.Pong,
            false);
        PerMessageDeflateMessageResult third = state.BeginIncomingDataFrame(
            WebSocketOpcode.Continuation,
            false,
            true);
        output.AddRange(TransformIncoming(
            state,
            compressed.AsSpan(thirdOffset).ToArray(),
            1));
        PerMessageDeflateMessageResult final = FinishIncomingTo(
            state,
            1,
            output);

        Assert.Multiple(() =>
        {
            Assert.That(first.Compressed, Is.True);
            Assert.That(firstEnd.MessageComplete, Is.False);
            Assert.That(ping.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.Compressed, Is.True);
            Assert.That(pong.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(third.Compressed, Is.True);
            Assert.That(final.MessageComplete, Is.True);
            Assert.That(output, Is.EqualTo(source));
        });
    }

    [Test]
    public void ContinuationAndControlFramesRejectRsv1()
    {
        PerMessageDeflateMessageState continuationState = CreateState(0, 100);
        continuationState.BeginIncomingDataFrame(
            WebSocketOpcode.Binary,
            true,
            false);
        continuationState.FinishIncomingFrame([]);

        PerMessageDeflateMessageResult continuation =
            continuationState.BeginIncomingDataFrame(
                WebSocketOpcode.Continuation,
                true,
                true);
        PerMessageDeflateMessageState controlState = CreateState(0, 100);
        controlState.BeginIncomingDataFrame(
            WebSocketOpcode.Binary,
            true,
            false);
        PerMessageDeflateMessageResult control =
            controlState.ObserveIncomingControl(
                WebSocketOpcode.Ping,
                true);
        PerMessageDeflateMessageState outgoingControlState =
            CreateState(0, 100);
        PerMessageDeflateMessageResult outgoingControl =
            outgoingControlState.ObserveOutgoingControl(
                WebSocketOpcode.Pong,
                true);

        Assert.Multiple(() =>
        {
            Assert.That(continuation.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-continuation-rsv1-invalid"));
            Assert.That(control.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-control-rsv1-invalid"));
            Assert.That(outgoingControl.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-control-rsv1-invalid"));
        });
    }

    [Test]
    public void Utf8ValidationRunsOnDecompressedBytesNotWireBytes()
    {
        byte[] invalidText = [0xff];
        byte[] compressed = CompressPayload(invalidText, 15);
        PerMessageDeflateMessageState textState = CreateState(0, 100);
        textState.BeginIncomingDataFrame(
            WebSocketOpcode.Text,
            true,
            true);

        PerMessageDeflateMessageResult invalid = TransformIncomingResult(
            textState,
            compressed,
            10);
        PerMessageDeflateMessageState binaryState = CreateState(0, 100);
        binaryState.BeginIncomingDataFrame(
            WebSocketOpcode.Binary,
            true,
            true);
        List<byte> binaryOutput = TransformIncoming(
            binaryState,
            compressed,
            10);
        PerMessageDeflateMessageResult binaryEnd = FinishIncoming(
            binaryState,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalid.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-text-utf8-invalid"));
            Assert.That(binaryOutput, Is.EqualTo(invalidText));
            Assert.That(binaryEnd.MessageComplete, Is.True);
        });
    }

    [Test]
    public void IncompleteDecompressedTextFailsOnlyAtMessageCompletion()
    {
        byte[] incompleteText = [0xe2, 0x82];
        byte[] compressed = CompressPayload(incompleteText, 15);
        PerMessageDeflateMessageState state = CreateState(0, 100);
        state.BeginIncomingDataFrame(
            WebSocketOpcode.Text,
            true,
            true);

        PerMessageDeflateMessageResult transformed =
            TransformIncomingResult(state, compressed, 1);
        PerMessageDeflateMessageResult completed = FinishIncoming(state, 1);

        Assert.Multiple(() =>
        {
            Assert.That(transformed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(completed.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-text-utf8-incomplete"));
        });
    }

    [Test]
    public void CompressedAndUncompressedMessagesCanAlternate()
    {
        PerMessageDeflateMessageState state = CreateState(5, 100);
        byte[] compressedSource = "compress me"u8.ToArray();
        PerMessageDeflateMessageResult compressed = state.BeginOutgoingMessage(
            WebSocketMessageType.Binary,
            true,
            compressedSource.Length,
            true);
        TransformOutgoing(state, compressedSource, 2);
        FinishOutgoing(state, 2);
        byte[] plainSource = [1, 2, 3];
        PerMessageDeflateMessageResult plain = state.BeginOutgoingMessage(
            WebSocketMessageType.Binary,
            true,
            plainSource.Length,
            true);
        List<byte> plainOutput = TransformOutgoing(state, plainSource, 1);
        FinishOutgoing(state, 1);

        Assert.Multiple(() =>
        {
            Assert.That(compressed.Compressed, Is.True);
            Assert.That(plain.Compressed, Is.False);
            Assert.That(plain.SetRsv1, Is.False);
            Assert.That(plainOutput, Is.EqualTo(plainSource));
        });
    }

    static PerMessageDeflateMessageState CreateState(
        int threshold,
        int maximumDecompressedLength)
    {
        var settings = new PerMessageDeflateDirectionSettings();
        settings.Valid = true;
        settings.OutgoingContextTakeover = true;
        settings.IncomingContextTakeover = true;
        settings.OutgoingWindowBits = 15;
        settings.IncomingWindowBits = 15;
        var codec = new PerMessageDeflateCodec(
            settings,
            maximumDecompressedLength);
        return new PerMessageDeflateMessageState(codec, threshold);
    }

    static List<byte> TransformOutgoing(
        PerMessageDeflateMessageState state,
        byte[] source,
        int outputChunkSize)
    {
        var output = new List<byte>();
        PerMessageDeflateMessageResult result =
            new PerMessageDeflateMessageResult();
        int sourceIndex = 0;
        int iteration = 0;
        while ((sourceIndex < source.Length ||
                result.Status == WebSocketParseStatus.NeedOutputCapacity) &&
            iteration < 100000)
        {
            var destination = new byte[outputChunkSize];
            result = state.TransformOutgoingPayload(
                source.AsSpan(sourceIndex),
                destination);
            sourceIndex = sourceIndex + result.ConsumedCount;
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        Assert.That(iteration, Is.LessThan(100000));
        return output;
    }

    static PerMessageDeflateMessageResult FinishOutgoing(
        PerMessageDeflateMessageState state,
        int outputChunkSize)
    {
        var ignored = new List<byte>();
        return FinishOutgoingTo(state, outputChunkSize, ignored);
    }

    static PerMessageDeflateMessageResult FinishOutgoingTo(
        PerMessageDeflateMessageState state,
        int outputChunkSize,
        List<byte> output)
    {
        PerMessageDeflateMessageResult result =
            new PerMessageDeflateMessageResult();
        result.Status = WebSocketParseStatus.NeedOutputCapacity;
        int iteration = 0;
        while (result.Status == WebSocketParseStatus.NeedOutputCapacity &&
            iteration < 100000)
        {
            var destination = new byte[outputChunkSize];
            result = state.FinishOutgoingFrame(destination);
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        Assert.That(iteration, Is.LessThan(100000));
        return result;
    }

    static List<byte> TransformIncoming(
        PerMessageDeflateMessageState state,
        byte[] source,
        int outputChunkSize)
    {
        var output = new List<byte>();
        TransformIncomingTo(state, source, outputChunkSize, output);
        return output;
    }

    static PerMessageDeflateMessageResult TransformIncomingResult(
        PerMessageDeflateMessageState state,
        byte[] source,
        int outputChunkSize)
    {
        var output = new List<byte>();
        return TransformIncomingTo(
            state,
            source,
            outputChunkSize,
            output);
    }

    static PerMessageDeflateMessageResult TransformIncomingTo(
        PerMessageDeflateMessageState state,
        byte[] source,
        int outputChunkSize,
        List<byte> output)
    {
        PerMessageDeflateMessageResult result =
            new PerMessageDeflateMessageResult();
        int sourceIndex = 0;
        int iteration = 0;
        while ((sourceIndex < source.Length ||
                result.Status == WebSocketParseStatus.NeedOutputCapacity) &&
            result.Status != WebSocketParseStatus.ProtocolFailure &&
            result.Status != WebSocketParseStatus.LimitRejected &&
            iteration < 100000)
        {
            var destination = new byte[outputChunkSize];
            result = state.TransformIncomingPayload(
                source.AsSpan(sourceIndex),
                destination);
            sourceIndex = sourceIndex + result.ConsumedCount;
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        Assert.That(iteration, Is.LessThan(100000));
        return result;
    }

    static PerMessageDeflateMessageResult FinishIncoming(
        PerMessageDeflateMessageState state,
        int outputChunkSize)
    {
        var ignored = new List<byte>();
        return FinishIncomingTo(state, outputChunkSize, ignored);
    }

    static PerMessageDeflateMessageResult FinishIncomingTo(
        PerMessageDeflateMessageState state,
        int outputChunkSize,
        List<byte> output)
    {
        PerMessageDeflateMessageResult result =
            new PerMessageDeflateMessageResult();
        result.Status = WebSocketParseStatus.NeedOutputCapacity;
        int iteration = 0;
        while (result.Status == WebSocketParseStatus.NeedOutputCapacity &&
            iteration < 100000)
        {
            var destination = new byte[outputChunkSize];
            result = state.FinishIncomingFrame(destination);
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        Assert.That(iteration, Is.LessThan(100000));
        return result;
    }

    static byte[] CompressPayload(byte[] source, int windowBits)
    {
        var settings = new PerMessageDeflateDirectionSettings();
        settings.Valid = true;
        settings.OutgoingWindowBits = windowBits;
        settings.IncomingWindowBits = windowBits;
        var codec = new PerMessageDeflateCodec(settings, source.Length + 1);
        var output = new List<byte>();
        var destination = new byte[source.Length + 100];
        PerMessageDeflateCodecResult transformed =
            codec.EncodeCompressedBytes(source, destination);
        AppendProduced(output, destination, transformed.ProducedCount);
        PerMessageDeflateCodecResult finished =
            new PerMessageDeflateCodecResult();
        finished.Status = WebSocketParseStatus.NeedOutputCapacity;
        while (finished.Status == WebSocketParseStatus.NeedOutputCapacity)
        {
            destination = new byte[source.Length + 100];
            finished = codec.FinishCompressedBytes(destination);
            AppendProduced(output, destination, finished.ProducedCount);
        }

        return output.ToArray();
    }

    static byte[] DecompressPayload(
        byte[] source,
        int outputLength,
        int windowBits)
    {
        var settings = new PerMessageDeflateDirectionSettings();
        settings.Valid = true;
        settings.OutgoingWindowBits = windowBits;
        settings.IncomingWindowBits = windowBits;
        var codec = new PerMessageDeflateCodec(settings, outputLength);
        var output = new List<byte>();
        var destination = new byte[outputLength + 1];
        PerMessageDeflateCodecResult transformed =
            codec.DecodeCompressedBytes(source, destination);
        AppendProduced(output, destination, transformed.ProducedCount);
        PerMessageDeflateCodecResult finished =
            codec.FinishDecompressedBytes(destination);
        AppendProduced(output, destination, finished.ProducedCount);
        return output.ToArray();
    }

    static void AppendProduced(
        List<byte> output,
        byte[] source,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            output.Add(source[index]);
            index++;
        }
    }

    static byte[] RandomBytes(int count, int seed)
    {
        var output = new byte[count];
        new Random(seed).NextBytes(output);
        return output;
    }
}
