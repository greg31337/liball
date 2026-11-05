using System;

namespace fake;

internal static class NanoIdGenerator
{
    internal const int DefaultLength = 21;
    internal const int ShortLength = 10;
    internal const int MaximumAlphabetLength = 256;
    internal const string DefaultAlphabet = "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    internal static string Generate(FakeRandom random, int length, string alphabet, int maximumOutputSize, int maximumCount)
    {
        ValidateAlphabetSize(alphabet);
        Guard.OutputSize(length, maximumOutputSize, "length");
        return CharacterStringGenerator.Generate(
            random,
            alphabet,
            length,
            true,
            maximumOutputSize,
            maximumCount,
            true);
    }

    internal static int Write(FakeRandom random, Span<char> destination, string alphabet, int maximumOutputSize, int maximumCount)
    {
        ValidateAlphabetSize(alphabet);
        return CharacterStringGenerator.Write(
            random,
            destination,
            alphabet,
            true,
            maximumOutputSize,
            maximumCount,
            true);
    }

    static void ValidateAlphabetSize(string alphabet)
    {
        Guard.NotNullOrEmpty(alphabet, "alphabet");
        if (alphabet.Length > MaximumAlphabetLength)
        {
            throw new ArgumentOutOfRangeException("alphabet", alphabet.Length, "alphabet cannot contain more than 256 characters.");
        }
    }
}
