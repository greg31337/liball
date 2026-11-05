using argon2;

namespace argon2.Tests.Algorithm;

public class ModularArithmeticTests
{
    [Test]
    public void Add64ReturnsOrdinarySums()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ModularArithmetic.Add64(0UL, 0UL), Is.EqualTo(0UL));
            Assert.That(ModularArithmetic.Add64(1UL, 2UL), Is.EqualTo(3UL));
            Assert.That(
                ModularArithmetic.Add64(0x0123456789ABCDEFUL, 0x1111111111111111UL),
                Is.EqualTo(0x123456789ABCDF00UL));
        });
    }

    [Test]
    public void Add64WrapsAtSixtyFourBits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ModularArithmetic.Add64(ulong.MaxValue, 1UL), Is.EqualTo(0UL));
            Assert.That(ModularArithmetic.Add64(ulong.MaxValue, ulong.MaxValue), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(
                ModularArithmetic.Add64(0xFFFFFFFF00000000UL, 0x0000000100000000UL),
                Is.EqualTo(0UL));
            Assert.That(
                ModularArithmetic.Add64(0xFFFFFFFFFFFFFFFFUL, 0x00000000FFFFFFFFUL),
                Is.EqualTo(0x00000000FFFFFFFEUL));
        });
    }

    [Test]
    public void RotateRight64MatchesKnownBitPatterns()
    {
        const ulong Value = 0x0123456789ABCDEFUL;

        Assert.Multiple(() =>
        {
            Assert.That(ModularArithmetic.RotateRight64(Value, 8), Is.EqualTo(0xEF0123456789ABCDUL));
            Assert.That(ModularArithmetic.RotateRight64(Value, 16), Is.EqualTo(0xCDEF0123456789ABUL));
            Assert.That(ModularArithmetic.RotateRight64(Value, 24), Is.EqualTo(0xABCDEF0123456789UL));
            Assert.That(ModularArithmetic.RotateRight64(Value, 32), Is.EqualTo(0x89ABCDEF01234567UL));
            Assert.That(ModularArithmetic.RotateRight64(Value, 63), Is.EqualTo(0x02468ACF13579BDEUL));
        });
    }

    [Test]
    public void RotateRight64NormalizesDistanceModuloWordSize()
    {
        const ulong Value = 0x0123456789ABCDEFUL;

        Assert.Multiple(() =>
        {
            Assert.That(ModularArithmetic.RotateRight64(Value, 0), Is.EqualTo(Value));
            Assert.That(ModularArithmetic.RotateRight64(Value, 64), Is.EqualTo(Value));
            Assert.That(
                ModularArithmetic.RotateRight64(Value, 65),
                Is.EqualTo(ModularArithmetic.RotateRight64(Value, 1)));
            Assert.That(
                ModularArithmetic.RotateRight64(Value, -1),
                Is.EqualTo(ModularArithmetic.RotateRight64(Value, 63)));
        });
    }
}
