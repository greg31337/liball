namespace ttf;

/// <summary>Represents one decoded and selected localized font name.</summary>
public class FontNameValue : IFontNameValue
{
    FontNameRecord record;
    string? matchedLanguageRange;
    FontNameMatchKind matchKind;

    internal FontNameValue(
        FontNameRecord record,
        string? matchedLanguageRange,
        FontNameMatchKind matchKind)
    {
        if (record == null)
        {
            throw new System.ArgumentNullException("record");
        }

        this.record = record;
        this.matchedLanguageRange = matchedLanguageRange;
        this.matchKind = matchKind;
    }

    /// <summary>Gets the decoded name text.</summary>
    /// <returns>The decoded text.</returns>
    public string GetValue() { return record.GetDecodedValue(); }

    /// <summary>Gets the resolved BCP 47 language tag.</summary>
    /// <returns>The language tag, or null when it is unknown.</returns>
    public string? GetLanguageTag() { return record.GetLanguageTag(); }

    /// <summary>Gets the complete source record selected for this value.</summary>
    /// <returns>The selected name record.</returns>
    public FontNameRecord GetRecord() { return record; }

    /// <summary>Gets the RFC 4647 language range that produced this selection.</summary>
    /// <returns>The matched range, or null for first-record selection.</returns>
    public string? GetMatchedLanguageRange() { return matchedLanguageRange; }

    /// <summary>Gets the fallback category used to select this name.</summary>
    /// <returns>The match category.</returns>
    public FontNameMatchKind GetMatchKind() { return matchKind; }
}
