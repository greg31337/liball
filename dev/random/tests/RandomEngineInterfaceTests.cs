namespace random.Tests;

public class RandomEngineInterfaceTests
{
    [Test]
    public void CommonContractDispatchesNativeWordsAndMetadataForEveryEngine()
    {
        Pcg64DxsmEngine pcg = Pcg64DxsmEngine.FromSeed((UInt128)42, (UInt128)54);
        Xoshiro256StarStarEngine xoshiro = Xoshiro256StarStarEngine.FromSeed(42UL);
        Philox4x64TenEngine philox = Philox4x64TenEngine.FromKey((UInt128)42);
        Mt19937Engine mt = Mt19937Engine.FromSeed(42U);
        Mt19937NumpyEngine numpyMt = Mt19937NumpyEngine.FromSeed(42U);
        SplitMix64Engine splitMix = SplitMix64Engine.FromSeed(42UL);
        IRandomEngine[] engines = [pcg, xoshiro, philox, mt, numpyMt, splitMix];
        ulong[] expected =
        [
            pcg.Clone().NextUInt64(),
            xoshiro.Clone().NextUInt64(),
            philox.Clone().NextUInt64(),
            (ulong)mt.Clone().NextUInt32(),
            (ulong)numpyMt.Clone().NextUInt32(),
            splitMix.Clone().NextUInt64()
        ];
        RandomAlgorithm[] algorithms =
        [
            RandomAlgorithm.Pcg64DxsmV1,
            RandomAlgorithm.Xoshiro256StarStarV1,
            RandomAlgorithm.Philox4x64TenV1,
            RandomAlgorithm.Mt19937V1,
            RandomAlgorithm.Mt19937NumpyV1,
            RandomAlgorithm.SplitMix64V1
        ];

        for (int index = 0; index < engines.Length; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(engines[index].EngineInfo().Algorithm(), Is.EqualTo(algorithms[index]));
                Assert.That(engines[index].NextRawWord(), Is.EqualTo(expected[index]));
            });
        }
    }

    [Test]
    public void CloneAndStateFactoryResumeEveryEngineExactly()
    {
        foreach (IRandomEngine engine in CreateEngines())
        {
            engine.NextRawWord();
            IRandomEngine clone = engine.CloneEngine();
            IRandomEngine restored = RandomEngineFactory.FromState(engine.ExportEngineState());
            ulong expected = engine.NextRawWord();

            Assert.Multiple(() =>
            {
                Assert.That(clone.EngineInfo().Algorithm(), Is.EqualTo(engine.EngineInfo().Algorithm()));
                Assert.That(restored.EngineInfo().Algorithm(), Is.EqualTo(engine.EngineInfo().Algorithm()));
                Assert.That(clone.NextRawWord(), Is.EqualTo(expected));
                Assert.That(restored.NextRawWord(), Is.EqualTo(expected));
            });
        }
    }

    [Test]
    public void OptionalCapabilityInterfacesMatchEngineMetadata()
    {
        IRandomEngine pcg = Pcg64DxsmEngine.FromSeed((UInt128)1, (UInt128)2);
        IRandomEngine xoshiro = Xoshiro256StarStarEngine.FromSeed(3UL);
        IRandomEngine philox = Philox4x64TenEngine.FromKey((UInt128)4);
        IRandomEngine mt = Mt19937Engine.FromSeed(5U);
        IRandomEngine splitMix = SplitMix64Engine.FromSeed(6UL);

        Assert.Multiple(() =>
        {
            Assert.That(pcg, Is.InstanceOf<IAdvanceableRandomEngine>());
            Assert.That(splitMix, Is.InstanceOf<IAdvanceableRandomEngine>());
            Assert.That(xoshiro, Is.InstanceOf<IJumpableRandomEngine>());
            Assert.That(philox, Is.InstanceOf<ISeekableRandomEngine>());
            Assert.That(philox, Is.InstanceOf<IBlockReservableRandomEngine>());
            Assert.That(mt, Is.Not.InstanceOf<IAdvanceableRandomEngine>());
            Assert.That(mt, Is.Not.InstanceOf<IJumpableRandomEngine>());
            Assert.That(mt, Is.Not.InstanceOf<ISeekableRandomEngine>());
            Assert.That(mt, Is.Not.InstanceOf<IBlockReservableRandomEngine>());
        });
    }

    [Test]
    public void OptionalCapabilityCallsReachTheConcreteEngines()
    {
        IRandomEngine pcg = Pcg64DxsmEngine.FromSeed((UInt128)1, (UInt128)2);
        Pcg64DxsmEngine directPcg = Pcg64DxsmEngine.FromSeed((UInt128)1, (UInt128)2);
        ((IAdvanceableRandomEngine)pcg).AdvanceEngine((UInt128)1000);
        directPcg.Advance((UInt128)1000);

        IRandomEngine xoshiro = Xoshiro256StarStarEngine.FromSeed(4UL);
        Xoshiro256StarStarEngine directXoshiro = Xoshiro256StarStarEngine.FromSeed(4UL);
        ((IJumpableRandomEngine)xoshiro).JumpEngine();
        ((IJumpableRandomEngine)xoshiro).LongJumpEngine();
        directXoshiro.Jump();
        directXoshiro.LongJump();

        IRandomEngine philox = Philox4x64TenEngine.FromKey((UInt128)5);
        Philox4x64TenEngine directPhilox = Philox4x64TenEngine.FromKey((UInt128)5);
        UInt128 block = WideMath.FromParts(7UL, 6UL);
        ((ISeekableRandomEngine)philox).SeekEngine(block);
        directPhilox.Seek(block);
        ulong[] reservedStart = ((IBlockReservableRandomEngine)philox).ReserveBlocksEngine((UInt128)3);
        ulong[] directReservedStart = directPhilox.ReserveBlocks((UInt128)3);

        Assert.Multiple(() =>
        {
            Assert.That(pcg.NextRawWord(), Is.EqualTo(directPcg.NextUInt64()));
            Assert.That(xoshiro.NextRawWord(), Is.EqualTo(directXoshiro.NextUInt64()));
            Assert.That(reservedStart, Is.EqualTo(directReservedStart));
            Assert.That(philox.NextRawWord(), Is.EqualTo(directPhilox.NextUInt64()));
        });
    }

    [Test]
    public void StateFactoryRejectsNullWithThePublicArgumentConvention()
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => RandomEngineFactory.FromState(NullReference.Value<global::random.RandomState>()));
        Assert.That(error.ArgumentName(), Is.EqualTo("randomState"));
    }

    static IRandomEngine[] CreateEngines()
    {
        return
        [
            Pcg64DxsmEngine.FromSeed((UInt128)11, (UInt128)12),
            Xoshiro256StarStarEngine.FromSeed(13UL),
            Philox4x64TenEngine.FromKey((UInt128)14),
            Mt19937Engine.FromSeed(15U),
            Mt19937NumpyEngine.FromSeed(16U),
            SplitMix64Engine.FromSeed(17UL)
        ];
    }
}
