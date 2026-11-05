using System;

namespace fake;

internal static class MaskFormatter
{
    const string DigitAlphabet = "0123456789";
    const string LetterAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    const string AlphaNumericAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    internal static string Format(IRandomSource random, string mask, int maximumOutputSize)
    {
        int length = Length(mask, maximumOutputSize);
        char[] result = new char[length];
        Write(random, new Span<char>(result), mask, maximumOutputSize);
        return new string(result);
    }

    internal static int OutputLength(string mask, int maximumOutputSize)
    {
        return Length(mask, maximumOutputSize);
    }

    internal static int Write(IRandomSource random, Span<char> destination, string mask, int maximumOutputSize)
    {
        int length = Length(mask, maximumOutputSize);
        if (destination.Length != length)
        {
            throw new ArgumentException($"destination length must be exactly {length}, but was {destination.Length}.", "destination");
        }
        int maskIndex = 0;
        int destinationIndex = 0;
        while (maskIndex < mask.Length)
        {
            char character = mask[maskIndex];
            if (character == '\\')
            {
                destination[destinationIndex] = mask[maskIndex + 1];
                destinationIndex = destinationIndex + 1;
                maskIndex = maskIndex + 2;
                continue;
            }
            if (character == '#')
            {
                destination[destinationIndex] = DigitAlphabet[random.IntFromMaximumExclusive(DigitAlphabet.Length)];
            }
            else if (character == '?')
            {
                destination[destinationIndex] = LetterAlphabet[random.IntFromMaximumExclusive(LetterAlphabet.Length)];
            }
            else if (character == '*')
            {
                destination[destinationIndex] = AlphaNumericAlphabet[random.IntFromMaximumExclusive(AlphaNumericAlphabet.Length)];
            }
            else
            {
                destination[destinationIndex] = character;
            }
            destinationIndex = destinationIndex + 1;
            maskIndex = maskIndex + 1;
        }
        return length;
    }

    static int Length(string mask, int maximumOutputSize)
    {
        UnicodeText.Validate(mask, "mask");
        int length = 0;
        int index = 0;
        while (index < mask.Length)
        {
            if (mask[index] == '\\')
            {
                if (index + 1 >= mask.Length)
                {
                    throw new ArgumentException($"mask contains a trailing escape at index {index}.", "mask");
                }
                char escaped = mask[index + 1];
                if (escaped != '#' && escaped != '?' && escaped != '*' && escaped != '\\')
                {
                    throw new ArgumentException($"mask contains the unsupported escape '\\{escaped}' at index {index}.", "mask");
                }
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
            length = length + 1;
        }
        return Guard.OutputSize(length, maximumOutputSize, "mask");
    }
}
