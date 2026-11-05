namespace random;

internal static class NumpySeedSequence
{
    internal const int PoolWordCount = 4;
    internal const uint InitialHashConstant = 0x43B0D7E5U;
    internal const uint HashMultiplier = 0x931E8875U;
    internal const uint InitialOutputConstant = 0x8B51F9DDU;
    internal const uint OutputMultiplier = 0x58F38DEDU;
    internal const uint LeftMixMultiplier = 0xCA01F9DDU;
    internal const uint RightMixMultiplier = 0x4973F715U;

    internal static uint[] GenerateState(uint[] entropy, int wordCount)
    {
        Guard.NotNull(entropy, "entropy");
        Guard.Count(wordCount, "wordCount");
        uint[] pool = MixEntropy(entropy);
        uint[] output = new uint[wordCount];
        uint hashConstant = InitialOutputConstant;
        int outputIndex = 0;
        while (outputIndex < output.Length)
        {
            uint value = pool[outputIndex % PoolWordCount] ^ hashConstant;
            hashConstant = Mt19937Engine.MultiplyModulo32(hashConstant, OutputMultiplier);
            value = Mt19937Engine.MultiplyModulo32(value, hashConstant);
            output[outputIndex] = value ^ (value >> 16);
            outputIndex++;
        }

        return output;
    }

    internal static uint[] MixEntropy(uint[] entropy)
    {
        uint[] pool = new uint[PoolWordCount];
        uint hashConstant = InitialHashConstant;
        int poolIndex = 0;
        while (poolIndex < PoolWordCount)
        {
            uint value = 0U;
            if (poolIndex < entropy.Length)
            {
                value = entropy[poolIndex];
            }

            pool[poolIndex] = HashMix(value, hashConstant);
            hashConstant = Mt19937Engine.MultiplyModulo32(hashConstant, HashMultiplier);
            poolIndex++;
        }

        int sourceIndex = 0;
        while (sourceIndex < PoolWordCount)
        {
            int destinationIndex = 0;
            while (destinationIndex < PoolWordCount)
            {
                if (sourceIndex != destinationIndex)
                {
                    uint hashed = HashMix(pool[sourceIndex], hashConstant);
                    hashConstant = Mt19937Engine.MultiplyModulo32(hashConstant, HashMultiplier);
                    pool[destinationIndex] = Mix(pool[destinationIndex], hashed);
                }

                destinationIndex++;
            }

            sourceIndex++;
        }

        sourceIndex = PoolWordCount;
        while (sourceIndex < entropy.Length)
        {
            int destinationIndex = 0;
            while (destinationIndex < PoolWordCount)
            {
                uint hashed = HashMix(entropy[sourceIndex], hashConstant);
                hashConstant = Mt19937Engine.MultiplyModulo32(hashConstant, HashMultiplier);
                pool[destinationIndex] = Mix(pool[destinationIndex], hashed);
                destinationIndex++;
            }

            sourceIndex++;
        }

        return pool;
    }

    internal static uint HashMix(uint value, uint hashConstant)
    {
        uint multiplier = Mt19937Engine.MultiplyModulo32(hashConstant, HashMultiplier);
        uint mixed = Mt19937Engine.MultiplyModulo32(value ^ hashConstant, multiplier);
        return mixed ^ (mixed >> 16);
    }

    internal static uint Mix(uint left, uint right)
    {
        uint leftProduct = Mt19937Engine.MultiplyModulo32(LeftMixMultiplier, left);
        uint rightProduct = Mt19937Engine.MultiplyModulo32(RightMixMultiplier, right);
        uint mixed = Mt19937Engine.SubtractModulo32(leftProduct, rightProduct);
        return mixed ^ (mixed >> 16);
    }
}
