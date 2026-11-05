namespace languagecodes;

internal class LookupOperationRow
{
    internal LanguageLookupOptions Options;
    internal LanguageLookupResult Result;

    internal LookupOperationRow(
        LanguageLookupOptions options,
        LanguageLookupResult result)
    {
        Options = options;
        Result = result;
    }
}

internal class NormalizationOperationRow
{
    internal LanguageNormalizationOptions Options;
    internal LanguageNormalizationResult Result;

    internal NormalizationOperationRow(
        LanguageNormalizationOptions options,
        LanguageNormalizationResult result)
    {
        Options = options;
        Result = result;
    }
}

internal class ValidationOperationRow
{
    internal LanguageValidationOptions Options;
    internal LanguageValidationResult Result;

    internal ValidationOperationRow(
        LanguageValidationOptions options,
        LanguageValidationResult result)
    {
        Options = options;
        Result = result;
    }
}

internal class TagMatchOperationRow
{
    internal LanguageTag Tag;
    internal LanguageSubtagMetadata Metadata;
    internal LanguageTagMatchResult Result;

    internal TagMatchOperationRow(
        LanguageTag tag,
        LanguageSubtagMetadata metadata,
        LanguageTagMatchResult result)
    {
        Tag = tag;
        Metadata = metadata;
        Result = result;
    }
}
