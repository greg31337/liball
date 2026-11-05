namespace ttf;

/// <summary>Describes how a localized font name was selected.</summary>
public enum FontNameMatchKind
{
    /// <summary>The selected record exactly matches the requested language.</summary>
    ExactLanguage = 0,

    /// <summary>The selected record matches a parent or compatible language.</summary>
    ParentLanguage = 1,

    /// <summary>An English record was selected as a fallback.</summary>
    EnglishFallback = 2,

    /// <summary>The first valid record was selected.</summary>
    FirstValidRecord = 3
}
