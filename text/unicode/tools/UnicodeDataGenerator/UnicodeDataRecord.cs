namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeDataRecord
{
    public int CodePoint;
    public string Name;
    public string GeneralCategory;
    public int CombiningClass;
    public string BidiClass;
    public bool BidiMirrored;
    public string DecompositionType;
    public int[] DecompositionMapping;
    public int DecimalDigitValue;
    public int DigitValue;
    public UnicodeNumericValue NumericValue;
    public int SimpleUppercaseMapping;
    public int SimpleLowercaseMapping;
    public int SimpleTitlecaseMapping;

    public UnicodeDataRecord(
        int codePoint,
        string name,
        string generalCategory,
        int combiningClass,
        string bidiClass,
        bool bidiMirrored,
        string decompositionType,
        int[] decompositionMapping,
        int decimalDigitValue,
        int digitValue,
        UnicodeNumericValue numericValue,
        int simpleUppercaseMapping,
        int simpleLowercaseMapping,
        int simpleTitlecaseMapping)
    {
        CodePoint = codePoint;
        Name = name;
        GeneralCategory = generalCategory;
        CombiningClass = combiningClass;
        BidiClass = bidiClass;
        BidiMirrored = bidiMirrored;
        DecompositionType = decompositionType;
        DecompositionMapping = decompositionMapping;
        DecimalDigitValue = decimalDigitValue;
        DigitValue = digitValue;
        NumericValue = numericValue;
        SimpleUppercaseMapping = simpleUppercaseMapping;
        SimpleLowercaseMapping = simpleLowercaseMapping;
        SimpleTitlecaseMapping = simpleTitlecaseMapping;
    }
}
