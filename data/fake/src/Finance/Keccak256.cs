namespace fake;

internal static class Keccak256
{
    static readonly ulong[] RoundConstants = new ulong[]
    {
        0x0000000000000001UL, 0x0000000000008082UL,
        0x800000000000808AUL, 0x8000000080008000UL,
        0x000000000000808BUL, 0x0000000080000001UL,
        0x8000000080008081UL, 0x8000000000008009UL,
        0x000000000000008AUL, 0x0000000000000088UL,
        0x0000000080008009UL, 0x000000008000000AUL,
        0x000000008000808BUL, 0x800000000000008BUL,
        0x8000000000008089UL, 0x8000000000008003UL,
        0x8000000000008002UL, 0x8000000000000080UL,
        0x000000000000800AUL, 0x800000008000000AUL,
        0x8000000080008081UL, 0x8000000000008080UL,
        0x0000000080000001UL, 0x8000000080008008UL
    };
    static readonly int[] Rotations = new int[]
    {
        0, 1, 62, 28, 27,
        36, 44, 6, 55, 20,
        3, 10, 43, 25, 39,
        41, 45, 15, 21, 8,
        18, 2, 61, 56, 14
    };

    internal static byte[] Hash(byte[] input)
    {
        Guard.NotNull(input, "input");
        const int Rate = 136;
        ulong[] state = new ulong[25];
        int offset = 0;
        while (input.Length - offset >= Rate)
        {
            Absorb(state, input, offset, Rate);
            Permute(state);
            offset = offset + Rate;
        }
        byte[] finalBlock = new byte[Rate];
        int remaining = input.Length - offset;
        int index = 0;
        while (index < remaining)
        {
            finalBlock[index] = input[offset + index];
            index = index + 1;
        }
        finalBlock[remaining] = 0x01;
        finalBlock[Rate - 1] = (byte)(finalBlock[Rate - 1] | 0x80);
        Absorb(state, finalBlock, 0, Rate);
        Permute(state);
        byte[] result = new byte[32];
        index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)((state[index / 8] >> ((index % 8) * 8)) & byte.MaxValue);
            index = index + 1;
        }
        return result;
    }

    static void Absorb(
        ulong[] state,
        byte[] input,
        int offset,
        int length)
    {
        int index = 0;
        while (index < length)
        {
            state[index / 8] = state[index / 8] ^
                ((ulong)input[offset + index] << ((index % 8) * 8));
            index = index + 1;
        }
    }

    static void Permute(ulong[] state)
    {
        ulong[] columns = new ulong[5];
        ulong[] differences = new ulong[5];
        ulong[] moved = new ulong[25];
        int round = 0;
        while (round < RoundConstants.Length)
        {
            int x = 0;
            while (x < 5)
            {
                columns[x] = state[x] ^ state[x + 5] ^ state[x + 10] ^
                    state[x + 15] ^ state[x + 20];
                x = x + 1;
            }
            x = 0;
            while (x < 5)
            {
                differences[x] = columns[(x + 4) % 5] ^
                    Rotate(columns[(x + 1) % 5], 1);
                x = x + 1;
            }
            int y = 0;
            while (y < 5)
            {
                x = 0;
                while (x < 5)
                {
                    state[x + 5 * y] = state[x + 5 * y] ^ differences[x];
                    x = x + 1;
                }
                y = y + 1;
            }
            y = 0;
            while (y < 5)
            {
                x = 0;
                while (x < 5)
                {
                    int destinationX = y;
                    int destinationY = (2 * x + 3 * y) % 5;
                    moved[destinationX + 5 * destinationY] = Rotate(
                        state[x + 5 * y],
                        Rotations[x + 5 * y]);
                    x = x + 1;
                }
                y = y + 1;
            }
            y = 0;
            while (y < 5)
            {
                x = 0;
                while (x < 5)
                {
                    state[x + 5 * y] = moved[x + 5 * y] ^
                        ((~moved[(x + 1) % 5 + 5 * y]) &
                            moved[(x + 2) % 5 + 5 * y]);
                    x = x + 1;
                }
                y = y + 1;
            }
            state[0] = state[0] ^ RoundConstants[round];
            round = round + 1;
        }
    }

    static ulong Rotate(ulong value, int count)
    {
        return count == 0
            ? value
            : value << count | value >> (64 - count);
    }
}
