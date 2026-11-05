using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class Utf8CodecTests
{
    [Test]
    public void DecodesEveryRfc3629ScalarBoundary()
    {
        byte[] bytes =
        [
            0x00,
            0x7F,
            0xC2, 0x80,
            0xDF, 0xBF,
            0xE0, 0xA0, 0x80,
            0xED, 0x9F, 0xBF,
            0xEE, 0x80, 0x80,
            0xEF, 0xBF, 0xBF,
            0xF0, 0x90, 0x80, 0x80,
            0xF4, 0x8F, 0xBF, 0xBF,
        ];
        string expected = "\u0000\u007F\u0080\u07FF\u0800\uD7FF\uE000\uFFFF" + char.ConvertFromUtf32(0x10000) + char.ConvertFromUtf32(0x10FFFF);

        Assert.That(DecodeBytes(bytes), Is.EqualTo(expected));
    }

    [Test]
    public void ScalarDecodingReportsConsumedBytesAndEndOfInput()
    {
        MemoryByteSource source = new MemoryByteSource(new byte[] { 0xC2, 0xA2, 0xF0, 0x9F, 0x98, 0x80 });
        Utf8Codec codec = new Utf8Codec(source);

        Utf8Scalar first = codec.DecodeUtf8Scalar();
        Utf8Scalar second = codec.DecodeUtf8Scalar();
        Utf8Scalar end = codec.DecodeUtf8Scalar();

        Assert.That(first.Value, Is.EqualTo(0xA2));
        Assert.That(first.ByteCount, Is.EqualTo(2));
        Assert.That(first.EndOfInput, Is.False);
        Assert.That(second.Value, Is.EqualTo(0x1F600));
        Assert.That(second.ByteCount, Is.EqualTo(4));
        Assert.That(second.EndOfInput, Is.False);
        Assert.That(end.ByteCount, Is.Zero);
        Assert.That(end.EndOfInput, Is.True);
        Assert.That(source.SourceBytePosition(), Is.EqualTo(6));
    }

    [Test]
    public void DecodingConsumesOnlyAnInitialBomAndHandlesOneByteStreamReads()
    {
        byte[] bytes = new byte[] { 0xEF, 0xBB, 0xBF, 0xE2, 0x82, 0xAC, 0xEF, 0xBB, 0xBF };
        OneByteStream stream = new OneByteStream(bytes);
        StreamByteSource source = new StreamByteSource(stream, false, 4);

        string decoded = new Utf8Codec(source).DecodeUtf8Text();

        Assert.That(decoded, Is.EqualTo("\u20AC\uFEFF"));
        Assert.That(source.InspectByteOrderMark(), Is.EqualTo(ByteOrderMarkKind.Utf8));
        Assert.That(source.SourceBytePosition(), Is.EqualTo(6));
        Assert.That(stream.ReadCalls, Is.GreaterThan(4));
    }

    [Test]
    public void RejectsInvalidLeadingAndContinuationBytes()
    {
        byte[][] invalidLeaders =
        [
            [0x80],
            [0xBF],
            [0xC0, 0x80],
            [0xC1, 0xBF],
            [0xF5, 0x80, 0x80, 0x80],
            [0xFF],
        ];
        foreach (byte[] bytes in invalidLeaders)
        {
            AssertInvalid(bytes, "Invalid UTF-8 leading byte");
        }

        byte[][] invalidContinuations =
        [
            [0xC2, 0x20],
            [0xC2, 0xC0],
            [0xE1, 0x20, 0x80],
            [0xE1, 0x80, 0x20],
            [0xE1, 0x80, 0xFF],
            [0xF1, 0x80, 0x80, 0x20],
        ];
        foreach (byte[] bytes in invalidContinuations)
        {
            AssertInvalid(bytes, "Invalid UTF-8 continuation byte");
        }
    }

    [Test]
    public void RejectsOverlongSurrogateAndOutOfRangeSequences()
    {
        byte[][] overlong =
        [
            [0xE0, 0x80, 0x80],
            [0xE0, 0x9F, 0xBF],
            [0xF0, 0x80, 0x80, 0x80],
            [0xF0, 0x8F, 0xBF, 0xBF],
        ];
        foreach (byte[] bytes in overlong)
        {
            AssertInvalid(bytes, "Overlong UTF-8 sequence");
        }

        AssertInvalid(new byte[] { 0xED, 0xA0, 0x80 }, "encodes a surrogate");
        AssertInvalid(new byte[] { 0xED, 0xBF, 0xBF }, "encodes a surrogate");
        AssertInvalid(new byte[] { 0xF4, 0x90, 0x80, 0x80 }, "exceeds U+10FFFF");
        AssertInvalid(new byte[] { 0xF4, 0xBF, 0xBF, 0xBF }, "exceeds U+10FFFF");
    }

    [Test]
    public void RejectsEveryTruncatedSequenceLength()
    {
        byte[][] truncated =
        [
            [0xC2],
            [0xE1],
            [0xE1, 0x80],
            [0xF1],
            [0xF1, 0x80],
            [0xF1, 0x80, 0x80],
            [0xEF, 0xBB],
        ];
        foreach (byte[] bytes in truncated)
        {
            AssertInvalid(bytes, "Truncated UTF-8 sequence");
        }
    }

    [Test]
    public void InvalidContinuationReportsItsLogicalByteOffset()
    {
        DecoderFallbackException exception = Assert.Throws<DecoderFallbackException>(() => DecodeBytes(new byte[] { 0x41, 0xC2, 0x20 }))!;

        Assert.That(exception.Message, Does.Contain("byte offset 2"));
    }

    private static string DecodeBytes(byte[] bytes)
    {
        MemoryByteSource source = new MemoryByteSource(bytes);
        return new Utf8Codec(source).DecodeUtf8Text();
    }

    private static void AssertInvalid(byte[] bytes, string message)
    {
        DecoderFallbackException exception = Assert.Throws<DecoderFallbackException>(() => DecodeBytes(bytes))!;
        Assert.That(exception.Message, Does.Contain(message));
    }

    private sealed class OneByteStream : MemoryStream
    {
        internal int ReadCalls;

        internal OneByteStream(byte[] bytes) : base(bytes)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls = ReadCalls + 1;
            return base.Read(buffer, offset, count == 0 ? 0 : 1);
        }
    }
}
