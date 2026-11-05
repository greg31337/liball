namespace languagecodes;

internal static class LanguageTagMetadataEngine
{
    internal static LanguageSubtagMetadata? GetLanguageSubtagMetadata(
        LanguageSubtagType type,
        string? value)
    {
        IanaSubtagRow? record = SubtagIndex.FindRecordOrRange(type, value);
        if (record == null)
        {
            return null;
        }

        return PublicResultFactory.CreateLanguageSubtagMetadata(record);
    }
}
