namespace languagecodes;

/// <summary>Provides catalog, normalization, validation, and language-tag operations.</summary>
public class LanguageCodeService :
    ILanguageCatalog,
    ILanguageNormalizer,
    ILanguageValidator,
    ILanguageTagService
{
    /// <summary>Creates a stateless language-code service.</summary>
    public LanguageCodeService()
    {
    }

    /// <summary>Returns every active reconciled ISO language entity.</summary>
    /// <returns>A stable defensive snapshot of active entries.</returns>
    public LanguageCodeEntry[] GetAllEntries()
    {
        return LanguageCatalogEngine.GetAllEntries();
    }

    /// <summary>Returns entries belonging to an ISO 639 set.</summary>
    /// <param name="set">The required ISO 639 set.</param>
    /// <returns>A stable defensive snapshot of matching entries.</returns>
    public LanguageCodeEntry[] GetEntriesBySet(Iso639Set set)
    {
        return LanguageCatalogEngine.GetEntriesBySet(set);
    }

    /// <summary>Returns entries having one language scope.</summary>
    /// <param name="scope">The required language scope.</param>
    /// <returns>A stable defensive snapshot of matching entries.</returns>
    public LanguageCodeEntry[] GetEntriesByScope(LanguageScope scope)
    {
        return LanguageCatalogEngine.GetEntriesByScope(scope);
    }

    /// <summary>Returns entries having one language type.</summary>
    /// <param name="languageType">The required language type.</param>
    /// <returns>A stable defensive snapshot of matching entries.</returns>
    public LanguageCodeEntry[] GetEntriesByType(LanguageType languageType)
    {
        return LanguageCatalogEngine.GetEntriesByType(languageType);
    }

    /// <summary>Looks up a language code using caller-selected policies.</summary>
    /// <param name="code">The code to resolve.</param>
    /// <param name="form">The code form to require.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>The typed lookup outcome.</returns>
    public LanguageLookupResult LookupCode(
        string? code,
        LanguageCodeForm form,
        LanguageLookupOptions options)
    {
        return LanguageLookupEngine.LookupCode(code, form, options);
    }

    /// <summary>Requires one language entity for a code.</summary>
    /// <param name="code">The code to resolve.</param>
    /// <param name="form">The code form to require.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>The uniquely resolved language entry.</returns>
    /// <exception cref="InvalidLanguageCodeException">The code is invalid or disallowed.</exception>
    /// <exception cref="LanguageCodeNotFoundException">No assigned entity resolves from the code.</exception>
    public LanguageCodeEntry RequireCode(
        string? code,
        LanguageCodeForm form,
        LanguageLookupOptions options)
    {
        return LanguageLookupEngine.RequireCode(code, form, options);
    }

    /// <summary>Looks up an exact or normalized language name.</summary>
    /// <param name="name">The language name to resolve.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>A result retaining all candidates when the name is ambiguous.</returns>
    public LanguageLookupResult LookupName(
        string? name,
        LanguageLookupOptions options)
    {
        return LanguageLookupEngine.LookupName(name, options);
    }

    /// <summary>Searches language names using caller-selected policies.</summary>
    /// <param name="query">The name query.</param>
    /// <param name="options">The search policies.</param>
    /// <returns>Deterministically ranked matching candidates.</returns>
    public LanguageMatch[] SearchNames(
        string? query,
        LanguageLookupOptions options)
    {
        return LanguageLookupEngine.SearchNames(query, options);
    }

    /// <summary>Resolves a code, alias, retired code, or language name.</summary>
    /// <param name="value">The code or name to resolve.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>The typed resolution outcome.</returns>
    public LanguageLookupResult ResolveValue(
        string? value,
        LanguageLookupOptions options)
    {
        return LanguageLookupEngine.ResolveValue(value, options);
    }

    /// <summary>Requires one entity from a code, alias, retired code, or name.</summary>
    /// <param name="value">The code or name to resolve.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>The uniquely resolved language entry.</returns>
    /// <exception cref="InvalidLanguageCodeException">A code-shaped value is invalid or disallowed.</exception>
    /// <exception cref="LanguageCodeNotFoundException">No entity resolves from the value.</exception>
    /// <exception cref="AmbiguousLanguageNameException">The value resolves ambiguously.</exception>
    public LanguageCodeEntry RequireValue(
        string? value,
        LanguageLookupOptions options)
    {
        return LanguageLookupEngine.RequireValue(value, options);
    }

    /// <summary>Returns the direct macrolanguage for an individual member.</summary>
    /// <param name="memberCode">The member language code.</param>
    /// <param name="includeRetiredRelationships">Whether retired mappings participate.</param>
    /// <returns>The macrolanguage entry, or null when no mapping exists.</returns>
    public LanguageCodeEntry? GetMacrolanguageForMember(
        string? memberCode,
        bool includeRetiredRelationships)
    {
        return LanguageRelationshipEngine.GetMacrolanguageForMember(
            memberCode,
            includeRetiredRelationships);
    }

    /// <summary>Returns every direct member of a macrolanguage.</summary>
    /// <param name="macrolanguageCode">The macrolanguage code.</param>
    /// <param name="includeRetiredRelationships">Whether retired mappings participate.</param>
    /// <returns>A stable defensive snapshot of member entries.</returns>
    public LanguageCodeEntry[] GetMacrolanguageMembers(
        string? macrolanguageCode,
        bool includeRetiredRelationships)
    {
        return LanguageRelationshipEngine.GetMacrolanguageMembers(
            macrolanguageCode,
            includeRetiredRelationships);
    }

    /// <summary>Returns direct or transitive ISO 639 Set 5 parents.</summary>
    /// <param name="groupCode">The child group code.</param>
    /// <param name="transitive">Whether all ancestors are returned.</param>
    /// <returns>A stable defensive snapshot of parent entries.</returns>
    public LanguageCodeEntry[] GetGroupParents(string? groupCode, bool transitive)
    {
        return LanguageRelationshipEngine.GetGroupParents(groupCode, transitive);
    }

    /// <summary>Returns direct or transitive ISO 639 Set 5 children.</summary>
    /// <param name="groupCode">The parent group code.</param>
    /// <param name="transitive">Whether all descendants are returned.</param>
    /// <returns>A stable defensive snapshot of child entries.</returns>
    public LanguageCodeEntry[] GetGroupChildren(string? groupCode, bool transitive)
    {
        return LanguageRelationshipEngine.GetGroupChildren(groupCode, transitive);
    }

    /// <summary>Returns authoritative retirement metadata for an ISO 639 Set 3 code.</summary>
    /// <param name="code">The retired code to query.</param>
    /// <returns>A defensive retirement snapshot, or null when none exists.</returns>
    public LanguageRetirement? GetRetirement(string? code)
    {
        return LanguageRetirementEngine.GetRetirement(code);
    }

    /// <summary>Normalizes an ISO language code using caller-selected policies.</summary>
    /// <param name="code">The language code to normalize.</param>
    /// <param name="options">The normalization policies.</param>
    /// <returns>The normalization outcome and audit trail.</returns>
    public LanguageNormalizationResult NormalizeCode(
        string? code,
        LanguageNormalizationOptions options)
    {
        return LanguageNormalizationEngine.NormalizeCode(code, options);
    }

    /// <summary>Converts an ISO language code to the requested code form.</summary>
    /// <param name="code">The language code to convert.</param>
    /// <param name="options">The conversion policies.</param>
    /// <returns>The conversion outcome and audit trail.</returns>
    public LanguageNormalizationResult ConvertCode(
        string? code,
        LanguageNormalizationOptions options)
    {
        return LanguageNormalizationEngine.ConvertCode(code, options);
    }

    /// <summary>Normalizes and canonicalizes a BCP 47 language tag.</summary>
    /// <param name="tag">The language tag to normalize.</param>
    /// <param name="options">The language-tag normalization policies.</param>
    /// <returns>The normalization outcome and audit trail.</returns>
    public LanguageNormalizationResult NormalizeTag(
        string? tag,
        LanguageNormalizationOptions options)
    {
        return LanguageNormalizationEngine.NormalizeTag(tag, options);
    }

    /// <summary>Validates a language code against assignment and caller policy.</summary>
    /// <param name="code">The language code to validate.</param>
    /// <param name="options">The validation policies.</param>
    /// <returns>Detailed validation outcome data.</returns>
    public LanguageValidationResult ValidateCode(
        string? code,
        LanguageValidationOptions options)
    {
        return LanguageValidationEngine.ValidateCode(code, options);
    }

    /// <summary>Checks a character span for lexical validity and assignment.</summary>
    /// <param name="code">The language-code characters to check.</param>
    /// <param name="options">The validation policies.</param>
    /// <returns>True when the span is assigned and accepted; otherwise false.</returns>
    public bool ValidateCodeSpan(
        global::System.ReadOnlySpan<char> code,
        LanguageValidationOptions options)
    {
        return LanguageValidationEngine.ValidateCodeSpan(code, options);
    }

    /// <summary>Checks whether a code is assigned in the requested form.</summary>
    /// <param name="code">The language code to check.</param>
    /// <param name="form">The required or automatically recognized form.</param>
    /// <returns>True when the code resolves to an assigned entity; otherwise false.</returns>
    public bool IsAssignedCode(string? code, LanguageCodeForm form)
    {
        return LanguageValidationEngine.IsAssignedCode(code, form);
    }

    /// <summary>Validates an RFC 5646 language tag against assignment and policy.</summary>
    /// <param name="tag">The language tag to validate.</param>
    /// <param name="options">The validation policies.</param>
    /// <returns>Detailed validation outcome data.</returns>
    public LanguageValidationResult ValidateTag(
        string? tag,
        LanguageValidationOptions options)
    {
        return LanguageValidationEngine.ValidateTag(tag, options);
    }

    /// <summary>Parses every component of a well-formed RFC 5646 language tag.</summary>
    /// <param name="tag">The language tag to parse.</param>
    /// <returns>A defensive parsed-tag snapshot.</returns>
    /// <exception cref="InvalidLanguageTagException">The value is not well formed.</exception>
    public LanguageTag ParseTag(string? tag)
    {
        return LanguageTagParser.ParseTag(tag);
    }

    /// <summary>Resolves the effective language component of a parsed tag.</summary>
    /// <param name="tag">The parsed language tag.</param>
    /// <returns>The typed language-resolution outcome.</returns>
    public LanguageLookupResult ResolveTagLanguage(LanguageTag tag)
    {
        return LanguageLookupEngine.ResolveTagLanguage(tag);
    }

    /// <summary>Returns IANA metadata for a language-related subtag or complete tag.</summary>
    /// <param name="type">The registry record type.</param>
    /// <param name="value">The subtag or complete tag to query.</param>
    /// <returns>A defensive metadata snapshot, or null when none exists.</returns>
    public LanguageSubtagMetadata? GetLanguageSubtagMetadata(
        LanguageSubtagType type,
        string? value)
    {
        return LanguageLookupEngine.GetLanguageSubtagMetadata(type, value);
    }

    /// <summary>Applies RFC 4647 basic filtering to caller-provided tags.</summary>
    /// <param name="range">The basic language range.</param>
    /// <param name="tags">The caller-provided language tags.</param>
    /// <returns>Matching tags in caller order.</returns>
    public LanguageTagMatchResult[] FilterTags(string? range, string[] tags)
    {
        return LanguageLookupEngine.FilterTags(range, tags);
    }

    /// <summary>Applies RFC 4647 lookup to caller-provided tags.</summary>
    /// <param name="range">The basic language range.</param>
    /// <param name="tags">The caller-provided language tags.</param>
    /// <returns>The selected tag or a typed no-match or invalid outcome.</returns>
    public LanguageTagMatchResult LookupBestTag(string? range, string[] tags)
    {
        return LanguageLookupEngine.LookupBestTag(range, tags);
    }
}
