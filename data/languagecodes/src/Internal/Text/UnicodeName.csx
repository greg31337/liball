namespace languagecodes;

internal static class UnicodeName
{
    internal static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string normalized = value.Normalize(global::System.Text.NormalizationForm.FormKC);
        string foldedCase = normalized.ToUpperInvariant();
        foldedCase = foldedCase.Normalize(global::System.Text.NormalizationForm.FormKC);
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(foldedCase.Length);
        bool pendingSeparator = false;
        int index;
        for (index = 0; index < foldedCase.Length; index++)
        {
            char character = foldedCase[index];
            if (char.IsWhiteSpace(character) || char.IsPunctuation(character))
            {
                if (builder.Length > 0)
                {
                    pendingSeparator = true;
                }

                continue;
            }

            if (pendingSeparator)
            {
                builder.Append(' ');
                pendingSeparator = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
