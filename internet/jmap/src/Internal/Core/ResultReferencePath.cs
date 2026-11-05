using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace jmap;

internal class ResultReferencePath
{
    internal JsonPointer Pointers = new JsonPointer();
    internal JmapJsonReader Reader = new JmapJsonReader(int.MaxValue, 1000);

    internal List<string> ParsePath(string? path)
    {
        if (!Pointers.IsPointer(path))
        {
            throw new JmapProtocolException("A result-reference path is not valid JSON Pointer syntax.", path, default(JsonElement));
        }

        return Pointers.ParsePointer(path);
    }

    internal JsonElement EvaluatePath(JsonElement arguments, string? path)
    {
        if (arguments.ValueKind == JsonValueKind.Undefined)
        {
            throw new JmapProtocolException("A result-reference path cannot be evaluated against an undefined value.", path, default(JsonElement));
        }

        List<string> tokens = ParsePath(path);
        string errorPath = path == null ? "" : path;
        return EvaluateTokens(arguments, tokens, 0, errorPath);
    }

    internal bool CanEvaluatePath(JsonElement arguments, string? path)
    {
        if (arguments.ValueKind == JsonValueKind.Undefined || !Pointers.IsPointer(path))
        {
            return false;
        }

        List<string> tokens = Pointers.ParsePointer(path);
        return CanEvaluateTokens(arguments, tokens, 0);
    }

    internal bool CanEvaluateTokens(JsonElement current, List<string> tokens, int tokenIndex)
    {
        if (tokenIndex == tokens.Count)
        {
            return current.ValueKind != JsonValueKind.Undefined;
        }

        string token = tokens[tokenIndex];
        if (current.ValueKind == JsonValueKind.Object)
        {
            JsonElement selected = ReadUniqueObjectMember(current, token);
            if (selected.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }

            return CanEvaluateTokens(selected, tokens, tokenIndex + 1);
        }

        if (current.ValueKind == JsonValueKind.Array)
        {
            if (token == "*")
            {
                foreach (JsonElement item in current.EnumerateArray())
                {
                    if (!CanEvaluateTokens(item, tokens, tokenIndex + 1))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (!IsArrayIndex(token))
            {
                return false;
            }

            long index = ParseArrayIndex(token);
            if (index >= current.GetArrayLength())
            {
                return false;
            }

            return CanEvaluateTokens(current[(int)index], tokens, tokenIndex + 1);
        }

        return false;
    }

    internal JsonElement ReadUniqueObjectMember(JsonElement value, string token)
    {
        JsonElement selected = default(JsonElement);
        int matches = 0;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == token)
            {
                selected = property.Value;
                matches = matches + 1;
            }
        }

        if (matches == 1)
        {
            return selected;
        }

        return default(JsonElement);
    }

    internal JsonElement EvaluateTokens(JsonElement current, List<string> tokens, int tokenIndex, string path)
    {
        if (tokenIndex == tokens.Count)
        {
            return current.Clone();
        }

        string token = tokens[tokenIndex];
        if (current.ValueKind == JsonValueKind.Object)
        {
            JsonElement selected = SelectObjectMember(current, token, path);
            return EvaluateTokens(selected, tokens, tokenIndex + 1, path);
        }

        if (current.ValueKind == JsonValueKind.Array)
        {
            if (token == "*")
            {
                return ProjectArray(current, tokens, tokenIndex + 1, path);
            }

            JsonElement selected = SelectArrayElement(current, token, path);
            return EvaluateTokens(selected, tokens, tokenIndex + 1, path);
        }

        throw new JmapProtocolException("A result-reference path cannot traverse a scalar or null value.", path, current.Clone());
    }

    internal JsonElement SelectObjectMember(JsonElement value, string token, string path)
    {
        JsonElement selected = default(JsonElement);
        int matches = 0;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == token)
            {
                selected = property.Value;
                matches = matches + 1;
            }
        }

        if (matches != 1)
        {
            throw new JmapProtocolException("A result-reference path does not identify a unique object member.", path, value.Clone());
        }

        return selected;
    }

    internal JsonElement SelectArrayElement(JsonElement value, string token, string path)
    {
        if (!IsArrayIndex(token))
        {
            throw new JmapProtocolException("A result-reference path contains an invalid array index.", path, value.Clone());
        }

        long index = ParseArrayIndex(token);
        if (index >= value.GetArrayLength())
        {
            throw new JmapProtocolException("A result-reference path array index is outside the array.", path, value.Clone());
        }

        return value[(int)index];
    }

    internal bool IsArrayIndex(string token)
    {
        if (token.Length == 0 || (token.Length > 1 && token[0] == '0'))
        {
            return false;
        }

        int index = 0;
        while (index < token.Length)
        {
            if (token[index] < '0' || token[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal long ParseArrayIndex(string token)
    {
        long result = 0L;
        int index = 0;
        while (index < token.Length)
        {
            int digit = token[index] - '0';
            if (result > (int.MaxValue - digit) / 10L)
            {
                return (long)int.MaxValue + 1L;
            }

            result = (result * 10L) + digit;
            index = index + 1;
        }

        return result;
    }

    internal JsonElement ProjectArray(JsonElement value, List<string> tokens, int tokenIndex, string path)
    {
        List<JsonElement> projected = new List<JsonElement>();
        foreach (JsonElement item in value.EnumerateArray())
        {
            JsonElement result = EvaluateTokens(item, tokens, tokenIndex, path);
            if (result.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement nested in result.EnumerateArray())
                {
                    projected.Add(nested.Clone());
                }
            }
            else
            {
                projected.Add(result.Clone());
            }
        }

        return BuildArray(projected);
    }

    internal JsonElement BuildArray(List<JsonElement> values)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        writer.WriteArrayStart();
        foreach (JsonElement value in values)
        {
            writer.WriteElementValue(value);
        }

        writer.WriteArrayEnd();
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }
}
