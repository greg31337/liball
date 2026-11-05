using System;
using System.Text;

namespace LanguageCodes.DataGenerator.Emission;

internal static class DeterministicCSharp
{
    internal static string StringLiteral(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        StringBuilder builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (char character in value)
        {
            switch (character)
            {
                case '\0':
                    builder.Append("\\0");
                    break;
                case '\a':
                    builder.Append("\\a");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\v':
                    builder.Append("\\v");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                default:
                    if (character < 0x20 || character > 0x7e)
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", global::System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    internal static string[] SortOrdinal(string[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        string[] sorted = new string[values.Length];
        Array.Copy(values, sorted, values.Length);
        Array.Sort(sorted, StringComparer.Ordinal);
        return sorted;
    }
}
