using System.Numerics;
using fake;

namespace fake.Tests;

[TestFixture]
public class IntegerRandomTests
{
    [Test]
    public void FullRangeMethodsAreDeterministicForEqualSeeds()
    {
        FakeRandom first = Fake.Create(8675309).Random();
        FakeRandom second = Fake.Create(8675309).Random();

        object[] firstValues = new object[]
        {
            first.SByte(), first.Byte(), first.Short(), first.UShort(),
            first.Int(), first.UInt(), first.Long(), first.ULong()
        };
        object[] secondValues = new object[]
        {
            second.SByte(), second.Byte(), second.Short(), second.UShort(),
            second.Int(), second.UInt(), second.Long(), second.ULong()
        };

        Assert.That(secondValues, Is.EqualTo(firstValues));
    }

    [Test]
    public void ExclusiveRangesCoverEveryFixedWidth()
    {
        FakeRandom random = Fake.Create(17).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(random.SByteFromMinimumAndMaximum(-100, 100), Is.InRange((sbyte)-100, (sbyte)99));
            Assert.That(random.ByteFromMinimumAndMaximum(20, 220), Is.InRange((byte)20, (byte)219));
            Assert.That(random.ShortFromMinimumAndMaximum(-30000, 30000), Is.InRange((short)-30000, (short)29999));
            Assert.That(random.UShortFromMinimumAndMaximum(1000, 65000), Is.InRange((ushort)1000, (ushort)64999));
            Assert.That(random.IntFromMinimumAndMaximum(-2000000000, 2000000000), Is.InRange(-2000000000, 1999999999));
            Assert.That(random.UIntFromMinimumAndMaximum(100U, 4000000000U), Is.InRange(100U, 3999999999U));
            Assert.That(random.LongFromMinimumAndMaximum(long.MinValue, long.MaxValue), Is.InRange(long.MinValue, long.MaxValue - 1L));
            Assert.That(random.ULongFromMinimumAndMaximum(0UL, ulong.MaxValue), Is.LessThan(ulong.MaxValue));
        }
    }

    [Test]
    public void MaximumOnlyRangesCoverEveryFixedWidth()
    {
        FakeRandom random = Fake.Create(18).Random();

        for (int index = 0; index < 200; index++)
        {
            Assert.That(random.SByteFromMaximumExclusive(100), Is.InRange((sbyte)0, (sbyte)99));
            Assert.That(random.ByteFromMaximumExclusive(200), Is.InRange((byte)0, (byte)199));
            Assert.That(random.ShortFromMaximumExclusive(30000), Is.InRange((short)0, (short)29999));
            Assert.That(random.UShortFromMaximumExclusive(60000), Is.InRange((ushort)0, (ushort)59999));
            Assert.That(random.IntFromMaximumExclusive(2000000000), Is.InRange(0, 1999999999));
            Assert.That(random.UIntFromMaximumExclusive(4000000000U), Is.InRange(0U, 3999999999U));
            Assert.That(random.LongFromMaximumExclusive(long.MaxValue), Is.InRange(0L, long.MaxValue - 1L));
            Assert.That(random.ULongFromMaximumExclusive(ulong.MaxValue), Is.LessThan(ulong.MaxValue));
        }
    }

    [Test]
    public void InclusiveSingletonRangesReturnTheOnlyValueWithoutAdvancing()
    {
        FakeRandom random = Fake.Create(19).Random();
        FakeState before = random.Snapshot();

        Assert.That(random.SByteFromMinimumAndMaximumAndMaximumInclusive(-7, -7, true), Is.EqualTo(-7));
        Assert.That(random.ByteFromMinimumAndMaximumAndMaximumInclusive(7, 7, true), Is.EqualTo(7));
        Assert.That(random.ShortFromMinimumAndMaximumAndMaximumInclusive(-700, -700, true), Is.EqualTo(-700));
        Assert.That(random.UShortFromMinimumAndMaximumAndMaximumInclusive(700, 700, true), Is.EqualTo(700));
        Assert.That(random.IntFromMinimumAndMaximumAndMaximumInclusive(-70000, -70000, true), Is.EqualTo(-70000));
        Assert.That(random.UIntFromMinimumAndMaximumAndMaximumInclusive(70000, 70000, true), Is.EqualTo(70000));
        Assert.That(random.LongFromMinimumAndMaximumAndMaximumInclusive(-7000000000L, -7000000000L, true), Is.EqualTo(-7000000000L));
        Assert.That(random.ULongFromMinimumAndMaximumAndMaximumInclusive(7000000000UL, 7000000000UL, true), Is.EqualTo(7000000000UL));
        Assert.That(random.BigIntegerFromMinimumAndMaximumAndMaximumInclusive(new BigInteger(-700), new BigInteger(-700), true), Is.EqualTo(new BigInteger(-700)));

        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void InclusiveFullRangesDoNotOverflow()
    {
        FakeRandom random = Fake.Create(20).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(random.SByteFromMinimumAndMaximumAndMaximumInclusive(sbyte.MinValue, sbyte.MaxValue, true), Is.InRange(sbyte.MinValue, sbyte.MaxValue));
            Assert.That(random.ByteFromMinimumAndMaximumAndMaximumInclusive(byte.MinValue, byte.MaxValue, true), Is.InRange(byte.MinValue, byte.MaxValue));
            Assert.That(random.ShortFromMinimumAndMaximumAndMaximumInclusive(short.MinValue, short.MaxValue, true), Is.InRange(short.MinValue, short.MaxValue));
            Assert.That(random.UShortFromMinimumAndMaximumAndMaximumInclusive(ushort.MinValue, ushort.MaxValue, true), Is.InRange(ushort.MinValue, ushort.MaxValue));
            Assert.That(random.IntFromMinimumAndMaximumAndMaximumInclusive(int.MinValue, int.MaxValue, true), Is.InRange(int.MinValue, int.MaxValue));
            Assert.That(random.UIntFromMinimumAndMaximumAndMaximumInclusive(uint.MinValue, uint.MaxValue, true), Is.InRange(uint.MinValue, uint.MaxValue));
            Assert.That(random.LongFromMinimumAndMaximumAndMaximumInclusive(long.MinValue, long.MaxValue, true), Is.InRange(long.MinValue, long.MaxValue));
            Assert.That(random.ULongFromMinimumAndMaximumAndMaximumInclusive(ulong.MinValue, ulong.MaxValue, true), Is.InRange(ulong.MinValue, ulong.MaxValue));
        }
    }

    [Test]
    public void InclusiveFlagControlsWhetherEqualBoundsAreValid()
    {
        FakeRandom random = Fake.Create().Random();

        Assert.That(random.IntFromMinimumAndMaximumAndMaximumInclusive(4, 4, true), Is.EqualTo(4));
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.IntFromMinimumAndMaximumAndMaximumInclusive(4, 4, false));

        Assert.That(exception.ParamName, Is.EqualTo("maximum"));
    }

    [Test]
    public void MaximumOnlyMethodsRejectNonpositiveMaximums()
    {
        FakeRandom random = Fake.Create().Random();

        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.SByteFromMaximumExclusive(0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.ByteFromMaximumExclusive(0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.ShortFromMaximumExclusive(0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.UShortFromMaximumExclusive(0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.IntFromMaximumExclusive(0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.UIntFromMaximumExclusive(0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.LongFromMaximumExclusive(0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.ULongFromMaximumExclusive(0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.BigInteger(BigInteger.Zero)));
    }

    [Test]
    public void ExclusiveMethodsRejectEmptyAndReversedRanges()
    {
        FakeRandom random = Fake.Create().Random();

        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.SByteFromMinimumAndMaximum(1, 1)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.ByteFromMinimumAndMaximum(2, 1)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.ShortFromMinimumAndMaximum(1, 1)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.UShortFromMinimumAndMaximum(2, 1)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.IntFromMinimumAndMaximum(1, 1)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.UIntFromMinimumAndMaximum(2, 1)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.LongFromMinimumAndMaximum(1, 1)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.ULongFromMinimumAndMaximum(2, 1)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.BigIntegerFromMinimumAndMaximum(new BigInteger(2), BigInteger.One)));
    }

    [Test]
    public void ThresholdRejectionDiscardsBiasedTailValues()
    {
        const ulong bound = 9223372036854775809UL;
        FakeRandom random = Fake.Create().Random();
        FakeRandom expectedPosition = Fake.Create().Random();
        random.ReseedFromSeedUsingString("naïve/東京");
        expectedPosition.ReseedFromSeedUsingString("naïve/東京");

        ulong actual = random.ULongFromMaximumExclusive(bound);
        expectedPosition.ULong();
        expectedPosition.ULong();

        Assert.That(actual, Is.EqualTo(219473929315991968UL));
        AssertStateEqual(random.Snapshot(), expectedPosition.Snapshot());
    }

    [Test]
    public void SmallBoundDistributionHasNoLargeBucketSkew()
    {
        FakeRandom random = Fake.Create(123456).Random();
        int[] counts = new int[10];

        for (int index = 0; index < 100000; index++)
        {
            counts[random.IntFromMaximumExclusive(10)]++;
        }

        Assert.That(counts, Has.All.InRange(9500, 10500));
    }

    [Test]
    public void BigIntegerSupportsHugeSignedIntervals()
    {
        FakeRandom random = Fake.Create(91).Random();
        BigInteger magnitude = BigInteger.One << 521;
        BigInteger minimum = -magnitude - 12345;
        BigInteger maximum = magnitude + 67890;

        for (int index = 0; index < 300; index++)
        {
            BigInteger value = random.BigIntegerFromMinimumAndMaximum(minimum, maximum);
            Assert.That(value, Is.GreaterThanOrEqualTo(minimum));
            Assert.That(value, Is.LessThan(maximum));
        }
    }

    [Test]
    public void BigIntegerMaximumOnlyRangeIsNonnegativeAndExclusive()
    {
        FakeRandom random = Fake.Create(92).Random();
        BigInteger maximum = (BigInteger.One << 257) + 99;

        for (int index = 0; index < 200; index++)
        {
            BigInteger value = random.BigInteger(maximum);
            Assert.That(value, Is.GreaterThanOrEqualTo(BigInteger.Zero));
            Assert.That(value, Is.LessThan(maximum));
        }
    }

    [Test]
    public void BigIntegerSamplingRejectsCandidatesOutsideRange()
    {
        FakeRandom random = Fake.Create(0).Random();
        FakeRandom expectedPosition = Fake.Create(0).Random();

        BigInteger actual = random.BigInteger(new BigInteger(129));
        expectedPosition.ULong();
        expectedPosition.ULong();

        Assert.That(actual, Is.EqualTo(new BigInteger(42)));
        AssertStateEqual(random.Snapshot(), expectedPosition.Snapshot());
    }

    [Test]
    public void BigIntegerHonorsConfiguredOutputLimit()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumOutputSize = 4;
        FakeRandom random = Fake.Create(options).Random();
        BigInteger maximum = BigInteger.One << 40;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.BigInteger(maximum));

        Assert.That(exception.ParamName, Is.EqualTo("maximum"));
    }

    [Test]
    public void CryptographicBoundedGenerationHonorsRanges()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        FakeRandom random = Fake.Create(options).Random();
        BigInteger bigMaximum = BigInteger.One << 130;

        for (int index = 0; index < 100; index++)
        {
            Assert.That(random.SByteFromMinimumAndMaximum(-9, 10), Is.InRange((sbyte)-9, (sbyte)9));
            Assert.That(random.ByteFromMinimumAndMaximum(10, 20), Is.InRange((byte)10, (byte)19));
            Assert.That(random.ShortFromMinimumAndMaximum(-99, 100), Is.InRange((short)-99, (short)99));
            Assert.That(random.UShortFromMinimumAndMaximum(100, 200), Is.InRange((ushort)100, (ushort)199));
            Assert.That(random.IntFromMinimumAndMaximum(-999, 1000), Is.InRange(-999, 999));
            Assert.That(random.UIntFromMinimumAndMaximum(1000, 2000), Is.InRange(1000U, 1999U));
            Assert.That(random.LongFromMinimumAndMaximum(-9999, 10000), Is.InRange(-9999L, 9999L));
            Assert.That(random.ULongFromMinimumAndMaximum(10000, 20000), Is.InRange(10000UL, 19999UL));
            Assert.That(random.BigInteger(bigMaximum), Is.InRange(BigInteger.Zero, bigMaximum - BigInteger.One));
        }
    }

    [Test]
    public void SnapshotRestoreReplaysMixedBoundedCalls()
    {
        FakeRandom random = Fake.Create(314159).Random();
        FakeState state = random.Snapshot();

        object[] expected = MixedValues(random);
        random.Restore(state);

        Assert.That(MixedValues(random), Is.EqualTo(expected));
    }

    static object[] MixedValues(FakeRandom random)
    {
        return new object[]
        {
            random.SByteFromMinimumAndMaximum(-12, 13),
            random.ByteFromMinimumAndMaximum(7, 201),
            random.ShortFromMinimumAndMaximum(-1234, 5678),
            random.UShortFromMinimumAndMaximum(55, 60000),
            random.IntFromMinimumAndMaximum(-123456, 987654),
            random.UIntFromMinimumAndMaximum(123U, 4000000000U),
            random.LongFromMinimumAndMaximum(long.MinValue, long.MaxValue),
            random.ULongFromMaximumExclusive(ulong.MaxValue),
            random.BigIntegerFromMinimumAndMaximum(-(BigInteger.One << 90), BigInteger.One << 100)
        };
    }

    static void AssertMaximumException(ArgumentOutOfRangeException exception)
    {
        Assert.That(exception.ParamName, Is.EqualTo("maximumExclusive"));
    }

    static void AssertRangeException(ArgumentOutOfRangeException exception)
    {
        Assert.That(exception.ParamName, Is.EqualTo("maximum"));
    }

    static void AssertStateEqual(FakeState actual, FakeState expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Algorithm, Is.EqualTo(expected.Algorithm));
            Assert.That(actual.InitialSeed, Is.EqualTo(expected.InitialSeed));
            Assert.That(actual.State0, Is.EqualTo(expected.State0));
            Assert.That(actual.State1, Is.EqualTo(expected.State1));
            Assert.That(actual.State2, Is.EqualTo(expected.State2));
            Assert.That(actual.State3, Is.EqualTo(expected.State3));
        });
    }
}
