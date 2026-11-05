using System;

namespace fake;

internal static class SecretGenerator
{
    const string HexAlphabet = "0123456789abcdef";
    const string TokenAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    const string RecoveryAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    const string LowercaseAlphabet = "abcdefghijklmnopqrstuvwxyz";
    const string UppercaseAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string DigitAlphabet = "0123456789";
    const string PasswordSymbolAlphabet = "!#$%&()*+,-./:;<=>?@[]^_{|}~";
    static readonly string[] PassphraseWords = new string[]
    {
        "acorn", "amber", "angel", "apple", "atlas", "badge", "baker", "basil",
        "beach", "berry", "birch", "bloom", "brook", "candy", "cedar", "charm",
        "cloud", "coral", "crane", "creek", "dance", "delta", "eagle", "ember",
        "flame", "frost", "glade", "grain", "grape", "grove", "hazel", "honey",
        "ivory", "lemon", "light", "lilac", "maple", "metal", "misty", "ocean",
        "olive", "orbit", "peach", "pearl", "prism", "river", "robin", "rocky",
        "shell", "shore", "solar", "spark", "spice", "stone", "storm", "sunny",
        "tiger", "trail", "ultra", "vivid", "water", "wheat", "world", "zebra"
    };

    internal static byte[] Bytes(FakeRandom random, int count, int maximumOutputSize)
    {
        int length = Guard.OutputSize(count, maximumOutputSize, "count");
        byte[] result = new byte[length];
        random.FillEntropy(new Span<byte>(result));
        return result;
    }

    internal static int WriteBytes(FakeRandom random, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(destination.Length, maximumOutputSize, "destination");
        random.FillEntropy(destination);
        return destination.Length;
    }

    internal static string Hex(FakeRandom random, int count, int maximumOutputSize)
    {
        return CharacterStringGenerator.Generate(random, HexAlphabet, count, true, maximumOutputSize, int.MaxValue, false);
    }

    internal static int WriteHex(FakeRandom random, Span<char> destination, int maximumOutputSize)
    {
        return CharacterStringGenerator.Write(random, destination, HexAlphabet, true, maximumOutputSize, int.MaxValue, false);
    }

    internal static string Token(FakeRandom random, int count, int maximumOutputSize)
    {
        return CharacterStringGenerator.Generate(random, TokenAlphabet, count, true, maximumOutputSize, int.MaxValue, false);
    }

    internal static int WriteToken(FakeRandom random, Span<char> destination, int maximumOutputSize)
    {
        return CharacterStringGenerator.Write(random, destination, TokenAlphabet, true, maximumOutputSize, int.MaxValue, false);
    }

    internal static string Pin(FakeRandom random, int digits, int maximumOutputSize)
    {
        int length = Guard.OutputSize(digits, maximumOutputSize, "digits");
        char[] result = new char[length];
        CharacterStringGenerator.Write(random, new Span<char>(result), DigitAlphabet, true, maximumOutputSize, int.MaxValue, false);
        return new string(result);
    }

    internal static int WritePin(FakeRandom random, Span<char> destination, int maximumOutputSize)
    {
        return CharacterStringGenerator.Write(random, destination, DigitAlphabet, true, maximumOutputSize, int.MaxValue, false);
    }

    internal static string RecoveryCode(FakeRandom random, int groupCount, int groupLength, char separator, int maximumCount, int maximumOutputSize)
    {
        int length = RecoveryOutputLength(groupCount, groupLength, separator, maximumCount, maximumOutputSize);
        char[] result = new char[length];
        WriteRecoveryCode(random, new Span<char>(result), groupCount, groupLength, separator, maximumCount, maximumOutputSize);
        return new string(result);
    }

    internal static int WriteRecoveryCode(FakeRandom random, Span<char> destination, int groupCount, int groupLength, char separator, int maximumCount, int maximumOutputSize)
    {
        int length = RecoveryOutputLength(groupCount, groupLength, separator, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, length);
        int destinationIndex = 0;
        int groupIndex = 0;
        while (groupIndex < groupCount)
        {
            if (groupIndex > 0)
            {
                destination[destinationIndex] = separator;
                destinationIndex = destinationIndex + 1;
            }
            int characterIndex = 0;
            while (characterIndex < groupLength)
            {
                destination[destinationIndex] = RecoveryAlphabet[random.IntFromMaximumExclusive(RecoveryAlphabet.Length)];
                destinationIndex = destinationIndex + 1;
                characterIndex = characterIndex + 1;
            }
            groupIndex = groupIndex + 1;
        }
        return length;
    }

    internal static string Password(FakeRandom random, int length, bool lowercase, bool uppercase, bool digits, bool symbols, int maximumOutputSize)
    {
        int outputLength = Guard.OutputSize(length, maximumOutputSize, "length");
        int required = EnabledCount(lowercase, uppercase, digits, symbols);
        if (required == 0)
        {
            throw new ArgumentException("At least one password character class must be enabled.");
        }
        if (outputLength < required)
        {
            throw new ArgumentOutOfRangeException("length", length, $"length must be at least {required} to include every enabled character class.");
        }
        string alphabet = PasswordAlphabet(lowercase, uppercase, digits, symbols);
        char[] result = new char[outputLength];
        int index = 0;
        if (lowercase)
        {
            result[index] = LowercaseAlphabet[random.IntFromMaximumExclusive(LowercaseAlphabet.Length)];
            index = index + 1;
        }
        if (uppercase)
        {
            result[index] = UppercaseAlphabet[random.IntFromMaximumExclusive(UppercaseAlphabet.Length)];
            index = index + 1;
        }
        if (digits)
        {
            result[index] = DigitAlphabet[random.IntFromMaximumExclusive(DigitAlphabet.Length)];
            index = index + 1;
        }
        if (symbols)
        {
            result[index] = PasswordSymbolAlphabet[random.IntFromMaximumExclusive(PasswordSymbolAlphabet.Length)];
            index = index + 1;
        }
        while (index < result.Length)
        {
            result[index] = alphabet[random.IntFromMaximumExclusive(alphabet.Length)];
            index = index + 1;
        }
        Shuffle(random, result);
        return new string(result);
    }

