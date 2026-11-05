using System;
using System.Text;
namespace token;

/// <summary>
/// Implements strict JSON reading and deterministic JSON writing.
/// </summary>
public class TokenJsonCodec : ITokenJsonCodec
{
    /// <summary>Initializes a stateless JSON codec.</summary>
    public TokenJsonCodec()
    {
    }

    /// <summary>Reads one duplicate-safe JSON value from text.</summary>
    /// <param name="text">The JSON text.</param>
    /// <param name="maximumLength">The maximum accepted UTF-8 byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays and objects.</param>
    /// <returns>The decoded value, or stable failure details.</returns>
    public JsonValueResult ReadJsonText(string text, int maximumLength, int maximumDepth)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        ValidateJsonLimits(maximumLength, maximumDepth);
        TokenBytesResult encoded = Utf8Codec.Encode(text);
        if (!encoded.Success)
        {
            return CreateJsonReadFailure(encoded.Error);
        }

        byte[] bytes = encoded.Bytes ?? throw new InvalidOperationException("A required value is null.");
        return ReadJsonBytesCore(bytes, maximumLength, maximumDepth);
    }

    /// <summary>Reads one duplicate-safe JSON value from strict UTF-8.</summary>
    /// <param name="utf8">The JSON bytes.</param>
    /// <param name="maximumLength">The maximum accepted byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays and objects.</param>
    /// <returns>The decoded value, or stable failure details.</returns>
    public JsonValueResult ReadJsonUtf8(byte[] utf8, int maximumLength, int maximumDepth)
    {
        if (utf8 == null)
        {
            throw new ArgumentNullException("utf8");
        }

        ValidateJsonLimits(maximumLength, maximumDepth);
        return ReadJsonBytesCore(utf8, maximumLength, maximumDepth);
    }

    /// <summary>Writes one JSON value with deterministic object-member ordering.</summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="maximumLength">The maximum output UTF-8 byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays and objects.</param>
    /// <returns>The compact JSON text, or stable failure details.</returns>
    public TokenTextResult WriteJsonText(JsonValue value, int maximumLength, int maximumDepth)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateJsonLimits(maximumLength, maximumDepth);
        StringBuilder builder = new StringBuilder();
        TokenResult writeResult = AppendJsonValue(builder, value, maximumDepth, 0);
        if (!writeResult.Success)
        {
            TokenTextResult failure = new TokenTextResult();
            failure.Error = writeResult.Error;
            return failure;
        }

        string text = builder.ToString();
        TokenBytesResult encoded = Utf8Codec.Encode(text);
        if (!encoded.Success)
        {
            TokenTextResult failure = new TokenTextResult();
            failure.Error = encoded.Error;
            return failure;
        }

        if ((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > maximumLength)
        {
            TokenTextResult failure = new TokenTextResult();
            failure.Error = CreateJsonSizeError(maximumLength);
            return failure;
        }

        TokenTextResult result = new TokenTextResult();
        result.Text = text;
        result.Success = true;
        return result;
    }

    /// <summary>Writes one JSON value as strict UTF-8 with deterministic object-member ordering.</summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="maximumLength">The maximum output byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays and objects.</param>
    /// <returns>The compact JSON bytes, or stable failure details.</returns>
    public TokenBytesResult WriteJsonUtf8(JsonValue value, int maximumLength, int maximumDepth)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateJsonLimits(maximumLength, maximumDepth);
        TokenTextResult textResult = WriteJsonText(value, maximumLength, maximumDepth);
        if (!textResult.Success)
        {
            TokenBytesResult failure = new TokenBytesResult();
            failure.Error = textResult.Error;
            return failure;
        }

        return Utf8Codec.Encode(textResult.Text ?? throw new InvalidOperationException("A required value is null."));
    }

    internal static JsonValueResult ReadJsonBytesCore(byte[] utf8, int maximumLength, int maximumDepth)
    {
        if (utf8.Length > maximumLength)
        {
            return CreateJsonReadFailure(CreateJsonSizeError(maximumLength));
        }

        JsonReadResult readResult = JsonReader.Read(utf8, maximumDepth);
        if (!readResult.Success)
        {
            return CreateJsonReadFailure(readResult.Error);
        }

        using (readResult.Document)
        {
            JsonValueResult result = new JsonValueResult();
            result.Value = JsonObject.CreateValue(
                (readResult.Document ?? throw new InvalidOperationException("A required value is null.")).RootElement);
            result.Success = true;
            return result;
        }
    }

    internal static TokenResult AppendJsonValue(
        StringBuilder builder,
        JsonValue value,
        int maximumDepth,
        int depth)
    {
        if (value == null)
        {
            return CreateInvalidJsonValue("A JSON value must not be null.", builder.Length);
        }

        switch (value.Kind)
        {
            case TokenJsonValueKind.Null:
                builder.Append("null");
                return CreateSuccessfulJsonWrite();
            case TokenJsonValueKind.Boolean:
                builder.Append(value.BooleanValue ? "true" : "false");
                return CreateSuccessfulJsonWrite();
            case TokenJsonValueKind.Number:
                return AppendJsonNumber(builder, value.NumberText);
            case TokenJsonValueKind.String:
                return AppendJsonString(builder, value.StringValue);
            case TokenJsonValueKind.Array:
                return AppendJsonArray(builder, value.ArrayValue, maximumDepth, depth);
            case TokenJsonValueKind.Object:
                return AppendJsonObject(builder, value.ObjectValue, maximumDepth, depth);
            default:
                return CreateInvalidJsonValue("The JSON value kind is invalid.", builder.Length);
        }
    }

    internal static TokenResult AppendJsonNumber(StringBuilder builder, string? numberText)
    {
        if (numberText == null)
        {
            return CreateInvalidJsonValue("A JSON number must contain number text.", builder.Length);
        }

        JsonReadResult readResult = JsonReader.Read(numberText, 0);
        if (!readResult.Success)
        {
            return CreateInvalidJsonValue("A JSON number must contain valid JSON number text.", builder.Length);
        }

        using (readResult.Document)
        {
            System.Text.Json.JsonElement element =
                (readResult.Document ?? throw new InvalidOperationException("A required value is null.")).RootElement;
            if (element.ValueKind != System.Text.Json.JsonValueKind.Number || element.GetRawText() != numberText)
            {
                return CreateInvalidJsonValue("A JSON number must contain only valid JSON number text.", builder.Length);
            }
        }

        builder.Append(numberText);
        return CreateSuccessfulJsonWrite();
    }

    internal static TokenResult AppendJsonString(StringBuilder builder, string? value)
    {
        if (value == null)
        {
            return CreateInvalidJsonValue("A JSON string must contain text.", builder.Length);
        }

        TokenTextResult quoted = JsonWriter.WriteStringValue(value);
        if (!quoted.Success)
        {
            TokenResult failure = new TokenResult();
            failure.Error = quoted.Error;
            return failure;
        }

        builder.Append(quoted.Text ?? throw new InvalidOperationException("A required value is null."));
        return CreateSuccessfulJsonWrite();
    }

    internal static TokenResult AppendJsonArray(
        StringBuilder builder,
        JsonValue[]? values,
        int maximumDepth,
        int depth)
    {
        TokenResult depthResult = CheckJsonDepth(builder.Length, maximumDepth, depth);
        if (!depthResult.Success)
        {
            return depthResult;
        }

        if (values == null)
        {
            return CreateInvalidJsonValue("A JSON array must contain an item array.", builder.Length);
        }

        builder.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            TokenResult itemResult = AppendJsonValue(builder, values[index], maximumDepth, depth + 1);
            if (!itemResult.Success)
            {
                return itemResult;
            }

            index += 1;
        }

        builder.Append(']');
        return CreateSuccessfulJsonWrite();
    }

    internal static TokenResult AppendJsonObject(
        StringBuilder builder,
        JsonObject? value,
        int maximumDepth,
        int depth)
    {
        TokenResult depthResult = CheckJsonDepth(builder.Length, maximumDepth, depth);
        if (!depthResult.Success)
        {
            return depthResult;
        }

        if (value == null || value.Members == null)
        {
            return CreateInvalidJsonValue("A JSON object must contain a member array.", builder.Length);
        }

        JsonMember[] members = value.Members;
        int[] order = new int[members.Length];
        string[] encodedNames = new string[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            JsonMember member = members[index];
            if (member == null || member.Name == null || member.Value == null)
            {
                return CreateInvalidJsonValue("A JSON object member must contain a name and value.", index);
            }

            TokenTextResult encodedName = JsonWriter.WriteStringValue(member.Name);
            if (!encodedName.Success)
            {
                TokenResult failure = new TokenResult();
                failure.Error = encodedName.Error;
                return failure;
            }

            encodedNames[index] = encodedName.Text ?? throw new InvalidOperationException("A required value is null.");
            order[index] = index;
            index += 1;
        }

        SortJsonMemberOrder(members, order);
        index = 1;
        while (index < order.Length)
        {
            string previous = members[order[index - 1]].Name ?? throw new InvalidOperationException("A required value is null.");
            string current = members[order[index]].Name ?? throw new InvalidOperationException("A required value is null.");
            if (CompareJsonNames(previous, current) == 0)
            {
                TokenResult duplicate = new TokenResult();
                duplicate.Error = new TokenError(
                    "duplicate_json_member",
                    "A deterministic JSON object must not contain duplicate member names.",
                    order[index]);
                return duplicate;
            }

            index += 1;
        }

        builder.Append('{');
        index = 0;
        while (index < order.Length)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            int memberIndex = order[index];
            builder.Append(encodedNames[memberIndex]);
            builder.Append(':');
            TokenResult memberResult = AppendJsonValue(
                builder,
                members[memberIndex].Value ?? throw new InvalidOperationException("A required value is null."),
                maximumDepth,
                depth + 1);
            if (!memberResult.Success)
            {
                return memberResult;
            }

            index += 1;
        }

        builder.Append('}');
        return CreateSuccessfulJsonWrite();
    }

    internal static void SortJsonMemberOrder(JsonMember[] members, int[] order)
    {
        int index = 1;
        while (index < order.Length)
        {
            int current = order[index];
            int insertion = index;
            string currentName = members[current].Name ?? throw new InvalidOperationException("A required value is null.");
            while (insertion > 0)
            {
                string previousName = members[order[insertion - 1]].Name ?? throw new InvalidOperationException("A required value is null.");
                if (CompareJsonNames(previousName, currentName) <= 0)
                {
                    break;
                }

                order[insertion] = order[insertion - 1];
                insertion -= 1;
            }

            order[insertion] = current;
            index += 1;
        }
    }

    internal static int CompareJsonNames(string left, string right)
    {
        int leftPosition = 0;
        int rightPosition = 0;
        while (leftPosition < left.Length && rightPosition < right.Length)
        {
            int leftScalar = GetJsonScalar(left, leftPosition);
            int rightScalar = GetJsonScalar(right, rightPosition);
            if (leftScalar < rightScalar)
            {
                return -1;
            }

            if (leftScalar > rightScalar)
            {
                return 1;
            }

            leftPosition += char.IsHighSurrogate(left[leftPosition]) ? 2 : 1;
            rightPosition += char.IsHighSurrogate(right[rightPosition]) ? 2 : 1;
        }

        if (leftPosition < left.Length)
        {
            return 1;
        }

        if (rightPosition < right.Length)
        {
            return -1;
        }

        return 0;
    }

    internal static int GetJsonScalar(string text, int position)
    {
        char first = text[position];
        if (!char.IsHighSurrogate(first))
        {
            return first;
        }

        char second = text[position + 1];
        return 65536 + ((first - 55296) << 10) + second - 56320;
    }

    internal static TokenResult CheckJsonDepth(int position, int maximumDepth, int depth)
    {
        if (depth >= maximumDepth)
        {
            TokenResult failure = new TokenResult();
            failure.Error = new TokenError(
                "json_depth_exceeded",
                "The JSON value exceeds the configured maximum depth.",
                position);
            return failure;
        }

        return CreateSuccessfulJsonWrite();
    }

    internal static TokenResult CreateInvalidJsonValue(string message, int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError("invalid_json_value", message, position);
        return result;
    }

    internal static TokenResult CreateSuccessfulJsonWrite()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static JsonValueResult CreateJsonReadFailure(TokenError? error)
    {
        JsonValueResult result = new JsonValueResult();
        result.Error = error;
        return result;
    }

    internal static TokenError CreateJsonSizeError(int maximumLength)
    {
        return new TokenError(
            "json_size_exceeded",
            "The JSON value exceeds the configured maximum length.",
            maximumLength);
    }

    internal static void ValidateJsonLimits(int maximumLength, int maximumDepth)
    {
        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }
    }
}
