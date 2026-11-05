using rtp;

namespace rtp.Tests.Wire;

public class RtpStreamFramingCodecTests
{
    [Test]
    public void EncodeWritesAnExactNetworkOrderFrameAndPreservesExtraCapacity()
    {
        IRtpStreamFramingCodec codec = new RtpStreamFramingCodec();
        byte[] datagram = new byte[] { 0x80, 96, 1, 2, 3 };
        byte[] destination = new byte[] { 77, 77, 77, 77, 77, 77, 77, 77 };

        int encodedSize = codec.GetRtpStreamFrameEncodedSize(datagram);
        RtpCodecResult result = codec.TryEncodeRtpStreamFrame(datagram, destination);
        byte[] decoded = codec.DecodeRtpStreamFrame(destination[..encodedSize]);

        Assert.Multiple(() =>
        {
            Assert.That(encodedSize, Is.EqualTo(7));
            Assert.That(result.Success, Is.True);
            Assert.That(result.WrittenCount, Is.EqualTo(7));
            Assert.That(destination,
                Is.EqualTo(new byte[] { 0, 5, 0x80, 96, 1, 2, 3, 77 }));
            Assert.That(decoded, Is.EqualTo(datagram));
            Assert.That(codec.EncodeRtpStreamFrame(datagram, new byte[7]),
                Is.EqualTo(7));
        });
    }

