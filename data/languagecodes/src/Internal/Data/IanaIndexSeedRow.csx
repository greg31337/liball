namespace languagecodes;

internal class IanaIndexSeedRow
{
    internal LanguageSubtagType RecordType;
    internal string Value;
    internal int RecordIndex;

    internal IanaIndexSeedRow(LanguageSubtagType recordType, string value, int recordIndex)
    {
        RecordType = recordType;
        Value = value;
        RecordIndex = recordIndex;
    }
}
