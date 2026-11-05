namespace random.Tests;

public class RandomSourceInterfaceTests
{
    [Test]
    public void PublicContractStaysPrimitiveAndLifecycleFree()
    {
        System.Reflection.MethodInfo[] methods = typeof(IRandomSource).GetMethods();
        string[] names = methods.Select(method => method.Name).OrderBy(name => name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new string[]
            {
                "FillBytes",
                "MathMode",
                "NextBoolean",
                "NextUInt32",
                "NextUInt64"
            }));
            Assert.That(typeof(IRandomSource).IsAssignableFrom(typeof(RandomGenerator)), Is.True);
            Assert.That(typeof(NormalSampler).GetConstructors()[0].GetParameters()[0].ParameterType,
                Is.EqualTo(typeof(IRandomSource)));
            Assert.That(typeof(RandomCollections).GetMethod("Permutation", [typeof(IRandomSource), typeof(int)]),
                Is.Not.Null);
        });
    }

    [Test]
    public void RandomGeneratorImplementsThePublicSamplingContract()
    {
        IRandomSource source = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(source.MathMode(), Is.EqualTo(RandomMathMode.Portable));
            Assert.That(source, Is.InstanceOf<RandomGenerator>());
        });
    }

    [Test]
    public void DistributionCanBeTestedWithAFiveMethodScriptedSource()
    {
        ScriptedRandomSource source = new ScriptedRandomSource(
            RandomMathMode.Portable,
            new uint[0],
            [1UL << 52],
            new bool[0],
            new byte[0]);
        UniformSampler sampler = new UniformSampler(source, 10.0, 20.0);

        Assert.Multiple(() =>
        {
            Assert.That(sampler.Sample(), Is.EqualTo(15.0));
            Assert.That(source.UInt64Calls(), Is.EqualTo(1));
        });
    }

    [Test]
    public void CollectionSamplingCanScriptOnlyThePrimitiveItConsumes()
    {
        ScriptedRandomSource source = new ScriptedRandomSource(
            RandomMathMode.Portable,
            [uint.MaxValue],
            new ulong[0],
            new bool[0],
            new byte[0]);

        string selected = ReflectionInfo.Required<string>(RandomCollections.Choose(source, new string[] { "first", "last" }));

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.EqualTo("last"));
            Assert.That(source.UInt32Calls(), Is.EqualTo(1));
        });
    }

    [Test]
    public void StringFacadeUsesTheSourceByteContractDirectly()
    {
        ScriptedRandomSource source = new ScriptedRandomSource(
            RandomMathMode.System,
            new uint[0],
            new ulong[0],
            new bool[0],
            [0x12, 0xAB]);

        Assert.That(RandomStrings.Hex(source, 2), Is.EqualTo("12AB"));
    }

    sealed class ScriptedRandomSource : IRandomSource
    {
        RandomMathMode mathMode;
        uint[] uint32Values;
        ulong[] uint64Values;
        bool[] booleanValues;
        byte[] byteValues;
        int uint32Index;
        int uint64Index;
        int booleanIndex;

        internal ScriptedRandomSource(
            RandomMathMode mathMode,
            uint[] uint32Values,
            ulong[] uint64Values,
            bool[] booleanValues,
            byte[] byteValues)
        {
            this.mathMode = mathMode;
            this.uint32Values = uint32Values;
            this.uint64Values = uint64Values;
            this.booleanValues = booleanValues;
            this.byteValues = byteValues;
        }

        public RandomMathMode MathMode()
        {
            return mathMode;
        }

        public uint NextUInt32()
        {
            return uint32Values[uint32Index++];
        }

        public ulong NextUInt64()
        {
            return uint64Values[uint64Index++];
        }

        public bool NextBoolean()
        {
            return booleanValues[booleanIndex++];
        }

        public void FillBytes(Span<byte> destination)
        {
            byteValues.AsSpan(0, destination.Length).CopyTo(destination);
        }

        internal int UInt32Calls()
        {
            return uint32Index;
        }

        internal int UInt64Calls()
        {
            return uint64Index;
        }
    }
}
