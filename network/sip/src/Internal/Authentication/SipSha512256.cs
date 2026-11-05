namespace sip;

internal static class SipSha512256
{
    static readonly ulong[] RoundConstants = new ulong[]
    {
        0x428a2f98d728ae22UL, 0x7137449123ef65cdUL,
        0xb5c0fbcfec4d3b2fUL, 0xe9b5dba58189dbbcUL,
        0x3956c25bf348b538UL, 0x59f111f1b605d019UL,
        0x923f82a4af194f9bUL, 0xab1c5ed5da6d8118UL,
        0xd807aa98a3030242UL, 0x12835b0145706fbeUL,
        0x243185be4ee4b28cUL, 0x550c7dc3d5ffb4e2UL,
        0x72be5d74f27b896fUL, 0x80deb1fe3b1696b1UL,
        0x9bdc06a725c71235UL, 0xc19bf174cf692694UL,
        0xe49b69c19ef14ad2UL, 0xefbe4786384f25e3UL,
        0x0fc19dc68b8cd5b5UL, 0x240ca1cc77ac9c65UL,
        0x2de92c6f592b0275UL, 0x4a7484aa6ea6e483UL,
        0x5cb0a9dcbd41fbd4UL, 0x76f988da831153b5UL,
        0x983e5152ee66dfabUL, 0xa831c66d2db43210UL,
        0xb00327c898fb213fUL, 0xbf597fc7beef0ee4UL,
        0xc6e00bf33da88fc2UL, 0xd5a79147930aa725UL,
        0x06ca6351e003826fUL, 0x142929670a0e6e70UL,
        0x27b70a8546d22ffcUL, 0x2e1b21385c26c926UL,
        0x4d2c6dfc5ac42aedUL, 0x53380d139d95b3dfUL,
        0x650a73548baf63deUL, 0x766a0abb3c77b2a8UL,
        0x81c2c92e47edaee6UL, 0x92722c851482353bUL,
        0xa2bfe8a14cf10364UL, 0xa81a664bbc423001UL,
        0xc24b8b70d0f89791UL, 0xc76c51a30654be30UL,
        0xd192e819d6ef5218UL, 0xd69906245565a910UL,
        0xf40e35855771202aUL, 0x106aa07032bbd1b8UL,
        0x19a4c116b8d2d0c8UL, 0x1e376c085141ab53UL,
        0x2748774cdf8eeb99UL, 0x34b0bcb5e19b48a8UL,
        0x391c0cb3c5c95a63UL, 0x4ed8aa4ae3418acbUL,
        0x5b9cca4f7763e373UL, 0x682e6ff3d6b2b8a3UL,
        0x748f82ee5defb2fcUL, 0x78a5636f43172f60UL,
        0x84c87814a1f0ab72UL, 0x8cc702081a6439ecUL,
        0x90befffa23631e28UL, 0xa4506cebde82bde9UL,
        0xbef9a3f7b2c67915UL, 0xc67178f2e372532bUL,
        0xca273eceea26619cUL, 0xd186b8c721c0c207UL,
        0xeada7dd6cde0eb1eUL, 0xf57d4f7fee6ed178UL,
        0x06f067aa72176fbaUL, 0x0a637dc5a2c898a6UL,
        0x113f9804bef90daeUL, 0x1b710b35131c471bUL,
        0x28db77f523047d84UL, 0x32caab7b40c72493UL,
        0x3c9ebe0a15c9bebcUL, 0x431d67c49c100d4cUL,
        0x4cc5d4becb3e42b6UL, 0x597f299cfc657e2aUL,
        0x5fcb6fab3ad6faecUL, 0x6c44198c4a475817UL
    };

    internal static byte[] Hash(byte[] value)
    {
        int remainder = value.Length % 128;
        int padding = remainder < 112 ? 128 - remainder : 256 - remainder;
        byte[] padded = new byte[value.Length + padding];
        System.Buffer.BlockCopy(value, 0, padded, 0, value.Length);
        padded[value.Length] = 0x80;
        ulong bitLength = (ulong)value.Length * 8UL;
        WriteUInt64(padded, padded.Length - 8, bitLength);

        ulong[] state = new ulong[]
        {
            0x22312194fc2bf72cUL, 0x9f555fa3c84c64c2UL,
            0x2393b86b6f53b151UL, 0x963877195940eabdUL,
            0x96283ee2a88effe3UL, 0xbe5e1e2553863992UL,
            0x2b0199fc2c85b8aaUL, 0x0eb72ddc81c52ca2UL
        };

        int offset = 0;
        while (offset < padded.Length)
        {
            Compress(padded, offset, state);
            offset = offset + 128;
        }

        byte[] result = new byte[32];
        int index = 0;
        while (index < 4)
        {
            WriteUInt64(result, index * 8, state[index]);
            index = index + 1;
        }
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(padded);
        System.Array.Clear(state);
        return result;
    }

