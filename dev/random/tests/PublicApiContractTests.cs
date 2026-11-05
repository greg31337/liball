using System.Numerics;

namespace random.Tests;

public class PublicApiContractTests
{
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;
    const ulong OneOutputSeed = 0xF8364607E9C949BDUL;
    const ulong UInt32MaximumOutputSeed = 0x25114ED53327345AUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void PublicUniformEndpointsFollowTheirDeclaredInclusionPolicies()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(Create(ZeroOutputSeed).NextSingleClosedOpen()), Is.EqualTo(0U));
            Assert.That(PortableMath.SingleBits(Create(UInt32MaximumOutputSeed).NextSingleClosedOpen()), Is.EqualTo(0x3F7FFFFFU));
            Assert.That(Create(ZeroOutputSeed).NextSingleOpenOpen(), Is.InRange(float.Epsilon, Math.BitDecrement(1.0f)));
            Assert.That(PortableMath.SingleBits(Create(UInt32MaximumOutputSeed).NextSingleOpenClosed()), Is.EqualTo(0x3F800000U));
            Assert.That(PortableMath.SingleBits(Create(OneOutputSeed).NextSingleClosedClosed()), Is.EqualTo(0U));
            Assert.That(PortableMath.SingleBits(Create(UInt32MaximumOutputSeed).NextSingleClosedClosed()), Is.EqualTo(0x3F800000U));

            Assert.That(PortableMath.DoubleBits(Create(ZeroOutputSeed).NextDoubleClosedOpen()), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(Create(UInt64MaximumOutputSeed).NextDoubleClosedOpen()), Is.EqualTo(0x3FEFFFFFFFFFFFFFUL));
            Assert.That(Create(ZeroOutputSeed).NextDoubleOpenOpen(), Is.InRange(double.Epsilon, Math.BitDecrement(1.0)));
            Assert.That(PortableMath.DoubleBits(Create(UInt64MaximumOutputSeed).NextDoubleOpenClosed()), Is.EqualTo(0x3FF0000000000000UL));
            Assert.That(PortableMath.DoubleBits(Create(OneOutputSeed).NextDoubleClosedClosed()), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(Create(UInt64MaximumOutputSeed).NextDoubleClosedClosed()), Is.EqualTo(0x3FF0000000000000UL));
        });

        Assert.Multiple(() =>
        {
            Assert.That(Create(ZeroOutputSeed).NextSingleInRange(-2.0f, 5.0f), Is.EqualTo(-2.0f));
            Assert.That(Create(UInt32MaximumOutputSeed).NextSingleInRange(-2.0f, 5.0f), Is.LessThan(5.0f));
            Assert.That(Create(ZeroOutputSeed).NextDoubleInRange(-2.0, 5.0), Is.EqualTo(-2.0));
            Assert.That(Create(UInt64MaximumOutputSeed).NextDoubleInRange(-2.0, 5.0), Is.LessThan(5.0));
            Assert.That(Create(ZeroOutputSeed).NextUInt64InRange(10UL, 18UL), Is.EqualTo(10UL));
            Assert.That(Create(UInt64MaximumOutputSeed).NextUInt64InRange(10UL, 18UL), Is.EqualTo(17UL));
            Assert.That(Create(ZeroOutputSeed).NextInt64InRange(-4L, 4L), Is.EqualTo(-4L));
            Assert.That(Create(UInt64MaximumOutputSeed).NextInt64InRange(-4L, 4L), Is.EqualTo(3L));
        });
    }

    [Test]
    public void InvalidArgumentsPreserveEngineAndBooleanReservoirState()
    {
        RandomGenerator generator = Create(101UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => generator.NextByteBounded(0)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextByteInRange(5, 5)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextUInt32Bounded(0U)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextUInt64Bounded(0UL)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextInt32Bounded(0)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextInt64Bounded(0L)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextBigInteger(BigInteger.Zero)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextSingleInRange(float.NaN, 1.0f)), "minimum" },
            new object[] { (TestDelegate)(() => generator.NextDoubleInRange(2.0, 1.0)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextDecimalInRange(2.0m, 1.0m)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextBits(-1)), "bitCount" },
            new object[] { (TestDelegate)(() => generator.FillBits(Span<ulong>.Empty, 65)), "bitCount" },
            new object[] { (TestDelegate)(() => generator.NextBooleanWithDoubleProbability(double.NaN)), "probability" },
            new object[] { (TestDelegate)(() => generator.NextBytes(-1)), "length" },
            new object[] { (TestDelegate)(() => generator.SpawnMany(-1)), "count" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.Multiple(() =>
            {
                Assert.That(error.GetType(), Is.EqualTo(typeof(RandomArgumentException)));
                Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void PublicFailureModesUseTheirDedicatedExceptionTypes()
    {
        RandomGenerator argumentGenerator = Create(201UL);
        Exception argument = Assert.Throws<RandomArgumentException>(() => argumentGenerator.NextUInt64Bounded(0UL));

        RandomGenerator capabilityGenerator = RandomGenerator.CreateMt19937(202U, RandomMathMode.Portable);
        Exception capability = Assert.Throws<RandomCapabilityException>(() => capabilityGenerator.Advance((UInt128)1));

        RandomGenerator stateGenerator = Create(203UL);
        RandomState incompatible = RandomGenerator.CreateMt19937(203U, RandomMathMode.Portable).ExportState();
        Exception state = Assert.Throws<RandomStateException>(() => stateGenerator.RestoreState(incompatible));

        HammersleySet finiteSet = new HammersleySet(1, 1);
        finiteSet.Next();
        Exception exhausted = Assert.Throws<RandomExhaustedException>(() => finiteSet.Next());

        SecureRandomGenerator secure = SecureRandomGenerator.CreateHmacDrbg(new byte[32], new byte[16], new byte[0]);
        secure.Uninstantiate();
        Exception security = Assert.Throws<RandomSecurityException>(() => secure.NextUInt32());

        Assert.Multiple(() =>
        {
            Assert.That(argument.GetType(), Is.EqualTo(typeof(RandomArgumentException)));
            Assert.That(capability.GetType(), Is.EqualTo(typeof(RandomCapabilityException)));
            Assert.That(state.GetType(), Is.EqualTo(typeof(RandomStateException)));
            Assert.That(exhausted.GetType(), Is.EqualTo(typeof(RandomExhaustedException)));
            Assert.That(security.GetType(), Is.EqualTo(typeof(RandomSecurityException)));
        });
    }

    [Test]
    public void UnsupportedCapabilitiesAreAtomicAcrossEveryEngineFamily()
    {
        RandomGenerator pcg = RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.Portable);
        RandomGenerator xoshiro = RandomGenerator.CreateXoshiro256StarStar(3UL, RandomMathMode.Portable);
        RandomGenerator philox = RandomGenerator.CreatePhilox4x64Ten((UInt128)4, RandomMathMode.Portable);
        RandomGenerator mt = RandomGenerator.CreateMt19937(5U, RandomMathMode.Portable);
        RandomGenerator numpy = RandomGenerator.CreateMt19937Numpy(6U, RandomMathMode.Portable);
        RandomGenerator splitMix = RandomGenerator.CreateSplitMix64(7UL, RandomMathMode.Portable);
        RandomGenerator[] generators = [pcg, xoshiro, philox, mt, numpy, splitMix];
        foreach (RandomGenerator generator in generators)
        {
            generator.NextBoolean();
        }

        AssertUnsupported(pcg, RandomCapability.Jump, pcg.Jump);
        AssertUnsupported(pcg, RandomCapability.LongJump, pcg.LongJump);
        AssertUnsupported(pcg, RandomCapability.Seek, () => pcg.Seek((UInt128)1));
        AssertUnsupported(pcg, RandomCapability.BlockReservation, () => pcg.ReserveBlocks((UInt128)1));
        AssertUnsupported(xoshiro, RandomCapability.Advance, () => xoshiro.Advance((UInt128)1));
        AssertUnsupported(xoshiro, RandomCapability.Seek, () => xoshiro.Seek((UInt128)1));
        AssertUnsupported(xoshiro, RandomCapability.BlockReservation, () => xoshiro.ReserveBlocks((UInt128)1));
        AssertUnsupported(philox, RandomCapability.Advance, () => philox.Advance((UInt128)1));
        AssertUnsupported(philox, RandomCapability.Jump, philox.Jump);
        AssertUnsupported(philox, RandomCapability.LongJump, philox.LongJump);
        AssertUnsupported(mt, RandomCapability.Advance, () => mt.Advance((UInt128)1));
        AssertUnsupported(mt, RandomCapability.Jump, mt.Jump);
        AssertUnsupported(mt, RandomCapability.LongJump, mt.LongJump);
        AssertUnsupported(mt, RandomCapability.Seek, () => mt.SeekCounter((ulong[])NullReference.Value<ulong[]>()));
        AssertUnsupported(mt, RandomCapability.BlockReservation, () => mt.ReserveBlocks((UInt128)1));
        AssertUnsupported(numpy, RandomCapability.Advance, () => numpy.Advance((UInt128)1));
        AssertUnsupported(numpy, RandomCapability.BlockReservation, () => numpy.ReserveBlocks((UInt128)1));
        AssertUnsupported(splitMix, RandomCapability.Jump, splitMix.Jump);
        AssertUnsupported(splitMix, RandomCapability.LongJump, splitMix.LongJump);
        AssertUnsupported(splitMix, RandomCapability.Seek, () => splitMix.Seek((UInt128)1));
        AssertUnsupported(splitMix, RandomCapability.BlockReservation, () => splitMix.ReserveBlocks((UInt128)1));
    }

    [Test]
    public void SupportedZeroDistanceAdvancePreservesTheCompleteState()
    {
        RandomGenerator[] generators =
        [
            RandomGenerator.CreatePcg64Dxsm((UInt128)11, (UInt128)12, RandomMathMode.Portable),
            RandomGenerator.CreateSplitMix64(13UL, RandomMathMode.Portable)
        ];

        foreach (RandomGenerator generator in generators)
        {
            generator.NextBoolean();
            byte[] before = generator.ExportState().ToBytes();

            generator.Advance((UInt128)0);

            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static void AssertUnsupported(
        RandomGenerator generator,
        RandomCapability expectedCapability,
        TestDelegate action)
    {
        byte[] before = generator.ExportState().ToBytes();

        RandomCapabilityException error = Assert.Throws<RandomCapabilityException>(action);

        Assert.Multiple(() =>
        {
            Assert.That(error.GetType(), Is.EqualTo(typeof(RandomCapabilityException)));
            Assert.That(error.Algorithm(), Is.EqualTo(generator.Info().Algorithm()));
            Assert.That(error.Capability(), Is.EqualTo(expectedCapability));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
