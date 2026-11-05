using System;
namespace token;

internal static class JwtAuthorizationProfileHelpers
{
    internal static string[] MergeClaims(string[]? configured, string[] required)
    {
        string[] merged = new string[(configured ?? throw new InvalidOperationException("A required value is null.")).Length + required.Length];
        int count = 0;
        int index = 0;
        while (index < configured.Length)
        {
            merged[count] = configured[index];
            count += 1;
            index += 1;
        }

        index = 0;
        while (index < required.Length)
        {
            string name = required[index];
            if (!Contains(merged, count, name))
            {
                merged[count] = name;
                count += 1;
            }

            index += 1;
        }

        string[] result = new string[count];
        index = 0;
        while (index < count)
        {
            result[index] = merged[index];
            index += 1;
        }

        return result;
    }

    internal static bool ValidString(JsonValue? value)
    {
        return value != null
            && value.Kind == TokenJsonValueKind.String
            && value.StringValue != null
            && value.StringValue.Length > 0
            && Utf8Codec.Encode(value.StringValue).Success;
    }

    internal static bool EqualText(string? left, string? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        TokenBytesResult leftBytes = Utf8Codec.Encode(left);
        TokenBytesResult rightBytes = Utf8Codec.Encode(right);
        return leftBytes.Success
            && rightBytes.Success
            && ConstantTime.AreEqual((leftBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")), (rightBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")));
    }

    internal static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static bool Contains(string[] values, int count, string expected)
    {
        int index = 0;
        while (index < count)
        {
            if (string.Equals(
                values[index],
                expected,
                StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
