using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace token;

internal class JwtClaimsWriteState
{
    internal StringBuilder Builder;
    internal TokenError? Error;
    internal int MaximumDepth;

    internal JwtClaimsWriteState(int maximumDepth)
    {
        Builder = new StringBuilder();
        Error = null;
        MaximumDepth = maximumDepth;
    }
}

internal static class JwtClaimsWriter
{
    internal static TokenTextResult Write(JsonObject values, int maximumDepth)
    {
        JwtClaimsWriteState state = new JwtClaimsWriteState(maximumDepth);
        AppendObject(state, values, 1);
        if (state.Error != null)
        {
            TokenTextResult failure = new TokenTextResult();
            failure.Error = state.Error;
            return failure;
        }

        TokenTextResult result = new TokenTextResult();
        result.Text = state.Builder.ToString();
        result.Success = true;
        return result;
    }

    static void AppendValue(JwtClaimsWriteState state, JsonValue value, int depth)
    {
        if (state.Error != null)
        {
            return;
        }

        if (value == null)
        {
            SetError(state, "invalid_claim_set", "A JWT claim JSON value is missing.");
            return;
        }

        switch (value.Kind)
        {
            case TokenJsonValueKind.Null:
                state.Builder.Append("null");
                break;
            case TokenJsonValueKind.Boolean:
                state.Builder.Append(value.BooleanValue ? "true" : "false");
                break;
            case TokenJsonValueKind.Number:
                AppendNumber(state, value.NumberText);
                break;
            case TokenJsonValueKind.String:
                AppendString(state, value.StringValue);
                break;
            case TokenJsonValueKind.Array:
                AppendArray(state, value.ArrayValue, depth + 1);
                break;
            case TokenJsonValueKind.Object:
                AppendObject(state, value.ObjectValue, depth + 1);
                break;
            default:
                SetError(state, "invalid_claim_set", "A JWT claim JSON value kind is invalid.");
                break;
        }
    }

    static void AppendObject(JwtClaimsWriteState state, JsonObject? value, int depth)
    {
        if (!CheckDepth(state, depth) || state.Error != null)
        {
            return;
        }

        if (value == null || value.Members == null)
        {
            SetError(state, "invalid_claim_set", "A JWT claim JSON object is incomplete.");
            return;
        }

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        state.Builder.Append('{');
        int index = 0;
        while (index < value.Members.Length)
        {
            JsonMember member = value.Members[index];
            if (member == null || member.Name == null || member.Value == null)
            {
                SetError(state, "invalid_claim_set", "A JWT claim JSON member is incomplete.");
                return;
            }

            if (!names.Add(member.Name))
            {
                SetError(
                    state,
                    "duplicate_json_member",
                    "A JWT claim JSON object contains a duplicate member name.");
                return;
            }

            if (index > 0)
            {
                state.Builder.Append(',');
            }

            AppendString(state, member.Name);
            if (state.Error != null)
            {
                return;
            }

            state.Builder.Append(':');
            AppendValue(state, member.Value, depth);
            if (state.Error != null)
            {
                return;
            }

            index += 1;
        }

        state.Builder.Append('}');
    }

    static void AppendArray(JwtClaimsWriteState state, JsonValue[]? values, int depth)
    {
        if (!CheckDepth(state, depth) || state.Error != null)
        {
            return;
        }

        if (values == null)
        {
            SetError(state, "invalid_claim_set", "A JWT claim JSON array is missing.");
            return;
        }

        state.Builder.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                state.Builder.Append(',');
            }

            AppendValue(state, values[index], depth);
            if (state.Error != null)
            {
                return;
            }

            index += 1;
        }

        state.Builder.Append(']');
    }

    static void AppendString(JwtClaimsWriteState state, string? value)
    {
        if (value == null)
        {
            SetError(state, "invalid_claim_set", "A JWT claim JSON string is missing.");
            return;
        }

        TokenTextResult encoded = JsonWriter.WriteStringValue(value);
        if (!encoded.Success)
        {
            state.Error = encoded.Error;
            return;
        }

        state.Builder.Append(encoded.Text);
    }

    static void AppendNumber(JwtClaimsWriteState state, string? value)
    {
        if (value == null)
        {
            SetError(state, "invalid_json_number", "A JWT claim number is missing.");
            return;
        }

        JsonReadResult parsed = JsonReader.Read(value, 1);
        if (!parsed.Success)
        {
            SetError(state, "invalid_json_number", "A JWT claim number is invalid.");
            return;
        }

        string raw = (parsed.Document ?? throw new InvalidOperationException("A required value is null.")).RootElement.GetRawText();
        bool valid = parsed.Document.RootElement.ValueKind == JsonValueKind.Number
            && string.Equals(raw, value, StringComparison.Ordinal);
        parsed.Document.Dispose();
        if (!valid)
        {
            SetError(state, "invalid_json_number", "A JWT claim number is invalid.");
            return;
        }

        state.Builder.Append(value);
    }

    static bool CheckDepth(JwtClaimsWriteState state, int depth)
    {
        if (depth <= state.MaximumDepth)
        {
            return true;
        }

        SetError(
            state,
            "json_depth_exceeded",
            "The JWT claim set exceeds the maximum JSON nesting depth.");
        return false;
    }

    static void SetError(JwtClaimsWriteState state, string code, string message)
    {
        if (state.Error == null)
        {
            state.Error = new TokenError(code, message, -1);
        }
    }
}
