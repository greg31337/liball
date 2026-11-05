using System.Collections.Generic;
using System.Text;

namespace jmap;

internal class JsonPointer
{
    internal ValueValidator Values = new ValueValidator();

    internal bool IsPointer(string? pointer)
    {
        if (pointer == null || pointer.Length > SecurityLimits.MaximumJsonPointerCharacters ||
            !Values.IsValidUnicode(pointer))
        {
            return false;
        }

        if (pointer.Length == 0)
        {
            return true;
        }

        if (pointer[0] != '/')
        {
            return false;
        }

        int index = 1;
        int tokens = 1;
        while (index < pointer.Length)
        {
            if (pointer[index] == '/')
            {
                tokens = tokens + 1;
                if (tokens > SecurityLimits.MaximumJsonPointerTokens)
                {
                    return false;
                }
            }

            if (pointer[index] == '~')
            {
                if (index + 1 >= pointer.Length || (pointer[index + 1] != '0' && pointer[index + 1] != '1'))
                {
                    return false;
                }

                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
        }

        return true;
    }

    internal List<string> ParsePointer(string? pointer)
    {
        if (!IsPointer(pointer) || pointer == null)
        {
            throw new JmapProtocolException("A JSON Pointer is not valid RFC 6901 syntax.", null, default(System.Text.Json.JsonElement));
        }

        List<string> tokens = new List<string>();
        if (pointer.Length == 0)
        {
            return tokens;
        }

        int tokenStart = 1;
        int index = 1;
        while (index <= pointer.Length)
        {
            if (index == pointer.Length || pointer[index] == '/')
            {
                string encodedToken = pointer.Substring(tokenStart, index - tokenStart);
                tokens.Add(UnescapeToken(encodedToken));
                tokenStart = index + 1;
            }

            index = index + 1;
        }

        return tokens;
    }

    internal string UnescapeToken(string? token)
    {
        if (token == null)
        {
            throw new System.ArgumentNullException("token");
        }

        if (!Values.IsValidUnicode(token))
        {
            throw new JmapProtocolException("A JSON Pointer reference token contains invalid Unicode.", null, default(System.Text.Json.JsonElement));
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < token.Length)
        {
            if (token[index] == '~')
            {
                if (index + 1 >= token.Length || (token[index + 1] != '0' && token[index + 1] != '1'))
                {
                    throw new JmapProtocolException("A JSON Pointer reference token contains an invalid escape.", null, default(System.Text.Json.JsonElement));
                }

                if (token[index + 1] == '0')
                {
                    result.Append('~');
                }
                else
                {
                    result.Append('/');
                }

                index = index + 2;
            }
            else
            {
                if (token[index] == '/')
                {
                    throw new JmapProtocolException("A JSON Pointer reference token contains an unescaped slash.", null, default(System.Text.Json.JsonElement));
                }

                result.Append(token[index]);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal string EscapeToken(string? token)
    {
        if (token == null)
        {
            throw new System.ArgumentNullException("token");
        }

        if (!Values.IsValidUnicode(token))
        {
            throw new JmapProtocolException("A JSON Pointer reference token contains invalid Unicode.", null, default(System.Text.Json.JsonElement));
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < token.Length)
        {
            if (token[index] == '~')
            {
                result.Append("~0");
            }
            else if (token[index] == '/')
            {
                result.Append("~1");
            }
            else
            {
                result.Append(token[index]);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string FormatPointer(List<string>? tokens)
    {
        if (tokens == null)
        {
            throw new System.ArgumentNullException("tokens");
        }

        if (tokens.Count > SecurityLimits.MaximumJsonPointerTokens)
        {
            throw new JmapProtocolException("A JSON Pointer contains too many reference tokens.", null, default(System.Text.Json.JsonElement));
        }

        StringBuilder result = new StringBuilder();
        foreach (string token in tokens)
        {
            result.Append('/');
            result.Append(EscapeToken(token));
            if (result.Length > SecurityLimits.MaximumJsonPointerCharacters)
            {
                throw new JmapProtocolException("A JSON Pointer exceeds the supported length.", null, default(System.Text.Json.JsonElement));
            }
        }

        return result.ToString();
    }
}
