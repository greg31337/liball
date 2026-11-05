using System.Collections;

namespace random.Tests;

public class RandomCollectionsPermutationTests
{
    [Test]
    public void PermutationMatchesNaturalOrderFisherYatesExactly()
    {
        RandomGenerator actualGenerator = Create(111UL);
        RandomGenerator referenceGenerator = Create(111UL);

        for (int repetition = 0; repetition < 257; repetition++)
        {
            int[] actual = RandomCollections.Permutation(actualGenerator, 31);
            int[] expected = Enumerable.Range(0, 31).ToArray();
            ReferenceShuffle(referenceGenerator, expected);
            Assert.That(actual, Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void SmallPermutationsReachEveryPossibleOrdering()
    {
        int[] factorials = [1, 1, 2, 6, 24, 120, 720];
        for (int count = 0; count <= 6; count++)
        {
            RandomGenerator generator = Create((ulong)(200 + count));
            HashSet<string> permutations = new HashSet<string>();
            for (int attempt = 0; attempt < factorials[count] * 30; attempt++)
            {
                permutations.Add(string.Join(",", RandomCollections.Permutation(generator, count)));
            }

            Assert.That(permutations.Count, Is.EqualTo(factorials[count]), $"count {count}");
        }
    }

    [Test]
    public void SubsetMatchesSortedPartialShuffleExactly()
    {
        RandomGenerator actualGenerator = Create(222UL);
        RandomGenerator referenceGenerator = Create(222UL);
        int[] expectedCandidates = Enumerable.Range(0, 17).ToArray();
        ReferencePartialShuffle(referenceGenerator, expectedCandidates, 7);
        int[] expected = expectedCandidates.Take(7).OrderBy(value => value).ToArray();

        int[] actual = RandomCollections.Subset(actualGenerator, 17, 7);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Is.Ordered.Ascending);
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SmallSubsetsReachEveryCombination()
    {
        RandomGenerator generator = Create(333UL);
        HashSet<string> subsets = new HashSet<string>();
        for (int attempt = 0; attempt < 2000; attempt++)
        {
            subsets.Add(string.Join(",", RandomCollections.Subset(generator, 6, 3)));
        }

        Assert.That(subsets.Count, Is.EqualTo(20));
    }

    [Test]
    public void CombinationIsAnExactSubsetAlias()
    {
        RandomGenerator subsetGenerator = Create(444UL);
        RandomGenerator combinationGenerator = Create(444UL);

        for (int repetition = 0; repetition < 100; repetition++)
        {
            Assert.That(
                RandomCollections.Combination(combinationGenerator, 23, 9),
                Is.EqualTo(RandomCollections.Subset(subsetGenerator, 23, 9)));
        }

        Assert.That(combinationGenerator.ExportState().ToBytes(), Is.EqualTo(subsetGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void TypedSamplingWithoutReplacementMatchesPartialShufflePrefix()
    {
        int[] source = Enumerable.Range(10, 20).ToArray();
        int[] original = (int[])source.Clone();
        int[] candidates = (int[])source.Clone();
        RandomGenerator actualGenerator = Create(555UL);
        RandomGenerator referenceGenerator = Create(555UL);
        ReferencePartialShuffle(referenceGenerator, candidates, 8);

        int[] actual = RandomCollections.SampleWithoutReplacement(actualGenerator, source, 8);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(candidates.Take(8)));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(8));
            Assert.That(source, Is.EqualTo(original));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void GeneralArraySamplingSupportsArbitraryLowerBounds()
    {
        Array source = Array.CreateInstance(typeof(string), [9], [-5]);
        string[] candidates = new string[9];
        for (int index = 0; index < candidates.Length; index++)
        {
            candidates[index] = $"value-{index}";
            source.SetValue(candidates[index], index - 5);
        }

        Array original = (Array)source.Clone();
        RandomGenerator actualGenerator = Create(666UL);
        RandomGenerator referenceGenerator = Create(666UL);
        ReferencePartialShuffle(referenceGenerator, candidates, 4);

        Array actual = RandomCollections.SampleWithoutReplacement(actualGenerator, source, 4);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.TypeOf<string[]>());
            Assert.That(actual.GetLowerBound(0), Is.Zero);
            Assert.That(actual.Cast<string>(), Is.EqualTo(candidates.Take(4)));
            Assert.That(source.Cast<string>(), Is.EqualTo(original.Cast<string>()));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ListSamplingSupportsReadOnlyListsWithoutMutation()
    {
        IList mutable = new ArrayList() { "a", "b", "c", "d", "e", "f" };
        IList source = ArrayList.ReadOnly(mutable);
        object[] candidates = source.Cast<object>().ToArray();
        RandomGenerator actualGenerator = Create(777UL);
        RandomGenerator referenceGenerator = Create(777UL);
        ReferencePartialShuffle(referenceGenerator, candidates, 4);

        object?[] actual = RandomCollections.SampleWithoutReplacement(actualGenerator, source, 4);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(candidates.Take(4)));
            Assert.That(source.Cast<object>(), Is.EqualTo(new object[] { "a", "b", "c", "d", "e", "f" }));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ExplicitSampleOverloadsRetainArrayTypes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new bool[] { true }, 1), Is.TypeOf<bool[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new byte[] { 1 }, 1), Is.TypeOf<byte[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new sbyte[] { 1 }, 1), Is.TypeOf<sbyte[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new short[] { 1 }, 1), Is.TypeOf<short[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new ushort[] { 1 }, 1), Is.TypeOf<ushort[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new int[] { 1 }, 1), Is.TypeOf<int[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new uint[] { 1 }, 1), Is.TypeOf<uint[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new long[] { 1 }, 1), Is.TypeOf<long[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new ulong[] { 1 }, 1), Is.TypeOf<ulong[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new char[] { 'x' }, 1), Is.TypeOf<char[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new float[] { 1 }, 1), Is.TypeOf<float[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new double[] { 1 }, 1), Is.TypeOf<double[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new decimal[] { 1 }, 1), Is.TypeOf<decimal[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new string[] { "x" }, 1), Is.TypeOf<string[]>());
            Assert.That(RandomCollections.SampleWithoutReplacement(Create(1UL), new object[] { "x" }, 1), Is.TypeOf<object[]>());
        });
    }

