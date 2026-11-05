namespace random.Tests;

public class RandomCollectionsDerangementTests
{
    [Test]
    public void DerangementsMatchUniformPermutationRejectionExactly()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        for (int count = 3; count <= 20; count++)
        {
            for (int repetition = 0; repetition < 100; repetition++)
            {
                int[] actual = RandomCollections.Derangement(actualGenerator, count);
                int[] expected = ReferenceDerangement(referenceGenerator, count);
                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void EverySmallDerangementIsReachable()
    {
        int[] derangementCounts = [0, 0, 1, 2, 9, 44, 265];
        for (int count = 3; count <= 6; count++)
        {
            RandomGenerator generator = Create((ulong)(200 + count));
            HashSet<string> observed = new HashSet<string>();
            for (int repetition = 0; repetition < derangementCounts[count] * 30; repetition++)
            {
                observed.Add(string.Join(",", RandomCollections.Derangement(generator, count)));
            }

            Assert.That(observed.Count, Is.EqualTo(derangementCounts[count]), $"count {count}");
        }
    }

    [Test]
    public void FourElementDerangementsAreFrequencyBalanced()
    {
        RandomGenerator generator = Create(303UL);
        Dictionary<string, int> counts = new Dictionary<string, int>();
        const int sampleCount = 90000;
        for (int repetition = 0; repetition < sampleCount; repetition++)
        {
            string key = string.Join(",", RandomCollections.Derangement(generator, 4));
            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;
            }

            counts[key]++;
        }

        Assert.That(counts.Count, Is.EqualTo(9));
        foreach (int frequency in counts.Values)
        {
            Assert.That(frequency, Is.InRange(9500, 10500));
        }
    }

    [Test]
    public void LargeDerangementsArePermutationsWithoutFixedPoints()
    {
        RandomGenerator generator = Create(404UL);
        for (int repetition = 0; repetition < 50; repetition++)
        {
            int[] actual = RandomCollections.Derangement(generator, 1000);
            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.EquivalentTo(Enumerable.Range(0, actual.Length)));
                Assert.That(actual.Where((value, index) => value == index), Is.Empty);
            });
        }
    }

    [Test]
    public void EmptyAndTwoElementDerangementsConsumeNoState()
    {
        RandomGenerator generator = Create(505UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        int[] empty = RandomCollections.Derangement(generator, 0);
        int[] pair = RandomCollections.Derangement(generator, 2);

        Assert.Multiple(() =>
        {
            Assert.That(empty, Is.Empty);
            Assert.That(pair, Is.EqualTo(new int[] { 1, 0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void ImpossibleAndInvalidSizesAreRejectedWithoutConsumption()
    {
        RandomGenerator generator = Create(606UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        RandomArgumentException singleton = Assert.Throws<RandomArgumentException>(() => RandomCollections.Derangement(generator, 1));
        RandomArgumentException negative = Assert.Throws<RandomArgumentException>(() => RandomCollections.Derangement(generator, -1));
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => RandomCollections.Derangement(NullReference.Value<global::random.IRandomSource>(), 1));

        Assert.Multiple(() =>
        {
            Assert.That(singleton.ArgumentName(), Is.EqualTo("count"));
            Assert.That(negative.ArgumentName(), Is.EqualTo("count"));
            Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static int[] ReferenceDerangement(RandomGenerator generator, int count)
    {
        while (true)
        {
            int[] candidate = Enumerable.Range(0, count).ToArray();
            ReferenceShuffle(generator, candidate);
            bool valid = true;
            for (int index = 0; index < candidate.Length; index++)
            {
                if (candidate[index] == index)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                return candidate;
            }
        }
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

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
