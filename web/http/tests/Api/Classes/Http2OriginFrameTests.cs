namespace http.Tests;

public class Http2OriginFrameTests
{
    [Test]
    public void ParsingExtractsRepeatedCanonicalOriginsInOrder()
    {
        byte[] payload = BuildPayload(
            "https://example.test"u8.ToArray(),
            "http://api.example.test:8080"u8.ToArray(),
            "https://[2001:db8::1]:8443"u8.ToArray());

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0xf0, 0, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed,
                Is.EqualTo(Http2Constants.FrameHeaderSize + payload.Length));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Origin));
            Assert.That(result.Frame.OriginIgnored, Is.False);
            Assert.That(result.Frame.Origins, Is.EqualTo(new[]
            {
                "https://example.test",
                "http://api.example.test:8080",
                "https://[2001:db8::1]:8443"
            }));
            Assert.That(result.Frame.Flags, Is.EqualTo(0xf0));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void ParsingSkipsInvalidIndividualOriginEntries()
    {
        byte[] payload = BuildPayload(
            Array.Empty<byte>(),
            "HTTPS://example.test"u8.ToArray(),
            "https://*.example.test"u8.ToArray(),
            "https://example.test/"u8.ToArray(),
            "https://example.test:443"u8.ToArray(),
            "https://user@example.test"u8.ToArray(),
            new byte[] { 0x68, 0x74, 0x74, 0x70, 0x73, 0x3a, 0x2f, 0x2f, 0xff },
            "https://valid.example.test:8443"u8.ToArray());

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame!.OriginIgnored, Is.False);
            Assert.That(result.Frame.Origins,
                Is.EqualTo(new[] { "https://valid.example.test:8443" }));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void ParsingAllowsAnEmptyOriginFrame()
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0, Array.Empty<byte>()));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame!.Origins, Is.Empty);
            Assert.That(result.Frame.OriginIgnored, Is.False);
        });
    }

    [Test]
    public void StreamAndIncompatibleFlagsCauseWholeFrameToBeIgnored()
    {
        byte[] malformedPayload = [0x00];
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult stream = codec.ParseHttp2Frame(
            BuildWire(0, 3, malformedPayload));
        Http2FrameResult flags = codec.ParseHttp2Frame(
            BuildWire(0x01, 0, malformedPayload));

        Assert.Multiple(() =>
        {
            Assert.That(stream.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(stream.Frame!.OriginIgnored, Is.True);
            Assert.That(stream.Frame.StreamId, Is.EqualTo(3));
            Assert.That(stream.Frame.Origins, Is.Empty);
            Assert.That(stream.Frame.Payload, Is.EqualTo(malformedPayload));
            Assert.That(flags.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(flags.Frame!.OriginIgnored, Is.True);
            Assert.That(flags.Frame.Flags, Is.EqualTo(0x01));
            Assert.That(flags.Frame.Origins, Is.Empty);
        });
    }

    [Test]
    public void ParsingRejectsTruncatedEntryLengthsAndValues()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult shortLength = codec.ParseHttp2Frame(
            BuildWire(0, 0, new byte[] { 0x00 }));
        Http2FrameResult shortValue = codec.ParseHttp2Frame(
            BuildWire(0, 0, new byte[] { 0x00, 0x03, 0x61, 0x62 }));

        Assert.Multiple(() =>
        {
            Assert.That(shortLength.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(shortLength.ErrorCode,
                Is.EqualTo("http2-origin-length-truncated"));
            Assert.That(shortLength.ErrorPosition,
                Is.EqualTo(Http2Constants.FrameHeaderSize));
            Assert.That(shortValue.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(shortValue.ErrorCode,
                Is.EqualTo("http2-origin-value-truncated"));
            Assert.That(shortValue.ErrorPosition,
                Is.EqualTo(Http2Constants.FrameHeaderSize));
        });
    }

    [Test]
    public void SerializationPreservesOriginsAndCompatibleFlags()
    {
        Http2Frame frame = CreateOriginFrame(
            "https://example.test",
            "http://api.example.test:8080");
        frame.Flags = 0xf0;
        frame.ReservedBit = true;
        byte[] expectedPayload = BuildPayload(
            "https://example.test"u8.ToArray(),
            "http://api.example.test:8080"u8.ToArray());

        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes,
                Is.EqualTo(BuildWire(0xf0, 0, expectedPayload)));
            Assert.That(serialized.Written,
                Is.EqualTo(Http2Constants.FrameHeaderSize +
                    expectedPayload.Length));
            Assert.That(serialized.Frame!.ReservedBit, Is.False);
            Assert.That(serialized.Frame.OriginIgnored, Is.False);
            Assert.That(serialized.Frame.Origins,
                Is.EqualTo(frame.Origins));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.Origins, Is.EqualTo(frame.Origins));
        });
    }

    [Test]
    public void SerializationRejectsInvalidOriginStrings()
    {
        string[] invalid =
        [
            "",
            "HTTPS://example.test",
            "https://EXAMPLE.test",
            "https://*.example.test",
            "https://example.test/",
            "https://example.test:443",
            "http://example.test:080",
            "ftp://example.test",
            "https://user@example.test",
            "https://example.test?q=1",
            "https://caf\u00e9.test"
        ];
        Http2FrameCodec codec = new Http2FrameCodec();

        Assert.Multiple(() =>
        {
            foreach (string origin in invalid)
            {
                Http2FrameResult result = codec.SerializeHttp2Frame(
                    CreateOriginFrame(origin));
                Assert.That(result.Status,
                    Is.EqualTo(HttpOperationStatus.Rejected), origin);
                Assert.That(result.ErrorCode,
                    Is.EqualTo("invalid-http2-origin-value"), origin);
            }
        });
    }

    [Test]
    public void SerializationRejectsNonzeroStreamAndIncompatibleFlags()
    {
        Http2Frame stream = CreateOriginFrame("https://example.test");
        stream.StreamId = 1;
        Http2Frame flags = CreateOriginFrame("https://example.test");
        flags.Flags = 0x08;
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult streamResult = codec.SerializeHttp2Frame(stream);
        Http2FrameResult flagsResult = codec.SerializeHttp2Frame(flags);

        Assert.Multiple(() =>
        {
            Assert.That(streamResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(streamResult.ErrorCode,
                Is.EqualTo("invalid-http2-origin-stream"));
            Assert.That(flagsResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(flagsResult.ErrorCode,
                Is.EqualTo("invalid-http2-origin-flags"));
        });
    }

    [Test]
    public void SerializationHonorsFrameLimitAndBoundedOutput()
    {
        Http2Frame one = CreateOriginFrame("https://a.test");
        Http2Frame two = CreateOriginFrame(
            "https://a.test", "https://b.test");
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 16;
        limited.peerMaximumFrameSize = 16;
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(two);
        byte[] exact = new byte[serialized.RequiredSize];
        byte[] shortOutput = Enumerable.Repeat(
            (byte)0xcc, serialized.RequiredSize - 1).ToArray();

        Http2FrameResult accepted = limited.SerializeHttp2Frame(one);
        Http2FrameResult rejected = limited.SerializeHttp2Frame(two);
        Http2FrameResult written = codec.WriteHttp2Frame(two, exact);
        Http2FrameResult needOutput =
            codec.WriteHttp2Frame(two, shortOutput);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(accepted.Frame!.Length, Is.EqualTo(16));
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(serialized.RequiredSize));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(exact, Is.EqualTo(serialized.Bytes));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize,
                Is.EqualTo(serialized.RequiredSize));
            Assert.That(shortOutput, Is.EqualTo(Enumerable.Repeat(
                (byte)0xcc, serialized.RequiredSize - 1).ToArray()));
        });
    }

    static Http2Frame CreateOriginFrame(params string[] origins)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Origin;
        frame.Origins.AddRange(origins);
        return frame;
    }

    static byte[] BuildPayload(params byte[][] origins)
    {
        int length = origins.Sum(origin => origin.Length + 2);
        byte[] payload = new byte[length];
        int offset = 0;
        foreach (byte[] origin in origins)
        {
            payload[offset] = (byte)(origin.Length >> 8);
            payload[offset + 1] = (byte)origin.Length;
            origin.CopyTo(payload, offset + 2);
            offset += origin.Length + 2;
        }

        return payload;
    }

    static byte[] BuildWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x0c;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
