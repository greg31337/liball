namespace languagecodes;

internal static class LanguageTagResolutionEngine
{
    internal static LanguageLookupResult ResolveTagLanguage(LanguageTag tag)
    {
        if (tag == null)
        {
            return CreateTagLanguageOutcome(
                LanguageLookupStatus.Invalid,
                null);
        }

        string? source = tag.Value == null ? tag.NormalizedValue : tag.Value;
        LanguageValidationResult grammar =
            LanguageTagValidationEngine.ValidateTagWellFormed(source);
        if (!grammar.IsWellFormed || source == null)
        {
            return CreateTagLanguageOutcome(
                LanguageLookupStatus.Invalid,
                source);
        }

        LanguageTag parsed = LanguageTagParser.ParseTag(source);
        if (parsed.IsPrivateUseOnly)
        {
            return CreateTagLanguageOutcome(
                LanguageLookupStatus.Reserved,
                parsed.NormalizedValue);
        }

        LanguageLookupOptions options = new LanguageLookupOptions
        {
            NormalizeCase = true,
            IncludeRetiredCodes = true
        };
        if (parsed.IsGrandfathered)
        {
            return PublicResultFactory.CopyLanguageLookupResult(
                LanguageLookupEngine.LookupCode(
                    parsed.NormalizedValue,
                    LanguageCodeForm.GrandfatheredTag,
                    options));
        }

        if (parsed.IsRedundant)
        {
            LanguageLookupResult complete = LanguageLookupEngine.LookupCode(
                parsed.NormalizedValue,
                LanguageCodeForm.RedundantTag,
                options);
            if (complete.Status == LanguageLookupStatus.Found ||
                complete.Status == LanguageLookupStatus.Ambiguous)
            {
                return PublicResultFactory.CopyLanguageLookupResult(complete);
            }
        }

        if (parsed.Extlangs.Length > 1)
        {
            return CreateTagLanguageOutcome(
                LanguageLookupStatus.Invalid,
                parsed.NormalizedValue);
        }

        string? effective = parsed.Language;
        if (parsed.Extlangs.Length == 1)
        {
            effective = parsed.Extlangs[0];
            if (!SubtagIndex.IsAssigned(LanguageSubtagType.Extlang, effective))
            {
                return CreateTagLanguageOutcome(
                    LanguageLookupStatus.NotFound,
                    effective);
            }

            if (!SubtagIndex.AllowsPrefix(
                LanguageSubtagType.Extlang,
                effective,
                parsed.Language))
            {
                return CreateTagLanguageOutcome(
                    LanguageLookupStatus.Invalid,
                    effective);
            }
        }

        if (effective == null || effective.Length == 0)
        {
            return CreateTagLanguageOutcome(
                LanguageLookupStatus.Invalid,
                effective);
        }

        LanguageLookupResult resolved =
            LanguageLookupEngine.LookupPreferredCode(effective, options);
        if (resolved.Status != LanguageLookupStatus.NotFound)
        {
            return PublicResultFactory.CopyLanguageLookupResult(resolved);
        }

        LanguageLookupResult retired = LanguageLookupEngine.LookupCode(
            effective,
            LanguageCodeForm.RetiredSet3,
            options);
        if (retired.Status == LanguageLookupStatus.Retired)
        {
            return PublicResultFactory.CopyLanguageLookupResult(retired);
        }

        return PublicResultFactory.CopyLanguageLookupResult(resolved);
    }

    internal static LanguageLookupResult CreateTagLanguageOutcome(
        LanguageLookupStatus status,
        string? normalizedQuery)
    {
        return PublicResultFactory.CopyLanguageLookupResult(
            new LanguageLookupResult
            {
                Status = status,
                NormalizedQuery = normalizedQuery
            });
    }
}
