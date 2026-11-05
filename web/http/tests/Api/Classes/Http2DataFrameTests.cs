using System.Text;

namespace http.Tests;

public class Http2DataFrameTests
{
    [Test]
    public void UnpaddedDataExposesApplicationBytesAndEndStream()
    {
        byte[] input = BuildDataWire(0x01, 1,
            Encoding.ASCII.GetBytes("abc"));

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(12));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType, Is.EqualTo(Http2FrameType.Data));
            Assert.That(result.Frame.EndStream, Is.True);
            Assert.That(result.Frame.IsPadded, Is.False);
            Assert.That(result.Frame.PaddingLength, Is.Zero);
            Assert.That(result.Frame.FlowControlledLength, Is.EqualTo(3));
            Assert.That(result.Frame.Data,
                Is.EqualTo(Encoding.ASCII.GetBytes("abc")));
            Assert.That(result.Frame.Payload,
                Is.EqualTo(Encoding.ASCII.GetBytes("abc")));
        });
    }

    [Test]
    public void PaddedDataSeparatesApplicationBytesAndCountsAllPayloadFlow()
    {
        byte[] payload =
        [
            0x02, (byte)'a', (byte)'b', (byte)'c', 0xde, 0xad
        ];
        byte[] input = BuildDataWire(0x09, 3, payload);

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.EndStream, Is.True);
            Assert.That(result.Frame.IsPadded, Is.True);
            Assert.That(result.Frame.PaddingLength, Is.EqualTo(2));
            Assert.That(result.Frame.Data,
                Is.EqualTo(Encoding.ASCII.GetBytes("abc")));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
            Assert.That(result.Frame.FlowControlledLength, Is.EqualTo(6));
        });
    }

    [Test]
    public void PaddedDataPermitsZeroPaddingAndEmptyApplicationData()
    {
        Http2FrameResult zeroPadding = new Http2FrameCodec().ParseHttp2Frame(
            BuildDataWire(0x08, 1,
                new byte[] { 0x00, (byte)'x', (byte)'y' }));
        Http2FrameResult emptyData = new Http2FrameCodec().ParseHttp2Frame(
            BuildDataWire(0x08, 1, new byte[] { 0x02, 0xaa, 0xbb }));

        Assert.Multiple(() =>
        {
            Assert.That(zeroPadding.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(zeroPadding.Frame!.PaddingLength, Is.Zero);
            Assert.That(zeroPadding.Frame.Data,
                Is.EqualTo(Encoding.ASCII.GetBytes("xy")));
            Assert.That(emptyData.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(emptyData.Frame!.PaddingLength, Is.EqualTo(2));
            Assert.That(emptyData.Frame.Data, Is.Empty);
            Assert.That(emptyData.Frame.FlowControlledLength, Is.EqualTo(3));
        });
    }

    [Test]
    public void MissingOrExcessivePaddingIsRejected()
    {
        Http2FrameResult missing = new Http2FrameCodec().ParseHttp2Frame(
            BuildDataWire(0x08, 1, Array.Empty<byte>()));
        Http2FrameResult excessive = new Http2FrameCodec().ParseHttp2Frame(
            BuildDataWire(0x08, 1, new byte[] { 0x03, 0xaa, 0xbb }));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missing.ErrorCode,
                Is.EqualTo("http2-data-pad-length-missing"));
            Assert.That(excessive.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(excessive.ErrorCode,
                Is.EqualTo("http2-data-padding-invalid"));
            Assert.That(excessive.Consumed, Is.EqualTo(12));
        });
    }

    [Test]
    public void DataOnStreamZeroIsRejected()
    {
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildDataWire(0, 0, new byte[] { 0x01 }));
        Http2Frame frame = CreateDataFrame(0, "x");
        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode, Is.EqualTo("http2-data-stream-zero"));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-data-stream-zero"));
        });
    }

    [Test]
    public void SerializationPreservesUnknownFlagsAndEmitsCanonicalHeader()
    {
        Http2Frame frame = CreateDataFrame(1, "abc");
        frame.Flags = 0x24;
        frame.EndStream = true;
        frame.ReservedBit = true;

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x03, 0x00, 0x25,
                0x00, 0x00, 0x00, 0x01,
                (byte)'a', (byte)'b', (byte)'c'
            }));
            Assert.That(result.Written, Is.EqualTo(12));
            Assert.That(result.RequiredSize, Is.EqualTo(12));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.ReservedBit, Is.False);
            Assert.That(result.Frame.Flags, Is.EqualTo(0x25));
            Assert.That(result.Frame.FlowControlledLength, Is.EqualTo(3));
        });
    }

    [Test]
    public void PaddedSerializationEmitsPadLengthAndZeroPadding()
    {
        Http2Frame frame = CreateDataFrame(5, "abc");
        frame.Flags = 0x01;
        frame.IsPadded = true;
        frame.PaddingLength = 2;

        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x06, 0x00, 0x08,
                0x00, 0x00, 0x00, 0x05,
                0x02, (byte)'a', (byte)'b', (byte)'c', 0x00, 0x00
            }));
            Assert.That(serialized.Frame!.Payload,
                Is.EqualTo(new byte[]
                {
                    0x02, (byte)'a', (byte)'b', (byte)'c', 0x00, 0x00
                }));
            Assert.That(serialized.Frame.FlowControlledLength, Is.EqualTo(6));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.Data,
                Is.EqualTo(Encoding.ASCII.GetBytes("abc")));
            Assert.That(parsed.Frame.PaddingLength, Is.EqualTo(2));
        });
    }

    [Test]
    public void PaddingCountsTowardTheActiveFrameSizeLimit()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        codec.configuredMaximumFrameSize = 5;
        codec.peerMaximumFrameSize = 5;
        Http2Frame exactFrame = CreateDataFrame(1, "abcd");
        exactFrame.IsPadded = true;
        Http2Frame tooLargeFrame = CreateDataFrame(1, "abcd");
        tooLargeFrame.IsPadded = true;
        tooLargeFrame.PaddingLength = 1;

        Http2FrameResult exact = codec.SerializeHttp2Frame(exactFrame);
        Http2FrameResult tooLarge = codec.SerializeHttp2Frame(tooLargeFrame);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exact.Frame!.Length, Is.EqualTo(5));
            Assert.That(exact.Frame.FlowControlledLength, Is.EqualTo(5));
            Assert.That(tooLarge.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(tooLarge.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
        });
    }

    [Test]
    public void BoundedWriteIsExactAndAllOrNothing()
    {
        Http2Frame frame = CreateDataFrame(1, "abc");
        byte[] exact = Enumerable.Repeat((byte)0xcc, 12).ToArray();
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 11).ToArray();
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult written = codec.WriteHttp2Frame(frame, exact);
        Http2FrameResult needOutput = codec.WriteHttp2Frame(frame, shortOutput);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(12));
            Assert.That(written.RequiredSize, Is.EqualTo(12));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(exact, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x03, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x01,
                (byte)'a', (byte)'b', (byte)'c'
            }));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(12));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 11).ToArray()));
        });
    }

    [Test]
    public void InvalidSerializationPaddingMetadataIsRejected()
    {
        Http2Frame negative = CreateDataFrame(1, "x");
        negative.IsPadded = true;
        negative.PaddingLength = -1;
        Http2Frame excessive = CreateDataFrame(1, "x");
        excessive.IsPadded = true;
        excessive.PaddingLength = 256;
        Http2Frame inconsistent = CreateDataFrame(1, "x");
        inconsistent.PaddingLength = 1;
        Http2FrameCodec codec = new Http2FrameCodec();

        Assert.Multiple(() =>
        {
            Assert.That(codec.SerializeHttp2Frame(negative).Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(codec.SerializeHttp2Frame(excessive).Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(codec.SerializeHttp2Frame(inconsistent).Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
        });
    }

    static Http2Frame CreateDataFrame(int streamId, string data)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.Data = Encoding.ASCII.GetBytes(data);
        return frame;
    }

    static byte[] BuildDataWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x00;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
