namespace json;

/// <summary>Evaluates decoded JSON Pointers against canonical JSON values.</summary>
public class JsonPointerNavigator : IJsonPointerNavigator
{
    /// <summary>Initializes a reusable JSON Pointer navigator.</summary>
    public JsonPointerNavigator()
    {
    }

    /// <summary>Resolves one pointer using an explicit duplicate-name policy.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="pointer">The decoded pointer.</param>
    /// <param name="duplicateKeyPolicy">The duplicate-name selection policy.</param>
    /// <returns>The resolved value.</returns>
    public JsonValue ResolvePointer(
        JsonValue root,
        JsonPointer pointer,
        JsonDuplicateKeyPolicy duplicateKeyPolicy)
    {
        return JsonPointerEvaluator.ResolveSingleValue(root, pointer, duplicateKeyPolicy);
    }

    /// <summary>Attempts to resolve one pointer without throwing for expected evaluation failures.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="pointer">The decoded pointer.</param>
    /// <param name="duplicateKeyPolicy">The duplicate-name selection policy.</param>
    /// <returns>The pointer evaluation outcome.</returns>
    public JsonPointerResult TryResolvePointer(
        JsonValue root,
        JsonPointer pointer,
        JsonDuplicateKeyPolicy duplicateKeyPolicy)
    {
        return JsonPointerEvaluator.TryResolveSingleValue(
            root,
            pointer,
            duplicateKeyPolicy);
    }

    /// <summary>Resolves all duplicate object-member matches selected by a pointer.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="pointer">The decoded pointer.</param>
    /// <returns>The all-match pointer evaluation outcome.</returns>
    public JsonPointerResult ResolveAllPointerMatches(JsonValue root, JsonPointer pointer)
    {
        return JsonPointerEvaluator.ResolveAllValues(root, pointer);
    }
}
