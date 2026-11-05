namespace random;

internal class Mt19937NumpyEngine : IRandomEngine
{
    internal const int AlgorithmVersion = 1;
    Mt19937Engine engine;

    internal Mt19937NumpyEngine(Mt19937Engine engine)
    {
        this.engine = engine;
    }

    internal static Mt19937NumpyEngine FromSeed(uint seed)
    {
        return new Mt19937NumpyEngine(CreateEngine([seed]));
    }

    internal static Mt19937NumpyEngine FromSeedArray(uint[] seed)
    {
        Guard.NotNull(seed, "seed");
        return new Mt19937NumpyEngine(CreateEngine(seed));
    }

    internal static Mt19937NumpyEngine FromNumpyState(uint[] key, int position)
    {
        return new Mt19937NumpyEngine(Mt19937Engine.FromStateWords(key, position));
    }

    internal static Mt19937NumpyEngine FromState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        return FromStatePayload(randomState.Payload());
    }

    internal static Mt19937NumpyEngine FromStatePayload(byte[] payload)
    {
        Mt19937Engine.ValidatePayload(payload);
        return new Mt19937NumpyEngine(Mt19937Engine.FromStatePayload(payload));
    }

    internal uint NextUInt32()
    {
        return engine.NextUInt32();
    }

    internal Mt19937NumpyEngine Clone()
    {
        return new Mt19937NumpyEngine(engine.Clone());
    }

    internal RandomState ExportState()
    {
        return StateCodec.Create(
            RandomAlgorithm.Mt19937NumpyV1,
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            StatePayload());
    }

    internal byte[] StatePayload()
    {
        return engine.StatePayload();
    }

    internal void RestoreState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        RestoreStatePayload(randomState.Payload());
    }

    internal void RestoreStatePayload(byte[] payload)
    {
        Mt19937Engine restored = Mt19937Engine.FromStatePayload(payload);
        engine = restored;
    }

    internal uint[] StateWords()
    {
        return engine.StateWords();
    }

    internal int StateIndex()
    {
        return engine.StateIndex();
    }

    internal RandomAlgorithmInfo Info()
    {
        return new RandomAlgorithmInfo(
            RandomAlgorithm.Mt19937NumpyV1,
            "MT19937 (NumPy)",
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            32,
            Mt19937Engine.StateByteCount,
            false,
            "2^19937 - 1 32-bit outputs",
            [RandomCapability.State, RandomCapability.Clone]);
    }

    /// <summary>Returns the engine metadata through the common engine contract.</summary>
    /// <returns>The NumPy-compatible MT19937 metadata.</returns>
    public RandomAlgorithmInfo EngineInfo()
    {
        return Info();
    }

    /// <summary>Returns one native engine word through the common engine contract.</summary>
    /// <returns>The next 32-bit MT19937 output widened to 64 bits.</returns>
    public ulong NextRawWord()
    {
        return (ulong)NextUInt32();
    }

    /// <summary>Clones the engine through the common engine contract.</summary>
    /// <returns>An independent engine at the same position.</returns>
    public IRandomEngine CloneEngine()
    {
        return Clone();
    }

    /// <summary>Exports state through the common engine contract.</summary>
    /// <returns>The current NumPy-compatible MT19937 state.</returns>
    public RandomState ExportEngineState()
    {
        return ExportState();
    }

    internal static Mt19937Engine CreateEngine(uint[] entropy)
    {
        uint[] stateWords = NumpySeedSequence.GenerateState(entropy, Mt19937Engine.StateWordCount);
        stateWords[0] = Mt19937Engine.UpperMask;
        return Mt19937Engine.FromStateWords(stateWords, Mt19937Engine.StateWordCount - 1);
    }

    internal static void ValidateRandomState(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        if (randomState.Algorithm() != RandomAlgorithm.Mt19937NumpyV1)
        {
            throw RandomStateException.Create("The state belongs to a different random algorithm.");
        }

        if (randomState.AlgorithmVersion() != AlgorithmVersion)
        {
            throw RandomStateException.Create("The NumPy-compatible MT19937 algorithm version is not supported.");
        }

        if (randomState.TransformVersion() != StateCodec.CurrentTransformVersion)
        {
            throw RandomStateException.Create("The random transform version is not supported.");
        }

        Mt19937Engine.ValidatePayload(randomState.Payload());
    }
}
