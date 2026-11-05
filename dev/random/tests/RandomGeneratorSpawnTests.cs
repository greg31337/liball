namespace random.Tests;

public class RandomGeneratorSpawnTests
{
    [Test]
    public void IndexedSpawnHasStableCrossEngineVector()
    {
        RandomGenerator[] parents = CreateGenerators();
        ulong[] expected =
        [
            4150823368225382055UL,
            16688891304190903071UL,
            11546310635489387717UL,
            1271023506UL,
            3424262827UL,
            12234754331684776561UL
        ];

        for (int index = 0; index < parents.Length; index++)
        {
            Assert.That(parents[index].Spawn(0x0123456789ABCDEFUL).NextRawWord(), Is.EqualTo(expected[index]));
        }
    }

    [Test]
    public void IndexedChildrenAreRepeatableDistinctAndDoNotMutateParents()
    {
        RandomGenerator[] parents = CreateGenerators();
        foreach (RandomGenerator parent in parents)
        {
            byte[] before = parent.ExportState().ToBytes();
            RandomGenerator childSeven = parent.Spawn(7UL);
            RandomGenerator childSevenAgain = parent.Spawn(7UL);
            RandomGenerator childEight = parent.Spawn(8UL);
            ulong sevenFirst = childSeven.NextRawWord();

            Assert.Multiple(() =>
            {
                Assert.That(childSeven.Info().Algorithm(), Is.EqualTo(parent.Info().Algorithm()));
                Assert.That(childSeven.MathMode(), Is.EqualTo(parent.MathMode()));
                Assert.That(childSevenAgain.NextRawWord(), Is.EqualTo(sevenFirst));
                Assert.That(childEight.NextRawWord(), Is.Not.EqualTo(sevenFirst));
                Assert.That(parent.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void ChildIdentityDoesNotDependOnCreationOrderOrParentPosition()
    {
        RandomGenerator parent = RandomGenerator.CreatePcg64Dxsm((UInt128)11, (UInt128)12, RandomMathMode.Portable);
        RandomGenerator beforeAdvance = parent.Spawn(99UL);
        parent.Spawn(100UL);
        parent.NextRawWord();
        parent.Advance((UInt128)12345);
        RandomGenerator afterAdvance = parent.Spawn(99UL);

        Assert.That(afterAdvance.NextRawWord(), Is.EqualTo(beforeAdvance.NextRawWord()));
    }

    [Test]
    public void NestedChildrenUseTheCompleteOrderedPath()
    {
        RandomGenerator root = RandomGenerator.CreateXoshiro256StarStar(123UL, RandomMathMode.Portable);
        RandomGenerator first = root.Spawn(7UL).Spawn(11UL).Spawn(13UL);
        RandomGenerator repeated = root.Spawn(7UL).Spawn(11UL).Spawn(13UL);
        RandomGenerator reordered = root.Spawn(13UL).Spawn(11UL).Spawn(7UL);

        ulong expected = first.NextRawWord();
        Assert.Multiple(() =>
        {
            Assert.That(repeated.NextRawWord(), Is.EqualTo(expected));
            Assert.That(reordered.NextRawWord(), Is.Not.EqualTo(expected));
        });
    }

    [Test]
    public void SpawnManyMatchesIndependentIndexedChildren()
    {
        RandomGenerator parent = RandomGenerator.CreateMt19937Numpy(42U, RandomMathMode.System);
        RandomGenerator[] children = parent.SpawnMany(5);

        Assert.That(children.Length, Is.EqualTo(5));
        for (int index = 0; index < children.Length; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(children[index].NextRawWord(), Is.EqualTo(parent.Spawn((ulong)index).NextRawWord()));
                Assert.That(children[index].MathMode(), Is.EqualTo(RandomMathMode.System));
            });
        }

        Assert.That(parent.SpawnMany(0), Is.Empty);
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => parent.SpawnMany(-1));
        Assert.That(error.ArgumentName(), Is.EqualTo("count"));
    }

    [Test]
    public void ClonePreservesRootIdentityAndCurrentSpawnPath()
    {
        RandomGenerator parent = RandomGenerator.CreateSplitMix64(987UL, RandomMathMode.Portable).Spawn(3UL).Spawn(5UL);
        parent.NextRawWord();
        RandomGenerator clone = parent.Clone();
        parent.NextRawWord();
        clone.NextRawWord();

        Assert.That(parent.Spawn(8UL).NextRawWord(), Is.EqualTo(clone.Spawn(8UL).NextRawWord()));
    }

    [Test]
    public void PcgChildrenUseOddDistinctNativeStreamIncrements()
    {
        RandomGenerator parent = RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.Portable);
        Pcg64DxsmEngine first = Pcg64DxsmEngine.FromState(parent.Spawn(1UL).ExportState());
        Pcg64DxsmEngine second = Pcg64DxsmEngine.FromState(parent.Spawn(2UL).ExportState());

        Assert.Multiple(() =>
        {
            Assert.That((first.IncrementValue() & (UInt128)1), Is.EqualTo((UInt128)1));
            Assert.That((second.IncrementValue() & (UInt128)1), Is.EqualTo((UInt128)1));
            Assert.That(first.IncrementValue(), Is.Not.EqualTo(second.IncrementValue()));
        });
    }

    [Test]
    public void PhiloxChildrenUseHighCounterPartitionsAndFreshBlockPositions()
    {
        RandomGenerator parent = RandomGenerator.CreatePhilox4x64Ten((UInt128)3, RandomMathMode.Portable);
        Philox4x64TenEngine first = Philox4x64TenEngine.FromState(parent.Spawn(1UL).ExportState());
        Philox4x64TenEngine second = Philox4x64TenEngine.FromState(parent.Spawn(2UL).ExportState());
        ulong[] firstCounter = first.CounterWords();
        ulong[] secondCounter = second.CounterWords();

        Assert.Multiple(() =>
        {
            Assert.That(firstCounter[0], Is.EqualTo(0UL));
            Assert.That(firstCounter[1], Is.EqualTo(0UL));
            Assert.That(firstCounter.AsSpan(2, 2).ToArray(), Is.Not.EqualTo(secondCounter.AsSpan(2, 2).ToArray()));
            Assert.That(first.BufferedLaneIndex(), Is.EqualTo(4));
            Assert.That(second.BufferedLaneIndex(), Is.EqualTo(4));
        });
    }

    [Test]
    public void DerivedStateEnginesProduceValidIndependentSnapshots()
    {
        RandomGenerator[] parents =
        [
            RandomGenerator.CreateXoshiro256StarStar(4UL, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937(5U, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937Numpy(6U, RandomMathMode.Portable),
            RandomGenerator.CreateSplitMix64(7UL, RandomMathMode.Portable)
        ];

        foreach (RandomGenerator parent in parents)
        {
            RandomGenerator child = parent.Spawn(ulong.MaxValue);
            RandomGenerator restored = RandomGenerator.FromState(child.ExportState(), RandomMathMode.Portable);
            Assert.That(restored.NextRawWord(), Is.EqualTo(child.NextRawWord()));
        }
    }

    [Test]
    public void GeneratorMetadataAdvertisesSpawnWithoutChangingEngineMetadata()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(8UL, RandomMathMode.Portable);
        RandomAlgorithmInfo generatorInfo = generator.Info();
        RandomAlgorithmInfo engineInfo = SplitMix64Engine.FromSeed(8UL).Info();

        Assert.Multiple(() =>
        {
            Assert.That(generatorInfo.HasCapability(RandomCapability.Spawn), Is.True);
            Assert.That(generatorInfo.HasCapability(RandomCapability.Reset), Is.True);
            Assert.That(engineInfo.HasCapability(RandomCapability.Spawn), Is.False);
        });
    }

    static RandomGenerator[] CreateGenerators()
    {
        return
        [
            RandomGenerator.CreatePcg64Dxsm((UInt128)11, (UInt128)12, RandomMathMode.Portable),
            RandomGenerator.CreateXoshiro256StarStar(13UL, RandomMathMode.System),
            RandomGenerator.CreatePhilox4x64Ten((UInt128)14, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937(15U, RandomMathMode.System),
            RandomGenerator.CreateMt19937Numpy(16U, RandomMathMode.Portable),
            RandomGenerator.CreateSplitMix64(17UL, RandomMathMode.System)
        ];
    }
}
