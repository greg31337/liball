using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class Utf16CodecTests
{
    [Test]
    public void DecodesBigAndLittleEndianScalarBoundaries()
    {
        int[] codeUnits = [0x0000, 0x0041, 0xD7FF, 0xE000, 0xFEFF, 0xFFFF, 0xD800, 0xDC00, 0xDBFF, 0xDFFF];
        string expected = "\u0000A\uD7FF\uE000\uFEFF\uFFFF" + char.ConvertFromUtf32(0x10000) + char.ConvertFromUtf32(0x10FFFF);

        Assert.That(DecodeBytes(EncodeCodeUnits(codeUnits, Utf16ByteOrder.BigEndian, ByteOrderMarkKind.None), Utf16ByteOrder.BigEndian), Is.EqualTo(expected));
        Assert.That(DecodeBytes(EncodeCodeUnits(codeUnits, Utf16ByteOrder.LittleEndian, ByteOrderMarkKind.None), Utf16ByteOrder.LittleEndian), Is.EqualTo(expected));
    }

    [Test]
    public void InitialBomSelectsByteOrderAndIsNotReturnedAsText()
    {
        byte[] bigEndian = EncodeCodeUnits([0x0041], Utf16ByteOrder.BigEndian, ByteOrderMarkKind.Utf16BigEndian);
        byte[] littleEndian = EncodeCodeUnits([0x0042], Utf16ByteOrder.LittleEndian, ByteOrderMarkKind.Utf16LittleEndian);
        byte[] bomOnly = EncodeCodeUnits([], Utf16ByteOrder.BigEndian, ByteOrderMarkKind.Utf16BigEndian);

        Assert.That(DecodeBytes(bigEndian, Utf16ByteOrder.LittleEndian), Is.EqualTo("A"));
        Assert.That(DecodeBytes(littleEndian, Utf16ByteOrder.BigEndian), Is.EqualTo("B"));
        Assert.That(DecodeBytes(bomOnly, Utf16ByteOrder.LittleEndian), Is.Empty);
    }

    [Test]
    public void ScalarDecodingReportsCodePointsConsumedBytesAndEndOfInput()
    {
        byte[] bytes = EncodeCodeUnits([0x00A2, 0xD83D, 0xDE00], Utf16ByteOrder.BigEndian, ByteOrderMarkKind.None);
        MemoryByteSource source = new MemoryByteSource(bytes);
        Utf16Codec codec = new Utf16Codec(source, Utf16ByteOrder.BigEndian);

        Utf16Scalar first = codec.DecodeUtf16Scalar();
        Utf16Scalar second = codec.DecodeUtf16Scalar();
        Utf16Scalar end = codec.DecodeUtf16Scalar();

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
    public void DecodesSurrogatePairsAcrossOneByteStreamReads()
    {
        byte[] bytes = EncodeCodeUnits([0xD83D, 0xDE00, 0x0041], Utf16ByteOrder.LittleEndian, ByteOrderMarkKind.Utf16LittleEndian);
        OneByteStream stream = new OneByteStream(bytes);
        StreamByteSource source = new StreamByteSource(stream, false, 4);

        string decoded = new Utf16Codec(source, Utf16ByteOrder.BigEndian).DecodeUtf16Text();

        Assert.That(decoded, Is.EqualTo(char.ConvertFromUtf32(0x1F600) + "A"));
        Assert.That(source.SourceBytePosition(), Is.EqualTo(6));
        Assert.That(stream.ReadCalls, Is.GreaterThan(4));
    }

    [Test]
    public void RejectsUnpairedAndMismatchedSurrogates()
    {
        AssertInvalid([0xDC, 0x00], Utf16ByteOrder.BigEndian, "Unpaired UTF-16 low surrogate", 0);
        AssertInvalid([0xDF, 0xFF], Utf16ByteOrder.BigEndian, "Unpaired UTF-16 low surrogate", 0);
        AssertInvalid([0xD8, 0x00], Utf16ByteOrder.BigEndian, "Truncated UTF-16 surrogate pair", 2);
        AssertInvalid([0xDB, 0xFF], Utf16ByteOrder.BigEndian, "Truncated UTF-16 surrogate pair", 2);
        AssertInvalid([0xD8, 0x00, 0x00, 0x41], Utf16ByteOrder.BigEndian, "Invalid UTF-16 low surrogate", 2);
        AssertInvalid([0xD8, 0x00, 0xD8, 0x01], Utf16ByteOrder.BigEndian, "Invalid UTF-16 low surrogate", 2);
    }

    [Test]
    public void RejectsOddByteInputAtTheLogicalCodeUnitOffset()
    {
        AssertInvalid([0x00], Utf16ByteOrder.BigEndian, "Odd number of bytes", 0);
        AssertInvalid([0x41, 0x00, 0x42], Utf16ByteOrder.LittleEndian, "Odd number of bytes", 2);
        AssertInvalid([0xFE, 0xFF, 0x00, 0x41, 0x00], Utf16ByteOrder.LittleEndian, "Odd number of bytes", 2);
        AssertInvalid([0xD8, 0x00, 0xDC], Utf16ByteOrder.BigEndian, "Odd number of bytes", 2);
    }

    [Test]
    public void RejectsIncompatibleBomAndInvalidFallbackOrder()
    {
        Assert.That(() => new Utf16Codec(new MemoryByteSource(new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }), Utf16ByteOrder.BigEndian), Throws.TypeOf<DecoderFallbackException>());
        Assert.That(() => new Utf16Codec(new MemoryByteSource(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x41 }), Utf16ByteOrder.BigEndian), Throws.TypeOf<DecoderFallbackException>());
        Assert.That(() => new Utf16Codec(new MemoryByteSource(new byte[0]), (Utf16ByteOrder)2), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static string DecodeBytes(byte[] bytes, Utf16ByteOrder fallbackByteOrder)
    {
        MemoryByteSource source = new MemoryByteSource(bytes);
        return new Utf16Codec(source, fallbackByteOrder).DecodeUtf16Text();
    }

    private static void AssertInvalid(byte[] bytes, Utf16ByteOrder byteOrder, string message, int offset)
    {
        DecoderFallbackException exception = Assert.Throws<DecoderFallbackException>(() => DecodeBytes(bytes, byteOrder))!;
        Assert.That(exception.Message, Does.Contain(message));
        Assert.That(exception.Message, Does.Contain("byte offset " + offset));
    }

    private static byte[] EncodeCodeUnits(int[] codeUnits, Utf16ByteOrder byteOrder, ByteOrderMarkKind byteOrderMark)
    {
        List<byte> bytes = [];
        if (byteOrderMark == ByteOrderMarkKind.Utf16BigEndian)
        {
            bytes.Add(0xFE);
            bytes.Add(0xFF);
        }
        else if (byteOrderMark == ByteOrderMarkKind.Utf16LittleEndian)
        {
            bytes.Add(0xFF);
            bytes.Add(0xFE);
        }

        foreach (int codeUnit in codeUnits)
        {
            byte high = (byte)(codeUnit / 0x100);
            byte low = (byte)(codeUnit % 0x100);
            if (byteOrder == Utf16ByteOrder.BigEndian)
            {
                bytes.Add(high);
                bytes.Add(low);
            }
            else
            {
                bytes.Add(low);
                bytes.Add(high);
            }
        }

        return bytes.ToArray();
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
