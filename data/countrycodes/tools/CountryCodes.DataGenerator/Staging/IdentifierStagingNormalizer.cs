using System.Text;

namespace CountryCodes.DataGenerator.Staging;

internal static class IdentifierStagingNormalizer
{
    internal static string UpperAscii(string value)
    {
        StringBuilder? builder = null;
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (character < 'a' || character > 'z')
            {
                if (builder != null)
                {
                    builder.Append(character);
                }

                continue;
            }

            if (builder == null)
            {
                builder = new StringBuilder(value.Length);
                builder.Append(value, 0, index);
            }

            builder.Append((char)(character - ('a' - 'A')));
        }

        return builder == null ? value : builder.ToString();
    }

    internal static string LowerAscii(string value)
    {
        StringBuilder? builder = null;
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (character < 'A' || character > 'Z')
            {
                if (builder != null)
                {
                    builder.Append(character);
                }

                continue;
            }

            if (builder == null)
            {
                builder = new StringBuilder(value.Length);
                builder.Append(value, 0, index);
            }

            builder.Append((char)(character + ('a' - 'A')));
        }

        return builder == null ? value : builder.ToString();
    }

    internal static string NormalizeSubdivision(string value)
    {
        string upper = UpperAscii(value);
        if (upper.IndexOf('-') >= 0 || upper.Length <= 2)
        {
            return upper;
        }

        bool hasCountryPrefix = IsAsciiLetter(upper[0]) && IsAsciiLetter(upper[1]);
        if (!hasCountryPrefix)
        {
            return upper;
        }

        return upper.Substring(0, 2) + "-" + upper.Substring(2);
    }

    private static bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
    }
}
