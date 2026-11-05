using System.Globalization;
using System.Text;

namespace CurrencyCodes.DataGenerator;

internal sealed class CanonicalCSharpValue
{
    private CanonicalCSharpValue(string expression)
    {
        Expression = expression;
    }

    internal string Expression { get; }

    internal static CanonicalCSharpValue String(string value)
    {
        return new CanonicalCSharpValue("\"" + EscapeString(value) + "\"");
    }

    internal static CanonicalCSharpValue NullableString(string? value)
    {
        return value == null ? new CanonicalCSharpValue("null") : String(value);
    }

    internal static CanonicalCSharpValue Integer(int value)
    {
        return new CanonicalCSharpValue(value.ToString(CultureInfo.InvariantCulture));
    }

    internal static CanonicalCSharpValue Boolean(bool value)
    {
        return new CanonicalCSharpValue(value ? "true" : "false");
    }

    internal static CanonicalCSharpValue Raw(string expression)
    {
        return new CanonicalCSharpValue(expression);
    }

    private static string EscapeString(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (character == '"')
            {
                builder.Append("\\\"");
            }
            else if (character == '\\')
            {
                builder.Append("\\\\");
            }
            else if (character < ' ' || character > '~')
            {
                builder.Append("\\u");
                builder.Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
