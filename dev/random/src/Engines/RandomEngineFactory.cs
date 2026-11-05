namespace random;

internal static class RandomEngineFactory
{
    internal static IRandomEngine FromState(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        switch (randomState.Algorithm())
        {
            case RandomAlgorithm.Pcg64DxsmV1:
                return Pcg64DxsmEngine.FromState(randomState);
            case RandomAlgorithm.Xoshiro256StarStarV1:
                return Xoshiro256StarStarEngine.FromState(randomState);
            case RandomAlgorithm.Philox4x64TenV1:
                return Philox4x64TenEngine.FromState(randomState);
            case RandomAlgorithm.Mt19937V1:
                return Mt19937Engine.FromState(randomState);
            case RandomAlgorithm.Mt19937NumpyV1:
                return Mt19937NumpyEngine.FromState(randomState);
            case RandomAlgorithm.SplitMix64V1:
                return SplitMix64Engine.FromState(randomState);
            default:
                throw RandomStateException.Create("The state algorithm is not supported by a deterministic engine.");
        }
    }
}
