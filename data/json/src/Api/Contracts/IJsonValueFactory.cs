namespace json;

/// <summary>Creates and clones canonical JSON model values.</summary>
public interface IJsonValueFactory
{
    /// <summary>Creates the JSON null value.</summary>
    /// <returns>The created value.</returns>
    JsonValue CreateNull();

    /// <summary>Creates a JSON boolean value.</summary>
    /// <param name="value">The boolean payload.</param>
    /// <returns>The created value.</returns>
    JsonValue CreateBoolean(bool value);

    /// <summary>Creates a JSON number after validating its exact lexeme.</summary>
    /// <param name="lexeme">The exact decimal lexeme.</param>
    /// <returns>The created value.</returns>
    JsonValue CreateNumber(string lexeme);

    /// <summary>Attempts to create a JSON number without throwing for an invalid lexeme.</summary>
    /// <param name="lexeme">The exact decimal lexeme.</param>
    /// <returns>The creation outcome.</returns>
    JsonValueResult TryCreateNumber(string lexeme);

    /// <summary>Creates a JSON string value.</summary>
    /// <param name="value">The decoded string payload.</param>
    /// <returns>The created value.</returns>
    JsonValue CreateString(string value);

    /// <summary>Creates a JSON array from ordered items.</summary>
    /// <param name="items">The ordered array items.</param>
    /// <returns>The created value.</returns>
    JsonValue CreateArray(global::System.Collections.Generic.List<JsonValue> items);

    /// <summary>Creates a JSON object from ordered, duplicate-preserving members.</summary>
    /// <param name="members">The ordered object members.</param>
    /// <returns>The created value.</returns>
    JsonValue CreateObject(global::System.Collections.Generic.List<JsonMember> members);

    /// <summary>Creates an iterative deep clone of a JSON value.</summary>
    /// <param name="value">The value to clone.</param>
    /// <returns>The independent clone.</returns>
    JsonValue CloneValue(JsonValue value);
}
