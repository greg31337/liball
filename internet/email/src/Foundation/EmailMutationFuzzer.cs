using System;
using System.Collections.Generic;

namespace email;

internal enum EmailMutationKind
{
    FlipBit = 0,
    ReplaceByte = 1,
    InsertBytes = 2,
    DeleteBytes = 3,
    DuplicateBytes = 4,
    Truncate = 5,
    SwapAdjacent = 6
}

internal class EmailMutationFuzzOptions
{
    internal const int DefaultMutationCount = 64;
    internal const int DefaultMaximumCaseBytes = 65536;
    internal const int DefaultMaximumMutationBytes = 32;

    long deterministicSeed;
    int mutationCount;
    int maximumCaseBytes;
    int maximumMutationBytes;

    internal EmailMutationFuzzOptions()
    {
        deterministicSeed = 0x5eed154;
        mutationCount = DefaultMutationCount;
        maximumCaseBytes = DefaultMaximumCaseBytes;
        maximumMutationBytes = DefaultMaximumMutationBytes;
    }

    internal long GetDeterministicSeed()
    {
        return deterministicSeed;
    }

    internal void SetDeterministicSeed(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        deterministicSeed = value;
    }

    internal int GetMutationCount()
    {
        return mutationCount;
    }

    internal void SetMutationCount(int value)
    {
        mutationCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    internal int GetMaximumCaseBytes()
    {
        return maximumCaseBytes;
    }

    internal void SetMaximumCaseBytes(int value)
    {
        maximumCaseBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    internal int GetMaximumMutationBytes()
    {
        return maximumMutationBytes;
    }

    internal void SetMaximumMutationBytes(int value)
    {
        maximumMutationBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    internal EmailMutationFuzzOptions Clone()
    {
        EmailMutationFuzzOptions copy = new EmailMutationFuzzOptions();
        copy.SetDeterministicSeed(deterministicSeed);
        copy.SetMutationCount(mutationCount);
        copy.SetMaximumCaseBytes(maximumCaseBytes);
        copy.SetMaximumMutationBytes(maximumMutationBytes);
        return copy;
    }
}

internal class EmailMutationCase
{
    int caseIndex;
    EmailMutationKind kind;
    int offset;
    int sourceByteCount;
    byte[] bytes;

    internal EmailMutationCase(int caseIndex, EmailMutationKind kind, int offset, int sourceByteCount, byte[] bytes)
    {
        if (caseIndex < 0)
        {
            throw new ArgumentOutOfRangeException("caseIndex");
        }

        if (kind < EmailMutationKind.FlipBit || kind > EmailMutationKind.SwapAdjacent)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (offset < 0 || sourceByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        this.caseIndex = caseIndex;
        this.kind = kind;
        this.offset = offset;
        this.sourceByteCount = sourceByteCount;
        this.bytes = Copy(bytes);
    }

    internal int GetCaseIndex()
    {
        return caseIndex;
    }

    internal EmailMutationKind GetKind()
    {
        return kind;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetSourceByteCount()
    {
        return sourceByteCount;
    }

    internal byte[] GetBytes()
    {
        return Copy(bytes);
    }

    internal string GetReplayIdentifier()
    {
        return caseIndex + ":" + kind + ":" + offset + ":" + sourceByteCount + ":" + bytes.Length;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}

internal static class EmailMutationFuzzer
{
    internal static List<EmailMutationCase> Generate(byte[] seed, EmailMutationFuzzOptions options)
    {
        if (seed == null)
        {
            throw new ArgumentNullException("seed");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailMutationFuzzOptions snapshot = options.Clone();
        if (seed.Length > snapshot.GetMaximumCaseBytes())
        {
            throw new EmailLimitException("MaxFuzzCaseBytes", snapshot.GetMaximumCaseBytes(), seed.Length);
        }

        EmailMutationRandom random = new EmailMutationRandom((ulong)snapshot.GetDeterministicSeed() ^ MixLength(seed.Length));

        List<EmailMutationCase> cases = new List<EmailMutationCase>();
        int index = 0;
        while (index < snapshot.GetMutationCount())
        {
            EmailMutationKind kind = (EmailMutationKind)(index % 7);
            EmailMutationValue mutation = Mutate(seed, kind, snapshot, random);
            cases.Add(new EmailMutationCase(index, kind, mutation.GetOffset(), seed.Length, mutation.GetBytes()));
            index++;
        }

        return cases;
    }

    static EmailMutationValue Mutate(byte[] seed, EmailMutationKind kind, EmailMutationFuzzOptions options, EmailMutationRandom random)
    {
        if (kind == EmailMutationKind.FlipBit)
        {
            return FlipBit(seed, random);
        }

        if (kind == EmailMutationKind.ReplaceByte)
        {
            return ReplaceByte(seed, random);
        }

        if (kind == EmailMutationKind.InsertBytes)
        {
            return InsertBytes(seed, options, random);
        }

        if (kind == EmailMutationKind.DeleteBytes)
        {
            return DeleteBytes(seed, options, random);
        }

        if (kind == EmailMutationKind.DuplicateBytes)
        {
            return DuplicateBytes(seed, options, random);
        }

        if (kind == EmailMutationKind.Truncate)
        {
            return Truncate(seed, random);
        }

        return SwapAdjacent(seed, options, random);
    }

    static EmailMutationValue FlipBit(byte[] seed, EmailMutationRandom random)
    {
        if (seed.Length == 0)
        {
            return new EmailMutationValue(0, new byte[] { random.NextByte() });
        }

        byte[] output = Copy(seed);
        int offset = random.NextInt(seed.Length);
        output[offset] = (byte)(output[offset] ^ (1 << random.NextInt(8)));
        return new EmailMutationValue(offset, output);
    }

    static EmailMutationValue ReplaceByte(byte[] seed, EmailMutationRandom random)
    {
        if (seed.Length == 0)
        {
            return new EmailMutationValue(0, new byte[] { random.NextByte() });
        }

        byte[] output = Copy(seed);
        int offset = random.NextInt(seed.Length);
        output[offset] = random.NextByte();
        return new EmailMutationValue(offset, output);
    }

    static EmailMutationValue InsertBytes(byte[] seed, EmailMutationFuzzOptions options, EmailMutationRandom random)
    {
        int available = options.GetMaximumCaseBytes() - seed.Length;
        if (available <= 0)
        {
            return ReplaceByte(seed, random);
        }

        int count = 1 + random.NextInt(Minimum(options.GetMaximumMutationBytes(), available));
        int offset = random.NextInt(seed.Length + 1);
        byte[] output = new byte[seed.Length + count];
        CopyRange(seed, 0, output, 0, offset);
        int index = 0;
        while (index < count)
        {
            output[offset + index] = random.NextByte();
            index++;
        }

        CopyRange(seed, offset, output, offset + count, seed.Length - offset);
        return new EmailMutationValue(offset, output);
    }

    static EmailMutationValue DeleteBytes(byte[] seed, EmailMutationFuzzOptions options, EmailMutationRandom random)
    {
        if (seed.Length == 0)
        {
            return new EmailMutationValue(0, new byte[0]);
        }

        int offset = random.NextInt(seed.Length);
        int count = 1 + random.NextInt(Minimum(options.GetMaximumMutationBytes(), seed.Length - offset));
        byte[] output = new byte[seed.Length - count];
        CopyRange(seed, 0, output, 0, offset);
        CopyRange(seed, offset + count, output, offset, seed.Length - offset - count);
        return new EmailMutationValue(offset, output);
    }

    static EmailMutationValue DuplicateBytes(byte[] seed, EmailMutationFuzzOptions options, EmailMutationRandom random)
    {
        int available = options.GetMaximumCaseBytes() - seed.Length;
        if (seed.Length == 0 || available <= 0)
        {
            return InsertBytes(seed, options, random);
        }

        int offset = random.NextInt(seed.Length);
        int count = 1 + random.NextInt(Minimum(Minimum(options.GetMaximumMutationBytes(), seed.Length - offset), available));
        int insertion = random.NextInt(seed.Length + 1);
        byte[] output = new byte[seed.Length + count];
        CopyRange(seed, 0, output, 0, insertion);
        CopyRange(seed, offset, output, insertion, count);
        CopyRange(seed, insertion, output, insertion + count, seed.Length - insertion);
        return new EmailMutationValue(offset, output);
    }

    static EmailMutationValue Truncate(byte[] seed, EmailMutationRandom random)
    {
        int offset = random.NextInt(seed.Length + 1);
        byte[] output = new byte[offset];
        CopyRange(seed, 0, output, 0, offset);
        return new EmailMutationValue(offset, output);
    }

    static EmailMutationValue SwapAdjacent(byte[] seed, EmailMutationFuzzOptions options, EmailMutationRandom random)
    {
        if (seed.Length < 2)
        {
            return InsertBytes(seed, options, random);
        }

        byte[] output = Copy(seed);
        int offset = random.NextInt(seed.Length - 1);
        byte current = output[offset];
        output[offset] = output[offset + 1];
        output[offset + 1] = current;
        return new EmailMutationValue(offset, output);
    }

    static ulong MixLength(int length)
    {
        ulong value = AddWrapped((ulong)length, 0x9e3779b97f4a7c15UL);
        value = MultiplyWrapped(value ^ (value >> 30), 0xbf58476d1ce4e5b9UL);
        value = MultiplyWrapped(value ^ (value >> 27), 0x94d049bb133111ebUL);
        return value ^ (value >> 31);
    }

    static ulong AddWrapped(ulong left, ulong right)
    {
        return (ulong)(((UInt128)left + right) & ulong.MaxValue);
    }

    static ulong MultiplyWrapped(ulong left, ulong right)
    {
        return (ulong)(((UInt128)left * right) & ulong.MaxValue);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }

    static void CopyRange(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int count)
    {
        if (count > 0)
        {
            Array.Copy(source, sourceOffset, destination, destinationOffset, count);
        }
    }

    static int Minimum(int left, int right)
    {
        return left < right ? left : right;
    }
}
internal class EmailMutationValue
{
    int offset;
    byte[] bytes;

    internal EmailMutationValue(int offset, byte[] bytes)
    {
        this.offset = offset;
        this.bytes = bytes;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal byte[] GetBytes()
    {
        return bytes;
    }
}
internal class EmailMutationRandom
{
    ulong state;

    internal EmailMutationRandom(ulong value)
    {
        state = value == 0 ? 0x9e3779b97f4a7c15UL : value;
    }

    internal int NextInt(int maximum)
    {
        return (int)(Next() % (ulong)maximum);
    }

    internal byte NextByte()
    {
        return (byte)(Next() & 255);
    }

    ulong Next()
    {
        state ^= state >> 12;
        state ^= state << 25;
        state ^= state >> 27;
        return MultiplyWrapped(state, 2685821657736338717UL);
    }

    static ulong MultiplyWrapped(ulong left, ulong right)
    {
        return (ulong)(((UInt128)left * right) & ulong.MaxValue);
    }
}
