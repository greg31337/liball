namespace random.Tests;

public class ExhaustiveSmallDomainTests
{
    [Test]
    public void EverySmallBoundedIntegerResultHasAnAcceptedWitness()
    {
        for (int bound = 1; bound <= 16; bound++)
        {
            for (int expected = 0; expected < bound; expected++)
            {
                IRandomSource generator32 = CreateScripted(AcceptedUInt32Word(bound, expected));
                IRandomSource generator64 = CreateScripted(AcceptedUInt64Word(bound, expected));

                Assert.Multiple(() =>
                {
                    Assert.That(IntegerUniform.NextUInt32(generator32, (uint)bound), Is.EqualTo((uint)expected), $"32-bit bound={bound}, result={expected}");
                    Assert.That(IntegerUniform.NextUInt64(generator64, (ulong)bound), Is.EqualTo((ulong)expected), $"64-bit bound={bound}, result={expected}");
                });
            }
        }
    }

    [Test]
    public void SmallPermutationsAreExactlyUniformAcrossAllDecisionPaths()
    {
        for (int count = 0; count <= 5; count++)
        {
            int[] bounds = DescendingBounds(count);
            List<int[]> paths = DecisionPaths(bounds);
            Dictionary<string, int> frequencies = new Dictionary<string, int>();

            foreach (int[] path in paths)
            {
                ulong[] words = WordsForPath(bounds, path);
                int[] permutation = RandomCollections.Permutation(CreateScripted(words), count);
                Increment(frequencies, Key(permutation));
            }

            Assert.Multiple(() =>
            {
                Assert.That(paths.Count, Is.EqualTo(Factorial(count)), $"path count for n={count}");
                Assert.That(frequencies.Count, Is.EqualTo(Factorial(count)), $"reachable permutations for n={count}");
                Assert.That(frequencies.Values, Is.All.EqualTo(1), $"frequency for n={count}");
            });
        }
    }

    [Test]
    public void SmallSubsetsAreExactlyUniformAcrossAllDecisionPaths()
    {
        for (int populationSize = 0; populationSize <= 5; populationSize++)
        {
            for (int count = 0; count <= populationSize; count++)
            {
                int[] bounds = AscendingPartialShuffleBounds(populationSize, count);
                List<int[]> paths = DecisionPaths(bounds);
                Dictionary<string, int> frequencies = new Dictionary<string, int>();

                foreach (int[] path in paths)
                {
                    ulong[] words = WordsForPath(bounds, path);
                    int[] subset = RandomCollections.Subset(CreateScripted(words), populationSize, count);
                    Increment(frequencies, Key(subset));
                }

                Assert.Multiple(() =>
                {
                    Assert.That(frequencies.Count, Is.EqualTo(Binomial(populationSize, count)), $"reachable subsets for n={populationSize}, k={count}");
                    Assert.That(frequencies.Values, Is.All.EqualTo(Factorial(count)), $"frequency for n={populationSize}, k={count}");
                });
            }
        }
    }

    [Test]
    public void SmallSamplesWithoutReplacementAreExactlyUniformAndReachable()
    {
        for (int populationSize = 0; populationSize <= 5; populationSize++)
        {
            int[] source = Enumerable.Range(0, populationSize).ToArray();
            for (int count = 0; count <= populationSize; count++)
            {
                int[] bounds = AscendingPartialShuffleBounds(populationSize, count);
                List<int[]> paths = DecisionPaths(bounds);
                Dictionary<string, int> frequencies = new Dictionary<string, int>();

                foreach (int[] path in paths)
                {
                    ulong[] words = WordsForPath(bounds, path);
                    int[] sample = RandomCollections.SampleWithoutReplacement(CreateScripted(words), source, count);
                    Increment(frequencies, Key(sample));
                }

                int expectedOutcomes = Factorial(populationSize) / Factorial(populationSize - count);
                Assert.Multiple(() =>
                {
                    Assert.That(frequencies.Count, Is.EqualTo(expectedOutcomes), $"reachable samples for n={populationSize}, k={count}");
                    Assert.That(frequencies.Values, Is.All.EqualTo(1), $"frequency for n={populationSize}, k={count}");
                });
            }
        }
    }

