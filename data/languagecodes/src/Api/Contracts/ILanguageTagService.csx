namespace languagecodes;

/// <summary>Defines RFC 5646 parsing, IANA metadata access, and RFC 4647 language-tag matching.</summary>
public interface ILanguageTagService
{
    /// <summary>Parses every component of a well-formed RFC 5646 language tag.</summary>
    /// <param name="tag">The language tag to parse.</param>
    /// <returns>A defensive parsed-tag snapshot.</returns>
    /// <exception cref="InvalidLanguageTagException">The value does not satisfy the RFC 5646 grammar.</exception>
    LanguageTag ParseTag(string? tag);

    /// <summary>Resolves the primary or extended language component of a parsed tag to the ISO catalog.</summary>
    /// <param name="tag">The parsed language tag.</param>
    /// <returns>A result describing a unique, private-use, unresolved, or invalid language component.</returns>
    LanguageLookupResult ResolveTagLanguage(LanguageTag tag);

    /// <summary>Returns IANA registry metadata for a language-related subtag or complete tag.</summary>
    /// <param name="type">The registry record type.</param>
    /// <param name="value">The subtag or complete tag to query.</param>
    /// <returns>A defensive metadata snapshot, or null when no record exists.</returns>
    LanguageSubtagMetadata? GetLanguageSubtagMetadata(LanguageSubtagType type, string? value);

    /// <summary>Applies RFC 4647 basic filtering to caller-provided language tags.</summary>
    /// <param name="range">The basic language range.</param>
    /// <param name="tags">The caller-provided tags, whose relative order is preserved.</param>
    /// <returns>The matching input tags in caller order.</returns>
    LanguageTagMatchResult[] FilterTags(string? range, string[] tags);

    /// <summary>Applies RFC 4647 lookup to select the best caller-provided language tag.</summary>
    /// <param name="range">The language range.</param>
    /// <param name="tags">The caller-provided tags, whose relative order breaks equal matches.</param>
    /// <returns>The selected tag and index, or a typed no-match or invalid-range outcome.</returns>
    LanguageTagMatchResult LookupBestTag(string? range, string[] tags);
}
