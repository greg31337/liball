namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCustomPropertySet"/>.</summary>
public interface ISvgCustomPropertySet
{
    /// <summary>Tokenizes and stores or replaces a custom property.</summary>
    /// <param name="name">The case-sensitive name beginning with two hyphens.</param>
    /// <param name="rawText">The exact component-value text.</param>
    /// <param name="maxTokens">The maximum stored token count.</param>
    /// <param name="maxTokenCharacters">The maximum raw token length.</param>
    /// <returns>The stored value or stable failure.</returns>
    SvgCustomPropertyStoreResult Set(string? name, string rawText, int maxTokens, int maxTokenCharacters);
    /// <summary>Returns the stored value for a case-sensitive name.</summary>
    /// <param name="name">The custom-property name.</param>
    /// <returns>The stored value, or <c>null</c>.</returns>
    SvgCustomPropertyValue? Get(string name);
    /// <summary>Removes a custom property when present.</summary>
    /// <param name="name">The custom-property name.</param>
    /// <returns><c>true</c> when a value was removed.</returns>
    bool Remove(string name);
    /// <summary>Returns the stored custom-property count.</summary>
    /// <returns>The number of stored values.</returns>
    int GetCount();
    /// <summary>Returns an independently allocated value-list snapshot.</summary>
    /// <returns>The stored values in insertion order.</returns>
    System.Collections.Generic.List<SvgCustomPropertyValue> GetValues();
}

/// <summary>Defines the public operations exposed by <see cref="SvgCssVariableResolver"/>.</summary>
public interface ISvgCssVariableResolver
{
    /// <summary>Resolves custom properties in one component-value string.</summary>
    /// <param name="properties">The custom-property table.</param>
    /// <param name="value">The component-value text.</param>
    /// <param name="maxDepth">The maximum nested variable depth.</param>
    /// <param name="maxExpansionTokens">The maximum expanded token count.</param>
    /// <param name="maxTokenCharacters">The maximum individual token length.</param>
    /// <returns>Substituted text, dependencies, or a stable failure.</returns>
    SvgCssVariableResolveResult Resolve(SvgCustomPropertySet properties, string value, int maxDepth, int maxExpansionTokens, int maxTokenCharacters);
}
