using System.Collections.Generic;
using System.Text;

namespace tiff;

internal class TiffJsonDescriptionParser
{
    internal string Source;
    internal int Position;
    internal int Depth;
    internal string Error;

    internal TiffJsonDescriptionParser(string source)
    {
        Source = source == null ? "" : source;
        Position = 0;
        Depth = 0;
        Error = "";
    }

    internal TiffJsonDescriptionParseResult ParseDescription()
    {
        SkipJsonWhitespace();
        bool looksLikeJson = Position < Source.Length && (Source[Position] == '{' || Source[Position] == '[');
        if (!looksLikeJson)
        {
            return TiffJsonDescriptionParseResult.Create(false, false, new TiffJsonNode(), "");
        }

        TiffJsonNode root = ParseJsonValue("");
        SkipJsonWhitespace();
        if (Error.Length == 0 && Position != Source.Length)
        {
            SetParseError("JSON content contains trailing characters.");
        }

        return TiffJsonDescriptionParseResult.Create(true, Error.Length == 0, root, Error);
    }

    internal TiffJsonNode ParseJsonValue(string name)
    {
        TiffJsonNode node = new TiffJsonNode();
        node.Name = name == null ? "" : name;
        SkipJsonWhitespace();
        int start = Position;
        Depth = Depth + 1;
        if (Depth > 64)
        {
            SetParseError("JSON content exceeds the supported nesting depth.");
        }
        else if (Position >= Source.Length)
        {
            SetParseError("JSON content ends before a value is complete.");
        }
        else if (Source[Position] == '{')
        {
            node = ParseJsonObject(node.Name);
        }
        else if (Source[Position] == '[')
        {
            node = ParseJsonArray(node.Name);
        }
        else if (Source[Position] == '"')
        {
            node.Kind = TiffJsonValueKind.String;
            node.Text = ReadJsonString();
        }
        else if (Source[Position] == '-' || IsJsonDigit(Source[Position]))
        {
            node.Kind = TiffJsonValueKind.Number;
            node.Text = ReadJsonNumber();
        }
        else if (Source[Position] == 't')
        {
            node.Kind = TiffJsonValueKind.Boolean;
            node.BooleanValue = true;
            ConsumeJsonLiteral("true");
        }
        else if (Source[Position] == 'f')
        {
            node.Kind = TiffJsonValueKind.Boolean;
            node.BooleanValue = false;
            ConsumeJsonLiteral("false");
        }
        else if (Source[Position] == 'n')
        {
            node.Kind = TiffJsonValueKind.Null;
            ConsumeJsonLiteral("null");
        }
        else
        {
            SetParseError("JSON content contains an invalid value.");
        }

        if (Error.Length == 0)
        {
            node.RawText = Source.Substring(start, Position - start);
        }

        Depth = Depth - 1;
        return node;
    }

    internal TiffJsonNode ParseJsonObject(string name)
    {
        TiffJsonNode node = new TiffJsonNode();
        node.Name = name;
        node.Kind = TiffJsonValueKind.Object;
        List<TiffJsonNode> children = new List<TiffJsonNode>();
        Position = Position + 1;
        SkipJsonWhitespace();
        if (Position < Source.Length && Source[Position] == '}')
        {
            Position = Position + 1;
            return node;
        }

        while (Error.Length == 0)
        {
            if (Position >= Source.Length || Source[Position] != '"')
            {
                SetParseError("JSON object property names must be strings.");
                break;
            }

            string propertyName = ReadJsonString();
            SkipJsonWhitespace();
            if (Position >= Source.Length || Source[Position] != ':')
            {
                SetParseError("JSON object property names must be followed by a colon.");
                break;
            }

            Position = Position + 1;
            TiffJsonNode child = ParseJsonValue(propertyName);
            if (Error.Length == 0)
            {
                children.Add(child);
            }

            SkipJsonWhitespace();
            if (Position < Source.Length && Source[Position] == '}')
            {
                Position = Position + 1;
                break;
            }

            if (Position >= Source.Length || Source[Position] != ',')
            {
                SetParseError("JSON object properties must be separated by commas.");
                break;
            }

            Position = Position + 1;
            SkipJsonWhitespace();
        }

        node.Children = children.ToArray();
        return node;
    }

    internal TiffJsonNode ParseJsonArray(string name)
    {
        TiffJsonNode node = new TiffJsonNode();
        node.Name = name;
        node.Kind = TiffJsonValueKind.Array;
        List<TiffJsonNode> children = new List<TiffJsonNode>();
        Position = Position + 1;
        SkipJsonWhitespace();
        if (Position < Source.Length && Source[Position] == ']')
        {
            Position = Position + 1;
            return node;
        }

        while (Error.Length == 0)
        {
            TiffJsonNode child = ParseJsonValue("");
            if (Error.Length == 0)
            {
                children.Add(child);
            }

            SkipJsonWhitespace();
            if (Position < Source.Length && Source[Position] == ']')
            {
                Position = Position + 1;
                break;
            }

            if (Position >= Source.Length || Source[Position] != ',')
            {
                SetParseError("JSON array values must be separated by commas.");
                break;
            }

            Position = Position + 1;
            SkipJsonWhitespace();
        }

        node.Children = children.ToArray();
        return node;
    }

