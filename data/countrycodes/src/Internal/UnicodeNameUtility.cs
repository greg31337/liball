using System.Globalization;
using System.Text;

namespace countrycodes;

internal static class UnicodeNameUtility
{
    internal static string? NormalizeDisplayName(string? value)
    {
        if (value == null || !IsWellFormedUtf16(value))
        {
            return null;
        }

        return value.Normalize(NormalizationForm.FormC);
    }

    internal static string? CreateMatchKey(
        string? value,
        bool foldWhitespace,
        bool foldPunctuation,
        bool foldCompatibility,
        bool foldDiacritics)
    {
        if (value == null || !IsWellFormedUtf16(value))
        {
            return null;
        }

        NormalizationForm preparationForm = NormalizationForm.FormC;
        if (foldCompatibility)
        {
            preparationForm = NormalizationForm.FormKD;
        }
        else if (foldDiacritics)
        {
            preparationForm = NormalizationForm.FormD;
        }

        string prepared = value.Normalize(preparationForm).ToLowerInvariant();
        StringBuilder builder = new StringBuilder(prepared.Length);
        bool separatorPending = false;
        foreach (Rune rune in prepared.EnumerateRunes())
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (foldDiacritics && IsDiacriticCategory(category))
            {
                continue;
            }

            if (foldWhitespace && Rune.IsWhiteSpace(rune))
            {
                separatorPending = builder.Length > 0;
                continue;
            }

            if (foldPunctuation && IsPunctuationCategory(category))
            {
                separatorPending = builder.Length > 0;
                continue;
            }

            if (!foldWhitespace && Rune.IsWhiteSpace(rune))
            {
                separatorPending = false;
                builder.Append(rune.ToString());
                continue;
            }

            if (separatorPending)
            {
                builder.Append(' ');
                separatorPending = false;
            }

            builder.Append(rune.ToString());
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    internal static bool IsWellFormedUtf16(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                index += 2;
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool IsDiacriticCategory(UnicodeCategory category)
    {
        return category == UnicodeCategory.NonSpacingMark ||
               category == UnicodeCategory.SpacingCombiningMark ||
               category == UnicodeCategory.EnclosingMark;
    }

    internal static bool IsPunctuationCategory(UnicodeCategory category)
    {
        return category == UnicodeCategory.ConnectorPunctuation ||
               category == UnicodeCategory.DashPunctuation ||
               category == UnicodeCategory.OpenPunctuation ||
               category == UnicodeCategory.ClosePunctuation ||
               category == UnicodeCategory.InitialQuotePunctuation ||
               category == UnicodeCategory.FinalQuotePunctuation ||
               category == UnicodeCategory.OtherPunctuation;
    }
}
