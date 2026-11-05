namespace ttf;

/// <summary>Represents one validated PDF name object value.</summary>
/// <remarks>
/// The decoded value is encoded as UTF-8 and limited to 127 bytes. PDF
/// delimiters, white space, number signs, and bytes outside printable ASCII
/// are serialized with uppercase <c>#XX</c> escapes. The serialized token
/// includes its leading slash.
/// </remarks>
public class PdfName : IPdfName
{
    const int MaximumByteLength = 127;
    string value;
    string escapedValue;
    int byteLength;

    /// <summary>Creates a validated PDF name from its decoded value.</summary>
    /// <param name="value">The nonempty decoded name value.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The value contains null or unpaired surrogate characters, or its UTF-8
    /// representation exceeds 127 bytes.
    /// </exception>
    public PdfName(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!IsValid(value))
        {
            throw new System.ArgumentException(
                "A PDF name must be nonempty valid Unicode without null characters and encode to at most 127 UTF-8 bytes.",
                "value");
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        this.value = value;
        byteLength = bytes.Length;
        escapedValue = EscapeBytes(bytes);
    }

    /// <summary>Reports whether a decoded string is a supported PDF name.</summary>
    /// <param name="value">The decoded candidate value.</param>
    /// <returns>True when construction would succeed.</returns>
    internal static bool IsValid(string value)
    {
        if (value == null || value.Length == 0 ||
            !ContainsValidCharacters(value))
        {
            return false;
        }

        return System.Text.Encoding.UTF8.GetByteCount(value) <=
            MaximumByteLength;
    }

    /// <summary>Gets the decoded name value.</summary>
    /// <returns>The original validated value.</returns>
    public string GetValue() { return value; }

    /// <summary>Gets the escaped name without its leading slash.</summary>
    /// <returns>The PDF-escaped byte representation.</returns>
    public string GetEscapedValue() { return escapedValue; }

    /// <summary>Gets the complete serialized PDF name token.</summary>
    /// <returns>A slash followed by the escaped value.</returns>
    public string GetToken() { return "/" + escapedValue; }

    /// <summary>Gets the decoded value's UTF-8 byte length.</summary>
    /// <returns>A value from one through 127.</returns>
    public int GetByteLength() { return byteLength; }

    static bool ContainsValidCharacters(string value)
    {
        int index = 0;

        while (index < value.Length)
        {
            char character = value[index];

            if (character == 0 ||
                character >= 0xDC00 && character <= 0xDFFF)
            {
                return false;
            }

            if (character >= 0xD800 && character <= 0xDBFF)
            {
                if (index + 1 >= value.Length)
                {
                    return false;
                }

                char following = value[index + 1];

                if (following < 0xDC00 || following > 0xDFFF)
                {
                    return false;
                }

                index += 1;
            }

            index += 1;
        }

        return true;
    }

    static string EscapeBytes(byte[] bytes)
    {
        System.Text.StringBuilder escaped = new System.Text.StringBuilder(
            bytes.Length);
        int index = 0;

        while (index < bytes.Length)
        {
            byte value = bytes[index];

            if (IsRegular(value))
            {
                escaped.Append((char)value);
            }
            else
            {
                escaped.Append('#');
                escaped.Append(HexDigit(value >> 4));
                escaped.Append(HexDigit(value & 15));
            }

            index += 1;
        }

        return escaped.ToString();
    }

    static bool IsRegular(byte value)
    {
        if (value < 33 || value > 126 || value == '#')
        {
            return false;
        }

        return value != '(' && value != ')' &&
            value != '<' && value != '>' &&
            value != '[' && value != ']' &&
            value != '{' && value != '}' &&
            value != '/' && value != '%';
    }

    static char HexDigit(int value)
    {
        return value < 10 ?
            (char)('0' + value) :
            (char)('A' + value - 10);
    }
}
