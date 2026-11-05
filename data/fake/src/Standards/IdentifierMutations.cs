using System;

namespace fake;

internal static class IdentifierMutations
{
    const string UnusualChecksum = "097522980X";
    const string UnusualLength = "00000000";
    const string UnusualCharacter = "1M8GDM9AXKP042788";
    const string UnusualPrefix = "9791090636071";
    const string UnusualVersion = "00000000-0000-0000-0000-000000000000";
    const string UnusualOverflowBoundary = "7ZZZZZZZZZZZZZZZZZZZZZZZZZ";
    const string InvalidChecksum = "0975229800";
    const string InvalidLength = "0000000";
    const string InvalidCharacter = "1M8GDM9IXKP042788";
    const string InvalidPrefix = "9771090636073";
    const string InvalidVersion = "919108f7-52d1-f320-9bac-f847db4148a8";
    const string InvalidOverflow = "80000000000000000000000000";

    internal static string ValidUnusual(
        IdentifierMutationKind kind,
        int maximumOutputSize)
    {
        ValidateKind(kind);
        string value = UnusualChecksum;
        switch (kind)
        {
            case IdentifierMutationKind.Length:
                value = UnusualLength;
                break;
            case IdentifierMutationKind.Character:
                value = UnusualCharacter;
                break;
            case IdentifierMutationKind.Prefix:
                value = UnusualPrefix;
                break;
            case IdentifierMutationKind.Version:
                value = UnusualVersion;
                break;
            case IdentifierMutationKind.Overflow:
                value = UnusualOverflowBoundary;
                break;
        }
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        return value;
    }

    internal static string InvalidFixture(
        IdentifierMutationKind kind,
        int maximumOutputSize)
    {
        ValidateKind(kind);
        string value = InvalidChecksum;
        switch (kind)
        {
            case IdentifierMutationKind.Length:
                value = InvalidLength;
                break;
            case IdentifierMutationKind.Character:
                value = InvalidCharacter;
                break;
            case IdentifierMutationKind.Prefix:
                value = InvalidPrefix;
                break;
            case IdentifierMutationKind.Version:
                value = InvalidVersion;
                break;
            case IdentifierMutationKind.Overflow:
                value = InvalidOverflow;
                break;
        }
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        return value;
    }

    internal static string Mutate(
        ReadOnlySpan<char> value,
        IdentifierMutationKind kind,
        int maximumCount,
        int maximumOutputSize)
    {
        ValidateKind(kind);
        RequireValue(value, maximumCount);
        if (kind == IdentifierMutationKind.Length)
        {
            return Shorten(value, maximumOutputSize);
        }
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        if (kind == IdentifierMutationKind.Version)
        {
            return Version(value);
        }
        if (kind == IdentifierMutationKind.Overflow)
        {
            return Overflow(value);
        }
        char[] result = value.ToArray();
        if (kind == IdentifierMutationKind.Checksum)
        {
            int index = ChecksumIndex(value, maximumCount);
            result[index] = DifferentCheckCharacter(result[index]);
        }
        else if (kind == IdentifierMutationKind.Character)
        {
            result[result.Length / 2] = ' ';
        }
        else
        {
            MutatePrefix(result, value, maximumCount);
        }
        return new string(result);
    }

    static string Shorten(
        ReadOnlySpan<char> value,
        int maximumOutputSize)
    {
        int length = value.Length - 1;
        if (value.Length >= 2 && char.IsLowSurrogate(value[value.Length - 1]))
        {
            length--;
        }
        Guard.OutputSize(length, maximumOutputSize, "value");
        return new string(value.Slice(0, length));
    }

    static string Version(ReadOnlySpan<char> value)
    {
        if (!UuidGenerator.Validate(value))
        {
            throw new ArgumentException("value must be canonical UUID text for a version mutation.", "value");
        }
        char[] result = value.ToArray();
        result[14] = result[14] == 'f' || result[14] == 'F' ? '0' : 'f';
        return new string(result);
    }

    static string Overflow(ReadOnlySpan<char> value)
    {
        if (!UlidGenerator.Validate(value))
        {
            throw new ArgumentException("value must be valid ULID text for an overflow mutation.", "value");
        }
        char[] result = value.ToArray();
        result[0] = '8';
        return new string(result);
    }

    static void MutatePrefix(
        char[] result,
        ReadOnlySpan<char> value,
        int maximumCount)
    {
        if (StandardIdentifierChecks.Validate(
            StandardIdentifierChecksumKind.Isbn13,
            value,
            maximumCount))
        {
            result[0] = '9';
            result[1] = '7';
            result[2] = '7';
            int check = StandardIdentifierChecks.Calculate(
                StandardIdentifierChecksumKind.Gtin,
                new ReadOnlySpan<char>(result, 0, 12),
                maximumCount);
            result[12] = (char)('0' + check);
            return;
        }
        result[0] = ' ';
    }

    static int ChecksumIndex(
        ReadOnlySpan<char> value,
        int maximumCount)
    {
        if (value.Length == VinAlgorithms.Length &&
            VinAlgorithms.Validate(value, maximumCount))
        {
            return 8;
        }
        return value.Length - 1;
    }

    static char DifferentCheckCharacter(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value == '9' ? '0' : (char)(value + 1);
        }
        if (value == 'X')
        {
            return '0';
        }
        return value == 'A' ? 'B' : 'A';
    }

    static void RequireValue(
        ReadOnlySpan<char> value,
        int maximumCount)
    {
        if (value.Length == 0)
        {
            throw new ArgumentException("value must contain at least one character.", "value");
        }
        Guard.Count(value.Length, maximumCount, "value");
        UnicodeText.Validate(new string(value), "value");
    }

    static void ValidateKind(IdentifierMutationKind kind)
    {
        switch (kind)
        {
            case IdentifierMutationKind.Checksum:
            case IdentifierMutationKind.Length:
            case IdentifierMutationKind.Character:
            case IdentifierMutationKind.Prefix:
            case IdentifierMutationKind.Version:
            case IdentifierMutationKind.Overflow:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported identifier mutation.");
        }
    }
}
