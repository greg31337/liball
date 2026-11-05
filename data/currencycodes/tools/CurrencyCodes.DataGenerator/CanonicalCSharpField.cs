namespace CurrencyCodes.DataGenerator;

internal sealed class CanonicalCSharpField
{
    internal CanonicalCSharpField(
        string fieldName,
        string typeName,
        bool isConstant,
        CanonicalCSharpValue value)
    {
        FieldName = fieldName;
        TypeName = typeName;
        IsConstant = isConstant;
        Value = value;
    }

    internal string FieldName { get; }

    internal string TypeName { get; }

    internal bool IsConstant { get; }

    internal CanonicalCSharpValue Value { get; }
}
