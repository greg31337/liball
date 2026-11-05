namespace sip;

internal static class SipUriEncoding
{
    const int UserComponent = 1;
    const int PasswordComponent = 2;
    const int ParameterComponent = 3;
    const int HeaderComponent = 4;
    const int TelephoneComponent = 5;

    internal static string EncodeUser(string value)
    {
        return EncodeComponent(value, UserComponent);
    }

    internal static string EncodePassword(string value)
    {
        return EncodeComponent(value, PasswordComponent);
    }

    internal static string EncodeParameter(string value)
    {
        return EncodeComponent(value, ParameterComponent);
    }

    internal static string EncodeHeader(string value)
    {
        return EncodeComponent(value, HeaderComponent);
    }

    internal static string EncodeTelephone(string value)
    {
        return EncodeComponent(value, TelephoneComponent);
    }

    static string EncodeComponent(string value, int component)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < bytes.Length)
        {
            int character = bytes[index];
            if (character < 128 && IsAllowedCharacter(character, component))
            {
                builder.Append((char)character);
            }
            else
            {
                builder.Append('%');
                builder.Append(GetHexCharacter(character / 16));
                builder.Append(GetHexCharacter(character % 16));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    static bool IsAllowedCharacter(int character, int component)
    {
        if (SipUriSyntax.IsUnreserved(character))
        {
            return true;
        }

        switch (component)
        {
            case UserComponent:
                return character == '&' || character == '=' || character == '+' || character == '$'
                    || character == ',' || character == ';' || character == '?' || character == '/';
            case PasswordComponent:
                return character == '&' || character == '=' || character == '+' || character == '$' || character == ',';
            case ParameterComponent:
                return character == '[' || character == ']' || character == '/' || character == ':'
                    || character == '&' || character == '+' || character == '$';
            case HeaderComponent:
                return character == '[' || character == ']' || character == '/' || character == '?'
                    || character == ':' || character == '+' || character == '$';
            case TelephoneComponent:
                return SipUriSyntax.IsAsciiDigit(character) || character >= 'A' && character <= 'F'
                    || character >= 'a' && character <= 'f' || character == '+' || character == '*'
                    || character == '#' || character == '-' || character == '.' || character == '(' || character == ')';
            default:
                return false;
        }
    }

    static char GetHexCharacter(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }

        return (char)('A' + value - 10);
    }
}
