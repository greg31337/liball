namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeDecompositionRecord
{
    public int CodePoint;
    public string DecompositionType;
    public int[] Mapping;
    public bool IsCompatibility;

    public UnicodeDecompositionRecord(int codePoint, string decompositionType, int[] mapping)
    {
        ArgumentNullException.ThrowIfNull(decompositionType);
        ArgumentNullException.ThrowIfNull(mapping);

        CodePoint = codePoint;
        DecompositionType = decompositionType;
        Mapping = mapping;
        IsCompatibility = decompositionType.Length > 0;
    }
}