    [Test]
    public void SmallOrderedSamplesReachEveryPossiblePrefix()
    {
        RandomGenerator generator = Create(888UL);
        HashSet<string> samples = new HashSet<string>();
        int[] source = [0, 1, 2, 3, 4];
        for (int attempt = 0; attempt < 3000; attempt++)
        {
            samples.Add(string.Join(",", RandomCollections.SampleWithoutReplacement(generator, source, 3)));
        }

        Assert.That(samples.Count, Is.EqualTo(60));
    }

    [Test]
    public void DuplicateValuesRemainEligibleThroughDistinctSourcePositions()
    {
        RandomGenerator generator = Create(999UL);
        string[] source = ["same", "same", "different"];
        bool observedBothEqualPositions = false;
        for (int attempt = 0; attempt < 100; attempt++)
        {
            string?[] sample = RandomCollections.SampleWithoutReplacement(generator, source, 2);
            if (sample[0] == "same" && sample[1] == "same")
            {
                observedBothEqualPositions = true;
                break;
            }
        }

        Assert.That(observedBothEqualPositions, Is.True);
    }

    [Test]
    public void DeterministicPermutationAndSubsetBoundsConsumeNoState()
    {
        RandomGenerator generator = Create(1010UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        int[] emptyPermutation = RandomCollections.Permutation(generator, 0);
        int[] singletonPermutation = RandomCollections.Permutation(generator, 1);
        int[] emptySubset = RandomCollections.Subset(generator, 10, 0);
        int[] fullSubset = RandomCollections.Subset(generator, 5, 5);
        int[] emptySample = RandomCollections.SampleWithoutReplacement(generator, new int[] { 1, 2, 3 }, 0);
        int[] singletonSample = RandomCollections.SampleWithoutReplacement(generator, new int[] { 42 }, 1);

        Assert.Multiple(() =>
        {
            Assert.That(emptyPermutation, Is.Empty);
            Assert.That(singletonPermutation, Is.EqualTo(new int[] { 0 }));
            Assert.That(emptySubset, Is.Empty);
            Assert.That(fullSubset, Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
            Assert.That(emptySample, Is.Empty);
            Assert.That(singletonSample, Is.EqualTo(new int[] { 42 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void FullOrderedSampleIsAUniformPermutationOfTheSource()
    {
        int[] source = [10, 20, 30, 40, 50, 60];
        int[] candidates = (int[])source.Clone();
        RandomGenerator actualGenerator = Create(1111UL);
        RandomGenerator referenceGenerator = Create(1111UL);
        ReferencePartialShuffle(referenceGenerator, candidates, candidates.Length);

        int[] actual = RandomCollections.SampleWithoutReplacement(actualGenerator, source, source.Length);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(candidates));
            Assert.That(actual, Is.EquivalentTo(source));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidPermutationInputsFailBeforeMutationOrRandomConsumption()
    {
        RandomGenerator generator = Create(1212UL);
        byte[] before = generator.ExportState().ToBytes();
        int[] source = [0, 1, 2, 3];
        Array rectangular = new int[2, 2];

        object[] cases =
        [
            new object[] { (TestDelegate)(() => RandomCollections.Permutation(NullReference.Value<global::random.IRandomSource>(), 4)), "generator" },
            new object[] { (TestDelegate)(() => RandomCollections.Permutation(generator, -1)), "count" },
            new object[] { (TestDelegate)(() => RandomCollections.Subset(generator, -1, 0)), "populationSize" },
            new object[] { (TestDelegate)(() => RandomCollections.Subset(generator, 4, -1)), "count" },
            new object[] { (TestDelegate)(() => RandomCollections.Combination(generator, 4, 5)), "count" },
            new object[] { (TestDelegate)(() => RandomCollections.SampleWithoutReplacement(generator, source, -1)), "count" },
            new object[] { (TestDelegate)(() => RandomCollections.SampleWithoutReplacement(generator, source, 5)), "count" },
            new object[] { (TestDelegate)(() => RandomCollections.SampleWithoutReplacement(generator, rectangular, 1)), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.SampleWithoutReplacement(generator, (IList)NullReference.Value<global::System.Collections.IList>(), 0)), "values" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.Multiple(() =>
        {
            Assert.That(source, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static void ReferenceShuffle(RandomGenerator generator, int[] values)
    {
        for (int current = values.Length - 1; current > 0; current--)
        {
            int selected = generator.NextInt32Bounded(current + 1);
            int temporary = values[current];
            values[current] = values[selected];
            values[selected] = temporary;
        }
    }

    static void ReferencePartialShuffle(RandomGenerator generator, int[] values, int count)
    {
        for (int current = 0; current < count; current++)
        {
            int selected = current + generator.NextInt32Bounded(values.Length - current);
            int temporary = values[current];
            values[current] = values[selected];
            values[selected] = temporary;
        }
    }

    static void ReferencePartialShuffle(RandomGenerator generator, string[] values, int count)
    {
        for (int current = 0; current < count; current++)
        {
            int selected = current + generator.NextInt32Bounded(values.Length - current);
            string temporary = values[current];
            values[current] = values[selected];
            values[selected] = temporary;
        }
    }

    static void ReferencePartialShuffle(RandomGenerator generator, object[] values, int count)
    {
        for (int current = 0; current < count; current++)
        {
            int selected = current + generator.NextInt32Bounded(values.Length - current);
            object temporary = values[current];
            values[current] = values[selected];
            values[selected] = temporary;
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
