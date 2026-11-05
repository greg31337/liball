using System.Numerics;

namespace random.Tests;

public class WideMathTests
{
    [Test]
    public void Multiply64To128PreservesBothHalves()
    {
        UInt128 product = WideMath.Multiply64To128(ulong.MaxValue, ulong.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(WideMath.Low64(product), Is.EqualTo(1UL));
            Assert.That(WideMath.High64(product), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(WideMath.MultiplyLow64(0xFEDCBA9876543210UL, 0x123456789ABCDEF0UL), Is.EqualTo(0x236D88FE5618CF00UL));
            Assert.That(WideMath.MultiplyHigh64(ulong.MaxValue, ulong.MaxValue), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(WideMath.Multiply64To128(0UL, ulong.MaxValue), Is.EqualTo((UInt128)0));
        });
    }

    [Test]
    public void CarryHelpersHandleBoundarySums()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WideMath.AddLow64(0UL, 0UL, 0UL), Is.EqualTo(0UL));
            Assert.That(WideMath.AddCarry64(0UL, 0UL, 0UL), Is.EqualTo(0UL));
            Assert.That(WideMath.AddLow64(ulong.MaxValue, 0UL, 1UL), Is.EqualTo(0UL));
            Assert.That(WideMath.AddCarry64(ulong.MaxValue, 0UL, 1UL), Is.EqualTo(1UL));
            Assert.That(WideMath.AddLow64(ulong.MaxValue, ulong.MaxValue, 1UL), Is.EqualTo(ulong.MaxValue));
            Assert.That(WideMath.AddCarry64(ulong.MaxValue, ulong.MaxValue, 1UL), Is.EqualTo(1UL));
        });

        RandomArgumentException error = Assert.Throws<RandomArgumentException>(
            () => WideMath.AddWithCarry(0UL, 0UL, 2UL));
        Assert.That(error.ArgumentName(), Is.EqualTo("carryIn"));
    }

    [Test]
    public void PartsRoundTripAcrossFullWidth()
    {
        UInt128 value = WideMath.FromParts(0xFEDCBA9876543210UL, 0x0123456789ABCDEFUL);

        Assert.Multiple(() =>
        {
            Assert.That(WideMath.High64(value), Is.EqualTo(0xFEDCBA9876543210UL));
            Assert.That(WideMath.Low64(value), Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(WideMath.FromParts(0UL, 0UL), Is.EqualTo((UInt128)0));
            Assert.That(WideMath.FromParts(ulong.MaxValue, ulong.MaxValue), Is.EqualTo(UInt128.MaxValue));
        });
    }

    [Test]
    public void ModularAdditionWrapsWithoutCheckedOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WideMath.AddModulo64(ulong.MaxValue, 1UL), Is.EqualTo(0UL));
            Assert.That(WideMath.AddModulo64(ulong.MaxValue, ulong.MaxValue), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(WideMath.AddModulo128(UInt128.MaxValue, (UInt128)1), Is.EqualTo((UInt128)0));
            Assert.That(WideMath.AddModulo128(UInt128.MaxValue, UInt128.MaxValue), Is.EqualTo(UInt128.MaxValue - (UInt128)1));
        });
    }

    [Test]
    public void ModularMultiplicationMatchesBigIntegerReference()
    {
        UInt128[] values =
        [
            (UInt128)0,
            (UInt128)1,
            (UInt128)ulong.MaxValue,
            WideMath.FromParts(1UL, 0UL),
            WideMath.FromParts(0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL),
            UInt128.MaxValue
        ];

        foreach (UInt128 left in values)
        {
            foreach (UInt128 right in values)
            {
                UInt128 actual = WideMath.MultiplyModulo128(left, right);
                Assert.That(actual, Is.EqualTo(ReferenceMultiply(left, right)), $"left={left}, right={right}");
            }
        }

        Assert.That(WideMath.MultiplyModulo128(UInt128.MaxValue, UInt128.MaxValue), Is.EqualTo((UInt128)1));
    }

    [Test]
    public void ModularMultiplyAddMatchesBigIntegerReference()
    {
        UInt128 value = WideMath.FromParts(0xDEADBEEF01234567UL, 0x89ABCDEF76543210UL);
        UInt128 multiplier = WideMath.FromParts(0x2360ED051FC65DA4UL, 0x4385DF649FCCF645UL);
        UInt128 increment = WideMath.FromParts(0x5851F42D4C957F2DUL, 0x14057B7EF767814FUL);

        UInt128 actual = WideMath.MultiplyAddModulo128(value, multiplier, increment);
        UInt128 expected = ReferenceMultiplyAdd(value, multiplier, increment);

        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(
            WideMath.MultiplyAddModulo128(UInt128.MaxValue, UInt128.MaxValue, UInt128.MaxValue),
            Is.EqualTo((UInt128)0));
    }

    static UInt128 ReferenceMultiply(UInt128 left, UInt128 right)
    {
        BigInteger modulus = BigInteger.One << 128;
        BigInteger result = ((BigInteger)left * (BigInteger)right) % modulus;
        return (UInt128)result;
    }

    static UInt128 ReferenceMultiplyAdd(UInt128 value, UInt128 multiplier, UInt128 increment)
    {
        BigInteger modulus = BigInteger.One << 128;
        BigInteger result = ((BigInteger)value * (BigInteger)multiplier + (BigInteger)increment) % modulus;
        return (UInt128)result;
    }
}
