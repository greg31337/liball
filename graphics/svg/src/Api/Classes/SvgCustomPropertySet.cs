namespace svg;

/// <summary>Stores case-sensitive custom-property token streams.</summary>
public class SvgCustomPropertySet : ISvgCustomPropertySet
{
    System.Collections.Generic.List<SvgCustomPropertyValue> values;

    /// <summary>Creates an empty custom-property set.</summary>
    public SvgCustomPropertySet()
    {
        values = new System.Collections.Generic.List<SvgCustomPropertyValue>();
    }

    /// <summary>Tokenizes and stores or replaces a custom property.</summary>
    /// <param name="name">The case-sensitive name beginning with two hyphens.</param>
    /// <param name="rawText">The exact component-value text.</param>
    /// <param name="maxTokens">The maximum stored token count.</param>
    /// <param name="maxTokenCharacters">The maximum raw token length.</param>
    /// <returns>The stored value or stable failure.</returns>
    public SvgCustomPropertyStoreResult Set(string? name, string rawText, int maxTokens, int maxTokenCharacters)
    {
        if (!SvgCssVariableRules.IsCustomName(name)) return new SvgCustomPropertyStoreResult(false, name == null ? SvgCssVariableError.InvalidArgument : SvgCssVariableError.InvalidName, null, 0);
        SvgCustomPropertyStoreResult tokenized = SvgCssVariableTokenizer.Tokenize((name ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), rawText, maxTokens, maxTokenCharacters);
        if (!tokenized.Success) return tokenized;
        int index = Find(name);
        if (index < 0) values.Add((tokenized.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        else values[index] = (tokenized.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        return tokenized;
    }

    /// <summary>Returns the stored value for a case-sensitive name.</summary>
    /// <param name="name">The custom-property name.</param>
    /// <returns>The stored value, or <c>null</c>.</returns>
    public SvgCustomPropertyValue? Get(string name)
    {
        int index = Find(name);
        return index < 0 ? null : values[index];
    }

    /// <summary>Removes a custom property when present.</summary>
    /// <param name="name">The custom-property name.</param>
    /// <returns><c>true</c> when a value was removed.</returns>
    public bool Remove(string name)
    {
        int index = Find(name);
        if (index < 0) return false;
        values.RemoveAt(index);
        return true;
    }

    /// <summary>Returns the stored custom-property count.</summary>
    /// <returns>The number of stored values.</returns>
    public int GetCount()
    {
        return values.Count;
    }

    /// <summary>Returns an independently allocated value-list snapshot.</summary>
    /// <returns>The stored values in insertion order.</returns>
    public System.Collections.Generic.List<SvgCustomPropertyValue> GetValues()
    {
        return new System.Collections.Generic.List<SvgCustomPropertyValue>(values);
    }

    int Find(string name)
    {
        if (name == null) return -1;
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Name == name) return index;
            index = index + 1;
        }
        return -1;
    }
}
