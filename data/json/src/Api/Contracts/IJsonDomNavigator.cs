namespace json;

/// <summary>Reads and mutates canonical JSON array and object values.</summary>
public interface IJsonDomNavigator
{
    /// <summary>Gets an array item by zero-based index.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The selected item.</returns>
    JsonValue GetArrayItem(JsonValue array, int index);

    /// <summary>Gets one object member value using an explicit duplicate-name policy.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="name">The exact decoded member name.</param>
    /// <param name="duplicateKeyPolicy">The duplicate-name selection policy.</param>
    /// <returns>The selected member value.</returns>
    JsonValue GetObjectValue(JsonValue objectValue, string name, JsonDuplicateKeyPolicy duplicateKeyPolicy);

    /// <summary>Gets all object members with an exact decoded name.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="name">The exact decoded member name.</param>
    /// <returns>The matching members in stored order.</returns>
    global::System.Collections.Generic.List<JsonMember> GetObjectMatches(JsonValue objectValue, string name);

    /// <summary>Appends an item to an array.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="value">The item to append.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult AppendArrayItem(JsonValue array, JsonValue value);

    /// <summary>Inserts an item into an array.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="value">The item to insert.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult InsertArrayItem(JsonValue array, int index, JsonValue value);

    /// <summary>Replaces an array item.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <param name="value">The replacement item.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult ReplaceArrayItem(JsonValue array, int index, JsonValue value);

    /// <summary>Removes an array item.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult RemoveArrayItem(JsonValue array, int index);

    /// <summary>Removes every item from an array.</summary>
    /// <param name="array">The array value.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult ClearArrayItems(JsonValue array);

    /// <summary>Appends a member to an object.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="member">The member to append.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult AppendObjectMember(JsonValue objectValue, JsonMember member);

    /// <summary>Inserts a member into an object.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="member">The member to insert.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult InsertObjectMember(JsonValue objectValue, int index, JsonMember member);

    /// <summary>Replaces an object member by stored index.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="index">The zero-based member index.</param>
    /// <param name="member">The replacement member.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult ReplaceObjectMember(JsonValue objectValue, int index, JsonMember member);

    /// <summary>Removes an object member by stored index.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="index">The zero-based member index.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult RemoveObjectMember(JsonValue objectValue, int index);

    /// <summary>Removes every member from an object.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <returns>The mutation outcome.</returns>
    JsonOperationResult ClearObjectMembers(JsonValue objectValue);
}