    [Test]
    public void CategoricalTablesHaveExactMassOnExhaustiveDyadicDecisionGrids()
    {
        double[] weights = [1.0, 2.0, 0.0, 1.0];
        int[] categoricalCounts = new int[weights.Length];
        int[] guideCounts = new int[weights.Length];
        int[] aliasCounts = new int[weights.Length];
        GuideTable guide = new GuideTable(weights);
        AliasTable alias = new AliasTable(weights);

        for (int point = 0; point < 8; point++)
        {
            ulong unitWord = (ulong)point << 50;
            CategoricalSampler categorical = new CategoricalSampler(CreateScripted(unitWord), weights);
            categoricalCounts[categorical.Sample()]++;
            guideCounts[guide.Sample(CreateScripted(unitWord))]++;

            for (int column = 0; column < weights.Length; column++)
            {
                ulong columnWord = AcceptedUInt32Word(weights.Length, column);
                aliasCounts[alias.Sample(CreateScripted(columnWord, unitWord))]++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(categoricalCounts, Is.EqualTo(new int[] { 2, 4, 0, 2 }));
            Assert.That(guideCounts, Is.EqualTo(new int[] { 2, 4, 0, 2 }));
            Assert.That(aliasCounts, Is.EqualTo(new int[] { 8, 16, 0, 8 }));
        });
    }

    static IRandomSource CreateScripted(params ulong[] words)
    {
        return new ScriptedRandomSource(words);
    }

    static ulong AcceptedUInt32Word(int bound, int result)
    {
        if (bound == 1)
        {
            return 0UL;
        }

        ulong upperBoundary = ((ulong)(result + 1) << 32) / (uint)bound;
        return (uint)(upperBoundary - 1UL);
    }

    static ulong AcceptedUInt64Word(int bound, int result)
    {
        if (bound == 1)
        {
            return 0UL;
        }

        UInt128 upperBoundary = ((UInt128)(result + 1) << 64) / (uint)bound;
        return (ulong)(upperBoundary - (UInt128)1);
    }

    static int[] DescendingBounds(int count)
    {
        int[] bounds = new int[count > 0 ? count - 1 : 0];
        for (int index = 0; index < bounds.Length; index++)
        {
            bounds[index] = count - index;
        }

        return bounds;
    }

    static int[] AscendingPartialShuffleBounds(int populationSize, int count)
    {
        int stochasticCount = count;
        if (stochasticCount == populationSize && stochasticCount > 0)
        {
            stochasticCount--;
        }

        int[] bounds = new int[stochasticCount];
        for (int index = 0; index < bounds.Length; index++)
        {
            bounds[index] = populationSize - index;
        }

        return bounds;
    }

    sealed class ScriptedRandomSource : IRandomSource
    {
        ulong[] words;
        int index;

        internal ScriptedRandomSource(ulong[] words)
        {
            this.words = words;
        }

        public RandomMathMode MathMode()
        {
            return RandomMathMode.Portable;
        }

        public uint NextUInt32()
        {
            return (uint)words[index++];
        }

        public ulong NextUInt64()
        {
            return words[index++];
        }

        public bool NextBoolean()
        {
            return (words[index++] & 1UL) != 0UL;
        }

        public void FillBytes(Span<byte> destination)
        {
            int destinationIndex = 0;
            while (destinationIndex < destination.Length)
            {
                ulong word = words[index++];
                int byteIndex = 0;
                while (byteIndex < 8 && destinationIndex < destination.Length)
                {
                    destination[destinationIndex] = (byte)(word & 0xFFUL);
                    word >>= 8;
                    byteIndex++;
                    destinationIndex++;
                }
            }
        }
    }

    static List<int[]> DecisionPaths(int[] bounds)
    {
        List<int[]> paths = new List<int[]>();
        EnumerateDecisionPaths(bounds, 0, new int[bounds.Length], paths);
        return paths;
    }

    static void EnumerateDecisionPaths(int[] bounds, int index, int[] current, List<int[]> paths)
    {
        if (index == bounds.Length)
        {
            paths.Add((int[])current.Clone());
            return;
        }

        for (int value = 0; value < bounds[index]; value++)
        {
            current[index] = value;
            EnumerateDecisionPaths(bounds, index + 1, current, paths);
        }
    }

    static ulong[] WordsForPath(int[] bounds, int[] path)
    {
        ulong[] words = new ulong[path.Length];
        for (int index = 0; index < words.Length; index++)
        {
            words[index] = AcceptedUInt32Word(bounds[index], path[index]);
        }

        return words;
    }

    static void Increment(Dictionary<string, int> frequencies, string key)
    {
        if (!frequencies.TryGetValue(key, out int count))
        {
            count = 0;
        }

        frequencies[key] = count + 1;
    }

    static string Key(int[] values)
    {
        return string.Join(",", values);
    }

    static int Factorial(int value)
    {
        int result = 1;
        for (int factor = 2; factor <= value; factor++)
        {
            result *= factor;
        }

        return result;
    }

    static int Binomial(int populationSize, int count)
    {
        return Factorial(populationSize) / (Factorial(count) * Factorial(populationSize - count));
    }
}
