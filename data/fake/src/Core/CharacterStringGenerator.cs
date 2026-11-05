using System;
using System.Collections.Generic;

namespace fake;

internal static class CharacterStringGenerator
{
    internal const string LowercaseAlphabet = "abcdefghijklmnopqrstuvwxyz";
    internal const string UppercaseAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    internal const string AlphaAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    internal const string AlphaNumericAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    internal const string SymbolAlphabet = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
    internal const string WhitespaceAlphabet = "\t\n\v\f\r ";
    internal const string PrintableAlphabet = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    internal static string Generate(FakeRandom random, string alphabet, int count, bool withReplacement, int maximumOutputSize, int maximumCount, bool validateAlphabet)
    {
        if (validateAlphabet)
        {
            ValidateAlphabet(alphabet, maximumCount);
        }
        int length = Guard.OutputSize(count, maximumOutputSize, "count");
        ValidateSufficiency(alphabet.Length, length, withReplacement);
        char[] result = new char[length];
        Fill(random, new Span<char>(result), alphabet, withReplacement);
        return new string(result);
    }

    internal static int Write(FakeRandom random, Span<char> destination, string alphabet, bool withReplacement, int maximumOutputSize, int maximumCount, bool validateAlphabet)
    {
        if (validateAlphabet)
        {
            ValidateAlphabet(alphabet, maximumCount);
        }
        Guard.OutputSize(destination.Length, maximumOutputSize, "destination");
        ValidateSufficiency(alphabet.Length, destination.Length, withReplacement);
        Fill(random, destination, alphabet, withReplacement);
        return destination.Length;
    }

    static void ValidateAlphabet(string alphabet, int maximumCount)
    {
        Guard.NotNullOrEmpty(alphabet, "alphabet");
        Guard.Count(alphabet.Length, maximumCount, "alphabet");
        HashSet<char> seen = new HashSet<char>();
        int index = 0;
        while (index < alphabet.Length)
        {
            char character = alphabet[index];
            if (char.IsSurrogate(character))
            {
                throw new ArgumentException("alphabet cannot contain UTF-16 surrogate code units.", "alphabet");
            }
            if (!seen.Add(character))
            {
                throw new ArgumentException($"alphabet contains the duplicate character U+{(int)character:X4}.", "alphabet");
            }
            index = index + 1;
        }
    }

    static void ValidateSufficiency(int alphabetLength, int count, bool withReplacement)
    {
        if (!withReplacement && count > alphabetLength)
        {
            throw new ArgumentOutOfRangeException("count", count, "count cannot exceed the alphabet length without replacement.");
        }
    }

    static void Fill(FakeRandom random, Span<char> destination, string alphabet, bool withReplacement)
    {
        if (withReplacement)
        {
            int index = 0;
            while (index < destination.Length)
            {
                destination[index] = alphabet[random.IntFromMaximumExclusive(alphabet.Length)];
                index = index + 1;
            }
            return;
        }
        int[] indexes = Selection.ChoiceIndexes(random, alphabet.Length, destination.Length, false);
        int selectedIndex = 0;
        while (selectedIndex < indexes.Length)
        {
            destination[selectedIndex] = alphabet[indexes[selectedIndex]];
            selectedIndex = selectedIndex + 1;
        }
    }
}
