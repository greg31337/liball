using System.Collections.Generic;
using System.Text;

namespace jmap;

internal class UriTemplateExpander
{
    internal ValueValidator Values = new ValueValidator();

    internal string ExpandUploadUrl(string? template, string? accountId)
    {
        Dictionary<string, string?> variables = new Dictionary<string, string?>();
        variables.Add(JmapProperties.AccountId, accountId);
        return ExpandEndpointTemplate(
            template,
            variables,
            new List<string> { JmapProperties.AccountId },
            "/uploadUrl");
    }

    internal string ExpandDownloadUrl(string? template, string? accountId, string? blobId, string? type, string? name)
    {
        Dictionary<string, string?> variables = new Dictionary<string, string?>();
        variables.Add(JmapProperties.AccountId, accountId);
        variables.Add(JmapProperties.BlobId, blobId);
        variables.Add(JmapProperties.Type, type);
        variables.Add(JmapProperties.Name, name);
        return ExpandEndpointTemplate(
            template,
            variables,
            new List<string> { JmapProperties.AccountId, JmapProperties.BlobId, JmapProperties.Type, JmapProperties.Name },
            "/downloadUrl");
    }

    internal string ExpandEventSourceUrl(string? template, string? types, string? closeAfter, string? ping)
    {
        Dictionary<string, string?> variables = new Dictionary<string, string?>();
        variables.Add(JmapProperties.Types, types);
        variables.Add(JmapProperties.CloseAfter, closeAfter);
        variables.Add(JmapProperties.Ping, ping);
        return ExpandEndpointTemplate(
            template,
            variables,
            new List<string> { JmapProperties.Types, JmapProperties.CloseAfter, JmapProperties.Ping },
            "/eventSourceUrl");
    }

    internal string ExpandEndpointTemplate(string? template, Dictionary<string, string?>? variables, List<string> requiredVariables, string path)
    {
        if (template == null)
        {
            throw new JmapProtocolException("A Session endpoint URI template is required.", path, default(System.Text.Json.JsonElement));
        }

        if (variables == null)
        {
            throw new System.ArgumentNullException("variables");
        }

        if (template.Length > SecurityLimits.MaximumUriTemplateCharacters ||
            !Values.IsValidUnicode(template))
        {
            throw new JmapProtocolException("A Session endpoint URI template exceeds a security bound or contains invalid Unicode.", path, default(System.Text.Json.JsonElement));
        }

        StringBuilder result = new StringBuilder();
        List<string> encounteredVariables = new List<string>();
        int index = 0;
        while (index < template.Length)
        {
            char character = template[index];
            if (character == '{')
            {
                int closingIndex = FindExpressionEnd(template, index, path);
                string variable = template.Substring(index + 1, closingIndex - index - 1);
                if (!IsLevelOneVariableName(variable))
                {
                    throw new JmapProtocolException("A Session endpoint URI template expression is not RFC 6570 Level 1.", path, default(System.Text.Json.JsonElement));
                }

                string? value = null;
                if (variables.ContainsKey(variable))
                {
                    value = variables[variable];
                }

                if (value == null)
                {
                    throw new JmapProtocolException("A Session endpoint URI template variable has no expansion value: " + variable, path, default(System.Text.Json.JsonElement));
                }

                if (!Values.IsValidUnicode(value) ||
                    Encoding.UTF8.GetByteCount(value) > SecurityLimits.MaximumUriVariableBytes)
                {
                    throw new JmapProtocolException("A Session endpoint URI template variable exceeds a security bound or contains invalid Unicode: " + variable, path, default(System.Text.Json.JsonElement));
                }

                result.Append(PercentEncodeValue(value));
                RequireExpandedLength(result, path);
                if (!encounteredVariables.Contains(variable))
                {
                    encounteredVariables.Add(variable);
                }

                index = closingIndex + 1;
            }
            else if (character == '}')
            {
                throw new JmapProtocolException("A Session endpoint URI template contains an unmatched closing brace.", path, default(System.Text.Json.JsonElement));
            }
            else if (character == '%')
            {
                if (index + 2 >= template.Length || !IsHexDigit(template[index + 1]) || !IsHexDigit(template[index + 2]))
                {
                    throw new JmapProtocolException("A Session endpoint URI template contains an invalid percent-encoded triplet.", path, default(System.Text.Json.JsonElement));
                }

                result.Append(character);
                result.Append(template[index + 1]);
                result.Append(template[index + 2]);
                RequireExpandedLength(result, path);
                index = index + 3;
            }
            else if (character <= 127)
            {
                if (!IsAllowedLiteral(character))
                {
                    throw new JmapProtocolException("A Session endpoint URI template contains a character not allowed by RFC 6570.", path, default(System.Text.Json.JsonElement));
                }

                result.Append(character);
                RequireExpandedLength(result, path);
                index = index + 1;
            }
            else if (character >= 160)
            {
                int characterLength = 1;
                if (char.IsHighSurrogate(character))
                {
                    characterLength = 2;
                }

                result.Append(PercentEncodeValue(template.Substring(index, characterLength)));
                RequireExpandedLength(result, path);
                index = index + characterLength;
            }
            else
            {
                throw new JmapProtocolException("A Session endpoint URI template contains a character not allowed by RFC 6570.", path, default(System.Text.Json.JsonElement));
            }
        }

        foreach (string requiredVariable in requiredVariables)
        {
            if (!encounteredVariables.Contains(requiredVariable))
            {
                throw new JmapProtocolException("A Session endpoint URI template is missing required variable: " + requiredVariable, path, default(System.Text.Json.JsonElement));
            }
        }

        return result.ToString();
    }

