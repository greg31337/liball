using System.Buffers.Binary;
using System.Security.Cryptography;

namespace random.Tests;

public class QmcValidationTests
{
    [Test]
    public void InitialPointsMatchPublishedSequenceDefinitions()
    {
        SobolSequence sobol = SobolSequence.CreateUnscrambled(2);
        HaltonSequence halton = HaltonSequence.CreateUnscrambled(2);
        HammersleySet hammersley = new HammersleySet(8, 2);
        double[][] expectedSobol =
        [
            [0.0, 0.0],
            [0.5, 0.5],
            [0.75, 0.25],
            [0.25, 0.75]
        ];
        double[][] expectedHalton =
        [
            [0.0, 0.0],
            [0.5, 1.0 / 3.0],
            [0.25, 2.0 / 3.0],
            [0.75, 1.0 / 9.0]
        ];
        double[][] expectedHammersley =
        [
            [0.0, 0.0],
            [1.0 / 8.0, 1.0 / 2.0],
            [2.0 / 8.0, 1.0 / 4.0],
            [3.0 / 8.0, 3.0 / 4.0]
        ];

        for (int point = 0; point < expectedSobol.Length; point++)
        {
            AssertPoint(sobol.Next(), expectedSobol[point]);
            AssertPoint(halton.Next(), expectedHalton[point]);
            AssertPoint(hammersley.Next(), expectedHammersley[point]);
        }
    }

    [Test]
    public void SkipAndResetReplayEveryStatefulQmcSequence()
    {
        SobolSequence sobol = SobolSequence.CreateUnscrambled(5);
        SobolSequence sobolReference = SobolSequence.CreateUnscrambled(5);
        HaltonSequence halton = HaltonSequence.CreateUnscrambled(5);
        HaltonSequence haltonReference = HaltonSequence.CreateUnscrambled(5);
        HammersleySet hammersley = new HammersleySet(4096, 5);
        HammersleySet hammersleyReference = new HammersleySet(4096, 5);
        double[] initialSobol = sobol.Fill(16);
        double[] initialHalton = halton.Fill(16);
        double[] initialHammersley = hammersley.Fill(16);
        sobolReference.Fill(1234);
        haltonReference.Fill(1234);
        hammersleyReference.Fill(1234);

        sobol.Reset();
        halton.Reset();
        hammersley.Reset();
        sobol.Skip(1234UL);
        halton.Skip(1234UL);
        hammersley.Skip(1234UL);

        Assert.Multiple(() =>
        {
            Assert.That(sobol.Next(), Is.EqualTo(sobolReference.Next()));
            Assert.That(halton.Next(), Is.EqualTo(haltonReference.Next()));
            Assert.That(hammersley.Next(), Is.EqualTo(hammersleyReference.Next()));
        });

        sobol.Reset();
        halton.Reset();
        hammersley.Reset();
        Assert.Multiple(() =>
        {
            Assert.That(sobol.Fill(16), Is.EqualTo(initialSobol));
            Assert.That(halton.Fill(16), Is.EqualTo(initialHalton));
            Assert.That(hammersley.Fill(16), Is.EqualTo(initialHammersley));
        });
    }

    [Test]
    public void IndependentlyRecreatedScramblersReplayAfterSkipAndReset()
    {
        Func<RandomGenerator, int, DigitalScrambler>[] factories =
        [
            DigitalScrambler.CreateDigitalShift,
            DigitalScrambler.CreateLinearMatrix,
            DigitalScrambler.CreateLinearMatrixWithDigitalShift
        ];

        for (int factoryIndex = 0; factoryIndex < factories.Length; factoryIndex++)
        {
            RandomGenerator firstGenerator = Create((ulong)(301 + factoryIndex));
            RandomGenerator secondGenerator = Create((ulong)(301 + factoryIndex));
            DigitalScrambler firstScrambler = factories[factoryIndex](firstGenerator, 6);
            DigitalScrambler secondScrambler = factories[factoryIndex](secondGenerator, 6);
            SobolSequence first = new SobolSequence(6, firstScrambler);
            SobolSequence second = new SobolSequence(6, secondScrambler);

            first.Skip(987654321UL);
            second.Skip(987654321UL);
            Assert.That(first.Fill(129), Is.EqualTo(second.Fill(129)), $"factory {factoryIndex} after skip");

            first.Reset();
            second.Reset();
            Assert.Multiple(() =>
            {
                Assert.That(first.FillPowerOfTwo(7), Is.EqualTo(second.FillPowerOfTwo(7)), $"factory {factoryIndex} after reset");
                Assert.That(firstGenerator.ExportState().ToBytes(), Is.EqualTo(secondGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void ExpandedJoeKuoDirectionDataMatchesCanonicalChecksum()
    {
        int dimensionCount = SobolDirectionNumbers.DimensionCount();
        int directionCount = SobolDirectionNumbers.BitCount();
        byte[] canonical = new byte[dimensionCount * directionCount * sizeof(ulong)];
        ulong[] directions = new ulong[directionCount];
        int offset = 0;
        for (int dimension = 1; dimension <= dimensionCount; dimension++)
        {
            SobolDirectionNumbers.Fill(dimension, directions);
            for (int direction = 0; direction < directionCount; direction++)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(canonical.AsSpan(offset, sizeof(ulong)), directions[direction]);
                offset += sizeof(ulong);
            }
        }

        string checksum = Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();

        // Canonical stream: dimensions 1..21201, then directions 1..64,
        // with each expanded 64-bit direction encoded little-endian.
        Assert.That(checksum, Is.EqualTo("1f7ea8d738da499329f6826a6fd5d923a7f668904cc04ff0d8c3546fd09576b8"));
    }

    [Test]
    public void LowDiscrepancyDesignsBeatFixedSeedPseudoRandomPoints()
    {
        const int pointCount = 128;
        const int dimension = 2;
        double[] sobol = SobolSequence.CreateUnscrambled(dimension).FillPowerOfTwo(7);
        double[] halton = HaltonSequence.CreateUnscrambled(dimension).Fill(pointCount);
        double[] hammersley = new HammersleySet(pointCount, dimension).Fill(pointCount);
        double[] pseudoRandom = new double[pointCount * dimension];
        RandomGenerator generator = Create(501UL);
        for (int index = 0; index < pseudoRandom.Length; index++)
        {
            pseudoRandom[index] = generator.NextDoubleClosedOpen();
        }

        double randomCentered = Discrepancy.Centered(pseudoRandom, pointCount, dimension);
        double randomL2Star = Discrepancy.L2Star(pseudoRandom, pointCount, dimension);
        double randomStar = Discrepancy.Star(pseudoRandom, pointCount, dimension);
        double[][] designs = [sobol, halton, hammersley];

        Assert.Multiple(() =>
        {
            foreach (double[] design in designs)
            {
                Assert.That(Discrepancy.Centered(design, pointCount, dimension), Is.LessThan(randomCentered));
                Assert.That(Discrepancy.L2Star(design, pointCount, dimension), Is.LessThan(randomL2Star));
                Assert.That(Discrepancy.Star(design, pointCount, dimension), Is.LessThan(randomStar));
            }
        });
    }

    static void AssertPoint(double[] actual, double[] expected)
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length));
        for (int coordinate = 0; coordinate < actual.Length; coordinate++)
        {
            Assert.That(actual[coordinate], Is.EqualTo(expected[coordinate]).Within(1.0e-15));
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
