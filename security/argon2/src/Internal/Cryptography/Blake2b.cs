using System;
using System.Security.Cryptography;

namespace argon2;

internal class Blake2b
{
    static readonly ulong[] InitializationVector =
    [
        0x6A09E667F3BCC908UL,
        0xBB67AE8584CAA73BUL,
        0x3C6EF372FE94F82BUL,
        0xA54FF53A5F1D36F1UL,
        0x510E527FADE682D1UL,
        0x9B05688C2B3E6C1FUL,
        0x1F83D9ABFB41BD6BUL,
        0x5BE0CD19137E2179UL
    ];

    static readonly byte[] MessageSchedule =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3,
        11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4,
        7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8,
        9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13,
        2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9,
        12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11,
        13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10,
        6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5,
        10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3
    ];

    ulong[] state = new ulong[8];
    byte[] buffer = new byte[128];
    int digestLength;
    ulong counterLow;
    ulong counterHigh;
    int bufferLength;
    bool finalized;

    internal Blake2b(int digestLength, ReadOnlySpan<byte> key)
    {
        if (digestLength < 1 || digestLength > 64)
        {
            throw new Argon2ParameterException("BLAKE2b digest length must be from 1 through 64 bytes.");
        }

        if (key.Length > 64)
        {
            throw new Argon2ParameterException("BLAKE2b key length must not exceed 64 bytes.");
        }

        this.digestLength = digestLength;

        int index = 0;
        while (index < state.Length)
        {
            state[index] = InitializationVector[index];
            index = index + 1;
        }

        ulong parameterBlock = 0x01010000UL
            | ((ulong)(uint)key.Length << 8)
            | (ulong)(uint)digestLength;
        state[0] = state[0] ^ parameterBlock;

        if (key.Length > 0)
        {
            key.CopyTo(new Span<byte>(buffer, 0, key.Length));
            bufferLength = buffer.Length;
        }
    }

    internal static void ComputeHash(ReadOnlySpan<byte> input, Span<byte> destination)
    {
        Blake2b hash = new Blake2b(destination.Length, ReadOnlySpan<byte>.Empty);
        hash.Update(input);
        hash.Finish(destination);
    }

    internal static void ComputeKeyedHash(
        ReadOnlySpan<byte> input,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        Blake2b hash = new Blake2b(destination.Length, key);
        hash.Update(input);
        hash.Finish(destination);
    }

    internal void Update(ReadOnlySpan<byte> input)
    {
        if (finalized)
        {
            throw new System.InvalidOperationException("The BLAKE2b hash has already been finalized.");
        }

        int inputOffset = 0;

        if (bufferLength > 0)
        {
            int available = buffer.Length - bufferLength;
            if (input.Length <= available)
            {
                input.CopyTo(new Span<byte>(buffer, bufferLength, input.Length));
                bufferLength = bufferLength + input.Length;
                return;
            }

            input.Slice(0, available).CopyTo(new Span<byte>(buffer, bufferLength, available));
            IncrementCounter(buffer.Length);
            Compress(buffer, false);
            CryptographicOperations.ZeroMemory(buffer);
            bufferLength = 0;
            inputOffset = available;
        }

        while (input.Length - inputOffset > buffer.Length)
        {
            IncrementCounter(buffer.Length);
            Compress(input.Slice(inputOffset, buffer.Length), false);
            inputOffset = inputOffset + buffer.Length;
        }

        int remaining = input.Length - inputOffset;
        input.Slice(inputOffset, remaining).CopyTo(new Span<byte>(buffer, 0, remaining));
        bufferLength = remaining;
    }

    internal void Finish(Span<byte> destination)
    {
        if (finalized)
        {
            throw new System.InvalidOperationException("The BLAKE2b hash has already been finalized.");
        }

        if (destination.Length != digestLength)
        {
            throw new Argon2ParameterException("The BLAKE2b destination length must match the configured digest length.");
        }

        IncrementCounter(bufferLength);
        Compress(buffer, true);

        int outputIndex = 0;
        while (outputIndex < destination.Length)
        {
            int wordIndex = outputIndex / 8;
            int byteIndex = outputIndex % 8;
            ulong value = (state[wordIndex] >> (byteIndex * 8)) & 0xFFUL;
            destination[outputIndex] = (byte)value;
            outputIndex = outputIndex + 1;
        }

        CryptographicOperations.ZeroMemory(buffer);
        Array.Clear(state);
        counterLow = 0UL;
        counterHigh = 0UL;
        bufferLength = 0;
        finalized = true;
    }

    void IncrementCounter(int byteCount)
    {
        ulong previous = counterLow;
        counterLow = ModularArithmetic.Add64(counterLow, (ulong)byteCount);
        if (counterLow < previous)
        {
            counterHigh = ModularArithmetic.Add64(counterHigh, 1UL);
        }
    }

    void Compress(ReadOnlySpan<byte> block, bool isFinal)
    {
        ulong[] message = new ulong[16];
        ulong[] working = new ulong[16];

        int index = 0;
        while (index < 16)
        {
            message[index] = ReadLittleEndian64(block, index * 8);
            index = index + 1;
        }

        index = 0;
        while (index < 8)
        {
            working[index] = state[index];
            working[index + 8] = InitializationVector[index];
            index = index + 1;
        }

        working[12] = working[12] ^ counterLow;
        working[13] = working[13] ^ counterHigh;
        if (isFinal)
        {
            working[14] = ~working[14];
        }

        int round = 0;
        while (round < 12)
        {
            int scheduleOffset = round * 16;
            Mix(
                working,
                0,
                4,
                8,
                12,
                message[MessageSchedule[scheduleOffset]],
                message[MessageSchedule[scheduleOffset + 1]]);
            Mix(
                working,
                1,
                5,
                9,
                13,
                message[MessageSchedule[scheduleOffset + 2]],
                message[MessageSchedule[scheduleOffset + 3]]);
            Mix(
                working,
                2,
                6,
                10,
                14,
                message[MessageSchedule[scheduleOffset + 4]],
                message[MessageSchedule[scheduleOffset + 5]]);
            Mix(
                working,
                3,
                7,
                11,
                15,
                message[MessageSchedule[scheduleOffset + 6]],
                message[MessageSchedule[scheduleOffset + 7]]);
            Mix(
                working,
                0,
                5,
                10,
                15,
                message[MessageSchedule[scheduleOffset + 8]],
                message[MessageSchedule[scheduleOffset + 9]]);
            Mix(
                working,
                1,
                6,
                11,
                12,
                message[MessageSchedule[scheduleOffset + 10]],
                message[MessageSchedule[scheduleOffset + 11]]);
            Mix(
                working,
                2,
                7,
                8,
                13,
                message[MessageSchedule[scheduleOffset + 12]],
                message[MessageSchedule[scheduleOffset + 13]]);
            Mix(
                working,
                3,
                4,
                9,
                14,
                message[MessageSchedule[scheduleOffset + 14]],
                message[MessageSchedule[scheduleOffset + 15]]);
            round = round + 1;
        }

        index = 0;
        while (index < 8)
        {
            state[index] = state[index] ^ working[index] ^ working[index + 8];
            index = index + 1;
        }

        Array.Clear(message);
        Array.Clear(working);
    }

    static void Mix(
        ulong[] working,
        int a,
        int b,
        int c,
        int d,
        ulong firstMessageWord,
        ulong secondMessageWord)
    {
        working[a] = ModularArithmetic.Add64(
            ModularArithmetic.Add64(working[a], working[b]),
            firstMessageWord);
        working[d] = ModularArithmetic.RotateRight64(working[d] ^ working[a], 32);
        working[c] = ModularArithmetic.Add64(working[c], working[d]);
        working[b] = ModularArithmetic.RotateRight64(working[b] ^ working[c], 24);
        working[a] = ModularArithmetic.Add64(
            ModularArithmetic.Add64(working[a], working[b]),
            secondMessageWord);
        working[d] = ModularArithmetic.RotateRight64(working[d] ^ working[a], 16);
        working[c] = ModularArithmetic.Add64(working[c], working[d]);
        working[b] = ModularArithmetic.RotateRight64(working[b] ^ working[c], 63);
    }

    static ulong ReadLittleEndian64(ReadOnlySpan<byte> source, int offset)
    {
        return (ulong)source[offset]
            | ((ulong)source[offset + 1] << 8)
            | ((ulong)source[offset + 2] << 16)
            | ((ulong)source[offset + 3] << 24)
            | ((ulong)source[offset + 4] << 32)
            | ((ulong)source[offset + 5] << 40)
            | ((ulong)source[offset + 6] << 48)
            | ((ulong)source[offset + 7] << 56);
    }
}
