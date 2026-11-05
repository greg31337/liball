namespace icc;

/// <summary>Represents one ICC dictionary name-value record.</summary>
public class IccMetadataEntry
{
    /// <summary>Gets or sets the metadata name.</summary>
    public string Name;
    /// <summary>Gets or sets the metadata value.</summary>
    public string Value;
    /// <summary>Gets or sets whether the optional value position is defined.</summary>
    public bool HasValue;
    /// <summary>Gets or sets whether the name appears in the ICC dictType metadata registry.</summary>
    public bool IsRegisteredName;
    /// <summary>Gets or sets localized display names.</summary>
    public IccLocalizedString[] DisplayNames;
    /// <summary>Gets or sets whether the localized display-name element position is defined.</summary>
    public bool HasDisplayNameElement;
    /// <summary>Gets or sets localized display values.</summary>
    public IccLocalizedString[] DisplayValues;
    /// <summary>Gets or sets whether the localized display-value element position is defined.</summary>
    public bool HasDisplayValueElement;

    /// <summary>Initializes an empty metadata entry.</summary>
    public IccMetadataEntry()
    {
        Name = string.Empty;
        Value = string.Empty;
        HasValue = false;
        IsRegisteredName = false;
        DisplayNames = new IccLocalizedString[0];
        HasDisplayNameElement = false;
        DisplayValues = new IccLocalizedString[0];
        HasDisplayValueElement = false;
    }
}
