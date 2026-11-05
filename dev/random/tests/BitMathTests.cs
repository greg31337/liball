namespace random.Tests;

public class BitMathTests
{
    [Test]
    public void RotationsMatchKnownValuesAndNormalizeOffsets()
    {
        const uint value32 = 0x12345678U;
        const ulong value64 = 0x0123456789ABCDEFUL;

        Assert.Multiple(() =>
        {
            Assert.That(BitMath.RotateLeft(value32, 8), Is.EqualTo(0x34567812U));
            Assert.That(BitMath.RotateRight(value32, 8), Is.EqualTo(0x78123456U));
            Assert.That(BitMath.RotateLeft(value32, 32), Is.EqualTo(value32));
            Assert.That(BitMath.RotateRight(value32, -8), Is.EqualTo(BitMath.RotateLeft(value32, 8)));
            Assert.That(BitMath.RotateLeft(value64, 8), Is.EqualTo(0x23456789ABCDEF01UL));
            Assert.That(BitMath.RotateRight(value64, 8), Is.EqualTo(0xEF0123456789ABCDUL));
            Assert.That(BitMath.RotateLeft(value64, 64), Is.EqualTo(value64));
            Assert.That(BitMath.RotateRight(value64, -8), Is.EqualTo(BitMath.RotateLeft(value64, 8)));
        });
    }

    [Test]
    public void BitCountsCoverZeroAndWordBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BitMath.BitCount(0U), Is.EqualTo(0));
            Assert.That(BitMath.BitCount(uint.MaxValue), Is.EqualTo(32));
            Assert.That(BitMath.BitCount(0xF0F00001U), Is.EqualTo(9));
            Assert.That(BitMath.BitCount(0UL), Is.EqualTo(0));
            Assert.That(BitMath.BitCount(ulong.MaxValue), Is.EqualTo(64));
            Assert.That(BitMath.BitCount(0x8000000000000001UL), Is.EqualTo(2));

            Assert.That(BitMath.LeadingZeroCount(0U), Is.EqualTo(32));
            Assert.That(BitMath.LeadingZeroCount(1U), Is.EqualTo(31));
            Assert.That(BitMath.LeadingZeroCount(1UL), Is.EqualTo(63));
            Assert.That(BitMath.TrailingZeroCount(0U), Is.EqualTo(32));
            Assert.That(BitMath.TrailingZeroCount(0x80000000U), Is.EqualTo(31));
            Assert.That(BitMath.TrailingZeroCount(0x8000000000000000UL), Is.EqualTo(63));
        });
    }

    [Test]
    public void LowMasksHandleEmptyPartialAndFullWidths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BitMath.LowMask32(0), Is.EqualTo(0U));
            Assert.That(BitMath.LowMask32(12), Is.EqualTo(0xFFFU));
            Assert.That(BitMath.LowMask32(32), Is.EqualTo(uint.MaxValue));
            Assert.That(BitMath.LowMask64(0), Is.EqualTo(0UL));
            Assert.That(BitMath.LowMask64(36), Is.EqualTo(0xFFFFFFFFFUL));
            Assert.That(BitMath.LowMask64(64), Is.EqualTo(ulong.MaxValue));
            Assert.That(BitMath.KeepLowBits(0xABCDEFFFU, 12), Is.EqualTo(0xFFFU));
            Assert.That(BitMath.KeepLowBits(0xABCDEF0123456789UL, 16), Is.EqualTo(0x6789UL));
        });

        RandomArgumentException low = Assert.Throws<RandomArgumentException>(() => BitMath.LowMask32(-1));
        RandomArgumentException high = Assert.Throws<RandomArgumentException>(() => BitMath.LowMask64(65));
        Assert.That(low.ArgumentName(), Is.EqualTo("bitCount"));
        Assert.That(high.ArgumentName(), Is.EqualTo("bitCount"));
    }

    [Test]
    public void EndianHelpersReverseEverySupportedWidth()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BitMath.ReverseEndianness((ushort)0x1234), Is.EqualTo((ushort)0x3412));
            Assert.That(BitMath.ReverseEndianness(0x12345678U), Is.EqualTo(0x78563412U));
            Assert.That(BitMath.ReverseEndianness(0x0123456789ABCDEFUL), Is.EqualTo(0xEFCDAB8967452301UL));
            Assert.That(BitMath.ReverseEndianness(BitMath.ReverseEndianness(0x0123456789ABCDEFUL)), Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(BitMath.IsLittleEndian(), Is.EqualTo(BitConverter.IsLittleEndian));
        });
    }

    [Test]
    public void ConstantTimeComparisonReturnsContentEquality()
    {
        byte[] value = [0, 1, 2, 3, 4, 255];
        byte[] equal = [0, 1, 2, 3, 4, 255];
        byte[] different = [0, 1, 2, 3, 5, 255];
        byte[] shorter = [0, 1, 2, 3, 4];

        Assert.Multiple(() =>
        {
            Assert.That(BitMath.ConstantTimeEquals(value, value), Is.True);
            Assert.That(BitMath.ConstantTimeEquals(value, equal), Is.True);
            Assert.That(BitMath.ConstantTimeEquals(value, different), Is.False);
            Assert.That(BitMath.ConstantTimeEquals(value, shorter), Is.False);
            Assert.That(BitMath.ConstantTimeEquals([], []), Is.True);
        });
    }
}
