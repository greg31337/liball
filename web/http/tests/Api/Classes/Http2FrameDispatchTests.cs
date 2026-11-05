namespace http.Tests;

public class Http2FrameDispatchTests
{
    [Test]
    public void EveryFrameTypeRoundTripsThroughAllocatingAndBoundedDispatch()
    {
        byte[][] frames = BuildRepresentativeFrames();
        Http2FrameCodec codec = new Http2FrameCodec();

        Assert.Multiple(() =>
        {
            foreach (byte[] wire in frames)
            {
                Http2FrameResult parsed = codec.ParseHttp2Frame(wire);
                Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done),
                    $"parse type 0x{wire[3]:x2}");
                Assert.That(parsed.Consumed, Is.EqualTo(wire.Length),
                    $"parse consumption type 0x{wire[3]:x2}");

                Http2FrameResult serialized =
                    codec.SerializeHttp2Frame(parsed.Frame!);
                Assert.That(serialized.Status,
                    Is.EqualTo(HttpOperationStatus.Done),
                    $"serialize type 0x{wire[3]:x2}");
                Assert.That(serialized.RequiredSize, Is.EqualTo(wire.Length),
                    $"serialized size type 0x{wire[3]:x2}");
                Assert.That(serialized.Written, Is.EqualTo(wire.Length),
                    $"serialized count type 0x{wire[3]:x2}");
                Assert.That(serialized.Bytes, Is.EqualTo(wire),
                    $"serialized bytes type 0x{wire[3]:x2}");

                byte[] exact = Enumerable.Repeat(
                    (byte)0xcc, wire.Length).ToArray();
                Http2FrameResult written =
                    codec.WriteHttp2Frame(parsed.Frame!, exact);
                Assert.That(written.Status,
                    Is.EqualTo(HttpOperationStatus.Done),
                    $"bounded write type 0x{wire[3]:x2}");
                Assert.That(written.RequiredSize, Is.EqualTo(wire.Length),
                    $"bounded size type 0x{wire[3]:x2}");
                Assert.That(written.Written, Is.EqualTo(wire.Length),
                    $"bounded count type 0x{wire[3]:x2}");
                Assert.That(written.Bytes, Is.Empty,
                    $"bounded ownership type 0x{wire[3]:x2}");
                Assert.That(exact, Is.EqualTo(wire),
                    $"bounded bytes type 0x{wire[3]:x2}");

                byte[] shortOutput = Enumerable.Repeat(
                    (byte)0xdd, wire.Length - 1).ToArray();
                Http2FrameResult shortage =
                    codec.WriteHttp2Frame(parsed.Frame!, shortOutput);
                Assert.That(shortage.Status,
                    Is.EqualTo(HttpOperationStatus.NeedOutput),
                    $"short write type 0x{wire[3]:x2}");
                Assert.That(shortage.RequiredSize, Is.EqualTo(wire.Length),
                    $"short required size type 0x{wire[3]:x2}");
                Assert.That(shortage.Written, Is.Zero,
                    $"short written count type 0x{wire[3]:x2}");
                Assert.That(shortOutput, Is.EqualTo(Enumerable.Repeat(
                    (byte)0xdd, wire.Length - 1).ToArray()),
                    $"short output mutation type 0x{wire[3]:x2}");
            }
        });
    }

    [Test]
    public void UndefinedFrameTypeIsNotDispatchedAsData()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = (Http2FrameType)0x7f;
        frame.StreamId = 1;
        frame.Data = new byte[] { 0xaa };
        byte[] output = Enumerable.Repeat((byte)0xee, 16).ToArray();
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult written = codec.WriteHttp2Frame(frame, output);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Unsupported));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-frame-serialization-not-implemented"));
            Assert.That(serialized.Frame, Is.SameAs(frame));
            Assert.That(serialized.Bytes, Is.Empty);
            Assert.That(serialized.Written, Is.Zero);
            Assert.That(serialized.RequiredSize, Is.Zero);
            Assert.That(written.Status,
                Is.EqualTo(HttpOperationStatus.Unsupported));
            Assert.That(output,
                Is.EqualTo(Enumerable.Repeat((byte)0xee, 16).ToArray()));
        });
    }

    static byte[][] BuildRepresentativeFrames()
    {
        return new byte[][]
        {
            BuildWire(0x00, 0xa1, 1, new byte[] { 0xaa }),
            BuildWire(0x01, 0xc5, 3, new byte[] { 0x82 }),
            BuildWire(0x02, 0xa5, 3,
                new byte[] { 0x00, 0x00, 0x00, 0x01, 0x0f }),
            BuildWire(0x03, 0xa5, 3,
                new byte[] { 0xde, 0xad, 0xbe, 0xef }),
            BuildWire(0x04, 0xa0, 0,
                new byte[] { 0x00, 0xff, 0xde, 0xad, 0xbe, 0xef }),
            BuildWire(0x05, 0xe4, 3,
                new byte[] { 0x00, 0x00, 0x00, 0x05, 0x82 }),
            BuildWire(0x06, 0xa1, 0,
                new byte[] { 0x00, 0x01, 0x02, 0x03,
                    0x04, 0x05, 0x06, 0x07 }),
            BuildWire(0x07, 0xa5, 0,
                new byte[] { 0x00, 0x00, 0x00, 0x03,
                    0xde, 0xad, 0xbe, 0xef, 0xaa }),
            BuildWire(0x08, 0xa5, 0,
                new byte[] { 0x00, 0x00, 0x00, 0x01 }),
            BuildWire(0x09, 0xc4, 3, new byte[] { 0x82 }),
            BuildWire(0x0c, 0xf0, 0,
                BuildOriginPayload("https://example.test")),
            BuildWire(0x10, 0xa5, 0,
                new byte[] { 0x00, 0x00, 0x00, 0x03,
                    0x75, 0x3d, 0x31 }),
            BuildWire(0xfe, 0xa5, 7, new byte[] { 0xaa, 0xbb })
        };
    }

    static byte[] BuildOriginPayload(string origin)
    {
        byte[] payload = new byte[origin.Length + 2];
        payload[0] = (byte)(origin.Length >> 8);
        payload[1] = (byte)origin.Length;
        int index = 0;
        while (index < origin.Length)
        {
            payload[index + 2] = (byte)origin[index];
            index = index + 1;
        }

        return payload;
    }

    static byte[] BuildWire(
        byte type,
        byte flags,
        int streamId,
        byte[] payload)
    {
        byte[] wire = new byte[
            Http2Constants.FrameHeaderSize + payload.Length];
        wire[0] = (byte)(payload.Length >> 16);
        wire[1] = (byte)(payload.Length >> 8);
        wire[2] = (byte)payload.Length;
        wire[3] = type;
        wire[4] = flags;
        wire[5] = (byte)(streamId >> 24);
        wire[6] = (byte)(streamId >> 16);
        wire[7] = (byte)(streamId >> 8);
        wire[8] = (byte)streamId;
        payload.CopyTo(wire, Http2Constants.FrameHeaderSize);
        return wire;
    }
}
