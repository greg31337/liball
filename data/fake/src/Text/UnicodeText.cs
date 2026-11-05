using System;
using System.Globalization;
using System.Text;

namespace fake;

internal static class UnicodeText
{
    internal static int GraphemeCount(string value)
    {
        Validate(value);
        return StringInfo.ParseCombiningCharacters(value).Length;
    }

    internal static string Truncate(string value, int maximumGraphemes, int maximumOutputSize)
    {
        Validate(value);
        Guard.NonNegative(maximumGraphemes, "maximumGraphemes");
        int[] boundaries = StringInfo.ParseCombiningCharacters(value);
        if (boundaries.Length <= maximumGraphemes)
        {
            Guard.OutputSize(value.Length, maximumOutputSize, "value");
            return value;
        }
        int length = maximumGraphemes == 0 ? 0 : boundaries[maximumGraphemes];
        Guard.OutputSize(length, maximumOutputSize, "maximumGraphemes");
        return value.Substring(0, length);
    }

    internal static string Clamp(FakeRandom random, string value, int minimumGraphemes, int maximumGraphemes, int maximumOutputSize)
    {
        Validate(value);
        Guard.NonNegative(minimumGraphemes, "minimumGraphemes");
        Guard.NonNegative(maximumGraphemes, "maximumGraphemes");
        if (maximumGraphemes < minimumGraphemes)
        {
            throw new ArgumentOutOfRangeException("maximumGraphemes", maximumGraphemes, "maximumGraphemes must be greater than or equal to minimumGraphemes.");
        }
        int[] boundaries = StringInfo.ParseCombiningCharacters(value);
        if (boundaries.Length > maximumGraphemes)
        {
            int length = maximumGraphemes == 0 ? 0 : boundaries[maximumGraphemes];
            Guard.OutputSize(length, maximumOutputSize, "maximumGraphemes");
            return value.Substring(0, length);
        }
        if (boundaries.Length < minimumGraphemes)
        {
            int missing = minimumGraphemes - boundaries.Length;
            Guard.OutputSize((long)value.Length + (long)missing, maximumOutputSize, "minimumGraphemes");
            string filler = CharacterStringGenerator.Generate(
                random,
                CharacterStringGenerator.AlphaAlphabet,
                missing,
                true,
                maximumOutputSize,
                int.MaxValue,
                false);
            return value + filler;
        }
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        return value;
    }

    internal static string Normalize(string value, NormalizationMode mode, int maximumOutputSize)
    {
        Validate(value);
        if (mode == NormalizationMode.None)
        {
            Guard.OutputSize(value.Length, maximumOutputSize, "value");
            return value;
        }
        NormalizationForm form = Form(mode);
        string normalized = value.Normalize(form);
        Guard.OutputSize(normalized.Length, maximumOutputSize, "value");
        return normalized;
    }

    internal static bool IsNormalized(string value, NormalizationMode mode)
    {
        Validate(value);
        if (mode == NormalizationMode.None)
        {
            return true;
        }
        return value.IsNormalized(Form(mode));
    }

    static NormalizationForm Form(NormalizationMode mode)
    {
        switch (mode)
        {
            case NormalizationMode.Nfc:
                return NormalizationForm.FormC;
            case NormalizationMode.Nfd:
                return NormalizationForm.FormD;
            case NormalizationMode.Nfkc:
                return NormalizationForm.FormKC;
            case NormalizationMode.Nfkd:
                return NormalizationForm.FormKD;
            default:
                throw new ArgumentOutOfRangeException("mode", mode, "mode is not supported.");
        }
    }

    internal static void Validate(string value)
    {
        Validate(value, "value");
    }

    internal static void Validate(string value, string parameterName)
    {
        Guard.NotNull(value, parameterName);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw new ArgumentException($"{parameterName} contains an unpaired UTF-16 high surrogate.", parameterName);
                }
                index = index + 2;
                continue;
            }
            if (char.IsLowSurrogate(character))
            {
                throw new ArgumentException($"{parameterName} contains an unpaired UTF-16 low surrogate.", parameterName);
            }
            index = index + 1;
        }
    }
}
