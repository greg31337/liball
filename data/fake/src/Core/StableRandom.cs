using System;
using System.Text;

namespace fake;

internal static class WrappingArithmetic
{
    internal static ulong Add(ulong left, ulong right)
    {
        ulong lower = (left & uint.MaxValue) + (right & uint.MaxValue);
        ulong upper = (left >> 32) + (right >> 32) + (lower >> 32);
        return (lower & uint.MaxValue) | ((upper & uint.MaxValue) << 32);
    }

    internal static ulong Multiply(ulong left, ulong right)
    {
        uint leftLow = (uint)(left & uint.MaxValue);
        uint rightLow = (uint)(right & uint.MaxValue);
        uint leftHigh = (uint)(left >> 32);
        uint rightHigh = (uint)(right >> 32);
        ulong lower = (ulong)leftLow * rightLow;
        ulong firstCross = ((ulong)leftHigh * rightLow) & uint.MaxValue;
        ulong secondCross = ((ulong)leftLow * rightHigh) & uint.MaxValue;
        ulong cross = (firstCross + secondCross) & uint.MaxValue;
        return Add(lower, cross << 32);
    }
}

internal static class StableSeed
{
    const ulong FnvOffset = 14695981039346656037UL;
    const ulong FnvPrime = 1099511628211UL;

    internal static ulong FromInt(int seed)
    {
        return FromLong((long)seed);
    }

    internal static ulong FromLong(long seed)
    {
        if (seed >= 0)
        {
            return (ulong)seed;
        }
        return ulong.MaxValue - (ulong)(-(seed + 1));
    }

    internal static ulong FromString(string seed)
    {
        Guard.NotNull(seed, "seed");
        byte[] bytes = Encoding.UTF8.GetBytes(seed);
        return FromBytes(bytes);
    }

    internal static ulong FromBytes(byte[] seed)
    {
        Guard.NotNull(seed, "seed");
        ulong hash = FnvOffset;
        int index = 0;
        while (index < seed.Length)
        {
            hash = hash ^ seed[index];
            hash = WrappingArithmetic.Multiply(hash, FnvPrime);
            index = index + 1;
        }
        return hash;
    }

    internal static ulong Derive(ulong parentSeed, string domain, string name)
    {
        Guard.NotNullOrWhiteSpace(domain, "domain");
        Guard.NotNullOrWhiteSpace(name, "name");
        ulong hash = FnvOffset;
        hash = Append(hash, Encoding.UTF8.GetBytes("fake.stream.v1"));
        hash = Append(hash, new byte[] { 0 });
        int shift = 0;
        while (shift < 64)
        {
            hash = hash ^ (byte)((parentSeed >> shift) & byte.MaxValue);
            hash = WrappingArithmetic.Multiply(hash, FnvPrime);
            shift = shift + 8;
        }
        hash = Append(hash, new byte[] { 0 });
        hash = Append(hash, Encoding.UTF8.GetBytes(domain));
        hash = Append(hash, new byte[] { 0 });
        return Append(hash, Encoding.UTF8.GetBytes(name));
    }

    internal static long ToLong(ulong seed)
    {
        if (seed <= (ulong)long.MaxValue)
        {
            return (long)seed;
        }
        return -((long)(ulong.MaxValue - seed)) - 1L;
    }

    static ulong Append(ulong hash, byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            hash = hash ^ bytes[index];
            hash = WrappingArithmetic.Multiply(hash, FnvPrime);
            index = index + 1;
        }
        return hash;
    }
}

internal class StableRandom
{
    internal ulong InitialSeed;
    ulong State0;
    ulong State1;
    ulong State2;
    ulong State3;
    ulong SplitState;

    internal StableRandom(ulong seed)
    {
        InitialSeed = seed;
        Reset();
    }

    internal ulong Next()
    {
        ulong result = WrappingArithmetic.Multiply(
            RotateLeft(WrappingArithmetic.Multiply(State1, 5UL), 7),
            9UL);
        ulong shifted = State1 << 17;
        State2 = State2 ^ State0;
        State3 = State3 ^ State1;
        State1 = State1 ^ State2;
        State0 = State0 ^ State3;
        State2 = State2 ^ shifted;
        State3 = RotateLeft(State3, 45);
        return result;
    }

    internal void Fill(Span<byte> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            ulong value = Next();
            int byteIndex = 0;
            while (index < destination.Length && byteIndex < 8)
            {
                destination[index] = (byte)(value & byte.MaxValue);
                value = value >> 8;
                index = index + 1;
                byteIndex = byteIndex + 1;
            }
        }
    }

    internal void Reseed(ulong seed)
    {
        InitialSeed = seed;
        Reset();
    }

    internal void Reset()
    {
        SplitState = InitialSeed;
        State0 = SplitMix();
        State1 = SplitMix();
        State2 = SplitMix();
        State3 = SplitMix();
        if (State0 == 0UL && State1 == 0UL && State2 == 0UL && State3 == 0UL)
        {
            State3 = 11400714819323198485UL;
        }
    }

    internal FakeState Snapshot()
    {
        return new FakeState(FakeState.StableAlgorithm, InitialSeed, State0, State1, State2, State3);
    }

    internal void Restore(FakeState state)
    {
        InitialSeed = state.InitialSeed;
        State0 = state.State0;
        State1 = state.State1;
        State2 = state.State2;
        State3 = state.State3;
        SplitState = InitialSeed;
    }

    ulong SplitMix()
    {
        SplitState = WrappingArithmetic.Add(SplitState, 11400714819323198485UL);
        ulong value = SplitState;
        value = WrappingArithmetic.Multiply(
            value ^ (value >> 30),
            13787848793156543929UL);
        value = WrappingArithmetic.Multiply(
            value ^ (value >> 27),
            10723151780598845931UL);
        return value ^ (value >> 31);
    }

    static ulong RotateLeft(ulong value, int count)
    {
        return (value << count) | (value >> (64 - count));
    }
}