    [Test]
    public void IncrementalDecodeWaitsForACompletePrefixAndPayload()
    {
        IRtpStreamFramingCodec codec = new RtpStreamFramingCodec();
        byte[] frame = new byte[] { 0, 4, 0x80, 96, 1, 2 };

        RtpCodecResult empty = codec.TryDecodeRtpStreamFrame(frame[..0]);
        RtpCodecResult prefix = codec.TryDecodeRtpStreamFrame(frame[..1]);
        RtpCodecResult payload = codec.TryDecodeRtpStreamFrame(frame[..5]);
        RtpCodecResult complete = codec.TryDecodeRtpStreamFrame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(prefix.Success, Is.False);
            Assert.That(prefix.ConsumedCount, Is.Zero);
            Assert.That(payload.Success, Is.False);
            Assert.That(payload.ConsumedCount, Is.Zero);
            Assert.That(payload.Data, Is.Empty);
            Assert.That(complete.Success, Is.True);
            Assert.That(complete.ConsumedCount, Is.EqualTo(6));
            Assert.That(complete.Data, Is.EqualTo(new byte[] { 0x80, 96, 1, 2 }));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.DecodeRtpStreamFrame(frame[..5]));
        });
    }

    [Test]
    public void ConcatenatedRtpAndRtcpFramesCanBeConsumedOneAtATime()
    {
        IRtpStreamFramingCodec codec = new RtpStreamFramingCodec();
        byte[] source = new byte[]
        {
            0, 4, 0x80, 96, 1, 2,
            0, 8, 0x80, 201, 0, 1, 0, 0, 0, 7
        };

        RtpCodecResult first = codec.TryDecodeRtpStreamFrame(source);
        RtpCodecResult second = codec.TryDecodeRtpStreamFrame(
            source.AsSpan(first.ConsumedCount));
        source[2] = 0;
        source[8] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.ConsumedCount, Is.EqualTo(6));
            Assert.That(first.Data, Is.EqualTo(new byte[] { 0x80, 96, 1, 2 }));
            Assert.That(second.Success, Is.True);
            Assert.That(second.ConsumedCount, Is.EqualTo(10));
            Assert.That(second.Data,
                Is.EqualTo(new byte[] { 0x80, 201, 0, 1, 0, 0, 0, 7 }));
        });
    }

    [Test]
    public void EveryPartialFrameAtLengthBoundariesWaitsWithoutConsumption()
    {
        IRtpStreamFramingCodec codec = new RtpStreamFramingCodec();
        int[] datagramLengths = new int[] { 0, 1, 255, 256, 257, 4096 };
        int lengthIndex = 0;
        while (lengthIndex < datagramLengths.Length)
        {
            byte[] datagram = CreatePattern(datagramLengths[lengthIndex]);
            byte[] frame = EncodeFrame(codec, datagram);
            int availableLength = 0;
            while (availableLength < frame.Length)
            {
                RtpCodecResult partial = codec.TryDecodeRtpStreamFrame(
                    frame.AsSpan(0, availableLength));

                Assert.Multiple(() =>
                {
                    Assert.That(partial.Success, Is.False);
                    Assert.That(partial.ConsumedCount, Is.Zero);
                    Assert.That(partial.Data, Is.Empty);
                });
                availableLength = availableLength + 1;
            }

            byte[] withTail = new byte[frame.Length + 3];
            frame.CopyTo(withTail, 0);
            Array.Fill(withTail, (byte)0xA5, frame.Length, 3);
            RtpCodecResult complete = codec.TryDecodeRtpStreamFrame(withTail);

            Assert.Multiple(() =>
            {
                Assert.That(frame[0],
                    Is.EqualTo((byte)(datagramLengths[lengthIndex] >> 8)));
                Assert.That(frame[1],
                    Is.EqualTo((byte)datagramLengths[lengthIndex]));
                Assert.That(complete.Success, Is.True);
                Assert.That(complete.ConsumedCount, Is.EqualTo(frame.Length));
                Assert.That(complete.Data, Is.EqualTo(datagram));
            });
            lengthIndex = lengthIndex + 1;
        }
    }

    [Test]
    public void NullRtpAndRtcpFramesAreConsumedIncrementallyAndRemainOwned()
    {
        IRtpStreamFramingCodec framingCodec = new RtpStreamFramingCodec();
        byte[] rtpDatagram = new byte[]
        {
            0x80, 96, 0x12, 0x34,
            1, 2, 3, 4,
            0x11, 0x22, 0x33, 0x44,
            0x55
        };
        byte[] rtcpDatagram = new byte[]
        {
            0x80, 201, 0, 1,
            0x55, 0x66, 0x77, 0x88
        };
        byte[][] datagrams = new byte[][]
        {
            [],
            rtpDatagram,
            [],
            rtcpDatagram
        };
        byte[][] frames = new byte[datagrams.Length][];
        int sourceLength = 0;
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            frames[frameIndex] = EncodeFrame(framingCodec, datagrams[frameIndex]);
            sourceLength = sourceLength + frames[frameIndex].Length;
            frameIndex = frameIndex + 1;
        }

        byte[] source = new byte[sourceLength];
        int offset = 0;
        frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            frames[frameIndex].CopyTo(source, offset);
            offset = offset + frames[frameIndex].Length;
            frameIndex = frameIndex + 1;
        }

        RtpCodecResult[] results = new RtpCodecResult[frames.Length];
        offset = 0;
        frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            results[frameIndex] = framingCodec.TryDecodeRtpStreamFrame(
                source.AsSpan(offset));
            offset = offset + results[frameIndex].ConsumedCount;
            frameIndex = frameIndex + 1;
        }

        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        IRtpPacketCodec rtpCodec = new RtpPacketCodec(configuration);
        IRtcpPacketCodec rtcpCodec = new RtcpPacketCodec(configuration);
        RtpPacket rtpPacket = rtpCodec.DecodeRtpPacket(results[1].Data);
        RtcpPacket rtcpPacket = rtcpCodec.DecodeRtcpPacket(results[3].Data);
        Array.Fill(source, (byte)0);

        Assert.Multiple(() =>
        {
            Assert.That(offset, Is.EqualTo(sourceLength));
            Assert.That(results[0].Success, Is.True);
            Assert.That(results[0].ConsumedCount, Is.EqualTo(2));
            Assert.That(results[0].Data, Is.Empty);
            Assert.That(results[1].ConsumedCount, Is.EqualTo(15));
            Assert.That(results[1].Data, Is.EqualTo(rtpDatagram));
            Assert.That(results[2].Success, Is.True);
            Assert.That(results[2].ConsumedCount, Is.EqualTo(2));
            Assert.That(results[2].Data, Is.Empty);
            Assert.That(results[3].ConsumedCount, Is.EqualTo(10));
            Assert.That(results[3].Data, Is.EqualTo(rtcpDatagram));
            Assert.That(rtpPacket.SequenceNumber, Is.EqualTo(0x1234));
            Assert.That(rtpPacket.Timestamp, Is.EqualTo(0x01020304U));
            Assert.That(rtpPacket.SynchronizationSource, Is.EqualTo(0x11223344U));
            Assert.That(rtpPacket.Payload, Is.EqualTo(new byte[] { 0x55 }));
            Assert.That(rtcpPacket.Type, Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(rtcpPacket.ReceiverReport?.SenderSynchronizationSource,
                Is.EqualTo(0x55667788U));
        });
    }

    [Test]
    public void NullFramesRoundTripAndOversizedPayloadsFailWithoutWriting()
    {
        IRtpStreamFramingCodec codec = new RtpStreamFramingCodec();
        byte[] nullDestination = new byte[] { 77, 77, 77 };
        byte[] oversizedDestination = new byte[] { 77, 77, 77 };
        byte[] oversized = new byte[
            RtpProtocolConstants.MaximumStreamFramePayloadSize + 1];

        RtpCodecResult nullEncode = codec.TryEncodeRtpStreamFrame(
            ReadOnlySpan<byte>.Empty,
            nullDestination);
        RtpCodecResult nullDecode = codec.TryDecodeRtpStreamFrame(
            new byte[] { 0, 0 });
        RtpCodecResult oversizedEncode = codec.TryEncodeRtpStreamFrame(
            oversized,
            oversizedDestination);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetRtpStreamFrameEncodedSize(
                ReadOnlySpan<byte>.Empty), Is.EqualTo(2));
            Assert.That(nullEncode.Success, Is.True);
            Assert.That(nullEncode.WrittenCount, Is.EqualTo(2));
            Assert.That(nullDestination, Is.EqualTo(new byte[] { 0, 0, 77 }));
            Assert.That(nullDecode.Success, Is.True);
            Assert.That(nullDecode.ConsumedCount, Is.EqualTo(2));
            Assert.That(nullDecode.Data, Is.Empty);
            Assert.That(codec.DecodeRtpStreamFrame(new byte[] { 0, 0 }), Is.Empty);
            Assert.That(oversizedEncode.Success, Is.False);
            Assert.That(oversizedEncode.WrittenCount, Is.Zero);
            Assert.That(oversizedDestination, Has.All.EqualTo(77));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpStreamFrameEncodedSize(oversized));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.EncodeRtpStreamFrame(oversized, oversizedDestination));
            Assert.That(oversizedDestination, Has.All.EqualTo(77));
        });
    }

    [Test]
    public void MaximumLengthFrameRoundTripsAndCapacityFailureIsAtomic()
    {
        IRtpStreamFramingCodec codec = new RtpStreamFramingCodec();
        byte[] datagram = new byte[RtpProtocolConstants.MaximumStreamFramePayloadSize];
        datagram[0] = 0x80;
        datagram[datagram.Length - 1] = 0x5A;
        int encodedSize = RtpProtocolConstants.StreamFrameLengthSize + datagram.Length;
        byte[] encoded = new byte[encodedSize];
        byte[] insufficient = new byte[encodedSize - 1];
        int index = 0;
        while (index < insufficient.Length)
        {
            insufficient[index] = 77;
            index = index + 1;
        }

        int written = codec.EncodeRtpStreamFrame(datagram, encoded);
        RtpCodecResult decoded = codec.TryDecodeRtpStreamFrame(encoded);
        RtpCodecResult partial = codec.TryDecodeRtpStreamFrame(encoded[..^1]);
        RtpCodecResult capacity = codec.TryEncodeRtpStreamFrame(
            datagram,
            insufficient);

        Assert.Multiple(() =>
        {
            Assert.That(RtpProtocolConstants.StreamFrameLengthSize, Is.EqualTo(2));
            Assert.That(RtpProtocolConstants.MaximumStreamFramePayloadSize,
                Is.EqualTo(65535));
            Assert.That(written, Is.EqualTo(65537));
            Assert.That(encoded[..2], Is.EqualTo(new byte[] { 0xFF, 0xFF }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.ConsumedCount, Is.EqualTo(65537));
            Assert.That(decoded.Data, Is.EqualTo(datagram));
            Assert.That(partial.Success, Is.False);
            Assert.That(partial.ConsumedCount, Is.Zero);
            Assert.That(partial.Data, Is.Empty);
            Assert.That(capacity.Success, Is.False);
            Assert.That(capacity.WrittenCount, Is.Zero);
            Assert.That(insufficient, Has.All.EqualTo(77));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeRtpStreamFrame(datagram, insufficient));
            Assert.That(insufficient, Has.All.EqualTo(77));
        });
    }

    [Test]
    public void EncodingSupportsOverlappingSourceAndDestinationStorage()
    {
        IRtpStreamFramingCodec codec = new RtpStreamFramingCodec();
        byte[] storage = new byte[] { 1, 2, 3, 4, 0, 0 };

        int written = codec.EncodeRtpStreamFrame(
            storage.AsSpan(0, 4),
            storage);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(6));
            Assert.That(storage, Is.EqualTo(new byte[] { 0, 4, 1, 2, 3, 4 }));
        });
    }

    static byte[] EncodeFrame(
        IRtpStreamFramingCodec codec,
        ReadOnlySpan<byte> datagram)
    {
        byte[] frame = new byte[codec.GetRtpStreamFrameEncodedSize(datagram)];
        codec.EncodeRtpStreamFrame(datagram, frame);
        return frame;
    }

    static byte[] CreatePattern(int length)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)(index * 37 + 11);
            index = index + 1;
        }

        return data;
    }
}
