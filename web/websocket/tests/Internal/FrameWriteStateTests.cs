using websocket;

namespace websocket.Tests;

public class FrameWriteStateTests
{
    [TestCase(0L, false, 2, 2)]
    [TestCase(125L, false, 2, 127)]
    [TestCase(126L, false, 4, 130)]
    [TestCase(65535L, false, 4, 65539)]
    [TestCase(65536L, false, 10, 65546)]
    [TestCase(0L, true, 6, 6)]
    [TestCase(126L, true, 8, 134)]
    [TestCase(65536L, true, 14, 65550)]
    public void MeasurementUsesCanonicalHeaderSizes(
        long payloadLength,
        bool masked,
        int expectedHeaderLength,
        int expectedEncodedLength)
    {
        var source = new FrameTestMaskKeySource();
        var request = CreateRequest(payloadLength, masked, [], 0, 0, 0, source);

        var result = FrameWriteState.MeasureFrame(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.HeaderLength, Is.EqualTo(expectedHeaderLength));
            Assert.That(result.EncodedLength, Is.EqualTo(expectedEncodedLength));
            Assert.That(result.HasError, Is.False);
            Assert.That(source.CallCount, Is.Zero);
        });
    }

    [TestCase(0L, "8200")]
    [TestCase(125L, "827d")]
    [TestCase(126L, "827e007e")]
    [TestCase(65535L, "827effff")]
    [TestCase(65536L, "827f0000000000010000")]
    public void WritingUsesCanonicalNetworkOrderLengthForms(long payloadLength, string expectedHeader)
    {
        var state = new FrameWriteState();
        var request = CreateRequest(payloadLength, false, [], 0, 0, 0, new FrameTestMaskKeySource());
        var destination = new byte[WebSocketProtocolConstants.MaximumFrameHeaderLength];

        var result = state.WriteFrame(request, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.ProducedCount, Is.EqualTo(expectedHeader.Length / 2));
            Assert.That(destination.AsSpan(0, result.ProducedCount).ToArray(), Is.EqualTo(Convert.FromHexString(expectedHeader)));
            Assert.That(result.Status, Is.EqualTo(payloadLength == 0 ? WebSocketParseStatus.Complete : WebSocketParseStatus.NeedInput));
        });
    }

    [Test]
    public void HeaderEncodesEveryFlagAndOpcodeBit()
    {
        var state = new FrameWriteState();
        var request = CreateRequest(0, false, [], 0, 0, 0, new FrameTestMaskKeySource());
        request.Header.Final = true;
        request.Header.Rsv1 = true;
        request.Header.Rsv2 = true;
        request.Header.Rsv3 = true;
        request.Header.Opcode = WebSocketOpcode.ReservedControl15;
        var destination = new byte[2];

        var result = state.WriteFrame(request, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(destination, Is.EqualTo(new byte[] { 0xff, 0x00 }));
        });
    }

    [Test]
    public void RfcExampleWritesOneFreshMaskAndMaskedPayload()
    {
        var state = new FrameWriteState();
        var source = new FrameTestMaskKeySource();
        source.NextKey = 0x37fa213dU;
        byte[] payload = [0xaa, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0xbb];
        var request = CreateRequest(5, true, payload, 1, 5, 0, source);
        request.Header.Final = true;
        request.Header.Opcode = WebSocketOpcode.Text;
        var destination = new byte[11];

        var result = state.WriteFrame(request, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(5));
            Assert.That(result.ProducedCount, Is.EqualTo(11));
            Assert.That(result.EndOfFrame, Is.True);
            Assert.That(result.HasMaskingKey, Is.True);
            Assert.That(result.MaskingKey, Is.EqualTo(0x37fa213dU));
            Assert.That(destination, Is.EqualTo(Convert.FromHexString("818537fa213d7f9f4d5158")));
            Assert.That(payload, Is.EqualTo(new byte[] { 0xaa, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0xbb }));
            Assert.That(source.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void PartialDestinationsReportHeaderAndPayloadProgressPrecisely()
    {
        var state = new FrameWriteState();
        var source = new FrameTestMaskKeySource();
        source.NextKey = 0x37fa213dU;
        byte[] payload = [0x48, 0x65, 0x6c, 0x6c, 0x6f];
        var request = CreateRequest(5, true, payload, 0, 5, 0, source);
        request.Header.Final = true;
        request.Header.Opcode = WebSocketOpcode.Text;
        var firstDestination = new byte[3];
        var secondDestination = new byte[5];
        var thirdDestination = Enumerable.Repeat((byte)0xaa, 4).ToArray();

        var first = state.WriteFrame(request, firstDestination);
        var second = state.WriteFrame(request, secondDestination);
        request.PayloadOffset = request.PayloadOffset + second.ConsumedCount;
        request.PayloadCount = request.PayloadCount - second.ConsumedCount;
        request.MaskingOffset = request.MaskingOffset + second.ConsumedCount;
        var third = state.WriteFrame(request, thirdDestination);
        var combined = firstDestination
            .Concat(secondDestination)
            .Concat(thirdDestination.Take(third.ProducedCount))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(first.ConsumedCount, Is.Zero);
            Assert.That(first.ProducedCount, Is.EqualTo(3));
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(second.ConsumedCount, Is.EqualTo(2));
            Assert.That(second.ProducedCount, Is.EqualTo(5));
            Assert.That(third.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(third.ConsumedCount, Is.EqualTo(3));
            Assert.That(third.ProducedCount, Is.EqualTo(3));
            Assert.That(thirdDestination[3], Is.EqualTo(0xaa));
            Assert.That(combined, Is.EqualTo(Convert.FromHexString("818537fa213d7f9f4d5158")));
            Assert.That(source.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SeparatePayloadChunksContinueTheMaskCycle()
    {
        var state = new FrameWriteState();
        var source = new FrameTestMaskKeySource();
        source.NextKey = 0x37fa213dU;
        byte[] payload = [0x48, 0x65, 0x6c, 0x6c, 0x6f];
        var firstRequest = CreateRequest(5, true, payload, 0, 2, 0, source);
        firstRequest.Header.Final = true;
        firstRequest.Header.Opcode = WebSocketOpcode.Text;
        var firstDestination = new byte[16];

        var first = state.WriteFrame(firstRequest, firstDestination);
        var secondRequest = CreateRequest(5, true, payload, 2, 3, 2, source);
        secondRequest.Header.Final = true;
        secondRequest.Header.Opcode = WebSocketOpcode.Text;
        var secondDestination = new byte[3];
        var second = state.WriteFrame(secondRequest, secondDestination);
        var combined = firstDestination.AsSpan(0, first.ProducedCount).ToArray()
            .Concat(secondDestination)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(first.ConsumedCount, Is.EqualTo(2));
            Assert.That(first.ProducedCount, Is.EqualTo(8));
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.ConsumedCount, Is.EqualTo(3));
            Assert.That(combined, Is.EqualTo(Convert.FromHexString("818537fa213d7f9f4d5158")));
            Assert.That(source.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ZeroDestinationStartsFrameWithoutProducingOrRefreshingMask()
    {
        var state = new FrameWriteState();
        var source = new FrameTestMaskKeySource();
        var request = CreateRequest(0, true, [], 0, 0, 0, source);
        var destination = new byte[6];

        var empty = state.WriteFrame(request, []);
        var complete = state.WriteFrame(request, destination);
        var repeated = state.WriteFrame(request, destination);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(empty.ProducedCount, Is.Zero);
            Assert.That(complete.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(complete.ProducedCount, Is.EqualTo(6));
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated.ProducedCount, Is.Zero);
            Assert.That(source.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ResetAcquiresANewKeyForTheNextFrame()
    {
        var state = new FrameWriteState();
        var source = new FrameTestMaskKeySource();
        var request = CreateRequest(0, true, [], 0, 0, 0, source);
        var firstDestination = new byte[6];
        var secondDestination = new byte[6];

        state.WriteFrame(request, firstDestination);
        state.ResetFrame();
        state.WriteFrame(request, secondDestination);

        Assert.Multiple(() =>
        {
            Assert.That(source.CallCount, Is.EqualTo(2));
            Assert.That(firstDestination.AsSpan(2, 4).ToArray(), Is.Not.EqualTo(secondDestination.AsSpan(2, 4).ToArray()));
        });
    }

    [Test]
    public void InvalidContinuationWritesNothingAndCanBeCorrected()
    {
        var state = new FrameWriteState();
        byte[] payload = [1, 2, 3];
        var firstRequest = CreateRequest(3, false, payload, 0, 1, 0, new FrameTestMaskKeySource());
        var firstDestination = new byte[3];
        var first = state.WriteFrame(firstRequest, firstDestination);
        var invalidRequest = CreateRequest(3, false, payload, 1, 2, 0, new FrameTestMaskKeySource());
        var rejectedDestination = Enumerable.Repeat((byte)0xaa, 2).ToArray();

        var invalid = state.WriteFrame(invalidRequest, rejectedDestination);
        invalidRequest.MaskingOffset = 1;
        var corrected = state.WriteFrame(invalidRequest, rejectedDestination);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(invalid.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalid.ConsumedCount, Is.Zero);
            Assert.That(invalid.ProducedCount, Is.Zero);
            Assert.That(invalid.Error.Identifier, Is.EqualTo("frame-write-continuation-invalid"));
            Assert.That(corrected.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(corrected.ConsumedCount, Is.EqualTo(2));
            Assert.That(rejectedDestination, Is.EqualTo(new byte[] { 2, 3 }));
        });
    }

    [Test]
    public void InvalidRequestsAndSizeOverflowAreReportedWithoutOutput()
    {
        var invalidRange = CreateRequest(1, false, [], 0, 1, 0, new FrameTestMaskKeySource());
        var missingMaskSource = CreateRequest(0, true, [], 0, 0, 0, new FrameTestMaskKeySource());
        missingMaskSource.HasMaskKeySource = false;
        missingMaskSource.MaskKeySource = null;
        var overflow = CreateRequest(int.MaxValue, false, [], 0, 0, 0, new FrameTestMaskKeySource());
        var destination = Enumerable.Repeat((byte)0xaa, 8).ToArray();
        var state = new FrameWriteState();

        var rangeResult = FrameWriteState.MeasureFrame(invalidRange);
        var sourceResult = state.WriteFrame(missingMaskSource, destination);
        var overflowResult = FrameWriteState.MeasureFrame(overflow);

        Assert.Multiple(() =>
        {
            Assert.That(rangeResult.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(rangeResult.Error.Identifier, Is.EqualTo("frame-write-range-invalid"));
            Assert.That(sourceResult.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(sourceResult.ProducedCount, Is.Zero);
            Assert.That(sourceResult.Error.Identifier, Is.EqualTo("frame-write-mask-source-required"));
            Assert.That(overflowResult.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(overflowResult.Error.Identifier, Is.EqualTo("frame-write-size-overflow"));
            Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xaa, 8)));
        });
    }

    [Test]
    public void FirstChunkMustStartAtMaskingOffsetZero()
    {
        var state = new FrameWriteState();
        byte[] payload = [1, 2, 3];
        var request = CreateRequest(3, false, payload, 1, 2, 1, new FrameTestMaskKeySource());
        var destination = Enumerable.Repeat((byte)0xaa, 8).ToArray();

        var result = state.WriteFrame(request, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(result.Error.Identifier, Is.EqualTo("frame-write-offset-invalid"));
            Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xaa, 8)));
        });
    }

    static WebSocketFrameWriteRequest CreateRequest(
        long payloadLength,
        bool masked,
        byte[] payload,
        int payloadOffset,
        int payloadCount,
        int maskingOffset,
        FrameTestMaskKeySource source)
    {
        var request = new WebSocketFrameWriteRequest();
        request.Header.Final = true;
        request.Header.Opcode = WebSocketOpcode.Binary;
        request.Header.Masked = masked;
        request.Header.PayloadLength = payloadLength;
        request.Payload = payload;
        request.PayloadOffset = payloadOffset;
        request.PayloadCount = payloadCount;
        request.MaskingOffset = maskingOffset;
        if (masked)
        {
            request.HasMaskKeySource = true;
            request.MaskKeySource = source;
        }

        return request;
    }
}

internal class FrameTestMaskKeySource : IWebSocketMaskKeySource
{
    internal int CallCount;
    internal uint NextKey = 0x11223344U;

    public uint CreateMaskingKey()
    {
        uint result = NextKey;
        NextKey = NextKey + 0x01010101U;
        CallCount = CallCount + 1;
        return result;
    }
}
