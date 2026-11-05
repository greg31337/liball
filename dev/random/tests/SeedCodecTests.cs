namespace random.Tests;

public class SeedCodecTests
{
    [Test]
    public void UInt64EncodingHasStableHeaderAndLittleEndianPayload()
    {
        // Provenance: independent byte layout fixture for the project's RNDSEED1 v1 wire format;
        // the payload follows the little-endian integer representation specified by System.Buffers.Binary.
        byte[] encoded = SeedCodec.EncodeUInt64(0x0123456789ABCDEFUL);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(SeedCodec.HeaderSize + 8));
            Assert.That(encoded.AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[] { 0x52, 0x4E, 0x44, 0x53, 0x45, 0x45, 0x44, 0x31 }));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 8), Is.EqualTo(1U));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 12), Is.EqualTo((uint)SeedDomain.UInt64));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 16), Is.EqualTo(8U));
            Assert.That(BinaryCodec.ReadBytes(encoded, SeedCodec.HeaderSize, 8), Is.EqualTo(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }));
            Assert.That(Convert.ToHexString(encoded), Is.EqualTo("524E445345454431010000000100000008000000EFCDAB8967452301"));
        });
    }

    [Test]
    public void SignedEncodingUsesCanonicalTwosComplementBytes()
    {
        byte[] negativeTwo = SeedCodec.EncodeInt64(-2L);
        byte[] minimum = SeedCodec.EncodeInt64(long.MinValue);

        Assert.Multiple(() =>
        {
            Assert.That(BinaryCodec.ReadUInt32(negativeTwo, 12), Is.EqualTo((uint)SeedDomain.Int64));
            Assert.That(BinaryCodec.ReadBytes(negativeTwo, SeedCodec.HeaderSize, 8), Is.EqualTo(new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }));
            Assert.That(BinaryCodec.ReadBytes(minimum, SeedCodec.HeaderSize, 8), Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0x80 }));
        });
    }

    [Test]
    public void ByteEncodingCopiesInputAndPreservesEveryByte()
    {
        byte[] seed = [0, 1, 127, 128, 255];
        byte[] encoded = SeedCodec.EncodeBytes(seed);
        seed[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(BinaryCodec.ReadUInt32(encoded, 12), Is.EqualTo((uint)SeedDomain.Bytes));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 16), Is.EqualTo(5U));
            Assert.That(BinaryCodec.ReadBytes(encoded, SeedCodec.HeaderSize, 5), Is.EqualTo(new byte[] { 0, 1, 127, 128, 255 }));
        });
    }

    [Test]
    public void TextEncodingUsesExactUtf8WithoutNormalization()
    {
        byte[] encoded = SeedCodec.EncodeText("Aé😀");
        byte[] composed = SeedCodec.EncodeText("é");
        byte[] decomposed = SeedCodec.EncodeText("e\u0301");

        Assert.Multiple(() =>
        {
            Assert.That(BinaryCodec.ReadUInt32(encoded, 12), Is.EqualTo((uint)SeedDomain.TextUtf8));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 16), Is.EqualTo(7U));
            Assert.That(BinaryCodec.ReadBytes(encoded, SeedCodec.HeaderSize, 7), Is.EqualTo(new byte[] { 0x41, 0xC3, 0xA9, 0xF0, 0x9F, 0x98, 0x80 }));
            Assert.That(composed, Is.Not.EqualTo(decomposed));
        });
    }

    [Test]
    public void InvalidUtf16IsRejectedBeforeEncoding()
    {
        RandomArgumentException high = Assert.Throws<RandomArgumentException>(() => SeedCodec.EncodeText("\uD800"));
        RandomArgumentException followedByNonLow = Assert.Throws<RandomArgumentException>(() => SeedCodec.EncodeText("\uD800A"));
        RandomArgumentException low = Assert.Throws<RandomArgumentException>(() => SeedCodec.EncodeText("\uDC00"));

        Assert.That(high.ArgumentName(), Is.EqualTo("seed"));
        Assert.That(followedByNonLow.ArgumentName(), Is.EqualTo("seed"));
        Assert.That(low.ArgumentName(), Is.EqualTo("seed"));
    }

    [Test]
    public void UInt32ArrayEncodingIsCanonicalAndDefensive()
    {
        uint[] seed = [0x01234567U, 0x89ABCDEFU, uint.MaxValue];
        byte[] encoded = SeedCodec.EncodeUInt32Array(seed);
        seed[0] = 0U;

        Assert.Multiple(() =>
        {
            Assert.That(BinaryCodec.ReadUInt32(encoded, 12), Is.EqualTo((uint)SeedDomain.UInt32Array));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 16), Is.EqualTo(12U));
            Assert.That(BinaryCodec.ReadBytes(encoded, SeedCodec.HeaderSize, 12), Is.EqualTo(new byte[]
            {
                0x67, 0x45, 0x23, 0x01,
                0xEF, 0xCD, 0xAB, 0x89,
                0xFF, 0xFF, 0xFF, 0xFF
            }));
        });
    }

    [Test]
    public void TypeDomainsSeparateIdenticalPayloadBytes()
    {
        byte[][] encodings =
        [
            SeedCodec.EncodeUInt64(0UL),
            SeedCodec.EncodeInt64(0L),
            SeedCodec.EncodeBytes(new byte[8]),
            SeedCodec.EncodeText("\0\0\0\0\0\0\0\0"),
            SeedCodec.EncodeUInt32Array([0U, 0U])
        ];

        Assert.That(encodings.Select(Convert.ToHexString).Distinct().Count(), Is.EqualTo(encodings.Length));
        Assert.That(encodings.Select(value => BinaryCodec.ReadUInt32(value, 12)).Distinct().Count(), Is.EqualTo(encodings.Length));
    }

    [Test]
    public void EmptySeedsRemainSeparatedByTheirTypeTags()
    {
        byte[] bytes = SeedCodec.EncodeBytes([]);
        byte[] text = SeedCodec.EncodeText("");
        byte[] integers = SeedCodec.EncodeUInt32Array([]);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Length, Is.EqualTo(SeedCodec.HeaderSize));
            Assert.That(text.Length, Is.EqualTo(SeedCodec.HeaderSize));
            Assert.That(integers.Length, Is.EqualTo(SeedCodec.HeaderSize));
            Assert.That(bytes, Is.Not.EqualTo(text));
            Assert.That(text, Is.Not.EqualTo(integers));
        });
    }

    [Test]
    public void NullReferenceSeedsUseArgumentFailures()
    {
        RandomArgumentException bytes = Assert.Throws<RandomArgumentException>(() => SeedCodec.EncodeBytes(NullReference.Value<byte[]>()));
        RandomArgumentException text = Assert.Throws<RandomArgumentException>(() => SeedCodec.EncodeText(NullReference.Value<string>()));
        RandomArgumentException integers = Assert.Throws<RandomArgumentException>(() => SeedCodec.EncodeUInt32Array(NullReference.Value<uint[]>()));

        Assert.That(bytes.ArgumentName(), Is.EqualTo("seed"));
        Assert.That(text.ArgumentName(), Is.EqualTo("seed"));
        Assert.That(integers.ArgumentName(), Is.EqualTo("seed"));
    }
}