    internal static int WritePassword(FakeRandom random, Span<char> destination, bool lowercase, bool uppercase, bool digits, bool symbols, int maximumOutputSize)
    {
        string password = Password(random, destination.Length, lowercase, uppercase, digits, symbols, maximumOutputSize);
        Copy(password, destination);
        return destination.Length;
    }

    internal static string Passphrase(FakeRandom random, int wordCount, string separator, int maximumCount, int maximumOutputSize)
    {
        int length = PassphraseOutputLength(wordCount, separator, maximumCount, maximumOutputSize);
        char[] result = new char[length];
        WritePassphrase(random, new Span<char>(result), wordCount, separator, maximumCount, maximumOutputSize);
        return new string(result);
    }

    internal static int WritePassphrase(FakeRandom random, Span<char> destination, int wordCount, string separator, int maximumCount, int maximumOutputSize)
    {
        int length = PassphraseOutputLength(wordCount, separator, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, length);
        int destinationIndex = 0;
        int wordIndex = 0;
        while (wordIndex < wordCount)
        {
            if (wordIndex > 0)
            {
                int separatorIndex = 0;
                while (separatorIndex < separator.Length)
                {
                    destination[destinationIndex] = separator[separatorIndex];
                    destinationIndex = destinationIndex + 1;
                    separatorIndex = separatorIndex + 1;
                }
            }
            string word = PassphraseWords[random.IntFromMaximumExclusive(PassphraseWords.Length)];
            int characterIndex = 0;
            while (characterIndex < word.Length)
            {
                destination[destinationIndex] = word[characterIndex];
                destinationIndex = destinationIndex + 1;
                characterIndex = characterIndex + 1;
            }
            wordIndex = wordIndex + 1;
        }
        return length;
    }

    internal static int RecoveryOutputLength(int groupCount, int groupLength, char separator, int maximumCount, int maximumOutputSize)
    {
        Guard.Count(groupCount, maximumCount, "groupCount");
        Guard.Positive(groupLength, "groupLength");
        if (!AsciiPunctuation(separator))
        {
            throw new ArgumentException("separator must be a printable ASCII punctuation character.", "separator");
        }
        long length = groupCount == 0 ? 0L : ((long)groupCount * (long)groupLength) + (long)groupCount - 1L;
        return Guard.OutputSize(length, maximumOutputSize, "groupCount");
    }

    internal static int PassphraseOutputLength(int wordCount, string separator, int maximumCount, int maximumOutputSize)
    {
        Guard.NotNull(separator, "separator");
        UnicodeText.Validate(separator, "separator");
        Guard.Count(wordCount, maximumCount, "wordCount");
        long length = wordCount == 0 ? 0L : ((long)wordCount * 5L) + ((long)wordCount - 1L) * (long)separator.Length;
        return Guard.OutputSize(length, maximumOutputSize, "wordCount");
    }

    static int EnabledCount(bool lowercase, bool uppercase, bool digits, bool symbols)
    {
        int count = 0;
        if (lowercase)
        {
            count = count + 1;
        }
        if (uppercase)
        {
            count = count + 1;
        }
        if (digits)
        {
            count = count + 1;
        }
        if (symbols)
        {
            count = count + 1;
        }
        return count;
    }

    static string PasswordAlphabet(bool lowercase, bool uppercase, bool digits, bool symbols)
    {
        string result = "";
        if (lowercase)
        {
            result = result + LowercaseAlphabet;
        }
        if (uppercase)
        {
            result = result + UppercaseAlphabet;
        }
        if (digits)
        {
            result = result + DigitAlphabet;
        }
        if (symbols)
        {
            result = result + PasswordSymbolAlphabet;
        }
        return result;
    }

    static bool AsciiPunctuation(char character)
    {
        return (character >= '!' && character <= '/')
            || (character >= ':' && character <= '@')
            || (character >= '[' && character <= '`')
            || (character >= '{' && character <= '~');
    }

    static void Shuffle(FakeRandom random, char[] values)
    {
        int index = values.Length - 1;
        while (index > 0)
        {
            int other = random.IntFromMaximumExclusive(index + 1);
            char temporary = values[index];
            values[index] = values[other];
            values[other] = temporary;
            index = index - 1;
        }
    }

    static void Copy(string value, Span<char> destination)
    {
        int index = 0;
        while (index < value.Length)
        {
            destination[index] = value[index];
            index = index + 1;
        }
    }

    static void RequireDestination(int actual, int required)
    {
        if (actual != required)
        {
            throw new ArgumentException($"destination length must be exactly {required}, but was {actual}.", "destination");
        }
    }
}
