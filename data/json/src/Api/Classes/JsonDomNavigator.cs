namespace json;

/// <summary>Reads and mutates ordered canonical JSON arrays and objects.</summary>
public class JsonDomNavigator : IJsonDomNavigator
{
    /// <summary>Initializes a reusable JSON DOM navigator.</summary>
    public JsonDomNavigator()
    {
    }

    /// <summary>Gets an array item by zero-based index.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The selected item.</returns>
    public JsonValue GetArrayItem(JsonValue array, int index)
    {
        return JsonDomOperations.GetArrayItem(array, index);
    }

    /// <summary>Gets one object member value using an explicit duplicate-name policy.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="name">The exact decoded member name.</param>
    /// <param name="duplicateKeyPolicy">The duplicate-name selection policy.</param>
    /// <returns>The selected member value.</returns>
    public JsonValue GetObjectValue(
        JsonValue objectValue,
        string name,
        JsonDuplicateKeyPolicy duplicateKeyPolicy)
    {
        return JsonDomOperations.GetObjectValue(objectValue, name, duplicateKeyPolicy);
    }

    /// <summary>Gets all object members with an exact decoded name.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="name">The exact decoded member name.</param>
    /// <returns>The matching members in stored order.</returns>
    public global::System.Collections.Generic.List<JsonMember> GetObjectMatches(
        JsonValue objectValue,
        string name)
    {
        return JsonDomOperations.GetObjectMatches(objectValue, name);
    }

    /// <summary>Appends an item to an array.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="value">The item to append.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult AppendArrayItem(JsonValue array, JsonValue value)
    {
        return JsonDomOperations.AppendArrayItem(array, value);
    }

    /// <summary>Inserts an item into an array.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="value">The item to insert.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult InsertArrayItem(JsonValue array, int index, JsonValue value)
    {
        return JsonDomOperations.InsertArrayItem(array, index, value);
    }

    /// <summary>Replaces an array item.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <param name="value">The replacement item.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult ReplaceArrayItem(JsonValue array, int index, JsonValue value)
    {
        return JsonDomOperations.ReplaceArrayItem(array, index, value);
    }

    /// <summary>Removes an array item.</summary>
    /// <param name="array">The array value.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult RemoveArrayItem(JsonValue array, int index)
    {
        return JsonDomOperations.RemoveArrayItem(array, index);
    }

    /// <summary>Removes every item from an array.</summary>
    /// <param name="array">The array value.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult ClearArrayItems(JsonValue array)
    {
        return JsonDomOperations.ClearArrayItems(array);
    }

    /// <summary>Appends a member to an object.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="member">The member to append.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult AppendObjectMember(JsonValue objectValue, JsonMember member)
    {
        return JsonDomOperations.AppendObjectMember(objectValue, member);
    }

    /// <summary>Inserts a member into an object.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="member">The member to insert.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult InsertObjectMember(
        JsonValue objectValue,
        int index,
        JsonMember member)
    {
        return JsonDomOperations.InsertObjectMember(objectValue, index, member);
    }

    /// <summary>Replaces an object member by stored index.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="index">The zero-based member index.</param>
    /// <param name="member">The replacement member.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult ReplaceObjectMember(
        JsonValue objectValue,
        int index,
        JsonMember member)
    {
        return JsonDomOperations.ReplaceObjectMember(objectValue, index, member);
    }

    /// <summary>Removes an object member by stored index.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <param name="index">The zero-based member index.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult RemoveObjectMember(JsonValue objectValue, int index)
    {
        return JsonDomOperations.RemoveObjectMember(objectValue, index);
    }

    /// <summary>Removes every member from an object.</summary>
    /// <param name="objectValue">The object value.</param>
    /// <returns>The mutation outcome.</returns>
    public JsonOperationResult ClearObjectMembers(JsonValue objectValue)
    {
        return JsonDomOperations.ClearObjectMembers(objectValue);
    }
}
