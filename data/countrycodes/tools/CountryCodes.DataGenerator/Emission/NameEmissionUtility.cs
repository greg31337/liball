using System.Globalization;
using System.Text;

namespace CountryCodes.DataGenerator.Emission;

internal static class NameEmissionUtility
{
    internal static string NormalizeDisplay(string value)
    {
        return value.Normalize(NormalizationForm.FormC);
    }

    internal static string CreateSearchKey(string displayName)
    {
        string decomposed = displayName.Normalize(NormalizationForm.FormKD).ToLowerInvariant();
        StringBuilder builder = new StringBuilder(decomposed.Length);
        bool separatorPending = false;
        foreach (Rune rune in decomposed.EnumerateRunes())
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.SpacingCombiningMark ||
                category == UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            if (Rune.IsLetterOrDigit(rune))
            {
                if (separatorPending)
                {
                    builder.Append(' ');
                    separatorPending = false;
                }

                builder.Append(rune.ToString());
            }
            else
            {
                separatorPending = builder.Length != 0;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
