namespace languagecodes;

/// <summary>Defines enumeration, lookup, history, and relationship operations over the ISO language catalog.</summary>
public interface ILanguageCatalog
{
    /// <summary>Returns every active reconciled ISO language entity in stable code order.</summary>
    /// <returns>A defensive snapshot of all active entries.</returns>
    LanguageCodeEntry[] GetAllEntries();

    /// <summary>Returns entries belonging to one ISO 639 set in stable code order.</summary>
    /// <param name="set">The required ISO 639 set.</param>
    /// <returns>A defensive snapshot of matching entries.</returns>
    LanguageCodeEntry[] GetEntriesBySet(Iso639Set set);

    /// <summary>Returns entries having one language scope in stable code order.</summary>
    /// <param name="scope">The required language scope.</param>
    /// <returns>A defensive snapshot of matching entries.</returns>
    LanguageCodeEntry[] GetEntriesByScope(LanguageScope scope);

    /// <summary>Returns entries having one language type in stable code order.</summary>
    /// <param name="languageType">The required language type.</param>
    /// <returns>A defensive snapshot of matching entries.</returns>
    LanguageCodeEntry[] GetEntriesByType(LanguageType languageType);

    /// <summary>Looks up a code using an explicit code form and caller-selected policies.</summary>
    /// <param name="code">The code to resolve.</param>
    /// <param name="form">The code form to require, or <see cref="LanguageCodeForm.Unknown"/> to recognize any form.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>A result describing a found, retired, reserved, invalid, or absent code.</returns>
    LanguageLookupResult LookupCode(string? code, LanguageCodeForm form, LanguageLookupOptions options);

    /// <summary>Requires one language entity for a code using an explicit code form.</summary>
    /// <param name="code">The code to resolve.</param>
    /// <param name="form">The code form to require, or <see cref="LanguageCodeForm.Unknown"/> to recognize any form.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>The uniquely resolved language entry.</returns>
    /// <exception cref="InvalidLanguageCodeException">The code is lexically invalid or disallowed by the options.</exception>
    /// <exception cref="LanguageCodeNotFoundException">No assigned language entity resolves from the code.</exception>
    LanguageCodeEntry RequireCode(string? code, LanguageCodeForm form, LanguageLookupOptions options);

    /// <summary>Looks up an exact or normalized language name.</summary>
    /// <param name="name">The language name to resolve.</param>
    /// <param name="options">The lookup and name-normalization policies.</param>
    /// <returns>A result retaining every candidate when the name is ambiguous.</returns>
    LanguageLookupResult LookupName(string? name, LanguageLookupOptions options);

    /// <summary>Searches language names using the requested exact, prefix, or contains mode.</summary>
    /// <param name="query">The name query.</param>
    /// <param name="options">The search, normalization, filtering, and limit policies.</param>
    /// <returns>Deterministically ranked matching candidates.</returns>
    LanguageMatch[] SearchNames(string? query, LanguageLookupOptions options);

    /// <summary>Resolves a code, alias, retired code, or name using deterministic code-first precedence.</summary>
    /// <param name="value">The code or name to resolve.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>A result retaining ambiguity and the matched field.</returns>
    LanguageLookupResult ResolveValue(string? value, LanguageLookupOptions options);

    /// <summary>Requires one entity from a code, alias, retired code, or name.</summary>
    /// <param name="value">The code or name to resolve.</param>
    /// <param name="options">The lookup policies.</param>
    /// <returns>The uniquely resolved language entry.</returns>
    /// <exception cref="InvalidLanguageCodeException">A code-shaped value is lexically invalid or disallowed.</exception>
    /// <exception cref="LanguageCodeNotFoundException">No entity resolves from the value.</exception>
    /// <exception cref="AmbiguousLanguageNameException">The value matches more than one language entity.</exception>
    LanguageCodeEntry RequireValue(string? value, LanguageLookupOptions options);

    /// <summary>Returns the direct macrolanguage for an individual-language member.</summary>
    /// <param name="memberCode">The member language code.</param>
    /// <param name="includeRetiredRelationships">Whether retired macrolanguage mappings participate.</param>
    /// <returns>The macrolanguage entry, or null when no mapping exists.</returns>
    LanguageCodeEntry? GetMacrolanguageForMember(string? memberCode, bool includeRetiredRelationships);

    /// <summary>Returns every direct member of a macrolanguage.</summary>
    /// <param name="macrolanguageCode">The macrolanguage code.</param>
    /// <param name="includeRetiredRelationships">Whether retired macrolanguage mappings participate.</param>
    /// <returns>A stable defensive snapshot of member entries.</returns>
    LanguageCodeEntry[] GetMacrolanguageMembers(string? macrolanguageCode, bool includeRetiredRelationships);

    /// <summary>Returns direct or transitive ISO 639 Set 5 parents for a group code.</summary>
    /// <param name="groupCode">The ISO 639 Set 5 child code.</param>
    /// <param name="transitive">Whether all ancestors rather than only direct parents are returned.</param>
    /// <returns>A stable defensive snapshot of parent entries.</returns>
    LanguageCodeEntry[] GetGroupParents(string? groupCode, bool transitive);

    /// <summary>Returns direct or transitive ISO 639 Set 5 children for a group code.</summary>
    /// <param name="groupCode">The ISO 639 Set 5 parent code.</param>
    /// <param name="transitive">Whether all descendants rather than only direct children are returned.</param>
    /// <returns>A stable defensive snapshot of child entries.</returns>
    LanguageCodeEntry[] GetGroupChildren(string? groupCode, bool transitive);

    /// <summary>Returns authoritative retirement metadata for an ISO 639 Set 3 code.</summary>
    /// <param name="code">The retired code to query.</param>
    /// <returns>A defensive retirement snapshot, or null when the code has no retirement record.</returns>
    LanguageRetirement? GetRetirement(string? code);
}
