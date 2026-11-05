namespace CurrencyCodes.DataGenerator;

internal sealed class SemanticDiffEntry
{
    internal SemanticDiffEntry(
        string category,
        string key,
        string field,
        string oldValue,
        string newValue)
    {
        Category = category;
        Key = key;
        Field = field;
        OldValue = oldValue;
        NewValue = newValue;
    }

    internal string Category { get; }

    internal string Key { get; }

    internal string Field { get; }

    internal string OldValue { get; }

    internal string NewValue { get; }
}
