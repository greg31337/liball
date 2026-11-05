using System.Collections;

namespace random.Tests;

public class RandomCollectionsReservoirTests
{
    [Test]
    public void BillionScaleSubsetMatchesVirtualPartialShuffleWithoutDenseStorage()
    {
        const int populationSize = 1000000000;
        const int count = 11;
        RandomGenerator actualGenerator = Create(123UL);
        RandomGenerator referenceGenerator = Create(123UL);
        int[] expected = ReferenceSparseIndexes(referenceGenerator, populationSize, count);
        Array.Sort(expected);

        int[] actual = RandomCollections.Subset(actualGenerator, populationSize, count);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Is.Ordered.Ascending);
            Assert.That(actual.Distinct().Count(), Is.EqualTo(count));
            Assert.That(actual, Is.All.InRange(0, populationSize - 1));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SparseListSamplingReadsOnlyTheSelectedPositions()
    {
        const int populationSize = 1000000;
        const int count = 9;
        VirtualList source = new VirtualList(populationSize);
        RandomGenerator actualGenerator = Create(234UL);
        RandomGenerator referenceGenerator = Create(234UL);
        int[] selectedIndexes = ReferenceSparseIndexes(referenceGenerator, populationSize, count);
        object[] expected = selectedIndexes.Select(index => (object)(index * 3 + 1)).ToArray();

        object?[] actual = RandomCollections.SampleWithoutReplacement(actualGenerator, (IList)source, count);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(source.IndexReads, Is.EqualTo(count));
            Assert.That(source.ReadIndexes, Is.EqualTo(selectedIndexes));
            Assert.That(source.EnumeratorRequests, Is.Zero);
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SparseArraySamplingMatchesTheSameVirtualSwapTraversal()
    {
        const int populationSize = 10000;
        const int count = 7;
        int[] source = Enumerable.Range(0, populationSize).Select(index => index * 2).ToArray();
        RandomGenerator actualGenerator = Create(345UL);
        RandomGenerator referenceGenerator = Create(345UL);
        int[] selectedIndexes = ReferenceSparseIndexes(referenceGenerator, populationSize, count);
        int[] expected = selectedIndexes.Select(index => source[index]).ToArray();

        int[] actual = RandomCollections.SampleWithoutReplacement(actualGenerator, source, count);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(source, Is.EqualTo(Enumerable.Range(0, populationSize).Select(index => index * 2)));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ZeroRandomAccessSampleReadsNoSourceElements()
    {
        TrackingList source = new TrackingList([1, 2, 3]);
        RandomGenerator generator = Create(400UL);
        byte[] before = generator.ExportState().ToBytes();

        object?[] actual = RandomCollections.SampleWithoutReplacement(generator, (IList)source, 0);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Empty);
            Assert.That(source.IndexReads, Is.Zero);
            Assert.That(source.EnumeratorRequests, Is.Zero);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void StreamingReservoirMatchesAlgorithmRAndEnumeratesExactlyOnce()
    {
        object[] values = Enumerable.Range(0, 100).Cast<object>().ToArray();
        TrackingEnumerable source = new TrackingEnumerable(values);
        RandomGenerator actualGenerator = Create(456UL);
        RandomGenerator referenceGenerator = Create(456UL);
        object[] expected = ReferenceReservoir(referenceGenerator, values, 13);

        object?[] actual = RandomCollections.ReservoirSample(actualGenerator, (IEnumerable)source, 13);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
            Assert.That(source.EnumeratorRequests, Is.EqualTo(1));
            Assert.That(source.MoveNextCalls, Is.EqualTo(values.Length + 1));
            Assert.That(source.CurrentReads, Is.EqualTo(values.Length));
            Assert.That(source.DisposeCalls, Is.EqualTo(1));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ListReservoirMatchesStreamingTraversalAndReadsEveryPositionOnce()
    {
        object[] values = Enumerable.Range(0, 73).Select(index => (object)$"value-{index}").ToArray();
        TrackingList source = new TrackingList(values);
        RandomGenerator actualGenerator = Create(567UL);
        RandomGenerator referenceGenerator = Create(567UL);
        object[] expected = ReferenceReservoir(referenceGenerator, values, 8);

        object?[] actual = RandomCollections.ReservoirSample(actualGenerator, (IList)source, 8);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(source.IndexReads, Is.EqualTo(values.Length));
            Assert.That(source.ReadIndexes, Is.EqualTo(Enumerable.Range(0, values.Length)));
            Assert.That(source.EnumeratorRequests, Is.Zero);
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SmallReservoirsReachEveryPossibleSubset()
    {
        object[] values = [0, 1, 2, 3, 4];
        RandomGenerator generator = Create(678UL);
        HashSet<string> subsets = new HashSet<string>();
        for (int attempt = 0; attempt < 2000; attempt++)
        {
            object?[] sample = RandomCollections.ReservoirSample(generator, (IEnumerable)values, 2);
            subsets.Add(string.Join(",", sample.Cast<int>().OrderBy(value => value)));
        }

        Assert.That(subsets.Count, Is.EqualTo(10));
    }

    [Test]
    public void ZeroReservoirSkipsTraversalAndConsumesNoState()
    {
        TrackingEnumerable stream = new TrackingEnumerable([1, 2, 3]);
        TrackingList list = new TrackingList([1, 2, 3]);
        RandomGenerator generator = Create(789UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        object?[] streamResult = RandomCollections.ReservoirSample(generator, (IEnumerable)stream, 0);
        object?[] listResult = RandomCollections.ReservoirSample(generator, (IList)list, 0);

        Assert.Multiple(() =>
        {
            Assert.That(streamResult, Is.Empty);
            Assert.That(listResult, Is.Empty);
            Assert.That(stream.EnumeratorRequests, Is.Zero);
            Assert.That(list.IndexReads, Is.Zero);
            Assert.That(list.EnumeratorRequests, Is.Zero);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void ExactSizeReservoirPreservesOrderWithoutRandomConsumption()
    {
        object[] values = ["a", "b", "c", "d"];
        TrackingEnumerable source = new TrackingEnumerable(values);
        RandomGenerator generator = Create(890UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        object?[] actual = RandomCollections.ReservoirSample(generator, (IEnumerable)source, values.Length);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(values));
            Assert.That(source.EnumeratorRequests, Is.EqualTo(1));
            Assert.That(source.DisposeCalls, Is.EqualTo(1));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void ShortStreamingSourceFailsAfterOneTraversalWithoutRandomConsumption()
    {
        TrackingEnumerable source = new TrackingEnumerable([1, 2, 3]);
        RandomGenerator generator = Create(901UL);
        byte[] before = generator.ExportState().ToBytes();

        RandomArgumentException error = Assert.Throws<RandomArgumentException>(
            () => RandomCollections.ReservoirSample(generator, (IEnumerable)source, 4));

        Assert.Multiple(() =>
        {
            Assert.That(error.ArgumentName(), Is.EqualTo("count"));
            Assert.That(source.EnumeratorRequests, Is.EqualTo(1));
            Assert.That(source.MoveNextCalls, Is.EqualTo(4));
            Assert.That(source.DisposeCalls, Is.EqualTo(1));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidReservoirArgumentsFailBeforeTraversalOrRandomConsumption()
    {
        TrackingEnumerable source = new TrackingEnumerable([1, 2, 3]);
        TrackingList list = new TrackingList([1, 2, 3]);
        RandomGenerator generator = Create(12UL);
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => RandomCollections.ReservoirSample(NullReference.Value<global::random.IRandomSource>(), (IEnumerable)source, 1)), "generator" },
            new object[] { (TestDelegate)(() => RandomCollections.ReservoirSample(generator, (IEnumerable)NullReference.Value<global::System.Collections.IEnumerable>(), 1)), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.ReservoirSample(generator, (IEnumerable)source, -1)), "count" },
            new object[] { (TestDelegate)(() => RandomCollections.ReservoirSample(generator, (IList)list, 4)), "count" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.Multiple(() =>
        {
            Assert.That(source.EnumeratorRequests, Is.Zero);
            Assert.That(list.IndexReads, Is.Zero);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static int[] ReferenceSparseIndexes(RandomGenerator generator, int populationSize, int count)
    {
        Dictionary<int, int> movedIndexes = new Dictionary<int, int>();
        int[] result = new int[count];
        for (int current = 0; current < count; current++)
        {
            int selected = current + generator.NextInt32Bounded(populationSize - current);
            int selectedValue = movedIndexes.TryGetValue(selected, out int mappedSelected) ? mappedSelected : selected;
            int currentValue = movedIndexes.TryGetValue(current, out int mappedCurrent) ? mappedCurrent : current;
            result[current] = selectedValue;
            movedIndexes[selected] = currentValue;
            movedIndexes.Remove(current);
        }

        return result;
    }

    static object[] ReferenceReservoir(RandomGenerator generator, object[] values, int count)
    {
        object[] result = values.Take(count).ToArray();
        for (int index = count; index < values.Length; index++)
        {
            long selected = generator.NextInt64Bounded((long)index + 1L);
            if (selected < count)
            {
                result[(int)selected] = values[index];
            }
        }

        return result;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    class VirtualList : IList
    {
        int count;

        public VirtualList(int count)
        {
            this.count = count;
            ReadIndexes = new List<int>();
        }

        public int IndexReads;
        public int EnumeratorRequests;
        public List<int> ReadIndexes;
        public int Count => count;
        public bool IsReadOnly => true;
        public bool IsFixedSize => true;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public object? this[int index]
        {
            get
            {
                IndexReads++;
                ReadIndexes.Add(index);
                return index * 3 + 1;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public IEnumerator GetEnumerator()
        {
            EnumeratorRequests++;
            throw new InvalidOperationException("Sparse random-access sampling must not enumerate the list.");
        }

        public int Add(object? value) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(object? value) => false;
        public int IndexOf(object? value) => -1;
        public void Insert(int index, object? value) => throw new NotSupportedException();
        public void Remove(object? value) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
        public void CopyTo(Array array, int index) => throw new NotSupportedException();
    }

    class TrackingList : IList
    {
        object[] values;

        public TrackingList(object[] values)
        {
            this.values = values;
            ReadIndexes = new List<int>();
        }

        public int IndexReads;
        public int EnumeratorRequests;
        public List<int> ReadIndexes;
        public int Count => values.Length;
        public bool IsReadOnly => true;
        public bool IsFixedSize => true;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public object? this[int index]
        {
            get
            {
                IndexReads++;
                ReadIndexes.Add(index);
                return values[index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public IEnumerator GetEnumerator()
        {
            EnumeratorRequests++;
            return values.GetEnumerator();
        }

        public int Add(object? value) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(object? value) => values.Contains(value);
        public int IndexOf(object? value) => Array.IndexOf(values, value);
        public void Insert(int index, object? value) => throw new NotSupportedException();
        public void Remove(object? value) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
        public void CopyTo(Array array, int index) => values.CopyTo(array, index);
    }

    class TrackingEnumerable : IEnumerable
    {
        object[] values;

        public TrackingEnumerable(object[] values)
        {
            this.values = values;
        }

        public int EnumeratorRequests;
        public int MoveNextCalls;
        public int CurrentReads;
        public int DisposeCalls;

        public IEnumerator GetEnumerator()
        {
            EnumeratorRequests++;
            return new TrackingEnumerator(this, values);
        }
    }

    class TrackingEnumerator : IEnumerator, IDisposable
    {
        TrackingEnumerable owner;
        object[] values;
        int index;

        public TrackingEnumerator(TrackingEnumerable owner, object[] values)
        {
            this.owner = owner;
            this.values = values;
            index = -1;
        }

        public object Current
        {
            get
            {
                owner.CurrentReads++;
                return values[index];
            }
        }

        public bool MoveNext()
        {
            owner.MoveNextCalls++;
            index++;
            return index < values.Length;
        }

        public void Reset()
        {
            index = -1;
        }

        public void Dispose()
        {
            owner.DisposeCalls++;
        }
    }
}
