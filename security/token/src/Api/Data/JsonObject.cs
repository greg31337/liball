using System;
using System.Collections.Generic;
using System.Text.Json;

namespace token;

/// <summary>
/// Represents a JSON object used by token headers, claims, and keys.
/// </summary>
public class JsonObject
{
    /// <summary>
    /// Contains object members in source order.
    /// </summary>
    public JsonMember[] Members;

    /// <summary>
    /// Initializes a JSON object with the supplied members.
    /// </summary>
    /// <param name="members">The members to store.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>members</c> is null.</exception>
    public JsonObject(JsonMember[] members)
    {
        if (members == null)
        {
            throw new ArgumentNullException("members");
        }

        Members = members.AsSpan().ToArray();
    }

    /// <summary>
    /// Creates an empty JSON object.
    /// </summary>
    /// <returns>An empty JSON object.</returns>
    internal static JsonObject CreateEmpty()
    {
        return new JsonObject(Array.Empty<JsonMember>());
    }

    /// <summary>
    /// Reads a strict JSON object with a default maximum nesting depth of 64.
    /// </summary>
    /// <param name="text">The JSON object text.</param>
    /// <returns>A result containing an independent object model, or failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static JsonObjectResult Read(string text)
    {
        return Read(text, 64);
    }

    /// <summary>
    /// Reads a strict JSON object with an explicit maximum nesting depth.
    /// </summary>
    /// <param name="text">The JSON object text.</param>
    /// <param name="maximumDepth">The maximum number of nested objects and arrays.</param>
    /// <returns>A result containing an independent object model, or failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static JsonObjectResult Read(string text, int maximumDepth)
    {
        JsonReadResult readResult = JsonReader.Read(text, maximumDepth);
        return FromReadResult(readResult);
    }

    /// <summary>
    /// Reads strict UTF-8 JSON object bytes with a default maximum nesting depth of 64.
    /// </summary>
    /// <param name="utf8">The UTF-8 JSON object bytes.</param>
    /// <returns>A result containing an independent object model, or failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    internal static JsonObjectResult Read(byte[] utf8)
    {
        return Read(utf8, 64);
    }

    /// <summary>
    /// Reads strict UTF-8 JSON object bytes with an explicit maximum nesting depth.
    /// </summary>
    /// <param name="utf8">The UTF-8 JSON object bytes.</param>
    /// <param name="maximumDepth">The maximum number of nested objects and arrays.</param>
    /// <returns>A result containing an independent object model, or failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static JsonObjectResult Read(byte[] utf8, int maximumDepth)
    {
        JsonReadResult readResult = JsonReader.Read(utf8, maximumDepth);
        return FromReadResult(readResult);
    }

    /// <summary>
    /// Finds a member by its decoded name using ordinal comparison.
    /// </summary>
    /// <param name="name">The member name to find.</param>
    /// <returns>The matching value, or <c>null</c> when no member has the name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    internal JsonValue? Get(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = 0;
        while (index < Members.Length)
        {
            if (string.Equals(Members[index].Name, name, StringComparison.Ordinal))
            {
                return Members[index].Value;
            }

            index += 1;
        }

        return null;
    }

    /// <summary>
    /// Determines whether a member exists using ordinal name comparison.
    /// </summary>
    /// <param name="name">The member name to find.</param>
    /// <returns><c>true</c> when the member exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    internal bool Contains(string name)
    {
        return Get(name) != null;
    }

    internal static JsonObjectResult FromReadResult(JsonReadResult readResult)
    {
        if (!readResult.Success)
        {
            JsonObjectResult failure = new JsonObjectResult();
            failure.Error = readResult.Error;
            return failure;
        }

        using (readResult.Document)
        {
            if ((readResult.Document ?? throw new InvalidOperationException("A required value is null.")).RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                JsonObjectResult failure = new JsonObjectResult();
                failure.Error = new TokenError(
                    "json_root_not_object",
                    "The JSON root value must be an object.",
                    0);
                return failure;
            }

            JsonValue root = CreateValue(readResult.Document.RootElement);
            JsonObjectResult result = new JsonObjectResult();
            result.Object = root.ObjectValue;
            result.Success = true;
            return result;
        }
    }

    internal static JsonValue CreateValue(JsonElement element)
    {
        JsonValue result = new JsonValue();

        switch ((int)element.ValueKind)
        {
            case (int)System.Text.Json.JsonValueKind.Null:
                result.Kind = TokenJsonValueKind.Null;
                break;
            case (int)System.Text.Json.JsonValueKind.False:
                result.Kind = TokenJsonValueKind.Boolean;
                result.BooleanValue = false;
                break;
            case (int)System.Text.Json.JsonValueKind.True:
                result.Kind = TokenJsonValueKind.Boolean;
                result.BooleanValue = true;
                break;
            case (int)System.Text.Json.JsonValueKind.Number:
                result.Kind = TokenJsonValueKind.Number;
                result.NumberText = element.GetRawText();
                break;
            case (int)System.Text.Json.JsonValueKind.String:
                result.Kind = TokenJsonValueKind.String;
                result.StringValue = element.GetString();
                break;
            case (int)System.Text.Json.JsonValueKind.Array:
                result.Kind = TokenJsonValueKind.Array;
                result.ArrayValue = CreateArray(element);
                break;
            case (int)System.Text.Json.JsonValueKind.Object:
                result.Kind = TokenJsonValueKind.Object;
                result.ObjectValue = CreateObject(element);
                break;
            default:
                throw new InvalidOperationException("The JSON value kind is unsupported.");
        }

        return result;
    }

    internal static JsonValue[] CreateArray(JsonElement element)
    {
        List<JsonValue> values = new List<JsonValue>();
        foreach (JsonElement item in element.EnumerateArray())
        {
            values.Add(CreateValue(item));
        }

        return values.ToArray();
    }

    internal static JsonObject CreateObject(JsonElement element)
    {
        List<JsonMember> members = new List<JsonMember>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            members.Add(new JsonMember(property.Name, CreateValue(property.Value)));
        }

        return new JsonObject(members.ToArray());
    }
}
