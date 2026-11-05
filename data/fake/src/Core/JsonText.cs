using System.Text.Json;

namespace fake;

internal static class JsonText
{
    internal static string Required(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw FakeException.Create($"JSON value '{path}' must be a string.");
        string? result = value.GetString();
        if (result == null)
            throw FakeException.Create($"JSON value '{path}' must be a string.");
        return result;
    }

    internal static string? Optional(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Null)
            return null;
        return Required(value, path);
    }
}
