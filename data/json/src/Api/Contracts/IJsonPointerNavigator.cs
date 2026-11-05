namespace json;

/// <summary>Evaluates decoded JSON Pointers against JSON values.</summary>
public interface IJsonPointerNavigator
{
    /// <summary>Resolves one pointer using an explicit duplicate-name policy.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="pointer">The decoded pointer.</param>
    /// <param name="duplicateKeyPolicy">The duplicate-name selection policy.</param>
    /// <returns>The resolved value.</returns>
    JsonValue ResolvePointer(JsonValue root, JsonPointer pointer, JsonDuplicateKeyPolicy duplicateKeyPolicy);

    /// <summary>Attempts to resolve one pointer without throwing for expected evaluation failures.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="pointer">The decoded pointer.</param>
    /// <param name="duplicateKeyPolicy">The duplicate-name selection policy.</param>
    /// <returns>The pointer evaluation outcome.</returns>
    JsonPointerResult TryResolvePointer(JsonValue root, JsonPointer pointer, JsonDuplicateKeyPolicy duplicateKeyPolicy);

    /// <summary>Resolves all duplicate object-member matches selected by a pointer.</summary>
    /// <param name="root">The root value.</param>
    /// <param name="pointer">The decoded pointer.</param>
    /// <returns>The all-match pointer evaluation outcome.</returns>
    JsonPointerResult ResolveAllPointerMatches(JsonValue root, JsonPointer pointer);
}
