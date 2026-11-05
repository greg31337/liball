using System.Text;

namespace cbor;

internal class CborDiagnosticEscapes
{
    internal static string EscapeText(string value, int maximum, string path)
    {
        StringBuilder escaped = new StringBuilder();
        string hexadecimal = "0123456789abcdef";
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            switch (character)
            {
                case '"':
                    AppendChecked(escaped, "\\\"", maximum, path);
                    break;
                case '\\':
                    AppendChecked(escaped, "\\\\", maximum, path);
                    break;
                case '\b':
                    AppendChecked(escaped, "\\b", maximum, path);
                    break;
                case '\f':
                    AppendChecked(escaped, "\\f", maximum, path);
                    break;
                case '\n':
                    AppendChecked(escaped, "\\n", maximum, path);
                    break;
                case '\r':
                    AppendChecked(escaped, "\\r", maximum, path);
                    break;
                case '\t':
                    AppendChecked(escaped, "\\t", maximum, path);
                    break;
                default:
                    if (character < 0x20)
                    {
                        string sequence = "\\u00"
                            + hexadecimal[(character >> 4) & 15]
                            + hexadecimal[character & 15];
                        AppendChecked(escaped, sequence, maximum, path);
                    }
                    else
                    {
                        AppendChecked(escaped, character.ToString(), maximum, path);
                    }
                    break;
            }
            index = index + 1;
        }
        return escaped.ToString();
    }

    internal static string FormatBytes(byte[] value, int maximum, string path)
    {
        string hexadecimal = "0123456789abcdef";
        if (value.Length > maximum / 2)
        {
            throw new CborDiagnosticException(
                "CBOR_DIAGNOSTIC_MAX_CHARACTERS",
                "The formatted diagnostic notation exceeds the character limit.",
                -1,
                path
            );
        }
        StringBuilder formatted = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            formatted.Append(hexadecimal[current >> 4]);
            formatted.Append(hexadecimal[current & 15]);
            index = index + 1;
        }
        return formatted.ToString();
    }

    internal static void AppendChecked(
        StringBuilder output,
        string value,
        int maximum,
        string path)
    {
        if (value.Length > maximum - output.Length)
        {
            throw new CborDiagnosticException(
                "CBOR_DIAGNOSTIC_MAX_CHARACTERS",
                "The formatted diagnostic notation exceeds the character limit.",
                -1,
                path
            );
        }
        output.Append(value);
    }
}
