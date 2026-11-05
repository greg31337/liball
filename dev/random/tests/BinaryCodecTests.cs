namespace random.Tests;

public class BinaryCodecTests
{
    [Test]
    public void ArrayWritesUseCanonicalLittleEndianOrderAndReturnOffsets()
    {
        byte[] buffer = new byte[31];
        int offset = 0;

        offset = BinaryCodec.WriteByte(buffer, offset, 0xAB);
        offset = BinaryCodec.WriteUInt16(buffer, offset, 0x1234);
        offset = BinaryCodec.WriteUInt32(buffer, offset, 0x89ABCDEFU);
        offset = BinaryCodec.WriteUInt64(buffer, offset, 0x0123456789ABCDEFUL);
        offset = BinaryCodec.WriteUInt128(
            buffer,
            offset,
            WideMath.FromParts(0x0011223344556677UL, 0x8899AABBCCDDEEFFUL));

        Assert.That(offset, Is.EqualTo(buffer.Length));
        Assert.That(buffer, Is.EqualTo(new byte[]
        {
            0xAB,
            0x34, 0x12,
            0xEF, 0xCD, 0xAB, 0x89,
            0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01,
            0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x99, 0x88,
            0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, 0x00
        }));
    }

    [Test]
    public void ArrayReadsRecoverEveryPrimitive()
    {
        byte[] buffer =
        [
            0xAB,
            0x34, 0x12,
            0xEF, 0xCD, 0xAB, 0x89,
            0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01,
            0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x99, 0x88,
            0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, 0x00
        ];

        Assert.Multiple(() =>
        {
            Assert.That(BinaryCodec.ReadByte(buffer, 0), Is.EqualTo(0xAB));
            Assert.That(BinaryCodec.ReadUInt16(buffer, 1), Is.EqualTo(0x1234));
            Assert.That(BinaryCodec.ReadUInt32(buffer, 3), Is.EqualTo(0x89ABCDEFU));
            Assert.That(BinaryCodec.ReadUInt64(buffer, 7), Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(
                BinaryCodec.ReadUInt128(buffer, 15),
                Is.EqualTo(WideMath.FromParts(0x0011223344556677UL, 0x8899AABBCCDDEEFFUL)));
        });
    }

    [Test]
    public void SpanOverloadsUseOffsetsRelativeToTheProvidedSpan()
    {
        byte[] storage = Enumerable.Repeat((byte)0xCC, 10).ToArray();
        Span<byte> destination = storage.AsSpan(2, 6);

        int offset = BinaryCodec.WriteUInt32(destination, 1, 0x12345678U);
        ReadOnlySpan<byte> source = storage.AsSpan(2, 6);
        uint decoded = BinaryCodec.ReadUInt32(source, 1);

        Assert.Multiple(() =>
        {
            Assert.That(offset, Is.EqualTo(5));
            Assert.That(decoded, Is.EqualTo(0x12345678U));
            Assert.That(storage, Is.EqualTo(new byte[] { 0xCC, 0xCC, 0xCC, 0x78, 0x56, 0x34, 0x12, 0xCC, 0xCC, 0xCC }));
        });
    }

    [Test]
    public void ByteCopiesAreIndependentAndAllowEmptyRangesAtTheEnd()
    {
        byte[] destination = [0, 0, 0, 0, 0, 0];
        int offset = BinaryCodec.WriteBytes(destination, 2, new byte[] { 1, 2, 3 });
        byte[] copy = BinaryCodec.ReadBytes(destination, 2, 3);
        copy[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(offset, Is.EqualTo(5));
            Assert.That(destination, Is.EqualTo(new byte[] { 0, 0, 1, 2, 3, 0 }));
            Assert.That(BinaryCodec.ReadBytes(destination, destination.Length, 0), Is.Empty);
            Assert.That(BinaryCodec.WriteBytes(destination.AsSpan(), destination.Length, ReadOnlySpan<byte>.Empty), Is.EqualTo(destination.Length));
        });
    }

    [Test]
    public void InvalidReadsReportOffsetLengthOrSource()
    {
        byte[] source = new byte[8];

        AssertArgument("offset", () => BinaryCodec.ReadUInt32(source, -1));
        AssertArgument("source", () => BinaryCodec.ReadUInt64(source, 1));
        AssertArgument("source", () => BinaryCodec.ReadByte(source, int.MaxValue));
        AssertArgument("length", () => BinaryCodec.ReadBytes(source, 0, -1));
        AssertArgument("source", () => BinaryCodec.ReadBytes(source, int.MaxValue, int.MaxValue));
        AssertArgument("source", () => BinaryCodec.ReadUInt32((byte[])NullReference.Value<byte[]>(), 0));
    }

    [Test]
    public void InvalidWritesDoNotPartiallyChangeDestination()
    {
        byte[] destination = Enumerable.Repeat((byte)0xA5, 8).ToArray();
        byte[] expected = (byte[])destination.Clone();

        AssertArgument("destination", () => BinaryCodec.WriteUInt64(destination, 1, ulong.MaxValue));
        AssertArgument("offset", () => BinaryCodec.WriteUInt32(destination, -1, uint.MaxValue));
        AssertArgument("destination", () => BinaryCodec.WriteBytes(destination, 7, new byte[] { 1, 2 }));
        AssertArgument("destination", () => BinaryCodec.WriteUInt16((byte[])NullReference.Value<byte[]>(), 0, 1));

        Assert.That(destination, Is.EqualTo(expected));
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