    static void Compress(byte[] source, int offset, ulong[] state)
    {
        ulong[] words = new ulong[80];
        int index = 0;
        while (index < 16)
        {
            words[index] = ReadUInt64(source, offset + index * 8);
            index = index + 1;
        }
        while (index < 80)
        {
            ulong first = SmallSigmaOne(words[index - 2]);
            ulong second = words[index - 7];
            ulong third = SmallSigmaZero(words[index - 15]);
            words[index] = Add(Add(first, second), Add(third, words[index - 16]));
            index = index + 1;
        }

        ulong a = state[0];
        ulong b = state[1];
        ulong c = state[2];
        ulong d = state[3];
        ulong e = state[4];
        ulong f = state[5];
        ulong g = state[6];
        ulong h = state[7];
        index = 0;
        while (index < 80)
        {
            ulong first = Add(h, BigSigmaOne(e));
            ulong second = Add(Choose(e, f, g), RoundConstants[index]);
            ulong temporaryOne = Add(Add(first, second), words[index]);
            ulong temporaryTwo = Add(BigSigmaZero(a), Majority(a, b, c));
            h = g;
            g = f;
            f = e;
            e = Add(d, temporaryOne);
            d = c;
            c = b;
            b = a;
            a = Add(temporaryOne, temporaryTwo);
            index = index + 1;
        }

        state[0] = Add(state[0], a);
        state[1] = Add(state[1], b);
        state[2] = Add(state[2], c);
        state[3] = Add(state[3], d);
        state[4] = Add(state[4], e);
        state[5] = Add(state[5], f);
        state[6] = Add(state[6], g);
        state[7] = Add(state[7], h);
        System.Array.Clear(words);
    }

    static ulong Add(ulong left, ulong right)
    {
        ulong low = (left & 0xffffffffUL) + (right & 0xffffffffUL);
        ulong high = (left >> 32) + (right >> 32) + (low >> 32);
        return high << 32 | low & 0xffffffffUL;
    }

    static ulong Choose(ulong first, ulong second, ulong third)
    {
        return (first & second) ^ (~first & third);
    }

    static ulong Majority(ulong first, ulong second, ulong third)
    {
        return (first & second) ^ (first & third) ^ (second & third);
    }

    static ulong BigSigmaZero(ulong value)
    {
        return System.Numerics.BitOperations.RotateRight(value, 28)
            ^ System.Numerics.BitOperations.RotateRight(value, 34)
            ^ System.Numerics.BitOperations.RotateRight(value, 39);
    }

    static ulong BigSigmaOne(ulong value)
    {
        return System.Numerics.BitOperations.RotateRight(value, 14)
            ^ System.Numerics.BitOperations.RotateRight(value, 18)
            ^ System.Numerics.BitOperations.RotateRight(value, 41);
    }

    static ulong SmallSigmaZero(ulong value)
    {
        return System.Numerics.BitOperations.RotateRight(value, 1)
            ^ System.Numerics.BitOperations.RotateRight(value, 8)
            ^ value >> 7;
    }

    static ulong SmallSigmaOne(ulong value)
    {
        return System.Numerics.BitOperations.RotateRight(value, 19)
            ^ System.Numerics.BitOperations.RotateRight(value, 61)
            ^ value >> 6;
    }

    static ulong ReadUInt64(byte[] source, int offset)
    {
        ulong value = 0UL;
        int index = 0;
        while (index < 8)
        {
            value = value << 8 | source[offset + index];
            index = index + 1;
        }
        return value;
    }

    static void WriteUInt64(byte[] destination, int offset, ulong value)
    {
        int index = 7;
        while (index >= 0)
        {
            destination[offset + index] = (byte)(value & 0xffUL);
            value = value >> 8;
            index = index - 1;
        }
    }
}
