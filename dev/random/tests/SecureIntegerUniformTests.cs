using System.Numerics;

namespace random.Tests;

public class SecureIntegerUniformTests
{
    [Test]
    public void BoundedUnsignedValuesMatchIndependentMultiplyHighMapping()
    {
        SecureRandomGenerator actual32 = Create();
        SecureRandomGenerator reference32 = Create();
        SecureRandomGenerator actual64 = Create();
        SecureRandomGenerator reference64 = Create();
        uint[] bounds32 = [1U, 3U, 10U, 2147483649U, uint.MaxValue];
        ulong[] bounds64 = [1UL, 3UL, 10UL, 9223372036854775809UL, ulong.MaxValue];

        foreach (uint bound in bounds32)
        {
            Assert.That(actual32.NextUInt32Bounded(bound), Is.EqualTo(ReferenceUInt32(reference32, bound)));
        }

        foreach (ulong bound in bounds64)
        {
            Assert.That(actual64.NextUInt64Bounded(bound), Is.EqualTo(ReferenceUInt64(reference64, bound)));
        }
    }

    [Test]
    public void SignedAndUnsignedRangesHandleDomainEdges()
    {
        SecureRandomGenerator generator = Create();
        for (int index = 0; index < 1000; index++)
        {
            uint unsigned32 = generator.NextUInt32InRange(17U, 4000000000U);
            int signed32 = generator.NextInt32InRange(-2000000000, 2000000000);
            ulong unsigned64 = generator.NextUInt64InRange(17UL, 18000000000000000000UL);
            long signed64 = generator.NextInt64InRange(-9000000000000000000L, 9000000000000000000L);
            Assert.Multiple(() =>
            {
                Assert.That(unsigned32, Is.InRange(17U, 3999999999U));
                Assert.That(signed32, Is.InRange(-2000000000, 1999999999));
                Assert.That(unsigned64, Is.InRange(17UL, 17999999999999999999UL));
                Assert.That(signed64, Is.InRange(-9000000000000000000L, 8999999999999999999L));
            });
        }
    }

    [Test]
    public void SingleValueDomainsAndBooleanUseExpectedConsumption()
    {
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();

        Assert.Multiple(() =>
        {
            Assert.That(actual.NextUInt32Bounded(1U), Is.EqualTo(0U));
            Assert.That(actual.NextUInt32InRange(9U, 10U), Is.EqualTo(9U));
            Assert.That(actual.NextInt32Bounded(1), Is.EqualTo(0));
            Assert.That(actual.NextInt32InRange(-4, -3), Is.EqualTo(-4));
            Assert.That(actual.NextUInt64Bounded(1UL), Is.EqualTo(0UL));
            Assert.That(actual.NextUInt64InRange(ulong.MaxValue - 1UL, ulong.MaxValue), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(actual.NextInt64Bounded(1L), Is.EqualTo(0L));
            Assert.That(actual.NextInt64InRange(long.MinValue, long.MinValue + 1L), Is.EqualTo(long.MinValue));
            Assert.That(actual.NextBigInteger(BigInteger.One), Is.EqualTo(BigInteger.Zero));
            Assert.That(actual.NextBigIntegerInRange(new BigInteger(-9), new BigInteger(-8)), Is.EqualTo(new BigInteger(-9)));
        });

        Assert.That(actual.NextBoolean(), Is.EqualTo(reference.NextUInt32Bounded(2U) != 0U));
        Assert.That(actual.NextUInt64(), Is.EqualTo(reference.NextUInt64()));
    }

    [Test]
    public void InvalidBoundsFailBeforeRequestingBytes()
    {
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => actual.NextUInt32Bounded(0U)), "bound" },
            new object[] { (TestDelegate)(() => actual.NextUInt32InRange(5U, 5U)), "maximum" },
            new object[] { (TestDelegate)(() => actual.NextInt32Bounded(0)), "bound" },
            new object[] { (TestDelegate)(() => actual.NextInt32InRange(5, 4)), "maximum" },
            new object[] { (TestDelegate)(() => actual.NextUInt64Bounded(0UL)), "bound" },
            new object[] { (TestDelegate)(() => actual.NextUInt64InRange(5UL, 5UL)), "maximum" },
            new object[] { (TestDelegate)(() => actual.NextInt64Bounded(-1L)), "bound" },
            new object[] { (TestDelegate)(() => actual.NextInt64InRange(5L, 4L)), "maximum" },
            new object[] { (TestDelegate)(() => actual.NextBigInteger(BigInteger.Zero)), "bound" },
            new object[] { (TestDelegate)(() => actual.NextBigIntegerInRange(BigInteger.One, BigInteger.One)), "maximum" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.That(actual.NextUInt64(), Is.EqualTo(reference.NextUInt64()));
    }

    [Test]
    public void BigIntegerValuesRemainInsideVeryLargeAndNegativeRanges()
    {
        SecureRandomGenerator generator = Create();
        BigInteger bound = (BigInteger.One << 521) - new BigInteger(17);
        BigInteger minimum = -(BigInteger.One << 400);
        BigInteger maximum = (BigInteger.One << 500) + new BigInteger(123);

        for (int index = 0; index < 100; index++)
        {
            BigInteger bounded = generator.NextBigInteger(bound);
            BigInteger ranged = generator.NextBigIntegerInRange(minimum, maximum);
            Assert.Multiple(() =>
            {
                Assert.That(bounded, Is.GreaterThanOrEqualTo(BigInteger.Zero));
                Assert.That(bounded, Is.LessThan(bound));
                Assert.That(ranged, Is.GreaterThanOrEqualTo(minimum));
                Assert.That(ranged, Is.LessThan(maximum));
            });
        }
    }

    static uint ReferenceUInt32(SecureRandomGenerator generator, uint bound)
    {
        if (bound == 1U)
        {
            return 0U;
        }

        uint threshold = (uint)(0x100000000UL % bound);
        while (true)
        {
            uint sample = generator.NextUInt32();
            ulong product = (ulong)sample * bound;
            if ((uint)product >= threshold)
            {
                return (uint)(product >> 32);
            }
        }
    }

    static ulong ReferenceUInt64(SecureRandomGenerator generator, ulong bound)
    {
        if (bound == 1UL)
        {
            return 0UL;
        }

        UInt128 domain = (UInt128)ulong.MaxValue + (UInt128)1;
        ulong threshold = (ulong)(domain % bound);
        while (true)
        {
            ulong sample = generator.NextUInt64();
            UInt128 product = (UInt128)sample * bound;
            if ((ulong)product >= threshold)
            {
                return (ulong)(product >> 64);
            }
        }
    }

    static SecureRandomGenerator Create()
    {
        return SecureRandomGenerator.CreateHmacDrbg(Sequence(0x00, 32), Sequence(0x20, 16), Sequence(0x30, 11));
    }

    static byte[] Sequence(int start, int length)
    {
        byte[] result = new byte[length];
        for (int index = 0; index < length; index++)
        {
            result[index] = (byte)(start + index);
        }

        return result;
    }
}
