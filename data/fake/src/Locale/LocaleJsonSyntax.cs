namespace fake;

internal class LocaleJsonSyntax
{
    string Text;
    string ResourceName;
    int Position;

    internal LocaleJsonSyntax(string text, string resourceName)
    {
        Text = text;
        ResourceName = resourceName;
        Position = 0;
    }

    internal static void Validate(string text, string resourceName)
    {
        LocaleJsonSyntax syntax = new LocaleJsonSyntax(text, resourceName);
        syntax.ValidateDocument();
    }

    void ValidateDocument()
    {
        SkipWhitespace();
        if (Position == Text.Length)
        {
            Error("expected a JSON value");
        }
        ReadValue(0);
        SkipWhitespace();
        if (Position != Text.Length)
        {
            Error("unexpected trailing content");
        }
    }

    void ReadValue(int depth)
    {
        if (depth > 64)
        {
            Error("nesting exceeds 64 levels");
        }
        SkipWhitespace();
        if (Position == Text.Length)
        {
            Error("expected a JSON value");
        }
        char character = Text[Position];
        if (character == '{')
        {
            ReadObject(depth);
        }
        else if (character == '[')
        {
            ReadArray(depth);
        }
        else if (character == '"')
        {
            ReadString();
        }
        else if (character == 't')
        {
            ReadLiteral("true");
        }
        else if (character == 'f')
        {
            ReadLiteral("false");
        }
        else if (character == 'n')
        {
            ReadLiteral("null");
        }
        else if (character == '-' || character >= '0' && character <= '9')
        {
            ReadNumber();
        }
        else
        {
            Error("expected a JSON value");
        }
    }

    void ReadObject(int depth)
    {
        Position = Position + 1;
        SkipWhitespace();
        if (Consume('}'))
        {
            return;
        }
        while (true)
        {
            if (Position == Text.Length || Text[Position] != '"')
            {
                Error("expected a quoted object member name");
            }
            ReadString();
            SkipWhitespace();
            if (!Consume(':'))
            {
                Error("expected ':' after object member name");
            }
            ReadValue(depth + 1);
            SkipWhitespace();
            if (Consume('}'))
            {
                return;
            }
            if (!Consume(','))
            {
                Error("expected ',' or '}' after object member value");
            }
            SkipWhitespace();
        }
    }

    void ReadArray(int depth)
    {
        Position = Position + 1;
        SkipWhitespace();
        if (Consume(']'))
        {
            return;
        }
        while (true)
        {
            ReadValue(depth + 1);
            SkipWhitespace();
            if (Consume(']'))
            {
                return;
            }
            if (!Consume(','))
            {
                Error("expected ',' or ']' after array item");
            }
            SkipWhitespace();
        }
    }

    void ReadString()
    {
        Position = Position + 1;
        while (Position < Text.Length)
        {
            char character = Text[Position];
            Position = Position + 1;
            if (character == '"')
            {
                return;
            }
            if (character < 0x20)
            {
                Error("unescaped control character in string");
            }
            if (character == '\\')
            {
                ReadEscape();
            }
            else if (character >= 0xD800 && character <= 0xDBFF)
            {
                if (Position == Text.Length || Text[Position] < 0xDC00 || Text[Position] > 0xDFFF)
                {
                    Error("unpaired high surrogate in string");
                }
                Position = Position + 1;
            }
            else if (character >= 0xDC00 && character <= 0xDFFF)
            {
                Error("unpaired low surrogate in string");
            }
        }
        Error("unterminated string");
    }

    void ReadEscape()
    {
        if (Position == Text.Length)
        {
            Error("unterminated string escape");
        }
        char escape = Text[Position];
        Position = Position + 1;
        bool simple = escape == '"' || escape == '\\' || escape == '/';
        simple = simple || escape == 'b' || escape == 'f' || escape == 'n';
        simple = simple || escape == 'r' || escape == 't';
        if (simple)
        {
            return;
        }
        if (escape != 'u')
        {
            Error($"unsupported string escape '\\{escape}'");
        }
        int scalar = ReadHexadecimalEscape();
        if (scalar >= 0xD800 && scalar <= 0xDBFF)
        {
            if (Position + 2 > Text.Length || Text[Position] != '\\' || Text[Position + 1] != 'u')
            {
                Error("escaped high surrogate is not followed by an escaped low surrogate");
            }
            Position = Position + 2;
            int low = ReadHexadecimalEscape();
            if (low < 0xDC00 || low > 0xDFFF)
            {
                Error("escaped high surrogate is not followed by an escaped low surrogate");
            }
        }
        else if (scalar >= 0xDC00 && scalar <= 0xDFFF)
        {
            Error("escaped low surrogate has no preceding high surrogate");
        }
    }

    int ReadHexadecimalEscape()
    {
        if (Position + 4 > Text.Length)
        {
            Error("incomplete hexadecimal string escape");
        }
        int value = 0;
        int count = 0;
        while (count < 4)
        {
            char character = Text[Position];
            Position = Position + 1;
            int digit = HexadecimalDigit(character);
            if (digit < 0)
            {
                Error("invalid hexadecimal string escape");
            }
            value = value * 16 + digit;
            count = count + 1;
        }
        return value;
    }

    void ReadNumber()
    {
        Consume('-');
        if (Position == Text.Length)
        {
            Error("incomplete number");
        }
        if (Consume('0'))
        {
            if (Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
            {
                Error("number has a leading zero");
            }
        }
        else
        {
            ReadDigits("expected an integer digit");
        }
        if (Consume('.'))
        {
            ReadDigits("expected a digit after decimal point");
        }
        if (Position < Text.Length && (Text[Position] == 'e' || Text[Position] == 'E'))
        {
            Position = Position + 1;
            if (Position < Text.Length && (Text[Position] == '+' || Text[Position] == '-'))
            {
                Position = Position + 1;
            }
            ReadDigits("expected an exponent digit");
        }
    }

    void ReadDigits(string message)
    {
        int start = Position;
        while (Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
        {
            Position = Position + 1;
        }
        if (Position == start)
        {
            Error(message);
        }
    }

    void ReadLiteral(string literal)
    {
        int index = 0;
        while (index < literal.Length)
        {
            if (Position == Text.Length || Text[Position] != literal[index])
            {
                Error($"expected literal '{literal}'");
            }
            Position = Position + 1;
            index = index + 1;
        }
    }

    bool Consume(char expected)
    {
        if (Position < Text.Length && Text[Position] == expected)
        {
            Position = Position + 1;
            return true;
        }
        return false;
    }

    void SkipWhitespace()
    {
        while (Position < Text.Length)
        {
            char character = Text[Position];
            if (character != ' ' && character != '\t' && character != '\r' && character != '\n')
            {
                return;
            }
            Position = Position + 1;
        }
    }

    static int HexadecimalDigit(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }
        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }
        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }
        return -1;
    }

    void Error(string message)
    {
        throw FakeException.Create($"Malformed locale pack resource '{ResourceName}' at character {Position}: {message}.");
    }
}