    internal string ReadJsonString()
    {
        StringBuilder builder = new StringBuilder();
        Position = Position + 1;
        while (Position < Source.Length && Error.Length == 0)
        {
            char value = Source[Position];
            Position = Position + 1;
            if (value == '"')
            {
                return builder.ToString();
            }

            if (value < 32)
            {
                SetParseError("JSON strings cannot contain unescaped control characters.");
            }
            else if (value != '\\')
            {
                builder.Append(value);
            }
            else if (Position >= Source.Length)
            {
                SetParseError("JSON string escape is incomplete.");
            }
            else
            {
                char escape = Source[Position];
                Position = Position + 1;
                if (escape == '"' || escape == '\\' || escape == '/')
                {
                    builder.Append(escape);
                }
                else if (escape == 'b')
                {
                    builder.Append('\b');
                }
                else if (escape == 'f')
                {
                    builder.Append('\f');
                }
                else if (escape == 'n')
                {
                    builder.Append('\n');
                }
                else if (escape == 'r')
                {
                    builder.Append('\r');
                }
                else if (escape == 't')
                {
                    builder.Append('\t');
                }
                else if (escape == 'u')
                {
                    int unicodeValue = ReadJsonHexValue();
                    if (unicodeValue >= 0)
                    {
                        builder.Append((char)unicodeValue);
                    }
                }
                else
                {
                    SetParseError("JSON string contains an invalid escape sequence.");
                }
            }
        }

        if (Error.Length == 0)
        {
            SetParseError("JSON string is not terminated.");
        }

        return builder.ToString();
    }

    internal string ReadJsonNumber()
    {
        int start = Position;
        if (Source[Position] == '-')
        {
            Position = Position + 1;
        }

        if (Position >= Source.Length)
        {
            SetParseError("JSON number is incomplete.");
            return "";
        }

        if (Source[Position] == '0')
        {
            Position = Position + 1;
            if (Position < Source.Length && IsJsonDigit(Source[Position]))
            {
                SetParseError("JSON numbers cannot contain leading zeroes.");
            }
        }
        else if (Source[Position] >= '1' && Source[Position] <= '9')
        {
            while (Position < Source.Length && IsJsonDigit(Source[Position]))
            {
                Position = Position + 1;
            }
        }
        else
        {
            SetParseError("JSON number requires an integer component.");
        }

        if (Error.Length == 0 && Position < Source.Length && Source[Position] == '.')
        {
            Position = Position + 1;
            if (Position >= Source.Length || !IsJsonDigit(Source[Position]))
            {
                SetParseError("JSON number fraction requires at least one digit.");
            }

            while (Position < Source.Length && IsJsonDigit(Source[Position]))
            {
                Position = Position + 1;
            }
        }

        if (Error.Length == 0 && Position < Source.Length && (Source[Position] == 'e' || Source[Position] == 'E'))
        {
            Position = Position + 1;
            if (Position < Source.Length && (Source[Position] == '+' || Source[Position] == '-'))
            {
                Position = Position + 1;
            }

            if (Position >= Source.Length || !IsJsonDigit(Source[Position]))
            {
                SetParseError("JSON number exponent requires at least one digit.");
            }

            while (Position < Source.Length && IsJsonDigit(Source[Position]))
            {
                Position = Position + 1;
            }
        }

        return Source.Substring(start, Position - start);
    }

    internal int ReadJsonHexValue()
    {
        if (Position + 4 > Source.Length)
        {
            SetParseError("JSON Unicode escape is incomplete.");
            return -1;
        }

        int result = 0;
        int count = 0;
        while (count < 4)
        {
            int digit = GetJsonHexDigit(Source[Position]);
            if (digit < 0)
            {
                SetParseError("JSON Unicode escape contains a non-hexadecimal digit.");
                return -1;
            }

            result = result * 16 + digit;
            Position = Position + 1;
            count = count + 1;
        }

        return result;
    }

    internal void ConsumeJsonLiteral(string literal)
    {
        int index = 0;
        while (index < literal.Length && Position + index < Source.Length && Source[Position + index] == literal[index])
        {
            index = index + 1;
        }

        if (index != literal.Length)
        {
            SetParseError("JSON content contains an invalid literal value.");
        }
        else
        {
            Position = Position + literal.Length;
        }
    }

    internal void SkipJsonWhitespace()
    {
        while (Position < Source.Length && (Source[Position] == ' ' || Source[Position] == '\t' || Source[Position] == '\r' || Source[Position] == '\n'))
        {
            Position = Position + 1;
        }
    }

    internal void SetParseError(string message)
    {
        if (Error.Length == 0)
        {
            Error = $"{message} Position {Position}.";
        }
    }

    internal static bool IsJsonDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static int GetJsonHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }
}

internal class TiffJsonDescriptionParseResult
{
    internal bool LooksLikeJson;
    internal bool Success;
    internal TiffJsonNode Root;
    internal string Error;

    internal TiffJsonDescriptionParseResult()
    {
        LooksLikeJson = false;
        Success = false;
        Root = new TiffJsonNode();
        Error = "";
    }

    internal static TiffJsonDescriptionParseResult Create(bool looksLikeJson, bool success, TiffJsonNode root, string error)
    {
        TiffJsonDescriptionParseResult result = new TiffJsonDescriptionParseResult();
        result.LooksLikeJson = looksLikeJson;
        result.Success = success;
        result.Root = root == null ? new TiffJsonNode() : root;
        result.Error = error == null ? "" : error;
        return result;
    }
}
