namespace random.Tests;

public class RandomGeneratorFillTests
{
    [Test]
    public void ByteFillMatchesCanonicalByteFillExactly()
    {
        RandomGenerator actual = Create();
        RandomGenerator reference = Create();
        byte[] actualValues = new byte[37];
        byte[] expectedValues = new byte[37];

        actual.Fill(actualValues);
        reference.FillBytes(expectedValues);

        Assert.Multiple(() =>
        {
            Assert.That(actualValues, Is.EqualTo(expectedValues));
            Assert.That(actual.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ThirtyTwoBitIntegerFillsMatchRepeatedScalarCalls()
    {
        RandomGenerator unsignedActual = Create();
        RandomGenerator unsignedReference = Create();
        RandomGenerator signedActual = Create();
        RandomGenerator signedReference = Create();
        uint[] unsignedValues = new uint[73];
        uint[] expectedUnsigned = new uint[73];
        int[] signedValues = new int[73];
        int[] expectedSigned = new int[73];
        for (int index = 0; index < expectedUnsigned.Length; index++)
        {
            expectedUnsigned[index] = unsignedReference.NextUInt32();
            expectedSigned[index] = signedReference.NextInt32();
        }

        unsignedActual.FillUInt32(unsignedValues);
        signedActual.FillInt32(signedValues);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedValues, Is.EqualTo(expectedUnsigned));
            Assert.That(unsignedActual.ExportState().ToBytes(), Is.EqualTo(unsignedReference.ExportState().ToBytes()));
            Assert.That(signedValues, Is.EqualTo(expectedSigned));
            Assert.That(signedActual.ExportState().ToBytes(), Is.EqualTo(signedReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SixtyFourBitIntegerFillsMatchRepeatedScalarCalls()
    {
        RandomGenerator unsignedActual = Create();
        RandomGenerator unsignedReference = Create();
        RandomGenerator signedActual = Create();
        RandomGenerator signedReference = Create();
        ulong[] unsignedValues = new ulong[73];
        ulong[] expectedUnsigned = new ulong[73];
        long[] signedValues = new long[73];
        long[] expectedSigned = new long[73];
        for (int index = 0; index < expectedUnsigned.Length; index++)
        {
            expectedUnsigned[index] = unsignedReference.NextUInt64();
            expectedSigned[index] = signedReference.NextInt64();
        }

        unsignedActual.FillUInt64(unsignedValues);
        signedActual.FillInt64(signedValues);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedValues, Is.EqualTo(expectedUnsigned));
            Assert.That(unsignedActual.ExportState().ToBytes(), Is.EqualTo(unsignedReference.ExportState().ToBytes()));
            Assert.That(signedValues, Is.EqualTo(expectedSigned));
            Assert.That(signedActual.ExportState().ToBytes(), Is.EqualTo(signedReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void FloatingAndDecimalFillsMatchRepeatedScalarCalls()
    {
        RandomGenerator singleActual = Create();
        RandomGenerator singleReference = Create();
        RandomGenerator doubleActual = Create();
        RandomGenerator doubleReference = Create();
        RandomGenerator decimalActual = Create();
        RandomGenerator decimalReference = Create();
        float[] singleValues = new float[51];
        float[] expectedSingles = new float[51];
        double[] doubleValues = new double[51];
        double[] expectedDoubles = new double[51];
        decimal[] decimalValues = new decimal[51];
        decimal[] expectedDecimals = new decimal[51];
        for (int index = 0; index < singleValues.Length; index++)
        {
            expectedSingles[index] = singleReference.NextSingle();
            expectedDoubles[index] = doubleReference.NextDouble();
            expectedDecimals[index] = decimalReference.NextDecimal();
        }

        singleActual.FillSingles(singleValues);
        doubleActual.FillDoubles(doubleValues);
        decimalActual.FillDecimals(decimalValues);

        Assert.Multiple(() =>
        {
            Assert.That(singleValues, Is.EqualTo(expectedSingles));
            Assert.That(singleActual.ExportState().ToBytes(), Is.EqualTo(singleReference.ExportState().ToBytes()));
            Assert.That(doubleValues, Is.EqualTo(expectedDoubles));
            Assert.That(doubleActual.ExportState().ToBytes(), Is.EqualTo(doubleReference.ExportState().ToBytes()));
            Assert.That(decimalValues, Is.EqualTo(expectedDecimals));
            Assert.That(decimalActual.ExportState().ToBytes(), Is.EqualTo(decimalReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void BooleanFillMatchesRepeatedScalarCallsAcrossReservoirWords()
    {
        RandomGenerator actual = Create();
        RandomGenerator reference = Create();
        bool[] actualValues = new bool[137];
        bool[] expectedValues = new bool[137];
        for (int index = 0; index < expectedValues.Length; index++)
        {
            expectedValues[index] = reference.NextBoolean();
        }

        actual.FillBooleans(actualValues);

        Assert.Multiple(() =>
        {
            Assert.That(actualValues, Is.EqualTo(expectedValues));
            Assert.That(actual.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(17)]
    [TestCase(32)]
    [TestCase(33)]
    [TestCase(63)]
    [TestCase(64)]
    public void RequestedBitFillMatchesRepeatedScalarCalls(int bitCount)
    {
        RandomGenerator[] actualGenerators =
        [
            RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937(42U, RandomMathMode.Portable)
        ];
        RandomGenerator[] referenceGenerators =
        [
            RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937(42U, RandomMathMode.Portable)
        ];

        for (int generatorIndex = 0; generatorIndex < actualGenerators.Length; generatorIndex++)
        {
            actualGenerators[generatorIndex].NextBoolean();
            referenceGenerators[generatorIndex].NextBoolean();
            ulong[] actualValues = new ulong[19];
            ulong[] expectedValues = new ulong[19];
            for (int valueIndex = 0; valueIndex < expectedValues.Length; valueIndex++)
            {
                expectedValues[valueIndex] = referenceGenerators[generatorIndex].NextBits(bitCount);
            }

            actualGenerators[generatorIndex].FillBits(actualValues, bitCount);
            Assert.Multiple(() =>
            {
                Assert.That(actualValues, Is.EqualTo(expectedValues));
                Assert.That(actualGenerators[generatorIndex].ExportState().ToBytes(), Is.EqualTo(referenceGenerators[generatorIndex].ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void RequestedBitFillRejectsInvalidCountsBeforeMutation()
    {
        RandomGenerator generator = Create();
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        RandomArgumentException negative = Assert.Throws<RandomArgumentException>(() => generator.FillBits(new ulong[1], -1));
        RandomArgumentException large = Assert.Throws<RandomArgumentException>(() => generator.FillBits(Span<ulong>.Empty, 65));
        Assert.Multiple(() =>
        {
            Assert.That(negative.ArgumentName(), Is.EqualTo("bitCount"));
            Assert.That(large.ArgumentName(), Is.EqualTo("bitCount"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void EmptyTypedSpansPreserveEngineAndReservoirState()
    {
        RandomGenerator generator = Create();
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => generator.Fill(Span<byte>.Empty));
            Assert.DoesNotThrow(() => generator.FillBits(Span<ulong>.Empty, 17));
            Assert.DoesNotThrow(() => generator.FillUInt32(Span<uint>.Empty));
            Assert.DoesNotThrow(() => generator.FillInt32(Span<int>.Empty));
            Assert.DoesNotThrow(() => generator.FillUInt64(Span<ulong>.Empty));
            Assert.DoesNotThrow(() => generator.FillInt64(Span<long>.Empty));
            Assert.DoesNotThrow(() => generator.FillSingles(Span<float>.Empty));
            Assert.DoesNotThrow(() => generator.FillDoubles(Span<double>.Empty));
            Assert.DoesNotThrow(() => generator.FillDecimals(Span<decimal>.Empty));
            Assert.DoesNotThrow(() => generator.FillBooleans(Span<bool>.Empty));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static RandomGenerator Create()
    {
        return RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
    }
}