    internal int FindExpressionEnd(string template, int openingIndex, string path)
    {
        int index = openingIndex + 1;
        while (index < template.Length)
        {
            if (template[index] == '{')
            {
                throw new JmapProtocolException("A Session endpoint URI template contains a nested expression.", path, default(System.Text.Json.JsonElement));
            }

            if (template[index] == '}')
            {
                return index;
            }

            index = index + 1;
        }

        throw new JmapProtocolException("A Session endpoint URI template contains an unclosed expression.", path, default(System.Text.Json.JsonElement));
    }

    internal bool IsLevelOneVariableName(string variable)
    {
        if (variable.Length == 0)
        {
            return false;
        }

        bool requiresCharacter = true;
        int index = 0;
        while (index < variable.Length)
        {
            char character = variable[index];
            if (character == '.')
            {
                if (requiresCharacter)
                {
                    return false;
                }

                requiresCharacter = true;
                index = index + 1;
            }
            else if (character == '%')
            {
                if (index + 2 >= variable.Length || !IsHexDigit(variable[index + 1]) || !IsHexDigit(variable[index + 2]))
                {
                    return false;
                }

                requiresCharacter = false;
                index = index + 3;
            }
            else
            {
                bool isUppercase = character >= 'A' && character <= 'Z';
                bool isLowercase = character >= 'a' && character <= 'z';
                bool isDigit = character >= '0' && character <= '9';
                if (!isUppercase && !isLowercase && !isDigit && character != '_')
                {
                    return false;
                }

                requiresCharacter = false;
                index = index + 1;
            }
        }

        return !requiresCharacter;
    }

    internal string PercentEncodeValue(string value)
    {
        if (!Values.IsValidUnicode(value) ||
            Encoding.UTF8.GetByteCount(value) > SecurityLimits.MaximumUriVariableBytes)
        {
            throw new JmapProtocolException("A URI template value exceeds a security bound or contains invalid Unicode.", null, default(System.Text.Json.JsonElement));
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        StringBuilder encoded = new StringBuilder();
        int index = 0;
        while (index < bytes.Length)
        {
            byte current = bytes[index];
            if (IsUnreservedByte(current))
            {
                encoded.Append((char)current);
            }
            else
            {
                encoded.Append('%');
                encoded.Append("0123456789ABCDEF"[(current >> 4) & 15]);
                encoded.Append("0123456789ABCDEF"[current & 15]);
            }

            index = index + 1;
        }

        return encoded.ToString();
    }

    internal void RequireExpandedLength(StringBuilder result, string path)
    {
        if (result.Length > SecurityLimits.MaximumExpandedUriCharacters)
        {
            throw new JmapProtocolException("An expanded Session endpoint URI exceeds the supported length.", path, default(System.Text.Json.JsonElement));
        }
    }

    internal bool IsUnreservedByte(byte value)
    {
        bool isUppercase = value >= (byte)'A' && value <= (byte)'Z';
        bool isLowercase = value >= (byte)'a' && value <= (byte)'z';
        bool isDigit = value >= (byte)'0' && value <= (byte)'9';
        return isUppercase || isLowercase || isDigit || value == (byte)'-' || value == (byte)'.' || value == (byte)'_' || value == (byte)'~';
    }

    internal bool IsAllowedLiteral(char value)
    {
        return value == '!' ||
            (value >= '#' && value <= '$') ||
            value == '&' ||
            (value >= '(' && value <= ';') ||
            value == '=' ||
            (value >= '?' && value <= '[') ||
            value == ']' ||
            value == '_' ||
            (value >= 'a' && value <= 'z') ||
            value == '~';
    }

    internal bool IsHexDigit(char value)
    {
        return (value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'F') ||
            (value >= 'a' && value <= 'f');
    }
}
