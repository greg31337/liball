using System.Text;

namespace http.Tests;

public class Http2FrameCodecExhaustiveTests
{
    [Test]
    public void EveryFrameTypeRoundTripsByteExactly()
    {
        IHttp2FrameCodec codec = new Http2FrameCodec();

        foreach (Http2Frame frame in CreateRepresentativeFrames())
        {
            Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
            Assert.That(serialized.Status, Is.EqualTo(HttpOperationStatus.Done),
                $"Could not serialize {frame.FrameType}.");

            Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done),
                $"Could not parse {frame.FrameType}.");
            Assert.That(parsed.Consumed, Is.EqualTo(serialized.Bytes.Length));

            Http2FrameResult roundTrip =
                codec.SerializeHttp2Frame(parsed.Frame!);
            Assert.That(roundTrip.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(roundTrip.Bytes, Is.EqualTo(serialized.Bytes),
                $"{frame.FrameType} did not round trip byte-exactly.");
        }
    }

    [Test]
    public void EveryValidKnownFlagCombinationRoundTripsWithUnknownFlags()
    {
        IHttp2FrameCodec codec = new Http2FrameCodec();

        foreach (Http2Frame frame in CreateRepresentativeFrames())
        {
            byte knownMask = GetKnownFlagMask(frame.FrameType);
            for (int flags = 0; flags <= 0xff; flags++)
            {
                if ((flags & ~knownMask) != 0)
                {
                    continue;
                }

                ApplyKnownFlags(frame, (byte)flags);
                frame.Flags = 0x80;
                Http2FrameResult serialized =
                    codec.SerializeHttp2Frame(frame);

                Assert.That(serialized.Status,
                    Is.EqualTo(HttpOperationStatus.Done),
                    $"Rejected {frame.FrameType} flags 0x{flags:x2}.");
                Assert.That(serialized.Bytes[4],
                    Is.EqualTo((byte)(0x80 | flags)));

                Http2FrameResult parsed =
                    codec.ParseHttp2Frame(serialized.Bytes);
                Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
                Http2FrameResult roundTrip =
                    codec.SerializeHttp2Frame(parsed.Frame!);
                Assert.That(roundTrip.Bytes, Is.EqualTo(serialized.Bytes));
            }
        }
    }

    [Test]
    public void EveryUnknownTypeAndFlagOctetRoundTripsExactly()
    {
        IHttp2FrameCodec codec = new Http2FrameCodec();

        for (int typeCode = 0; typeCode <= 0xff; typeCode++)
        {
            if (IsRegisteredTypeCode((byte)typeCode))
            {
                continue;
            }

            for (int flags = 0; flags <= 0xff; flags++)
            {
                byte[] wire = BuildWire(
                    (byte)typeCode,
                    (byte)flags,
                    Http2Constants.MaximumStreamIdentifier,
                    new byte[] { 0x00, 0xff });
                Http2FrameResult parsed = codec.ParseHttp2Frame(wire);

                Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(parsed.Frame!.FrameType,
                    Is.EqualTo(Http2FrameType.Unknown));
                Assert.That(parsed.Frame.TypeCode, Is.EqualTo((byte)typeCode));
                Assert.That(parsed.Frame.Flags, Is.EqualTo((byte)flags));
                Assert.That(codec.SerializeHttp2Frame(parsed.Frame).Bytes,
                    Is.EqualTo(wire));
            }
        }
    }

    [Test]
    public void EveryFrameTypeNeedsInputAtEveryTruncationBoundary()
    {
        IHttp2FrameCodec codec = new Http2FrameCodec();

        foreach (Http2Frame frame in CreateRepresentativeFrames())
        {
            byte[] wire = codec.SerializeHttp2Frame(frame).Bytes;
            for (int length = 0; length < wire.Length; length++)
            {
                Http2FrameResult result = codec.ParseHttp2Frame(
                    wire.AsSpan(0, length).ToArray());

                Assert.That(result.Status,
                    Is.EqualTo(HttpOperationStatus.NeedInput),
                    $"{frame.FrameType} accepted prefix length {length}.");
                Assert.That(result.Consumed, Is.Zero);
                Assert.That(result.RequiredSize, Is.EqualTo(
                    length < Http2Constants.FrameHeaderSize ?
                    Http2Constants.FrameHeaderSize : wire.Length));
            }
        }
    }

    [Test]
    public void EveryFrameTypeNeedsOutputForEveryShortDestination()
    {
        IHttp2FrameCodec codec = new Http2FrameCodec();

        foreach (Http2Frame frame in CreateRepresentativeFrames())
        {
            byte[] expected = codec.SerializeHttp2Frame(frame).Bytes;
            for (int length = 0; length < expected.Length; length++)
            {
                byte[] output = Enumerable.Repeat((byte)0xa5, length).ToArray();
                Http2FrameResult result =
                    codec.WriteHttp2Frame(frame, output.AsSpan());

                Assert.That(result.Status,
                    Is.EqualTo(HttpOperationStatus.NeedOutput),
                    $"{frame.FrameType} accepted output length {length}.");
                Assert.That(result.Written, Is.Zero);
                Assert.That(result.RequiredSize, Is.EqualTo(expected.Length));
                Assert.That(result.Bytes, Is.Empty);
                Assert.That(output,
                    Is.EqualTo(Enumerable.Repeat((byte)0xa5, length)));
            }

            byte[] oversized =
                Enumerable.Repeat((byte)0xa5, expected.Length + 1).ToArray();
            Http2FrameResult written =
                codec.WriteHttp2Frame(frame, oversized.AsSpan());
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(expected.Length));
            Assert.That(oversized.AsSpan(0, expected.Length).ToArray(),
                Is.EqualTo(expected));
            Assert.That(oversized[expected.Length], Is.EqualTo(0xa5));
        }
    }

    [Test]
    public void EveryPaddedFrameAcceptsBothPaddingLengthBoundaries()
    {
        IHttp2FrameCodec codec = new Http2FrameCodec();
        Http2Frame[] frames =
        [
            CreateDataFrame(),
            CreateHeadersFrame(),
            CreatePushPromiseFrame()
        ];

        foreach (Http2Frame frame in frames)
        {
            frame.IsPadded = true;
            frame.PaddingLength = 0;
            Http2FrameResult zero = codec.SerializeHttp2Frame(frame);
            Assert.That(zero.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(codec.ParseHttp2Frame(zero.Bytes).Frame!.PaddingLength,
                Is.Zero);

            frame.PaddingLength = 255;
            Http2FrameResult maximum = codec.SerializeHttp2Frame(frame);
            Assert.That(maximum.Status, Is.EqualTo(HttpOperationStatus.Done));
            Http2FrameResult parsed = codec.ParseHttp2Frame(maximum.Bytes);
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.PaddingLength, Is.EqualTo(255));
            Assert.That(codec.SerializeHttp2Frame(parsed.Frame).Bytes,
                Is.EqualTo(maximum.Bytes));

            frame.PaddingLength = 256;
            Assert.That(codec.SerializeHttp2Frame(frame).Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
        }
    }

    [Test]
    public void MaximumTwentyFourBitPayloadLengthRoundTrips()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        codec.configuredMaximumFrameSize = Http2Constants.MaximumFrameSize;
        codec.peerMaximumFrameSize = Http2Constants.MaximumFrameSize;
        Http2Frame frame = CreateUnknownFrame();
        frame.Payload = new byte[Http2Constants.MaximumFrameSize];
        frame.Payload[0] = 0x5a;
        frame.Payload[frame.Payload.Length - 1] = 0xa5;

        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.RequiredSize,
                Is.EqualTo(Http2Constants.MaximumFrameSize + 9));
            Assert.That(serialized.Bytes[0..3],
                Is.EqualTo(new byte[] { 0xff, 0xff, 0xff }));
        });

        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);
        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Consumed,
                Is.EqualTo(Http2Constants.MaximumFrameSize + 9));
            Assert.That(parsed.Frame!.Payload.Length,
                Is.EqualTo(Http2Constants.MaximumFrameSize));
            Assert.That(parsed.Frame.Payload[0], Is.EqualTo(0x5a));
            Assert.That(parsed.Frame.Payload[^1], Is.EqualTo(0xa5));
        });
    }

    [Test]
    public void ReservedStreamBitIsMaskedForEveryFrameType()
    {
        IHttp2FrameCodec codec = new Http2FrameCodec();

        foreach (Http2Frame frame in CreateRepresentativeFrames())
        {
            byte[] canonical = codec.SerializeHttp2Frame(frame).Bytes;
            byte[] reserved = canonical.ToArray();
            reserved[5] = (byte)(reserved[5] | 0x80);

            Http2FrameResult parsed = codec.ParseHttp2Frame(reserved);
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.ReservedBit, Is.True);
            Assert.That(parsed.Frame.StreamId, Is.EqualTo(frame.StreamId));

            Http2FrameResult normalized =
                codec.SerializeHttp2Frame(parsed.Frame);
            Assert.That(normalized.Bytes, Is.EqualTo(canonical));
            Assert.That(normalized.Frame!.ReservedBit, Is.False);
        }
    }

    static List<Http2Frame> CreateRepresentativeFrames()
    {
        return new List<Http2Frame>
        {
            CreateDataFrame(),
            CreateHeadersFrame(),
            CreatePriorityFrame(),
            CreateResetStreamFrame(),
            CreateSettingsFrame(),
            CreatePushPromiseFrame(),
            CreatePingFrame(),
            CreateGoAwayFrame(),
            CreateWindowUpdateFrame(),
            CreateContinuationFrame(),
            CreateOriginFrame(),
            CreatePriorityUpdateFrame(),
            CreateUnknownFrame()
        };
    }

    static Http2Frame CreateDataFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = 1;
        frame.Data = new byte[] { 0x00, 0xff };
        return frame;
    }

    static Http2Frame CreateHeadersFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = 1;
        frame.HeaderBlockFragment = new byte[] { 0x82 };
        return frame;
    }

    static Http2Frame CreatePriorityFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Priority;
        frame.StreamId = 3;
        frame.StreamDependency = 1;
        frame.Weight = 256;
        return frame;
    }

    static Http2Frame CreateResetStreamFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.RstStream;
        frame.StreamId = 1;
        frame.HasErrorCode = true;
        frame.ErrorCode = Http2ErrorCode.NoError;
        frame.ErrorCodeValue = 0;
        frame.IsKnownErrorCode = true;
        return frame;
    }

    static Http2Frame CreateSettingsFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        return frame;
    }

    static Http2Frame CreatePushPromiseFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = 1;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = 2;
        frame.HeaderBlockFragment = new byte[] { 0x82 };
        return frame;
    }

    static Http2Frame CreatePingFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Ping;
        frame.OpaqueData = Encoding.ASCII.GetBytes("12345678");
        return frame;
    }

    static Http2Frame CreateGoAwayFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.GoAway;
        frame.HasLastStreamId = true;
        frame.LastStreamId = Http2Constants.MaximumStreamIdentifier;
        frame.HasErrorCode = true;
        frame.ErrorCode = Http2ErrorCode.NoError;
        frame.ErrorCodeValue = 0;
        frame.IsKnownErrorCode = true;
        frame.DebugData = new byte[] { 0x00, 0xff };
        return frame;
    }

    static Http2Frame CreateWindowUpdateFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.WindowUpdate;
        frame.StreamId = Http2Constants.MaximumStreamIdentifier;
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = Http2Constants.MaximumWindowSize;
        return frame;
    }

    static Http2Frame CreateContinuationFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Continuation;
        frame.StreamId = 1;
        frame.HeaderBlockFragment = new byte[] { 0x82 };
        return frame;
    }

    static Http2Frame CreateOriginFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Origin;
        frame.Origins.Add("https://example.com");
        return frame;
    }

    static Http2Frame CreatePriorityUpdateFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PriorityUpdate;
        frame.HasPrioritizedElementId = true;
        frame.PrioritizedElementId = 1;
        frame.PriorityFieldValue = Encoding.ASCII.GetBytes("u=3");
        return frame;
    }

    static Http2Frame CreateUnknownFrame()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Unknown;
        frame.TypeCode = 0xfe;
        frame.Payload = new byte[] { 0x00, 0xff };
        return frame;
    }

    static byte GetKnownFlagMask(Http2FrameType frameType)
    {
        switch (frameType)
        {
            case Http2FrameType.Data:
                return 0x09;
            case Http2FrameType.Headers:
                return 0x2d;
            case Http2FrameType.Settings:
            case Http2FrameType.Ping:
                return 0x01;
            case Http2FrameType.PushPromise:
                return 0x0c;
            case Http2FrameType.Continuation:
                return 0x04;
            default:
                return 0;
        }
    }

    static void ApplyKnownFlags(Http2Frame frame, byte flags)
    {
        switch (frame.FrameType)
        {
            case Http2FrameType.Data:
                frame.EndStream = (flags & 0x01) != 0;
                frame.IsPadded = (flags & 0x08) != 0;
                frame.PaddingLength = 0;
                break;
            case Http2FrameType.Headers:
                frame.EndStream = (flags & 0x01) != 0;
                frame.EndHeaders = (flags & 0x04) != 0;
                frame.IsPadded = (flags & 0x08) != 0;
                frame.PaddingLength = 0;
                frame.HasPriority = (flags & 0x20) != 0;
                frame.ExclusiveDependency = false;
                frame.StreamDependency = 0;
                frame.Weight = frame.HasPriority ? 1 : 0;
                break;
            case Http2FrameType.Settings:
            case Http2FrameType.Ping:
                frame.Acknowledgement = (flags & 0x01) != 0;
                break;
            case Http2FrameType.PushPromise:
                frame.EndHeaders = (flags & 0x04) != 0;
                frame.IsPadded = (flags & 0x08) != 0;
                frame.PaddingLength = 0;
                break;
            case Http2FrameType.Continuation:
                frame.EndHeaders = (flags & 0x04) != 0;
                break;
            default:
                break;
        }
    }

    static bool IsRegisteredTypeCode(byte typeCode)
    {
        return typeCode == 0x00 || typeCode == 0x01 ||
            typeCode == 0x02 || typeCode == 0x03 ||
            typeCode == 0x04 || typeCode == 0x05 ||
            typeCode == 0x06 || typeCode == 0x07 ||
            typeCode == 0x08 || typeCode == 0x09 ||
            typeCode == 0x0c || typeCode == 0x10;
    }

    static byte[] BuildWire(
        byte typeCode,
        byte flags,
        int streamId,
        byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = typeCode;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
