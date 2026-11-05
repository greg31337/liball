namespace dns.Tests.Internal.Wire;

public class DnsTcpFrameCodecTests
{
    [Test]
    public void CompleteFrameRoundTripsWithExactLengthPrefix()
    {
        byte[] message = Enumerable.Range(1, 12)
            .Select(value => (byte)value)
            .ToArray();
        DnsTcpFrame frame = CompleteFrame(message);

        DnsEncodeResult encoded = DnsTcpFrameCodec.EncodeTcpFrame(
            frame, new DnsEncodeOptions());
        DnsDecodeResult decoded = DnsTcpFrameCodec.DecodeTcpFrame(
            encoded.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.BytesWritten, Is.EqualTo(14));
            Assert.That(encoded.Bytes[0], Is.EqualTo(0));
            Assert.That(encoded.Bytes[1], Is.EqualTo(12));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.BytesConsumed, Is.EqualTo(14));
            Assert.That(decoded.TcpFrame!.MessageLength, Is.EqualTo(12));
            Assert.That(decoded.TcpFrame.MessageBytes, Is.EqualTo(message));
            Assert.That(decoded.TcpFrame.Complete, Is.True);
            Assert.That(decoded.TrailingBytes, Is.Empty);
        });
    }

    [Test]
    public void EncoderCanDeriveAnUnsetMessageLength()
    {
        DnsTcpFrame frame = CompleteFrame([1, 2, 3]);
        frame.MessageLength = 0;

        DnsEncodeResult result = DnsTcpFrameCodec.EncodeTcpFrame(
            frame, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes,
                Is.EqualTo(new byte[] { 0, 3, 1, 2, 3 }));
            Assert.That(frame.MessageLength, Is.EqualTo(0));
        });
    }

    [Test]
    public void MaximumProtocolLengthRoundTrips()
    {
        byte[] message = new byte[DnsProtocolLimits.MaximumTcpMessageLength];
        message[0] = 1;
        message[message.Length - 1] = 2;

        DnsEncodeResult encoded = DnsTcpFrameCodec.EncodeTcpFrame(
            CompleteFrame(message), new DnsEncodeOptions());
        DnsDecodeResult decoded = DnsTcpFrameCodec.DecodeTcpFrame(
            encoded.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes[0], Is.EqualTo(255));
            Assert.That(encoded.Bytes[1], Is.EqualTo(255));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.TcpFrame!.MessageLength,
                Is.EqualTo(DnsProtocolLimits.MaximumTcpMessageLength));
            Assert.That(decoded.TcpFrame.MessageBytes[0], Is.EqualTo(1));
            Assert.That(decoded.TcpFrame.MessageBytes[^1], Is.EqualTo(2));
        });
    }

    [Test]
    public void PartialPrefixFailsButPartialPayloadReturnsIncompleteFrame()
    {
        DnsDecodeResult prefix = DnsTcpFrameCodec.DecodeTcpFrame(
            [0], new DnsDecodeOptions());
        DnsDecodeResult payload = DnsTcpFrameCodec.DecodeTcpFrame(
            [0, 5, 1, 2, 3], new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(prefix.Success, Is.False);
            Assert.That(prefix.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(prefix.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(payload.Success, Is.True);
            Assert.That(payload.BytesConsumed, Is.EqualTo(5));
            Assert.That(payload.TcpFrame!.MessageLength, Is.EqualTo(5));
            Assert.That(payload.TcpFrame.MessageBytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(payload.TcpFrame.Complete, Is.False);
        });
    }

    [Test]
    public void CompleteFrameTrailingPolicySupportsPersistentStreams()
    {
        byte[] first = EncodeFrame([1, 2, 3]);
        byte[] second = EncodeFrame([4, 5]);
        byte[] combined = first.Concat(second).ToArray();
        DnsDecodeOptions permissive = new DnsDecodeOptions();
        permissive.RequireCompleteInput = false;

        DnsDecodeResult strict = DnsTcpFrameCodec.DecodeTcpFrame(
            combined, new DnsDecodeOptions());
        DnsDecodeResult allowed = DnsTcpFrameCodec.DecodeTcpFrame(
            combined, permissive);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.TrailingData));
            Assert.That(strict.Diagnostics[0].Offset, Is.EqualTo(first.Length));
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.BytesConsumed, Is.EqualTo(first.Length));
            Assert.That(allowed.TcpFrame!.MessageBytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(allowed.TrailingBytes, Is.EqualTo(second));
        });
    }

    [Test]
    public void ZeroOversizeIncompleteAndMismatchedFramesAreRejected()
    {
        DnsDecodeOptions limitedDecode = new DnsDecodeOptions();
        limitedDecode.MaximumMessageLength = 3;
        DnsEncodeOptions limitedEncode = new DnsEncodeOptions();
        limitedEncode.MaximumMessageLength = 3;
        DnsTcpFrame incomplete = CompleteFrame([1]);
        incomplete.Complete = false;
        DnsTcpFrame mismatch = CompleteFrame([1, 2]);
        mismatch.MessageLength = 3;

        DnsDecodeResult zero = DnsTcpFrameCodec.DecodeTcpFrame(
            [0, 0], new DnsDecodeOptions());
        DnsDecodeResult oversized = DnsTcpFrameCodec.DecodeTcpFrame(
            [0, 4], limitedDecode);
        DnsEncodeResult encodeOversized = DnsTcpFrameCodec.EncodeTcpFrame(
            CompleteFrame([1, 2, 3, 4]), limitedEncode);
        DnsEncodeResult encodeIncomplete = DnsTcpFrameCodec.EncodeTcpFrame(
            incomplete, new DnsEncodeOptions());
        DnsEncodeResult encodeMismatch = DnsTcpFrameCodec.EncodeTcpFrame(
            mismatch, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(zero.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTcpFrame));
            Assert.That(oversized.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(encodeOversized.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(encodeIncomplete.Diagnostics[0].Field,
                Is.EqualTo("tcpFrame.complete"));
            Assert.That(encodeMismatch.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTcpFrame));
        });
    }

    [Test]
    public void AccumulatorCombinesSplitPrefixAndPayloadReads()
    {
        DnsTcpFrameAccumulator accumulator = new DnsTcpFrameAccumulator();
        DnsDecodeOptions options = new DnsDecodeOptions();

        TcpFrameAccumulationResult prefix = accumulator.AppendBytes(
            [0], options);
        TcpFrameAccumulationResult payloadStart = accumulator.AppendBytes(
            [3, 1], options);
        TcpFrameAccumulationResult completed = accumulator.AppendBytes(
            [2, 3], options);

        Assert.Multiple(() =>
        {
            Assert.That(prefix.Success, Is.True);
            Assert.That(prefix.Frames, Is.Empty);
            Assert.That(prefix.BufferedByteCount, Is.EqualTo(1));
            Assert.That(prefix.PartialFrame, Is.Null);
            Assert.That(payloadStart.Success, Is.True);
            Assert.That(payloadStart.BufferedByteCount, Is.EqualTo(3));
            Assert.That(payloadStart.PartialFrame!.MessageLength, Is.EqualTo(3));
            Assert.That(payloadStart.PartialFrame.MessageBytes,
                Is.EqualTo(new byte[] { 1 }));
            Assert.That(payloadStart.PartialFrame.Complete, Is.False);
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Frames, Has.Count.EqualTo(1));
            Assert.That(completed.Frames[0].MessageBytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(completed.BufferedByteCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void AccumulatorEmitsMultipleFramesAndRetainsNextPartialFrame()
    {
        byte[] first = EncodeFrame([1]);
        byte[] second = EncodeFrame([2, 3]);
        byte[] third = EncodeFrame([4, 5, 6, 7]);
        byte[] initial = first
            .Concat(second)
            .Concat(third.Take(4))
            .ToArray();
        DnsTcpFrameAccumulator accumulator = new DnsTcpFrameAccumulator();

        TcpFrameAccumulationResult multiple = accumulator.AppendBytes(
            initial, new DnsDecodeOptions());
        TcpFrameAccumulationResult final = accumulator.AppendBytes(
            third.Skip(4).ToArray(), new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(multiple.Success, Is.True);
            Assert.That(multiple.Frames, Has.Count.EqualTo(2));
            Assert.That(multiple.Frames[0].MessageBytes,
                Is.EqualTo(new byte[] { 1 }));
            Assert.That(multiple.Frames[1].MessageBytes,
                Is.EqualTo(new byte[] { 2, 3 }));
            Assert.That(multiple.PartialFrame!.MessageLength, Is.EqualTo(4));
            Assert.That(multiple.PartialFrame.MessageBytes,
                Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(final.Frames, Has.Count.EqualTo(1));
            Assert.That(final.Frames[0].MessageBytes,
                Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(final.BufferedByteCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void AccumulatorEmitsMultipleFramesFromSingleOctetReads()
    {
        byte[] stream = EncodeFrame([1, 2, 3])
            .Concat(EncodeFrame([4]))
            .Concat(EncodeFrame([5, 6]))
            .ToArray();
        DnsTcpFrameAccumulator accumulator = new DnsTcpFrameAccumulator();
        List<byte[]> messages = new List<byte[]>();

        foreach (byte value in stream)
        {
            TcpFrameAccumulationResult result = accumulator.AppendBytes(
                [value], new DnsDecodeOptions());
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesAccepted, Is.EqualTo(1));
            foreach (DnsTcpFrame frame in result.Frames)
            {
                messages.Add(frame.MessageBytes);
            }
        }

        TcpFrameAccumulationResult empty = accumulator.AppendBytes(
            [], new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(messages, Is.EqualTo(new byte[][]
            {
                new byte[] { 1, 2, 3 },
                new byte[] { 4 },
                new byte[] { 5, 6 }
            }));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.BufferedByteCount, Is.Zero);
            Assert.That(empty.PartialFrame, Is.Null);
        });
    }

    [Test]
    public void AccumulatorRejectsInvalidLengthsAndCanRecoverAfterReset()
    {
        DnsTcpFrameAccumulator accumulator = new DnsTcpFrameAccumulator();
        DnsDecodeOptions options = new DnsDecodeOptions();
        DnsDecodeOptions limited = new DnsDecodeOptions();
        limited.MaximumMessageLength = 3;

        TcpFrameAccumulationResult zero = accumulator.AppendBytes(
            [0, 0], options);
        TcpFrameAccumulationResult recovered = accumulator.AppendBytes(
            EncodeFrame([1]), options);
        accumulator.ResetAccumulator();
        TcpFrameAccumulationResult oversized = accumulator.AppendBytes(
            [0, 4], limited);

        Assert.Multiple(() =>
        {
            Assert.That(zero.Success, Is.False);
            Assert.That(zero.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTcpFrame));
            Assert.That(zero.BufferedByteCount, Is.EqualTo(0));
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.Frames, Has.Count.EqualTo(1));
            Assert.That(oversized.Success, Is.False);
            Assert.That(oversized.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(oversized.BufferedByteCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void FramedDnsMessageRoundTripsThroughBothCodecs()
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 4660;
        DnsEncodeResult messageBytes = DnsMessageEncoder.EncodeMessage(
            message, new DnsEncodeOptions());
        DnsEncodeResult framed = DnsTcpFrameCodec.EncodeTcpFrame(
            CompleteFrame(messageBytes.Bytes), new DnsEncodeOptions());
        DnsDecodeResult frame = DnsTcpFrameCodec.DecodeTcpFrame(
            framed.Bytes, new DnsDecodeOptions());
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            frame.TcpFrame!.MessageBytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(framed.Success, Is.True);
            Assert.That(frame.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message!.Header.Identifier, Is.EqualTo(4660));
        });
    }

    static DnsTcpFrame CompleteFrame(byte[] message)
    {
        DnsTcpFrame frame = new DnsTcpFrame();
        frame.MessageLength = message.Length;
        frame.MessageBytes = message;
        frame.Complete = true;
        return frame;
    }

    static byte[] EncodeFrame(byte[] message)
    {
        DnsEncodeResult result = DnsTcpFrameCodec.EncodeTcpFrame(
            CompleteFrame(message), new DnsEncodeOptions());
        Assert.That(result.Success, Is.True);
        return result.Bytes;
    }
}
