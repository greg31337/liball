namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontNameValue"/>.</summary>
public interface IFontNameValue
{
    /// <summary>Gets the decoded name text.</summary>
    /// <returns>The decoded text.</returns>
    string GetValue();

    /// <summary>Gets the resolved BCP 47 language tag.</summary>
    /// <returns>The language tag, or null when it is unknown.</returns>
    string? GetLanguageTag();

    /// <summary>Gets the complete source record selected for this value.</summary>
    /// <returns>The selected name record.</returns>
    FontNameRecord GetRecord();

    /// <summary>Gets the RFC 4647 language range that produced this selection.</summary>
    /// <returns>The matched range, or null for first-record selection.</returns>
    string? GetMatchedLanguageRange();

    /// <summary>Gets the fallback category used to select this name.</summary>
    /// <returns>The match category.</returns>
    FontNameMatchKind GetMatchKind();
}
