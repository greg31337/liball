using System;
using System.Collections.Generic;
using System.Text.Json;

namespace token;

/// <summary>
/// Represents one JSON value in the token object model.
/// </summary>
public class JsonValue
{
    /// <summary>
    /// Identifies which value field is active.
    /// </summary>
    public TokenJsonValueKind Kind;

    /// <summary>
    /// Contains the boolean value when <see cref="Kind"/> is <see cref="TokenJsonValueKind.Boolean"/>.
    /// </summary>
    public bool BooleanValue;

    /// <summary>
    /// Contains the exact JSON number text when <see cref="Kind"/> is <see cref="TokenJsonValueKind.Number"/>.
    /// </summary>
    public string? NumberText;

    /// <summary>
    /// Contains the decoded string when <see cref="Kind"/> is <see cref="TokenJsonValueKind.String"/>.
    /// </summary>
    public string? StringValue;

    /// <summary>
    /// Contains the array items when <see cref="Kind"/> is <see cref="TokenJsonValueKind.Array"/>.
    /// </summary>
    public JsonValue[]? ArrayValue;

    /// <summary>
    /// Contains the object when <see cref="Kind"/> is <see cref="TokenJsonValueKind.Object"/>.
    /// </summary>
    public JsonObject? ObjectValue;

    /// <summary>
    /// Initializes a JSON null value.
    /// </summary>
    public JsonValue()
    {
        Kind = TokenJsonValueKind.Null;
        BooleanValue = false;
        NumberText = null;
        StringValue = null;
        ArrayValue = null;
        ObjectValue = null;
    }
}
